using AppHost.Elastic;
using AppHost.Extensions;
using Docker.DotNet;
using Docker.DotNet.Models;
using Projects;
using UKHO.Aspire.Configuration.Hosting;
using UKHO.Search.Configuration;

namespace AppHost
{
    public class AppHost
    {
        public static async Task Main(string[] args)
        {
            var builder = DistributedApplication.CreateBuilder(args);

            var environmentParameter = builder.AddParameter("environment");
            var environment = await environmentParameter.Resource.GetValueAsync(CancellationToken.None) ?? string.Empty;

            var azureStoragePathParameter = builder.AddParameter("azure-storage");
            var azureStoragePathValue = await azureStoragePathParameter.Resource.GetValueAsync(CancellationToken.None);

            var runModeParameter = builder.AddParameter("runmode");
            var runModeValue = await runModeParameter.Resource.GetValueAsync(CancellationToken.None);

            var ingestionModeParameter = builder.AddParameter("ingestionMode");

            var runMode = Enum.TryParse<RunMode>(runModeValue, true, out var parsedRunMode) ? parsedRunMode : RunMode.Services;

            var storage = builder.AddAzureStorage(ServiceNames.Storage)
                                 .RunAsEmulator(e =>
                                 {
                                     e.WithDataBindMount(azureStoragePathValue);

                                     // The Azure Storage (Azurite) emulator supports two URL styles.
                                     // We use IP/path-style URLs from other containers, e.g. http://storage:10000/devstoreaccount1/container.
                                     // Ensure Azurite parses the account name from the request URI path.
                                     e.WithArgs("--disableProductStyleUrl");
                                 });

            var storageQueue = storage.AddQueues(ServiceNames.Queues);
            var storageTable = storage.AddTables(ServiceNames.Tables);
            var storageBlob = storage.AddBlobs(ServiceNames.Blobs);

            var sqlServer = builder.AddSqlServer(ServiceNames.SqlServer)
                                   .WithLifetime(ContainerLifetime.Persistent)
                                   .WithDataVolume()
                                   .AddDatabase(StorageNames.FileShareEmulatorDatabase);

            switch (runMode)
            {
                case RunMode.Services:
                {
                    var addsEnvironment = builder.AddPublishOnlyParameter("addsEnvironment");

                    var elasticPasswordParameter = builder.AddParameter("elastic-password");
                    var keyCloakUsernameParameter = builder.AddParameter("keycloak-username");
                    var keyCloakPasswordParameter = builder.AddParameter("keycloak-password");


                    var keycloak = builder.AddKeycloak(ServiceNames.KeyCloak, 8080, keyCloakUsernameParameter, keyCloakPasswordParameter)
                                          .WithDataVolume()
                                          .WithRealmImport("./Realms")
                                          .WithLifetime(ContainerLifetime.Persistent);

                    var elasticsearch = builder.AddElasticsearchWithKibana(ServiceNames.ElasticSearch, elasticPasswordParameter)
                                               .WithElasticsearchSetup();

                    var ingestionService = builder.AddProject<IngestionServiceHost>(ServiceNames.Ingestion)
                                                  .WithExternalHttpEndpoints()
                                                   .WithEnvironment("ingestionmode", ingestionModeParameter)
                                                  .WithReference(storageQueue)
                                                  .WithReference(storageTable)
                                                  .WithReference(storageBlob)
                                                  .WithReference(elasticsearch)
                                                  .WaitFor(storageQueue)
                                                  .WaitFor(storageTable)
                                                  .WaitFor(storageBlob)
                                                  .WaitFor(elasticsearch);

                    var queryService = builder.AddProject<QueryServiceHost>(ServiceNames.Query)
                                              .WithExternalHttpEndpoints()
                                              .WithReference(keycloak)
                                              .WithReference(elasticsearch)
                                              .WaitFor(keycloak)
                                              .WaitFor(elasticsearch);

                    var fileShareEmulator = builder.AddProject<FileShareEmulator>(ServiceNames.FileShareEmulator)
                                                   .WithExternalHttpEndpoints()
                                                   .WithEnvironment("environment", environmentParameter)
                                                   .WithReference(sqlServer)
                                                   .WithReference(storageQueue)
                                                   .WithReference(storageBlob)
                                                   .WithReference(elasticsearch)
                                                   .WaitFor(sqlServer)
                                                   .WaitFor(storageQueue)
                                                   .WaitFor(storageBlob)
                                                   .WaitFor(ingestionService)
                                                   .WaitFor(elasticsearch)
                                                   .WithScalar("Emulator API");

                    var rulesWorkbench = builder.AddProject<RulesWorkbench>(ServiceNames.RulesWorkbench)
                                                .WithExternalHttpEndpoints()
                                                .WithReference(sqlServer)
                                                .WithReference(storageBlob)
                                                .WaitFor(sqlServer)
                                                .WaitFor(storageBlob);

                    // Configuration
                    if (builder.ExecutionContext.IsRunMode)
                    {
                        builder.AddConfigurationEmulator(ServiceConfiguration.ServiceGroupName, [ingestionService, queryService!], [fileShareEmulator], @"../../../configuration/configuration.json", @"../../../configuration/external-services.json");
                    }
                    else
                    {
                        var appConfig = builder.AddConfiguration(ServiceNames.Configuration, addsEnvironment!, [ingestionService, queryService]);
                    }

                    break;
                }

                case RunMode.Export:
                {
                    builder.AddProject<FileShareImageBuilder>(ServiceNames.FileShareBuilder)
                           .WithEnvironment("environment", environmentParameter)
                           .WithEnvironment("ingestionmode", ingestionModeParameter)
                           .WithReference(sqlServer)
                           .WaitFor(sqlServer)
                           .WithExplicitStart();
                    break;
                }

                case RunMode.Import:
                {
                    var loaderDataImage = $"fss-data-{environment}";
                    var loaderDataVolumeName = $"{ServiceNames.FileShareEmulator}-data";

                    var imageMetadata = await GetDockerImageMetadataAsync(loaderDataImage);

                    // The data loader (for file share emulator) needs a set of seed files available at runtime.
                    // Docker/Aspire cannot mount a Docker image filesystem directly as a volume, so we:
                    // 1) create/mount a named volume (persistent across runs) and
                    // 2) run a one-shot init container from the data image which copies the seed files into that volume.
                    //
                    // Subsequent runs are fast because the named volume is already populated and the init container becomes a no-op.
                    var fileShareLoaderDataSeeder = builder.AddContainer($"{ServiceNames.FileShareLoader}-data-seeder", loaderDataImage)
                                                           .WithVolume(loaderDataVolumeName, "/seed")
                                                           .WithEntrypoint("/bin/sh")
                                                           .WithArgs("-c",
                                                               // If the volume is empty, copy the data image's seeded content into it and then write a
                                                               // sentinel file as the final step. 
                                                               "if [ -z \"$(ls -A /seed 2>/dev/null)\" ]; then echo '[data-seeder] Seeding volume...'; rm -f /seed/.seed.complete; cp -a /data/. /seed/; echo 'ok' > /seed/.seed.complete; else echo '[data-seeder] Volume already seeded.'; fi");

                    builder.AddContainer(ServiceNames.FileShareLoader, loaderDataImage)
                           .WithDockerfile("../../../tools/FileShareImageLoader", "Dockerfile")
                           .WithBuildArg("BUILD_CONFIGURATION", "Debug")
                           .WithEnvironment("environment", environmentParameter)
                           .WithEnvironment("dataimage", loaderDataImage)
                           .WithEnvironment("dataimage_tags", imageMetadata.Tags)
                           .WithEnvironment("dataimage_digest", imageMetadata.Digest)
                           .WithEnvironment("dataimage_size_bytes", imageMetadata.SizeBytes.ToString())
                           .WithEnvironment("dataimage_created_utc", imageMetadata.CreatedUtc)
                           .WithReference(storageBlob)
                           .WithReference(sqlServer)
                           .WaitFor(storageBlob)
                           .WaitFor(sqlServer)
                           .WithVolume(loaderDataVolumeName, "/data")
                           .WithExplicitStart();

                    break;
                }
            }

            await builder.Build()
                         .RunAsync();
        }

        private static async Task<DockerImageMetadata> GetDockerImageMetadataAsync(string imageReference)
        {
            try
            {
                using var client = new DockerClientConfiguration().CreateClient();

                // Attempt by reference; Docker can resolve tags and local images.
                ImageInspectResponse inspect;
                try
                {
                    inspect = await client.Images.InspectImageAsync(imageReference)
                                          .ConfigureAwait(false);
                }
                catch
                {
                    inspect = await client.Images.InspectImageAsync(imageReference + ":latest")
                                          .ConfigureAwait(false);
                }

                return new DockerImageMetadata(inspect.RepoTags is { Count: > 0 } ? string.Join(",", inspect.RepoTags) : string.Empty, inspect.RepoDigests is { Count: > 0 } ? string.Join(",", inspect.RepoDigests) : string.Empty, inspect.Size, inspect.Created.ToUniversalTime()
                                                                                                                                                                                                                                                          .ToString("O"));
            }
            catch
            {
                return new DockerImageMetadata(string.Empty, string.Empty, 0, string.Empty);
            }
        }

        private sealed record DockerImageMetadata(string Tags, string Digest, long SizeBytes, string CreatedUtc);
    }
}