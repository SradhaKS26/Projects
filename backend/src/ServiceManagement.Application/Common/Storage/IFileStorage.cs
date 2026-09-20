namespace ServiceManagement.Application.Common.Storage;

/// <summary>
/// Blob storage abstraction. Phase 3A uses a local-disk implementation; object
/// storage can replace it without changing callers or the metadata schema.
/// </summary>
public interface IFileStorage
{
    /// <summary>Persists the stream and returns the storage key that was written.</summary>
    Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
