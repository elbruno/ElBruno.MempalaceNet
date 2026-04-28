# Skill Integration Deep Dive Guide

**Version:** v0.7.0+  
**Status:** Reference Guide  
**Audience:** Skill Developers, MemPalace.NET Contributors

---

## Table of Contents

1. [What is a MemPalace.NET Skill?](#what-is-a-mempalacenet-skill)
2. [Skill Anatomy](#skill-anatomy)
3. [Manifest Schema Reference](#manifest-schema-reference)
4. [Creating a Custom Skill](#creating-a-custom-skill)
5. [Publishing to Skill Marketplace](#publishing-to-skill-marketplace)
6. [Real Example](#real-example)

---

## What is a MemPalace.NET Skill?

A **MemPalace.NET Skill** is a reusable, self-contained package that extends the MemPalace.NET platform with:

- **New CLI commands** (via Spectre.Console.Cli integration)
- **Custom memory operations** (mining, search, transformation)
- **Domain-specific patterns** (RAG, agent memory, knowledge graphs)
- **Pre-built templates** (reducing boilerplate for common tasks)

### Use Cases

- **RAG Context Injection:** Retrieve relevant memory for LLM prompts
- **Agent Diaries:** Persistent multi-turn conversation state
- **Knowledge Graph Queries:** Temporal entity relationship tracking
- **Custom Miners:** Domain-specific file/data extraction
- **Search Strategies:** Hybrid, reranked, or filtered search patterns

### Examples of Skills

| Skill ID | Purpose | Use Case |
|----------|---------|----------|
| `rag-context-injector` | Semantic search + LLM injection | Documentation Q&A |
| `agent-diary` | Agent memory diary management | Conversational AI |
| `kg-temporal-queries` | Knowledge graph time-slicing | Organizational charts |
| `hybrid-search-reranking` | Hybrid + LLM reranking | High-precision retrieval |

---

## Skill Anatomy

### Project Structure

```
your-skill/
├── skill.manifest.json          # Skill metadata (required)
├── appsettings.json            # Configuration template
├── README.md                   # User documentation
├── YourSkill.csproj            # .NET project file
├── Program.cs                  # Entry point & DI setup
├── src/
│   ├── SkillCommand.cs         # CLI command implementation
│   ├── SkillService.cs         # Business logic
│   └── SkillOptions.cs         # Command options
└── tests/
    ├── SkillCommandTests.cs    # Unit tests
    └── SkillServiceTests.cs    # Integration tests
```

### Entry Point (Program.cs)

The entry point registers your skill with the MemPalace CLI:

```csharp
using Spectre.Console.Cli;
using YourSkill;

var app = new CommandApp();
var registrar = new TypeRegistrar(services);

app.Configure(config =>
{
    config
        .AddCommand<YourSkillCommand>("your-skill")
        .WithDescription("Brief description of what your skill does");
});

app.Run(args);
```

**Key concepts:**
- Use `TypeRegistrar` for dependency injection
- Register commands with descriptive names
- Follow Spectre.Console.Cli conventions

### Manifest File (skill.manifest.json)

The manifest tells MemPalace how to discover and configure your skill:

```json
{
  "id": "your-skill-id",
  "name": "Your Skill Name",
  "version": "1.0.0",
  "description": "What your skill does",
  "author": "Your Name",
  "license": "MIT",
  "repository": "https://github.com/yourusername/your-skill",
  "documentation": "https://docs.yourdomain.com/your-skill",
  "tags": ["tag1", "tag2", "tag3"],
  "discoverable": true,
  "dependencies": [
    {
      "name": "mempalacenet",
      "version": ">=0.7.0"
    },
    {
      "name": "Microsoft.Extensions.AI",
      "version": ">=10.3.0"
    }
  ],
  "commands": [
    {
      "name": "your-skill-main",
      "description": "Main command for your skill"
    }
  ],
  "configuration": {
    "schema": "appsettings.json",
    "required": ["your-skill:enabled"],
    "optional": ["your-skill:loglevel"]
  }
}
```

### Configuration (appsettings.json)

Provides runtime configuration for your skill:

```json
{
  "your-skill": {
    "enabled": true,
    "cache-results": true,
    "batch-size": 100,
    "timeout-seconds": 30,
    "logging": {
      "log-level": "Information"
    }
  }
}
```

**Best practices:**
- Use namespaced keys (`your-skill:*`)
- Provide sensible defaults
- Document all options in README

### Dependencies

Every skill needs core MemPalace packages:

```xml
<ItemGroup>
  <!-- Core -->
  <PackageReference Include="MemPalace.Core" Version="0.7.0" />
  <PackageReference Include="MemPalace.Backends.Sqlite" Version="0.7.0" />
  
  <!-- AI integration -->
  <PackageReference Include="MemPalace.Ai" Version="0.7.0" />
  <PackageReference Include="Microsoft.Extensions.AI" Version="10.3.0" />
  
  <!-- CLI framework -->
  <PackageReference Include="Spectre.Console.Cli" Version="0.51.0" />
  <PackageReference Include="Spectre.Console" Version="0.51.0" />
  
  <!-- Optional: Mining, Search, Agents -->
  <PackageReference Include="MemPalace.Mining" Version="0.7.0" />
  <PackageReference Include="MemPalace.Search" Version="0.7.0" />
  <PackageReference Include="MemPalace.Agents" Version="0.7.0" />
</ItemGroup>
```

---

## Manifest Schema Reference

### Top-Level Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | Yes | Unique skill identifier (kebab-case, 3-50 chars) |
| `name` | string | Yes | Display name (1-100 chars) |
| `version` | string | Yes | Semantic version (e.g., "1.0.0") |
| `description` | string | Yes | Brief skill description (1-300 chars) |
| `author` | string | Yes | Skill creator name or organization |
| `license` | string | Yes | SPDX license identifier (e.g., "MIT") |
| `repository` | string | Yes | GitHub repo URL |
| `documentation` | string | No | Documentation URL |
| `tags` | string[] | No | Skill categories (3-10 tags) |
| `discoverable` | boolean | No | Whether to show in skill discovery (default: true) |

### Dependencies Array

Each dependency specifies required packages:

```json
{
  "name": "mempalacenet",
  "version": ">=0.7.0"   // Semver range
}
```

**Common dependency versions:**
- MemPalace.Core: `>=0.7.0`
- Microsoft.Extensions.AI: `>=10.3.0`
- Spectre.Console.Cli: `>=0.51.0`

### Commands Array

Declares CLI commands provided by the skill:

```json
{
  "name": "rag-inject",
  "description": "Inject semantic context into LLM prompts"
}
```

### Configuration Object

Defines settings schema and validation:

```json
{
  "schema": "appsettings.json",
  "required": ["your-skill:enabled", "your-skill:api-key"],
  "optional": ["your-skill:cache-size"]
}
```

---

## Creating a Custom Skill

### Step 1: Initialize Project Structure

```bash
# Clone the template
git clone https://github.com/elbruno/mempalacenet.git
cd mempalacenet/examples/CustomSkillTemplate

# Or create manually
mkdir my-skill
cd my-skill
dotnet new console -n MySkill
```

### Step 2: Create the Manifest

Create `skill.manifest.json` at project root:

```json
{
  "id": "my-skill",
  "name": "My Custom Skill",
  "version": "1.0.0",
  "description": "Does something useful with MemPalace memories",
  "author": "Your Name",
  "license": "MIT",
  "repository": "https://github.com/yourusername/my-skill",
  "tags": ["custom", "example"],
  "discoverable": true,
  "dependencies": [
    { "name": "mempalacenet", "version": ">=0.7.0" },
    { "name": "Microsoft.Extensions.AI", "version": ">=10.3.0" }
  ],
  "commands": [
    { "name": "my-skill-run", "description": "Run the skill" }
  ]
}
```

### Step 3: Implement the CLI Command

Create `src/MySkillCommand.cs`:

```csharp
using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using MemPalace;

namespace MySkill;

[Description("Run My Skill")]
public class MySkillCommand : AsyncCommand<MySkillSettings>
{
    private readonly Palace _palace;

    public MySkillCommand(Palace palace)
    {
        _palace = palace;
    }

    public override async Task<int> ExecuteAsync(CommandContext ctx, MySkillSettings settings)
    {
        try
        {
            AnsiConsole.MarkupLine("[bold cyan]My Skill[/] is running...");
            
            // Your skill logic here
            var results = await _palace.Search(
                query: settings.Query,
                wing: settings.Wing,
                limit: settings.Limit
            );

            AnsiConsole.MarkupLine($"Found [green]{results.Count}[/] memories");
            foreach (var result in results)
            {
                AnsiConsole.MarkupLine($"  [dim]Score:[/] {result.Score:F3}");
                AnsiConsole.MarkupLine($"  [dim]Content:[/] {result.Memory.Content.Substring(0, 50)}...");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}

public class MySkillSettings : CommandSettings
{
    [CommandArgument(0, "[QUERY]")]
    [Description("Search query")]
    public string Query { get; set; } = string.Empty;

    [CommandOption("--wing <WING>")]
    [Description("Target wing")]
    public string Wing { get; set; } = "default";

    [CommandOption("--limit <N>")]
    [Description("Result limit")]
    public int Limit { get; set; } = 10;
}
```

### Step 4: Wire Up Dependency Injection

Create `Program.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using MySkill;
using MemPalace;

var services = new ServiceCollection();

// Register MemPalace
services.AddScoped<Palace>(sp => 
    Palace.Create("./my-palace").Result
);

// Register your command
services.AddScoped<MySkillCommand>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config
        .AddCommand<MySkillCommand>("my-skill-run")
        .WithDescription("Run My Skill");
});

return app.Run(args);

// Spectre.Console.Cli TypeRegistrar helper
public partial class TypeRegistrar : ITypeRegistrar, IDisposable
{
    private readonly IServiceCollection _services;

    public TypeRegistrar(IServiceCollection services)
    {
        _services = services;
    }

    public ITypeResolver Build() => 
        new TypeResolver(_services.BuildServiceProvider());

    public void Register(Type service, Type implementation) =>
        _services.AddScoped(service, implementation);

    public void Register(Type service, Func<IServiceProvider, object> factory) =>
        _services.AddScoped(service, sp => factory(sp));

    public void RegisterInstance(Type service, object instance) =>
        _services.AddScoped(service, _ => instance);

    public void Dispose() { }
}

public class TypeResolver : ITypeResolver, IDisposable
{
    private readonly ServiceProvider _provider;

    public TypeResolver(ServiceProvider provider) => _provider = provider;

    public object? Resolve(Type type) => _provider.GetService(type);

    public void Dispose() => _provider.Dispose();
}
```

### Step 5: Register with MemPalace CLI

Update MemPalace CLI to discover your skill:

**For local development:**
1. Place your skill in `mempalacenet/skills/` directory
2. Update CLI `Program.cs`:

```csharp
// Register custom skill
services.AddScoped<MySkillCommand>();
```

3. Add to CLI config:

```csharp
app.Configure(config =>
{
    // ... existing commands
    config
        .AddCommand<MySkillCommand>("my-skill-run")
        .WithDescription("Run My Skill");
});
```

### Step 6: Test Locally

```bash
# Build the skill
dotnet build

# Run via CLI
dotnet run -- my-skill-run --query "test search" --wing example

# Or package as tool
dotnet pack -c Release
dotnet tool install --add-source ./bin/Release MySkill
my-skill-run --query "test search"
```

---

## Publishing to Skill Marketplace

### Pre-Publication Checklist

- [ ] Manifest file is valid JSON
- [ ] All required fields are populated
- [ ] Version follows semantic versioning
- [ ] License is SPDX-compatible
- [ ] Repository is publicly accessible
- [ ] README documents all commands and options
- [ ] Code builds without errors or warnings
- [ ] All tests pass
- [ ] Dependency versions are pinned to known-good releases
- [ ] Skill is discoverable (`"discoverable": true`)

### Naming Conventions

| Component | Convention | Example |
|-----------|-----------|---------|
| Skill ID | kebab-case | `my-skill-name` |
| NuGet package | PascalCase.Skill.Name | `MySkill.Skill.Name` |
| Main command | kebab-case | `my-skill-main` |
| Tag names | lowercase | `rag`, `agent`, `search` |

### Submission Process (v1.0+)

1. **Submit to MemPalace Skill Registry:**
   ```bash
   mempalacenet skill publish ./my-skill --token <registry-token>
   ```

2. **Wait for approval** (registry maintainers review)

3. **Skill appears in marketplace:**
   ```bash
   mempalacenet skill discover --tag my-tag
   ```

### Deferred (v1.0)

- Remote registry API
- Version constraint resolution
- Automatic dependency fetching
- Skill rating/review system

---

## Real Example

See [examples/CustomSkillTemplate/](../../examples/CustomSkillTemplate/) for a complete working skill template:

- `skill.manifest.json` — Fully populated manifest
- `Program.cs` — DI setup and command registration
- `appsettings.json` — Configuration template
- `README.md` — User documentation
- `.csproj` — Project file with dependencies
- `src/SkillCommand.cs` — Minimal working command implementation

### Quick Test

```bash
cd examples/CustomSkillTemplate
dotnet run -- custom-skill --help
dotnet run -- custom-skill "your query here"
```

---

## Troubleshooting

### Issue: "Cannot find MemPalace packages"

**Solution:** Ensure you have the correct NuGet source configured:
```bash
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

### Issue: "Skill not discovered after install"

**Solution:** Verify manifest is valid JSON and `discoverable` is `true`:
```bash
cat skill.manifest.json | jq .
```

### Issue: "Command doesn't appear in CLI"

**Solution:** Check that skill is registered in CLI `Program.cs`:
```csharp
app.Configure(config =>
{
    config.AddCommand<YourSkillCommand>("your-skill");
});
```

---

## Further Reading

- [Skill Discovery & Marketplace](./skill-discovery.md) — User guide for discovering skills
- [Skill Manifest Schema Reference](./skill-manifest-schema.md) — Detailed manifest documentation
- [SKILL_PATTERNS.md](../SKILL_PATTERNS.md) — Teaching patterns for MemPalace integration
- [CLI Reference](../cli.md) — MemPalace CLI command reference
- [Architecture](../architecture.md) — System design overview

---

## License

MIT License — see [LICENSE](../../LICENSE) for details.
