using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Bootstrap;
using UKHO.Search.Infrastructure.Ingestion.DeadLetter;
using UKHO.Search.Infrastructure.Ingestion.Diagnostics;
using UKHO.Search.Infrastructure.Ingestion.Elastic;
using UKHO.Search.Infrastructure.Ingestion.Pipeline;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Nodes;
using UKHO.Search.Infrastructure.Ingestion.Pipeline.Terminal;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Infrastructure.Ingestion.Rules.Actions;
using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;
using UKHO.Search.Infrastructure.Ingestion.Rules.Templating;
using UKHO.Search.Infrastructure.Ingestion.Rules.Validation;
using UKHO.Search.Infrastructure.Ingestion.Statistics;
using UKHO.Search.Ingestion;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Providers;
using UKHO.Search.Ingestion.Providers.FileShare;
using UKHO.Search.Ingestion.Providers.FileShare.Injection;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Rules;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Infrastructure.Ingestion.Injection
{
    public static class InjectionExtensions
    {
        /// <summary>
        /// Registers only the ingestion rules engine dependencies.
        /// </summary>
        /// <remarks>
        /// This is intended for the Rules Workbench host (Blazor UI) which needs to evaluate rules
        /// without wiring up the full ingestion pipeline (queues, Elasticsearch, file-share provider services).
        /// The ingestion service host should continue to call <see cref="AddIngestionServices"/>.
        /// </remarks>
        public static IServiceCollection AddIngestionRulesEngine(this IServiceCollection collection)
        {
            // RulesWorkbench uses this to reuse the existing ingestion rules engine in an isolated way.
             collection.AddSingleton<RuleFileLoader>();
            collection.AddSingleton<IngestionRulesLoader>();
            collection.AddSingleton<IngestionRulesPathValidator>();
            collection.AddSingleton<IngestionRulesValidator>();
            collection.AddSingleton<IngestionRulesCatalog>();
            collection.AddSingleton<IIngestionRulesCatalog>(sp => sp.GetRequiredService<IngestionRulesCatalog>());
            collection.AddSingleton<IPathResolver, IngestionRulesPathResolver>();
            collection.AddSingleton<IngestionRulesPredicateEvaluator>();
            collection.AddSingleton<IngestionRulesTemplateExpander>();
            collection.AddSingleton<IngestionRulesActionApplier>();
            collection.AddSingleton<IIngestionRulesEngine, IngestionRulesEngine>();

            return collection;
        }

        public static IServiceCollection AddIngestionServices(this IServiceCollection collection)
        {
            collection.AddFileShareProvider();

            collection.AddScoped<IIngestionProviderContext, IngestionProviderContext>();

             collection.AddSingleton<RuleFileLoader>();
            collection.AddSingleton<IngestionRulesLoader>();
            collection.AddSingleton<IngestionRulesPathValidator>();
            collection.AddSingleton<IngestionRulesValidator>();
            collection.AddSingleton<IngestionRulesCatalog>();
            collection.AddSingleton<IIngestionRulesCatalog>(sp => sp.GetRequiredService<IngestionRulesCatalog>());
            collection.AddSingleton<IPathResolver, IngestionRulesPathResolver>();
            collection.AddSingleton<IngestionRulesPredicateEvaluator>();
            collection.AddSingleton<IngestionRulesTemplateExpander>();
            collection.AddSingleton<IngestionRulesActionApplier>();
            collection.AddSingleton<IIngestionRulesEngine, IngestionRulesEngine>();
            collection.AddScoped<IIngestionEnricher, IngestionRulesEnricher>();

            collection.AddSingleton<IIngestionDataProviderFactory>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var bulkIndexClient = sp.GetRequiredService<IBulkIndexClient<IndexOperation>>();
                var blobServiceClient = sp.GetRequiredService<BlobServiceClient>();

                var queueName = configuration["ingestion:filesharequeuename"] ?? "file-share-queue";
                var ingressCapacity = configuration.GetValue("ingestion:providerIngressCapacity", 256);

                var providerName = FileShareIngestionDataProviderFactory.ProviderName;

                var indexRetryMaxAttempts = configuration.GetValue<int>("ingestion:indexRetryMaxAttempts");
                var indexRetryBaseDelayMs = configuration.GetValue<int>("ingestion:indexRetryBaseDelayMilliseconds");
                var indexRetryMaxDelayMs = configuration.GetValue<int>("ingestion:indexRetryMaxDelayMilliseconds");
                var indexRetryJitterMs = configuration.GetValue<int>("ingestion:indexRetryJitterMilliseconds");

                var factories = new FileShareIngestionProcessingGraphFactories
                {
                    CreateRequestDeadLetterSinkNode = (name, input, supervisor) => new BlobDeadLetterSinkNode<IngestionRequest>(name, input, blobServiceClient, configuration, configuration.GetValue("ingestion:deadletterFatalIfCannotPersist", true), logger: loggerFactory.CreateLogger(name), fatalErrorReporter: supervisor, providerName: providerName),

                    CreateIndexDeadLetterSinkNode = (name, input, supervisor) => new BlobDeadLetterSinkNode<IndexOperation>(name, input, blobServiceClient, configuration, configuration.GetValue("ingestion:deadletterFatalIfCannotPersist", true), logger: loggerFactory.CreateLogger(name), fatalErrorReporter: supervisor, providerName: providerName),

                    CreateDiagnosticsSinkNode = (name, input, supervisor) => new DiagnosticsSinkNode<IndexOperation>(name, input, loggerFactory.CreateLogger(name), supervisor, providerName),

                    CreateBulkIndexNode = (name, lane, input, successOutput, deadLetterOutput, supervisor) => new InOrderBulkIndexNode(name, input, bulkIndexClient, successOutput, deadLetterOutput, indexRetryMaxAttempts, TimeSpan.FromMilliseconds(indexRetryBaseDelayMs), TimeSpan.FromMilliseconds(indexRetryMaxDelayMs), TimeSpan.FromMilliseconds(indexRetryJitterMs),
                        logger: loggerFactory.CreateLogger(name), fatalErrorReporter: supervisor, providerName: providerName),

                    CreateAckNode = (name, lane, input, supervisor) => new AckSinkNode<IndexOperation>(name, input, loggerFactory.CreateLogger(name), supervisor, providerName)
                };

                var processingGraphDependencies = new FileShareIngestionProcessingGraphDependencies
                {
                    Configuration = configuration,
                    LoggerFactory = loggerFactory,
                    Factories = factories,
                    ScopeFactory = scopeFactory
                };

                return new FileShareIngestionDataProviderFactory(queueName, processingGraphDependencies, loggerFactory, ingressCapacity);
            });

            collection.AddSingleton<IIngestionProviderService, IngestionProviderService>();
            collection.AddSingleton<CanonicalIndexDefinition>();
            collection.AddSingleton<IBootstrapService, BootstrapService>();
            collection.AddSingleton<IStatisticsService, StatisticsService>();

            collection.AddSingleton<IQueueClientFactory, AzureQueueClientFactory>();

            collection.AddSingleton<IBulkIndexClient<IndexOperation>, ElasticsearchBulkIndexClient>();

            collection.AddHostedService<IngestionPipelineHostedService>();

            return collection;
        }
    }
}