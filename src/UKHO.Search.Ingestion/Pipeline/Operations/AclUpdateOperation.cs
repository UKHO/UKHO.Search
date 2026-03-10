namespace UKHO.Search.Ingestion.Pipeline.Operations
{
    public sealed record AclUpdateOperation : IndexOperation
    {
        public AclUpdateOperation(string documentId, IReadOnlyList<string> securityTokens) : base(documentId)
        {
            SecurityTokens = securityTokens;
        }

        public IReadOnlyList<string> SecurityTokens { get; init; }
    }
}