# Regla: Seguridad y datos personales

## Secretos

- **Nunca** se commitea un secreto: API keys, contraseñas, cadenas de conexión reales, claves de firma JWT, tokens.
- Los valores reales viven en `.env` (ignorado por Git), user-secrets de .NET o el gestor de secretos del entorno de despliegue.
- `.env.example` contiene solo nombres de variables y valores de ejemplo evidentemente falsos.
- Antes de cada commit: `gitleaks` (también corre en CI). Si se detecta un secreto ya commiteado, se rota de inmediato; borrar el commit no basta.
- No pegues secretos en documentación, capturas, issues, logs ni prompts.

## Autenticación y autorización

- Contraseñas con hash adaptativo (`PasswordHasher` de ASP.NET Core / PBKDF2). Nunca en texto plano ni con hash rápido.
- Access token JWT de vida corta; refresh token rotado, almacenado solo como hash, enviado en cookie `HttpOnly; Secure; SameSite=Strict`.
- La clave de firma JWT tiene al menos 256 bits y proviene de configuración segura.
- **Propiedad de datos:** toda consulta de recursos de usuario filtra por el `UserId` del token. Un recurso de otro usuario responde `404`, no `403`.
- Límite de intentos (rate limiting) en endpoints de autenticación.

## Datos personales

- Perfiles y vacantes son datos personales. **No se registran en logs** (ni completos ni parciales), tampoco prompts ni respuestas completas del modelo.
- Se permite registrar: identificadores, códigos de error, latencias, conteo de tokens, versión de prompt, modelo.
- Los audit logs no contienen contenido del perfil ni de la vacante.
- Los datos del usuario no se usan para entrenar modelos ni se comparten con terceros, salvo el envío necesario al proveedor de IA para ejecutar el análisis solicitado.
- Toda nueva columna con datos personales debe documentarse en `docs/security.md` y considerarse en la eliminación de cuenta.

## Dependencias y configuración

- Dependencias fijadas por versión. Revisar avisos de vulnerabilidades (`dotnet list package --vulnerable`, `npm audit`).
- CORS restringido a orígenes configurados. HTTPS en producción. Cabeceras de seguridad básicas.
- Swagger UI solo habilitado en entornos de desarrollo.
