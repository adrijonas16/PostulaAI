# Sistema de IA

> Estado: **diseño (Fase 0)**. La implementación llega en la Fase 3. Nada de lo descrito aquí como flujo está implementado todavía, salvo el contrato preliminar `IAIClient`.

## Principio rector

La IA **nunca** inventa experiencia, estudios, habilidades, certificaciones o logros. Solo puede usar lo que el usuario escribió en su perfil. Puede sugerir cómo presentarlo mejor; no puede agregar contenido.

## Flujo

```text
Perfil + vacante
→ construcción de prompt versionado          PostulaAI.AI/Prompts/job-analysis-v1.txt
→ Claude API                                 vía IAIClient (solo backend)
→ JSON estructurado
→ validación de esquema                      PostulaAI.AI/Schemas/job-analysis-v1.schema.json
→ validación de reglas de negocio            PostulaAI.AI/Services
→ reintento de reparación (máx. 1)           PostulaAI.AI/Prompts/job-analysis-repair-v1.txt
→ guardado solo si el resultado es válido    Modules/Analyses
```

1. `Analyses` verifica propiedad del perfil y la vacante y el límite de uso.
2. Se crean los snapshots inmutables y un `Analysis` en estado `pending`.
3. `PostulaAI.AI` arma el prompt desde el archivo versionado, con el idioma solicitado (inicialmente `es`).
4. Se llama a `IAIClient.CompleteAsync`. La implementación de Claude maneja `429`/`529` con reintentos acotados que respetan `retry-after`.
5. La respuesta se valida contra el JSON Schema y luego contra reglas de negocio (ver abajo).
6. Si falla, se envía **un** prompt de reparación con los errores de validación concretos. Si vuelve a fallar → `failed` sin resultado.
7. Si pasa → `completed` con `ResultJson`, puntaje y metadatos.

## Esquema de salida (v1)

```json
{
  "compatibilityScore": 0,
  "summary": "string",
  "strengths": [{ "requirement": "string", "evidence": "string", "importance": "required | preferred" }],
  "gaps": [{
    "requirement": "string",
    "gapType": "missing_skill | missing_experience | unclear_evidence | other",
    "importance": "required | preferred",
    "recommendation": "string"
  }],
  "keywordsToInclude": ["string"],
  "cvRecommendations": ["string"],
  "adaptedProfessionalSummary": "string",
  "coverLetterDraft": "string",
  "disclaimer": "string"
}
```

## Reglas de negocio planificadas (además del esquema)

- `compatibilityScore` entero entre 0 y 100.
- Ningún texto obligatorio vacío. Longitudes máximas para evitar respuestas desbordadas.
- Cada `strengths[].evidence` no vacía.
- `disclaimer` presente.
- **Comprobación de invención:** las tecnologías y habilidades mencionadas en `strengths` deben aparecer en el perfil. La comparación será determinista (normalización de mayúsculas, acentos y alias conocidos). En producción, una coincidencia dudosa genera una alerta en los metadatos. En las evaluaciones cuenta como fallo.

## Metadatos guardados por análisis

Estado, `PromptId`, `PromptVersion`, modelo, idioma, latencia, tokens de entrada y salida, `RepairAttempted`, `ErrorCode`, snapshots de perfil y vacante y `ResultJson` (solo si es válido). **Nunca** se guarda ni se registra la API key. Prompts y respuestas completas no se escriben en logs.

## Abstracción del proveedor

`IAIClient` (en `PostulaAI.AI/Clients`) recibe un prompt de sistema, mensajes y un máximo de tokens, y devuelve contenido, modelo, tokens, latencia y motivo de parada. La lógica de producto no conoce a Claude. Cambiar de proveedor implica una nueva implementación de `IAIClient` y volver a ejecutar las evaluaciones.

El identificador del modelo y la forma exacta de solicitar salida estructurada se configuran y verifican contra la documentación vigente de la API de Claude al implementar la Fase 3. No quedan fijos en el código.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| El modelo inventa experiencia | Instrucciones explícitas, evidencia obligatoria por fortaleza, comprobación determinista, casos trampa en evals. **Ninguna mitigación es perfecta**; el usuario siempre ve el aviso. |
| JSON inválido o incompleto | Validación de esquema y reglas de negocio, una reparación, nunca se guarda un resultado inválido. |
| Puntaje interpretado como verdad | Se muestra como estimación con banda y conteos, nunca aislado. |
| Inyección de instrucciones desde la vacante | El texto de la vacante va delimitado como dato; el prompt indica ignorar instrucciones contenidas en él. Validación de salida independiente. |
| Rate limit / caída del proveedor | Reintentos acotados, error claro, el intento cuenta para el límite solo si llegó al proveedor. |
| Coste por abuso | Límite de análisis por usuario y longitud máxima de perfil y vacante. |
| Datos personales | Solo backend, sin logs de contenido, sin uso para entrenamiento (ver `security.md`). |

## Límites del sistema

- No verifica la veracidad del perfil: si el usuario escribe algo falso, el análisis lo tomará como dado.
- No lee PDFs ni enlaces en v1; solo texto ingresado manualmente.
- El puntaje no está calibrado contra resultados reales de contratación.
- La calidad en idiomas distintos al español solo se conocerá tras las evaluaciones.
