# MassCombatPathing — 大规模战斗寻路（方案 B）

**状态：** 规则已录入 SPEC v0.73.6（SPEC_03 §3.12 MassCombatPathing / §3.14 推进；SPEC_04 §6/§9.7/§9.22）。**选定方案 B**（FlowField + AttackSlot + LocalDetour）。MP-01～MP-07 已落地（含 200v200 压测入口；约定机型 ≤2.5 ms 数字待手验粘贴）。

**工作量：** 整体难度 **3**，一律拆步；每 issue 独立 Agent 会话；编码前须负责人授权该切片。

**容量目标：** 双方各约 200（合计约 400）存活可移动单位；移动逻辑预算导向 ≤ ~2.5 ms/帧。

## 切片一览

| ID | 文件 | 依赖 | 难度 | 状态 |
|----|------|------|------|------|
| MP-00 | [00-spec-close.md](issues/00-spec-close.md) | — | 3 | done（本会话规则录入） |
| MP-01 | [01-flow-field-core.md](issues/01-flow-field-core.md) | MP-00 | 3 | done |
| MP-02 | [02-attack-slot.md](issues/02-attack-slot.md) | MP-00 | 3 | done |
| MP-03 | [03-local-detour-spatial-hash.md](issues/03-local-detour-spatial-hash.md) | MP-00 | 3 | done |
| MP-04 | [04-pushmap-advance-wire.md](issues/04-pushmap-advance-wire.md) | MP-01, MP-03 | 3 | done |
| MP-05 | [05-chase-combat-wire.md](issues/05-chase-combat-wire.md) | MP-02, MP-03, MP-04 | 3 | done |
| MP-06 | [06-defend-parity.md](issues/06-defend-parity.md) | MP-05 | 2 | done |
| MP-07 | [07-perf-stress-200.md](issues/07-perf-stress-200.md) | MP-05 | 3 | done（压测入口；约定机型 ≤2.5ms 数字待手验粘贴） |

## 并行建议

- MP-01 ∥ MP-02 ∥ MP-03（纯 Core 可并行；合并前对齐 API）
- MP-04 阻塞于 MP-01+MP-03
- MP-06 ∥ MP-07 可在 MP-05 后部分并行

## 权威

- [SPEC_03 §3.12 大规模战斗寻路](../../SPEC_03_GameRules.md)
- [SPEC_03 §3.14 士兵推进 / FlowField](../../SPEC_03_GameRules.md)
- [SPEC_04 §9.7 大规模战斗寻路运行时契约](../../SPEC_04_Technical.md)
