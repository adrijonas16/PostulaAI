# Seguridad

> Estado: **Fases 3–4**. Lo implementado se indica con ✅; lo demás está planificado.

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
| Verificación de correo | Al registrarse se envía un enlace de un solo uso (token aleatorio de 256 bits; solo se guarda su hash SHA-256; vence en 24 h). Sin confirmar el correo no se pueden usar las funciones de IA (`403 email_not_verified`), para que crear cuentas desechables no dé IA gratis. El reenvío exige sesión y tiene una espera mínima de 60 s. Solo se exige si hay SMTP configurado (`Email:SmtpHost`); en local, Mailpit recibe los correos. |

## Autorización

- ✅ Cada recurso pertenece a un `UserId`. Los servicios reciben el `UserId` del token y filtran por él.
- ✅ El perfil solo es accesible por `/profiles/me`: no existe ruta para leer el de otro usuario. Una prueba de integración A/B lo verifica.
- ✅ Vacantes: toda consulta filtra por `UserId` y `Id` a la vez; una vacante ajena responde `404` en GET, PUT, DELETE y `PUT /application`, y nunca aparece en el listado (pruebas de integración A/B).
- ✅ La búsqueda escapa los comodines de `LIKE`, así que el texto del usuario se compara literalmente.
- ✅ Análisis: misma regla, con prueba de integración A/B. Crear un análisis exige que la vacante sea del usuario.

## Datos personales

| Dato | Dónde | Notas |
|---|---|---|
| Correo, nombre | `User` | Necesario para la cuenta. `EmailVerifiedAt` indica cuándo se confirmó |
| Tokens de verificación | `EmailVerificationToken` | Solo el hash. Se borran con el usuario |
| Uso de IA | `AiUsageEvent` | Usuario, función (análisis o CV) y fecha; sin contenido. Se borra con el usuario |
| Perfil profesional completo | `ProfessionalProfile` y tablas hijas | Lo escribe el usuario |
| Vacantes | `JobPosting` | Texto pegado por el usuario |
| Postulaciones | `JobPosting.ApplicationStatus`, `AppliedOn`, `ApplicationNotes`, `SubmittedSummary`, `SubmittedCoverLetter` | Lo que el usuario registra sobre su postulación, incluidos los textos que envió. Se borra con la vacante. |
| Snapshots | `Analysis.ProfileSnapshot`, `Analysis.JobPostingSnapshot` | Copia del perfil y la vacante al momento del análisis |
| Resultados de IA | `Analysis.ResultJson` | Derivado del perfil |

Reglas:

- **No se registran en logs**: contenido de perfiles, vacantes, prompts ni respuestas del modelo. Solo identificadores, códigos de error, latencias, tokens y versiones.
- ✅ Los audit logs no contienen contenido personal: solo la acción, los identificadores, el `traceId` y la fecha. Acciones registradas: registro, login correcto y fallido, reutilización de refresh token, logout y actualización de perfil.
- Los datos se envían al proveedor de IA únicamente para ejecutar el análisis que el usuario solicita. No se usan para entrenar modelos ni se venden o comparten con terceros.
- **Eliminación de cuenta** *(roadmap)*: `DELETE /api/v1/users/me` borrará en cascada perfil, vacantes, análisis y tokens, y anonimizará los audit logs (`UserId` → null).

## Límites de uso ✅ *(Fase 3)*

- Máximo configurable de análisis por usuario en una ventana móvil (`UsageLimits:Analyses:MaxPerWindow`, por defecto 10 cada 24 h). Solo cuentan los intentos que llegaron al proveedor de IA.
- Longitud máxima de perfil y vacante para controlar coste y abuso.
- ✅ Presupuesto diario del sitio (`UsageLimits:AiDaily:MaxRequests`, por defecto 200; `AI_DAILY_MAX_REQUESTS` en `.env`): tope de peticiones de IA (análisis e importaciones de CV de todos los usuarios) en 24 h móviles. Al llegar al tope, la IA responde `503 ai_daily_budget_reached` hasta que se libere cupo. Cada petición se registra antes de llamar al proveedor, así que las fallidas o reparadas también cuentan. Dos peticiones simultáneas pueden superar el tope en una.
- Recomendado además en producción: un workspace propio en la consola de Anthropic con límite de gasto mensual y alertas.
- Errores `429`, `529` y `5xx` del proveedor: hasta 2 reintentos con espera acotada (respeta `retry-after`, máximo 20 s) y después un mensaje claro al usuario.
- Los logs del análisis solo registran id, estado, código de error, modelo, versión de prompt, tokens, latencia y si hubo reparación. Nunca el perfil, la vacante, el prompt ni la respuesta.

## Importación de CV ✅

- El archivo se procesa en memoria y se descarta: no se guarda en disco, base de datos ni logs. Tampoco se guarda su texto.
- El tipo se detecta por contenido (`%PDF-` o ZIP con `word/document.xml`), no por la extensión. Hay límites de tamaño (5 MB por archivo, 20 MB del XML descomprimido, 15 páginas de PDF) y el XML se lee sin DTD, contra XXE.
- El texto del CV se envía al proveedor de IA solo para extraer los campos, y el resultado no se guarda hasta que el usuario lo confirma.
- La auditoría registra `profile.cv_imported` sin contenido.

## Infraestructura

- ✅ En `docker-compose`, PostgreSQL y la API solo escuchan en `127.0.0.1`.
- ✅ nginx añade `X-Content-Type-Options`, `X-Frame-Options` y `Referrer-Policy`.
- ✅ Swagger UI solo en desarrollo.
- ✅ CORS restringido a orígenes configurados.
- ✅ Dependabot para NuGet, npm y GitHub Actions; CI reporta paquetes vulnerables.
- HTTPS obligatorio en producción, terminado en el proxy del proveedor de despliegue *(Fase 6)*.

## Reportar una vulnerabilidad

Mientras no exista un canal dedicado, abre un *security advisory* privado en el repositorio de GitHub. No uses issues públicos.
