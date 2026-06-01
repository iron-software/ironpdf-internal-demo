namespace IronPdfDemo.Models.Responses;

public class FormFieldsResponse
{
    public List<FormFieldInfo> Fields { get; set; } = new();
}

public class FormFieldInfo
{
    public string Name { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;
    public string? CurrentValue { get; set; }
    public List<string> Options { get; set; } = new();
}
