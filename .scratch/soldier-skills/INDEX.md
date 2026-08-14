# 士兵技能 SoldierSkills

**状态：** SPEC 规则已关（v0.82.16 / §3.11 / §3.15 / SPEC_04 §9.9 / §9.9b / §9.21 / §9.24）；**SS-01～SS-04 完成**（v0.82.21 / D-062 **完成**）。

**选定：** 难度 **2**；方案 **A**（扩展现有桥：`WarriorInstance` JSON 加 `SoldierSkills`；Mode1 `ManufactureService` / Mode2 `AutoManufactureService` 授予；`SoldierManufactureMagicBookHook` **二次扫描** `SoldierSkillLevelAdd`）。

**工作量：** 须拆步。Demo 验收项 **D-062** 已由 SS-01～04 落地（SPEC_03 §3.8）。

| 片 | 文件 | 目标 | 状态 |
|----|------|------|------|
| SS-00 | [issues/00-spec-close.md](issues/00-spec-close.md) | SPEC 士兵技能规则录入 | **done**（勿再开） |
| SS-01 | [issues/01-config-table-loader.md](issues/01-config-table-loader.md) | CSV/Excel 列 + SkillConfig 加载 + DefaultSkillIds | **done** |
| SS-02 | [issues/02-warrior-instance-persist.md](issues/02-warrior-instance-persist.md) | `WarriorInstance.SoldierSkills` + WarriorPool JSON | **done** |
| SS-03 | [issues/03-mode1-manufacture-grant.md](issues/03-mode1-manufacture-grant.md) | Mode1 制造/再造授予 DefaultSkillIds@Lv1 | **done** |
| SS-04 | [issues/04-mode2-grant-and-magicbook.md](issues/04-mode2-grant-and-magicbook.md) | Mode2 授予 + `SoldierSkillLevelAdd` 二次扫描 | **done** |

分步粘贴指令：[new-agent-prompts.md](new-agent-prompts.md)

**总约束：**

1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；设计变更先改 SPEC/Changelog 再编码。
2. 每会话 **只实现一个分片**；禁止顺手做下一片。
3. **不**实现技能施放 / `SkillEffect` 正文驱动 / Mode1 选技能 UI / 经验升级 / 魔法书运行时改等级。
4. **不**实现 `ForceClass`（第一次钩子已有空 apply；本垂直只保证授予发生在第一次钩子返回之后）。
5. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。
