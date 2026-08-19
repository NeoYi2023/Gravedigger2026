---
title: 士兵技能效果 Skill_04～12 — SPEC 关闭 + D-073 + EffectKind 框架
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 WarriorCombat / SkillCast
  - SPEC_03 §3.8 D-073
  - SPEC_04 §9.21 SkillConfig
  - SPEC_04 §9.21b SkillEffectConfig
  - SPEC_04 §9.22 PushMap 战斗接线
  - SPEC_00 v0.82.82
selected_approach: B+ — SkillEffectKind 登记制 + EffectParams 表驱动 + CombatStatusService + Handler 注册表；Session 只调管线；验收号 D-073（D-072 已被魔法书删除占用）
---

## 目标

关闭「Mode2 PushMap 士兵技能 `Skill_04`～`Skill_12` 战斗效果」的规则与验收框架；**为后续新技能铺路**，避免按 `SkillId` 硬编码。

## 范围（仅 SPEC / Changelog / spec-map / CONTEXT 术语）

### SPEC_03 §3.12 SkillCast 扩写

- 声明 **D-073**：PushMap 忠诚兵（非 Rebel）持有并已实现 `EffectImplemented=1` 的 `Skill_04`～`Skill_12` 按 `SkillConfig` + `SkillEffectConfig` 生效。
- **架构原则（中英）：**
  - `SkillEffectConfig.EffectKind` = 登记制 PascalCase Token（同 MagicBook `EffectPayload`）；**禁止**在 Session/View 写 `if (skillId == "Skill_06")`。
  - `SkillEffectConfig.EffectParams` = `Key=Value|Key=Value|…`；等级差写在 **各行** Params 或引用 Lv 列；**不解析** `Description` 自然语言。
  - `CombatStatusService`：无敌 / 击晕 / 减速 / 灼烧 DoT 的统一 Tick + 查询 API；怪物与士兵状态分离或分 bucket（写清）。
  - **钩子枚举**（示例，可增补）：`OnOutgoingDamageSettle` / `OnIncomingDamageSettle` / `OnWarriorAaHitConfirm` / `OnWarriorTargetAcquired` / `OnWarriorWouldDie` / `OnProjectileHit` / `OnSkillInternalCooldown`。
  - `SkillEffectPipeline`（命名可 SPEC 定稿）：查实例 `SoldierSkills` → `SkillConfig.SkillEffectId` → `SkillEffectConfig` → 按 `TriggerHook` 调度已注册 Handler。
  - `Skill_01`/`Skill_02`/`Skill_03`（D-069）**本片保留** `SoldierSkillCast`；SPEC 注明后续可迁为 Kind，**不阻塞 SE-01～09**。
- 每技能 **1 段** SkillCast 规则摘要（对齐现有 CSV `CastTarget` / `ExtraActivationCondition` / Description 数值）。

### SPEC_04 §9.21b 扩列 + 登记表

新增列（Mode2 表；Mode1 可空列占位）：

| 字段 | 类型 | 说明 |
|------|------|------|
| EffectKind | string | 登记制 Token；空 = 未实现 |
| EffectParams | string | Token 允许的 Key=Value |
| TriggerHook | string | 管线插入点枚举 |

**EffectKind 登记表（本片 9 技能 + 预留）：**

| Token | TriggerHook | 允许 Params（示例） | 对应技能 |
|-------|-------------|---------------------|----------|
| `OutgoingMulOnNewTargetFirstHit` | `OnOutgoingDamageSettle` | `Mul` | Skill_04 |
| `CheatDeathInvincible` | `OnWarriorWouldDie` | `InvincibleSeconds` | Skill_05 |
| `OnAaHitChanceAoeStun` | `OnWarriorAaHitConfirm` | `Chance`,`Radius`,`StunSeconds` | Skill_06 |
| `OnAaHitAoeSlow` | `OnWarriorAaHitConfirm` | `Radius`,`SlowMoveMul`,`SlowAttackMul`,`DurationSeconds`,`InternalCooldownSeconds` | Skill_07 |
| `OutgoingMulVsMonsterType` | `OnOutgoingDamageSettle` | `MonsterType`,`Mul` | Skill_08 |
| `StackingOutgoingMulTimed` | `OnSkillInternalCooldown` | `StackBonus`,`MaxTotalBonus`,`TickSeconds` | Skill_09 |
| `RangedPierceExtraHits` | `OnProjectileHit` | `ExtraHitCount`,`DamageMul` | Skill_10 |
| `OnAaHitApplyBurn` | `OnWarriorAaHitConfirm` | `TickDamageMul`,`TickIntervalSeconds`,`DurationSeconds`,`StackMode=RefreshDuration` | Skill_11 |
| `RetargetFarthestTeleportBehind` | `OnWarriorTargetAcquired` | （CD 读 SkillConfig.BaseCooldownSeconds） | Skill_12 |

补全 Mode2 样例 FK 行 `SkillEffect_06_1`～`SkillEffect_12_5`（Notes + 上表 Kind/Params 样例）。

### SPEC_03 §3.8 D-073

- P1；PushMap；9 技能 Lv1～5 可手验；`EffectImplemented=1`；与 D-069 / D-071 共存。

### SPEC_04 §6 / §9.22

- 脚本路径意图：`Core/Combat/CombatStatusService.cs`、`Core/Combat/SkillEffectPipeline.cs`、`Core/Combat/SkillEffects/*Handler.cs`。
- PushMap Session **只**在既有结算点调用 Pipeline/Status；CombatSkillIcon 仍走 `SkillIconPopup` / `SkillPersistChanged`。

### SPEC_00 Changelog + spec-map + CONTEXT

- 增 D-073、EffectKind、CombatStatus 术语（1 句）。

## 不做

- Unity C# / Prefab / Excel 改表（SE-01～09）
- `Skill_01`/`Skill_02`/`Skill_03` 迁移到新管线
- Defend 接线
- 通用「解析 Description」效果器

## 验收

- [x] §3.12 SkillCast + EffectKind 架构 + 9 技能规则摘要（中英）
- [x] §9.21b 新列 + 登记表 + FK 样例（中英）
- [x] D-073 写入 §3.8（中英；D-072 保留为魔法书删除）
- [x] Changelog / spec-map / CONTEXT 已记
- [x] INDEX SE-00→done

## 依赖

- D-069（SC-00～03 done）作为既有 SkillCast 基线

## 编码前

- 本片 **无编码**
