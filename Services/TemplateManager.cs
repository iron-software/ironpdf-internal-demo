using System;
using System.Collections.Generic;
using System.IO;
using PdfGeneratorDemo.Models;

namespace PdfGeneratorDemo.Services
{
    public class TemplateManager
    {
        private readonly string _templatesFolder;

        public TemplateManager(string templatesFolder)
        {
            _templatesFolder = templatesFolder;
            Directory.CreateDirectory(_templatesFolder);
        }

        public List<TemplateConfiguration> DiscoverTemplates()
        {
            var templates = new List<TemplateConfiguration>();
            var htmlFiles = Directory.GetFiles(_templatesFolder, "*.html");

            foreach (var file in htmlFiles)
            {
                templates.Add(new TemplateConfiguration
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    TemplatePath = file
                });
            }

            return templates;
        }
    }
}
