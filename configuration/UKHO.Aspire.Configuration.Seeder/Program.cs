using Azure.Data.AppConfiguration;
using Azure.Identity;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UKHO.Aspire.Configuration.Seeder.Services;

namespace UKHO.Aspire.Configuration.Seeder
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            using var bootstrapLoggerFactory = CreateBootstrapLoggerFactory();
            var logger = bootstrapLoggerFactory.CreateLogger<Program>();

            logger.LogDebug("Seeder starting. Args: {Args}", args);

            var sentinel = Environment.GetEnvironmentVariable(WellKnownConfigurationName.ConfigurationFilePath);

            logger.LogDebug(
                "Env var {ConfigurationFilePathEnvVar} is {State}",
                WellKnownConfigurationName.ConfigurationFilePath,
                string.IsNullOrEmpty(sentinel) ? "not set" : "set");

            if (string.IsNullOrEmpty(sentinel))
            {
                logger.LogDebug("Running in command line mode (no Aspire configuration env vars detected).");

                var parseResult = Parser.Default.ParseArguments<CommandLineParameters>(args);

                if (parseResult.Value == null)
                {
                    logger.LogError("Failed to parse command line arguments.");
                    return -1;
                }

                var parameters = parseResult.Value;

                try
                {
                    logger.LogDebug(
                        "Parsed args. ServiceName={ServiceName}, EnvironmentName={EnvironmentName}, ConfigurationFilePath={ConfigurationFilePath}, ServicesFilePath={ServicesFilePath}, AppConfigServiceUrl={AppConfigServiceUrl}",
                        parameters.ServiceName,
                        parameters.EnvironmentName,
                        parameters.ConfigurationFilePath,
                        parameters.ServicesFilePath,
                        parameters.AppConfigServiceUrl);

                    ValidateFilePath(logger, parameters.ConfigurationFilePath, nameof(parameters.ConfigurationFilePath));
                    ValidateFilePath(logger, parameters.ServicesFilePath, nameof(parameters.ServicesFilePath));
                    ValidateUri(logger, parameters.AppConfigServiceUrl, nameof(parameters.AppConfigServiceUrl));

                    var configService = new ConfigurationService(bootstrapLoggerFactory.CreateLogger<ConfigurationService>());
                    var configClient = new ConfigurationClient(new Uri(parameters.AppConfigServiceUrl), new DefaultAzureCredential());

                    var addsEnvironment = AddsEnvironment.Parse(parameters.EnvironmentName);

                    logger.LogInformation("Seeding configuration. Environment={Environment}, Label={Label}", addsEnvironment, parameters.ServiceName.ToLowerInvariant());

                    await configService.SeedConfigurationAsync(addsEnvironment, configClient, parameters.ServiceName, parameters.ConfigurationFilePath, parameters.ServicesFilePath, CancellationToken.None);

                    logger.LogInformation("Seeding completed successfully.");

                    return 0;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Seeder failed.");
                    return -1;
                }
            }

            var environment = AddsEnvironment.GetEnvironment();

            logger.LogDebug("Running via Aspire. AddsEnvironment={Environment}", environment);

            if (environment.IsLocal())
            {
                var builder = Host.CreateApplicationBuilder(args);

                builder.Logging.ClearProviders();
                builder.Logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss.fff ";
                });
                builder.Logging.SetMinimumLevel(LogLevel.Debug);

                logger.LogDebug("Host builder created. Starting local seeding hosted service.");

                var configFilePath = builder.Configuration[WellKnownConfigurationName.ConfigurationFilePath]!;
                var serviceFilePath = builder.Configuration[WellKnownConfigurationName.ExternalServicesFilePath]!;

                var serviceName = builder.Configuration[WellKnownConfigurationName.ServiceName]!;

                logger.LogDebug(
                    "Configuration resolved. ServiceName={ServiceName}, ConfigurationFilePath={ConfigurationFilePath}, ExternalServicesFilePath={ExternalServicesFilePath}",
                    serviceName,
                    configFilePath,
                    serviceFilePath);

                builder.Services.AddSingleton<ConfigurationService>();

                builder.Services.AddSingleton(x =>
                {
                    var factoryLogger = x.GetRequiredService<ILoggerFactory>().CreateLogger("ConfigurationClientFactory");
                    var serviceEnvironmentKey = $"services__{WellKnownConfigurationName.ConfigurationServiceName}__http__0";
                    var url = Environment.GetEnvironmentVariable(serviceEnvironmentKey);

                    factoryLogger.LogDebug(
                        "Resolving App Configuration endpoint from env var {ServiceEnvironmentKey}. Found={Found}",
                        serviceEnvironmentKey,
                        !string.IsNullOrWhiteSpace(url));

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        throw new InvalidOperationException($"Environment variable '{serviceEnvironmentKey}' was not set.");
                    }

                    factoryLogger.LogInformation("Using App Configuration endpoint {Endpoint}", url);

                    var conStr = $"Endpoint={url};Id=aac-credential;Secret=c2VjcmV0;";
                    return new ConfigurationClient(conStr);
                });

                builder.Services.AddHostedService(x =>
                {
                    var factoryLogger = x.GetRequiredService<ILoggerFactory>().CreateLogger("LocalSeederServiceFactory");
                    var hostedLifetime = x.GetRequiredService<IHostApplicationLifetime>();
                    var configService = x.GetRequiredService<ConfigurationService>();

                    factoryLogger.LogDebug("Creating LocalSeederService.");

                    return new LocalSeederService(
                        hostedLifetime,
                        configService,
                        serviceName,
                        x.GetRequiredService<ConfigurationClient>(),
                        configFilePath,
                        serviceFilePath,
                        x.GetRequiredService<ILogger<LocalSeederService>>());
                });

                var app = builder.Build();

                var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
                appLogger.LogDebug("Host built; running.");

                await app.RunAsync();

                appLogger.LogDebug("Host stopped.");
            }

            // We are running from Aspire
            logger.LogDebug("Not local environment; nothing to do.");
            return 0;
        }

        private static ILoggerFactory CreateBootstrapLoggerFactory()
        {
            return LoggerFactory.Create(logging =>
            {
                logging.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss.fff ";
                });
                logging.SetMinimumLevel(LogLevel.Debug);
            });
        }

        private static void ValidateUri(ILogger logger, string url, string name)
        {
            logger.LogDebug("URL is {Name}: {Url}", name, url);

            if (!Uri.TryCreate(url, UriKind.Absolute, out var _))
            {
                throw new ArgumentException($"Invalid URI: {url} ({name})");
            }
        }

        private static void ValidateFilePath(ILogger logger, string path, string name)
        {
            logger.LogDebug("File path {Name}: {Path}", name, path);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path} ({name})");
            }
        }

        public class CommandLineParameters
        {
            [Value(0, HelpText = "The ADDS service name (e.g. 'EFS')", Required = true)]
            public required string ServiceName { get; set; }

            [Value(1, HelpText = "The ADDS environment name (e.g. 'dev')", Required = true)]
            public required string EnvironmentName { get; set; }

            [Value(2, HelpText = "Configuration JSON file path", Required = true)]
            public required string ConfigurationFilePath { get; set; }

            [Value(3, HelpText = "Services JSON file path", Required = true)]
            public required string ServicesFilePath { get; set; }

            [Value(4, HelpText = "Azure App Configuration Service URL", Required = true)]
            public required string AppConfigServiceUrl { get; set; }
        }
    }
}