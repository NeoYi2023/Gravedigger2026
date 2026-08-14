# ToolsPanel GM 发放主角装备 / 魔法书

**状态：** SPEC 已关（v0.82.17 / UI-019 / D-061）；**TP-00～TP-02 完成**。  
**选定：** 难度 **2**；方案 **A**（通用 `GmGrantListPanel`，对齐 UI-008 LevelSelectPanel）。  
**魔法书发放：** `SpecialEquipSlotsService.TryEquip`（无独立仓库）。  
**工作量：** 须拆步。

| 片 | 文件 | 目标 | 状态 |
|----|------|------|------|
| TP-00 | [issues/00-spec-close.md](issues/00-spec-close.md) | SPEC §3.5 / UI-019 / D-061 | **done** |
| TP-01 | [issues/01-equip-grant.md](issues/01-equip-grant.md) | GmGrantListPanel + 「增加主角装备」 | **done** |
| TP-02 | [issues/02-magicbook-grant.md](issues/02-magicbook-grant.md) | 「增加魔法书」TryEquip | **done** |

分步粘贴指令：[new-agent-prompts.md](new-agent-prompts.md)

**总约束：**

1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；设计变更先改 SPEC/Changelog 再编码。
2. 每会话 **只实现一个分片**；禁止顺手做下一片。
3. 魔法书 **不**新建仓库；点一次 = `TryEquip`（唯一已装 / 槽满失败）。
4. Dig HUD 现有 GM **保留**。
5. **不**做正式装备仓 UI / 魔法书装配 UI / 制造·战斗 Token handler。
6. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。
