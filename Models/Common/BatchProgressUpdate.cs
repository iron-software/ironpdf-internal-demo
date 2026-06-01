namespace IronPdfDemo.Models.Common;

public class BatchProgressUpdate
{
    public string JobId { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int Processed { get; set; }
    public int Failed { get; set; }
    public string CurrentItem { get; set; } = string.Empty;
    public string Status { get; set; } = "processing";
    public double ProgressPct => TotalItems > 0 ? (double)Processed / TotalItems * 100 : 0;
    public string? ErrorDetail { get; set; }
    public List<BatchItemResult> Results { get; set; } = new();
}

public class BatchItemResult
{
    public string FileName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? DownloadUrl { get; set; }
    public string? Error { get; set; }
}
