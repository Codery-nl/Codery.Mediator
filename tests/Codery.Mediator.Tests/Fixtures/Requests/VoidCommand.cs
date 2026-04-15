namespace Codery.Mediator.Tests.Fixtures.Requests;

public sealed record VoidCommand(string Name) : IRequest;

public sealed class VoidCommandHandler : IRequestHandler<VoidCommand, Unit>
{
    public Task<Unit> Handle(VoidCommand request, CancellationToken cancellationToken)
    {
        return Unit.Task;
    }
}
