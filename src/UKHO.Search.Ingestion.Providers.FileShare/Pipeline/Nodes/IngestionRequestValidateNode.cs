using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Nodes
{
    public sealed class IngestionRequestValidateNode : NodeBase<Envelope<IngestionRequest>, Envelope<IngestionRequest>>
    {
        private readonly ChannelWriter<Envelope<IngestionRequest>> _deadLetterOutput;
        private readonly ILogger? _logger;

        public IngestionRequestValidateNode(string name, ChannelReader<Envelope<IngestionRequest>> input, ChannelWriter<Envelope<IngestionRequest>> output, ChannelWriter<Envelope<IngestionRequest>> deadLetterOutput, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, input, output, logger, fatalErrorReporter)
        {
            _deadLetterOutput = deadLetterOutput;
            _logger = logger;
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

            if (!TryValidate(item, out var error))
            {
                item.MarkFailed(error);

                _logger?.LogWarning("Validation failed. NodeName={NodeName} Key={Key} MessageId={MessageId} Attempt={Attempt} ErrorCode={ErrorCode}", Name, item.Key, item.MessageId, item.Attempt, item.Error?.Code);

                await _deadLetterOutput.WriteAsync(item, cancellationToken)
                                       .ConfigureAwait(false);
                Metrics.RecordOut(item);
                return;
            }

            await WriteAsync(item, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override void CompleteOutputs(Exception? error = null)
        {
            base.CompleteOutputs(error);
            _deadLetterOutput.TryComplete(error);
        }

        private bool TryValidate(Envelope<IngestionRequest> envelope, out PipelineError error)
        {
            error = null!;

            var request = envelope.Payload;

            var present = 0;
            if (request.AddItem is not null)
            {
                present++;
            }

            if (request.UpdateItem is not null)
            {
                present++;
            }

            if (request.DeleteItem is not null)
            {
                present++;
            }

            if (request.UpdateAcl is not null)
            {
                present++;
            }

            if (present != 1)
            {
                error = CreateValidationError("PAYLOAD_ONEOF", "IngestionRequest must contain exactly one of AddItem, UpdateItem, DeleteItem, UpdateAcl.");
                return false;
            }

            string? id;

            switch (request.RequestType)
            {
                case IngestionRequestType.AddItem:
                    if (request.AddItem is null)
                    {
                        error = CreateValidationError("ADD_MISSING", "RequestType is AddItem but AddItem payload is missing.");
                        return false;
                    }

                    id = request.AddItem.Id;
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        error = CreateValidationError("ID_EMPTY", "AddItem.Id must be non-empty.");
                        return false;
                    }

                    if (!HasValidSecurityTokens(request.AddItem.SecurityTokens))
                    {
                        error = CreateValidationError("TOKENS_INVALID", "AddItem.SecurityTokens must be non-empty and cannot contain blank tokens.");
                        return false;
                    }

                    break;

                case IngestionRequestType.UpdateItem:
                    if (request.UpdateItem is null)
                    {
                        error = CreateValidationError("UPDATE_MISSING", "RequestType is UpdateItem but UpdateItem payload is missing.");
                        return false;
                    }

                    id = request.UpdateItem.Id;
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        error = CreateValidationError("ID_EMPTY", "UpdateItem.Id must be non-empty.");
                        return false;
                    }

                    if (!HasValidSecurityTokens(request.UpdateItem.SecurityTokens))
                    {
                        error = CreateValidationError("TOKENS_INVALID", "UpdateItem.SecurityTokens must be non-empty and cannot contain blank tokens.");
                        return false;
                    }

                    break;

                case IngestionRequestType.DeleteItem:
                    if (request.DeleteItem is null)
                    {
                        error = CreateValidationError("DELETE_MISSING", "RequestType is DeleteItem but DeleteItem payload is missing.");
                        return false;
                    }

                    id = request.DeleteItem.Id;
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        error = CreateValidationError("ID_EMPTY", "DeleteItem.Id must be non-empty.");
                        return false;
                    }

                    break;

                case IngestionRequestType.UpdateAcl:
                    if (request.UpdateAcl is null)
                    {
                        error = CreateValidationError("ACL_MISSING", "RequestType is UpdateAcl but UpdateAcl payload is missing.");
                        return false;
                    }

                    id = request.UpdateAcl.Id;
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        error = CreateValidationError("ID_EMPTY", "UpdateAcl.Id must be non-empty.");
                        return false;
                    }

                    if (!HasValidSecurityTokens(request.UpdateAcl.SecurityTokens))
                    {
                        error = CreateValidationError("TOKENS_INVALID", "UpdateAcl.SecurityTokens must be non-empty and cannot contain blank tokens.");
                        return false;
                    }

                    break;

                default:
                    error = CreateValidationError("REQUESTTYPE_UNSUPPORTED", $"Unsupported IngestionRequestType '{request.RequestType}'.");
                    return false;
            }

            if (!string.Equals(envelope.Key, id, StringComparison.Ordinal))
            {
                error = CreateValidationError("KEY_ID_MISMATCH", "Envelope.Key must match the Id in the request payload.");
                return false;
            }

            return true;
        }

        private PipelineError CreateValidationError(string code, string message)
        {
            return new PipelineError
            {
                Category = PipelineErrorCategory.Validation,
                Code = code,
                Message = message,
                ExceptionType = null,
                ExceptionMessage = null,
                StackTrace = null,
                IsTransient = false,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                NodeName = Name,
                Details = new Dictionary<string, string>()
            };
        }

        private static bool HasValidSecurityTokens(string[]? tokens)
        {
            return tokens is { Length: > 0 } && !tokens.Any(string.IsNullOrWhiteSpace);
        }
    }
}