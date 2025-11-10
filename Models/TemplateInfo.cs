using System.Collections.Generic;

namespace PdfGeneratorDemo.Models
{
    public class TemplateInfo
    {
        public string TemplateContent { get; set; } = string.Empty;
        public HashSet<string> RequiredFields { get; set; } = new HashSet<string>();
    }
}
