using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Entities;
using RefreshTokenEntity = AssignmentSystem.Domain.Entities.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AssignmentSystem.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthTokensDto>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokensDto>
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(UserManager<User> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthTokensDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Student";

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, role);
        var refreshToken = _tokenService.GenerateRefreshToken();

        // Store refresh token
        user.RefreshTokens.Add(new RefreshTokenEntity
        {
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        });
        await _userManager.UpdateAsync(user);

        return new AuthTokensDto(accessToken, refreshToken, user.Id, user.Name, user.Email!, role);
    }
}
