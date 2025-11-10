using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PdfGeneratorDemo.Models;

namespace PdfGeneratorDemo.Services
{
    public class DynamicCsvReader
    {
        public List<DataRecord> ReadData(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"CSV file not found: {filePath}");

            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0)
                throw new InvalidDataException("CSV file is empty");

            var headers = ParseCsvLine(lines[0]).Select(h => h.Trim()).ToArray();
            var records = new List<DataRecord>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;

                try
                {
                    var values = ParseCsvLine(lines[i]);
                    var record = new DataRecord();

                    for (int j = 0; j < headers.Length && j < values.Length; j++)
                        record[headers[j]] = values[j].Trim();

                    records.Add(record);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing row {i + 1}: {ex.Message}");
                }
            }

            return records;
        }

        private static string[] ParseCsvLine(string line)
        {
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
}
