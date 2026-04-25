# Contributing to MemPalace.NET

Thank you for your interest in contributing to MemPalace.NET! 🎉

We're building a local-first AI memory system that respects privacy, runs entirely offline by default, and integrates seamlessly with the .NET ecosystem. Whether you're fixing a bug, adding a feature, improving documentation, or sharing ideas, your contributions are valued and welcome.

## Table of Contents

- [Welcome](#welcome)
- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Code Style & Standards](#code-style--standards)
- [Testing Requirements](#testing-requirements)
- [Documentation](#documentation)
- [Commit Message Format](#commit-message-format)
- [Pull Request Process](#pull-request-process)
- [Issue Reporting](#issue-reporting)
- [Questions?](#questions)

---

## Welcome

We welcome all types of contributions:

- **💡 Ideas & Feature Requests** — Share your vision for what MemPalace.NET could become
- **🐛 Bug Reports** — Help us identify and fix issues
- **📖 Documentation** — Improve guides, fix typos, add examples
- **🔧 Code Contributions** — Submit pull requests for features or fixes
- **💬 Community Support** — Answer questions in Discussions

All contributions, regardless of size, help make MemPalace.NET better for everyone.

## Code of Conduct

This project adheres to our [Code of Conduct](.github/CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior to the maintainers.

---

## Getting Started

### Prerequisites

- **.NET 10 SDK** or later ([download here](https://dotnet.microsoft.com/download))
- **Git** for version control
- A code editor (Visual Studio, VS Code, Rider, or your favorite IDE)

### Clone the Repository

```bash
git clone https://github.com/elbruno/mempalacenet.git
cd mempalacenet
```

### Build the Solution

```bash
dotnet build src/MemPalace.slnx
```

This will restore NuGet packages and compile all projects.

### Run Tests

Verify your setup by running the test suite:

```bash
dotnet test src/
```

You should see **152 tests passing** with no failures. If tests fail, check the [Troubleshooting](#troubleshooting) section.

### Troubleshooting

**Build fails with SDK version mismatch:**
- Ensure you have .NET 10 or later installed
- Check `global.json` for the required SDK version
- Run `dotnet --version` to confirm

**Tests fail on fresh clone:**
- Clean build artifacts: `dotnet clean src/`
- Delete `bin/` and `obj/` folders
- Rebuild: `dotnet build src/`
- Retry tests: `dotnet test src/`

**Missing dependencies:**
- Run `dotnet restore src/` to fetch NuGet packages
- Check internet connectivity if restore fails

**Still stuck?** Open a [Discussion](https://github.com/elbruno/mempalacenet/discussions) with your error message and we'll help!

---

## Development Workflow

### 1. Create a Feature Branch

Work on a dedicated branch based on `main`:

```bash
git checkout main
git pull origin main
git checkout -b feature/your-feature-name
```

Branch naming conventions:
- `feature/` — New features (e.g., `feature/bm25-search`)
- `fix/` — Bug fixes (e.g., `fix/embedding-null-check`)
- `docs/` — Documentation updates (e.g., `docs/contributing-guide`)
- `test/` — Test improvements (e.g., `test/search-coverage`)

### 2. Make Your Changes

- Follow the [Code Style & Standards](#code-style--standards)
- Write or update tests to cover your changes
- Keep commits focused and atomic
- Test locally before pushing

### 3. Ensure Quality

Before pushing, run these checks:

```bash
# Build the solution
dotnet build src/

# Run all tests
dotnet test src/

# (Optional) Run benchmarks if performance-critical
dotnet run --project src/MemPalace.Benchmarks/
```

All tests must pass before submitting a PR.

### 4. Commit Your Changes

Follow the [Commit Message Format](#commit-message-format):

```bash
git add .
git commit -m "feat(search): add BM25 keyword scoring"
```

### 5. Push and Open a Pull Request

```bash
git push origin feature/your-feature-name
```

Then open a PR on GitHub:
- Use the [Pull Request Template](.github/pull_request_template.md)
- Link related issues (e.g., `Closes #42`)
- Provide a clear description of changes
- Request review from maintainers

---

## Code Style & Standards

We follow standard .NET conventions to keep the codebase consistent and maintainable.

### Naming Conventions

- **PascalCase** for public APIs (classes, methods, properties)
  ```csharp
  public class PalaceBackend
  {
      public Task<QueryResult> SearchAsync(string query) { }
  }
  ```

- **camelCase** for local variables and private fields
  ```csharp
  private readonly IEmbedder _embedder;
  var searchResults = await collection.QueryAsync(embedding);
  ```

- **Interfaces** prefixed with `I` (e.g., `IBackend`, `IEmbedder`)

### Documentation

- **XML doc comments** for all public types and members:
  ```csharp
  /// <summary>
  /// Embeds text using the configured embedder and searches the collection.
  /// </summary>
  /// <param name="query">The text to search for.</param>
  /// <returns>A list of matching records ranked by similarity.</returns>
  public async Task<List<QueryResult>> SearchAsync(string query) { }
  ```

### Async Patterns

- Use `async`/`await` for I/O-bound operations
- Suffix async methods with `Async` (e.g., `AddAsync`, `QueryAsync`)
- Use `Task` or `Task<T>` return types
- Avoid blocking calls (no `.Result` or `.Wait()`)
- Use `ConfigureAwait(false)` in library code when safe

### Dependencies

- **Minimize external dependencies** — evaluate necessity before adding NuGet packages
- **Prefer Microsoft.Extensions.*** — use M.E.AI, M.E.DependencyInjection, M.E.Logging
- **Discuss first** — open an issue to propose new dependencies before submitting a PR

### Code Organization

- **Keep methods focused** — single responsibility, <50 lines when possible
- **Avoid deep nesting** — use guard clauses and early returns
- **Make code testable** — inject dependencies, avoid static state
- **Respect abstractions** — depend on interfaces (`IBackend`, `IEmbedder`), not concrete types

---

## Testing Requirements

All code contributions must include tests. We use **xUnit** for unit and integration tests.

### Test Structure

Tests live in `src/MemPalace.Tests/` organized by component:

```
MemPalace.Tests/
├── Ai/            # Embedder tests
├── Backends/      # Backend conformance and SQLite tests
├── Cli/           # CLI command tests
├── KnowledgeGraph/
├── Mcp/
├── Mining/
├── Search/
└── ...
```

### Writing Tests

- **Unit tests** for isolated logic (e.g., embedder wrappers, search ranking)
- **Integration tests** for backend operations (e.g., add, query, delete)
- **Test both happy path and edge cases**:
  - Valid input → expected output
  - Invalid input → appropriate exceptions
  - Boundary conditions (empty collections, null values)

Example test:

```csharp
[Fact]
public async Task AddAsync_ValidRecords_StoresSuccessfully()
{
    // Arrange
    var backend = CreateBackend();
    var collection = await backend.GetCollectionAsync(palaceRef, "test", create: true, embedder);
    var records = new[] { new EmbeddedRecord("id1", "doc1", new[] { 1f, 0f }) };

    // Act
    await collection.AddAsync(records);

    // Assert
    var result = await collection.GetAsync(ids: new[] { "id1" });
    Assert.Single(result.Documents);
    Assert.Equal("doc1", result.Documents[0]);
}
```

### Test Naming

- Use descriptive names: `MethodName_Scenario_ExpectedBehavior`
- Examples:
  - `QueryAsync_EmptyCollection_ReturnsEmptyResults`
  - `EmbedAsync_NullText_ThrowsArgumentNullException`

### Running Tests

```bash
# Run all tests
dotnet test src/

# Run specific test project
dotnet test src/MemPalace.Tests/

# Run tests with coverage (if configured)
dotnet test src/ --collect:"XPlat Code Coverage"
```

### Coverage Goals

- Aim for **>80% coverage** on new code
- Focus on critical paths (backend operations, search, embedding)
- Don't sacrifice test quality for coverage numbers

---

## Documentation

Documentation is as important as code. Keep it up to date with your changes.

### When to Update Docs

- **User-facing changes** → Update `README.md`
- **New features** → Add pages to `docs/` (e.g., `docs/search.md`)
- **API changes** → Update XML comments and relevant docs
- **Breaking changes** → Document migration path in `CHANGELOG.md`

### Documentation Structure

- **README.md** — Quick start, feature overview, installation
- **docs/architecture.md** — Project layout, component contracts
- **docs/{feature}.md** — Deep dives (e.g., `mining.md`, `mcp.md`)
- **.github/CONTRIBUTING.md** — This guide
- **.github/DEVELOPMENT.md** — Developer reference

### Examples

If your PR changes how a feature is used, update or add examples:

```bash
# Before
mempalacenet search "query"

# After (added --hybrid flag)
mempalacenet search "query" --hybrid
```

---

## Commit Message Format

We use **Conventional Commits** for clear, consistent history.

### Format

```
{type}({scope}): {message}

[optional body]

[optional footer(s)]
```

### Types

- `feat` — New feature
- `fix` — Bug fix
- `docs` — Documentation only
- `style` — Code formatting (no logic change)
- `refactor` — Code restructuring (no behavior change)
- `test` — Add or update tests
- `perf` — Performance improvement
- `chore` — Maintenance (deps, build scripts)

### Scopes

Use the component name:
- `agents`, `ai`, `backends`, `cli`, `core`, `kg`, `mcp`, `mining`, `search`, `tests`

### Examples

```bash
feat(search): add BM25 keyword scoring
fix(backends): handle null metadata in SQLite queries
docs(contributing): add testing guidelines
test(search): improve hybrid search coverage
refactor(ai): simplify embedder factory
perf(backends): optimize cosine similarity calculation
chore(deps): update Microsoft.Extensions.AI to 1.0.0
```

### Breaking Changes

If your commit introduces breaking changes, add `BREAKING CHANGE:` in the footer:

```
feat(backends): require embedder identity on collection creation

BREAKING CHANGE: `GetCollectionAsync` now requires an `embedder` parameter.
Migration: Pass your embedder instance when opening collections.
```

---

## Pull Request Process

### Before Submitting

1. **Self-review** — Read through your changes as if reviewing someone else's code
2. **Run tests** — Ensure `dotnet test src/` passes
3. **Update docs** — Reflect changes in relevant documentation
4. **Squash if needed** — Clean up messy commit history (optional, we can squash on merge)

### PR Template

Use the [Pull Request Template](.github/pull_request_template.md) to describe:

- **What changed** — Brief summary
- **Why** — Motivation and context
- **How** — Implementation notes (if complex)
- **Testing** — How you verified the changes
- **Checklist** — Tests pass, docs updated, etc.

### Linking Issues

Reference related issues in your PR description:

```markdown
Closes #42
Fixes #58
Related to #67
```

### Review Process

- Maintainers will review your PR and provide feedback
- **Be responsive** — Address comments and questions
- **Iterate respectfully** — Code review is collaborative, not adversarial
- **CI must pass** — PRs cannot merge with failing tests

### Merging

Once approved and CI passes, a maintainer will merge your PR. Thank you for contributing!

---

## Issue Reporting

Found a bug or have a feature request? We'd love to hear about it!

### Bug Reports

Use the [Bug Report Template](.github/ISSUE_TEMPLATE/bug_report.md):

- **Describe the bug** — What happened vs. what you expected
- **Steps to reproduce** — Minimal, reproducible example
- **Environment** — OS, .NET version, MemPalace.NET version
- **Logs/screenshots** — Any relevant output

### Feature Requests

Use the [Feature Request Template](.github/ISSUE_TEMPLATE/feature_request.md):

- **Describe the feature** — What problem does it solve?
- **Use case** — How would you use it?
- **Alternatives considered** — Other approaches you've tried

### Search First

Before opening a new issue, search [existing issues](https://github.com/elbruno/mempalacenet/issues) to avoid duplicates.

---

## Questions?

- **General questions** — [GitHub Discussions](https://github.com/elbruno/mempalacenet/discussions)
- **Bug reports or feature requests** — [GitHub Issues](https://github.com/elbruno/mempalacenet/issues)
- **Security concerns** — See [SECURITY.md](.github/SECURITY.md)
- **Contact the maintainer** — [@elbruno](https://github.com/elbruno)

---

## Thank You!

Your contributions make MemPalace.NET better for everyone. Whether you're fixing a typo, adding a feature, or helping others in Discussions, we appreciate your time and effort.

Happy coding! 🚀
