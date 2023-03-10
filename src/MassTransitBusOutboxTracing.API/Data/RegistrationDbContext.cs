using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MassTransitBusOutboxTracing.API.Data;

public class RegistrationDbContext : DbContext
{
    public RegistrationDbContext(DbContextOptions<RegistrationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        MapRegistration(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }

    static void MapRegistration(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<Registration> registration = modelBuilder.Entity<Registration>();

        registration.Property(x => x.Id);
        registration.HasKey(x => x.Id);

        registration.Property(x => x.RegistrationId);
        registration.Property(x => x.RegistrationDate);
        registration.Property(x => x.MemberId).HasMaxLength(64);
        registration.Property(x => x.EventId).HasMaxLength(64);
        registration.Property(x => x.Payment);

        registration.HasIndex(x => new
        {
            x.MemberId,
            x.EventId
        }).IsUnique();
    }
}
