using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PaddyWise.Api.Services.PestDisease;

public class AzureBlobPhotoStorageService : IPhotoStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureBlobPhotoStorageService> _logger;

    public AzureBlobPhotoStorageService(
        IConfiguration configuration, ILogger<AzureBlobPhotoStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> UploadPhotoAsync(
        Stream content, string contentType, string fileExtension, CancellationToken ct)
    {
        var connectionString = _configuration["Storage:ConnectionString"];
        var containerName = _configuration["Storage:ContainerName"];

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(containerName))
        {
            throw new StorageNotConfiguredException(
                "Storage:ConnectionString and Storage:ContainerName are required to upload a " +
                "photo. Set them with 'dotnet user-secrets set \"Storage:ConnectionString\" " +
                "\"<connection string>\"' and '... \"Storage:ContainerName\" \"<container>\"' " +
                "for local development, or the matching double-underscore environment " +
                "variables in deployment.");
        }

        var containerClient = new BlobContainerClient(connectionString, containerName);

        // Public read on blobs only (not container listing) — the URL has to be fetchable both
        // by the browser <img> tag and by CropAnalysisAgent's own server-side HTTP GET.
        // CreateIfNotExistsAsync only applies the access level on first creation — if the
        // container already existed (e.g. created manually, or left over from an earlier
        // attempt that failed before the account allowed public access), it's a silent no-op
        // and the container stays at whatever access level it already had. SetAccessPolicyAsync
        // makes this self-healing: every upload confirms the level is actually Blob, not just
        // hopes creation set it correctly once.
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);
        await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: ct);

        // Server-generated name only — never derived from the client's own filename.
        var blobName = $"{Guid.NewGuid():N}{fileExtension}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: ct);

        _logger.LogInformation("Uploaded observation photo {BlobName} to container {Container}.",
            blobName, containerName);

        return blobClient.Uri.ToString();
    }
}
