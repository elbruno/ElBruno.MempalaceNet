# Knowledge Graph (KG)

## Overview

MemPalace.NET's Knowledge Graph tracks relationships between entities over time. Unlike traditional knowledge graphs, ours is **temporal** — every relationship (triple) has validity intervals, allowing you to query the state of the graph at any point in history.

## Conceptual Model

### Entities

Entities are identified by `type:id` pairs. Examples:
- `agent:tyrell` — an agent/person named Tyrell
- `project:MemPalace.Core` — a project
- `session:abc-123` — a Copilot session
- `decision:tech-stack` — a documented decision

### Triples

A triple is a subject-predicate-object relationship:
```
agent:tyrell  worked-on  project:MemPalace.Core
```

### Temporal Validity

Each triple has:
- **ValidFrom**: Start of validity (inclusive)
- **ValidTo**: End of validity (exclusive), null if still valid
- **RecordedAt**: When the triple was recorded

Example: "Tyrell worked on MemPalace.Core from Jan 1 to Jun 1, 2026":
```
Subject: agent:tyrell
Predicate: worked-on
Object: project:MemPalace.Core
ValidFrom: 2026-01-01T00:00:00Z
ValidTo: 2026-06-01T00:00:00Z
RecordedAt: 2026-01-01T10:30:00Z
```

Querying at `2026-03-01` returns this triple. Querying at `2026-12-01` does not.

## SQLite Schema

```sql
CREATE TABLE triples (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  s_type TEXT NOT NULL,              -- Subject type
  s_id TEXT NOT NULL,                -- Subject ID
  predicate TEXT NOT NULL,           -- Relationship type
  o_type TEXT NOT NULL,              -- Object type
  o_id TEXT NOT NULL,                -- Object ID
  props TEXT,                        -- JSON metadata (optional)
  valid_from TEXT NOT NULL,          -- ISO8601 UTC
  valid_to TEXT,                     -- ISO8601 UTC, nullable
  recorded_at TEXT NOT NULL          -- ISO8601 UTC
);

CREATE INDEX idx_triples_subject ON triples(s_type, s_id);
CREATE INDEX idx_triples_object ON triples(o_type, o_id);
CREATE INDEX idx_triples_predicate ON triples(predicate);
CREATE INDEX idx_triples_valid ON triples(valid_from, valid_to);
```

All timestamps stored as ISO8601 strings in UTC for portability and SQLite compatibility.

## Usage Examples

### CLI Commands

#### Add a Relationship

```bash
# Bruno's work session with Roy on Phase 6
mempalacenet kg add agent:roy worked-on phase:Phase6

# With explicit validity period
mempalacenet kg add agent:roy worked-on phase:Phase6 \
  --valid-from 2026-04-24T10:00:00Z \
  --valid-to 2026-04-24T18:00:00Z
```

#### Query

```bash
# Find all agents who worked on Phase 6 (? = wildcard)
mempalacenet kg query "? worked-on phase:Phase6"

# Find everything Roy worked on
mempalacenet kg query "agent:roy worked-on ?"

# Query as of a specific date
mempalacenet kg query "? worked-on project:MemPalace.Core" --at 2026-03-01
```

#### Timeline

```bash
# View all events involving Roy
mempalacenet kg timeline agent:roy

# Filter to specific time range
mempalacenet kg timeline agent:roy --from 2026-04-01 --to 2026-05-01
```

### Programmatic API

```csharp
using MemPalace.KnowledgeGraph;

// Setup (in Program.cs or Startup)
services.AddMemPalaceKnowledgeGraph(o => 
    o.DatabasePath = ".mempalace/kg.db");

// Inject into services
public class MyService
{
    private readonly IKnowledgeGraph _kg;

    public MyService(IKnowledgeGraph kg)
    {
        _kg = kg;
    }

    public async Task RecordWorkSession()
    {
        // Create a triple
        var triple = new Triple(
            new EntityRef("agent", "roy"),
            "worked-on",
            new EntityRef("phase", "Phase6"),
            properties: new Dictionary<string, object?>
            {
                ["hours"] = 4.5,
                ["commit"] = "a1b2c3d"
            }
        );

        // Add with temporal bounds
        var now = DateTimeOffset.UtcNow;
        var temporal = new TemporalTriple(
            triple,
            validFrom: now,
            validTo: null,  // Still ongoing
            recordedAt: now
        );

        var id = await _kg.AddAsync(temporal);

        // Later: end the validity when work session ends
        await _kg.EndValidityAsync(id, DateTimeOffset.UtcNow);
    }

    public async Task QueryRelationships()
    {
        // Wildcard query: who worked on Phase 6?
        var pattern = new TriplePattern(
            Subject: null,  // wildcard
            Predicate: "worked-on",
            Object: new EntityRef("phase", "Phase6")
        );

        var results = await _kg.QueryAsync(pattern);

        foreach (var result in results)
        {
            Console.WriteLine($"{result.Triple.Subject} -> {result.Triple.Object}");
            if (result.Triple.Properties != null)
            {
                Console.WriteLine($"  Properties: {string.Join(", ", result.Triple.Properties)}");
            }
        }
    }

    public async Task ViewTimeline()
    {
        var entity = new EntityRef("agent", "roy");
        var timeline = await _kg.TimelineAsync(entity);

        foreach (var evt in timeline)
        {
            var symbol = evt.Direction == "outgoing" ? "→" : "←";
            Console.WriteLine($"{evt.At:yyyy-MM-dd HH:mm} {symbol} {evt.Predicate} {evt.Other}");
        }
    }
}
```

## Example: Bruno's Session Tracking

When Bruno starts a session with Roy to work on Phase 6:

```csharp
// Record session start
await _kg.AddAsync(new TemporalTriple(
    new Triple(
        new EntityRef("session", "session-123"),
        "worked-on",
        new EntityRef("phase", "Phase6"),
        new Dictionary<string, object?> { ["agent"] = "roy" }
    ),
    validFrom: DateTimeOffset.UtcNow,
    validTo: null,
    recordedAt: DateTimeOffset.UtcNow
));

// Roy integrates Microsoft.Extensions.AI
await _kg.AddAsync(new TemporalTriple(
    new Triple(
        new EntityRef("agent", "roy"),
        "integrated",
        new EntityRef("library", "Microsoft.Extensions.AI"),
        new Dictionary<string, object?> { ["version"] = "10.4.1" }
    ),
    validFrom: DateTimeOffset.UtcNow,
    validTo: null,
    recordedAt: DateTimeOffset.UtcNow
));

// Roy made a decision about default embeddings
await _kg.AddAsync(new TemporalTriple(
    new Triple(
        new EntityRef("agent", "roy"),
        "decided",
        new EntityRef("decision", "local-embeddings-default"),
        new Dictionary<string, object?> {
            ["rationale"] = "Zero-config, privacy-first, ONNX-based"
        }
    ),
    validFrom: DateTimeOffset.UtcNow,
    validTo: null,
    recordedAt: DateTimeOffset.UtcNow
));
```

Later, query what Roy worked on:
```csharp
var pattern = new TriplePattern(
    new EntityRef("agent", "roy"),
    null,  // any predicate
    null   // any object
);

var results = await _kg.QueryAsync(pattern);
// Returns: worked-on, integrated, decided triples
```

## Integration with Copilot Sessions

Future phases will auto-populate the KG from:
1. **Session mining**: Extract entities, relationships from conversation turns
2. **File changes**: `agent:X edited file:Y.cs`
3. **Decisions**: Document architectural choices
4. **Dependencies**: Track when libraries are added/removed
5. **Tests**: Link tests to features

Example auto-generated triples:
```
agent:roy       worked-on       project:MemPalace.KnowledgeGraph
agent:roy       used            library:Microsoft.Data.Sqlite
agent:roy       created         file:SqliteKnowledgeGraph.cs
agent:roy       wrote-test-for  type:SqliteKnowledgeGraph
agent:roy       decided         decision:temporal-kg-sqlite
```

## Query Patterns

| Pattern | Description |
|---------|-------------|
| `? worked-on project:X` | Who worked on project X? |
| `agent:X worked-on ?` | What did agent X work on? |
| `agent:X ? agent:Y` | All relationships between X and Y |
| `? decided ?` | All decision-making events |
| `? used library:?` | Which agents used which libraries? |

## Performance

- **Indexes** on subject, object, predicate, and validity ensure fast queries
- **Bulk inserts** via `AddManyAsync` use transactions for efficiency
- **Temporal queries** leverage ISO8601 string comparisons (efficient in SQLite)

## Future Extensions (not Phase 6)

- **Graph visualization**: Render entity relationships as network diagrams
- **Inference rules**: Derive implicit relationships (e.g., "worked-on project:X" → "familiar-with codebase:X")
- **Change detection**: Alert when relationships become invalid
- **Analytics**: Who collaborates most? What are the knowledge silos?

---

**Phase 6 Implementation Complete**: Core types, SQLite backend, CLI commands, DI registration, tests.
