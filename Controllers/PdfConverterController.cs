using Microsoft.AspNetCore.Mvc;
using IronPdfConverter.Models;
using IronPdfConverter.Services.Interfaces;

namespace IronPdfConverter.Controllers
{
    [ApiController]
    [Route("[controller]")]
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
        public async Task<IActionResult> ConvertHtmlToPdf([FromBody] ConversionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.HtmlContent))
                    return BadRequest("HTML content is required");

                // FIXED: Remove the second parameter
                var resultPath = await _pdfService.ConvertHtmlToPdfAsync(request.HtmlContent);

                return Ok(new { success = true, filePath = resultPath, fileName = Path.GetFileName(resultPath) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertHtmlToPdf");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("convert-url")]
        public async Task<IActionResult> ConvertUrlToPdf([FromBody] ConversionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Url))
                    return BadRequest("URL is required");

                // FIXED: Remove the second parameter
                var resultPath = await _pdfService.ConvertUrlToPdfAsync(request.Url);

                return Ok(new { success = true, filePath = resultPath, fileName = Path.GetFileName(resultPath) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertUrlToPdf");
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

                var resultPaths = await _pdfService.ProcessMultipleFilesAsync(request.Files.ToList());

                return Ok(new
                {
                    success = true,
                    filePaths = resultPaths,
                    fileNames = resultPaths.Select(Path.GetFileName).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertFiles");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("merge-pdfs")]
        public async Task<IActionResult> MergePdfs([FromBody] MergeRequest request)
        {
            try
            {
                if (request.FilePaths == null || !request.FilePaths.Any())
                    return BadRequest("No PDF files provided for merging");

                // Convert filenames to full paths
                var fullPaths = request.FilePaths.Select(fileName =>
                    Path.Combine(_fileStorageService.GetOutputDirectory(), fileName)
                ).ToList();

                // FIXED: Remove the second parameter
                var resultPath = await _pdfService.MergePdfsAsync(fullPaths);

                return Ok(new { success = true, filePath = resultPath, fileName = Path.GetFileName(resultPath) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MergePdfs");
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
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}