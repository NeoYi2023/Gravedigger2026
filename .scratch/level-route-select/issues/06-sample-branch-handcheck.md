# LRS-06 手验清单 — 关卡路线多选一（D-086 / UI-031）

## 前置

1. Unity 菜单：`Gravedigger2026/Level/Ensure LevelRouteSelectRoot Prefab (UI-031)`（可选；无 Prefab 时 Meta 用 RuntimeFactory）
2. 若 Mode2 `关卡_关卡运作表_Level_LevelOperationConfig.xlsx` 曾被 Excel 占用未写入：关闭 Excel 后重跑 `python .scratch/tools/lrs02_level_route_config.py`（CSV 已就绪）
3. Mode2 进档

## Mode2 Level_01 分支

| 步骤 | 期望 |
|------|------|
| Hub 选 Level_01 → 进入 | 打开路线选择；Stage1「商店」可点；Stage2 两张挖坟卡锁定 |
| 点商店 → 关闭商店推进 | 回路线；商店已通关；Stage2「挖坟A/B」均可点；连线可见 |
| 任选 Dig A 或 Dig B 通关 | 发精魂（A=30 / B=50）；解锁自动制造；另一 Dig 仍可点但本关通常只走一条 |
| 继续 AM → UM → PushMap 通关 | 末选项空 UnlockNext → VictorySettlement；回难度 Hub |

## Mode1 线性

| 步骤 | 期望 |
|------|------|
| Level_01 进入 | 路线图仅每 Stage 一选项链 Dig→UM→PushMap |
| 逐项通关 | 行为与旧线性等价；末 PushMap 空 UnlockNext → 胜利 |

## 配置抽查

- Mode2 `Level_LevelOperationConfig` Stage2：`Opt_L01_S2_Dig_A` + `Opt_L01_S2_Dig_B`
- `Level_SubLevelConfig` UnlockNext 仅指向 Stage+1；末行 PushMap UnlockNext 空
- UI/日志含 LevelId / Stage / Option / GameplayType

## Play Mode（负责人勾选）

- [ ] Mode2 分支手验通过
- [ ] Mode1 线性手验通过
- [ ] 路线连线与奖励发放可观察
