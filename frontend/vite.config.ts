/// <reference types="vitest/config" />
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The API is always reached through the same origin (/api) so the refresh-token cookie
// can be SameSite=Strict. In development Vite proxies to the local ASP.NET Core API
// (or to the `api` container when running docker-compose.watch.yml).
const apiTarget = process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5080'
// Inside Docker on Windows/macOS, file change events from the host do not arrive; poll instead.
const usePolling = process.env.VITE_USE_POLLING === 'true'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    ...(usePolling ? { watch: { usePolling: true, interval: 500 } } : {}),
    proxy: {
      '/api': { target: apiTarget, changeOrigin: false },
      '/health': { target: apiTarget, changeOrigin: false },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
})
