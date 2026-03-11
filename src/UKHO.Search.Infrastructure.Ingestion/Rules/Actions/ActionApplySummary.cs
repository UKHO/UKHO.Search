namespace UKHO.Search.Infrastructure.Ingestion.Rules.Actions
{
    internal sealed class ActionApplySummary
    {
        public int KeywordsAdded { get; set; }

        public int SearchTextAdded { get; set; }

        public int ContentAdded { get; set; }

        public int FacetValuesAdded { get; set; }

        public int DocumentTypeSet { get; set; }

        public void Add(ActionApplySummary other)
        {
            KeywordsAdded += other.KeywordsAdded;
            SearchTextAdded += other.SearchTextAdded;
            ContentAdded += other.ContentAdded;
            FacetValuesAdded += other.FacetValuesAdded;
            DocumentTypeSet += other.DocumentTypeSet;
        }
    }
}