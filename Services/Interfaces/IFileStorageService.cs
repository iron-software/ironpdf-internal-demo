using Microsoft.AspNetCore.Http;

namespace IronPdfConverter.Services.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string subDirectory = "");
        string GetOutputDirectory();
        string GenerateUniqueFileName(string extension);
        string GenerateUniqueFileName(string originalFileName, string extension);
        void EnsureOutputDirectoryExists();
        void CleanTempFiles();
    }
}