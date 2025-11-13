using System;
using System.IO;
using System.Text.Json;
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
            var licenseKey = LoadLicenseKeyFromAppSettings();
            if (!string.IsNullOrEmpty(licenseKey))
            {
                IronPdf.License.LicenseKey = licenseKey;
            }

            try
            {
                var baseDir = AppContext.BaseDirectory;
                var projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\"));

                var config = new GeneratorConfiguration
                {
                    TemplatesFolder = Path.Combine(projectRoot, "Templates"),
                    OutputFolder = Path.Combine(projectRoot, "Output")
                };

                var templateSelection = DisplayTemplateSelectionMenu();
                var inputType = DisplayInputTypeMenu();
                var orientation = DisplayOrientationMenu();

                var controller = new DocumentGenerationController(config);
                var result = await controller.ExecuteAsync(templateSelection, inputType, orientation);

                DisplaySummary(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static TemplateSelection DisplayTemplateSelectionMenu()
        {
            Console.WriteLine("==================================");
            Console.WriteLine("     PDF GENERATOR DEMO");
            Console.WriteLine("==================================");
            Console.WriteLine("Select templates to process:");
            Console.WriteLine("1. Plain HTML Template");
            Console.WriteLine("2. HTML with CSS Template");
            Console.WriteLine("3. HTML with JavaScript Template");
            Console.WriteLine("4. All Templates in Folder");
            Console.WriteLine("==================================");

            while (true)
            {
                Console.Write("Enter your choice (1-4): ");
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        return TemplateSelection.PlainHtml;
                    case "2":
                        return TemplateSelection.HtmlWithCss;
                    case "3":
                        return TemplateSelection.HtmlWithJs;
                    case "4":
                        return TemplateSelection.AllTemplates;
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1, 2, 3, or 4.");
                        break;
                }
            }
        }

        static InputType DisplayInputTypeMenu()
        {
            Console.WriteLine("\nSelect input type:");
            Console.WriteLine("1. Plain HTML");
            Console.WriteLine("2. HTML with CSS");
            Console.WriteLine("3. HTML with JavaScript");
            Console.WriteLine("==================================");

            while (true)
            {
                Console.Write("Enter your choice (1-3): ");
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        return InputType.PlainHtml;
                    case "2":
                        return InputType.HtmlWithCss;
                    case "3":
                        return InputType.HtmlWithJs;
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1, 2, or 3.");
                        break;
                }
            }
        }

        static PaperOrientation DisplayOrientationMenu()
        {
            Console.WriteLine("\nSelect paper orientation:");
            Console.WriteLine("1. Portrait");
            Console.WriteLine("2. Landscape");
            Console.WriteLine("==================================");

            while (true)
            {
                Console.Write("Enter your choice (1-2): ");
                var input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        return PaperOrientation.Portrait;
                    case "2":
                        return PaperOrientation.Landscape;
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1 or 2.");
                        break;
                }
            }
        }

        static string LoadLicenseKeyFromAppSettings()
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\"));
                var configFile = Path.Combine(projectRoot, "appsettings.json");

                if (File.Exists(configFile))
                {
                    var jsonContent = File.ReadAllText(configFile);
                    using JsonDocument doc = JsonDocument.Parse(jsonContent);

                    if (doc.RootElement.TryGetProperty("IronPdf", out var ironPdfElement) &&
                        ironPdfElement.TryGetProperty("LicenseKey", out var licenseKeyElement))
                    {
                        return licenseKeyElement.GetString();
                    }

                    if (doc.RootElement.TryGetProperty("IronPdfLicenseKey", out var directLicenseKeyElement))
                    {
                        return directLicenseKeyElement.GetString();
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
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
            Console.WriteLine("==============================");
        }
    }

    public enum TemplateSelection
    {
        PlainHtml,
        HtmlWithCss,
        HtmlWithJs,
        AllTemplates
    }

    public enum InputType
    {
        PlainHtml,
        HtmlWithCss,
        HtmlWithJs
    }

    public enum PaperOrientation
    {
        Portrait,
        Landscape
    }
}