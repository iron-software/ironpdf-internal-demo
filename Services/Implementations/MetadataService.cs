using System.Diagnostics;
using IronPdf;
using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;
using IronPdfDemo.Services.Interfaces;

namespace IronPdfDemo.Services.Implementations;

public class MetadataService : IMetadataService
{
    private readonly IFileStorageService _storage;
    private readonly ILogger<MetadataService> _logger;

    public MetadataService(IFileStorageService storage, ILogger<MetadataService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<PdfOperationResult<PdfMetadataInfo>> GetMetadataAsync(Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        try
        {
            using var fs = File.Create(tempPath);
            await file.CopyToAsync(fs, ct);
            fs.Close();

            using var doc = PdfDocument.FromFile(tempPath);
            var info = new FileInfo(tempPath);

            return PdfOperationResult<PdfMetadataInfo>.Success(new PdfMetadataInfo
            {
                Title = doc.MetaData.Title,
                Author = doc.MetaData.Author,
                Subject = doc.MetaData.Subject,
                Keywords = doc.MetaData.Keywords,
                Creator = doc.MetaData.Creator,
                Producer = doc.MetaData.Producer,
                CreatedDate = doc.MetaData.CreationDate,
                ModifiedDate = doc.MetaData.ModifiedDate,
                PageCount = doc.PageCount,
                IsEncrypted = doc.SecuritySettings?.UserPassword?.Length > 0,
                HasSignature = TryGetSignatureCount(doc) > 0,
                FileSizeBytes = info.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetMetadata failed");
            return PdfOperationResult<PdfMetadataInfo>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> SetMetadataAsync(SetMetadataRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        try
        {
            if (req.File is null)
                return PdfOperationResult<PdfFileResponse>.Failure("No file provided.");

            using var fs = File.Create(tempPath);
            await req.File.CopyToAsync(fs, ct);
            fs.Close();

            using var doc = PdfDocument.FromFile(tempPath);

            if (req.Title is not null) doc.MetaData.Title = req.Title;
            if (req.Author is not null) doc.MetaData.Author = req.Author;
            if (req.Subject is not null) doc.MetaData.Subject = req.Subject;
            if (req.Keywords is not null) doc.MetaData.Keywords = req.Keywords;
            if (req.Creator is not null) doc.MetaData.Creator = req.Creator;
            doc.MetaData.ModifiedDate = DateTime.UtcNow;

            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "metadata");
            sw.Stop();

            return PdfOperationResult<PdfFileResponse>.Success(new PdfFileResponse
            {
                FileName = fileName,
                DownloadUrl = _storage.GetDownloadUrl(fileName),
                FileSizeBytes = doc.BinaryData.Length,
                PageCount = doc.PageCount,
                ElapsedMs = sw.ElapsedMilliseconds
            }, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetMetadata failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> ConvertToPdfAAsync(ConvertToPdfARequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        var outPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}_pdfa.pdf");
        try
        {
            if (req.File is null)
                return PdfOperationResult<PdfFileResponse>.Failure("No file provided.");

            using var fs = File.Create(tempPath);
            await req.File.CopyToAsync(fs, ct);
            fs.Close();

            using var doc = PdfDocument.FromFile(tempPath);
            var version = req.Conformance.ToUpperInvariant() == "PDFA3B"
                ? PdfAVersions.PdfA3b
                : PdfAVersions.PdfA1b;

            doc.SaveAsPdfA(outPath, version);
            var bytes = await File.ReadAllBytesAsync(outPath, ct);
            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(bytes, baseName, $"pdfa-{req.Conformance.ToLower()}");
            sw.Stop();

            return PdfOperationResult<PdfFileResponse>.Success(new PdfFileResponse
            {
                FileName = fileName,
                DownloadUrl = _storage.GetDownloadUrl(fileName),
                FileSizeBytes = bytes.Length,
                PageCount = doc.PageCount,
                ElapsedMs = sw.ElapsedMilliseconds
            }, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConvertToPdfA failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            if (File.Exists(outPath)) File.Delete(outPath);
        }
    }

    private static int TryGetSignatureCount(PdfDocument doc)
    {
        try { return doc.GetVerifiedSignatures().Count; }
        catch { return 0; }
    }
}
