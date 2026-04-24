# Deckard — History

## Core Context
- **Project:** MemPalace.NET — .NET port of https://github.com/MemPalace/mempalace
- **User:** Bruno Capuano
- **Role:** Lead / Architect
- **Stack:** Latest .NET, Microsoft.Extensions.AI, Microsoft Agent Framework
- **Original concepts:** verbatim storage; wings (people/projects) → rooms (topics) → drawers (content); pluggable backend (default ChromaDB in Python — we'll default to SQLite + sqlite-vec or Microsoft.SemanticKernel.Connectors style); 96.6% R@5 on LongMemEval (raw, no LLM); knowledge graph with temporal validity windows; MCP server with 29 tools; agent diaries.

## Learnings

### 2025-04-24: Phase 0 — Solution Scaffold + CI

**Target Framework:** net10.0 (.NET 10.0.300-preview.0.26177.108 installed)

**Project Graph:**
```
MemPalace.Core (no dependencies)
├── MemPalace.Backends.Sqlite → Core
├── MemPalace.Ai → Core
├── MemPalace.KnowledgeGraph → Core
├── MemPalace.Mcp → Core, Ai
├── MemPalace.Agents → Core, Ai
├── MemPalace.Cli → Core, Backends.Sqlite, Ai, Agents
└── MemPalace.Tests → Core, Backends.Sqlite
```

**Gotchas:**
- .NET 10 SDK creates `.slnx` (new XML format) by default instead of classic `.sln`
- Directory.Build.props conditional on `OutputType != 'Exe'` and `IsPackable != 'false'` correctly applies `TreatWarningsAsErrors` to library projects only
- CI workflow requires `dotnet-quality: 'preview'` for .NET 10 preview builds

**Status:** ✅ Committed (2b8d2fc), pushed to main. CI will validate on next run.
