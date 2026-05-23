#Requires -Version 5.1
$ErrorActionPreference = 'Stop'

$SetupDir = $PSScriptRoot
$RepoRoot = Split-Path -Parent $SetupDir
$ProjectFile = Join-Path $RepoRoot 'StrinovaReplayManager.csproj'
$IssFile = Join-Path $SetupDir 'StrinovaReplayManager.iss'
$PublishDir = Join-Path $RepoRoot 'bin\Release\net10.0-windows10.0.19041.0\win-x64\publish'
$OutputDir = Join-Path $SetupDir 'Output'
$RedistDir = Join-Path $SetupDir 'Redist'
$WinAppRuntimeUrl = 'https://aka.ms/windowsappsdk/2.1/2.1.3/windowsappruntimeinstall-x64.exe'
$WinAppRuntimeInstaller = Join-Path $RedistDir 'windowsappruntimeinstall-x64.exe'

function Ensure-WinAppRuntimeInstaller {
    if (-not (Test-Path $RedistDir)) {
        New-Item -ItemType Directory -Path $RedistDir | Out-Null
    }

    if (Test-Path $WinAppRuntimeInstaller) {
        Write-Host "Windows App Runtime installer already present: $WinAppRuntimeInstaller" -ForegroundColor DarkGray
        return
    }

    Write-Host 'Downloading Windows App Runtime installer...' -ForegroundColor Cyan
    Invoke-WebRequest -Uri $WinAppRuntimeUrl -OutFile $WinAppRuntimeInstaller
    if (-not (Test-Path $WinAppRuntimeInstaller)) {
        throw "Windows App Runtime installer was not downloaded to: $WinAppRuntimeInstaller"
    }
}

function Find-InnoSetupCompiler {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )

    foreach ($path in $candidates) {
        if (Test-Path $path) {
            return $path
        }
    }

    $fromPath = Get-Command iscc -ErrorAction SilentlyContinue
    if ($fromPath) {
        return $fromPath.Source
    }

    throw @"
Inno Setup 6 compiler (ISCC.exe) was not found.
Install Inno Setup 6 from https://jrsoftware.org/isinfo.php
or add ISCC.exe to your PATH.
"@
}

Write-Host 'Publishing Strinova Replay Manager (Release, win-x64)...' -ForegroundColor Cyan
Push-Location $RepoRoot
try {
    dotnet publish $ProjectFile -c Release -r win-x64 --self-contained true
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path (Join-Path $PublishDir 'StrinovaReplayManager.exe'))) {
    throw "Publish output not found at: $PublishDir"
}

Ensure-WinAppRuntimeInstaller

$iscc = Find-InnoSetupCompiler
Write-Host "Building installer with: $iscc" -ForegroundColor Cyan
& $iscc $IssFile
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compile failed with exit code $LASTEXITCODE"
}

$setupExe = Get-ChildItem -Path $OutputDir -Filter '*-Setup.exe' -File |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $setupExe) {
    throw "Installer was not created in: $OutputDir"
}

Write-Host ''
Write-Host 'Done.' -ForegroundColor Green
Write-Host "Setup executable: $($setupExe.FullName)" -ForegroundColor Green
