using Shouldly;
using UKHO.Search.Geo;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.DeadLetter;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.DeadLetter
{
    public sealed class IndexOperationPayloadDiagnosticsTests
    {
        [Fact]
        public void Create_WhenPayloadIsUpsertOperation_ShouldIncludeCanonicalDocumentAndGeoPolygons()
        {
            IndexOperation payload = new UpsertOperation("doc-1", CreateCanonicalDocument("doc-1"));

            var diagnostics = DeadLetterPayloadDiagnosticsBuilder.Create(payload);

            diagnostics.RuntimePayloadType.ShouldBe(typeof(UpsertOperation).FullName);
            diagnostics.PayloadSnapshot.ShouldNotBeNull();

            var snapshot = diagnostics.PayloadSnapshot!.Value;
            snapshot.GetProperty("documentId")
                    .GetString()
                    .ShouldBe("doc-1");
            snapshot.GetProperty("document")
                    .GetProperty("id")
                    .GetString()
                    .ShouldBe("doc-1");
            snapshot.GetProperty("document")
                    .GetProperty("geoPolygons")[0]
                    .GetProperty("rings")[0][0]
                    .GetProperty("longitude")
                    .GetDouble()
                    .ShouldBe(1d);
            snapshot.GetProperty("document")
                    .GetProperty("geoPolygons")[0]
                    .GetProperty("rings")[0][0]
                    .GetProperty("latitude")
                    .GetDouble()
                    .ShouldBe(2d);
        }

        [Fact]
        public void Create_WhenPayloadIsDeleteOrAclUpdateOperation_ShouldPreserveOperationIdentityAndMembers()
        {
            IndexOperation deletePayload = new DeleteOperation("doc-delete");
            IndexOperation aclPayload = new AclUpdateOperation("doc-acl", ["token-a", "token-b"]);

            var deleteDiagnostics = DeadLetterPayloadDiagnosticsBuilder.Create(deletePayload);
            var aclDiagnostics = DeadLetterPayloadDiagnosticsBuilder.Create(aclPayload);

            deleteDiagnostics.RuntimePayloadType.ShouldBe(typeof(DeleteOperation).FullName);
            deleteDiagnostics.PayloadSnapshot.ShouldNotBeNull();
            deleteDiagnostics.PayloadSnapshot!.Value.GetProperty("documentId")
                              .GetString()
                              .ShouldBe("doc-delete");

            aclDiagnostics.RuntimePayloadType.ShouldBe(typeof(AclUpdateOperation).FullName);
            aclDiagnostics.PayloadSnapshot.ShouldNotBeNull();
            aclDiagnostics.PayloadSnapshot!.Value.GetProperty("documentId")
                           .GetString()
                           .ShouldBe("doc-acl");
            aclDiagnostics.PayloadSnapshot!.Value.GetProperty("securityTokens")[0]
                           .GetString()
                           .ShouldBe("token-a");
            aclDiagnostics.PayloadSnapshot!.Value.GetProperty("securityTokens")[1]
                           .GetString()
                           .ShouldBe("token-b");
        }

        private static CanonicalDocument CreateCanonicalDocument(string documentId)
        {
            var document = CanonicalDocument.CreateMinimal(documentId, "file-share", new IndexRequest(documentId, Array.Empty<IngestionProperty>(), ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList()), DateTimeOffset.UnixEpoch);
            document.AddGeoPolygon(GeoPolygon.Create(new[]
            {
                GeoCoordinate.Create(1d, 2d),
                GeoCoordinate.Create(3d, 2d),
                GeoCoordinate.Create(3d, 4d),
                GeoCoordinate.Create(1d, 2d)
            }));

            return document;
        }
    }
}
