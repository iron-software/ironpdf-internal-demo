using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public async Task<GenerationResult> ExecuteAsync(TemplateSelection templateSelection, InputType inputType, PaperOrientation orientation)
        {
            var result = new GenerationResult();
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Discovering templates...");
            var allTemplates = _templateManager.DiscoverTemplates();

            if (allTemplates.Count == 0)
            {
                Console.WriteLine("No HTML templates found in the Templates folder.");
                stopwatch.Stop();
                result.TotalDuration = stopwatch.Elapsed;
                return result;
            }

            // Filter templates based on selection
            var templatesToProcess = FilterTemplates(allTemplates, templateSelection);

            if (templatesToProcess.Count == 0)
            {
                Console.WriteLine($"No templates found matching the selection: {templateSelection}");
                stopwatch.Stop();
                result.TotalDuration = stopwatch.Elapsed;
                return result;
            }

            Console.WriteLine($"\nGeneration Settings:");
            Console.WriteLine($"- Template Selection: {templateSelection}");
            Console.WriteLine($"- Input Type: {inputType}");
            Console.WriteLine($"- Orientation: {orientation}");
            Console.WriteLine($"- Templates to Process: {templatesToProcess.Count}");
            Console.WriteLine("Starting PDF generation...\n");

            foreach (var template in templatesToProcess)
            {
                var templateResult = new TemplateResult { TemplateName = template.Name };
                try
                {
                    Console.WriteLine($"Processing: {template.Name}");
                    var templateInfo = _templateAnalyzer.AnalyzeTemplate(template.TemplatePath);
                    string outputFile = Path.Combine(_config.OutputFolder, $"{template.Name}.pdf");

                    // Create PDF options based on user selection
                    var pdfOptions = CreatePdfOptions(inputType, orientation);

                    var success = await _pdfService.GeneratePdfAsync(templateInfo.TemplateContent, outputFile, pdfOptions);

                    if (success)
                    {
                        templateResult.Success = true;
                        templateResult.OutputPath = outputFile;
                        Console.WriteLine($"✓ Generated: {outputFile}");
                    }
                    else
                    {
                        templateResult.Success = false;
                        templateResult.ErrorMessage = "PDF generation failed (see error log)";
                        Console.WriteLine($"✗ Failed to generate: {template.Name}");
                    }
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

        private List<TemplateInfo> FilterTemplates(List<TemplateInfo> allTemplates, TemplateSelection selection)
        {
            return selection switch
            {
                TemplateSelection.PlainHtml => allTemplates.Where(t => t.Name.Contains("plain", StringComparison.OrdinalIgnoreCase)).ToList(),
                TemplateSelection.HtmlWithCss => allTemplates.Where(t => t.Name.Contains("css", StringComparison.OrdinalIgnoreCase)).ToList(),
                TemplateSelection.HtmlWithJs => allTemplates.Where(t => t.Name.Contains("js", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("javascript", StringComparison.OrdinalIgnoreCase)).ToList(),
                TemplateSelection.AllTemplates => allTemplates,
                _ => allTemplates
            };
        }

        private PdfGenerationOptions CreatePdfOptions(InputType inputType, PaperOrientation orientation)
        {
            var options = inputType switch
            {
                InputType.PlainHtml => PdfGenerationOptions.PlainHtml,
                InputType.HtmlWithCss => PdfGenerationOptions.HtmlWithCss,
                InputType.HtmlWithJs => PdfGenerationOptions.HtmlWithJs,
                _ => PdfGenerationOptions.PlainHtml
            };

            options.PaperOrientation = orientation switch
            {
                PaperOrientation.Landscape => IronPdf.Rendering.PdfPaperOrientation.Landscape,
                PaperOrientation.Portrait => IronPdf.Rendering.PdfPaperOrientation.Portrait,
                _ => IronPdf.Rendering.PdfPaperOrientation.Portrait
            };

            return options;
        }
    }
}