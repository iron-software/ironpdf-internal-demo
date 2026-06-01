using IronPdfDemo.Models.Requests;
using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IronPdfDemo.Controllers;

/// <summary>Password protection, digital signatures, and security operations.</summary>
[ApiController]
[Route("api/security")]
[Produces("application/json")]
public class SecurityController(ISecurityService svc) : ControllerBase
{
    /// <summary>Password-protect a PDF with owner/user passwords and permission flags.</summary>
    [HttpPost("protect")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Protect([FromForm] PasswordProtectRequest req, CancellationToken ct)
    {
        var result = await svc.PasswordProtectAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Remove password protection from a PDF.</summary>
    [HttpPost("remove-password")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> RemovePassword([FromForm] RemovePasswordRequest req, CancellationToken ct)
    {
        var result = await svc.RemovePasswordAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Digitally sign a PDF with a PFX certificate.</summary>
    [HttpPost("sign")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Sign([FromForm] SignPdfRequest req, CancellationToken ct)
    {
        var result = await svc.SignPdfAsync(req, ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>Verify whether all digital signatures in a PDF are valid.</summary>
    [HttpPost("verify-signature")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> VerifySignature([FromForm] VerifySignatureRequest req, CancellationToken ct)
    {
        var result = await svc.VerifySignatureAsync(req, ct);
        return result.IsSuccess
            ? Ok(new { isValid = result.Data, operationId = result.OperationId })
            : BadRequest(new { error = result.Error });
    }
}
