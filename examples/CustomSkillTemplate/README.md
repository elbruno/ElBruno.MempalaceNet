# Custom Skill Template

A complete, working template for creating **MemPalace.NET Skills** — reusable packages that extend the platform with new CLI commands, mining strategies, and search patterns.

This template demonstrates best practices for:

✅ Project structure and layout  
✅ Manifest file configuration  
✅ Dependency injection setup (Spectre.Console.Cli)  
✅ CLI command implementation  
✅ Configuration management  
✅ Rich console output with Spectre.Console  

---

## Quick Start

### 1. Clone or Copy This Template

```bash
# Navigate to examples directory
cd examples/CustomSkillTemplate

# Or copy to a new location
cp -r CustomSkillTemplate my-skill
cd my-skill
```

### 2. Build and Run

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run the template
dotnet run -- custom-skill "your query here"

# Get help
dotnet run -- custom-skill --help
```

### 3. Customize for Your Skill

Edit the following files:

| File | What to Change |
|------|----------------|
| `skill.manifest.json` | Skill metadata (name, version, tags, dependencies) |
| `appsettings.json` | Configuration options |
| `src/CustomSkillService.cs` | Your actual business logic |
| `src/CustomSkillCommand.cs` | CLI command behavior |
| `Program.cs` | DI registration (if needed) |
| `CustomSkillTemplate.csproj` | NuGet package references |
| `README.md` | Documentation for your skill |

---

## Project Structure

```
CustomSkillTemplate/
├── skill.manifest.json              # Skill metadata (required for discovery)
├── appsettings.json                 # Configuration defaults
├── CustomSkillTemplate.csproj       # .NET project file with dependencies
├── Program.cs                       # Entry point & DI setup
├── src/
│   ├── CustomSkillCommand.cs        # CLI command (Spectre.Console.Cli)
│   └── CustomSkillService.cs        # Business logic & service interface
└── README.md                        # This file
```

---

## Key Files Explained

### skill.manifest.json

Metadata that MemPalace CLI uses for discovery and installation:

```json
{
  "id": "custom-skill",              // Unique identifier (kebab-case)
  "name": "Custom Skill Template",   // Display name
  "version": "1.0.0",                // Semantic version
  "description": "...",              // Brief description
  "author": "Your Name",             // Author/organization
  "license": "MIT",                  // SPDX license
  "tags": ["template", "example"],   // Search tags
  "discoverable": true,              // Show in marketplace
  "dependencies": [                  // Required packages
    { "name": "mempalacenet", "version": ">=0.7.0" }
  ],
  "commands": [                      // CLI commands provided
    { "name": "custom-skill", "description": "..." }
  ]
}
```

### Program.cs

Sets up dependency injection and registers CLI commands:

```csharp
// Build configuration
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

// Register services
var services = new ServiceCollection();
services.AddScoped<ICustomSkillService, CustomSkillService>();

// Create CLI app with DI support
var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.AddCommand<CustomSkillCommand>("custom-skill");
});
```

### CustomSkillCommand.cs

Implements the CLI command using Spectre.Console:

```csharp
[Description("Execute custom skill with semantic search")]
public class CustomSkillCommand : AsyncCommand<CustomSkillSettings>
{
    private readonly ICustomSkillService _skillService;

    public override async Task<int> ExecuteAsync(CommandContext ctx, CustomSkillSettings settings)
    {
        var result = await _skillService.ExecuteAsync(settings.Query, settings.Wing, settings.Limit);
        DisplayResults(result);
        return 0;
    }
}
```

### CustomSkillService.cs

Contains the actual business logic:

```csharp
public class CustomSkillService : ICustomSkillService
{
    public async Task<CustomSkillResult> ExecuteAsync(string query, string? wing = null, int limit = 10)
    {
        // Replace with actual Palace integration:
        // var results = await palace.Search(query, wing: wing, limit: limit);
        // return MapResults(results);
    }
}
```

---

## Usage Examples

### Basic Query

```bash
dotnet run -- custom-skill "semantic search"
```

**Output:**
```
Custom Skill executing query: "semantic search"

┌──────────┬─────────────────────────────────────────┬───────────┐
│ Score    │ Content                                 │ Wing      │
├──────────┼─────────────────────────────────────────┼───────────┤
│ 0.950    │ Mock result 1 for query: semantic se... │ default   │
│ 0.870    │ Mock result 2 for query: semantic se... │ default   │
└──────────┴─────────────────────────────────────────┴───────────┘

Tip: Use --wing to limit results to a specific wing
```

### With Options

```bash
dotnet run -- custom-skill "kubernetes" --wing ops --limit 5
```

### Help

```bash
dotnet run -- custom-skill --help
```

---

## How to Adapt This Template

### 1. Rename the Skill

```bash
# Update project name in .csproj
sed -i 's/CustomSkillTemplate/MySkill/g' CustomSkillTemplate.csproj

# Rename classes in source files
# - CustomSkillCommand → MySkillCommand
# - CustomSkillService → MySkillService
# - CustomSkillSettings → MySkillSettings
```

### 2. Update Manifest

```json
{
  "id": "my-skill",
  "name": "My Custom Skill",
  "tags": ["my-tag", "feature"],
  "dependencies": [
    { "name": "mempalacenet", "version": ">=0.7.0" },
    { "name": "MyCustomPackage", "version": ">=1.0.0" }
  ]
}
```

### 3. Integrate with Palace

Replace mock implementation in `CustomSkillService.cs`:

```csharp
public class CustomSkillService : ICustomSkillService
{
    private readonly Palace _palace;
    private readonly IConfiguration _config;

    public CustomSkillService(Palace palace, IConfiguration config)
    {
        _palace = palace;
        _config = config;
    }

    public async Task<CustomSkillResult> ExecuteAsync(string query, string? wing = null, int limit = 10)
    {
        // Real Palace integration
        var searchResults = await _palace.Search(
            query: query,
            wing: wing ?? _config.GetValue("custom-skill:default-wing", "default"),
            limit: limit
        );

        return new CustomSkillResult
        {
            Query = query,
            Wing = wing ?? "default",
            Timestamp = DateTime.UtcNow,
            Items = searchResults
                .Select(r => new CustomSkillItem
                {
                    Score = r.Score,
                    Content = r.Memory.Content,
                    Metadata = r.Memory.Metadata
                })
                .ToArray()
        };
    }
}
```

### 4. Add More Commands

```csharp
// In Program.cs
app.Configure(config =>
{
    config
        .AddCommand<MySkillCommand>("my-skill")
        .AddCommand<MySkillAnalyzeCommand>("my-skill-analyze")
        .AddCommand<MySkillExportCommand>("my-skill-export");
});
```

---

## Testing Locally

### Build and Test

```bash
dotnet build -c Release
dotnet test
```

### Package as NuGet Tool

```bash
dotnet pack -c Release

# Install locally
dotnet tool install --add-source ./bin/Release my-skill --version 1.0.0

# Run globally
my-skill "your query"
```

### Publish to NuGet

```bash
dotnet nuget push ./bin/Release/MySkill.1.0.0.nupkg --api-key YOUR_NUGET_KEY
```

---

## Next Steps

### Option 1: Extend with More Features

Add more service methods to `ICustomSkillService`:

```csharp
public interface ICustomSkillService
{
    Task<CustomSkillResult> ExecuteAsync(...);
    Task<AnalysisResult> AnalyzeAsync(...);
    Task ExportResultsAsync(...);
}
```

### Option 2: Publish to Skill Marketplace

Follow the publishing guide: [docs/guides/skill-integration-deep-dive.md](../../docs/guides/skill-integration-deep-dive.md)

### Option 3: Integrate with Agent Framework

Use Microsoft.Agents.AI to turn your skill into an intelligent agent:

```csharp
var skillAgent = new MemPalaceAgent(_palace, skillService);
var response = await skillAgent.ExecuteAsync("user message");
```

---

## Configuration

Edit `appsettings.json` to customize default behavior:

```json
{
  "custom-skill": {
    "enabled": true,
    "result-limit": 10,
    "default-wing": "default",
    "logging": {
      "log-level": "Information"
    }
  }
}
```

Override via environment variables:

```bash
export CUSTOM_SKILL_RESULT_LIMIT=20
export CUSTOM_SKILL_DEFAULT_WING=work
dotnet run -- custom-skill "query"
```

---

## Common Patterns

### Pattern: RAG Context Injection

```csharp
public async Task<string> AnswerWithContext(string question)
{
    var context = await _skillService.ExecuteAsync(question, "docs");
    var prompt = $"Context: {string.Join("\n", context.Items.Select(i => i.Content))}\n\nQuestion: {question}";
    return await _llm.CompleteAsync(prompt);
}
```

### Pattern: Agent Memory Diary

```csharp
public async Task RecordInteraction(string userMsg, string agentResp)
{
    var content = $"User: {userMsg}\nAgent: {agentResp}";
    await _skillService.ExecuteAsync(content, "agents/my-agent");
}
```

### Pattern: Async Processing with Status

```csharp
public async Task<CustomSkillResult> ExecuteWithProgressAsync(string query, IProgress<string> progress)
{
    progress?.Report("Starting...");
    var result = await _skillService.ExecuteAsync(query);
    progress?.Report($"Found {result.Items.Length} results");
    return result;
}
```

---

## Troubleshooting

### Build Fails: Package Not Found

**Problem:** `error NU1101: Unable to find package MemPalace.Core`

**Solution:** Ensure NuGet is configured:
```bash
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
dotnet restore
```

### Command Not Recognized

**Problem:** `dotnet run -- custom-skill` shows "Unknown command"

**Solution:** Verify command is registered in `Program.cs`:
```csharp
app.Configure(config =>
{
    config.AddCommand<CustomSkillCommand>("custom-skill");
});
```

### Configuration Not Loading

**Problem:** `appsettings.json` changes have no effect

**Solution:** Ensure file is copied to output:
```xml
<!-- In .csproj -->
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## Links & Resources

- **Deep Dive Guide:** [docs/guides/skill-integration-deep-dive.md](../../docs/guides/skill-integration-deep-dive.md)
- **Pattern Library:** [docs/SKILL_PATTERNS.md](../../docs/SKILL_PATTERNS.md)
- **CLI Reference:** [docs/cli.md](../../docs/cli.md)
- **Architecture:** [docs/architecture.md](../../docs/architecture.md)
- **Spectre.Console Docs:** https://spectreconsole.net/cli

---

## License

MIT License — see [LICENSE](../../LICENSE) for details.

---

**Ready to create your own skill?** Start editing the files above and follow the patterns demonstrated in this template.

Built with ❤️ as part of MemPalace.NET
