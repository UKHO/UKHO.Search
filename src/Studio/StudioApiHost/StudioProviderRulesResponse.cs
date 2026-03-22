namespace StudioApiHost
{
    public sealed class StudioProviderRulesResponse
    {
        public required string ProviderName { get; init; }

        public required string DisplayName { get; init; }

        public string? Description { get; init; }

        public required IReadOnlyList<StudioRuleSummaryResponse> Rules { get; init; }
    }
}
