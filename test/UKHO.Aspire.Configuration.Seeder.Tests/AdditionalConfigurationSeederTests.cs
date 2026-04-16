using System.Net.Mime;
using Microsoft.Extensions.Logging;
using UKHO.Aspire.Configuration.Seeder.AdditionalConfiguration;
using UKHO.Aspire.Configuration.Seeder.Tests.TestSupport;
using Xunit;

namespace UKHO.Aspire.Configuration.Seeder.Tests
{
    /// <summary>
    /// Verifies how <see cref="AdditionalConfigurationSeeder"/> discovers files, builds keys, logs missing roots, and writes plain-text settings.
    /// </summary>
    public sealed class AdditionalConfigurationSeederTests
    {
        /// <summary>
        /// Verifies that the seeder rejects a missing configuration client before attempting any file-system work.
        /// </summary>
        [Fact]
        public async Task SeedAsync_WhenConfigurationClientMissing_ShouldThrowArgumentNullException()
        {
            // Create the seeder with a capture logger so the constructor dependency matches production usage.
            var logger = new TestLogger<AdditionalConfigurationSeeder>();
            var seeder = new AdditionalConfigurationSeeder(logger);

            // Invoke the entry point with a null client to protect the guard clause.
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => seeder.SeedAsync(
                null!,
                "local",
                "C:\\root",
                "prefix",
                CancellationToken.None));

            // The thrown exception should identify the missing client parameter.
            Assert.Equal("configurationClient", exception.ParamName);
        }

        /// <summary>
        /// Verifies that invalid label, root-path, and prefix values are rejected consistently.
        /// </summary>
        /// <param name="label">The label value supplied to the seeder.</param>
        /// <param name="rootPath">The additional configuration root path supplied to the seeder.</param>
        /// <param name="prefix">The key prefix supplied to the seeder.</param>
        [Theory]
        [InlineData(null, "C:\\root", "prefix")]
        [InlineData(" ", "C:\\root", "prefix")]
        [InlineData("local", null, "prefix")]
        [InlineData("local", " ", "prefix")]
        [InlineData("local", "C:\\root", null)]
        [InlineData("local", "C:\\root", " ")]
        public async Task SeedAsync_WhenArgumentsInvalid_ShouldThrowArgumentException(string? label, string? rootPath, string? prefix)
        {
            // Create a fake client even though the guard clause should fail before the client is used.
            var logger = new TestLogger<AdditionalConfigurationSeeder>();
            var seeder = new AdditionalConfigurationSeeder(logger);
            var client = new TestConfigurationClient();

            // Execute the seeding request with one invalid argument to protect each ThrowIfNullOrWhiteSpace guard.
            await Assert.ThrowsAnyAsync<ArgumentException>(() => seeder.SeedAsync(
                client,
                label!,
                rootPath!,
                prefix!,
                CancellationToken.None));
        }

        /// <summary>
        /// Verifies that a missing root path produces a warning and skips all writes.
        /// </summary>
        [Fact]
        public async Task SeedAsync_WhenRootPathMissing_ShouldLogWarningAndSkipWrites()
        {
            // Use a guaranteed-nonexistent path so the missing-directory branch is exercised deterministically.
            var logger = new TestLogger<AdditionalConfigurationSeeder>();
            var seeder = new AdditionalConfigurationSeeder(logger);
            var client = new TestConfigurationClient();
            var missingPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            // Run the seeder against the missing path.
            await seeder.SeedAsync(client, "local", missingPath, "additional", CancellationToken.None);

            // No settings should be written when the directory does not exist.
            Assert.Empty(client.Writes);

            // The warning should explain that the path was skipped.
            var warning = Assert.Single(logger.Entries, entry => entry.LogLevel == LogLevel.Warning);
            Assert.Contains(missingPath, warning.Message, StringComparison.Ordinal);
            Assert.Contains("Skipping", warning.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that each discovered file becomes a plain-text configuration setting with the expected key and label.
        /// </summary>
        [Fact]
        public async Task SeedAsync_WhenFilesExist_ShouldWritePlainTextSettingsWithGeneratedKeysAndLabels()
        {
            // Arrange a small directory tree that exercises both root-level and nested path handling.
            using var directory = new TemporaryDirectory();
            directory.CreateFile("alpha.txt", "alpha value");
            directory.CreateFile(System.IO.Path.Combine("nested", "beta.json"), "{\"hello\":\"world\"}");

            var logger = new TestLogger<AdditionalConfigurationSeeder>();
            var seeder = new AdditionalConfigurationSeeder(logger);
            var client = new TestConfigurationClient();

            // Execute the seeding process against the temporary file tree.
            await seeder.SeedAsync(client, "dev", directory.Path, "additional", CancellationToken.None);

            // Materialise the writes by key so the assertions focus on the generated settings rather than discovery order.
            var writesByKey = client.Writes.ToDictionary(write => write.Setting.Key, write => write.Setting, StringComparer.Ordinal);
            Assert.Equal(2, writesByKey.Count);

            // Root-level files should use only the prefix and file name without extension.
            var rootSetting = Assert.Contains("additional:alpha", writesByKey);
            Assert.Equal("alpha value", rootSetting.Value);
            Assert.Equal("dev", rootSetting.Label);
            Assert.Equal(MediaTypeNames.Text.Plain, rootSetting.ContentType);

            // Nested files should incorporate the relative directory segments into the generated key.
            var nestedSetting = Assert.Contains("additional:nested:beta", writesByKey);
            Assert.Equal("{\"hello\":\"world\"}", nestedSetting.Value);
            Assert.Equal("dev", nestedSetting.Label);
            Assert.Equal(MediaTypeNames.Text.Plain, nestedSetting.ContentType);

            // The happy path should log debug entries rather than warnings.
            Assert.DoesNotContain(logger.Entries, entry => entry.LogLevel >= LogLevel.Warning);
        }

        /// <summary>
        /// Verifies that repository rule files stored under rules/ingestion/file-share seed into the namespace-aware App Configuration key space.
        /// </summary>
        [Fact]
        public async Task SeedAsync_WhenRuleFileStoredUnderIngestionNamespace_ShouldWriteNamespaceAwareRuleKey()
        {
            // Arrange a repository-like rules tree so the seeder exercises the real path-to-key contract for ingestion rules.
            using var directory = new TemporaryDirectory();
            directory.CreateFile(System.IO.Path.Combine("ingestion", "file-share", "bu-sample-rule.json"), "{\"rule\":{\"id\":\"bu-sample-rule\"}}");

            var logger = new TestLogger<AdditionalConfigurationSeeder>();
            var seeder = new AdditionalConfigurationSeeder(logger);
            var client = new TestConfigurationClient();

            // Execute the seeder using the repository rules root and the production prefix used by AppHost.
            await seeder.SeedAsync(client, "local", directory.Path, "rules", CancellationToken.None);

            // The emitted setting must include both the ingestion namespace and the logical provider path segments.
            var write = Assert.Single(client.Writes);
            Assert.Equal("rules:ingestion:file-share:bu-sample-rule", write.Setting.Key);
            Assert.Equal("local", write.Setting.Label);
            Assert.Equal(MediaTypeNames.Text.Plain, write.Setting.ContentType);
            Assert.Equal("{\"rule\":{\"id\":\"bu-sample-rule\"}}", write.Setting.Value);

            // The seeder should complete without warning because the nested repository path is a supported generic case.
            Assert.DoesNotContain(logger.Entries, entry => entry.LogLevel >= LogLevel.Warning);
        }

        /// <summary>
        /// Verifies that cancellation is observed before processing the first discovered file.
        /// </summary>
        [Fact]
        public async Task SeedAsync_WhenCancellationAlreadyRequested_ShouldThrowBeforeWritingAnySetting()
        {
            // Create one file so the loop would have work to do if cancellation were not checked first.
            using var directory = new TemporaryDirectory();
            directory.CreateFile("alpha.txt", "alpha value");

            var logger = new TestLogger<AdditionalConfigurationSeeder>();
            var seeder = new AdditionalConfigurationSeeder(logger);
            var client = new TestConfigurationClient();
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Execute with a pre-cancelled token to protect the cancellation guard at the start of the loop.
            await Assert.ThrowsAsync<OperationCanceledException>(() => seeder.SeedAsync(
                client,
                "local",
                directory.Path,
                "additional",
                cancellationTokenSource.Token));

            // No writes should occur because cancellation is checked before the file is read or written.
            Assert.Empty(client.Writes);
        }

        /// <summary>
        /// Verifies that the seeder preserves the file enumeration order when emitting multiple writes.
        /// </summary>
        [Fact]
        public async Task SeedAsync_WhenMultipleFilesDiscovered_ShouldWriteSettingsInEnumerationOrder()
        {
            // Arrange several files and capture the enumerator order up front so the assertion follows the same source order as production.
            using var directory = new TemporaryDirectory();
            directory.CreateFile("root.txt", "root value");
            directory.CreateFile(System.IO.Path.Combine("first", "alpha.txt"), "alpha value");
            directory.CreateFile(System.IO.Path.Combine("second", "beta.txt"), "beta value");

            var expectedKeys = AdditionalConfigurationFileEnumerator
                .EnumerateFiles(directory.Path)
                .Select(filePath => AdditionalConfigurationKeyBuilder.Build(
                    "additional",
                    AdditionalConfigurationFileEnumerator.GetRelativePathSegments(directory.Path, filePath),
                    System.IO.Path.GetFileNameWithoutExtension(filePath)))
                .ToArray();

            var logger = new TestLogger<AdditionalConfigurationSeeder>();
            var seeder = new AdditionalConfigurationSeeder(logger);
            var client = new TestConfigurationClient();

            // Execute the seeding process using the same directory tree the expected-order snapshot was derived from.
            await seeder.SeedAsync(client, "local", directory.Path, "additional", CancellationToken.None);

            // The emitted writes should follow the enumerator order exactly because the production code streams directly over that sequence.
            Assert.Equal(expectedKeys, client.Writes.Select(write => write.Setting.Key));
        }
    }
}
