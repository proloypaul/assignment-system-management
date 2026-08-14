using AssignmentSystem.Application.Common.Interfaces;
using AssignmentSystem.Application.Features.Auth.Commands.Register;
using AssignmentSystem.Domain.Entities;
using AssignmentSystem.Tests.Builders;
using AssignmentSystem.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AssignmentSystem.Tests.Unit.Application.Auth;

/// <summary>
/// Unit tests for <see cref="RegisterCommandHandler"/>.
/// Mocks: UserManager&lt;User&gt;, ITokenService.
/// </summary>
[Trait("Category", "Unit")]
public class RegisterCommandHandlerTests
{
    // ── Fixtures ───────────────────────────────────────────────────────────────

    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        _userManagerMock = MockUserManagerFactory.Create();
        _tokenServiceMock = new Mock<ITokenService>();

        _sut = new RegisterCommandHandler(
            _userManagerMock.Object,
            _tokenServiceMock.Object);
    }

    // ── Role validation ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenRoleIsNotStudentOrTeacher_ThrowsArgumentException(string invalidRole)
    {
        // Arrange
        var command = new RegisterCommand("Test User", "test@example.com", "Password1!", invalidRole);

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid role*");
    }

    // ── Duplicate email ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var existingUser = new UserBuilder().Build();

        _userManagerMock
            .Setup(m => m.FindByEmailAsync(existingUser.Email!))
            .ReturnsAsync(existingUser); // email already taken

        var command = new RegisterCommand("New User", existingUser.Email!, "Password1!", "Student");

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    // ── CreateAsync failure ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCreateAsyncFails_ThrowsInvalidOperationExceptionWithErrorDetails()
    {
        // Arrange
        _userManagerMock
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null); // email not taken

        var identityErrors = new[]
        {
            new IdentityError { Description = "Password is too short." },
            new IdentityError { Description = "Password requires a digit." }
        };

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(identityErrors));

        var command = new RegisterCommand("New User", "new@example.com", "weak", "Student");

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Registration failed*");
    }

    // ── Happy paths ────────────────────────────────────────────────────────────

    private void SetupSuccessfulRegistration(string role)
    {
        _userManagerMock
            .Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), role))
            .ReturnsAsync(IdentityResult.Success);

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), role))
            .Returns("access-token");

        _tokenServiceMock
            .Setup(t => t.GenerateRefreshToken())
            .Returns("refresh-token");
    }

    [Fact]
    public async Task Handle_WithValidStudentData_ReturnsAuthTokensDtoWithStudentRole()
    {
        // Arrange
        SetupSuccessfulRegistration("Student");
        var command = new RegisterCommand("Alice Smith", "alice@example.com", "Password1!", "Student");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Role.Should().Be("Student");
    }

    [Fact]
    public async Task Handle_WithValidTeacherData_ReturnsAuthTokensDtoWithTeacherRole()
    {
        // Arrange
        SetupSuccessfulRegistration("Teacher");
        var command = new RegisterCommand("Bob Jones", "bob@example.com", "Password1!", "Teacher");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Role.Should().Be("Teacher");
    }

    [Fact]
    public async Task Handle_WithValidData_OutputEmailMatchesInput()
    {
        // Arrange
        const string expectedEmail = "charlie@example.com";
        SetupSuccessfulRegistration("Student");
        var command = new RegisterCommand("Charlie", expectedEmail, "Password1!", "Student");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Email.Should().Be(expectedEmail);
    }

    [Fact]
    public async Task Handle_WithValidData_CallsAddToRoleAsyncWithCorrectRole()
    {
        // Arrange
        const string expectedRole = "Teacher";
        SetupSuccessfulRegistration(expectedRole);
        var command = new RegisterCommand("Diana", "diana@example.com", "Password1!", expectedRole);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        // every newly registered user must be assigned their specified role
        _userManagerMock.Verify(
            m => m.AddToRoleAsync(It.IsAny<User>(), expectedRole),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidData_CallsGenerateAccessTokenOnce()
    {
        // Arrange
        SetupSuccessfulRegistration("Student");
        var command = new RegisterCommand("Eve", "eve@example.com", "Password1!", "Student");

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _tokenServiceMock.Verify(
            t => t.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), "Student"),
            Times.Once);
    }
}
