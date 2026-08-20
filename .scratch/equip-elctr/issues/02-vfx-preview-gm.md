---
title: 落雷序列帧、士兵待机预览、Dig HUD GM
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-078
  - SPEC_03 §3.10 Demo GM
  - SPEC_04 §13
selected_approach: A — catalog sprites + runtime View; DefendPrefabCatalog appearance
---

## 目标

播 `Elctr_0`～`Elctr_3`；产兵则坟位待机 2s；HUD 获得/划入引雷。

## 验收

- [x] `DigLightningBoltView` + Catalog 序列帧
- [x] Appearance Prefab 待机预览后销毁 View
- [x] Dig HUD GM 获得引雷 / 划入引雷升级
