using System.Diagnostics;
using System.Text;
using MsEhrLogger.Application.Ports.In;
using MsEhrLogger.Domain.Exceptions;
using MsEhrLogger.Infrastructure.Dtos;
using MsEhrLogger.Infrastructure.Observability;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MsEhrLogger.Infrastructure.Adapters.Rest;

public class AppointmentConsumerAdapter : BackgroundService
{
    private const string MainExchange  = "medipass.events";
    private const string EhrQueue      = "medipass.ehr.appointments";
    private const string RoutingKey    = "appointment.confirmed";
    private const string DlxExchange   = "medipass.dlx";
    private const string DlqQueue      = "medipass.ehr.appointments.dlq";
    private const string DlqRoutingKey = "ehr.dlq";

    private readonly IEhrLoggerUseCasePort               _useCase;
    private readonly IConnection                         _connection;
    private readonly IModel                              _channel;
    private readonly ILogger<AppointmentConsumerAdapter> _logger;
    private readonly IHttpClientFactory                  _httpClientFactory;

    public AppointmentConsumerAdapter(
        IEhrLoggerUseCasePort                    useCase,
        IConnection                              connection,
        ILogger<AppointmentConsumerAdapter>      logger,
        IHttpClientFactory                       httpClientFactory)
    {
        _useCase           = useCase;
        _connection        = connection;
        _channel           = _connection.CreateModel();
        _logger            = logger;
        _httpClientFactory = httpClientFactory;

        DeclareTopology();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        EhrMetrics.ConsumerActive.Set(1);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceivedAsync;

        _channel.BasicConsume(
            queue:    EhrQueue,
            autoAck:  false,
            consumer: consumer);

        _logger.LogInformation(
            "[EHR-Consumer] Escuchando cola {Queue} en exchange {Exchange}",
            EhrQueue, MainExchange);

        return Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        var body    = args.Body.ToArray();
        var payload = Encoding.UTF8.GetString(body);

        _logger.LogInformation(
            "[EHR-Consumer] Mensaje recibido RoutingKey={Key}", args.RoutingKey);

        var sw = Stopwatch.StartNew();

        try
        {
            var ehrEvent = JsonConvert.DeserializeObject<AppointmentConfirmedEvent>(payload);
            if (ehrEvent is null)
                throw new InvalidEhrRecordException("El payload del evento es nulo o inválido.");

            _logger.LogInformation(
                "[EHR-Consumer] Procesando AppointmentId={Id} CorrelationId={Corr}",
                ehrEvent.AppointmentId, ehrEvent.CorrelationId);

            await _useCase.ProcessAppointmentConfirmedAsync(ehrEvent);
            _channel.BasicAck(args.DeliveryTag, multiple: false);

            sw.Stop();
            EhrMetrics.RecordsSaved.Inc();
            EhrMetrics.ProcessingDuration.Observe(sw.Elapsed.TotalSeconds);

            _logger.LogInformation(
                "[EHR-Consumer] Registro EHR guardado en {Ms}ms", sw.ElapsedMilliseconds);

            // Notificar al Frontend vía API Gateway (SignalR Webhook)
            try 
            {
                var client = _httpClientFactory.CreateClient();
                var notification = new { Message = $"Historial Clínico del paciente '{ehrEvent.PatientId}' actualizado (Cita: {ehrEvent.AppointmentId})" };
                var content = new StringContent(JsonConvert.SerializeObject(notification), Encoding.UTF8, "application/json");
                await client.PostAsync("http://ms-apigateway:5000/internal/notify", content);
            }
            catch(Exception ex)
            {
                _logger.LogWarning("[EHR-Consumer] No se pudo notificar al Frontend: {Msg}", ex.Message);
            }
        }
        catch (InvalidEhrRecordException ex)
        {
            sw.Stop();
            _logger.LogError(
                "[EHR-Consumer] Error de dominio (no reintentable): {Msg}", ex.Message);

            EhrMetrics.DeadLetterMessages.Inc();
            _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[EHR-Consumer] Error transitorio procesando mensaje");
            _channel.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
        }
    }


    private void DeclareTopology()
    {
        _channel.ExchangeDeclare(DlxExchange, ExchangeType.Direct, durable: true);

        _channel.QueueDeclare(DlqQueue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(DlqQueue, DlxExchange, DlqRoutingKey);

        _channel.ExchangeDeclare(MainExchange, ExchangeType.Topic, durable: true);

        var queueArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"]    = DlxExchange,
            ["x-dead-letter-routing-key"] = DlqRoutingKey,
            ["x-message-ttl"]             = 300_000 // 5 minutos
        };

        _channel.QueueDeclare(EhrQueue,
            durable:    true,
            exclusive:  false,
            autoDelete: false,
            arguments:  queueArgs);

        _channel.QueueBind(EhrQueue, MainExchange, RoutingKey);

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
    }

    public override void Dispose()
    {
        EhrMetrics.ConsumerActive.Set(0);
        _channel?.Close();
        base.Dispose();
    }
}
