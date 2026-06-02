# MS-Insurance

El **MS-Insurance** es el microservicio responsable de validar la cobertura médica de los pacientes con sus respectivas aseguradoras. 

## Arquitectura y Contexto

A diferencia del MS-AgendaHub, este microservicio es intencionalmente más ligero. En el ecosistema Medipass, su rol es actuar como un simulador o intermediario hacia APIs de aseguradoras de terceros.
Actualmente implementa lógica "en memoria" o "stubbed" para propósitos de demostración y validación rápida en un entorno controlado.

## Tecnologías

- **Framework:** ASP.NET Core 8 Web API
- **Patrón:** Minimal APIs / Controladores Ligeros
- **Observabilidad:** OpenTelemetry, Prometheus-net

## Comunicación

Este servicio es invocado **síncronamente** vía HTTP por el `MS-AgendaHub`.
Durante el flujo de agendamiento, el AgendaHub detiene su proceso y le pregunta al `MS-Insurance` si el `ProcedureId` solicitado está cubierto por la aseguradora del paciente.

## Endpoints Principales

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/api/Insurance/validate` | Recibe el ID del Paciente y el Procedimiento a realizar. Retorna si el seguro aprueba o rechaza la cobertura. |
| `GET`  | `/api/Insurance/procedures` | (Opcional) Retorna un catálogo de procedimientos con sus costos o coberturas simuladas. |

## Reglas de Negocio Simuladas

Para la demostración, el servicio puede tener reglas codificadas (Hardcoded) como:
- Rechazar ciertos procedimientos (ej. Cirugías plásticas).
- Aprobar automáticamente chequeos generales.
- Generar rechazos para pacientes con "seguro vencido".

Esto permite probar el manejo de excepciones de dominio (`DomainException`) y errores HTTP 400/500 en la observabilidad general del sistema.
