# Regla: Arquitectura

## Estructura obligatoria

```text
backend/   ASP.NET Core (.NET 10 LTS) — monolito modular
frontend/  React + TypeScript + Vite
evals/     Dataset, runner y reportes de evaluación de prompts
docs/      Documentación técnica y de producto
.claude/   Reglas y skills para contribuciones asistidas
.github/   CI
```

## Backend

- `PostulaAI.Api` es el host. Cada módulo vive en `Modules/<Modulo>/` y contiene sus endpoints/controladores, servicios, DTOs, validadores y configuración de EF Core.
- Módulos: `Auth`, `Users`, `ProfessionalProfiles`, `JobPostings`, `Analyses`, `UsageLimits`, `AuditLogs`. Código transversal en `Shared/`.
- **`PostulaAI.AI` es una biblioteca separada** (`Clients/`, `Prompts/`, `Schemas/`, `Services/`, `Evaluations/`). Producción (`PostulaAI.Api`) y el runner de evaluaciones (`evals/runner`) la referencian; **nunca** dupliques prompts, esquemas o validadores fuera de ella.
- `PostulaAI.AI` no depende de `PostulaAI.Api`, de EF Core ni de ASP.NET Core. Solo recibe y devuelve modelos propios.
- Todo acceso a proveedores de IA pasa por `IAIClient`. Ningún módulo llama a la API de Claude directamente.
- Un módulo no accede a las tablas de otro módulo saltándose su servicio, salvo lecturas de solo-lectura documentadas.

## Controladores / endpoints

- Delgados: reciben la petición, validan forma, llaman a un servicio y mapean el resultado. Sin lógica de negocio, sin acceso directo a `DbContext`, sin prompts.
- Rutas versionadas bajo `/api/v1`.
- Errores con `ProblemDetails` (RFC 9457) e identificador de correlación.

## Datos

- PostgreSQL + EF Core con migraciones versionadas en el repositorio.
- Toda entidad de usuario tiene `UserId` y toda consulta filtra por el usuario autenticado.
- Los análisis guardan snapshots inmutables del perfil y la vacante.

## Frontend

- Organización por feature: `src/features/<feature>/`, código compartido en `src/shared/`.
- Acceso a la API solo mediante `src/shared/api/`. Ningún componente hace `fetch` directo.
- Nunca se llama a la API de Claude desde el frontend.

## Cambios que requieren aprobación explícita

- Cambiar el stack (lenguaje, framework, base de datos).
- Mover lógica de IA fuera de `PostulaAI.AI`.
- Modificar el esquema de salida del análisis.
- Cambiar el modelo de autenticación.
