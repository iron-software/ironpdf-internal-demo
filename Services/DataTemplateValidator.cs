using System;
using System.Collections.Generic;
using System.Linq;
using PdfGeneratorDemo.Models;

namespace PdfGeneratorDemo.Services
{
    public class DataTemplateValidator
    {
        public ValidationResult Validate(List<DataRecord> records, TemplateInfo template)
        {
            var result = new ValidationResult();

            if (records.Count == 0)
            {
                result.Errors.Add("No data records found.");
                return result;
            }

            var csvFields = new HashSet<string>(records[0].FieldNames, StringComparer.OrdinalIgnoreCase);
            var missing = template.RequiredFields.Where(f => !csvFields.Contains(f)).ToList();

            if (missing.Any())
                result.Errors.Add($"Missing fields in CSV: {string.Join(", ", missing)}");

            // Optionally sample a few records for empty mandatory fields
            int sample = Math.Min(records.Count, 5);
            for (int i = 0; i < sample; i++)
            {
                var empties = template.RequiredFields.Where(f => !records[i].IsFieldValid(f)).ToList();
                if (empties.Any())
                    result.Errors.Add($"Record {i + 1} has empty/missing fields: {string.Join(", ", empties)}");
            }

            return result;
        }
    }
}
