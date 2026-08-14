---
title: WarriorInstance.SoldierSkills 与 WarriorPool JSON 持久化
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.11
  - SPEC_03 §3.8 D-062
  - SPEC_04 §6 WarriorPool
  - SPEC_04 §9.9 SoldierSkills
selected_approach: A — JsonUtility 可序列化 DTO 数组；旧档缺字段视为空列表
---

## 目标

士兵池快照可读写 `SoldierSkills`；进档恢复、回档/删档行为与现有 WarriorPool 一致。本片 **不** 在制造时授予（列表默认为空）。

## 范围

- `WarriorInstance`：`List<SoldierSkillEntry> SoldierSkills`（或等价只读列表 + 可变内部）。条目 `{ SkillId, SkillLevel }`；须 `[Serializable]` 且 **public 字段**（JsonUtility）。
- `WarriorSaveDto`：`SoldierSkillEntry[] SoldierSkills`；缺省 `Array.Empty`。
- `WarriorPoolService.ToDto` / `FromDto` 往返；`FromDto` 遇 null/空 → 空列表。
- 旧存档无该字段：加载成功、列表空，不丢其它快照字段。
- `ManufactureService.RepairMissingStatSnapshots` **不得**清空已有 `SoldierSkills`。
- 彻底死亡已删整实例 → 技能随实例消失；本片不改死亡流程。

## 不做

- 制造授予（SS-03/SS-04）
- 改存档键名 / 迁移旧 JSON schema 以外的字段
- 技能施放 UI

## 验收

- [x] 代码或 Play：写入含 1 条 `{Skill_01, 1}` 的实例 → 再进档仍在
- [x] 旧档（无 SoldierSkills）加载不报错、列表空
- [x] 勾 issue；INDEX SS-02→done；回复变更清单

## 依赖

- SS-01

## 编码前

- 方案 **A** 已锁；可直接编码

## 完成备注

- `SoldierSkillEntry`：`[Serializable]` public `SkillId` / `SkillLevel`；`WarriorInstance.SoldierSkills` 为 `readonly List`（制造授予仍空，待 SS-03/04）
- `WarriorSaveDto.SoldierSkills` 缺省 `Array.Empty`；`FromDto` 对 null/空/`SkillId` 空条目 → 空列表（旧档缺字段安全）
- `RepairMissingStatSnapshots` 只补 StatBlock/配方，注释明确不清空 `SoldierSkills`
- SPEC_00 **v0.82.19**；D-062 进行中（SS-01/02 已落地，待 SS-03～04）
