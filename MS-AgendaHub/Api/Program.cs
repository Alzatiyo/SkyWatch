using Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001);
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "MS-AgendaHub - Medipass",
        Version     = "v1",
        Description = "Microservicio Principal de Agendamiento de Citas Médicas Especializadas."
    });
});

builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracingBuilder =>
    {
        tracingBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("ms-agendahub"))
            .AddAspNetCoreInstrumentation(options => 
            {
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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    int retries = 10;
    while (retries > 0)
    {
        try
        {
            context.Database.Migrate();
            Console.WriteLine("Migraciones aplicadas correctamente en AgendaHub.");
            break;
        }
        catch (Exception ex)
        {
            retries--;
            Console.WriteLine($"DB no disponible. Reintentando... ({retries} intentos). {ex.Message}");
            if (retries == 0) throw;
            Thread.Sleep(5000);
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MS-AgendaHub v1");
});

app.UseRouting();
app.UseHttpMetrics();        
app.UseAuthorization();
app.MapControllers();
app.MapMetrics("/metrics");  
app.MapHealthChecks("/health");

app.Run();
