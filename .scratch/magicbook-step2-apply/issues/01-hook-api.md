---
title: 造兵不套书 + ApplyAtSlot / Refinalize
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 步骤 3–5 / 魔法书
  - SPEC_04 §9.24
selected_approach: A — WarriorInstance ApplyAtSlot + RefinalizeInstance
---

## 目标

造兵循环去掉魔法书；钩子改为对池内实例单槽 apply；支持 Refinalize + persist。

## 完成备注

- `TryCraftOne` 默认定种族 + DefaultSkillIds；无造兵钩子
- `ApplyEquippedBookAtSlot` / `ApplyRemainingSlots` 对 `WarriorInstance`
- `ForceClass` 命中 Clear 重授；`RaceWeightPick`/`StatMul Primary` 用 SourceItemIds
- `ApplyBookAtSlotAndRefinalize` / `ApplyRemainingBooksAndRefinalize` / `RefinalizeInstance`
