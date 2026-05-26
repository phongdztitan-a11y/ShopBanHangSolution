# Cap nhat URL ngrok cho GOI CAI (may B), KHONG ghi de cau hinh dev localhost tren may A.
#
# Sau khi chay ngrok http 5191:
#   .\scripts\update-ngrok-url.ps1 -ForInstaller
#   .\build-setup.ps1 -Force
#
# Discovery (tuy chon):
#   .\scripts\update-ngrok-url.ps1 -ForInstaller -DiscoveryFile ".\discovery\endpoint.json"

param(
    [int]$NgrokApiPort = 4040,
    [switch]$ForInstaller,
    [string]$DiscoveryFile = "",
    [string]$UploadCommand = ""
)

$ErrorActionPreference = "Stop"
$offlineFirstRoot = Split-Path $PSScriptRoot -Parent
$installerUrlFile = Join-Path $offlineFirstRoot "installer\remote-server.url"

function Get-NgrokPublicUrl {
    $uri = "http://127.0.0.1:$NgrokApiPort/api/tunnels"
    $resp = Invoke-RestMethod -Uri $uri -Method Get
    $tunnel = $resp.tunnels | Where-Object { $_.proto -eq "https" } | Select-Object -First 1
    if (-not $tunnel) { $tunnel = $resp.tunnels | Select-Object -First 1 }
    if (-not $tunnel) { throw "Khong thay tunnel ngrok. Hay chay: ngrok http 5191" }
    return ($tunnel.public_url.TrimEnd('/') + "/api/")
}

function Normalize-ApiUrl([string]$url) {
    $url = $url.Trim()
    if (-not $url.EndsWith('/')) { $url += '/' }
    if ($url -notmatch '/api/') {
        if ($url -match '/api$') { $url += '/' }
        else { $url = $url.TrimEnd('/') + '/api/' }
    }
    return $url
}

if (-not $ForInstaller) {
    Write-Host "Mac dinh chi cap nhat URL cho goi cai (installer\remote-server.url)." -ForegroundColor Yellow
    Write-Host "Them -ForInstaller de xac nhan (hoac script se hoi)." -ForegroundColor Yellow
    $confirm = Read-Host "Cap nhat cho goi cai may B? (y/N)"
    if ($confirm -notmatch '^y') { exit 0 }
}

$apiUrl = Normalize-ApiUrl (Get-NgrokPublicUrl)
Write-Host "Ngrok API URL: $apiUrl" -ForegroundColor Cyan

$installerDir = Split-Path $installerUrlFile -Parent
if (-not (Test-Path $installerDir)) { New-Item -ItemType Directory -Path $installerDir -Force | Out-Null }
Set-Content -Path $installerUrlFile -Value $apiUrl -Encoding UTF8
Write-Host "  installer\remote-server.url (dung khi build-setup.ps1)" -ForegroundColor Green

$discoveryPayload = @{ apiBaseUrl = $apiUrl } | ConvertTo-Json -Compress
if ($DiscoveryFile) {
    $dir = Split-Path $DiscoveryFile -Parent
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    Set-Content -Path $DiscoveryFile -Value $discoveryPayload -Encoding UTF8
    Write-Host "  discovery JSON -> $DiscoveryFile"
}

if ($UploadCommand) {
    Write-Host "  Upload discovery..."
    Invoke-Expression $UploadCommand
}

Write-Host ""
Write-Host "May A (dev): giu appsettings.json = http://localhost:5191/api/" -ForegroundColor Cyan
Write-Host "May B: chay build-setup.ps1 -Force roi mang ShopBanHang-Installer di cai." -ForegroundColor Cyan
