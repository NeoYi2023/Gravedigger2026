---
title: COC 观察范围与三维选敌
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.21
  - SPEC_03 §3.8 D-100
  - SPEC_04 §9.9b
  - SPEC_04 §9.19
  - SPEC_04 §9.35
---

只在 COC 里，用职业观察范围替换全局迷雾半径和「最近 + 警戒半径」选敌。按距离、目标价值、目标类型的乘积分锁定目标。本片不放可破坏障碍。

依赖：无。后续 [02-destructible](02-destructible.md) 才把障碍算进候选。整包难度 3。选定方案 A：COC 自己算目标，再交给现有追击；`PushMapAdvanceView.TryGetEngageMonster` 的默认最近路径不改。

## 规则

- `Manufacture_ClassConfig` 增加 `ObserveRange`、`DistancePriority`、`TargetValueMode`、`TargetTypeScores`。Mode1 与 Mode2 Excel 都加列（保留三行表头）再 Bake。Mode2 样例：全职业 `ObserveRange=2`、`优先最近`、`由大到小`、`敌方单位;1`。
- `Defend_MonsterConfig` 增加 `TargetValue`。样例全怪物填 `1`。缺省 1。本片不读 `ObstaclePathMode`（留给 03）。
- 观察范围是地面 XZ 圆（世界单位），同时是选敌筛选半径和该存活士兵的迷雾半径。缺省或 ≤0 用 2。`CocGameplayConfig.RevealRadius` 不再给士兵盖章，只作这个回退。
- 需要新目标时才算分：`最终分 = 距离分 × 价值分 × 类型分`，取最高。锁定到目标死亡，或目标中心离开观察范围。更高分出现也不换。COC 停用「更近就抢」和追击卡住换目标。
- 距离：地板格 = 1 世界单位。格数 = `max(1, ceil(XZ中心距 / 1))`。优先最近 = `1/格数`（2 位小数）。优先最远 = 格数。迷雾格 0.25 不参与距离分。
- 价值：`由大到小` 用怪物 `TargetValue`；`无视价值` 乘 1。
- 类型：只有写进 `TargetTypeScores` 的才是候选。本片样例只有 `敌方单位`。
- 平局：价值分更高，再取更近，再取稳定 Id。
- 已进 `AttackRange` 就攻击；否则走现有 AttackSlot。没有候选时仍走向唯一最终 BOSS。
- 推图、防守、搜打撤不读这些列，选敌不变。

## Acceptance

- [x] Excel 已改（第 1～2 行文档头保留）；见 SPEC_04 §14.7
- [x] CSV 由 Excel Bake（Mode1 与 Mode2）
- [x] 没有只改 CSV、不同步 Excel
- [ ] 两名观察范围不同的士兵，迷雾圆和开始追怪的距离不同（样例全职业为 2，手测时改表）
- [x] 价值相同且都是「优先最近」时，打观察范围内最近的怪
- [x] 锁定后旁边出现更近的怪也不换，直到当前目标死亡或走出圆
- [x] 没有候选时仍走向最终 BOSS
- [x] 推图 / 防守 / 搜打撤仍是原来的最近选敌
