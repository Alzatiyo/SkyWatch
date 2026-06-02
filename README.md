# Medipass Ecosystem

Medipass es un ecosistema de microservicios de alta complejidad diseñado para la gestión crítica de citas médicas de alta especialidad (Oncología, Cardiología), validación de seguros de salud (Insurance) y registro de historias clínicas electrónicas (EHR). El sistema está construido con un enfoque en alta disponibilidad, observabilidad integral y comunicación asíncrona para mitigar el error humano y garantizar la consistencia de los datos en tiempo real.

## Arquitectura General

El sistema se compone de varios microservicios y un Gateway que orquesta las peticiones, implementando patrones como **Hexagonal Architecture (Ports & Adapters)**, **Event-Driven Architecture (RabbitMQ)**, y **Reverse Proxy (YARP)**.

[**Diagrama de Arquitectura**](https://drive.google.com/file/d/1bnwIki5hsP8sAqMCdCeY7UQVfKw4t9Gz/view?usp=sharing)

### Flujo Principal de Agendamiento
1. El **Frontend (React)** envía una petición de agendamiento al **MS-ApiGateway**.
2. El **MS-ApiGateway** enruta la petición al **MS-AgendaHub**.
3. El **MS-AgendaHub** valida de manera *síncrona* (HTTP) con el **MS-Insurance** si el procedimiento está cubierto por el seguro del paciente.
4. Si es válido, se guarda la cita en **SQL Server** y se emite un evento asíncrono a **RabbitMQ**.
5. El **MS-EHRLogger** consume el evento de RabbitMQ, guarda un registro inmutable en **MongoDB** y notifica al Gateway mediante un webhook interno.
6. El **MS-ApiGateway** recibe la notificación y la empuja al Frontend en tiempo real vía **SignalR**.

---

## Stack Tecnológico

- **Backend:** .NET 8 (C#)
- **Frontend:** React, Vite, TailwindCSS
- **Bases de Datos:** SQL Server (Relacional) y MongoDB (NoSQL)
- **Mensajería:** RabbitMQ (MassTransit)
- **Proxy/Gateway:** YARP (Yet Another Reverse Proxy)
- **Tiempo Real:** SignalR
- **Observabilidad:** OpenTelemetry, Prometheus (Métricas), Grafana (Dashboards), Jaeger (Trazabilidad Distribuida)
- **Orquestación:** Docker & Docker Compose

---

## Microservicios

1. **[MS-ApiGateway](./MS-ApiGateway/README.md):** Punto de entrada principal. Proxy reverso (YARP) y Hub de notificaciones en tiempo real (SignalR).
2. **[MS-AgendaHub](./MS-AgendaHub/README.md):** Core del negocio. Manejo de doctores, pacientes y agendamiento de citas.
3. **[MS-Insurance](./MS-Insurance/README.md):** Validador de coberturas médicas.
4. **[MS-EHRLogger](./MS-EHRLOGGER/README.md):** Historial Clínico Electrónico. Consumidor de eventos y persistencia NoSQL.

---

## Cómo ejecutar el proyecto

Todo el entorno está dockerizado para facilitar su levantamiento con un solo comando.

```bash
# Levantar todo el ecosistema (Bases de datos, RabbitMQ, Observabilidad y Microservicios)
docker-compose up -d --build

# Para detener el entorno y borrar los volúmenes (resetear BDs):
docker-compose down -v
```

### Puertos de los Servicios (Docker)

| Servicio | Puerto Local | Descripción |
|----------|--------------|-------------|
| **Frontend** | `3001` | Interfaz de usuario (React) |
| **MS-ApiGateway** | `5000` | Entrada unificada (YARP + SignalR) |
| **MS-AgendaHub** | `5001` | API de Agendamiento |
| **MS-Insurance** | `5002` | API de Seguros |
| **MS-EHRLogger** | `5003` | API y Worker de Logs Clínicos |
| **Grafana** | `3000` | Dashboards de Observabilidad |
| **Jaeger (UI)** | `16686` | Interfaz de Trazabilidad |
| **Prometheus** | `9090` | Servidor de Métricas |
| **RabbitMQ (UI)**| `15672` | Panel de control de mensajes |

---

## Observabilidad

Todo el ecosistema está instrumentado con **OpenTelemetry**. 
- **Métricas:** Los servicios exponen un endpoint `/metrics` que Prometheus raspa (scrape) automáticamente. Grafana consume estos datos para mostrar el "Medipass Dashboard" con conteo de errores, latencia, CPU/Memoria y métricas de negocio.
- **Trazabilidad (Traces):** Las peticiones HTTP y los mensajes de RabbitMQ inyectan y propagan el contexto de OpenTelemetry (`TraceId`). Esto permite ver en Jaeger el ciclo de vida completo de una petición, desde que entra al Gateway hasta que se procesa en el Logger de MongoDB.
