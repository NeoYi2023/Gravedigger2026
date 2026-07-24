# SPEC 章节速查 / SPEC Section Map

实现前按需读取对应 SPEC 文件；本表仅作索引。复制到项目后按实际章节增补「玩法模块」行。

## 流程与范围

| 主题 | SPEC 位置 | 说明 |
|------|-----------|------|
| Agent 入口与路由 | [AGENTS.md](../../AGENTS.md) | SPEC 权威顺序 |
| Agent flow 路由 | [agent-router](../agent-router/SKILL.md) | 不确定用哪个 skill 时 |
| Matt skills 适配 | [matt-skills-setup](../matt-skills-setup/SKILL.md) | 本地 issue + SPEC-first |
| 领域术语表 | [CONTEXT.md](../../CONTEXT.md) | Glossary，链接 SPEC |
| 本地 issue 约定 | [issue-tracker.md](../../docs/agents/issue-tracker.md) | `spec_refs`、垂直切片 |
| 三阶段开发流程 | [SPEC_01](../../SPEC_01_Workflow.md) | 规则录入 → Demo → 验证 |
| 协作约定 | [SPEC_01 §5](../../SPEC_01_Workflow.md) | Changelog、双语同步 |
| 游戏概述 | [SPEC_02](../../SPEC_02_GameOverview.md) | 平台、定位、核心玩法 |
| Demo 验收标准 | [SPEC_03 §3.8](../../SPEC_03_GameRules.md) | D-xxx 验收项 |
| Demo 实现边界 | [SPEC_04 §6](../../SPEC_04_Technical.md) | 范围内/外 |
| 编程难度分级 | [SPEC_01 §7](../../SPEC_01_Workflow.md) | 1~3；2/3 方案比选 |
| 编码前工作量预估 | [SPEC_01 §7.5](../../SPEC_01_Workflow.md) | 须拆步 → `/to-issues` |
| 方案比选执行细节 | [SKILL §8](SKILL.md) | AskQuestion 题序 |
| Unity C# 规范 | [SKILL §5](SKILL.md) | 命名、结构、性能、审查 |

## 工程基础

| 主题 | SPEC 位置 | 说明 |
|------|-----------|------|
| 工程与环境 | [SPEC_04 §1](../../SPEC_04_Technical.md) | Unity 版本、路径 |
| 目录结构 | [SPEC_04 §2](../../SPEC_04_Technical.md) | Assets 约定 |
| 代码规范 | [SPEC_04 §3](../../SPEC_04_Technical.md) | 命名、命名空间 |
| 跨平台输入 | [SPEC_04 §4](../../SPEC_04_Technical.md) | 输入抽象 |
| 性能与资源 | [SPEC_04 §5](../../SPEC_04_Technical.md) | 对象池等 |
| 版本控制 | [SPEC_04 §7](../../SPEC_04_Technical.md) | .gitignore |
| 本地化 | [SPEC_04 §8](../../SPEC_04_Technical.md) | Key 体系（若启用） |
| 预制体优先 | [SPEC_04 §13](../../SPEC_04_Technical.md) | Prefab / SO 决策 |

## 玩法模块（项目填写）

| 主题 | 规则 (SPEC_03) | 技术 (SPEC_04) |
|------|----------------|----------------|
| Meta 存档（固定 3 槽） | [§3.4](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md) 持久化意图 |
| 工具面板 / 进档壳层 | [§3.5](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md) 壳层 UI |
| GameplayState（Dig / UpgradeManufacture / Defend） | [§3.1](../../SPEC_03_GameRules.md)、[§3.7](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md)、[§13](../../SPEC_04_Technical.md) |
| 关卡阶段流水线 / LevelOperation | [§3.9](../../SPEC_03_GameRules.md) | [§9](../../SPEC_04_Technical.md) |
| 挖坟 Dig / DigMap / Grave | [§3.10](../../SPEC_03_GameRules.md) | [§9](../../SPEC_04_Technical.md)、[§13](../../SPEC_04_Technical.md) |
| 升级与制造 UpgradeManufacture | [§3.11](../../SPEC_03_GameRules.md) | [§9.8](../../SPEC_04_Technical.md) ProtagonistLevelConfig |
| 防守 Defend / BattleMap / StartBattle | [§3.12](../../SPEC_03_GameRules.md) | [§9.7](../../SPEC_04_Technical.md) DefendGameplayConfig + NavMesh |
| Demo 验收 D-001～D-004 | [§3.8](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md) |

## Changelog

所有 SPEC 变更记录在 [SPEC_00 §4](../../SPEC_00_Index.md)。
