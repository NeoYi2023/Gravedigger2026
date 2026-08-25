---
title: 为首套配套砖创建 Isometric Rule Tile 并挂 Palette
status: todo
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §13 MapAutoTile
  - SPEC_04 §2 Art/Maps/RuleTiles
depends_on:
  - MA-00
selected_approach: A — Wall A / RT_WallA（Editor Ensure 生成 Isometric Rule Tile + FantasyTileset_A 槽）
---

## 目标

落地第一套可刷的自动接边刷笔，使 Palette 选中后刷区域时边/角自动匹配。

## 锁定

- 砖族：**Wall A**（无 Cliff_*；墙基用 Wall）
- 方案 A：子集 **Wall A1_N/E/S/W** + 内部 **Blank**
- 资产：`Assets/Art/Maps/RuleTiles/RT_WallA.asset`
- 菜单：`Gravedigger2026/Maps/Ensure Wall A Rule Tile (RT_WallA)`
- 脚本：`Assets/Editor/Maps/WallARuleTileBuilder.cs`

## 步骤（负责人）

1. 打开 Unity 工程，等编译完成。
2. 菜单 **Gravedigger2026 → Maps → Ensure Wall A Rule Tile (RT_WallA)**。
3. Tile Palette → **FantasyTileset_A** → 选 `RT_WallA`（槽约 `(-1,0)`）。
4. 在临时 Tilemap 上填一块矩形区域，确认内部 Blank、外沿为墙朝向砖。

## 验收

- [ ] `Art/Maps/RuleTiles/RT_WallA.asset` 存在且为 Isometric Rule Tile
- [ ] **FantasyTileset_A** Palette 可见该刷笔
- [ ] 空 Tilemap 上刷矩形区域时边界为边砖、角为角砖（非全中心墙）
- [ ] 无 `SmallScaleInt/` 引用
