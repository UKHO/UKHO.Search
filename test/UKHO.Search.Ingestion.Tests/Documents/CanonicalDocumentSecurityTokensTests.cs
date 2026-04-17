using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    /// <summary>
    /// Verifies normalization and mutation behaviour for canonical security tokens.
    /// </summary>
    public sealed class CanonicalDocumentSecurityTokensTests
    {
        /// <summary>
        /// Confirms that single-token mutation trims, lowercases, de-duplicates, and sorts canonical security tokens.
        /// </summary>
        [Fact]
        public void AddSecurityToken_normalizes_dedupes_and_orders_values()
        {
            // Create a minimal canonical document so the test exercises the same runtime shape used by ingestion.
            var document = CreateDocument();

            // Add repeated values with different whitespace and casing to prove the mutator normalizes into one set.
            document.AddSecurityToken("Token-C");
            document.AddSecurityToken(" token-a ");
            document.AddSecurityToken("TOKEN-B");
            document.AddSecurityToken("token-c");

            // The canonical set should contain one lowercased copy of each token in deterministic sorted order.
            document.SecurityTokens.ShouldBe(["seed-token", "token-a", "token-b", "token-c"]);
        }

        /// <summary>
        /// Confirms that the single-token overload ignores null, empty, and whitespace-only inputs.
        /// </summary>
        [Fact]
        public void AddSecurityToken_ignores_null_or_whitespace_values()
        {
            var document = CreateDocument();

            // These inputs should not survive canonical normalization.
            document.AddSecurityToken((string?)null);
            document.AddSecurityToken(string.Empty);
            document.AddSecurityToken("   ");

            document.SecurityTokens.ShouldBe(["seed-token"]);
        }

        /// <summary>
        /// Confirms that the enumerable overload tolerates a null collection and reuses the single-value normalization path.
        /// </summary>
        [Fact]
        public void AddSecurityToken_collection_overload_handles_null_and_reuses_single_value_path()
        {
            var document = CreateDocument();

            // A null collection should be ignored, and the remaining values should be normalized item by item.
            document.AddSecurityToken((IEnumerable<string?>?)null);
            document.AddSecurityToken(["Token-B", " token-a ", null, "TOKEN-B", " "]);

            document.SecurityTokens.ShouldBe(["seed-token", "token-a", "token-b"]);
        }

        /// <summary>
        /// Builds the minimal canonical document used by the security-token tests.
        /// </summary>
        /// <returns>A canonical document seeded with one request-derived security token.</returns>
        private static CanonicalDocument CreateDocument()
        {
            // Seed the request with one valid token so the tests start from the same minimal canonical state used by runtime dispatch.
            return CanonicalDocument.CreateMinimal(
                "doc-1",
                "file-share",
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["Seed-Token"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
                DateTimeOffset.UnixEpoch);
        }
    }
}
