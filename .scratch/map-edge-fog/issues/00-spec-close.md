---
title: 地图边缘迷雾 — SPEC 约定关合
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §13 MapEdgeFog
  - SPEC_00 Changelog v0.83.18
  - CONTEXT MapEdgeFog
selected_approach: A — 世界空间雾片挂地图 Prefab；遮空白为主；可与 CameraFog 叠加
---

## 目标

把「遮可玩区外空白 + 氛围」写成 **地图表现约定**（非新玩法规则）。不新增 SPEC_03 D-xxx。

## 锁定（方案 A）

- 世界空间边缘雾挂 `Prefabs/Maps/{MapId}`；相对 IsoDiamond / `DigMapBounds` 外侧。
- 1 环或 ≤4 边软边雾片；Sorting 高于地面/Water/Foam，低于单位与战斗特效。
- 配置：Prefab SerializeField 或 SO → `Assets/Settings/Maps/`；贴图可复用 `Art/Maps/Fog_*.png`。
- 静态、零 Update；不参与 NavMesh / 空气墙 / 占领。
- 与 `CameraFogOverlay` 职责分离，可叠加。

## 验收

- [x] SPEC_04 §13 中英写入
- [x] CONTEXT 术语 MapEdgeFog
- [x] Art/Maps README 摆放约定
- [x] SPEC_00 Changelog v0.83.18
