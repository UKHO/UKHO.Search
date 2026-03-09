using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline
{
    public sealed class IngestionPipelineHostedService : IHostedService
    {
        private readonly FileShareIngestionPipelineAdapter _adapter;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<IngestionPipelineHostedService> _logger;
        private FileShareIngestionGraphHandle? _graph;
        private Task? _runTask;

        public IngestionPipelineHostedService(FileShareIngestionPipelineAdapter adapter, IHostApplicationLifetime hostApplicationLifetime, ILogger<IngestionPipelineHostedService> logger)
        {
            _adapter = adapter;
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _graph = _adapter.BuildAzureQueueBacked(_hostApplicationLifetime.ApplicationStopping);
            _runTask = RunAsync();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_graph is null)
            {
                return;
            }

            await _graph.Supervisor.StopAsync(cancellationToken)
                        .ConfigureAwait(false);

            if (_runTask is not null)
            {
                await Task.WhenAny(_runTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken))
                          .ConfigureAwait(false);
            }
        }

        private async Task RunAsync()
        {
            if (_graph is null)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Ingestion pipeline starting.");

                await _graph.Supervisor.StartAsync()
                            .ConfigureAwait(false);

                await _graph.Supervisor.Completion.ConfigureAwait(false);

                if (_graph.Supervisor.FatalException is not null)
                {
                    _logger.LogError(_graph.Supervisor.FatalException, "Ingestion pipeline stopped due to fatal node failure. FatalNodeName={FatalNodeName}", _graph.Supervisor.FatalNodeName);
                }
                else
                {
                    _logger.LogInformation("Ingestion pipeline completed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ingestion pipeline runner failed.");
                throw;
            }
        }
    }
}