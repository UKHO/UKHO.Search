using System.Reflection;
using System.Runtime.CompilerServices;
using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Documents
{
    public sealed class CanonicalDocumentProviderTests
    {
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

        [Fact]
        public void CreateMinimal_throws_when_provider_is_missing()
        {
            Should.Throw<ArgumentException>(() => CanonicalDocument.CreateMinimal(
                "doc-1",
                " ",
                new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList()),
                DateTimeOffset.UnixEpoch));
        }

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
