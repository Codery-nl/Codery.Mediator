using Codery.Mediator;

namespace Codery.Mediator.Sample.Api.Features.PlaceOrder;

public sealed class PlaceOrderHandler(IPublisher publisher) : IRequestHandler<PlaceOrderCommand, Unit>
{
    public async Task<Unit> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        // Simulate order placement
        var orderId = Guid.NewGuid().ToString("N")[..8];

        // Publish notification
        await publisher.Publish(new OrderPlacedNotification(orderId, request.ProductName), cancellationToken);

        return Unit.Value;
    }
}
