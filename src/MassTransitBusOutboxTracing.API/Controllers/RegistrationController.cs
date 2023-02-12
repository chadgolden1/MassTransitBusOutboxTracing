using MassTransit;
using MassTransitBusOutboxTracing.API.Contracts;
using MassTransitBusOutboxTracing.API.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace MassTransitBusOutboxTracing.API.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class RegistrationController : ControllerBase
{
    private readonly RegistrationDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RegistrationController(
        RegistrationDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        IServiceScopeFactory serviceScopeFactory)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _serviceScopeFactory = serviceScopeFactory;
    }

    [HttpPost(Name = "SubmitRegistration")]
    [ProducesResponseType(202)]
    public async Task<IActionResult> SubmitRegistration()
    {
        var registration = new Registration
        {
            RegistrationId = NewId.NextGuid(),
            RegistrationDate = DateTime.UtcNow,
            MemberId = Guid.NewGuid().ToString(),
            EventId = Guid.NewGuid().ToString(),
            Payment = RandomNumberGenerator.GetInt32(1, 1000)
        };

        await _dbContext.Database.BeginTransactionAsync();

        await _dbContext.AddAsync(registration);

        await _publishEndpoint.Publish(new RegistrationSubmitted
        {
            Id = registration.RegistrationId
        });

        await _publishEndpoint.Publish(new SendRegistrationNotification
        {
            Id = registration.RegistrationId
        });

        await _dbContext.SaveChangesAsync();

        await _dbContext.Database.CommitTransactionAsync();

        return Accepted();
    }

    [HttpPost(Name = "SubmitLotsOfRegistrations")]
    [ProducesResponseType(202)]
    public async Task<IActionResult> SubmitLotsOfRegistrations(int numberOfTasks = 50)
    {
        var submitTasks = Enumerable.Range(0, numberOfTasks).Select(_ => SubmitRegistrationTask(1));

        await Task.WhenAll(submitTasks);

        return Accepted();
    }

    [HttpPost(Name = "SubmitLotsOfRegistrationsInSingleScope")]
    [ProducesResponseType(202)]
    public async Task<IActionResult> SubmitLotsOfRegistrationsInSingleScope(int numberOfTasks = 1, int registrationsPerScope = 50)
    {
        var submitTasks = Enumerable.Range(0, numberOfTasks).Select(_ => SubmitRegistrationTask(registrationsPerScope));

        await Task.WhenAll(submitTasks);

        return Accepted();
    }

    private async Task SubmitRegistrationTask(int registrationsPerScope = 1)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();

        await dbContext.Database.BeginTransactionAsync();

        foreach (var i in Enumerable.Range(0, registrationsPerScope))
        {
            var registration = new Registration
            {
                RegistrationId = NewId.NextGuid(),
                RegistrationDate = DateTime.UtcNow,
                MemberId = Guid.NewGuid().ToString(),
                EventId = Guid.NewGuid().ToString(),
                Payment = RandomNumberGenerator.GetInt32(1, 1000)
            };

            await dbContext.AddAsync(registration);

            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            await publishEndpoint.Publish(new RegistrationSubmitted
            {
                Id = registration.RegistrationId
            });

            await publishEndpoint.Publish(new SendRegistrationNotification
            {
                Id = registration.RegistrationId
            });
        }

        await dbContext.SaveChangesAsync();

        await dbContext.Database.CommitTransactionAsync();
    }
}