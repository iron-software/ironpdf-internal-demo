namespace IronPdfDemo.Configuration;

public class SignalROptions
{
    public const string SectionName = "SignalR";
    public string BatchProgressHubPath { get; set; } = "/hubs/batch-progress";
}
