using Microsoft.Extensions.Logging;
using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Infrastructure.Query.Rules
{
    /// <summary>
    /// Caches the validated query-rule snapshot and reloads it when configuration changes are detected.
    /// </summary>
    internal sealed class QueryRulesCatalog : IQueryRulesCatalog
    {
        private readonly QueryRulesLoader _loader;
        private readonly ILogger<QueryRulesCatalog> _logger;
        private QueryRulesSnapshot? _snapshot;
        private QueryRulesCatalogDiagnostics? _diagnostics;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryRulesCatalog"/> class.
        /// </summary>
        /// <param name="loader">The loader that produces validated query-rule snapshots.</param>
        /// <param name="logger">The logger that records catalog load and reload diagnostics.</param>
        public QueryRulesCatalog(QueryRulesLoader loader, ILogger<QueryRulesCatalog> logger)
        {
            // Retain the collaborators once so every catalog read goes through the same validated load path.
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Ensures that the initial query-rule snapshot has been loaded.
        /// </summary>
        internal void EnsureLoaded()
        {
            _ = GetSnapshot();
        }

        /// <summary>
        /// Reloads the validated query-rule snapshot from the underlying source.
        /// </summary>
        internal void Reload()
        {
            // Replace the cached snapshot atomically so concurrent evaluators always see one complete validated snapshot.
            var nextSnapshot = LoadSnapshot();
            Interlocked.Exchange(ref _snapshot, nextSnapshot);
        }

        /// <summary>
        /// Gets the current validated query-rule snapshot.
        /// </summary>
        /// <returns>The validated query-rule snapshot used by the runtime.</returns>
        public QueryRulesSnapshot GetSnapshot()
        {
            return Volatile.Read(ref _snapshot) ?? LoadFirstTime();
        }

        /// <summary>
        /// Gets diagnostics about the currently loaded query-rule snapshot.
        /// </summary>
        /// <returns>The current query-rule catalog diagnostics.</returns>
        public QueryRulesCatalogDiagnostics GetDiagnostics()
        {
            _ = GetSnapshot();
            return Volatile.Read(ref _diagnostics) ?? new QueryRulesCatalogDiagnostics();
        }

        /// <summary>
        /// Loads the initial snapshot only once, even if multiple callers arrive concurrently.
        /// </summary>
        /// <returns>The cached or newly loaded validated snapshot.</returns>
        private QueryRulesSnapshot LoadFirstTime()
        {
            var loaded = LoadSnapshot();
            var existing = Interlocked.CompareExchange(ref _snapshot, loaded, comparand: null);
            return existing ?? loaded;
        }

        /// <summary>
        /// Loads the latest validated snapshot from the underlying loader.
        /// </summary>
        /// <returns>The latest validated query-rule snapshot.</returns>
        private QueryRulesSnapshot LoadSnapshot()
        {
            // Delegate validation to the dedicated loader and log the resulting rule count for startup and refresh diagnostics.
            var snapshot = _loader.Load();
            var diagnostics = new QueryRulesCatalogDiagnostics
            {
                LoadedAtUtc = DateTimeOffset.UtcNow,
                RuleCount = snapshot.Rules.Count
            };

            // Publish the diagnostics alongside the snapshot so services can include the load timestamp in planning diagnostics.
            Interlocked.Exchange(ref _diagnostics, diagnostics);
            _logger.LogInformation("Loaded query rules. RuleCount={RuleCount}", snapshot.Rules.Count);
            return snapshot;
        }
    }
}
