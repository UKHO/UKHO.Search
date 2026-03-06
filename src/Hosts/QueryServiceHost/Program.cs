using QueryServiceHost.Components;
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

            builder.AddConfiguration(ServiceConfiguration.ServiceGroupName, ServiceNames.Configuration);

            builder.AddElasticsearchClient(ServiceNames.ElasticSearch);

            builder.Services.AddQueryServices();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddRadzenComponents();
            builder.Services.AddRadzenQueryStringThemeService();
            builder.Services.AddLocalization();

            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
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