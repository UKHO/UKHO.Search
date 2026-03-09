using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using UKHO.Search.Pipelines.DeadLetter;
using UKHO.Search.Pipelines.Messaging;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
    public sealed class DeadLetterSinkNode<TPayload> : SinkNodeBase<Envelope<TPayload>>
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new(StringComparer.OrdinalIgnoreCase);
        private readonly bool _fatalIfCannotPersist;

        private readonly string _filePath;
        private readonly ILogger? _logger;
        private readonly IDeadLetterMetadataProvider _metadataProvider;
        private readonly Func<Envelope<TPayload>, string?>? _snapshotter;
        private int _persistedCount;

        public DeadLetterSinkNode(string name, ChannelReader<Envelope<TPayload>> input, string filePath, bool fatalIfCannotPersist = false, Func<Envelope<TPayload>, string?>? snapshotter = null, IDeadLetterMetadataProvider? metadataProvider = null, ILogger? logger = null, IPipelineFatalErrorReporter? fatalErrorReporter = null) : base(name, input, logger, fatalErrorReporter)
        {
            _filePath = filePath;
            _fatalIfCannotPersist = fatalIfCannotPersist;
            _snapshotter = snapshotter;
            _metadataProvider = metadataProvider ?? new DefaultDeadLetterMetadataProvider();
            _logger = logger;
        }

        public int PersistedCount => _persistedCount;

        protected override async ValueTask HandleItemAsync(Envelope<TPayload> item, CancellationToken cancellationToken)
        {
            try
            {
                var record = new DeadLetterRecord<TPayload>
                {
                    DeadLetteredAtUtc = DateTimeOffset.UtcNow,
                    NodeName = Name,
                    Envelope = item,
                    Error = item.Error,
                    RawSnapshot = _snapshotter?.Invoke(item),
                    Metadata = new DeadLetterMetadata
                    {
                        AppVersion = _metadataProvider.AppVersion,
                        CommitId = _metadataProvider.CommitId,
                        HostName = _metadataProvider.HostName
                    }
                };

                var json = JsonSerializer.Serialize(record);
                await AppendLineAsync(json, cancellationToken)
                    .ConfigureAwait(false);
                Interlocked.Increment(ref _persistedCount);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Dead-letter persist failed in '{NodeName}' for MessageId={MessageId} Key='{Key}' ErrorCode='{ErrorCode}'.", Name, item.MessageId, item.Key, item.Error?.Code);

                if (_fatalIfCannotPersist)
                {
                    throw;
                }
            }
        }

        private async Task AppendLineAsync(string line, CancellationToken cancellationToken)
        {
            var fileLock = _fileLocks.GetOrAdd(_filePath, _ => new SemaphoreSlim(1, 1));
            await fileLock.WaitAsync(cancellationToken)
                          .ConfigureAwait(false);
            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await using var stream = await OpenAppendStreamWithRetryAsync(cancellationToken)
                    .ConfigureAwait(false);
                await using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken)
                            .ConfigureAwait(false);
            }
            finally
            {
                fileLock.Release();
            }
        }

        private async Task<FileStream> OpenAppendStreamWithRetryAsync(CancellationToken cancellationToken)
        {
            const int maxAttempts = 250;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return new FileStream(_filePath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true);
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken)
                              .ConfigureAwait(false);
                }
            }

            throw new IOException($"Timed out acquiring exclusive append access for dead-letter file '{_filePath}'.");
        }
    }
}