using System.Text;
using System.Text.Json;
using UKHO.Search.Query.Models;

namespace UKHO.Search.Infrastructure.Query.Search
{
    /// <summary>
    /// Translates repository-owned query plans into deterministic Elasticsearch request bodies.
    /// </summary>
    internal static class ElasticsearchQueryMapper
    {
        /// <summary>
        /// Creates the Elasticsearch search request body for the supplied repository-owned query plan.
        /// </summary>
        /// <param name="plan">The query plan that should be translated into Elasticsearch JSON.</param>
        /// <returns>The Elasticsearch search request body.</returns>
        internal static string CreateRequestBody(QueryPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);

            // Build the raw JSON directly so the mapper can be tested deterministically without depending on client-side DSL serialization details.
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

            writer.WriteStartObject();
            writer.WriteNumber("size", 25);
            writer.WritePropertyName("query");

            if (!HasExecutableClauses(plan))
            {
                WriteMatchNoneQuery(writer);
            }
            else
            {
                WriteExecutableQuery(writer, plan);
            }

            if (plan.Execution.Sorts.Count > 0)
            {
                WriteSorts(writer, plan.Execution.Sorts);
            }

            writer.WriteEndObject();
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>
        /// Writes a match-none query body for an empty plan.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch query JSON.</param>
        private static void WriteMatchNoneQuery(Utf8JsonWriter writer)
        {
            // Use match_none so the executor can safely handle empty plans without falling back to a broad match-all query.
            writer.WriteStartObject();
            writer.WritePropertyName("match_none");
            writer.WriteStartObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        /// <summary>
        /// Determines whether the supplied query plan contains any executable model or default clauses.
        /// </summary>
        /// <param name="plan">The query plan that should be inspected.</param>
        /// <returns><see langword="true" /> when the plan contains executable clauses; otherwise, <see langword="false" />.</returns>
        private static bool HasExecutableClauses(QueryPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);

            // Consider both rule-shaped canonical model values and residual default contributions when deciding whether the query can execute.
            return plan.Defaults.Items.Count > 0
                || plan.Execution.Filters.Count > 0
                || plan.Execution.Boosts.Count > 0
                || plan.Model.Keywords.Count > 0
                || plan.Model.Authority.Count > 0
                || plan.Model.Region.Count > 0
                || plan.Model.Format.Count > 0
                || plan.Model.MajorVersion.Count > 0
                || plan.Model.MinorVersion.Count > 0
                || plan.Model.Category.Count > 0
                || plan.Model.Series.Count > 0
                || plan.Model.Instance.Count > 0
                || plan.Model.Title.Count > 0
                || !string.IsNullOrWhiteSpace(plan.Model.SearchText)
                || !string.IsNullOrWhiteSpace(plan.Model.Content);
        }

        /// <summary>
        /// Writes the executable query body from both the canonical model and the supplied default contributions.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch query JSON.</param>
        /// <param name="plan">The query plan whose canonical model and defaults should become Elasticsearch should-clauses.</param>
        private static void WriteExecutableQuery(Utf8JsonWriter writer, QueryPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);

            // Translate rule-shaped canonical model values and residual defaults into one bool query so both surfaces can contribute independently.
            writer.WriteStartObject();
            writer.WritePropertyName("bool");
            writer.WriteStartObject();

            if (plan.Execution.Filters.Count > 0)
            {
                WriteFilters(writer, plan.Execution.Filters);
            }

            writer.WritePropertyName("should");
            writer.WriteStartArray();

            WriteModelContributions(writer, plan.Model);
            WriteBoosts(writer, plan.Execution.Boosts);

            foreach (var contribution in plan.Defaults.Items)
            {
                WriteContribution(writer, contribution);
            }

            writer.WriteEndArray();
            writer.WriteNumber("minimum_should_match", 1);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes the execution-time filter directives as non-scoring filter clauses.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch query JSON.</param>
        /// <param name="filters">The filter directives that should be translated.</param>
        private static void WriteFilters(Utf8JsonWriter writer, IReadOnlyCollection<QueryExecutionFilterDirective> filters)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(filters);

            // Emit filters separately from should clauses so rule-driven constraints do not contribute to scoring.
            writer.WritePropertyName("filter");
            writer.WriteStartArray();

            foreach (var filter in filters)
            {
                WriteFilterClause(writer, filter);
            }

            writer.WriteEndArray();
        }

        /// <summary>
        /// Writes the execution-time boost directives as additional scoring clauses.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch query JSON.</param>
        /// <param name="boosts">The boost directives that should be translated.</param>
        private static void WriteBoosts(Utf8JsonWriter writer, IReadOnlyCollection<QueryExecutionBoostDirective> boosts)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(boosts);

            // Append explicit boost clauses after canonical model clauses so rule-authored scoring adjustments remain visible and deterministic.
            foreach (var boost in boosts)
            {
                WriteBoostClause(writer, boost);
            }
        }

        /// <summary>
        /// Writes the canonical-model contributions owned by the query plan.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch clause JSON.</param>
        /// <param name="model">The canonical query model whose field values should be translated into executable clauses.</param>
        private static void WriteModelContributions(Utf8JsonWriter writer, CanonicalQueryModel model)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(model);

            // Emit canonical field clauses first so explicit rule-driven intent appears ahead of residual defaults in the generated request body.
            WriteStringTermsClause(writer, "keywords", model.Keywords);
            WriteStringTermsClause(writer, "authority", model.Authority);
            WriteStringTermsClause(writer, "region", model.Region);
            WriteStringTermsClause(writer, "format", model.Format);
            WriteIntegerTermsClause(writer, "majorVersion", model.MajorVersion);
            WriteIntegerTermsClause(writer, "minorVersion", model.MinorVersion);
            WriteStringTermsClause(writer, "category", model.Category);
            WriteStringTermsClause(writer, "series", model.Series);
            WriteStringTermsClause(writer, "instance", model.Instance);
            WriteStringTermsClause(writer, "title", model.Title);

            if (!string.IsNullOrWhiteSpace(model.SearchText))
            {
                WriteAnalyzedMatchClause(writer, "searchText", model.SearchText, boost: 2.0d);
            }

            if (!string.IsNullOrWhiteSpace(model.Content))
            {
                WriteAnalyzedMatchClause(writer, "content", model.Content, boost: 1.0d);
            }
        }

        /// <summary>
        /// Writes one default contribution as an Elasticsearch clause.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch clause JSON.</param>
        /// <param name="contribution">The contribution to translate.</param>
        private static void WriteContribution(Utf8JsonWriter writer, QueryDefaultFieldContribution contribution)
        {
            ArgumentNullException.ThrowIfNull(contribution);

            // Route the contribution into the exact DSL shape required by its matching mode.
            writer.WriteStartObject();

            switch (contribution.MatchingMode)
            {
                case QueryDefaultMatchingMode.ExactTerms:
                    WriteExactTermsContribution(writer, contribution);
                    break;

                case QueryDefaultMatchingMode.AnalyzedText:
                    WriteAnalyzedTextContribution(writer, contribution);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported default matching mode '{contribution.MatchingMode}'.");
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes one canonical-model string-terms clause when the supplied value set is non-empty.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch clause JSON.</param>
        /// <param name="fieldName">The canonical field name targeted by the clause.</param>
        /// <param name="values">The string values that should be matched against the field.</param>
        private static void WriteStringTermsClause(Utf8JsonWriter writer, string fieldName, IReadOnlyCollection<string> values)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
            ArgumentNullException.ThrowIfNull(values);

            if (values.Count == 0)
            {
                return;
            }

            // Emit a terms query for the explicit canonical model values so rule-driven keyword and taxonomy intent reaches Elasticsearch.
            writer.WriteStartObject();
            writer.WritePropertyName("terms");
            writer.WriteStartObject();
            writer.WritePropertyName(fieldName);
            writer.WriteStartArray();

            foreach (var value in values)
            {
                writer.WriteStringValue(value);
            }

            writer.WriteEndArray();
            writer.WriteString("_name", fieldName);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes one canonical-model integer-terms clause when the supplied value set is non-empty.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch clause JSON.</param>
        /// <param name="fieldName">The canonical field name targeted by the clause.</param>
        /// <param name="values">The integer values that should be matched against the field.</param>
        private static void WriteIntegerTermsClause(Utf8JsonWriter writer, string fieldName, IReadOnlyCollection<int> values)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
            ArgumentNullException.ThrowIfNull(values);

            if (values.Count == 0)
            {
                return;
            }

            // Emit a terms query for numeric canonical model values such as recognized major and minor versions.
            writer.WriteStartObject();
            writer.WritePropertyName("terms");
            writer.WriteStartObject();
            writer.WritePropertyName(fieldName);
            writer.WriteStartArray();

            foreach (var value in values)
            {
                writer.WriteNumberValue(value);
            }

            writer.WriteEndArray();
            writer.WriteString("_name", fieldName);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes one analyzed match clause for an explicit canonical-model text contribution.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch clause JSON.</param>
        /// <param name="fieldName">The analyzed field targeted by the clause.</param>
        /// <param name="text">The analyzed text that should be matched.</param>
        /// <param name="boost">The boost that should be applied to the clause.</param>
        private static void WriteAnalyzedMatchClause(Utf8JsonWriter writer, string fieldName, string text, double boost)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
            ArgumentException.ThrowIfNullOrWhiteSpace(text);

            // Reuse the same match-query shape used by residual defaults so explicit analyzed model intent remains deterministic and testable.
            writer.WriteStartObject();
            writer.WritePropertyName("match");
            writer.WriteStartObject();
            writer.WritePropertyName(fieldName);
            writer.WriteStartObject();
            writer.WriteString("query", text);
            writer.WriteNumber("boost", boost);
            writer.WriteString("_name", fieldName);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes one execution-time filter clause.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch clause JSON.</param>
        /// <param name="filter">The filter directive to translate.</param>
        private static void WriteFilterClause(Utf8JsonWriter writer, QueryExecutionFilterDirective filter)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(filter);

            // Translate integer-backed and string-backed filters into deterministic terms queries with no explicit boost.
            writer.WriteStartObject();
            writer.WritePropertyName("terms");
            writer.WriteStartObject();
            writer.WritePropertyName(filter.FieldName);
            writer.WriteStartArray();

            if (filter.IntegerValues.Count > 0)
            {
                foreach (var value in filter.IntegerValues)
                {
                    writer.WriteNumberValue(value);
                }
            }
            else
            {
                foreach (var value in filter.StringValues)
                {
                    writer.WriteStringValue(value);
                }
            }

            writer.WriteEndArray();
            writer.WriteString("_name", $"filter:{filter.FieldName}");
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes one execution-time boost clause.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch clause JSON.</param>
        /// <param name="boost">The boost directive to translate.</param>
        private static void WriteBoostClause(Utf8JsonWriter writer, QueryExecutionBoostDirective boost)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(boost);

            // Translate the explicit boost according to its matching mode so query rules can shape scoring independently from defaults.
            writer.WriteStartObject();

            if (boost.MatchingMode == QueryExecutionBoostMatchingMode.AnalyzedText)
            {
                writer.WritePropertyName("match");
                writer.WriteStartObject();
                writer.WritePropertyName(boost.FieldName);
                writer.WriteStartObject();
                writer.WriteString("query", boost.Text);
                writer.WriteNumber("boost", boost.Weight);
                writer.WriteString("_name", $"boost:{boost.FieldName}");
                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.WriteEndObject();
                return;
            }

            writer.WritePropertyName("terms");
            writer.WriteStartObject();
            writer.WritePropertyName(boost.FieldName);
            writer.WriteStartArray();

            if (boost.IntegerValues.Count > 0)
            {
                foreach (var value in boost.IntegerValues)
                {
                    writer.WriteNumberValue(value);
                }
            }
            else
            {
                foreach (var value in boost.StringValues)
                {
                    writer.WriteStringValue(value);
                }
            }

            writer.WriteEndArray();
            writer.WriteNumber("boost", boost.Weight);
            writer.WriteString("_name", $"boost:{boost.FieldName}");
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes one exact-terms contribution.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch clause JSON.</param>
        /// <param name="contribution">The contribution to translate.</param>
        private static void WriteExactTermsContribution(Utf8JsonWriter writer, QueryDefaultFieldContribution contribution)
        {
            // Emit a terms query so residual tokens target the lowercase keyword field directly.
            writer.WritePropertyName("terms");
            writer.WriteStartObject();
            writer.WritePropertyName(contribution.FieldName);
            writer.WriteStartArray();

            foreach (var term in contribution.Terms)
            {
                writer.WriteStringValue(term);
            }

            writer.WriteEndArray();
            writer.WriteNumber("boost", contribution.Boost);
            writer.WriteString("_name", contribution.FieldName);
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes one analyzed-text contribution.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch clause JSON.</param>
        /// <param name="contribution">The contribution to translate.</param>
        private static void WriteAnalyzedTextContribution(Utf8JsonWriter writer, QueryDefaultFieldContribution contribution)
        {
            // Emit a match query so residual text targets the analyzed text fields with explicit boosts.
            writer.WritePropertyName("match");
            writer.WriteStartObject();
            writer.WritePropertyName(contribution.FieldName);
            writer.WriteStartObject();
            writer.WriteString("query", contribution.Text);
            writer.WriteNumber("boost", contribution.Boost);
            writer.WriteString("_name", contribution.FieldName);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes the execution-time sort directives.
        /// </summary>
        /// <param name="writer">The JSON writer receiving the Elasticsearch request JSON.</param>
        /// <param name="sorts">The ordered sort directives to translate.</param>
        private static void WriteSorts(Utf8JsonWriter writer, IReadOnlyCollection<QueryExecutionSortDirective> sorts)
        {
            // Keep sort translation deterministic so later rule-driven sorts can be tested in a stable order.
            writer.WritePropertyName("sort");
            writer.WriteStartArray();

            foreach (var sort in sorts)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(sort.FieldName);
                writer.WriteStartObject();
                writer.WriteString("order", sort.Direction == QueryExecutionSortDirection.Descending ? "desc" : "asc");
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}
