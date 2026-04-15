using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace UKHO.Search.Ingestion.Tests.TestEnrichers
{
    /// <summary>
    /// Test enricher that removes all canonical security tokens so validation-path tests can exercise the dead-letter safeguard.
    /// </summary>
    internal sealed class SecurityTokenClearingEnricher : IIngestionEnricher
    {
        private readonly int _ordinal;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityTokenClearingEnricher"/> class.
        /// </summary>
        /// <param name="ordinal">The enricher ordering value used by the pipeline.</param>
        public SecurityTokenClearingEnricher(int ordinal = 0)
        {
            // Store the requested ordinal so tests can compose this enricher with other enrichers deterministically.
            _ordinal = ordinal;
        }

        /// <summary>
        /// Gets the ordering value used when the pipeline sorts enrichers before execution.
        /// </summary>
        public int Ordinal => _ordinal;

        /// <summary>
        /// Removes all retained canonical security tokens from the supplied document.
        /// </summary>
        /// <param name="request">The ingestion request that triggered enrichment.</param>
        /// <param name="document">The canonical document being enriched.</param>
        /// <param name="cancellationToken">The cancellation token supplied by the pipeline.</param>
        /// <returns>A completed task once the canonical token set has been cleared.</returns>
        public Task TryBuildEnrichmentAsync(IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(document);

            // Clear the retained canonical set to simulate a pipeline stage that leaves the document without any indexable security tokens.
            document.SecurityTokens.Clear();
            return Task.CompletedTask;
        }
    }
}
