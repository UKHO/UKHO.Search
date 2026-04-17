using Shouldly;
using UKHO.Search.Query.Models;
using Xunit;

namespace UKHO.Search.Query.Tests
{
    /// <summary>
    /// Verifies the defaults of the rule-driven execution contracts introduced for filters and boosts.
    /// </summary>
    public sealed class QueryRuleExecutionContractsTests
    {
        /// <summary>
        /// Verifies that execution directives default to empty filter and boost collections.
        /// </summary>
        [Fact]
        public void QueryExecutionDirectives_defaults_to_empty_filters_and_boosts()
        {
            // Construct the execution contract directly because its defaults must be safe before any rule engine runs.
            var directives = new QueryExecutionDirectives();

            directives.Filters.ShouldBeEmpty();
            directives.Boosts.ShouldBeEmpty();
            directives.Sorts.ShouldBeEmpty();
        }
    }
}
