# Arquitectura

> Estado: **Fase 1**. Este documento describe la arquitectura objetivo del MVP. Lo que aún no existe en código está marcado como *(pendiente — Fase N)*.

## Vista general

```text
 Navegador ──► Frontend (React + Vite, servido por nginx)
                 │  /api/*  (mismo origen: la cookie del refresh token puede ser SameSite=Strict)
                 ▼
             PostulaAI.Api (ASP.NET Core, .NET 10 LTS) ──► PostgreSQL 16
                 │
                 └─► PostulaAI.AI (biblioteca) ──► IAIClient ──► Claude API
                                    ▲
             evals/runner ──────────┘   (mismos prompts, esquema y validadores)
```

## Decisiones

| Decisión | Motivo |
|---|---|
| **Monorepo** | Un cambio de prompt y la actualización de sus evaluaciones van en el mismo commit; CI y `docker-compose` únicos. |
| **Monolito modular** (no microservicios) | Un producto en fase MVP no justifica el coste operativo de servicios separados. Los módulos tienen fronteras claras para poder separarse si hiciera falta. |
| **`PostulaAI.AI` como biblioteca propia** | Producción y el runner de evaluaciones comparten exactamente el mismo código de IA; una prueba de arquitectura impide que dependa de ASP.NET Core o EF Core. |
| **Controladores delgados + servicios por módulo** | La lógica de negocio queda testeable sin HTTP. |
| **Snapshots en cada análisis** | Si el usuario edita su perfil o vacante después, el análisis histórico sigue siendo coherente con lo que se evaluó. |
| **Análisis síncrono en v1** | Más simple de construir y probar. El campo `Status` deja preparado el paso a una cola en segundo plano (ver roadmap). |
| **Mismo origen para frontend y API** | Permite cookie `SameSite=Strict` para el refresh token y evita configurar CORS con credenciales en producción. |

## Estructura del backend

```text
backend/
  PostulaAI.slnx · global.json · Directory.Build.props · Directory.Packages.props
  src/PostulaAI.Api/
    Program.cs
    Modules/
      Auth/ Users/ ProfessionalProfiles/ JobPostings/ Analyses/ UsageLimits/ AuditLogs/
    Shared/
      Errors/        ProblemDetails, códigos de error, manejador global
      Persistence/   AppDbContext, migraciones
  src/PostulaAI.AI/
    Clients/ Prompts/ Schemas/ Services/ Evaluations/
  tests/PostulaAI.UnitTests · tests/PostulaAI.IntegrationTests
```

Implementado en Fase 0: host, ProblemDetails, manejador global de excepciones, `AppDbContext` vacío, health checks (`/health/live`, `/health/ready`), OpenAPI + Swagger UI en desarrollo, CORS configurable, contrato preliminar de `IAIClient`, prueba de arquitectura y pruebas de los endpoints de salud.

## Modelo de datos

Implementado en Fase 1: `User`, `RefreshToken`, `ProfessionalProfile` y sus tablas hijas, `AuditLog` (migración `InitialCreate`). `JobPosting` y `Analysis`: *pendiente — Fases 2 y 3*.

```text
User 1──* RefreshToken
User 1──* ProfessionalProfile 1──* WorkExperience | Education | Project | Certification | ProfileSkill | ProfileLanguage
User 1──* JobPosting
User 1──* Analysis *──0..1 ProfessionalProfile
                  *──0..1 JobPosting
User 1──* AuditLog
```

| Entidad | Campos principales |
|---|---|
| `User` | Id, Email (único), PasswordHash, DisplayName, CreatedAt, UpdatedAt |
| `RefreshToken` | Id, UserId, TokenHash, FamilyId, ExpiresAt, CreatedAt, RevokedAt, ReplacedByTokenId |
| `ProfessionalProfile` | Id, UserId, FullName, Headline, Summary, LinkedInUrl, GitHubUrl, PortfolioUrl, timestamps |
| `WorkExperience` | Id, ProfileId, Company, Role, StartDate, EndDate?, IsCurrent, Description, Achievements, Technologies, SortOrder |
| `Education` | Id, ProfileId, Institution, Degree, FieldOfStudy, StartDate, EndDate?, Status |
| `Project` | Id, ProfileId, Name, Description, Technologies, Url? |
| `Certification` | Id, ProfileId, Name, Issuer, IssuedOn?, Url? |
| `ProfileSkill` | Id, ProfileId, Name, Category (`technical` \| `soft`) |
| `ProfileLanguage` | Id, ProfileId, Language, Proficiency |
| `JobPosting` | Id, UserId, Title, Company, Location, WorkMode (`onsite` \| `hybrid` \| `remote` \| `unspecified`), Description, timestamps |
| `Analysis` | Id, UserId, ProfileId?, JobPostingId?, Status (`pending` \| `completed` \| `failed`), CompatibilityScore?, ResultJson? (jsonb, solo si es válido), ProfileSnapshot (jsonb), JobPostingSnapshot (jsonb), PromptId, PromptVersion, Model, Language, InputTokens, OutputTokens, LatencyMs, RepairAttempted, ErrorCode?, CreatedAt, CompletedAt? |
| `AuditLog` | Id, UserId?, Action, EntityType, EntityId?, CorrelationId, Metadata (jsonb, sin datos personales), OccurredAt |

En v1 hay **un perfil por usuario**, garantizado con un índice único en `professional_profiles.UserId`; admitir varios en el futuro solo requiere una migración que elimine ese índice. Las listas de tecnologías y logros se guardan como `text[]`, y los enums como texto. Los límites de uso se calculan contando intentos de `Analysis` por usuario en una ventana configurable, sin tabla adicional.

## Endpoints

Todos bajo `/api/v1`, salvo salud. Errores en formato ProblemDetails con `errorCode` y `traceId`.

| Método | Ruta | Descripción | Estado |
|---|---|---|---|
| GET | `/health/live` | Proceso activo | ✅ Fase 0 |
| GET | `/health/ready` | Base de datos accesible | ✅ Fase 0 |
| GET | `/openapi/v1.json` | Documento OpenAPI (solo desarrollo) | ✅ Fase 0 |
| POST | `/auth/register` | Registro | ✅ Fase 1 |
| POST | `/auth/login` | Inicio de sesión → access token + cookie de refresh | ✅ Fase 1 |
| POST | `/auth/refresh` | Rota el refresh token | ✅ Fase 1 |
| POST | `/auth/logout` | Revoca la familia de tokens | ✅ Fase 1 |
| GET | `/auth/me` | Usuario actual | ✅ Fase 1 |
| GET / PUT | `/profiles/me` | Leer / reemplazar el perfil completo | ✅ Fase 1 |
| GET / POST | `/job-postings` | Listar (paginado) / crear | Fase 2 |
| GET / PUT / DELETE | `/job-postings/{id}` | Detalle / editar / eliminar | Fase 2 |
| POST | `/analyses` | Ejecutar análisis `{ jobPostingId, language }` | Fase 3 |
| GET | `/analyses` | Historial paginado | Fase 2–3 |
| GET / DELETE | `/analyses/{id}` | Detalle / eliminar | Fase 3 |
| GET | `/usage/me` | Análisis restantes en la ventana actual | Fase 3 |

**Autorización:** toda consulta filtra por el `UserId` del token. Un recurso de otro usuario responde `404`.

## Frontend

```text
frontend/src/
  app/        rutas, layouts (público y de aplicación), página 404
  features/   landing · auth · dashboard · profile · jobs · analyses · settings
  shared/     ui/ (componentes accesibles) · api/ (cliente HTTP tipado, sesión en memoria) · lib/
```

Rutas: `/`, `/iniciar-sesion`, `/registro`, `/app`, `/app/perfil`, `/app/vacantes/nueva`, `/app/vacantes/:id/editar`, `/app/analisis/nuevo`, `/app/analisis`, `/app/analisis/:id`, `/app/configuracion`.

Herramientas: React 19, React Router, TanStack Query, react-hook-form + zod, Tailwind CSS 4, TypeScript estricto, oxlint, Vitest + Testing Library + MSW.

Sesión en el frontend: el access token vive solo en memoria; al cargar la app se restaura llamando a `/auth/refresh` (la cookie `HttpOnly` la envía el navegador). Las renovaciones son de un solo vuelo para no disparar la detección de reutilización. Las rutas `/app/*` están protegidas por `RequireAuth`.

## Despliegue

`docker-compose.yml` define `postgres`, `api` y `frontend`. nginx sirve el frontend y hace de proxy de `/api` hacia la API. El despliegue público queda para la Fase 6.
