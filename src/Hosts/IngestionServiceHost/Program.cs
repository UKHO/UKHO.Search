using IngestionServiceHost.Components;
using Radzen;
using UKHO.Aspire.Configuration;
using UKHO.Search.Configuration;
using UKHO.Search.Infrastructure.Ingestion.Bootstrap;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.ServiceDefaults;

namespace IngestionServiceHost
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            builder.AddConfiguration(ServiceConfiguration.ServiceGroupName, ServiceNames.Configuration);

            builder.AddElasticsearchClient(ServiceNames.ElasticSearch);
            builder.AddAzureQueueServiceClient(ServiceNames.Queues);

            builder.Services.AddIngestionServices();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddRadzenComponents();
            builder.Services.AddRadzenQueryStringThemeService();
            builder.Services.AddLocalization();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var bootstrapService = scope.ServiceProvider.GetRequiredService<IBootstrapService>();
                bootstrapService.BootstrapAsync().GetAwaiter().GetResult();
            }

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