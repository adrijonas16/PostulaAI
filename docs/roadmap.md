# Roadmap

| Fase | Contenido | Estado |
|---|---|---|
| 0 | Estructura del monorepo, reglas, skills, documentación, esqueletos de backend y frontend, Docker Compose, CI | ✅ Implementada. CI en verde: backend compilado sin advertencias, 5 pruebas de backend y 13 de frontend, sin paquetes vulnerables |
| 1 | Autenticación (registro, login, refresh rotado, logout), usuarios, perfil profesional, auditoría básica | ✅ Implementada. CI en verde (ver PR de la Fase 1) |
| 2 | Vacantes (CRUD), historial, dashboard | Pendiente |
| 3 | `ClaudeClient`, prompts v1, esquema, validación, reparación, límites de uso, endpoint de análisis | Pendiente |
| 4 | Interfaz de resultados (banda de compatibilidad, fortalezas, brechas, palabras clave, textos adaptados, aviso) | Pendiente |
| 5 | Dataset de ≥ 15 casos, runner de evaluaciones, reporte | Pendiente |
| 6 | Imágenes Docker finales, despliegue de la demo, capturas, documentación final | Pendiente |

## Después del MVP

- Eliminación de cuenta y exportación de datos (`DELETE /api/v1/users/me`, `GET /api/v1/users/me/export`).
- Análisis en segundo plano con cola y notificación (el campo `Status` ya lo admite).
- Carga de CV en PDF con extracción revisable por el usuario.
- Varios perfiles por usuario en la interfaz.
- Comparación entre versiones de prompt en un panel interno.
- Idiomas adicionales para la respuesta, tras evaluarlos.
