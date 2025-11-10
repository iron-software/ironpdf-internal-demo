using System.IO;
using PdfGeneratorDemo.Models;

namespace PdfGeneratorDemo.Services
{
    public class TemplateAnalyzer
    {
        public TemplateInfo AnalyzeTemplate(string templatePath)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template not found: {templatePath}");

            return new TemplateInfo { TemplateContent = File.ReadAllText(templatePath) };
        }
    }
}
