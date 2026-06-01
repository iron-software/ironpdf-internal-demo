using System.Diagnostics;
using IronPdf;
using IronPdf.Rendering;
using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;
using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace IronPdfDemo.Services.Implementations;

public class ConversionService : IConversionService
{
    private readonly IFileStorageService _storage;
    private readonly ILogger<ConversionService> _logger;

    public ConversionService(IFileStorageService storage, ILogger<ConversionService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<PdfOperationResult<PdfFileResponse>> HtmlToPdfAsync(HtmlConversionRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var renderer = BuildRenderer(req.PaperSize, req.Orientation, req.MarginTopMm, req.MarginBottomMm,
                req.MarginLeftMm, req.MarginRightMm, req.EnableJs, req.CssMediaType, req.JsWaitMs);

            var html = req.HtmlContent;
            if (!html.TrimStart().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) &&
                !html.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                html = $"<!DOCTYPE html><html><head><meta charset='utf-8'/></head><body>{html}</body></html>";
            }

            var pdf = await renderer.RenderHtmlAsPdfAsync(html);
            var baseName = req.OutputFileName ?? "html-to-pdf";
            var fileName = await _storage.SaveFileAsync(pdf.BinaryData, baseName);
            sw.Stop();

            return PdfOperationResult<PdfFileResponse>.Success(new PdfFileResponse
            {
                FileName = fileName,
                DownloadUrl = _storage.GetDownloadUrl(fileName),
                FileSizeBytes = pdf.BinaryData.Length,
                PageCount = pdf.PageCount,
                ElapsedMs = sw.ElapsedMilliseconds
            }, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HtmlToPdf failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> UrlToPdfAsync(UrlConversionRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            if (!Uri.TryCreate(req.Url, UriKind.Absolute, out _))
                return PdfOperationResult<PdfFileResponse>.Failure("Invalid URL", "INVALID_URL");

            var renderer = BuildRenderer(req.PaperSize, req.Orientation, enableJs: req.EnableJs, jsWaitMs: req.JsWaitMs);
            var pdf = await renderer.RenderUrlAsPdfAsync(req.Url);
            var baseName = req.OutputFileName ?? "url-to-pdf";
            var fileName = await _storage.SaveFileAsync(pdf.BinaryData, baseName);
            sw.Stop();

            return PdfOperationResult<PdfFileResponse>.Success(new PdfFileResponse
            {
                FileName = fileName,
                DownloadUrl = _storage.GetDownloadUrl(fileName),
                FileSizeBytes = pdf.BinaryData.Length,
                PageCount = pdf.PageCount,
                ElapsedMs = sw.ElapsedMilliseconds
            }, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UrlToPdf failed for {Url}", req.Url);
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> ImageToPdfAsync(IFormFile file, string paperSize = "A4", CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var imageBytes = ms.ToArray();

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var mime = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
            var base64 = Convert.ToBase64String(imageBytes);
            var html = $"<!DOCTYPE html><html><head><meta charset='utf-8'/><style>body{{margin:0;padding:0;}}img{{max-width:100%;height:auto;display:block;}}</style></head><body><img src='data:{mime};base64,{base64}'/></body></html>";

            var renderer = BuildRenderer(paperSize);
            var pdf = await renderer.RenderHtmlAsPdfAsync(html);
            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var fileName = await _storage.SaveFileAsync(pdf.BinaryData, baseName, "image");
            sw.Stop();

            return PdfOperationResult<PdfFileResponse>.Success(new PdfFileResponse
            {
                FileName = fileName,
                DownloadUrl = _storage.GetDownloadUrl(fileName),
                FileSizeBytes = pdf.BinaryData.Length,
                PageCount = pdf.PageCount,
                ElapsedMs = sw.ElapsedMilliseconds
            }, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ImageToPdf failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> DocxToPdfAsync(IFormFile file, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.docx");
        try
        {
            using (var fs = File.Create(tempPath))
                await file.CopyToAsync(fs, ct);

            var renderer = new DocxToPdfRenderer();
            var pdf = await renderer.RenderDocxAsPdfAsync(tempPath);
            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var fileName = await _storage.SaveFileAsync(pdf.BinaryData, baseName, "docx");
            sw.Stop();

            return PdfOperationResult<PdfFileResponse>.Success(new PdfFileResponse
            {
                FileName = fileName,
                DownloadUrl = _storage.GetDownloadUrl(fileName),
                FileSizeBytes = pdf.BinaryData.Length,
                PageCount = pdf.PageCount,
                ElapsedMs = sw.ElapsedMilliseconds
            }, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocxToPdf failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<BatchResultResponse>> BatchConvertAsync(
        List<IFormFile> files, string jobId, string paperSize = "A4",
        IProgress<BatchProgressUpdate>? progress = null, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var results = new List<BatchFileResult>();
        var update = new BatchProgressUpdate { JobId = jobId, TotalItems = files.Count, Status = "processing" };

        foreach (var file in files)
        {
            if (ct.IsCancellationRequested) break;
            update.CurrentItem = file.FileName;
            progress?.Report(update);

            var itemSw = Stopwatch.StartNew();
            try
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                PdfOperationResult<PdfFileResponse>? result = null;

                if (ext == ".pdf")
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms, ct);
                    var baseName = Path.GetFileNameWithoutExtension(file.FileName);
                    var fileName = await _storage.SaveFileAsync(ms.ToArray(), baseName, "copy");
                    result = PdfOperationResult<PdfFileResponse>.Success(new PdfFileResponse
                    {
                        FileName = fileName,
                        DownloadUrl = _storage.GetDownloadUrl(fileName),
                        FileSizeBytes = ms.Length
                    });
                }
                else if (new[] { ".html", ".htm" }.Contains(ext))
                {
                    using var sr = new StreamReader(file.OpenReadStream());
                    var html = await sr.ReadToEndAsync(ct);
                    result = await HtmlToPdfAsync(new HtmlConversionRequest { HtmlContent = html, PaperSize = paperSize }, ct);
                }
                else if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }.Contains(ext))
                {
                    result = await ImageToPdfAsync(file, paperSize, ct);
                }
                else if (ext == ".docx")
                {
                    result = await DocxToPdfAsync(file, ct);
                }

                itemSw.Stop();
                if (result?.IsSuccess == true)
                {
                    update.Processed++;
                    results.Add(new BatchFileResult
                    {
                        OriginalFileName = file.FileName,
                        Success = true,
                        DownloadUrl = result.Data!.DownloadUrl,
                        ElapsedMs = itemSw.ElapsedMilliseconds
                    });
                    update.Results.Add(new BatchItemResult { FileName = file.FileName, Success = true, DownloadUrl = result.Data!.DownloadUrl });
                }
                else
                {
                    update.Failed++;
                    results.Add(new BatchFileResult { OriginalFileName = file.FileName, Success = false, Error = result?.Error ?? "Unsupported format", ElapsedMs = itemSw.ElapsedMilliseconds });
                    update.Results.Add(new BatchItemResult { FileName = file.FileName, Success = false, Error = result?.Error ?? "Unsupported format" });
                }
            }
            catch (Exception ex)
            {
                itemSw.Stop();
                update.Failed++;
                _logger.LogError(ex, "Batch item failed: {File}", file.FileName);
                results.Add(new BatchFileResult { OriginalFileName = file.FileName, Success = false, Error = ex.Message, ElapsedMs = itemSw.ElapsedMilliseconds });
                update.Results.Add(new BatchItemResult { FileName = file.FileName, Success = false, Error = ex.Message });
            }
            progress?.Report(update);
        }

        update.Status = "completed";
        progress?.Report(update);
        sw.Stop();

        return PdfOperationResult<BatchResultResponse>.Success(new BatchResultResponse
        {
            JobId = jobId,
            TotalFiles = files.Count,
            Succeeded = update.Processed,
            Failed = update.Failed,
            Results = results,
            TotalElapsedMs = sw.ElapsedMilliseconds
        }, sw.ElapsedMilliseconds);
    }

    public async Task<PdfOperationResult<ThumbnailResponse>> ExtractThumbnailAsync(IFormFile file, int pageNumber = 1, int widthPx = 300)
    {
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        try
        {
            using (var fs = File.Create(tempPath))
                await file.CopyToAsync(fs);

            using var doc = PdfDocument.FromFile(tempPath);
            if (pageNumber < 1 || pageNumber > doc.PageCount)
                return PdfOperationResult<ThumbnailResponse>.Failure($"Page {pageNumber} does not exist. Document has {doc.PageCount} pages.", "INVALID_PAGE");

            var bitmaps = doc.ToBitmap(150);
            using var bmp = bitmaps[pageNumber - 1];
            var pngBytes = bmp.ExportBytes();
            var base64 = Convert.ToBase64String(pngBytes);

            return PdfOperationResult<ThumbnailResponse>.Success(new ThumbnailResponse
            {
                Base64Image = $"data:image/png;base64,{base64}",
                PageNumber = pageNumber,
                WidthPx = bmp.Width,
                HeightPx = bmp.Height
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thumbnail extraction failed");
            return PdfOperationResult<ThumbnailResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static ChromePdfRenderer BuildRenderer(
        string paperSize = "A4",
        string orientation = "Portrait",
        int marginTop = 15, int marginBottom = 15,
        int marginLeft = 15, int marginRight = 15,
        bool enableJs = false,
        string cssMediaType = "Print",
        int jsWaitMs = 0)
    {
        var renderer = new ChromePdfRenderer();
        renderer.RenderingOptions.PaperSize = paperSize.ToUpperInvariant() switch
        {
            "LETTER" => PdfPaperSize.Letter,
            "LEGAL" => PdfPaperSize.Legal,
            "A3" => PdfPaperSize.A3,
            "A5" => PdfPaperSize.A5,
            _ => PdfPaperSize.A4
        };
        renderer.RenderingOptions.PaperOrientation = orientation.ToUpperInvariant() == "LANDSCAPE"
            ? PdfPaperOrientation.Landscape
            : PdfPaperOrientation.Portrait;
        renderer.RenderingOptions.MarginTop = marginTop;
        renderer.RenderingOptions.MarginBottom = marginBottom;
        renderer.RenderingOptions.MarginLeft = marginLeft;
        renderer.RenderingOptions.MarginRight = marginRight;
        renderer.RenderingOptions.EnableJavaScript = enableJs;
        renderer.RenderingOptions.CssMediaType = cssMediaType.ToUpperInvariant() == "SCREEN"
            ? PdfCssMediaType.Screen
            : PdfCssMediaType.Print;
        renderer.RenderingOptions.Timeout = 60;
        if (enableJs && jsWaitMs > 0)
            renderer.RenderingOptions.WaitFor.JavaScript(jsWaitMs);
        return renderer;
    }
}
