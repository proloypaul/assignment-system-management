namespace AssignmentSystem.Application.Common.Models;

public record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    Guid UserId,
    string Name,
    string Email,
    string Role
);

