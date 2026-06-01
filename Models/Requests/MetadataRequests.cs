using Microsoft.AspNetCore.Http;

namespace IronPdfDemo.Models.Requests;

public class SetMetadataRequest
{
    public IFormFile? File { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Subject { get; set; }
    public string? Keywords { get; set; }
    public string? Creator { get; set; }
}

public class ConvertToPdfARequest
{
    public IFormFile? File { get; set; }
    public string Conformance { get; set; } = "PdfA1b";
}
