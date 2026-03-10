namespace UKHO.Search.Infrastructure.Ingestion.Rules.Model
{
    internal sealed class FacetAddDto
    {
        public string? Name { get; set; }

        public string? Value { get; set; }

        public string[]? Values { get; set; }
    }
}
