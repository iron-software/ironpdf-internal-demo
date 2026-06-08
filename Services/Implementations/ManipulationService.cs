using System.Diagnostics;
using IronPdf;
using IronPdf.Rendering;
using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;
using IronPdfDemo.Services.Interfaces;

namespace IronPdfDemo.Services.Implementations;

public class ManipulationService : IManipulationService
{
    private readonly IFileStorageService _storage;
    private readonly ILogger<ManipulationService> _logger;

    public ManipulationService(IFileStorageService storage, ILogger<ManipulationService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<PdfOperationResult<PdfFileResponse>> MergeAsync(MergePdfsRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var tempFiles = new List<string>();
        try
        {
            if (req.Files.Count < 2)
                return PdfOperationResult<PdfFileResponse>.Failure("At least 2 PDFs required for merge.", "INSUFFICIENT_FILES");

            var pdfs = new List<PdfDocument>();
            foreach (var file in req.Files)
            {
                var tmp = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
                tempFiles.Add(tmp);
                using var fs = File.Create(tmp);
                await file.CopyToAsync(fs, ct);
                pdfs.Add(PdfDocument.FromFile(tmp));
            }

            var merged = PdfDocument.Merge(pdfs);

            if (req.AddPageNumbers)
            {
                merged.AddTextFooters(new IronPdf.TextHeaderFooter
                {
                    CenterText = "Page {page} of {total-pages}",
                    FontSize = 8,
                    DrawDividerLine = true
                });
            }

            var fileName = await _storage.SaveFileAsync(merged.BinaryData, "merged");
            sw.Stop();
            foreach (var p in pdfs) p.Dispose();

            return PdfOperationResult<PdfFileResponse>.Success(new PdfFileResponse
            {
                FileName = fileName,
                DownloadUrl = _storage.GetDownloadUrl(fileName),
                FileSizeBytes = merged.BinaryData.Length,
                PageCount = merged.PageCount,
                ElapsedMs = sw.ElapsedMilliseconds
            }, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merge failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            foreach (var t in tempFiles) if (File.Exists(t)) File.Delete(t);
        }
    }

    public async Task<PdfOperationResult<List<PdfFileResponse>>> SplitAsync(SplitPdfRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        try
        {
            if (req.File is null)
                return PdfOperationResult<List<PdfFileResponse>>.Failure("No file provided.");

            using var fs = File.Create(tempPath);
            await req.File.CopyToAsync(fs, ct);
            fs.Close();

            using var doc = PdfDocument.FromFile(tempPath);
            if (req.SplitAtPage < 1 || req.SplitAtPage >= doc.PageCount)
                return PdfOperationResult<List<PdfFileResponse>>.Failure($"SplitAtPage must be between 1 and {doc.PageCount - 1}.");

            var part1 = doc.CopyPages(0, req.SplitAtPage - 1);
            var part2 = doc.CopyPages(req.SplitAtPage, doc.PageCount - 1);
            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);

            var file1 = await _storage.SaveFileAsync(part1.BinaryData, baseName, "part1");
            var file2 = await _storage.SaveFileAsync(part2.BinaryData, baseName, "part2");
            sw.Stop();

            return PdfOperationResult<List<PdfFileResponse>>.Success(new List<PdfFileResponse>
            {
                new() { FileName = file1, DownloadUrl = _storage.GetDownloadUrl(file1), FileSizeBytes = part1.BinaryData.Length, PageCount = part1.PageCount, ElapsedMs = sw.ElapsedMilliseconds },
                new() { FileName = file2, DownloadUrl = _storage.GetDownloadUrl(file2), FileSizeBytes = part2.BinaryData.Length, PageCount = part2.PageCount, ElapsedMs = sw.ElapsedMilliseconds }
            }, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Split failed");
            return PdfOperationResult<List<PdfFileResponse>>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> RotatePagesAsync(RotatePagesRequest req, CancellationToken ct = default)
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
            var rotation = req.RotateDeg switch
            {
                90 => PdfPageRotation.Clockwise90,
                180 => PdfPageRotation.Clockwise180,
                270 => PdfPageRotation.Clockwise270,
                _ => PdfPageRotation.Clockwise90
            };

            var pagesToRotate = req.PageNumbers.Count > 0
                ? req.PageNumbers.Select(p => p - 1).Where(i => i >= 0 && i < doc.PageCount)
                : Enumerable.Range(0, doc.PageCount);

            foreach (var idx in pagesToRotate)
                doc.SetPageRotation(idx, rotation);

            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "rotated");
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
            _logger.LogError(ex, "Rotate failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> CompressAsync(CompressPdfRequest req, CancellationToken ct = default)
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
            var originalSize = doc.BinaryData.Length;

            doc.CompressImages(req.ImageQuality);
            doc.CompressStructTree();

            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "compressed");
            sw.Stop();

            _logger.LogInformation("Compressed {Original} bytes → {Compressed} bytes ({Pct:F1}% reduction)",
                originalSize, doc.BinaryData.Length, (1.0 - (double)doc.BinaryData.Length / originalSize) * 100);

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
            _logger.LogError(ex, "Compress failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> RedactAsync(RedactPdfRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        try
        {
            if (req.File is null)
                return PdfOperationResult<PdfFileResponse>.Failure("No file provided.");
            if (req.TextToRedact.Count == 0)
                return PdfOperationResult<PdfFileResponse>.Failure("No redaction terms provided.");

            using var fs = File.Create(tempPath);
            await req.File.CopyToAsync(fs, ct);
            fs.Close();

            using var doc = PdfDocument.FromFile(tempPath);
            foreach (var term in req.TextToRedact.Where(t => !string.IsNullOrWhiteSpace(t)))
                doc.RedactTextOnAllPages(term, req.CaseSensitive);

            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "redacted");
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
            _logger.LogError(ex, "Redact failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<List<PageInfo>>> GetPageInfoAsync(Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        try
        {
            using var fs = File.Create(tempPath);
            await file.CopyToAsync(fs, ct);
            fs.Close();

            using var doc = PdfDocument.FromFile(tempPath);
            var pages = new List<PageInfo>();
            for (int i = 0; i < doc.PageCount; i++)
            {
                var page = doc.Pages[i];
                pages.Add(new PageInfo
                {
                    PageNumber = i + 1,
                    WidthMm = page.Width,
                    HeightMm = page.Height,
                    RotationDeg = (int)page.PageRotation * 90
                });
            }
            return PdfOperationResult<List<PageInfo>>.Success(pages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPageInfo failed");
            return PdfOperationResult<List<PageInfo>>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
