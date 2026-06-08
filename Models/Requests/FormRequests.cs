using Microsoft.AspNetCore.Http;

namespace IronPdfDemo.Models.Requests;

public class FillFormRequest
{
    public IFormFile? File { get; set; }
    public Dictionary<string, string> FieldValues { get; set; } = new();
}

public class FlattenFormRequest
{
    public IFormFile? File { get; set; }
}
