namespace StudioApiHost
{
    public sealed class StudioRuleSummaryResponse
    {
        public required string Id { get; init; }

        public string? Context { get; init; }

        public string? Title { get; init; }

        public string? Description { get; init; }

        public required bool Enabled { get; init; }
    }
}
