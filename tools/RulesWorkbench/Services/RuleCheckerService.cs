using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using RulesWorkbench.Contracts;

namespace RulesWorkbench.Services
{
    public sealed class RuleCheckerService
    {
        private const string BusinessUnitNameProperty = "BusinessUnitName";
        private const string FileShareProviderName = "file-share";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private readonly AppConfigRulesSnapshotStore _appConfigRulesSnapshotStore;
        private readonly BatchPayloadLoader _batchPayloadLoader;
        private readonly EvaluationPayloadMapper _payloadMapper;
        private readonly RuleEvaluationService _ruleEvaluationService;
        private readonly ILogger<RuleCheckerService> _logger;

        public RuleCheckerService(
            BatchPayloadLoader batchPayloadLoader,
            EvaluationPayloadMapper payloadMapper,
            RuleEvaluationService ruleEvaluationService,
            AppConfigRulesSnapshotStore appConfigRulesSnapshotStore,
            ILogger<RuleCheckerService> logger)
        {
            _batchPayloadLoader = batchPayloadLoader;
            _payloadMapper = payloadMapper;
            _ruleEvaluationService = ruleEvaluationService;
            _appConfigRulesSnapshotStore = appConfigRulesSnapshotStore;
            _logger = logger;
        }

        public async Task<RuleCheckerRunResultDto> CheckBatchAsync(string batchId, CancellationToken cancellationToken)
        {
            return await CheckBatchAsync(batchId, null, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<RuleCheckerRunResultDto> CheckBatchAsync(string batchId, string? selectedBusinessUnitName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(batchId))
            {
                return RuleCheckerRunResultDto.Failure("Batch id is required.");
            }

            var loadResult = await _batchPayloadLoader.TryLoadAsync(batchId, cancellationToken)
                .ConfigureAwait(false);

            if (!loadResult.Found || loadResult.Payload is null)
            {
                return RuleCheckerRunResultDto.Failure(loadResult.Error ?? $"Batch '{batchId}' not found.");
            }

            return await CheckPayloadAsync(loadResult.Payload, selectedBusinessUnitName, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<RuleCheckerRunResultDto> CheckPayloadAsync(EvaluationPayloadDto payload, CancellationToken cancellationToken)
        {
            return await CheckPayloadAsync(payload, null, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<RuleCheckerRunResultDto> CheckPayloadAsync(EvaluationPayloadDto payload, string? selectedBusinessUnitName, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(payload);

            cancellationToken.ThrowIfCancellationRequested();

            var (request, validation) = _payloadMapper.TryMapToIndexRequest(payload);
            if (!validation.IsValid || request is null)
            {
                var error = validation.Errors.Count > 0
                    ? string.Join(Environment.NewLine, validation.Errors)
                    : "Payload could not be mapped to an IndexRequest.";
                return RuleCheckerRunResultDto.Failure(error);
            }

            _logger.LogInformation("Running checker evaluation. BatchId={BatchId}", payload.Id);
            var evaluationReport = await _ruleEvaluationService.EvaluateFileShareAsync(request, cancellationToken)
                .ConfigureAwait(false);

            var report = BuildReport(payload, selectedBusinessUnitName, evaluationReport);
            return RuleCheckerRunResultDto.Success(report);
        }

        private RuleCheckerReportDto BuildReport(EvaluationPayloadDto payload, string? selectedBusinessUnitName, RuleEvaluationReportDto evaluationReport)
        {
            var businessUnitName = string.IsNullOrWhiteSpace(selectedBusinessUnitName)
                ? TryGetBusinessUnitName(payload)
                : selectedBusinessUnitName.Trim();
            var matchedRuleIds = new HashSet<string>(evaluationReport.MatchedRules.Select(x => x.RuleId), StringComparer.OrdinalIgnoreCase);
            var candidateRules = GetCandidateRules(businessUnitName, matchedRuleIds);
            var missingRequiredFields = GetMissingRequiredFields(evaluationReport.FinalDocumentJson);
            var runtimeWarnings = new List<string>(evaluationReport.RuntimeWarnings);

            if (string.IsNullOrWhiteSpace(businessUnitName))
            {
                runtimeWarnings.Add("BusinessUnitName is missing from the payload; candidate rule matching may be incomplete.");
            }

            if (candidateRules.Count == 0 && !string.IsNullOrWhiteSpace(businessUnitName))
            {
                runtimeWarnings.Add($"No candidate rules were identified for business unit '{businessUnitName}'.");
                _logger.LogWarning("No candidate rules were identified for checker evaluation. BatchId={BatchId} BusinessUnitName={BusinessUnitName}", payload.Id, businessUnitName);
            }

            if (candidateRules.Count > 0 && evaluationReport.MatchedRules.Count == 0 && missingRequiredFields.Count > 0)
            {
                runtimeWarnings.Add($"Candidate rules were identified for business unit '{businessUnitName}', but none matched the batch.");
                _logger.LogWarning("Candidate rules were identified but none matched. BatchId={BatchId} BusinessUnitName={BusinessUnitName} CandidateRuleCount={CandidateRuleCount}", payload.Id, businessUnitName, candidateRules.Count);
            }

            if (evaluationReport.MatchedRules.Count > 0 && missingRequiredFields.Count > 0)
            {
                runtimeWarnings.Add($"Rules matched, but the required fields are still missing: {string.Join(", ", missingRequiredFields)}.");
                _logger.LogWarning("Rules matched but required fields remain missing. BatchId={BatchId} BusinessUnitName={BusinessUnitName} MatchedRuleCount={MatchedRuleCount} MissingRequiredFields={MissingRequiredFields}", payload.Id, businessUnitName, evaluationReport.MatchedRules.Count, missingRequiredFields);
            }

            var status = DetermineStatus(missingRequiredFields, evaluationReport.ValidationErrors, runtimeWarnings);

            _logger.LogInformation("Checker report built. BatchId={BatchId} BusinessUnitName={BusinessUnitName} CandidateRuleCount={CandidateRuleCount} MatchedRuleCount={MatchedRuleCount} MissingRequiredFieldCount={MissingRequiredFieldCount} Status={Status}", payload.Id, businessUnitName, candidateRules.Count, evaluationReport.MatchedRules.Count, missingRequiredFields.Count, status);

            return new RuleCheckerReportDto
            {
                Batch = new RuleCheckerBatchSummaryDto
                {
                    BatchId = payload.Id,
                    CreatedOn = payload.Timestamp,
                    BusinessUnitName = businessUnitName ?? string.Empty,
                },
                Status = status,
                Payload = payload,
                RawPayloadJson = JsonSerializer.Serialize(payload, _jsonOptions),
                FinalDocumentJson = evaluationReport.FinalDocumentJson,
                MissingRequiredFields = missingRequiredFields,
                MatchedRules = evaluationReport.MatchedRules,
                CandidateRules = candidateRules,
                ValidationErrors = evaluationReport.ValidationErrors,
                RuntimeWarnings = runtimeWarnings,
            };
        }

        private List<RuleCheckerCandidateRuleDto> GetCandidateRules(string? businessUnitName, HashSet<string> matchedRuleIds)
        {
            if (string.IsNullOrWhiteSpace(businessUnitName))
            {
                return new List<RuleCheckerCandidateRuleDto>();
            }

            var normalizedBusinessUnitName = businessUnitName.ToLowerInvariant();
            return _appConfigRulesSnapshotStore.GetRules(null)
                .Where(x => string.Equals(x.Provider, FileShareProviderName, StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(x.Context, normalizedBusinessUnitName, StringComparison.Ordinal))
                .Select(x => new RuleCheckerCandidateRuleDto
                {
                    RuleId = x.RuleId,
                    Description = TryGetDescription(x.RuleJson),
                    RuleJson = x.RuleJson?.ToJsonString(_jsonOptions) ?? string.Empty,
                    IsMatched = matchedRuleIds.Contains(x.RuleId),
                })
                .ToList();
        }

        private static string? TryGetDescription(JsonNode? ruleJson)
        {
            if (ruleJson is not JsonObject obj)
            {
                return null;
            }

            return obj["description"]?.GetValue<string?>() ?? obj["Description"]?.GetValue<string?>();
        }

        private static IReadOnlyList<string> GetMissingRequiredFields(string finalDocumentJson)
        {
            if (string.IsNullOrWhiteSpace(finalDocumentJson))
            {
                return new[] { "Title", "Category", "Series", "Instance" };
            }

            JsonNode? node;
            try
            {
                node = JsonNode.Parse(finalDocumentJson);
            }
            catch (JsonException)
            {
                return new[] { "Title", "Category", "Series", "Instance" };
            }

            var missing = new List<string>();
            if (!HasValues(node, "title"))
            {
                missing.Add("Title");
            }

            if (!HasValues(node, "category"))
            {
                missing.Add("Category");
            }

            if (!HasValues(node, "series"))
            {
                missing.Add("Series");
            }

            if (!HasValues(node, "instance"))
            {
                missing.Add("Instance");
            }

            return missing;
        }

        private static bool HasValues(JsonNode? node, string propertyName)
        {
            if (node is not JsonObject obj)
            {
                return false;
            }

            var value = obj[propertyName] ?? obj[propertyName.ToUpperInvariant()] ?? obj[char.ToUpperInvariant(propertyName[0]) + propertyName[1..]];
            if (value is JsonArray array)
            {
                return array.Count > 0;
            }

            return value is not null;
        }

        private static RuleCheckerStatus DetermineStatus(IReadOnlyList<string> missingRequiredFields, IReadOnlyList<string> validationErrors, IReadOnlyList<string> runtimeWarnings)
        {
            if (missingRequiredFields.Count > 0)
            {
                return RuleCheckerStatus.Fail;
            }

            if (validationErrors.Count > 0 || runtimeWarnings.Count > 0)
            {
                return RuleCheckerStatus.Warning;
            }

            return RuleCheckerStatus.Ok;
        }

        private static string? TryGetBusinessUnitName(EvaluationPayloadDto payload)
        {
            var property = payload.Properties.FirstOrDefault(x => string.Equals(x.Name, BusinessUnitNameProperty, StringComparison.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(property?.Value)
                ? null
                : property.Value.Trim();
        }
    }
}
