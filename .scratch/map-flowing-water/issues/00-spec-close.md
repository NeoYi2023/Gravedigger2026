---
title: 地图流动水面 — SPEC 约定关合
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §1 Built-in RP
  - SPEC_04 §2 Art/Maps 与禁止运行时引用 SmallScaleInt
  - SPEC_04 §13 Isometric Tilemap / Grid RotX 90° / XZ 砖面
  - SPEC_00 Changelog
selected_approach: A — 逐行移植两支 shader；世界 UV 用 xz；不改玩法规则
---

## 目标

把「Fantasy kingdom 示例那种连续流动水面」写成 **地图表现约定**（非新玩法规则）。不新增 SPEC_03 D-xxx，除非负责人要求单独验收条。

## 锁定（方案 A）

- 管线：Built-in RP；禁止运行时引用 `SmallScaleInt/` 或 URP include。
- 正式资源：`Assets/Art/Maps/Shaders/Water/`（shader ×2、贴图 ×2、`Water.mat` / `Foam.mat`）。
- 用法：Grid 下两层 Tilemap——`Water`（Chunk + Water.mat，Order 低于 Foam）与 `Foam`（Individual + Foam.mat）。
- Shader 世界 UV：**xz**（Grid RotX 90° 后砖面在 XZ；厂商原 `xy` 在本工程只会沿一条轴流动）。
- 视觉参数对齐厂商 `Water.mat` / `Foam.mat`；深浅仍用原 world-Y 混合（本工程地面 Y≈0，深浅差会很小，可接受）。
- 水面不参与 NavMesh / 空气墙 / 占领规则。

## 验收

- [x] SPEC_04 §13（及 §2 路径一句）中英写入上述约定
- [x] `Art/Maps/README.md` 写清两层刷法
- [x] CONTEXT 术语一行（如 FlowingWater / 流动水面）
- [x] SPEC_00 Changelog；本 issue → done
