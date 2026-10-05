---
title: 进图刷怪并停在刷怪点
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.21
  - SPEC_03 §3.8 D-099
  - SPEC_04 §9.35
  - SPEC_04 §9.36
---

从沙盘进入 COC 后，按已有 `CocSpawnConfig` 在地图刷怪点刷出怪物。开局停在刷怪点，不追击。本片不洒兵、不走向最终 BOSS、不结算。返回沙盘仍不扣次、不删士兵库。

依赖：02 的地图与进入。父片 [03-deploy-and-boss](03-deploy-and-boss.md)。编码前再确认难度与方案。

## 规则

- 每个玩法恰好一行 `FinalBoss`，数量为 1。样例表已在切片 02 写好，本片只消费，不改表。
- 开战刷出：`Normal`、`MiniBoss`、`FinalBoss` 都在对应 `SpawnPoint`。缺标记的行跳过并 Warning。
- 本片场上没有我方士兵，怪物不追击。
- 不布阵、不放主角、不护盾、不失控、不发经验。
- 返回沙盘不算单局失败。

## Acceptance

- [x] 进入后能看见普通怪、小 BOSS、最终 BOSS 停在各自刷怪点
- [x] 怪物不主动追击
- [x] 返回沙盘不扣次，士兵库不变
