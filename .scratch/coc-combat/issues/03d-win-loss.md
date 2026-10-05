---
title: 击杀最终 BOSS 通关，失败扣次
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.21
  - SPEC_03 §3.8 D-099
  - SPEC_04 §6
  - SPEC_04 §9.36
---

击杀唯一最终 BOSS 为胜利：不扣次，沙盘节点改为已通关，回沙盘且不能再进。退出，或场上全死且库空，为单局失败：已洒出的不回库，剩余次数扣 1。扣完仍大于 0 回沙盘；扣到 0 回主界面。

依赖：03c 的走向最终 BOSS。父片 [03-deploy-and-boss](03-deploy-and-boss.md)。

选定方案：A（Controller 以 `CocRoundResult` 回调；`SandboxProgressService.TryMarkCleared` / `TryConsumeRoundLoss`；壳层按结果回沙盘或 Title）。

## 规则

- 胜利：最终 BOSS 死亡。不扣剩余次数。节点 `Cleared`，方格显示「已通关」且不能再进，然后回同一难度沙盘。
- 单局失败：点退出（无二次确认）；或场上我方全部死亡且士兵库没有可洒的士兵。已洒出的不回库，退出时仍活着的也删除。没洒出的留在库里。然后剩余次数扣 1。
- 扣完仍大于 0：回同一难度沙盘。再次进入时怪物和场上单位重置。
- 扣到 0：回主界面。该节点不能再进。
- 小 BOSS 死亡不算胜利。

## Acceptance

- [x] 击杀最终 BOSS 后沙盘该格显示「已通关」且不能再进
- [x] 退出或兵尽且库空后次数少 1
- [x] 最后 1 次失败回到主界面
- [x] 没洒出的士兵还在库里；已洒出的不回来
