---
title: MassCombatPathing — SPEC 关闭（方案 B）
status: done
difficulty: 3
demo_scope: authorized
spec_refs:
  - SPEC_03 §3.12 大规模战斗寻路（MassCombatPathing，方案 B）
  - SPEC_03 §3.14 士兵推进 / FlowField
  - SPEC_04 §6 / §9.7 大规模战斗寻路运行时契约
  - SPEC_00 v0.73.0
approach: B
---

## 目标

锁定双方约 200 人规模的寻路规则：**FlowField（共享目标）+ AttackSlot（追击/攻击）+ LocalDetour（友军左右绕）**；产出可执行切片，本会话不编码。

## 范围

- SPEC_03 术语表 + §3.12 规则正文（双语）+ §3.14 推进改写 + 开放问题勾选
- SPEC_04 §6 纪要 + §9.7 技术契约（双语）
- CONTEXT / spec-map / SPEC_00 Changelog v0.73.0
- `.scratch/mass-pathing/` INDEX + MP-01～MP-07 issues

## 不做

- 任何 C# / Prefab / 场景改动
- 替换现有 `NavMeshAgent` 运行时行为

## 验收

- [x] 方案 B 写入 SPEC 并双语同步
- [x] 性能与禁止项（禁全员每帧 CalculatePath、禁友军 Carve）已写明
- [x] issues 可独立 Agent 接手

## 依赖

- 无
