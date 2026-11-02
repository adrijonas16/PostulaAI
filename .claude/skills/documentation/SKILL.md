---
name: documentation
description: Use when writing or updating PostulaAI documentation (README, docs/, CLAUDE.md) or after finishing a phase or feature.
---

# Documentación

## Principios

- En español, directa y verificable. Código y comandos en bloques.
- **No documentes como existente lo que no existe.** Lo planificado se marca con *(pendiente — Fase N)* o va a `docs/roadmap.md`.
- No incluyas métricas de IA sin un reporte en `evals/reports/` que las respalde.
- Sin secretos, datos personales reales ni capturas con datos reales. Capturas solo con datos ficticios.

## Qué actualizar según el cambio

| Cambio | Documentos |
|---|---|
| Endpoint nuevo o modificado | `docs/architecture.md` (tabla de endpoints) |
| Entidad o columna | `docs/architecture.md` (modelo), `docs/security.md` si contiene datos personales |
| Prompt, esquema o validador | `docs/ai-system.md`, `docs/evaluation.md` |
| Variable de entorno | `.env.example`, `README.md` |
| Comando nuevo | `README.md`, `CLAUDE.md` |
| Fase terminada | `docs/roadmap.md` (estado) y "Limitaciones conocidas" del README |
