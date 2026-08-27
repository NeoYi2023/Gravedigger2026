# 地图边缘迷雾（MapEdgeFog）

**状态：** ME-00/01 完成；ME-02 待手验。  
**选定方案：** A — 世界空间边缘雾挂 `Prefabs/Maps/{MapId}`；遮可玩区外空白；与 `CameraFogOverlay` 职责分离。  
**难度：** 2（已确认）

| Issue | 状态 | 说明 |
|-------|------|------|
| ME-00 | done | SPEC_04 §13 + CONTEXT + Art/Maps README + Changelog（v0.83.18） |
| ME-01 | done | `MapEdgeFogView` + `MapEdgeFogBuilder`；打开 Unity / 菜单写入 Prefab |
| ME-02 | todo | 手验 / 多图微调尺寸透明度（可选 SO）；本会话不做 |

**权威：** SPEC_04 §13 MapEdgeFog  

**ME-01 菜单：**  
`Gravedigger2026/Maps/Ensure Map Edge Fog`
