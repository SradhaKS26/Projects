namespace ServiceManagement.Infrastructure.Options;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Directory for provider documents. Relative paths are resolved against the content root.</summary>
    public string LocalRoot { get; set; } = "storage/provider-documents";

    public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;

    public string[] AllowedContentTypes { get; set; } =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp"
    ];
}
