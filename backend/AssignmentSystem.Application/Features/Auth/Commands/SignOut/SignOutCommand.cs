using MediatR;

namespace AssignmentSystem.Application.Features.Auth.Commands.SignOut;

public record SignOutCommand : IRequest;

public class SignOutCommandHandler : IRequestHandler<SignOutCommand>
{
    public Task Handle(SignOutCommand request, CancellationToken cancellationToken)
    {
        // Cookie clearing is handled at the controller level.
        // Future: could revoke refresh token from DB here if token is passed in.
        return Task.CompletedTask;
    }
}
