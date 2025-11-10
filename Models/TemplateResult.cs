using System;

namespace PdfGeneratorDemo.Models
{
    public class TemplateResult
    {
        public string TemplateName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
    }
}
