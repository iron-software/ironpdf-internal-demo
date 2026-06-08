namespace IronPdfDemo.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";
    public string OutputDirectory { get; set; } = "Output";
    public string TempDirectory { get; set; } = "Temp";
    public int MaxFileSizeMb { get; set; } = 50;
    public int RetainOutputDays { get; set; } = 7;
    public bool EnableAutoCleanup { get; set; } = true;
}
