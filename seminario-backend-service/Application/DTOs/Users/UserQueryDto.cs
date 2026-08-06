namespace Seminario.Application.DTOs.Users;

public class UserQueryDto
{
    public string? Search { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
}
