---
title: TF-05 激活/解散与属性·专属技能 overlay
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 属性与专属技能加成 / 解散
  - SPEC_04 §9.30 StatModifiers / ExclusiveSkillIds / ExclusiveSkillEffectIds
approach: A
depends_on:
  - TF-04
---

## 目标

阵型激活态应用 `StatModifiers` 与 `ExclusiveSkillIds` runtime overlay；存活 <Min 解散并撤 overlay。

## 范围

- 开战激活评估 + 成员死亡 / Rebel 后再评估
- Stat overlay：对齐 Combat `StatMul` 语义；不改 `WarriorInstance.BaseStats`
- ExclusiveSkill overlay：SkillCast / EffectKind 管线只读层；可选 `ExclusiveSkillEffectIds` 直挂 SkillEffectConfig
- 解散：撤 overlay；GoalKind 回退；Defend 新 Home = 当前世界坐标
- 与 FormationBond Buff 叠加；重复 `SkillEffectId` Warning

## 不做

- 新 EffectKind Handler（除非样例专属技能需要最小桩）
- Prepare 坐标回写

## 验收

- [x] 激活态 Stat 可观察（如 Strength Mul）
- [x] 阵亡至 <Min 立即解散，加成消失
- [x] Rebel 退出阵型且无加成

## 依赖

- TF-04

## 落地摘要

选定方案 A（难度 2）：`TacticalFormationRuntimeService` 持 overlay 会话；`StatModifiers` 解析为 StatMul 与魔法书 buff 乘积，开战写入战斗派生；`SkillEffectPipeline` / SkillCast 只读拼接 `ExclusiveSkillIds` 与直挂 `ExclusiveSkillEffectIds`。阵亡 / Rebel `TryNotifyMemberLost`：Rebel 撤本人 overlay；存活 <Min 解散并重算派生（RemainingHp 钳制新 MaxHP），PushMap 回 `Objective`，Defend Home=当前世界坐标。加载期与 FormationBond `BondBuff` 重复 `SkillEffectId` Warning。Correctness 菜单同 TF-04a（含 overlay/解散用例）。样例表 Stat 填充 → TF-06。
