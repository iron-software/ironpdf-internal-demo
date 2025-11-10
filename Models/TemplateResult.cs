using System;
using System.Collections.Generic;

namespace PdfGeneratorDemo.Models
{
    public class TemplateResult
    {
        public string TemplateName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> RequiredFields { get; set; } = new();
        public int TotalRecords { get; set; }
        public int DocumentsGenerated { get; set; }
        public string OutputPath { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }
}
