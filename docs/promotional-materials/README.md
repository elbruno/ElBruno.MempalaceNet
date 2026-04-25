# Promotional Materials for MemPalace.NET v0.5.0

This directory contains promotional materials for the MemPalace.NET v0.5.0 release.

## Contents

### Social Media Posts

- **[linkedin-post.md](linkedin-post.md)** — LinkedIn announcement post (280 characters) with extended version and hashtags
- **[twitter-post.md](twitter-post.md)** — Twitter/X post (280 characters) with optional thread and hashtags

### Blog Content

- **[blog-announcement.md](blog-announcement.md)** — Full blog post (800-1000 words) covering:
  - What's new in v0.5.0
  - Why local semantic memory matters
  - Getting started guide
  - Architecture highlights
  - Roadmap and next steps

### Image Assets

- **[image-generation-prompts.md](image-generation-prompts.md)** — Detailed prompts for generating:
  - Logo image (1024x1024) for NuGet package
  - LinkedIn hero banner (1200x628)
  - Twitter card (1024x512)
  - Blog header image (1200x400)

## Using These Materials

### Social Media

1. **LinkedIn**: Copy the 273-character post from `linkedin-post.md` and include the NuGet package link
2. **Twitter/X**: Use the 280-character post from `twitter-post.md`, optionally follow up with the thread
3. **Attach Images**: Generate images using prompts from `image-generation-prompts.md` or use placeholders

### Blog Publishing

1. Copy content from `blog-announcement.md`
2. Generate the blog header image using the prompt provided
3. Add code syntax highlighting as appropriate
4. Include links to GitHub and NuGet

### Image Generation

See `image-generation-prompts.md` for:
- Detailed prompts for each image type
- Recommended tools (DALL-E 3, Midjourney, Stable Diffusion)
- CLI command examples
- File organization structure
- Fallback placeholder options

## Image Directory Structure

Generated images should be saved to:

```
docs/promotional-materials/images/
├── mempalacenet-logo-1024.png          # Logo (1024x1024)
├── linkedin-hero-1200x628.png          # LinkedIn banner (1200x628)
├── twitter-card-1024x512.png           # Twitter card (1024x512)
└── blog-header-1200x400.png            # Blog header (1200x400)
```

## Brand Guidelines

### Colors

- **Primary Purple:** #512BD4 (.NET brand color)
- **Primary Blue:** #0078D4 (Microsoft blue)
- **Accent Purple:** #8b5cf6
- **Accent Blue:** #3b82f6
- **Dark Background:** #0a0e27 to #2d1b69 (gradient)

### Typography

- **Headings:** Modern sans-serif (Segoe UI, Inter, or similar)
- **Body:** Clean, readable font
- **Code:** Monospace (Consolas, Fira Code)

### Key Messages

- **Local-first:** No cloud dependencies, data stays on your machine
- **Semantic memory:** Store verbatim, search semantically
- **Microsoft integrations:** Extensions.AI + Agent Framework
- **Hierarchical organization:** Wings → Rooms → Drawers
- **Open source:** MIT license, community-driven

## Publishing Checklist

- [ ] Generate all images using prompts
- [ ] Test social media posts for character count
- [ ] Review blog post for accuracy and completeness
- [ ] Add code examples to blog post
- [ ] Include proper links (NuGet, GitHub)
- [ ] Schedule posts for coordinated release
- [ ] Monitor engagement and respond to questions

## Questions?

For questions about promotional materials, contact Bruno Capuano or open an issue on GitHub.

---

**License:** All promotional materials are released under the MIT license, consistent with the MemPalace.NET project.
