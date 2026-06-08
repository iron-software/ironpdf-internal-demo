using IronPdfDemo.Models.Requests;
using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IronPdfDemo.Controllers;

/// <summary>Merge, split, rotate, compress, redact, and inspect PDFs.</summary>
[ApiController]
[Route("api/manipulation")]
[Produces("application/json")]
public class ManipulationController(IManipulationService svc) : ControllerBase
{
    /// <summary>Merge two or more PDFs into one.</summary>
    [HttpPost("merge")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Merge([FromForm] MergePdfsRequest req, CancellationToken ct)
    {
        var result = await svc.MergeAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Split a PDF at the specified page number.</summary>
    [HttpPost("split")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Split([FromForm] SplitPdfRequest req, CancellationToken ct)
    {
        var result = await svc.SplitAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Rotate pages in a PDF by 90, 180 or 270 degrees.</summary>
    [HttpPost("rotate")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Rotate([FromForm] RotatePagesRequest req, CancellationToken ct)
    {
        var result = await svc.RotatePagesAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Compress a PDF by reducing image quality and structure tree.</summary>
    [HttpPost("compress")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Compress([FromForm] CompressPdfRequest req, CancellationToken ct)
    {
        var result = await svc.CompressAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Redact text patterns from all pages of a PDF.</summary>
    [HttpPost("redact")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Redact([FromForm] RedactPdfRequest req, CancellationToken ct)
    {
        var result = await svc.RedactAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Get page dimensions and rotation information for each page.</summary>
    [HttpPost("page-info")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> PageInfo(IFormFile file, CancellationToken ct)
    {
        if (file is null) return BadRequest(new { error = "No file provided." });
        var result = await svc.GetPageInfoAsync(file, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }
}
