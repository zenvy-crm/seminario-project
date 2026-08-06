namespace Seminario.Application.Common;

public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public List<string> Errors { get; set; } = new();
}
