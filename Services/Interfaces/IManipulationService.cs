using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;

namespace IronPdfDemo.Services.Interfaces;

public interface IManipulationService
{
    Task<PdfOperationResult<PdfFileResponse>> MergeAsync(MergePdfsRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<List<PdfFileResponse>>> SplitAsync(SplitPdfRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> RotatePagesAsync(RotatePagesRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> CompressAsync(CompressPdfRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> RedactAsync(RedactPdfRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<List<PageInfo>>> GetPageInfoAsync(Microsoft.AspNetCore.Http.IFormFile file, CancellationToken ct = default);
}
