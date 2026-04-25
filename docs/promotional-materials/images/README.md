# MemPalace.NET v0.5.0 Promotional Images

## Status: Pending Manual Generation

The t2i CLI tool is available but requires API key configuration for FLUX.2 Pro or MAI-Image-2 providers. Images need to be generated manually using the detailed prompts in `../image-generation-prompts.md`.

## Required Images

1. **mempalacenet-logo-1024.png** (1024x1024)
   - Purpose: NuGet package icon
   - Prompt: See `../image-generation-prompts.md` Section 1
   - Status: ⏳ Pending

2. **linkedin-hero-1200x628.png** (1200x628)
   - Purpose: LinkedIn announcement post banner
   - Prompt: See `../image-generation-prompts.md` Section 2
   - Status: ⏳ Pending

3. **twitter-card-1024x512.png** (1024x512)
   - Purpose: Twitter/X social card
   - Prompt: See `../image-generation-prompts.md` Section 3
   - Status: ⏳ Pending

4. **blog-header-1200x400.png** (1200x400)
   - Purpose: Blog post header
   - Prompt: See `../image-generation-prompts.md` Section 4
   - Status: ⏳ Pending

## Generation Options

### Option 1: DALL-E 3 (Recommended)
- Requires: OpenAI API key or ChatGPT Plus subscription
- Best for: Detailed prompts with specific layouts
- Process: Copy prompts from `image-generation-prompts.md` to ChatGPT or API

### Option 2: Midjourney
- Requires: Midjourney subscription (via Discord)
- Best for: Artistic, high-quality renders
- Process: Use prompts in Midjourney Discord bot

### Option 3: t2i CLI (After Configuration)
```bash
# Configure t2i with API key
t2i configure

# Generate images using prompts
t2i generate --prompt "[PROMPT]" --size [SIZE] --format png --output [OUTPUT_PATH]
```

### Option 4: Design Tools
- Figma + AI plugins
- Canva with AI generation
- Adobe Firefly

## Temporary Placeholders

While images are being generated, use these placeholder URLs in documentation:

```markdown
![MemPalace.NET Logo](https://via.placeholder.com/1024x1024/512BD4/FFFFFF?text=MemPalace.NET)
![LinkedIn Hero](https://via.placeholder.com/1200x628/0078D4/FFFFFF?text=MemPalace.NET+v0.5.0)
![Twitter Card](https://via.placeholder.com/1024x512/8b5cf6/FFFFFF?text=MemPalace.NET)
![Blog Header](https://via.placeholder.com/1200x400/0a0e27/FFFFFF?text=Semantic+Memory+for+.NET)
```

## Once Generated

After generating images:
1. Save images to this directory with exact filenames above
2. Update this README to mark as complete (✅)
3. Update `../README.md` with image references
4. Commit and push to repository
5. Update `.squad/agents/rachael/history.md` with completion status

## License

All images will be licensed under MIT, same as the MemPalace.NET project.
