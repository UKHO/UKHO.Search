using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Tests.TestSupport;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class RulesEnginePayloadSelectionRegressionTests
    {
        [Fact]
        public void AddItem_is_preferred_when_both_AddItem_and_UpdateItem_are_present()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
            {
              "schemaVersion": "1.0",
              "rules": {
                "file-share": [
                  {
                    "id": "payload-selection",
                    "enabled": true,
                    "if": { "id": "add-id" },
                    "then": { "keywords": { "add": [ "matched" ] } }
                  }
                ]
              }
            }
            """);

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var request = new IngestionRequest
            {
                RequestType = IngestionRequestType.AddItem,
                AddItem = new AddItemRequest("add-id", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList()),
                UpdateItem = new UpdateItemRequest("update-id", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList())
            };

            var document = CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);

            engine.Apply("file-share", request, document);

            document.Keywords.ShouldContain("matched");
        }

        [Fact]
        public void UpdateItem_is_used_when_AddItem_is_absent()
        {
            using var temp = new TempRulesRoot();
            temp.WriteRulesFile("""
            {
              "schemaVersion": "1.0",
              "rules": {
                "file-share": [
                  {
                    "id": "payload-selection-update",
                    "enabled": true,
                    "if": { "id": "update-id" },
                    "then": { "keywords": { "add": [ "matched" ] } }
                  }
                ]
              }
            }
            """);

            using var provider = CreateProvider(temp.RootPath);
            var engine = provider.GetRequiredService<IIngestionRulesEngine>();

            var update = new UpdateItemRequest("update-id", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList());
            var request = new IngestionRequest(IngestionRequestType.UpdateItem, addItem: null, update, deleteItem: null, updateAcl: null);

            var document = CanonicalDocument.CreateMinimal("doc-1", Array.Empty<IngestionProperty>(), DateTimeOffset.UnixEpoch);

            engine.Apply("file-share", request, document);

            document.Keywords.ShouldContain("matched");
        }

        private static ServiceProvider CreateProvider(string contentRootPath)
        {
            var services = new ServiceCollection();

            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { ContentRootPath = contentRootPath });
            services.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug));
            services.AddIngestionServices();

            return services.BuildServiceProvider();
        }
    }
}
