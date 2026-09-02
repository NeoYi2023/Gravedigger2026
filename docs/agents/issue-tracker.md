# Local issue tracker

Demo / coding slices live under `.scratch/<feature-slug>/issues/`.

## Frontmatter (required)

```yaml
---
title: Short verb phrase
status: todo
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-001
  - SPEC_04 §6
---
```

- `spec_refs` must point to real SPEC sections or acceptance IDs
- `demo_scope: in-scope` must match SPEC_03 §3.8
- One issue = one end-to-end verifiable vertical slice

## Acceptance (config tables)

When an issue touches `ConfigTables/**`:

- [ ] Excel updated (rows 1–2 doc header preserved); see [SPEC_04 §14.7](../../SPEC_04_Technical.md)
- [ ] CSV baked from Excel (Mode1 and/or Mode2 as scoped)
- [ ] No CSV-only delivery without Excel sync

## Workflow

1. Prefer `/to-issues` after workload split or multi-task planning
2. Implement at most one unblocked issue per session
3. Do not use `/to-prd` — SPEC sections are the PRD
