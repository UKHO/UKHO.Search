using System.Collections.Generic;
using UKHO.Search.Ingestion.Contracts;

namespace UKHO.Search.Studio.Providers.FileShare
{
    public sealed class FileShareStudioBatchPayloadSource
    {
        public Guid BatchId { get; init; }

        public DateTimeOffset CreatedOn { get; init; }

        public string? ActiveBusinessUnitName { get; init; }

        public IReadOnlyList<KeyValuePair<string, string>> Attributes { get; init; } = Array.Empty<KeyValuePair<string, string>>();

        public IngestionFileList Files { get; init; } = new();
    }
}
