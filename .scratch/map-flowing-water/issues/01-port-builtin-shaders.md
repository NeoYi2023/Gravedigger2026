---
title: 移植 Water/Foam shader 到 Built-in 并落盘 Art/Maps
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §1 Built-in RP
  - SPEC_04 §2 Art/Maps
  - SPEC_04 §13 地图 Tilemap 表现（MW-00 关合后以该节水面小节为准）
  - .scratch/map-flowing-water/issues/00-spec-close.md
depends_on:
  - MW-00
selected_approach: A — 逐行 CG 移植；世界 UV xz；材质参数对齐厂商
---

## 目标

把厂商 URP 两支 shader 改成 Built-in CG，连同贴图与材质复制到 `Art/Maps`，使编辑器里给 TilemapRenderer 赋材质后能看到连续流动（**不**改 `Ground_*` / `PushMap_*` Prefab）。

## 源

`Gravedigger2026/Assets/SmallScaleInt/Fantasy kingdom Tileset/Example scene/Shaders/Water/`

- `WaterUnlitDistort.shader` → `SmallScale/URP2D/Water_Unlit_Masked`
- `Foam_Unlit_World.shader` → `SmallScale/URP2D/Foam_Unlit_World`
- `waterImage.png` / `waterNormal.png` / `Water.mat` / `Foam.mat`

## 落盘（本会话）

`Assets/Art/Maps/Shaders/Water/`

- `Water_Unlit_Masked.shader` → `Gravedigger/Maps/Water_Unlit_Masked`
- `Foam_Unlit_World.shader` → `Gravedigger/Maps/Foam_Unlit_World`
- `waterImage.png` / `waterNormal.png`（Repeat；normal 非 sRGB / Normal Map）
- `Water.mat` / `Foam.mat`（槽位与 float 对齐厂商）

世界 UV：`worldPos.xz`。未改地图 Prefab。

## 验收

- [x] 两支 shader 为 Built-in CG（无 URP `#include` / `LightMode=Universal2D`）
- [x] 资源仅在 `Art/Maps/Shaders/Water/`
- [x] 世界 UV 用 xz
- [x] 本会话不改地图 Prefab（刷水 = MW-02）
- [ ] Unity 手验：无粉红；多砖无接缝滚动；Foam 层 Individual 才有岸边白边（负责人）
