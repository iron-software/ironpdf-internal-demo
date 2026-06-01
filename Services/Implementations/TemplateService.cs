using System.Diagnostics;
using IronPdf;
using IronPdf.Rendering;
using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;
using IronPdfDemo.Services.Interfaces;

namespace IronPdfDemo.Services.Implementations;

public class TemplateService : ITemplateService
{
    private readonly IFileStorageService _storage;
    private readonly IAnnotationService _annotation;
    private readonly ISecurityService _security;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<TemplateService> _logger;

    private static readonly string[] AvailableTemplates =
    [
        "Invoice", "Report", "Dashboard", "Certificate", "Resume", "Brochure", "FormTemplate"
    ];

    public TemplateService(
        IFileStorageService storage,
        IAnnotationService annotation,
        ISecurityService security,
        IWebHostEnvironment env,
        ILogger<TemplateService> logger)
    {
        _storage = storage;
        _annotation = annotation;
        _security = security;
        _env = env;
        _logger = logger;
    }

    public IReadOnlyList<string> GetAvailableTemplates() => AvailableTemplates;

    public async Task<PdfOperationResult<PdfFileResponse>> GenerateFromTemplateAsync(GenerateTemplateRequest req, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var templatePath = Path.Combine(_env.ContentRootPath, "Templates", $"{req.TemplateName}.html");
            if (!File.Exists(templatePath))
                return PdfOperationResult<PdfFileResponse>.Failure($"Template '{req.TemplateName}' not found.", "TEMPLATE_NOT_FOUND");

            var html = await File.ReadAllTextAsync(templatePath, ct);
            foreach (var kv in req.Variables)
                html = html.Replace($"{{{{{kv.Key}}}}}", kv.Value);

            var renderer = BuildRenderer(req);
            var pdf = await renderer.RenderHtmlAsPdfAsync(html);

            if (req.AddHeaderFooter)
            {
                pdf.AddHtmlHeaders(new HtmlHeaderFooter
                {
                    HtmlFragment = $"<div style='font-family:Arial;font-size:9pt;text-align:center;'>{req.TemplateName} — Generated {DateTime.Now:yyyy-MM-dd}</div>",
                    MaxHeight = 12,
                    DrawDividerLine = true
                });
                pdf.AddHtmlFooters(new HtmlHeaderFooter
                {
                    HtmlFragment = "<div style='font-family:Arial;font-size:8pt;text-align:center;color:#777;'>Page {page} of {total-pages} &nbsp;|&nbsp; IronPDF Demo</div>",
                    MaxHeight = 10,
                    DrawDividerLine = true
                });
            }

            if (req.AddWatermark && !string.IsNullOrWhiteSpace(req.WatermarkText))
            {
                var wm = $"<h1 style='color:#C0C0C0;font-size:80pt;font-family:Arial;font-weight:bold;'>{req.WatermarkText}</h1>";
                pdf.ApplyWatermark(wm, -45, 20, IronPdf.Editing.VerticalAlignment.Middle, IronPdf.Editing.HorizontalAlignment.Center);
            }

            pdf.MetaData.Title = $"{req.TemplateName} — IronPDF Demo";
            pdf.MetaData.Author = "IronPDF Demo App";
            pdf.MetaData.Creator = "IronPDF 2026.5.2";
            pdf.MetaData.Subject = $"Generated from {req.TemplateName} template";
            pdf.MetaData.CreationDate = DateTime.UtcNow;

            if (req.PasswordProtect && !string.IsNullOrWhiteSpace(req.Password))
            {
                pdf.SecuritySettings.UserPassword = req.Password;
                pdf.SecuritySettings.AllowUserPrinting = IronPdf.Security.PdfPrintSecurity.FullPrintRights;
            }

            var fileName = await _storage.SaveFileAsync(pdf.BinaryData, req.TemplateName.ToLower());
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
            _logger.LogError(ex, "GenerateFromTemplate failed for {Template}", req.TemplateName);
            return PdfOperationResult<PdfFileResponse>.Failure(ex.Message);
        }
    }

    public async Task<PdfOperationResult<ThumbnailResponse>> GetTemplatePreviewAsync(string templateName)
    {
        try
        {
            var templatePath = Path.Combine(_env.ContentRootPath, "Templates", $"{templateName}.html");
            if (!File.Exists(templatePath))
                return PdfOperationResult<ThumbnailResponse>.Failure($"Template '{templateName}' not found.");

            var html = await File.ReadAllTextAsync(templatePath);
            var renderer = new ChromePdfRenderer();
            renderer.RenderingOptions.PaperSize = PdfPaperSize.A4;
            renderer.RenderingOptions.MarginTop = 10;
            renderer.RenderingOptions.MarginBottom = 10;
            renderer.RenderingOptions.MarginLeft = 10;
            renderer.RenderingOptions.MarginRight = 10;

            if (templateName.Equals("Dashboard", StringComparison.OrdinalIgnoreCase))
            {
                renderer.RenderingOptions.EnableJavaScript = true;
                renderer.RenderingOptions.WaitFor.JavaScript(800);
            }

            var pdf = await renderer.RenderHtmlAsPdfAsync(html);
            var bitmaps = pdf.ToBitmap(96);
            using var bmp = bitmaps[0];
            var pngBytes = bmp.ExportBytes();

            return PdfOperationResult<ThumbnailResponse>.Success(new ThumbnailResponse
            {
                Base64Image = $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}",
                PageNumber = 1,
                WidthPx = bmp.Width,
                HeightPx = bmp.Height
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTemplatePreview failed for {Template}", templateName);
            return PdfOperationResult<ThumbnailResponse>.Failure(ex.Message);
        }
    }

    private static ChromePdfRenderer BuildRenderer(GenerateTemplateRequest req)
    {
        var renderer = new ChromePdfRenderer();
        renderer.RenderingOptions.PaperSize = req.PaperSize.ToUpperInvariant() switch
        {
            "LETTER" => PdfPaperSize.Letter,
            "A3" => PdfPaperSize.A3,
            _ => PdfPaperSize.A4
        };
        renderer.RenderingOptions.PaperOrientation = req.Orientation.ToUpperInvariant() == "LANDSCAPE"
            ? PdfPaperOrientation.Landscape
            : PdfPaperOrientation.Portrait;
        renderer.RenderingOptions.MarginTop = 15;
        renderer.RenderingOptions.MarginBottom = 15;
        renderer.RenderingOptions.MarginLeft = 15;
        renderer.RenderingOptions.MarginRight = 15;
        renderer.RenderingOptions.EnableJavaScript = req.EnableJs;
        renderer.RenderingOptions.CssMediaType = PdfCssMediaType.Screen;
        renderer.RenderingOptions.Timeout = 60;
        if (req.EnableJs)
            renderer.RenderingOptions.WaitFor.JavaScript(1000);
        renderer.RenderingOptions.CreatePdfFormsFromHtml =
            req.TemplateName.Equals("FormTemplate", StringComparison.OrdinalIgnoreCase);
        return renderer;
    }
}
