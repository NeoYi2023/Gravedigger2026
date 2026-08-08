# MassCombatSoftCollision — 方案 B+（BMH 借鉴；无 Follow）

**状态：** 规则草案已录入 SPEC v0.74.0（SPEC_03 §3.12 方案 B+；SPEC_04 §9.7 B+ 契约；CONTEXT 术语）。**叠在方案 B 之上**，不替换 FlowField / AttackSlot / LocalDetour。

**执行序（2026-08-07 四目标定序）：** ①大队同点推进（FlowField 已有）②追怪不挤点（SC-02 Surround）③并肩不穿模（SC-01/03 SoftCollision）④超大规模流体（后置评估）。当前授权切片顺序：**SC-01 → SC-02 → SC-03 → SC-04**。

**进展：** SC-01/SC-02/SC-03/SC-04 均已落地（逐切片授权 + 方案比选）；SC-04 对比压测与 B+ 回退旋钮已就位，离线 Mono 参考数字见 [04](issues/04-perf-regression.md)，Unity Editor 权威数字待负责人跑菜单粘贴。

**明确不做：** Follow / 军队半径粘随主角（BMH `NORMAL` + `ArmyRadius` + FollowSpeed）；BMH 军队规模动态 Capsule 缩放。

**保留：** PushMap `Objective` FlowField（共享世界点推进 ≠ 跟随）。

**容量：** 同方案 B，双方约 200；SoftCollision 计入 ≤~2.5 ms/帧预算。

## 切片一览

| ID | 文件 | 依赖 | 难度 | 状态 |
|----|------|------|------|------|
| SC-00 | [00-spec-close.md](issues/00-spec-close.md) | 方案 B / MP-07 | 2 | done（本会话规则录入） |
| SC-01 | [01-soft-collision-core.md](issues/01-soft-collision-core.md) | SC-00, MP-03 | 3 | done（核心编码落地；接线→SC-03） |
| SC-02 | [02-surround-gap-slots.md](issues/02-surround-gap-slots.md) | SC-00, MP-02 | 3 | done（核心编码落地；接线→SC-03） |
| SC-03 | [03-scheduler-wire.md](issues/03-scheduler-wire.md) | SC-01, SC-02 | 3 | done（接线落地；手玩验收待负责人确认） |
| SC-04 | [04-perf-regression.md](issues/04-perf-regression.md) | SC-03, MP-07 | 2 | done（压测扩展落地；Editor 权威数字待粘贴） |

## 并行建议

- SC-01 ∥ SC-02（Core 可并行）
- SC-03 阻塞于 SC-01+SC-02
- Sweep 模式 **P2**，本索引不切片

## NewAgent 分步话术

可粘贴正文：[new-agent-prompts.md](new-agent-prompts.md)（每会话只贴一个分片）。

## 权威

- [SPEC_03 §3.12 方案 B+](../../SPEC_03_GameRules.md)
- [SPEC_04 §9.7 B+ 运行时契约](../../SPEC_04_Technical.md)
- 既有方案 B：[../mass-pathing/INDEX.md](../mass-pathing/INDEX.md)
