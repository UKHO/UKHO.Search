using Microsoft.Extensions.Options;
using QueryServiceHost.Models;

namespace QueryServiceHost.Services
{
    public class StubQueryUiSearchClient : IQueryUiSearchClient
    {
        private readonly IOptionsMonitor<StubQueryUiSearchClientOptions> _options;

        public StubQueryUiSearchClient(IOptionsMonitor<StubQueryUiSearchClientOptions> options)
        {
            _options = options;
        }

        public async Task<QueryResponse> SearchAsync(QueryRequest request, CancellationToken cancellationToken)
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

            var queryText = request.QueryText?.Trim();

            var hasFacetSelections = request.SelectedFacets.Values.Any(v => v is not null && v.Count > 0);

            var allHits = GetDeterministicHits();

            IEnumerable<Hit> filtered = allHits;

            if (!string.IsNullOrWhiteSpace(queryText))
            {
                filtered = filtered.Where(h => h.Title.Contains(queryText, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var facetSelection in request.SelectedFacets)
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
                Hits = hits,
                Facets = facets,
                Total = hits.Count,
                Duration = sw.Elapsed
            };
        }

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
