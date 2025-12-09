using IronPdfConverter.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IronPdfConverter.Services.Implementations
{
    public class FileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<FileStorageService> logger)
        {
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subDirectory = "")
        {
            EnsureOutputDirectoryExists();

            var uploadsPath = Path.Combine(GetOutputDirectory(), subDirectory);
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var uniqueFileName = GenerateUniqueFileName(file.FileName);
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation($"File saved: {filePath}");
            return filePath;
        }

        public string GetOutputDirectory()
        {
            var customOutputPath = _configuration["OutputDirectory"];
            if (!string.IsNullOrEmpty(customOutputPath))
                return Path.Combine(_environment.ContentRootPath, customOutputPath);

            return Path.Combine(_environment.ContentRootPath, "Output");
        }

        public string GenerateUniqueFileName(string extension)
        {
            return $"{Guid.NewGuid()}{extension}";
        }

        public string GenerateUniqueFileName(string originalFileName, string extension = ".pdf")
        {
            try
            {
                // Extract the base filename without extension
                var baseName = Path.GetFileNameWithoutExtension(originalFileName);

                // If baseName is empty or too long, use GUID
                if (string.IsNullOrWhiteSpace(baseName) || baseName.Length > 100)
                {
                    return GenerateUniqueFileName(extension);
                }

                // Clean the filename: remove invalid characters
                var invalidChars = Path.GetInvalidFileNameChars();
                baseName = string.Join("_", baseName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).Trim();

                // If cleaning removed everything, use GUID
                if (string.IsNullOrWhiteSpace(baseName))
                {
                    return GenerateUniqueFileName(extension);
                }

                // Limit length
                if (baseName.Length > 50)
                {
                    baseName = baseName.Substring(0, 50);
                }

                // Add timestamp and unique identifier
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 6);

                return $"{baseName}_{timestamp}_{uniqueId}{extension}";
            }
            catch (Exception)
            {
                // Fallback to GUID if anything goes wrong
                return GenerateUniqueFileName(extension);
            }
        }

        public void EnsureOutputDirectoryExists()
        {
            var outputDir = GetOutputDirectory();
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
                _logger.LogInformation($"Output directory created: {outputDir}");
            }
        }

        public void CleanTempFiles()
        {
            var tempDir = Path.Combine(GetOutputDirectory(), "temp");
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                    _logger.LogInformation($"Temp files cleaned: {tempDir}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error cleaning temp files in: {tempDir}");
                }
            }
        }
    }
}