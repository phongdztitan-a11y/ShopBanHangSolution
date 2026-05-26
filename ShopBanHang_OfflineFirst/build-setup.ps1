# Build setup.exe + client.zip cho MAY B (ngrok). KHONG doi appsettings dev tren may A.
#
#   .\build-setup.ps1 -Force
#   .\scripts\update-ngrok-url.ps1 -ForInstaller   # sau khi ngrok chay
#   .\build-setup.ps1 -Force                       # doc installer\remote-server.url
#
#   .\build-setup.ps1 -Force -NgrokApiUrl "https://xxx.ngrok-free.dev/api/"

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$NgrokApiUrl = "",
    [string]$DiscoveryUrl = "",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$clientProject = Join-Path $root "ShopBanHang_OfflineFirst\ShopBanHang_OfflineFirst.csproj"
$setupProject = Join-Path $root "installer\setup.csproj"
$installerUrlFile = Join-Path $root "installer\remote-server.url"
$dist = Join-Path $root "dist"
$clientOut = Join-Path $dist "client-staging"
$clientZip = Join-Path $dist "client.zip"
$setupOut = Join-Path $dist "setup-out"

function Normalize-ApiUrl([string]$url) {
    $url = $url.Trim()
    if (-not $url.EndsWith('/')) { $url += '/' }
    if ($url -notmatch '/api/') {
        if ($url -match '/api$') { $url += '/' }
        else { $url = $url.TrimEnd('/') + '/api/' }
    }
    return $url
}

function Get-NgrokPublicUrl {
    $resp = Invoke-RestMethod -Uri "http://127.0.0.1:4040/api/tunnels" -Method Get
    $tunnel = $resp.tunnels | Where-Object { $_.proto -eq "https" } | Select-Object -First 1
    if (-not $tunnel) { $tunnel = $resp.tunnels | Select-Object -First 1 }
    if (-not $tunnel) { throw "Ngrok chua chay. Hay: ngrok http 5191" }
    return Normalize-ApiUrl ($tunnel.public_url.TrimEnd('/') + "/")
}

function Read-FirstLineUrl([string]$path) {
    if (-not (Test-Path $path)) { return "" }
    foreach ($line in Get-Content $path) {
        $t = $line.Trim()
        if ($t.Length -gt 0 -and -not $t.StartsWith('#')) { return $t }
    }
    return ""
}

if (-not (Test-Path $clientProject)) { Write-Error "Client project not found: $clientProject" }

if (Test-Path $dist) {
    if ($Force) { Remove-Item -Recurse -Force $dist }
    else { Write-Host "dist exists. Use -Force to rebuild."; exit 1 }
}
New-Item -ItemType Directory -Path $dist | Out-Null

Write-Host "[1/3] Publishing client (self-contained)..."
dotnet publish $clientProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $clientOut
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Xoa file URL lech tu dev (neu co)
Remove-Item (Join-Path $clientOut "server.url") -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $clientOut "discovery.url") -Force -ErrorAction SilentlyContinue

# URL cho may B: tham so > installer\remote-server.url > ngrok dang chay
$remoteUrl = $NgrokApiUrl.Trim()
if (-not $remoteUrl) { $remoteUrl = Read-FirstLineUrl $installerUrlFile }
if (-not $remoteUrl) {
    try { $remoteUrl = Get-NgrokPublicUrl; Write-Host "   Lay URL tu ngrok dang chay." }
    catch {
        Write-Host "   Khong co installer\remote-server.url va ngrok chua chay." -ForegroundColor Yellow
        $remoteUrl = Read-Host "Nhap URL ngrok cho may B (https://....ngrok-free.dev/api/)"
    }
}
$remoteUrl = Normalize-ApiUrl $remoteUrl
Set-Content -Path (Join-Path $clientOut "server.url") -Value $remoteUrl -Encoding UTF8
Write-Host "   May B server.url -> $remoteUrl" -ForegroundColor Green

if ($DiscoveryUrl) {
    Set-Content -Path (Join-Path $clientOut "discovery.url") -Value $DiscoveryUrl.Trim() -Encoding UTF8
    Write-Host "   discovery.url"
}

# appsettings trong goi cai van la localhost (fallback); server.url (ngrok) uu tien hon
Write-Host "   appsettings trong goi cai = localhost (chi dung neu xoa server.url tren may B)"

Write-Host "[2/3] Creating client.zip..."
if (Test-Path $clientZip) { Remove-Item $clientZip -Force }
Compress-Archive -Path (Join-Path $clientOut "*") -DestinationPath $clientZip -CompressionLevel Optimal

Write-Host "[3/3] Building setup.exe..."
dotnet publish $setupProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $setupOut
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item $clientZip (Join-Path $setupOut "client.zip") -Force
Copy-Item (Join-Path $setupOut "setup.exe") (Join-Path $dist "setup.exe") -Force

$portableDir = Join-Path $dist "ShopBanHang-Installer"
New-Item -ItemType Directory -Path $portableDir -Force | Out-Null
Copy-Item (Join-Path $dist "setup.exe") $portableDir -Force
Copy-Item $clientZip $portableDir -Force
$guide = Join-Path $root "scripts\HUONG-DAN-CAI-DAT.txt"
if (Test-Path $guide) { Copy-Item $guide $portableDir -Force }

Remove-Item -Recurse -Force $clientOut -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  Portable -> $portableDir"
Write-Host "May A dev: http://localhost:5191/api/ (khong dung file trong dist)"
