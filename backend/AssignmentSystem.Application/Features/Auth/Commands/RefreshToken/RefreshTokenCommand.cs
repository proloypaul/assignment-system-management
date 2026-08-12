using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Entities;
using RefreshTokenEntity = AssignmentSystem.Domain.Entities.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AssignmentSystem.Application.Features.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string Token) : IRequest<AuthTokensDto>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokensDto>
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(UserManager<User> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthTokensDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find the user who owns this refresh token
        var users = _userManager.Users.ToList();
        User? matchedUser = null;
        RefreshTokenEntity? matchedToken = null;

        foreach (var u in users)
        {
            var token = u.RefreshTokens.FirstOrDefault(t =>
                t.Token == request.Token &&
                !t.IsRevoked &&
                t.ExpiresAt > DateTime.UtcNow);

            if (token != null)
            {
                matchedUser = u;
                matchedToken = token;
                break;
            }
        }

        if (matchedUser is null || matchedToken is null)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        // Revoke the old token
        matchedToken.IsRevoked = true;

        // Issue new tokens
        var roles = await _userManager.GetRolesAsync(matchedUser);
        var role = roles.FirstOrDefault() ?? "Student";

        var newAccessToken = _tokenService.GenerateAccessToken(matchedUser.Id, matchedUser.Email!, role);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        matchedUser.RefreshTokens.Add(new RefreshTokenEntity
        {
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });

        await _userManager.UpdateAsync(matchedUser);

        return new AuthTokensDto(newAccessToken, newRefreshToken);
    }
}
