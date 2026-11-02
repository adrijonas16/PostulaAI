---
name: dotnet-backend
description: Use when adding or changing backend code in PostulaAI (ASP.NET Core .NET 10, EF Core, PostgreSQL) — new endpoints, modules, entities, migrations or services.
---

# Backend .NET en PostulaAI

Lee primero `.claude/rules/architecture.md`, `code-quality.md` y `security.md`.

## Añadir una funcionalidad a un módulo

1. Ubica el módulo en `backend/src/PostulaAI.Api/Modules/<Modulo>/`. Si no existe y no está en la lista de `architecture.md`, pide aprobación.
2. Define DTOs de entrada/salida como `record` en `Contracts/`. Nunca expongas entidades de EF Core en la API.
3. Escribe el validador (FluentValidation) junto al DTO de entrada.
4. Implementa la lógica en un servicio (`<Modulo>Service`) que reciba `CancellationToken` y el `UserId` actual explícitamente. Toda consulta filtra por `UserId`.
5. El controlador/endpoint solo: valida, llama al servicio, mapea a `200/201/204` o `ProblemDetails`.
6. Si cambia el modelo: configura la entidad en `Persistence/<Entidad>Configuration.cs` y crea migración:
   ```bash
   dotnet ef migrations add <Nombre> --project backend/src/PostulaAI.Api
   ```
   Revisa el SQL generado antes de commitear.
7. Pruebas: unitarias para el servicio y el validador; integración para autorización por propietario.
8. Documenta el endpoint con atributos de OpenAPI (resumen, códigos de respuesta).

## Verificación

```bash
dotnet build backend/PostulaAI.slnx
dotnet test backend/PostulaAI.slnx
```

Si el SDK de .NET no está disponible en el entorno, dilo explícitamente y verifica en GitHub Actions. No afirmes que compila sin haberlo visto compilar.
