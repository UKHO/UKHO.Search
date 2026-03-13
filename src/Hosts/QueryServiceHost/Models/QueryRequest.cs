namespace QueryServiceHost.Models
{
    public class QueryRequest
    {
        public string? QueryText { get; init; }

        public IReadOnlyDictionary<string, IReadOnlySet<string>> SelectedFacets { get; init; } =
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
    }
}
