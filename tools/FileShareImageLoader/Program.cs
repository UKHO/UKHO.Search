using FileShareImageLoader.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace FileShareImageLoader
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                var environmentName = Environment.GetEnvironmentVariable("environment");

                if (string.IsNullOrWhiteSpace(environmentName))
                {
                    throw new ArgumentException("Cannot read 'environment' value");
                }

                builder.Configuration["environment"] = environmentName;

                builder.AddSqlServerClient(StorageNames.FileShareEmulatorDatabase);
                builder.AddAzureBlobServiceClient(connectionName: "blobs");

                builder.Services.AddSingleton<BacpacImporter>();
                builder.Services.AddSingleton<SchemaMigration>();
                builder.Services.AddSingleton<ContentImporter>();

                await using var app = builder.Build();

                var bacpacPath = $"/data/{environmentName}.bacpac";

                var sqlConnection = app.Services.GetRequiredService<SqlConnection>();
                var sqlConnectionString = sqlConnection.ConnectionString;

                var importer = app.Services.GetRequiredService<BacpacImporter>();
                await importer
                    .EnsureDatabaseSeededAsync(sqlConnectionString, StorageNames.FileShareEmulatorDatabase, bacpacPath, CancellationToken.None)
                    .ConfigureAwait(false);

                var schemaMigration = app.Services.GetRequiredService<SchemaMigration>();

                var dataImageName =
                    Environment.GetEnvironmentVariable("dataimage")
                    ?? "unknown";

                var imageInfo = new LocalMetadataImageInfo(
                    Version: Environment.GetEnvironmentVariable("dataimage_version"),
                    Tags: Environment.GetEnvironmentVariable("dataimage_tags"),
                    Digest: Environment.GetEnvironmentVariable("dataimage_digest"),
                    SizeBytes: Environment.GetEnvironmentVariable("dataimage_size_bytes"),
                    CreatedUtc: Environment.GetEnvironmentVariable("dataimage_created_utc"));

                await schemaMigration
                    .ApplyAsync(sqlConnectionString, dataImageName, imageInfo, CancellationToken.None)
                    .ConfigureAwait(false);

                var contentImporter = app.Services.GetRequiredService<ContentImporter>();
                await contentImporter.ImportAsync(environmentName, CancellationToken.None).ConfigureAwait(false);

                Console.WriteLine("[Loader] Completed.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Loader] Failed: {ex.GetType().Name}: {ex.Message}");
                return 1;
            }
        }
    }
}
