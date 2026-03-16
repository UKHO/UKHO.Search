using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RulesWorkbench.Contracts;
using UKHO.Search.Infrastructure.Ingestion.Rules;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;

namespace RulesWorkbench.Services
{
    public sealed class RuleEvaluationService
    {
        private const string ProviderName = "file-share";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly IIngestionRulesEngine _engine;
        private readonly IIngestionRulesCatalog _catalog;
        private readonly ILogger<RuleEvaluationService> _logger;
        private readonly RulesSnapshotStore _rulesSnapshotStore;

        public RuleEvaluationService(
            IIngestionRulesEngine engine,
            IIngestionRulesCatalog catalog,
            RulesSnapshotStore rulesSnapshotStore,
            ILogger<RuleEvaluationService> logger)
        {
            _engine = engine;
            _catalog = catalog;
            _rulesSnapshotStore = rulesSnapshotStore;
            _logger = logger;
        }

        public async Task<RuleEvaluationReportDto> EvaluateFileShareAsync(IndexRequest indexRequest, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(indexRequest);

            cancellationToken.ThrowIfCancellationRequested();

            // Ensure catalog is loaded before we temporarily provide the workbench rules file.
            // The catalog uses ContentRootPath + ingestion-rules.json. Workbench maintains an in-memory copy,
            // but the engine is currently coupled to catalog rules.
            _catalog.EnsureLoaded();

            var validationErrors = new List<string>();

            var sourceCopy = new IndexRequest(indexRequest.Id,
                indexRequest.Properties is null ? new IngestionPropertyList() : new IngestionPropertyList(indexRequest.Properties),
                indexRequest.SecurityTokens?.ToArray() ?? Array.Empty<string>(),
                indexRequest.Timestamp,
                indexRequest.Files ?? new IngestionFileList());

            var request = new IngestionRequest(IngestionRequestType.IndexItem, sourceCopy, deleteItem: null, updateAcl: null);

            var document = new CanonicalDocument
            {
                Id = sourceCopy.Id,
                Timestamp = sourceCopy.Timestamp,
                Source = sourceCopy
            };

            try
            {
                _logger.LogInformation("Running rules evaluation. ProviderName={ProviderName} RequestId={RequestId}", ProviderName, indexRequest.Id);

                _engine.Apply(ProviderName, request, document);

                // TODO (F1 follow-up): capture matched/fired rule IDs and action summaries.
                // Engine only logs matched IDs today.

                var docJson = JsonSerializer.Serialize(document, JsonOptions);

                return await Task.FromResult(new RuleEvaluationReportDto
                {
                    ProviderName = ProviderName,
                    FinalDocumentJson = docJson,
                    ValidationErrors = validationErrors,
                    RuntimeWarnings = new List<string>(),
                    MatchedRules = new List<RuleEvaluationMatchedRuleDto>()
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Rules evaluation failed. ProviderName={ProviderName} RequestId={RequestId}", ProviderName, indexRequest.Id);
                validationErrors.Add(ex.Message);

                return await Task.FromResult(new RuleEvaluationReportDto
                {
                    ProviderName = ProviderName,
                    FinalDocumentJson = string.Empty,
                    ValidationErrors = validationErrors,
                    RuntimeWarnings = new List<string>(),
                    MatchedRules = new List<RuleEvaluationMatchedRuleDto>()
                });
            }
        }
    }
}
