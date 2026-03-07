namespace UKHO.Search.Pipelines.Nodes
{
    public interface INode
    {
        string Name { get; }

        Task Completion { get; }

        Task StartAsync(CancellationToken cancellationToken);

        ValueTask StopAsync(CancellationToken cancellationToken);
    }
}