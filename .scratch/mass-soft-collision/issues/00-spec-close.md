# SC-00 — SPEC 关闭（方案 B+ 规则录入）

**状态：** done  
**难度：** 2  
**依赖：** 方案 B（MP-00～MP-07）已锁

## 目标

将 Be My Horde 可观测的「战斗移动模式 + 软碰撞」整理为 Gravedigger2026 **方案 B+**，并写入权威 SPEC。

## 已完成

- [x] SPEC_03 §3.12：CombatMoveMode（Chase/Surround/Sweep）+ SoftCollision + SurroundGap；**明确无 Follow**
- [x] SPEC_04 §6 / §9.7：B+ 运行时契约、伪代码 API、Demo 常量、验收
- [x] CONTEXT 术语：CombatMoveMode / SoftCollision / SurroundGap
- [x] SPEC_00 Changelog v0.74.0
- [x] 本 scratch 索引与后续切片草案

## 不做

- 编码
- Follow / ArmyRadius 粘随
- Sweep 接线（枚举可留，P2）

## 验收

- 读 SPEC 即可单独开 SC-01/SC-02 Agent，无需再翻 BMH 逆向笔记
