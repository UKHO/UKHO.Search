namespace QueryServiceHost.Models
{
    public class FilterChip
    {
        public required string GroupName { get; init; }

        public required string Value { get; init; }

        public string Label => $"{GroupName}: {Value}";
    }
}
