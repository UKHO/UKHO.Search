using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Pipeline;
using UKHO.Search.Ingestion.Pipeline.Operations;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Documents;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Nodes
{
    public sealed class IngestionRequestDispatchNode : NodeBase<Envelope<IngestionRequest>, Envelope<IngestionPipelineContext>>
    {
        private readonly CanonicalDocumentBuilder _canonicalBuilder;
        private readonly ChannelWriter<Envelope<IngestionRequest>> _deadLetterOutput;
        private readonly ILogger? _logger;
        private readonly ProviderParameters? _providerParameters;

        public IngestionRequestDispatchNode(string name, ChannelReader<Envelope<IngestionRequest>> input, ChannelWriter<Envelope<IngestionPipelineContext>> output, ChannelWriter<Envelope<IngestionRequest>> deadLetterOutput, CanonicalDocumentBuilder canonicalBuilder, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null, string? providerName = null) : base(name, input,
            output, logger, fatalErrorReporter, providerName: providerName)
        {
            _deadLetterOutput = deadLetterOutput;
            _canonicalBuilder = canonicalBuilder;
            _logger = logger;
            _providerParameters = string.IsNullOrWhiteSpace(providerName) ? null : new ProviderParameters(providerName);
        }

        protected override async ValueTask HandleItemAsync(Envelope<IngestionRequest> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);

            if (item.Status != MessageStatus.Ok)
            {
                await _deadLetterOutput.WriteAsync(item, cancellationToken)
                                       .ConfigureAwait(false);
                Metrics.RecordOut(item);
                return;
            }

            IndexOperation operation;

            try
            {
                operation = Dispatch(item);
            }
            catch (Exception ex)
            {
                item.MarkFailed(new PipelineError
                {
                    Category = PipelineErrorCategory.Transform,
                    Code = "DISPATCH_ERROR",
                    Message = "Failed to dispatch ingestion request to an index operation.",
                    ExceptionType = ex.GetType()
                                      .FullName,
                    ExceptionMessage = ex.Message,
                    StackTrace = ex.StackTrace,
                    IsTransient = false,
                    OccurredAtUtc = DateTimeOffset.UtcNow,
                    NodeName = Name,
                    Details = new Dictionary<string, string>()
                });

                _logger?.LogWarning(ex, "Dispatch failed. NodeName={NodeName} Key={Key} MessageId={MessageId} Attempt={Attempt}", Name, item.Key, item.MessageId, item.Attempt);

                await _deadLetterOutput.WriteAsync(item, cancellationToken)
                                       .ConfigureAwait(false);
                Metrics.RecordOut(item);
                return;
            }

            var outEnvelope = item.MapPayload(new IngestionPipelineContext
            {
                Request = item.Payload,
                Operation = operation
            });

            await WriteAsync(outEnvelope, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override void CompleteOutputs(Exception? error = null)
        {
            base.CompleteOutputs(error);
            _deadLetterOutput.TryComplete(error);
        }

        private IndexOperation Dispatch(Envelope<IngestionRequest> item)
        {
            var request = item.Payload;
            var documentId = item.Key;

            switch (request.RequestType)
            {
                case IngestionRequestType.IndexItem:
                {
                    if (request.IndexItem is null)
                    {
                        throw new InvalidOperationException("IndexItem payload missing.");
                    }

                    var providerParameters = ResolveProviderParameters(item.Context);
                    var doc = _canonicalBuilder.BuildForUpsert(documentId, request, providerParameters);
                    return new UpsertOperation(documentId, doc);
                }

                case IngestionRequestType.DeleteItem:
                {
                    if (request.DeleteItem is null)
                    {
                        throw new InvalidOperationException("DeleteItem payload missing.");
                    }

                    return new DeleteOperation(documentId);
                }

                case IngestionRequestType.UpdateAcl:
                {
                    if (request.UpdateAcl is null)
                    {
                        throw new InvalidOperationException("UpdateAcl payload missing.");
                    }

                    return new AclUpdateOperation(documentId, request.UpdateAcl.SecurityTokens);
                }

                default:
                    throw new InvalidOperationException($"Unsupported request type '{request.RequestType}'.");
            }
        }

        private ProviderParameters ResolveProviderParameters(MessageContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.TryGetItem<ProviderParameters>(ProviderEnvelopeContextKeys.ProviderParameters, out var providerParameters) && providerParameters is not null)
            {
                return providerParameters;
            }

            if (_providerParameters is not null)
            {
                context.SetItem(ProviderEnvelopeContextKeys.ProviderParameters, _providerParameters);
                return _providerParameters;
            }

            throw new InvalidOperationException("Provider context is required before dispatching an index request.");
        }
    }
}