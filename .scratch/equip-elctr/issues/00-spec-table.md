---
title: SPEC 引雷规则与 EquipEffect 表行
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-078
  - SPEC_03 §3.10
  - SPEC_03 §3.16
  - SPEC_04 §9.25
  - SPEC_00 v0.82.96
selected_approach: A — DigLightning* event tokens; current-row interval 15/13/11/9/7
---

## 目标

关闭 `Equip_Elctr` 规则：间隔、清坟无掉落无炸药、LootDrop 扫描主要手、入士兵池、4 帧序列、2s 预览。写入 Mode1/Mode2 CSV EquipEffect。

## 验收

- [x] SPEC_03 §3.8 D-078；§3.16 Dig 事件型效果（引雷）+ Demo 目录
- [x] SPEC_04 §9.25 事件键登记
- [x] CSV EquipEffect 非空；ItemCatalog `Equip_Elctr`
- [x] SPEC_00 Changelog v0.82.96；CONTEXT；spec-map
