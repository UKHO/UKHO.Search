using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Workbench.Infrastructure.Modules;
using UKHO.Workbench.Modules;
using Xunit;

namespace UKHO.Workbench.Infrastructure.Tests
{
    /// <summary>
    /// Verifies that the Workbench module loader records diagnosable failures for invalid assemblies.
    /// </summary>
    public class ModuleLoaderTests
    {
        /// <summary>
        /// Confirms that an invalid assembly does not stop the loader from returning a structured failure result.
        /// </summary>
        [Fact]
        public void CaptureFailuresForInvalidAssemblies()
        {
            // The loader test uses a fake DLL file so the invalid-assembly path can be exercised without compiling a dedicated bad module.
            var temporaryDirectoryPath = Path.Combine(Path.GetTempPath(), $"workbench-module-load-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporaryDirectoryPath);

            try
            {
                var invalidAssemblyPath = Path.Combine(temporaryDirectoryPath, "UKHO.Workbench.Modules.Invalid.dll");
                File.WriteAllText(invalidAssemblyPath, "not a managed assembly");

                var loader = new ModuleLoader(NullLogger<ModuleLoader>.Instance);
                var contributionRegistry = new WorkbenchContributionRegistry();
                var discoveredAssemblies = new[]
                {
                    new DiscoveredModuleAssembly("UKHO.Workbench.Modules.Invalid", invalidAssemblyPath, temporaryDirectoryPath)
                };

                // Loading should continue gracefully and report the failure details rather than throwing for the whole discovery run.
                var result = loader.LoadModules(discoveredAssemblies, new ServiceCollection(), contributionRegistry);

                result.LoadedModules.ShouldBeEmpty();
                result.Failures.Count.ShouldBe(1);
                result.Failures[0].ModuleId.ShouldBe("UKHO.Workbench.Modules.Invalid");
                result.Failures[0].ProbeRoot.ShouldBe(temporaryDirectoryPath);
                result.Failures[0].FailureStage.ShouldBe("assembly-load");
                result.Failures[0].AssemblyPath.ShouldBe(invalidAssemblyPath);
                contributionRegistry.ToolDefinitions.ShouldBeEmpty();
            }
            finally
            {
                // The temporary invalid assembly file is removed after the failure path has been verified.
                Directory.Delete(temporaryDirectoryPath, true);
            }
        }
    }
}
