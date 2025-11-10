using IronPdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfGeneratorDemo.Models;
using PdfGeneratorDemo.Utilities;

namespace PdfGeneratorDemo.Services
{
    public class DynamicDocumentGenerator
    {
        private readonly TemplateInfo _template;
        private readonly int _maxThreads;
        private readonly int _progressInterval;

        private static readonly ThreadLocal<ChromePdfRenderer> _renderer = new(() =>
        {
            var renderer = new ChromePdfRenderer();
            renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print;
            renderer.RenderingOptions.EnableJavaScript = false;
            return renderer;
        });

        public DynamicDocumentGenerator(TemplateInfo template, int maxThreads, int progressInterval)
        {
            _template = template;
            _maxThreads = maxThreads;
            _progressInterval = progressInterval;
            _ = _renderer.Value; // warmup
        }

        public async Task GenerateDocumentsInBulk(List<DataRecord> records, string outputDir, CancellationToken ct = default)
        {
            Directory.CreateDirectory(outputDir);
            int total = records.Count;
            int done = 0;
            var start = DateTime.Now;

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _maxThreads,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(records, options, async (record, token) =>
            {
                await GenerateSingle(record, outputDir).ConfigureAwait(false);
                int current = Interlocked.Increment(ref done);
                if (current % _progressInterval == 0 || current == total)
                {
                    var elapsed = DateTime.Now - start;
                    Console.WriteLine($"Progress: {current}/{total} (Elapsed: {elapsed:mm\\:ss})");
                }
            });
        }

        private async Task GenerateSingle(DataRecord record, string outputDir)
        {
            try
            {
                string html = _template.TemplateContent;

                foreach (var field in _template.RequiredFields)
                    html = html.Replace($"{{{{{field}}}}}", HtmlHelper.EscapeHtml(record[field]));

                html = html.Replace("{{GenerationTime}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                var pdf = await _renderer.Value.RenderHtmlAsPdfAsync(html).ConfigureAwait(false);

                string fileName = FileNameGenerator.Generate(record, 0);
                var fullPath = Path.Combine(outputDir, fileName);

                pdf.SaveAs(fullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating document: {ex.Message}");
            }
        }
    }
}
