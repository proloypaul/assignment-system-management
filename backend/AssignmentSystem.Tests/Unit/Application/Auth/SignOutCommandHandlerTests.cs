using AssignmentSystem.Application.Features.Auth.Commands.SignOut;
using FluentAssertions;

namespace AssignmentSystem.Tests.Unit.Application.Auth;

/// <summary>
/// Unit tests for <see cref="SignOutCommandHandler"/>.
/// This handler is a deliberate pass-through — cookie clearing happens at the
/// controller level. These tests act as a contract ensuring the handler never
/// throws unexpectedly and always completes cleanly.
/// </summary>
[Trait("Category", "Unit")]
public class SignOutCommandHandlerTests
{
    private readonly SignOutCommandHandler _sut = new();

    [Fact]
    public async Task Handle_Always_CompletesWithoutException()
    {
        // Arrange
        var command = new SignOutCommand();

        // Act
        var act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync(
            because: "sign-out should never throw — failures here would lock users out");
    }

    [Fact]
    public async Task Handle_Always_ReturnsRanToCompletionTask()
    {
        // Arrange
        var command = new SignOutCommand();

        // Act
        var task = _sut.Handle(command, CancellationToken.None);
        await task;

        // Assert
        task.IsCompletedSuccessfully.Should().BeTrue(
            because: "the handler must always return a successfully completed task");
    }
}
