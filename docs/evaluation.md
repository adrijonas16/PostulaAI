# Evaluación de prompts

> Estado: **diseño (Fase 0)**. Dataset, runner y reportes llegan en las Fases 3 y 5. **No hay resultados todavía; ninguna métrica de este proyecto ha sido medida.**

## Qué se evalúa

> El prompt `cv-extraction-v1` (importación de CV) también debe tener su propio dataset y métricas (campos copiados correctamente, cero habilidades inventadas). Pendiente en la Fase 5.

El prompt de análisis (`job-analysis-vN`) junto con su esquema y sus validadores, usando **el mismo código** que producción (`PostulaAI.AI`).

## Dataset (`evals/datasets/job-analysis-dataset-v1.json`)

Habrá al menos 15 casos **ficticios y anonimizados** de perfil + vacante. Se escriben a mano para cubrir:

| Categoría | Qué pone a prueba |
|---|---|
| Alta coincidencia | Que reconozca fortalezas con evidencia real |
| Baja coincidencia | Que no infle el puntaje |
| Falta de requisito obligatorio | Que lo marque como brecha `required` |
| Perfil incompleto | Que declare información insuficiente en lugar de suponer |
| Vacante ambigua | Que no invente requisitos |
| Junior frente a vacante senior | Brecha de experiencia honesta |
| Vacante en inglés | Respuesta en el idioma solicitado (español) |
| Tecnologías similares pero no idénticas | Por ejemplo, perfil con PostgreSQL y vacante que pide Oracle: no debe afirmar Oracle |
| Casos trampa | Vacantes diseñadas para tentar al modelo a inventar experiencia |

Cada caso declara expectativas verificables, por ejemplo:

```json
{
  "id": "trap-001",
  "category": "trap",
  "language": "es",
  "profile": { "...": "..." },
  "jobPosting": { "...": "..." },
  "expectations": {
    "mustNotClaim": ["Kubernetes", "Oracle"],
    "requiredGapsMustInclude": ["Kubernetes"],
    "scoreRange": [0, 60]
  }
}
```

*(El formato definitivo se fija en la Fase 5.)*

## Métricas

| Métrica | Definición | Tipo |
|---|---|---|
| JSON válido | Pasa esquema + reglas de negocio (tras, como máximo, una reparación) | Determinista |
| Reparaciones necesarias | Casos que requirieron prompt de reparación | Determinista |
| Sin información inventada | Ninguna tecnología/habilidad de `mustNotClaim` ni ajena al perfil aparece en `strengths` | Determinista |
| Detección de brechas críticas | Todas las brechas de `requiredGapsMustInclude` aparecen como `gaps` con `importance: required` | Determinista |
| Idioma correcto | El texto libre está en el idioma solicitado | Heurística determinista |
| Puntaje en rango | `compatibilityScore` dentro de `scoreRange` cuando el caso lo define | Determinista |
| Recomendaciones útiles y honestas | Concretas y sin sugerir agregar lo que el usuario no tiene | Revisión humana con rúbrica (se reporta aparte, nunca se mezcla con las métricas automáticas) |

## Cómo se ejecutará *(pendiente — Fase 5)*

```bash
export ANTHROPIC_API_KEY=...       # nunca en el repositorio
./evals/run-evals.sh --prompt-version v1
```

Salidas:

- `evals/results/job-analysis-v1-results.json`: resultado por caso y por comprobación.
- `evals/reports/job-analysis-v1-report.md`: tabla resumen con modelo, fecha y commit.
- `evals/results/raw/`: respuestas crudas, ignoradas por Git.

Sin API key, el runner termina con el estado "no ejecutado" y no genera métricas.

## Cómo interpretar los resultados

- Las métricas son sobre un dataset pequeño y sintético: sirven para **comparar versiones de prompt**, no como medida de calidad absoluta.
- "Sin información inventada" debe ser 100 %. Cualquier fallo se investiga caso por caso antes de publicar una versión.
- Un nuevo prompt no puede empeorar "JSON válido" ni "Sin información inventada" respecto a la versión anterior.
- Los modelos no son deterministas: el reporte indica el modelo y la fecha, y conviene repetir ejecuciones antes de concluir.

## Cómo agregar casos

1. Escribe datos 100 % ficticios. No uses perfiles ni vacantes reales.
2. Asigna una categoría y expectativas verificables.
3. Si el caso cambia el significado de las métricas, crea una nueva versión del dataset (`-v2`) en lugar de modificar la existente.
4. Ejecuta las evaluaciones y adjunta el reporte en el PR.
