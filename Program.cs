using IronPdf;
using System;
using System.Linq;
using System.Threading.Tasks;
using PdfGeneratorDemo.Configuration;
using PdfGeneratorDemo.Controllers;
using PdfGeneratorDemo.Models;

namespace PdfGeneratorDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IronPdf.License.LicenseKey = ""; 

            try
            {
                var config = new GeneratorConfiguration
                {
                    TemplatesFolder = "Templates",
                    DataFolder = "Data",
                    OutputBaseFolder = "Output",
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    ShowProgressInterval = 20
                };

                var controller = new DocumentGenerationController(config);
                var result = await controller.ExecuteAsync();
                DisplaySummary(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        static void DisplaySummary(GenerationResult result)
        {
            Console.WriteLine($"\n{'='}{new string('=', 70)}");
            Console.WriteLine("GENERATION SUMMARY");
            Console.WriteLine($"{'='}{new string('=', 70)}");
            Console.WriteLine($"Total Templates Processed: {result.TemplateResults.Count}");
            Console.WriteLine($"Total Documents Generated: {result.TotalDocumentsGenerated}");
            Console.WriteLine($"Total Duration: {result.TotalDuration:hh\\:mm\\:ss}");
            Console.WriteLine($"Success Rate: {result.SuccessRate:P1}");

            if (result.TemplateResults.Any(r => !r.Success))
            {
                Console.WriteLine("\nFailed Templates:");
                foreach (var failed in result.TemplateResults.Where(r => !r.Success))
                {
                    Console.WriteLine($"  - {failed.TemplateName}: {failed.ErrorMessage}");
                }
            }

            Console.WriteLine($"{'='}{new string('=', 70)}\n");
        }
    }
}
