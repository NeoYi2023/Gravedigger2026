---
title: CI-01 ClassConfig/MonsterConfig SilhouetteIconAssetId + loader
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-089
  - SPEC_04 §9.9b
  - SPEC_04 §9.19
  - SPEC_04 §14.7
depends_on:
  - 00-spec-close
approach: A
---

# 01 — 配置表新列 + 加载器

## 目标

为职业/怪物表增加「士兵简画图标」列；Excel 为源 → Bake CSV；加载器解析 + 内存 `TableOrder`；Resources 图标副本。

## 步骤

1. **SPEC 已关** — 字段名 `SilhouetteIconAssetId`（中文：士兵简画图标）。
2. **Excel（Mode1 + Mode2）** 保留三行表头增列：
   - `制造_职业配置表_Manufacture_ClassConfig.xlsx`
   - `防守_怪物配置表_Defend_MonsterConfig.xlsx`
3. **Demo 填值（Mode2 优先）：**
   - 职业按 `BaseClass`：`HPPK_UI_Class_BaseWarrior|Archer|Mage|Rogue`（进阶复用同族）
   - `Monster_01`～`04` → `HPPK_UI_Monster_01`～`04`；其余暂复用 `_01`
   - Mode1 同步空列（兼容共享加载器）
4. Bake Mode1 / Mode2 Tables；核对 CSV 表头含新列。
5. C#：
   - `ClassConfigRow.SilhouetteIconAssetId` + `TableOrder`
   - `MonsterConfigRow.SilhouetteIconAssetId` + `TableOrder`
   - `ConfigCsvRepository.LoadClasses` / `LoadMonsters`：`OptionalText`；缺列/空 → `""`；写 `TableOrder = i`
6. 复制源图到 `Assets/Resources/UI/Icons/`（`HPPK_UI_*`、`HPPK_UI_Class_*`、`HPPK_UI_Monster_*`）；确认 Sprite 导入。

## 验收

- [x] Mode1+Mode2 Excel 第 1～2 行保留；新列三行对齐
- [x] Bake 后 CSV 含 `SilhouetteIconAssetId`
- [x] 加载器不因缺列失败；Mode2 样例行有非空简画 Id
- [x] `Resources.Load<Sprite>("UI/Icons/HPPK_UI_Class_BaseWarrior")` 等可用
- [x] 交付摘要列出 Excel 路径与 Bake 结果（§14.7）

## 备注

禁止只改 CSV。运行时 HUD 见 02。

选定方案：A（2026-09-07）
