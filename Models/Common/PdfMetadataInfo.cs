namespace IronPdfDemo.Models.Common;

public class PdfMetadataInfo
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Subject { get; set; }
    public string? Keywords { get; set; }
    public string? Creator { get; set; }
    public string? Producer { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int PageCount { get; set; }
    public bool IsEncrypted { get; set; }
    public bool HasSignature { get; set; }
    public string? PdfVersion { get; set; }
    public long FileSizeBytes { get; set; }
}
