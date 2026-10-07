---
title: COC 可破坏障碍
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.21
  - SPEC_03 §3.8 D-100
  - SPEC_04 §9.9b
---

在 COC 地图预制体上放可破坏障碍：血量、目标价值、1 格临时空气墙。配了「可破坏障碍物」的职业会把它算进切片 01 的选敌。每次命中扣 1 点血。

依赖：[01-observe-score](01-observe-score.md)。怪物怎么绕墙属于 [03-monster-path](03-monster-path.md)。

选定方案 A：阶段并行候选 + 障碍命中通道。障碍不健化为怪。

## 规则

- 障碍挂在地图模型上，不进 `Defend_MonsterConfig`。组件：血量、目标价值、占地 1 个地板格、临时空气墙。
- 开局空气墙不可走。静态策划 `AirWall` 仍走开局烘焙。可破坏墙用 `NavMeshObstacle` 雕刻；血量归零时关掉雕刻，不整张重烘。
- 血量归零：空气墙立刻消失，模型沿 Y 下沉（约 0.4 秒、下沉 1）。
- 职业 `TargetTypeScores` 里有 `可破坏障碍物` 才成为候选，并使用组件上的目标价值。没写这个类型的职业不打墙。
- 士兵沿用该职业攻速和普攻动作。近战和远程每次命中都只扣 1，不吃 `NormalAttackPower`。多个士兵可打同一堵墙。
- 样例：只给 `Class_BaseWarrior` 加上 `|可破坏障碍物;1`，并在 `Coc_Lv2_01` 放 1 个障碍。
- 推图、防守、搜打撤不生成、不攻击这种墙。

## Acceptance

- [x] Excel 已改（第 1～2 行文档头保留）；见 SPEC_04 §14.7
- [x] CSV 由 Excel Bake（Mode2 战士类型串）
- [x] 没有只改 CSV、不同步 Excel
- [ ] 枯骨战士会打观察范围内的墙；未配置该类型的射手不打
- [ ] 每次命中墙血 −1，不按普通攻击力扣
- [ ] 墙血打光后空气墙消失、模型下沉，士兵可以走过去
