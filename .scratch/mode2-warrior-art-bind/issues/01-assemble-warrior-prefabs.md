---
title: 扩展 Assembler 从 Art 组装缺失士兵 Prefab 并绑定 Catalog
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-056
  - SPEC_04 §15.2 士兵 AppearanceId
  - SPEC_04 §6 士兵外观 From-Art
  - SPEC_00 v0.80.5
selected_approach: B — 扩展 WarriorAppearancePrefabAssembler From-Art；Catalog 用 CloneApp02 同款刷新（并集 Mode1+Mode2 CSV）
---

## 目标

Art 就绪的 AppearanceId 有游戏 Prefab 并出现在 Defend/UM Catalog，使圣骑士/暗黑法师/亡灵职业兵在演出、布阵、战斗中显示烘焙模型。

## 范围

- 扩展 `Gravedigger2026/Assets/Editor/Defend/WarriorAppearancePrefabAssembler.cs`
  - 缺 `Warriors/{AppearanceId}.prefab` 且 `Art/Characters/Appearances/{Id}/` 有 Controller + Idle Sprite → 按 `MonsterModelPrefabAssembler` 同结构创建（根 + Visual，`localEuler(90,0,0)`，`sortingOrder=200`，Idle 南向 Sprite + Animator）
  - 已有 Prefab → 保持现有「只修 Visual 结构」行为；**禁止** Capsule 覆盖已组装 Visual
- 组装后刷新 `DefendPrefabCatalog` + `UpgradeManufacturePrefabCatalog` 士兵列表（抽出或复用 `CloneApp02FallbackAppearances.RefreshWarriorCatalogBindings`）
- **禁止**调用 `DefendAssetBuilder.GenerateAll`
- 本轮须产出：`App_0_01` `App_0_02` `App_0_03` `App_0_11` `App_0_12` `App_0_13` `App_0_21` `App_0_22` `App_0_23` `App_0_31` `App_0_32` `App_0_33` `App_4_41` `App_5_51`

## 不做

- 职业布阵区（WA-02）
- 重新导出/Repair Character Creator（Art 已就绪）
- 改运行时选外观算法
- 覆盖 `App_01`–`App_10` / `App_90`–`App_99` 的已有 Prefab 内容（除非只跑 Visual 结构确保）

## 验收

- [x] `Assets/Prefabs/Defend/Warriors/` 存在上述 14 个 Prefab，根下有 `Visual`+SpriteRenderer+Animator，Controller 指向对应 Art
- [x] Defend / UM Catalog 能 `TryGetWarriorAppearance("App_4_41")` 与 `App_5_51` 及全部 `App_0_*`（资产已绑定；进 Play 再确认）
- [ ] Mode2 自动制造圣骑士/暗黑法师：演出 Idle 有图；布阵预览/Combat 为烘焙整角而非空/漏绑定（手验）
- [ ] Console 无 `[DefendStage] Appearance Prefab missing`（针对上述 Id）（手验）
- [x] PushMap Catalog 地图绑定未被 GenerateAll 清空（`PushMap_Demo_01` 仍在）

## 依赖

- WA-00

## 编码前

- 难度 2；整包已选定 **方案 B**；本片只需确认实现细节（Catalog 刷新是抽共享方法还是复制一段）后编码

## 实现备注

- Catalog 刷新改为 `public RefreshWarriorCatalogBindings`，并集 Mode1+Mode2 `Manufacture_BodyAppearanceConfig.csv`（否则 Mode2 专属 Id 进不了共享 Catalog）。
- 本机 Unity 已占用工程，Prefab 按 App_01 模板从 Art GUID 写入；菜单 `Assemble Warrior Appearance Prefabs` 仍可用于以后补装。
