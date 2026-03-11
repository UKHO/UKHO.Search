using System.Reflection;
using System.Text;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Elastic;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Elastic
{
    public sealed class CanonicalIndexDefinitionTests
    {
        [Fact]
        public void CreateIndex_request_includes_expected_mappings()
        {
            var settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"));
            var client = new ElasticsearchClient(settings);

            var definition = new CanonicalIndexDefinition();

            var request = definition.Configure(new CreateIndexRequestDescriptor("idx-canonical"));

            var json = Serialize(client, request);

            using var parsed = JsonDocument.Parse(json);

            if (!parsed.RootElement.TryGetProperty("mappings", out var mappings) && !parsed.RootElement.TryGetProperty("Mappings", out mappings))
            {
                throw new InvalidOperationException($"Expected create-index request JSON to include 'mappings'. JSON: {json}");
            }

            if (!mappings.TryGetProperty("properties", out var properties) && !mappings.TryGetProperty("Properties", out properties))
            {
                throw new InvalidOperationException($"Expected create-index request JSON to include 'mappings.properties'. JSON: {json}");
            }

            properties.GetProperty("source")
                      .GetProperty("enabled")
                      .GetBoolean()
                      .ShouldBeFalse();
            properties.GetProperty("keywords")
                      .GetProperty("type")
                      .GetString()
                      .ShouldBe("keyword");

            var searchText = properties.GetProperty("searchText");
            searchText.GetProperty("type")
                      .GetString()
                      .ShouldBe("text");
            searchText.GetProperty("analyzer")
                      .GetString()
                      .ShouldBe("english");

            var content = properties.GetProperty("content");
            content.GetProperty("type")
                   .GetString()
                   .ShouldBe("text");
            content.GetProperty("analyzer")
                   .GetString()
                   .ShouldBe("english");

            // 'object' mappings don't always emit an explicit 'type' property, so just assert the field is present.
            properties.TryGetProperty("facets", out var _)
                      .ShouldBeTrue();

            if (!mappings.TryGetProperty("dynamic_templates", out var dynamicTemplates) && !mappings.TryGetProperty("DynamicTemplates", out dynamicTemplates))
            {
                throw new InvalidOperationException($"Expected create-index request JSON to include 'mappings.dynamic_templates'. JSON: {json}");
            }

            dynamicTemplates.ValueKind.ShouldBe(JsonValueKind.Array);
            dynamicTemplates.GetArrayLength()
                            .ShouldBeGreaterThan(0);

            // Verify we have a dynamic template that maps facets.* as keyword
            var facetsTemplate = dynamicTemplates.EnumerateArray()
                                                 .SelectMany(t => t.EnumerateObject())
                                                 .FirstOrDefault(p => string.Equals(p.Name, "facets_as_keyword", StringComparison.Ordinal));
            facetsTemplate.Value.ValueKind.ShouldBe(JsonValueKind.Object);

            var pathMatch = facetsTemplate.Value.GetProperty("path_match");
            if (pathMatch.ValueKind == JsonValueKind.String)
            {
                pathMatch.GetString()
                         .ShouldBe("facets.*");
            }
            else
            {
                pathMatch.ValueKind.ShouldBe(JsonValueKind.Array);
                pathMatch.EnumerateArray()
                         .Select(x => x.GetString())
                         .ShouldContain("facets.*");
            }

            facetsTemplate.Value.GetProperty("mapping")
                          .GetProperty("type")
                          .GetString()
                          .ShouldBe("keyword");
        }

        private static string Serialize(ElasticsearchClient client, CreateIndexRequestDescriptor descriptor)
        {
            using var ms = new MemoryStream();

            var transport = client.Transport;
            var serializer = GetAnyPropertyValue(transport, "RequestResponseSerializer") ?? GetAnyPropertyValue(transport, "Serializer") ?? GetAnyPropertyValue(GetAnyPropertyValue(transport, "Configuration"), "RequestResponseSerializer") ?? GetAnyPropertyValue(GetAnyPropertyValue(transport, "Configuration"), "Serializer");

            serializer.ShouldNotBeNull("Could not locate Elasticsearch request/response serializer on the configured client.");

            var request = MaterializeRequest(descriptor);
            InvokeSerialize(serializer!, request, ms);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static object MaterializeRequest(CreateIndexRequestDescriptor descriptor)
        {
            var requestType = typeof(CreateIndexRequest);

            var toRequest = descriptor.GetType()
                                      .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                      .FirstOrDefault(m => m.GetParameters()
                                                            .Length == 0 && requestType.IsAssignableFrom(m.ReturnType));

            if (toRequest is null)
            {
                // Some Elastic client versions serialize descriptors directly; fall back to the descriptor itself.
                return descriptor;
            }

            var request = toRequest.Invoke(descriptor, null);
            return request ?? descriptor;
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
                                                .GetMethods()
                                                .Where(m => string.Equals(m.Name, "Serialize", StringComparison.Ordinal))
                                                .ToArray();

            var methods = allSerializeMethods.Where(m => !m.ContainsGenericParameters)
                                             .ToArray();

            var twoParam = methods.FirstOrDefault(m =>
            {
                var p = m.GetParameters();
                return p.Length == 2 && p[1].ParameterType == typeof(Stream);
            });

            if (twoParam is not null)
            {
                twoParam.Invoke(serializer, new[] { value, stream });
                return;
            }

            var threeParam = methods.FirstOrDefault(m =>
            {
                var p = m.GetParameters();
                return p.Length == 3 && p[1].ParameterType == typeof(Stream) && p[2].ParameterType.IsEnum;
            });

            if (threeParam is null)
            {
                // Fall back to generic Serialize<T>(T, Stream [, formatting])
                var generic = allSerializeMethods.FirstOrDefault(m => m.IsGenericMethodDefinition && m.GetParameters()
                                                                                                      .Length >= 2 && m.GetParameters()[1].ParameterType == typeof(Stream));
                if (generic is null)
                {
                    throw new InvalidOperationException($"Could not find a compatible Serialize overload on serializer type '{serializer.GetType().FullName}'.");
                }

                var constructed = generic.MakeGenericMethod(value.GetType());
                var p = constructed.GetParameters();
                if (p.Length == 2)
                {
                    constructed.Invoke(serializer, new[] { value, stream });
                    return;
                }

                if (p.Length == 3 && p[2].ParameterType.IsEnum)
                {
                    var formattingValue = Enum.ToObject(p[2].ParameterType, 0);
                    constructed.Invoke(serializer, new[] { value, stream, formattingValue });
                    return;
                }

                throw new InvalidOperationException($"Found generic Serialize method on '{serializer.GetType().FullName}' but could not invoke a supported overload.");
            }

            var formatting = Enum.ToObject(threeParam.GetParameters()[2].ParameterType, 0);
            threeParam.Invoke(serializer, new[] { value, stream, formatting });
        }
    }
}