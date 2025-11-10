using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
        private readonly DynamicCsvReader _csvReader;
        private readonly DataTemplateValidator _validator;

        public DocumentGenerationController(GeneratorConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _templateManager = new TemplateManager(_config.TemplatesFolder, _config.DataFolder);
            _templateAnalyzer = new TemplateAnalyzer();
            _csvReader = new DynamicCsvReader();
            _validator = new DataTemplateValidator();
        }

        public async Task<GenerationResult> ExecuteAsync(CancellationToken ct = default)
        {
            var result = new GenerationResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                Console.WriteLine("Phase 1: Discovering Templates...");
                var templates = _templateManager.DiscoverTemplates();

                if (templates.Count == 0)
                {
                    Console.WriteLine("No templates found.");
                    ShowExpectedStructure();
                    return result;
                }

                Console.WriteLine($"Found {templates.Count} template(s).\n");

                foreach (var template in templates)
                {
                    var templateResult = await ProcessTemplateAsync(template, ct);
                    result.TemplateResults.Add(templateResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Controller error: {ex.Message}");
            }

            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;
            return result;
        }

        private async Task<TemplateResult> ProcessTemplateAsync(TemplateConfiguration config, CancellationToken ct)
        {
            var result = new TemplateResult { TemplateName = config.Name };
            var stopwatch = Stopwatch.StartNew();

            try
            {
                Console.WriteLine($"\nProcessing: {config.Name}");

                var templateInfo = _templateAnalyzer.AnalyzeTemplate(config.TemplatePath);
                result.RequiredFields = templateInfo.RequiredFields.ToList();

                var records = _csvReader.ReadData(config.DataPath);
                result.TotalRecords = records.Count;

                var validation = _validator.Validate(records, templateInfo);
                result.ValidationErrors = validation.Errors;

                if (!validation.IsValid)
                {
                    result.Success = false;
                    result.ErrorMessage = "Validation failed.";
                    return result;
                }

                if (_config.ValidateOnly)
                {
                    result.Success = true;
                    return result;
                }

                string outputDir = Path.Combine(_config.OutputBaseFolder, config.Name);
                var generator = new DynamicDocumentGenerator(templateInfo, _config.MaxDegreeOfParallelism, _config.ShowProgressInterval);
                await generator.GenerateDocumentsInBulk(records, outputDir, ct);

                result.Success = true;
                result.DocumentsGenerated = records.Count;
                result.OutputPath = Path.GetFullPath(outputDir);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            return result;
        }

        private void ShowExpectedStructure()
        {
            Console.WriteLine("Expected folder structure:");
            Console.WriteLine($"  {_config.TemplatesFolder}/template1.html");
            Console.WriteLine($"  {_config.DataFolder}/template1.csv");
        }
    }
}
