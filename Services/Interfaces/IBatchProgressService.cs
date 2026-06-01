using IronPdfDemo.Models.Common;

namespace IronPdfDemo.Services.Interfaces;

public interface IBatchProgressService
{
    Task SendProgressAsync(BatchProgressUpdate update, CancellationToken ct = default);
}
