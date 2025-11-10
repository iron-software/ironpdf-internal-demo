using System;
using System.Collections.Generic;

namespace PdfGeneratorDemo.Models
{
    public class GenerationResult
    {
        public List<TemplateResult> TemplateResults { get; set; } = new();
        public TimeSpan TotalDuration { get; set; }
    }
}
