# InSaveShell 装备 / 魔法书弹窗

**状态：** EM-00 **done**；EM-01 **done**；EM-02 **done**；EM-03 **done**。  
**选定：** 难度 **2**；方案 **A**（抽取共享 `BookRow` Prefab；弹窗与 AM 演出嵌套同一份；`SpecialEquipSlotsService.TrySwap` + `Changed` 双端刷新）。  
**负责人确认：** 装备弹窗 **只读**；魔法书 **任意两槽互换**（含拖到空槽=搬书）；工作量 **须拆步**。

| 片 | 文件 | 目标 | 状态 |
|----|------|------|------|
| EM-00 | [issues/00-spec-close.md](issues/00-spec-close.md) | SPEC UI-022/023 + D-067/068 | **done** |
| EM-01 | [issues/01-shell-buttons-panels.md](issues/01-shell-buttons-panels.md) | 入口按钮 + 两弹窗壳 | **done** |
| EM-02 | [issues/02-equipment-warehouse-readonly.md](issues/02-equipment-warehouse-readonly.md) | 装备仓只读列表（D-067） | **done** |
| EM-03 | [issues/03-magicbook-reorder.md](issues/03-magicbook-reorder.md) | 共享 BookRow + 拖拽排序（D-068） | **done** |

分步粘贴指令：[new-agent-prompts.md](new-agent-prompts.md)

**总约束：**

1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；设计变更先改 SPEC/Changelog 再编码。
2. 每会话 **只实现一个分片**；禁止顺手做下一片。
3. 魔法书 **无**独立仓库；装入仍走 Tools GM `TryEquip`（UI-019 / D-061）；本片 **不做**卸下。
4. Tools GM（增加主角装备 / 增加魔法书）与 Dig HUD GM **保留**。
5. 装备弹窗 **只读**：不升级、不划入公共经验、不卸下。
6. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。
