using MemPalace.Core.Backends;
using MemPalace.Core.Model;
using MemPalace.Core.Backends.InMemory;
using MemPalace.KnowledgeGraph;
using System.Text.RegularExpressions;

namespace SemanticKnowledgeGraph;

/// <summary>
/// Demonstrates building a temporal knowledge graph from documents.
/// Shows entity extraction, relationship tracking, semantic search over entities,
/// and temporal queries (what was true at a specific time).
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🕸️  MemPalace.NET - Semantic Knowledge Graph Example\n");
        Console.WriteLine("This example demonstrates:");
        Console.WriteLine("  ✓ Building a knowledge graph from sample documents");
        Console.WriteLine("  ✓ Extracting entities and relationships");
        Console.WriteLine("  ✓ Temporal validity (what was true when)");
        Console.WriteLine("  ✓ Querying the graph by patterns");
        Console.WriteLine("  ✓ Entity timelines\n");

        // Step 1: Initialize the knowledge graph
        Console.WriteLine("📦 Step 1: Initializing knowledge graph...");
        var kgPath = Path.Combine(Path.GetTempPath(), $"mempalace-kg-{Guid.NewGuid()}.db");
        await using var kg = new SqliteKnowledgeGraph(kgPath);
        Console.WriteLine($"✓ Created knowledge graph at {kgPath}\n");

        // Step 2: Load and process sample documents
        Console.WriteLine("📄 Step 2: Processing sample documents...");
        var sampleDocsPath = Path.Combine(Directory.GetCurrentDirectory(), "SampleDocs");
        
        if (!Directory.Exists(sampleDocsPath))
        {
            Console.WriteLine($"⚠️  Sample docs directory not found at {sampleDocsPath}");
            Console.WriteLine("Creating sample documents...");
            Directory.CreateDirectory(sampleDocsPath);
            await CreateSampleDocumentsAsync(sampleDocsPath);
        }

        var documents = await LoadDocumentsAsync(sampleDocsPath);
        Console.WriteLine($"✓ Loaded {documents.Count} documents\n");

        // Step 3: Extract entities and relationships from documents
        Console.WriteLine("🔍 Step 3: Extracting entities and relationships...");
        var extractor = new SimpleEntityExtractor();
        var allTriples = new List<TemporalTriple>();
        
        foreach (var doc in documents)
        {
            var triples = extractor.ExtractTriples(doc.Content, doc.Timestamp);
            allTriples.AddRange(triples);
            Console.WriteLine($"  • {doc.Name}: {triples.Count} relationships found");
        }

        var inserted = await kg.AddManyAsync(allTriples);
        Console.WriteLine($"✓ Inserted {inserted} triples into the graph\n");

        // Step 4: Query the graph by patterns
        Console.WriteLine("🔎 Step 4: Querying the knowledge graph...\n");

        // Find all projects Alice worked on
        Console.WriteLine("Query: What projects did Alice work on?");
        var aliceProjects = await kg.QueryAsync(
            new TriplePattern(
                Subject: new EntityRef("person", "alice"),
                Predicate: "works-on",
                Object: null));

        foreach (var triple in aliceProjects)
        {
            Console.WriteLine($"  • Alice works on {triple.Triple.Object}");
            Console.WriteLine($"    Valid from: {triple.ValidFrom:yyyy-MM-dd}");
            if (triple.ValidTo.HasValue)
                Console.WriteLine($"    Valid to: {triple.ValidTo:yyyy-MM-dd}");
        }
        Console.WriteLine();

        // Find all team members
        Console.WriteLine("Query: Who are the team members?");
        var teamMembers = await kg.QueryAsync(
            new TriplePattern(
                Subject: null,
                Predicate: "member-of",
                Object: new EntityRef("team", "engineering")));

        var members = teamMembers.Select(t => t.Triple.Subject.Id).Distinct();
        foreach (var member in members)
        {
            Console.WriteLine($"  • {member}");
        }
        Console.WriteLine();

        // Find Alice's manager
        Console.WriteLine("Query: Who manages Alice?");
        var aliceManager = await kg.QueryAsync(
            new TriplePattern(
                Subject: null,
                Predicate: "manages",
                Object: new EntityRef("person", "alice")));

        foreach (var triple in aliceManager)
        {
            Console.WriteLine($"  • {triple.Triple.Subject.Id} manages Alice");
        }
        Console.WriteLine();

        // Step 5: Temporal queries (what was true at a specific time)
        Console.WriteLine("🕐 Step 5: Temporal queries...\n");

        var march1 = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);
        Console.WriteLine($"Query: What projects was Alice working on as of {march1:yyyy-MM-dd}?");
        var aliceProjectsMarch = await kg.QueryAsync(
            new TriplePattern(
                Subject: new EntityRef("person", "alice"),
                Predicate: "works-on",
                Object: null),
            at: march1);

        foreach (var triple in aliceProjectsMarch)
        {
            Console.WriteLine($"  • {triple.Triple.Object}");
        }
        Console.WriteLine();

        // Step 6: Entity timeline
        Console.WriteLine("📅 Step 6: Entity timeline...\n");
        
        Console.WriteLine("Timeline for Alice:");
        var aliceTimeline = await kg.TimelineAsync(new EntityRef("person", "alice"));
        
        foreach (var evt in aliceTimeline.OrderBy(e => e.Timestamp))
        {
            var direction = evt.IsOutgoing ? "→" : "←";
            var otherEntity = evt.IsOutgoing ? evt.RelatedEntity : evt.SourceEntity;
            Console.WriteLine($"  {evt.Timestamp:yyyy-MM-dd} {direction} {evt.Predicate} {otherEntity}");
        }
        Console.WriteLine();

        // Step 7: Graph statistics
        Console.WriteLine("📊 Step 7: Graph statistics...\n");
        
        var totalTriples = await kg.CountAsync();
        Console.WriteLine($"Total triples: {totalTriples}");
        
        // Count entities by type
        var personTriples = await kg.QueryAsync(
            new TriplePattern(Subject: null, Predicate: null, Object: null));
        
        var entityTypes = personTriples
            .SelectMany(t => new[] { t.Triple.Subject, t.Triple.Object })
            .GroupBy(e => e.Type)
            .OrderByDescending(g => g.Count());

        Console.WriteLine("\nEntities by type:");
        foreach (var group in entityTypes)
        {
            var distinctCount = group.Select(e => e.Id).Distinct().Count();
            Console.WriteLine($"  • {group.Key}: {distinctCount}");
        }
        Console.WriteLine();

        Console.WriteLine("✅ Example completed successfully!");
        Console.WriteLine("\n💡 Next steps:");
        Console.WriteLine("  • Combine with semantic search to find related entities");
        Console.WriteLine("  • Use the knowledge graph to enrich memory search results");
        Console.WriteLine("  • Build agent memory diaries using temporal relationships");
        Console.WriteLine("  • Track changes to entity relationships over time");
        
        // Clean up
        if (File.Exists(kgPath))
        {
            File.Delete(kgPath);
        }
    }

    static async Task<List<Document>> LoadDocumentsAsync(string path)
    {
        var documents = new List<Document>();
        var files = Directory.GetFiles(path, "*.md");

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            var name = Path.GetFileNameWithoutExtension(file);
            var timestamp = File.GetLastWriteTimeUtc(file);
            documents.Add(new Document(name, content, timestamp));
        }

        return documents;
    }

    static async Task CreateSampleDocumentsAsync(string path)
    {
        var docs = new Dictionary<string, string>
        {
            ["team-update-jan.md"] = @"# Team Update - January 2025

Alice joined the engineering team as a Senior Developer. She'll be working on Project Phoenix, 
our new API platform. Bob is her manager and will help with onboarding.

Charlie, our tech lead, is leading the architecture design for Phoenix.",

            ["team-update-feb.md"] = @"# Team Update - February 2025

Alice has made great progress on Project Phoenix. She implemented the authentication module 
and is now working on the database layer.

Diana joined the team as a junior developer and will assist Alice on Phoenix. 
Bob continues to manage both Alice and Diana.",

            ["team-update-mar.md"] = @"# Team Update - March 2025

Project Phoenix is nearing completion! Alice finished the API endpoints and Diana completed 
the documentation.

Alice has been reassigned to Project Quantum, a new AI initiative. Charlie will be the 
tech lead for Quantum as well.

Bob was promoted to Director of Engineering and now manages the entire engineering team.",

            ["project-status.md"] = @"# Project Status - March 2025

## Project Phoenix
- Status: Completed
- Team: Alice (lead), Diana (docs), Charlie (architecture)
- Launched: March 15, 2025

## Project Quantum
- Status: Active
- Team: Alice (developer), Charlie (tech lead)
- Started: March 20, 2025"
        };

        foreach (var (filename, content) in docs)
        {
            await File.WriteAllTextAsync(Path.Combine(path, filename), content);
        }
    }
}

record Document(string Name, string Content, DateTimeOffset Timestamp);

/// <summary>
/// Simple pattern-based entity and relationship extractor.
/// In production, use NLP libraries or LLMs for more accurate extraction.
/// </summary>
class SimpleEntityExtractor
{
    public List<TemporalTriple> ExtractTriples(string text, DateTimeOffset timestamp)
    {
        var triples = new List<TemporalTriple>();
        
        // Pattern: "X joined the Y team"
        var joinedPattern = new Regex(@"(\w+)\s+joined\s+the\s+(\w+)\s+team", RegexOptions.IgnoreCase);
        foreach (Match match in joinedPattern.Matches(text))
        {
            var person = match.Groups[1].Value.ToLowerInvariant();
            var team = match.Groups[2].Value.ToLowerInvariant();
            
            triples.Add(new TemporalTriple(
                new Triple(
                    new EntityRef("person", person),
                    "member-of",
                    new EntityRef("team", team),
                    null),
                timestamp,
                null,
                timestamp));
        }

        // Pattern: "X is Y's manager" or "Y's manager"
        var managerPattern = new Regex(@"(\w+)\s+is\s+(?:her|his|their)\s+manager", RegexOptions.IgnoreCase);
        foreach (Match match in managerPattern.Matches(text))
        {
            var manager = match.Groups[1].Value.ToLowerInvariant();
            
            // Find the person being managed (look backwards in the sentence)
            var precedingText = text.Substring(0, match.Index);
            var personMatch = Regex.Match(precedingText, @"(\w+)\.?\s*$");
            if (personMatch.Success)
            {
                var person = personMatch.Groups[1].Value.ToLowerInvariant();
                triples.Add(new TemporalTriple(
                    new Triple(
                        new EntityRef("person", manager),
                        "manages",
                        new EntityRef("person", person),
                        null),
                    timestamp,
                    null,
                    timestamp));
            }
        }

        // Pattern: "X manages Y"
        var managesPattern = new Regex(@"(\w+)\s+(?:manages|manage)\s+(?:both\s+)?(\w+)(?:\s+and\s+(\w+))?", RegexOptions.IgnoreCase);
        foreach (Match match in managesPattern.Matches(text))
        {
            var manager = match.Groups[1].Value.ToLowerInvariant();
            var person1 = match.Groups[2].Value.ToLowerInvariant();
            
            if (person1 != "the" && person1 != "a" && person1 != "an")
            {
                triples.Add(new TemporalTriple(
                    new Triple(
                        new EntityRef("person", manager),
                        "manages",
                        new EntityRef("person", person1),
                        null),
                    timestamp,
                    null,
                    timestamp));
            }

            if (match.Groups[3].Success)
            {
                var person2 = match.Groups[3].Value.ToLowerInvariant();
                triples.Add(new TemporalTriple(
                    new Triple(
                        new EntityRef("person", manager),
                        "manages",
                        new EntityRef("person", person2),
                        null),
                    timestamp,
                    null,
                    timestamp));
            }
        }

        // Pattern: "X working on Project Y" or "X works on Y"
        var workingOnPattern = new Regex(@"(\w+)\s+(?:working|works?)\s+on\s+(?:Project\s+)?(\w+)", RegexOptions.IgnoreCase);
        foreach (Match match in workingOnPattern.Matches(text))
        {
            var person = match.Groups[1].Value.ToLowerInvariant();
            var project = match.Groups[2].Value.ToLowerInvariant();
            
            triples.Add(new TemporalTriple(
                new Triple(
                    new EntityRef("person", person),
                    "works-on",
                    new EntityRef("project", project),
                    null),
                timestamp,
                null,
                timestamp));
        }

        // Pattern: "X is/was reassigned to Project Y"
        var reassignedPattern = new Regex(@"(\w+)\s+(?:has\s+been\s+)?reassigned\s+to\s+(?:Project\s+)?(\w+)", RegexOptions.IgnoreCase);
        foreach (Match match in reassignedPattern.Matches(text))
        {
            var person = match.Groups[1].Value.ToLowerInvariant();
            var newProject = match.Groups[2].Value.ToLowerInvariant();
            
            triples.Add(new TemporalTriple(
                new Triple(
                    new EntityRef("person", person),
                    "works-on",
                    new EntityRef("project", newProject),
                    null),
                timestamp,
                null,
                timestamp));
        }

        // Pattern: "X as a/the Y" (role assignment)
        var rolePattern = new Regex(@"(\w+)\s+(?:as|is)\s+(?:a|an|the)\s+([\w\s]+?)(?:\.|,|and|$)", RegexOptions.IgnoreCase);
        foreach (Match match in rolePattern.Matches(text))
        {
            var person = match.Groups[1].Value.ToLowerInvariant();
            var role = match.Groups[2].Value.Trim().ToLowerInvariant();
            
            if (role.Contains("developer") || role.Contains("manager") || role.Contains("lead") || role.Contains("director"))
            {
                triples.Add(new TemporalTriple(
                    new Triple(
                        new EntityRef("person", person),
                        "has-role",
                        new EntityRef("role", role.Replace(" ", "-")),
                        null),
                    timestamp,
                    null,
                    timestamp));
            }
        }

        return triples;
    }
}
