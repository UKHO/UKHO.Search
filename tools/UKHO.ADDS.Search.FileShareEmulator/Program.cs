using UKHO.ADDS.Search.FileShareEmulator.Configuration;
using UKHO.ADDS.Search.OldFileShareEmulator.Components;
using UKHO.ADDS.Search.FileShareEmulator.Health;
using UKHO.ADDS.Search.FileShareEmulator.Infrastructure;

namespace UKHO.ADDS.Search.FileShareEmulator;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var environmentName = Environment.GetEnvironmentVariable("environment");

        if (string.IsNullOrWhiteSpace(environmentName))
        {
            throw new ArgumentException("Cannot read 'environment' value");
        }

        builder.Configuration["environment"] = environmentName;

        builder.Services.AddHealthChecks().AddCheck<DataSeededHealthCheck>("data-seeded");

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.AddSqlServerClient(StorageNames.FileShareEmulatorDatabase);

        builder.Services.AddSingleton<BacpacImporter>();

        var app = builder.Build();

        // Startup seeding/import flow:
        // - The AppHost mounts a named volume at `/data` and may seed it from a data image.
        // - If the SQL database does not yet contain the `Batch` table, we import a bacpac snapshot found at:
        //   `/data/{environment}.bacpac`.
        // - The readiness endpoint (`/health/ready`) only reports healthy when both the sentinel file exists and
        //   the table check/bacpac import has completed.
        try
        {
            var connectionString = builder.Configuration.GetConnectionString(StorageNames.FileShareEmulatorDatabase);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Missing connection string '{StorageNames.FileShareEmulatorDatabase}'.");
            }

            var bacpacPath = $"/data/{environmentName}.bacpac";

            await app.Services.GetRequiredService<BacpacImporter>()
                .EnsureDatabaseSeededAsync(connectionString, StorageNames.FileShareEmulatorDatabase, bacpacPath, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Startup] Bacpac import failed: {ex.GetType().Name}: {ex.Message}");
            throw;
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapHealthChecks("/health/ready");

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        await app.RunAsync().ConfigureAwait(false);
    }
}
