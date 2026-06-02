using MongoDB.Driver;
using MsEhrLogger.Application.Ports.In;
using MsEhrLogger.Application.Ports.Out;
using MsEhrLogger.Application.UseCases;
using MsEhrLogger.Domain.Services;
using MsEhrLogger.Infrastructure.Adapters.Persistence;
using MsEhrLogger.Infrastructure.Adapters.Rest;
using MsEhrLogger.Infrastructure.Mappers;
using MsEhrLogger.Infrastructure.Mappers.Interface;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;

namespace MsEhrLogger.Infrastructure.Config;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        services
            .AddMongoDB(configuration)
            .AddRabbitMQ(configuration)
            .AddApplicationServices()
            .AddObservability(configuration);

        return services;
    }


    private static IServiceCollection AddMongoDB(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        services.AddSingleton<AppDbContext>();
        services.AddSingleton<IMongoDatabase>(sp =>
            sp.GetRequiredService<AppDbContext>().Database);

        services.AddSingleton<IEhrRecordRepositoryPort, EhrRecordRepositoryAdapter>();
        return services;
    }


    private static IServiceCollection AddRabbitMQ(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        services.AddSingleton<IConnection>(_ =>
        {
            var factory = new ConnectionFactory
            {
                HostName               = configuration["RabbitMQ:Host"]     ?? "localhost",
                Port                   = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
                UserName               = configuration["RabbitMQ:Username"] ?? "medipass",
                Password               = configuration["RabbitMQ:Password"] ?? "medipass_pass",
                VirtualHost            = "/",
                DispatchConsumersAsync = true
            };

            int retries = 10;
            while (retries > 0)
            {
                try
                {
                    Console.WriteLine("[EHR] Intentando conectar a RabbitMQ...");
                    var connection = factory.CreateConnection("ms-ehrlogger");
                    Console.WriteLine("[EHR] Conexión a RabbitMQ establecida.");
                    return connection;
                }
                catch (Exception ex)
                {
                    retries--;
                    Console.WriteLine($"[EHR] RabbitMQ no disponible. Reintentando en 5s... ({retries} intentos restantes). {ex.Message}");
                    if (retries == 0) throw;
                    Thread.Sleep(5000);
                }
            }

            throw new Exception("No se pudo conectar a RabbitMQ tras múltiples intentos.");
        });

        services.AddSingleton<IEhrEventPublisherPort, EhrEventPublisherAdapter>();
        services.AddHostedService<AppointmentConsumerAdapter>();
        return services;
    }


    private static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IEhrRecordMapper, EhrRecordMapper>();
        services.AddSingleton<EhrRecordService>();
        services.AddScoped<IEhrLoggerUseCasePort, EhrLoggerUseCase>();
        return services;
    }


    private static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService("ms-ehrlogger"))
                    .AddAspNetCoreInstrumentation(options => 
                    {
                        options.Filter = context => !context.Request.Path.StartsWithSegments("/metrics") && !context.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddJaegerExporter(opts =>
                    {
                        opts.AgentHost = configuration["Jaeger:Host"] ?? "localhost";
                        opts.AgentPort = int.Parse(configuration["Jaeger:Port"] ?? "6831");
                    });
            });

        return services;
    }
}
