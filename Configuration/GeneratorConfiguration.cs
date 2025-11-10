using System;

namespace PdfGeneratorDemo.Configuration
{
    public class GeneratorConfiguration
    {
        public string TemplatesFolder { get; set; } = "Templates";
        public string DataFolder { get; set; } = "Data";
        public string OutputBaseFolder { get; set; } = "Output";
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
        public int ShowProgressInterval { get; set; } = 20;
        public bool ValidateOnly { get; set; } = false;
    }
}
