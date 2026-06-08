namespace IronPdfDemo.Models.Responses;

public class PdfFileResponse
{
    public string FileName { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int PageCount { get; set; }
    public long ElapsedMs { get; set; }
    public string OperationId { get; set; } = string.Empty;
}
