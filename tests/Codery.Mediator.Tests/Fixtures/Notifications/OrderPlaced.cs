namespace Codery.Mediator.Tests.Fixtures.Notifications;

public sealed record OrderPlaced(string OrderId) : INotification;
