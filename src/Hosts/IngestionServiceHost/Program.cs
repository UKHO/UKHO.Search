using IngestionServiceHost.Components;
using Radzen;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.Aspire.Configuration;
using UKHO.Aspire.Configuration.Remote;
using UKHO.Search.Configuration;
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

            var ingestionMode = IngestionModeParser.Parse(Environment.GetEnvironmentVariable("ingestionmode"));
            builder.Services.AddSingleton(new IngestionModeOptions(ingestionMode));

            // Reduce noisy Azure SDK client logs (e.g. Azure Storage Queue polling).
            builder.Logging.AddFilter("Azure", LogLevel.Warning);
            builder.Logging.AddFilter("Azure.Core", LogLevel.Warning);
            builder.Logging.AddFilter("Azure.Storage", LogLevel.Warning);
            builder.Logging.AddFilter("Azure.Storage.Queues", LogLevel.Warning);

            builder.AddConfiguration(ServiceConfiguration.ServiceGroupName, ServiceNames.Configuration);

            builder.AddElasticsearchClient(ServiceNames.ElasticSearch);
            builder.AddAzureQueueServiceClient(ServiceNames.Queues);
            builder.AddAzureBlobServiceClient(ServiceNames.Blobs);

            builder.Services.AddIngestionServices();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                   .AddInteractiveServerComponents();

            builder.Services.AddRadzenComponents();
            builder.Services.AddRadzenQueryStringThemeService();
            builder.Services.AddLocalization();

            builder.Services.AddSingleton<FileShareReadOnlyClientFactory>();

            builder.Services.AddSingleton<IFileShareReadOnlyClient>(sp =>
            {
                var externalServiceRegistry = sp.GetRequiredService<IExternalServiceRegistry>();
                var fileShareEndpoint = externalServiceRegistry.GetServiceEndpoint("FileShare"); // Using the tag "FileShare" to retrieve the endpoint defined in external-services.json

                var baseAddress = fileShareEndpoint.Uri;

                // Local emulator has no auth configured, so pass an empty token.
                return sp.GetRequiredService<FileShareReadOnlyClientFactory>()
                         .CreateClient(baseAddress.ToString(), string.Empty);
            });

            var app = builder.Build();

            app.Logger.LogInformation("Ingestion mode resolved as '{IngestionMode}'.", ingestionMode);

            // The ingestion bootstrap/startup path is owned by IngestionPipelineHostedService.
            // Do not eagerly call IBootstrapService here, otherwise startup work and logs run twice.

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