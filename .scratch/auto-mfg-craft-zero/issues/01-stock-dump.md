---
title: AM-CRAFT-01 RunBatch 仓库槽位 dump
status: done
difficulty: 1
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 Demo 诊断日志
  - SPEC_04 §6
  - SPEC_00 v0.84.32
approach: C
---

## 目标

进 AutoManufacture 时 Console 能看出：停造原因、锚点主要手等级、仓库按槽/BodyLevel 各有多少。

## 范围

- `AutoManufactureService.RunBatch` 结束打 `Stock dump`
- 选到主要手时打 `BodyPartId` / `BodyLevel` / `ClassRestrict`
- 最低配方不足原因补 ArmSecondary 计数

## 不做

- 改选料 / 次要手回退 / Dig 飞入账

## 验收

- [ ] Play Mode：Dig_01 + Dig_02 后再进 AM，Console 有 `[AutoManufacture] Stock dump` 与 `Pick primary`
- [ ] 0 兵时 `stop=` 可读（最低配方 / 无次要手近似品质 / 无 Head 近似品质 等）
