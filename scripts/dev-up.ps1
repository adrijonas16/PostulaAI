<#
  Levanta PostulaAI en local con Docker Desktop (Windows PowerShell 5.1+ o PowerShell 7).
  - Verifica Docker.
  - Crea .env con secretos aleatorios si no existe (nunca se sube a Git).
  - Comprueba que los puertos estén libres y dice qué programa los ocupa.
  - Construye y arranca postgres + api + frontend y espera a que la API esté lista.
  - Con -Watch arranca en modo recarga automática: la app se ejecuta desde el código y
    cualquier cambio (una edición o un git pull) se ve sin reconstruir nada.
  Uso: powershell -ExecutionPolicy Bypass -File scripts\dev-up.ps1 [-Watch]
#>
param([switch]$Watch)

$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

function Write-Problem([string]$Message) { Write-Host $Message -ForegroundColor Red }

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
  Write-Problem 'No se encontró Docker. Instala Docker Desktop: https://www.docker.com/products/docker-desktop/'
  exit 1
}

# Windows PowerShell 5.1 turns redirected native stderr into a terminating error under 'Stop',
# so native commands whose output we inspect run with 'Continue' and rely on the exit code.
$previousPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
docker info 2>&1 | Out-Null
$dockerExitCode = $LASTEXITCODE
$ErrorActionPreference = $previousPreference
if ($dockerExitCode -ne 0) {
  Write-Problem 'Docker Desktop no está en ejecución. Ábrelo, espera a que diga "Engine running" y vuelve a ejecutar este script.'
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
  $content = $content -replace '(?m)^(ConnectionStrings__Postgres=.*Password=).*$', "`${1}$pgPassword"
  # UTF-8 without BOM: Docker Compose does not accept a BOM at the start of .env.
  [System.IO.File]::WriteAllText((Join-Path (Get-Location) '.env'), $content, (New-Object System.Text.UTF8Encoding($false)))
  Write-Host 'Se creó .env con secretos aleatorios locales.' -ForegroundColor Green
}

function Get-EnvValue([string]$Name, [string]$Default) {
  $line = Get-Content .env | Where-Object { $_ -match "^\s*$Name\s*=" } | Select-Object -Last 1
  if ($null -eq $line) { return $Default }
  $value = ($line -split '=', 2)[1].Trim()
  if ($value -eq '') { return $Default }
  return $value
}

$ports = [ordered]@{
  'WEB_HOST_PORT'      = [int](Get-EnvValue 'WEB_HOST_PORT' '5173')
  'API_HOST_PORT'      = [int](Get-EnvValue 'API_HOST_PORT' '58080')
  'POSTGRES_HOST_PORT' = [int](Get-EnvValue 'POSTGRES_HOST_PORT' '55432')
  'MAIL_UI_HOST_PORT'  = [int](Get-EnvValue 'MAIL_UI_HOST_PORT' '8025')
}

$composeFile = if ($Watch) { 'docker-compose.watch.yml' } else { 'docker-compose.yml' }

# Stop our own containers (from either mode) first so their ports do not count as "in use".
$ErrorActionPreference = 'Continue'
docker compose -f docker-compose.yml stop 2>&1 | Out-Null
docker compose -f docker-compose.watch.yml stop 2>&1 | Out-Null
$ErrorActionPreference = $previousPreference

$conflicts = @()
foreach ($entry in $ports.GetEnumerator()) {
  $listeners = Get-NetTCPConnection -State Listen -LocalPort $entry.Value -ErrorAction SilentlyContinue
  foreach ($listener in $listeners) {
    $process = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
    $name = if ($null -ne $process) { $process.ProcessName } else { "PID $($listener.OwningProcess)" }
    $conflicts += [pscustomobject]@{ Variable = $entry.Key; Port = $entry.Value; Process = $name }
  }
}

if ($conflicts.Count -gt 0) {
  Write-Problem 'Hay puertos ocupados por otros programas:'
  $conflicts | Sort-Object Port -Unique | ForEach-Object {
    Write-Problem ("  Puerto {0} lo usa '{1}'. Cambia {2} en .env a otro número (por ejemplo {3})." -f $_.Port, $_.Process, $_.Variable, ($_.Port + 1))
  }
  Write-Host 'Abre el archivo con: notepad .env   y vuelve a ejecutar este script.'
  exit 1
}

if ($Watch) {
  Write-Host 'Levantando en modo recarga automática (la primera vez descarga dependencias y compila; puede tardar varios minutos)...'
  # Recreate the containers and their dependency volumes so new packages or settings always take effect.
  docker compose -f $composeFile up -d --remove-orphans --force-recreate --renew-anon-volumes
} else {
  Write-Host 'Construyendo y levantando contenedores (la primera vez tarda varios minutos)...'
  docker compose -f $composeFile up -d --build --remove-orphans
}
if ($LASTEXITCODE -ne 0) {
  Write-Problem 'docker compose falló. Revisa el mensaje anterior.'
  exit 1
}

$apiPort = $ports['API_HOST_PORT']
$webPort = $ports['WEB_HOST_PORT']
Write-Host 'Esperando a que la API y la base de datos estén listas...'
$deadline = (Get-Date).AddSeconds($(if ($Watch) { 600 } else { 180 }))
$ready = $false
while ((Get-Date) -lt $deadline) {
  try {
    $response = Invoke-WebRequest -Uri "http://localhost:$apiPort/health/ready" -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) { $ready = $true; break }
  } catch { Start-Sleep -Seconds 3 }
}

if (-not $ready) {
  Write-Problem 'La API no respondió a tiempo. Estado y últimos logs:'
  docker compose -f $composeFile ps -a
  docker compose -f $composeFile logs --tail 60 api
  exit 1
}

Write-Host ''
Write-Host 'PostulaAI está listo:' -ForegroundColor Green
Write-Host "  App:      http://localhost:$webPort"
Write-Host "  API:      http://localhost:$apiPort/health/ready"
Write-Host "  Swagger:  http://localhost:$apiPort/swagger"
Write-Host "  Correos:  http://localhost:$($ports['MAIL_UI_HOST_PORT'])  (aquí llegan los correos de confirmación en local)"
if ((Get-EnvValue 'ANTHROPIC_API_KEY' '') -eq '') {
  Write-Host '  Nota: ANTHROPIC_API_KEY está vacía; el análisis con IA responderá "no configurado".' -ForegroundColor Yellow
}
if ($Watch) {
  Write-Host '  Modo recarga automática: los cambios en el código se aplican solos.' -ForegroundColor Green
  Write-Host '  La página se actualiza al instante; la API se reinicia sola en unos segundos.'
  Write-Host "Para ver los logs: docker compose -f $composeFile logs -f api frontend"
}
Write-Host "Para detenerlo: docker compose -f $composeFile down"
