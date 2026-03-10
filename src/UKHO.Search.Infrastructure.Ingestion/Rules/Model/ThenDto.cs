namespace UKHO.Search.Infrastructure.Ingestion.Rules.Model
{
    internal sealed class ThenDto
    {
        public KeywordsActionDto? Keywords { get; set; }

        public SearchTextActionDto? SearchText { get; set; }

        public ContentActionDto? Content { get; set; }

        public FacetsActionDto? Facets { get; set; }

        public DocumentTypeActionDto? DocumentType { get; set; }
    }
}
