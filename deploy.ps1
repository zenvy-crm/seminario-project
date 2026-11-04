<#
.SYNOPSIS
    Sube el API a la VM y lo levanta como un contenedor mas junto al stack db-infra.

.DESCRIPTION
    1. Genera api.env a partir del .env local si todavia no existe (las bases de datos
       pasan a resolverse por nombre de contenedor: mssql-relational / mongo-nosql).
    2. Construye la imagen AQUI (Docker Desktop) y la exporta con "docker save".
    3. Copia imagen + docker-compose.yml + api.env por scp, y en la VM hace
       "docker load" + "docker compose up -d".
    4. Espera a que GET /api/health responda 200.

    La imagen se construye en esta maquina y no en la VM a proposito: en la VM los
    contenedores de build no resuelven DNS (apt y NuGet fallan) y la descarga de la imagen
    del SDK es muy lenta. Asi la VM solo ejecuta y no necesita salida a internet.

    Si la VM pide contrasena, ssh/scp la solicitan en consola (2 veces). Para evitarlo,
    instale su llave publica en la VM:
        type $env:USERPROFILE\.ssh\id_ed25519.pub | ssh USUARIO@192.168.1.135 "mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys"

.EXAMPLE
    .\deploy.ps1 -User miusuario
#>
param(
    [string]$User,
    [string]$VmHost = "192.168.1.135",
    [int]$Port = 5000,
    [ValidatePattern('^[\w./-]+$')][string]$RemoteDir = "seminario-api",
    # Solo construye y deja el paquete en %TEMP%\seminario-api.tgz, sin tocar la VM.
    [switch]$PackageOnly
)

$ErrorActionPreference = "Stop"

$repoRoot   = $PSScriptRoot
$projectDir = Join-Path $repoRoot "seminario-backend-service"
$apiEnv     = Join-Path $repoRoot "api.env"
$localEnv   = Join-Path $projectDir ".env"
$target     = "$User@$VmHost"
$image      = "seminario-backend-service:latest"
$stage      = Join-Path $env:TEMP "seminario-api-stage"
$package    = Join-Path $env:TEMP "seminario-api.tgz"

if (-not $PackageOnly -and -not $User) {
    throw "Indique el usuario SSH de la VM: .\deploy.ps1 -User <usuario>"
}

function Test-DockerDaemon {
    # Local a la funcion: con "Stop", PowerShell 5.1 convierte en excepcion el stderr
    # redirigido de un ejecutable nativo (que es justo lo que pasa con el daemon apagado).
    $ErrorActionPreference = "Continue"
    docker info --format '{{.ServerVersion}}' 2>&1 | Out-Null
    return $LASTEXITCODE -eq 0
}

# --- 1. api.env -------------------------------------------------------------
if (-not (Test-Path $apiEnv)) {
    if (-not (Test-Path $localEnv)) {
        throw "No existe api.env ni $localEnv. Copie api.env.example como api.env y completelo."
    }

    $lines = Get-Content $localEnv |
        Where-Object { $_ -notmatch '^\s*ASPNETCORE_(ENVIRONMENT|URLS)\s*=' } |
        ForEach-Object {
            if ($_ -match '^\s*ConnectionStrings__DefaultConnection\s*=') {
                $_ -replace 'Server=[^;]+', 'Server=mssql-relational,1433'
            }
            elseif ($_ -match '^\s*MongoDB__ConnectionString\s*=') {
                $_ -replace '(?<=@)[^@/?]+(?=[/?]|$)', 'mongo-nosql:27017'
            }
            else { $_ }
        }

    $content = (@("ASPNETCORE_ENVIRONMENT=Production") + $lines) -join "`n"
    # LF y sin BOM: el archivo lo lee docker compose en Linux.
    [IO.File]::WriteAllText($apiEnv, $content + "`n", (New-Object Text.UTF8Encoding $false))
    Write-Host "api.env generado desde .env (revise Cors__AllowedOrigin si el frontend no corre en localhost:5173)." -ForegroundColor Yellow
}

# --- 2. Construir la imagen en local ----------------------------------------
if (-not (Test-DockerDaemon)) {
    $desktop = Join-Path $env:ProgramFiles "Docker\Docker\Docker Desktop.exe"
    if (-not (Test-Path $desktop)) {
        throw "Docker no esta corriendo en esta maquina. Inicie Docker Desktop y vuelva a ejecutar."
    }

    Write-Host "Docker Desktop esta apagado; iniciandolo..." -ForegroundColor Yellow
    Start-Process $desktop
    for ($i = 0; $i -lt 36 -and -not (Test-DockerDaemon); $i++) { Start-Sleep -Seconds 5 }
    if (-not (Test-DockerDaemon)) { throw "Docker Desktop no arranco en 3 minutos." }
}

Write-Host "Construyendo $image ..." -ForegroundColor Cyan
# --provenance=false: imagen de una sola plataforma, sin indice de atestaciones, para que
# "docker load" la acepte tambien en un Docker mas antiguo en la VM.
docker build --platform linux/amd64 --provenance=false -t $image $projectDir
if ($LASTEXITCODE -ne 0) { throw "docker build fallo con codigo $LASTEXITCODE" }

Write-Host "Exportando y comprimiendo la imagen..." -ForegroundColor Cyan
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    docker save -o (Join-Path $stage "seminario-api-image.tar") $image
    if ($LASTEXITCODE -ne 0) { throw "docker save fallo con codigo $LASTEXITCODE" }

    Copy-Item (Join-Path $repoRoot "docker-compose.yml") $stage
    Copy-Item $apiEnv $stage

    tar -czf $package -C $stage seminario-api-image.tar docker-compose.yml api.env
    if ($LASTEXITCODE -ne 0) { throw "tar fallo con codigo $LASTEXITCODE" }
}
finally {
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host ("Paquete: {0:N0} MB" -f ((Get-Item $package).Length / 1MB))

if ($PackageOnly) {
    Write-Host "Paquete listo en $package (no se subio a la VM)." -ForegroundColor Green
    exit 0
}

# --- 3. Subir y levantar ----------------------------------------------------
Write-Host "Copiando paquete a $target..." -ForegroundColor Cyan
scp $package "${target}:/tmp/seminario-api.tgz"
if ($LASTEXITCODE -ne 0) { throw "scp fallo con codigo $LASTEXITCODE" }
Remove-Item $package -Force

# Solo comillas simples: PowerShell 5.1 estropea las dobles al pasarlas a ssh.
$remote = @(
    "docker network inspect db-infra_db-infra-net >/dev/null 2>&1 || { echo 'ERROR: no existe la red db-infra_db-infra-net; levante primero el stack db-infra.'; exit 1; }"
    "mkdir -p $RemoteDir"
    "cd $RemoteDir"
    # Restos del despliegue antiguo, que copiaba el codigo fuente para compilar en la VM.
    "rm -rf ./seminario-backend-service"
    "tar -xzf /tmp/seminario-api.tgz"
    "rm -f /tmp/seminario-api.tgz"
    "chmod 600 api.env"
    "docker load -i seminario-api-image.tar"
    "rm -f seminario-api-image.tar"
    "API_PORT=$Port docker compose up -d --force-recreate"
    "docker compose ps"
) -join " && "

Write-Host "Cargando la imagen y levantando el contenedor en la VM..." -ForegroundColor Cyan
ssh $target $remote
if ($LASTEXITCODE -ne 0) { throw "El despliegue remoto fallo con codigo $LASTEXITCODE" }

# --- 4. Verificar -----------------------------------------------------------
$healthUrl = "http://${VmHost}:$Port/api/health"
Write-Host "Esperando $healthUrl ..." -ForegroundColor Cyan
for ($i = 0; $i -lt 30; $i++) {
    try {
        $health = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 3
        Write-Host "OK: $($health | ConvertTo-Json -Compress)" -ForegroundColor Green
        exit 0
    }
    catch {
        Start-Sleep -Seconds 3
    }
}

Write-Host "El API no respondio en 90s. Revise los logs en la VM:" -ForegroundColor Red
Write-Host "  ssh $target 'cd $RemoteDir && docker compose logs --tail 80 api'"
exit 1
