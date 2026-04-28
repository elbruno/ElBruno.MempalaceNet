# v0.7.0 Test Strategy Guide

**Version:** v0.7.0  
**Author:** Bryant (QA/Tester)  
**Date:** 2026-04-27  
**Audience:** Developers, QA engineers, contributors

---

## Overview

This guide provides a comprehensive testing strategy for MemPalace.NET v0.7.0, covering:
- Test approach for 4 major features
- Test matrices and edge cases
- Integration test scenarios
- Test execution instructions

**v0.7.0 Features:**
1. ~~MCP SSE Transport~~ (deferred to v0.8.0)
2. Wake-up LLM Summarization (P0)
3. Skill Marketplace CLI (P1)
4. Custom Embedder Interface (P0)

---

## Test Framework

**Stack:**
- xUnit (test runner)
- FluentAssertions (assertions)
- NSubstitute (mocking)
- Coverlet (code coverage)

**Constraints:**
- ❌ No external LLM API calls in tests
- ❌ No ONNX model downloads in CI
- ❌ No MCP server spawning in unit tests
- ✅ Mock all external dependencies
- ✅ Deterministic embedders for testing

---

## Feature Testing

### 1. Wake-Up LLM Summarization

**CLI Command:**
```bash
mempalacenet wake-up --wing <wing-name> --limit <N> [--summarize]
```

**Test Scenarios:**

#### Happy Path
```csharp
[Fact]
public async Task WakeUp_ReturnsLastNMemories()
{
    // Arrange: palace with 50 memories in "test-wing"
    var palace = await SetupTestPalace(memoryCount: 50, wing: "test-wing");
    
    // Act: wake-up last 10
    var results = await palace.WakeUp(wing: "test-wing", limit: 10);
    
    // Assert
    results.Should().HaveCount(10);
    results.Should().BeInDescendingOrder(m => m.Timestamp); // most recent first
}
```

#### Edge Cases
- Empty wing (0 memories) → return empty list
- Wing with < N memories → return all available
- Invalid wing name → return clear error
- IChatClient not configured → fallback to raw memories
- LLM timeout (>30s) → cancel and return raw memories

#### Mock LLM
```csharp
[Fact]
public async Task WakeUp_WithSummarize_CallsLLM()
{
    // Arrange: mock IChatClient
    var mockChatClient = Substitute.For<IChatClient>();
    mockChatClient.CompleteAsync(Arg.Any<string>(), Arg.Any<ChatOptions>())
        .Returns("Summarized context: recent work on X, Y, Z.");
    
    // Act: wake-up with summarization
    var summary = await palace.WakeUp(wing: "test-wing", limit: 10, summarize: true);
    
    // Assert
    summary.Should().Contain("Summarized context");
    await mockChatClient.Received(1).CompleteAsync(Arg.Any<string>(), Arg.Any<ChatOptions>());
}
```

---

### 2. Skill Marketplace CLI

**CLI Commands:**
```bash
mempalacenet skill list
mempalacenet skill install <path-or-url>
mempalacenet skill enable <name>
mempalacenet skill disable <name>
```

**Test Scenarios:**

#### Happy Path
```csharp
[Fact]
public async Task SkillInstall_FromValidPath_RegistersSkill()
{
    // Arrange: valid skill fixture
    var skillPath = Path.Combine(TestFixtures, "Skills", "valid-skill");
    
    // Act: install skill
    var result = await SkillManager.InstallAsync(skillPath);
    
    // Assert
    result.Success.Should().BeTrue();
    var skills = await SkillManager.ListAsync();
    skills.Should().Contain(s => s.Name == "valid-skill");
}
```

#### Edge Cases
- Missing manifest → error: "Manifest not found"
- Malformed YAML → error: "Failed to parse manifest"
- Invalid schema → error with field-specific message
- Duplicate skill → error or prompt overwrite

#### Manifest Validation
```csharp
[Theory]
[InlineData("missing-name", "Field 'name' is required")]
[InlineData("invalid-version", "Invalid version format")]
[InlineData("missing-capabilities", "Field 'capabilities' is required")]
public void ManifestParser_InvalidSchema_ThrowsValidationError(string fixtureName, string expectedError)
{
    // Arrange
    var manifestPath = Path.Combine(TestFixtures, "Skills", fixtureName, ".copilot-skill.yaml");
    
    // Act
    var act = () => ManifestParser.Parse(manifestPath);
    
    // Assert
    act.Should().Throw<ValidationException>()
        .WithMessage($"*{expectedError}*");
}
```

---

### 3. Custom Embedder Interface

**Factory Pattern:**
```csharp
var embedder = EmbedderFactory.Create("Local", config); // ONNX
var embedder = EmbedderFactory.Create("MyCustom", config); // User-provided
```

**Test Scenarios:**

#### Happy Path
```csharp
[Fact]
public void EmbedderFactory_CreateLocal_ReturnsMeaiEmbedder()
{
    // Arrange: config for Local provider
    var config = new EmbedderConfig { Provider = "Local", Model = "all-MiniLM-L6-v2" };
    
    // Act
    var embedder = EmbedderFactory.Create(config);
    
    // Assert
    embedder.Should().NotBeNull();
    embedder.ModelIdentity.Should().Contain("Local");
}
```

#### Edge Cases
- Unknown provider → fallback to "Local" with warning
- Duplicate provider registration → error or overwrite
- Custom embedder throws in constructor → wrap in EmbedderError
- Wrong dimensions returned → detect at first embed, throw exception

#### Custom Embedder Registration
```csharp
[Fact]
public void AddEmbedder_CustomProvider_IsDiscoverable()
{
    // Arrange: DI container with custom embedder
    var services = new ServiceCollection();
    services.AddEmbedder<DeterministicEmbedder>("Deterministic");
    var provider = services.BuildServiceProvider();
    
    // Act: list providers
    var factory = provider.GetRequiredService<IEmbedderFactory>();
    var providers = factory.ListProviders();
    
    // Assert
    providers.Should().Contain("Deterministic");
}
```

---

## Integration Tests

### E2E: Wake-Up + LLM

**Scenario:** Init palace → mine memories → wake-up with summarization

```csharp
[Fact]
public async Task E2E_WakeUpWithLLM_ReturnsContextSummary()
{
    // Arrange: init palace, mine 50 memories
    var palace = await Palace.Create("~/test-palace");
    await palace.Mine("~/test-docs", wing: "docs");
    
    // Mock IChatClient
    var mockLLM = Substitute.For<IChatClient>();
    mockLLM.CompleteAsync(Arg.Any<string>())
        .Returns("Summary: 50 docs about vector search, embeddings, and RAG.");
    
    // Act: wake-up with summarization
    var summary = await palace.WakeUp(wing: "docs", limit: 10, chatClient: mockLLM);
    
    // Assert
    summary.Should().Contain("Summary:");
    await mockLLM.Received(1).CompleteAsync(Arg.Any<string>());
}
```

---

### E2E: Custom Embedder Workflow

**Scenario:** Register custom embedder → init palace → mine → search

```csharp
[Fact]
public async Task E2E_CustomEmbedder_WorksInPalace()
{
    // Arrange: register DeterministicEmbedder
    var services = new ServiceCollection();
    services.AddEmbedder<DeterministicEmbedder>("Deterministic");
    var config = new PalaceConfig 
    { 
        Path = "~/test-palace", 
        EmbedderProvider = "Deterministic" 
    };
    
    // Act: init palace, mine, search
    var palace = await Palace.Create(config);
    await palace.Store("test content", wing: "test");
    var results = await palace.Search(query: "test", wing: "test");
    
    // Assert
    results.Should().NotBeEmpty();
    results[0].Memory.ModelIdentity.Should().Contain("Deterministic");
}
```

---

### E2E: Skill + MCP Tool Discovery

**Scenario:** Install skill with custom MCP tools → MCP server discovers new tools

```csharp
[Fact]
public async Task E2E_SkillInstall_McpToolsDiscovered()
{
    // Arrange: install skill with custom tools
    var skillPath = Path.Combine(TestFixtures, "Skills", "mcp-tools-skill");
    await SkillManager.InstallAsync(skillPath);
    
    // Act: query MCP server for tools
    var mcpClient = new MockMcpClient();
    var tools = await mcpClient.ListToolsAsync();
    
    // Assert
    tools.Should().Contain(t => t.Name == "custom_skill_tool");
}
```

---

## Test Fixtures

### Skill Fixtures

**Location:** `src/MemPalace.Tests/Fixtures/Skills/`

```
Skills/
├── valid-skill/
│   ├── .copilot-skill.yaml    # Complete manifest
│   └── README.md
├── missing-manifest/
│   └── README.md              # No .copilot-skill.yaml
├── malformed-yaml/
│   └── .copilot-skill.yaml    # Invalid YAML syntax
└── invalid-schema/
    └── .copilot-skill.yaml    # Missing required fields
```

**valid-skill/.copilot-skill.yaml:**
```yaml
name: valid-skill
version: 0.1.0
description: Test skill for validation
author: test-author
capabilities:
  - search
  - kg
```

---

### Embedder Fixtures

**Location:** `src/MemPalace.Tests/Fixtures/Embedders/`

**DeterministicEmbedder.cs** (already exists):
```csharp
public class DeterministicEmbedder : IEmbedder
{
    public string ModelIdentity => "Deterministic:hash-based";
    public int Dimensions { get; private set; } = 128;

    public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IEnumerable<string> texts, 
        CancellationToken ct = default)
    {
        // Hash-based deterministic vectors for testing
        var vectors = texts.Select(t => GenerateDeterministicVector(t)).ToList();
        return new ValueTask<IReadOnlyList<ReadOnlyMemory<float>>>(vectors);
    }

    private ReadOnlyMemory<float> GenerateDeterministicVector(string text)
    {
        var hash = text.GetHashCode();
        var vector = new float[Dimensions];
        var rng = new Random(hash);
        for (int i = 0; i < Dimensions; i++)
            vector[i] = (float)rng.NextDouble();
        return new ReadOnlyMemory<float>(vector);
    }
}
```

**ThrowingEmbedder.cs** (new):
```csharp
public class ThrowingEmbedder : IEmbedder
{
    public string ModelIdentity => "Throwing:error-test";
    public int Dimensions => throw new InvalidOperationException("Embedder not initialized");

    public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IEnumerable<string> texts, 
        CancellationToken ct = default)
    {
        throw new InvalidOperationException("Embedder failed to embed");
    }
}
```

---

## Running Tests

### All Tests
```bash
dotnet test src/MemPalace.slnx
```

### Feature-Specific Tests
```bash
# Wake-up tests only
dotnet test --filter "FullyQualifiedName~WakeUp"

# Skill CLI tests only
dotnet test --filter "FullyQualifiedName~Skill"

# Embedder factory tests only
dotnet test --filter "FullyQualifiedName~Embedder"

# Integration tests only
dotnet test --filter "Category=Integration"
```

### Coverage Report
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

---

## Test Execution Schedule

### Week 1-2: Unit Tests
**Owner:** Bryant  
**Focus:** Wake-up command, skill CLI, embedder factory

**Tasks:**
1. Implement `WakeUpCommandTests.cs` (10 tests)
2. Implement `SkillCommandTests.cs` (8 tests)
3. Implement `EmbedderFactoryTests.cs` (7 tests)
4. Implement manifest parsing tests (5 tests)

**Exit Criteria:** 30+ unit tests green, < 50ms avg execution

---

### Week 3-4: Integration Tests
**Owner:** Bryant + Agent code reviews  
**Focus:** Cross-feature E2E scenarios

**Tasks:**
1. Implement `WakeUpIntegrationTests.cs` (4 tests)
2. Implement `CustomEmbedderWorkflowTests.cs` (3 tests)
3. Implement `SkillMcpDiscoveryTests.cs` (2 tests)
4. Implement `McpWakeUpTests.cs` (2 tests)

**Exit Criteria:** 11+ integration tests green, < 5s avg execution

---

### Week 5: Regression Testing
**Owner:** Bryant  
**Focus:** Verify no regressions in existing 152 tests

**Tasks:**
1. Run full test suite: `dotnet test src/`
2. Document any failures or performance regressions
3. Fix or triage failures with agent owners

**Exit Criteria:** All 152 existing tests still green, no perf degradation

---

### Week 6: CI Integration
**Owner:** Bryant + Deckard  
**Focus:** Update CI pipeline, add coverage gate

**Tasks:**
1. Update `.github/workflows/ci.yml` to include new tests
2. Add Coverlet coverage gate (≥ 80%)
3. Configure test timeouts (unit: 1min, integration: 5min)
4. Verify CI green on pull request

**Exit Criteria:** CI pipeline green, coverage ≥ 80%, no flaky tests

---

## Coverage Targets

| Test Category | Target | Measurement |
|---------------|--------|-------------|
| Unit Tests | ≥ 90% | Coverlet line coverage |
| Integration Tests | ≥ 70% | End-to-end scenario coverage |
| Overall v0.7.0 | ≥ 85% | Combined line coverage |

**Coverage Reports:**
- Coverlet LCOV format
- GitHub Actions CI summary
- SonarQube integration (future)

---

## Troubleshooting

### Test Failures

**"Embedder not initialized" error:**
- **Cause:** IEmbedder.Dimensions called before first EmbedAsync()
- **Fix:** Call EmbedAsync() at least once before accessing Dimensions

**"IChatClient not configured" warning:**
- **Cause:** Wake-up with `--summarize` but no LLM provider configured
- **Fix:** Mock IChatClient in tests, or configure local provider in appsettings.json

**"Skill manifest not found" error:**
- **Cause:** Skill installation from invalid path
- **Fix:** Verify skill path contains `.copilot-skill.yaml` file

---

### CI Timeouts

**Symptom:** Integration tests exceed 5s threshold

**Solutions:**
1. Reduce dataset size in tests (use 10 memories instead of 100)
2. Mock slow dependencies (embedders, LLMs)
3. Use deterministic embedders (no real model inference)
4. Run integration tests in parallel (xUnit parallelization)

---

## Best Practices

### 1. Mock External Dependencies
```csharp
// ❌ Bad: Real API call
var client = new HttpClient();
var response = await client.GetAsync("https://api.example.com");

// ✅ Good: Mock HTTP client
var mockHttp = Substitute.For<HttpMessageHandler>();
mockHttp.Send(Arg.Any<HttpRequestMessage>()).Returns(new HttpResponseMessage(HttpStatusCode.OK));
```

### 2. Use Deterministic Embedders
```csharp
// ❌ Bad: Real ONNX embedder (slow, non-deterministic)
var embedder = new LocalEmbedder(modelPath: "all-MiniLM-L6-v2.onnx");

// ✅ Good: Hash-based embedder (fast, deterministic)
var embedder = new DeterministicEmbedder(dimensions: 128);
```

### 3. Assert on Behavior, Not Implementation
```csharp
// ❌ Bad: Testing internal state
Assert.Equal(10, service._internalCache.Count);

// ✅ Good: Testing observable behavior
var results = await service.Search(query: "test");
results.Should().HaveCount(10);
```

---

## FAQ

**Q: Should I test MCP SSE transport in v0.7.0?**  
A: No, MCP SSE deferred to v0.8.0. Focus on wake-up, skill CLI, and embedder factory.

**Q: Should I spawn a real MCP server in integration tests?**  
A: No, use mock stdio transport or in-memory MCP client. Real server spawning is manual smoke test only.

**Q: Should I test Ollama provider in v0.7.0?**  
A: No, Ollama deferred to v0.7+. Add test placeholder with `[Fact(Skip = "Ollama deferred")]`.

**Q: How many tests should v0.7.0 have?**  
A: Estimate 70-100 new tests (40 unit, 30 integration). Total: 222-252 tests.

---

## References

- **Test Strategy ADR:** `.squad/decisions/inbox/bryant-v070-test-strategy.md`
- **v0.7.0 Roadmap:** `.squad/agents/deckard/history.md` (lines 837-930)
- **Existing Tests:** `src/MemPalace.Tests/`
- **xUnit Documentation:** https://xunit.net/
- **FluentAssertions:** https://fluentassertions.com/
- **NSubstitute:** https://nsubstitute.github.io/

---

**END OF GUIDE**
