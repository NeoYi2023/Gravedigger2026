# 主角装备引雷 Equip_Elctr（D-078）

**选定：** 难度 **2**；方案 **A**（`DigLightningEffectConfig` + `DigLightningScheduler`；`ClearGraveByLightning`；`GmSoldierGrantService` 入池；序列帧 + 2s 待机 View）。

**工作量：** 须拆步。本轮一次落地 EL-00～EL-02。

| 片 | 文件 | 目标 | 状态 |
|----|------|------|------|
| EL-00 | [issues/00-spec-table.md](issues/00-spec-table.md) | SPEC + EquipEffect CSV + Token | **done** |
| EL-01 | [issues/01-lightning-scheduler.md](issues/01-lightning-scheduler.md) | 定时落雷 + 清坟旁路 + 入兵 | **done** |
| EL-02 | [issues/02-vfx-preview-gm.md](issues/02-vfx-preview-gm.md) | 序列帧 / 待机预览 / Dig HUD GM | **done** |

分步粘贴指令：[new-agent-prompts.md](new-agent-prompts.md)
