using RulesWorkbench.Components;
using RulesWorkbench.Builder;
using RulesWorkbench.Services;
using UKHO.Aspire.Configuration;
using UKHO.Search.Configuration;
using UKHO.Search.Infrastructure.Ingestion.Injection;
using UKHO.Search.Infrastructure.Ingestion.Rules;

namespace RulesWorkbench
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddSingleton<RulesSnapshotStore>();
            builder.Services.AddSingleton<AppConfigRulesSnapshotStore>();
            builder.Services.AddSingleton<IRuleBuilderMapper, RuleBuilderMapper>();
            builder.Services.AddSingleton<IRuleJsonValidator, SystemTextJsonRuleJsonValidator>();
            builder.Services.AddSingleton<EvaluationPayloadMapper>();
            builder.Services.AddSingleton<RuleEvaluationService>();
            builder.Services.AddScoped<BatchPayloadLoader>();
            builder.Services.AddScoped<IClipboardService, BrowserClipboardService>();

            builder.AddConfiguration(ServiceConfiguration.ServiceGroupName, ServiceNames.Configuration);

            builder.Services.AddIngestionRulesEngine();

            builder.AddSqlServerClient(StorageNames.FileShareEmulatorDatabase);
            builder.AddAzureBlobServiceClient(ServiceNames.Blobs);

            var app = builder.Build();

            {
                using var scope = app.Services.CreateScope();

                // Startup-load rule engine rules from Azure App Configuration.
                // Hot refresh is deferred to a later work item.
                scope.ServiceProvider.GetRequiredService<IIngestionRulesCatalog>().EnsureLoaded();
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

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
