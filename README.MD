Medipass: Sistema Distribudo de Agendamiento Especializado


Medipass es un ecosistema de microservicios de alta complejidad diseñado para la gestión crítica de citas médicas de alta especialidad (Oncología, Cardiología). El sistema implementa patrones avanzados de diseño arquitectónico y de infraestructura para mitigar el error humano, garantizar la consistencia de los datos en tiempo real y asegurar la observabilidad total de las transacciones de salud.


Arquitectura del Sistema
El ecosistema está diseñado bajo el patrón de Arquitectura Hexagonal, garantizando un desacoplamiento estricto entre las reglas del negocio (Dominio), los casos de uso (Aplicación) y los detalles tecnológicos o de comunicación (Infraestructura).

Composición de los Microservicios

MS-AgendaHub (Principal / Core):

Responsabilidad: Gestiona el núcleo del negocio de agendamiento de citas especializadas.
Persistencia: Base de datos relacional independiente en PostgreSQL.
MS-Insurance (Síncrono / REST):

Responsabilidad: Valida en tiempo real y bajo demanda HTTP el estado de cobertura de las pólizas de los pacientes.
Persistencia: Base de datos independiente en Oracle.

MS-EHRLogger (Asíncrono / Broker):

Responsdbilidad: Registra de manera inmutable y en segundo plano los resúmenes de las citas en el Historial Clínico Digital.
Persistencia: Base de datos NoSQL documental orientada a alta velocidad de escritura en MongoDB.

Diagrama:
https://drive.google.com/file/d/1bnwIki5hsP8sAqMCdCeY7UQVfKw4t9Gz/view?usp=sharing