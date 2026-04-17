using QueryServiceHost.Components;
using QueryServiceHost.Services;
using QueryServiceHost.State;
using Radzen;
using UKHO.Aspire.Configuration;
using UKHO.Search.Configuration;
using UKHO.Search.Infrastructure.Query.Injection;
using UKHO.Search.ServiceDefaults;

namespace QueryServiceHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create the Query host application builder and attach the repository-standard service defaults before host-specific services are added.
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            // Reduce noisy framework logs so developer diagnostics remain focused on the query UI and authentication flow.
            builder.Logging.AddFilter("Microsoft.AspNetCore.Components.Server", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Components.RenderTree", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning);

            // Load shared configuration and external service clients required by the Query host runtime.
            builder.AddConfiguration(ServiceConfiguration.ServiceGroupName, ServiceNames.Configuration);
            builder.AddElasticsearchClient(ServiceNames.ElasticSearch);

            // Register the query infrastructure and the interactive UI dependencies needed by the Blazor host.
            builder.Services.AddQueryServices();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                   .AddInteractiveServerComponents();

            builder.Services.AddServerSideBlazor().AddCircuitOptions(options =>
            {
                options.DetailedErrors = builder.Environment.IsDevelopment();
            });

            builder.Services.AddRadzenComponents();
            builder.Services.AddRadzenQueryStringThemeService();
            builder.Services.AddLocalization();

            // Reuse the shared browser-host authentication composition so QueryServiceHost follows the same Keycloak flow as the other protected browser hosts.
            builder.Services.AddKeycloakBrowserHostAuthentication("search-workbench", "query");

            // Register the host adapter that bridges the UI onto the repository-owned query planning and Elasticsearch execution pipeline.
            builder.Services.AddSingleton<IQueryUiSearchClient, QueryUiSearchClient>();
            builder.Services.AddScoped<QueryUiState>();

            // Build the application only after all UI, infrastructure, and authentication services have been registered.
            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            else
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            // Expose the shared login and logout lifecycle routes before the authenticated UI pipeline begins.
            app.MapKeycloakBrowserHostAuthenticationEndpoints();

            // Restore the authenticated principal before authorization evaluates the protected Query UI routes.
            app.UseAuthentication();
            app.UseAuthorization();

            // Serve static assets before the interactive component endpoints so the protected shell can load its client resources normally.
            app.MapStaticAssets();

            // Map the protected interactive Query UI after the authentication middleware has been added to the request pipeline.
            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}