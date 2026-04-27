---
name: t2i
description: 'Use the t2i CLI to generate AI images from text prompts via Microsoft Foundry providers (FLUX.2, MAI-Image-2). Activate when the user asks to generate images, automate image creation in scripts, or set up image generation for CI/CD.'
---

# t2i — Text-to-Image CLI Skill

This skill teaches AI agents (GitHub Copilot, Claude Code, and MCP-aware assistants) how to use the **t2i** command-line tool for image generation. Learn the commands, workflows, and best practices for automating text-to-image tasks in scripts and terminal environments.

## When to Use This Skill

Activate this skill when:
- User asks to generate an image from a text prompt
- User mentions text-to-image, image generation, or AI images
- User wants to automate image generation in a script or pipeline
- User needs batch image generation across multiple prompts
- User is setting up image generation for CI/CD or deployment workflows
- User requests help with t2i command syntax or configuration

## Quick Reference

### Core Commands

| Command | Purpose | Key Flags |
|---------|---------|-----------|
| `t2i "<prompt>"` | Generate image from text | `--provider`, `--output`, `--width`, `--height`, `--steps` |
| `t2i config` | Interactive setup wizard | `show`, `set`, `remove`, `path` |
| `t2i providers` | List available providers | None |
| `t2i secrets set <provider>` | Configure API credentials | `--field` |
| `t2i secrets list` | Show configured secrets | None |
| `t2i secrets remove <provider>` | Delete credentials | `--field` |
| `t2i secrets test <provider>` | Test provider connectivity | None |
| `t2i doctor` | Run full diagnostics | None |
| `t2i init` | Setup skill files | `--target`, `--force` |
| `t2i version` | Show version info | None |

### Generate Command Flags

| Flag | Description | Default |
|------|-------------|---------|
| `--provider` | Provider to use (foundry-flux2, foundry-mai2) | foundry-flux2 |
| `--output`, `-o` | Output file path | `generated-<timestamp>.png` |
| `--width`, `-w` | Image width in pixels | 512 |
| `--height` | Image height in pixels | 512 |
| `--steps`, `-s` | Number of inference steps | 20 |
| `--endpoint` | Override provider endpoint | From config |
| `--api-key` | Override API key (NOT RECOMMENDED) | From secrets |

### Config Command Actions

```bash
t2i config              # Launch interactive wizard
t2i config show         # Display all configuration
t2i config set <key> <value>  # Set config value (e.g., foundry-flux2.endpoint)
t2i config remove <provider>  # Remove provider config and secrets
t2i config path         # Show config file location
```

### Secrets Command Actions

```bash
t2i secrets set <provider>       # Interactively set secrets
t2i secrets list                 # Show all secrets (redacted)
t2i secrets remove <provider>    # Remove all secrets for provider
t2i secrets remove <provider> --field <name>  # Remove specific field
t2i secrets test <provider>      # Test provider connection
```

### Init Command Targets

```bash
t2i init                 # Create skill files for GitHub and Claude (default)
t2i init --target github # Create .github/skills/t2i/SKILL.md only
t2i init --target claude # Create .claude/skills/t2i/SKILL.md only
t2i init --force         # Overwrite existing files
```

## Providers

Two cloud providers available in the **Lite** edition:

| Provider | Model | URL | Best For |
|----------|-------|-----|----------|
| `foundry-flux2` | FLUX.2 Pro | Microsoft Foundry | High-quality images, fine-grained control, batch jobs, production use |
| `foundry-mai2` | MAI-Image-2 | Microsoft Foundry | Fast iteration, rich prompt understanding, synchronous API, rapid prototyping |

**Default:** `foundry-flux2` if user doesn't specify `--provider`.

### Provider Decision Tree

**Choose `foundry-flux2` if:**
- You need the highest quality images
- You're working on production assets
- Fine-grained control over generation parameters is important
- You can wait 10-30 seconds for results

**Choose `foundry-mai2` if:**
- You need fast iteration (< 10 seconds)
- You're prototyping or experimenting
- Your prompts are conversational or complex
- You prefer synchronous API responses

### Required Configuration

Both providers require:
- **endpoint** — Microsoft Foundry API endpoint (set via `t2i config set <provider>.endpoint <url>`)
- **apiKey** — API key for authentication (set securely via `t2i secrets set <provider>`)

## Common Workflows

### 1. First-Time Setup

```bash
# Step 1: Interactive config
t2i config

# Step 2: Enter API credentials when prompted
# (CLI stores securely via DPAPI on Windows, encrypted on macOS/Linux)

# Step 3: Verify connection
t2i doctor

# Step 4: Generate your first image
t2i "a robot painting a landscape"
```

**Agent tip:** If user skips `t2i config`, they'll get a "not configured" error. Always suggest running it first.

### 2. Generate One Image

```bash
# Basic: uses default provider and outputs to current directory
t2i "a cyberpunk city at night, neon lights"

# With custom filename
t2i "a robot waving" --output my-robot.png

# Specific provider and dimensions
t2i "minimalist line art of a cat" \
  --provider foundry-mai2 \
  --width 1024 \
  --height 1024 \
  --output cat.png

# High-quality image with more inference steps
t2i "photorealistic mountain landscape" \
  --steps 50 \
  --width 1920 \
  --height 1080 \
  --output landscape.png
```

**Output:** Images are saved to the specified path or auto-generated filename `<prompt-slug>-<timestamp>.png`

### 3. Batch Generate via Shell Loop

**Bash:**
```bash
#!/bin/bash
prompts=(
  "a robot painting a landscape"
  "a cyberpunk city at night"
  "a watercolor painting of a castle"
)

for i in "${!prompts[@]}"; do
  prompt="${prompts[$i]}"
  echo "Generating $((i+1))/${#prompts[@]}: $prompt"
  t2i "$prompt" --output "image-$(printf '%02d' $((i+1))).png"
  sleep 2  # rate limiting
done
```

**PowerShell:**
```powershell
$prompts = @(
    "a robot painting a landscape",
    "a cyberpunk city at night",
    "a watercolor painting of a castle"
)

for ($i = 0; $i -lt $prompts.Count; $i++) {
    $prompt = $prompts[$i]
    Write-Host "Generating $($i+1)/$($prompts.Count): $prompt"
    $index = ($i + 1).ToString("D2")
    & t2i $prompt --output "image-$index.png"
    Start-Sleep -Seconds 2  # rate limiting
}
```

### 4. CI/CD Integration (GitHub Actions)

```yaml
name: Generate Marketing Assets

on:
  workflow_dispatch:
  schedule:
    - cron: '0 0 * * 1'  # Weekly on Monday

jobs:
  generate-images:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Install t2i
        run: dotnet tool install -g ElBruno.Text2Image.Cli

      - name: Generate hero image
        env:
          T2I_FOUNDRY_FLUX2_ENDPOINT: ${{ secrets.T2I_ENDPOINT }}
          T2I_FOUNDRY_FLUX2_API_KEY: ${{ secrets.T2I_API_KEY }}
        run: |
          t2i "futuristic tech product hero image" \
            --output assets/hero.png \
            --width 1920 \
            --height 1080

      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: generated-images
          path: assets/*.png
```

### 5. Troubleshooting Workflow

```bash
# Step 1: Check system health and configuration
t2i doctor

# Step 2: List all providers and their status
t2i providers

# Step 3: Verify secrets are configured
t2i secrets list

# Step 4: Test specific provider connectivity
t2i secrets test foundry-flux2

# Step 5: View current configuration
t2i config show

# Step 6: Check config file location
t2i config path
```

## Important Rules for Agents

### 1. Always Verify Configuration First
Before suggesting any image generation command, check if the user has run `t2i config`. If they haven't or see configuration errors, suggest:
```bash
t2i config  # Interactive setup
# OR
t2i doctor  # Check current status
```

### 2. Never Expose API Keys
**DO NOT:**
- Include API keys, tokens, or secrets in code examples
- Display secrets in commit messages or logs
- Use `--api-key` flag in scripts (use secrets store instead)

**ALWAYS:**
- Direct users to `t2i secrets set <provider>` for credential management
- Use environment variables in CI/CD: `T2I_<PROVIDER>_<FIELD>`
- Store locally via DPAPI (Windows) or encrypted files (macOS/Linux)

### 3. Use Predictable Filenames in Scripts
When scripting batch jobs, always specify `--output`:
```bash
# GOOD: predictable, parseable filenames
t2i "prompt" --output "image-01.png"

# BAD: random filenames are hard to track
t2i "prompt"  # generates "prompt-slug-20240315-143022.png"
```

### 4. Default to foundry-flux2
If the user doesn't specify a provider:
- Use `foundry-flux2` for best quality
- Only suggest `foundry-mai2` if speed is critical or user prefers it

### 5. Run `t2i doctor` for Diagnostics
If the user reports generation failures or API errors:
```bash
t2i doctor  # One command checks everything:
            # - System info
            # - Provider availability
            # - Configuration status
            # - Secret store health
```

### 6. Suggest `t2i init` for New Repos
When onboarding a new project:
```bash
t2i init  # Creates skill files for AI agents
          # - .github/skills/t2i/SKILL.md
          # - .claude/skills/t2i/SKILL.md
```

### 7. Respect Rate Limits
When generating multiple images:
```bash
# Add delays between requests
for prompt in prompts; do
  t2i "$prompt" --output "image-$i.png"
  sleep 2  # 2 second delay
done
```

## Using t2i with AI Coding Agents

### GitHub Copilot CLI Integration

Once you run `t2i init`, GitHub Copilot CLI can discover and use this skill automatically:

**Example prompts:**
```text
"Generate a logo image and save it as logo.png"
→ Copilot will run: t2i "company logo, modern design" --output logo.png

"Create test images for my gallery using different styles"
→ Copilot will batch generate with various prompts

"Show me what t2i commands are available"
→ Copilot will reference this skill file
```

### Claude Code Integration

After `t2i init --target claude`, Claude can assist with:
- Script automation for batch generation
- CI/CD pipeline setup
- Troubleshooting configuration issues
- Workflow optimization

**Example workflow:**
```text
User: "Help me set up image generation in CI"
Claude: "Let's configure GitHub Actions for t2i:
1. Run 't2i config' locally to test
2. Add secrets to GitHub repo settings
3. Create workflow file with env vars
4. Test with workflow_dispatch"
```

## Secrets & Security

### Storage Priority

**t2i** checks for secrets in this order:

1. **Environment variables** — `T2I_<PROVIDER>_<FIELD>` (best for CI/CD)
   ```bash
   # Example for foundry-flux2
   export T2I_FOUNDRY_FLUX2_ENDPOINT="https://..."
   export T2I_FOUNDRY_FLUX2_API_KEY="your-key-here"
   ```

2. **Platform-specific secure storage:**
   - **Windows:** DPAPI encrypted file at `%LOCALAPPDATA%\t2i\secrets.dpapi` (per-user encryption)
   - **macOS/Linux:** File at `~/.config/t2i/secrets.json` with `0600` permissions

3. **Config overrides** — `--endpoint` and `--api-key` flags (NOT RECOMMENDED for scripts)

### Local Development Setup

**Interactive (recommended):**
```bash
t2i secrets set foundry-flux2
# Prompts for: endpoint, apiKey
# Stores securely via DPAPI/encrypted file
```

**Manual (environment variables):**
```bash
# Windows PowerShell
$env:T2I_FOUNDRY_FLUX2_ENDPOINT = "https://..."
$env:T2I_FOUNDRY_FLUX2_API_KEY = "..."

# Linux/macOS
export T2I_FOUNDRY_FLUX2_ENDPOINT="https://..."
export T2I_FOUNDRY_FLUX2_API_KEY="..."
```

### CI/CD Setup

**GitHub Actions:**
```yaml
name: Generate Images

on: [push]

jobs:
  generate:
    runs-on: ubuntu-latest
    steps:
      - name: Install t2i
        run: dotnet tool install -g ElBruno.Text2Image.Cli

      - name: Generate
        env:
          T2I_FOUNDRY_FLUX2_ENDPOINT: ${{ secrets.T2I_ENDPOINT }}
          T2I_FOUNDRY_FLUX2_API_KEY: ${{ secrets.T2I_API_KEY }}
        run: t2i "test image" --output output.png
```

**Azure Pipelines:**
```yaml
steps:
- task: DotNetCoreCLI@2
  inputs:
    command: 'custom'
    custom: 'tool'
    arguments: 'install -g ElBruno.Text2Image.Cli'

- script: t2i "azure logo" --output logo.png
  env:
    T2I_FOUNDRY_FLUX2_ENDPOINT: $(T2I_ENDPOINT)
    T2I_FOUNDRY_FLUX2_API_KEY: $(T2I_API_KEY)
```

### Security Best Practices

**DO:**
- ✓ Use `t2i secrets set` for local development
- ✓ Store secrets in CI/CD secret managers (GitHub Secrets, Azure Key Vault)
- ✓ Use environment variables in automated workflows
- ✓ Add secret paths to `.gitignore`:
  ```gitignore
  # Windows
  %LOCALAPPDATA%/t2i/secrets.dpapi
  
  # macOS/Linux
  ~/.config/t2i/secrets.json
  ```

**DON'T:**
- ✗ Hardcode API keys in scripts or config files
- ✗ Commit secrets to version control
- ✗ Use `--api-key` flag in shared scripts
- ✗ Share secret store files between users

### Rotating Credentials

```bash
# Update existing secrets
t2i secrets set foundry-flux2

# Verify new secrets work
t2i secrets test foundry-flux2

# Remove old secrets
t2i secrets remove foundry-flux2 --field apiKey
```

## Troubleshooting

### Common Errors and Solutions

#### Error: "No default provider configured"
**Cause:** User hasn't run initial configuration.

**Solution:**
```bash
t2i config  # Interactive setup
# OR
t2i config set foundry-flux2.endpoint "https://..."
t2i secrets set foundry-flux2
```

#### Error: "Provider 'X' not found"
**Cause:** Invalid provider name.

**Solution:**
```bash
t2i providers  # List all available providers
# Valid providers: foundry-flux2, foundry-mai2
```

#### Error: "Missing secret 'apiKey' for provider"
**Cause:** API credentials not configured.

**Solution:**
```bash
# Interactive
t2i secrets set foundry-flux2

# OR via environment variable
export T2I_FOUNDRY_FLUX2_API_KEY="your-key"
```

#### Error: "Generation failed: 401 Unauthorized"
**Cause:** Invalid or expired API key.

**Solution:**
```bash
# Test connectivity
t2i secrets test foundry-flux2

# Rotate credentials
t2i secrets set foundry-flux2
```

#### Error: "Generation failed: 429 Too Many Requests"
**Cause:** Rate limit exceeded.

**Solution:**
```bash
# Add delays in batch scripts
sleep 2  # Bash
Start-Sleep -Seconds 2  # PowerShell
```

#### Error: "Could not write to output path"
**Cause:** Permission issues or invalid path.

**Solution:**
```bash
# Check permissions
ls -la output-dir/  # Linux/macOS
Get-Acl output-dir\  # Windows

# Use absolute path
t2i "prompt" --output "/full/path/to/image.png"
```

### Diagnostic Commands

Run these in order when troubleshooting:

```bash
# 1. Check overall health
t2i doctor

# 2. View current configuration
t2i config show

# 3. List configured providers
t2i providers

# 4. Check secrets
t2i secrets list

# 5. Test specific provider
t2i secrets test foundry-flux2

# 6. Find config file
t2i config path
```

### Log Files and Debugging

**Check config location:**
```bash
t2i config path
# Windows: %APPDATA%\t2i\config.json
# macOS: ~/Library/Application Support/t2i/config.json
# Linux: ~/.config/t2i/config.json
```

**Manually inspect config:**
```bash
# Windows PowerShell
cat $env:APPDATA\t2i\config.json

# Linux/macOS
cat ~/.config/t2i/config.json
```

**Reset configuration:**
```bash
# Remove provider config
t2i config remove foundry-flux2

# Remove all secrets
t2i secrets remove foundry-flux2

# Start fresh
t2i config
```

### Getting Help

If you're still stuck:

1. **Run full diagnostics:**
   ```bash
   t2i doctor > diagnostics.txt
   t2i config show >> diagnostics.txt
   t2i providers >> diagnostics.txt
   ```

2. **Check documentation:**
   - Full CLI docs: [docs/cli-tool.md](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/cli-tool.md)
   - GitHub repo: [elbruno/ElBruno.Text2Image](https://github.com/elbruno/ElBruno.Text2Image)

3. **Report issues:**
   - GitHub Issues: [Report a bug](https://github.com/elbruno/ElBruno.Text2Image/issues/new)
   - Include: `t2i version`, `t2i doctor` output, error messages

## Examples for AI Agents

### Good Agent Behavior ✓

**Example 1: User asks to generate an image**
```text
User: "Create a logo for my startup"
Agent: 
1. Checks: "Have you configured t2i? Run 't2i doctor' to verify."
2. User confirms setup is done
3. Agent runs: t2i "modern startup logo, minimalist design" --output logo.png
4. Verifies output exists: ls -la logo.png
```

**Example 2: User reports error**
```text
User: "t2i command failed with 'missing secret' error"
Agent:
1. Runs: t2i secrets list
2. Identifies missing apiKey
3. Suggests: "Run 't2i secrets set foundry-flux2' to configure credentials"
4. After setup, tests: t2i secrets test foundry-flux2
```

**Example 3: Batch generation**
```text
User: "Generate 5 test images"
Agent:
1. Creates script with --output for each image
2. Adds sleep delays between requests
3. Runs script and verifies all outputs exist
4. Reports: "Generated 5 images: image-01.png through image-05.png"
```

### Bad Agent Behavior ✗

**Don't do this:**
```bash
# ✗ Exposing API keys
t2i "prompt" --api-key "sk-abc123..."

# ✗ No output specification in scripts
for i in {1..10}; do
  t2i "prompt"  # Random filenames!
done

# ✗ Skipping configuration check
t2i "prompt"  # Fails with "not configured"

# ✗ No rate limiting
for prompt in prompts; do
  t2i "$prompt"  # Too fast!
done
```

**Do this instead:**
```bash
# ✓ Use secrets store
t2i secrets set foundry-flux2

# ✓ Specify output paths
for i in {1..10}; do
  t2i "prompt" --output "image-$(printf '%02d' $i).png"
  sleep 2
done

# ✓ Check config first
t2i doctor && t2i "prompt"

# ✓ Add delays
for prompt in prompts; do
  t2i "$prompt" --output "img-$i.png"
  sleep 2
done
```

## More Info

- **Full documentation:** [docs/cli-tool.md](https://github.com/elbruno/ElBruno.Text2Image/blob/main/docs/cli-tool.md)
- **GitHub repository:** [elbruno/ElBruno.Text2Image](https://github.com/elbruno/ElBruno.Text2Image)
- **Package:** [NuGet: ElBruno.Text2Image.Cli](https://www.nuget.org/packages/ElBruno.Text2Image.Cli/)
- **Report issues:** [GitHub Issues](https://github.com/elbruno/ElBruno.Text2Image/issues)
