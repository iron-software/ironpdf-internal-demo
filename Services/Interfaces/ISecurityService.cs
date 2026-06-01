using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;

namespace IronPdfDemo.Services.Interfaces;

public interface ISecurityService
{
    Task<PdfOperationResult<PdfFileResponse>> PasswordProtectAsync(PasswordProtectRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> RemovePasswordAsync(RemovePasswordRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> SignPdfAsync(SignPdfRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<bool>> VerifySignatureAsync(VerifySignatureRequest request, CancellationToken ct = default);
}
