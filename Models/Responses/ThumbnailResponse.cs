namespace IronPdfDemo.Models.Responses;

public class ThumbnailResponse
{
    public string Base64Image { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int WidthPx { get; set; }
    public int HeightPx { get; set; }
}
