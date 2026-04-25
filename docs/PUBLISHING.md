# NuGet Publishing Guide

This guide walks through publishing all MemPalace.NET packages to NuGet.org.

## 📦 Packages Overview

The solution contains **9 publishable packages** (3 projects are excluded: Tests, Benchmarks as CLI tools are handled separately):

| Package | Description | Type |
|---------|-------------|------|
| `MemPalace.Core` | Core domain types and storage interfaces | Library |
| `MemPalace.Backends.Sqlite` | SQLite backend with vector storage | Library |
| `MemPalace.Ai` | Microsoft.Extensions.AI integration with embeddings | Library |
| `MemPalace.Search` | Semantic and hybrid search engine | Library |
| `MemPalace.KnowledgeGraph` | Temporal knowledge graph with entity-relationship triples | Library |
| `MemPalace.Mining` | Content ingestion pipeline | Library |
| `MemPalace.Mcp` | Model Context Protocol (MCP) server | Library |
| `MemPalace.Agents` | Microsoft Agent Framework integration | Library |
| `mempalacenet` | Command-line tool (CLI) | .NET Tool |
| `mempalacenet-bench` | Benchmark tool for search quality testing | .NET Tool |

**Note:** `MemPalace.Tests` is marked `IsPackable=false` and won't be published.

## ✅ Prerequisites

### 1. NuGet.org Account
- Create account at https://www.nuget.org/users/account/LogOn
- Verify your email address

### 2. API Key Setup
1. Go to https://www.nuget.org/account/apikeys
2. Click "Create" to generate a new API key
3. Configure:
   - **Key Name:** `MemPalaceNet Publishing` (or your preferred name)
   - **Glob Pattern:** `MemPalace.*` (to restrict to your packages)
   - **Expiration:** Choose appropriate duration
   - **Scopes:** Select "Push new packages and package versions"
4. Copy the generated API key (you won't see it again!)
5. Store securely in your environment:

```powershell
# PowerShell (Windows)
$env:NUGET_API_KEY = "your-api-key-here"

# Or persist it
[System.Environment]::SetEnvironmentVariable('NUGET_API_KEY', 'your-api-key-here', 'User')
```

```bash
# Bash (Linux/Mac)
export NUGET_API_KEY="your-api-key-here"

# Add to ~/.bashrc or ~/.zshrc for persistence
echo 'export NUGET_API_KEY="your-api-key-here"' >> ~/.bashrc
```

## 🔧 Package Metadata

All packages share common metadata defined in `src/Directory.Build.props`:

```xml
<Authors>Bruno Capuano</Authors>
<Company>Bruno Capuano</Company>
<Copyright>Copyright (c) Bruno Capuano. Licensed under the MIT License.</Copyright>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
<PackageProjectUrl>https://github.com/elbruno/mempalacenet</PackageProjectUrl>
<RepositoryUrl>https://github.com/elbruno/mempalacenet</RepositoryUrl>
<RepositoryType>git</RepositoryType>
<Version>0.1.0-preview.1</Version>
<PackageTags>ai;agents;memory;rag;mcp;dotnet;embeddings;palace</PackageTags>
```

Each `.csproj` file includes:
- `PackageId` - Unique package identifier
- `Description` - Package-specific description
- `PackageReadmeFile` - Links to main README.md

## 📋 Publishing Process

### Step 1: Pre-flight Checks

Before publishing, ensure:

```powershell
# 1. Clean build
dotnet clean
dotnet build --configuration Release

# 2. Run all tests
dotnet test --configuration Release

# 3. Verify version in Directory.Build.props
# Current: 0.1.0-preview.1
```

### Step 2: Pack All Packages

Create NuGet packages for all publishable projects:

```powershell
# Navigate to repository root
cd C:\src\elbruno.mempalacenet

# Pack all packages (creates .nupkg files in bin\Release)
dotnet pack --configuration Release --output ./artifacts/packages

# Verify packages created
dir artifacts\packages\*.nupkg
```

Expected output:
```
MemPalace.Core.0.1.0-preview.1.nupkg
MemPalace.Backends.Sqlite.0.1.0-preview.1.nupkg
MemPalace.Ai.0.1.0-preview.1.nupkg
MemPalace.Search.0.1.0-preview.1.nupkg
MemPalace.KnowledgeGraph.0.1.0-preview.1.nupkg
MemPalace.Mining.0.1.0-preview.1.nupkg
MemPalace.Mcp.0.1.0-preview.1.nupkg
MemPalace.Agents.0.1.0-preview.1.nupkg
mempalacenet.0.1.0-preview.1.nupkg
mempalacenet-bench.0.1.0-preview.1.nupkg
```

### Step 3: Dry Run (Optional but Recommended)

Test package validity without publishing:

```powershell
# Install package validator tool
dotnet tool install -g dotnet-validate

# Validate packages
dotnet validate package local .\artifacts\packages\*.nupkg
```

### Step 4: Publish Packages

#### Option A: Publish All at Once

```powershell
# Set API key if not already in environment
$env:NUGET_API_KEY = "your-api-key-here"

# Push all packages
Get-ChildItem .\artifacts\packages\*.nupkg | ForEach-Object {
    dotnet nuget push $_.FullName --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
}
```

#### Option B: Publish Individually (Recommended for First Release)

This allows you to control order and catch any issues early:

```powershell
# 1. Core package (no dependencies)
dotnet nuget push .\artifacts\packages\MemPalace.Core.0.1.0-preview.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# 2. Backends (depends on Core)
dotnet nuget push .\artifacts\packages\MemPalace.Backends.Sqlite.0.1.0-preview.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# 3. AI (depends on Core)
dotnet nuget push .\artifacts\packages\MemPalace.Ai.0.1.0-preview.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# 4. Search (depends on Core, Ai)
dotnet nuget push .\artifacts\packages\MemPalace.Search.0.1.0-preview.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# 5. KnowledgeGraph (depends on Core)
dotnet nuget push .\artifacts\packages\MemPalace.KnowledgeGraph.0.1.0-preview.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# 6. Mining (depends on Core, Ai)
dotnet nuget push .\artifacts\packages\MemPalace.Mining.0.1.0-preview.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# 7. Mcp (depends on Core, Ai, Search, KnowledgeGraph)
dotnet nuget push .\artifacts\packages\MemPalace.Mcp.0.1.0-preview.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# 8. Agents (depends on Core, Ai, Search, KnowledgeGraph, Mcp)
dotnet nuget push .\artifacts\packages\MemPalace.Agents.0.1.0-preview.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# 9. CLI Tool (depends on all libraries)
dotnet nuget push .\artifacts\packages\mempalacenet.0.1.0-preview.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

# 10. Benchmark Tool
dotnet nuget push .\artifacts\packages\mempalacenet-bench.0.1.0-preview.1.nupkg --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
```

### Step 5: Verify Publication

After publishing, verify packages appear on NuGet.org:

1. **Check Package Status:**
   - Go to https://www.nuget.org/packages/manage/upload
   - View "My Packages" tab
   - Each package should show as "Published" or "Validating"

2. **Direct Package URLs:**
   - https://www.nuget.org/packages/MemPalace.Core
   - https://www.nuget.org/packages/MemPalace.Backends.Sqlite
   - https://www.nuget.org/packages/MemPalace.Ai
   - https://www.nuget.org/packages/MemPalace.Search
   - https://www.nuget.org/packages/MemPalace.KnowledgeGraph
   - https://www.nuget.org/packages/MemPalace.Mining
   - https://www.nuget.org/packages/MemPalace.Mcp
   - https://www.nuget.org/packages/MemPalace.Agents
   - https://www.nuget.org/packages/mempalacenet
   - https://www.nuget.org/packages/mempalacenet-bench

3. **Test Installation:**
   ```powershell
   # Test library package
   dotnet add package MemPalace.Core --version 0.1.0-preview.1
   
   # Test CLI tool
   dotnet tool install --global mempalacenet --version 0.1.0-preview.1
   ```

**Note:** Package indexing can take 10-15 minutes. The package will be visible immediately on your account but may take time to appear in search results.

## 🔄 Version Updates

To publish a new version:

1. **Update Version:**
   ```xml
   <!-- src/Directory.Build.props -->
   <Version>0.1.0-preview.2</Version>
   ```

2. **Update CHANGELOG.md** (document changes)

3. **Follow Publishing Process** (Steps 1-5 above)

4. **Tag Release:**
   ```powershell
   git tag -a v0.1.0-preview.2 -m "Release v0.1.0-preview.2"
   git push origin v0.1.0-preview.2
   ```

## 🚨 Troubleshooting

### Error: "Package already exists"
- Cannot overwrite existing versions
- Increment version in `Directory.Build.props`
- Re-pack and publish

### Error: "401 Unauthorized"
- Check API key is valid: https://www.nuget.org/account/apikeys
- Verify key has "Push" permissions
- Ensure key glob pattern matches package IDs

### Error: "Package validation failed"
- Run `dotnet validate package local` for details
- Common issues:
  - Missing or invalid license
  - Missing README.md
  - Invalid package metadata

### Error: "Assembly outside lib folder"
- .NET tools must set `<PackAsTool>true</PackAsTool>`
- Already correctly configured for `mempalacenet` and `mempalacenet-bench`

### Package Not Appearing in Search
- Indexing takes 10-15 minutes after upload
- Check directly via package URL
- Verify package status in "My Packages"

### Dependencies Not Resolving
- Ensure dependency packages are published first
- Core libraries should be published before dependent packages
- Check package versions match

## 📚 Additional Resources

- [NuGet Package Documentation](https://learn.microsoft.com/en-us/nuget/)
- [Creating NuGet Packages](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package-dotnet-cli)
- [NuGet Package Metadata](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices)
- [Publishing to NuGet.org](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package)

## 🎯 Quick Reference

```powershell
# Complete workflow
cd C:\src\elbruno.mempalacenet
dotnet clean
dotnet build --configuration Release
dotnet test --configuration Release
dotnet pack --configuration Release --output ./artifacts/packages
Get-ChildItem .\artifacts\packages\*.nupkg | ForEach-Object {
    dotnet nuget push $_.FullName --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json
}
```
