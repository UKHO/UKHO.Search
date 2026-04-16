using System.Diagnostics;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Query.Models;
using UKHO.Search.Query.Abstractions;
using UKHO.Search.Query.Models;
using UKHO.Search.Query.Results;

namespace UKHO.Search.Infrastructure.Query.Search
{
    /// <summary>
    /// Executes repository-owned query plans against Elasticsearch.
    /// </summary>
    public sealed class ElasticsearchQueryExecutor : IQueryPlanExecutor
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly ElasticsearchClient _client;
        private readonly string _indexName;
        private readonly ILogger<ElasticsearchQueryExecutor> _logger;

        /// <summary>
        /// Initializes the Elasticsearch query executor with the configured Elasticsearch client and canonical index name.
        /// </summary>
        /// <param name="client">The Elasticsearch client configured by the host runtime.</param>
        /// <param name="configuration">The application configuration that contains the canonical index name.</param>
        /// <param name="logger">The logger used to emit structured execution diagnostics and failures.</param>
        public ElasticsearchQueryExecutor(ElasticsearchClient client, IConfiguration configuration, ILogger<ElasticsearchQueryExecutor> logger)
        {
            // Capture the external dependencies and resolve the canonical index name once during composition.
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ArgumentNullException.ThrowIfNull(configuration);

            _indexName = configuration["ingestion:indexname"] ?? throw new InvalidOperationException("Missing required configuration value 'ingestion:indexname'.");
        }

        /// <summary>
        /// Executes the supplied query plan against Elasticsearch.
        /// </summary>
        /// <param name="plan">The repository-owned query plan that should be translated into Elasticsearch behavior.</param>
        /// <param name="cancellationToken">The cancellation token that stops execution when the caller no longer needs the result.</param>
        /// <returns>The executed search result.</returns>
        public async Task<QuerySearchResult> SearchAsync(QueryPlan plan, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(plan);

            // Generate the deterministic request body up front so both executed and short-circuited paths can project the same diagnostics payload.
            var requestBody = ElasticsearchQueryMapper.CreateRequestBody(plan);

            if (!HasExecutableClauses(plan))
            {
                // Avoid issuing a broad query when the planner produced neither rule-driven canonical intent nor residual default contributions.
                _logger.LogWarning("Skipping Elasticsearch execution because the query plan produced no executable clauses.");
                return new QuerySearchResult
                {
                    Plan = plan,
                    ElasticsearchRequestJson = requestBody,
                    Hits = Array.Empty<QuerySearchHit>(),
                    Total = 0,
                    Duration = TimeSpan.Zero,
                    Warnings = ["The query plan produced no executable clauses, so Elasticsearch execution was skipped."]
                };
            }

            try
            {
                var endpoint = new EndpointPath(Elastic.Transport.HttpMethod.POST, $"/{Uri.EscapeDataString(_indexName)}/_search");
                var stopwatch = Stopwatch.StartNew();

                // Execute the raw JSON body through the configured Elasticsearch transport so host wiring still uses the real client path.
                var response = await _client.Transport.RequestAsync<StringResponse>(endpoint, PostData.String(requestBody), null, null, cancellationToken)
                    .ConfigureAwait(false);
                stopwatch.Stop();

                if (response.ApiCallDetails?.HasSuccessfulStatusCode != true || string.IsNullOrWhiteSpace(response.Body))
                {
                    // Surface invalid responses explicitly so host callers do not quietly render misleading empty states.
                    _logger.LogError(
                        response.ApiCallDetails?.OriginalException,
                        "Elasticsearch query execution returned an invalid response. StatusCode={StatusCode}",
                        response.ApiCallDetails?.HttpStatusCode);

                    throw new InvalidOperationException("Elasticsearch query execution returned an invalid response.");
                }

                var result = ParseResponseBody(plan, requestBody, response.Body, stopwatch.Elapsed);
                _logger.LogInformation("Executed Elasticsearch query. IndexName={IndexName} Total={Total}", _indexName, result.Total);

                return result;
            }
            catch (Exception ex)
            {
                // Log the failure at the infrastructure boundary so the host can correlate Elasticsearch failures with planning diagnostics.
                _logger.LogError(ex, "Failed to execute the repository-owned query plan against Elasticsearch.");
                throw;
            }
        }

        /// <summary>
        /// Determines whether the supplied query plan contains any executable rule-driven or default clauses.
        /// </summary>
        /// <param name="plan">The query plan that should be inspected before Elasticsearch execution is attempted.</param>
        /// <returns><see langword="true" /> when the plan contains executable clauses; otherwise, <see langword="false" />.</returns>
        internal static bool HasExecutableClauses(QueryPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);

            // Consider both rule-shaped canonical model values and residual default contributions so model-only plans such as "latest solas" still execute.
            return plan.Defaults.Items.Count > 0
                || plan.Execution.Filters.Count > 0
                || plan.Execution.Boosts.Count > 0
                || plan.Model.Keywords.Count > 0
                || plan.Model.Authority.Count > 0
                || plan.Model.Region.Count > 0
                || plan.Model.Format.Count > 0
                || plan.Model.MajorVersion.Count > 0
                || plan.Model.MinorVersion.Count > 0
                || plan.Model.Category.Count > 0
                || plan.Model.Series.Count > 0
                || plan.Model.Instance.Count > 0
                || plan.Model.Title.Count > 0
                || !string.IsNullOrWhiteSpace(plan.Model.SearchText)
                || !string.IsNullOrWhiteSpace(plan.Model.Content);
        }

        /// <summary>
        /// Parses an Elasticsearch search response body into the repository-owned search result shape.
        /// </summary>
        /// <param name="plan">The query plan that produced the response.</param>
        /// <param name="requestBody">The raw Elasticsearch request body that was sent to the search engine.</param>
        /// <param name="responseBody">The raw Elasticsearch response body.</param>
        /// <param name="duration">The measured execution duration.</param>
        /// <returns>The parsed repository-owned search result.</returns>
        internal static QuerySearchResult ParseResponseBody(QueryPlan plan, string requestBody, string responseBody, TimeSpan duration)
        {
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentException.ThrowIfNullOrWhiteSpace(requestBody);
            ArgumentException.ThrowIfNullOrWhiteSpace(responseBody);

            // Deserialize the transport response into a focused internal envelope so the outer host never depends on Elasticsearch response types.
            var envelope = JsonSerializer.Deserialize<ElasticsearchSearchResponseEnvelope>(responseBody, SerializerOptions)
                ?? throw new InvalidOperationException("Elasticsearch query execution returned a response body that could not be deserialized.");

            var hits = envelope.Hits?.Hits.Select(MapHit).ToArray() ?? Array.Empty<QuerySearchHit>();
            var total = envelope.Hits?.Total?.Value ?? hits.LongLength;

            return new QuerySearchResult
            {
                Plan = plan,
                ElasticsearchRequestJson = requestBody,
                Hits = hits,
                Total = total,
                Duration = duration,
                SearchEngineDuration = envelope.TookMilliseconds is int tookMilliseconds ? TimeSpan.FromMilliseconds(tookMilliseconds) : null
            };
        }

        /// <summary>
        /// Maps one Elasticsearch hit envelope into the repository-owned hit shape.
        /// </summary>
        /// <param name="hit">The Elasticsearch hit envelope to map.</param>
        /// <returns>The mapped repository-owned hit.</returns>
        private static QuerySearchHit MapHit(ElasticsearchSearchHitEnvelope hit)
        {
            ArgumentNullException.ThrowIfNull(hit);

            // Deserialize the canonical source payload so the query UI receives a stable, repository-owned projection.
            var sourceDocument = hit.Source.ValueKind is JsonValueKind.Object
                ? hit.Source.Deserialize<ElasticsearchQueryDocument>(SerializerOptions)
                : null;

            var title = sourceDocument?.Title.FirstOrDefault() ?? "(untitled)";
            var type = sourceDocument?.Category.FirstOrDefault() ?? sourceDocument?.Format.FirstOrDefault();
            var region = sourceDocument?.Region.FirstOrDefault();
            JsonElement? raw = hit.Source.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? null : hit.Source.Clone();

            return new QuerySearchHit
            {
                Title = title,
                Type = type,
                Region = region,
                MatchedFields = hit.MatchedQueries.ToArray(),
                Raw = raw
            };
        }
    }
}
