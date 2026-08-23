using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Features.Auth.Commands.Login;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Tests.Builders;
using AssignmentSystem.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AssignmentSystem.Tests.Unit.Application.Auth;

/// <summary>
/// Unit tests for <see cref="LoginCommandHandler"/>.
/// Mocks: UserManager&lt;User&gt;, ITokenService, IConfiguration.
/// </summary>
[Trait("Category", "Unit")]
public class LoginCommandHandlerTests
{
    // ── Fixtures ───────────────────────────────────────────────────────────────

    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly IConfiguration _configuration;
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _userManagerMock = MockUserManagerFactory.Create();
        _tokenServiceMock = new Mock<ITokenService>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt__RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        _sut = new LoginCommandHandler(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _configuration);
    }

    // ── Unhappy paths ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsArgumentException()
    {
        // Arrange
        _userManagerMock
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var command = new LoginCommand("notfound@example.com", "anyPassword");

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid credentials*");
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ThrowsArgumentException()
    {
        // Arrange
        var user = new UserBuilder().Build();

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(user.Email!))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>()))
            .ReturnsAsync(false);

        var command = new LoginCommand(user.Email!, "WrongPassword123!");

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid credentials*");
    }

    // ── Happy path ─────────────────────────────────────────────────────────────

    private void SetupSuccessfulLogin(User user, string role, string accessToken = "access-token", string refreshToken = "refresh-token")
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { role });
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(user.Id, user.Email!, role)).Returns(accessToken);
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns(refreshToken);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAuthTokensDtoWithCorrectEmail()
    {
        // Arrange
        var user = new UserBuilder().Build();
        SetupSuccessfulLogin(user, "Student");
        var command = new LoginCommand(user.Email!, "CorrectPassword1!");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsCorrectUserId()
    {
        // Arrange
        var user = new UserBuilder().Build();
        SetupSuccessfulLogin(user, "Student");
        var command = new LoginCommand(user.Email!, "CorrectPassword1!");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_WhenUserHasStudentRole_ReturnsDtoWithStudentRole()
    {
        // Arrange
        var user = new UserBuilder().Build();
        SetupSuccessfulLogin(user, "Student");
        var command = new LoginCommand(user.Email!, "CorrectPassword1!");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Role.Should().Be("Student");
    }

    [Fact]
    public async Task Handle_WhenUserHasTeacherRole_ReturnsDtoWithTeacherRole()
    {
        // Arrange
        var user = new UserBuilder().Build();
        SetupSuccessfulLogin(user, "Teacher");
        var command = new LoginCommand(user.Email!, "CorrectPassword1!");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Role.Should().Be("Teacher");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoRole_DefaultsToStudentRole()
    {
        // Arrange
        var user = new UserBuilder().Build();

        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        // Empty roles list — the handler should default to "Student"
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), "Student")).Returns("token");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh");

        var command = new LoginCommand(user.Email!, "CorrectPassword1!");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Role.Should().Be("Student",
            because: "when a user has no assigned role, the system defaults to Student");
    }

    [Fact]
    public async Task Handle_WithValidCredentials_AddsNewRefreshTokenToUser()
    {
        // Arrange
        var user = new UserBuilder().Build();
        var initialTokenCount = user.RefreshTokens.Count;
        SetupSuccessfulLogin(user, "Student");
        var command = new LoginCommand(user.Email!, "CorrectPassword1!");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        user.RefreshTokens.Count.Should().Be(initialTokenCount + 1,
            because: "a new refresh token must be persisted on every successful login");
    }

    [Fact]
    public async Task Handle_WithValidCredentials_CallsUserManagerUpdateAsyncOnce()
    {
        // Arrange
        var user = new UserBuilder().Build();
        SetupSuccessfulLogin(user, "Student");
        var command = new LoginCommand(user.Email!, "CorrectPassword1!");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        // user must be saved after adding the refresh token
        _userManagerMock.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_CallsGenerateAccessTokenOnce()
    {
        // Arrange
        var user = new UserBuilder().Build();
        SetupSuccessfulLogin(user, "Student");
        var command = new LoginCommand(user.Email!, "CorrectPassword1!");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(t =>
            t.GenerateAccessToken(user.Id, user.Email!, "Student"), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_CallsGenerateRefreshTokenOnce()
    {
        // Arrange
        var user = new UserBuilder().Build();
        SetupSuccessfulLogin(user, "Student");
        var command = new LoginCommand(user.Email!, "CorrectPassword1!");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(t => t.GenerateRefreshToken(), Times.Once);
    }
}
