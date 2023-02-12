namespace MassTransitBusOutboxTracing.API.Contracts;

public record SendRegistrationNotification
{
    public Guid Id { get; init; }
}