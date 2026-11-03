#!/bin/sh
# Frontend dev server for docker-compose.watch.yml.
# Installs dependencies, starts Vite and, whenever package-lock.json changes (for example after a
# `git pull` that adds a package), reinstalls and restarts Vite on its own. No manual restart needed.
set -u

lock_hash() { md5sum package-lock.json | cut -d' ' -f1; }

while true; do
  echo "[dev] Installing dependencies..."
  npm ci --no-audit --no-fund || { echo "[dev] npm ci failed; retrying in 10 s"; sleep 10; continue; }
  current=$(lock_hash)

  # Run Vite directly (not through npx) so stopping this PID really frees the port.
  node node_modules/vite/bin/vite.js --host 0.0.0.0 --port 5173 --strictPort &
  vite_pid=$!

  while kill -0 "$vite_pid" 2>/dev/null && [ "$(lock_hash)" = "$current" ]; do
    sleep 3
  done

  if kill -0 "$vite_pid" 2>/dev/null; then
    echo "[dev] package-lock.json changed: reinstalling and restarting Vite."
    kill "$vite_pid"
  else
    echo "[dev] Vite stopped; restarting."
  fi
  wait "$vite_pid" 2>/dev/null
done
