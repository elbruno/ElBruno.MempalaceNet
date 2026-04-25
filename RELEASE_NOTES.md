# v0.6.0-preview.1: Copilot Skill Infrastructure Ready

## What's New

### 🎯 GitHub Copilot Skill Infrastructure
- **Manifest**: `.github/copilot-skill.yaml` with 6 semantic memory capabilities
- **Teaching Patterns**: 5 production-ready C# patterns (RAG, agent memory, KG, local-first, hybrid search)
- **Integration Guide**: Step-by-step setup for GitHub Copilot + MCP server
- **Code Generation Hints**: Copilot instructions for AI-assisted code generation

### 🔬 v0.6.0 Foundation Research Complete
- **sqlite-vec Integration**: Approved for implementation (10-25x speedup at 100K vectors)
- **BM25 Keyword Search**: Custom lightweight implementation planned (200 LOC)
- **LongMemEval Validation**: Framework ready (91% R@5 target, 1.5 hrs to validate)
- **Timeline**: 9-12 weeks for v0.6.0 production release

### 📝 Documentation Improvements
- **Updated README**: Correct version badge, About Author section, samples link
- **Copilot Skill Docs**: 5 teaching patterns + integration checklist
- **Benchmarking Docs**: LongMemEval validation approach documented

## Release Notes

- ✅ All 152 tests passing
- ✅ 10 NuGet packages published
- ✅ Copilot Skill ready for team review & publication
- ✅ v0.6.0 research foundation locked in

## Known Limitations

- **Preview Status**: Microsoft.Extensions.AI.Ollama dependency still in preview
- **sqlite-vec**: Spike PR pending; production integration in v0.6.0
- **BM25**: Implementation approved; ships with v0.6.0-preview.1 or later
- **Wake-up Feature**: Deferred to v0.7.0

## Getting Started

```bash
dotnet add package ElBruno.MempalaceNet --prerelease
```

See [README](https://github.com/elbruno/ElBruno.MempalaceNet#readme) for quick start.

## What's Next

**v0.6.0 (Weeks 1-9):**
- sqlite-vec integration (Weeks 1-4)
- BM25 hybrid search (Weeks 1-4 parallel)
- LongMemEval R@5 validation (Weeks 5-8)
- Release v0.6.0-preview.1 (Week 9)

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
