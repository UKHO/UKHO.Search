using System.IO.Compression;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Search.Configuration;
using UKHO.Search.Ingestion;
using UKHO.Search.Ingestion.Pipeline.Documents;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment;
using UKHO.Search.Ingestion.Providers.FileShare.Enrichment.Handlers;
using UKHO.Search.Ingestion.Requests;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Enrichment
{
    public sealed class BatchContentEnricherTests
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
            var enricher = new BatchContentEnricher(downloader, Array.Empty<IBatchContentHandler>(), NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest("batch-1");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);

            calls.ShouldBe(new[] { request.IndexItem!.Id });
            document.Content.ShouldBeEmpty();
            document.Keywords.ShouldBeEmpty();
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_invokes_S100_handler_when_catalog_xml_is_in_nested_path()
        {
            var zipBytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["a/b/c/catalog.xml"] = "<catalog />"u8.ToArray()
            });

            var downloader = new FakeZipDownloader(_ => new MemoryStream(zipBytes));

            var handler = new S100BatchContentHandler(NullLogger<S100BatchContentHandler>.Instance);
            var enricher = new BatchContentEnricher(downloader, new[] { handler }, NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest("batch-nested-catalog");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_extracts_files_from_single_nested_zip_before_invoking_handlers()
        {
            var nestedZipBytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["nested.txt"] = "Nested"u8.ToArray()
            });

            var outerZipBytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["root.txt"] = "Root"u8.ToArray(),
                ["nested.zip"] = nestedZipBytes
            });

            var downloader = new FakeZipDownloader(_ => new MemoryStream(outerZipBytes));

            IReadOnlyList<string>? capturedPaths = null;
            var handler = new RecordingHandler(paths => capturedPaths = paths.ToList());
            var enricher = new BatchContentEnricher(downloader, new[] { handler }, NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest("batch-single-nested-zip");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);

            capturedPaths.ShouldNotBeNull();
            capturedPaths.ShouldContain(p => Path.GetFileName(p).Equals("nested.txt", StringComparison.OrdinalIgnoreCase));

            var nestedDirectoryPath = capturedPaths.Single(p => Path.GetFileName(p).Equals("nested.txt", StringComparison.OrdinalIgnoreCase));
            Path.GetFileName(Path.GetDirectoryName(nestedDirectoryPath)!).ShouldBe("nested");
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_extracts_files_from_multi_level_nested_zips()
        {
            var nestedLevel2Bytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["deep.txt"] = "Deep"u8.ToArray()
            });

            var nestedLevel1Bytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["level2.zip"] = nestedLevel2Bytes
            });

            var outerZipBytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["level1.zip"] = nestedLevel1Bytes
            });

            var downloader = new FakeZipDownloader(_ => new MemoryStream(outerZipBytes));

            IReadOnlyList<string>? capturedPaths = null;
            var handler = new RecordingHandler(paths => capturedPaths = paths.ToList());
            var enricher = new BatchContentEnricher(downloader, new[] { handler }, NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest("batch-multi-nested-zip");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);

            capturedPaths.ShouldNotBeNull();
            capturedPaths.ShouldContain(p => Path.GetFileName(p).Equals("deep.txt", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_throws_when_nested_zip_is_corrupt_and_includes_path()
        {
            var outerZipBytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["nested.zip"] = [1, 2, 3, 4, 5]
            });

            var downloader = new FakeZipDownloader(_ => new MemoryStream(outerZipBytes));
            var enricher = new BatchContentEnricher(downloader, Array.Empty<IBatchContentHandler>(), NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest("batch-corrupt-nested-zip");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            var ex = await Should.ThrowAsync<InvalidOperationException>(() => enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None));
            ex.Message.ShouldContain("nested.zip");
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_throws_when_download_fails()
        {
            var downloader = new FakeZipDownloader(_ => throw new InvalidOperationException("nope"));
            var enricher = new BatchContentEnricher(downloader, Array.Empty<IBatchContentHandler>(), NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest("batch-2");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await Should.ThrowAsync<InvalidOperationException>(() => enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None));
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_throws_when_zip_is_corrupt()
        {
            var corrupt = new MemoryStream([1, 2, 3, 4, 5]);

            var downloader = new FakeZipDownloader(_ => corrupt);
            var enricher = new BatchContentEnricher(downloader, Array.Empty<IBatchContentHandler>(), NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest("batch-3");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

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
            var handler = new TextExtractionBatchContentHandler(new[] { ".txt" }, NullLogger<TextExtractionBatchContentHandler>.Instance);
            var enricher = new BatchContentEnricher(downloader, new[] { handler }, NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest("batch-4");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

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
            var handler = new TextExtractionBatchContentHandler(new[] { ".txt", ".PDF" }, NullLogger<TextExtractionBatchContentHandler>.Instance);
            var enricher = new BatchContentEnricher(downloader, new[] { handler }, NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest("batch-6-case-insensitive");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);

            document.Content.ShouldContain("hello world");
            document.Keywords.ShouldContain("a");
        }

        [Fact]
        public async Task TryBuildEnrichmentAsync_normalizes_hyphenated_numeric_filename_keywords_for_search_recall()
        {
            var zipBytes = CreateZipBytes(new Dictionary<string, byte[]>
            {
                ["S-100.TXT"] = "HELLO WORLD"u8.ToArray()
            });

            var downloader = new FakeZipDownloader(_ => new MemoryStream(zipBytes));
            var handler = new TextExtractionBatchContentHandler(new[] { ".txt" }, NullLogger<TextExtractionBatchContentHandler>.Instance);
            var enricher = new BatchContentEnricher(downloader, new[] { handler }, NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest("batch-s100-keyword");
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);

            document.Content.ShouldContain("hello world");
            document.Keywords.ShouldContain("s-100");
            document.Keywords.ShouldContain("s100");
            document.Keywords.ShouldNotContain("S-100");
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
            var handler = new TextExtractionBatchContentHandler(new[] { ".txt" }, NullLogger<TextExtractionBatchContentHandler>.Instance);
            var enricher = new BatchContentEnricher(downloader, new[] { handler }, NullLogger<BatchContentEnricher>.Instance, new IngestionModeOptions(IngestionMode.Strict));

            var request = CreateAddRequest(batchId);
            var document = CanonicalDocument.CreateMinimal("doc-1", "file-share", request.IndexItem!, request.IndexItem.Timestamp);

            await enricher.TryBuildEnrichmentAsync(request, document, CancellationToken.None);

            var batchRoot = Path.Combine(Path.GetTempPath(), "ukho-search", "fileshare", "kreuzberg", batchId);
            if (Directory.Exists(batchRoot))
            {
                Directory.EnumerateDirectories(batchRoot)
                         .ShouldBeEmpty();
            }
        }

        private static IngestionRequest CreateAddRequest(string batchId)
        {
            var add = new IndexRequest(batchId, Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList());
            return new IngestionRequest(IngestionRequestType.IndexItem, add, null, null);
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

        private sealed class RecordingHandler : IBatchContentHandler
        {
            private readonly Action<IEnumerable<string>> _callback;

            public RecordingHandler(Action<IEnumerable<string>> callback)
            {
                ArgumentNullException.ThrowIfNull(callback);

                _callback = callback;
            }

            public Task HandleFiles(IEnumerable<string> paths, IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
            {
                _callback(paths);
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingHandler : IBatchContentHandler
        {
            public Task HandleFiles(IEnumerable<string> paths, IngestionRequest request, CanonicalDocument document, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("boom");
            }
        }
    }
}
