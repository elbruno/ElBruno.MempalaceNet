# Decision: NuGet Publishing Status — v0.6.0

**Date:** 2025-04-27  
**Status:** ✅ Resolved  
**Requestor:** Bruno Capuano  
**Architect:** Deckard

---

## Question
Are the MemPalace.NET packages published to NuGet.org correctly? If not, why not? Attempt to publish if needed.

---

## Investigation Summary

### NuGet.org Status (API audit)
| Package | NuGet Version | Local Version | Status |
|---------|---------------|---------------|--------|
| mempalace.core | 0.6.0 | 0.6.0 | ✅ Current |
| mempalace.backends.sqlite | 0.6.0 | 0.6.0 | ✅ Current |
| mempalace.ai | 0.6.0 | 0.6.0 | ✅ Current |
| mempalace.search | 0.6.0 | 0.6.0 | ✅ Current |
| mempalace.knowledgegraph | 0.6.0 | 0.6.0 | ✅ Current |
| mempalace.mining | 0.6.0 | 0.6.0 | ✅ Current |
| mempalace.mcp | 0.6.0 | 0.6.0 | ✅ Current |
| mempalace.agents | 0.6.0 | 0.6.0 | ✅ Current |
| mempalacenet (CLI) | 0.6.0 | 0.6.0 | ✅ Current |
| mempalacenet-bench | 0.6.0 | 0.6.0 | ✅ Current |

**Verification Method:**  
`https://api.nuget.org/v3-flatcontainer/{package-id}/index.json` for each package.

---

## GitHub Actions Workflow Status

**Workflow:** `.github/workflows/publish.yml`  
**Latest Run:** 24938559571 (✅ Success, ~2 days ago)  
**Trigger:** Release event (tag `v0.6.0`)  
**Auth:** OIDC via `NuGet/login@v1` with `secrets.NUGET_USER`  
**Process:**
1. Checkout → Setup .NET 10 → Restore → Build → Test
2. Pack all projects to `artifacts/`
3. Push to NuGet.org with `--skip-duplicate`

**Git Tags:**
- `v0.6.0` ✅
- `v0.6.0-preview.1` ✅
- `v0.5.0-preview.1` ✅

---

## Local Environment Check

- ✅ .NET SDK: `10.0.300-preview.0.26177.108` (net10.0 target supported)
- ❌ NUGET_API_KEY: Not set (not required — GitHub Actions handles publishing)

---

## Decision

**No action needed.**  

All packages are correctly published to NuGet.org at version 0.6.0 via the automated GitHub Actions workflow. The current process is robust and follows best practices:

1. **Automated publishing via GitHub Actions** (triggered by release creation)
2. **OIDC authentication** (no long-lived API keys in local env)
3. **Dependency-aware publishing order** (workflow uses `dotnet nuget push artifacts/*.nupkg`)
4. **Test gate** (unit tests must pass before publishing)

---

## Recommendations for Future Releases

1. **Continue using GitHub Actions** for all NuGet publishing:
   - Create GitHub release with semantic version tag (e.g., `v0.7.0`)
   - Workflow auto-triggers and publishes
   - No local API key management required
2. **Manual dispatch option** remains available for hotfixes:
   - Actions → Publish to NuGet → Run workflow
   - Specify version override if needed
3. **Pre-release versions** follow SemVer 2.0 (e.g., `0.7.0-preview.1`)
4. **PUBLISHING_CHECKLIST.md** is accurate; retain as reference for manual local publishing (edge cases only)

---

## References

- **Workflow:** `.github/workflows/publish.yml`
- **Checklist:** `PUBLISHING_CHECKLIST.md`
- **Version:** `src/Directory.Build.props` (v0.6.0)
- **GitHub Release:** https://github.com/elbruno/ElBruno.MempalaceNet/releases/tag/v0.6.0
- **NuGet Profile:** https://www.nuget.org/profiles/elbruno
