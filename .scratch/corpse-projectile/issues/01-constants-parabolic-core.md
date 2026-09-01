---
title: 常量表 + 抛物线击飞核心
status: done
difficulty: 2
demo_scope: in-scope
approach: A
spec_refs:
  - SPEC_03 §3.12 DeathCorpseProjectile
  - SPEC_04 §9.20b CombatConstantConfig
  - SPEC_04 §15.5
  - SPEC_03 §3.8 D-083
---

## 目标

落地常量键与 **抛物线轨迹采样**（取代纯 XZ 线性 `Lerp`）；**不接** Session 砸击、**不改** View 死亡流程（CP-02/03 接线）。

## 范围

1. **常量表（Mode1 + Mode2）** Excel/CSV 增行：
   - `DeathKnockbackPeakHeight`（样例 `1.2`）
   - `DeathCorpseSmashDamageMul`（样例 `1`）
   - `DeathCorpseSmashHitRadius`（样例 `0.55`）
   - 更新 `DeathDie2KnockbackThreshold` 注释（砸击门闩同键）
2. **代码：** `CombatConstantKeys` / `CombatRuntimeTuning` 加载三键 + Safety 默认值
3. **`MonsterDeathPresentation` 扩展：**
   - `ShouldEnableCorpseSmash(float knockbackDistance)` → `distance >= DeathDie2KnockbackThreshold`
   - `TrySampleParabolicKnockback(origin, end, y0, startedAt, duration, now, out position)` — Y 公式见 SPEC §15.5
   - 保留 `TrySampleKnockback` 或内部改调抛物线（**废止**对外仍走线性者须在 PR 说明）
   - `TryDirectionalKnockbackTarget` 仍只算 XZ 终点（Y 由抛物线采样）
4. **自检：** 纯 C# 断言或 Editor 菜单 `Run Corpse Projectile Parabolic Checks`（峰值 t=0.5、终点 XZ、duration 结束）

## 不做

- `TryApplyCorpseSmashDamage`（CP-02）
- `PushMapMonsterAgentView` / `MonsterAgentView` 接线（CP-03/04）
- 美术 polish（落地尘土等）

## 验收

- [ ] Mode1+Mode2 `Combat_CombatConstantConfig.csv` 含三新键样例值
- [ ] `CombatRuntimeTuning` 可读新键
- [ ] 抛物线：t=0 起点、t=1 终点 XZ、t=0.5 Y 峰值 ≈ `y0+PeakHeight`
- [ ] `ShouldEnableCorpseSmash(0.9)=false`、`ShouldEnableCorpseSmash(1.0)=true`（阈值样例 1）
- [ ] 现有 `ComputeKnockbackDistance` / `ShouldPreferDie2` 行为 **不变**

## 依赖

- [00-spec-close.md](00-spec-close.md)

## 编码前

- 难度 2：可直接编码（SPEC 已锁）
