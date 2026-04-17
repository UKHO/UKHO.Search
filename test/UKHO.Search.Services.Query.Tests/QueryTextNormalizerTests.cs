using Shouldly;
using UKHO.Search.Services.Query.Normalization;
using Xunit;

namespace UKHO.Search.Services.Query.Tests
{
    /// <summary>
    /// Verifies the deterministic normalization behavior used by the query planning pipeline.
    /// </summary>
    public sealed class QueryTextNormalizerTests
    {
        /// <summary>
        /// Verifies that normalization lowercases, trims, collapses whitespace, and seeds residual content deterministically.
        /// </summary>
        [Fact]
        public void Normalize_when_query_contains_mixed_case_and_repeated_whitespace_returns_clean_snapshot()
        {
            // Create the real normalizer because this test verifies the concrete normalization behavior introduced by work item one.
            var normalizer = new QueryTextNormalizer();

            // Normalize a query that exercises lowercasing, trimming, and repeated-whitespace collapse.
            var snapshot = normalizer.Normalize("  LaTeSt   SOLAS\t  Notice  ");

            snapshot.RawText.ShouldBe("  LaTeSt   SOLAS\t  Notice  ");
            snapshot.NormalizedText.ShouldBe("  latest   solas\t  notice  ");
            snapshot.CleanedText.ShouldBe("latest solas notice");
            snapshot.Tokens.ShouldBe(["latest", "solas", "notice"]);
            snapshot.ResidualTokens.ShouldBe(["latest", "solas", "notice"]);
            snapshot.ResidualText.ShouldBe("latest solas notice");
        }

        /// <summary>
        /// Verifies that normalization turns a null query into a stable empty snapshot.
        /// </summary>
        [Fact]
        public void Normalize_when_query_is_null_returns_empty_snapshot()
        {
            // Create the real normalizer because the empty-input contract is part of the concrete implementation.
            var normalizer = new QueryTextNormalizer();

            // Normalize a null query so the planner can rely on non-null strings and collections.
            var snapshot = normalizer.Normalize(null);

            snapshot.RawText.ShouldBeEmpty();
            snapshot.NormalizedText.ShouldBeEmpty();
            snapshot.CleanedText.ShouldBeEmpty();
            snapshot.Tokens.ShouldBeEmpty();
            snapshot.ResidualTokens.ShouldBeEmpty();
            snapshot.ResidualText.ShouldBeEmpty();
        }
    }
}
