namespace IronPdfDemo.Configuration;

public class IronPdfOptions
{
    public const string SectionName = "IronPdf";
    public string LicenseKey { get; set; } = string.Empty;
    public int DefaultTimeoutSeconds { get; set; } = 60;
    public bool EnableJavaScriptByDefault { get; set; } = true;
    public string DefaultPaperSize { get; set; } = "A4";
    public int DefaultMarginMm { get; set; } = 15;
}
