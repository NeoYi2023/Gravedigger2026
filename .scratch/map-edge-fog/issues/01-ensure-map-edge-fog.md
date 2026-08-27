---
title: Editor Ensure 地图边缘迷雾到样例 Prefab
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §13 MapEdgeFog
  - .scratch/map-edge-fog/issues/00-spec-close.md
depends_on:
  - ME-00
selected_approach: A — MapEdgeFogView + MapEdgeFogBuilder；Fog_1 单片 RotX90 铺 XZ
---

## 目标

在 `Ground_01`…`05`、`PushMap_Demo_01`…`03` 上 Ensure 世界空间边缘雾，打开地图即遮可玩区外空白。

## 范围

- 运行时薄组件 `MapEdgeFogView`（SerializeField：贴图/颜色/尺寸倍率/sorting；相对 `DigMapBounds` 适配）
- Editor `MapEdgeFogBuilder`：菜单 Ensure；one-shot InitializeOnLoad；Batch `-executeMethod`
- 贴图默认 `Art/Maps/Fog_1.png`；Sorting 高于地面 Tilemap（含 Water/Foam），不挡玩法逻辑
- **不**改 NavMesh / AirWall / WalkSurface / CameraFogService

## 非范围

- 屏幕 CameraFog 重新开启 / 调参（ME-02 外）
- 粒子雾、RT 遮罩、URP Fog
- 每图美术精修（ME-02）

## 验收

- [x] 菜单 `Gravedigger2026/Maps/Ensure Map Edge Fog` 已实现（Builder + one-shot）
- [x] 脚本落地：`MapEdgeFogView` + `MapEdgeFogBuilder`；打开 Unity / 跑菜单后写入 Prefab
- [x] 不参与寻路/空气墙；无每帧 Update 逻辑
- [x] Art/Maps README 写明 Ensure 菜单

**注：** 本机无 Unity 可执行文件时 Prefab 二进制未在本会话改写；负责人打开工程或跑菜单即可落盘。
