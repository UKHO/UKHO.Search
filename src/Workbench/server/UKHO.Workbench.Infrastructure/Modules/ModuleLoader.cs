using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.Workbench.Modules;

namespace UKHO.Workbench.Infrastructure.Modules
{
    /// <summary>
    /// Loads discovered module assemblies and invokes their bounded registration entry points.
    /// </summary>
    public class ModuleLoader
    {
        private readonly ILogger<ModuleLoader> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleLoader"/> class.
        /// </summary>
        /// <param name="logger">The logger used to record assembly loading, registration progress, and failures.</param>
        public ModuleLoader(ILogger<ModuleLoader> logger)
        {
            // The loader owns reflection and activation so the host can stay focused on orchestration and notifications.
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Loads the discovered modules and allows each valid module to register services and static contributions.
        /// </summary>
        /// <param name="discoveredAssemblies">The discovered and enabled module assemblies that should be processed.</param>
        /// <param name="services">The host service collection that still accepts registrations before container finalization.</param>
        /// <param name="contributionRegistry">The bounded contribution registry that collects module-provided Workbench definitions.</param>
        /// <returns>The aggregated outcome of the module loading run.</returns>
        public ModuleLoadResult LoadModules(
            IReadOnlyList<DiscoveredModuleAssembly> discoveredAssemblies,
            IServiceCollection services,
            IWorkbenchContributionRegistry contributionRegistry)
        {
            // Module loading must keep service registration and contribution collection under host control.
            ArgumentNullException.ThrowIfNull(discoveredAssemblies);
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(contributionRegistry);

            var loadResult = new ModuleLoadResult();

            foreach (var discoveredAssembly in discoveredAssemblies)
            {
                // Each discovery record is handled independently so one bad module cannot prevent later modules from being considered.
                if (!TryLoadAssembly(discoveredAssembly, loadResult, out var assembly))
                {
                    continue;
                }

                if (!TryCreateModule(assembly, discoveredAssembly, loadResult, out var module))
                {
                    continue;
                }

                TryRegisterModule(module, discoveredAssembly, services, contributionRegistry, loadResult);
            }

            return loadResult;
        }

        /// <summary>
        /// Attempts to load the assembly described by the discovery record.
        /// </summary>
        /// <param name="discoveredAssembly">The discovery record being processed.</param>
        /// <param name="loadResult">The aggregate load result that should capture any failure.</param>
        /// <param name="assembly">The loaded assembly when the operation succeeds; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the assembly was loaded successfully; otherwise, <see langword="false"/>.</returns>
        private bool TryLoadAssembly(DiscoveredModuleAssembly discoveredAssembly, ModuleLoadResult loadResult, out Assembly assembly)
        {
            // Assembly loading is isolated so a file-system or CLR load failure can be reported with a clear startup stage.
            ArgumentNullException.ThrowIfNull(discoveredAssembly);
            ArgumentNullException.ThrowIfNull(loadResult);

            try
            {
                assembly = LoadAssembly(discoveredAssembly.AssemblyPath);

                _logger.LogInformation(
                    "Loaded Workbench module assembly {ModuleId} from {AssemblyPath} discovered under probe root {ProbeRoot}.",
                    discoveredAssembly.ModuleId,
                    discoveredAssembly.AssemblyPath,
                    discoveredAssembly.ProbeRoot);

                return true;
            }
            catch (Exception exception)
            {
                // The loader logs detailed technical information while also returning a structured failure that preserves the assembly and probe-root context.
                RecordFailure(discoveredAssembly, "assembly-load", exception, loadResult);
                assembly = null!;
                return false;
            }
        }

        /// <summary>
        /// Attempts to create the bounded module entry point from a loaded assembly.
        /// </summary>
        /// <param name="assembly">The loaded assembly being processed.</param>
        /// <param name="discoveredAssembly">The discovery record that identifies the module candidate.</param>
        /// <param name="loadResult">The aggregate load result that should capture any failure.</param>
        /// <param name="module">The instantiated module entry point when the operation succeeds; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the module entry point was created successfully; otherwise, <see langword="false"/>.</returns>
        private bool TryCreateModule(Assembly assembly, DiscoveredModuleAssembly discoveredAssembly, ModuleLoadResult loadResult, out IWorkbenchModule module)
        {
            // Module entry creation is isolated so reflection failures are distinguished from file loading and registration failures.
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(discoveredAssembly);
            ArgumentNullException.ThrowIfNull(loadResult);

            try
            {
                module = CreateModule(assembly, discoveredAssembly);

                _logger.LogInformation(
                    "Created Workbench module entry point {ModuleType} for module {ModuleId} from {AssemblyPath}.",
                    module.GetType().FullName,
                    discoveredAssembly.ModuleId,
                    discoveredAssembly.AssemblyPath);

                return true;
            }
            catch (Exception exception)
            {
                // Reflection-based startup failures are captured separately so the failure stage points at entry-point discovery rather than registration.
                RecordFailure(discoveredAssembly, "module-entry", exception, loadResult);
                module = null!;
                return false;
            }
        }

        /// <summary>
        /// Attempts to register the created module with the host.
        /// </summary>
        /// <param name="module">The instantiated module entry point.</param>
        /// <param name="discoveredAssembly">The discovery record that identifies the module candidate.</param>
        /// <param name="services">The mutable host service collection.</param>
        /// <param name="contributionRegistry">The bounded contribution registry that receives module contributions.</param>
        /// <param name="loadResult">The aggregate load result that should capture success or failure.</param>
        private void TryRegisterModule(
            IWorkbenchModule module,
            DiscoveredModuleAssembly discoveredAssembly,
            IServiceCollection services,
            IWorkbenchContributionRegistry contributionRegistry,
            ModuleLoadResult loadResult)
        {
            // Registration is isolated so module-thrown exceptions are attributed to the registration stage instead of earlier startup work.
            ArgumentNullException.ThrowIfNull(module);
            ArgumentNullException.ThrowIfNull(discoveredAssembly);
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(contributionRegistry);
            ArgumentNullException.ThrowIfNull(loadResult);

            try
            {
                var context = new ModuleRegistrationContext(module.Metadata, services, contributionRegistry);

                _logger.LogInformation(
                    "Registering Workbench module {ModuleId} from {AssemblyPath} discovered under probe root {ProbeRoot}.",
                    module.Metadata.Id,
                    discoveredAssembly.AssemblyPath,
                    discoveredAssembly.ProbeRoot);

                module.Register(context);
                loadResult.AddLoadedModule(new LoadedWorkbenchModule(module.Metadata, discoveredAssembly.AssemblyPath, discoveredAssembly.ProbeRoot));

                _logger.LogInformation(
                    "Registered Workbench module {ModuleId} from {AssemblyPath} discovered under probe root {ProbeRoot}.",
                    module.Metadata.Id,
                    discoveredAssembly.AssemblyPath,
                    discoveredAssembly.ProbeRoot);
            }
            catch (Exception exception)
            {
                // Registration failures are recorded with the discovery metadata so startup diagnostics can identify both the probe root and the failure stage.
                RecordFailure(discoveredAssembly, "registration", exception, loadResult);
            }
        }

        /// <summary>
        /// Records a structured module-loading failure and writes the corresponding diagnostic log entry.
        /// </summary>
        /// <param name="discoveredAssembly">The discovery record that identifies the failing module candidate.</param>
        /// <param name="failureStage">The startup stage where the failure occurred.</param>
        /// <param name="exception">The exception describing the failure.</param>
        /// <param name="loadResult">The aggregate load result that should capture the failure.</param>
        private void RecordFailure(
            DiscoveredModuleAssembly discoveredAssembly,
            string failureStage,
            Exception exception,
            ModuleLoadResult loadResult)
        {
            // A single helper keeps failure logging and the structured failure payload aligned across all startup stages.
            ArgumentNullException.ThrowIfNull(discoveredAssembly);
            ArgumentException.ThrowIfNullOrWhiteSpace(failureStage);
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentNullException.ThrowIfNull(loadResult);

            _logger.LogError(
                exception,
                "Failed during Workbench module stage {FailureStage} for module {ModuleId} from {AssemblyPath} discovered under probe root {ProbeRoot}.",
                failureStage,
                discoveredAssembly.ModuleId,
                discoveredAssembly.AssemblyPath,
                discoveredAssembly.ProbeRoot);

            loadResult.AddFailure(
                new ModuleLoadFailure(
                    discoveredAssembly.ModuleId,
                    discoveredAssembly.AssemblyPath,
                    discoveredAssembly.ProbeRoot,
                    failureStage,
                    exception.Message));
        }

        /// <summary>
        /// Loads an assembly from disk or reuses the already loaded assembly instance when possible.
        /// </summary>
        /// <param name="assemblyPath">The absolute path to the assembly that should be loaded.</param>
        /// <returns>The loaded assembly instance.</returns>
        private static Assembly LoadAssembly(string assemblyPath)
        {
            // Module tests may already have loaded the assembly, so the loader reuses matching paths before attempting another load.
            ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
            var fullAssemblyPath = Path.GetFullPath(assemblyPath);

            var existingAssembly = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(
                assembly => !string.IsNullOrWhiteSpace(assembly.Location)
                    && string.Equals(Path.GetFullPath(assembly.Location), fullAssemblyPath, StringComparison.OrdinalIgnoreCase));

            if (existingAssembly is not null)
            {
                return existingAssembly;
            }

            return AssemblyLoadContext.Default.LoadFromAssemblyPath(fullAssemblyPath);
        }

        /// <summary>
        /// Creates the bounded module entry point from a loaded assembly.
        /// </summary>
        /// <param name="assembly">The assembly that should contain the module entry point.</param>
        /// <param name="discoveredAssembly">The discovery record that identifies the assembly being processed.</param>
        /// <returns>The instantiated module entry point.</returns>
        private static IWorkbenchModule CreateModule(Assembly assembly, DiscoveredModuleAssembly discoveredAssembly)
        {
            // A valid module assembly must expose exactly one concrete IWorkbenchModule entry point with a public parameterless constructor.
            ArgumentNullException.ThrowIfNull(assembly);
            ArgumentNullException.ThrowIfNull(discoveredAssembly);

            var moduleTypes = assembly
                .GetTypes()
                .Where(type => typeof(IWorkbenchModule).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                .ToArray();

            if (moduleTypes.Length != 1)
            {
                throw new InvalidOperationException(
                    $"The Workbench module assembly '{discoveredAssembly.AssemblyPath}' must expose exactly one concrete {nameof(IWorkbenchModule)} implementation.");
            }

            var module = Activator.CreateInstance(moduleTypes[0]) as IWorkbenchModule;
            if (module is null)
            {
                throw new InvalidOperationException(
                    $"The Workbench module entry point '{moduleTypes[0].FullName}' could not be instantiated.");
            }

            return module;
        }
    }
}
