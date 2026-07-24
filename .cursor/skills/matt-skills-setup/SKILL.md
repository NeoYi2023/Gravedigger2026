---
name: matt-skills-setup
description: >-
  Configure a SPEC-first Unity repo for mattpocock engineering skills: local
  markdown issues, CONTEXT glossary, docs/agents/*. Run once per repo or after
  reinstalling upstream skills. Use when setting up agent workflow or re-running
  Matt skills setup.
disable-model-invocation: true
---

# Matt Skills Setup (SPEC-first)

Scaffold per-repo configuration for [mattpocock/skills](https://github.com/mattpocock/skills) **without** replacing SPEC authority.

**Do not run upstream `/setup-matt-pocock-skills` alone** — it assumes GitHub issues and standalone CONTEXT authority. Use this skill instead.

## Preconditions

- Upstream skills installed under `.agents/skills/` (via `npx skills@latest add mattpocock/skills`)
- Project skills under `.cursor/skills/` (from SPECandSKILL kit)

## Process

### 1. Explore

Read existing files (do not overwrite user edits blindly):

- `AGENTS.md`, `CONTEXT.md`, `docs/agents/`, `.scratch/`, `.gitignore`

### 2. Confirm with user (one section at a time)

**A — Issue tracker:** Local markdown only → `docs/agents/issue-tracker.md`. Issues under `.scratch/<feature-slug>/issues/`.

**B — Triage labels:** Default five roles → `docs/agents/triage-labels.md`. `/triage` disabled until needed.

**C — Domain docs:** SPEC-first → `docs/agents/domain.md`. `CONTEXT.md` + `docs/adr/`.

### 3. Write or repair

| File | Purpose |
|------|---------|
| `AGENTS.md` | Routing + authority order |
| `CONTEXT.md` | Glossary with SPEC links |
| `docs/agents/issue-tracker.md` | Local issue format + `spec_refs` |
| `docs/agents/domain.md` | SPEC > CONTEXT > ADR |
| `docs/agents/triage-labels.md` | Status strings |
| `.scratch/issues/.gitkeep` | Issue root placeholder |
| `.scratch/handoffs/.gitkeep` | Handoff summaries |

Append to `.gitignore` if missing:

```
.scratch/handoffs/
.scratch/**/PRD.md
.scratch/**/.draft/
```

### 4. Done

Tell the user:

- Use `agent-router` when unsure which flow to run
- Grilling → SPEC + CONTEXT; not `/to-prd`
- Demo tasks → `/to-issues` with `spec_refs`
- Encoding → `unity-spec-dev-workflow` §8 + §2
