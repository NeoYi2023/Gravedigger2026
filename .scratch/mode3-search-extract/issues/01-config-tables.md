---
title: SE-01 配置表 Excel + Bake（Mode2）
status: done
difficulty: 2
demo_scope: out-of-scope
spec_refs:
  - SPEC_04 §9.31 SubLevelConfig
  - SPEC_04 §9.32 SearchExtractGameplayConfig
  - SPEC_04 §9.33 SearchExtractWaveSpawnConfig
  - SPEC_04 §14.7 Excel 三行表头
  - SPEC_03 §3.19
approach: A
depends_on:
  - SE-00
  - workshop-config-fields
blocked_by: none（工作坊 2026-09-02 已签字）
---

## 目标

按工作坊签字落地 Mode2 Excel + CSV；`ConfigCsvRepository` 加载三表；SubLevel 增 `SearchExtract` 样例行。

## 范围

- `Assets/ConfigTables/Mode2/Excel/` 子关卡表增列 + 新 SearchExtract 两表
- Bake Mode2 Tables → `Mode2/Csv/`
- 加载器 + 缺列 Warning/失败策略对齐 §9
- 样例：`SearchExtract_01` + 1 条 SubLevel 选项

## 不做

- StageModule / 运行时逻辑（SE-03+）
- 工作坊未签字前 **禁止** 改 Excel（已解除）

## 工作坊签字字段（权威；与 SPEC_04 §9.31～§9.33 对齐）

- SubLevel 增列：`GatherPointCount`（int）、`GatherPointRewards`（string，`N:ItemId;Count|…`）
- 玩法表 PK `SearchExtract_01`；`MapId=PushMap_Demo_01`；`GatherCountdownSeconds=30`；`StageExpReward=0`（Leave 可入账）
- 刷怪表：一行一波 `WaveIndex`；无 `WaveCount` / ClockDirection；`SpawnPointId`；每点 2 波，FirstDelay=2、Interval=8，`Monster_01`×3
- 子关卡样例：`GatherPointCount=2`；`GatherPointRewards=1:Spirit;10|2:Spirit;20`
- Bake：`Gravedigger2026/Config/Bake Mode2 Tables`

## 验收

- [x] 字段工作坊已签字
- [x] Mode2 CSV 可加载；非法 FK 按 §9 策略
- [x] 只改 Excel 后 Bake；未删三行表头

## 落地摘要（方案 A）

- Mode2 Excel：`搜打撤_玩法配置表_SearchExtract_SearchExtractGameplayConfig.xlsx`、`搜打撤_刷怪波次配置表_SearchExtract_SearchExtractWaveSpawnConfig.xlsx`；子关卡表增列并保留三行表头
- 脚本：`.scratch/tools/se01_search_extract_config.py`（同时写 CSV；Unity 菜单 `Gravedigger2026/Config/Bake Mode2 Tables` 可再产 CSV）
- 样例子关卡 `Opt_SE_Demo_01` **未挂**运作表（SE-09 手验再挂）；加载器 Mode1 缺 `GatherPoint*` 列视为空
- StageModule **未做**（SE-03）
