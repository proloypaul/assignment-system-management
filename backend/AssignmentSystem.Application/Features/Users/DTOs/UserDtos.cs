namespace AssignmentSystem.Application.Features.Users.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfileImage { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Role { get; set; } = string.Empty;
}

public record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    string? Phone,
    string Role);

public record UpdateUserRequest(
    string? Name,
    string? Phone,
    string? ProfileImage);
