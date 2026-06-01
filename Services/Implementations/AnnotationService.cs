using System.Diagnostics;
using IronPdf;
using IronPdf.Editing;
using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;
using IronPdfDemo.Services.Interfaces;

namespace IronPdfDemo.Services.Implementations;

public class AnnotationService : IAnnotationService
{
    private readonly IFileStorageService _storage;
    private readonly ILogger<AnnotationService> _logger;

    public AnnotationService(IFileStorageService storage, ILogger<AnnotationService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<PdfOperationResult<PdfFileResponse>> AddHeaderFooterAsync(AddHeaderFooterRequest req, CancellationToken ct = default)
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

            var headerParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(req.HeaderLeft)) headerParts.Add($"<span style='float:left'>{req.HeaderLeft}</span>");
            if (!string.IsNullOrWhiteSpace(req.HeaderCenter)) headerParts.Add($"<span style='text-align:center;display:block'>{req.HeaderCenter}</span>");
            if (req.HeaderShowPageNum) headerParts.Add("<span style='float:right'>Page {page} of {total-pages}</span>");
            if (req.HeaderShowDate) headerParts.Add($"<span style='float:right'>{DateTime.Now:yyyy-MM-dd}</span>");

            if (headerParts.Count > 0)
            {
                doc.AddHtmlHeaders(new HtmlHeaderFooter
                {
                    HtmlFragment = $"<div style='width:100%;font-family:Arial,sans-serif;font-size:9pt;'>{string.Join("", headerParts)}</div>",
                    MaxHeight = req.HeaderHeightMm,
                    DrawDividerLine = true
                });
            }

            var footerParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(req.FooterLeft)) footerParts.Add($"<span style='float:left'>{req.FooterLeft}</span>");
            if (!string.IsNullOrWhiteSpace(req.FooterCenter)) footerParts.Add($"<span style='text-align:center;display:block'>{req.FooterCenter}</span>");
            if (req.FooterShowPageNum) footerParts.Add("<span style='float:right'>Page {page} of {total-pages}</span>");

            if (footerParts.Count > 0)
            {
                doc.AddHtmlFooters(new HtmlHeaderFooter
                {
                    HtmlFragment = $"<div style='width:100%;font-family:Arial,sans-serif;font-size:8pt;color:#555;'>{string.Join("", footerParts)}</div>",
                    MaxHeight = req.FooterHeightMm,
                    DrawDividerLine = true
                });
            }

            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "headerfooter");
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
            _logger.LogError(ex, "AddHeaderFooter failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> AddTextWatermarkAsync(AddTextWatermarkRequest req, CancellationToken ct = default)
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
            var opacityPct = (int)(req.Opacity * 100);
            var watermarkHtml = $"<h1 style='color:{req.Color};font-size:{req.FontSizePt}pt;font-family:Arial,sans-serif;font-weight:bold;letter-spacing:4px;'>{req.Text}</h1>";

            doc.ApplyWatermark(watermarkHtml, (int)req.Rotation, opacityPct,
                VerticalAlignment.Middle, HorizontalAlignment.Center);

            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "watermarked");
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
            _logger.LogError(ex, "AddTextWatermark failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> AddImageWatermarkAsync(AddImageWatermarkRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var tempPdf = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        var tempImg = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}_wm");
        try
        {
            if (req.PdfFile is null || req.ImageFile is null)
                return PdfOperationResult<PdfFileResponse>.Failure("Both PDF and image files are required.");

            using (var fs = File.Create(tempPdf)) await req.PdfFile.CopyToAsync(fs, ct);
            var ext = Path.GetExtension(req.ImageFile.FileName);
            tempImg += ext;
            using (var fs = File.Create(tempImg)) await req.ImageFile.CopyToAsync(fs, ct);

            using var doc = PdfDocument.FromFile(tempPdf);
            using var imgMs = new MemoryStream(await File.ReadAllBytesAsync(tempImg, ct));
            var mime = ext.ToLowerInvariant() switch { ".png" => "image/png", ".gif" => "image/gif", _ => "image/jpeg" };
            var base64 = Convert.ToBase64String(imgMs.ToArray());
            var watermarkHtml = $"<img src='data:{mime};base64,{base64}' style='opacity:{req.Opacity};max-width:300px;'/>";
            doc.ApplyWatermark(watermarkHtml, (int)req.Rotation, (int)(req.Opacity * 100),
                IronPdf.Editing.VerticalAlignment.Middle, IronPdf.Editing.HorizontalAlignment.Center);

            var baseName = Path.GetFileNameWithoutExtension(req.PdfFile.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "img-watermarked");
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
            _logger.LogError(ex, "AddImageWatermark failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPdf)) File.Delete(tempPdf);
            if (File.Exists(tempImg)) File.Delete(tempImg);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> AddAnnotationAsync(AddAnnotationRequest req, CancellationToken ct = default)
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
            var pageIndex = Math.Max(0, req.PageNumber - 1);
            if (pageIndex >= doc.PageCount)
                return PdfOperationResult<PdfFileResponse>.Failure($"Page {req.PageNumber} does not exist.");

            var annotation = new IronPdf.Annotations.TextAnnotation(pageIndex)
            {
                Title = req.Author,
                Subject = "Annotation",
                Contents = req.Text,
                X = (int)req.X,
                Y = (int)req.Y,
                Width = 200,
                Height = 100,
                Opacity = 100,
                ReadOnly = false,
                Printable = true
            };
            doc.Annotations.Add(annotation);

            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "annotated");
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
            _logger.LogError(ex, "AddAnnotation failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> AddBookmarksAsync(AddBookmarkRequest req, CancellationToken ct = default)
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
            var topLevelBookmarks = new List<IronPdf.Bookmarks.IPdfBookmark>();

            foreach (var bm in req.Bookmarks)
            {
                var pageIndex = Math.Max(0, bm.PageNumber - 1);
                if (bm.ParentIndex < 0 || bm.ParentIndex >= topLevelBookmarks.Count)
                {
                    var bookmark = doc.Bookmarks.AddBookMarkAtEnd(bm.Text, pageIndex);
                    topLevelBookmarks.Add(bookmark);
                }
                else
                {
                    topLevelBookmarks[bm.ParentIndex].Children.AddBookMarkAtEnd(bm.Text, pageIndex);
                }
            }

            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "bookmarked");
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
            _logger.LogError(ex, "AddBookmarks failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
