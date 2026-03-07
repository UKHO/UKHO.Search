namespace UKHO.Search.Pipelines.Channels
{
    public interface IQueueDepthProvider
    {
        long QueueDepth { get; }
    }
}