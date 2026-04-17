using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Radzen;
using UKHO.Search.ServiceDefaults;
using UKHO.Workbench.Commands;
using UKHO.Workbench.Explorers;
using UKHO.Workbench.Infrastructure;
using UKHO.Workbench.Infrastructure.Modules;
using UKHO.Workbench.Modules;
using UKHO.Workbench.Output;
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

            // Consume the shared browser-host authentication composition so Workbench and future hosts stay aligned.
            builder.Services.AddKeycloakBrowserHostAuthentication("search-workbench", "workbench");

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

            // Expose the shared authentication lifecycle endpoints before the authenticated UI pipeline executes.
            app.MapKeycloakBrowserHostAuthenticationEndpoints();

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

                // The output stream becomes the user-visible historical trace for startup work, so the module loader writes its completion summary into the buffered startup output.
                startupNotificationStore.AddOutput(
                    OutputLevel.Debug,
                    "Module loader",
                    "Workbench module startup completed.",
                    $"Loaded {loadResult.LoadedModules.Count} module(s) with {loadResult.Failures.Count} failure(s) during startup.");

                foreach (var loadedModule in loadResult.LoadedModules)
                {
                    // Successful module registrations are logged with the originating probe root so startup diagnostics can trace where each module came from.
                    startupLogger.LogInformation(
                        "Workbench module {ModuleId} loaded from {AssemblyPath} discovered under probe root {ProbeRoot}.",
                        loadedModule.Metadata.Id,
                        loadedModule.AssemblyPath,
                        loadedModule.ProbeRoot);

                    // Successful module discovery is also mirrored into the shell output stream so the output panel becomes the primary startup trace.
                    startupNotificationStore.AddOutput(
                        OutputLevel.Debug,
                        "Module loader",
                        $"Loaded Workbench module '{loadedModule.Metadata.Id}'.",
                        $"Assembly path: {loadedModule.AssemblyPath}\nProbe root: {loadedModule.ProbeRoot}");
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

                    // Module-load failures remain visible in the shell-owned output stream even when the shell continues running with the remaining modules.
                    startupNotificationStore.AddOutput(
                        OutputLevel.Warning,
                        "Module loader",
                        $"The Workbench module '{failure.ModuleId}' could not be loaded.",
                        $"Stage: {failure.FailureStage}\nAssembly path: {failure.AssemblyPath}\nProbe root: {failure.ProbeRoot}\nMessage: {failure.Message}");

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
                startupNotificationStore.AddOutput(
                    OutputLevel.Error,
                    "Module loader",
                    "Workbench module startup failed.",
                    "Module discovery failed before dependency injection finalization. The shell will continue with host-provided tools only. Check the application logs for more detail.");
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
            var outputService = services.GetRequiredService<IWorkbenchOutputService>();
            var contributionRegistry = services.GetRequiredService<WorkbenchContributionRegistry>();
            var startupNotificationStore = services.GetRequiredService<WorkbenchStartupNotificationStore>();

            // Buffered startup output from the pre-DI module discovery phase is replayed first so later bootstrap entries continue the same chronological shell trace.
            ReplayBufferedStartupOutput(outputService, startupNotificationStore);

            try
            {
                // The host-owned overview tool remains available even when the activity rail is primarily driven by module explorers.
                shellManager.RegisterTool(
                    new ToolDefinition(
                        WorkbenchHostShellDefaults.OverviewToolId,
                        "Workbench overview",
                        typeof(WorkbenchOverviewTool),
                        WorkbenchHostShellDefaults.FallbackExplorerId,
                        "dashboard",
                        "Shows the first host-owned Workbench tool."));

                var hasModuleExplorers = contributionRegistry.ExplorerContributions.Count > 0;

                // The host only contributes its fallback explorer when no module explorer is available to populate the activity rail.
                if (!hasModuleExplorers)
                {
                    shellManager.RegisterExplorer(new ExplorerContribution(WorkbenchHostShellDefaults.FallbackExplorerId, WorkbenchHostShellDefaults.FallbackExplorerDisplayName, "dashboard_customize", 0));
                    shellManager.RegisterExplorerSection(new ExplorerSectionContribution(WorkbenchHostShellDefaults.HostToolsSectionId, WorkbenchHostShellDefaults.FallbackExplorerId, "Host tools", 100));
                }

                // Host menu, toolbar, and status contributions remain available regardless of whether the rail is module-driven.
                shellManager.RegisterCommand(
                    new CommandContribution(
                        WorkbenchHostShellDefaults.OverviewCommandId,
                        "Open Workbench overview",
                        CommandScope.Host,
                        icon: "dashboard",
                        description: "Opens the host-owned overview tool.",
                        activationTarget: ActivationTarget.CreateToolSurfaceTarget(
                            WorkbenchHostShellDefaults.OverviewToolId,
                            initialTitle: "Workbench overview",
                            initialIcon: "dashboard")));
                if (!hasModuleExplorers)
                {
                    shellManager.RegisterExplorerItem(
                        new ExplorerItem(
                            "explorer.item.host.overview",
                            WorkbenchHostShellDefaults.FallbackExplorerId,
                            WorkbenchHostShellDefaults.HostToolsSectionId,
                            "Workbench overview",
                            WorkbenchHostShellDefaults.OverviewCommandId,
                            ActivationTarget.CreateToolSurfaceTarget(
                                WorkbenchHostShellDefaults.OverviewToolId,
                                initialTitle: "Workbench overview",
                                initialIcon: "dashboard"),
                            "dashboard",
                            "Host-owned exemplar tool that explains the current Workbench slice.",
                            100));
                }

                // The baseline menu bar must remain useful even before any active tool contributes runtime menus.
                RegisterMinimumShellMenus(shellManager);

                shellManager.RegisterExplorerToolbar(new ExplorerToolbarContribution(WorkbenchHostShellDefaults.OverviewExplorerToolbarId, "Home", WorkbenchHostShellDefaults.OverviewCommandId, icon: "dashboard", order: 100));

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

                foreach (var explorerToolbarContribution in contributionRegistry.ExplorerToolbarContributions)
                {
                    shellManager.RegisterExplorerToolbar(explorerToolbarContribution);
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

                var initialExplorerId = shellManager.Explorers.FirstOrDefault()?.Id;

                if (!string.IsNullOrWhiteSpace(initialExplorerId))
                {
                    shellManager.SetActiveExplorer(initialExplorerId);
                }

                // When a module contributes a tool, the first contributed tool becomes the initial focus to prove end-to-end discovery and activation.
                var initialToolId = contributionRegistry.ToolDefinitions.FirstOrDefault()?.Id
                    ?? WorkbenchHostShellDefaults.OverviewToolId;

                shellManager.ActivateTool(ActivationTarget.CreateToolSurfaceTarget(initialToolId));

                // Shell bootstrap now projects a real host-owned startup event into the shared output stream so the panel can show meaningful session history immediately.
                outputService.Write(
                    OutputLevel.Info,
                    "Shell",
                    "Workbench shell ready.",
                    $"The initial tool '{initialToolId}' was activated during host startup.");
            }
            catch (Exception exception)
            {
                // Startup failures are logged with enough detail for diagnosis while allowing the host to continue and show any resulting empty shell state.
                logger.LogError(exception, "Workbench shell bootstrap failed while registering the initial host-owned tool slice.");

                // The shared output stream mirrors the user-visible shell history while structured logs continue to carry the full diagnostic record.
                outputService.Write(
                    OutputLevel.Error,
                    "Shell",
                    "Workbench shell bootstrap failed.",
                    "The Workbench shell could not complete startup registration for one or more tools. Check the application logs for more detail.");

                outputService.Write(
                    OutputLevel.Warning,
                    "Notifications",
                    "Workbench shell bootstrap failed",
                    "The Workbench shell could not complete startup registration for one or more tools. Check the application logs for more detail.");

                startupNotificationStore.Add(
                    NotificationSeverity.Warning,
                    "Workbench shell bootstrap failed",
                    "The Workbench shell could not complete startup registration for one or more tools. Check the application logs for more detail.");
            }
        }

        /// <summary>
        /// Replays buffered startup output entries into the shared shell-wide output service.
        /// </summary>
        /// <param name="outputService">The shell-wide output service that should receive the buffered startup entries.</param>
        /// <param name="startupNotificationStore">The store that buffered startup output before the host service provider existed.</param>
        private static void ReplayBufferedStartupOutput(
            IWorkbenchOutputService outputService,
            WorkbenchStartupNotificationStore startupNotificationStore)
        {
            // Startup output must be replayed explicitly because module discovery runs before DI finalization and therefore before the singleton output service can be resolved.
            ArgumentNullException.ThrowIfNull(outputService);
            ArgumentNullException.ThrowIfNull(startupNotificationStore);

            foreach (var startupOutputEntry in startupNotificationStore.DequeueOutputEntries())
            {
                // The preserved output entry timestamps maintain startup chronology when the buffered events are appended after DI is available.
                outputService.Write(startupOutputEntry);
            }
        }

        /// <summary>
        /// Registers the minimum host-provided shell menu presence required to keep the restored menu bar visible and meaningful.
        /// </summary>
        /// <param name="shellManager">The shell manager that owns command and menu registration for the bootstrap shell.</param>
        private static void RegisterMinimumShellMenus(WorkbenchShellManager shellManager)
        {
            // The flat bootstrap menu model uses one placeholder command per minimum shell menu until richer grouped menu content is specified.
            ArgumentNullException.ThrowIfNull(shellManager);

            shellManager.RegisterCommand(
                new CommandContribution(
                    WorkbenchHostShellDefaults.EditCommandId,
                    "Edit",
                    CommandScope.Host,
                    executionHandler: CompleteShellMenuPlaceholderCommandAsync));
            shellManager.RegisterCommand(
                new CommandContribution(
                    WorkbenchHostShellDefaults.ViewCommandId,
                    "View",
                    CommandScope.Host,
                    executionHandler: CompleteShellMenuPlaceholderCommandAsync));
            shellManager.RegisterCommand(
                new CommandContribution(
                    WorkbenchHostShellDefaults.HelpCommandId,
                    "Help",
                    CommandScope.Host,
                    executionHandler: CompleteShellMenuPlaceholderCommandAsync));

            shellManager.RegisterMenu(new MenuContribution(WorkbenchHostShellDefaults.EditMenuId, "Edit", WorkbenchHostShellDefaults.EditCommandId, order: 200));
            shellManager.RegisterMenu(new MenuContribution(WorkbenchHostShellDefaults.ViewMenuId, "View", WorkbenchHostShellDefaults.ViewCommandId, order: 300));
            shellManager.RegisterMenu(new MenuContribution(WorkbenchHostShellDefaults.HelpMenuId, "Help", WorkbenchHostShellDefaults.HelpCommandId, order: 400));
        }

        /// <summary>
        /// Completes a temporary shell-menu command without changing the current Workbench state.
        /// </summary>
        /// <param name="activeToolContext">The currently active tool context, which is unused while the minimum shell menus are placeholders.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the shared command pipeline.</param>
        /// <returns>A completed task because the placeholder command intentionally performs no action yet.</returns>
        private static Task CompleteShellMenuPlaceholderCommandAsync(ToolContext? activeToolContext, CancellationToken cancellationToken)
        {
            // The placeholder menus must still execute through the shared command path even though their detailed behavior is deferred by specification.
            _ = activeToolContext;
            _ = cancellationToken;
            return Task.CompletedTask;
        }
    }
}