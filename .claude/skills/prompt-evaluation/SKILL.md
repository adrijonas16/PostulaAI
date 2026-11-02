---
name: prompt-evaluation
description: Use whenever a prompt, AI output schema or AI validator in PostulaAI changes, or when adding evaluation cases or interpreting evaluation reports.
---

# Evaluación de prompts

Base: `.claude/rules/ai-safety.md` y `docs/evaluation.md`.

## Cuando cambias un prompt, esquema o validador

1. No edites un prompt publicado. Crea `backend/src/PostulaAI.AI/Prompts/<id>-v<N+1>.txt`.
2. Actualiza la configuración (`AI:Prompts:JobAnalysis:Version`) solo después de evaluar.
3. Ejecuta las evaluaciones de la versión anterior y la nueva con el mismo dataset y modelo:
   ```bash
   ./evals/run-evals.sh --prompt-version v1
   ./evals/run-evals.sh --prompt-version v2
   ```
4. Compara los reportes en `evals/reports/`. Una versión nueva no puede empeorar "Casos sin información inventada" ni "JSON válido".
5. Adjunta ambos reportes al PR. Si no hay API key, escribe "no ejecutado" y no fusiones cambios de prompt hasta ejecutarlos.

## Añadir un caso

- Datos 100 % ficticios y anonimizados.
- Cada caso declara expectativas verificables (`expectations`): tecnologías permitidas, brechas obligatorias esperadas, idioma, rango de puntaje si aplica.
- Prioriza casos trampa: vacantes que piden algo cercano a lo que el perfil tiene pero no idéntico.

## Reglas

- Los archivos de `evals/results/` y `evals/reports/` los genera el runner. Nunca se editan a mano.
- Las comprobaciones deterministas viven en `PostulaAI.AI/Evaluations` para que producción y evals compartan lógica.
