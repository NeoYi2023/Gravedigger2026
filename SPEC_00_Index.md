# Gravedigger2026 — SPEC 总索引 / SPEC Master Index

**文档版本 / Document Version:** v0.36.0
**最后更新 / Last Updated:** 2026-07-25  
**当前阶段 / Current Phase:** Demo 开发 / Demo development（Dig D-020 + UM D-030～D-032 落地；Defend 待续）  

**套件维护路径：** `F:\CursorGame_Git\SPECandSKILL\Gravedigger2026\`  
**日常开发权威：** 复制到 Cursor 工作区根后的 `SPEC_*.md`（工作区：`F:\CursorGame_Git\Gravedigger2026`）

---

## 1. 文档说明

### 简体中文

本项目的 `SPEC_*.md` 是 **Gravedigger2026** 的唯一权威设计文档。公共流程模板来自 `SPECandSKILL\SPEC\`；本目录为项目专属内容。双语规范见公共模板约定。

### English

`SPEC_*.md` is the single source of truth for **Gravedigger2026**. Common workflow templates live in `SPECandSKILL\SPEC\`; this folder holds project-specific content.

---

## 2. 文件清单与阅读顺序

### 简体中文

| 序号 | 文件 | 说明 |
|------|------|------|
| 00 | [SPEC_00_Index.md](SPEC_00_Index.md) | 总索引、变更日志（本文件） |
| 01 | [SPEC_01_Workflow.md](SPEC_01_Workflow.md) | 三阶段开发流程与协作约定 |
| 02 | [SPEC_02_GameOverview.md](SPEC_02_GameOverview.md) | 游戏概述、平台与定位 |
| 03 | [SPEC_03_GameRules.md](SPEC_03_GameRules.md) | 游戏规则主体（Demo 外壳 + 关卡阶段 / 挖坟 / 升级与制造 / 防守 / 科技树框架） |
| 04 | [SPEC_04_Technical.md](SPEC_04_Technical.md) | 技术规范、Demo 边界、配置表字段与工程约定（§14） |

**建议阅读顺序：** 01 → 02 → 03 → 04。

### English

| No. | File | Description |
|-----|------|-------------|
| 00 | [SPEC_00_Index.md](SPEC_00_Index.md) | Master index, changelog |
| 01 | [SPEC_01_Workflow.md](SPEC_01_Workflow.md) | Three-phase workflow |
| 02 | [SPEC_02_GameOverview.md](SPEC_02_GameOverview.md) | Game overview |
| 03 | [SPEC_03_GameRules.md](SPEC_03_GameRules.md) | Game rules (Demo shell + Level stages / Dig / UpgradeManufacture / Defend / TechTree framework) |
| 04 | [SPEC_04_Technical.md](SPEC_04_Technical.md) | Technical standards, Demo boundary, config tables + engineering rules (§14) |

---

## 3. 双语书写规范

### 简体中文

- 每个一级章节含 `### 简体中文` 与 `### English`；变更须双块同步并记入 Changelog。

### English

- Each top-level section has both language blocks; keep them in sync and log changes.

---

## 4. 变更日志 / Changelog

### 简体中文

| 日期 | 版本 | 摘要（中文） |
|------|------|-------------|
| 2026-07-25 | v0.36.0 | UM 布阵区方案 A：`BattleFormationService`（连续坐标上阵/下阵/改位 + 控制力占用）+ `FormationPanelView`；与士兵池打通；存档级持有供 Defend Prepare 共用；同步 SPEC_03 §3.8 D-032、SPEC_04 §6 |
| 2026-07-25 | v0.35.0 | UM 制造区方案 A：`ManufactureService`（15 严格槽位 / 预览 / 精魂闸门）+ `WarriorPoolService`；追加加载 Soul/Class/Gem/Race/BodyPart/Appearance/Equip/GemSuffix 八表；`WarehouseService` 支持 BodyPart 入账与按 Id 扣减；临时 `Prefabs/Defend/Warriors/{AppearanceId}`；同步 SPEC_03 §3.8 D-031、SPEC_04 §6 |
| 2026-07-25 | v0.34.0 | UM 升级区方案 A：`UpgradeManufactureStageModule` + `ProtagonistProgressService` + 读 `ProtagonistLevelConfig`；Debug 注入经验连升；同步 SPEC_03 §3.8 D-030、SPEC_04 §6 |
| 2026-07-25 | v0.33.0 | Dig 垂直切片方案 A：`DigStageModule` + `DigSessionService` + `DigPrefabCatalog`；按 `DigMapId` Instantiate `Prefabs/Maps/`；挖掘/入账/DigStageSummary→交还驱动；同步 SPEC_03 §3.8 D-020、SPEC_04 §6 |
| 2026-07-25 | v0.32.0 | 关卡驱动方案 A：运行时只读 Csv（Editor=`Assets/ConfigTables/Csv`；Player=`StreamingAssets/ConfigTables/Csv`）；`LevelOperationDriver` + `IStageModule` 钩子；Tools「关卡」启 `Level_01`；UM ConfigId 忽略；MapId 仅解析/日志；同步 SPEC_03 §3.8 D-003/D-004/D-010、SPEC_04 §6/§14 |
| 2026-07-25 | v0.31.1 | SPEC_04 §1 录入本机 Unity 编辑器路径：`F:\Unity\Unity 2021.3.40f1\Editor\Unity.exe` |
| 2026-07-25 | v0.31.0 | Meta 壳方案 A：PlayerPrefs 三槽占用；单场景 Boot + Prefab UI；工具设置/关卡 Toast；Demo Debug 切态验 D-004；同步 SPEC_04 §6 / SPEC_03 §3.8 D-001～D-004 状态 |
| 2026-07-25 | v0.30.0 | Demo 验收扩大为 Meta 壳 + Dig→UM→Defend 流水线垂直切片（§3.8 D-001～D-043）；UM 阶段 `GameplayConfigId`=忽略；Defend Demo 最小刷怪点/NavMesh；同步 SPEC_03/04 §6 / CONTEXT / issues |
| 2026-07-25 | v0.29.0 | Dig/Defend 地图表现共用 `Ground_01`…`Ground_05`：DigGameplayConfig 增 `DigMapId`；`BattleMapId` 合法值改为 `Ground_*`，Prefab 解析 → `Assets/Prefabs/Maps/`；源参考 Example Scene Grid/Ground；同步 SPEC_03/04 / CONTEXT / spec-map / 配表样例 |
| 2026-07-25 | v0.28.0 | SPEC_04 §15 角色美术管线：Character Creator 烘焙整角；工具目录禁入游戏资源；补丁导出→`Art/Characters`→Prefabs；`AppearanceId`/`ModelId`/主角 Prefab 解析；Mount/Wing 打进外观；同步 SPEC_03 / CONTEXT / spec-map |
| 2026-07-25 | v0.27.0 | 套件/工作区路径改为本机 `F:\CursorGame_Git\SPECandSKILL` 与 `F:\CursorGame_Git\Gravedigger2026`；工作区 SPEC/CONTEXT/Skills 回写套件，关闭此前 E: 路径不可达导致的待同步 |
| 2026-07-25 | v0.26.0 | 配置表结构锁定：士兵 Skills=`SkillId;Level|…`；EquipStats；CombatConvertCoeffs；Class/Monster AttackRange 等命中列；GemType 六类；ComboKey；MoveStyle/AttackPriority；IconStyle 三列；BattleMapId/ModelId Prefab 名；TechUiFrameType；UnlockedFeature 开放名单；SkillConfig 不扩效果列；同步 SPEC_03/04 / CONTEXT / spec-map |
| 2026-07-25 | v0.25.0 | SPEC_04 §14：Excel 磁盘名改为四段 `{系统中文}_{表中文}_{系统英文}_{表英文}.xlsx`；CSV 仍为 `{系统英文}_{表英文}.csv`；打表按英文后缀两段映射；§9 各表磁盘名分列；同步 CONTEXT / spec-map |
| 2026-07-25 | v0.24.0 | §3.11 灵魂职业：新增 ClassId / ClassConfig（ClassName、PrimaryStat、CombatConvertCoeffs 占位）；SoulConfig 仅引用 ClassId，移除 ClassName/PrimaryStat；命名与 ClassAffinity 经 ClassConfig；§3.12 Primary 取自职业表，全局派生常量为过渡；同步 SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.23.0 | §3.12 Demo 第一版战斗边界：士兵与怪物仅普通攻击、不施放技能；SoulConfig 增 AttackMode；法师普攻=射手远程通道（仅 PrimaryStat=Intelligence）；SkillCooldown/Skills 保留不驱动；同步 SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.22.0 | §3.11/§3.12 士兵战斗派生公式：PrimaryStat；StaticStat/FinalStat 分层；NormalAttackPower=Primary×1.5；AttackSpeed=0.5+60/max(Agi,1)；SkillCooldown=max(0.1,BaseCD−30/max(Int,1))；MaxHP=ceil(BodyLife+Str×3)；SoulConfig.PrimaryStat + SkillConfig.BaseCooldownSeconds；同步 CONTEXT / SPEC_04 / spec-map |
| 2026-07-25 | v0.21.0 | §3.12 士兵战斗：EngageZone 内最近选敌；AttackRange；命中方案 D（近战前摇确认 / 远程弹道）；CombatDead vs PermanentDeath（Ended/LevelFailure 结算）；宝石特例 HP≤0 立即彻底死亡；§3.11 物资仅彻底死亡；AttackPriority 本批不驱动选目标；同步 SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.20.0 | §3.11/§3.12 失控规则关闭：失控程度=ΣCost/Cap−1；四档；开战锁定与独立 roll；叛变就近目标（含破盾）；最终率=档位+种族+Σ宝石+Σ技能（clamp）；技能加成非零时释放技能再 roll；SPEC_04 §9.20 LossOfControlConfig + §9.21 SkillConfig 骨架；Race/Gem 失控加成字段；同步 CONTEXT / spec-map |
| 2026-07-25 | v0.19.0 | §3.11 躯体材料表扩写（BodyPartConfig：等级/StatBonus/AutoConvert/美术等；Base(S)=Σ StatBonus）；新增躯体外观表 BodyAppearanceConfig 与选取算法（平均等级→四舍五入、职业倾向、IsFallback 保底、全表随机）；WarriorInstance.AppearanceId；LootDrop Id 可解析 BodyPartId；SPEC_04 §9.13+ 节号顺延；同步 CONTEXT / spec-map |
| 2026-07-25 | v0.18.0 | 名词统一：单位中文称谓「战士」→「士兵」（制造/上阵/属性构成等）；英文标识 `Warrior*` / `PreferWarrior` 不变；职业名 `ClassName` 仍可为「战士」；同步 SPEC_03/04、CONTEXT、spec-map |
| 2026-07-24 | v0.17.0 | §3.12 防守补全：护盾（普通攻击次数；初值=ProtagonistMaxHP；归零 LevelFailure）；战斗倒计时刷怪；WaveSpawnConfig / MonsterConfig（SPEC_04 §9.17–§9.18）；DefendGameplayConfig 增 CombatDurationSeconds；同步 CONTEXT / spec-map。**工作区已更新；套件路径 `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` 本机不可达，待同步** |
| 2026-07-24 | v0.16.0 | §3.13 科技树框架：中心向外、InitiallyUnlocked 默认学会、前置+LearnCost、设置页 2D 画布（UI-012）；SPEC_04 §9.15 TechTreeConfig / §9.16 TechEffectConfig；经验仍 Defend 胜利入账；同步 CONTEXT / spec-map。**工作区已更新；套件路径 `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` 本机不可达，待同步** |
| 2026-07-24 | v0.15.0 | SPEC_04：配置表公共工程约定——统一 `ConfigTables/Excel`+`Csv`；命名 `系统_表名`；双格式强制；打表菜单约定（§14）；关闭 §9 载体 TBD；§13 表项与非表 SO 分流；同步 CONTEXT / Skill 速查。**工作区已更新；套件路径 `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` 本机不可达，待同步** |
| 2026-07-24 | v0.14.0 | §3.11 战士制造：槽位/最低要求/预览与精魂闸门；部位加权定种族；六宝石类型互斥且 GemMult 按维 Σ；命名 Prefix+Race+Class+Suffix；死亡宝石全部回仓；SPEC_04 §9.9–§9.14（ClassName/GemType/BodyPart/ExtraEquipment/GemSuffix）；同步 CONTEXT / spec-map |
| 2026-07-24 | v0.13.0 | §3.11 FinalStat 按单项属性汇总（先定 S 再取来源）；力量例；`max(0,…)` 下限；GemMult 改为五维；SPEC_04 §9.10 / WarriorInstance 同步；CONTEXT |
| 2026-07-24 | v0.12.0 | §3.11 种族：由躯体决定；五维 RaceAdjustCoeff；FinalStat+=Base×RaceAdjustCoeff；不计控制力；SPEC_04 §9.11 RaceConfig；同步 CONTEXT / spec-map |
| 2026-07-24 | v0.11.0 | §3.11 宝石：制造可选镶嵌（≤1）；FinalStat+=Base×GemMult；死亡宝石回仓、其余绑定材料销毁；ControlPowerCost 含 GemCost；SPEC_04 §9.10 GemConfig；同步 CONTEXT / spec-map |
| 2026-07-24 | v0.10.0 | §3.11 战士属性构成关闭：战士信息/基础属性/灵魂/额外装备/控制力占用；FinalStat=Base+Equip+SkillBuffCoeff×Base（Buff 仅运行时）；装备制造锁定；SPEC_04 §9.9 SoulConfig + WarriorInstance 快照；同步 CONTEXT / spec-map |
| 2026-07-23 | v0.9.0 | §3.11 / SPEC_04 §9.8：主角升级配置表 ProtagonistLevelConfig（累计经验阈值、预留解锁、科技点、控制力上限、主角 MaxHP）；LevelFailure 无关卡结算奖励且不入账本阶段经验、已获不扣；同步 CONTEXT / spec-map |
| 2026-07-23 | v0.8.2 | Dig 缺口：加权字段通用规则（Weight=0 剔除；Dig 空有效列表放弃该次生成）；MaterialConfig / CurrencyConfig 增 AppearanceIconId、AssetPath、WarehouseQualityOutlineId；能力表改 §9.6、防守表改 §9.7；同步 CONTEXT |
| 2026-07-23 | v0.8.1 | 挖坟缺口补录：障碍物（Digger/Grave 圆半径 Prefab）；Warehouse+精魂入账（堆叠 10000、AutoConvert）；LootDrop=`Id_Count|…`（保留 Id=`Spirit`）；DigProtagonistCapabilities 四项算法（伤害/单次速度 min0.1/光标半径/可挖类型）；SPEC_04 §9.4 MaterialConfig / §9.5 能力伪结构（原 §9.4 防守表改编号 §9.6）；同步 CONTEXT |
| 2026-07-23 | v0.8.0 | §3.10 / §3.9：Dig 无胜负；有效时长=配置基础+科技时长加成；DigStageSummary（UI-011）仅汇总本阶段已获奖励、无额外发放；归零取消进行中 DigAction |
| 2026-07-23 | v0.7.0 | §3.11 框架关闭：经验阶段末入账+溢出保留；战士独立实例；控制力=等级+科技；失控占位不挡开战；布阵连续坐标+共享编辑器；无独立阶段结算；完整科技树/配方/失控效果另专题 |
| 2026-07-23 | v0.6.3 | §3.11 / §3.12：控制力超额（失控）不阻止开战；失控效果仅在战斗中按档次生效 |
| 2026-07-23 | v0.6.2 | §3.12：开战须至少上阵 1 名战士；否则开战按钮禁用或提示不可开战 |
| 2026-07-23 | v0.6.1 | BattleFormation：Defend `Prepare` 可调整位置/上下阵并写回同一套数据（不可制造）；开战按当前布阵部署；同步 §3.11 / §3.12 |
| 2026-07-23 | v0.6.0 | 新增 SPEC_03 §3.12 防守（Defend）：准备态→开战、BattleMap 部署、NavMesh 寻路与 1s 目标修正、末波清场胜利与主角阵亡关卡失败；SPEC_04 §9.4 DefendGameplayConfig；同步 CONTEXT / SPEC_02 / §3.9 |
| 2026-07-23 | v0.5.2 | §3.11 / UI-010：升级与制造主屏 = 同屏三区并列（升级/制造/布阵）+ 底部「完成」按钮 |
| 2026-07-23 | v0.5.1 | §3.11 / §3.9：升级与制造阶段结束 = 玩家主动确认「完成 / 进入下一阶段」（无强制倒计时；本版无最低门槛） |
| 2026-07-23 | v0.5.0 | `SewRevive` 正式更名为 **升级与制造 / UpgradeManufacture**；新增 SPEC_03 §3.11 框架（经验升级→科技点、材料造战士、控制力/失控、战斗布阵持久化）；同步 SPEC_02/04、CONTEXT |
| 2026-07-23 | v0.4.1 | 强化 SPEC_04 §13：**预制体优先（Prefab-first）** 为实际开发默认原则；补齐适用对象、例外与禁止项；决策表默认行改为 Prefab |
| 2026-07-23 | v0.4.0 | 录入挖坟交互与奖励（§3.10）：主角/圆圈光标/0.2s 触发/0.8s 帧动画/扣血与图标样式/固定动画序列/DigReward；新增坟墓品质定义表（SPEC_04 §9.3）；挖坟伤害-科技绑定占位 |
| 2026-07-23 | v0.3.0 | 录入关卡阶段流水线（§3.9）与挖坟生成/倒计时规则（§3.10）；关卡运作表 + 挖坟配置表（SPEC_04 §9）；末阶段结束触发胜利结算 |
| 2026-07-23 | v0.2.0 | 录入最小 Demo：三玩法状态占位、固定 3 槽存档、工具面板（设置/关卡占位）；验收 D-001～D-004 |
| 2026-07-23 | v0.1.0 | 自 SPECandSKILL 套件创建项目 SPEC 骨架；录入 Unity 2021.3.40f1 与工程路径 |

### English

| Date | Version | Summary (English) |
|------|---------|-------------------|
| 2026-07-25 | v0.36.0 | UM formation Approach A: `BattleFormationService` (continuous-coord deploy/undeploy/reposition + ControlPower usage) + `FormationPanelView`; wired to warrior pool; save-scoped for shared Defend Prepare; synced SPEC_03 §3.8 D-032, SPEC_04 §6 |
| 2026-07-25 | v0.35.0 | UM manufacture Approach A: `ManufactureService` (15 strict slots / preview / Spirit gate) + `WarriorPoolService`; loads Soul/Class/Gem/Race/BodyPart/Appearance/Equip/GemSuffix tables; `WarehouseService` credits BodyParts and consumes by Id; temp `Prefabs/Defend/Warriors/{AppearanceId}`; synced SPEC_03 §3.8 D-031, SPEC_04 §6 |
| 2026-07-25 | v0.34.0 | UM upgrade panel Approach A: `UpgradeManufactureStageModule` + `ProtagonistProgressService` + `ProtagonistLevelConfig`; Debug Exp inject → chain level-up; synced SPEC_03 §3.8 D-030, SPEC_04 §6 |
| 2026-07-25 | v0.33.0 | Dig vertical Approach A: `DigStageModule` + `DigSessionService` + `DigPrefabCatalog`; instantiate `Prefabs/Maps/` by `DigMapId`; dig/loot/DigStageSummary→return to Level driver; synced SPEC_03 §3.8 D-020, SPEC_04 §6 |
| 2026-07-25 | v0.32.0 | Level-driver Approach A: runtime CSV-only (Editor=`Assets/ConfigTables/Csv`; Player=`StreamingAssets/ConfigTables/Csv`); `LevelOperationDriver` + `IStageModule` hooks; Tools Level starts `Level_01`; UM ConfigId ignored; MapId resolve/log only; synced SPEC_03 §3.8 D-003/D-004/D-010, SPEC_04 §6/§14 |
| 2026-07-25 | v0.31.1 | SPEC_04 §1: local Unity Editor path `F:\Unity\Unity 2021.3.40f1\Editor\Unity.exe` |
| 2026-07-25 | v0.31.0 | Meta shell Approach A: PlayerPrefs 3-slot occupied; Boot scene + Prefab UI; Tools Settings/Level Toast; Demo Debug cycle for D-004; synced SPEC_04 §6 / SPEC_03 §3.8 D-001–D-004 status |
| 2026-07-25 | v0.30.0 | Demo acceptance expanded to Meta shell + Dig→UM→Defend pipeline vertical (§3.8 D-001–D-043); UM stage `GameplayConfigId`=ignore; Defend Demo-min spawn/NavMesh; synced SPEC_03/04 §6 / CONTEXT / issues |
| 2026-07-25 | v0.29.0 | Dig/Defend map presentation shares `Ground_01`…`Ground_05`: DigGameplayConfig adds `DigMapId`; `BattleMapId` allowed values → `Ground_*`, Prefab resolve → `Assets/Prefabs/Maps/`; source ref Example Scene Grid/Ground; synced SPEC_03/04 / CONTEXT / spec-map / sample tables |
| 2026-07-25 | v0.28.0 | SPEC_04 §15 Character Art Pipeline: Character Creator baked whole characters; ban game assets under vendor tool folder; patched export → `Art/Characters` → Prefabs; `AppearanceId`/`ModelId`/protagonist Prefab resolve; Mount/Wing baked into appearance; synced SPEC_03 / CONTEXT / spec-map |
| 2026-07-25 | v0.27.0 | Kit/workspace paths updated to local `F:\CursorGame_Git\SPECandSKILL` and `F:\CursorGame_Git\Gravedigger2026`; workspace SPEC/CONTEXT/Skills synced back to kit; closes prior E: path unreachable pending sync |
| 2026-07-25 | v0.26.0 | Config-table schema lock: soldier Skills=`SkillId;Level|…`; EquipStats; CombatConvertCoeffs; Class/Monster AttackRange hit columns; six GemTypes; ComboKey; MoveStyle/AttackPriority; IconStyle three columns; BattleMapId/ModelId Prefab names; TechUiFrameType; open UnlockedFeature list; SkillConfig no effect columns yet; synced SPEC_03/04 / CONTEXT / spec-map |
| 2026-07-25 | v0.25.0 | SPEC_04 §14: Excel disk names are four-part `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}.xlsx`; CSV stays `{SystemEN}_{TableEN}.csv`; bake maps via English suffix; §9 disk names split Excel/CSV; synced CONTEXT / spec-map |
| 2026-07-25 | v0.24.0 | §3.11 soul Class: new ClassId / ClassConfig (ClassName, PrimaryStat, CombatConvertCoeffs placeholder); SoulConfig references ClassId only (drops ClassName/PrimaryStat); naming & ClassAffinity via ClassConfig; §3.12 Primary from Class table; global derive constants interim; synced SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.23.0 | §3.12 Demo v1 combat scope: soldiers & monsters normal attacks only (no skill casts); SoulConfig.AttackMode; Mage normal = Archer Ranged channel (PrimaryStat=Intelligence only); SkillCooldown/Skills retained but unused; synced SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.22.0 | §3.11/§3.12 soldier combat derives: PrimaryStat; StaticStat/FinalStat layers; NormalAttackPower=Primary×1.5; AttackSpeed=0.5+60/max(Agi,1); SkillCooldown=max(0.1,BaseCD−30/max(Int,1)); MaxHP=ceil(BodyLife+Str×3); SoulConfig.PrimaryStat + SkillConfig.BaseCooldownSeconds; synced CONTEXT / SPEC_04 / spec-map |
| 2026-07-25 | v0.21.0 | §3.12 WarriorCombat: nearest target in EngageZone; AttackRange; hit scheme D (melee windup confirm / ranged projectile); CombatDead vs PermanentDeath (settle on Ended/LevelFailure); gem exception immediate PermanentDeath on HP≤0; §3.11 material fate only on PermanentDeath; AttackPriority unused for targeting this batch; synced SPEC_04 / CONTEXT / spec-map |
| 2026-07-25 | v0.20.0 | Closed §3.11/§3.12 LossOfControl: Degree=ΣCost/Cap−1; four tiers; lock + per-soldier roll at StartBattle; Rebel nearest targeting (Shield −1); FinalChance=tier+race+Σgem+Σskill (clamp); skill-cast re-roll when ΣSkillBonus≠0; SPEC_04 §9.20 LossOfControlConfig + §9.21 SkillConfig skeleton; Race/Gem chance-bonus fields; synced CONTEXT / spec-map |
| 2026-07-25 | v0.19.0 | §3.11 BodyPartConfig expanded (BodyLevel/StatBonus/AutoConvert/art; Base(S)=Σ StatBonus); new BodyAppearanceConfig + pick rules (avg level→round, ClassAffinity, IsFallback, else table-random); WarriorInstance.AppearanceId; LootDrop may resolve BodyPartId; SPEC_04 §9.13+ renumbered; synced CONTEXT / spec-map |
| 2026-07-25 | v0.18.0 | Terminology: CN unit name 战士→士兵 (manufacture/deploy/composition); EN ids `Warrior*` / `PreferWarrior` unchanged; ClassName may still be profession「战士」; synced SPEC_03/04, CONTEXT, spec-map |
| 2026-07-24 | v0.17.0 | §3.12 Defend fill: Shield (normal-hit count; init=`ProtagonistMaxHP`; ≤0 → LevelFailure); countdown spawn; WaveSpawnConfig / MonsterConfig (SPEC_04 §9.17–§9.18); DefendGameplayConfig +`CombatDurationSeconds`; synced CONTEXT / spec-map. **Workspace updated; suite path `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` unreachable on this machine — sync pending** |
| 2026-07-24 | v0.16.0 | §3.13 TechTree framework: center-out, InitiallyUnlocked default learn, prereqs+LearnCost, Settings 2D canvas (UI-012); SPEC_04 §9.15 TechTreeConfig / §9.16 TechEffectConfig; Exp still Defend victory only; synced CONTEXT / spec-map. **Workspace updated; suite path `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` unreachable on this machine — sync pending** |
| 2026-07-24 | v0.15.0 | SPEC_04: config-table engineering rules — unified `ConfigTables/Excel`+`Csv`; naming `{System}_{Table}`; dual-format required; Bake Tables menu (§14); closed §9 carrier TBD; §13 splits table rows vs non-table SO; synced CONTEXT / Skill. **Workspace updated; suite path `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\` unreachable on this machine — sync pending** |
| 2026-07-24 | v0.14.0 | §3.11 warrior manufacture: slots/min requirements/preview+Spirit gate; BodyPart weight-1 race pick; 6 type-exclusive gems with per-dim Σ GemMult; name Prefix+Race+Class+Suffix; death returns all gems; SPEC_04 §9.9–§9.14; synced CONTEXT / spec-map |
| 2026-07-24 | v0.13.0 | §3.11 FinalStat per-attribute aggregation (pick S then sources); Strength example; `max(0,…)` floor; GemMult five-dim; SPEC_04 §9.10 / WarriorInstance synced; CONTEXT |
| 2026-07-24 | v0.12.0 | §3.11 Race: from Body; five-dim RaceAdjustCoeff; FinalStat+=Base×RaceAdjustCoeff; no ControlPower term; SPEC_04 §9.11 RaceConfig; synced CONTEXT / spec-map |
| 2026-07-24 | v0.11.0 | §3.11 Gem: optional socket (≤1); FinalStat+=Base×GemMult; on death Gem→Warehouse, other bound materials destroyed; ControlPowerCost includes GemCost; SPEC_04 §9.10 GemConfig; synced CONTEXT / spec-map |
| 2026-07-24 | v0.10.0 | §3.11 warrior attribute composition closed: Info/BaseStats/Soul/ExtraEquipment/ControlPowerCost; FinalStat=Base+Equip+SkillBuffCoeff×Base (runtime Buff only); equip locked at manufacture; SPEC_04 §9.9 SoulConfig + WarriorInstance snapshot; synced CONTEXT / spec-map |
| 2026-07-23 | v0.9.0 | §3.11 / SPEC_04 §9.8: ProtagonistLevelConfig (cumulative Exp, reserved unlocks, TechPoints, ControlPower cap, MaxHP); LevelFailure: no level settlement rewards / no stage Exp; already-owned kept; synced CONTEXT / spec-map |
| 2026-07-23 | v0.8.2 | Dig gaps: weighted-field common rules (Weight=0 dropped; Dig empty effective list abandons spawn); MaterialConfig / CurrencyConfig add AppearanceIconId, AssetPath, WarehouseQualityOutlineId; capabilities → §9.6, Defend → §9.7; synced CONTEXT |
| 2026-07-23 | v0.8.1 | Dig gap fill: DigObstacle (Digger/Grave circle radii on Prefabs); Warehouse + SpiritEssence credit (stack 10000, AutoConvert); LootDrop=`Id_Count|…` (reserved Id=`Spirit`); DigProtagonistCapabilities formulas (damage / dig speed min 0.1 / cursor radius / diggable types); SPEC_04 §9.4 MaterialConfig / §9.5 capabilities (Defend table renumbered §9.6); synced CONTEXT |
| 2026-07-23 | v0.8.0 | §3.10 / §3.9: Dig no win/lose; effective duration = config base + tech duration bonus; DigStageSummary (UI-011) aggregates stage rewards only, no extra grants; cancel in-progress DigAction on timeout |
| 2026-07-23 | v0.7.0 | Closed §3.11 framework: stage-end Exp + overflow kept; warrior instances; ControlPower=level+tech; LossOfControl placeholder non-blocking; continuous shared formation editor; no stage settlement; full tech/recipes/tier effects deferred |
| 2026-07-23 | v0.6.3 | §3.11 / §3.12: Over-ControlPower (LossOfControl) does not block StartBattle; tier effects apply in combat only |
| 2026-07-23 | v0.6.2 | §3.12: StartBattle requires ≥1 deployed warrior; else disable button or show cannot-start hint |
| 2026-07-23 | v0.6.1 | BattleFormation: Defend `Prepare` may edit positions/deploy/undeploy and write back the same dataset (no manufacture); StartBattle uses current formation; synced §3.11 / §3.12 |
| 2026-07-23 | v0.6.0 | Added SPEC_03 §3.12 Defend: Prepare→StartBattle, BattleMap deploy, NavMesh pathing + 1s retarget, last-wave clear victory & protagonist LevelFailure; SPEC_04 §9.4 DefendGameplayConfig; synced CONTEXT / SPEC_02 / §3.9 |
| 2026-07-23 | v0.5.2 | §3.11 / UI-010: UpgradeManufacture main screen = three side-by-side panels + bottom Complete |
| 2026-07-23 | v0.5.1 | §3.11 / §3.9: UpgradeManufacture stage ends on player confirm "Complete / Next stage" (no countdown; no mandatory gate this version) |
| 2026-07-23 | v0.5.0 | Renamed `SewRevive` → **UpgradeManufacture**; added SPEC_03 §3.11 framework (exp→tech points, material warriors, ControlPower/LossOfControl, BattleFormation persistence); synced SPEC_02/04, CONTEXT |
| 2026-07-23 | v0.4.1 | Strengthened SPEC_04 §13: **Prefab-first** as the default authoring principle; added targets, exceptions, and forbidden patterns; decision table default row is Prefab |
| 2026-07-23 | v0.4.0 | Dig interaction & rewards (§3.10): Digger / circle cursor / 0.2s trigger / 0.8s dig anim / HP & icon styles / fixed anim sequence / DigReward; GraveQualityConfig (SPEC_04 §9.3); dig-damage tech binding placeholder |
| 2026-07-23 | v0.3.0 | Level stage pipeline (§3.9) and Dig spawn/countdown (§3.10); LevelOperation + DigGameplayConfig (SPEC_04 §9); VictorySettlement after last stage |
| 2026-07-23 | v0.2.0 | Minimal Demo rules: three gameplay placeholders, 3 save slots, ToolsPanel stubs; acceptance D-001–D-004 |
| 2026-07-23 | v0.1.0 | Created project SPEC skeleton from SPECandSKILL kit; recorded Unity 2021.3.40f1 and project paths |
