using Azure.Monitor.OpenTelemetry.Exporter;
using MassTransit;
using MassTransit.Metadata;
using MassTransitBusOutboxTracing.API;
using MassTransitBusOutboxTracing.API.Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using System.Diagnostics;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry()
    .WithTracing(x =>
    {
        var traceBuilder = x.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("api")
                .AddTelemetrySdk()
                .AddEnvironmentVariableDetector())
            .AddSqlClientInstrumentation(cfg =>
            {
                cfg.SetDbStatementForText = true;
            })
            .AddSource("MassTransit")
            .AddAspNetCoreInstrumentation()
            .AddJaegerExporter(o =>
            {
                o.AgentHost = HostMetadataCache.IsRunningInContainer ? "jaeger" : "localhost";
                o.AgentPort = 6831;
                o.MaxPayloadSizeInBytes = 4096;
                o.ExportProcessorType = ExportProcessorType.Batch;
                o.BatchExportProcessorOptions = new BatchExportProcessorOptions<Activity>
                {
                    MaxQueueSize = 2048,
                    ScheduledDelayMilliseconds = 5000,
                    ExporterTimeoutMilliseconds = 30000,
                    MaxExportBatchSize = 512,
                };
            });

        if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:ConnectionString"]))
        {
            traceBuilder.AddAzureMonitorTraceExporter(cfg =>
            {
                cfg.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
            });
        }
    })
    .StartWithHost();

builder.Services.AddDbContext<RegistrationDbContext>(x =>
{
    var connectionString = builder.Configuration.GetConnectionString("Default");

    x.UseSqlServer(connectionString, options =>
    {
        options.MinBatchSize(1);
    });
});

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<RegistrationDbContext>(o =>
    {
#if PREVIEW
        o.QueryDelay = TimeSpan.FromSeconds(30);
#else
        o.QueryDelay = TimeSpan.FromSeconds(5);
#endif
        o.UseSqlServer();
        o.UseBusOutbox();
        o.DisableInboxCleanupService();
    });

    x.AddConsumers(typeof(Program).Assembly);

    x.UsingInMemory((ctx, cfg) =>
    {
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.UseCustomDeliveryService<RegistrationDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using var scope = app.Services.CreateScope();

var context = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();

context.Database.EnsureDeleted();

context.Database.EnsureCreated();

app.Run();
