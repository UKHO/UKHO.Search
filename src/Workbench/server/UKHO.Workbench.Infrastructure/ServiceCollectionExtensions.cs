using Microsoft.Extensions.DependencyInjection;
using UKHO.Workbench.Services;
using UKHO.Workbench.Infrastructure.Modules;

namespace UKHO.Workbench.Infrastructure
{
    /// <summary>
    /// Registers the minimal server-side infrastructure wiring required by the Workbench host.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the server-side Workbench infrastructure registrations and composes the inward service layer.
        /// </summary>
        /// <param name="services">The service collection that receives the Workbench infrastructure registrations.</param>
        /// <returns>The same service collection so the host can continue fluent registration.</returns>
        public static IServiceCollection AddWorkbenchInfrastructure(this IServiceCollection services)
        {
            // Guard the extension entry point so a host configuration error fails fast.
            ArgumentNullException.ThrowIfNull(services);

            // Compose the inward layer now so later infrastructure registrations can remain centralized here.
            services.AddWorkbenchServices();

            // Module discovery services remain singleton because host startup uses them as one coordinated registration pipeline.
            services.AddSingleton<ModulesConfigurationReader>();
            services.AddSingleton<ModuleAssemblyScanner>();
            services.AddSingleton<ModuleLoader>();

            // Returning the service collection preserves the infrastructure composition seam for later work items.
            return services;
        }
    }
}
