# MS-ApiGateway

El **MS-ApiGateway** es el punto de entrada unificado y la puerta de enlace pública de todo el ecosistema Medipass. Sirve tanto para enrutar tráfico HTTP hacia los microservicios internos, como para mantener conexiones bidireccionales en tiempo real con los clientes web.

## Arquitectura y Contexto

Aplica los patrones arquitectónicos **API Gateway** y **Reverse Proxy**. Esto evita que el Frontend (aplicación React) tenga que conocer la topología de la red interna o lidiar con múltiples URLs e IPs de los distintos microservicios.
Además, actúa como la capa que inicia el rastreo distribuido (Distributed Tracing), inyectando el primer `TraceId` en las cabeceras HTTP antes de enrutarlas.

## Tecnologías

- **Framework:** ASP.NET Core 8
- **Proxy Reverso:** YARP (Yet Another Reverse Proxy)
- **Tiempo Real:** SignalR (WebSockets)
- **Observabilidad:** OpenTelemetry (El punto de partida del Trace)

## YARP (Yet Another Reverse Proxy)

Se utiliza YARP porque es nativo de .NET, altamente performante y fácil de configurar a través del `appsettings.json`.
El archivo de configuración de YARP define:
- **Routes:** Las URLs de entrada (ej. `/api/agendahub/{**catch-all}`).
- **Clusters:** Los destinos físicos a donde se reenvía el tráfico (ej. `http://ms-agendahub:5001`).

## SignalR (Notificaciones en Tiempo Real)

El Gateway hospeda un Hub de SignalR (`NotificationHub`) en la ruta `/notifications`. 
Dado que el microservicio `MS-EHRLogger` guarda registros de forma asíncrona, el usuario en el Frontend no sabe cuándo terminó realmente de procesarse su cita. 

**Flujo:**
1. El cliente (React) se conecta al Hub de SignalR en el Gateway.
2. Cuando `MS-EHRLogger` termina de guardar en MongoDB, hace una petición HTTP POST al endpoint interno del Gateway (`/internal/notify`).
3. El Gateway recibe esta petición e invoca el método `ReceiveEhrNotification` en todos los clientes de SignalR conectados.
4. El Frontend recibe el evento y despliega una tostada o alerta de éxito ("Registro guardado exitosamente en el EHR").

## Endpoints

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `CUALQUIERA` | `/api/agendahub/*` | Redirigido automáticamente por YARP al MS-AgendaHub. |
| `WS/HTTP` | `/notifications` | Hub de conexión para WebSockets (SignalR). |
| `POST` | `/internal/notify` | Webhook interno para que el MS-EHRLogger dispare eventos SignalR al Frontend. |
