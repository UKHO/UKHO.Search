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
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            builder.Logging.AddFilter("Microsoft.AspNetCore.Components.Server", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Components.RenderTree", LogLevel.Warning);
            builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning);

            builder.AddConfiguration(ServiceConfiguration.ServiceGroupName, ServiceNames.Configuration);

            builder.AddElasticsearchClient(ServiceNames.ElasticSearch);

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

            builder.Services.AddOptions<StubQueryUiSearchClientOptions>().BindConfiguration("QueryUi:StubSearch");
            builder.Services.AddSingleton<IQueryUiSearchClient, StubQueryUiSearchClient>();
            builder.Services.AddScoped<QueryUiState>();

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

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
               .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}