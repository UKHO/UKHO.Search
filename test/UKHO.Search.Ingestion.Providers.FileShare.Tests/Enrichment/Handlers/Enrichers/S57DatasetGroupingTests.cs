using Shouldly;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers.Enrichers;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Enrichment.Handlers.Enrichers
{
    public sealed class S57DatasetGroupingTests
    {
        [Fact]
        public void GroupDatasets_returns_only_groups_with_000_present_and_orders_members()
        {
            var paths = new[]
            {
                @"c:\data\A.002",
                @"c:\data\A.000",
                @"c:\data\A.001",
                @"c:\data\B.001",
                @"c:\data\B.002",
                @"c:\data\nope.txt"
            };

            var groups = S57DatasetGrouper.GroupDatasets(paths);

            groups.Count.ShouldBe(1);
            groups[0].BaseName.ShouldBe("A");
            groups[0].EntryPointPath.ShouldBe(@"c:\data\A.000");
            groups[0].MemberPaths.ShouldBe(new[]
            {
                @"c:\data\A.000",
                @"c:\data\A.001",
                @"c:\data\A.002"
            });
        }

        [Fact]
        public void GroupDatasets_groups_are_case_insensitive_by_base_name_and_extension()
        {
            var paths = new[]
            {
                @"c:\data\Mixed.000",
                @"c:\data\mixed.001",
                @"c:\data\MIXED.002",
            };

            var groups = S57DatasetGrouper.GroupDatasets(paths);

            groups.Count.ShouldBe(1);
            groups[0].BaseName.ShouldBe("Mixed");
            groups[0].MemberPaths.ShouldBe(new[]
            {
                @"c:\data\Mixed.000",
                @"c:\data\mixed.001",
                @"c:\data\MIXED.002"
            });
        }

        [Fact]
        public void GroupDatasets_ignores_non_numeric_extensions()
        {
            var paths = new[]
            {
                @"c:\data\A.000",
                @"c:\data\A.abc",
                @"c:\data\A.12",
                @"c:\data\A.000x",
                @"c:\data\A.001",
            };

            var groups = S57DatasetGrouper.GroupDatasets(paths);

            groups.Count.ShouldBe(1);
            groups[0].MemberPaths.ShouldBe(new[]
            {
                @"c:\data\A.000",
                @"c:\data\A.001"
            });
        }

        [Fact]
        public void GroupDatasets_when_multiple_000_candidates_exist_chooses_lexicographically_first_entry_point()
        {
            var paths = new[]
            {
                @"c:\data\a.000",
                @"c:\data\A.000",
                @"c:\data\A.001",
            };

            var groups = S57DatasetGrouper.GroupDatasets(paths);

            groups.Count.ShouldBe(1);
            groups[0].EntryPointPath.ShouldBe(@"c:\data\A.000");
        }
    }
}
