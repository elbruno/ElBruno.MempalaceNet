using System.ComponentModel;
using MemPalace.KnowledgeGraph;
using MemPalace.Mcp.Security;
using ModelContextProtocol.Server;

namespace MemPalace.Mcp.Tools;

/// <summary>
/// MCP tools for knowledge graph write operations.
/// </summary>
[McpServerToolType]
public class KnowledgeGraphWriteTools
{
    private readonly IKnowledgeGraph _knowledgeGraph;
    private readonly SecurityValidator _validator;

    public KnowledgeGraphWriteTools(
        IKnowledgeGraph knowledgeGraph,
        SecurityValidator validator)
    {
        _knowledgeGraph = knowledgeGraph;
        _validator = validator;
    }

    /// <summary>
    /// Add an entity to the knowledge graph.
    /// </summary>
    [McpServerTool]
    [Description("Add an entity to the knowledge graph. Entity reference format: 'type:id' (e.g., 'person:alice', 'project:mempalace').")]
    public async Task<KgAddEntityResponse> KgAddEntity(
        [Description("Entity reference in format 'type:id'")] string entity,
        [Description("Optional properties as JSON object")] Dictionary<string, object>? properties = null,
        CancellationToken ct = default)
    {
        // Validate inputs
        _validator.ValidateEntityRef(entity);

        var entityRef = EntityRef.Parse(entity);

        // Add entity (knowledge graph internally handles storage)
        // Create a temporal triple for the entity type
        var triple = new TemporalTriple(
            Triple: new Triple(
                Subject: entityRef,
                Predicate: "type",
                Object: EntityRef.Parse($"type:{entityRef.Type}")
            ),
            ValidFrom: DateTimeOffset.UtcNow,
            ValidTo: null,
            RecordedAt: DateTimeOffset.UtcNow
        );

        await _knowledgeGraph.AddAsync(triple, ct);

        // Audit log
        await _validator.AuditWriteOperationAsync(
            "kg_add_entity",
            "knowledge_graph",
            entity,
            properties,
            ct);

        return new KgAddEntityResponse(entity, "added");
    }

    /// <summary>
    /// Add a relationship to the knowledge graph.
    /// </summary>
    [McpServerTool]
    [Description("Add a relationship (triple) to the knowledge graph. Creates a directed edge from subject to object.")]
    public async Task<KgAddRelationshipResponse> KgAddRelationship(
        [Description("Subject entity reference (e.g., 'person:alice')")] string subject,
        [Description("Predicate/relationship type (e.g., 'works-on', 'manages')")] string predicate,
        [Description("Object entity reference (e.g., 'project:mempalace')")] string @object,
        [Description("Valid from timestamp (ISO8601, optional, defaults to now)")] string? validFrom = null,
        [Description("Valid to timestamp (ISO8601, optional, null = ongoing)")] string? validTo = null,
        CancellationToken ct = default)
    {
        // Validate inputs
        _validator.ValidateEntityRef(subject);
        _validator.ValidateEntityRef(@object);

        if (string.IsNullOrWhiteSpace(predicate))
        {
            throw new ArgumentException("Predicate cannot be empty", nameof(predicate));
        }

        var subjectRef = EntityRef.Parse(subject);
        var objectRef = EntityRef.Parse(@object);

        DateTimeOffset validFromTime = validFrom != null 
            ? DateTimeOffset.Parse(validFrom) 
            : DateTimeOffset.UtcNow;

        DateTimeOffset? validToTime = validTo != null 
            ? DateTimeOffset.Parse(validTo) 
            : null;

        var temporalTriple = new TemporalTriple(
            Triple: new Triple(
                Subject: subjectRef,
                Predicate: predicate,
                Object: objectRef
            ),
            ValidFrom: validFromTime,
            ValidTo: validToTime,
            RecordedAt: DateTimeOffset.UtcNow
        );

        await _knowledgeGraph.AddAsync(temporalTriple, ct);

        // Audit log
        await _validator.AuditWriteOperationAsync(
            "kg_add_relationship",
            "knowledge_graph",
            $"{subject}-{predicate}-{@object}",
            new Dictionary<string, object>
            {
                ["subject"] = subject,
                ["predicate"] = predicate,
                ["object"] = @object
            },
            ct);

        return new KgAddRelationshipResponse(subject, predicate, @object, "added");
    }
}

// Response DTOs
public record KgAddEntityResponse(string Entity, string Status);
public record KgAddRelationshipResponse(string Subject, string Predicate, string Object, string Status);
