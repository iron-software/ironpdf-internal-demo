using Microsoft.AspNetCore.Http;

namespace IronPdfDemo.Models.Requests;

public class MergePdfsRequest
{
    public List<IFormFile> Files { get; set; } = new();
    public bool AddPageNumbers { get; set; } = false;
}

public class SplitPdfRequest
{
    public IFormFile? File { get; set; }
    public int SplitAtPage { get; set; } = 1;
}

public class RotatePagesRequest
{
    public IFormFile? File { get; set; }
    public int RotateDeg { get; set; } = 90;
    public List<int> PageNumbers { get; set; } = new();
}

public class CompressPdfRequest
{
    public IFormFile? File { get; set; }
    public int ImageQuality { get; set; } = 80;
}

public class RedactPdfRequest
{
    public IFormFile? File { get; set; }
    public List<string> TextToRedact { get; set; } = new();
    public bool CaseSensitive { get; set; } = false;
    public bool RedactPartialWords { get; set; } = false;
}
