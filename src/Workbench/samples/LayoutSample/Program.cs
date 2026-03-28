using LayoutSample.Components;

namespace LayoutSample
{
    /// <summary>
    /// Builds and runs the standalone Workbench layout sample host.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Creates the standalone Blazor Server pipeline and starts processing requests for the extracted layout showcase.
        /// </summary>
        /// <param name="args">Command-line arguments passed to the sample host.</param>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register Razor components with interactive server rendering so the splitter demo can process drag events.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                // Route production exceptions back to the sample root and enable HSTS for non-development environments.
                app.UseExceptionHandler("/");
                app.UseHsts();
            }

            // Keep the standalone sample aligned with the default secure ASP.NET Core request pipeline.
            app.UseHttpsRedirection();

            // Enable antiforgery protection for interactive server component requests.
            app.UseAntiforgery();

            // Expose the sample's static assets together with static web assets from the referenced Workbench component library.
            app.MapStaticAssets();

            // Map the standalone sample app and enable interactive server rendering for the extracted home-route showcase.
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Start the minimal host so the sample can run directly without Aspire orchestration.
            app.Run();
        }
    }
}
