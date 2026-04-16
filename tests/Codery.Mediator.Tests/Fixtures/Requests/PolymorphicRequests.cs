namespace Codery.Mediator.Tests.Fixtures.Requests;

public record BaseQuery(string Value) : IRequest<string>;

public record DerivedQuery(string Value) : BaseQuery(Value);

public sealed record DeeplyDerivedQuery(string Value) : DerivedQuery(Value);

public sealed class BaseQueryHandler : IRequestHandler<BaseQuery, string>
{
    public Task<string> Handle(BaseQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Base: {request.Value}");
    }
}
