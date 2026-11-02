# Regla: Pruebas

## Qué debe tener pruebas (obligatorio)

| Área | Tipo mínimo |
|---|---|
| Hashing de contraseñas, emisión y rotación de tokens, detección de reutilización | Unitarias |
| Autorización por propietario (A no ve/modifica datos de B) en cada recurso | Integración |
| Validadores de entrada | Unitarias |
| Construcción de prompts (variables reemplazadas, sin datos faltantes) | Unitarias |
| Validación de esquema y reglas de negocio de la salida de IA | Unitarias con fixtures válidos e inválidos |
| Flujo de reparación (inválido → reparación → válido / inválido → `failed` sin resultado) | Unitarias con `IAIClient` falso |
| Límites de uso | Unitarias + integración |
| Formularios y flujos principales del frontend (login, perfil, vacante, resultado) | Componentes con Testing Library |

## Reglas

- Las pruebas no llaman a la API real de Claude. Usan una implementación falsa de `IAIClient`.
- Las pruebas de integración usan PostgreSQL real (Testcontainers o servicio de CI), no un proveedor en memoria.
- No se usan datos personales reales en fixtures; todo es ficticio.
- Una tarea no está terminada si las pruebas no pasan. No se desactivan ni se marcan como omitidas pruebas para "pasar" CI.
- Si algo no pudo ejecutarse localmente (por ejemplo, el SDK de .NET no está disponible), se dice explícitamente y se verifica en CI.

## Comandos

```bash
# Backend
dotnet test backend/PostulaAI.slnx

# Frontend
cd frontend && npm run lint && npm run typecheck && npm test

# Evaluaciones (requiere ANTHROPIC_API_KEY)
./evals/run-evals.sh
```
