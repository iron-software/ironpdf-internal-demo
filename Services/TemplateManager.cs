using System.Collections.Generic;
using System.IO;
using System.Linq;
using PdfGeneratorDemo.Models;

namespace PdfGeneratorDemo.Services
{
    public class TemplateManager
    {
        private readonly string _templatesFolder;

        public TemplateManager(string templatesFolder)
        {
            _templatesFolder = templatesFolder;
        }

        public List<TemplateInfo> DiscoverTemplates()
        {
            var templates = new List<TemplateInfo>();

            if (!Directory.Exists(_templatesFolder))
                return templates;

            var htmlFiles = Directory.GetFiles(_templatesFolder, "*.html")
                                  .Concat(Directory.GetFiles(_templatesFolder, "*.htm"));

            foreach (var file in htmlFiles)
            {
                templates.Add(new TemplateInfo
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    TemplatePath = file
                });
            }

            return templates;
        }
    }
}