using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;
using Microsoft.AspNetCore.Http;

namespace IronPdfDemo.Services.Interfaces;

public interface IConversionService
{
    Task<PdfOperationResult<PdfFileResponse>> HtmlToPdfAsync(HtmlConversionRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> UrlToPdfAsync(UrlConversionRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> ImageToPdfAsync(IFormFile file, string paperSize = "A4", CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> DocxToPdfAsync(IFormFile file, CancellationToken ct = default);
    Task<PdfOperationResult<BatchResultResponse>> BatchConvertAsync(List<IFormFile> files, string jobId, string paperSize = "A4", IProgress<BatchProgressUpdate>? progress = null, CancellationToken ct = default);
    Task<PdfOperationResult<ThumbnailResponse>> ExtractThumbnailAsync(IFormFile file, int pageNumber = 1, int widthPx = 300);
}
