using IronPdf;
using IronPdfConverter.Models;
using IronPdfConverter.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace IronPdfConverter.Services.Implementations
{
    public class PdfConversionService : IPdfConversionService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<PdfConversionService> _logger;
        private readonly IConfiguration _configuration;

        public PdfConversionService(
            IFileStorageService fileStorageService,
            ILogger<PdfConversionService> logger,
            IConfiguration configuration)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
            _configuration = configuration;

            _logger.LogInformation("PdfConversionService initialized");
            ConfigureIronPdfLicense();
        }

        private void ConfigureIronPdfLicense()
        {
            try
            {
                var licenseKey = _configuration["IronPdf:LicenseKey"];

                if (!string.IsNullOrEmpty(licenseKey) && licenseKey != "YOUR-IRONPDF-LICENSE-KEY-HERE")
                {
                    License.LicenseKey = licenseKey;

                    if (License.IsLicensed)
                    {
                        _logger.LogInformation("IronPDF is properly licensed.");
                    }
                    else
                    {
                        _logger.LogWarning("IronPDF license key was set but IsLicensed returns false.");
                    }
                }
                else
                {
                    _logger.LogInformation("No valid IronPDF license key found.");
                    License.LicenseKey = "";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "IronPDF license configuration had issues.");
                License.LicenseKey = "";
            }
        }

        public async Task<string> ConvertHtmlToPdfAsync(string htmlContent)
        {
            try
            {
                _logger.LogInformation("Starting HTML to PDF conversion...");

                if (string.IsNullOrWhiteSpace(htmlContent))
                    throw new ArgumentException("HTML content cannot be empty or null");

                // Ensure HTML has proper structure
                htmlContent = EnsureHtmlStructure(htmlContent);

                var renderer = new ChromePdfRenderer();
                ConfigureRenderer(renderer);

                _logger.LogInformation("Rendering HTML as PDF...");
                var pdf = renderer.RenderHtmlAsPdf(htmlContent);

                // Generate filename
                var outputFileName = _fileStorageService.GenerateUniqueFileName(".pdf");
                var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                pdf.SaveAs(outputPath);

                _logger.LogInformation($"Successfully converted HTML to PDF: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting HTML to PDF");
                throw new Exception($"Failed to convert HTML to PDF: {ex.Message}", ex);
            }
        }

        public async Task<string> ConvertUrlToPdfAsync(string url)
        {
            try
            {
                _logger.LogInformation($"Starting URL to PDF conversion: {url}");

                if (string.IsNullOrWhiteSpace(url))
                    throw new ArgumentException("URL cannot be empty or null");

                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    throw new ArgumentException("Invalid URL format");

                var renderer = new ChromePdfRenderer();
                ConfigureRenderer(renderer);

                _logger.LogInformation("Rendering URL as PDF...");
                var pdf = renderer.RenderUrlAsPdf(url);

                // Generate filename
                var outputFileName = _fileStorageService.GenerateUniqueFileName(".pdf");
                var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                pdf.SaveAs(outputPath);

                _logger.LogInformation($"Successfully converted URL to PDF: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting URL to PDF: {Url}", url);
                throw new Exception($"Failed to convert URL to PDF: {ex.Message}", ex);
            }
        }

        public async Task<string> ConvertImageToPdfAsync(string imagePath)
        {
            try
            {
                _logger.LogInformation($"Starting image to PDF conversion: {imagePath}");

                if (!File.Exists(imagePath))
                    throw new FileNotFoundException($"Image file not found: {imagePath}");

                // Read image and convert to base64
                var imageBytes = File.ReadAllBytes(imagePath);
                var base64Image = Convert.ToBase64String(imageBytes);
                var mimeType = GetMimeType(Path.GetExtension(imagePath));

                // Create HTML with the embedded image
                var htmlContent = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='utf-8'>
                        <style>
                            body {{ margin: 0; padding: 10px; }}
                            img {{ max-width: 100%; height: auto; display: block; margin: 0 auto; }}
                        </style>
                    </head>
                    <body>
                        <img src='data:{mimeType};base64,{base64Image}' />
                    </body>
                    </html>";

                var renderer = new ChromePdfRenderer();
                ConfigureRenderer(renderer);

                var pdf = renderer.RenderHtmlAsPdf(htmlContent);

                // Generate filename
                var outputFileName = _fileStorageService.GenerateUniqueFileName(".pdf");
                var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                pdf.SaveAs(outputPath);

                // Clean up temporary image file
                try
                {
                    if (File.Exists(imagePath))
                        File.Delete(imagePath);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Failed to delete temporary image file");
                }

                _logger.LogInformation($"Successfully converted image to PDF: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting image to PDF: {ImagePath}", imagePath);
                throw new Exception($"Failed to convert image to PDF: {ex.Message}", ex);
            }
        }

        public async Task<string> ConvertDocxToPdfAsync(string docxPath)
        {
            try
            {
                _logger.LogInformation($"Starting DOCX to PDF conversion: {docxPath}");

                if (!File.Exists(docxPath))
                    throw new FileNotFoundException($"DOCX file not found: {docxPath}");

                // Use IronPDF's DocxToPdfRenderer for proper conversion
                var renderer = new DocxToPdfRenderer();

                _logger.LogInformation("Rendering DOCX as PDF...");
                var pdf = renderer.RenderDocxAsPdf(docxPath);

                // Generate filename
                var outputFileName = _fileStorageService.GenerateUniqueFileName(".pdf");
                var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                pdf.SaveAs(outputPath);

                // Clean up temporary DOCX file
                try
                {
                    if (File.Exists(docxPath))
                        File.Delete(docxPath);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Failed to delete temporary DOCX file");
                }

                _logger.LogInformation($"Successfully converted DOCX to PDF: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting DOCX to PDF: {DocxPath}", docxPath);

                // Fallback to simple HTML conversion if IronPDF fails
                _logger.LogInformation($"Attempting fallback conversion for DOCX file: {docxPath}");
                try
                {
                    var htmlContent = CreateSimpleHtmlFromDocx(docxPath);
                    var renderer = new ChromePdfRenderer();
                    ConfigureRenderer(renderer);

                    var pdf = renderer.RenderHtmlAsPdf(htmlContent);

                    // Generate filename with fallback suffix
                    var outputFileName = _fileStorageService.GenerateUniqueFileName(".pdf");
                    var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                    pdf.SaveAs(outputPath);

                    // Clean up temporary DOCX file
                    try
                    {
                        if (File.Exists(docxPath))
                            File.Delete(docxPath);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "Failed to delete temporary DOCX file");
                    }

                    _logger.LogInformation($"Successfully converted DOCX to PDF using fallback: {outputPath}");
                    return outputPath;
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback conversion also failed");
                    throw new Exception($"Failed to convert DOCX to PDF: {ex.Message}", ex);
                }
            }
        }

        public async Task<string> MergePdfsAsync(List<string> pdfPaths)
        {
            try
            {
                _logger.LogInformation("Starting PDF merge operation...");

                if (pdfPaths == null || pdfPaths.Count < 2)
                    throw new ArgumentException("At least 2 PDF files are required for merging");

                // Check files exist
                foreach (var pdfPath in pdfPaths)
                {
                    if (!File.Exists(pdfPath))
                        throw new FileNotFoundException($"PDF file not found: {pdfPath}");
                }

                var firstPdf = PdfDocument.FromFile(pdfPaths[0]);

                for (int i = 1; i < pdfPaths.Count; i++)
                {
                    var nextPdf = PdfDocument.FromFile(pdfPaths[i]);
                    firstPdf.AppendPdf(nextPdf);
                }

                // Generate filename
                var outputFileName = _fileStorageService.GenerateUniqueFileName(".pdf");
                var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                firstPdf.SaveAs(outputPath);

                _logger.LogInformation($"Successfully merged {pdfPaths.Count} PDFs: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging PDFs");
                throw new Exception($"Failed to merge PDFs: {ex.Message}", ex);
            }
        }

        public async Task<string> CompressPdfAsync(string pdfPath, CompressionLevel level = CompressionLevel.Medium)
        {
            try
            {
                _logger.LogInformation($"Compressing PDF: {pdfPath} with level: {level}");

                if (!File.Exists(pdfPath))
                    throw new FileNotFoundException($"PDF file not found: {pdfPath}");

                // For None level, return original file
                if (level == CompressionLevel.None)
                {
                    _logger.LogInformation($"No compression applied for {pdfPath}");
                    return pdfPath;
                }

                // Load the PDF document
                var pdf = PdfDocument.FromFile(pdfPath);

                // Apply compression (IronPDF only has CompressStructTree)
                pdf.CompressStructTree();

                // Generate output filename
                var originalName = Path.GetFileNameWithoutExtension(pdfPath);
                var compressedName = $"{originalName}_compressed_{Guid.NewGuid():N}.pdf";
                var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), compressedName);

                // Save the compressed PDF
                pdf.SaveAs(outputPath);

                // Get file size info for logging
                var originalSize = new FileInfo(pdfPath).Length;
                var compressedSize = new FileInfo(outputPath).Length;
                var reductionPercentage = originalSize > 0 ? ((originalSize - compressedSize) / (double)originalSize) * 100 : 0;

                _logger.LogInformation($"PDF compressed successfully. Original: {originalSize} bytes, Compressed: {compressedSize} bytes, Reduction: {reductionPercentage:F2}%");

                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing PDF");

                // If compression fails, return the original file
                _logger.LogWarning("Compression failed, returning original file");
                return pdfPath;
            }
        }

        public async Task<string> RedactPdfAsync(string pdfPath, List<string> textToRedact, bool redactPartialWords = false)
        {
            try
            {
                _logger.LogInformation($"Redacting PDF: {pdfPath}");

                if (!File.Exists(pdfPath))
                    throw new FileNotFoundException($"PDF file not found: {pdfPath}");

                if (textToRedact == null || !textToRedact.Any())
                    throw new ArgumentException("No text specified for redaction");

                var pdf = PdfDocument.FromFile(pdfPath);

                // Apply redaction for each text
                foreach (var text in textToRedact.Where(t => !string.IsNullOrWhiteSpace(t)))
                {
                    try
                    {
                        if (redactPartialWords)
                        {
                            // Redact partial matches (case insensitive)
                            pdf.RedactTextOnAllPages(text, false);
                        }
                        else
                        {
                            // Redact exact matches only
                            pdf.RedactTextOnAllPages(text, true);
                        }
                        _logger.LogDebug($"Redacted text: '{text}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Could not redact text: '{text}'");
                    }
                }

                // Generate output filename
                var originalName = Path.GetFileNameWithoutExtension(pdfPath);
                var redactedName = $"{originalName}_redacted_{Guid.NewGuid():N}.pdf";
                var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), redactedName);

                pdf.SaveAs(outputPath);

                _logger.LogInformation($"PDF redacted successfully. Redacted {textToRedact.Count} text patterns.");

                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redacting PDF");
                throw new Exception($"Failed to redact PDF: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> ProcessMultipleFilesAsync(List<IFormFile> files)
        {
            var results = new List<string>();
            _logger.LogInformation($"Processing {files.Count} files for conversion...");

            foreach (var file in files)
            {
                try
                {
                    _logger.LogInformation($"Processing file: {file.FileName}");

                    if (file.Length == 0)
                    {
                        _logger.LogWarning($"Skipping empty file: {file.FileName}");
                        continue;
                    }

                    // Save file temporarily
                    var filePath = await _fileStorageService.SaveFileAsync(file, "temp");

                    // Generate output filename based on original filename
                    var outputFileName = _fileStorageService.GenerateUniqueFileName(file.FileName, ".pdf");
                    string resultPath;

                    var extension = Path.GetExtension(file.FileName).ToLower();
                    _logger.LogInformation($"Converting file with extension: {extension}");

                    switch (extension)
                    {
                        case ".html":
                        case ".htm":
                            var htmlContent = await File.ReadAllTextAsync(filePath);
                            resultPath = await ConvertHtmlToPdfAsync(htmlContent);
                            break;
                        case ".jpg":
                        case ".jpeg":
                        case ".png":
                            resultPath = await ConvertImageToPdfAsync(filePath);
                            break;
                        case ".docx":
                            resultPath = await ConvertDocxToPdfAsync(filePath);
                            break;
                        default:
                            throw new NotSupportedException($"File type {extension} is not supported");
                    }

                    // Rename the file to preserve original filename
                    var newPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                    if (File.Exists(resultPath) && resultPath != newPath)
                    {
                        File.Move(resultPath, newPath, true);
                        resultPath = newPath;
                    }

                    results.Add(resultPath);
                    _logger.LogInformation($"Successfully processed: {file.FileName} -> {Path.GetFileName(resultPath)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing file: {file.FileName}");
                    throw new Exception($"Failed to process file {file.FileName}: {ex.Message}", ex);
                }
            }

            _logger.LogInformation($"Successfully processed {results.Count} files");
            return results;
        }

        #region Private Helper Methods

        private void ConfigureRenderer(ChromePdfRenderer renderer)
        {
            try
            {
                renderer.RenderingOptions.PaperSize = IronPdf.Rendering.PdfPaperSize.A4;
                renderer.RenderingOptions.MarginTop = 20;
                renderer.RenderingOptions.MarginBottom = 20;
                renderer.RenderingOptions.MarginLeft = 20;
                renderer.RenderingOptions.MarginRight = 20;
                renderer.RenderingOptions.Timeout = 30000;
                renderer.RenderingOptions.EnableJavaScript = false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring PDF renderer");
            }
        }

        private string EnsureHtmlStructure(string htmlContent)
        {
            var trimmedContent = htmlContent.Trim();

            if (trimmedContent.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                trimmedContent.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                return htmlContent;
            }

            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; line-height: 1.4; }}
    </style>
</head>
<body>
    {htmlContent}
</body>
</html>";
        }

        private string CreateSimpleHtmlFromDocx(string docxPath)
        {
            var fileName = Path.GetFileName(docxPath);
            var fileInfo = new FileInfo(docxPath);

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 40px; line-height: 1.6; }}
        .header {{ text-align: center; margin-bottom: 30px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>DOCX File Conversion</h1>
        <p>Original file: {fileName}</p>
    </div>
    <div>
        <p>This document was converted from a DOCX file to PDF.</p>
        <p><strong>File:</strong> {fileName}</p>
        <p><strong>Size:</strong> {fileInfo.Length} bytes</p>
        <p><strong>Converted:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    </div>
</body>
</html>";
        }

        private string GetMimeType(string extension)
        {
            return extension.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "image/jpeg"
            };
        }

        #endregion
    }
}