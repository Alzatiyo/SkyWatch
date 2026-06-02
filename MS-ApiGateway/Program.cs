using MS_ApiGateway.Hubs;
using Microsoft.AspNetCore.SignalR;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add SignalR
builder.Services.AddSignalR();

// Add CORS para el frontend React
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3001") // Puerto del frontend en docker
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Observability: OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder =>
    {
        tracingBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ms-apigateway"))
            .AddAspNetCoreInstrumentation(options =>
            {
                // Ignoramos el endpoint de metrics para que no inunde Jaeger
                options.Filter = context => !context.Request.Path.StartsWithSegments("/metrics") && !context.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation()
            .AddJaegerExporter(opts =>
            {
                opts.AgentHost = builder.Configuration["Jaeger:Host"] ?? "localhost";
                opts.AgentPort = int.Parse(builder.Configuration["Jaeger:Port"] ?? "6831");
            });
    });

var app = builder.Build();

app.UseCors("FrontendPolicy");

app.UseRouting();
app.UseHttpMetrics(); // Prometheus Metrics middleware

app.MapReverseProxy();
app.MapHub<NotificationHub>("/notifications");
app.MapMetrics("/metrics");

// Webhook interno para que MS-EHRLogger avise al Gateway que ya guardó en Mongo
app.MapPost("/internal/notify", async (IHubContext<NotificationHub> hubContext, NotificationDto request) =>
{
    await hubContext.Clients.All.SendAsync("ReceiveEhrNotification", request.Message);
    return Results.Ok();
});

app.Run();

public class NotificationDto
{
    public string Message { get; set; } = string.Empty;
}
