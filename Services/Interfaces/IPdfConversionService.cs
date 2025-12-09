public interface IPdfConversionService
{
    Task<string> ConvertHtmlToPdfAsync(string htmlContent);
    Task<string> ConvertUrlToPdfAsync(string url);
    Task<string> ConvertImageToPdfAsync(string imagePath);
    Task<string> ConvertDocxToPdfAsync(string docxPath);
    Task<string> MergePdfsAsync(List<string> pdfPaths);
    Task<List<string>> ProcessMultipleFilesAsync(List<IFormFile> files);
    Task<string> CompressPdfAsync(string pdfPath, CompressionLevel level = CompressionLevel.Medium);

    // Add redaction method
    Task<string> RedactPdfAsync(string pdfPath, List<string> textToRedact, bool redactPartialWords = false);
}

public enum CompressionLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Maximum = 4
}