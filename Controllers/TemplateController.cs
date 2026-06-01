using IronPdfDemo.Models.Requests;
using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IronPdfDemo.Controllers;

/// <summary>Generate PDFs from built-in templates (Invoice, Report, Dashboard, Certificate, Resume, Brochure, Form).</summary>
[ApiController]
[Route("api/templates")]
[Produces("application/json")]
public class TemplateController(ITemplateService svc) : ControllerBase
{
    /// <summary>List all available template names.</summary>
    [HttpGet]
    public IActionResult List() => Ok(svc.GetAvailableTemplates());

    /// <summary>Generate a PDF from a named template with optional variable substitution and post-processing.</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateTemplateRequest req, CancellationToken ct)
    {
        var result = await svc.GenerateFromTemplateAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Get a thumbnail preview image of the first page of a template.</summary>
    [HttpGet("preview/{templateName}")]
    public async Task<IActionResult> Preview(string templateName)
    {
        var result = await svc.GetTemplatePreviewAsync(templateName);
        return result.IsSuccess ? Ok(result.Data) : NotFound(new { error = result.Error });
    }
}
