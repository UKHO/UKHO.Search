using System.Text.Json;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Model
{
    internal sealed class RuleDto
    {
        public string? Id { get; set; }

        public string? Context { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public bool? Enabled { get; set; }

        public JsonElement If { get; set; }

        public JsonElement Match { get; set; }

        public ThenDto? Then { get; set; }
    }
}