namespace IronPdfConverter.Models
{
    public class FileConversionResult
    {
        public string OriginalFileName { get; set; }
        public string OutputPath { get; set; }
        public FileConversionStatus Status { get; set; }
        public string Error { get; set; }
    }

    public enum FileConversionStatus
    {
        Pending,
        Success,
        Failed,
        Skipped
    }
}