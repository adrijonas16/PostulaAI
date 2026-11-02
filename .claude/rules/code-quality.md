# Regla: Calidad de código

## Tipado estricto

- C#: `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` y analizadores habilitados (configurado en `backend/Directory.Build.props`). No uses `!` (null-forgiving) para silenciar advertencias sin justificarlo en un comentario.
- TypeScript: `strict: true`, `noUncheckedIndexedAccess: true`. Prohibido `any` explícito; usa `unknown` y estrecha el tipo. Prohibido `// @ts-ignore`; si es inevitable, `// @ts-expect-error` con motivo.

## Validación

- Toda entrada externa se valida en el backend (FluentValidation o validación equivalente), aunque el frontend ya la valide.
- En el frontend, los formularios se validan con esquemas `zod`.
- Toda salida de la IA se valida (ver `ai-safety.md`).
- Las respuestas de la API que consume el frontend se tipan; no asumas formas no documentadas.

## Manejo de errores

- Errores esperados (validación, no encontrado, límite alcanzado, IA no disponible) se modelan explícitamente y se traducen a `ProblemDetails` con un código estable (`errorCode`).
- No captures excepciones para ignorarlas. Si capturas, registra (sin datos sensibles) y propaga o traduce.
- Los mensajes al usuario son comprensibles y no exponen detalles internos (stack traces, SQL, respuestas crudas del proveedor).

## Estilo

- Código, identificadores y comentarios en inglés. Textos de interfaz y documentación en español.
- C#: convenciones de .NET, `async` de punta a punta con `CancellationToken`, `record` para DTOs inmutables.
- React: componentes funcionales, hooks, sin lógica de negocio en componentes de presentación.
- Respeta `.editorconfig` y `oxlint` (`frontend/.oxlintrc.json`, ejecutado con `--deny-warnings`). No desactives reglas de lint globalmente sin justificarlo en el PR.

## Accesibilidad básica (frontend)

- HTML semántico, un `<h1>` por página, `label` asociado a cada campo, foco visible, contraste AA, navegación completa por teclado.
- Los estados de carga y error se anuncian (`aria-live` o `role="alert"`).
