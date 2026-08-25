# 地图自动接边（MapAutoTile / Isometric Rule Tile）

**状态：** SPEC 已关合；MA-01 脚本已落地，待 Unity 菜单生成 `RT_WallA`。  
**选定方案：** A — **Wall A** / `RT_WallA`（Isometric Rule Tile；填区域 → Blank 内 + A1 边/角）。  
**难度：** 2（已确认）

| Issue | 状态 | 说明 |
|-------|------|------|
| MA-00 | done | SPEC_04 §2/§13 + README + CONTEXT + Changelog |
| MA-01 | todo | Editor Ensure 已写；打开 Unity 跑菜单生成资产并手验 |
| MA-02 | todo | 在样例地图 Prefab 上刷区手验 |

**与 FlowingWater 边界：** 本功能不覆盖 `Water`/`Foam` 层。

**菜单：** `Gravedigger2026/Maps/Ensure Wall A Rule Tile (RT_WallA)`
