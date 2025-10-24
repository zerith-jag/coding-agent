# Documentation Style Guide

This guide keeps our documentation consistent, readable, and easy to maintain.

## Principles

- Be clear, concise, and concrete.
- Prefer examples over theory.
- Optimize for skimmability: headings, lists, and short paragraphs.
- Keep docs current: update "Last Updated" on each edit.

## Document Header

Each major doc should start with a short metadata block:

```text
**Status**: Draft | In Progress | Complete
**Version**: x.y.z
**Last Updated**: <Month DD, YYYY>
```

Place this block immediately below the H1 title.

## File & Folder Conventions

- Use `kebab-case` filenames, with numeric prefixes for ordered series (e.g., `00-overview.md`).
- Keep one primary topic per file.
- Images/diagrams go under `docs/assets/`.

## Markdown Rules

- One `# H1` per file (the title). Use `##` and `###` for subsections.
- Surround headings and lists with blank lines (Markdownlint MD022/MD032).
- Keep lines reasonably short (wrap around ~120 chars).
- Always specify a language for fenced code blocks (e.g., ` ```powershell`, ` ```yaml`, ` ```csharp`).
- Use backticks for inline code and placeholders (avoid `<angle-bracket>` placeholders).
- Prefer relative links within the repo (e.g., `./01-service-catalog.md`).
- Tables are fine but keep them simple and scannable.

## Linking & Cross-References

- Link to local docs using relative paths.
- For external links, prefer stable docs (versioned URLs when possible).
- When referencing non-existent future docs, clearly mark as TODO and add to the roadmap.

## Diagrams

- Prefer PlantUML (`.puml`) or Mermaid fenced blocks.
- Store source files in `docs/architecture/diagrams/` and export images as needed to `docs/assets/`.

## Tone & Voice

- Address the reader directly ("you").
- Use present tense and active voice.
- Avoid hype; be specific and honest about trade-offs.

## Review & Ownership

- Add a "Document Owner" and "Review Cycle" (monthly/bi-weekly) near the end of major docs.
- PRs that change behavior should update related docs in the same PR.

## Common Sections (when relevant)

- Executive Summary
- Architecture Overview / Context
- Assumptions & Non-Goals
- API/Contracts
- Deployment / Operations / Runbooks
- Security & Compliance Considerations
- Open Questions / Next Steps

## Lint & Link Checks

- The repo runs Markdown lint and link checks in CI on PRs (`.github/workflows/docs-ci.yml`).
- Fix lint warnings (headings/lists spacing, fenced code language, etc.).
- For flaky external links, prefer documenting the stable resource or add a note.

## Examples

### Good Inline Code

- Use: `Fixes #123` instead of `Fixes #<issue-number>`
- Use: `commit-sha-or-tag` instead of `<commit SHA or tag>`

### Good Code Fence

```powershell
# Start shared infrastructure
docker compose -f deployment/docker-compose/docker-compose.dev.yml up -d
```

### Good Sectioning

```markdown
# Service Catalog

**Status**: Complete
**Version**: 2.0.0
**Last Updated**: October 24, 2025

## API Endpoints
...
```
