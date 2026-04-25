# Semantic Knowledge Graph Example

A console application demonstrating MemPalace.NET's temporal knowledge graph capabilities.

## What This Example Shows

This example demonstrates building and querying a knowledge graph from documents:

1. **Entity Extraction** — Extract people, projects, teams from text
2. **Relationship Tracking** — Build triples (subject-predicate-object)
3. **Temporal Validity** — Track when relationships were true
4. **Pattern Queries** — Find entities matching specific patterns
5. **Entity Timelines** — View history of an entity's relationships

## Key Concepts Demonstrated

- **Temporal Triples** — Relationships with validity periods (ValidFrom, ValidTo)
- **Entity References** — Typed entities (person:alice, project:phoenix)
- **Pattern Matching** — Query by subject, predicate, or object
- **Timeline Queries** — Historical view of entity relationships
- **Graph Statistics** — Count entities and relationships

## Running the Example

```bash
# Navigate to the example directory
cd examples/SemanticKnowledgeGraph

# Run the example
dotnet run
```

## Expected Output

You should see:

```
🕸️  MemPalace.NET - Semantic Knowledge Graph Example

This example demonstrates:
  ✓ Building a knowledge graph from sample documents
  ✓ Extracting entities and relationships
  ✓ Temporal validity (what was true when)
  ✓ Querying the graph by patterns
  ✓ Entity timelines

📦 Step 1: Initializing knowledge graph...
✓ Created knowledge graph at /tmp/mempalace-kg-abc123.db

📄 Step 2: Processing sample documents...
✓ Loaded 4 documents

🔍 Step 3: Extracting entities and relationships...
  • team-update-jan: 5 relationships found
  • team-update-feb: 4 relationships found
  • team-update-mar: 6 relationships found
  • project-status: 3 relationships found
✓ Inserted 18 triples into the graph

🔎 Step 4: Querying the knowledge graph...

Query: What projects did Alice work on?
  • Alice works on project:phoenix
    Valid from: 2025-01-01
  • Alice works on project:quantum
    Valid from: 2025-03-01
...
```

## Code Structure

- **Program.cs** (~250 lines)
  - Knowledge graph initialization
  - Document loading from markdown files
  - Entity and relationship extraction
  - Pattern-based queries
  - Temporal queries
  - Timeline visualization
  - Graph statistics

- **SimpleEntityExtractor** (~100 lines)
  - Regex-based entity extraction
  - Relationship pattern matching
  - Temporal triple creation

- **SampleDocs/** (4 markdown files)
  - Team update documents
  - Project status reports
  - Demonstrating temporal changes

## Sample Documents

The example includes 4 markdown files that tell a story:

- **team-update-jan.md** — Alice joins, works on Phoenix
- **team-update-feb.md** — Diana joins, assists Alice
- **team-update-mar.md** — Alice moves to Quantum, Bob promoted
- **project-status.md** — Final project statuses

These documents contain temporal information that the knowledge graph captures:
- Who worked on which project when
- Manager relationships over time
- Role changes and promotions

## Queries Demonstrated

1. **Find all projects a person worked on**
   ```csharp
   await kg.QueryAsync(new TriplePattern(
       Subject: new EntityRef("person", "alice"),
       Predicate: "works-on",
       Object: null));
   ```

2. **Find all team members**
   ```csharp
   await kg.QueryAsync(new TriplePattern(
       Subject: null,
       Predicate: "member-of",
       Object: new EntityRef("team", "engineering")));
   ```

3. **Temporal query (as of specific date)**
   ```csharp
   await kg.QueryAsync(
       new TriplePattern(...),
       at: new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero));
   ```

4. **Entity timeline**
   ```csharp
   await kg.TimelineAsync(new EntityRef("person", "alice"));
   ```

## Entity Extraction

The example uses simple regex patterns to extract entities and relationships:

- **"X joined the Y team"** → (person:X, member-of, team:Y)
- **"X manages Y"** → (person:X, manages, person:Y)
- **"X working on Project Y"** → (person:X, works-on, project:Y)
- **"X as a/the Y"** → (person:X, has-role, role:Y)

In production, you'd use:
- NLP libraries (spaCy, Stanford CoreNLP)
- LLMs for entity extraction (OpenAI, Ollama)
- Custom domain-specific extractors

## Next Steps

After understanding this example, try:

1. **Combine with Semantic Search**
   ```csharp
   // Search memories, then enrich with knowledge graph context
   var searchResults = await searchService.SearchAsync("Alice's work");
   var alice = new EntityRef("person", "alice");
   var context = await kg.TimelineAsync(alice);
   ```

2. **Track Relationship Changes**
   ```csharp
   // End validity of old relationships
   await kg.EndValidityAsync(oldTripleId, DateTime.UtcNow);
   
   // Add new relationships
   await kg.AddAsync(new TemporalTriple(...));
   ```

3. **Build Agent Memory Diaries**
   ```csharp
   // Track what an agent learned, did, or observed
   await kg.AddAsync(new TemporalTriple(
       new Triple(
           new EntityRef("agent", "deckard"),
           "learned",
           new EntityRef("concept", "temporal-graphs"),
           null),
       DateTime.UtcNow,
       null,
       DateTime.UtcNow));
   ```

4. **Query Historical State**
   ```csharp
   // What was true 6 months ago?
   var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
   var historicalState = await kg.QueryAsync(pattern, at: sixMonthsAgo);
   ```

## What You'll Learn

- How to model temporal relationships with validity windows
- Pattern matching for flexible graph queries
- Entity timelines for historical context
- Practical entity extraction from documents
- Graph-based knowledge organization

## Dependencies

- MemPalace.Core
- MemPalace.Backends.Sqlite
- MemPalace.Ai
- MemPalace.KnowledgeGraph
- MemPalace.Search

All packages are available on NuGet at version `0.1.0-preview.1`.

## Production Considerations

For production use:

1. **Better Entity Extraction**
   - Use NLP libraries or LLM-based extraction
   - Handle coreference resolution
   - Normalize entity names

2. **Relationship Updates**
   - Implement proper validity ending for changed relationships
   - Track sources/provenance of triples
   - Handle conflicting information

3. **Scale**
   - Consider Qdrant/Weaviate for large graphs
   - Add indexing for common query patterns
   - Implement graph summarization

4. **Integration**
   - Combine with semantic search for hybrid retrieval
   - Use for agent context enrichment
   - Build recommendation systems
