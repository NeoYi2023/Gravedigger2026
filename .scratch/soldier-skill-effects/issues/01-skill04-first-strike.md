---
title: Skill_04 先发制人 + SkillEffect 管线 bootstrap
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 SkillCast
  - SPEC_03 §3.8 D-073
  - SPEC_04 §9.21 Skill_04 样例
  - SPEC_04 §9.21b OutgoingMulOnNewTargetFirstHit
  - SPEC_04 §9.22 PushMap
selected_approach: B+ — 首片落地 SkillEffectPipeline + Handler 注册 + CombatStatusService 空壳；Skill_04 为首个 Handler
---

## 目标

1. **Bootstrap** 可扩展技能效果管线（SE-00 SPEC 定稿后编码）。
2. 实现 `Skill_04` 先发制人 Lv1～5：对**新选定目标**的**第一次普攻** Outgoing × `Mul`（20%～60%）。

## 范围

### 框架（本片必须落地，供 SE-02～09 复用）

- `SkillEffectKind` 常量 / 登记表读取（`ConfigCsvRepository` 加载 `EffectKind`/`EffectParams`/`TriggerHook`）
- `SkillEffectParams.Parse`（`Key=Value|…`，对齐 `MagicBookEffectParams` 风格）
- `ISkillEffectHandler` + 按 `TriggerHook` 的注册表（新增 Kind = 新类 + Register，**不改** Session 分支）
- `SkillEffectPipeline.Dispatch(hook, context)` — Session 在 `SettleMonsterDamage` 等既有入口调用
- `CombatStatusService`：**空壳** + `Tick(deltaTime)` 挂载点（SE-02+ 填状态）
- 士兵运行时：`LastNormalAttackTargetRuntimeId` 或等价字段，用于判定「新目标首击」

### Skill_04 业务

- `EffectKind=OutgoingMulOnNewTargetFirstHit`；Lv1～5 `EffectParams` 例：`Mul=1.2` … `Mul=1.6`
- `ExtraActivationCondition=普攻攻击新目标敌人的第一次` → 换目标后下一发首击可再触发
- **不**占 Skill_03 通道；**不**单独 CD（`BaseCooldownSeconds=0`）
- 与 `Skill_02` 舒适叠乘顺序写清：`NAP × (1+Comfort) × Mul` 或 SPEC 顺序
- PushMap `SettleMonsterDamage` 前经 Pipeline 调 Handler

### 配置

- Mode2 `Combat_SkillEffectConfig`：补/改 `SkillEffect_04_1`～`_5` 的 `EffectKind`/`EffectParams`/`TriggerHook`
- `Combat_SkillConfig`：`Skill_04` Lv1～5 `EffectImplemented=1` → Bake Mode2 CSV

## 不做

- `Skill_05`+ 效果
- `Skill_01`/`Skill_02`/`Skill_03` 迁入 Pipeline
- Defend 接线
- 解析 Description 正文

## 验收

- [x] PushMap 刺客（`Class_Rogue` / `Skill_04`）换目标后首击增伤可观察；同目标第二击无加成
- [x] Lv1 +20% vs Lv5 +60% 可区分
- [x] Session 无 `Skill_04` 硬编码分支（grep 验证）
- [x] `Skill_04` 全等级 `EffectImplemented=1`；UI-021 绿
- [x] D-073 首条勾选；勾 issue；INDEX SE-01→done

## 依赖

- [SE-00](00-spec-close.md)（done）

## 编码前

- 难度 **3**（框架 + 首技能）；须先读 SE-00 定稿的 Kind/Hook 名
