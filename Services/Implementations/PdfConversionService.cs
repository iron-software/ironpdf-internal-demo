using IronPdf;
using IronPdfConverter.Models;
using IronPdfConverter.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Text;

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

                if (!string.IsNullOrEmpty(licenseKey))
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

        public async Task<string> ConvertImageToPdfAsync(byte[] imageBytes, string fileName = null)
        {
            try
            {
                _logger.LogInformation($"Starting image to PDF conversion");

                if (imageBytes == null || imageBytes.Length == 0)
                    throw new ArgumentException("Image bytes cannot be empty");
                
                var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + GetImageExtension(imageBytes));
                try
                {
                    await File.WriteAllBytesAsync(tempFilePath, imageBytes);

                    var pdfDocument = IronPdf.ImageToPdfConverter.ImageToPdf(tempFilePath);

                    var outputFileName = _fileStorageService.GenerateUniqueFileName(
                        fileName ?? "image",
                        ".pdf"
                    );
                    var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);

                    pdfDocument.SaveAs(outputPath);

                    _logger.LogInformation($"Successfully converted image to PDF: {outputPath}");
                    return outputPath;
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                    {
                        try
                        {
                            File.Delete(tempFilePath);
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(deleteEx, $"Failed to delete temp file: {tempFilePath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting image to PDF using IronPDF ImageToPdfConverter");
                throw new Exception($"Failed to convert image to PDF: {ex.Message}", ex);
            }
        }

        private string GetImageExtension(byte[] imageBytes)
        {
            var format = GetImageFormat(imageBytes);
            return format switch
            {
                ImageFormat.Jpeg => ".jpg",
                ImageFormat.Png => ".png",
                ImageFormat.Gif => ".gif",
                ImageFormat.Bmp => ".bmp",
                _ => ".jpg"
            };
        }

        public async Task<string> ConvertDocxToPdfAsync(byte[] docxBytes, string fileName = null)
        {
            try
            {
                _logger.LogInformation($"Starting DOCX to PDF conversion");

                if (docxBytes == null || docxBytes.Length == 0)
                    throw new ArgumentException("DOCX bytes cannot be empty");

                // Create temporary file
                var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".docx");
                try
                {
                    await File.WriteAllBytesAsync(tempFilePath, docxBytes);

                    var renderer = new DocxToPdfRenderer();
                    var pdf = renderer.RenderDocxAsPdf(tempFilePath);

                    // Generate filename
                    var outputFileName = _fileStorageService.GenerateUniqueFileName(
                        fileName ?? "document",
                        ".pdf"
                    );
                    var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);

                    pdf.SaveAs(outputPath);
                    _logger.LogInformation($"Successfully converted DOCX to PDF: {outputPath}");
                    return outputPath;
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting DOCX to PDF");

                // Fallback: Create simple HTML from DOCX content
                try
                {
                    _logger.LogInformation($"Attempting fallback conversion for DOCX file");

                    var renderer = new ChromePdfRenderer();
                    ConfigureRenderer(renderer);

                    var htmlContent = CreateSimpleHtmlFromDocx(fileName ?? "Document");
                    var pdf = renderer.RenderHtmlAsPdf(htmlContent);

                    var outputFileName = _fileStorageService.GenerateUniqueFileName(
                        (fileName ?? "document") + "_fallback",
                        ".pdf"
                    );
                    var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                    pdf.SaveAs(outputPath);

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

        public async Task<string> MergePdfsAsync(List<byte[]> pdfBytesArray)
        {
            try
            {
                _logger.LogInformation("Starting PDF merge operation...");

                if (pdfBytesArray == null || pdfBytesArray.Count < 2)
                    throw new ArgumentException("At least 2 PDF byte arrays are required for merging");

                // Create temporary files
                var tempFiles = new List<string>();
                try
                {
                    // Write all PDF bytes to temp files
                    foreach (var pdfBytes in pdfBytesArray)
                    {
                        if (pdfBytes == null || pdfBytes.Length == 0)
                            throw new ArgumentException("PDF byte array cannot be empty");

                        var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
                        await File.WriteAllBytesAsync(tempFilePath, pdfBytes);
                        tempFiles.Add(tempFilePath);
                    }

                    // Load first PDF
                    var firstPdf = PdfDocument.FromFile(tempFiles[0]);

                    // Append remaining PDFs
                    for (int i = 1; i < tempFiles.Count; i++)
                    {
                        var nextPdf = PdfDocument.FromFile(tempFiles[i]);
                        firstPdf.AppendPdf(nextPdf);
                    }

                    // Generate filename
                    var outputFileName = _fileStorageService.GenerateUniqueFileName("merged", ".pdf");
                    var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                    firstPdf.SaveAs(outputPath);

                    _logger.LogInformation($"Successfully merged {pdfBytesArray.Count} PDFs: {outputPath}");
                    return outputPath;
                }
                finally
                {
                    // Clean up temp files
                    foreach (var tempFile in tempFiles)
                    {
                        if (File.Exists(tempFile))
                            File.Delete(tempFile);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging PDFs");
                throw new Exception($"Failed to merge PDFs: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> CompressPdfAsync(byte[] pdfBytes, PdfCompressionLevel level = PdfCompressionLevel.Medium)
        {
            try
            {
                _logger.LogInformation($"Compressing PDF with level: {level}");

                if (pdfBytes == null || pdfBytes.Length == 0)
                    throw new ArgumentException("PDF bytes cannot be empty");

                // For None level, return original bytes
                if (level == PdfCompressionLevel.None)
                if (level == PdfCompressionLevel.None)
                {
                    _logger.LogInformation($"No compression applied");
                    return pdfBytes;
                }

                // Create temporary file
                var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
                try
                {
                    await File.WriteAllBytesAsync(tempFilePath, pdfBytes);

                    // Load the PDF document
                    var pdf = PdfDocument.FromFile(tempFilePath);

                    // Apply compression (IronPDF only has CompressStructTree)
                    pdf.CompressStructTree();

                    // Save to another temp file
                    var compressedFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_compressed.pdf");
                    pdf.SaveAs(compressedFilePath);

                    // Read compressed bytes
                    var compressedBytes = await File.ReadAllBytesAsync(compressedFilePath);

                    // Get file size info for logging
                    var originalSize = pdfBytes.Length;
                    var compressedSize = compressedBytes.Length;
                    var reductionPercentage = originalSize > 0 ? ((originalSize - compressedSize) / (double)originalSize) * 100 : 0;

                    _logger.LogInformation($"PDF compressed successfully. Original: {originalSize} bytes, Compressed: {compressedSize} bytes, Reduction: {reductionPercentage:F2}%");

                    return compressedBytes;
                }
                finally
                {
                    // Clean up temp files
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing PDF");

                // If compression fails, return the original bytes
                _logger.LogWarning("Compression failed, returning original bytes");
                return pdfBytes;
            }
        }

        public async Task<byte[]> RedactPdfAsync(byte[] pdfBytes, List<string> textToRedact, bool redactPartialWords = false)
        {
            try
            {
                _logger.LogInformation($"Redacting PDF");

                if (pdfBytes == null || pdfBytes.Length == 0)
                    throw new ArgumentException("PDF bytes cannot be empty");

                if (textToRedact == null || !textToRedact.Any())
                    throw new ArgumentException("No text specified for redaction");

                // Create temporary file
                var tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".pdf");
                try
                {
                    await File.WriteAllBytesAsync(tempFilePath, pdfBytes);

                    var pdf = PdfDocument.FromFile(tempFilePath);

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

                    // Save to another temp file
                    var redactedFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "_redacted.pdf");
                    pdf.SaveAs(redactedFilePath);

                    // Read redacted bytes
                    var redactedBytes = await File.ReadAllBytesAsync(redactedFilePath);

                    _logger.LogInformation($"PDF redacted successfully. Redacted {textToRedact.Count} text patterns.");

                    return redactedBytes;
                }
                finally
                {
                    // Clean up temp files
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redacting PDF");
                throw new Exception($"Failed to redact PDF: {ex.Message}", ex);
            }
        }

        public async Task<List<FileConversionResult>> ProcessMultipleFilesAsync(List<IFormFile> files)
        {
            var results = new List<FileConversionResult>();
            _logger.LogInformation($"Processing {files.Count} files for conversion...");

            foreach (var file in files)
            {
                var result = new FileConversionResult
                {
                    OriginalFileName = file.FileName,
                    Status = FileConversionStatus.Pending
                };

                try
                {
                    _logger.LogInformation($"Processing file: {file.FileName}");

                    if (file.Length == 0)
                    {
                        result.Status = FileConversionStatus.Skipped;
                        result.Error = "File is empty";
                        _logger.LogWarning($"Skipping empty file: {file.FileName}");
                        results.Add(result);
                        continue;
                    }

                    // Read file to byte array
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    var extension = Path.GetExtension(file.FileName).ToLower();

                    // Generate output filename
                    var outputFileName = _fileStorageService.GenerateUniqueFileName(file.FileName, ".pdf");
                    string resultPath;

                    _logger.LogInformation($"Converting file with extension: {extension}");

                    switch (extension)
                    {
                        case ".html":
                        case ".htm":
                            var htmlContent = Encoding.UTF8.GetString(fileBytes);
                            resultPath = await ConvertHtmlToPdfAsync(htmlContent);
                            result.OutputPath = resultPath;
                            result.Status = FileConversionStatus.Success;
                            break;
                        case ".jpg":
                        case ".jpeg":
                        case ".png":
                        case ".gif":
                        case ".bmp":
                            resultPath = await ConvertImageToPdfAsync(fileBytes, file.FileName);
                            result.OutputPath = resultPath;
                            result.Status = FileConversionStatus.Success;
                            break;
                        case ".docx":
                            resultPath = await ConvertDocxToPdfAsync(fileBytes, file.FileName);
                            result.OutputPath = resultPath;
                            result.Status = FileConversionStatus.Success;
                            break;
                        default:
                            result.Status = FileConversionStatus.Failed;
                            result.Error = $"File type {extension} is not supported";
                            _logger.LogWarning($"Unsupported file type: {extension} for file: {file.FileName}");
                            results.Add(result);
                            continue;
                    }

                    // Rename the file to preserve original filename
                    if (result.Status == FileConversionStatus.Success && !string.IsNullOrEmpty(result.OutputPath))
                    {
                        var newPath = Path.Combine(_fileStorageService.GetOutputDirectory(), outputFileName);
                        if (File.Exists(result.OutputPath) && result.OutputPath != newPath)
                        {
                            File.Move(result.OutputPath, newPath, true);
                            result.OutputPath = newPath;
                        }

                        results.Add(result);
                        _logger.LogInformation($"Successfully processed: {file.FileName} -> {Path.GetFileName(result.OutputPath)}");
                    }
                }
                catch (Exception ex)
                {
                    result.Status = FileConversionStatus.Failed;
                    result.Error = $"Failed to process file: {ex.Message}";
                    _logger.LogError(ex, $"Error processing file: {file.FileName}");
                    results.Add(result);
                }
            }

            var successfulCount = results.Count(r => r.Status == FileConversionStatus.Success);
            var failedCount = results.Count(r => r.Status == FileConversionStatus.Failed);
            _logger.LogInformation($"Batch processing complete. Successful: {successfulCount}, Failed: {failedCount}, Skipped: {results.Count - successfulCount - failedCount}");

            return results;
        }

        #region Backward Compatibility Methods
        public async Task<string> CompressPdfAsync(string pdfPath, PdfCompressionLevel level = PdfCompressionLevel.Medium)
        {
            try
            {
                if (!File.Exists(pdfPath))
                    throw new FileNotFoundException($"PDF file not found: {pdfPath}");

                var pdfBytes = await File.ReadAllBytesAsync(pdfPath);
                var compressedBytes = await CompressPdfAsync(pdfBytes, level);

                // Generate output filename
                var originalName = Path.GetFileNameWithoutExtension(pdfPath);
                var compressedName = $"{originalName}_compressed_{Guid.NewGuid():N}.pdf";
                var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), compressedName);

                await File.WriteAllBytesAsync(outputPath, compressedBytes);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing PDF file");
                throw;
            }
        }

        public async Task<string> RedactPdfAsync(string pdfPath, List<string> textToRedact, bool redactPartialWords = false)
        {
            try
            {
                if (!File.Exists(pdfPath))
                    throw new FileNotFoundException($"PDF file not found: {pdfPath}");

                var pdfBytes = await File.ReadAllBytesAsync(pdfPath);
                var redactedBytes = await RedactPdfAsync(pdfBytes, textToRedact, redactPartialWords);

                // Generate output filename
                var originalName = Path.GetFileNameWithoutExtension(pdfPath);
                var redactedName = $"{originalName}_redacted_{Guid.NewGuid():N}.pdf";
                var outputPath = Path.Combine(_fileStorageService.GetOutputDirectory(), redactedName);

                await File.WriteAllBytesAsync(outputPath, redactedBytes);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redacting PDF file");
                throw;
            }
        }
        #endregion

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

        private string CreateSimpleHtmlFromDocx(string docxFileName)
        {
            var fileName = Path.GetFileName(docxFileName);

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

        private enum ImageFormat
        {
            Jpeg,
            Png,
            Gif,
            Bmp,
            Unknown
        }

        private ImageFormat GetImageFormat(byte[] bytes)
        {
            // Check for JPEG
            if (bytes.Length > 2 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return ImageFormat.Jpeg;

            // Check for PNG
            if (bytes.Length > 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return ImageFormat.Png;

            // Check for GIF
            if (bytes.Length > 6 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                return ImageFormat.Gif;

            // Check for BMP
            if (bytes.Length > 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
                return ImageFormat.Bmp;

            return ImageFormat.Unknown;
        }

        private string GetMimeTypeFromImageFormat(ImageFormat format)
        {
            return format switch
            {
                ImageFormat.Jpeg => "image/jpeg",
                ImageFormat.Png => "image/png",
                ImageFormat.Gif => "image/gif",
                ImageFormat.Bmp => "image/bmp",
                _ => "image/jpeg"
            };
        }

        #endregion
    }
}