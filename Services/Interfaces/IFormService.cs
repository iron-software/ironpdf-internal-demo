using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;
using Microsoft.AspNetCore.Http;

namespace IronPdfDemo.Services.Interfaces;

public interface IFormService
{
    Task<PdfOperationResult<FormFieldsResponse>> GetFormFieldsAsync(IFormFile file, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> FillFormAsync(FillFormRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> FlattenFormAsync(FlattenFormRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<PdfFileResponse>> CreateFormFromHtmlAsync(string htmlContent, CancellationToken ct = default);
}
