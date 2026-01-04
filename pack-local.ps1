# pack-local.ps1 - Build and pack for local development

# Create local packages folder if it doesn't exist
New-Item -ItemType Directory -Path ./local-packages -Force | Out-Null

# Clear old packages
Remove-Item ./local-packages/*.nupkg -ErrorAction SilentlyContinue

# Clear NuGet cache for Cascade packages (prevents stale cache issues)
dotnet nuget locals all --list
Write-Host "Clearing cached Cascade packages..." -ForegroundColor Yellow
$cacheDir = (dotnet nuget locals global-packages --list) -replace "global-packages: ", ""
Remove-Item "$cacheDir/cascade.*" -Recurse -ErrorAction SilentlyContinue

# Pack with auto-generated version
Write-Host "Packing Cascade.Core..." -ForegroundColor Green
dotnet pack src/Cascade.Core -o ./local-packages

Write-Host "Packing Cascade.NServiceBus..." -ForegroundColor Green
dotnet pack src/Cascade.NServiceBus -o ./local-packages

Write-Host "Packing Cascade.NServiceBus.Framework..." -ForegroundColor Green
dotnet pack src/Cascade.NServiceBus.Framework -o ./local-packages

# Show what was created
Write-Host "`nCreated packages:" -ForegroundColor Green
Get-ChildItem ./local-packages/*.nupkg | ForEach-Object { Write-Host "  $_" -ForegroundColor Cyan }