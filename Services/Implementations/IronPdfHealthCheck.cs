using IronPdf;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IronPdfDemo.Services.Implementations;

public class IronPdfHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                ["licensed"] = License.IsLicensed,
                ["version"] = typeof(PdfDocument).Assembly.GetName().Version?.ToString() ?? "unknown"
            };
            return Task.FromResult(HealthCheckResult.Healthy("IronPDF is operational.", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(ex.Message));
        }
    }
}
