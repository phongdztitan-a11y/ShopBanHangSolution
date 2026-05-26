# Seed PI 4.2 test data (>=500 SP, >=500 invoices pending sync). DEV/TEST ONLY.
# Does NOT affect build-setup.ps1 or setup.exe.
#
# Close POS app first, then:
#   cd c:\Users\phong\Desktop\doan\app\ShopBanHang_OfflineFirst
#   .\scripts\seed-pi42-test-data.ps1 -MaChiNhanh CN_GOC -SoSanPham 500 -SoHoaDon 500
#
# Optional reset first:
#   .\scripts\reset-all-databases.ps1

param(
    [string]$MaChiNhanh = "CN_GOC",
    [int]$SoSanPham = 500,
    [int]$SoHoaDon = 500,
    [string]$DbDirectory = "",
    [int]$BatchSize = 100,
    [switch]$SkipIfEnough,
    [switch]$WhatIf,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$offlineFirstRoot = Split-Path $PSScriptRoot -Parent
$seedProject = Join-Path $offlineFirstRoot "tools\SeedPi42TestData\SeedPi42TestData.csproj"

if (-not (Test-Path $seedProject)) {
    Write-Error "Not found: $seedProject"
}

if ([string]::IsNullOrWhiteSpace($DbDirectory)) {
    $DbDirectory = Join-Path $offlineFirstRoot "ShopBanHang_OfflineFirst\bin\Debug\net10.0-windows"
    if (-not (Test-Path $DbDirectory)) {
        $DbDirectory = Join-Path $offlineFirstRoot "ShopBanHang_OfflineFirst"
    }
}

$proc = Get-Process -Name "ShopBanHang_OfflineFirst" -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "ERROR: ShopBanHang_OfflineFirst is running (PID $($proc.Id -join ','))" -ForegroundColor Red
    Write-Host "Close the POS app first to avoid shop.db lock." -ForegroundColor Red
    exit 2
}

Write-Host "=== Seed PI 4.2 (manual) ===" -ForegroundColor Cyan
Write-Host "  MaChiNhanh : $MaChiNhanh"
Write-Host "  SoSanPham  : $SoSanPham"
Write-Host "  SoHoaDon   : $SoHoaDon"
Write-Host "  DbDirectory: $DbDirectory"
Write-Host ""
Write-Host "Safe:" -ForegroundColor DarkGray
Write-Host "  - Does not change App.xaml.cs / build-setup / installer"
Write-Host "  - Do not package seeded shop.db into client.zip"
Write-Host "  - Sync may push data to SQL Server if online"
Write-Host ""

if (-not $Force -and -not $WhatIf) {
    $r = Read-Host "Write to shop.db? (y/N)"
    if ($r -notmatch '^[yY]') {
        Write-Host "Cancelled."
        exit 0
    }
}

$dotnetArgs = @(
    "run", "--project", $seedProject, "--",
    "--ma-chi-nhanh", $MaChiNhanh,
    "--so-san-pham", $SoSanPham.ToString(),
    "--so-hoa-don", $SoHoaDon.ToString(),
    "--db-dir", $DbDirectory,
    "--batch-size", $BatchSize.ToString()
)

if ($SkipIfEnough) { $dotnetArgs += "--skip-if-enough" }
if ($WhatIf) { $dotnetArgs += "--whatif" }

$cmdLine = "dotnet " + ($dotnetArgs -join " ")
Write-Host $cmdLine -ForegroundColor DarkGray
& dotnet @dotnetArgs
exit $LASTEXITCODE
