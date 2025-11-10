using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using PdfGeneratorDemo.Configuration;
using PdfGeneratorDemo.Models;
using PdfGeneratorDemo.Services;

namespace PdfGeneratorDemo.Controllers
{
    public class DocumentGenerationController
    {
        private readonly GeneratorConfiguration _config;
        private readonly TemplateManager _templateManager;
        private readonly TemplateAnalyzer _templateAnalyzer;
        private readonly PdfGeneratorService _pdfService;

        public DocumentGenerationController(GeneratorConfiguration config)
        {
            _config = config;
            _templateManager = new TemplateManager(_config.TemplatesFolder);
            _templateAnalyzer = new TemplateAnalyzer();
            _pdfService = new PdfGeneratorService();
        }

        public async Task<GenerationResult> ExecuteAsync()
        {
            var result = new GenerationResult();
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Discovering templates...");
            var templates = _templateManager.DiscoverTemplates();

            if (templates.Count == 0)
            {
                Console.WriteLine("No HTML templates found in the Templates folder.");
                stopwatch.Stop();
                result.TotalDuration = stopwatch.Elapsed;
                return result;
            }

            foreach (var template in templates)
            {
                var templateResult = new TemplateResult { TemplateName = template.Name };
                try
                {
                    Console.WriteLine($"\nProcessing: {template.Name}");
                    var templateInfo = _templateAnalyzer.AnalyzeTemplate(template.TemplatePath);
                    string outputFile = Path.Combine(_config.OutputFolder, $"{template.Name}.pdf");

                    await _pdfService.GeneratePdfAsync(templateInfo.TemplateContent, outputFile);
                    templateResult.Success = true;
                    templateResult.OutputPath = outputFile;
                    Console.WriteLine($"✓ Generated: {outputFile}");
                }
                catch (Exception ex)
                {
                    templateResult.Success = false;
                    templateResult.ErrorMessage = ex.Message;
                    Console.WriteLine($"✗ Error processing {template.Name}: {ex.Message}");
                }
                result.TemplateResults.Add(templateResult);
            }

            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;
            return result;
        }
    }
}
