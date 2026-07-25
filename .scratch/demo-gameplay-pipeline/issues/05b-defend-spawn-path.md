---
title: Defend — 刷怪波次与最小寻路扣盾
status: todo
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.12 刷怪 / 目标选择与寻路
  - SPEC_04 §9.18 WaveSpawnConfig
  - SPEC_04 §9.19 MonsterConfig
  - SPEC_04 §9.7 NavMesh 约定
---

## 目标

按剩余秒激活刷怪行；临时固定出生点；怪物 NavMesh 接近并普攻扣主角护盾。

## 范围

- 在已加载的 `Prefabs/Maps/{BattleMapId}` 上刷怪与寻路（地图加载见 05a）
- Demo 最小：固定点 / 地图内随机即可（精确 OutsideMap 后置，以 00 条文为准）
- 临时 `Prefabs/Defend/Monsters/{ModelId}`
- 护盾 ≤ 0 → LevelFailure 钩子可先打日志或切 Ended

## 不做

- 士兵还击、技能、完整失控
- 另造一套与 Dig 不同命名体系的地图 Prefab（须共用 `Ground_*`）

## 验收

- [ ] 样例波次能出怪；怪能碰到主角扣盾

## 依赖

- [05a](05a-defend-prepare-shield.md)
