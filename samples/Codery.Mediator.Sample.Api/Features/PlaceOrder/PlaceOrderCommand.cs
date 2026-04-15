using Codery.Mediator;

namespace Codery.Mediator.Sample.Api.Features.PlaceOrder;

public sealed record PlaceOrderCommand(string ProductName, int Quantity) : IRequest;
