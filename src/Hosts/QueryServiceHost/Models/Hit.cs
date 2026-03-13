using System.Text.Json;

namespace QueryServiceHost.Models
{
    public class Hit
    {
        public required string Title { get; init; }

        public string? Type { get; init; }

        public string? Region { get; init; }

        public IReadOnlyList<string> MatchedFields { get; init; } = Array.Empty<string>();

        public JsonElement? Raw { get; init; }
    }
}
