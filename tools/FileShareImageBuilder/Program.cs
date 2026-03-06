using FileShareImageBuilder.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using UKHO.ADDS.Clients.Common.Authentication;
using UKHO.ADDS.Clients.FileShareService.ReadOnly;
using UKHO.Search.Configuration;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace FileShareImageBuilder
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Logging.ClearProviders();
            builder.Logging.AddSimpleConsole(o => { o.TimestampFormat = "HH:mm:ss "; });
            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            builder.AddSqlServerClient(StorageNames.FileShareEmulatorDatabase);
            builder.AddAzureBlobServiceClient(ServiceNames.Blobs);

            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<FileShareReadOnlyClientFactory>();

            builder.Services.AddSingleton<IAuthenticationTokenProvider>(_ =>
            {
                var tenantId = ConfigurationReader.GetTenantId();
                var clientId = ConfigurationReader.GetClientId();

                if (string.IsNullOrWhiteSpace(tenantId))
                    throw new InvalidOperationException("Missing 'tenantId' in configuration.override.json.");

                if (string.IsNullOrWhiteSpace(clientId))
                    throw new InvalidOperationException("Missing 'clientId' in configuration.override.json.");

                var scopes = new[] { $"{clientId}/.default" };

                var app = PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                    .WithDefaultRedirectUri()
                    .Build();

                // Optional persistent cache (best-effort). If it fails, interactive auth still works.
                try
                {
                    MsalTokenCacheHelper.EnableSerialization(app.UserTokenCache);
                }
                catch
                {
                    // Ignore cache persistence failures.
                }

                return new MsalAuthenticationTokenProvider(app, scopes);
            });

            builder.Services.AddSingleton<IFileShareReadOnlyClient>(sp =>
            {
                var baseAddress = ConfigurationReader.GetRemoteServiceBaseAddress();
                var tokenProvider = sp.GetRequiredService<IAuthenticationTokenProvider>();
                return sp.GetRequiredService<FileShareReadOnlyClientFactory>().CreateClient(baseAddress, tokenProvider);
            });

            builder.Services.AddSingleton<MetadataImporter>();
            builder.Services.AddSingleton<ContentImporter>();
            builder.Services.AddSingleton<DataCleaner>();
            builder.Services.AddSingleton<MetadataExporter>();
            builder.Services.AddSingleton<ImageExporter>();
            builder.Services.AddSingleton<ImageLoader>();
            builder.Services.AddSingleton<BinCleaner>();
            builder.Services.AddSingleton<ImageBuilder>();

            using var host = builder.Build();

            await host.Services.GetRequiredService<ImageBuilder>()
                .RunAsync()
                .ConfigureAwait(false);
        }
    }
}