---
title: AM-RAIN-01 常量 + Dig 图加载
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_04 §9.20b AutoMfg*
  - SPEC_04 §2 Resources/UI/Dig
  - SPEC_04 §14.7
depends_on:
  - AM-RAIN-00
---

## 目标

Mode2 `CombatConstantConfig` 写入 `AutoMfg*` 样例；`CombatConstantKeys` + Safety；`Art/UI/Dig` → `Resources/UI/Dig` 副本；缺图按 BodySlot 回退。

## 验收

- [x] Excel + Mode2 CSV（UTF-8）；Mode1 CSV 同步安全默认
- [x] Keys/Safety 齐全（`AutoMfgPresentationConstants`）
- [x] Resources 有 Dig 图与 MagicCircle_1 + UnknownSoldier_1
- [x] `DigBodyArtLoader` 可解析 ArtAssetId / BodySlot 回退
