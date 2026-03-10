using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UKHO.Search.Ingestion.Providers.FileShare.Pipeline
{
    public sealed record FileShareIngestionGraphDependencies
    {
        public required IConfiguration Configuration { get; init; }

        public required ILoggerFactory LoggerFactory { get; init; }

        public required FileShareIngestionGraphFactories Factories { get; init; }

        public required IServiceScopeFactory ScopeFactory { get; init; }
    }
}