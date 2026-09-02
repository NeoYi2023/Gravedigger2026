---
title: TF-06 样例书/阵/手验
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-084
  - SPEC_03 §3.18
  - SPEC_04 §9.30 Demo 样例行
approach: B
depends_on:
  - TF-05
---

## 目标

端到端样例：魔法书授予楔阵技能 → AutoManufacture/GM 造兵 → 布阵 snap → Defend+PushMap 战斗保持阵型 + leash + 解散。

## 范围

- 样例内容：`Form_Wedge_01` + `MagicBook_Form_Wedge`（issue 初稿名 `MagicBook_GrantWedge` 以表行为准）+ `Skill_Form_Wedge` + Pattern Prefab `FormationPattern_Wedge_01`
- `StatModifiers=Stat=Strength|Mul=1.15`（ExclusiveSkill 留空；overlay 管线由 TF-05 Correctness 覆盖）
- GM「添加士兵」若已装备楔阵书则 **仅**走 `GrantFormationSkill`
- 手验清单：Mode2 UM 组阵、PushMap 推进、Mode1 Defend 守点、阵亡解散
- D-084 §3.8 状态更新为 **完成**（清单就绪；Play Mode 由负责人勾选）

## 不做

- 第二种阵型（后置）
- Mode1 手动制造跑 Token
- GM 添加士兵跑 `StatMul` / `ForceClass` / `SoldierSkillLevelAdd` / `RaceWeightPick`
- 新 ExclusiveSkill / SkillEffect 样例行

## 验收

- [ ] 负责人手验 Defend + PushMap 各 1 局通过（Play Mode，见下表）
- [x] issues INDEX 与本文件记录手验步骤
- [x] SPEC_03 §3.8 D-084 标记完成

## 依赖

- TF-01～TF-05

## 编码前

- 难度 2（负责人确认）；选定方案 **B** — GM 仅 `GrantFormationSkill`

## 落地摘要

选定方案 B。`Form_Wedge_01` 填 `Stat=Strength|Mul=1.15`（Mode1+Mode2 Excel → Bake CSV）。`GmSoldierGrantService.TryAdd` 在入池前调用 `ApplyEquippedGrantFormationSkillBooks`（Dig 闪电 `TryGrantOne` 不跑）。手验清单见下。Changelog SPEC_00 v0.83.68。

---

## D-084 手验清单（Play Mode）

前置：Unity Editor 打开工程；Console 无配置加载失败。菜单 `Gravedigger2026/Formation/Run Tactical Formation Runtime Correctness (TF-04a)` 应通过（含 TF-05 overlay/解散）。

样例 Id：

| 种类 | Id |
|------|-----|
| 阵型 | `Form_Wedge_01`（Min=3 / Max=5 / Strength×1.15） |
| 魔法书 | `MagicBook_Form_Wedge`（Tools / Dig HUD 文案「楔阵」） |
| 标记技能 | `Skill_Form_Wedge` |
| Pattern | `FormationPattern_Wedge_01` |

### A — Mode2：书 → AM 造兵 → UM 组阵 → PushMap

| # | 步骤 | 期望 | 勾选 |
|---|------|------|------|
| 1 | 选 **Mode2** 进档。Tools「增加魔法书」点「楔阵」（或 Dig HUD 上层魔法书 GM） | 槽内出现楔阵书；`IsUnique=0` 可叠但一本即可 | [ ] |
| 2 | Tools「关卡」进 `Level_01`：Shop → Dig（攒最低配方或 Debug 灌料）→ AutoManufacture | Console：`GrantFormationSkill hit skill=Skill_Form_Wedge@1 formation=Form_Wedge_01`（≥3 兵） | [ ] |
| 3 | 进入 UM，打开布阵 | ≥3 名带楔阵技能的已上阵士兵 **snap 成楔形**（覆盖职业区）；点任一成员 = **整阵**拖动 | [ ] |
| 4 | 下阵至 &lt;3 人再上阵回 ≥3 | &lt;Min 退回职业区螺旋；再 ≥Min 重新 snap | [ ] |
| 5 | UM「完成」→ PushMap Prepare → 开战 | Console 开战有楔阵 overlay（Strength Mul）；脚下 Debug「阵型」 | [ ] |
| 6 | 推进遇敌 | 成员保持相对站位跟虚拟中心；接敌不无限追超 leash 目标（远处敌不追） | [ ] |
| 7 | 阵亡至激活成员 &lt;3 | 立即解散：加成消失；GoalKind 回「推进」；不瞬移回职业区 | [ ] |

### B — Mode1：GM 造兵 → UM 组阵 → Defend

Mode2 样例关无 Defend。Mode1 `Level_03` = Dig → UM → Defend。GM「添加士兵」**仅 UM 布阵打开**可用。

| # | 步骤 | 期望 | 勾选 |
|---|------|------|------|
| 8 | 选 **Mode1** 进档。Tools「增加魔法书」点「楔阵」 | 槽内楔阵书 | [ ] |
| 9 | 关卡 `Level_03` → Dig 过后进入 UM，打开布阵。Tools「添加士兵」：任意有外观匹配的职业/种族，数量 **3**，勾选自动上阵 | Console：`GrantFormationSkill hit`；布阵 snap 楔形（与 A3 相同） | [ ] |
| 10 | UM「完成」→ Defend Prepare → 开战 | 整阵守组阵点；脚下「阵型」；接敌 leash 同 A6 | [ ] |
| 11 | 阵亡至 &lt;3 | 解散；GoalKind 回「回阵」；新 Home = 解散瞬间世界坐标 | [ ] |

### 观察点（两模式共用）

- Overlay：开战日志含 `Stat=` Strength×1.15；解散后派生回落（HP 钳制新 MaxHP）
- ExclusiveSkill 本样例为空；专属 overlay 以 Correctness 菜单为准
- Mode1 手动制造 **不会**授予楔阵技能（仅 GM 添加士兵例外）

**备注：** Agent 本片完成样例表 + GM 接线 + 清单文档；Play Mode 勾选由负责人在 Editor 执行。

---

## 平行阵样例（后补；不重开 D-084）

| 种类 | Id |
|------|-----|
| 阵型 | `Form_Wedge_02`（Min=3 / Max=10 / Strength×1.15） |
| 魔法书 | `MagicBook_Form_Wedge_02`（Tools 文案「平行阵」） |
| 标记技能 | `Skill_Form_Wedge_02` |
| Pattern | `FormationPattern_Wedge_02`（10 槽两排平行线） |

手验：Tools「增加魔法书」选「平行阵」→ 造兵/GM 授予 `Skill_Form_Wedge_02` → UM ≥3 人 snap 为两排；可与楔阵并行装备对照。
