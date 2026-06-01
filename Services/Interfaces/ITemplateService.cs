using IronPdfDemo.Models.Common;
using IronPdfDemo.Models.Requests;
using IronPdfDemo.Models.Responses;

namespace IronPdfDemo.Services.Interfaces;

public interface ITemplateService
{
    IReadOnlyList<string> GetAvailableTemplates();
    Task<PdfOperationResult<PdfFileResponse>> GenerateFromTemplateAsync(GenerateTemplateRequest request, CancellationToken ct = default);
    Task<PdfOperationResult<ThumbnailResponse>> GetTemplatePreviewAsync(string templateName);
}
