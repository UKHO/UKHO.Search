using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UKHO.Search.Infrastructure.Ingestion.Queue;
using UKHO.Search.Services.Ingestion.Providers;

namespace UKHO.Search.Infrastructure.Ingestion.Pipeline
{
    public sealed class IngestionPipelineHostedService : IHostedService
    {
        private readonly IConfiguration _configuration;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<IngestionPipelineHostedService> _logger;
        private readonly IIngestionProviderService _providerService;
        private readonly IQueueClientFactory _queueClientFactory;
        private CancellationTokenSource? _runCts;
        private Task? _runTask;

        public IngestionPipelineHostedService(IConfiguration configuration, IIngestionProviderService providerService, IQueueClientFactory queueClientFactory, IHostApplicationLifetime hostApplicationLifetime, ILogger<IngestionPipelineHostedService> logger)
        {
            _configuration = configuration;
            _providerService = providerService;
            _queueClientFactory = queueClientFactory;
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(_hostApplicationLifetime.ApplicationStopping, cancellationToken);
            _runTask = RunAsync(_runCts.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _runCts?.Cancel();

            if (_runTask is not null)
            {
                await Task.WhenAny(_runTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken))
                          .ConfigureAwait(false);
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Ingestion queue host starting.");

                var node = new IngestionSourceNode("ingestion-source-queue", _configuration, _providerService, _queueClientFactory, _logger);

                await node.StartAsync(cancellationToken)
                          .ConfigureAwait(false);

                await node.Completion.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ingestion queue host runner failed.");
                throw;
            }
        }
    }
}