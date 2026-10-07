---
title: COC 怪物绕开与无视障碍
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.21
  - SPEC_03 §3.8 D-100
  - SPEC_04 §9.19
---

配表 `ObstaclePathMode`。COC 里「视为障碍」绕开墙，「无视障碍」直线顶墙不拆。本片不实现「视为目标」拆墙。

依赖：[02-destructible](02-destructible.md)。后续：[03b-treat-as-target](03b-treat-as-target.md)。

选定方案 A：COC 覆盖层判定模式，再交给现有追击；共享 View 只加可选钩子。

## 规则

- `Defend_MonsterConfig.ObstaclePathMode`。缺省 `视为障碍`。非法值整表加载失败。Mode1 与 Mode2 Excel 都加列再 Bake。
- 只在 COC 读。推图、防守、搜打撤忽略本列，寻路不变。
- `视为障碍`：把墙当不可走，绕开（沿用切片 02 雕刻）。
- `无视障碍`：不绕。怪物沿 XZ 直线走向当前士兵，顶在空气墙上，直到墙被士兵或其他怪拆掉。本片不打墙。
- `视为目标`：本片按 `视为障碍` 绕开；拆墙留给 03b。
- 样例：`Monster_01` 视为障碍、`Monster_02` 无视障碍、`Monster_03` 视为目标（本片仍绕开）；`Coc_Lv2_01` 普通怪各刷 1 只。

## Acceptance

- [x] Excel 已改（第 1～2 行文档头保留）；见 SPEC_04 §14.7
- [x] CSV 由 Excel Bake（Mode1 与 Mode2）
- [x] 没有只改 CSV、不同步 Excel
- [ ] 「视为障碍」的怪绕开墙去打士兵
- [ ] 「无视障碍」的怪顶在墙上，直到墙被拆掉才能过去
- [ ] 「视为目标」本片仍绕开，不拆墙
- [ ] 推图 / 防守 / 搜打撤的怪物寻路与改表前一致
