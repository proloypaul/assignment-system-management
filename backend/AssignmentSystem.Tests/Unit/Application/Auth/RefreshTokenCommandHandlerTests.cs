using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Features.Auth.Commands.RefreshToken;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Tests.Builders;
using AssignmentSystem.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AssignmentSystem.Tests.Unit.Application.Auth;

/// <summary>
/// Unit tests for <see cref="RefreshTokenCommandHandler"/>.
/// Mocks: UserManager&lt;User&gt;, ITokenService, IConfiguration.
///
/// Key note: The handler calls _userManager.Users.ToList() to scan all users.
/// We mock the Users IQueryable to return a controlled list.
/// </summary>
[Trait("Category", "Unit")]
public class RefreshTokenCommandHandlerTests
{
    // ── Fixtures ───────────────────────────────────────────────────────────────

    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly IConfiguration _configuration;
    private readonly RefreshTokenCommandHandler _sut;

    public RefreshTokenCommandHandlerTests()
    {
        _userManagerMock = MockUserManagerFactory.Create();
        _tokenServiceMock = new Mock<ITokenService>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt__RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        _sut = new RefreshTokenCommandHandler(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _configuration);
    }

    // ── Helper: set up the UserManager.Users queryable ─────────────────────────

    private void SetupUsers(IEnumerable<User> users)
    {
        _userManagerMock
            .Setup(m => m.Users)
            .Returns(users.AsQueryable());
    }

    // ── Unhappy paths ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoUserHasTheToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange: user list with no matching token
        var user = new UserBuilder().Build(); // user has zero refresh tokens
        SetupUsers([user]);

        var command = new RefreshTokenCommand("non-existent-token");

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid or expired*");
    }

    [Fact]
    public async Task Handle_WhenTokenIsRevoked_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        const string tokenValue = "revoked-token-abc";
        var revokedToken = new RefreshTokenBuilder().WithToken(tokenValue).Revoked().Build();
        var user = new UserBuilder().WithRefreshToken(revokedToken).Build();
        SetupUsers([user]);

        var command = new RefreshTokenCommand(tokenValue);

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid or expired*");
    }

    [Fact]
    public async Task Handle_WhenTokenIsExpired_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        const string tokenValue = "expired-token-xyz";
        var expiredToken = new RefreshTokenBuilder().WithToken(tokenValue).Expired().Build();
        var user = new UserBuilder().WithRefreshToken(expiredToken).Build();
        SetupUsers([user]);

        var command = new RefreshTokenCommand(tokenValue);

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid or expired*");
    }

    // ── Happy paths ────────────────────────────────────────────────────────────

    private (User user, string tokenValue) SetupValidTokenScenario(string role = "Student")
    {
        const string tokenValue = "valid-refresh-token-123";

        var validToken = new RefreshTokenBuilder().WithToken(tokenValue).Valid().Build();
        var user = new UserBuilder().WithRefreshToken(validToken).Build();

        SetupUsers([user]);

        _userManagerMock
            .Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { role });

        _userManagerMock
            .Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(user.Id, user.Email!, role))
            .Returns("new-access-token");

        _tokenServiceMock
            .Setup(t => t.GenerateRefreshToken())
            .Returns("new-refresh-token");

        return (user, tokenValue);
    }

    [Fact]
    public async Task Handle_WithValidToken_RevokesOldToken()
    {
        // Arrange
        var (user, tokenValue) = SetupValidTokenScenario();
        var oldToken = user.RefreshTokens.First(t => t.Token == tokenValue);

        // Act
        await _sut.Handle(new RefreshTokenCommand(tokenValue), CancellationToken.None);

        // Assert
        oldToken.IsRevoked.Should().BeTrue(
            because: "token rotation requires the old token to be invalidated immediately");
    }

    [Fact]
    public async Task Handle_WithValidToken_AddsNewRefreshTokenToUser()
    {
        // Arrange
        var (user, tokenValue) = SetupValidTokenScenario();
        var countBefore = user.RefreshTokens.Count;

        // Act
        await _sut.Handle(new RefreshTokenCommand(tokenValue), CancellationToken.None);

        // Assert
        user.RefreshTokens.Count.Should().Be(countBefore + 1,
            because: "a new refresh token must be issued to replace the old one");
    }

    [Fact]
    public async Task Handle_WithValidToken_CallsUserManagerUpdateAsyncOnce()
    {
        // Arrange
        var (user, tokenValue) = SetupValidTokenScenario();

        // Act
        await _sut.Handle(new RefreshTokenCommand(tokenValue), CancellationToken.None);

        // Assert
        // the user entity must be persisted after token rotation
        _userManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidToken_ReturnsNewTokensInDto()
    {
        // Arrange
        var (_, tokenValue) = SetupValidTokenScenario();

        // Act
        var result = await _sut.Handle(new RefreshTokenCommand(tokenValue), CancellationToken.None);

        // Assert
        result.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-refresh-token");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoRole_DefaultsToStudentRole()
    {
        // Arrange
        const string tokenValue = "valid-token-no-role";
        var validToken = new RefreshTokenBuilder().WithToken(tokenValue).Valid().Build();
        var user = new UserBuilder().WithRefreshToken(validToken).Build();

        SetupUsers([user]);

        // Return empty roles list
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), "Student")).Returns("token");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("new-refresh");

        // Act
        var result = await _sut.Handle(new RefreshTokenCommand(tokenValue), CancellationToken.None);

        // Assert
        result.Role.Should().Be("Student",
            because: "users without an explicit role default to Student");
    }
}
