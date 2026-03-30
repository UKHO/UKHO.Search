using Aspire.Hosting.Azure;
using Projects;

namespace UKHO.Aspire.Configuration.Hosting
{
    public static class DistributedApplicationBuilderExtensions
    {
        public static IResourceBuilder<AzureAppConfigurationResource> AddConfiguration(this IDistributedApplicationBuilder builder, string configurationName, IResourceBuilder<ParameterResource> addsEnvironment, IEnumerable<IResourceBuilder<ProjectResource>> configurationAwareProjects)
        {
            var appConfig = builder.AddAzureAppConfiguration(configurationName);

            foreach (var project in configurationAwareProjects)
            {
                project.WithReference(appConfig);
                project.WithEnvironment(WellKnownConfigurationName.AddsEnvironmentName, addsEnvironment);
            }

            return appConfig;
        }

        /// <summary>
        /// Adds the local App Configuration emulator and a seeder process used to populate configuration values.
        /// </summary>
        /// <param name="builder">The distributed application builder.</param>
        /// <param name="serviceName">The service name used as the App Configuration label.</param>
        /// <param name="configurationAwareProjects">Projects that should reference the configuration service.</param>
        /// <param name="externalServiceMocks">External service mock projects that the seeder should reference.</param>
        /// <param name="configJsonPath">Path to the primary configuration JSON file.</param>
        /// <param name="externalServicesPath">Path to the external services JSON file.</param>
        /// <param name="additionalConfigurationPath">
        /// Optional root directory containing additional configuration files to ingest. Defaults to <see cref="string.Empty"/>
        /// which disables additional ingestion.
        /// </param>
        /// <param name="additionalConfigurationPrefix">
        /// Optional key prefix used when writing additional configuration to App Configuration. Defaults to <see cref="string.Empty"/>
        /// which disables additional ingestion.
        /// </param>
        public static IResourceBuilder<ProjectResource> AddConfigurationEmulator(this IDistributedApplicationBuilder builder, string serviceName, IEnumerable<IResourceBuilder<ProjectResource>> configurationAwareProjects, IEnumerable<IResourceBuilder<ProjectResource>> externalServiceMocks, string configJsonPath, string externalServicesPath, string additionalConfigurationPath = "", string additionalConfigurationPrefix = "")
        {
            var configResolvedPath = Path.GetFullPath(configJsonPath, builder.Environment.ContentRootPath);
            var configFilePath = CopyToTempFile(configResolvedPath);

            var externalServicesResolvedPath = Path.GetFullPath(externalServicesPath, builder.Environment.ContentRootPath);
            var externalServicesFilePath = CopyToTempFile(externalServicesResolvedPath);

            var emulator = builder.AddProject<UKHO_Aspire_Configuration_Emulator>(WellKnownConfigurationName.ConfigurationServiceName)
                                  .WithExternalHttpEndpoints()
                                  .WithHttpHealthCheck("/health")
                                  .WithEnvironment(WellKnownConfigurationName.AddsEnvironmentName, AddsEnvironment.Local.Value);

            // Only add the seeder service in local development environment
            var seederService = builder.AddProject<UKHO_Aspire_Configuration_Seeder>(WellKnownConfigurationName.ConfigurationSeederName)
                                       .WithReference(emulator)
                                       .WaitFor(emulator)
                                       .WithEnvironment(x =>
                                       {
                                           x.EnvironmentVariables.Add(WellKnownConfigurationName.AddsEnvironmentName, AddsEnvironment.Local.Value);

                                           x.EnvironmentVariables.Add(WellKnownConfigurationName.ConfigurationFilePath, configFilePath);
                                           x.EnvironmentVariables.Add(WellKnownConfigurationName.ExternalServicesFilePath, externalServicesFilePath);
                                           x.EnvironmentVariables.Add(WellKnownConfigurationName.ServiceName, serviceName);

                                            x.EnvironmentVariables.Add(WellKnownConfigurationName.AdditionalConfigurationPath, additionalConfigurationPath);
                                            x.EnvironmentVariables.Add(WellKnownConfigurationName.AdditionalConfigurationPrefix, additionalConfigurationPrefix);
                                       });

            foreach (var mock in externalServiceMocks)
            {
                seederService.WithReference(mock);
            }

            foreach (var project in configurationAwareProjects)
            {
                project.WithReference(emulator);
                project.WaitFor(emulator);

                project.WaitForCompletion(seederService);
                project.WithEnvironment(WellKnownConfigurationName.AddsEnvironmentName, AddsEnvironment.Local.Value);
            }

            return emulator;
        }

        private static string CopyToTempFile(string sourceFilePath)
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            var content = File.ReadAllText(sourceFilePath);
            File.WriteAllText(tempFilePath, content);

            return tempFilePath;
        }
    }
}