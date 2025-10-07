using IronPdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TicketBatchGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize IronPDF license if needed
            IronPdf.License.LicenseKey = "";


            // Path to your CSV file
            string csvFilePath = "flight_records.csv";
            string templateFilePath = "TicketTemplate.html";
            string outputDirectory = "OutputTickets";

            // Verify CSV file exists
            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine($"Error: CSV file not found at {csvFilePath}");
                return;
            }

            // Verify template file exists
            if (!File.Exists(templateFilePath))
            {
                Console.WriteLine($"Error: Template file not found at {templateFilePath}");
                return;
            }

            // Create and start stopwatch
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Starting ticket generation at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            try
            {
                // Step 1: Read passenger data from CSV
                var passengers = CsvDataReader.ReadPassengerData(csvFilePath);
                Console.WriteLine($"Loaded {passengers.Count} passenger records from {csvFilePath}");

                // Step 2: Generate tickets
                var generator = new TicketGenerator(templateFilePath);
                await generator.GenerateTicketsInBulk(passengers, outputDirectory);

                stopwatch.Stop();

                Console.WriteLine($"\nTicket generation completed!");
                Console.WriteLine($"Files saved to: {Path.GetFullPath(outputDirectory)}");
                Console.WriteLine($"Total processing time: {stopwatch.Elapsed:mm\\:ss\\.fff}");
                Console.WriteLine($"Average time per ticket: {stopwatch.Elapsed.TotalMilliseconds / passengers.Count:F0} ms");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"An error occurred after {stopwatch.Elapsed:mm\\:ss\\.fff}: {ex.Message}");
            }
        }
    }

    public static class ValidationHelper
    {
        public static bool AllPropertiesHaveValues(params string[] values)
        {
            return values.All(value => !string.IsNullOrWhiteSpace(value));
        }

        public static bool AnyPropertyMissing(params string[] values)
        {
            return values.Any(value => string.IsNullOrWhiteSpace(value));
        }
    }

    public class TicketGenerator
    {
        private int _processedCount = 0;
        private readonly object _lockObject = new object();
        private readonly string _htmlTemplate;

        private static readonly ThreadLocal<ChromePdfRenderer> _renderer = new ThreadLocal<ChromePdfRenderer>(() =>
        {
            var renderer = new ChromePdfRenderer();
            renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print;
            renderer.RenderingOptions.EnableJavaScript = false;
            return renderer;
        });

        public TicketGenerator(string templateFilePath)
        {
            // Load HTML template from file
            _htmlTemplate = File.ReadAllText(templateFilePath);
            Console.WriteLine($"Loaded HTML template from {templateFilePath}");

            // Pre-warm the renderer
            _ = _renderer.Value;
        }

        public async Task GenerateTicketsInBulk(List<PassengerTicketData> passengers, string outputDirectory, CancellationToken ct = default)
        {
            Directory.CreateDirectory(outputDirectory);
            _processedCount = 0;
            var totalCount = passengers.Count;
            var startTime = DateTime.Now;

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(passengers, options, async (passenger, cancellationToken) =>
            {
                await GenerateSingleTicketAsync(passenger, outputDirectory).ConfigureAwait(false);

                lock (_lockObject)
                {
                    _processedCount++;
                    if (_processedCount % 20 == 0 || _processedCount == totalCount)
                    {
                        var elapsed = DateTime.Now - startTime;
                        var remaining = totalCount > 0
                            ? TimeSpan.FromTicks(elapsed.Ticks * (totalCount - _processedCount) / _processedCount)
                            : TimeSpan.Zero;

                        Console.WriteLine($"Processed {_processedCount}/{totalCount} tickets " +
                                        $"(Elapsed: {elapsed:mm\\:ss}, " +
                                        $"ETA: {remaining:mm\\:ss})");
                    }
                }
            });
        }

        private async Task GenerateSingleTicketAsync(PassengerTicketData passenger, string outputDirectory)
        {
            try
            {
                if (!passenger.IsValid())
                {
                    Console.WriteLine($"Skipping invalid passenger data for {passenger.PassengerName ?? "(unknown)"}");
                    return;
                }

                string html = _htmlTemplate
                    .Replace("{{PassengerName}}", EscapeHtml(passenger.PassengerName))
                    .Replace("{{FlightNumber}}", EscapeHtml(passenger.FlightNumber))
                    .Replace("{{DepartureAirport}}", EscapeHtml(passenger.DepartureAirport))
                    .Replace("{{ArrivalAirport}}", EscapeHtml(passenger.ArrivalAirport))
                    .Replace("{{FlightDate}}", EscapeHtml(passenger.FlightDate))
                    .Replace("{{DepartureTime}}", EscapeHtml(passenger.DepartureTime))
                    .Replace("{{CabinClass}}", EscapeHtml(passenger.CabinClass))
                    .Replace("{{SeatNumber}}", EscapeHtml(passenger.SeatNumber))
                    .Replace("{{BarcodeData}}", EscapeHtml(passenger.BarcodeData))
                    .Replace("{{GenerationTime}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                var renderer = _renderer.Value;
                var pdf = await renderer.RenderHtmlAsPdfAsync(html).ConfigureAwait(false);

                var safeName = $"{EscapeFileName(passenger.PassengerName)}_{passenger.FlightNumber}_{passenger.SeatNumber}.pdf";
                var filePath = Path.Combine(outputDirectory, safeName);

                // Use synchronous SaveAs since IronPdf doesn't have SaveAsAsync
                pdf.SaveAs(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating ticket for {passenger?.PassengerName ?? "(unknown)"}: {ex.Message}");
            }
        }

        private static string EscapeFileName(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Concat(input.Select(c => invalidChars.Contains(c) ? '_' : c)).Replace(' ', '_');
        }

        private static string EscapeHtml(string input) =>
            input?
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;") ?? string.Empty;
    }

    public class CsvDataReader
    {
        public static List<PassengerTicketData> ReadPassengerData(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"CSV file not found: {filePath}");

            var passengers = new List<PassengerTicketData>();

            // Read all lines and skip the header
            var lines = File.ReadAllLines(filePath).Skip(1);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var values = ParseCsvLine(line);
                    if (values.Length < 9)
                        throw new FormatException("Line does not contain 9 fields.");

                    passengers.Add(new PassengerTicketData
                    {
                        PassengerName = values[0],
                        FlightNumber = values[1],
                        DepartureAirport = values[2],
                        ArrivalAirport = values[3],
                        FlightDate = values[4],
                        DepartureTime = values[5],
                        CabinClass = values[6],
                        SeatNumber = values[7],
                        BarcodeData = values[8]
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing line: {line}. Error: {ex.Message}");
                }
            }

            return passengers;
        }

        private static string[] ParseCsvLine(string line)
        {
            // Robust CSV parsing that handles quoted fields and commas
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '\"')
                {
                    if (inQuotes && i < line.Length - 1 && line[i + 1] == '\"')
                    {
                        // Escaped quote
                        current.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }

    public class PassengerTicketData
    {
        public string PassengerName { get; set; }
        public string FlightNumber { get; set; }
        public string DepartureAirport { get; set; }
        public string ArrivalAirport { get; set; }
        public string FlightDate { get; set; }
        public string DepartureTime { get; set; }
        public string CabinClass { get; set; }
        public string SeatNumber { get; set; }
        public string BarcodeData { get; set; }

        public bool IsValid()
        {
            return ValidationHelper.AllPropertiesHaveValues(
                PassengerName, FlightNumber, DepartureAirport, ArrivalAirport,
                FlightDate, DepartureTime, CabinClass, SeatNumber, BarcodeData
            );
        }
    }
}