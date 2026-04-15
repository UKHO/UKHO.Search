using System.Reflection;
using System.Runtime.CompilerServices;
using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    /// <summary>
    /// Verifies provider and minimal-construction behaviour for canonical documents.
    /// </summary>
    public sealed class CanonicalDocumentProviderTests
    {
        /// <summary>
        /// Confirms that minimal construction preserves the provider identifier.
        /// </summary>
        [Fact]
        public void CreateMinimal_sets_provider()
        {
            var document = CanonicalDocument.CreateMinimal(
                "doc-1",
                "file-share",
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
                DateTimeOffset.UnixEpoch);

            document.Provider.ShouldBe("file-share");
        }

        /// <summary>
        /// Confirms that minimal construction also normalizes request security tokens into canonical state.
        /// </summary>
        [Fact]
        public void CreateMinimal_copies_normalized_security_tokens_from_source()
        {
            var source = new IndexRequest(
                "doc-1",
                Array.Empty<IngestionProperty>(),
                ["Token-B", " TOKEN-A ", "token-b"],
                DateTimeOffset.UnixEpoch,
                new IngestionFileList());

            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", source, DateTimeOffset.UnixEpoch);

            document.Source.SecurityTokens.ShouldBe(["Token-B", " TOKEN-A ", "token-b"]);
            document.SecurityTokens.ShouldBe(["token-a", "token-b"]);
        }

        /// <summary>
        /// Confirms that minimal construction still rejects missing provider values.
        /// </summary>
        [Fact]
        public void CreateMinimal_throws_when_provider_is_missing()
        {
            Should.Throw<ArgumentException>(() => CanonicalDocument.CreateMinimal(
                "doc-1",
                " ",
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
                DateTimeOffset.UnixEpoch));
        }

        /// <summary>
        /// Verifies that <see cref="CanonicalDocument.Provider"/> remains init-only.
        /// </summary>
        [Fact]
        public void Provider_is_init_only()
        {
            var providerProperty = typeof(CanonicalDocument).GetProperty(nameof(CanonicalDocument.Provider), BindingFlags.Instance | BindingFlags.Public);

            providerProperty.ShouldNotBeNull();
            providerProperty!.SetMethod.ShouldNotBeNull();
            providerProperty.SetMethod.ReturnParameter.GetRequiredCustomModifiers().ShouldContain(typeof(IsExternalInit));
        }
    }
}
