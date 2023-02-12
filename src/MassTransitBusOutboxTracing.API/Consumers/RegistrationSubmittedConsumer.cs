using MassTransit;
using MassTransitBusOutboxTracing.API.Contracts;

namespace MassTransitBusOutboxTracing.API.Consumers;

public class RegistrationSubmittedConsumer : IConsumer<RegistrationSubmitted>
{
    public Task Consume(ConsumeContext<RegistrationSubmitted> context)
    {
        return Task.CompletedTask;
    }
}
