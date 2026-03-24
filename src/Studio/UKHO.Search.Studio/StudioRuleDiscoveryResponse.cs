namespace UKHO.Search.Studio
{
    public sealed class StudioRuleDiscoveryResponse
    {
        public required string SchemaVersion { get; init; }

        public required IReadOnlyList<StudioProviderRulesResponse> Providers { get; init; }
    }
}
