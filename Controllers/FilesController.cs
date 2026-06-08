using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IronPdfDemo.Controllers;

/// <summary>Download and list generated PDF files.</summary>
[ApiController]
[Route("api/files")]
public class FilesController(IFileStorageService storage) : ControllerBase
{
    /// <summary>Download a previously generated PDF by file name.</summary>
    [HttpGet("download/{fileName}")]
    public IActionResult Download(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains(".."))
            return BadRequest(new { error = "Invalid filename." });

        var filePath = Path.Combine(storage.GetOutputDirectory(), fileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = $"File '{fileName}' not found." });

        return PhysicalFile(filePath, "application/pdf", fileName);
    }

    /// <summary>List all available output PDF files.</summary>
    [HttpGet("list")]
    public IActionResult ListFiles()
    {
        var files = storage.ListOutputFiles().Select(f => new
        {
            f.Name,
            SizeBytes = f.Length,
            CreatedUtc = f.CreationTimeUtc,
            DownloadUrl = storage.GetDownloadUrl(f.Name)
        });
        return Ok(files);
    }
}
