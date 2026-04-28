# v0.7.0 Phase 2 Integration Test Strategy

**Author:** Bryant (Tester/QA)  
**Date:** 2025-01-27  
**Version:** 1.0  
**Status:** Planning

---

## Overview

Phase 2 integration testing validates that the three Phase 1 features work together in realistic end-to-end scenarios:

1. **MCP SSE Transport** (Tyrell) — HTTP/SSE server for MCP protocol
2. **Wake-up LLM Summarization** (Roy) — Enriched context via LLM summaries
3. **Skill CLI Scaffolding** (Rachael) — Plugin system for extending CLI

This document defines integration test scenarios, fixtures, and CI/CD strategy.

---

## Integration Test Scenarios

### Scenario 1: MCP SSE + Skill CLI Integration

**Goal:** Verify skills can be invoked over MCP SSE transport

**Test Flow:**
1. Install a test skill via `SkillManager`
2. Start `HttpSseTransport` on `localhost:5050`
3. Create MCP session via POST `/mcp`
4. Send MCP tool call request for skill command
5. Verify skill executes and returns result via SSE

**Expected Behavior:**
- Skill is discovered by MCP server
- Tool call routes to skill handler
- SSE stream receives response
- Session state persists across calls

**Coverage:** MCP transport + Skill runtime

---

### Scenario 2: Wake-up + Palace Integration

**Goal:** Verify WakeUp command enriches results with LLM summaries

**Test Flow:**
1. Create test palace with 20 memories
2. Configure `LLMMemorySummarizer` with mock chat client
3. Call `Palace.WakeUp(wing: "test-wing", limit: 10)`
4. Verify summarizer is invoked
5. Check that summary is included in WakeUp result

**Expected Behavior:**
- WakeUp retrieves most recent memories
- Summarizer generates bullet-point summary
- Summary attached to result metadata
- Graceful fallback if LLM fails

**Coverage:** Wake-up + Summarization + Palace

---

### Scenario 3: Full v0.7.0 E2E — Skill → Palace → Wake-up

**Goal:** End-to-end validation of all Phase 1 features

**Test Flow:**
1. Install "memory-summary" skill (test fixture)
2. Start MCP SSE server
3. Create MCP session
4. Store 30 memories in palace via MCP Store tool
5. Send MCP tool call to "memory-summary" skill
6. Skill invokes `Palace.WakeUp()` with summarization
7. Verify SSE stream receives enriched summary

**Expected Behavior:**
- Memories stored via MCP
- Skill CLI routes tool call
- WakeUp generates LLM summary
- MCP returns enriched response
- No errors or timeouts

**Coverage:** Full stack (MCP + Skills + Summarization + Palace)

---

### Scenario 4: MCP SSE Multi-Session Isolation

**Goal:** Verify MCP sessions are isolated and thread-safe

**Test Flow:**
1. Start MCP SSE server
2. Create 5 parallel sessions
3. Each session stores/retrieves different memories
4. Each session invokes different skills
5. Verify no cross-session data leakage

**Expected Behavior:**
- Sessions have unique IDs
- Session state is isolated
- Concurrent requests handled safely
- Session cleanup works correctly

**Coverage:** MCP transport + SessionManager

---

### Scenario 5: Skill Installation + MCP Discovery

**Goal:** Verify MCP server discovers newly installed skills

**Test Flow:**
1. Start MCP SSE server
2. Query available tools via MCP `tools/list`
3. Install new skill via `SkillManager.InstallAsync()`
4. Query tools again
5. Verify new skill appears in tool list

**Expected Behavior:**
- Initial tool list doesn't include new skill
- After install, skill is discovered
- Skill metadata is correctly exposed
- Skill can be invoked immediately

**Coverage:** Skill CLI + MCP tool discovery

---

## Test Fixtures

### Fixture 1: TestSkillFixture

**Purpose:** Create disposable test skills for integration tests

**Provides:**
- Dummy skill directory with `skill.json`
- Test entry point script
- Automatic cleanup after test

**Example:**
```csharp
public class TestSkillFixture : IDisposable
{
    public string SkillPath { get; }
    public string SkillId { get; }

    public TestSkillFixture(string skillId = "test-skill")
    {
        SkillPath = CreateTestSkill(skillId);
        SkillId = skillId;
    }

    private string CreateTestSkill(string id)
    {
        // Create temp skill directory
        // Write skill.json manifest
        // Write entry point file
        return skillPath;
    }

    public void Dispose()
    {
        if (Directory.Exists(SkillPath))
            Directory.Delete(SkillPath, recursive: true);
    }
}
```

---

### Fixture 2: McpServerFixture

**Purpose:** Start/stop MCP SSE server for integration tests

**Provides:**
- Running MCP server on random available port
- Pre-created test palace
- HTTP client for making requests
- Automatic cleanup

**Example:**
```csharp
public class McpServerFixture : IAsyncLifetime
{
    public HttpSseTransport Transport { get; private set; }
    public IPalace Palace { get; private set; }
    public int Port { get; private set; }
    public HttpClient Client { get; private set; }

    public async Task InitializeAsync()
    {
        Port = GetRandomAvailablePort();
        Palace = await CreateTestPalace();
        Transport = new HttpSseTransport(logger, port: Port);
        await Transport.StartAsync();
        Client = new HttpClient();
    }

    public async Task DisposeAsync()
    {
        await Transport.StopAsync();
        Transport.Dispose();
        Client.Dispose();
        await Palace.DisposeAsync();
    }

    private int GetRandomAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
```

---

### Fixture 3: MockLLMFixture

**Purpose:** Mock LLM for deterministic summarization testing

**Provides:**
- Mock `IChatClient` with predictable responses
- Configurable summary text
- Call tracking for assertions

**Example:**
```csharp
public class MockLLMFixture
{
    public IChatClient ChatClient { get; }
    public MockChatClient MockClient { get; }

    public MockLLMFixture(string summaryResponse = "Test summary")
    {
        MockClient = new MockChatClient(summaryResponse);
        ChatClient = MockClient;
    }

    public class MockChatClient : IChatClient
    {
        private readonly string _response;
        public int CallCount { get; private set; }
        public IList<ChatMessage>? LastMessages { get; private set; }

        public MockChatClient(string response)
        {
            _response = response;
        }

        public async Task<ChatResponse> GetResponseAsync(
            IList<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken ct = default)
        {
            CallCount++;
            LastMessages = messages;
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, _response));
        }

        // ... (implement other IChatClient members)
    }
}
```

---

### Fixture 4: TestPalaceFixture

**Purpose:** Create isolated test palace with sample data

**Provides:**
- Temp SQLite palace
- Pre-populated with test memories
- Multiple wings/rooms for testing
- Automatic cleanup

**Example:**
```csharp
public class TestPalaceFixture : IAsyncLifetime
{
    public IPalace Palace { get; private set; }
    public string PalacePath { get; private set; }

    public async Task InitializeAsync()
    {
        PalacePath = Path.Combine(Path.GetTempPath(), "mempalace-test", Guid.NewGuid().ToString());
        Palace = await Palace.Create(PalacePath);

        // Seed test data
        await SeedTestMemories();
    }

    private async Task SeedTestMemories()
    {
        await Palace.Store("Memory 1", wing: "test-wing");
        await Palace.Store("Memory 2", wing: "test-wing");
        await Palace.Store("Memory 3", wing: "test-wing", room: "test-room");
        // ...
    }

    public async Task DisposeAsync()
    {
        await Palace.DisposeAsync();
        if (Directory.Exists(PalacePath))
            Directory.Delete(PalacePath, recursive: true);
    }
}
```

---

## Test Organization

### Directory Structure
```
src/MemPalace.Tests/
├── Integration/                    # NEW
│   ├── McpSkillIntegrationTests.cs
│   ├── WakeUpSummaryIntegrationTests.cs
│   ├── FullStackE2ETests.cs
│   ├── McpSessionIsolationTests.cs
│   ├── SkillDiscoveryIntegrationTests.cs
│   └── Fixtures/
│       ├── TestSkillFixture.cs
│       ├── McpServerFixture.cs
│       ├── MockLLMFixture.cs
│       └── TestPalaceFixture.cs
├── Mcp/
│   └── Transports/
│       ├── HttpSseTransportTests.cs      # Existing (Tyrell)
│       └── SessionManagerTests.cs        # Existing
├── Ai/
│   └── Summarization/
│       └── MemorySummarizerTests.cs      # Existing (Roy)
└── Cli/
    └── Skill/
        ├── SkillManagerTests.cs          # Existing (Rachael)
        └── SkillManifestTests.cs         # Existing
```

### Test Naming Convention
- **Unit tests:** `ClassName_MethodName_ExpectedBehavior`
- **Integration tests:** `Feature1Feature2_Scenario_ExpectedBehavior`

**Examples:**
- `McpSkill_InvokeOverSse_ReturnsResult`
- `WakeUpSummary_LLMEnabled_EnrichesResults`
- `FullStack_StoreAndSummarize_E2ESuccess`

---

## Test Traits and Categories

Use xUnit traits for filtering:

```csharp
[Fact]
[Trait("Category", "Integration")]
[Trait("Category", "v070")]
[Trait("Feature", "MCP+Skill")]
public async Task McpSkill_InvokeOverSse_ReturnsResult()
{
    // ...
}
```

**Categories:**
- `Integration` — All integration tests
- `v070` — Tests for v0.7.0 release
- `E2E` — Full end-to-end tests (slowest)
- `Feature` — Specific feature area (e.g., `MCP+Skill`, `WakeUp+LLM`)

**Filtering Examples:**
```bash
# Run all v0.7.0 integration tests
dotnet test --filter "Category=Integration&Category=v070"

# Run full E2E tests only
dotnet test --filter "Category=E2E"

# Run MCP+Skill integration tests
dotnet test --filter "Feature=MCP+Skill"
```

---

## Coverage Targets

| Test Type | Target Coverage | Priority |
|-----------|----------------|----------|
| Unit tests (Phase 1) | 80-85% | ✅ Complete |
| Integration tests (Phase 2) | **85%+** | 🔴 To implement |
| E2E tests | **90%+** | 🔴 To implement |

**Coverage Measurement:**
```bash
dotnet test --collect:"XPlat Code Coverage" --filter "Category=Integration"
```

**Tool:** Use ReportGenerator for HTML reports:
```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/*.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

---

## CI/CD Integration

### GitHub Actions Workflow

**File:** `.github/workflows/integration-tests.yml`

```yaml
name: v0.7.0 Integration Tests

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'  # or whatever version
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore
      
      - name: Run Integration Tests
        run: |
          dotnet test \
            --filter "Category=Integration&Category=v070" \
            --collect:"XPlat Code Coverage" \
            --logger "trx;LogFileName=integration-tests.trx" \
            --verbosity normal
      
      - name: Generate Coverage Report
        if: always()
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator \
            -reports:"**/coverage.cobertura.xml" \
            -targetdir:"coverage-report" \
            -reporttypes:"Html;MarkdownSummaryGithub"
      
      - name: Upload Coverage Report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: coverage-report/
      
      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: "**/TestResults/*.trx"
      
      - name: Check Coverage Threshold
        run: |
          # Parse coverage.cobertura.xml and fail if < 85%
          # (Use a script or tool like coverlet)
```

### Pre-commit Hook (Optional)

**File:** `.git/hooks/pre-commit`

```bash
#!/bin/bash
# Run fast integration tests before commit
dotnet test --filter "Category=Integration&Category!=E2E" --verbosity quiet
if [ $? -ne 0 ]; then
    echo "❌ Integration tests failed. Commit aborted."
    exit 1
fi
echo "✅ Integration tests passed."
```

---

## Timeline Estimate

| Task | Estimated Time | Priority |
|------|---------------|----------|
| Create test fixtures | 4 hours | High |
| Implement Scenario 1 (MCP+Skill) | 3 hours | High |
| Implement Scenario 2 (WakeUp+LLM) | 2 hours | High |
| Implement Scenario 3 (Full E2E) | 4 hours | High |
| Implement Scenario 4 (Multi-session) | 2 hours | Medium |
| Implement Scenario 5 (Discovery) | 2 hours | Medium |
| CI/CD integration | 2 hours | High |
| Documentation + cleanup | 1 hour | Low |
| **Total** | **20 hours** (~2.5 days) | |

---

## Dependencies and Risks

### Dependencies
- ✅ Phase 1 unit tests pass (blocked by build error)
- ✅ HttpSseTransport compiles (blocked by UseUrls issue)
- ✅ SkillManager API stable
- ✅ LLMMemorySummarizer API stable

### Risks
- **MCP SSE flakiness:** HTTP tests can be flaky with port conflicts
  - **Mitigation:** Use random available ports, retry logic
- **LLM timeout:** Real LLM calls slow down tests
  - **Mitigation:** Use mocks for integration tests
- **File system cleanup:** Temp files can accumulate
  - **Mitigation:** Robust fixture disposal, CI cleanup

---

## Success Criteria

✅ Phase 2 integration tests are **approved** if:

1. All 5 integration test scenarios implemented
2. All tests pass consistently (no flakiness)
3. Coverage ≥ 85% for integration test code paths
4. CI/CD workflow runs successfully on PR
5. Documentation complete and reviewed

---

## Next Steps

1. **Bryant:** Wait for Tyrell to fix build blocker
2. **Bryant:** Implement test fixtures (4 hours)
3. **Bryant:** Implement Scenarios 1-3 (9 hours)
4. **Bryant:** Implement Scenarios 4-5 (4 hours)
5. **Bryant:** Set up CI/CD workflow (2 hours)
6. **Bryant:** Run full test suite, measure coverage
7. **Bryant:** Create approval summary in decisions inbox

---

## References

- xUnit fixtures: https://xunit.net/docs/shared-context
- Code coverage: https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage
- GitHub Actions: https://docs.github.com/en/actions
- MCP spec: https://modelcontextprotocol.io/

---

**Status:** Waiting on Phase 1 build fix before implementation can begin.
