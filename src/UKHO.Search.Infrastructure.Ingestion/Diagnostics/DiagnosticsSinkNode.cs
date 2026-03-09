using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Nodes;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Infrastructure.Ingestion.Diagnostics
{
    public sealed class DiagnosticsSinkNode<TPayload> : SinkNodeBase<Envelope<TPayload>>
    {
        private readonly ILogger? _logger;

        public DiagnosticsSinkNode(string name, ChannelReader<Envelope<TPayload>> input, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null, string? providerName = null) : base(name, input, logger, fatalErrorReporter, providerName: providerName)
        {
            _logger = logger;
        }

        protected override ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
        {
            item.Context.AddBreadcrumb(Name);

            if (_logger is null)
            {
                return ValueTask.CompletedTask;
            }

            var error = item.Error;
            var breadcrumbs = item.Context.Breadcrumbs.Count == 0 ? null : string.Join(" > ", item.Context.Breadcrumbs);

            _logger.LogInformation("Diagnostics event. NodeName={NodeName} Key={Key} MessageId={MessageId} Attempt={Attempt} Status={Status} ErrorCategory={ErrorCategory} ErrorCode={ErrorCode} Breadcrumbs={Breadcrumbs}", Name, item.Key, item.MessageId, item.Attempt, item.Status, error?.Category, error?.Code, breadcrumbs);

            return ValueTask.CompletedTask;
        }
    }
}