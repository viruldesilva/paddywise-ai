namespace PaddyWise.Api.Services.PestDisease;

/// <summary>Stores an observation photo and hands back a public URL for it — currently backed
/// by Azure Blob Storage. Kept as an interface so the backing store could change without
/// touching callers.</summary>
public interface IPhotoStorageService
{
    /// <summary>Uploads the content under a server-generated name and returns its public URL.
    /// Throws StorageNotConfiguredException if Storage:ConnectionString /
    /// Storage:ContainerName are not configured.</summary>
    Task<string> UploadPhotoAsync(
        Stream content, string contentType, string fileExtension, CancellationToken ct);
}

/// <summary>A server setup problem (missing secrets), not something the caller did wrong —
/// callers should surface this as a 500, not a 400.</summary>
public class StorageNotConfiguredException : Exception
{
    public StorageNotConfiguredException(string message) : base(message)
    {
    }
}
