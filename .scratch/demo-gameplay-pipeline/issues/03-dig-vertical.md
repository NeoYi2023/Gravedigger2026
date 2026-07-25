---
title: Dig 垂直切片 — 生成 / 挖掘 / 奖励 / 阶段汇总
status: done
difficulty: 2
demo_scope: in-scope
selected_approach: A
spec_refs:
  - SPEC_03 §3.10
  - SPEC_04 §9.2 DigGameplayConfig（含 DigMapId）
  - SPEC_04 §9.3 GraveQualityConfig
  - SPEC_04 §9.4 MaterialConfig
  - SPEC_04 §9.5 CurrencyConfig
  - SPEC_04 §9.6 DigProtagonistCapabilities
  - SPEC_04 §13 Prefabs/Dig、Prefabs/Maps
---

## 目标

可玩一轮挖坟阶段：倒计时结束 → DigStageSummary → 交还关卡驱动。

## 范围

- 按行 `DigMapId`（`Ground_01`…`Ground_05`）加载地图：`Assets/Prefabs/Maps/{DigMapId}.prefab`（与 Defend 共用地面变体池；Demo 可从 Example Scene `Ground (N)` 复制）
- 临时 Prefab：`Prefabs/Dig/Digger`、按 QualityId 的 Grave（障碍圆）
- 开局/速率生成、有效时长、DigAction 扣血与奖励入账
- 能力：默认初值或样例 Tech 效果即可（完整科技树见 06）

## 不做

- 科技树 UI、正式动画清单、完整存档序列化
- 运行时直接引用 `SmallScaleInt/` 工具目录资源

## 实现摘要（方案 A）

- `DigStageModule` + 纯 C# `DigSessionService` + `DigPrefabCatalog`
- Prefab：`Prefabs/Maps/Ground_*`、`Prefabs/Dig/Digger`、`Grave_Q*`、`DigStageRoot`、`DigRewardFlyer`
- 菜单：`Gravedigger2026/Dig/Generate Dig Prefabs + Catalog`（打开工程会自动生成）
- Dig_01 样例时长改为 **20s** 便于手验；能力 Demo 默认（DigDamage=25，可挖全部品质）
- SPEC v0.33.0：§3.8 D-020；SPEC_04 §6 Dig 方案 A

## 验收

- [x] 坟墓可挖、可掉落；时长到 → 汇总确认 → 进入下一阶段
- [x] 地图按 DigMapId 实例化；Digger/Grave 路径符合 Prefabs/Dig；地图路径符合 Prefabs/Maps

## 依赖

- [00](00-expand-demo-scope.md)、[02](02-config-level-driver.md)
