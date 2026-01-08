using IronPdfConverter.Models;
using IronPdfConverter.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;

namespace IronPdfConverter.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IPdfConversionService _pdfService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public ConversionRequest ConversionRequest { get; set; } = new ConversionRequest();

        [BindProperty]
        public MergeRequest MergeRequest { get; set; } = new MergeRequest();

        [BindProperty]
        public bool EnableRedaction { get; set; }

        [BindProperty]
        public string RedactionText { get; set; }

        [BindProperty]
        public bool RedactPartialWords { get; set; }

        public List<string> AvailableFiles { get; set; } = new List<string>();
        public List<FileDisplayInfo> FileDisplays { get; set; } = new List<FileDisplayInfo>();

        public IndexModel(
            IPdfConversionService pdfService,
            IFileStorageService fileStorageService,
            ILogger<IndexModel> logger)
        {
            _pdfService = pdfService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public void OnGet()
        {
            LoadAvailableFiles();
        }

        // ========== DOWNLOAD METHOD ==========
        public IActionResult OnGetDownloadFile(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    TempData["Error"] = "File name is required";
                    return RedirectToPage();
                }

                var filePath = Path.Combine(_fileStorageService.GetOutputDirectory(), fileName);

                _logger.LogInformation($"Attempting to download file from: {filePath}");
                _logger.LogInformation($"File exists: {System.IO.File.Exists(filePath)}");

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = $"File not found: {fileName}";
                    return RedirectToPage();
                }

                // Read the file bytes
                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                _logger.LogInformation($"File size: {fileBytes.Length} bytes");

                // This will force download with the correct filename
                // The third parameter (fileName) automatically sets Content-Disposition header
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file");
                TempData["Error"] = $"Error downloading file: {ex.Message}";
                return RedirectToPage();
            }
        }

        // ========== TEST METHOD (Optional) ==========
        public IActionResult OnGetTestDownload()
        {
            // Create simple test content
            var testContent = "This is a test file for download. If this downloads, your server is configured correctly.";
            var bytes = Encoding.UTF8.GetBytes(testContent);

            // This should always force download
            return File(bytes, "text/plain", "test-download.txt");
        }

        public async Task<IActionResult> OnPostConvertFilesAsync()
        {
            try
            {
                if (ConversionRequest.Files == null || !ConversionRequest.Files.Any())
                {
                    TempData["Error"] = "No files provided";
                    return RedirectToPage();
                }

                var results = await _pdfService.ProcessMultipleFilesAsync(ConversionRequest.Files.ToList());

                var successfulCount = results.Count(r => r.Status == FileConversionStatus.Success);
                var failedCount = results.Count(r => r.Status == FileConversionStatus.Failed);

                if (successfulCount > 0)
                {
                    // Apply redaction if enabled
                    if (EnableRedaction && !string.IsNullOrWhiteSpace(RedactionText))
                    {
                        var redactionList = RedactionText.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim())
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .ToList();

                        if (redactionList.Any())
                        {
                            foreach (var result in results.Where(r => r.Status == FileConversionStatus.Success))
                            {
                                try
                                {
                                    // Use the backward compatibility method
                                    await _pdfService.RedactPdfAsync(
                                        result.OutputPath,
                                        redactionList,
                                        RedactPartialWords
                                    );
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, $"Failed to redact file: {result.OriginalFileName}");
                                }
                            }
                        }
                    }

                    TempData["Success"] = $"Successfully converted {successfulCount} file(s)" +
                        (failedCount > 0 ? $", {failedCount} file(s) failed" : "");
                }
                else
                {
                    TempData["Error"] = "No files were successfully converted";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting files");
                TempData["Error"] = $"Error: {ex.Message}";
            }

            LoadAvailableFiles();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostConvertUrlAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ConversionRequest.Url))
                {
                    TempData["Error"] = "URL is required";
                    return RedirectToPage();
                }

                var resultPath = await _pdfService.ConvertUrlToPdfAsync(ConversionRequest.Url);

                // Apply redaction if enabled
                if (EnableRedaction && !string.IsNullOrWhiteSpace(RedactionText))
                {
                    var redactionList = RedactionText.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    if (redactionList.Any())
                    {
                        // Use the backward compatibility method
                        await _pdfService.RedactPdfAsync(
                            resultPath,
                            redactionList,
                            RedactPartialWords
                        );
                    }
                }

                TempData["Success"] = $"Successfully converted URL to PDF: {Path.GetFileName(resultPath)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting URL to PDF");
                TempData["Error"] = $"Error: {ex.Message}";
            }

            LoadAvailableFiles();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostConvertHtmlAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ConversionRequest.HtmlContent))
                {
                    TempData["Error"] = "HTML content is required";
                    return RedirectToPage();
                }

                var resultPath = await _pdfService.ConvertHtmlToPdfAsync(ConversionRequest.HtmlContent);

                // Apply redaction if enabled
                if (EnableRedaction && !string.IsNullOrWhiteSpace(RedactionText))
                {
                    var redactionList = RedactionText.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    if (redactionList.Any())
                    {
                        // Use the backward compatibility method
                        await _pdfService.RedactPdfAsync(
                            resultPath,
                            redactionList,
                            RedactPartialWords
                        );
                    }
                }

                TempData["Success"] = $"Successfully converted HTML to PDF: {Path.GetFileName(resultPath)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting HTML to PDF");
                TempData["Error"] = $"Error: {ex.Message}";
            }

            LoadAvailableFiles();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMergePdfsAsync()
        {
            try
            {
                if (MergeRequest.FilePaths == null || !MergeRequest.FilePaths.Any())
                {
                    TempData["Error"] = "No PDF files selected for merging";
                    return RedirectToPage();
                }

                if (MergeRequest.FilePaths.Count < 2)
                {
                    TempData["Error"] = "Select at least 2 PDF files to merge";
                    return RedirectToPage();
                }

                // Convert filenames to full paths and read as byte arrays
                var pdfBytesList = new List<byte[]>();
                foreach (var fileName in MergeRequest.FilePaths)
                {
                    var filePath = Path.Combine(_fileStorageService.GetOutputDirectory(), fileName);
                    if (System.IO.File.Exists(filePath))
                    {
                        var pdfBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                        pdfBytesList.Add(pdfBytes);
                    }
                }

                var resultPath = await _pdfService.MergePdfsAsync(pdfBytesList);

                // Apply redaction if enabled
                if (EnableRedaction && !string.IsNullOrWhiteSpace(RedactionText))
                {
                    var redactionList = RedactionText.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    if (redactionList.Any())
                    {
                        // Use the backward compatibility method
                        await _pdfService.RedactPdfAsync(
                            resultPath,
                            redactionList,
                            RedactPartialWords
                        );
                    }
                }

                TempData["Success"] = $"Successfully merged {pdfBytesList.Count} PDFs: {Path.GetFileName(resultPath)}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging PDFs");
                TempData["Error"] = $"Error: {ex.Message}";
            }

            LoadAvailableFiles();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRedactPdfAsync(string fileName, string redactionText, bool redactPartialWords)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    TempData["Error"] = "File name is required";
                    return RedirectToPage();
                }

                if (string.IsNullOrWhiteSpace(redactionText))
                {
                    TempData["Error"] = "Redaction text is required";
                    return RedirectToPage();
                }

                var filePath = Path.Combine(_fileStorageService.GetOutputDirectory(), fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = "File not found";
                    return RedirectToPage();
                }

                var redactionList = redactionText.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                // Use the backward compatibility method
                await _pdfService.RedactPdfAsync(filePath, redactionList, redactPartialWords);

                TempData["Success"] = $"Successfully redacted PDF: {Path.GetFileNameWithoutExtension(fileName)}_redacted.pdf";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redacting PDF");
                TempData["Error"] = $"Error: {ex.Message}";
            }

            LoadAvailableFiles();
            return RedirectToPage();
        }

        private void LoadAvailableFiles()
        {
            try
            {
                var outputDir = _fileStorageService.GetOutputDirectory();
                if (Directory.Exists(outputDir))
                {
                    AvailableFiles = Directory.GetFiles(outputDir, "*.pdf").Select(Path.GetFileName).OrderByDescending(f => f).ToList();

                    FileDisplays = AvailableFiles.Select(fileName =>
                    {
                        var filePath = Path.Combine(outputDir, fileName);
                        var fileInfo = new FileInfo(filePath);
                        return new FileDisplayInfo
                        {
                            FileName = fileName,
                            FileSize = FormatFileSize(fileInfo.Length),
                            CreatedDate = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm")
                        };
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available files");
                AvailableFiles = new List<string>();
                FileDisplays = new List<FileDisplayInfo>();
            }
        }

        public FileDisplayInfo GetFileDisplay(string fileName)
        {
            var filePath = Path.Combine(_fileStorageService.GetOutputDirectory(), fileName);
            if (System.IO.File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                return new FileDisplayInfo
                {
                    FileName = fileName,
                    FileSize = FormatFileSize(fileInfo.Length),
                    CreatedDate = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm")
                };
            }
            return new FileDisplayInfo { FileName = fileName, FileSize = "N/A", CreatedDate = "N/A" };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class FileDisplayInfo
    {
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string CreatedDate { get; set; }
    }
}