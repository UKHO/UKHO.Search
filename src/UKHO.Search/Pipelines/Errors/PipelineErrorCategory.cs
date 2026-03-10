namespace UKHO.Search.Pipelines.Errors
{
    public enum PipelineErrorCategory
    {
        Validation,
        Transform,
        Dependency,
        Timeout,
        BulkIndex,
        Unknown
    }
}