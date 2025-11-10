using System.IO;
using System.Text.RegularExpressions;
using PdfGeneratorDemo.Models;

namespace PdfGeneratorDemo.Services
{
    public class TemplateAnalyzer
    {
        private static readonly Regex PlaceholderRegex =
            new(@"\{\{([a-zA-Z0-9_]+)\}\}", RegexOptions.Compiled);

        public TemplateInfo AnalyzeTemplate(string templatePath)
        {
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template not found: {templatePath}");

            string content = File.ReadAllText(templatePath);
            var info = new TemplateInfo { TemplateContent = content };

            var matches = PlaceholderRegex.Matches(content);
            foreach (Match m in matches)
            {
                string field = m.Groups[1].Value;
                if (field != "GenerationTime")
                    info.RequiredFields.Add(field);
            }

            return info;
        }
    }
}
