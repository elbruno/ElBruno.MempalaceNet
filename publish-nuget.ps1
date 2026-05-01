# Publish MemPalace.NET v0.13.0 to NuGet.org
# Run: ./publish-nuget.ps1

$packages = @(
    "src/MemPalace.Core/bin/Release/MemPalace.Core.0.13.0.nupkg",
    "src/MemPalace.Diagnostics/bin/Release/MemPalace.Diagnostics.0.13.0.nupkg",
    "src/MemPalace.Backends.Sqlite/bin/Release/MemPalace.Backends.Sqlite.0.13.0.nupkg",
    "src/MemPalace.Ai/bin/Release/MemPalace.Ai.0.13.0.nupkg",
    "src/MemPalace.Search/bin/Release/MemPalace.Search.0.13.0.nupkg",
    "src/MemPalace.Mining/bin/Release/MemPalace.Mining.0.13.0.nupkg",
    "src/MemPalace.KnowledgeGraph/bin/Release/MemPalace.KnowledgeGraph.0.13.0.nupkg",
    "src/MemPalace.Agents/bin/Release/MemPalace.Agents.0.13.0.nupkg",
    "src/MemPalace.Mcp/bin/Release/MemPalace.Mcp.0.13.0.nupkg",
    "src/MemPalace.Cli/bin/Release/mempalacenet.0.13.0.nupkg"
)

Write-Host "📦 Publishing MemPalace.NET v0.13.0 to NuGet.org" -ForegroundColor Cyan
Write-Host "Packages to publish:" -ForegroundColor Green
$packages | ForEach-Object { Write-Host "  ✓ $_" }

$apiKey = $env:NUGET_API_KEY
if (-not $apiKey) {
    Write-Host "`n⚠️  NUGET_API_KEY environment variable not set!" -ForegroundColor Yellow
    Write-Host "Please set the API key before running this script:" -ForegroundColor Yellow
    Write-Host '  $env:NUGET_API_KEY = "your-api-key-here"' -ForegroundColor Gray
    Write-Host "`nThen run: .\publish-nuget.ps1" -ForegroundColor Gray
    exit 1
}

Write-Host "`n🚀 Publishing packages..." -ForegroundColor Cyan
$failed = 0

foreach ($package in $packages) {
    if (Test-Path $package) {
        Write-Host "Publishing $package..." -ForegroundColor Yellow
        dotnet nuget push $package --api-key $apiKey --source https://api.nuget.org/v3/index.json --skip-duplicate
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Published" -ForegroundColor Green
        } else {
            Write-Host "  ✗ Failed" -ForegroundColor Red
            $failed++
        }
    } else {
        Write-Host "  ✗ Package not found: $package" -ForegroundColor Red
        $failed++
    }
}

if ($failed -eq 0) {
    Write-Host "`n✅ All packages published successfully!" -ForegroundColor Green
    Write-Host "`nVerify on NuGet.org:" -ForegroundColor Cyan
    Write-Host "  https://www.nuget.org/packages/MemPalace.Core" -ForegroundColor Gray
} else {
    Write-Host "`n❌ $failed package(s) failed to publish" -ForegroundColor Red
    exit 1
}
