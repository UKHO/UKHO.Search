namespace UKHO.Search.Pipelines.Supervision
{
    public interface IPipelineFatalErrorReporter
    {
        void ReportFatal(string nodeName, Exception exception);
    }
}