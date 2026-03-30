using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Workbench.Infrastructure.Modules;
using Xunit;

namespace UKHO.Workbench.Infrastructure.Tests
{
    /// <summary>
    /// Verifies that the Workbench module assembly scanner respects approved probe roots, naming rules, and enablement settings.
    /// </summary>
    public class ModuleAssemblyScannerTests
    {
        /// <summary>
        /// Confirms that the scanner resolves relative probe roots and suppresses disabled modules.
        /// </summary>
        [Fact]
        public void ResolveRelativeProbeRootsAndSkipDisabledModules()
        {
            // The scanner test creates a temporary probe root so file-system discovery behavior can be exercised without repository side effects.
            var temporaryDirectoryPath = Path.Combine(Path.GetTempPath(), $"workbench-module-scan-{Guid.NewGuid():N}");
            var probeRootPath = Path.Combine(temporaryDirectoryPath, "modules");
            Directory.CreateDirectory(probeRootPath);

            try
            {
                File.WriteAllText(Path.Combine(probeRootPath, "UKHO.Workbench.Modules.Search.dll"), string.Empty);
                File.WriteAllText(Path.Combine(probeRootPath, "UKHO.Workbench.Modules.Admin.dll"), string.Empty);
                File.WriteAllText(Path.Combine(probeRootPath, "NotAWorkbenchModule.dll"), string.Empty);

                var options = new ModulesOptions
                {
                    ProbeRoots = ["modules"],
                    Modules =
                    [
                        new ModuleOptions { Id = "UKHO.Workbench.Modules.Search", Enabled = true },
                        new ModuleOptions { Id = "UKHO.Workbench.Modules.Admin", Enabled = false }
                    ]
                };

                var scanner = new ModuleAssemblyScanner(NullLogger<ModuleAssemblyScanner>.Instance);

                // Only enabled assemblies with the approved naming convention should survive scanning.
                var discoveredAssemblies = scanner.Scan(options, temporaryDirectoryPath);

                discoveredAssemblies.Count.ShouldBe(1);
                discoveredAssemblies[0].ModuleId.ShouldBe("UKHO.Workbench.Modules.Search");
                discoveredAssemblies[0].ProbeRoot.ShouldBe(Path.GetFullPath(probeRootPath));
                discoveredAssemblies[0].AssemblyPath.ShouldBe(Path.GetFullPath(Path.Combine(probeRootPath, "UKHO.Workbench.Modules.Search.dll")));
            }
            finally
            {
                // The temporary probe root is removed after the scan assertion completes.
                Directory.Delete(temporaryDirectoryPath, true);
            }
        }
    }
}
