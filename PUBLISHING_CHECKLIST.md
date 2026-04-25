# NuGet Publishing Checklist

Quick reference for publishing MemPalace.NET packages to NuGet.org.

## 🚀 Automated Publishing (Recommended)

The repository has a **GitHub Actions workflow** (`.github/workflows/publish.yml`) for automated publishing.

### Option 1: Publish on Release
1. Create a GitHub Release with a tag (e.g., `v0.5.0`)
2. Workflow triggers automatically
3. Packages are built, tested, and published to NuGet.org

### Option 2: Manual Dispatch
1. Go to Actions → Publish to NuGet → Run workflow
2. Optionally specify version (or leave empty to use Directory.Build.props)
3. Workflow builds, tests, and publishes

**Prerequisites:**
- Repository secret `NUGET_USER` must be set in Settings → Secrets → Actions
- NuGet OIDC authentication configured (NuGet/login@v1 action)

## 📋 Pre-Flight Checks

- [ ] All tests passing: `dotnet test --configuration Release`
- [ ] Clean build successful: `dotnet build --configuration Release`
- [ ] Version bumped in `src/Directory.Build.props`
- [ ] CHANGELOG.md updated with release notes
- [ ] README.md reflects current functionality
- [ ] All commits pushed to main branch
- [ ] API key available: `$env:NUGET_API_KEY` set

## 🔨 Build & Pack

```powershell
# Clean slate
dotnet clean

# Release build
dotnet build --configuration Release

# Create packages
dotnet pack --configuration Release --output ./artifacts/packages

# Verify packages created (should see 10 .nupkg files)
dir artifacts\packages\*.nupkg
```

## ✅ Validation (Optional but Recommended)

```powershell
# Install validator if needed
dotnet tool install -g dotnet-validate

# Validate all packages
dotnet validate package local .\artifacts\packages\*.nupkg
```

## 🚀 Publish to NuGet.org

### Recommended: Individual Publishing (maintains dependency order)

```powershell
cd artifacts\packages

# Core (no dependencies)
dotnet nuget push MemPalace.Core.*.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# Backends & AI (depend on Core)
dotnet nuget push MemPalace.Backends.Sqlite.*.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push MemPalace.Ai.*.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# Search & KnowledgeGraph (depend on Core, Ai)
dotnet nuget push MemPalace.Search.*.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push MemPalace.KnowledgeGraph.*.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# Mining (depends on Core, Ai)
dotnet nuget push MemPalace.Mining.*.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# Mcp (depends on Core, Ai, Search, KnowledgeGraph)
dotnet nuget push MemPalace.Mcp.*.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# Agents (depends on Core, Ai, Search, KnowledgeGraph, Mcp)
dotnet nuget push MemPalace.Agents.*.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# CLI tools (depend on all libraries)
dotnet nuget push mempalacenet.*.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push mempalacenet-bench.*.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

### Alternative: Batch Publishing

```powershell
# Publish all at once (use with caution for first release)
Get-ChildItem .\artifacts\packages\*.nupkg | ForEach-Object {
    dotnet nuget push $_.FullName --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
}
```

## 🔍 Post-Publish Verification

- [ ] Check "My Packages" at https://www.nuget.org/packages/manage/upload
- [ ] Verify each package shows "Published" status
- [ ] Wait 10-15 minutes for indexing
- [ ] Test installation:
  ```powershell
  dotnet tool install --global mempalacenet --version 0.1.0-preview.1
  dotnet add package MemPalace.Core --version 0.1.0-preview.1
  ```
- [ ] Verify package pages are live:
  - https://www.nuget.org/packages/MemPalace.Core
  - https://www.nuget.org/packages/mempalacenet

## 🏷️ Git Tagging

```powershell
# Tag the release
git tag -a v0.1.0-preview.1 -m "Release v0.1.0-preview.1"
git push origin v0.1.0-preview.1

# Create GitHub release (optional)
gh release create v0.1.0-preview.1 --title "v0.1.0-preview.1" --notes-file CHANGELOG.md
```

## ⚠️ Common Issues

| Issue | Solution |
|-------|----------|
| "Package already exists" | Bump version in `Directory.Build.props` |
| "401 Unauthorized" | Check API key: https://www.nuget.org/account/apikeys |
| Package not in search | Wait 10-15 minutes for indexing |
| Dependency resolution fails | Publish Core packages first |

## 📝 Current Package Versions

| Package | Current Version |
|---------|----------------|
| All packages | 0.1.0-preview.1 |

*Update this table after each release*

## 📚 Full Documentation

For detailed instructions, see [docs/PUBLISHING.md](docs/PUBLISHING.md)
