# MassTransitBusOutboxTracing
Playing around with Jaeger tracing with MassTransit transactional outbox (bus outbox portion)

## How to run
Run Docker Compose to spin up SQL Server and Jaeger
```cmd
docker compose up
```

Then run the web API project in Visual Studio, dotnet CLI, etc.