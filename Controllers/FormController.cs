using IronPdfDemo.Models.Requests;
using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IronPdfDemo.Controllers;

/// <summary>Create, inspect, fill, and flatten PDF forms (AcroForms).</summary>
[ApiController]
[Route("api/forms")]
[Produces("application/json")]
public class FormController(IFormService svc) : ControllerBase
{
    /// <summary>Get all form fields and their current values from a PDF.</summary>
    [HttpPost("fields")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> GetFields(IFormFile file, CancellationToken ct)
    {
        if (file is null) return BadRequest(new { error = "No file provided." });
        var result = await svc.GetFormFieldsAsync(file, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Fill form fields by name-value pairs.</summary>
    [HttpPost("fill")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Fill([FromForm] FillFormRequest req, CancellationToken ct)
    {
        var result = await svc.FillFormAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Flatten all form fields (make them non-editable).</summary>
    [HttpPost("flatten")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Flatten([FromForm] FlattenFormRequest req, CancellationToken ct)
    {
        var result = await svc.FlattenFormAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Create a PDF form from HTML containing input/select/checkbox/radio elements.</summary>
    [HttpPost("create-from-html")]
    public async Task<IActionResult> CreateFromHtml([FromBody] string htmlContent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(htmlContent)) return BadRequest(new { error = "HTML content required." });
        var result = await svc.CreateFormFromHtmlAsync(htmlContent, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }
}
