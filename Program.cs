using System;
using System.IO;
using System.Threading.Tasks;
using PdfGeneratorDemo.Configuration;
using PdfGeneratorDemo.Controllers;
using PdfGeneratorDemo.Models;

namespace PdfGeneratorDemo
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            IronPdf.License.LicenseKey = ""; // PLACE KEY STRING

            try
            {
                var baseDir = AppContext.BaseDirectory;
                var projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\"));

                var config = new GeneratorConfiguration
                {
                    TemplatesFolder = Path.Combine(projectRoot, "Templates"),
                    OutputFolder = Path.Combine(projectRoot, "Output")
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
            Console.WriteLine("\n==============================");
            Console.WriteLine("PDF GENERATION SUMMARY");
            Console.WriteLine("==============================");
            Console.WriteLine($"Templates Processed: {result.TemplateResults.Count}");
            Console.WriteLine($"Success: {result.TemplateResults.FindAll(r => r.Success).Count}");
            Console.WriteLine($"Errors: {result.TemplateResults.FindAll(r => !r.Success).Count}");
            Console.WriteLine($"Total Duration: {result.TotalDuration:mm\\:ss}");
        }
    }
}
