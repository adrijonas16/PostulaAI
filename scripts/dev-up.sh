#!/usr/bin/env bash
# Levanta PostulaAI en local con Docker (macOS/Linux). Crea .env con secretos aleatorios si no existe.
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

docker compose up -d --build

for _ in $(seq 1 60); do
  if curl -fsS http://localhost:8080/health/ready >/dev/null 2>&1; then
    echo "PostulaAI está listo: http://localhost:5173"
    exit 0
  fi
  sleep 3
done
echo "La API no respondió a tiempo. Últimos logs:" >&2
docker compose logs --tail 60 api
exit 1
