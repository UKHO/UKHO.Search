using System.Text.Json;
using QueryServiceHost.Models;
using UKHO.Search.Query.Models;
using UKHO.Search.Query.Results;
using UKHO.Search.Services.Query.Abstractions;

namespace QueryServiceHost.Services
{
    /// <summary>
    /// Adapts the host-local query UI contract onto the repository-owned query search application service.
    /// </summary>
    public sealed class QueryUiSearchClient : IQueryUiSearchClient
    {
        private static readonly JsonSerializerOptions PlanJsonSerializerOptions = new()
        {
            WriteIndented = true
        };

        private readonly IQuerySearchService _querySearchService;
        private readonly ILogger<QueryUiSearchClient> _logger;

        /// <summary>
        /// Initializes the host search client with the repository-owned query search application service.
        /// </summary>
        /// <param name="querySearchService">The application service that plans and executes repository-owned query searches.</param>
        /// <param name="logger">The logger used to emit structured host-adapter diagnostics.</param>
        public QueryUiSearchClient(IQuerySearchService querySearchService, ILogger<QueryUiSearchClient> logger)
        {
            // Capture the injected collaborators once so the host adapter remains a thin composition-layer bridge.
            _querySearchService = querySearchService ?? throw new ArgumentNullException(nameof(querySearchService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes a query UI search request through the repository-owned query pipeline.
        /// </summary>
        /// <param name="request">The host-local query UI request that contains the user query text and current facet state.</param>
        /// <param name="cancellationToken">The cancellation token that stops the search when the caller no longer needs the result.</param>
        /// <returns>The host-local query response projected from the repository-owned query result.</returns>
        public async Task<QueryResponse> SearchAsync(QueryRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (request.SelectedFacets.Values.Any(static values => values.Count > 0))
            {
                // Surface the current limitation explicitly so contributors can see that slice one ignores host facet selections.
                _logger.LogInformation("Query UI facet selections were supplied but are not yet translated by the slice-one query pipeline.");
            }

            // Delegate the real query work to the application service so the host remains free of planning and Elasticsearch logic.
            var result = await _querySearchService.SearchAsync(request.QueryText, cancellationToken)
                .ConfigureAwait(false);

            return ProjectResponse(result, usedEditedPlan: false);
        }

        /// <summary>
        /// Executes a caller-supplied query plan through the repository-owned query pipeline.
        /// </summary>
        /// <param name="plan">The repository-owned query plan supplied by the host editor workflow.</param>
        /// <param name="cancellationToken">The cancellation token that stops the execution when the caller no longer needs the result.</param>
        /// <returns>The host-local query response projected from the repository-owned query result.</returns>
        public async Task<QueryResponse> ExecutePlanAsync(QueryPlan plan, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(plan);

            // Delegate supplied-plan execution to the application service so the host stays free of query execution logic.
            var result = await _querySearchService.ExecutePlanAsync(plan, cancellationToken)
                .ConfigureAwait(false);

            return ProjectResponse(result, usedEditedPlan: true);
        }

        /// <summary>
        /// Projects the repository-owned query result into the richer host response required by the Blazor workspace shell.
        /// </summary>
        /// <param name="result">The repository-owned query result returned by the application service.</param>
        /// <param name="usedEditedPlan"><see langword="true"/> when the response came from edited-plan execution; otherwise <see langword="false"/>.</param>
        /// <returns>The host-local query response projected for the interactive workspace.</returns>
        private QueryResponse ProjectResponse(QuerySearchResult result, bool usedEditedPlan)
        {
            // Translate the repository-owned result into the host-local model so Blazor components remain decoupled from backend contracts.
            return new QueryResponse
            {
                Plan = result.Plan,
                GeneratedPlanJson = SerializePlan(result.Plan),
                ElasticsearchRequestJson = FormatJsonForDisplay(result.ElasticsearchRequestJson),
                Hits = result.Hits.Select(static hit => new Hit
                {
                    Title = hit.Title,
                    Type = hit.Type,
                    Region = hit.Region,
                    MatchedFields = hit.MatchedFields.ToArray(),
                    Raw = hit.Raw
                }).ToArray(),
                Facets = Array.Empty<FacetGroup>(),
                Total = result.Total,
                Duration = result.Duration,
                SearchEngineDuration = result.SearchEngineDuration,
                Warnings = result.Warnings.ToArray(),
                UsedEditedPlan = usedEditedPlan
            };
        }

        /// <summary>
        /// Serializes the repository-owned query plan into formatted JSON for Monaco display.
        /// </summary>
        /// <param name="plan">The executed query plan returned by the repository-owned query pipeline.</param>
        /// <returns>The formatted JSON representation shown in the host editor.</returns>
        private string SerializePlan(QueryPlan plan)
        {
            // Serialize the canonical query plan into a stable, readable JSON payload that developers can inspect in Monaco.
            try
            {
                return JsonSerializer.Serialize(plan, PlanJsonSerializerOptions);
            }
            catch (Exception ex)
            {
                // Preserve the raw-query result path even when the plan cannot be projected into editor JSON.
                _logger.LogError(ex, "The generated query plan could not be serialized for the Query UI workspace.");

                return JsonSerializer.Serialize(
                    new
                    {
                        error = "The generated query plan could not be serialized for Monaco display."
                    },
                    PlanJsonSerializerOptions);
            }
        }

        /// <summary>
        /// Formats Elasticsearch request JSON for diagnostics display without changing its semantic content.
        /// </summary>
        /// <param name="json">The Elasticsearch request JSON returned by the repository-owned execution pipeline.</param>
        /// <returns>A readable JSON payload suitable for the diagnostics panel.</returns>
        private string FormatJsonForDisplay(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return string.Empty;
            }

            try
            {
                // Reformat the payload for diagnostics readability while preserving the infrastructure-generated request structure.
                using var document = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(document.RootElement, PlanJsonSerializerOptions);
            }
            catch (Exception ex)
            {
                // Preserve the original JSON payload when formatting fails so diagnostics remain available.
                _logger.LogWarning(ex, "The Elasticsearch request JSON could not be reformatted for diagnostics display.");
                return json;
            }
        }
    }
}
