# v0.7.0: Production-Grade Search Foundation Ready

## What's New

### 🚀 P0 Blockers Complete
- **#3 - CLI DI Fix**: EmptyAgentRegistry fallback for graceful CLI handling
- **#2 - Wake-Up Summarization**: LLM-based summary layer with text fallback
- **#4 - Ollama Support**: Restored Microsoft.Extensions.AI.Ollama stable integration

### 🛡️ P2 MCP Security & Features (Phase 2)
- **#21 - MCP Security**: Comprehensive validation layer with audit logging & confirmation prompts
- **#18 - Write Operations Testing**: Complete test suite for all 8 write tools
- **#14 - MCP CLI SSE**: Server-Sent Events transport integration (--transport sse flag)
- **#6 - Tool Expansion**: 15-tool catalog with 8 new write operations (palace_store, palace_batch_store, palace_delete, knowledge graph mutations)
- **#12 - Skill CLI Integration**: SkillInvoker middleware for MCP tool calls

### ⚛️ CLI UX Enhancements
- **#16 - Error Messages**: Contextual remediation suggestions with ErrorFormatter
- **#20 - Progress Bars**: Spectre.Console progress tracking for long operations
- **#7 - CLI Polish**: Consistent formatting, improved help text, table layouts
- **#15 - Tool Catalog Docs**: Auto-generated MCP tool reference (docs/mcp-tools-catalog.md)

### 🧪 Test Infrastructure & CI/CD
- **#8 - R@5 Regression Tests**: LongMemEval validation in CI (96%+ maintained)
- **#19 - Integration Workflows**: Full xplat coverage reporting (85% threshold)
- **#17 - E2E Scenarios**: 5 comprehensive end-to-end test cases
- **#10 - MCP Integration Tests**: 6 agent integration scenarios + performance benchmarks
- **#13 - Backend Optimization**: SQLite indexes + cursor pagination for WakeUpAsync

### 📚 Documentation Updates
- **#9 - Skill Patterns**: Updated v0.7.0 patterns with wake-up & write operations
- **docs/mcp-security.md**: Security validation patterns & audit logging
- **docs/troubleshooting.md**: Comprehensive troubleshooting guide (346 lines)

## Release Metrics

- ✅ **20 GitHub issues resolved** (P0/P1/P2)
- ✅ **5,847+ lines of code** added
- ✅ **43 files** modified/created
- ✅ **185+ tests** (unit + integration + E2E)
- ✅ **CI/CD workflows** configured & passing
- ✅ **LongMemEval R@5 ≥ 96%** validated

## Installation

```bash
# NuGet (stable)
dotnet add package ElBruno.MempalaceNet

# CLI tool
dotnet tool install -g mempalacenet
```

## Getting Started

See [README](https://github.com/elbruno/ElBruno.MempalaceNet#readme) and [docs/SKILL_PATTERNS.md](docs/SKILL_PATTERNS.md) for quick start and patterns.

## Contributors

- 🔧 **Tyrell**: Core engine (P0 #3/#2, optimization #13)
- 🤖 **Roy**: AI integration (P0 #4, P2 MCP #6/#14/#18/#21)
- ⚛️ **Rachael**: CLI UX (P1/P2 #7/#15/#16/#20, docs #9)
- 🧪 **Bryant**: Test infrastructure (P1/P2 #8/#10/#17/#19)

---

**v0.7.0 is production-ready for Phase 2 deployment. Next: v0.8.0 with sqlite-vec & BM25 search optimization.**

**Copilot Skill Publication (Weeks 10+):**
- Submit to GitHub Copilot Skill registry
- Announce across LinkedIn, Twitter, blog

**v1.0 Roadmap:**
- Remove preview suffix (stable API)
- Full marketplace listing
- Multi-framework support

## Credits

- MemPalace.NET Team: Deckard, Tyrell, Roy, Rachael, Bryant
- Original MemPalace: https://github.com/MemPalace/mempalace
- Sponsors: ElBruno

---

*Released on 2025-04-25 | [Full Changelog](https://github.com/elbruno/ElBruno.MempalaceNet/releases)*
