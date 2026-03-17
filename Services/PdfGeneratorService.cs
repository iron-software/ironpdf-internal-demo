using System;
using System.IO;
using System.Threading.Tasks;
using IronPdf;

namespace PdfGeneratorDemo.Services
{
    public class PdfGeneratorService
    {
        public async Task<bool> GeneratePdfAsync(string htmlContent, string outputPath, PdfGenerationOptions options = null)
        {
            try
            {
                var renderer = new ChromePdfRenderer();

                // Apply rendering options
                ApplyRenderingOptions(renderer, options);

                var pdf = await renderer.RenderHtmlAsPdfAsync(htmlContent);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                pdf.SaveAs(outputPath);
                return true;
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - let the caller handle the failure
                Console.WriteLine($"Error generating PDF {outputPath}: {ex.Message}");
                return false;
            }
        }

        private void ApplyRenderingOptions(ChromePdfRenderer renderer, PdfGenerationOptions options)
        {
            if (options == null) return;

            // JavaScript handling
            renderer.RenderingOptions.EnableJavaScript = options.EnableJavaScript;
            if (options.EnableJavaScript)
            {
                renderer.RenderingOptions.WaitFor.JavaScript();
            }

            // CSS media type
            renderer.RenderingOptions.CssMediaType = options.CssMediaType;

            // Paper orientation
            renderer.RenderingOptions.PaperOrientation = options.PaperOrientation;


        }
    }

    public class PdfGenerationOptions
    {
        public bool EnableJavaScript { get; set; } = false;
        public IronPdf.Rendering.PdfCssMediaType CssMediaType { get; set; } = IronPdf.Rendering.PdfCssMediaType.Print;
        public IronPdf.Rendering.PdfPaperOrientation PaperOrientation { get; set; } = IronPdf.Rendering.PdfPaperOrientation.Portrait;

        // Preset configurations for common use cases
        public static PdfGenerationOptions PlainHtml => new PdfGenerationOptions
        {
            EnableJavaScript = false,
            CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print,
            PaperOrientation = IronPdf.Rendering.PdfPaperOrientation.Portrait
        };

        public static PdfGenerationOptions HtmlWithCss => new PdfGenerationOptions
        {
            EnableJavaScript = false,
            CssMediaType = IronPdf.Rendering.PdfCssMediaType.Screen,
            PaperOrientation = IronPdf.Rendering.PdfPaperOrientation.Portrait
        };

        public static PdfGenerationOptions HtmlWithJs => new PdfGenerationOptions
        {
            EnableJavaScript = true,
            CssMediaType = IronPdf.Rendering.PdfCssMediaType.Screen,
            PaperOrientation = IronPdf.Rendering.PdfPaperOrientation.Portrait
        };
    }
}