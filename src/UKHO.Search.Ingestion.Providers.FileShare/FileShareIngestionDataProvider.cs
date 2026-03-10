using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Ingestion.Requests.Serialization;
using UKHO.Search.Pipelines.Messaging;

namespace UKHO.Search.Ingestion.Providers.FileShare
{
    public sealed class FileShareIngestionDataProvider : IIngestionDataProvider, IAsyncDisposable
    {
        private const int DefaultIngressCapacity = 256;
        private static readonly JsonSerializerOptions _serializerOptions = IngestionJsonSerializerOptions.Create();
        private readonly object _disposeGate = new();

        private readonly IngestionRequestIngressChannel _ingress;
        private readonly ILogger<FileShareIngestionDataProvider> _logger;
        private readonly string _providerName;
        private readonly FileShareIngestionProcessingGraphDependencies? _processingGraphDependencies;
        private readonly CancellationTokenSource _shutdown = new();
        private readonly object _startGate = new();
        private int _disposed;
        private FileShareIngestionProcessingGraphHandle? _processingGraph;

        private Task? _startTask;

        public FileShareIngestionDataProvider() : this(DefaultIngressCapacity, NullLogger<FileShareIngestionDataProvider>.Instance)
        {
        }

        public FileShareIngestionDataProvider(int ingressCapacity, ILogger<FileShareIngestionDataProvider> logger) : this(FileShareIngestionDataProviderFactory.ProviderName, null, ingressCapacity, logger)
        {
        }

        public FileShareIngestionDataProvider(string providerName, int ingressCapacity, ILogger<FileShareIngestionDataProvider> logger) : this(providerName, null, ingressCapacity, logger)
        {
        }

        public FileShareIngestionDataProvider(FileShareIngestionProcessingGraphDependencies? processingGraphDependencies, int ingressCapacity, ILogger<FileShareIngestionDataProvider> logger) : this(FileShareIngestionDataProviderFactory.ProviderName, processingGraphDependencies, ingressCapacity, logger)
        {
        }

        public FileShareIngestionDataProvider(string providerName, FileShareIngestionProcessingGraphDependencies? processingGraphDependencies, int ingressCapacity, ILogger<FileShareIngestionDataProvider> logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

            _providerName = providerName;
            _ingress = new IngestionRequestIngressChannel(ingressCapacity);
            _processingGraphDependencies = processingGraphDependencies;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal ChannelReader<Envelope<IngestionRequest>> IngressReader => _ingress.Reader;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            lock (_disposeGate)
            {
                _logger.LogInformation("Stopping provider processing graph. ProviderName={ProviderName}", Name);
                _ingress.Writer.TryComplete();
            }

            if (_processingGraph is not null)
            {
                var completed = await Task.WhenAny(_processingGraph.Supervisor.Completion, Task.Delay(TimeSpan.FromSeconds(5)))
                                          .ConfigureAwait(false);

                if (completed != _processingGraph.Supervisor.Completion)
                {
                    _logger.LogWarning("Provider processing graph did not drain within timeout; cancelling. ProviderName={ProviderName}", Name);
                    _shutdown.Cancel();
                    await Task.WhenAny(_processingGraph.Supervisor.Completion, Task.Delay(TimeSpan.FromSeconds(5)))
                              .ConfigureAwait(false);
                }
            }

            _shutdown.Cancel();
            _shutdown.Dispose();
        }

        public string Name => _providerName;

        public ValueTask<IngestionRequest> DeserializeIngestionRequestAsync(string messageText, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                throw new JsonException("Queue message body is required.");
            }

            var request = JsonSerializer.Deserialize<IngestionRequest>(messageText, _serializerOptions);
            if (request is null)
            {
                throw new JsonException("Queue message could not be deserialized to IngestionRequest.");
            }

            return ValueTask.FromResult(request);
        }

        public async ValueTask ProcessIngestionRequestAsync(Envelope<IngestionRequest> envelope, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(envelope);

            if (Volatile.Read(ref _disposed) != 0)
            {
                throw new ObjectDisposedException(nameof(FileShareIngestionDataProvider));
            }

            await EnsureProcessingGraphStartedAsync()
                .ConfigureAwait(false);

            _logger.LogDebug("Enqueuing ingestion request. ProviderName={ProviderName} Key={Key} MessageId={MessageId}", Name, envelope.Key, envelope.MessageId);

            await _ingress.Writer.WriteAsync(envelope, cancellationToken)
                          .ConfigureAwait(false);

            _logger.LogInformation("Accepted ingestion request. ProviderName={ProviderName} Key={Key} MessageId={MessageId}", Name, envelope.Key, envelope.MessageId);
        }

        private Task EnsureProcessingGraphStartedAsync()
        {
            if (_processingGraphDependencies is null)
            {
                return Task.CompletedTask;
            }

            var current = Volatile.Read(ref _startTask);
            if (current is not null)
            {
                return current;
            }

            lock (_startGate)
            {
                if (_startTask is not null)
                {
                    return _startTask;
                }

                _logger.LogInformation("Starting provider processing graph. ProviderName={ProviderName}", Name);

                 _processingGraph = FileShareIngestionProcessingGraph.Build(IngressReader, _processingGraphDependencies, Name, _shutdown.Token);
                _startTask = _processingGraph.Supervisor.StartAsync();

                return _startTask;
            }
        }
    }
}