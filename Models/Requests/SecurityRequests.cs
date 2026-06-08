using Microsoft.AspNetCore.Http;

namespace IronPdfDemo.Models.Requests;

public class PasswordProtectRequest
{
    public IFormFile? File { get; set; }
    public string? OwnerPassword { get; set; }
    public string? UserPassword { get; set; }
    public bool AllowPrinting { get; set; } = true;
    public bool AllowCopying { get; set; } = false;
    public bool AllowEditing { get; set; } = false;
    public bool AllowAnnotations { get; set; } = false;
}

public class RemovePasswordRequest
{
    public IFormFile? File { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class SignPdfRequest
{
    public IFormFile? PdfFile { get; set; }
    public IFormFile? CertificateFile { get; set; }
    public string CertPassword { get; set; } = string.Empty;
    public string SigningReason { get; set; } = "Approved";
    public string ContactInfo { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class VerifySignatureRequest
{
    public IFormFile? File { get; set; }
}
