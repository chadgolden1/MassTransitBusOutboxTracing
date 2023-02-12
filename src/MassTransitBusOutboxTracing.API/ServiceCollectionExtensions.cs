using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace MassTransitBusOutboxTracing.API;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection UseCustomDeliveryService<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.RemoveHostedService<BusOutboxDeliveryService<TDbContext>>();
        services.AddHostedService<CustomBusOutboxDeliveryService<TDbContext>>();
        return services;
    }
}
