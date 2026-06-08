using IronPdfDemo.Services.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IronPdfDemo.Services.Implementations;

public class StorageHealthCheck(IFileStorageService storage) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var outDir = storage.GetOutputDirectory();
            var tmpDir = storage.GetTempDirectory();
            var data = new Dictionary<string, object>
            {
                ["output_dir"] = outDir,
                ["output_exists"] = Directory.Exists(outDir),
                ["temp_dir"] = tmpDir,
                ["temp_exists"] = Directory.Exists(tmpDir),
                ["output_files"] = storage.ListOutputFiles().Count()
            };
            return Task.FromResult(HealthCheckResult.Healthy("Storage is accessible.", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(ex.Message));
        }
    }
}
