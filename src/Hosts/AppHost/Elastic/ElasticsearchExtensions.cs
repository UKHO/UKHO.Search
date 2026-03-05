using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AppHost.Elastic
{
    public static class ElasticsearchExtensions
    {
        public static IResourceBuilder<ElasticsearchResource> AddElasticsearchWithKibana(
            this IDistributedApplicationBuilder builder,
            string name,
            IResourceBuilder<ParameterResource> password,
            Action<IResourceBuilder<ElasticsearchResource>>? configureElasticsearch = null,
            Action<IResourceBuilder<ContainerResource>>? configureKibana = null)
        {
            var elasticsearch = builder
                .AddElasticsearch(name, password)
                .WithEnvironment("xpack.security.enrollment.enabled", "true")
                .WithLifetime(ContainerLifetime.Persistent)
                .WithVolume($"{name}-data", "/usr/share/elasticsearch/data");

            configureElasticsearch?.Invoke(elasticsearch);

            var kibana = builder
                .AddContainer($"{name}-kibana", "docker.elastic.co/kibana/kibana", "8.17.3")
                .WithEnvironment("ELASTICSEARCH_HOSTS", elasticsearch.GetEndpoint("http"))
                .WithEnvironment("ELASTICSEARCH_USERNAME", "kibana_system")
                .WithEnvironment("ELASTICSEARCH_PASSWORD", password)
                .WithHttpEndpoint(targetPort: 5601)
                .WithLifetime(ContainerLifetime.Persistent)
                .WaitFor(elasticsearch)
                .WithParentRelationship(elasticsearch);

            configureKibana?.Invoke(kibana);

            return elasticsearch;
        }

        public static IResourceBuilder<ElasticsearchResource> WithElasticsearchSetup(
            this IResourceBuilder<ElasticsearchResource> builder,
            string kibanaAdminUsername = "kibana_admin",
            string[]? kibanaAdminRoles = null,
            string kibanaAdminFullName = "Kibana Administrator")
        {
            kibanaAdminRoles ??= ["kibana_admin", "superuser"];

            var annotation = new ElasticsearchSetupAnnotation
            {
                KibanaAdminUsername = kibanaAdminUsername,
                KibanaAdminRoles = kibanaAdminRoles,
                KibanaAdminFullName = kibanaAdminFullName
            };

            builder.WithAnnotation(annotation);

            // Subscribe to ResourceReadyEvent
            builder.ApplicationBuilder.Eventing.Subscribe<ResourceReadyEvent>(async (evt, ct) =>
            {
                // Only handle events for this specific Elasticsearch resource
                if (evt.Resource.Name != builder.Resource.Name)
                    return;

                if (evt.Resource is not ElasticsearchResource elasticsearchResource)
                    return;

                var logger = evt.Services.GetRequiredService<ILogger<ElasticsearchResource>>();

                var password = await elasticsearchResource.PasswordParameter.GetValueAsync(ct);
                if (password == null)
                {
                    logger.LogWarning("Password for Elasticsearch resource {ResourceName} is null, skipping user setup",
                        elasticsearchResource.Name);
                    return;
                }

                await SetupElasticsearchUsersAsync(elasticsearchResource, annotation, password, logger, ct);
            });

            return builder;
        }

        private static async Task SetupElasticsearchUsersAsync(
            ElasticsearchResource resource,
            ElasticsearchSetupAnnotation setupAnnotation,
            string password,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation("Elasticsearch {ResourceName} is ready, starting user setup", resource.Name);

                var endpoint = resource.GetEndpoint("http");
                var baseUrl = $"http://{endpoint.Host}:{endpoint.Port}";

                using var httpClient = new HttpClient();
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"elastic:{password}"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                // Set kibana_system password
                logger.LogInformation("Setting kibana_system password for {ResourceName}", resource.Name);
                var kibanaSystemResponse = await httpClient.PostAsJsonAsync(
                    $"{baseUrl}/_security/user/kibana_system/_password",
                    new { password },
                    cancellationToken);

                if (!kibanaSystemResponse.IsSuccessStatusCode)
                {
                    var error = await kibanaSystemResponse.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogError("Failed to set kibana_system password: {Error}", error);
                    return;
                }

                logger.LogInformation("Successfully set kibana_system password");

                // Create admin user
                logger.LogInformation(
                    "Creating admin user '{Username}' for {ResourceName}",
                    setupAnnotation.KibanaAdminUsername,
                    resource.Name);

                var adminUserResponse = await httpClient.PostAsJsonAsync(
                    $"{baseUrl}/_security/user/{setupAnnotation.KibanaAdminUsername}",
                    new
                    {
                        password,
                        roles = setupAnnotation.KibanaAdminRoles,
                        full_name = setupAnnotation.KibanaAdminFullName
                    },
                    cancellationToken);

                if (!adminUserResponse.IsSuccessStatusCode)
                {
                    var error = await adminUserResponse.Content.ReadAsStringAsync(cancellationToken);
                    logger.LogWarning("Failed to create admin user (may already exist): {Error}", error);
                    return;
                }

                logger.LogInformation(
                    "Successfully created admin user '{Username}'",
                    setupAnnotation.KibanaAdminUsername);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting up Elasticsearch users for {ResourceName}", resource.Name);
            }
        }
    }
}