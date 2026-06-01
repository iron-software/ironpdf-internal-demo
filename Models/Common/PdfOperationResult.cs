namespace IronPdfDemo.Models.Common;

public class PdfOperationResult<T>
{
    public bool IsSuccess { get; protected set; }
    public T? Data { get; protected set; }
    public string? Error { get; protected set; }
    public string? ErrorCode { get; protected set; }
    public long ElapsedMs { get; set; }
    public string OperationId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    public static PdfOperationResult<T> Success(T data, long elapsedMs = 0)
        => new() { IsSuccess = true, Data = data, ElapsedMs = elapsedMs };

    public static PdfOperationResult<T> Failure(string error, string errorCode = "GENERAL_ERROR")
        => new() { IsSuccess = false, Error = error, ErrorCode = errorCode };
}
