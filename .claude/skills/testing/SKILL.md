---
name: testing
description: Use when writing or fixing tests in PostulaAI, or before declaring any task done — covers backend unit/integration tests, frontend component tests and what must be covered.
---

# Pruebas en PostulaAI

La lista de lo que **debe** tener pruebas está en `.claude/rules/testing.md`.

## Backend

- Unitarias: `backend/tests/PostulaAI.UnitTests` (xUnit). Sin base de datos ni red.
- Integración: `backend/tests/PostulaAI.IntegrationTests` con `WebApplicationFactory<Program>` y PostgreSQL real (Testcontainers localmente; servicio `postgres` en CI).
- IA: sustituye `IAIClient` por un falso que devuelva fixtures de `tests/.../Fixtures/ai/`. Nunca llames a la API real en pruebas.
- Patrón para autorización: crea usuario A y B, crea recurso con A, verifica que B recibe `404` en GET/PUT/DELETE.
- Nombres: `Metodo_Escenario_ResultadoEsperado`.

## Frontend

- Vitest + Testing Library + `@testing-library/user-event`.
- Mock de red con MSW cuando el componente consulta la API.
- Prueba comportamiento visible (texto, roles, foco), no detalles de implementación.

## Antes de dar una tarea por terminada

```bash
dotnet test backend/PostulaAI.slnx
cd frontend && npm run lint && npm run typecheck && npm test
```

Reporta exactamente qué se ejecutó y qué no. Si algo solo se verificó en CI, dilo.
