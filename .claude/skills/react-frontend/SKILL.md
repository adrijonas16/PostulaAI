---
name: react-frontend
description: Use when adding or changing PostulaAI frontend code (React, TypeScript, Vite) — pages, forms, components, API calls or styling.
---

# Frontend React en PostulaAI

Lee primero `.claude/rules/code-quality.md` (tipado y accesibilidad).

## Estructura

```text
frontend/src/
  app/        router, layout, proveedores
  features/   auth · profile · jobs · analyses · settings (páginas, componentes, hooks, esquemas zod)
  shared/     api/ (cliente HTTP tipado) · ui/ (componentes base) · lib/
```

## Pasos

1. Coloca el código en la feature correspondiente. Lo reutilizable va a `shared/` solo cuando lo usan dos features.
2. Llamadas al backend solo mediante `shared/api/`. Datos de servidor con TanStack Query; nada de `fetch` en componentes.
3. Formularios con `react-hook-form` + esquema `zod`; los mensajes de error se asocian al campo (`aria-describedby`).
4. Textos de interfaz en español, claros y sin jerga de "chatbot".
5. Resultados de IA: nunca mostrar el puntaje como un número aislado; siempre con banda, conteos y el aviso de transparencia.
6. El access token vive solo en memoria; nunca en `localStorage`.
7. Pruebas con Vitest + Testing Library: consulta por rol/label, no por clases CSS.

## Verificación

```bash
cd frontend
npm run lint && npm run typecheck && npm test && npm run build
```
