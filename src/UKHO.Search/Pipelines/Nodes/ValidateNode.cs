using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class ValidateNode<TPayload> : NodeBase<Envelope<TPayload>, Envelope<TPayload>>
    {
        private readonly ChannelWriter<Envelope<TPayload>>? errorOutput;
        private readonly bool forwardFailedToMainOutput;

        public ValidateNode(string name, ChannelReader<Envelope<TPayload>> input, ChannelWriter<Envelope<TPayload>> output, ChannelWriter<Envelope<TPayload>>? errorOutput = null, bool forwardFailedToMainOutput = true, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, input, output, logger, fatalErrorReporter)
        {
            this.errorOutput = errorOutput;
            this.forwardFailedToMainOutput = forwardFailedToMainOutput;
        }

        protected override async ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);

            if (string.IsNullOrWhiteSpace(item.Key) && item.Status == MessageStatus.Ok)
            {
                item.MarkFailed(new PipelineError
                {
                    Category = PipelineErrorCategory.Validation,
                    Code = "KEY_EMPTY",
                    Message = "Key must be non-empty.",
                    ExceptionType = null,
                    ExceptionMessage = null,
                    StackTrace = null,
                    IsTransient = false,
                    OccurredAtUtc = DateTimeOffset.UtcNow,
                    NodeName = Name,
                    Details = new Dictionary<string, string>()
                });
            }

            if (item.Status == MessageStatus.Failed && errorOutput is not null)
            {
                await errorOutput.WriteAsync(item, cancellationToken)
                                 .ConfigureAwait(false);
                Metrics.RecordOut(item);
            }

            if (item.Status != MessageStatus.Failed || forwardFailedToMainOutput)
            {
                await WriteAsync(item, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        protected override void CompleteOutputs(Exception? error = null)
        {
            base.CompleteOutputs(error);
            errorOutput?.TryComplete(error);
        }
    }
}