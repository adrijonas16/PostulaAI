# PostulaAI

Plataforma web que compara tu perfil profesional con una vacante y te dice, con evidencia, en qué coincides, qué te falta y cómo adaptar tu postulación, **sin inventar nada**.

> **Estado del proyecto: Fases 1 a 4 completadas** (cuentas, perfil, vacantes y análisis con IA con su interfaz de resultados). Las evaluaciones reproducibles del prompt (Fase 5) están pendientes y **no se ha medido aún la calidad del prompt**. Consulta el [roadmap](docs/roadmap.md).

## Problema

Muchas personas envían el mismo CV a todas las vacantes. Adaptarlo bien exige comparar requisito por requisito, distinguir lo obligatorio de lo deseable y reconocer honestamente lo que falta. Las herramientas de IA genéricas suelen "embellecer" el perfil con experiencia que la persona no tiene.

## Para quién

Estudiantes, perfiles junior, desarrolladores y profesionales que quieren adaptar sus postulaciones, y consultores de empleabilidad que los acompañan.

## Propuesta de valor

Compatibilidad **explicada** (nunca un número aislado), fortalezas respaldadas por el perfil, brechas reales y textos adaptados que usan únicamente información que el usuario proporcionó.

## Funcionalidades del MVP

| Funcionalidad | Fase |
|---|---|
| Registro, inicio y cierre de sesión | ✅ 1 |
| Perfil profesional: experiencia, educación, proyectos, certificaciones, habilidades, idiomas y enlaces | ✅ 1 |
| Vacantes: crear, editar, eliminar e historial con búsqueda | ✅ 2 |
| Análisis con IA: compatibilidad estimada, fortalezas, brechas, palabras clave, recomendaciones, resumen adaptado y carta de presentación | ✅ 3–4 |
| Historial y detalle de análisis | ✅ 3–4 |
| Evaluación reproducible del prompt | 5 |

## Política de no invención

La IA **nunca** afirma experiencia, estudios, habilidades, certificaciones o logros que el usuario no escribió. Cada fortaleza debe citar evidencia del perfil. Lo que no está respaldado se reporta como brecha. Las salidas se validan con un esquema y reglas de negocio, y los casos trampa del dataset de evaluación miden este comportamiento. Todo resultado incluye un aviso: es una recomendación y no garantiza contratación. Detalle en [docs/ai-system.md](docs/ai-system.md).

## Tecnologías

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core sobre **.NET 10 LTS**, Entity Framework Core, Npgsql |
| Base de datos | PostgreSQL 16 |
| Frontend | React 19, TypeScript (strict), Vite, React Router, TanStack Query, react-hook-form + zod, Tailwind CSS |
| IA | Claude API (solo desde el backend) detrás de la interfaz `IAIClient` |
| Pruebas | xUnit, WebApplicationFactory + PostgreSQL real · Vitest, Testing Library, MSW |
| Calidad | Warnings como errores en C#, oxlint, gitleaks, Dependabot |
| Infraestructura | Docker Compose, GitHub Actions |

## Arquitectura

Monorepo con un monolito modular en .NET y una biblioteca de IA aislada que comparten producción y el runner de evaluaciones.

```text
backend/    PostulaAI.Api (módulos Auth, Users, ProfessionalProfiles, JobPostings, Analyses, UsageLimits, AuditLogs)
            PostulaAI.AI  (Clients, Prompts, Schemas, Services, Evaluations)
frontend/   React + Vite (app, features, shared)
evals/      dataset, runner, resultados y reportes
docs/       arquitectura, IA, evaluación, seguridad, privacidad, roadmap
```

Más detalle en [docs/architecture.md](docs/architecture.md).

## Flujo de IA

```text
Perfil + vacante → prompt versionado → Claude API → JSON estructurado
→ validación de esquema → validación de reglas de negocio
→ reparación controlada (máx. 1) → guardado solo si es válido
```

Cada análisis guarda su estado, el prompt y su versión, el modelo, el idioma, la latencia, los tokens, si hubo reparación y una copia del perfil y la vacante usados.

## Ejecutar localmente

### Requisitos

- .NET SDK 10.0
- Node.js 22+
- Docker (para PostgreSQL o para el stack completo)

### Opción rápida (Docker)

Con Docker Desktop abierto, un solo comando crea `.env` con secretos aleatorios, construye todo y espera a que esté listo:

```powershell
# Windows
powershell -ExecutionPolicy Bypass -File scripts\dev-up.ps1
```

```bash
# macOS / Linux
./scripts/dev-up.sh
```

Luego abre http://localhost:5173. Para detenerlo: `docker compose down`.

### Configuración manual

```bash
cp .env.example .env     # reemplaza los valores de ejemplo
```

### PostgreSQL

```bash
docker compose up -d postgres
```

### Backend

```bash
# Secretos locales fuera del repositorio (user-secrets de .NET)
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project backend/src/PostulaAI.Api
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=postulaai;Username=postulaai;Password=<tu-password-local>" --project backend/src/PostulaAI.Api

dotnet run --project backend/src/PostulaAI.Api   # en Development aplica las migraciones al iniciar
# API:      http://localhost:5080
# Salud:    http://localhost:5080/health/live · /health/ready
# Swagger:  http://localhost:5080/swagger (solo en Development)
```

### Frontend

```bash
cd frontend
npm ci
npm run dev              # http://localhost:5173 (hace de proxy de /api hacia :5080)
```

### Todo con Docker

```bash
docker compose up --build
# Frontend: http://localhost:5173 · API: http://localhost:8080
```

## Variables de entorno

| Variable | Uso | Desde |
|---|---|---|
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | Base de datos de Docker Compose | Fase 0 |
| `ConnectionStrings__Postgres` | Cadena de conexión del backend | Fase 0 |
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Production` | Fase 0 |
| `JWT_SIGNING_KEY` (`Jwt__SigningKey` en el backend) | Clave de firma JWT, **obligatoria**, ≥ 32 bytes aleatorios. Sin ella la API no arranca | Fase 1 |
| `ANTHROPIC_API_KEY` | Clave de la API de Claude (solo backend) | Fase 3 |
| `ANTHROPIC_MODEL` | Identificador del modelo (vacío → `claude-sonnet-5-5`) | Fase 3 |

Nunca se commitean valores reales. Ver [docs/security.md](docs/security.md).

## Pruebas

Las pruebas de integración usan una base PostgreSQL real que crean y eliminan solas; necesitan `ConnectionStrings__Postgres` apuntando a un servidor donde el usuario pueda crear bases (por ejemplo, el de `docker compose up -d postgres`).

[![CI](https://github.com/adrijonas16/PostulaAI/actions/workflows/ci.yml/badge.svg)](https://github.com/adrijonas16/PostulaAI/actions/workflows/ci.yml)

```bash
export ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=postgres;Username=postulaai;Password=<tu-password-local>"
dotnet test backend/PostulaAI.slnx

cd frontend
npm run lint && npm run typecheck && npm test && npm run build
```

CI (`.github/workflows/ci.yml`) ejecuta en cada push y PR: escaneo de secretos, build y pruebas del backend, lint, typecheck, pruebas y build del frontend, y build de las imágenes Docker.

## Evaluaciones

*(Pendiente — Fase 5.)* Con `ANTHROPIC_API_KEY` configurada:

```bash
./evals/run-evals.sh
```

Metodología en [docs/evaluation.md](docs/evaluation.md). **Todavía no hay resultados publicados.**

## Capturas

Se agregarán con datos ficticios cuando existan las pantallas funcionales.

## Demo

Aún no hay despliegue público (Fase 6).

## Limitaciones conocidas

- Sin `ANTHROPIC_API_KEY` el análisis responde "no configurado" (no consume el límite de uso).
- El análisis es síncrono (15–60 s); si dos peticiones llegan a la vez, el límite de uso puede excederse en uno.
- La verificación contra invención es determinista y cubre un vocabulario curado de tecnologías, certificaciones e idiomas; no detecta todas las invenciones posibles (por ejemplo, años de experiencia).
- Aún no hay métricas de calidad del prompt: el dataset y el runner de evaluaciones son la Fase 5.
- Registrarse con un correo ya usado responde "ya existe una cuenta": permite saber si un correo está registrado (compromiso aceptado en v1, mitigado con límite de intentos).
- Si la misma sesión se abre en varias pestañas, un refresco simultáneo puede cerrar la sesión (detección de reutilización del refresh token). Dentro de una pestaña las renovaciones son de un solo vuelo.
- La eliminación de cuenta aún no está disponible (roadmap).
- El backend se compila y se prueba en GitHub Actions; el entorno de desarrollo asistido no tiene el SDK de .NET, y la migración inicial se generó con `dotnet ef` en CI.
- El análisis no verifica la veracidad del perfil: toma como cierto lo que el usuario escribe.
- Solo entrada manual de texto. No hay carga de PDF.
- El puntaje es una estimación no calibrada contra resultados reales de contratación.

## Roadmap

Ver [docs/roadmap.md](docs/roadmap.md).

## Uso responsable de IA

- Los datos del usuario solo se envían al proveedor de IA para generar el análisis solicitado. No se usan para entrenar modelos.
- No se registran perfiles, vacantes ni respuestas del modelo en los logs.
- Cada resultado se presenta como recomendación, con su aviso de transparencia.
- Ninguna afirmación sobre la calidad de la IA se publica sin un reporte de evaluación que la respalde.

Política de privacidad: [docs/privacy-policy.md](docs/privacy-policy.md).

## Contribuir con Claude

Lee [CLAUDE.md](CLAUDE.md) y las reglas en [.claude/rules/](.claude/rules/).
