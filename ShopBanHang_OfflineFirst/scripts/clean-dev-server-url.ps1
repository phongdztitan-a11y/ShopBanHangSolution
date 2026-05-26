# Xoa server.url khoi thu muc dev/bin (de app may A chi dung localhost tu appsettings.json)
$root = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $root "ShopBanHang_OfflineFirst"
$removed = 0

foreach ($path in @(
    (Join-Path $proj "server.url"),
    (Join-Path $proj "discovery.url")
)) {
    if (Test-Path $path) {
        Remove-Item $path -Force
        Write-Host "Removed: $path"
        $removed++
    }
}

Get-ChildItem -Path $proj -Recurse -Filter "server.url" -File -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\bin\\|\\obj\\' } |
    ForEach-Object {
        Remove-Item $_.FullName -Force
        Write-Host "Removed: $($_.FullName)"
        $removed++
    }

if ($removed -eq 0) { Write-Host "Khong co server.url / discovery.url can xoa." }
else { Write-Host "Xong. F5 tren may A se dung http://localhost:5191/api/" -ForegroundColor Green }
