---
title: 扩大 Demo 验收边界并补齐编码前 SPEC 缝
status: done
difficulty: 1
demo_scope: spec-gate
spec_refs:
  - SPEC_03 §3.8
  - SPEC_03 §3.9
  - SPEC_04 §6
  - SPEC_04 §9.1
  - SPEC_04 §9.2 DigMapId
  - SPEC_04 §9.7 BattleMapId
  - SPEC_04 §13 Prefabs/Maps
  - SPEC_00 Changelog（含 v0.29.0 地图约定；本片 v0.30.0）
---

## 目标

把 Demo 从「仅 Meta 壳」扩大为可验收「一条关卡流水线垂直切片」；并补齐会卡住编码的规则缝。纯文档，无 Unity 代码。

## 必须写入 SPEC

1. **SPEC_03 §3.8 / SPEC_04 §6**：验收项覆盖 Dig → UpgradeManufacture → Defend 垂直切片（临时美术允许）；明确仍排除项（完整技能、正式美术、完整存档 schema 等）
2. **UpgradeManufacture 阶段 `GameplayConfigId`**：空 / 忽略 / 另开表（三选一并关闭）→ **已关闭：忽略**
3. **Defend 最小实现约定**：临时固定出生点；NavMesh 最小可走面；精确 OutsideMap 几何可后置
4. Changelog（SPEC_00）→ **v0.30.0**

## 已关闭（无需再在本 issue 发明）

- Dig/Defend 地图表现共用 `Ground_01`…`Ground_05`（SPEC v0.29.0）
- `DigGameplayConfig.DigMapId` / `DefendGameplayConfig.BattleMapId` → `Assets/Prefabs/Maps/{Id}.prefab`
- Demo 源参考 Example Scene `Grid`/`Ground (N)`，复制为项目 Prefab（禁止运行时引用 `SmallScaleInt/`）
- UM `GameplayConfigId` = **忽略**（可不空；不查表；不另开 UM 玩法配置表）

## 验收

- [x] §3.8 新验收 ID 列表与「范围外」清单清晰（D-001～D-043）
- [x] UM ConfigId 语义有明确条文（忽略）
- [x] Defend 刷怪点/NavMesh「Demo 最小」有明确条文（地图 Prefab 路径已定，本项只管出生点/烘焙范围）
- [x] 本 feature 下其余 `planned` issues 可据此改为 `in-scope`

## 备注

负责人已确认：实现顺序先 Meta 壳，再玩法。难度 1；UM ConfigId=忽略（2026-07-25）。
