namespace UKHO.Search.Infrastructure.Ingestion.Rules.Evaluation
{
    internal interface IPathResolver
    {
        IReadOnlyList<string> Resolve(object payload, string path);
    }
}
