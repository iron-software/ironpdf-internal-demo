using System.ComponentModel.DataAnnotations;

namespace IronPdfDemo.Models.Requests;

public class HtmlConversionRequest
{
    [Required] public string HtmlContent { get; set; } = string.Empty;
    public string PaperSize { get; set; } = "A4";
    public string Orientation { get; set; } = "Portrait";
    public int MarginTopMm { get; set; } = 15;
    public int MarginBottomMm { get; set; } = 15;
    public int MarginLeftMm { get; set; } = 15;
    public int MarginRightMm { get; set; } = 15;
    public bool EnableJs { get; set; } = false;
    public string CssMediaType { get; set; } = "Print";
    public int JsWaitMs { get; set; } = 0;
    public string? OutputFileName { get; set; }
}

public class UrlConversionRequest
{
    [Required, Url] public string Url { get; set; } = string.Empty;
    public string PaperSize { get; set; } = "A4";
    public string Orientation { get; set; } = "Portrait";
    public bool EnableJs { get; set; } = true;
    public int JsWaitMs { get; set; } = 500;
    public string? OutputFileName { get; set; }
}
