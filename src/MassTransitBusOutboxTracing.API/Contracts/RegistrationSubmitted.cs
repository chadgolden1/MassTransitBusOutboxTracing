namespace MassTransitBusOutboxTracing.API.Contracts;

public record RegistrationSubmitted
{
    public Guid Id { get; init; }
}
