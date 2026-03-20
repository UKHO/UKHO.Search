using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RulesWorkbench.Contracts;
using RulesWorkbench.Services;
using Shouldly;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using Xunit;

namespace RulesWorkbench.Tests
{
    public sealed class RuleCheckerServiceTests
    {
        [Fact]
        public async Task CheckPayloadAsync_returns_fail_and_candidate_rules_for_matching_business_unit()
        {
            var service = CreateService(new StubIngestionRulesEngine(new RuleEvaluationReportDto
            {
                ProviderName = "file-share",
                FinalDocumentJson = """
                {
                  "category": [ "charts" ]
                }
                """,
                MatchedRules = new List<RuleEvaluationMatchedRuleDto>
                {
                    new() { RuleId = "bu-admiralty-rule-1", Description = "Admiralty rule", Summary = "Added category." }
                }
            }));

            var payload = new EvaluationPayloadDto
            {
                Id = "batch-1",
                Timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                SecurityTokens = new List<string> { "public" },
                Properties = new List<EvaluationPayloadPropertyDto>
                {
                    new() { Name = "BusinessUnitName", Type = "String", Value = "Admiralty" },
                    new() { Name = "CatalogueName", Type = "String", Value = "Demo" }
                },
                Files = new List<EvaluationPayloadFileDto>
                {
                    new() { Filename = "demo.000", MimeType = "application/octet-stream", Size = 10, Timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z") }
                }
            };

            var result = await service.CheckPayloadAsync(payload, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Report.ShouldNotBeNull();
            result.Report!.Status.ShouldBe(RuleCheckerStatus.Fail);
            result.Report.Batch.BusinessUnitName.ShouldBe("Admiralty");
            result.Report.MissingRequiredFields.ShouldBe(new[] { "Title", "Series", "Instance" });
            result.Report.MatchedRules.Select(x => x.RuleId).ShouldBe(new[] { "bu-admiralty-rule-1" });
            result.Report.CandidateRules.Select(x => x.RuleId).ShouldBe(new[] { "bu-admiralty-rule-1", "bu-admiralty-rule-2" });
            result.Report.CandidateRules.Count(x => x.IsMatched).ShouldBe(1);
            result.Report.CandidateRules.Single(x => x.RuleId == "bu-admiralty-rule-2").RuleJson.ShouldContain("\"id\": \"bu-admiralty-rule-2\"");
            result.Report.RuntimeWarnings.ShouldContain(x => x.Contains("rules matched", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task CheckPayloadAsync_returns_ok_when_required_fields_are_present()
        {
            var service = CreateService(new StubIngestionRulesEngine(new RuleEvaluationReportDto
            {
                ProviderName = "file-share",
                FinalDocumentJson = """
                {
                  "title": [ "Admiralty Week 10" ],
                  "category": [ "charts" ],
                  "series": [ "series-a" ],
                  "instance": [ "week-10" ]
                }
                """,
                MatchedRules = new List<RuleEvaluationMatchedRuleDto>()
            }));

            var payload = new EvaluationPayloadDto
            {
                Id = "batch-2",
                Timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                SecurityTokens = new List<string> { "public" },
                Properties = new List<EvaluationPayloadPropertyDto>
                {
                    new() { Name = "BusinessUnitName", Type = "String", Value = "Admiralty" }
                },
                Files = new List<EvaluationPayloadFileDto>()
            };

            var result = await service.CheckPayloadAsync(payload, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Report.ShouldNotBeNull();
            result.Report!.Status.ShouldBe(RuleCheckerStatus.Ok);
            result.Report.MissingRequiredFields.ShouldBeEmpty();
        }

        [Fact]
        public async Task CheckPayloadAsync_returns_fail_when_title_is_missing_even_if_other_required_fields_are_present()
        {
            var service = CreateService(new StubIngestionRulesEngine(new RuleEvaluationReportDto
            {
                ProviderName = "file-share",
                FinalDocumentJson = """
                {
                  "category": [ "charts" ],
                  "series": [ "series-a" ],
                  "instance": [ "week-10" ]
                }
                """,
                MatchedRules = new List<RuleEvaluationMatchedRuleDto>()
            }));

            var payload = new EvaluationPayloadDto
            {
                Id = "batch-2a",
                Timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                SecurityTokens = new List<string> { "public" },
                Properties = new List<EvaluationPayloadPropertyDto>
                {
                    new() { Name = "BusinessUnitName", Type = "String", Value = "Admiralty" }
                },
                Files = new List<EvaluationPayloadFileDto>()
            };

            var result = await service.CheckPayloadAsync(payload, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Report.ShouldNotBeNull();
            result.Report!.Status.ShouldBe(RuleCheckerStatus.Fail);
            result.Report.MissingRequiredFields.ShouldBe(new[] { "Title" });
        }

        [Fact]
        public async Task CheckPayloadAsync_uses_selected_business_unit_name_for_candidate_rules()
        {
            var service = CreateService(new StubIngestionRulesEngine(new RuleEvaluationReportDto
            {
                ProviderName = "file-share",
                FinalDocumentJson = """
                {
                  "category": [ "charts" ]
                }
                """,
                MatchedRules = new List<RuleEvaluationMatchedRuleDto>()
            }));

            var payload = new EvaluationPayloadDto
            {
                Id = "batch-3",
                Timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                SecurityTokens = new List<string> { "public" },
                Properties = new List<EvaluationPayloadPropertyDto>(),
                Files = new List<EvaluationPayloadFileDto>()
            };

            var result = await service.CheckPayloadAsync(payload, "Admiralty", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Report.ShouldNotBeNull();
            result.Report!.Batch.BusinessUnitName.ShouldBe("Admiralty");
            result.Report.CandidateRules.Select(x => x.RuleId).ShouldBe(new[] { "bu-admiralty-rule-1", "bu-admiralty-rule-2" });
            result.Report.RuntimeWarnings.ShouldNotContain(x => x.Contains("BusinessUnitName is missing", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task CheckPayloadAsync_does_not_match_candidate_rules_by_filename_prefix_when_context_differs()
        {
            var service = CreateService(new StubIngestionRulesEngine(new RuleEvaluationReportDto
            {
                ProviderName = "file-share",
                FinalDocumentJson = """
                {
                  "category": [ "charts" ]
                }
                """,
                MatchedRules = new List<RuleEvaluationMatchedRuleDto>()
            }));

            var payload = new EvaluationPayloadDto
            {
                Id = "batch-4",
                Timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                SecurityTokens = new List<string> { "public" },
                Properties = new List<EvaluationPayloadPropertyDto>(),
                Files = new List<EvaluationPayloadFileDto>()
            };

            var result = await service.CheckPayloadAsync(payload, "adds", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Report.ShouldNotBeNull();
            result.Report!.CandidateRules.Select(x => x.RuleId).ShouldBe(new[] { "some-unrelated-rule-id" });
            result.Report.CandidateRules.ShouldNotContain(x => x.RuleId == "bu-adds-s100-4-base-exchange-set-product-type");
        }

        [Fact]
        public async Task CheckPayloadAsync_matches_candidate_rules_by_context_even_when_filename_shape_is_not_informative()
        {
            var service = CreateService(new StubIngestionRulesEngine(new RuleEvaluationReportDto
            {
                ProviderName = "file-share",
                FinalDocumentJson = """
                {
                  "category": [ "charts" ]
                }
                """,
                MatchedRules = new List<RuleEvaluationMatchedRuleDto>()
            }));

            var payload = new EvaluationPayloadDto
            {
                Id = "batch-5",
                Timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                SecurityTokens = new List<string> { "public" },
                Properties = new List<EvaluationPayloadPropertyDto>(),
                Files = new List<EvaluationPayloadFileDto>()
            };

            var result = await service.CheckPayloadAsync(payload, "adds-s100", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Report.ShouldNotBeNull();
            result.Report!.CandidateRules.Select(x => x.RuleId).ShouldBe(new[] { "bu-adds-s100-4-base-exchange-set-product-type" });
        }

        [Fact]
        public async Task CheckPayloadAsync_when_candidate_rules_exist_but_none_match_adds_explainable_warning()
        {
            var service = CreateService(new StubIngestionRulesEngine(new RuleEvaluationReportDto
            {
                ProviderName = "file-share",
                FinalDocumentJson = "{}",
                MatchedRules = new List<RuleEvaluationMatchedRuleDto>()
            }));

            var payload = new EvaluationPayloadDto
            {
                Id = "batch-6",
                Timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                SecurityTokens = new List<string> { "public" },
                Properties = new List<EvaluationPayloadPropertyDto>(),
                Files = new List<EvaluationPayloadFileDto>()
            };

            var result = await service.CheckPayloadAsync(payload, "admiralty", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Report.ShouldNotBeNull();
            result.Report!.RuntimeWarnings.ShouldContain(x => x.Contains("none matched", StringComparison.OrdinalIgnoreCase));
        }

        private static RuleCheckerService CreateService(IIngestionRulesEngine engine)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["rules:file-share:bu-admiralty-rule-1"] = "{\"schemaVersion\":\"1.0\",\"rule\":{\"id\":\"bu-admiralty-rule-1\",\"context\":\"admiralty\",\"description\":\"Admiralty rule\",\"if\":{\"id\":\"batch-1\"},\"then\":{\"category\":{\"add\":[\"charts\"]}}}}",
                    ["rules:file-share:bu-admiralty-rule-2"] = "{\"schemaVersion\":\"1.0\",\"rule\":{\"id\":\"bu-admiralty-rule-2\",\"context\":\"admiralty\",\"description\":\"Second admiralty rule\",\"if\":{\"id\":\"batch-1\"},\"then\":{\"series\":{\"add\":[\"series-a\"]}}}}",
                    ["rules:file-share:bu-fisheries-rule-1"] = "{\"schemaVersion\":\"1.0\",\"rule\":{\"id\":\"bu-fisheries-rule-1\",\"context\":\"fisheries\",\"description\":\"Fisheries rule\",\"if\":{\"id\":\"batch-1\"},\"then\":{\"instance\":{\"add\":[\"instance-a\"]}}}}",
                    ["rules:file-share:bu-adds-s100-4-base-exchange-set-product-type"] = "{\"schemaVersion\":\"1.0\",\"rule\":{\"id\":\"bu-adds-s100-4-base-exchange-set-product-type\",\"context\":\"adds-s100\",\"description\":\"ADDS-S100 rule\",\"if\":{\"id\":\"batch-1\"},\"then\":{\"instance\":{\"add\":[\"instance-a\"]}}}}",
                    ["rules:file-share:some-unrelated-rule-id"] = "{\"schemaVersion\":\"1.0\",\"rule\":{\"id\":\"some-unrelated-rule-id\",\"context\":\"adds\",\"description\":\"ADDS rule with non-prefix id\",\"if\":{\"id\":\"batch-1\"},\"then\":{\"series\":{\"add\":[\"series-a\"]}}}}"
                })
                .Build();

            var validator = new SystemTextJsonRuleJsonValidator();
            var snapshotStore = new AppConfigRulesSnapshotStore(configuration, validator, NullLogger<AppConfigRulesSnapshotStore>.Instance);
            var payloadMapper = new EvaluationPayloadMapper();
            var evaluationService = new RuleEvaluationService(engine, new StubIngestionRulesCatalog(), NullLogger<RuleEvaluationService>.Instance);
            var loader = new BatchPayloadLoader(new SqlConnection("Server=(local);Database=doesnotmatter;Trusted_Connection=True;"), NullLogger<BatchPayloadLoader>.Instance);

            return new RuleCheckerService(loader, payloadMapper, evaluationService, snapshotStore, NullLogger<RuleCheckerService>.Instance);
        }

        private sealed class StubIngestionRulesCatalog : IIngestionRulesCatalog
        {
            public void EnsureLoaded()
            {
            }

            public IReadOnlyDictionary<string, IReadOnlyList<string>> GetRuleIdsByProvider()
            {
                return new Dictionary<string, IReadOnlyList<string>>();
            }
        }

        private sealed class StubIngestionRulesEngine : IIngestionRulesEngine
        {
            private readonly RuleEvaluationReportDto _report;

            public StubIngestionRulesEngine(RuleEvaluationReportDto report)
            {
                _report = report;
            }

            public void Apply(string providerName, UKHO.Search.Ingestion.Requests.IngestionRequest request, UKHO.Search.Ingestion.Pipeline.Documents.CanonicalDocument document)
            {
            }

            public IngestionRulesApplyReport ApplyWithReport(string providerName, UKHO.Search.Ingestion.Requests.IngestionRequest request, UKHO.Search.Ingestion.Pipeline.Documents.CanonicalDocument document)
            {
                if (!string.IsNullOrWhiteSpace(_report.FinalDocumentJson))
                {
                    var node = JsonNode.Parse(_report.FinalDocumentJson)!.AsObject();
                    foreach (var value in node["title"]?.AsArray().Select(x => x?.GetValue<string>()) ?? Enumerable.Empty<string?>())
                    {
                        document.AddTitle(value);
                    }

                    foreach (var value in node["category"]?.AsArray().Select(x => x?.GetValue<string>()) ?? Enumerable.Empty<string?>())
                    {
                        document.AddCategory(value);
                    }

                    foreach (var value in node["series"]?.AsArray().Select(x => x?.GetValue<string>()) ?? Enumerable.Empty<string?>())
                    {
                        document.AddSeries(value);
                    }

                    foreach (var value in node["instance"]?.AsArray().Select(x => x?.GetValue<string>()) ?? Enumerable.Empty<string?>())
                    {
                        document.AddInstance(value);
                    }
                }

                return new IngestionRulesApplyReport
                {
                    MatchedRules = _report.MatchedRules.Select(x => new IngestionRulesMatchedRule
                    {
                        RuleId = x.RuleId,
                        Description = x.Description,
                        Summary = x.Summary,
                    }).ToArray()
                };
            }
        }
    }
}
