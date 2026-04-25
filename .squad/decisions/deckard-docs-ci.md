# Deckard: Docs Cleanup & CI Decision — 2026-04-25

## Summary
Fixed documentation accuracy issues (MCP tool count overstated, wake-up command listed prematurely) and formalized Bruno's CI directive to save GitHub Actions minutes.

## Changes

### 1. Documentation Accuracy
**Files:** `README.md`, `docs/RELEASE-v0.1.md`

**Issue:** Both files claimed "29 tools" in MCP server, but only 7 tools delivered in v0.1.0 (palace_search, palace_recall, palace_get, palace_list_wings, kg_query, kg_timeline, palace_health).

**Fix:** Updated both files to state "7 tools in v0.1" with clear forward path to 29 tools post-v0.1.

**Rationale:** Accuracy matters for user expectations. Python reference has 29 tools; .NET v0.1 intentionally ships 7 read-only tools. Phase 11+ will expand to 29.

### 2. Quick Start Accuracy
**File:** `README.md`

**Issue:** Quick start included `mempalacenet wake-up` command, which is documented as post-v0.1 work (PLAN.md Phase 11).

**Fix:** Removed wake-up from quick start commands. Still listed in Roadmap as post-v0.1.

**Rationale:** Users following quick start should not encounter unimplemented features. Roadmap clearly documents when wake-up arrives.

### 3. CI Decision Captured
**File:** `.squad/decisions.md` (new section: CI & Operations)

**Directive:** Bruno Capuano requested keeping CI limited to version tags (`v*`) to save GitHub Actions minutes.

**Decision:** CI workflow remains tag-triggered + manual dispatch only. No main branch or scheduled triggers. Individual development builds tested locally.

**Rationale:** 
- Saves minutes (user request)
- Tag releases are the main CI validation point (packing, test coverage)
- Main branch pushes can be built/tested locally by developers
- No loss of quality: release tests still run before publishing

## Files Modified
- `README.md` — tool count, quick start commands
- `docs/RELEASE-v0.1.md` — tool count
- `.squad/decisions.md` — formalized CI decision

## Files Not Modified (by design)
- `.github/workflows/ci.yml` — CI already implements tag-triggered strategy; no change needed
- Project code — purely administrative/doc work

## Verification
All changes are documentation and squad housekeeping:
- README/RELEASE docs are human-readable (verified against history.md audit findings)
- Decisions.md merged from inbox
- No code changes; no tests to break
- Decision captured for future reference

## Next Steps for Bruno
1. Review docs changes to confirm accuracy
2. Proceed with v0.1.0 tagging when Phase 9 (Bryant's benchmarks) is green
3. Tag command: `git tag -a v0.1.0 -m "MemPalace.NET v0.1.0" && git push --tags`
