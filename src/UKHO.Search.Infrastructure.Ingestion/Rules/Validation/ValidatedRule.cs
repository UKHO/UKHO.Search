using System.Text.Json;
using UKHO.Search.Infrastructure.Ingestion.Rules.Model;

namespace UKHO.Search.Infrastructure.Ingestion.Rules.Validation
{
    internal sealed class ValidatedRule
    {
        public required string Id { get; init; }

        public string? Description { get; init; }

        public required bool Enabled { get; init; }

        public required JsonElement Predicate { get; init; }

        public required ThenDto Then { get; init; }
    }
}