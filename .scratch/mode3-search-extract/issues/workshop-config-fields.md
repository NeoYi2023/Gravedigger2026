# 工作坊：配置表字段（SE-01 前置）

**状态：** **已签字**（2026-09-02）→ 解锁 [01-config-tables.md](01-config-tables.md)  
**权威：** [SPEC_04 §9.31～§9.33](../../SPEC_04_Technical.md) · [SPEC_03 §3.19](../../SPEC_03_GameRules.md)

本文件 **不是** 编码 issue。负责人已逐项确认；SE-01 Agent 可按本文摘要改 Excel 并 Bake。

---

## 1. 子关卡表 `Level_SubLevelConfig`

| # | 决策项 | SPEC 草案 | 选项（择一或说明） |
|---|--------|-----------|-------------------|
| 1.1 | 搜集点数量 N | 新列 `GatherPointCount`（int，仅 SearchExtract） | **A) 单列 int** |
| 1.2 | 每点奖励编码 | 新列 `GatherPointRewards`（string） | **A)** `N:ItemId;Count\|…`；`\|` 分段，`N:` 开头为新点 |
| 1.3 | 路线图展示 | 点奖励是否在 UI-031 摘要显示 | **C) 两者都展示**（入账仍分离） |

**签字：** [x] 1.1  [x] 1.2  [x] 1.3

---

## 2. 玩法表 `SearchExtractGameplayConfig`（§9.32）

| # | 决策项 | SPEC 草案 | 选项 |
|---|--------|-----------|------|
| 2.1 | 主键列名 | `GameplayConfigId` | **确认** |
| 2.2 | 地图 | `MapId`（同 PushMap 合法池） | **确认** |
| 2.3 | 默认搜集倒计时 | `GatherCountdownSeconds`（float，全局默认） | **A) 全局 per GameplayConfigId** |
| 2.4 | 阶段经验 | `StageExpReward` | **B) 可配正整数**（Leave 时入账；0=不发） |
| 2.5 | Excel 四段名 | `搜打撤_玩法配置表_SearchExtract_SearchExtractGameplayConfig` | **确认** |

**签字：** [x] 2.1～2.5

---

## 3. 刷怪表 `SearchExtractWaveSpawnConfig`（§9.33）

| # | 决策项 | SPEC 草案 | 选项 |
|---|--------|-----------|------|
| 3.1 | 搜集点键 | `GatherPointOrder`（对齐 `ObjectiveOrder`） | **确认** |
| 3.2 | 波次建模 | 每波一行 vs 一行含 `WaveCount` | **A) 一行一波**（`WaveIndex`；**去掉** `WaveCount`） |
| 3.3 | 第一波前置 | `FirstWaveDelaySeconds` | **确认**（自点激活起） |
| 3.4 | 波间间隔 | `WaveIntervalSeconds` | **确认**（第 2 波起） |
| 3.5 | 出怪方向 | `SpawnDirection` | **A) 仅 `SpawnPointId`**（Demo 不设 ClockDirection） |
| 3.6 | 每波怪物 | `MonsterId` + `SpawnCount` | **确认** |
| 3.7 | per-point 倒计时覆盖 | 可选列 `GatherCountdownSeconds` | **B) 不加**，只用 §9.32 全局 |
| 3.8 | Excel 四段名 | `搜打撤_刷怪波次配置表_SearchExtract_SearchExtractWaveSpawnConfig` | **确认** |

**签字：** [x] 3.1～3.8

---

## 4. 规则/UI 待澄清（影响表或 HUD）

| # | 决策项 | SPEC 默认 | 选项 |
|---|--------|-----------|------|
| 4.1 | 倒计时 HUD | TBD | **A) Combat 顶栏显示剩余秒** |
| 4.2 | 搜集中手动杀怪掉落 | TBD | **A) 无额外掉落**，仅倒计时胜利清场 |
| 4.3 | 样例 GameplayConfigId | — | **`SearchExtract_01`** |
| 4.4 | 样例 N 与地图 | — | **`PushMap_Demo_01`，N=2**（前 2 个 Objective） |

**签字：** [x] 4.1～4.4

---

## 5. 负责人确认区

- 确认日期：**2026-09-02**
- 选定方案摘要（可粘贴）：

```
1.1 A  GatherPointCount int
1.2 A  GatherPointRewards 编码：| 分段；N: 开头为新点（同点多道具可续 ItemId;Count）
      例 1:Spirit;10|2:Spirit;20 ；同点两道具 1:Spirit;10|Gold;5
1.3 C  UI-031 同时展示点奖励摘要 + 子关卡 Reward；入账分离（点=单点胜利；关=Leave）
2.1–2.2–2.5 确认命名；Excel 搜打撤_玩法配置表_SearchExtract_SearchExtractGameplayConfig
2.3 A  GatherCountdownSeconds 仅玩法表全局（§9.33 无覆盖列）
2.4 B  StageExpReward 可配 ≥0；Leave 时 AddExperience；0=不发
3.1/3.3/3.4/3.6/3.8 确认；Excel 搜打撤_刷怪波次配置表_SearchExtract_SearchExtractWaveSpawnConfig
3.2 A  一行一波 WaveIndex；去掉 WaveCount。同点各波行 FirstDelay/Interval 须相同，以 WaveIndex=1 为准
3.5 A  仅 SpawnPointId（无 SpawnDirection / SpawnClockHour）
3.7 B  无 per-point 倒计时列
4.1 A  Combat 顶栏剩余秒（HUD 实现后置 SE-04）
4.2 A  手动杀怪无掉落；不走 MonsterConfig Loot
4.3 SearchExtract_01
4.4 MapId=PushMap_Demo_01，GatherPointCount=2
样例数值：倒计时 30s；每点 2 波；FirstDelay=2s、Interval=8s；每波 Monster_01 ×3
点奖励 1:Spirit;10|2:Spirit;20
样例 StageExpReward=0（列可配；Leave 入账 max(0,表值)）
Mode2 Excel：Assets/ConfigTables/Mode2/Excel/
Bake：Gravedigger2026/Config/Bake Mode2 Tables → Mode2/Csv/
```

- **SE-01 可开工：** [x] 是  [ ] 否（备注：仍须 §8 难度确认后再改 Excel）

---

## 验收（工作坊完成）

- [x] §9.31～§9.33 列名在 SPEC 中已无 `TBD`（或仅保留明确 defer 项）
- [x] 样例行数值（倒计时秒、波次、怪物 Id）已写在签字摘要或 Excel 草稿
- [x] Mode2 Excel 路径与 Bake 菜单已告知 SE-01 Agent
