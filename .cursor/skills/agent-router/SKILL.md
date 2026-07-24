---
name: agent-router
description: >-
  Route SPEC-first Unity agent work to the correct skill flow: SPEC phases,
  grill-with-docs, to-issues, handoff, or unity-spec-dev-workflow. Use when the
  user is unsure which skill to invoke, asks "what flow should I use", or starts
  a new agent session on a SPEC-kit project.
disable-model-invocation: true
---

# Agent Router (SPEC-first)

Read [AGENTS.md](../../AGENTS.md) and [CONTEXT.md](../../CONTEXT.md) before routing.

## Authority

| Need | Source |
|------|--------|
| Game rules | [SPEC_03](../../SPEC_03_GameRules.md) |
| Workflow / difficulty / workload | [SPEC_01](../../SPEC_01_Workflow.md) |
| Unity / assets | [SPEC_04](../../SPEC_04_Technical.md) + [unity-spec-dev-workflow](../unity-spec-dev-workflow/SKILL.md) |
| Vocabulary | [CONTEXT.md](../../CONTEXT.md) |
| Demo tasks | `.scratch/<feature>/issues/` per [issue-tracker.md](../../docs/agents/issue-tracker.md) |

**Forbidden:** `/to-prd` (use SPEC sections). **Forbidden:** upstream `/implement` (use unity-spec-dev-workflow §2).

## Quick routing table

| Situation | Route |
|-----------|-------|
| 规则不清晰 / 新模块 / 需对齐术语 | `/grill-with-docs` 或 `spec-grill-me` → SPEC + CONTEXT + Changelog |
| 规则已清晰，仅写 SPEC | [unity-spec-dev-workflow §1](../unity-spec-dev-workflow/SKILL.md) |
| Demo 多任务 / 大计划拆分 / 工作量须拆步 | `/to-issues` → `.scratch/<feature>/issues/`（须 `spec_refs`） |
| Demo 单任务 Unity 编码 | [unity-spec-dev-workflow §8](../unity-spec-dev-workflow/SKILL.md) → §2 |
| 会话过长 / 换模型 | `/handoff` + `.scratch/handoffs/` |
| 硬 bug / 性能回归 | `diagnosing-bugs` |
| 首次整合 / 重装上游 skills | `matt-skills-setup` |
| 不确定 | 读本表 + 问用户当前阶段（规则录入 / Demo / 验证） |

## Main flow

```text
规则录入:
  规则模糊? → /grill-with-docs 或 spec-grill-me → SPEC + CONTEXT
  规则清晰? → unity-spec-dev-workflow §1

Demo 开发（须负责人授权）:
  工作量须拆步或多切片? → /to-issues → 每 issue 独立会话实现
  单任务且可单次完成? → §8 工作量+难度确认 → §2 实现

跨会话:
  /handoff（保留 SPEC/issue 路径引用，不重复正文）
```

## Encoding guardrails

Never skip [unity-spec-dev-workflow §8](../unity-spec-dev-workflow/SKILL.md):

- Workload "must split" → step prompts + `/to-issues`; current session at most first approved slice
- Difficulty 2/3 → **no code** until user selects approach
- SPEC update before code if design changes

## Deferred skills

| Skill | Enable when |
|-------|-------------|
| `/tdd` | Demo 开发启动 + SPEC_04 测试约定就绪 |
| `/triage` | External issues become intake surface |
| `/prototype` | Runnable answer needed for design question |

## Standalone

- `/grill-me` — no repo / no SPEC (stateless); prefer `/grill-with-docs` or `spec-grill-me` in-repo
- `/handoff` — always available for session bridge
