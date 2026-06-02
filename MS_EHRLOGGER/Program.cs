using Microsoft.OpenApi.Models;
using MsEhrLogger.Infrastructure.Config;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// ⚙️ Infraestructura (DB, RabbitMQ, Observabilidad) ⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️⚙️
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClient();

// ── Controllers ────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddNewtonsoftJson(opts =>
    {
        opts.SerializerSettings.DateFormatHandling =
            Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
    });

// ── Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "MS-EHRLogger API",
        Version     = "v1",
        Description = """
            **Microservicio Asíncrono — Electronic Health Record Logger**

            Recibe eventos de citas confirmadas desde RabbitMQ (publicados por MS-AgendaHub)
            y persiste el resumen en el Historial Clínico Digital (MongoDB).

            ### Arquitectura
            - **Patrón**: Hexagonal (Ports & Adapters) — misma estructura que MS-AgendaHub
            - **Comunicación**: Asíncrona vía RabbitMQ
            - **Persistencia**: MongoDB (base de datos exclusiva)
            - **Observabilidad**: Prometheus + Grafana + Jaeger

            ### Reglas de Negocio
            1. El proceso de agenda **no espera** la respuesta del EHR (asíncrono)
            2. Idempotencia: mensajes duplicados son detectados y descartados
            3. Dead Letter Queue para reintentos agotados
            """,
        Contact = new OpenApiContact
        {
            Name  = "Medipass Team",
            Email = "devteam@medipass.com"
        }
    });
    c.EnableAnnotations();
});

// ── Health Checks ──────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Pipeline HTTP ──────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MS-EHRLogger v1");
    c.RoutePrefix = "swagger";
    c.DisplayRequestDuration();
});

app.UseRouting();
app.UseHttpMetrics(); // Prometheus: métricas HTTP automáticas
app.MapControllers();
app.MapMetrics("/metrics"); // GET /metrics → Prometheus scrape
app.MapHealthChecks("/health");

app.Run();
