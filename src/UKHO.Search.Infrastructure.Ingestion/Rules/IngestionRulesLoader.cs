using System.Text.Json;
using Microsoft.Extensions.Hosting;
using UKHO.Search.Infrastructure.Ingestion.Rules.Model;

namespace UKHO.Search.Infrastructure.Ingestion.Rules
{
    internal sealed class IngestionRulesLoader
    {
        private const string RulesFileName = "ingestion-rules.json";

        private readonly IHostEnvironment _hostEnvironment;

        public IngestionRulesLoader(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public RulesetDto Load()
        {
            var rulesPath = Path.Combine(_hostEnvironment.ContentRootPath, RulesFileName);
            if (!File.Exists(rulesPath))
            {
                throw new IngestionRulesValidationException($"Missing required rules file '{RulesFileName}' at '{rulesPath}'.");
            }

            var json = File.ReadAllText(rulesPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new IngestionRulesValidationException($"Rules file '{RulesFileName}' is empty.");
            }

            try
            {
                var dto = JsonSerializer.Deserialize<RulesetDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (dto is null)
                {
                    throw new IngestionRulesValidationException($"Rules file '{RulesFileName}' could not be parsed.");
                }

                return dto;
            }
            catch (JsonException ex)
            {
                throw new IngestionRulesValidationException($"Rules file '{RulesFileName}' contains invalid JSON.", innerException: ex);
            }
        }
    }
}
