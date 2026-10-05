---
title: 没有敌人时走向最终 BOSS
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.21
  - SPEC_03 §3.8 D-099
  - SPEC_04 §9.36
---

士兵找不到敌人时，沿 NavMesh 走向本图唯一最终 BOSS。敌人进入该兵自己的攻击半径后改为攻击。小 BOSS 死亡不算胜利。本片仍不结算通关。

依赖：03b 的洒兵与选敌。父片 [03-deploy-and-boss](03-deploy-and-boss.md)。

选定方案：A（控制器内补 march 分支；缓存唯一 `IsBoss`；无选敌时 `GoalKind.ChaseAnchor`）。

## 规则

- 找不到敌人时沿 NavMesh 最短路径走向唯一 `FinalBoss`，路径排除空气墙。
- 敌人进入该兵攻击半径后，改为攻击该目标并继续战斗。
- 最终 BOSS 与小 BOSS 都已在 03a 刷出。本片不因 BOSS 死亡切换界面。

## Acceptance

- [x] 没有敌人时士兵走向最终 BOSS
- [x] 路上遇到敌人会改打，打完再继续走向最终 BOSS
- [x] 小 BOSS 死亡后战斗不结束
