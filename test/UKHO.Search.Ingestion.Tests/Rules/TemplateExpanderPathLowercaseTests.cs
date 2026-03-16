using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation;
using UKHO.Search.Infrastructure.Ingestion.Rules.Templating;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Rules
{
    public sealed class TemplateExpanderPathLowercaseTests
    {
        [Fact]
        public void Path_variable_resolves_when_path_uses_mixed_case_property_key()
        {
            var payload = new IndexRequest("doc-1", new IngestionPropertyList
            {
                new IngestionProperty { Name = "weeknumber", Type = IngestionPropertyType.String, Value = "10" }
            }, ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var resolver = new IngestionRulesPathResolver();
            var context = new TemplateContext(payload, resolver, val: Array.Empty<string>());
            var expander = new IngestionRulesTemplateExpander();

            expander.ExpandToInt("toInt($path:properties[\"WeEkNuMbEr\"])", context)
                    .ShouldBe(new[] { 10 });
        }

        [Fact]
        public void Path_variable_treats_missing_lowercase_key_as_missing()
        {
            var payload = new IndexRequest("doc-1", new IngestionPropertyList(), ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var resolver = new IngestionRulesPathResolver();
            var context = new TemplateContext(payload, resolver, val: Array.Empty<string>());
            var expander = new IngestionRulesTemplateExpander();

            expander.ExpandToInt("toInt($path:properties[\"Week Number\"])", context)
                    .ShouldBe(Array.Empty<int>());
        }

        [Fact]
        public void Path_variable_supports_property_key_containing_spaces()
        {
            var payload = new IndexRequest("doc-1", new IngestionPropertyList
            {
                new IngestionProperty { Name = "week number", Type = IngestionPropertyType.String, Value = "10" }
            }, ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var resolver = new IngestionRulesPathResolver();
            var context = new TemplateContext(payload, resolver, val: Array.Empty<string>());
            var expander = new IngestionRulesTemplateExpander();

            expander.Expand("$path:properties[\"week number\"]", context)
                    .ShouldBe(new[] { "10" });

            expander.ExpandToInt("toInt($path:properties[\"week number\"])", context)
                    .ShouldBe(new[] { 10 });
        }

        [Fact]
        public void Path_variable_supports_multiple_variables_in_a_single_template()
        {
            var payload = new IndexRequest("doc-1", new IngestionPropertyList
            {
                new IngestionProperty { Name = "a b", Type = IngestionPropertyType.String, Value = "left" },
                new IngestionProperty { Name = "c", Type = IngestionPropertyType.String, Value = "right" }
            }, ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var resolver = new IngestionRulesPathResolver();
            var context = new TemplateContext(payload, resolver, val: Array.Empty<string>());
            var expander = new IngestionRulesTemplateExpander();

            expander.Expand("$path:properties[\"a b\"]-$path:properties[\"c\"]", context)
                    .ShouldBe(new[] { "left-right" });
        }

        [Fact]
        public void Path_variable_still_terminates_at_whitespace_outside_brackets()
        {
            var payload = new IndexRequest("doc-1", new IngestionPropertyList
            {
                new IngestionProperty { Name = "a b", Type = IngestionPropertyType.String, Value = "left" },
                new IngestionProperty { Name = "c", Type = IngestionPropertyType.String, Value = "right" }
            }, ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var resolver = new IngestionRulesPathResolver();
            var context = new TemplateContext(payload, resolver, val: Array.Empty<string>());
            var expander = new IngestionRulesTemplateExpander();

            expander.Expand("$path:properties[\"a b\"] $path:properties[\"c\"]", context)
                    .ShouldBe(new[] { "left right" });
        }

        [Fact]
        public void Path_variable_supports_prefix_and_suffix_text_when_key_contains_spaces()
        {
            var payload = new IndexRequest("doc-1", new IngestionPropertyList
            {
                new IngestionProperty { Name = "a b", Type = IngestionPropertyType.String, Value = "VAL" }
            }, ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var resolver = new IngestionRulesPathResolver();
            var context = new TemplateContext(payload, resolver, val: Array.Empty<string>());
            var expander = new IngestionRulesTemplateExpander();

            expander.Expand("prefix-$path:properties[\"a b\"]-suffix", context)
                    .ShouldBe(new[] { "prefix-VAL-suffix" });
        }

        [Fact]
        public void Malformed_path_variable_does_not_throw_and_produces_no_values()
        {
            var payload = new IndexRequest("doc-1", new IngestionPropertyList
            {
                new IngestionProperty { Name = "a b", Type = IngestionPropertyType.String, Value = "VAL" }
            }, ["t"], DateTimeOffset.UnixEpoch, new IngestionFileList());

            var resolver = new IngestionRulesPathResolver();
            var context = new TemplateContext(payload, resolver, val: Array.Empty<string>());
            var expander = new IngestionRulesTemplateExpander();

            expander.Expand("$path:properties[\"a b\"", context)
                    .ShouldBeEmpty();

            expander.ExpandToInt("toInt($path:properties[\"week number\" )", context)
                    .ShouldBeEmpty();
        }
    }
}
