# CLAUDE.md — Guía para contribuciones con Claude en PostulaAI

## Visión del producto

PostulaAI es un SaaS que compara el perfil profesional de una persona con una vacante y entrega una compatibilidad estimada, fortalezas respaldadas por el perfil, brechas reales, palabras clave, recomendaciones honestas, un resumen adaptado y un borrador de carta de presentación.

**Regla absoluta:** la IA nunca inventa experiencia, estudios, habilidades, certificaciones o logros que el usuario no proporcionó.

## Stack

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core sobre .NET 10 (LTS), EF Core, PostgreSQL 16 |
| Frontend | React + TypeScript (strict) + Vite |
| IA | Claude API, solo desde el backend, mediante `IAIClient` |
| Pruebas | xUnit, WebApplicationFactory, Testcontainers · Vitest + Testing Library |
| Infra | Docker Compose · GitHub Actions |

## Estructura del repositorio

```text
backend/
  PostulaAI.slnx
  src/PostulaAI.Api/       Host + módulos (Modules/<Modulo>) + Shared/
  src/PostulaAI.AI/        Clients · Prompts · Schemas · Services · Evaluations
  tests/                   PostulaAI.UnitTests · PostulaAI.IntegrationTests
frontend/                  React + Vite (src/app, src/features, src/shared)
evals/                     datasets · runner · results · reports
docs/                      architecture · ai-system · evaluation · security · privacy-policy · roadmap
.claude/rules/             Reglas obligatorias (leer antes de cambiar código)
.claude/skills/            Procedimientos reutilizables
.github/workflows/ci.yml   CI
```

## Reglas (resumen; el detalle está en `.claude/rules/`)

- **Arquitectura** (`architecture.md`): controladores delgados; lógica en servicios testeables; IA aislada en `PostulaAI.AI`, compartida por producción y evals.
- **Calidad** (`code-quality.md`): `Nullable` + warnings como errores en C#; `strict` en TypeScript, sin `any`; validación de entradas en frontend y backend; errores explícitos con `ProblemDetails`.
- **Seguridad** (`security.md`): ningún secreto en Git; JWT corto + refresh token rotado en cookie `HttpOnly`; nada de datos personales en logs.
- **Propiedad de datos**: toda consulta filtra por el `UserId` autenticado; recurso ajeno → `404`.
- **IA** (`ai-safety.md`): nunca inventar experiencia; validar el JSON con esquema y reglas de negocio; un solo reintento de reparación; guardar solo resultados válidos; prompts versionados e inmutables; **todo cambio de prompt actualiza y ejecuta las evaluaciones**.
- **Pruebas** (`testing.md`): funcionalidades críticas con pruebas; nunca la API real de Claude en tests.
- **Honestidad**: no afirmar funciones, pruebas ejecutadas ni métricas que no se hayan verificado.

## Comandos frecuentes

```bash
# Infraestructura local
cp .env.example .env                       # y completa valores locales
docker compose up -d postgres              # solo base de datos
docker compose up --build                  # todo el stack

# Backend
dotnet restore backend/PostulaAI.slnx
dotnet build   backend/PostulaAI.slnx
dotnet test    backend/PostulaAI.slnx      # integración: requiere ConnectionStrings__Postgres (crea y borra su propia BD)
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project backend/src/PostulaAI.Api
dotnet run --project backend/src/PostulaAI.Api
dotnet ef migrations add <Nombre> --project backend/src/PostulaAI.Api

# Frontend
cd frontend && npm ci
npm run dev | npm run lint | npm run typecheck | npm test | npm run build

# Evaluaciones (requiere ANTHROPIC_API_KEY)
./evals/run-evals.sh

# Secretos
gitleaks detect --source . --no-banner
```

## Definition of Done

Una tarea está terminada solo si:

1. Compila sin advertencias (backend) y pasa lint + typecheck (frontend).
2. Las pruebas existentes pasan y la funcionalidad nueva crítica tiene pruebas.
3. Se respetan las reglas de `.claude/rules/`.
4. No hay secretos ni datos personales reales en el diff (`gitleaks` limpio).
5. Si cambió un prompt/esquema/validador: evaluaciones ejecutadas y reporte adjunto (o "no ejecutado" declarado y el cambio no se fusiona).
6. La documentación afectada está actualizada (ver skill `documentation`).
7. El resumen de la tarea indica exactamente qué se ejecutó, dónde (local o CI) y qué quedó pendiente.

**Ejecuta las pruebas antes de dar una tarea por terminada.** Si el entorno no permite ejecutarlas (por ejemplo, sin SDK de .NET), dilo y verifica en GitHub Actions.

## No modificar sin aprobación explícita

- El stack tecnológico o el modelo de autenticación.
- El esquema de salida del análisis (`PostulaAI.AI/Schemas`).
- Prompts publicados (se crean versiones nuevas, no se editan).
- Migraciones ya aplicadas en `main`.
- Archivos generados en `evals/results/` y `evals/reports/`.
- `.github/workflows/` y las reglas de `.claude/rules/`.
- Historial de Git: nada de `push --force` ni reescritura de `main`.
