using Projects;
using UKHO.Search.Configuration;

namespace UKHO.Search.AppHost;

public class AppHost
{
    public static async Task Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var keyCloakUsernameParameter = builder.AddParameter("keycloak-username");
        var keyCloakPasswordParameter = builder.AddParameter("keycloak-password", true);

        var environmentParameter = builder.AddParameter("environment");
        var environment = await environmentParameter.Resource.GetValueAsync(CancellationToken.None) ?? string.Empty;

        var emulatorPersistentParameter = builder.AddParameter("emulator-persistent");

        var azureStoragePathParameter = builder.AddParameter("azure-storage");
        var azureStoragePathValue = await azureStoragePathParameter.Resource.GetValueAsync(CancellationToken.None);

        var runModeParameter = builder.AddParameter("runmode");
        var runModeValue = await runModeParameter.Resource.GetValueAsync(CancellationToken.None);

        var runMode = Enum.TryParse<RunMode>(runModeValue, true, out var parsedRunMode)
            ? parsedRunMode
            : RunMode.Services;

        var storage = builder.AddAzureStorage(ServiceNames.Storage)
            .RunAsEmulator(e => { e.WithDataBindMount(azureStoragePathValue); });

        var storageQueue = storage.AddQueues(ServiceNames.Queues);
        var storageTable = storage.AddTables(ServiceNames.Tables);
        var storageBlob = storage.AddBlobs(ServiceNames.Blobs);

        var sqlServer = builder.AddSqlServer(ServiceNames.SqlServer)
            .WithLifetime(ContainerLifetime.Persistent)
            .AddDatabase(StorageNames.FileShareEmulatorDatabase);

        switch (runMode)
        {
            case RunMode.Services:
            {
                var keycloak = builder.AddKeycloak(ServiceNames.KeyCloak, 8080, keyCloakUsernameParameter,
                        keyCloakPasswordParameter)
                    .WithDataVolume()
                    .WithRealmImport("./Realms")
                    .WithLifetime(ContainerLifetime.Persistent);

                var elasticSearch = builder.AddElasticsearch(ServiceNames.ElasticSearch)
                    .WithLifetime(ContainerLifetime.Persistent)
                    .WithDataVolume()
                    .WaitFor(storage);

                builder.AddProject<UKHO_Search_Ingestion>(ServiceNames.Ingestion)
                    .WithExternalHttpEndpoints()
                    .WithReference(storageQueue)
                    .WithReference(storageTable)
                    .WithReference(storageBlob)
                    .WithReference(elasticSearch)
                    .WaitFor(storageQueue)
                    .WaitFor(storageTable)
                    .WaitFor(storageBlob)
                    .WaitFor(elasticSearch);

                builder.AddProject<UKHO_Search_Query>(ServiceNames.Query)
                    .WithExternalHttpEndpoints()
                    .WithReference(keycloak)
                    .WithReference(elasticSearch)
                    .WaitFor(keycloak)
                    .WaitFor(elasticSearch);

                builder.AddProject<FileShareEmulator>(ServiceNames.FileShareEmulator)
                    .WithExternalHttpEndpoints()
                    .WithReference(sqlServer)
                    .WithReference(storageQueue)
                    .WithReference(storageBlob)
                    .WaitFor(sqlServer)
                    .WaitFor(storageQueue)
                    .WaitFor(storageBlob);

                break;
            }

            case RunMode.Export:
            {
                builder.AddProject<FileShareImageBuilder>(ServiceNames.FileShareBuilder)
                    .WithEnvironment("environment", environmentParameter)
                    .WithReference(sqlServer)
                    .WaitFor(sqlServer)
                    .WithExplicitStart();
                break;
            }

            case RunMode.Import:
            {
                var loaderDataImage = $"fss-data-{environment}";
                var loaderDataVolumeName = $"{ServiceNames.FileShareEmulator}-data";

                // The data loader (for file share emulator) needs a set of seed files available at runtime.
                // Docker/Aspire cannot mount a Docker image filesystem directly as a volume, so we:
                // 1) create/mount a named volume (persistent across runs) and
                // 2) run a one-shot init container from the data image which copies the seed files into that volume.
                //
                // Subsequent runs are fast because the named volume is already populated and the init container becomes a no-op.
                var fileShareEmulatorDataSeeder = builder
                    .AddContainer($"{ServiceNames.FileShareEmulator}-data-seeder", loaderDataImage)
                    .WithVolume(loaderDataVolumeName, "/seed")
                    .WithEntrypoint("/bin/sh")
                    .WithArgs(
                        "-c",
                        // If the volume is empty, copy the data image's seeded content into it and then write a
                        // sentinel file as the final step. 
                        "if [ -z \"$(ls -A /seed 2>/dev/null)\" ]; then echo '[data-seeder] Seeding volume...'; rm -f /seed/.seed.complete; cp -a /data/. /seed/; echo 'ok' > /seed/.seed.complete; else echo '[data-seeder] Volume already seeded.'; fi");

                builder.AddContainer(ServiceNames.FileShareLoader, loaderDataImage)
                    .WithDockerfile("../FileShareImageLoader", "Dockerfile")
                    .WithBuildArg("BUILD_CONFIGURATION", "Debug")
                    .WithEnvironment("environment", environmentParameter)
                    .WithReference(storageBlob)
                    .WithReference(sqlServer)
                    .WaitFor(storageBlob)
                    .WaitFor(sqlServer)
                    .WithVolume(loaderDataVolumeName, "/data")
                    .WithExplicitStart();

                break;
            }
        }

        await builder.Build().RunAsync();
    }
}