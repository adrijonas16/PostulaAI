# Regla: Seguridad y honestidad de la IA

## Regla absoluta

La IA **nunca** puede afirmar experiencia, estudios, habilidades, tecnologías, certificaciones o logros que el usuario no haya proporcionado en su perfil. Puede sugerir mejor redacción, orden y estrategia; no puede inventar.

Toda fortaleza (`strengths[]`) debe citar evidencia presente en el perfil. Si la evidencia es ambigua o no existe, se reporta como brecha (`unclear_evidence` o `missing_*`), no como fortaleza.

## Flujo obligatorio

```text
Perfil + vacante
→ construcción de prompt versionado (PostulaAI.AI/Prompts)
→ IAIClient (Claude API, solo backend)
→ JSON estructurado
→ validación de esquema (PostulaAI.AI/Schemas)
→ validación de reglas de negocio
→ como máximo UN reintento con el prompt de reparación
→ guardado solo si el resultado es válido
```

- Si la validación falla tras la reparación, el análisis queda `failed`, **sin** `ResultJson`, y el usuario recibe un error claro. Nunca se guarda un resultado parcial como válido.
- Cada análisis registra: estado, id y versión del prompt, modelo, idioma, latencia, tokens, si hubo reparación, snapshots de perfil y vacante.

## Prompts

- Viven como archivos versionados en `backend/src/PostulaAI.AI/Prompts/` con nombre `<id>-v<N>.txt`. Nunca dentro de controladores ni como cadenas largas en código.
- **Un prompt publicado no se edita.** Cualquier cambio de comportamiento crea una versión nueva (`-v2`) y se cambia la configuración para usarla.
- **Ningún cambio de prompt, esquema o validador se fusiona sin actualizar y ejecutar las evaluaciones** (`evals/`) y sin adjuntar el reporte resultante en el PR. Si no se pudieron ejecutar (sin API key), el PR lo declara explícitamente.

## Afirmaciones verificables

- No se afirma en README, docs, UI o mensajes de commit ninguna capacidad, precisión o métrica de la IA que no esté respaldada por un reporte en `evals/reports/` o una prueba automatizada.
- Los resultados de evaluación jamás se editan a mano ni se fabrican. Si no se ejecutaron, se escribe "no ejecutado".

## Presentación al usuario

- El puntaje es una estimación, no una verdad. Se muestra con contexto (banda y conteo de requisitos cumplidos/faltantes) y nunca como único indicador.
- Todo resultado incluye el aviso de transparencia: es una recomendación y no garantiza contratación.

## Proveedor

- La API key solo existe en el backend. Nunca en el frontend, el repositorio, logs ni respuestas.
- Manejar `429`/`529` del proveedor con reintentos acotados y respetando `retry-after`; si persiste, error comprensible para el usuario.
