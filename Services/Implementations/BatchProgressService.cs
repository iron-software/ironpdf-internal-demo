using IronPdfDemo.Hubs;
using IronPdfDemo.Models.Common;
using IronPdfDemo.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace IronPdfDemo.Services.Implementations;

public class BatchProgressService : IBatchProgressService
{
    private readonly IHubContext<BatchProgressHub> _hub;

    public BatchProgressService(IHubContext<BatchProgressHub> hub) => _hub = hub;

    public Task SendProgressAsync(BatchProgressUpdate update, CancellationToken ct = default)
        => _hub.Clients.Group(update.JobId).SendAsync("progressUpdate", update, ct);
}
