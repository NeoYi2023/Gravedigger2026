---
title: SE-06 方向/前置/间隔/波次刷怪
status: done
difficulty: 3
demo_scope: out-of-scope
spec_refs:
  - SPEC_03 §3.19 刷怪
  - SPEC_04 §9.33 SearchExtractWaveSpawnConfig
  - SPEC_04 §9.19 MonsterConfig
  - SPEC_04 §6 PushMap 刷怪散布（PM-10 复用）
approach: A
depends_on:
  - SE-05
  - SE-01
---

## 目标

按 §9.33（v0.84.00）：点激活后各行独立 `FirstWaveDelaySeconds` 首刷，再按 `WaveIntervalSeconds`×`RepeatSpawnCount` 行内重复；方向解析 `SpawnPointId`。

## 范围

- Session 波次调度 + `SearchExtractSpawnRequested` 事件
- View：Instantiate 怪（复用 `PushMapMonsterAgentView` 或共享 Monster 视图）；BodyRadius 散开
- 单点胜利前持续资格；胜利后该点停刷
- Aggro/AI：Demo 可先用 PushMap 默认追击

## 不做

- UI-032 / 无敌清场（SE-07）
- Defend WaveSpawnConfig

## 验收

- [x] 配置样例：至少 1 点 2 波、不同方向或 SpawnPoint
- [x] 前置秒与间隔可观察
- [x] 点未激活不刷

## 依赖

- SE-05；SE-01
