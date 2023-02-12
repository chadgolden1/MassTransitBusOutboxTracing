using MassTransit;
using MassTransitBusOutboxTracing.API.Contracts;

namespace MassTransitBusOutboxTracing.API.Consumers;

public class SendRegistrationNotificationConsumer : IConsumer<SendRegistrationNotification>
{
    public Task Consume(ConsumeContext<SendRegistrationNotification> context)
    {
        return Task.CompletedTask;
    }
}
