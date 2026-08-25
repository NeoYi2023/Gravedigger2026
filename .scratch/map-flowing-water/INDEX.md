# 地图连续流动水面（Built-in 移植）

**状态：** MW-00/01 完成；MW-02 Builder 已落地，待 Unity 打开一次写入 Prefab / 手验。  
**选定方案：** A — 逐行移植厂商 Water/Foam 到 Built-in CG；世界 UV 用 **xz**；资源落 `Art/Maps`。  
**难度：** 2（已确认）

| Issue | 状态 | 说明 |
|-------|------|------|
| MW-00 | done | SPEC_04 §2/§13 + Art/Maps README + CONTEXT：水面两层 Tilemap 约定 |
| MW-01 | done | Built-in shader ×2 + 贴图/材质 → `Art/Maps/Shaders/Water/`（未改地图 Prefab） |
| MW-02 | in-progress | Editor `MapFlowingWaterBuilder` 已提交；打开 Unity / 跑菜单后写 Prefab |

**正式资源：**  
`Assets/Art/Maps/Shaders/Water/`（`Water_Unlit_Masked` / `Foam_Unlit_World`、`waterImage` / `waterNormal`、`Water.mat` / `Foam.mat`）

**源（仅创作，禁止运行时引用）：**  
`Assets/SmallScaleInt/Fantasy kingdom Tileset/Example scene/Shaders/Water/`

**MW-02 菜单：**  
`Gravedigger2026/Maps/Ensure Flowing Water Layers (preserve paint | force Demo pond)`
