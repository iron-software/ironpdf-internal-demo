using IronPdfDemo.Configuration;
using IronPdfDemo.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace IronPdfDemo.Services.Implementations;

public class FileStorageService : IFileStorageService
{
    private readonly StorageOptions _options;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(
        IOptions<StorageOptions> options,
        IWebHostEnvironment env,
        IHttpContextAccessor httpContextAccessor,
        ILogger<FileStorageService> logger)
    {
        _options = options.Value;
        _env = env;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string GetOutputDirectory()
    {
        var path = Path.IsPathRooted(_options.OutputDirectory)
            ? _options.OutputDirectory
            : Path.Combine(_env.ContentRootPath, _options.OutputDirectory);
        return path;
    }

    public string GetTempDirectory()
    {
        var path = Path.IsPathRooted(_options.TempDirectory)
            ? _options.TempDirectory
            : Path.Combine(_env.ContentRootPath, _options.TempDirectory);
        return path;
    }

    public async Task<string> SaveFileAsync(byte[] data, string baseName, string suffix = "")
    {
        EnsureDirectoriesExist();
        var fileName = GenerateUniqueFileName(baseName, suffix);
        var filePath = Path.Combine(GetOutputDirectory(), fileName);
        await File.WriteAllBytesAsync(filePath, data);
        _logger.LogDebug("Saved PDF: {FileName} ({Bytes} bytes)", fileName, data.Length);
        return fileName;
    }

    public string GenerateUniqueFileName(string baseName, string suffix = "")
    {
        var sanitized = SanitizeFileName(baseName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var tag = string.IsNullOrWhiteSpace(suffix) ? "" : $"_{suffix}";
        return $"{sanitized}{tag}_{timestamp}_{uniqueId}.pdf";
    }

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(GetOutputDirectory());
        Directory.CreateDirectory(GetTempDirectory());
    }

    public IEnumerable<FileInfo> ListOutputFiles()
    {
        var dir = new DirectoryInfo(GetOutputDirectory());
        return dir.Exists
            ? dir.GetFiles("*.pdf").OrderByDescending(f => f.CreationTimeUtc)
            : Enumerable.Empty<FileInfo>();
    }

    public void CleanOldFiles(int retainDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retainDays);
        foreach (var file in ListOutputFiles().Where(f => f.CreationTimeUtc < cutoff))
        {
            try { file.Delete(); _logger.LogInformation("Cleaned old file: {Name}", file.Name); }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not delete old file: {Name}", file.Name); }
        }
    }

    public string GetDownloadUrl(string fileName)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return $"/api/files/download/{fileName}";
        var req = ctx.Request;
        return $"{req.Scheme}://{req.Host}/api/files/download/{fileName}";
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "output" : sanitized[..Math.Min(sanitized.Length, 50)];
    }
}
