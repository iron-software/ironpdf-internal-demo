using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;

namespace IronPdfDemo.Services.Interfaces;

public interface IAnnotationService
{
    Task<PdfOperationResult<PdfFileResponse>> AddHeaderFooterAsync(AddHeaderFooterRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> AddTextWatermarkAsync(AddTextWatermarkRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> AddImageWatermarkAsync(AddImageWatermarkRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> AddAnnotationAsync(AddAnnotationRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> AddBookmarksAsync(AddBookmarkRequest request, CancellationToken ct = default);
}
