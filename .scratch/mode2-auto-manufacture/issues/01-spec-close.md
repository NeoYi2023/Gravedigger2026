---
title: Mode2 AutoManufacture — SPEC 规则录入收尾确认
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.15 AutoManufacture
  - SPEC_03 §3.8 D-050～D-053
  - SPEC_03 §3.9 / §3.11 Mode2 差分
  - SPEC_04 §9.9b / §9.12 / §9.24 / §6 / §13
  - CONTEXT AutoManufacture terms
  - SPEC_00 v0.77.0
---

## 目标

关闭 Mode2 自动制造规则库（非 Unity 编码）。

## 范围

- SPEC_03 §3.15 双语全规则 + 术语 + §3.9 流水线 + Mode2 UM 差分 + D-050～D-053
- SPEC_04 ClassConfig / BodyPart 扩列、MagicBookConfig、FormationClassZone、费用屏蔽
- CONTEXT / Changelog / spec-map 同步
- 本目录 AM-02～AM-08 分步指令

## 不做

- Unity C# / Prefab / Excel 实文件改写（表数据行另片）
- 具体魔法书效果实现

## 验收

- [x] §3.15 / §9.9b / §9.12 / §9.24 已写入
- [x] D-050～D-053 验收占位已立
- [x] issues AM-02～AM-08 已发布

## 依赖

- 无

## 编码前

- 本片无编码；后续片须负责人授权 Demo
