using Microsoft.AspNetCore.Mvc;
using IronPdfConverter.Models;
using IronPdfConverter.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;

namespace IronPdfConverter.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfConverterController : ControllerBase
    {
        private readonly IPdfConversionService _pdfService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<PdfConverterController> _logger;

        public PdfConverterController(
            IPdfConversionService pdfService,
            IFileStorageService fileStorageService,
            ILogger<PdfConverterController> logger)
        {
            _pdfService = pdfService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [HttpPost("convert-html")]
        public async Task<IActionResult> ConvertHtmlToPdf([FromBody] HtmlConversionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.HtmlContent))
                    return BadRequest("HTML content is required");

                var resultPath = await _pdfService.ConvertHtmlToPdfAsync(request.HtmlContent);

                // Apply redaction if requested
                if (request.TextToRedact != null && request.TextToRedact.Any())
                {
                    var pdfBytes = await System.IO.File.ReadAllBytesAsync(resultPath);
                    var redactedBytes = await _pdfService.RedactPdfAsync(
                        pdfBytes,
                        request.TextToRedact,
                        request.RedactPartialWords
                    );

                    // Save redacted file
                    var redactedPath = resultPath.Replace(".pdf", "_redacted.pdf");
                    await System.IO.File.WriteAllBytesAsync(redactedPath, redactedBytes);
                    resultPath = redactedPath;
                }

                var fileName = Path.GetFileName(resultPath);
                var fileBytes = await System.IO.File.ReadAllBytesAsync(resultPath);

                // CORRECT USAGE: File() returns a FileContentResult
                return base.File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertHtmlToPdf");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("convert-url")]
        public async Task<IActionResult> ConvertUrlToPdf([FromBody] HtmlConversionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.HtmlContent)) // Using HtmlContent as URL
                    return BadRequest("URL is required");

                var resultPath = await _pdfService.ConvertUrlToPdfAsync(request.HtmlContent);

                // Apply redaction if requested
                if (request.TextToRedact != null && request.TextToRedact.Any())
                {
                    var pdfBytes = await System.IO.File.ReadAllBytesAsync(resultPath);
                    var redactedBytes = await _pdfService.RedactPdfAsync(
                        pdfBytes,
                        request.TextToRedact,
                        request.RedactPartialWords
                    );

                    var redactedPath = resultPath.Replace(".pdf", "_redacted.pdf");
                    await System.IO.File.WriteAllBytesAsync(redactedPath, redactedBytes);
                    resultPath = redactedPath;
                }

                var fileName = Path.GetFileName(resultPath);
                var fileBytes = await System.IO.File.ReadAllBytesAsync(resultPath);

                return base.File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertUrlToPdf");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("convert-image")]
        public async Task<IActionResult> ConvertImageToPdf([FromForm] FileConversionRequest request)
        {
            try
            {
                if (request.File == null)
                    return BadRequest("File is required");

                using var memoryStream = new MemoryStream();
                await request.File.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var resultPath = await _pdfService.ConvertImageToPdfAsync(fileBytes, request.File.FileName);

                // Apply redaction if requested
                if (request.TextToRedact != null && request.TextToRedact.Any())
                {
                    var pdfBytes = await System.IO.File.ReadAllBytesAsync(resultPath);
                    var redactedBytes = await _pdfService.RedactPdfAsync(
                        pdfBytes,
                        request.TextToRedact,
                        request.RedactPartialWords
                    );

                    var redactedPath = resultPath.Replace(".pdf", "_redacted.pdf");
                    await System.IO.File.WriteAllBytesAsync(redactedPath, redactedBytes);
                    resultPath = redactedPath;
                }

                var fileName = Path.GetFileName(resultPath);
                var outputBytes = await System.IO.File.ReadAllBytesAsync(resultPath);

                return base.File(outputBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertImageToPdf");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("convert-docx")]
        public async Task<IActionResult> ConvertDocxToPdf([FromForm] FileConversionRequest request)
        {
            try
            {
                if (request.File == null)
                    return BadRequest("File is required");

                using var memoryStream = new MemoryStream();
                await request.File.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var resultPath = await _pdfService.ConvertDocxToPdfAsync(fileBytes, request.File.FileName);

                // Apply redaction if requested
                if (request.TextToRedact != null && request.TextToRedact.Any())
                {
                    var pdfBytes = await System.IO.File.ReadAllBytesAsync(resultPath);
                    var redactedBytes = await _pdfService.RedactPdfAsync(
                        pdfBytes,
                        request.TextToRedact,
                        request.RedactPartialWords
                    );

                    var redactedPath = resultPath.Replace(".pdf", "_redacted.pdf");
                    await System.IO.File.WriteAllBytesAsync(redactedPath, redactedBytes);
                    resultPath = redactedPath;
                }

                var fileName = Path.GetFileName(resultPath);
                var outputBytes = await System.IO.File.ReadAllBytesAsync(resultPath);

                return base.File(outputBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertDocxToPdf");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("convert-files")]
        public async Task<IActionResult> ConvertFiles([FromForm] ConversionRequest request)
        {
            try
            {
                if (request.Files == null || !request.Files.Any())
                    return BadRequest("No files provided");

                var results = await _pdfService.ProcessMultipleFilesAsync(request.Files.ToList());

                var successfulResults = results.Where(r => r.Status == FileConversionStatus.Success).ToList();
                var failedResults = results.Where(r => r.Status == FileConversionStatus.Failed || r.Status == FileConversionStatus.Skipped).ToList();

                // Create a ZIP file with all successful conversions
                if (successfulResults.Any())
                {
                    using var memoryStream = new MemoryStream();
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var result in successfulResults)
                        {
                            if (!string.IsNullOrEmpty(result.OutputPath) && System.IO.File.Exists(result.OutputPath))
                            {
                                var entryName = Path.GetFileName(result.OutputPath);
                                var entry = archive.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Optimal);

                                using (var entryStream = entry.Open())
                                using (var fileStream = System.IO.File.OpenRead(result.OutputPath))
                                {
                                    await fileStream.CopyToAsync(entryStream);
                                }
                            }
                        }
                    }

                    memoryStream.Position = 0;
                    return base.File(memoryStream.ToArray(), "application/zip", "converted_files.zip");
                }

                return Ok(new
                {
                    success = false,
                    message = "No files were successfully converted",
                    details = failedResults.Select(r => new { r.OriginalFileName, r.Error })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertFiles");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("merge-pdfs")]
        public async Task<IActionResult> MergePdfs([FromForm] MergePdfRequest request)
        {
            try
            {
                if (request.Files == null || !request.Files.Any())
                    return BadRequest("No PDF files provided for merging");

                var pdfBytesList = new List<byte[]>();
                foreach (var file in request.Files)
                {
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    pdfBytesList.Add(memoryStream.ToArray());
                }

                var resultPath = await _pdfService.MergePdfsAsync(pdfBytesList);

                // Apply redaction if requested
                if (request.TextToRedact != null && request.TextToRedact.Any())
                {
                    var pdfBytes = await System.IO.File.ReadAllBytesAsync(resultPath);
                    var redactedBytes = await _pdfService.RedactPdfAsync(
                        pdfBytes,
                        request.TextToRedact,
                        request.RedactPartialWords
                    );

                    var redactedPath = resultPath.Replace(".pdf", "_redacted.pdf");
                    await System.IO.File.WriteAllBytesAsync(redactedPath, redactedBytes);
                    resultPath = redactedPath;
                }

                var fileName = Path.GetFileName(resultPath);
                var fileBytes = await System.IO.File.ReadAllBytesAsync(resultPath);

                return base.File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MergePdfs");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("compress-pdf")]
        public async Task<IActionResult> CompressPdf([FromForm] CompressionRequest request)
        {
            try
            {
                if (request.File == null)
                    return BadRequest("File is required");

                using var memoryStream = new MemoryStream();
                await request.File.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var compressedBytes = await _pdfService.CompressPdfAsync(fileBytes, request.Level);

                // Use base.File() to avoid conflict with System.IO.File
                return base.File(compressedBytes, "application/pdf", "compressed.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CompressPdf");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("redact-pdf")]
        public async Task<IActionResult> RedactPdf([FromForm] RedactionRequest request)
        {
            try
            {
                if (request.File == null)
                    return BadRequest("File is required");

                if (request.TextToRedact == null || !request.TextToRedact.Any())
                    return BadRequest("No text specified for redaction");

                using var memoryStream = new MemoryStream();
                await request.File.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var redactedBytes = await _pdfService.RedactPdfAsync(
                    fileBytes,
                    request.TextToRedact,
                    request.RedactPartialWords
                );

                return base.File(redactedBytes, "application/pdf", "redacted.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RedactPdf");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("download/{fileName}")]
        public IActionResult DownloadFile(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_fileStorageService.GetOutputDirectory(), fileName);

                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found");

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return base.File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpGet("list-files")]
        public IActionResult ListFiles()
        {
            try
            {
                var outputDir = _fileStorageService.GetOutputDirectory();
                var files = Directory.GetFiles(outputDir, "*.pdf")
                    .Select(f => new
                    {
                        fileName = Path.GetFileName(f),
                        filePath = f,
                        fileSize = new FileInfo(f).Length,
                        createdDate = System.IO.File.GetCreationTime(f),
                        downloadUrl = Url.Action("DownloadFile", new { fileName = Path.GetFileName(f) })
                    })
                    .OrderByDescending(f => f.createdDate)
                    .ToList();

                return Ok(new { success = true, files = files, count = files.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}