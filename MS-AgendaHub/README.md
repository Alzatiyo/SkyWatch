# MS-AgendaHub

El **MS-AgendaHub** es el microservicio central o *Core* del ecosistema Medipass. Su responsabilidad principal es la gestión de pacientes, doctores y el agendamiento de citas médicas.

## Arquitectura

Este microservicio implementa una **Arquitectura Hexagonal (Ports & Adapters)** estricta, también conocida como *Clean Architecture*. Esto garantiza que el dominio del negocio sea completamente agnóstico de las tecnologías externas (Bases de datos, APIs de terceros, etc.).

### Capas principales:
1. **Domain:** Contiene las entidades, enumeradores, excepciones de dominio, constructores (Builders) y los servicios de dominio (lógica de negocio pura).
2. **Application:** Contiene los Casos de Uso (Use Cases) y define los **Puertos** (Interfaces de entrada y salida).
3. **Infrastructure:** Contiene los **Adaptadores** que implementan los puertos definidos en Application. Aquí reside la comunicación con la Base de Datos (SQL Server), el cliente HTTP para consultar `MS-Insurance`, y el publicador de eventos hacia RabbitMQ (MassTransit).
4. **Api:** El host de ASP.NET Core que expone los controladores REST, configura inyección de dependencias y la observabilidad (OpenTelemetry).

## Tecnologías

- **Framework:** ASP.NET Core 8 Web API
- **Base de Datos:** SQL Server 2022
- **ORM:** Entity Framework Core (Code-First)
- **Mensajería:** MassTransit (RabbitMQ)
- **Observabilidad:** OpenTelemetry, Prometheus-net

## Dependencias Externas

- **MS-Insurance:** Comunicación síncrona vía HTTP (`HttpClient`) para validar la cobertura del seguro antes de agendar.
- **RabbitMQ:** Comunicación asíncrona. Publica eventos (`EhrEvent`) cuando una cita es agendada con éxito para que el `MS-EHRLogger` la procese.

## Endpoints Principales

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `GET` | `/api/Catalog/doctors` | Retorna la lista de doctores disponibles |
| `GET` | `/api/Catalog/patients` | Retorna la lista de pacientes registrados |
| `GET` | `/api/Catalog/procedures` | Retorna la lista de procedimientos médicos |
| `POST`| `/api/Appointment` | Crea una nueva cita médica. Falla si el seguro no la cubre (Validación con Insurance) |

*(Nota: En producción, estos endpoints se consumen a través del MS-ApiGateway, no directamente).*

---

### Estructura de Directorios

```text
Aplication
│
├── Ports
│   ├── In  (IAppointmentUseCasePort.cs)
│   └── Out (IAppointmentRepositoryPort.cs, IInsuranceServicePort.cs, etc.)
│
└── UseCases (AppointmentUseCase.cs)

Domain
│
├── Builders (AppointmentBuilder.cs)
├── Enums    (AppointmentStatus.cs, InsuranceStatus.cs, etc.)
├── Exceptions (DomainException.cs)
├── Models   (Appointment.cs, Doctor.cs, Patient.cs)
└── Services (AppointmentService.cs)

Infrastructure
│
├── Adapters
│   ├── Persistence (Entidades de EF Core y el Adapter de Repositorio)
│   └── Rest        (Controladores y Adapters externos HTTP/RabbitMQ)
├── Config          (AppDbContext y Configuración de Servicios)
├── Dtos            
├── Mappers         
└── Migrations      (Migraciones de EF Core)
```