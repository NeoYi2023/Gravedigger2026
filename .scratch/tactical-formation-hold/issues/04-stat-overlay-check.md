---
title: TFH-04 核对阵型属性加成仍生效
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 属性与专属技能
  - SPEC_03 §3.8 D-094
  - SPEC_04 §9.30 TacticalFormationConfig
  - SPEC_04 §9.7 战术阵型运行时契约
approach: A
depends_on:
  - TFH-00
---

## 目标

确认守槽之后，阵型里的士兵仍然吃到表现有的属性乘区。不新做一套 Buff，也不擅自改血量或攻击倍率。

## 需求指令

现有链路保持：`TacticalFormationConfig.StatModifiers` → `TacticalFormationStatOverlay` → 与魔法书 Combat StatMul 相乘 → 战斗派生。不改 `WarriorInstance.BaseStats`。死亡换档时 `RemainingHp` 仍钳制到新 MaxHP。

本片默认**不改** Excel / CSV。楔阵现表是 `Stat=Strength|Mul=1.15`（5 级 1.30）。攻击力没有单独的乘数列，近战攻击由力量等主属性派生；血量要用 `Stat=MaxHP`。负责人没有给出新数字之前，禁止改表。

核对守槽战斗（TFH-01 之后）开战注册和「仍 ≥ Min 时重算 overlay」两条路仍读到该乘区。若 TFH-01 尚未编码，本片只做不依赖新 Goal 的核对：成员在组内时 `TryGetStatMul` 为力量 1.15，解散或离组后回到 1。

Correctness 已有等级换档覆盖的，补一句断言即可，不要复制一套结算。

## 不做

- 新 Buff 类型、新特效、战斗图标
- 猜测并写入 MaxHP / 攻击倍率
- 改 FormationBond

## 验收

- [ ] 楔阵 1 级组成员的力量乘区仍是 1.15，且不写进 `BaseStats`
- [ ] 离组或解散后该乘区撤掉
- [ ] 没有在未获数值的情况下改 Excel 或 CSV

## 依赖

- TFH-00（SPEC 写明沿用 StatMul）
- 与 TFH-01 可并行；若要在守槽局内手验乘区，放在 TFH-01 之后
