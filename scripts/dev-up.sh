#!/usr/bin/env bash
# Levanta PostulaAI en local con Docker (macOS/Linux). Crea .env con secretos aleatorios si no existe.
# Uso: ./scripts/dev-up.sh [--watch]   (--watch: recarga automática desde el código)
set -euo pipefail
cd "$(dirname "$0")/.."

command -v docker >/dev/null || { echo "No se encontró Docker." >&2; exit 1; }
docker info >/dev/null 2>&1 || { echo "Docker no está en ejecución." >&2; exit 1; }

if [ ! -f .env ]; then
  pg_password=$(openssl rand -base64 24 | tr -dc 'A-Za-z0-9')
  jwt_key=$(openssl rand -base64 48)
  sed -e "s|^POSTGRES_PASSWORD=.*|POSTGRES_PASSWORD=${pg_password}|" \
      -e "s|^JWT_SIGNING_KEY=.*|JWT_SIGNING_KEY=${jwt_key}|" \
      -e "s|^ConnectionStrings__Postgres=.*|ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=postulaai;Username=postulaai;Password=${pg_password}|" \
      .env.example > .env
  echo "Se creó .env con secretos aleatorios locales."
fi

env_value() { grep -E "^$1=" .env | tail -n 1 | cut -d= -f2- | tr -d '[:space:]'; }
api_port=$(env_value API_HOST_PORT); api_port=${api_port:-58080}
web_port=$(env_value WEB_HOST_PORT); web_port=${web_port:-5173}

compose_file=docker-compose.yml
attempts=60
docker compose -f docker-compose.yml stop >/dev/null 2>&1 || true
docker compose -f docker-compose.watch.yml stop >/dev/null 2>&1 || true
if [ "${1:-}" = "--watch" ]; then
  compose_file=docker-compose.watch.yml
  attempts=200
  docker compose -f "$compose_file" up -d --remove-orphans
else
  docker compose -f "$compose_file" up -d --build --remove-orphans
fi

for _ in $(seq 1 "$attempts"); do
  if curl -fsS "http://localhost:${api_port}/health/ready" >/dev/null 2>&1; then
    echo "PostulaAI está listo: http://localhost:${web_port}"
    exit 0
  fi
  sleep 3
done
echo "La API no respondió a tiempo. Últimos logs:" >&2
docker compose -f "$compose_file" logs --tail 60 api
exit 1
