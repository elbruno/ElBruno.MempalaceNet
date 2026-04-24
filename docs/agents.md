# MemPalace.NET Agents

MemPalace.NET supports autonomous AI agents backed by Microsoft.Extensions.AI, with persistent per-agent diaries and access to palace search and knowledge graph tools.

## Agent Descriptor Schema

Agents are defined via YAML files in `.mempalace/agents/`:

```yaml
id: scribe
name: Scribe
persona: You are a helpful AI assistant named Scribe who helps users understand and work with the MemPalace knowledge management system.
instructions: |
  Your role is to:
  - Answer questions about MemPalace.NET architecture and capabilities
  - Help users navigate their memory palace
  - Suggest relevant memories and knowledge graph connections
  - Explain concepts clearly and concisely
  
  You have access to palace_search and kg_query tools to retrieve information from the palace.
  Always ground your responses in the retrieved context when available.
wing: assistants  # Optional: organize agents by wing/namespace
```

### Fields

- **id** (required): Unique identifier for the agent (kebab-case).
- **name** (required): Human-readable agent name.
- **persona** (required): Agent's identity and role (system prompt).
- **instructions** (required): Detailed instructions for the agent.
- **wing** (optional): Organizational namespace/category.

## Agent Tools

Agents automatically have access to these MemPalace tools (if services are registered):

### `palace_search`

Search for memories in the palace.

**Parameters:**
- `query` (string): Search query text
- `collection` (string, default="default"): Collection to search
- `topK` (int, default=5): Number of results

**Returns:** Top-K search results with scores and document text.

### `kg_query`

Query the knowledge graph for relationships.

**Parameters:**
- `subject` (string): Subject entity (e.g., "agent:tyrell") or empty for wildcard
- `predicate` (string): Relationship predicate (e.g., "worked-on") or empty for wildcard
- `obj` (string): Object entity or empty for wildcard

**Returns:** Matching triples.

## Agent Diary

Each agent maintains a persistent diary (stored as embeddings in the palace backend). Every turn (user message + assistant response) is automatically recorded.

**Diary storage:**
- Backend: `IBackend` with `PalaceRef("agents")`
- Collection: `agent_diary:{agentId}`
- Embeddings: Generated via configured `IEmbedder`
- Search: Semantic search via `ISearchService`

## CLI Commands

### List Agents

```bash
mempalacenet agents list
```

Lists all agents discovered from `.mempalace/agents/*.yaml`.

### Run Agent (One-Shot)

```bash
mempalacenet agents run scribe "What is MemPalace?"
```

Invokes the agent with a single message and prints the response.

**Output includes:**
- Agent response
- Token usage (input/output)
- Latency

### Chat with Agent (Interactive)

```bash
mempalacenet agents chat scribe
```

Starts an interactive REPL with the agent. Type `exit` to quit.

**Features:**
- Multi-turn conversation history
- Diary recording for each turn
- Ctrl+C to exit

### View Agent Diary

```bash
# Show recent entries
mempalacenet agents diary scribe --tail 10

# Search diary
mempalacenet agents diary scribe --search "knowledge graph"
```

**Options:**
- `--tail N`: Show N recent entries (default: 50)
- `--search "query"`: Semantic search over diary entries

## DI Registration

```csharp
services.AddMemPalaceAgents(options =>
{
    options.AgentsPath = Path.Combine(Directory.GetCurrentDirectory(), ".mempalace", "agents");
});
```

**Requirements:**
- `IChatClient` must be registered (call `AddMemPalaceAi()` first).
- Optionally: `ISearchService`, `IKnowledgeGraph`, `IBackend` for tool support and diary.

**Registered services:**
- `IAgentRegistry`: Discover and retrieve agents
- `IAgentDiary`: Per-agent persistent memory
- `IMemPalaceAgentBuilder`: Build agents from descriptors

## Microsoft.Extensions.AI Integration

MemPalace agents use `IChatClient` from Microsoft.Extensions.AI (v10.5.0+) as the LLM abstraction. This enables:

- **Model flexibility**: Swap Ollama, OpenAI, Azure OpenAI, etc. without changing agent code.
- **Tool support**: Agents automatically invoke `palace_search` and `kg_query` as needed.
- **Standard patterns**: Built on Microsoft's official AI abstractions.

**Note:** Full IChatClient integration is stubbed in Phase 8 pending API verification. Current implementation returns echo responses for testing. Full LLM invocation will be enabled once the Microsoft.Agents.AI compatibility issue is resolved.

## Sample Agent

See `src/MemPalace.Cli/samples/agents/scribe.yaml` for a reference agent definition.

## Architecture Notes

### Agent Runtime

- **MemPalaceAgent**: Implements `IMemPalaceAgent`, wraps `IChatClient`, records turns to diary.
- **MemPalaceAgentBuilder**: Builds agents from descriptors, wires tools, configures diary.
- **YamlAgentRegistry**: Discovers agents from YAML files, caches built instances.

### Agent Context

Each agent invocation receives:
- **ConversationId**: Unique ID for the conversation session
- **History**: Prior turns (`ChatMessage[]`)
- **Metadata**: Arbitrary key-value context

### Agent Response

Each invocation returns:
- **Content**: Agent's text response
- **NewMessages**: Turn history (user + assistant messages)
- **Trace**: Token counts, latency, tool calls

## Troubleshooting

**"IChatClient not registered" error:**
Call `AddMemPalaceAi()` before `AddMemPalaceAgents()` in DI setup.

**Agents not found:**
Ensure YAML files exist in `.mempalace/agents/` directory.

**Diary not persisting:**
Verify `IBackend` and `IEmbedder` are registered and functional.

## Future Enhancements (Post-Phase 8)

- Full Microsoft.Agents.AI integration (multi-agent orchestration, planner)
- Agent-to-agent communication
- Tool discovery from MCP servers
- RAG-based diary recall (auto-inject relevant past turns)
- Agent permissions and sandboxing
