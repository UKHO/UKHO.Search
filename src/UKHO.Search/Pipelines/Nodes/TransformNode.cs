using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class TransformNode<TIn, TOut> : NodeBase<Envelope<TIn>, Envelope<TOut>>
    {
        private readonly bool faultPipelineOnException;
        private readonly Func<TIn, CancellationToken, ValueTask<TOut>> transform;

        public TransformNode(string name, ChannelReader<Envelope<TIn>> input, ChannelWriter<Envelope<TOut>> output, Func<TIn, CancellationToken, ValueTask<TOut>> transform, bool faultPipelineOnException = false, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, input, output, logger, fatalErrorReporter)
        {
            this.transform = transform;
            this.faultPipelineOnException = faultPipelineOnException;
        }

        protected override async ValueTask HandleItemAsync(Envelope<TIn> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);

            if (item.Status != MessageStatus.Ok)
            {
                await WriteAsync(item.MapPayload(default(TOut)!), cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            try
            {
                var payload = await transform(item.Payload, cancellationToken)
                    .ConfigureAwait(false);
                await WriteAsync(item.MapPayload(payload), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (faultPipelineOnException)
                {
                    throw;
                }

                item.MarkFailed(new PipelineError
                {
                    Category = PipelineErrorCategory.Transform,
                    Code = "TRANSFORM_ERROR",
                    Message = "Transform failed.",
                    ExceptionType = ex.GetType()
                                      .FullName,
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    IsTransient = false,
                    OccurredAtUtc = DateTimeOffset.UtcNow,
                    NodeName = Name,
                    Details = new Dictionary<string, string>()
                });

                await WriteAsync(item.MapPayload(default(TOut)!), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
    }
}