namespace IronPdfDemo.Services.Interfaces;

public interface IFileStorageService
{
    string GetOutputDirectory();
    string GetTempDirectory();
    Task<string> SaveFileAsync(byte[] data, string baseName, string suffix = "");
    string GenerateUniqueFileName(string baseName, string suffix = "");
    void EnsureDirectoriesExist();
    IEnumerable<FileInfo> ListOutputFiles();
    void CleanOldFiles(int retainDays);
    string GetDownloadUrl(string fileName);
}
