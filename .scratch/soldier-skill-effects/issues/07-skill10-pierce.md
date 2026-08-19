---
title: Skill_10 贯穿（远程弹道穿透）
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 SkillCast / 命中方案 D
  - SPEC_03 §3.8 D-073
  - SPEC_04 §9.21 Skill_10 样例
  - SPEC_04 §9.21b RangedPierceExtraHits
selected_approach: B+ — OnProjectileHit Handler；ProjectileView 可穿透通道（非 SkillId 硬编码）
---

## 目标

`Skill_10` 贯穿 Lv1～5：远程箭矢命中 1 名敌人后 **不消失**，继续飞行再命中 **下 1～5 名** 敌人，各造成 **100%** 伤害。

## 范围

- 扩展弹道契约：命中后可 `Continue` 而非立刻 Despawn；记录 `alreadyHitRuntimeIds` 防重复
- Handler `RangedPierceExtraHits` @ `OnProjectileHit`：返回 `extraHitsRemaining` / `DamageMul=1`
- Params：`ExtraHitCount=1..5`（Lv1 再穿 1 = 共 2 人；对齐 Description「命中下 N 名」）
- 飞行方向：命中瞬间速度方向或朝「下一最近未命中敌」—— **须在 SE-00 锁定一种**（建议：保持当前弹道方向，XZ 扫过半径内下一目标；避免每弹道 A*）
- 近战持有本技能：**无弹道则不触发**（本技能挂长弓手）
- 表 + `EffectImplemented=1`

## 不做

- 无限穿透 / 地形穿墙
- Defend
- 改近战挥砍为 AOE

## 验收

- [x] 长弓手（`Class_Longbowman`）一箭可伤多名直线/前方敌人；Lv1 vs Lv5 穿透人数可区分
- [x] 已命中怪不重复结算
- [x] Projectile 通道通用（后续其它穿透 Kind 可复用）；勾 issue；INDEX SE-07→done

## 依赖

- [SE-01](01-skill04-first-strike.md)

## 编码前

- 难度 **3**（弹道契约变更）；SE-00 须锁飞行规则
