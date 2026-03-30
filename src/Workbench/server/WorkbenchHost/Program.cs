using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Radzen;
using UKHO.Search.ServiceDefaults;
using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Infrastructure;
using UKHO.Workbench.Infrastructure.Modules;
using UKHO.Workbench.Modules;
using UKHO.Workbench.Services.Shell;
using UKHO.Workbench.Tools;
using UKHO.Workbench.WorkbenchShell;
using WorkbenchHost.Components;
using WorkbenchHost.Components.Tools;
using WorkbenchHost.Components.WorkbenchShell;
using WorkbenchHost.Extensions;
using WorkbenchHost.Services;

namespace WorkbenchHost
{
    /// <summary>
    ///     Hosts the Workbench Blazor shell and configures its supporting infrastructure.
    /// </summary>
    public class Program
    {
        /// <summary>
        ///     Builds and runs the Workbench host application.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the host process.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var moduleContributionRegistry = new WorkbenchContributionRegistry();
            var startupNotificationStore = new WorkbenchStartupNotificationStore();

            // Apply the shared service-default configuration used across the repository's hosts.
            builder.AddServiceDefaults();

            // Register the workbench infrastructure services required by the host.
            builder.Services.AddWorkbenchInfrastructure();
            builder.Services.AddSingleton<IWorkbenchContributionRegistry>(moduleContributionRegistry);
            builder.Services.AddSingleton(moduleContributionRegistry);
            builder.Services.AddSingleton(startupNotificationStore);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                   .AddInteractiveServerComponents()
                   .AddHubOptions(options => options.MaximumReceiveMessageSize = 10 * 1024 * 1024);

            // Register Radzen services so the host can render the shell using Radzen assets.
            builder.Services.AddRadzenComponents();

            builder.Services.AddRadzenCookieThemeService(options =>
            {
                options.Name = "workbench-theme";
                options.Duration = TimeSpan.FromDays(365);
            });

            builder.Services.AddHttpClient();

            // Provide the current HTTP context and authorization helpers required by the host.
            builder.Services.AddHttpContextAccessor().AddTransient<AuthorizationHandler>();

            // Normalize realm-role claims before authorization runs against the current principal.
            builder.Services.AddTransient<IClaimsTransformation, KeycloakRealmRoleClaimsTransformation>();

            var oidcScheme = OpenIdConnectDefaults.AuthenticationScheme;

            // Configure cookie-backed OpenID Connect authentication against the shared Keycloak realm.
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = oidcScheme;
            })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddKeycloakOpenIdConnect("keycloak", "ukho-search", oidcScheme, options =>
                {
                    options.ClientId = "search-workbench";
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    options.SaveTokens = true;
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                });

            // Flow the authenticated user through the Blazor component tree.
            builder.Services.AddCascadingAuthenticationState();

            // Require authenticated users by default for the workbench surface.
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // Discover and register module-provided services and tools before the host finalizes the service provider.
            ConfigureWorkbenchModules(builder, moduleContributionRegistry, startupNotificationStore);

            var app = builder.Build();

            // Register the host-owned exemplar tool, any discovered module tools, and select the initial tool for the shell.
            BootstrapWorkbenchShell(app.Services);

            var forwardingOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };

            forwardingOptions.KnownIPNetworks.Clear();
            forwardingOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(forwardingOptions);

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found");
            app.UseHttpsRedirection();
            app.UseAntiforgery();


            app.MapStaticAssets();
            app.UseAntiforgery();

            // Expose login and logout endpoints before the authenticated UI pipeline executes.
            app.MapLoginAndLogout();

            // Authenticate requests before enforcing the fallback authorization policy.
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode();

            app.Run();
        }

        /// <summary>
        /// Reads the host-owned module configuration, discovers enabled module assemblies, and invokes bounded module registration.
        /// </summary>
        /// <param name="builder">The web application builder that still owns the mutable service collection.</param>
        /// <param name="moduleContributionRegistry">The bounded contribution registry that collects module-provided tools.</param>
        /// <param name="startupNotificationStore">The store that buffers user-safe startup notifications until the shell is interactive.</param>
        private static void ConfigureWorkbenchModules(
            WebApplicationBuilder builder,
            WorkbenchContributionRegistry moduleContributionRegistry,
            WorkbenchStartupNotificationStore startupNotificationStore)
        {
            // Module discovery must complete before DI finalization so modules can register services through the bounded startup contract.
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(moduleContributionRegistry);
            ArgumentNullException.ThrowIfNull(startupNotificationStore);

            using var startupLoggerFactory = CreateStartupLoggerFactory(builder.Configuration);
            var startupLogger = startupLoggerFactory.CreateLogger(typeof(Program));

            try
            {
                var configurationPath = Path.Combine(builder.Environment.ContentRootPath, "modules.json");
                var configurationDirectoryPath = Path.GetDirectoryName(configurationPath)
                    ?? throw new InvalidOperationException("The Workbench module configuration directory could not be resolved.");

                var configurationReader = new ModulesConfigurationReader(startupLoggerFactory.CreateLogger<ModulesConfigurationReader>());
                var assemblyScanner = new ModuleAssemblyScanner(startupLoggerFactory.CreateLogger<ModuleAssemblyScanner>());
                var moduleLoader = new ModuleLoader(startupLoggerFactory.CreateLogger<ModuleLoader>());

                // The host reads the module configuration first so probe roots and per-module enablement remain host-controlled.
                var options = configurationReader.Read(configurationPath);

                // Discovery is constrained to approved probe roots and naming conventions before any assembly is loaded.
                var discoveredAssemblies = assemblyScanner.Scan(options, configurationDirectoryPath);

                // Loaded modules can now register services and static tool contributions through the bounded registration contract.
                var loadResult = moduleLoader.LoadModules(discoveredAssemblies, builder.Services, moduleContributionRegistry);

                startupLogger.LogInformation(
                    "Workbench module startup completed with {LoadedCount} loaded modules and {FailureCount} failures.",
                    loadResult.LoadedModules.Count,
                    loadResult.Failures.Count);

                foreach (var loadedModule in loadResult.LoadedModules)
                {
                    // Successful module registrations are logged with the originating probe root so startup diagnostics can trace where each module came from.
                    startupLogger.LogInformation(
                        "Workbench module {ModuleId} loaded from {AssemblyPath} discovered under probe root {ProbeRoot}.",
                        loadedModule.Metadata.Id,
                        loadedModule.AssemblyPath,
                        loadedModule.ProbeRoot);
                }

                foreach (var failure in loadResult.Failures)
                {
                    // User-facing notifications intentionally stay high level while technical detail remains in structured logs.
                    startupLogger.LogWarning(
                        "Workbench module {ModuleId} failed during stage {FailureStage} from {AssemblyPath} discovered under probe root {ProbeRoot}. Message: {FailureMessage}",
                        failure.ModuleId,
                        failure.FailureStage,
                        failure.AssemblyPath,
                        failure.ProbeRoot,
                        failure.Message);

                    startupNotificationStore.Add(
                        NotificationSeverity.Warning,
                        "Workbench module unavailable",
                        $"The module '{failure.ModuleId}' could not be loaded during the {failure.FailureStage} stage and will be skipped for this session.");
                }
            }
            catch (Exception exception)
            {
                // A host-level configuration or discovery failure should not prevent the shell from starting with host-owned tools.
                startupLogger.LogError(exception, "Workbench module startup failed before DI finalization.");
                startupNotificationStore.Add(
                    NotificationSeverity.Warning,
                    "Workbench module startup failed",
                    "One or more configured Workbench modules could not be loaded during startup. The shell will continue with host-provided tools only.");
            }
        }

        /// <summary>
        /// Creates a temporary logger factory that mirrors the configured host logging sources closely enough for startup diagnostics.
        /// </summary>
        /// <param name="configuration">The host configuration that may contain logging settings.</param>
        /// <returns>A logger factory that can be used before the application service provider is finalized.</returns>
        private static ILoggerFactory CreateStartupLoggerFactory(IConfiguration configuration)
        {
            // Startup discovery runs before the host service provider exists, so a temporary logger factory is used for early diagnostics.
            ArgumentNullException.ThrowIfNull(configuration);

            return LoggerFactory.Create(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
            });
        }

        /// <summary>
        /// Registers and activates the host-owned bootstrap tool set used by the initial Workbench shell slice.
        /// </summary>
        /// <param name="services">The application service provider used to resolve the singleton shell manager.</param>
        private static void BootstrapWorkbenchShell(IServiceProvider services)
        {
            // Host startup owns shell bootstrap so host tools and module tools both flow through one activation path.
            ArgumentNullException.ThrowIfNull(services);

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(typeof(Program));
            var shellManager = services.GetRequiredService<WorkbenchShellManager>();
            var contributionRegistry = services.GetRequiredService<WorkbenchContributionRegistry>();
            var startupNotificationStore = services.GetRequiredService<WorkbenchStartupNotificationStore>();

            try
            {
                // The bootstrap explorer always exposes the host-owned overview tool so the shell remains useful even when module loading fails.
                shellManager.RegisterTool(
                    new ToolDefinition(
                        WorkbenchHostShellDefaults.OverviewToolId,
                        "Workbench overview",
                        typeof(WorkbenchOverviewTool),
                        WorkbenchHostShellDefaults.BootstrapExplorerId,
                        "dashboard",
                        "Shows the first host-owned Workbench tool."));

                // The host owns the primary explorer shell surface and its baseline command/menu/toolbar/status contributions.
                shellManager.RegisterExplorer(new ExplorerContribution(WorkbenchHostShellDefaults.BootstrapExplorerId, WorkbenchHostShellDefaults.BootstrapExplorerDisplayName, "dashboard_customize", 0));
                shellManager.RegisterExplorerSection(new ExplorerSectionContribution(WorkbenchHostShellDefaults.HostToolsSectionId, WorkbenchHostShellDefaults.BootstrapExplorerId, "Host tools", 100));
                shellManager.RegisterCommand(
                    new CommandContribution(
                        WorkbenchHostShellDefaults.OverviewCommandId,
                        "Open Workbench overview",
                        CommandScope.Host,
                        icon: "dashboard",
                        description: "Opens the host-owned overview tool.",
                        activationTarget: ActivationTarget.CreateToolSurfaceTarget(WorkbenchHostShellDefaults.OverviewToolId)));
                shellManager.RegisterExplorerItem(
                    new ExplorerItem(
                        "explorer.item.host.overview",
                        WorkbenchHostShellDefaults.BootstrapExplorerId,
                        WorkbenchHostShellDefaults.HostToolsSectionId,
                        "Workbench overview",
                        WorkbenchHostShellDefaults.OverviewCommandId,
                        ActivationTarget.CreateToolSurfaceTarget(WorkbenchHostShellDefaults.OverviewToolId),
                        "dashboard",
                        "Host-owned exemplar tool that explains the current Workbench slice.",
                        100));
                shellManager.RegisterMenu(new MenuContribution(WorkbenchHostShellDefaults.OverviewMenuId, "Overview", WorkbenchHostShellDefaults.OverviewCommandId, icon: "dashboard", order: 100));
                shellManager.RegisterToolbar(new ToolbarContribution(WorkbenchHostShellDefaults.OverviewToolbarId, "Overview", WorkbenchHostShellDefaults.OverviewCommandId, icon: "dashboard", order: 100));
                shellManager.RegisterStatusBar(new StatusBarContribution(WorkbenchHostShellDefaults.HostReadyStatusId, "Workbench shell ready", icon: "check_circle", order: 100));

                // Module-contributed tools and shell surfaces use the same registration path as host-owned contributions.
                foreach (var moduleToolDefinition in contributionRegistry.ToolDefinitions)
                {
                    shellManager.RegisterTool(moduleToolDefinition);
                }

                foreach (var commandContribution in contributionRegistry.CommandContributions)
                {
                    shellManager.RegisterCommand(commandContribution);
                }

                foreach (var explorerContribution in contributionRegistry.ExplorerContributions)
                {
                    shellManager.RegisterExplorer(explorerContribution);
                }

                foreach (var explorerSectionContribution in contributionRegistry.ExplorerSectionContributions)
                {
                    shellManager.RegisterExplorerSection(explorerSectionContribution);
                }

                foreach (var explorerItem in contributionRegistry.ExplorerItems)
                {
                    shellManager.RegisterExplorerItem(explorerItem);
                }

                foreach (var menuContribution in contributionRegistry.MenuContributions)
                {
                    shellManager.RegisterMenu(menuContribution);
                }

                foreach (var toolbarContribution in contributionRegistry.ToolbarContributions)
                {
                    shellManager.RegisterToolbar(toolbarContribution);
                }

                foreach (var statusBarContribution in contributionRegistry.StatusBarContributions)
                {
                    shellManager.RegisterStatusBar(statusBarContribution);
                }

                shellManager.SetActiveExplorer(WorkbenchHostShellDefaults.BootstrapExplorerId);

                // When a module contributes a tool, the first contributed tool becomes the initial focus to prove end-to-end discovery and activation.
                var initialToolId = contributionRegistry.ToolDefinitions.FirstOrDefault()?.Id
                    ?? WorkbenchHostShellDefaults.OverviewToolId;

                shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(initialToolId));
            }
            catch (Exception exception)
            {
                // Startup failures are logged with enough detail for diagnosis while allowing the host to continue and show any resulting empty shell state.
                logger.LogError(exception, "Workbench shell bootstrap failed while registering the initial host-owned tool slice.");
                startupNotificationStore.Add(
                    NotificationSeverity.Warning,
                    "Workbench shell bootstrap failed",
                    "The Workbench shell could not complete startup registration for one or more tools. Check the application logs for more detail.");
            }
        }
    }
}