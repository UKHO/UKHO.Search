using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Enrichment
{
    public sealed class FileContentEnricherTests
    {
        [Fact]
        public async Task TryBuildEnrichmentAsync_does_not_extract_content_when_allowlist_missing()
        {
            var zipBytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["a.txt"] = "Hello world"u8.ToArray(),
                ["catalog.xml"] = "<catalog />"u8.ToArray()
            });

            var calls = new List<string>();
            var downloader = new FakeZipDownloader(batchId =>
            {
                calls.Add(batchId);
                return new MemoryStream(zipBytes);
            });
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>())
                                                          .Build();
            var enricher = new FileContentEnricher(downloader, configuration, NullLogger<FileContentEnricher>.Instance);

            var request = CreateAddRequest("batch-1");
            var document = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);

            calls.ShouldBe(new[] { request.AddItem!.Id });
            document.Content.ShouldBeEmpty();
            document.Keywords.ShouldBeEmpty();
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_throws_when_download_fails()
        {
            var downloader = new FakeZipDownloader(_ => throw new InvalidOperationException("nope"));
            var configuration = CreateConfig(".txt");
            var enricher = new FileContentEnricher(downloader, configuration, NullLogger<FileContentEnricher>.Instance);

            var request = CreateAddRequest("batch-2");
            var document = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await Should.ThrowAsync<InvalidOperationException>(() => enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None));
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_throws_when_zip_is_corrupt()
        {
            var corrupt = new MemoryStream([1, 2, 3, 4, 5]);

            var downloader = new FakeZipDownloader(_ => corrupt);
            var configuration = CreateConfig(".txt");
            var enricher = new FileContentEnricher(downloader, configuration, NullLogger<FileContentEnricher>.Instance);

            var request = CreateAddRequest("batch-3");
            var document = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await Should.ThrowAsync<InvalidDataException>(() => enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None));
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_extracts_text_from_allowlisted_files_and_sets_keyword_without_extension()
        {
            var zipBytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["a.txt"] = "Hello world"u8.ToArray(),
                ["b.bin"] = [0, 1, 2]
            });

            var downloader = new FakeZipDownloader(_ => new MemoryStream(zipBytes));
            var configuration = CreateConfig(".txt");
            var enricher = new FileContentEnricher(downloader, configuration, NullLogger<FileContentEnricher>.Instance);

            var request = CreateAddRequest("batch-4");
            var document = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);

            document.Content.ShouldContain("hello world");
            document.Keywords.ShouldContain("a");
            document.Keywords.ShouldNotContain("b");
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_allowlist_extension_check_is_case_insensitive()
        {
            var zipBytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["a.TXT"] = "Hello world"u8.ToArray()
            });

            var downloader = new FakeZipDownloader(_ => new MemoryStream(zipBytes));
            var configuration = CreateConfig(".txt;.PDF");
            var enricher = new FileContentEnricher(downloader, configuration, NullLogger<FileContentEnricher>.Instance);

            var request = CreateAddRequest("batch-6-case-insensitive");
            var document = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);

            document.Content.ShouldContain("hello world");
            document.Keywords.ShouldContain("a");
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_cleans_up_temp_workspace()
        {
            var batchId = "batch-5-cleanup";

            var zipBytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["a.txt"] = "Hello"u8.ToArray()
            });

            var downloader = new FakeZipDownloader(_ => new MemoryStream(zipBytes));
            var configuration = CreateConfig(".txt");
            var enricher = new FileContentEnricher(downloader, configuration, NullLogger<FileContentEnricher>.Instance);

            var request = CreateAddRequest(batchId);
            var document = CanonicalDocument.CreateMinimal("doc-1", request.AddItem!.Properties, request.AddItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);

            var batchRoot = Path.Combine(Path.GetTempPath(), "ukho-search", "fileshare", "kreuzberg", batchId);
            if (Directory.Exists(batchRoot))
            {
                Directory.EnumerateDirectories(batchRoot)
                         .ShouldBeEmpty();
            }
        }

        private static IConfiguration CreateConfig(string allowlist)
        {
            return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                                             {
                                                 ["ingestion:fileContentExtractionAllowedExtensions"] = allowlist
                                             })
                                             .Build();
        }

        private static IngestionRequest CreateAddRequest(string batchId)
        {
            var add = new AddItemRequest(batchId, Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList());
            return new IngestionRequest(IngestionRequestType.AddItem, add, null, null, null);
        }

        private static byte[] CreateZipBytes(IReadOnlyDictionary<string, byte[]> entries)
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var (name, bytes) in entries)
                {
                    var entry = zip.CreateEntry(name);
                    using var entryStream = entry.Open();
                    entryStream.Write(bytes);
                }
            }

            return ms.ToArray();
        }

        private sealed class FakeZipDownloader : IFileShareZipDownloader
        {
            private readonly Func<string, Stream> _handler;

            public FakeZipDownloader(Func<string, Stream> handler)
            {
                _handler = handler;
            }

            public Task<Stream> DownloadZipFileAsync(string batchId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_handler(batchId));
            }
        }
    }
}