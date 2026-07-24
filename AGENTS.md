# Gravedigger2026 — Agent 指引

**权威顺序：** `SPEC_*.md` > `CONTEXT.md`（术语索引）> `docs/adr/`（架构决策）> 本文件

当前阶段：**规则录入**。Unity 编码须负责人明确授权 Demo 开发。

**SPEC 维护：**
- 套件专属目录：`E:\Work\Cursor\SPECandSKILL\Gravedigger2026\`
- 日常开发以工作区根（`E:\Work\Cursor\Gravedigger2026\Gravedigger2026`）下已复制的 `SPEC_*.md` 为准；重要变更请同步回套件目录。

## 三阶段 SPEC 流程

| 阶段 | 目标 | 主要 Skill |
|------|------|------------|
| 1 规则录入 | 结构化写入 SPEC_03，禁止编码 | `/grill-with-docs` 或 `spec-grill-me`；[unity-spec-dev-workflow](.cursor/skills/unity-spec-dev-workflow/SKILL.md) §1 |
| 2 Demo 开发 | SPEC 优先 → 垂直切片 → 实现 | `/to-issues`；`unity-spec-dev-workflow` §8 + §2 |
| 3 验证与决策 | 对照 SPEC_03 §3.8 验收 | 负责人验证；结论写入 Changelog |

不确定用哪个 flow → **`agent-router`**

## Issue tracker

Demo 任务：`.scratch/<feature-slug>/issues/`。见 [`docs/agents/issue-tracker.md`](docs/agents/issue-tracker.md)。**不使用** `/to-prd`。

## Domain docs

`CONTEXT.md` 为术语表。见 [`docs/agents/domain.md`](docs/agents/domain.md)。

## Matt 工程技能（可选）

安装到 `.agents/skills/` 后，用 [matt-skills-setup](.cursor/skills/matt-skills-setup/SKILL.md) 适配。编码走 `unity-spec-dev-workflow` §2，不用上游 `/implement`。

## 强制覆盖规则

1. Grilling → SPEC（双语 + Changelog）；CONTEXT 只收术语
2. Issue 须含 `spec_refs`；不超出 SPEC_03 §3.8
3. 编码前 → §8 工作量 + 难度；须拆步 → `/to-issues`；2/3 → 方案比选
4. Handoff → `.scratch/handoffs/`
5. 选项确认 → `AskQuestion`

## 项目 Skills

- `unity-spec-dev-workflow`
- `agent-router`
- `spec-grill-me`
- `matt-skills-setup`
