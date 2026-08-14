using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Common.Models;
using AssignmentSystem.Domain.Entities;
using RefreshTokenEntity = AssignmentSystem.Domain.Entities.RefreshToken;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AssignmentSystem.Application.Features.Auth.Commands.Register;

public record RegisterCommand(string Name, string Email, string Password, string Role) : IRequest<AuthTokensDto>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthTokensDto>
{
    private readonly UserManager<User> _userManager;
    private readonly ITokenService _tokenService;

    public RegisterCommandHandler(UserManager<User> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthTokensDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (request.Role != "Student" && request.Role != "Teacher")
            throw new ArgumentException("Invalid role specified. Only Student or Teacher is allowed.");

        if (await _userManager.FindByEmailAsync(request.Email) != null)
            throw new InvalidOperationException("User with this email already exists.");

        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Registration failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await _userManager.AddToRoleAsync(user, request.Role);

        // Auto-login after registration
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, request.Role);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return new AuthTokensDto(accessToken, refreshToken, user.Id, user.Name, user.Email!, request.Role);
    }
}
