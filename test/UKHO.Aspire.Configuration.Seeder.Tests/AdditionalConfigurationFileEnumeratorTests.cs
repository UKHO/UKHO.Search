using UKHO.Aspire.Configuration.Seeder.AdditionalConfiguration;
using UKHO.Aspire.Configuration.Seeder.Tests.TestSupport;
using Xunit;

namespace UKHO.Aspire.Configuration.Seeder.Tests
{
    /// <summary>
    /// Verifies that <see cref="AdditionalConfigurationFileEnumerator"/> discovers files recursively and derives relative path segments for key generation.
    /// </summary>
    public sealed class AdditionalConfigurationFileEnumeratorTests
    {
        /// <summary>
        /// Verifies that the file enumerator rejects a missing root path.
        /// </summary>
        /// <param name="rootPath">The root path value supplied to the enumerator.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void EnumerateFiles_WhenRootPathInvalid_ShouldThrowArgumentException(string? rootPath)
        {
            // Invoke the enumerator with an invalid root path to protect the guard clause.
            var exception = Assert.ThrowsAny<ArgumentException>(() => AdditionalConfigurationFileEnumerator.EnumerateFiles(rootPath!).ToArray());

            // The exception should identify the invalid root-path parameter.
            Assert.Equal("rootPath", exception.ParamName);
        }

        /// <summary>
        /// Verifies that recursive enumeration returns files from the full directory tree.
        /// </summary>
        [Fact]
        public void EnumerateFiles_WhenNestedFilesExist_ShouldReturnAllFiles()
        {
            // Arrange a temporary directory tree with files at the root and in nested subdirectories.
            using var directory = new TemporaryDirectory();
            var rootFile = directory.CreateFile("root.json", "{}");
            var nestedFile = directory.CreateFile(Path.Combine("nested", "child.json"), "{}");
            var deeperFile = directory.CreateFile(Path.Combine("nested", "deeper", "grandchild.json"), "{}");

            // Enumerate the files beneath the temporary root.
            var files = AdditionalConfigurationFileEnumerator.EnumerateFiles(directory.Path)
                .OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // The enumerator should return every file in the tree exactly once.
            Assert.Equal(
                new[] { rootFile, nestedFile, deeperFile }.OrderBy(filePath => filePath, StringComparer.OrdinalIgnoreCase),
                files);
        }

        /// <summary>
        /// Verifies that a root-level file produces no relative directory segments.
        /// </summary>
        [Fact]
        public void GetRelativePathSegments_WhenFileAtRoot_ShouldReturnEmptySegments()
        {
            // Resolve the relative directory segments for a file directly under the configured root.
            var segments = AdditionalConfigurationFileEnumerator.GetRelativePathSegments(
                "C:\\root",
                "C:\\root\\file.json");

            // There should be no relative directory segments for a root-level file.
            Assert.Empty(segments);
        }

        /// <summary>
        /// Verifies that nested files produce one segment per relative directory.
        /// </summary>
        [Fact]
        public void GetRelativePathSegments_WhenFileNested_ShouldReturnDirectorySegments()
        {
            // Resolve the relative directory segments for a file in a nested hierarchy.
            var segments = AdditionalConfigurationFileEnumerator.GetRelativePathSegments(
                "C:\\root",
                "C:\\root\\a\\b\\file.json");

            // The returned segment list should preserve the relative folder hierarchy.
            Assert.Equal(["a", "b"], segments);
        }

        /// <summary>
        /// Verifies that repository rule files under the ingestion namespace surface the namespace and provider segments separately.
        /// </summary>
        [Fact]
        public void GetRelativePathSegments_WhenRuleFileStoredUnderIngestionNamespace_ShouldReturnNamespaceThenProvider()
        {
            // Resolve the relative path for a repository rule file beneath rules/ingestion/file-share.
            var segments = AdditionalConfigurationFileEnumerator.GetRelativePathSegments(
                "C:\\repo\\rules",
                "C:\\repo\\rules\\ingestion\\file-share\\bu-sample-rule.json");

            // The helper must preserve both directory levels so the seeder can generate the correct App Configuration key.
            Assert.Equal(["ingestion", "file-share"], segments);
        }

        /// <summary>
        /// Verifies that invalid root-path and file-path inputs are rejected.
        /// </summary>
        /// <param name="rootPath">The root path value supplied to the relative path helper.</param>
        /// <param name="filePath">The file path value supplied to the relative path helper.</param>
        [Theory]
        [InlineData(null, "C:\\root\\file.json")]
        [InlineData("", "C:\\root\\file.json")]
        [InlineData(" ", "C:\\root\\file.json")]
        [InlineData("C:\\root", null)]
        [InlineData("C:\\root", "")]
        [InlineData("C:\\root", " ")]
        public void GetRelativePathSegments_WhenInputsInvalid_ShouldThrowArgumentException(string? rootPath, string? filePath)
        {
            // Execute the helper with an invalid input so each guard clause stays protected.
            Assert.ThrowsAny<ArgumentException>(() => AdditionalConfigurationFileEnumerator.GetRelativePathSegments(rootPath!, filePath!));
        }

        /// <summary>
        /// Verifies that alternative directory separators are handled when deriving relative path segments.
        /// </summary>
        [Fact]
        public void GetRelativePathSegments_WhenAlternativeSeparatorsUsed_ShouldReturnExpectedSegments()
        {
            // Use forward slashes to ensure the helper supports the platform's alternative separator as well as the primary separator.
            var segments = AdditionalConfigurationFileEnumerator.GetRelativePathSegments(
                "C:/root",
                "C:/root/a/b/file.json");

            // The returned segments should still match the relative directory hierarchy.
            Assert.Equal(["a", "b"], segments);
        }
    }
}
