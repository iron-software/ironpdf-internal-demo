using Microsoft.AspNetCore.SignalR;

namespace IronPdfDemo.Hubs;

public class BatchProgressHub : Hub
{
    public async Task JoinJob(string jobId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, jobId);

    public async Task LeaveJob(string jobId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId);
}
