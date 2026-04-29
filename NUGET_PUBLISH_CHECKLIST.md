# v0.8.0 NuGet Publication Checklist

## Status: ✅ READY FOR AUTOMATED PUBLISH

The **"Publish to NuGet"** GitHub Actions workflow is configured and ready. It will automatically trigger when:

1. **A GitHub release is created** for the v0.8.0 tag
2. **Or manually triggered** via GitHub Actions UI

---

## Option 1: Create GitHub Release (Recommended)

The v0.8.0 tag is already pushed to GitHub. Create a release to trigger the automated publish:

```bash
# Via GitHub CLI (if authenticated)
gh release create v0.8.0 \
  --title "v0.8.0: BM25 Keyword Search + LLM Reranking" \
  --notes "See RELEASE_NOTES.md for full details"

# Or via GitHub Web UI
# https://github.com/elbruno/ElBruno.MempalaceNet/releases/new
# Select v0.8.0 tag, add title and notes, click "Publish release"
```

---

## Option 2: Manual Workflow Trigger

If GitHub Token is available, trigger the workflow directly:

```bash
export GITHUB_TOKEN=<your-github-token>

curl -X POST \
  -H "Authorization: Bearer $GITHUB_TOKEN" \
  -H "Accept: application/vnd.github.v3+json" \
  https://api.github.com/repos/elbruno/ElBruno.MempalaceNet/actions/workflows/publish.yml/dispatches \
  -d '{"ref":"main","inputs":{"version":"0.8.0"}}'
```

---

## Option 3: GitHub Actions UI

1. Go to: https://github.com/elbruno/ElBruno.MempalaceNet/actions
2. Select "Publish to NuGet" workflow
3. Click "Run workflow" button
4. Set branch to `main`, version to `0.8.0`
5. Click "Run workflow"

---

## Workflow Details

**File:** `.github/workflows/publish.yml`  
**Triggers:**
- ✅ Release published (automatic)
- ✅ `workflow_dispatch` with optional version input
- ✅ Can specify custom version or use Directory.Build.props (0.8.0)

**Steps:**
1. Setup .NET 10 (preview)
2. Restore dependencies
3. Build in Release mode
4. Run unit tests
5. Pack NuGet packages
6. Authenticate with NuGet (OIDC → temp API key)
7. Push to NuGet.org
8. Upload package artifact

---

## Git Status

| Item | Status |
|------|--------|
| v0.8.0 tag | ✅ Created & pushed to GitHub |
| main branch | ✅ Merged & pushed to GitHub |
| RELEASE_NOTES.md | ✅ Updated with v0.8.0 section |
| Directory.Build.props | ✅ Version bumped to 0.8.0 |
| Package file | ✅ Built locally (146 MB) |

---

## Expected Output

Once the workflow completes:

1. ✅ Package published to NuGet.org: https://www.nuget.org/packages/mempalacenet/0.8.0
2. ✅ NuGet package (.nupkg) available as GitHub Actions artifact
3. ✅ Installation command available:
   ```bash
   dotnet add package mempalacenet --version 0.8.0
   ```

---

**Next Step:** Create GitHub release OR trigger workflow dispatch to publish.
