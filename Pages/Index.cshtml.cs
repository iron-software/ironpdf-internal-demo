using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IronPdfConverter.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

namespace IronPdfConverter.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IPdfConversionService _pdfService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IPdfConversionService pdfService,
            IFileStorageService fileStorageService,
            ILogger<IndexModel> logger)
        {
            _pdfService = pdfService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [BindProperty]
        public ConversionRequest ConversionRequest { get; set; } = new();

        [BindProperty]
        public MergeRequest MergeRequest { get; set; } = new();

        [BindProperty]
        public bool EnableRedaction { get; set; } = false;

        [BindProperty]
        public string RedactionText { get; set; } = string.Empty;

        [BindProperty]
        public bool RedactPartialWords { get; set; } = false;

        public List<string> AvailableFiles { get; set; } = new();
        public List<FileDisplayInfo> FileDisplays { get; set; } = new();

        public void OnGet()
        {
            RefreshAvailableFiles();
            RefreshFileDisplays();
        }

        public async Task<IActionResult> OnPostConvertFilesAsync()
        {
            try
            {
                if (ConversionRequest.Files == null || !ConversionRequest.Files.Any())
                {
                    TempData["Error"] = "Please select at least one file";
                    return RedirectToPage();
                }

                _logger.LogInformation($"Processing {ConversionRequest.Files.Count} files for conversion");

                // Validate file types and sizes
                var validFiles = new List<IFormFile>();
                foreach (var file in ConversionRequest.Files)
                {
                    var extension = Path.GetExtension(file.FileName).ToLower();
                    var allowedExtensions = new[] { ".html", ".htm", ".png", ".jpg", ".jpeg", ".docx" };

                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["Error"] = $"File type {extension} is not supported. Supported types: .html, .htm, .png, .jpg, .jpeg, .docx";
                        return RedirectToPage();
                    }

                    if (file.Length > 10 * 1024 * 1024) // 10MB limit
                    {
                        TempData["Error"] = $"File {file.FileName} is too large. Maximum size is 10MB.";
                        return RedirectToPage();
                    }

                    validFiles.Add(file);
                }

                var resultPaths = await _pdfService.ProcessMultipleFilesAsync(validFiles);

                // Apply redaction if enabled for each converted file
                if (EnableRedaction && !string.IsNullOrWhiteSpace(RedactionText))
                {
                    var textsToRedact = RedactionText.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    if (textsToRedact.Any())
                    {
                        var redactedPaths = new List<string>();
                        foreach (var path in resultPaths)
                        {
                            try
                            {
                                var redactedPath = await _pdfService.RedactPdfAsync(path, textsToRedact, RedactPartialWords);
                                redactedPaths.Add(redactedPath);

                                // Clean up original (non-redacted) file
                                if (System.IO.File.Exists(path) && path != redactedPath)
                                {
                                    System.IO.File.Delete(path);
                                }
                            }
                            catch (Exception redactEx)
                            {
                                _logger.LogWarning(redactEx, "Failed to redact file: {Path}", path);
                                redactedPaths.Add(path); // Keep original if redaction fails
                            }
                        }
                        resultPaths = redactedPaths;
                    }
                }

                TempData["Success"] = $"Successfully converted {resultPaths.Count} files to PDF" +
                                     (EnableRedaction ? " with redaction applied" : "");
                RefreshAvailableFiles();
                RefreshFileDisplays();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting files");
                TempData["Error"] = $"Error converting files: {ex.Message}";
                return RedirectToPage();
            }
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

                if (!ConversionRequest.Url.StartsWith("http://") && !ConversionRequest.Url.StartsWith("https://"))
                {
                    ConversionRequest.Url = "https://" + ConversionRequest.Url;
                }

                if (!Uri.IsWellFormedUriString(ConversionRequest.Url, UriKind.Absolute))
                {
                    TempData["Error"] = "Please enter a valid URL";
                    return RedirectToPage();
                }

                var resultPath = await _pdfService.ConvertUrlToPdfAsync(ConversionRequest.Url);

                // Apply redaction if enabled
                if (EnableRedaction && !string.IsNullOrWhiteSpace(RedactionText))
                {
                    var textsToRedact = RedactionText.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    if (textsToRedact.Any())
                    {
                        try
                        {
                            var originalPath = resultPath;
                            resultPath = await _pdfService.RedactPdfAsync(resultPath, textsToRedact, RedactPartialWords);

                            // Clean up original (non-redacted) file
                            if (System.IO.File.Exists(originalPath) && originalPath != resultPath)
                            {
                                System.IO.File.Delete(originalPath);
                            }
                        }
                        catch (Exception redactEx)
                        {
                            _logger.LogWarning(redactEx, "Failed to redact PDF from URL");
                            // Continue with non-redacted version
                        }
                    }
                }

                // Generate meaningful filename
                var uri = new Uri(ConversionRequest.Url);
                var domain = uri.Host.Replace("www.", "");
                var suffix = EnableRedaction ? "_redacted" : "";
                var outputFileName = _fileStorageService.GenerateUniqueFileName(domain + suffix, ".pdf");

                var newPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                if (System.IO.File.Exists(resultPath) && resultPath != newPath)
                {
                    System.IO.File.Move(resultPath, newPath, true);
                    resultPath = newPath;
                }

                TempData["Success"] = $"URL successfully converted to PDF: {Path.GetFileName(resultPath)}" +
                                     (EnableRedaction ? " (with redaction)" : "");
                RefreshAvailableFiles();
                RefreshFileDisplays();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting URL: {Url}", ConversionRequest.Url);
                TempData["Error"] = $"Error converting URL: {ex.Message}";
                return RedirectToPage();
            }
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

                if (ConversionRequest.HtmlContent.Length > 100000)
                {
                    TempData["Error"] = "HTML content is too large";
                    return RedirectToPage();
                }

                var resultPath = await _pdfService.ConvertHtmlToPdfAsync(ConversionRequest.HtmlContent);

                // Apply redaction if enabled
                if (EnableRedaction && !string.IsNullOrWhiteSpace(RedactionText))
                {
                    var textsToRedact = RedactionText.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    if (textsToRedact.Any())
                    {
                        try
                        {
                            var originalPath = resultPath;
                            resultPath = await _pdfService.RedactPdfAsync(resultPath, textsToRedact, RedactPartialWords);

                            // Clean up original (non-redacted) file
                            if (System.IO.File.Exists(originalPath) && originalPath != resultPath)
                            {
                                System.IO.File.Delete(originalPath);
                            }
                        }
                        catch (Exception redactEx)
                        {
                            _logger.LogWarning(redactEx, "Failed to redact HTML PDF");
                            // Continue with non-redacted version
                        }
                    }
                }

                string outputFileName;
                var titleMatch = System.Text.RegularExpressions.Regex.Match(
                    ConversionRequest.HtmlContent,
                    @"<title[^>]*>\s*(.+?)\s*</title>",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (titleMatch.Success && titleMatch.Groups.Count > 1)
                {
                    var title = titleMatch.Groups[1].Value;
                    var suffix = EnableRedaction ? "_redacted" : "";
                    outputFileName = _fileStorageService.GenerateUniqueFileName(title + suffix, ".pdf");
                }
                else
                {
                    var suffix = EnableRedaction ? "_redacted" : "";
                    outputFileName = _fileStorageService.GenerateUniqueFileName("HTML_Document" + suffix, ".pdf");
                }

                var newPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                if (System.IO.File.Exists(resultPath) && resultPath != newPath)
                {
                    System.IO.File.Move(resultPath, newPath, true);
                    resultPath = newPath;
                }

                TempData["Success"] = $"HTML successfully converted to PDF: {Path.GetFileName(resultPath)}" +
                                     (EnableRedaction ? " (with redaction)" : "");
                RefreshAvailableFiles();
                RefreshFileDisplays();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting HTML");
                TempData["Error"] = $"Error converting HTML: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostMergePdfsAsync()
        {
            try
            {
                if (MergeRequest.FilePaths == null || MergeRequest.FilePaths.Count < 2)
                {
                    TempData["Error"] = "Please select at least 2 PDF files to merge";
                    return RedirectToPage();
                }

                var missingFiles = new List<string>();
                var fullPaths = new List<string>();

                foreach (var fileName in MergeRequest.FilePaths)
                {
                    var fullPath = Path.Combine(_fileStorageService.GetOutputDirectory(), fileName);
                    if (!System.IO.File.Exists(fullPath))
                    {
                        missingFiles.Add(fileName);
                    }
                    else
                    {
                        fullPaths.Add(fullPath);
                    }
                }

                if (missingFiles.Any())
                {
                    TempData["Error"] = $"Files not found: {string.Join(", ", missingFiles)}";
                    return RedirectToPage();
                }

                if (fullPaths.Count < 2)
                {
                    TempData["Error"] = "Please select at least 2 valid PDF files";
                    return RedirectToPage();
                }

                var resultPath = await _pdfService.MergePdfsAsync(fullPaths);

                // Apply redaction if enabled
                if (EnableRedaction && !string.IsNullOrWhiteSpace(RedactionText))
                {
                    var textsToRedact = RedactionText.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    if (textsToRedact.Any())
                    {
                        try
                        {
                            var originalPath = resultPath;
                            resultPath = await _pdfService.RedactPdfAsync(resultPath, textsToRedact, RedactPartialWords);

                            // Clean up original (non-redacted) file
                            if (System.IO.File.Exists(originalPath) && originalPath != resultPath)
                            {
                                System.IO.File.Delete(originalPath);
                            }
                        }
                        catch (Exception redactEx)
                        {
                            _logger.LogWarning(redactEx, "Failed to redact merged PDF");
                            // Continue with non-redacted version
                        }
                    }
                }

                var suffix = EnableRedaction ? "_redacted" : "";
                var outputFileName = _fileStorageService.GenerateUniqueFileName("Merged_Document" + suffix, ".pdf");
                var newPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                if (System.IO.File.Exists(resultPath) && resultPath != newPath)
                {
                    System.IO.File.Move(resultPath, newPath, true);
                    resultPath = newPath;
                }

                TempData["Success"] = $"Successfully merged {fullPaths.Count} PDF files" +
                                     (EnableRedaction ? " with redaction applied" : "");
                RefreshAvailableFiles();
                RefreshFileDisplays();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging PDFs");
                TempData["Error"] = $"Error merging PDFs: {ex.Message}";
                return RedirectToPage();
            }
        }

        // New method for standalone redaction of existing PDFs
        public async Task<IActionResult> OnPostRedactPdfAsync(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    TempData["Error"] = "File name is required";
                    return RedirectToPage();
                }

                if (string.IsNullOrWhiteSpace(RedactionText))
                {
                    TempData["Error"] = "Redaction text is required";
                    return RedirectToPage();
                }

                var filePath = Path.Combine(_fileStorageService.GetOutputDirectory(), fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = $"File not found: {fileName}";
                    return RedirectToPage();
                }

                var textsToRedact = RedactionText.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                var redactedPath = await _pdfService.RedactPdfAsync(filePath, textsToRedact, RedactPartialWords);

                // Generate new filename with redacted suffix
                var originalName = Path.GetFileNameWithoutExtension(fileName);
                var redactedName = $"{originalName}_redacted_{Guid.NewGuid():N}.pdf";
                var newPath = Path.Combine(_fileStorageService.GetOutputDirectory(), redactedName);

                if (System.IO.File.Exists(redactedPath) && redactedPath != newPath)
                {
                    System.IO.File.Move(redactedPath, newPath, true);
                    redactedPath = newPath;
                }

                TempData["Success"] = $"Successfully redacted PDF: {Path.GetFileName(redactedPath)}";
                RefreshAvailableFiles();
                RefreshFileDisplays();

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redacting PDF: {FileName}", fileName);
                TempData["Error"] = $"Error redacting PDF: {ex.Message}";
                return RedirectToPage();
            }
        }

        public IActionResult OnGetDownloadFile(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    TempData["Error"] = "File name is required";
                    return RedirectToPage();
                }

                if (fileName.Contains("..") || fileName.Contains("/") || fileName.Contains("\\"))
                {
                    TempData["Error"] = "Invalid file name";
                    return RedirectToPage();
                }

                var filePath = Path.Combine(_fileStorageService.GetOutputDirectory(), fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["Error"] = $"File not found: {fileName}";
                    return RedirectToPage();
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);

                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FileName}", fileName);
                TempData["Error"] = $"Error downloading file: {ex.Message}";
                return RedirectToPage();
            }
        }

        public FileDisplayInfo GetFileDisplay(string fileName)
        {
            return FileDisplays.FirstOrDefault(f => f.FileName == fileName) ?? new FileDisplayInfo
            {
                FileName = fileName,
                FileSize = "Unknown",
                CreatedDate = "Unknown"
            };
        }

        private void RefreshAvailableFiles()
        {
            try
            {
                var outputDir = _fileStorageService.GetOutputDirectory();
                if (Directory.Exists(outputDir))
                {
                    AvailableFiles = Directory.GetFiles(outputDir, "*.pdf")
                        .Select(Path.GetFileName)
                        .Where(name => name != null)
                        .Select(name => name!)
                        .OrderByDescending(name =>
                        {
                            var filePath = Path.Combine(outputDir, name);
                            return new FileInfo(filePath).CreationTime;
                        })
                        .ToList();
                }
                else
                {
                    AvailableFiles = new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing available files");
                AvailableFiles = new List<string>();
            }
        }

        private void RefreshFileDisplays()
        {
            FileDisplays = new List<FileDisplayInfo>();

            try
            {
                var outputDir = _fileStorageService.GetOutputDirectory();
                if (Directory.Exists(outputDir))
                {
                    foreach (var file in AvailableFiles)
                    {
                        var filePath = Path.Combine(outputDir, file);
                        if (System.IO.File.Exists(filePath))
                        {
                            var fileInfo = new FileInfo(filePath);
                            FileDisplays.Add(new FileDisplayInfo
                            {
                                FileName = file,
                                FileSize = $"{fileInfo.Length / 1024} KB",
                                CreatedDate = fileInfo.CreationTime.ToString("MMM dd, yyyy")
                            });
                        }
                        else
                        {
                            FileDisplays.Add(new FileDisplayInfo
                            {
                                FileName = file,
                                FileSize = "Unknown",
                                CreatedDate = "Unknown"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing file displays");
                FileDisplays = AvailableFiles.Select(f => new FileDisplayInfo
                {
                    FileName = f,
                    FileSize = "Unknown",
                    CreatedDate = "Unknown"
                }).ToList();
            }
        }
    }

    public class ConversionRequest
    {
        public string? HtmlContent { get; set; }
        public string? Url { get; set; }
        public List<IFormFile>? Files { get; set; }
    }

    public class MergeRequest
    {
        public List<string> FilePaths { get; set; } = new List<string>();
    }

    public class FileDisplayInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string CreatedDate { get; set; } = string.Empty;
    }
}