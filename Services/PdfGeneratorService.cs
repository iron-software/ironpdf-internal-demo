using System.IO;
using System.Threading.Tasks;
using IronPdf;

namespace PdfGeneratorDemo.Services
{
    public class PdfGeneratorService
    {
        public async Task GeneratePdfAsync(string htmlContent, string outputPath)
        {
            var renderer = new ChromePdfRenderer
            {
                RenderingOptions = { CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print }
            };

            var pdf = await renderer.RenderHtmlAsPdfAsync(htmlContent);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            pdf.SaveAs(outputPath);
        }
    }
}
