# Decision: v0.1.0 Release Execution

**Date:** 2026-04-25  
**Author:** Deckard (Lead/Architect)  
**Status:** ✅ Executed  
**Category:** Release Management

## Context

MemPalace.NET reached v0.1.0 readiness after completing Phases 0-10. All core features shipped: local-first memory with ONNX embeddings, M.E.AI integration, Agent Framework support, MCP server (7 tools), temporal knowledge graph, semantic/hybrid search, SQLite backend, CLI tools, and comprehensive documentation.

**Readiness Checklist:**
- ✅ 129/129 tests passing
- ✅ All 10 projects have NuGet metadata
- ✅ README.md release-quality (113 lines, architecture table, quick start)
- ✅ Full documentation suite (CHANGELOG, RELEASE notes, 10 topic docs)
- ✅ CI workflow with pack job (triggered on v* tags)
- ✅ Known limitations documented
- ✅ Bryant's benchmark harness complete (Phase 9)
- ✅ Deckard's polish complete (Phase 10)

## Decision

**Execute v0.1.0 release immediately** using pre-drafted release notes (docs/RELEASE-v0.1.md).

**Rationale:**
1. All architectural scope delivered
2. Test suite green (129 passing)
3. Documentation accurate (7 tools, no wake-up command)
4. Known limitations clearly stated (O(n) vector search, token overlap keywords)
5. No blocking issues; post-v0.1 roadmap clear

## Execution

**Commands:**
```bash
cd C:\src\elbruno.mempalacenet
git tag v0.1.0
git push origin v0.1.0
gh release create v0.1.0 --title "v0.1.0: MemPalace.NET Preview" --notes-file docs\RELEASE-v0.1.md
```

**Results:**
- Tag created at current HEAD (67 objects, 21.79 KiB compressed)
- Pushed to origin successfully
- GitHub release created: https://github.com/elbruno/mempalacenet/releases/tag/v0.1.0
- NuGet publish workflow will trigger on tag push

## Package Contents

**8 Core Libraries:**
- MemPalace.Core — domain types, storage interfaces
- MemPalace.Backends.Sqlite — SQLite backend with BLOB vectors
- MemPalace.Ai — M.E.AI integration (ONNX default)
- MemPalace.Mining — filesystem + conversation miners
- MemPalace.Search — semantic, keyword, hybrid search
- MemPalace.KnowledgeGraph — temporal triples
- MemPalace.Mcp — Model Context Protocol server (7 tools)
- MemPalace.Agents — Agent Framework integration

**2 CLI Tools:**
- mempalacenet — main CLI (init, mine, search, agents, kg, mcp)
- mempalacenet-bench — benchmark harness (LongMemEval, LoCoMo, ConvoMem, MemBench)

**Documentation:**
- README.md (113 lines, quick start, architecture)
- docs/CHANGELOG.md (phase-by-phase history)
- docs/RELEASE-v0.1.md (highlights, getting started, known limitations)
- 10 topic docs (architecture, concepts, backends, AI, mining, search, KG, MCP, agents, CLI, benchmarks)

## Known Limitations (Documented)

1. **Vector storage:** SQLite uses O(n) brute-force cosine similarity (acceptable for <100K vectors; upgrade to sqlite-vec or Qdrant planned post-v0.1)
2. **Keyword search:** Token overlap (BM25 planned post-v0.1)
3. **Wake-up summaries:** Not yet implemented (Phase 11+)
4. **Parity validation:** Real LongMemEval R@5 parity deferred to Phase 11 (harness supports it; decision to defer execution made in audit)

## Impact

**Immediate:**
- v0.1.0 available on GitHub with release notes
- NuGet packages will be published via CI workflow
- Public preview ready for local development and experimentation

**Post-v0.1 Work:**
- Phase 11: BM25 keyword search, wake-up summaries, parity validation runs
- Phase 12+: Vector store upgrade (sqlite-vec/Qdrant), MCP tool expansion (7 → 29)

## Team Coordination

- **Bryant (Phase 9):** Benchmark harness delivered; parity validation deferred to Phase 11 per decision log
- **Deckard (Phase 10):** Polish complete; release executed
- **Rachael:** Can proceed with CLI hardening independently
- **Scribe:** Will merge this decision to formal record

## References

- Release notes: docs/RELEASE-v0.1.md
- Changelog: docs/CHANGELOG.md
- GitHub release: https://github.com/elbruno/mempalacenet/releases/tag/v0.1.0
- Audit report: .squad/agents/deckard/roadmap-audit-2026-04-25.md
- Readiness report: .squad/decisions/inbox/deckard-readiness-report.md

---

**Deckard signing off:** v0.1.0 is live. Preview ready for the world. 🚀
