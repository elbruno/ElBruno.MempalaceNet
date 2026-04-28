# CLI Embedder Configuration — MemPalace.NET

This guide shows how to configure embedder providers when using the `mempalacenet` CLI tool.

---

## Quick Reference

```bash
# Local ONNX embeddings (default, no config needed)
mempalacenet search "query text"

# OpenAI embeddings
export OPENAI_API_KEY="sk-..."
mempalacenet search "query text" --embedder openai

# Azure OpenAI embeddings
export AZURE_OPENAI_API_KEY="..."
export AZURE_OPENAI_ENDPOINT="https://....openai.azure.com"
mempalacenet search "query text" --embedder azureopenai --deployment my-deployment
```

---

## Local Provider (Default)

The CLI uses **local ONNX embeddings** by default. No configuration required.

```bash
# Initialize palace (downloads model on first run)
mempalacenet init ~/my-palace

# Mine files with local embeddings
mempalacenet mine ~/docs --wing documentation --mode files

# Search with local embeddings
mempalacenet search "how to configure auth?"
```

### Custom Local Model

Specify a different HuggingFace model:

```bash
# Use multilingual model
mempalacenet mine ~/docs --wing docs --model "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
```

**Note:** Model name must be a valid HuggingFace model ID.

---

## OpenAI Provider

### Environment Variable (Recommended)

```bash
# Set API key
export OPENAI_API_KEY="sk-..."

# Use OpenAI embeddings
mempalacenet mine ~/docs --wing docs --embedder openai

# Search with OpenAI
mempalacenet search "React patterns" --embedder openai
```

### Specify Model

```bash
# Use text-embedding-3-large (higher quality, more expensive)
mempalacenet mine ~/docs --wing docs --embedder openai --model text-embedding-3-large
```

### Command-Line API Key (Not Recommended)

```bash
# Avoid: API key visible in shell history
mempalacenet search "query" --embedder openai --api-key "sk-..."
```

**Security warning:** Use environment variables instead of command-line flags for API keys.

---

## Azure OpenAI Provider

### Environment Variables (Recommended)

```bash
# Set credentials
export AZURE_OPENAI_API_KEY="..."
export AZURE_OPENAI_ENDPOINT="https://YOUR_RESOURCE.openai.azure.com"

# Use Azure OpenAI
mempalacenet mine ~/docs --wing docs --embedder azureopenai --deployment text-embedding-ada-002

# Search with Azure OpenAI
mempalacenet search "query" --embedder azureopenai --deployment my-deployment
```

### Required Parameters

- **--deployment:** Your deployment name (required for Azure)
- **AZURE_OPENAI_ENDPOINT:** Your Azure OpenAI resource endpoint
- **AZURE_OPENAI_API_KEY:** Your API key

---

## Configuration File (v1.0)

**Coming in v1.0:** Store embedder configuration in a file.

```json
// ~/.mempalace/config.json (planned)
{
  "embedder": {
    "type": "openai",
    "model": "text-embedding-3-small",
    "apiKey": "${OPENAI_API_KEY}" // Reference environment variable
  },
  "palace": {
    "defaultPath": "~/my-palace"
  }
}
```

---

## CLI Flags Reference

### Global Embedder Flags

| Flag | Description | Default |
|------|-------------|---------|
| `--embedder <type>` | Embedder type (`local`, `openai`, `azureopenai`) | `local` |
| `--model <name>` | Model name | Provider-specific |
| `--api-key <key>` | API key (not recommended, use env var) | `null` |
| `--endpoint <url>` | Azure OpenAI endpoint | `null` |
| `--deployment <name>` | Azure deployment name | `null` |

### Command-Specific Examples

#### Mine Command

```bash
# Local with custom model
mempalacenet mine ~/docs --wing docs --model "all-mpnet-base-v2"

# OpenAI
mempalacenet mine ~/docs --wing docs --embedder openai

# Azure OpenAI
mempalacenet mine ~/docs --wing docs --embedder azureopenai --deployment my-deployment
```

#### Search Command

```bash
# Local (default)
mempalacenet search "query text"

# OpenAI
mempalacenet search "query text" --embedder openai

# Azure OpenAI
mempalacenet search "query text" --embedder azureopenai --deployment my-deployment
```

#### Wake-Up Command

```bash
# Local (default)
mempalacenet wake-up --wing conversations

# OpenAI (for LLM summarization)
mempalacenet wake-up --wing conversations --embedder openai
```

---

## Environment Variable Reference

### OpenAI

| Variable | Description | Required |
|----------|-------------|----------|
| `OPENAI_API_KEY` | OpenAI API key | ✅ Yes |
| `OPENAI_MODEL` | Default model (optional) | ❌ No |

### Azure OpenAI

| Variable | Description | Required |
|----------|-------------|----------|
| `AZURE_OPENAI_API_KEY` | Azure OpenAI API key | ✅ Yes |
| `AZURE_OPENAI_ENDPOINT` | Resource endpoint URL | ✅ Yes |
| `AZURE_OPENAI_DEPLOYMENT` | Default deployment name (optional) | ❌ No |

### Local Provider

| Variable | Description | Required |
|----------|-------------|----------|
| `HUGGINGFACE_CACHE` | Model cache directory | ❌ No |

---

## Embedder Switching

**Warning:** You cannot switch embedders for an existing collection. Collections enforce embedder identity.

### Scenario: Migrate from Local to OpenAI

```bash
# 1. Mine with local embeddings
mempalacenet mine ~/docs --wing docs --embedder local

# 2. Later: Try to mine with OpenAI
mempalacenet mine ~/more-docs --wing docs --embedder openai
# ❌ Error: EmbedderIdentityMismatchException

# 3. Solution: Create new wing with OpenAI embeddings
mempalacenet mine ~/more-docs --wing docs-openai --embedder openai
```

### Migration Strategy

1. **Create new wing/collection** with new embedder
2. **Re-embed all documents** into new collection
3. **Update search queries** to use new wing
4. **Delete old wing** (optional)

```bash
# Migrate docs from local to OpenAI
export OPENAI_API_KEY="sk-..."

# Step 1: Create new wing
mempalacenet mine ~/docs --wing docs-v2 --embedder openai

# Step 2: Verify search works
mempalacenet search "test query" --wing docs-v2

# Step 3: Delete old wing (optional)
mempalacenet delete-wing docs
```

---

## Performance Considerations

### Local Provider

- **First run:** Downloads model (~20-100 MB), takes 1-5 minutes
- **Subsequent runs:** Instant (model cached)
- **Embedding speed:** ~100-500 docs/sec (CPU-dependent)

### OpenAI Provider

- **Rate limits:** Tier-dependent (Tier 1: 500 RPM, Tier 5: 10,000 RPM)
- **Batch size:** Up to 2048 texts per request
- **Embedding speed:** ~1000-5000 docs/sec (network + API capacity)

### Azure OpenAI Provider

- **Rate limits:** Deployment-specific (configured in Azure Portal)
- **Embedding speed:** Similar to OpenAI, depends on region + capacity

---

## Troubleshooting

### Error: "API key is required"

```
InvalidOperationException: OpenAI API key is required.
```

**Solution:**
```bash
export OPENAI_API_KEY="sk-..."
```

### Error: "Endpoint is required"

```
InvalidOperationException: Azure OpenAI endpoint is required.
```

**Solution:**
```bash
export AZURE_OPENAI_ENDPOINT="https://....openai.azure.com"
```

### Error: "Deployment name is required"

```
InvalidOperationException: Azure OpenAI deployment name is required.
```

**Solution:**
```bash
mempalacenet search "query" --embedder azureopenai --deployment my-deployment
```

### Error: "EmbedderIdentityMismatchException"

```
EmbedderIdentityMismatchException: Collection 'docs' was created with
embedder 'local:all-MiniLM-L6-v2' but opened with 'openai:text-embedding-3-small'.
```

**Solution:** Create a new collection with the new embedder (see "Embedder Switching" above).

### Error: "Model download failed"

```
ElBruno.LocalEmbeddings: Failed to download model 'all-MiniLM-L6-v2'
```

**Solution:**
- Check internet connection
- Verify model name is correct (HuggingFace model ID)
- Ensure disk space available (~100 MB per model)

---

## Best Practices

### Security

1. **Use environment variables** for API keys (never command-line flags)
2. **Rotate keys regularly** (especially for production deployments)
3. **Use Azure Key Vault** for enterprise scenarios

### Cost Optimization

1. **Use Local provider** for development/testing (free)
2. **Use OpenAI text-embedding-3-small** for production (cheapest remote option)
3. **Batch embeddings** in mine commands (fewer API calls)
4. **Monitor usage** via OpenAI dashboard or Azure Portal

### Performance

1. **Cache models locally** (Local provider)
2. **Use async/parallel** for large datasets
3. **Monitor rate limits** (OpenAI/Azure)

---

## Next Steps

- **Embedder Guide:** See [embedder-guide.md](./embedder-guide.md)
- **Architecture Details:** See [embedder-architecture.md](./embedder-architecture.md)
- **CLI Reference:** See [cli.md](./cli.md)

---

## License

MIT License — See [LICENSE](../LICENSE) for details.
