using IronPdfDemo.Models.Requests;
using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IronPdfDemo.Controllers;

/// <summary>Read and write PDF metadata; convert to PDF/A.</summary>
[ApiController]
[Route("api/metadata")]
[Produces("application/json")]
public class MetadataController(IMetadataService svc) : ControllerBase
{
    /// <summary>Get all metadata from a PDF (title, author, page count, encryption status, etc.).</summary>
    [HttpPost("get")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> GetMetadata(IFormFile file, CancellationToken ct)
    {
        if (file is null) return BadRequest(new { error = "No file provided." });
        var result = await svc.GetMetadataAsync(file, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Set standard metadata fields (title, author, subject, keywords, creator).</summary>
    [HttpPost("set")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> SetMetadata([FromForm] SetMetadataRequest req, CancellationToken ct)
    {
        var result = await svc.SetMetadataAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Convert a PDF to PDF/A archival format (PDF/A-1b or PDF/A-3b).</summary>
    [HttpPost("pdfa")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ConvertToPdfA([FromForm] ConvertToPdfARequest req, CancellationToken ct)
    {
        var result = await svc.ConvertToPdfAAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }
}
