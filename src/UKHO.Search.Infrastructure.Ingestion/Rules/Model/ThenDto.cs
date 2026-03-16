namespace UKHO.Search.Infrastructure.Ingestion.Rules.Model
{
    internal sealed class ThenDto
    {
        public KeywordsActionDto? Keywords { get; set; }

        public StringAddActionDto? Authority { get; set; }

        public StringAddActionDto? Region { get; set; }

        public StringAddActionDto? Format { get; set; }

        public IntAddActionDto? MajorVersion { get; set; }

        public IntAddActionDto? MinorVersion { get; set; }

        public StringAddActionDto? Category { get; set; }

        public StringAddActionDto? Series { get; set; }

        public StringAddActionDto? Instance { get; set; }

        public SearchTextActionDto? SearchText { get; set; }

        public ContentActionDto? Content { get; set; }

        public FacetsActionDto? Facets { get; set; }

        public DocumentTypeActionDto? DocumentType { get; set; }
    }
}