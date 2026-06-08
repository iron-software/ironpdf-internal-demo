using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;
using Microsoft.AspNetCore.Http;

namespace IronPdfDemo.Services.Interfaces;

public interface IMetadataService
{
    Task<PdfOperationResult<PdfMetadataInfo>> GetMetadataAsync(IFormFile file, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> SetMetadataAsync(SetMetadataRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> ConvertToPdfAAsync(ConvertToPdfARequest request, CancellationToken ct = default);
}
