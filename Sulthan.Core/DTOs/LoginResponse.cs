namespace Sulthan.Shared.DTOs;

public class LoginResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}