using System.Xml.Linq;
using Xunit;

namespace UKHO.Search.Ingestion.Contracts.Tests
{
    /// <summary>
    /// Verifies that the contracts package project file stays within the dependency-light boundary defined for WP100.
    /// </summary>
    public sealed class ContractsProjectBoundaryTests
    {
        /// <summary>
        /// Confirms that the contracts project does not gain any project references.
        /// </summary>
        [Fact]
        public void ContractsProject_WhenLoaded_DoesNotContainProjectReferences()
        {
            // Load the canonical contracts project file from the repository so the test exercises the real boundary.
            var projectDocument = LoadContractsProjectDocument();

            // The WP100 boundary is broken if the contracts package points at any repository project.
            var projectReferences = projectDocument.Descendants("ProjectReference")
                                                   .ToList();

            Assert.True(
                projectReferences.Count == 0,
                $"The contracts project must not reference other projects. Found: {string.Join(", ", projectReferences.Select(r => r.Attribute("Include")?.Value ?? "<unknown>"))}");
        }

        /// <summary>
        /// Confirms that the contracts project does not gain any package references.
        /// </summary>
        [Fact]
        public void ContractsProject_WhenLoaded_DoesNotContainPackageReferences()
        {
            // Load the canonical contracts project file from the repository so the test inspects the actual package boundary.
            var projectDocument = LoadContractsProjectDocument();

            // WP100 keeps the contracts package limited to the in-box .NET base class library and framework-provided JSON APIs.
            var packageReferences = projectDocument.Descendants("PackageReference")
                                                   .ToList();

            Assert.True(
                packageReferences.Count == 0,
                $"The contracts project must not reference external packages. Found: {string.Join(", ", packageReferences.Select(r => r.Attribute("Include")?.Value ?? "<unknown>"))}");
        }

        /// <summary>
        /// Loads the contracts project file from the repository root for boundary assertions.
        /// </summary>
        /// <returns>
        /// An XML document representing the contracts project file.
        /// </returns>
        private static XDocument LoadContractsProjectDocument()
        {
            // Resolve the repository root first so the audit still works if the test output directory shape changes.
            var repositoryRoot = FindRepositoryRoot();

            // Build the project path from the repository root so the test always inspects the real contracts project.
            var projectPath = Path.Combine(
                repositoryRoot,
                "src",
                "UKHO.Search.Ingestion.Contracts",
                "UKHO.Search.Ingestion.Contracts.csproj");

            Assert.True(File.Exists(projectPath), $"Expected contracts project file was not found at '{projectPath}'.");

            return XDocument.Load(projectPath);
        }

        /// <summary>
        /// Walks upward from the test output directory until the repository root is found.
        /// </summary>
        /// <returns>
        /// The absolute path to the repository root.
        /// </returns>
        private static string FindRepositoryRoot()
        {
            // Start from the current test host output directory and move upward until the solution marker is found.
            var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

            while (currentDirectory is not null)
            {
                var solutionPath = Path.Combine(currentDirectory.FullName, "Search.slnx");
                if (File.Exists(solutionPath))
                {
                    return currentDirectory.FullName;
                }

                currentDirectory = currentDirectory.Parent;
            }

            throw new InvalidOperationException("Could not locate the repository root from the test output directory.");
        }
    }
}