namespace QueryServiceHost.Models
{
    public class FacetGroup
    {
        public required string Name { get; init; }

        public IReadOnlyList<FacetValue> Values { get; init; } = Array.Empty<FacetValue>();

        public bool IsCollapsed { get; set; }
    }
}
