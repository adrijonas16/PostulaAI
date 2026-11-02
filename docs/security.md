# Seguridad

> Estado: **Fase 1**. Lo implementado se indica con ✅; lo demás está planificado.

## Secretos

- ✅ `.env` ignorado por Git; `.env.example` sin valores reales.
- ✅ `gitleaks` en CI sobre todo el historial; también se ejecuta antes de cada push.
- La API key de Claude, la clave de firma JWT y la cadena de conexión solo existen en variables de entorno o en el gestor de secretos del despliegue.
- Si un secreto llega al repositorio, se **rota** de inmediato. Reescribir el historial no basta.

## Autenticación ✅ *(Fase 1)*

| Elemento | Decisión |
|---|---|
| Contraseñas | `PasswordHasher` de ASP.NET Core (PBKDF2 con sal, iteraciones según la versión del framework) |
| Access token | JWT firmado (HS256, clave ≥ 256 bits), vida de 15 minutos, guardado solo en memoria en el frontend |
| Refresh token | Aleatorio de 256 bits. Solo se guarda su hash. Se envía en cookie `HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth` y se rota en cada uso |
| Reutilización | Si se presenta un refresh token ya rotado, se revoca toda su familia |
| Fuerza bruta | Rate limiting por IP y por cuenta en `/auth/*` |
| Errores | Mensaje genérico ("credenciales inválidas") sin revelar si el correo existe; con un correo inexistente se calcula igualmente un hash para no delatarlo por tiempo de respuesta |
| Registro | Responde `409` si el correo ya existe. Esto permite enumerar correos; es un compromiso aceptado en v1, mitigado por el rate limiting |
| Configuración | La API no arranca sin `Jwt:SigningKey` de al menos 32 bytes (validación al inicio) |
| Frontend | El access token nunca se guarda en `localStorage` ni `sessionStorage` (hay una prueba que lo verifica) |

## Autorización

- ✅ Cada recurso pertenece a un `UserId`. Los servicios reciben el `UserId` del token y filtran por él.
- ✅ El perfil solo es accesible por `/profiles/me`: no existe ruta para leer el de otro usuario. Una prueba de integración A/B lo verifica.
- Recursos con identificador (vacantes, análisis): un recurso ajeno responde `404` *(Fases 2 y 3)*.

## Datos personales

| Dato | Dónde | Notas |
|---|---|---|
| Correo, nombre | `User` | Necesario para la cuenta |
| Perfil profesional completo | `ProfessionalProfile` y tablas hijas | Lo escribe el usuario |
| Vacantes | `JobPosting` | Texto pegado por el usuario |
| Snapshots | `Analysis.ProfileSnapshot`, `Analysis.JobPostingSnapshot` | Copia del perfil y la vacante al momento del análisis |
| Resultados de IA | `Analysis.ResultJson` | Derivado del perfil |

Reglas:

- **No se registran en logs**: contenido de perfiles, vacantes, prompts ni respuestas del modelo. Solo identificadores, códigos de error, latencias, tokens y versiones.
- ✅ Los audit logs no contienen contenido personal: solo la acción, los identificadores, el `traceId` y la fecha. Acciones registradas: registro, login correcto y fallido, reutilización de refresh token, logout y actualización de perfil.
- Los datos se envían al proveedor de IA únicamente para ejecutar el análisis que el usuario solicita. No se usan para entrenar modelos ni se venden o comparten con terceros.
- **Eliminación de cuenta** *(roadmap)*: `DELETE /api/v1/users/me` borrará en cascada perfil, vacantes, análisis y tokens, y anonimizará los audit logs (`UserId` → null).

## Límites de uso *(Fase 3)*

- Máximo configurable de análisis por usuario y por ventana (valor inicial propuesto: 10 por día).
- Longitud máxima de perfil y vacante para controlar coste y abuso.
- Errores `429` y `529` del proveedor: reintentos acotados con `retry-after` y después un mensaje claro al usuario.

## Infraestructura

- ✅ En `docker-compose`, PostgreSQL y la API solo escuchan en `127.0.0.1`.
- ✅ nginx añade `X-Content-Type-Options`, `X-Frame-Options` y `Referrer-Policy`.
- ✅ Swagger UI solo en desarrollo.
- ✅ CORS restringido a orígenes configurados.
- ✅ Dependabot para NuGet, npm y GitHub Actions; CI reporta paquetes vulnerables.
- HTTPS obligatorio en producción, terminado en el proxy del proveedor de despliegue *(Fase 6)*.

## Reportar una vulnerabilidad

Mientras no exista un canal dedicado, abre un *security advisory* privado en el repositorio de GitHub. No uses issues públicos.
