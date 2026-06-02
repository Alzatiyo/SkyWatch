# MS-EHRLogger

El **MS-EHRLogger** (Electronic Health Record Logger) es un microservicio fundamental en el ecosistema Medipass, especializado en la persistencia inmutable y asíncrona de los historiales clínicos.

## Arquitectura y Contexto

Este servicio funciona principalmente como un **Consumidor / Worker Service**. Su arquitectura está orientada a eventos (Event-Driven). 
No necesita ser llamado explícitamente por el usuario ni por otros servicios de manera síncrona; su trabajo es reaccionar a eventos en la red de mensajería, procesarlos y guardarlos para cumplimiento normativo y registro médico.

## Tecnologías

- **Framework:** ASP.NET Core 8 Web API / Worker
- **Base de Datos:** MongoDB (NoSQL)
- **Mensajería:** MassTransit, RabbitMQ
- **Observabilidad:** OpenTelemetry (con soporte especial para propagación de trazas en RabbitMQ).

## Flujo de Trabajo

1. **Escucha:** El servicio está suscrito a la cola de RabbitMQ esperando el evento de integración `EhrEvent` (publicado por `MS-AgendaHub` cuando se completa una cita).
2. **Procesa:** Convierte los datos del evento en un documento BSON estructurado (Log Médico).
3. **Persiste:** Guarda el registro en una colección de MongoDB de manera inmutable.
4. **Notifica:** Una vez el guardado es exitoso, realiza una petición `POST` interna (vía HTTP) al `MS-ApiGateway` en el endpoint `/internal/notify` para informar que la acción se completó con éxito.

## Endpoints y Colas

| Tipo | Ruta / Cola | Descripción |
|------|-------------|-------------|
| **Cola AMQP** | `ehr-event-queue` | Consumidor de mensajes de RabbitMQ provenientes del AgendaHub. |
| **HTTP GET** | `/api/EhrLog` | Endpoint de consulta de solo lectura para listar los logs clínicos guardados. |

## Base de Datos NoSQL

Se seleccionó **MongoDB** para este servicio porque los registros de Historial Clínico (EHR) pueden variar fuertemente en esquema dependiendo del procedimiento o especialidad. Además, la persistencia orientada a documentos es altamente eficiente para grandes volúmenes de logs inmutables que rara vez sufren modificaciones (Append-only).
