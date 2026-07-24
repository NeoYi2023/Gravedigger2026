---
name: spec-grill-me
description: >-
  SPEC 规则录入访谈：一次一问、每敲定一项即写入 SPEC_03/SPEC_04 与 Changelog。
  用于新模块录入、规则模糊需边问边写 SPEC 时。用户提及 grill-me、规则录入访谈、
  新玩法录入时使用。
disable-model-invocation: true
---

# Spec Grill-me — 边访谈边编辑 SPEC

将 grilling 纪律适配为 **规则录入阶段**（[SPEC_01 §2](../../SPEC_01_Workflow.md)）。

**权威：** `SPEC_*.md` > [CONTEXT.md](../../CONTEXT.md)（仅术语）> `docs/adr/`（仅架构决策）

**禁止：** 本阶段编写游戏代码；禁止只更新中文块；禁止一次抛出多个问题。

## 何时使用

| 场景 | 使用 |
|------|------|
| 新模块，尚无 SPEC 章节 | **本 Skill** |
| 规则已清晰，仅需结构化写入 | [unity-spec-dev-workflow §1](../unity-spec-dev-workflow/SKILL.md) |
| 术语/ADR 对齐为主 | `/grill-with-docs`（产出仍须落 SPEC） |

## 会话启动

1. 请负责人提供 **模块名称 + 1~3 句描述**
2. 预读：[spec-map.md](../unity-spec-dev-workflow/spec-map.md)、[SPEC_02](../../SPEC_02_GameOverview.md)、[SPEC_03](../../SPEC_03_GameRules.md)
3. 分配章节号：从 SPEC_03 下一个可用 `## 3.x` 起；若属已有大节扩展则增小节
4. 插入占位骨架（状态 `未定义 / Undefined`）

## Grilling 纪律

- **一次只问一个问题**；每问附带 **推荐答案**
- 能靠读 SPEC 回答的，先探索再提问
- 用具体场景压测边界；结论写入 SPEC，术语同步 [CONTEXT.md](../../CONTEXT.md) 一行

## 访谈决策树（顺序）

| 顺序 | 议题 | 写入位置 |
|------|------|----------|
| 1 | 定位与边界 | 新节开篇 + 交叉引用核心循环 |
| 2 | 触发与结束 | 流程表 |
| 3 | 实体与状态 | 实体/状态小节 |
| 4 | 资源与经济 | 相关小节或新节 |
| 5 | UI/UX | UI 清单新 ID |
| 6 | 与已有系统联动 | 交叉引用 |
| 7 | 数据结构 | SPEC_04 |
| 8 | Demo 验收项 | SPEC_03 §3.8（若属 Demo） |
| 9 | 实现优先级 | P0–P4 表 |

未决项标 `TBD`；节末维护 **待澄清清单**。

## 每次敲定后（inline）

1. [SPEC_03](../../SPEC_03_GameRules.md) — 中英文双块同步
2. [SPEC_04](../../SPEC_04_Technical.md) — 若涉及技术约定
3. [SPEC_00 Changelog](../../SPEC_00_Index.md) — 日期与摘要
4. 回复：本问结论、已写章节、剩余 TBD

## Definition of Done

- [ ] 实现者可仅凭 SPEC 实现，无需再问业务规则
- [ ] P0 路径有流程表 / 状态图 / 数据结构
- [ ] 交叉引用无矛盾
- [ ] 若属 Demo：§3.8 有验收项
- [ ] 中英文同步；Changelog 已记录

## 长会话

接近 context 上限 → `/handoff`，摘要复制至 `.scratch/handoffs/`。
