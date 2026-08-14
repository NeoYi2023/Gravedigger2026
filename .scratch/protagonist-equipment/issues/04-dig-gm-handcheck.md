---
title: Dig HUD GM 主角装备手验入口
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.16
  - SPEC_03 §3.8 D-059
  - SPEC_04 §6 Dig Demo GM
selected_approach: A — DigHudView 增 GM 按钮（对齐「增加躯体材料」/「装备战士强化」）
---

## 目标

Dig HUD 提供最小 GM：发放样例装备、注入公共经验，便于手验 PE-02/PE-03，无需正式 UI。

## 范围

- Dig HUD 按钮（文案可临时中文）：如「获得挖坟圈装备」「装备公共经验+50」
- 接线 `ProtagonistEquipmentService.TryAcquire("Equip_DigRing")`（或 PE-01 样例 Id）与 `SpendCommonExp` / 注入公共池
- 可选：日志打印当前 Level / CurrentExp / DigCursorRadius

## 不做

- 正式装备仓库 UI / 升级面板
- 商店 / Dig 掉落入账
- Mode1/Mode2 差分 UI polish

## 验收

- [x] Play Mode Dig：点 GM 获得装备 → caps/光标变化可观察
- [x] 再点同 Id → 经验/升级可观察
- [x] 公共经验注入后可划入升级（若 GM 提供划入；否则注入 + 日志验证池）
- [x] D-059 手验清单可勾

## 依赖

- PE-03

## 编码前

- 难度 1；可直接编码

## 完成摘要（PE-04）

- `DigHudView`：GM「获得挖坟圈装备」「装备公共经验+50」「划入挖坟圈升级」
- `DigStageController` → `TryAcquire` / `DebugGrantCommonExp(50)` / `TrySpendCommonExp`；日志 Level/Exp/DigCursorRadius
- `DigStageModule` + `MetaShellController` 传入 `ProtagonistEquipmentService`
- SPEC_00 v0.82.9；D-059 **完成**
