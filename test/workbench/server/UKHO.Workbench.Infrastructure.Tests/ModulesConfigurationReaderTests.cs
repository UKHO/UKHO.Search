using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using UKHO.Workbench.Infrastructure.Modules;
using Xunit;

namespace UKHO.Workbench.Infrastructure.Tests
{
    /// <summary>
    /// Verifies that the Workbench module configuration reader deserializes the host-owned <c>modules.json</c> format correctly.
    /// </summary>
    public class ModulesConfigurationReaderTests
    {
        /// <summary>
        /// Confirms that probe roots and per-module enablement flags are read from disk.
        /// </summary>
        [Fact]
        public void ReadTheHostOwnedModuleConfigurationFile()
        {
            // The reader test uses a temporary file so the repository tree stays untouched while the JSON contract is verified.
            var temporaryDirectoryPath = Path.Combine(Path.GetTempPath(), $"workbench-modules-config-{Guid.NewGuid():N}");
            Directory.CreateDirectory(temporaryDirectoryPath);

            try
            {
                var configurationPath = Path.Combine(temporaryDirectoryPath, "modules.json");
                File.WriteAllText(
                    configurationPath,
                    """
                    {
                      "probeRoots": ["modules", "../shared-modules"],
                      "modules": [
                        { "id": "UKHO.Workbench.Modules.Search", "enabled": true },
                        { "id": "UKHO.Workbench.Modules.Admin", "enabled": false }
                      ]
                    }
                    """);

                var reader = new ModulesConfigurationReader(NullLogger<ModulesConfigurationReader>.Instance);

                // Reading the file should preserve the probe roots and explicit enablement decisions declared by the host.
                var options = reader.Read(configurationPath);

                options.ProbeRoots.Count.ShouldBe(2);
                options.ProbeRoots[0].ShouldBe("modules");
                options.Modules.Count.ShouldBe(2);
                options.IsModuleEnabled("UKHO.Workbench.Modules.Search").ShouldBeTrue();
                options.IsModuleEnabled("UKHO.Workbench.Modules.Admin").ShouldBeFalse();
                options.IsModuleEnabled("UKHO.Workbench.Modules.FileShare").ShouldBeTrue();
            }
            finally
            {
                // Temporary configuration files are removed so repeated test runs remain isolated.
                Directory.Delete(temporaryDirectoryPath, true);
            }
        }
    }
}
