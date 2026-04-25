# Image Generation Prompts for MemPalace.NET v0.5.0

This document contains detailed prompts for generating promotional images for MemPalace.NET v0.5.0 release.

---

## 1. Logo Image (NuGet Package Icon)

**Purpose:** Official logo for NuGet package listing and branding  
**Dimensions:** 1024x1024 pixels  
**Format:** PNG with transparent background  
**Style:** Modern, clean, professional

### Prompt

```
Create a modern, professional logo for "MemPalace.NET", a semantic memory library for .NET AI agents.

Visual concept: Combine architectural elements (palace/memory palace metaphor) with technology/AI symbolism.

Design elements:
- Central icon showing a stylized palace or mansion with clean geometric lines
- Palace should have distinct "wings" or sections representing the hierarchical memory structure
- Incorporate subtle neural network or circuit board patterns within the palace structure
- Use a .NET-inspired color palette: purple (#512BD4), blue (#0078D4), with accent colors
- Modern, minimalist aesthetic - not ornate or classical
- Icon should work well at small sizes (128x128) and large sizes (1024x1024)
- Transparent background
- Optional: Subtle gradient or glow effect

Typography (if included):
- "MemPalace" in a modern sans-serif font
- ".NET" in a smaller, complementary font
- Consider stacking or horizontal layout

The logo should convey: memory, organization, intelligence, .NET technology, and local-first/privacy.

Style: Tech startup, modern software library, professional but approachable.
```

**Suggested Tool:** DALL-E 3, Midjourney, or Stable Diffusion XL  
**Save As:** `docs/promotional-materials/images/mempalacenet-logo-1024.png`

---

## 2. LinkedIn Hero Image

**Purpose:** Banner image for LinkedIn announcement post  
**Dimensions:** 1200x628 pixels  
**Format:** PNG or JPEG  
**Style:** Tech/gradient theme, professional

### Prompt

```
Create a professional LinkedIn hero banner for "MemPalace.NET", a semantic memory system for .NET AI agents.

Layout:
- Landscape format (1200x628 pixels)
- Clean, modern design with technology aesthetic

Visual elements:
- Left side: Stylized AI agent or robot with glowing neural connections
- Center/Right: Visual representation of semantic memory:
  - Floating memory nodes connected by lines (knowledge graph visualization)
  - Semantic search visualization with vector embeddings
  - Hierarchical structure showing "wings → rooms → drawers"
- Background: Deep blue-purple gradient (#1a1a2e to #16213e to #0f3460)
- Accent colors: Electric blue (#00d4ff), purple (#a855f7), teal (#14b8a6)

Text overlay:
- Main headline: "Local Semantic Memory for .NET Agents"
- Subheadline: "MemPalace.NET v0.5.0 • Now on NuGet"
- Font: Modern sans-serif (Segoe UI, Inter, or similar)
- Text color: White with subtle glow effect

Visual style:
- Professional tech announcement
- Cinematic depth with layered elements
- Subtle particle effects or data flow animations suggested
- No cloud imagery (emphasize "local-first")

The image should convey: cutting-edge AI technology, local privacy, semantic intelligence, .NET ecosystem.
```

**Suggested Tool:** DALL-E 3, Midjourney, or professional design tools (Figma + AI assist)  
**Save As:** `docs/promotional-materials/images/linkedin-hero-1200x628.png`

---

## 3. Twitter Card Image

**Purpose:** Compact social card for Twitter/X posts  
**Dimensions:** 1024x512 pixels  
**Format:** PNG or JPEG  
**Style:** Bold, eye-catching, minimal text

### Prompt

```
Create a bold, eye-catching Twitter card image for "MemPalace.NET", a semantic memory library for .NET AI agents.

Layout:
- Horizontal format (1024x512 pixels)
- Simple, high-impact design that reads well at small sizes

Visual elements:
- Background: Vibrant gradient (purple #8b5cf6 to blue #3b82f6)
- Left side: MemPalace.NET logo (simplified icon if needed)
- Right side: Key benefit visualization:
  - Icon showing local/offline capability (server with house/lock symbol)
  - Neural network nodes representing semantic memory
  - .NET logo or text integration

Text overlay:
- Main text: "MemPalace.NET"
- Subtitle: "Semantic Memory for AI"
- Small badge: "v0.5.0 • Local-First"
- Font: Bold, modern sans-serif
- Text color: White with shadow for readability

Visual style:
- High contrast for mobile viewing
- Bold shapes and clear hierarchy
- Tech-forward, energetic
- Minimal but impactful

Design principles:
- Must be readable on mobile screens
- Work well as Twitter card thumbnail
- Stand out in fast-scrolling feeds
- Convey core value proposition at a glance
```

**Suggested Tool:** DALL-E 3, Canva, or Figma with AI generation  
**Save As:** `docs/promotional-materials/images/twitter-card-1024x512.png`

---

## 4. Blog Header Image

**Purpose:** Hero image for blog post and documentation  
**Dimensions:** 1200x400 pixels  
**Format:** PNG or JPEG  
**Style:** Professional, technical but accessible

### Prompt

```
Create a professional blog header image for "MemPalace.NET v0.5.0: Semantic Memory for Local AI Agents".

Layout:
- Wide banner format (1200x400 pixels)
- Suitable for blog post headers and documentation

Visual elements:
- Central concept: Semantic search and embeddings visualization
- Show transformation: text → embeddings → vector space → search results
- Visual metaphors:
  - Word/document clouds being transformed into vector representations
  - 3D space with clustered semantic concepts
  - Hierarchical organization (wings/rooms/drawers) subtly represented
  - Knowledge graph with temporal connections
- Background: Clean gradient (dark blue #0a0e27 to deep purple #2d1b69)
- Accent elements: Glowing nodes, connection lines, floating UI elements

Technical accuracy:
- Show vector dimensions (384d embeddings)
- Include cosine similarity visualization
- Represent ONNX/local processing (no cloud icons)
- Microsoft.Extensions.AI visual branding if possible

Text overlay:
- Title: "MemPalace.NET v0.5.0"
- Subtitle: "Semantic Memory for Local AI Agents"
- Font: Clean, readable sans-serif
- Text placement: Left or center, with good contrast

Visual style:
- Educational and approachable, not intimidating
- Professional developer tool aesthetic
- Balance technical depth with visual appeal
- Suitable for technical blog audience

The image should convey: semantic search technology, local processing, hierarchical organization, .NET professionalism.
```

**Suggested Tool:** DALL-E 3, Midjourney, or custom design with technical diagrams  
**Save As:** `docs/promotional-materials/images/blog-header-1200x400.png`

---

## Image Generation Instructions

### Recommended Tools

**For AI Generation:**
1. **DALL-E 3** (via ChatGPT Plus or API)
   - Best for: Detailed prompts with specific layouts
   - Pros: Excellent text rendering, follows complex instructions
   - Cons: Requires OpenAI API or ChatGPT Plus subscription

2. **Midjourney** (via Discord)
   - Best for: Artistic, high-quality renders
   - Pros: Beautiful gradients, professional quality
   - Cons: Less precise control over layout/text

3. **Stable Diffusion XL** (local or via Stability AI)
   - Best for: Local generation, customization
   - Pros: Free, local-first, ControlNet support
   - Cons: Requires setup, text rendering can be inconsistent

**For Design Tools:**
- **Figma** + AI plugins (Diagram, Genius)
- **Canva** with AI image generation
- **Adobe Firefly** for controlled generation

### Command-Line Generation Example

If using Stable Diffusion with a CLI tool:

```bash
# Install stable-diffusion-cli (example)
pip install diffusers transformers accelerate

# Generate logo
python -m diffusers.pipelines.stable_diffusion_xl.pipeline_stable_diffusion_xl \
  --prompt "$(cat docs/promotional-materials/image-generation-prompts.md | sed -n '/^### Prompt$/,/^**Suggested Tool:**$/p' | head -n -1 | tail -n +2)" \
  --output "docs/promotional-materials/images/mempalacenet-logo-1024.png" \
  --width 1024 \
  --height 1024

# Similar for other images with respective prompts and dimensions
```

**Alternative: Using DALL-E 3 API**

```bash
# Requires OPENAI_API_KEY environment variable
curl https://api.openai.com/v1/images/generations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $OPENAI_API_KEY" \
  -d '{
    "model": "dall-e-3",
    "prompt": "[PASTE PROMPT HERE]",
    "n": 1,
    "size": "1024x1024",
    "quality": "hd"
  }'
```

### File Organization

Save generated images to:

```
docs/promotional-materials/images/
├── mempalacenet-logo-1024.png          # Logo (1024x1024)
├── linkedin-hero-1200x628.png          # LinkedIn banner (1200x628)
├── twitter-card-1024x512.png           # Twitter card (1024x512)
└── blog-header-1200x400.png            # Blog header (1200x400)
```

### Post-Generation Editing

After generation, consider these refinements:

1. **Text Clarity:** If AI-generated text is unclear, overlay clean text using Figma/Photoshop
2. **Brand Colors:** Adjust colors to match .NET palette (#512BD4 purple, #0078D4 blue)
3. **Compression:** Optimize PNGs with `pngquant` or similar tools
4. **Metadata:** Add alt text and copyright information

### Fallback: Placeholder Images

If image generation tools are unavailable, use these placeholders:

```markdown
![MemPalace.NET Logo](https://via.placeholder.com/1024x1024/512BD4/FFFFFF?text=MemPalace.NET)
![LinkedIn Hero](https://via.placeholder.com/1200x628/0078D4/FFFFFF?text=MemPalace.NET+v0.5.0)
![Twitter Card](https://via.placeholder.com/1024x512/8b5cf6/FFFFFF?text=MemPalace.NET)
![Blog Header](https://via.placeholder.com/1200x400/0a0e27/FFFFFF?text=Semantic+Memory+for+.NET)
```

---

## Usage Guidelines

### Logo Usage
- Minimum size: 64x64 pixels
- Always maintain aspect ratio
- Provide sufficient whitespace around logo
- Use on light or dark backgrounds with appropriate contrast

### Social Media Images
- Test visibility on mobile devices
- Ensure text remains readable at thumbnail size
- Follow platform-specific image guidelines
- Include alt text for accessibility

### Blog/Documentation Images
- Use high-resolution versions for blog headers
- Optimize file size for web delivery
- Consider responsive image sets for different screen sizes
- Maintain consistent visual style across all images

---

## License

All images generated for MemPalace.NET are licensed under the same MIT license as the project. Attribution to MemPalace.NET and Bruno Capuano is appreciated but not required.

---

## Revision History

- **v1.0** (2026-04-25): Initial prompts for v0.5.0 release
