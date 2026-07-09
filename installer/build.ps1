# MeuPDV — build completo para desktop Windows
# Gera API publicada + Angular em wwwroot + instalador NSIS (electron-builder)
#
# Pré-requisitos: .NET 10 SDK, Node.js 20+, npm
# Uso: .\installer\build.ps1

param(
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',
    [switch]$SkipInstaller,
    [switch]$SkipNpmCi
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent
$WebDir = Join-Path $Root 'src\MeuPdv.Web'
$ApiDir = Join-Path $Root 'src\MeuPdv.Api'
$DesktopDir = Join-Path $Root 'src\MeuPdv.Desktop'
$WwwRoot = Join-Path $ApiDir 'wwwroot'
$ApiOut = Join-Path $Root 'dist\api'
$AngularOut = Join-Path $WebDir 'dist\meupdv-web\browser'

Write-Host '=== MeuPDV — Build Desktop ===' -ForegroundColor Cyan

# 1. Angular
Write-Host '[1/4] Build Angular...' -ForegroundColor Yellow
Push-Location $WebDir
if (-not $SkipNpmCi) { npm ci }
npm run build
Pop-Location

if (-not (Test-Path $AngularOut)) {
    throw "Saída do Angular não encontrada em $AngularOut"
}

# 2. Copiar para wwwroot da API
Write-Host '[2/4] Copiar frontend para wwwroot...' -ForegroundColor Yellow
if (Test-Path $WwwRoot) { Remove-Item $WwwRoot -Recurse -Force }
New-Item -ItemType Directory -Path $WwwRoot -Force | Out-Null
Copy-Item (Join-Path $AngularOut '*') $WwwRoot -Recurse

# 3. Publicar API
Write-Host '[3/4] Publicar API .NET...' -ForegroundColor Yellow
if (Test-Path $ApiOut) { Remove-Item $ApiOut -Recurse -Force }
dotnet publish (Join-Path $ApiDir 'MeuPdv.Api.csproj') -c $Configuration -o $ApiOut -r win-x64 --self-contained true
if ($LASTEXITCODE -ne 0) { throw 'Falha ao publicar API' }

# 4. Electron + instalador
if (-not $SkipInstaller) {
    Write-Host '[4/4] Empacotar Electron (NSIS)...' -ForegroundColor Yellow
    Push-Location $DesktopDir
    if (-not $SkipNpmCi) { npm ci }
    npm run dist
    Pop-Location
} else {
    Write-Host '[4/4] Instalador ignorado (-SkipInstaller)' -ForegroundColor DarkGray
}

Write-Host ''
Write-Host 'Build concluído!' -ForegroundColor Green
Write-Host "  API:       $ApiOut"
if (-not $SkipInstaller) {
    Write-Host "  Instalador: $Root\dist\desktop\"
}
