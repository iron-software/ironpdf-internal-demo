using IronPdfDemo.Models.Requests;
using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IronPdfDemo.Controllers;

/// <summary>Converts various formats to PDF.</summary>
[ApiController]
[Route("api/conversion")]
[Produces("application/json")]
public class ConversionController(IConversionService svc, ILogger<ConversionController> logger) : ControllerBase
{
    /// <summary>Convert HTML string to PDF.</summary>
    [HttpPost("html-to-pdf")]
    public async Task<IActionResult> HtmlToPdf([FromBody] HtmlConversionRequest req, CancellationToken ct)
    {
        var result = await svc.HtmlToPdfAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Convert a URL to PDF.</summary>
    [HttpPost("url-to-pdf")]
    public async Task<IActionResult> UrlToPdf([FromBody] UrlConversionRequest req, CancellationToken ct)
    {
        var result = await svc.UrlToPdfAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Convert an image file to PDF.</summary>
    [HttpPost("image-to-pdf")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImageToPdf(IFormFile file, [FromForm] string paperSize = "A4", CancellationToken ct = default)
    {
        if (file is null) return BadRequest(new { error = "No file provided." });
        var result = await svc.ImageToPdfAsync(file, paperSize, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Convert a DOCX file to PDF.</summary>
    [HttpPost("docx-to-pdf")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> DocxToPdf(IFormFile file, CancellationToken ct = default)
    {
        if (file is null) return BadRequest(new { error = "No file provided." });
        var result = await svc.DocxToPdfAsync(file, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Batch convert multiple files to PDF with SignalR progress reporting.</summary>
    [HttpPost("batch")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> BatchConvert(
        [FromForm] List<IFormFile> files,
        [FromForm] string jobId,
        [FromForm] string paperSize = "A4",
        CancellationToken ct = default)
    {
        if (files.Count == 0) return BadRequest(new { error = "No files provided." });
        var result = await svc.BatchConvertAsync(files, jobId, paperSize, null, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Extract a thumbnail image from a PDF page.</summary>
    [HttpPost("thumbnail")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Thumbnail(IFormFile file, [FromForm] int pageNumber = 1, [FromForm] int widthPx = 300)
    {
        if (file is null) return BadRequest(new { error = "No file provided." });
        var result = await svc.ExtractThumbnailAsync(file, pageNumber, widthPx);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }
}
