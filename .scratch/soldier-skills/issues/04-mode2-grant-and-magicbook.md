---
title: Mode2 授予 DefaultSkillIds + SoldierSkillLevelAdd 二次扫描
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 流水线 4b/4c
  - SPEC_03 §3.8 D-062
  - SPEC_04 §9.24 SoldierSkillLevelAdd
selected_approach: A — 第一次钩子返回后授予；二次扫描槽左→右；只升已有技能
---

## 目标

Mode2 AutoManufacture：`ClassId` 最终定稿（第一次 `SoldierManufacture` 钩子含未来 `ForceClass` 之后）授予 DefaultSkillIds@Lv1，再二次扫描 `SoldierSkillLevelAdd`。完成后勾 D-062。

## 范围

- `AutoCraftDraft` 增加 `SoldierSkills`（或等价列表）；`BuildWarriorInstance` 拷入 `WarriorInstance`。
- `AutoManufactureService` 在 `_magicBookHook.ApplySoldierManufactureEffects(draft)` **返回后**、`FinalizeDraft` 前后均可授予（须用钩子可能改写后的 `draft.ClassId`）。建议：钩子 → 授予 → 二次扫描 → `FinalizeDraft`。
- 授予复用 SS-03 helper。
- 扩展钩子（**不要**在第一次 `ApplySoldierManufactureEffects` 里升技能）：
  - 第一次扫描遇到 `SoldierSkillLevelAdd`：跳过（可打 deferred 日志，勿当未知 payload 空 apply 刷屏）
  - 新增二次扫描 API（如 `ApplySoldierSkillLevelSecondPass(draft)`）：槽左→右；`EffectPhase` 含 `SoldierManufacture` 且 payload=`SoldierSkillLevelAdd`
  - 必填 `SkillId`、`Delta`（整数，可负）；缺/非法 → 该书无效 + Warning
  - 仅当 draft **已有**该 SkillId：`SkillLevel += Delta`，钳制到 `SkillConfig` 该 Id 的最小/最大等级；无该技能 → 跳过（**不新授**）
- 样例魔法书（Mode1+Mode2 `Manufacture_MagicBookConfig`）：
  - `MagicBook_SoldierSkillLevel` | `IsUnique=0` | `EffectPhase=SoldierManufacture` | `EffectPayload=SoldierSkillLevelAdd` | `EffectParams=SkillId=Skill_01|Delta=1` | DisplayName=`士兵技能升级`
  - SPEC_04 §9.24「已定义行」可补这一行（Changelog）
- 手验入口：优先复用 Tools「增加魔法书」（D-061）若已落地；否则 Dig HUD 加 GM「装备士兵技能升级」，对齐 `MagicBook_WarriorEnhance`。
- 制造日志打印授予结果与升等后等级。
- D-062 状态改为完成（中英）；Changelog。

## 不做

- 实现 `ForceClass` / `StatAdd` / `QualityDelta` 等其它 Token
- 技能施放、Mode1 读魔法书升技能、魔法书新授予技能
- 正式魔法书装配 UI
- 给非战士职业填默认技能

## 验收

- [x] Mode2 不装备该书：战士（`Class_Warrior` / `Class_Warrior_0`）造出 `Skill_01@1`；其它职业空列表
- [x] 装备样例书后再造战士：`Skill_01@2`（钳制不超过表内最大级）
- [x] 装备该书但职业无该技能：不新授、不报错崩溃
- [x] 进档 `SoldierSkills` 仍在
- [x] D-062 可勾完成；INDEX SS-04→done；回复变更清单

## 依赖

- SS-03

## 编码前

- 方案 **A** 已锁；可直接编码

## 完成备注

- `AutoCraftDraft.SoldierSkills`；`BuildWarriorInstance` 拷入 `WarriorInstance`
- 流水线：第一次钩子 → `SoldierSkillGrant.GrantDefaultSkillsAtLevel1` → `ApplySoldierSkillLevelSecondPass` → `FinalizeDraft`
- 第一次扫描 `SoldierSkillLevelAdd` 记 deferred 日志，不空 apply 刷屏
- 二次扫描：槽左→右；缺/非法 Key → Warning 该书无效；无该技能 skip 不新授；钳制 `TryGetSkillLevelRange`
- 样例书 `MagicBook_SoldierSkillLevel`（Mode1+Mode2）；手验走 Tools「增加魔法书」（D-061）
- SPEC_00 **v0.82.21**；D-062 **完成**
