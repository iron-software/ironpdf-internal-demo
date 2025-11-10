using System;
using System.Collections.Generic;
using System.IO;
using PdfGeneratorDemo.Models;

namespace PdfGeneratorDemo.Services
{
    public class TemplateManager
    {
        private readonly string _templatesFolder;
        private readonly string _dataFolder;

        public TemplateManager(string templatesFolder, string dataFolder)
        {
            _templatesFolder = templatesFolder;
            _dataFolder = dataFolder;

            Directory.CreateDirectory(_templatesFolder);
            Directory.CreateDirectory(_dataFolder);
        }

        public List<TemplateConfiguration> DiscoverTemplates()
        {
            var templates = new List<TemplateConfiguration>();

            if (!Directory.Exists(_templatesFolder))
                return templates;

            var htmlFiles = Directory.GetFiles(_templatesFolder, "*.html");
            foreach (var templatePath in htmlFiles)
            {
                string name = Path.GetFileNameWithoutExtension(templatePath);
                string csvPath = Path.Combine(_dataFolder, $"{name}.csv");

                if (File.Exists(csvPath))
                {
                    templates.Add(new TemplateConfiguration
                    {
                        Name = name,
                        TemplatePath = templatePath,
                        DataPath = csvPath
                    });
                }
                else
                {
                    Console.WriteLine($"Warning: Template '{name}.html' found but no matching '{name}.csv'");
                }
            }

            return templates;
        }
    }
}
