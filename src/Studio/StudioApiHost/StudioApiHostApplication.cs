using Scalar.AspNetCore;
using UKHO.Search.Ingestion.Providers.FileShare.Injection;
using UKHO.Search.ProviderModel;
using UKHO.Search.Studio;
using UKHO.Search.Studio.Providers.FileShare.Injection;

namespace StudioApiHost
{
    public static class StudioApiHostApplication
    {
        private const string OpenApiDocumentName = "v1";
        private const string OpenApiRoutePattern = "/openapi/{documentName}.json";

        public static WebApplication BuildApp(string[] args, Action<WebApplicationBuilder>? configureBuilder = null)
        {
            var builder = WebApplication.CreateBuilder(args);
            var studioShellOrigin = "http://localhost:3000";

            configureBuilder?.Invoke(builder);

            builder.Services.AddAuthorization();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("StudioShell", policy =>
                {
                    policy.WithOrigins(studioShellOrigin)
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });
            builder.Services.AddOpenApi(OpenApiDocumentName);
            builder.Services.AddFileShareProviderMetadata();
            builder.Services.AddFileShareStudioProvider();

            var app = builder.Build();

            app.Services.GetRequiredService<IStudioProviderRegistrationValidator>()
               .Validate();

            app.UseCors("StudioShell");
            app.UseAuthorization();

            app.MapOpenApi(OpenApiRoutePattern);
            app.MapScalarApiReference(
                "/scalar/v1",
                options =>
                {
                    options.WithOpenApiRoutePattern(OpenApiRoutePattern);
                    options.Servers = [];
                });

            app.MapGet("/providers", (IProviderCatalog providerCatalog) =>
            {
                return TypedResults.Ok(providerCatalog.GetAllProviders());
            })
            .WithName("GetProviders");

            app.MapGet("/echo", () => TypedResults.Text("Hello from StudioApiHost echo."))
               .WithName("GetEcho");

            return app;
        }
    }
}
