---
title: 空气墙特殊移动字段与职业表
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.14
  - SPEC_03 §3.21
  - SPEC_03 §3.8 D-101
  - SPEC_04 §9.9b
  - SPEC_04 §9.22
  - SPEC_04 §14.7
---

切片 01：写入 SPEC，并在 `AirWall` 与 `Manufacture_ClassConfig` 加上「特殊移动」字段与样例。跑时寻路仍全部挡住，留给 [02-dual-navmesh-flowfield](02-dual-navmesh-flowfield.md)。

整包难度 2。选定方案 A：双烘焙 NavMesh。本片只做作者字段与表。

## 规则

- `AirWall.SupportsSpecialMove`：Inspector 勾选，缺省否。勾选后，对该墙仅 `ClassConfig.SpecialMove=1` 的士兵视为可走（本片尚未接线）。
- `ClassConfig.SpecialMove`：`0`=不具备，`1`=具备。缺列/空 → `0`；非 `0|1` → 加载失败。Mode1 全 `0`；Mode2 仅 `Class_BaseRogue=1`。
- 作者硬约束不变：目标点 / 刷怪点 / BOSS 点不得落在任一空气墙内（含勾选墙）。
- 样例：`Coc_Lv2_01` 的 `AirWall_45` 勾选并改名为 `AirWall_SpecialMove`。

## Acceptance

- [x] Excel 已改（第 1～2 行文档头保留）；见 SPEC_04 §14.7
- [x] CSV 由 Excel Bake（Mode1 与 Mode2）
- [x] 没有只改 CSV、不同步 Excel
- [x] Inspector 能勾「支持特殊移动」；未勾的墙与现状一致
- [x] Mode2 CSV `Class_BaseRogue` 为 `1`，其余为 `0`；Mode1 全 `0`
- [x] `Coc_Lv2_01` 有一堵样例墙勾选
- [ ] 开战寻路仍全部挡住（切片 02 才穿墙）
