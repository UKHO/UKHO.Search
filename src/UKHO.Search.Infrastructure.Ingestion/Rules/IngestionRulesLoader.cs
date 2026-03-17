using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Rules.Model;
using UKHO.Search.Infrastructure.Ingestion.Rules.Validation;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class IngestionRulesLoader
    {
        private readonly IngestionRulesSource _source;
        private readonly ILogger<IngestionRulesLoader> _logger;

        public IngestionRulesLoader(IngestionRulesSource source, ILogger<IngestionRulesLoader> logger)
        {
            _source = source;
            _logger = logger;
        }

        public RulesetDto Load()
        {
            return _source.LoadStrict();
        }

    }
}