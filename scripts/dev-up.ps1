<#
  Levanta PostulaAI en local con Docker Desktop (Windows PowerShell 5.1+ o PowerShell 7).
  - Verifica Docker.
  - Crea .env con secretos aleatorios si no existe (nunca se sube a Git).
  - Construye y arranca postgres + api + frontend y espera a que la API esté lista.
  Uso: powershell -ExecutionPolicy Bypass -File scripts\dev-up.ps1
#>
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
  Write-Host 'No se encontró Docker. Instala Docker Desktop: https://www.docker.com/products/docker-desktop/' -ForegroundColor Red
  exit 1
}
docker info *> $null
if ($LASTEXITCODE -ne 0) {
  Write-Host 'Docker Desktop no está en ejecución. Ábrelo, espera a que diga "Engine running" y vuelve a ejecutar este script.' -ForegroundColor Red
  exit 1
}

function New-Secret([int]$Bytes) {
  $buffer = New-Object byte[] $Bytes
  $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
  $rng.GetBytes($buffer)
  $rng.Dispose()
  return [Convert]::ToBase64String($buffer)
}

if (-not (Test-Path .env)) {
  $pgPassword = (New-Secret 24) -replace '[^A-Za-z0-9]', ''
  $jwtKey = New-Secret 48
  $content = [System.IO.File]::ReadAllText((Resolve-Path .env.example))
  $content = $content -replace '(?m)^POSTGRES_PASSWORD=.*$', "POSTGRES_PASSWORD=$pgPassword"
  $content = $content -replace '(?m)^JWT_SIGNING_KEY=.*$', "JWT_SIGNING_KEY=$jwtKey"
  $content = $content -replace '(?m)^ConnectionStrings__Postgres=.*$', "ConnectionStrings__Postgres=Host=localhost;Port=5432;Database=postulaai;Username=postulaai;Password=$pgPassword"
  # UTF-8 without BOM: Docker Compose does not accept a BOM at the start of .env.
  [System.IO.File]::WriteAllText((Join-Path (Get-Location) '.env'), $content, (New-Object System.Text.UTF8Encoding($false)))
  Write-Host 'Se creó .env con secretos aleatorios locales.' -ForegroundColor Green
}

Write-Host 'Construyendo y levantando contenedores (la primera vez tarda varios minutos)...'
docker compose up -d --build
if ($LASTEXITCODE -ne 0) { Write-Host 'docker compose falló. Revisa el mensaje anterior.' -ForegroundColor Red; exit 1 }

Write-Host 'Esperando a que la API y la base de datos estén listas...'
$deadline = (Get-Date).AddSeconds(180)
$ready = $false
while ((Get-Date) -lt $deadline) {
  try {
    $response = Invoke-WebRequest -Uri 'http://localhost:8080/health/ready' -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) { $ready = $true; break }
  } catch { Start-Sleep -Seconds 3 }
}

if (-not $ready) {
  Write-Host 'La API no respondió a tiempo. Últimos logs:' -ForegroundColor Yellow
  docker compose logs --tail 60 api
  exit 1
}

Write-Host ''
Write-Host 'PostulaAI está listo:' -ForegroundColor Green
Write-Host '  App:      http://localhost:5173'
Write-Host '  API:      http://localhost:8080/health/ready'
Write-Host '  Swagger:  http://localhost:8080/swagger'
Write-Host 'Para detenerlo: docker compose down'
