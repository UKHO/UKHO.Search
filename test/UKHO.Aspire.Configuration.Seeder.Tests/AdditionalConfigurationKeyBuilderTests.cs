using UKHO.Aspire.Configuration.Seeder.AdditionalConfiguration;
using Xunit;

namespace UKHO.Aspire.Configuration.Seeder.Tests
{
    /// <summary>
    /// Verifies how <see cref="AdditionalConfigurationKeyBuilder"/> combines the configured prefix, relative path segments, and file name into an App Configuration key.
    /// </summary>
    public sealed class AdditionalConfigurationKeyBuilderTests
    {
        /// <summary>
        /// Verifies that a root-level file produces a key containing only the prefix and file name.
        /// </summary>
        [Fact]
        public void Build_WhenNoPathSegments_ShouldReturnPrefixAndFileName()
        {
            // Build a key for a file that sits directly under the configured root path.
            var key = AdditionalConfigurationKeyBuilder.Build("prefix", Array.Empty<string>(), "file");

            // The generated key should contain only the prefix and the file name without extension.
            Assert.Equal("prefix:file", key);
        }

        /// <summary>
        /// Verifies that nested path segments are appended in order to the generated key.
        /// </summary>
        [Fact]
        public void Build_WhenNestedSegmentsPresent_ShouldReturnFullKeyInOriginalOrder()
        {
            // Build a key for a file in a nested folder hierarchy.
            var key = AdditionalConfigurationKeyBuilder.Build("prefix", ["a", "b"], "file");

            // The generated key should preserve the directory order exactly.
            Assert.Equal("prefix:a:b:file", key);
        }

        /// <summary>
        /// Verifies that rule files authored beneath the ingestion namespace preserve both the namespace and provider segments.
        /// </summary>
        [Fact]
        public void Build_WhenRuleFileUsesIngestionNamespace_ShouldReturnNamespaceAwareRuleKey()
        {
            // Build the key that should be produced for a repository rule stored under rules/ingestion/file-share.
            var key = AdditionalConfigurationKeyBuilder.Build("rules", ["ingestion", "file-share"], "bu-sample-rule");

            // The generated key must retain the ingestion namespace as well as the logical provider segment.
            Assert.Equal("rules:ingestion:file-share:bu-sample-rule", key);
        }

        /// <summary>
        /// Verifies that blank relative path segments are ignored instead of creating empty key tokens.
        /// </summary>
        [Fact]
        public void Build_WhenSegmentsContainWhitespace_ShouldIgnoreEmptySegments()
        {
            // Provide a mixture of valid and blank segments to exercise the filtering logic inside the builder.
            var key = AdditionalConfigurationKeyBuilder.Build("prefix", ["a", " ", string.Empty, "b"], "file");

            // Only non-empty path segments should appear in the generated key.
            Assert.Equal("prefix:a:b:file", key);
        }

        /// <summary>
        /// Verifies that an invalid prefix is rejected immediately.
        /// </summary>
        /// <param name="prefix">The prefix value supplied to the key builder.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Build_WhenPrefixInvalid_ShouldThrowArgumentException(string? prefix)
        {
            // Execute the builder with an invalid prefix to protect the guard clause.
            var exception = Assert.ThrowsAny<ArgumentException>(() => AdditionalConfigurationKeyBuilder.Build(prefix!, Array.Empty<string>(), "file"));

            // The exception should identify the invalid prefix parameter.
            Assert.Equal("prefix", exception.ParamName);
        }

        /// <summary>
        /// Verifies that an invalid file name is rejected immediately.
        /// </summary>
        /// <param name="fileNameWithoutExtension">The file name supplied to the key builder.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void Build_WhenFileNameInvalid_ShouldThrowArgumentException(string? fileNameWithoutExtension)
        {
            // Execute the builder with an invalid file name to protect the second guard clause.
            var exception = Assert.ThrowsAny<ArgumentException>(() => AdditionalConfigurationKeyBuilder.Build("prefix", Array.Empty<string>(), fileNameWithoutExtension!));

            // The exception should identify the invalid file name parameter.
            Assert.Equal("fileNameWithoutExtension", exception.ParamName);
        }

        /// <summary>
        /// Verifies that multiple path segments remain in the same order they were provided.
        /// </summary>
        [Fact]
        public void Build_WhenManyValidSegmentsProvided_ShouldPreserveSegmentOrdering()
        {
            // Supply several valid segments interleaved with ignored blanks so the ordering assertion is unambiguous.
            var key = AdditionalConfigurationKeyBuilder.Build("prefix", ["first", " ", "second", "third"], "file");

            // The builder should emit the non-empty segments in their original order.
            Assert.Equal("prefix:first:second:third:file", key);
        }
    }
}
