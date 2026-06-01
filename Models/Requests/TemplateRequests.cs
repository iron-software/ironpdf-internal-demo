using System.ComponentModel.DataAnnotations;

namespace IronPdfDemo.Models.Requests;

public class GenerateTemplateRequest
{
    [Required] public string TemplateName { get; set; } = string.Empty;
    public Dictionary<string, string> Variables { get; set; } = new();
    public string PaperSize { get; set; } = "A4";
    public string Orientation { get; set; } = "Portrait";
    public bool EnableJs { get; set; } = true;
    public bool AddWatermark { get; set; } = false;
    public string WatermarkText { get; set; } = "SAMPLE";
    public bool AddHeaderFooter { get; set; } = false;
    public bool PasswordProtect { get; set; } = false;
    public string? Password { get; set; }
}
