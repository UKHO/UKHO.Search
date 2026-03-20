using System.Reflection;
using System.Text;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Shouldly;
using UKHO.Search.Geo;
using UKHO.Search.Infrastructure.Ingestion.Elastic;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Elastic
{
    public sealed class ElasticsearchBulkIndexClientGeoJsonPayloadTests
    {
        [Fact]
        public void Bulk_payload_serializes_single_geo_polygon_as_geojson_polygon()
        {
            var client = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri("http://localhost:9200")));
            var document = CreateMinimalDocument("doc-1");
            document.AddGeoPolygon(GeoPolygon.Create(new[]
            {
                GeoCoordinate.Create(1d, 2d),
                GeoCoordinate.Create(3d, 2d),
                GeoCoordinate.Create(3d, 4d),
                GeoCoordinate.Create(1d, 2d)
            }));

            var bulkRequest = new BulkRequest("idx-canonical")
            {
                Operations = new BulkOperationsCollection
                {
                    new BulkIndexOperation<CanonicalIndexDocument>(ElasticsearchBulkIndexClient.CreateIndexDocument(document))
                    {
                        Id = document.Id
                    }
                }
            };

            var payload = Serialize(client, bulkRequest);
            var lines = payload.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines.Length.ShouldBe(2);

            using var operationJson = JsonDocument.Parse(lines[1]);
            operationJson.RootElement.GetProperty("geoPolygons")
                         .GetProperty("type")
                         .GetString()
                         .ShouldBe("Polygon");
            operationJson.RootElement.GetProperty("geoPolygons")
                         .GetProperty("coordinates")[0][0][0]
                         .GetDouble()
                         .ShouldBe(1d);
            operationJson.RootElement.GetProperty("geoPolygons")
                         .GetProperty("coordinates")[0][0][1]
                         .GetDouble()
                         .ShouldBe(2d);
            payload.ShouldNotContain("\"rings\"");
            payload.ShouldNotContain("\"longitude\"");
            payload.ShouldNotContain("\"latitude\"");
            operationJson.RootElement.GetProperty("provider")
                         .GetString()
                         .ShouldBe("file-share");
        }

        [Fact]
        public void Bulk_payload_serializes_multiple_geo_polygons_as_geojson_multipolygon()
        {
            var client = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri("http://localhost:9200")));
            var document = CreateMinimalDocument("doc-1");
            document.AddGeoPolygon(GeoPolygon.Create(new[]
            {
                GeoCoordinate.Create(1d, 2d),
                GeoCoordinate.Create(3d, 2d),
                GeoCoordinate.Create(3d, 4d),
                GeoCoordinate.Create(1d, 2d)
            }));
            document.AddGeoPolygon(GeoPolygon.Create(new[]
            {
                GeoCoordinate.Create(10d, 20d),
                GeoCoordinate.Create(30d, 20d),
                GeoCoordinate.Create(30d, 40d),
                GeoCoordinate.Create(10d, 20d)
            }));

            var bulkRequest = new BulkRequest("idx-canonical")
            {
                Operations = new BulkOperationsCollection
                {
                    new BulkIndexOperation<CanonicalIndexDocument>(ElasticsearchBulkIndexClient.CreateIndexDocument(document))
                    {
                        Id = document.Id
                    }
                }
            };

            var payload = Serialize(client, bulkRequest);
            var lines = payload.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines.Length.ShouldBe(2);

            using var operationJson = JsonDocument.Parse(lines[1]);
            operationJson.RootElement.GetProperty("geoPolygons")
                         .GetProperty("type")
                         .GetString()
                         .ShouldBe("MultiPolygon");
            operationJson.RootElement.GetProperty("geoPolygons")
                         .GetProperty("coordinates")[0][0][0][0]
                         .GetDouble()
                         .ShouldBe(1d);
            operationJson.RootElement.GetProperty("geoPolygons")
                         .GetProperty("coordinates")[1][0][0][0]
                         .GetDouble()
                         .ShouldBe(10d);
        }

        [Fact]
        public void Bulk_payload_omits_geo_polygons_when_document_has_no_polygons()
        {
            var client = new ElasticsearchClient(new ElasticsearchClientSettings(new Uri("http://localhost:9200")));
            var document = CreateMinimalDocument("doc-1");

            var bulkRequest = new BulkRequest("idx-canonical")
            {
                Operations = new BulkOperationsCollection
                {
                    new BulkIndexOperation<CanonicalIndexDocument>(ElasticsearchBulkIndexClient.CreateIndexDocument(document))
                    {
                        Id = document.Id
                    }
                }
            };

            var payload = Serialize(client, bulkRequest);
            var lines = payload.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines.Length.ShouldBe(2);

            using var operationJson = JsonDocument.Parse(lines[1]);
            operationJson.RootElement.TryGetProperty("geoPolygons", out _)
                         .ShouldBeFalse();
        }

        private static CanonicalDocument CreateMinimalDocument(string documentId)
        {
            return CanonicalDocument.CreateMinimal(documentId, "file-share", new IndexRequest(documentId, Array.Empty<IngestionProperty>(), ["t1"], DateTimeOffset.UnixEpoch, new IngestionFileList()), DateTimeOffset.UnixEpoch);
        }

        private static string Serialize(ElasticsearchClient client, BulkRequest request)
        {
            using var ms = new MemoryStream();

            var transport = client.Transport;
            var serializer = GetAnyPropertyValue(transport, "RequestResponseSerializer") ?? GetAnyPropertyValue(transport, "Serializer") ?? GetAnyPropertyValue(GetAnyPropertyValue(transport, "Configuration"), "RequestResponseSerializer") ?? GetAnyPropertyValue(GetAnyPropertyValue(transport, "Configuration"), "Serializer");

            serializer.ShouldNotBeNull("Could not locate Elasticsearch request/response serializer on the configured client.");

            InvokeSerialize(serializer!, request, ms);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static object? GetAnyPropertyValue(object? instance, string propertyName)
        {
            if (instance is null)
            {
                return null;
            }

            var type = instance.GetType();

            var prop = type.GetProperty(propertyName);
            if (prop is not null)
            {
                return prop.GetValue(instance);
            }

            foreach (var iface in type.GetInterfaces())
            {
                var ifaceProp = iface.GetProperty(propertyName);
                var getter = ifaceProp?.GetGetMethod();
                if (getter is not null)
                {
                    return getter.Invoke(instance, null);
                }
            }

            return null;
        }

        private static void InvokeSerialize(object serializer, object value, Stream stream)
        {
            var allSerializeMethods = serializer.GetType()
                                                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                                .Where(m => string.Equals(m.Name, "Serialize", StringComparison.Ordinal))
                                                .ToArray();

            var methods = allSerializeMethods.Where(m => !m.ContainsGenericParameters)
                                             .ToArray();

            var twoParam = methods.FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                return parameters.Length == 2 && parameters[1].ParameterType == typeof(Stream);
            });

            if (twoParam is not null)
            {
                twoParam.Invoke(serializer, new[] { value, stream });
                return;
            }

            var threeParam = methods.FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                return parameters.Length == 3 && parameters[1].ParameterType == typeof(Stream) && parameters[2].ParameterType.IsEnum;
            });

            if (threeParam is not null)
            {
                var formattingValue = Enum.ToObject(threeParam.GetParameters()[2].ParameterType, 0);
                threeParam.Invoke(serializer, new[] { value, stream, formattingValue });
                return;
            }

            var generic = allSerializeMethods.FirstOrDefault(m => m.IsGenericMethodDefinition && m.GetParameters().Length >= 2 && m.GetParameters()[1].ParameterType == typeof(Stream));
            if (generic is null)
            {
                throw new InvalidOperationException($"Could not find a compatible Serialize overload on serializer type '{serializer.GetType().FullName}'.");
            }

            var constructed = generic.MakeGenericMethod(value.GetType());
            var constructedParameters = constructed.GetParameters();
            if (constructedParameters.Length == 2)
            {
                constructed.Invoke(serializer, new[] { value, stream });
                return;
            }

            if (constructedParameters.Length == 3 && constructedParameters[2].ParameterType.IsEnum)
            {
                var formattingValue = Enum.ToObject(constructedParameters[2].ParameterType, 0);
                constructed.Invoke(serializer, new[] { value, stream, formattingValue });
                return;
            }

            throw new InvalidOperationException($"Found generic Serialize method on '{serializer.GetType().FullName}' but could not invoke a supported overload.");
        }
    }
}
