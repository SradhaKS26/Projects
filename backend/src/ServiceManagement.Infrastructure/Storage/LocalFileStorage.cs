using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Application.Common.Storage;
using ServiceManagement.Infrastructure.Options;

namespace ServiceManagement.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<FileStorageOptions> options, IHostEnvironment environment)
    {
        var configured = options.Value.LocalRoot;
        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));

        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageKey);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(output, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageKey);
        if (!File.Exists(path))
        {
            throw new NotFoundException("File not found.");
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = Resolve(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string Resolve(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)
            || storageKey.Contains("..", StringComparison.Ordinal)
            || Path.IsPathRooted(storageKey))
        {
            throw new AppException("Invalid storage key.");
        }

        var full = Path.GetFullPath(Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(_root, StringComparison.Ordinal))
        {
            throw new AppException("Invalid storage key.");
        }

        return full;
    }
}
