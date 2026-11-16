# Modelo de amenazas – Jun48714/Academico

> Construido a partir del código y de las respuestas del propietario (Junior). Lo marcado como *(inferido)* es lectura del código; lo marcado como **[propietario]** lo confirmó el usuario.

## 1. Visión general del sistema
Sistema de gestión de asesoría académica (estudiantes, profesores, grupos y líneas de investigación, citas, contratos de asesoría) basado en la plantilla "Clean Architecture .NET 8". Un único servicio ASP.NET Core expone REST (`/api/v1/*`) y gRPC, con SQL Server (EF Core), Redis (caché distribuida), RabbitMQ (eventos de dominio) y una integración con Google Calendar. Hay un frontend Angular (`AppTarea/`) que consume la API de citas.

## 2. Componentes
| Componente | Código que cubre |
|---|---|
| **API** | `CleanArchitecture.Api/` (controladores REST, `Program.cs`, CORS, health checks, `BackgroundServices/`) |
| **Auth** | `CleanArchitecture.Api/Extensions/ServiceCollectionExtension.cs` (JWT), `CleanArchitecture.Domain/Commands/Users/*` (login, cambio de contraseña, roles), `CleanArchitecture.Domain/Settings/TokenSettings.cs` |
| **Dominio y Datos** | `CleanArchitecture.Application/`, `CleanArchitecture.Domain/` (comandos, handlers, validaciones, RabbitMQ), `CleanArchitecture.Infrastructure/` (EF Core, repositorios, event store), `CleanArchitecture.Shared/` |
| **gRPC** | `CleanArchitecture.Application/gRPC/`, `CleanArchitecture.gRPC/`, `CleanArchitecture.Proto/` |
| **Calendar** | `CleanArchitecture.Api/Services/GoogleCalendarService.cs`, `CleanArchitecture.Api/Controllers/CalendarController.cs`, `calendarSettings*.json` |
| **Frontend** | `AppTarea/` (Angular) |
| **Infraestructura** | `k8s-deployments/`, `docker-compose.yml`, `Dockerfile`, `azure-pipelines.yml`, `appsettings*.json`, archivo `edy` (clave privada OpenSSH en la raíz) |

## 3. Límites de confianza y puntos de entrada
- **Internet → API**: `k8s-deployments/clean-architecture.yml` publica el puerto 80 con un Service `LoadBalancer` y CORS es `AllowAll` (`Program.cs`) *(inferido)*. **[propietario]** Es un proyecto académico/demo sin despliegue real; aun así el propietario quiere que se consideren atacantes anónimos de Internet.
- **Endpoints anónimos** *(inferido)*: `POST /api/v1/user/login` (`[AllowAnonymous]`); `AppointmentController`, `ProfessorController`, `StudentController`, `ResearchGroupController`, `ResearchLineController`, `AdvisoryContractController`, `MyController` tienen `[Authorize]` comentado o ausente; `CalendarController` no tiene autorización; `/healthz` público.
- **Endpoints autenticados**: `UserController` y `TenantController` (`[Authorize]`); autorización por rol Admin/User dentro de los handlers (`_user.GetUserRole()`).
- **gRPC** (`UsersApiImplementation`, `TenantsApiImplementation`): sin `[Authorize]` visible; reflexión gRPC solo en Development.
- **API → SQL Server / Redis / RabbitMQ**: servicios internos de k8s (ClusterIP); Redis sin contraseña (`ALLOW_EMPTY_PASSWORD`), credenciales de SQL/RabbitMQ en texto plano en manifiestos y `appsettings.json`.
- **API → Google Calendar**: OAuth de usuario con `credentials.json`/`token.json` en disco (`../Credentials`), un único calendario compartido (`primary`).

## 4. Activos y sensibilidad
- **[propietario] Activo crítico:** datos personales de estudiantes, profesores y usuarios (nombre, email, relaciones de asesoría).
- **[propietario] Activo crítico:** credenciales y el secreto de firma JWT (`Auth:Secret` en `appsettings.json`), hashes de contraseña (BCrypt): con el secreto se pueden forjar tokens Admin.
- Citas, contratos de asesoría, grupos/líneas de investigación – media.
- Calendario de Google y su token OAuth – media/alta.
- Credenciales de infraestructura (SA de SQL Server, RabbitMQ, clave privada `edy`) – alta.

## 5. Perfiles de atacante
**[propietario]** Todos los atacantes cuentan:
1. Anónimo en Internet (alcanza la API y los endpoints sin auth).
2. Usuario autenticado con rol `User` que intenta escalar a `Admin` o leer/modificar datos de otros.
3. Usuario de un tenant que intenta acceder a datos de otro tenant.
4. *(inferido)* Quien tenga acceso de lectura al repositorio (secretos versionados).

## 6. Controles existentes
- JWT HMAC-SHA256 con validación de issuer/audience/lifetime (nota: `RequireSignedTokens = false`).
- Contraseñas con BCrypt; validación de comandos con FluentValidation.
- Chequeos de rol en handlers (`CreateUser`, `UpdateUser`, `Update/DeleteAppointment`...).
- EF Core parametrizado; Swagger y reflexión gRPC solo en Development.

## 7. Fuera de alcance / riesgo aceptado
- **[propietario]** El frontend Angular `AppTarea/` está fuera de alcance: los hallazgos que solo afecten a `AppTarea/` no merecen corrección.
- **[propietario]** Todo lo demás cuenta (no hay riesgo aceptado), incluidas las credenciales versionadas en docker-compose/k8s/appsettings y el archivo `edy`.
- **[propietario]** No hay despliegue real (proyecto académico); no se rebaja nada por ello porque el propietario pidió considerar todos los atacantes.

## 8. Preguntas abiertas
- ¿El archivo `edy` (clave privada OpenSSH) da acceso a algún sistema real?
- ¿El calendario de Google configurado es real o de prueba?
