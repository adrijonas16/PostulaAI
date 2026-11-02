# Sistema de IA

> Estado: **implementado (Fase 3)**. Cubierto por pruebas unitarias e de integración con un cliente de IA falso. **La calidad del prompt con el modelo real todavía no se ha medido**; eso es la Fase 5 (evaluaciones).

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

## Implementación

| Pieza | Archivo |
|---|---|
| Prompt principal y de reparación (recursos embebidos, inmutables) | `PostulaAI.AI/Prompts/job-analysis-v1.txt`, `job-analysis-repair-v1.txt` |
| Esquema enviado como salida estructurada | `PostulaAI.AI/Schemas/job-analysis-v1.schema.json` |
| Plantillas `{{variable}}` en una sola pasada (los datos del usuario no pueden inyectar marcadores) | `Prompts/PromptTemplate.cs` |
| Validación de esquema: deserialización estricta (rechaza propiedades desconocidas, faltantes, `null` y enums inválidos) | `Services/JobAnalysisJson.cs` |
| Reglas de negocio (rangos, textos vacíos, límites, duplicados) | `Services/JobAnalysisRules.cs` |
| Verificación contra invención (vocabulario curado de ~90 tecnologías, herramientas, certificaciones e idiomas, con alias y equivalencias como PostgreSQL ⇒ SQL) | `Evaluations/TechVocabulary.cs`, `Evaluations/GroundingChecker.cs` |
| Orquestación con un único intento de reparación | `Services/JobAnalysisService.cs` |
| Cliente de la API Messages de Claude (`output_config.format` = `json_schema`), reintentos acotados | `Clients/ClaudeClient.cs` |
| Persistencia, snapshots, límites de uso y mapeo de errores | `PostulaAI.Api/Modules/Analyses`, `Modules/UsageLimits` |

Configuración: `Anthropic:ApiKey` (secreto), `Anthropic:Model` (por defecto `claude-sonnet-5-5`), `AI:JobAnalysis:PromptVersion` (por defecto `v1`), `AI:JobAnalysis:MaxOutputTokens` (16 000, incluye margen para el razonamiento adaptativo del modelo).

La verificación contra invención revisa la evidencia de cada fortaleza, el resumen adaptado y las palabras clave. Si encuentra un término que no está en el perfil, pide una reparación indicando el término exacto. Si persiste, el análisis falla con `ai_invalid_output` y no se guarda.

### Códigos de error

| Código | HTTP | Significado |
|---|---|---|
| `ai_not_configured` | 503 | Falta `ANTHROPIC_API_KEY` o fue rechazada. No consume uso. |
| `ai_rate_limited` | 503 | 429/529 persistentes tras los reintentos. |
| `ai_unavailable` | 503 | Error de red o 5xx persistente. |
| `ai_invalid_output` | 502 | El resultado no pasó la validación ni tras la reparación. |
| `ai_refused` / `ai_request_rejected` | 422 | El modelo se negó o la API rechazó la solicitud. |

## Importación de CV

Un segundo flujo, separado del análisis, usa la misma biblioteca:

```text
Archivo PDF/DOCX (máx. 5 MB, tipo detectado por contenido)
→ extracción de texto en memoria (PdfPig / OOXML); el archivo nunca se guarda
→ prompt versionado cv-extraction-v1 (copiar, no inventar ni traducir; desconocido = null)
→ Claude con salida estructurada (Schemas/cv-extraction-v1.schema.json)
→ deserialización estricta; un solo reintento con cv-extraction-repair-v1
→ verificación: se eliminan habilidades, tecnologías e idiomas que no aparecen literalmente en el CV (con aviso)
→ normalización a las reglas del perfil (fechas, URLs http/https, límites, duplicados)
→ borrador NO guardado: el usuario lo revisa, completa lo que falte y guarda
```

Endpoint: `POST /api/v1/profiles/me/import` (multipart, campo `file`). Límite por usuario: 5 importaciones por hora (`RateLimiting:CvImport`). Errores propios: `cv_unsupported_format` 415, `cv_too_large` 413, `cv_no_text` y `cv_unreadable` 422, además de los `ai_*`.

## Abstracción del proveedor

`IAIClient` (en `PostulaAI.AI/Clients`) recibe un prompt de sistema, mensajes y un máximo de tokens, y devuelve contenido, modelo, tokens, latencia y motivo de parada. La lógica de producto no conoce a Claude. Cambiar de proveedor implica una nueva implementación de `IAIClient` y volver a ejecutar las evaluaciones.

El modelo se configura con `Anthropic:Model`. La salida estructurada usa `output_config.format`, según la documentación vigente de la API de Claude (fuera de beta desde enero de 2026).

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
