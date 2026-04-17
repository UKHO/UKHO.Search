using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using UKHO.Search.Infrastructure.Query.Search;
using UKHO.Search.Infrastructure.Query.Rules;
using UKHO.Search.Infrastructure.Query.TypedExtraction;
using UKHO.Search.Query.Abstractions;
using UKHO.Search.Services.Query.Abstractions;
using UKHO.Search.Services.Query.Execution;
using UKHO.Search.Services.Query.Normalization;
using UKHO.Search.Services.Query.Planning;
using UKHO.Search.Services.Query.Rules;

namespace UKHO.Search.Infrastructure.Query.Injection
{
    /// <summary>
    /// Provides the composition-root extensions that register the query-side runtime services.
    /// </summary>
    public static class InjectionExtensions
    {
        /// <summary>
        /// Registers the repository-owned query planning and Elasticsearch execution services.
        /// </summary>
        /// <param name="collection">The service collection that should receive the query registrations.</param>
        /// <returns>The same service collection so registrations can be chained fluently.</returns>
        public static IServiceCollection AddQueryServices(this IServiceCollection collection)
        {
            // Guard the composition entry point so host wiring fails early if the DI container is unavailable.
            ArgumentNullException.ThrowIfNull(collection);

            // Register the inward-facing planning services first so the host can depend on repository-owned abstractions.
            collection.AddSingleton<IQueryTextNormalizer, QueryTextNormalizer>();
            collection.AddSingleton<ITypedQuerySignalExtractor, MicrosoftRecognizersTypedQuerySignalExtractor>();
            collection.AddSingleton<IQueryRulesSource, AppConfigQueryRulesSource>();
            collection.AddSingleton<QueryRulesValidator>();
            collection.AddSingleton<QueryRulesLoader>();
            collection.AddSingleton<QueryRulesCatalog>(static provider =>
            {
                // Load the validated query-rule snapshot during singleton creation so invalid rule contracts fail the host fast.
                var catalog = ActivatorUtilities.CreateInstance<QueryRulesCatalog>(provider);
                catalog.EnsureLoaded();
                return catalog;
            });
            collection.AddSingleton<IQueryRulesCatalog>(static provider => provider.GetRequiredService<QueryRulesCatalog>());
            collection.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, AppConfigQueryRulesRefreshService>());
            collection.AddSingleton<IQueryRuleEngine, ConfigurationQueryRuleEngine>();
            collection.AddSingleton<IQueryPlanService, QueryPlanService>();
            collection.AddSingleton<IQuerySearchService, QuerySearchService>();

            // Register the Elasticsearch execution adapter last so application services resolve the concrete infrastructure boundary.
            collection.AddSingleton<IQueryPlanExecutor, ElasticsearchQueryExecutor>();

            return collection;
        }
    }
}