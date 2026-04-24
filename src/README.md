# Source

All source code lives under this folder. Project structure:

- `MemPalace.Core/` — domain + storage contracts
- `MemPalace.Backends.Sqlite/` — default SQLite + sqlite-vec backend
- `MemPalace.Ai/` — Microsoft.Extensions.AI integration
- `MemPalace.KnowledgeGraph/` — temporal entity-relationship graph
- `MemPalace.Mcp/` — Model Context Protocol server
- `MemPalace.Agents/` — Microsoft Agent Framework integration
- `MemPalace.Cli/` — `mempalace` command-line interface
- `MemPalace.Tests/` — xUnit test suite

See [`docs/PLAN.md`](../docs/PLAN.md) for the implementation roadmap.
