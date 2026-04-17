using Microsoft.Extensions.Options;
using System.Text.Json;
using QueryServiceHost.Models;
using UKHO.Search.Query.Models;

namespace QueryServiceHost.Services
{
    /// <summary>
    /// Provides a deterministic in-memory search client used by local development and fallback host scenarios.
    /// </summary>
    public class StubQueryUiSearchClient : IQueryUiSearchClient
    {
        private static readonly JsonSerializerOptions PlanJsonSerializerOptions = new()
        {
            WriteIndented = true
        };

        private readonly IOptionsMonitor<StubQueryUiSearchClientOptions> _options;

        /// <summary>
        /// Initializes the deterministic stub client with its mutable options source.
        /// </summary>
        /// <param name="options">The options monitor that controls latency and failure simulation.</param>
        public StubQueryUiSearchClient(IOptionsMonitor<StubQueryUiSearchClientOptions> options)
        {
            // Capture the options monitor once so each call can read the latest stub configuration.
            _options = options;
        }

        /// <summary>
        /// Executes the raw-query stub path using deterministic in-memory data.
        /// </summary>
        /// <param name="request">The host-local request that contains the raw query text and facet selections.</param>
        /// <param name="cancellationToken">The cancellation token that stops the simulated execution when the caller no longer needs the result.</param>
        /// <returns>The projected host response built from deterministic in-memory hits.</returns>
        public async Task<QueryResponse> SearchAsync(QueryRequest request, CancellationToken cancellationToken)
        {
            // Reuse the shared deterministic execution path and omit plan JSON because the stub raw-query path does not generate repository plans.
            return await ExecuteDeterministicSearchAsync(
                request.QueryText,
                request.SelectedFacets,
                generatedPlanJson: string.Empty,
                usedEditedPlan: false,
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a caller-supplied query plan using deterministic in-memory data.
        /// </summary>
        /// <param name="plan">The repository-owned query plan supplied by the host editor workflow.</param>
        /// <param name="cancellationToken">The cancellation token that stops the simulated execution when the caller no longer needs the result.</param>
        /// <returns>The projected host response built from deterministic in-memory hits.</returns>
        public async Task<QueryResponse> ExecutePlanAsync(QueryPlan plan, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(plan);

            // Reuse the deterministic execution path and surface the supplied plan JSON so the stub behaves like the real host adapter.
            return await ExecuteDeterministicSearchAsync(
                plan.Input.RawText,
                new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase),
                JsonSerializer.Serialize(plan, PlanJsonSerializerOptions),
                usedEditedPlan: true,
                cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Applies the configured stub behavior and returns deterministic hits for either raw-query or edited-plan execution.
        /// </summary>
        /// <param name="queryText">The text used to filter deterministic hits.</param>
        /// <param name="selectedFacets">The selected facet filters that should be applied to the deterministic hit set.</param>
        /// <param name="generatedPlanJson">The plan JSON that should be returned to the host editor, if any.</param>
        /// <param name="usedEditedPlan"><see langword="true"/> when the caller executed an edited plan; otherwise <see langword="false"/>.</param>
        /// <param name="cancellationToken">The cancellation token that stops the simulated execution when the caller no longer needs the result.</param>
        /// <returns>The deterministic host response for the simulated execution.</returns>
        private async Task<QueryResponse> ExecuteDeterministicSearchAsync(
            string? queryText,
            IReadOnlyDictionary<string, IReadOnlySet<string>> selectedFacets,
            string generatedPlanJson,
            bool usedEditedPlan,
            CancellationToken cancellationToken)
        {
            var options = _options.CurrentValue;

            if (options.SimulatedLatencyMs > 0)
            {
                await Task.Delay(options.SimulatedLatencyMs, cancellationToken);
            }

            if (options.AlwaysFail)
            {
                throw new InvalidOperationException("Stubbed search failure (StubQueryUiSearchClientOptions.AlwaysFail=true)");
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var trimmedQueryText = queryText?.Trim();

            var allHits = GetDeterministicHits();

            IEnumerable<Hit> filtered = allHits;

            if (!string.IsNullOrWhiteSpace(trimmedQueryText))
            {
                filtered = filtered.Where(h => h.Title.Contains(trimmedQueryText, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var facetSelection in selectedFacets)
            {
                if (facetSelection.Value is null || facetSelection.Value.Count == 0)
                {
                    continue;
                }

                filtered = facetSelection.Key.ToLowerInvariant() switch
                {
                    "region" => filtered.Where(h => h.Region is not null && facetSelection.Value.Contains(h.Region)),
                    "type" => filtered.Where(h => h.Type is not null && facetSelection.Value.Contains(h.Type)),
                    _ => filtered
                };
            }

            var hits = filtered.ToList();

            var facets = GetStaticFacets();

            sw.Stop();

            return new QueryResponse
            {
                GeneratedPlanJson = generatedPlanJson,
                Hits = hits,
                Facets = facets,
                Total = hits.Count,
                Duration = sw.Elapsed,
                Warnings = ["The deterministic stub client does not project repository-owned diagnostics or Elasticsearch request JSON."],
                UsedEditedPlan = usedEditedPlan
            };
        }

        /// <summary>
        /// Builds the static facet groups returned by the deterministic stub client.
        /// </summary>
        /// <returns>The facet groups surfaced to the host UI.</returns>
        private static IReadOnlyList<FacetGroup> GetStaticFacets()
        {
            return
            [
                new FacetGroup
                {
                    Name = "Region",
                    Values =
                    [
                        new FacetValue { Value = "North Sea", Count = 54 },
                        new FacetValue { Value = "Baltic", Count = 12 },
                        new FacetValue { Value = "Atlantic", Count = 8 }
                    ]
                },
                new FacetGroup
                {
                    Name = "Type",
                    Values =
                    [
                        new FacetValue { Value = "Wreck", Count = 39 },
                        new FacetValue { Value = "Pipeline", Count = 22 },
                        new FacetValue { Value = "Cable", Count = 17 }
                    ]
                }
            ];
        }

        /// <summary>
        /// Builds the deterministic hit set used by the stub execution path.
        /// </summary>
        /// <returns>The fixed set of in-memory hits that back the stub client.</returns>
        private static IReadOnlyList<Hit> GetDeterministicHits()
        {
            return
            [
                new Hit { Title = "Wreck - North Sea - Example 001", Type = "Wreck", Region = "North Sea" },
                new Hit { Title = "Wreck - North Sea - Example 002", Type = "Wreck", Region = "North Sea" },
                new Hit { Title = "Pipeline - Norway - Example 003", Type = "Pipeline", Region = "Atlantic" },
                new Hit { Title = "Cable - Baltic - Example 004", Type = "Cable", Region = "Baltic" },
                new Hit { Title = "Pipeline - North Sea - Example 005", Type = "Pipeline", Region = "North Sea" }
            ];
        }
    }
}
