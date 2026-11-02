# Evaluaciones de prompts

Estado: **estructura creada; dataset, runner y resultados pendientes (Fases 3 y 5). Ninguna evaluación ha sido ejecutada.**

```text
evals/
  datasets/   job-analysis-dataset-v1.json      ≥ 15 casos ficticios y anonimizados (Fase 5)
  runner/     PostulaAI.Evals (consola .NET)     usa PostulaAI.AI: mismos prompts, esquema y validadores que producción
  results/    job-analysis-<version>-results.json   generado por el runner, nunca editado a mano
  reports/    job-analysis-<version>-report.md      generado por el runner, nunca editado a mano
  run-evals.sh                                   script de entrada (Fase 5)
```

- Requiere `ANTHROPIC_API_KEY` en el entorno. Sin clave, el runner no inventa resultados: termina indicando "no ejecutado".
- Las respuestas crudas del proveedor se guardan en `results/raw/`, que está ignorado por Git.
- Metodología completa: [`docs/evaluation.md`](../docs/evaluation.md).
