---
title: 地图自动接边 — SPEC 约定关合
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_04 §2 Art/Maps/RuleTiles
  - SPEC_04 §13 MapAutoTile / Isometric Rule Tile
  - SPEC_00 Changelog v0.83.10
selected_approach: A — Isometric Rule Tile；不含 FlowingWater
---

## 目标

把「Tile Palette 刷区域、边界自动换配套砖」写成 **编辑器刷图约定**（非玩法规则）。不新增 SPEC_03 D-xxx。

## 锁定（方案 A）

- 包：`com.unity.2d.tilemap.extras`（工程已有）；刷笔 = **Isometric Rule Tile**。
- 资产：`Assets/Art/Maps/RuleTiles/`；Sprite/普通 Tile 仍在 `Art/Maps/Tiles/`。
- Palette：权威刷图 `Art/Maps/Palettes/FantasyTileset_A`。
- 范围：地面与其它装饰/地形配套砖；**不含** FlowingWater 的 Water/Foam。
- 不参与 NavMesh / 空气墙 / WalkSurface / 占领。
- 禁止运行时/已提交 Prefab 引用 `SmallScaleInt/`。

## 验收

- [x] SPEC_04 §2/§13 中英写入
- [x] `Art/Maps/README.md` 刷法
- [x] CONTEXT 术语 MapAutoTile
- [x] SPEC_00 Changelog v0.83.10
