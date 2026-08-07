# SC-03 — MassMoveScheduler / Stage 接线

**状态：** todo  
**难度：** 3  
**依赖：** SC-01, SC-02

## 目标

`MassMoveScheduler.Tick`：`desiredDir → LocalDetour → + SoftCollision.Correction → View.Move`；近战多打一认领带 Surround。

## 范围

- PushMap / Defend 注册 SoftCollision；死亡注销
- 交战 `repulsionScale` 降低；保持关闭 NavMeshAgent RVO（既有）
- CombatMoveMode 推导：AttackSlot+近战 Surround；否则 Chase；Objective/Home 无 Follow
- **回归：** 不得出现全队粘随主角

## 不做

- Sweep 技能波
- 改 FlowField 语义

## 验收

- 手玩：多近战围怪可见缺口；重叠减轻；无 Follow 回归
