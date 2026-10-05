---
title: 沙盘进入现有玩法与 COC 占位
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.20
  - SPEC_03 §3.8 D-098
  - SPEC_04 §9.1c
---

点击方格进入现有挖坟、商店、自动造兵；结束后回到同一难度沙盘，不调用 `TryAdvanceStage`。COC 只显示占位页。

## Acceptance

- [x] Dig 使用表内 `GameplayConfigId`（样例 `Dig_01`）进入 `DigStageRoot`
- [x] 商店与自动造兵走现有模块，不改模块内部
- [x] 结束回同一难度沙盘
- [x] `CocCombat` 占位页可返回沙盘
