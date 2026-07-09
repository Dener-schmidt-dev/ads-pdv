# MeuPDV — ambiente de desenvolvimento local (Windows)
# Verifica pré-requisitos, instala o que faltar (winget), dependências do projeto e sobe API + Angular.
#
# Uso:
#   .\dev.ps1              # instala o necessário, sobe API e Angular, abre o navegador
#   .\dev.ps1 -Desktop     # inclui janela Electron
#   .\dev.ps1 -Setup       # só verifica/instala (não sobe serviços)
#   .\dev.ps1 -Stop        # encerra a sessão dev iniciada por este script
#   .\dev.ps1 -NoAutoInstall  # não instala .NET/Node/MongoDB (só verifica)
#
# Credenciais dev: admin / admin123

param(
    [switch]$Setup,
    [switch]$Stop,
    [switch]$Desktop,
    [switch]$SkipInstall,
    [switch]$NoAutoInstall,
    [switch]$NoBrowser,
    [switch]$SkipMongoCheck,
    [switch]$Elevated
)

$ErrorActionPreference = 'Stop'

$Root = $PSScriptRoot
$DevDir = Join-Path $Root '.dev'
$SessionFile = Join-Path $DevDir 'session.json'
$ApiDir = Join-Path $Root 'src\MeuPdv.Api'
$WebDir = Join-Path $Root 'src\MeuPdv.Web'
$DesktopDir = Join-Path $Root 'src\MeuPdv.Desktop'
$ApiUrl = 'http://localhost:5050'
$WebUrl = 'http://localhost:4200'
$HealthUrl = "$ApiUrl/api/health"

$WingetPackages = @{
    DotNetSdk   = 'Microsoft.DotNet.SDK.10'
    NodeJs      = 'OpenJS.NodeJS.LTS'
    MongoDb     = 'MongoDB.Server'
    MongoDbTools = 'MongoDB.DatabaseTools'
}

function Test-SmartAppControlEnabled {
    try {
        $policy = Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' -ErrorAction Stop
        # 1 = Smart App Control ativo (bloqueia binários locais não assinados)
        return $policy.VerifiedAndReputablePolicyState -eq 1
    } catch {
        return $false
    }
}

function Test-DotNetRunAllowed {
    $probeDir = Join-Path $env:TEMP 'MeuPdvSacProbe'
    try {
        if (Test-Path $probeDir) {
            Remove-Item $probeDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        dotnet new console -o $probeDir -n MeuPdvSacProbe --force 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { return $true }

        $csproj = Join-Path $probeDir 'MeuPdvSacProbe.csproj'
        $xml = Get-Content $csproj -Raw
        if ($xml -notmatch 'UseAppHost') {
            $xml = $xml -replace '</PropertyGroup>', "    <UseAppHost>false</UseAppHost>`r`n  </PropertyGroup>"
            Set-Content $csproj $xml -Encoding UTF8
        }

        Push-Location $probeDir
        dotnet build --nologo -v q 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { return $true }

        $prevEap = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $output = (& dotnet run --no-build --nologo 2>&1 | Out-String)
        } finally {
            $ErrorActionPreference = $prevEap
        }
        return $output -notmatch 'Controle de Aplicativo|800711C7|Application Control'
    } finally {
        Pop-Location -ErrorAction SilentlyContinue
        Remove-Item $probeDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Ensure-DotNetCanRun {
    if (Test-DotNetRunAllowed) { return }

    $sacHint = if (Test-SmartAppControlEnabled) {
        'Smart App Control está ATIVO neste PC.'
    } else {
        'Uma política de Controle de Aplicativo (WDAC) está bloqueando programas compilados localmente.'
    }

    throw @"
$sacHint
O Windows impediu a execução de um app .NET de teste — a API do MeuPDV não conseguirá subir até corrigir isso.

Como resolver (recomendado para desenvolvimento):
  1. Configurações → Privacidade e segurança → Segurança do Windows
  2. Controle de aplicativos e navegador → Configurações do Smart App Control
  3. Desative o Smart App Control e reinicie o PC

Alternativa: clone o projeto fora do OneDrive (ex.: C:\dev\pdv) após desativar o SAC.
Obs.: depois de desligar o SAC, só é possível reativá-lo reinstalando o Windows.

Depois de reiniciar, execute novamente: .\dev.ps1
"@
}

function Write-Step([string]$Message) {
    Write-Host $Message -ForegroundColor Yellow
}

function Write-Ok([string]$Message) {
    Write-Host $Message -ForegroundColor Green
}

function Write-Warn([string]$Message) {
    Write-Host $Message -ForegroundColor DarkYellow
}

function Test-Command([string]$Name) {
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Refresh-SessionPath {
    $machine = [Environment]::GetEnvironmentVariable('Path', 'Machine')
    $user = [Environment]::GetEnvironmentVariable('Path', 'User')
    $env:Path = "$machine;$user"

    $nodeDirs = @(
        "${env:ProgramFiles}\nodejs",
        "${env:ProgramFiles(x86)}\nodejs"
    )
    foreach ($dir in $nodeDirs) {
        if ((Test-Path $dir) -and ($env:Path -notlike "*$dir*")) {
            $env:Path = "$dir;$env:Path"
        }
    }
}

function Get-NodeMajorVersion {
    if (-not (Test-Command 'node')) { return $null }
    $raw = (node -v 2>$null).Trim() -replace '^v', ''
    try { return [version]$raw } catch { return $null }
}

function Get-DotNetSdkVersion {
    if (-not (Test-Command 'dotnet')) { return $null }
    try { return [version]((dotnet --version 2>$null).Trim()) } catch { return $null }
}

function Test-PortListening([int]$Port) {
    $conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    return $null -ne $conn
}

function Test-Winget {
    return Test-Command 'winget'
}

function Install-WingetPackage([string]$Id, [string]$Label) {
    Write-Step "Instalando $Label via winget ($Id)..."
    Write-Warn 'Pode pedir confirmação na janela do instalador. Aguarde...'

    $args = @(
        'install', '--id', $Id,
        '-e',
        '--accept-package-agreements',
        '--accept-source-agreements',
        '--disable-interactivity'
    )

    & winget @args
    $code = $LASTEXITCODE
    if ($code -ne 0 -and $code -ne 2316632105) {
        # 2316632105 = já instalado / nada a atualizar
        throw "Falha ao instalar $Label (winget exit $code). Tente manualmente: winget install $Id"
    }

    Refresh-SessionPath
    Start-Sleep -Seconds 2
    Write-Ok "$Label instalado ou já estava presente"
}

function Request-AdminRelaunch {
    Write-Warn 'Instalação de ferramentas do sistema requer permissão de administrador.'
    Write-Step 'Reabrindo o script elevado...'

    $argList = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $PSCommandPath,
        '-Elevated'
    )

    if ($Setup) { $argList += '-Setup' }
    if ($Stop) { $argList += '-Stop' }
    if ($Desktop) { $argList += '-Desktop' }
    if ($SkipInstall) { $argList += '-SkipInstall' }
    if ($NoAutoInstall) { $argList += '-NoAutoInstall' }
    if ($NoBrowser) { $argList += '-NoBrowser' }
    if ($SkipMongoCheck) { $argList += '-SkipMongoCheck' }

    Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $argList
    exit 0
}

function Test-MongoAvailable {
    $mongoService = Get-Service -Name 'MongoDB' -ErrorAction SilentlyContinue
    if ($mongoService -and $mongoService.Status -eq 'Running') {
        return $true
    }
    return Test-PortListening 27017
}

function Start-MongoService {
    $mongoService = Get-Service -Name 'MongoDB' -ErrorAction SilentlyContinue
    if (-not $mongoService) {
        return $false
    }

    if ($mongoService.Status -ne 'Running') {
        Write-Step 'Iniciando serviço MongoDB...'
        try {
            Start-Service MongoDB
            Start-Sleep -Seconds 3
        } catch {
            if (-not (Test-IsAdmin)) {
                Write-Warn 'Sem permissão para iniciar o MongoDB. Execute o script como administrador.'
            }
            return $false
        }
    }

    return (Get-Service MongoDB).Status -eq 'Running'
}

function Get-MissingPrerequisites {
    $missing = [System.Collections.Generic.List[string]]::new()

    $dotnet = Get-DotNetSdkVersion
    if (-not $dotnet -or $dotnet.Major -lt 10) {
        $missing.Add('DotNet')
    }

    $node = Get-NodeMajorVersion
    if (-not $node -or $node.Major -lt 20) {
        $missing.Add('Node')
    }

    if (-not $SkipMongoCheck -and -not (Test-MongoAvailable)) {
        $missing.Add('MongoDb')
    }

    return $missing
}

function Install-SystemPrerequisites([string[]]$Missing) {
    if ($Missing.Count -eq 0) { return }

    if (-not (Test-Winget)) {
        throw @'
winget não encontrado. No Windows 10/11 instale "App Installer" pela Microsoft Store
ou instale manualmente:
  .NET SDK 10: https://dotnet.microsoft.com/download
  Node.js 20 LTS: https://nodejs.org/
  MongoDB: https://www.mongodb.com/try/download/community
'@
    }

    if (-not (Test-IsAdmin) -and -not $Elevated) {
        Request-AdminRelaunch
    }

    Write-Host ''
    Write-Host '=== MeuPDV — Instalando ferramentas do sistema ===' -ForegroundColor Cyan
    Write-Host ''

    if ($Missing -contains 'DotNet') {
        Install-WingetPackage -Id $WingetPackages.DotNetSdk -Label '.NET SDK 10'
    }

    if ($Missing -contains 'Node') {
        Install-WingetPackage -Id $WingetPackages.NodeJs -Label 'Node.js LTS'
    }

    if ($Missing -contains 'MongoDb') {
        Install-WingetPackage -Id $WingetPackages.MongoDb -Label 'MongoDB Server'
        Start-Sleep -Seconds 5
        if (-not (Start-MongoService)) {
            Write-Warn 'MongoDB instalado, mas o serviço ainda não está ativo. Aguardando...'
            for ($i = 0; $i -lt 12; $i++) {
                Start-Sleep -Seconds 5
                if (Start-MongoService) { break }
            }
        }
    }

    Refresh-SessionPath
}

function Ensure-Prerequisites {
    Write-Host ''
    Write-Host '=== MeuPDV — Verificando pré-requisitos ===' -ForegroundColor Cyan
    Write-Host ''

    if (-not $NoAutoInstall) {
        $missing = @(Get-MissingPrerequisites)
        if ($missing.Count -gt 0) {
            Write-Warn "Faltando: $($missing -join ', ')"
            Install-SystemPrerequisites -Missing $missing

            $stillMissing = @(Get-MissingPrerequisites)
            if ($stillMissing.Count -gt 0) {
                Write-Warn @"
Alguns itens ainda não estão disponíveis: $($stillMissing -join ', ')
Feche e abra um NOVO terminal PowerShell e execute novamente: .\dev.ps1
(Instaladores recém-concluídos podem exigir terminal novo para atualizar o PATH)
"@
                if ($stillMissing -contains 'Node') {
                    throw 'Node.js 20+ ainda não detectado após instalação.'
                }
                if ($stillMissing -contains 'DotNet') {
                    throw '.NET SDK 10 ainda não detectado após instalação.'
                }
                if ($stillMissing -contains 'MongoDb') {
                    throw 'MongoDB ainda não está acessível em localhost:27017.'
                }
            }
        }
    }

    $dotnet = Get-DotNetSdkVersion
    if (-not $dotnet -or $dotnet.Major -lt 10) {
        throw '.NET SDK 10 não encontrado. Execute sem -NoAutoInstall ou instale: https://dotnet.microsoft.com/download'
    }
    Write-Ok ".NET SDK $dotnet"

    $node = Get-NodeMajorVersion
    if (-not $node -or $node.Major -lt 20) {
        throw 'Node.js 20+ não encontrado. Execute sem -NoAutoInstall ou instale: https://nodejs.org/'
    }
    Write-Ok "Node.js v$node"

    if (-not (Test-Command 'npm')) {
        throw 'npm não encontrado. Reinstale o Node.js LTS.'
    }
    Write-Ok "npm $(npm -v)"

    Ensure-DotNetCanRun

    if (-not $SkipMongoCheck) {
        if (-not (Test-MongoAvailable)) {
            if (-not $NoAutoInstall) {
                Start-MongoService | Out-Null
            }
        }

        if (Test-MongoAvailable) {
            $svc = Get-Service -Name 'MongoDB' -ErrorAction SilentlyContinue
            if ($svc -and $svc.Status -eq 'Running') {
                Write-Ok 'MongoDB (serviço Windows) em execução'
            } else {
                Write-Ok 'MongoDB respondendo na porta 27017'
            }
        } else {
            throw @'
MongoDB não detectado em localhost:27017.
Execute .\dev.ps1 novamente (como administrador) ou instale manualmente:
https://www.mongodb.com/try/download/community
'@
        }
    } else {
        Write-Warn 'Verificação do MongoDB ignorada (-SkipMongoCheck)'
    }

    Ensure-MongoDatabaseTools
}

function Ensure-MongoDatabaseTools {
    if (Test-Command 'mongodump') {
        Write-Ok 'MongoDB Database Tools (mongodump) disponível'
        return
    }

    Write-Warn 'mongodump não encontrado — tela de Backup não funcionará até instalar MongoDB Database Tools.'

    if ($NoAutoInstall -or -not (Test-Winget)) { return }

    if (-not (Test-IsAdmin)) {
        Write-Warn 'Para instalar automaticamente, execute .\dev.ps1 -Setup em terminal como administrador.'
        return
    }

    try {
        Install-WingetPackage -Id $WingetPackages.MongoDbTools -Label 'MongoDB Database Tools'
        if (Test-Command 'mongodump') {
            Write-Ok 'MongoDB Database Tools instalado'
        }
    } catch {
        Write-Warn 'Instale manualmente: winget install MongoDB.DatabaseTools'
    }
}

function Wait-HttpOk([string]$Url, [int]$TimeoutSeconds = 90) {
    for ($i = 0; $i -lt $TimeoutSeconds; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                return $true
            }
        } catch {
            # aguardando serviço subir
        }
        Start-Sleep -Seconds 1
    }
    return $false
}

function Stop-DevSession {
    if (-not (Test-Path $SessionFile)) {
        Write-Warn 'Nenhuma sessão dev registrada (.dev/session.json não encontrado).'
        return
    }

    $session = Get-Content $SessionFile -Raw | ConvertFrom-Json
    foreach ($entry in $session.processes) {
        $proc = Get-Process -Id $entry.pid -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Step "Encerrando $($entry.name) (PID $($entry.pid))..."
            Stop-Process -Id $entry.pid -Force -ErrorAction SilentlyContinue
        }
    }

    Remove-Item $SessionFile -Force -ErrorAction SilentlyContinue
    Write-Ok 'Sessão dev encerrada.'
}

function Start-DevWindow([string]$Name, [string]$WorkingDirectory, [string]$Command) {
    $title = "MeuPDV - $Name"
    $escapedDir = $WorkingDirectory.Replace("'", "''")
    $escapedCmd = $Command.Replace("'", "''")
    $psCommand = @"
Set-Location '$escapedDir'
`$host.UI.RawUI.WindowTitle = '$title'
$escapedCmd
"@

    $proc = Start-Process -FilePath 'powershell.exe' -PassThru -ArgumentList @(
        '-NoExit',
        '-NoLogo',
        '-Command',
        $psCommand
    )

    return [pscustomobject]@{
        name = $Name
        pid  = $proc.Id
    }
}

function Ensure-DevDir {
    if (-not (Test-Path $DevDir)) {
        New-Item -ItemType Directory -Path $DevDir -Force | Out-Null
    }
}

function Install-ProjectDependencies {
    Write-Host ''
    Write-Host '=== MeuPDV — Instalando dependências do projeto ===' -ForegroundColor Cyan
    Write-Host ''

    Write-Step 'Restaurando pacotes .NET...'
    dotnet restore (Join-Path $ApiDir 'MeuPdv.Api.csproj')
    if ($LASTEXITCODE -ne 0) { throw 'Falha no dotnet restore' }
    Write-Ok 'Pacotes .NET restaurados'

    if (-not $SkipInstall) {
        $installWeb = $Setup -or -not (Test-Path (Join-Path $WebDir 'node_modules'))
        if ($installWeb) {
            Write-Step 'npm install (MeuPdv.Web)...'
            Push-Location $WebDir
            npm install
            if ($LASTEXITCODE -ne 0) { Pop-Location; throw 'Falha no npm install (Web)' }
            Pop-Location
            Write-Ok 'Dependências do Angular instaladas'
        } else {
            Write-Ok 'MeuPdv.Web — node_modules já existe'
        }

        if ($Desktop -or $Setup) {
            $installDesktop = $Setup -or -not (Test-Path (Join-Path $DesktopDir 'node_modules'))
            if ($installDesktop) {
                Write-Step 'npm install (MeuPdv.Desktop)...'
                Push-Location $DesktopDir
                npm install
                if ($LASTEXITCODE -ne 0) { Pop-Location; throw 'Falha no npm install (Desktop)' }
                Pop-Location
                Write-Ok 'Dependências do Electron instaladas'
            } else {
                Write-Ok 'MeuPdv.Desktop — node_modules já existe'
            }
        }
    } else {
        Write-Warn 'Instalação npm ignorada (-SkipInstall)'
    }
}

function Start-DevEnvironment {
    if (Test-Path $SessionFile) {
        $existing = Get-Content $SessionFile -Raw | ConvertFrom-Json
        $alive = @($existing.processes | Where-Object {
            Get-Process -Id $_.pid -ErrorAction SilentlyContinue
        })
        if ($alive.Count -gt 0) {
            throw @"
Já existe uma sessão dev ativa (PIDs: $($alive.pid -join ', ')).
Encerre com: .\dev.ps1 -Stop
"@
        }
        Remove-Item $SessionFile -Force
    }

    if (Test-PortListening 5050) {
        throw 'Porta 5050 em uso. Encerre o processo anterior ou execute .\dev.ps1 -Stop'
    }
    if (Test-PortListening 4200) {
        throw 'Porta 4200 em uso. Encerre o processo anterior ou execute .\dev.ps1 -Stop'
    }

    Write-Host ''
    Write-Host '=== MeuPDV — Iniciando ambiente ===' -ForegroundColor Cyan
    Write-Host ''

    Ensure-DevDir
    $processes = @()

    Write-Step 'Subindo API (.NET) em nova janela...'
    $processes += Start-DevWindow -Name 'API' -WorkingDirectory $ApiDir -Command 'dotnet run'

    Write-Step "Aguardando API em $HealthUrl ..."
    if (-not (Wait-HttpOk $HealthUrl 120)) {
        Stop-Process -Id $processes[0].pid -Force -ErrorAction SilentlyContinue
        throw 'API não respondeu a tempo. Verifique a janela MeuPDV - API.'
    }
    Write-Ok "API disponível em $ApiUrl"

    Write-Step 'Subindo Angular em nova janela...'
    $processes += Start-DevWindow -Name 'Web' -WorkingDirectory $WebDir -Command 'npm start'

    Write-Step "Aguardando frontend em $WebUrl ..."
    if (-not (Wait-HttpOk $WebUrl 180)) {
        foreach ($p in $processes) {
            Stop-Process -Id $p.pid -Force -ErrorAction SilentlyContinue
        }
        throw 'Angular não respondeu a tempo. Verifique a janela MeuPDV - Web.'
    }
    Write-Ok "Frontend disponível em $WebUrl"

    if ($Desktop) {
        Write-Step 'Subindo Electron em nova janela...'
        $processes += Start-DevWindow -Name 'Desktop' -WorkingDirectory $DesktopDir -Command 'npm start'
        Write-Ok 'Electron iniciado (aguarde a janela do app)'
    }

    $session = @{
        startedAt = (Get-Date).ToString('o')
        processes = $processes
    }
    $session | ConvertTo-Json -Depth 4 | Set-Content $SessionFile -Encoding UTF8

    Write-Host ''
    Write-Host '=== Ambiente pronto ===' -ForegroundColor Green
    Write-Host "  API:       $ApiUrl"
    Write-Host "  Frontend:  $WebUrl"
    Write-Host "  Login:     admin / admin123"
    Write-Host ''
    Write-Host "  Janelas: MeuPDV - API, MeuPDV - Web$(if ($Desktop) { ', MeuPDV - Desktop' })"
    Write-Host '  Encerrar:  .\dev.ps1 -Stop'
    Write-Host ''

    if (-not $NoBrowser) {
        Start-Process $WebUrl
    }
}

# --- main ---

Refresh-SessionPath

if ($Stop) {
    Stop-DevSession
    exit 0
}

Ensure-Prerequisites
Install-ProjectDependencies

if ($Setup) {
    Write-Host ''
    Write-Ok 'Setup concluído. Para subir o ambiente: .\dev.ps1'
    exit 0
}

Start-DevEnvironment
