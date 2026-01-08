using IronPdfConverter.Models;
using Microsoft.AspNetCore.Http;

namespace IronPdfConverter.Services.Interfaces
{
    public interface IPdfConversionService
    {
        Task<string> ConvertHtmlToPdfAsync(string htmlContent);
        Task<string> ConvertUrlToPdfAsync(string url);
        Task<string> ConvertImageToPdfAsync(byte[] imageBytes, string fileName = null);
        Task<string> ConvertDocxToPdfAsync(byte[] docxBytes, string fileName = null);
        Task<string> MergePdfsAsync(List<byte[]> pdfBytesArray);
        Task<List<FileConversionResult>> ProcessMultipleFilesAsync(List<IFormFile> files);
        Task<byte[]> CompressPdfAsync(byte[] pdfBytes, PdfCompressionLevel level = PdfCompressionLevel.Medium);
        Task<byte[]> RedactPdfAsync(byte[] pdfBytes, List<string> textToRedact, bool redactPartialWords = false);

        // Helper methods for backward compatibility
        Task<string> CompressPdfAsync(string pdfPath, PdfCompressionLevel level = PdfCompressionLevel.Medium);
        Task<string> RedactPdfAsync(string pdfPath, List<string> textToRedact, bool redactPartialWords = false);
    }

    // Renamed to avoid conflict with System.IO.Compression.CompressionLevel
    public enum PdfCompressionLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        High = 3,
        Maximum = 4
    }
}