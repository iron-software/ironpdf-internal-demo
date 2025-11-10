using System;
using System.IO;
using System.Linq;
using PdfGeneratorDemo.Models;

namespace PdfGeneratorDemo.Utilities
{
    public static class FileNameGenerator
    {
        private static readonly string[] IdentifierFields =
        {
            "PassengerName", "CustomerName", "Name", "ID", "OrderNumber", "TicketNumber"
        };

        public static string Generate(DataRecord record, int index)
        {
            string baseName = IdentifierFields
                .FirstOrDefault(f => record.HasField(f) && !string.IsNullOrWhiteSpace(record[f])) is string field
                    ? record[field]
                    : $"document_{index + 1:D5}";

            baseName = EscapeFileName(baseName);
            if (baseName.Length > 80)
                baseName = baseName.Substring(0, 80);

            return $"{baseName}.pdf";
        }

        private static string EscapeFileName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "document";
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Concat(input.Select(c => invalidChars.Contains(c) ? '_' : c)).Replace(' ', '_');
        }
    }
}
