using System;
using System.Collections.Generic;

namespace PdfGeneratorDemo.Models
{
    public class DataRecord
    {
        private readonly Dictionary<string, string> _fields = new(StringComparer.OrdinalIgnoreCase);

        public string this[string field]
        {
            get => _fields.TryGetValue(field, out var value) ? value : string.Empty;
            set => _fields[field] = value;
        }

        public IEnumerable<string> FieldNames => _fields.Keys;
        public bool HasField(string name) => _fields.ContainsKey(name);
        public bool IsFieldValid(string name) => HasField(name) && !string.IsNullOrWhiteSpace(this[name]);
    }
}
