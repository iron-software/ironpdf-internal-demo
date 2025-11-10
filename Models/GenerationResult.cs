using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGeneratorDemo.Models
{
    public class GenerationResult
    {
        public List<TemplateResult> TemplateResults { get; set; } = new();
        public TimeSpan TotalDuration { get; set; }
        public int TotalDocumentsGenerated => TemplateResults.Sum(r => r.DocumentsGenerated);
        public double SuccessRate => TemplateResults.Count == 0
            ? 0
            : (double)TemplateResults.Count(r => r.Success) / TemplateResults.Count;
    }
}
