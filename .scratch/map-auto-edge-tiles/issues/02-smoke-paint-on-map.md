---
title: 在样例地图 Prefab 上刷区手验自动接边
status: todo
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_04 §13 MapAutoTile
  - SPEC_04 §13 Isometric Tilemap / Prefabs/Maps
depends_on:
  - MA-01
selected_approach: A — 仅在地面 Tilemap 层手刷一小块验证；不改玩法标记
---

## 目标

在正式地图 Prefab 上确认 MapAutoTile 与现有 Isometric Grid（RotX≈90°）兼容，且不破坏寻路足迹。

## 步骤

1. 打开任一 `Prefabs/Maps/Ground_*`（或约定样例 PushMap）。
2. 在**地面** Tilemap（非 Water/Foam）用 MA-01 刷笔刷一小块区域。
3. 确认边/角自动正确；Play 模式视觉正常。
4. 确认 `WalkSurface` / NavMesh / AirWall / DigMapBounds 未因刷砖改变。
5. 若不希望样例保留试刷内容，提交前擦除或仅留有意设计的区域。

## 验收

- [ ] 样例 Prefab 上刷区边/角正确
- [ ] 玩法足迹与碰撞标记不变
- [ ] 无 `SmallScaleInt/` 引用
