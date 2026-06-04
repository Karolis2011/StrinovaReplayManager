# Probes Strinova replay CDN availability for local Demos filenames.
# Usage: .\probe-replay-cdn.ps1 [-FixturePath path\to\demos-symlinks.txt]

param(
    [string]$FixturePath = (Join-Path $PSScriptRoot 'fixtures\demos-symlinks.txt')
)

$Base = 'https://replay-download.strinova.com/record/'

function Get-ServerName([string]$local) {
    $stem = $local.Substring(0, $local.Length - 7)
    $parts = $stem -split '_'
    return (($parts | Select-Object -Skip 1) -join '_') + '.replay'
}

function Get-Unix([string]$local) {
    $stem = $local.Substring(0, $local.Length - 7)
    $parts = $stem -split '_'
    $pen = $parts[$parts.Length - 2]
    if ($pen.Length -eq 10 -and $pen -match '^\d+$') { return [long]$pen }
    return $null
}

function Probe-Url([string]$url) {
    try {
        $r = Invoke-WebRequest -Uri $url -Method Head -TimeoutSec 20 -UseBasicParsing
        return [int]$r.StatusCode
    }
    catch {
        if ($_.Exception.Response) {
            $code = [int]$_.Exception.Response.StatusCode.value__
            if ($code -eq 405) {
                try {
                    $r2 = Invoke-WebRequest -Uri $url -Method Get -Headers @{ Range = 'bytes=0-0' } -TimeoutSec 20 -UseBasicParsing
                    return [int]$r2.StatusCode
                }
                catch {
                    if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode.value__ }
                }
            }
            return $code
        }
        return -1
    }
}

if (-not (Test-Path $FixturePath)) {
    Write-Error "Fixture not found: $FixturePath"
    exit 1
}

$locals = Get-Content $FixturePath | Where-Object { $_.Trim().Length -gt 0 }
$now = [DateTimeOffset]::UtcNow
$rows = foreach ($local in $locals) {
    $server = Get-ServerName $local.Trim()
    $status = Probe-Url ($Base + $server)
    $u = Get-Unix $local.Trim()
    $age = $null
    if ($u) { $age = ($now - [DateTimeOffset]::FromUnixTimeSeconds($u)).TotalDays }
    [pscustomobject]@{ Local = $local.Trim(); Server = $server; Status = $status; AgeDays = if ($null -ne $age) { [math]::Round($age, 1) } else { $null } }
}

$rows = $rows | Sort-Object { if ($null -eq $_.AgeDays) { [double]::MaxValue } else { $_.AgeDays } }
Write-Host "now_utc $($now.UtcDateTime.ToString('o'))"
Write-Host "count $($rows.Count)"
Write-Host ''
$rows | Format-Table Status, AgeDays, Server -AutoSize

$avail = @($rows | Where-Object { $_.Status -in 200, 206 })
$miss = @($rows | Where-Object { $_.Status -eq 404 })
Write-Host ''
Write-Host "available: $($avail.Count) 404: $($miss.Count)"
if ($avail.Count -gt 0) {
    $maxA = ($avail | Where-Object { $null -ne $_.AgeDays } | Measure-Object -Property AgeDays -Maximum).Maximum
    Write-Host "oldest_available_days=$maxA"
}
if ($miss.Count -gt 0) {
    $valid404 = $miss | Where-Object { $null -ne $_.AgeDays }
    if ($valid404.Count -gt 0) {
        $minM = ($valid404 | Measure-Object -Property AgeDays -Minimum).Minimum
        Write-Host "youngest_404_days=$minM"
    }
}

if ($avail.Count -gt 0 -and ($avail | Where-Object { $null -ne $_.AgeDays }).Count -gt 0) {
    $maxA = ($avail | Where-Object { $null -ne $_.AgeDays } | Measure-Object -Property AgeDays -Maximum).Maximum
    Write-Host "suggest_probe_skip_days=$([math]::Ceiling($maxA + 1)) (informational; app uses ReplayCloudRetention)"
}

exit 0
