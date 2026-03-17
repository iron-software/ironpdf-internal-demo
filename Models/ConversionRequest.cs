using Microsoft.AspNetCore.Http;
using IronPdfConverter.Services.Interfaces; // Add this using

namespace IronPdfConverter.Models
{
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

    // New request models for byte array support
    public class HtmlConversionRequest
    {
        public string HtmlContent { get; set; }
        public List<string> TextToRedact { get; set; } = new List<string>();
        public bool RedactPartialWords { get; set; }
    }

    public class FileConversionRequest
    {
        public IFormFile File { get; set; }
        public List<string> TextToRedact { get; set; } = new List<string>();
        public bool RedactPartialWords { get; set; }
    }

    public class MergePdfRequest
    {
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
        public List<string> TextToRedact { get; set; } = new List<string>();
        public bool RedactPartialWords { get; set; }
    }

    public class RedactionRequest
    {
        public IFormFile File { get; set; }
        public List<string> TextToRedact { get; set; } = new List<string>();
        public bool RedactPartialWords { get; set; }
    }

    public class CompressionRequest
    {
        public IFormFile File { get; set; }
        public PdfCompressionLevel Level { get; set; } = PdfCompressionLevel.Medium; // Use renamed enum
    }
}