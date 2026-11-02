---
name: security-review
description: Use before committing, before opening a PR, or when touching auth, user data, logging, configuration or AI provider calls in PostulaAI.
---

# Revisión de seguridad

Base: `.claude/rules/security.md`.

## Lista de verificación

1. **Secretos**
   - `gitleaks detect --source . --no-banner` sin hallazgos.
   - `git diff --cached` no contiene claves, tokens, contraseñas ni cadenas de conexión reales.
   - `.env` no está en el índice (`git ls-files | grep -E '(^|/)\.env$'` vacío).
2. **Autorización**: cada consulta nueva filtra por `UserId`; existe prueba de integración A/B.
3. **Logs**: ningún log incluye contenido de perfil, vacante, prompt o respuesta del modelo. Busca `Log*(` en el diff y revisa argumentos.
4. **Entradas**: todo DTO nuevo tiene validador con longitudes máximas.
5. **Errores**: las respuestas de error no exponen excepciones, SQL ni respuestas crudas del proveedor.
6. **IA**: la API key solo se lee en backend desde configuración; no aparece en respuestas ni en el frontend (`grep -ri anthropic frontend/src` no debe encontrar claves).
7. **Dependencias**: `dotnet list backend/PostulaAI.slnx package --vulnerable` y `npm audit --omit=dev`.
8. **Datos personales nuevos**: documentados en `docs/security.md` y contemplados en eliminación de cuenta.

Reporta los hallazgos con archivo y línea; no los corrijas en silencio.
