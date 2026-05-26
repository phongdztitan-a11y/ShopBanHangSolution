# Reset local SQLite + SQL Server to empty fresh schema.
# Close app and API before running:
#   .\scripts\reset-all-databases.ps1

param(
    [string]$SqlServerInstance = "TRAN-PHONG\SQLEXPRESS",
    [string]$DatabaseName = "ShopBanHang_DoAn"
)

$ErrorActionPreference = "Stop"
$offlineFirstRoot = Split-Path $PSScriptRoot -Parent
$repoRoot = Split-Path $offlineFirstRoot -Parent
$serverProject = Join-Path $repoRoot "WebApplication3\WebApplication3\WebApplication3.csproj"

Write-Host "=== Reset ShopBanHang databases ===" -ForegroundColor Cyan

$shopDbCandidates = @(
    (Join-Path $offlineFirstRoot "ShopBanHang_OfflineFirst\shop.db"),
    (Join-Path $offlineFirstRoot "ShopBanHang_OfflineFirst\bin\Debug\net10.0-windows\shop.db"),
    (Join-Path $offlineFirstRoot "ShopBanHang_OfflineFirst\bin\Release\net10.0-windows\shop.db"),
    (Join-Path $offlineFirstRoot "ShopBanHang_OfflineFirst\bin\Release\net10.0-windows\win-x64\shop.db"),
    (Join-Path $offlineFirstRoot "publish\client\shop.db"),
    (Join-Path $offlineFirstRoot "dist\client-staging\shop.db")
)

$deleted = 0
foreach ($p in $shopDbCandidates) {
    if (Test-Path $p) {
        Remove-Item $p -Force
        Write-Host "  Deleted SQLite: $p" -ForegroundColor Yellow
        $deleted++
    }
}

Get-ChildItem -Path $offlineFirstRoot -Recurse -Filter "shop.db" -File -ErrorAction SilentlyContinue |
    ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Host "  Deleted SQLite: $($_.FullName)" -ForegroundColor Yellow
        $deleted++
    }

if ($deleted -eq 0) {
    Write-Host "  No shop.db found (created on next app run)." -ForegroundColor DarkGray
}

if (-not (Test-Path $serverProject)) {
    Write-Error "Server project not found: $serverProject"
}

Write-Host ""
Write-Host "Resetting SQL Server: $DatabaseName on $SqlServerInstance" -ForegroundColor Cyan

Push-Location (Split-Path $serverProject -Parent)
try {
    dotnet ef database drop --force --project $serverProject
} catch {
    Write-Host "  ef drop skipped or failed, trying sqlcmd..." -ForegroundColor DarkGray
}
Pop-Location

$sqlDrop = "IF DB_ID(N'$DatabaseName') IS NOT NULL BEGIN ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DatabaseName]; END"
$sqlcmdPath = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($sqlcmdPath) {
    & sqlcmd -S $SqlServerInstance -E -Q $sqlDrop
    Write-Host "  sqlcmd drop executed." -ForegroundColor Yellow
}

Write-Host "  Applying EF migrations..."
Push-Location (Split-Path $serverProject -Parent)
dotnet ef database update --project $serverProject
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Error "dotnet ef database update failed"
}
Pop-Location

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
Write-Host "  Server DB recreated (empty)."
Write-Host "  Client: delete shop.db on machine B install folder manually if needed."
Write-Host "  Local client seed: admin / 123 after first F5."
Write-Host "  Optional: .\build-setup.ps1 -Force"
