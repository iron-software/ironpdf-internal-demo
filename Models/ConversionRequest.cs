namespace IronPdfConverter.Models
{
    public class ConversionRequest
    {
        public string? HtmlContent { get; set; }
        public string? Url { get; set; }
        public List<IFormFile>? Files { get; set; }
    }

    public class MergeRequest
    {
        public List<string> FilePaths { get; set; } = new List<string>();
    }
}