namespace QueryServiceHost.Models
{
    public class FacetValue
    {
        public required string Value { get; init; }

        public long Count { get; init; }

        public bool IsSelected { get; set; }
    }
}
