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
| 配置表工程约定 / 打表 | [SPEC_04 §14](../../SPEC_04_Technical.md) | Excel 四段中英名 + CSV 两段英文名、Bake Tables 菜单 |
| 角色美术管线 / 烘焙整角 | [SPEC_04 §15](../../SPEC_04_Technical.md) | Character Creator；工具目录禁入；导出补丁→Art/Characters→Prefabs |
| 代码规范 | [SPEC_04 §3](../../SPEC_04_Technical.md) | 命名、命名空间 |
| 跨平台输入 | [SPEC_04 §4](../../SPEC_04_Technical.md) | 输入抽象 |
| 性能与资源 | [SPEC_04 §5](../../SPEC_04_Technical.md) | 对象池等 |
| 版本控制 | [SPEC_04 §7](../../SPEC_04_Technical.md) | .gitignore |
| 本地化 | [SPEC_04 §8](../../SPEC_04_Technical.md) | Key 体系（若启用） |
| 预制体优先 | [SPEC_04 §13](../../SPEC_04_Technical.md) | Prefab / ConfigTables / SO 决策 |

## 玩法模块（项目填写）

| 主题 | 规则 (SPEC_03) | 技术 (SPEC_04) |
|------|----------------|----------------|
| Meta 存档（固定 3 槽） | [§3.4](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md) 持久化意图 |
| 工具面板 / 进档壳层 | [§3.5](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md) 壳层 UI |
| GameplayState（Dig / UpgradeManufacture / Defend） | [§3.1](../../SPEC_03_GameRules.md)、[§3.7](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md)、[§13](../../SPEC_04_Technical.md) |
| 关卡阶段流水线 / LevelOperation | [§3.9](../../SPEC_03_GameRules.md) | [§9](../../SPEC_04_Technical.md) |
| 挖坟 Dig / DigMap / Grave | [§3.10](../../SPEC_03_GameRules.md) | [§9.2](../../SPEC_04_Technical.md) DigMapId→`Prefabs/Maps/Ground_*`；[§13](../../SPEC_04_Technical.md)；[§15](../../SPEC_04_Technical.md) Digger 烘焙整角 |
| 升级与制造 UpgradeManufacture | [§3.11](../../SPEC_03_GameRules.md) | [§9.8](../../SPEC_04_Technical.md) ProtagonistLevelConfig；[§9.9](../../SPEC_04_Technical.md) SoulConfig / WarriorInstance；[§9.9b](../../SPEC_04_Technical.md) ClassConfig；[§9.10](../../SPEC_04_Technical.md) GemConfig；[§9.11](../../SPEC_04_Technical.md) RaceConfig；[§9.12](../../SPEC_04_Technical.md) BodyPartConfig；[§9.13](../../SPEC_04_Technical.md) BodyAppearanceConfig；[§9.14](../../SPEC_04_Technical.md) ExtraEquipmentConfig；[§9.15](../../SPEC_04_Technical.md) GemSuffixNameConfig；[§9.20](../../SPEC_04_Technical.md) LossOfControlConfig；[§9.21](../../SPEC_04_Technical.md) SkillConfig 骨架 |
| 士兵属性构成 | [§3.11](../../SPEC_03_GameRules.md) 士兵属性构成 | [§9.9](../../SPEC_04_Technical.md)–[§9.15](../../SPEC_04_Technical.md)；Base(S)=Σ StatBonus；StaticStat / FinalStat；ClassId→ClassConfig（PrimaryStat；CombatConvertCoeffs；AttackRange 等）；MaxHP=ceil(BodyLife+Str×3)；多宝石 Σ GemMult |
| 士兵制造流程 / 槽位 / 命名 | [§3.11](../../SPEC_03_GameRules.md) 制造士兵 | 槽位与最低要求；SpiritCost 闸门；部位加权定种族；外观选取（含保底；ClassAffinity→ClassConfig.ClassName）；WarriorName；`AppearanceId`→`Prefabs/Defend/Warriors/`（[§15](../../SPEC_04_Technical.md)） |
| 控制力 / 失控 / 叛变 | [§3.11](../../SPEC_03_GameRules.md) 控制力与失控、[§3.12](../../SPEC_03_GameRules.md) 失控判定与叛变 | Degree=ΣCost/Cap−1；四档；开战锁定 roll；FinalChance；Rebel 就近；[§9.20](../../SPEC_04_Technical.md) / [§9.21](../../SPEC_04_Technical.md) |
| 防守 Defend / BattleMap / StartBattle / Shield / 刷怪 | [§3.12](../../SPEC_03_GameRules.md) | [§9.7](../../SPEC_04_Technical.md) DefendGameplayConfig（`BattleMapId`→`Prefabs/Maps/Ground_*`）；[§9.18](../../SPEC_04_Technical.md) WaveSpawnConfig；[§9.19](../../SPEC_04_Technical.md) MonsterConfig + NavMesh；失控见 [§9.20](../../SPEC_04_Technical.md)；角色视觉 [§15](../../SPEC_04_Technical.md) |
| 士兵战斗 / EngageZone / 命中方案 D / 死亡分层 / 战斗派生公式 | [§3.12](../../SPEC_03_GameRules.md) WarriorCombat、[§3.11](../../SPEC_03_GameRules.md) 士兵死亡与材料去向 | EngageZone Prefab；SoulConfig.AttackMode；ClassConfig.PrimaryStat + AttackRange 等；近战前摇 / 远程弹道；法师=远程+Intelligence（同射手通道）；**Demo v1 仅普攻**；CombatDead vs PermanentDeath；宝石特例；NormalAttackPower / AttackSpeed / SkillCooldown（CombatConvertCoeffs；Demo 不驱动技能） |
| 科技树 TechTree / TechItem / TechEffect | [§3.13](../../SPEC_03_GameRules.md) | [§9.16](../../SPEC_04_Technical.md) TechTreeConfig；[§9.17](../../SPEC_04_Technical.md) TechEffectConfig |
| Demo 验收 D-001～D-004 | [§3.8](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md) |

## Changelog

所有 SPEC 变更记录在 [SPEC_00 §4](../../SPEC_00_Index.md)。
