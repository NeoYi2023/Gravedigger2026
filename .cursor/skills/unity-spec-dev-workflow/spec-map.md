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
| 角色美术管线 / 烘焙整角 | [SPEC_04 §15](../../SPEC_04_Technical.md) | Character Creator；工具目录禁入；导出补丁→Art/Characters→Prefabs；[§15.2](../../SPEC_04_Technical.md) AllIn1 VisualStyle 新增预设流程 |
| 代码规范 | [SPEC_04 §3](../../SPEC_04_Technical.md) | 命名、命名空间 |
| 跨平台输入 | [SPEC_04 §4](../../SPEC_04_Technical.md) | 输入抽象占位；UI-024 PlayerPointer 硬件光标 |
| 性能与资源 | [SPEC_04 §5](../../SPEC_04_Technical.md) | 对象池等 |
| 版本控制 | [SPEC_04 §7](../../SPEC_04_Technical.md) | .gitignore |
| 本地化 | [SPEC_04 §8](../../SPEC_04_Technical.md) | Key 体系（若启用） |
| 预制体优先 | [SPEC_04 §13](../../SPEC_04_Technical.md) | Prefab / ConfigTables / SO 决策 |

## 玩法模块（项目填写）

| 主题 | 规则 (SPEC_03) | 技术 (SPEC_04) |
|------|----------------|----------------|
| Meta 存档（固定 3 槽） | [§3.4](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md) 持久化意图 |
| Title 登录设置 / 显示（UI-028） | [§3.6](../../SPEC_03_GameRules.md) UI-028 | [§6](../../SPEC_04_Technical.md) `DisplaySettingsService` 机台级 |
| 工具面板 / 进档壳层 | [§3.5](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md) 壳层 UI；UI-022 / UI-023 装备仓只读 + 魔法书 BookRow 排序/删除（D-068 / D-072）；UI-024 运行时光标 |
| GameplayState（Dig / AutoManufacture / UpgradeManufacture / Defend / PushMap / Shop） | [§3.1](../../SPEC_03_GameRules.md)、[§3.7](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md)、[§13](../../SPEC_04_Technical.md) |
| 关卡阶段流水线 / LevelOperation | [§3.9](../../SPEC_03_GameRules.md) | [§9](../../SPEC_04_Technical.md) |
| 挖坟 Dig / DigMap / Grave | [§3.10](../../SPEC_03_GameRules.md) | [§9.2](../../SPEC_04_Technical.md) DigMapId→`Prefabs/Maps/Ground_*`；镜头迷雾 `CameraFogOverlay`←`Art/Maps/Fogs/Fog_1.png`；[§13](../../SPEC_04_Technical.md)（含 FlowingWater 两层 Tilemap、MapAutoTile / Isometric Rule Tile、MapEdgeFog 世界空间边缘雾）；[§15](../../SPEC_04_Technical.md) Digger 烘焙整角 |
| 升级与制造 UpgradeManufacture | [§3.11](../../SPEC_03_GameRules.md) | [§9.8](../../SPEC_04_Technical.md) ProtagonistLevelConfig；[§9.9](../../SPEC_04_Technical.md) SoulConfig / WarriorInstance；[§9.9b](../../SPEC_04_Technical.md) ClassConfig；[§9.10](../../SPEC_04_Technical.md) GemConfig；[§9.11](../../SPEC_04_Technical.md) RaceConfig；[§9.12](../../SPEC_04_Technical.md) BodyPartConfig；[§9.13](../../SPEC_04_Technical.md) BodyAppearanceConfig；[§9.14](../../SPEC_04_Technical.md) ExtraEquipmentConfig；[§9.15](../../SPEC_04_Technical.md) GemSuffixNameConfig；[§9.20](../../SPEC_04_Technical.md) LossOfControlConfig；[§9.21](../../SPEC_04_Technical.md) SkillConfig；[§9.21b](../../SPEC_04_Technical.md) SkillEffectConfig；UI-021 / D-065 Mode2 士兵栏悬浮框 **完成**；D-070 技能 Icon 实现状态指示器 **完成** |
| Mode2 自动制造 AutoManufacture | [§3.15](../../SPEC_03_GameRules.md) | [§9.9b](../../SPEC_04_Technical.md) ClassConfig（AttackMode/PlacementOrder/DefaultAppearanceId/DefaultSkillIds）；[§9.12](../../SPEC_04_Technical.md) BodyPart 扩列；[§9.24](../../SPEC_04_Technical.md) MagicBookConfig（含 SoldierSkillLevelAdd / ForceClass / VisualStyle 列 / `Style_ScaleModel` 放大通道）；[§15.2](../../SPEC_04_Technical.md) AllIn1 VisualStyle Catalog；[§13](../../SPEC_04_Technical.md) FormationClassZone（IsoDiamond，同 WalkSurface）；[§6](../../SPEC_04_Technical.md) AutoManufactureBatch；UI-015 / D-054 制造记录；UI-016 / D-055 演出；D-066 放大模型 **完成**；UI-023 / D-068 共享 BookRow + TrySwap + D-072 弹窗删除；issues `.scratch/mode2-auto-manufacture/`、`.scratch/visual-scale-model/`、`.scratch/insave-equip-magicbook-ui/`；职业区改形 `.scratch/formation-class-zone-isodiamond/` |
| 主角装备 ProtagonistEquipment | [§3.16](../../SPEC_03_GameRules.md) | [§9.25](../../SPEC_04_Technical.md) ProtagonistEquipmentConfig；[§9.6](../../SPEC_04_Technical.md) Dig caps 与科技加法；Dig 事件 Token `DigOnGraveClear` / `Explosive*`（D-077）、`DigLightning*`（D-078 `Equip_Elctr`）；静态键 `DigProcessSpawnCountBonus`（D-079 `Equip_Detector`）、`GraveSpawnWeightBonus` 种族信物（D-080 Human/Elf/Orc）；[§6](../../SPEC_04_Technical.md) EquipCommonExp / ProtagonistEquipmentWarehouse 持久化意图；UI-022 / D-067 仓只读弹窗 |
| 士兵属性构成 | [§3.11](../../SPEC_03_GameRules.md) 士兵属性构成 | [§9.9](../../SPEC_04_Technical.md)–[§9.15](../../SPEC_04_Technical.md)、[§9.20b](../../SPEC_04_Technical.md)；Base(S)=Σ StatBonus；StaticStat / FinalStat；ClassId→ClassConfig（PrimaryStat；CombatConvertCoeffs；AttackRange；DefaultSkillIds）；缺键回退 CombatConstantConfig；MaxHP=ceil(BodyLife+Str×MaxHpStrengthMult)；多宝石 Σ GemMult；SoldierSkills |
| 士兵制造流程 / 槽位 / 命名 | [§3.11](../../SPEC_03_GameRules.md) 制造士兵 | 槽位与最低要求（躯干+臂2+腿2；灵魂可选→Soul_00+Class_Servants）；SpiritCost 闸门；部位加权定种族；外观选取（含保底；ClassAffinity→ClassConfig.ClassName）；WarriorName；`AppearanceId`→`Prefabs/Defend/Warriors/`（[§15](../../SPEC_04_Technical.md)）；制造授予 SoldierSkills |
| 士兵技能 SoldierSkill | [§3.11](../../SPEC_03_GameRules.md) 士兵技能授予、[§3.12](../../SPEC_03_GameRules.md) SkillCast、[§3.15](../../SPEC_03_GameRules.md) DefaultSkillIds / SoldierSkillLevelAdd | [§9.9](../../SPEC_04_Technical.md) WarriorInstance.SoldierSkills；[§9.9b](../../SPEC_04_Technical.md) DefaultSkillIds；[§9.21](../../SPEC_04_Technical.md) SkillConfig；[§9.21b](../../SPEC_04_Technical.md) SkillEffectConfig（EffectKind 登记制）；[§9.24](../../SPEC_04_Technical.md) SoldierSkillLevelAdd；D-069 PushMap `Skill_03` 连发 + `Skill_01` 格挡 + `Skill_02` 舒适；**D-073 Skill_04～12 EffectKind + CombatStatusService**（SE-00～09 **完成**；issues `.scratch/soldier-skill-effects/`） |
| 控制力 / 失控 / 叛变 | [§3.11](../../SPEC_03_GameRules.md) 控制力与失控、[§3.12](../../SPEC_03_GameRules.md) 失控判定与叛变 | Degree=ΣCost/Cap−1；四档；开战锁定 roll；FinalChance；Rebel 就近；[§9.20](../../SPEC_04_Technical.md) / [§9.21](../../SPEC_04_Technical.md) |
| 防守 Defend / BattleMap / StartBattle / Shield / 刷怪 | [§3.12](../../SPEC_03_GameRules.md) | [§9.7](../../SPEC_04_Technical.md) DefendGameplayConfig（`BattleMapId`→`Prefabs/Maps/Ground_*`）；[§9.18](../../SPEC_04_Technical.md) WaveSpawnConfig；[§9.19](../../SPEC_04_Technical.md) MonsterConfig + NavMesh；失控见 [§9.20](../../SPEC_04_Technical.md)；角色视觉 [§15](../../SPEC_04_Technical.md) |
| 士兵战斗 / EngageZone / 命中方案 D / 死亡分层 / 战斗派生公式 | [§3.12](../../SPEC_03_GameRules.md) WarriorCombat / SkillCast、[§3.11](../../SPEC_03_GameRules.md) 士兵死亡与材料去向 | EngageZone Prefab；SoulConfig.AttackMode；ClassConfig.PrimaryStat + AttackRange 等；近战前摇 / 远程弹道；法师=远程+Intelligence（同射手通道）；**D-069 PushMap Skill_03 连发 + Skill_01 格挡 + Skill_02 舒适**；**D-071 CombatSkillIcon**（头顶飘 / 脚下持续）；**D-073 Skill_04～12 EffectKind 管线**；CombatDead vs PermanentDeath；宝石特例；NormalAttackPower / AttackSpeed / SkillCooldown（CooldownMode Mode2 提交后进 CD）；SkillConfig / SkillEffectConfig |
| 阵容羁绊 FormationBond | [§3.17](../../SPEC_03_GameRules.md) | [§9.26](../../SPEC_04_Technical.md) FormationBondConfig；BondBuff FK SkillEffectConfig；FormationBondEvaluator；布阵/战斗左上 HUD；issues `.scratch/formation-bond/` |
| 大规模战斗寻路 MassCombatPathing（方案 B） | [§3.12](../../SPEC_03_GameRules.md) MassCombatPathing、[§3.14](../../SPEC_03_GameRules.md) 士兵推进 | [§9.7](../../SPEC_04_Technical.md) FlowField / AttackSlot / LocalDetour；issues `.scratch/mass-pathing/` |
| 科技树 TechTree / TechItem / TechEffect | [§3.13](../../SPEC_03_GameRules.md) | [§9.16](../../SPEC_04_Technical.md) TechTreeConfig；[§9.17](../../SPEC_04_Technical.md) TechEffectConfig |
| 推图战 PushMap / ObjectivePoint / CaptureZone / AggroMode / CameraFollowPath | [§3.14](../../SPEC_03_GameRules.md) | [§9.22](../../SPEC_04_Technical.md) PushMapGameplayConfig；[§9.23](../../SPEC_04_Technical.md) PushMapSpawnConfig；[§9.19](../../SPEC_04_Technical.md) MonsterConfig.AggroMode/AlertRadius；地图标记 [§13](../../SPEC_04_Technical.md)；Combat Auto 镜头轨 `CameraFollowPath` |
| Mode2 商店 Shop / ShopStageRoot | [§3.5](../../SPEC_03_GameRules.md)、[§3.9](../../SPEC_03_GameRules.md)、UI-026 | [§9.1](../../SPEC_04_Technical.md) `GameplayType=Shop`；[§9.27](../../SPEC_04_Technical.md)/[§9.28](../../SPEC_04_Technical.md) 商品池/刷新价；[§10](../../SPEC_04_Technical.md) Prefab + `ShopStageModule` + `ShopSellService`；D-075 / D-076 |
| BGM / Audio_BgmConfig | [§3.4](../../SPEC_03_GameRules.md) | [§9.29](../../SPEC_04_Technical.md) `Audio_BgmConfig`；[§6](../../SPEC_04_Technical.md) `BgmService` / `BgmClipCatalog` |
| Demo 验收 D-001～D-045 / D-050～D-076 | [§3.8](../../SPEC_03_GameRules.md) | [§6](../../SPEC_04_Technical.md) |

## Changelog

所有 SPEC 变更记录在 [SPEC_00 §4](../../SPEC_00_Index.md)。
