namespace IronPdfDemo.Models.Responses;

public class BatchResultResponse
{
    public string JobId { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<BatchFileResult> Results { get; set; } = new();
    public long TotalElapsedMs { get; set; }
}

public class BatchFileResult
{
    public string OriginalFileName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? DownloadUrl { get; set; }
    public string? Error { get; set; }
    public long ElapsedMs { get; set; }
}
