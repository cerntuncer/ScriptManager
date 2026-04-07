# API (5281) ve MVC (5270) — iki ayrı pencerede; Visual Studio dışında hızlı başlatma için.
$root = $PSScriptRoot
$api = Join-Path $root "API"
$mvc = Join-Path $root "ScriptManager"

Start-Process pwsh -ArgumentList @(
    "-NoExit", "-Command",
    "Set-Location -LiteralPath '$api'; Write-Host 'API http://localhost:5281'; dotnet run"
)
Start-Sleep -Seconds 2
Start-Process pwsh -ArgumentList @(
    "-NoExit", "-Command",
    "Set-Location -LiteralPath '$mvc'; Write-Host 'MVC http://localhost:5270'; dotnet run"
)
Start-Sleep -Seconds 3
Start-Process "http://localhost:5281/swagger"
Start-Process "http://localhost:5270"
