using FileShareEmulator.Components;
using FileShareEmulator.Api;
using FileShareEmulator.Services;
using Radzen;
using Radzen.Blazor;
using UKHO.Search.Configuration;
using UKHO.Search.ServiceDefaults;

namespace FileShareEmulator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.AddSqlServerClient(StorageNames.FileShareEmulatorDatabase);
            builder.AddAzureBlobServiceClient(connectionName: ServiceNames.Blobs);

            builder.Services.AddScoped<StatisticsService>();

            builder.Services.AddRadzenComponents();
            builder.Services.AddRadzenQueryStringThemeService();
            builder.Services.AddLocalization();

            var app = builder.Build();

            app.MapDefaultEndpoints();
            app.MapBatchFilesApi();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
