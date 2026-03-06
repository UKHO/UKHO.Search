using Azure;
using Azure.Storage.Blobs;

namespace FileShareEmulator.Api
{
    public static class BatchFilesApi
    {
        public static IEndpointRouteBuilder MapBatchFilesApi(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/batch/{batchId}/files", async (
                    string batchId,
                    BlobServiceClient blobServiceClient,
                    IConfiguration configuration,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(batchId))
                    {
                        return Results.BadRequest("batchId is required.");
                    }

                    var containerName = configuration["ASPNETCORE_ENVIRONMENT"] ?? httpContext.RequestServices.GetService<IHostEnvironment>()?.EnvironmentName;
                    if (string.IsNullOrWhiteSpace(containerName))
                    {
                        return Results.Problem("Unable to determine blob container name.", statusCode: StatusCodes.Status500InternalServerError);
                    }

                    var blobName = $"{batchId}/{batchId}.zip";
                    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    var blobClient = containerClient.GetBlobClient(blobName);

                    try
                    {
                        var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                        var contentType = download.Value.Details.ContentType;
                        if (string.IsNullOrWhiteSpace(contentType))
                        {
                            contentType = "application/zip";
                        }

                        var fileName = $"{batchId}.zip";
                        return Results.Stream(download.Value.Content,
                            contentType: contentType,
                            fileDownloadName: fileName);
                    }
                    catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status404NotFound)
                    {
                        return Results.NotFound();
                    }
                    catch (RequestFailedException ex) when (ex.Status == StatusCodes.Status403Forbidden)
                    {
                        return Results.StatusCode(StatusCodes.Status403Forbidden);
                    }
                    catch (RequestFailedException)
                    {
                        return Results.StatusCode(StatusCodes.Status502BadGateway);
                    }
                })
                .WithName("GetBatchFiles")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status403Forbidden)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status502BadGateway)
                .Produces(StatusCodes.Status500InternalServerError);

            return endpoints;
        }
    }
}
