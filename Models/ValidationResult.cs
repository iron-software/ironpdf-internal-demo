using System.Collections.Generic;

namespace PdfGeneratorDemo.Models
{
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; set; } = new();
    }
}
