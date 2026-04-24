# MemPalace.NET Agents

MemPalace.NET supports autonomous AI agents backed by **Microsoft Agent Framework (`Microsoft.Agents.AI` 1.3.0)**, with persistent per-agent diaries and access to palace search and knowledge graph tools.

## Prerequisites

Agents require an `IChatClient` from Microsoft.Extensions.AI to be registered in DI. Without it, agent commands will fail with a clear error message.

**Example registration:**

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

// Register an OpenAI chat client
services.AddChatClient(builder => builder
    .Use(new OpenAIChatClient("gpt-4", Environment.GetEnvironmentVariable("OPENAI_API_KEY"))));

// Or register any other IChatClient implementation
```

## Agent Runtime

MemPalace agents are powered by `ChatClientAgent` from Microsoft Agent Framework, which wraps an `IChatClient` and provides:
- **Tool calling** via `AIFunction` (converts MCP tools to AI functions)
- **Automatic function invocation** (the agent decides when to call tools)
- **Usage tracking** (input/output tokens from the underlying model)
- **Structured responses** via `AgentResponse` (text, messages, usage)

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

Agents automatically have access to MemPalace tools (if the corresponding services are registered in DI). Tools are converted to `AIFunction`s via `AIFunctionFactory.Create(...)` from Microsoft.Extensions.AI.

### `palace_search`

Search for memories in the palace. Available if `ISearchService` is registered.

**Parameters:**
- `query` (string, required): Search query text
- `collection` (string, default="default"): Collection to search in
- `topK` (int, default=5): Number of results to return

**Description for LLM:** "Search for memories in the palace matching the query"

**Returns:** Top-K search results formatted as `[score] document` per line.

### `kg_query`

Query the knowledge graph for relationships. Available if `IKnowledgeGraph` is registered.

**Parameters:**
- `subject` (string, nullable): Subject entity (e.g., "agent:roy") or null for wildcard
- `predicate` (string, nullable): Relationship predicate (e.g., "worked-on") or null for wildcard
- `object` (string, nullable): Object entity (e.g., "project:MemPalace.Mcp") or null for wildcard

**Description for LLM:** "Query the knowledge graph for entity relationships (triples). Use null for wildcards."

**Returns:** Matching triples formatted as `subject predicate object` per line.

## NuGet Packages

- **Microsoft.Agents.AI** 1.3.0 — Agent Framework base types (`ChatClientAgent`, `AIAgent`, `AgentResponse`)
- **Microsoft.Extensions.AI.Abstractions** 10.5.0 — `IChatClient`, `ChatMessage`, `ChatOptions`, `AITool`, `AIFunction`
- **Microsoft.Extensions.Hosting** 10.0.7 — DI and hosting abstractions
- **YamlDotNet** 16.3.0 — YAML parsing for agent descriptors

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
