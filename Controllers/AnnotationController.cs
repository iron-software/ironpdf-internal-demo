using IronPdfDemo.Models.Requests;
using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IronPdfDemo.Controllers;

/// <summary>Add headers/footers, watermarks, annotations, and bookmarks to PDFs.</summary>
[ApiController]
[Route("api/annotations")]
[Produces("application/json")]
public class AnnotationController(IAnnotationService svc) : ControllerBase
{
    /// <summary>Add configurable headers and footers to a PDF.</summary>
    [HttpPost("header-footer")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> HeaderFooter([FromForm] AddHeaderFooterRequest req, CancellationToken ct)
    {
        var result = await svc.AddHeaderFooterAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Apply a text watermark (e.g. CONFIDENTIAL, DRAFT) to a PDF.</summary>
    [HttpPost("watermark/text")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> TextWatermark([FromForm] AddTextWatermarkRequest req, CancellationToken ct)
    {
        var result = await svc.AddTextWatermarkAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Apply an image watermark to a PDF.</summary>
    [HttpPost("watermark/image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImageWatermark([FromForm] AddImageWatermarkRequest req, CancellationToken ct)
    {
        var result = await svc.AddImageWatermarkAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Add a sticky-note text annotation to a specific page.</summary>
    [HttpPost("annotate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Annotate([FromForm] AddAnnotationRequest req, CancellationToken ct)
    {
        var result = await svc.AddAnnotationAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Add navigation bookmarks/outline to a PDF.</summary>
    [HttpPost("bookmarks")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Bookmarks([FromForm] AddBookmarkRequest req, CancellationToken ct)
    {
        var result = await svc.AddBookmarksAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }
}
