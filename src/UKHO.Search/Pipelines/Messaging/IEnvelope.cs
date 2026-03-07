using UKHO.Search.Pipelines.Errors;

namespace UKHO.Search.Pipelines.Messaging
{
    public interface IEnvelope
    {
        Guid MessageId { get; }

        string Key { get; }

        int Attempt { get; }

        MessageStatus Status { get; }

        PipelineError? Error { get; }
    }
}