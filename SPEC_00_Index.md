# Gravedigger2026 — SPEC 总索引 / SPEC Master Index

**文档版本 / Document Version:** v0.14.0  
**最后更新 / Last Updated:** 2026-07-24  
**当前阶段 / Current Phase:** 规则录入 / Rule definition  

**套件维护路径：** `E:\Work\Cursor\SPECandSKILL\Gravedigger2026\`  
**日常开发权威：** 复制到 Cursor 工作区根后的 `SPEC_*.md`（工作区：`E:\Work\Cursor\Gravedigger2026\Gravedigger2026`）

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
| 03 | [SPEC_03_GameRules.md](SPEC_03_GameRules.md) | 游戏规则主体（Demo 外壳 + 关卡阶段 / 挖坟 / 升级与制造 / 防守框架） |
| 04 | [SPEC_04_Technical.md](SPEC_04_Technical.md) | 技术规范、Demo 边界与配置表约定 |

**建议阅读顺序：** 01 → 02 → 03 → 04。

### English

| No. | File | Description |
|-----|------|-------------|
| 00 | [SPEC_00_Index.md](SPEC_00_Index.md) | Master index, changelog |
| 01 | [SPEC_01_Workflow.md](SPEC_01_Workflow.md) | Three-phase workflow |
| 02 | [SPEC_02_GameOverview.md](SPEC_02_GameOverview.md) | Game overview |
| 03 | [SPEC_03_GameRules.md](SPEC_03_GameRules.md) | Game rules (Demo shell + Level stages / Dig / UpgradeManufacture / Defend framework) |
| 04 | [SPEC_04_Technical.md](SPEC_04_Technical.md) | Technical standards, Demo boundary, config tables |

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
