using System.Diagnostics;
using IronPdf;
using IronPdf.Rendering;
using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;
using IronPdfDemo.Services.Interfaces;

namespace IronPdfDemo.Services.Implementations;

public class FormService : IFormService
{
    private readonly IFileStorageService _storage;
    private readonly ILogger<FormService> _logger;

    public FormService(IFileStorageService storage, ILogger<FormService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<PdfOperationResult<FormFieldsResponse>> GetFormFieldsAsync(Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct = default)
    {
        var tempPath = Path.Combine(_storage.GetTempDirectory(), $"{Guid.NewGuid()}.pdf");
        try
        {
            using var fs = File.Create(tempPath);
            await file.CopyToAsync(fs, ct);
            fs.Close();

            using var doc = PdfDocument.FromFile(tempPath);
            var fields = doc.Form.Select(f => new FormFieldInfo
            {
                Name = f.Name,
                FieldType = f.GetType().Name.Replace("PdfForm", "").Replace("Field", "").ToLower(),
                CurrentValue = f.Value,
                Options = new List<string>()
            }).ToList();

            return PdfOperationResult<FormFieldsResponse>.Success(new FormFieldsResponse { Fields = fields });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetFormFields failed");
            return PdfOperationResult<FormFieldsResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> FillFormAsync(FillFormRequest req, CancellationToken ct = default)
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
            foreach (var kv in req.FieldValues)
            {
                try
                {
                    var field = doc.Form.FindFormField(kv.Key);
                    if (field is not null)
                        field.Value = kv.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not set field {Field}", kv.Key);
                }
            }

            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "filled");
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
            _logger.LogError(ex, "FillForm failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> FlattenFormAsync(FlattenFormRequest req, CancellationToken ct = default)
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
            doc.Flatten();
            var baseName = Path.GetFileNameWithoutExtension(req.File.FileName);
            var fileName = await _storage.SaveFileAsync(doc.BinaryData, baseName, "flattened");
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
            _logger.LogError(ex, "FlattenForm failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public async Task<PdfOperationResult<PdfFileResponse>> CreateFormFromHtmlAsync(string htmlContent, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var renderer = new ChromePdfRenderer();
            renderer.RenderingOptions.CreatePdfFormsFromHtml = true;
            renderer.RenderingOptions.EnableJavaScript = false;
            renderer.RenderingOptions.PaperSize = PdfPaperSize.A4;
            renderer.RenderingOptions.MarginTop = 20;
            renderer.RenderingOptions.MarginBottom = 20;
            renderer.RenderingOptions.MarginLeft = 20;
            renderer.RenderingOptions.MarginRight = 20;

            var pdf = await renderer.RenderHtmlAsPdfAsync(htmlContent);
            var fileName = await _storage.SaveFileAsync(pdf.BinaryData, "form-from-html");
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
            _logger.LogError(ex, "CreateFormFromHtml failed");
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
    }
}
