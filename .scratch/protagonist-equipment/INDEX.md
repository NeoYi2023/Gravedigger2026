# 主角装备 ProtagonistEquipment

**状态：** SPEC 规则已关（v0.82.5 / §3.16 / §9.25）；**D-059 垂直完成（v0.82.9 / PE-01～PE-04）**；**D-060 矿灯完成（v0.82.14 / PE-05～PE-08）**。

**选定：** 难度 **2**；方案 **A**（纯 C# `ProtagonistEquipmentService` + PlayerPrefs；Dig caps = 科技加法 + 装备 Dig 加法；矿灯 = `GraveSpawnWeightBonus` 活叠加；最小 GM 手验，正式 UI / 制造·战斗 Token 后置）。

**工作量：** 须拆步。

| 片 | 文件 | 目标 | 状态 |
|----|------|------|------|
| PE-00 | [issues/00-spec-close.md](issues/00-spec-close.md) | SPEC §3.16 / §9.25 规则录入 | **done**（勿再开） |
| PE-01 | [issues/01-config-table-loader.md](issues/01-config-table-loader.md) | 表文件 + 加载 + 样例 Dig 装备行 | **done** |
| PE-02 | [issues/02-equipment-service-persist.md](issues/02-equipment-service-persist.md) | 仓 / 转化 / 升级 / EquipCommonExp / 存档 | **done** |
| PE-03 | [issues/03-dig-caps-merge.md](issues/03-dig-caps-merge.md) | DigProtagonistCapabilities 科技+装备叠加 | **done** |
| PE-04 | [issues/04-dig-gm-handcheck.md](issues/04-dig-gm-handcheck.md) | Dig HUD GM 发放/公共经验手验 | **done** |
| PE-05 | [issues/05-miner-lamp-spec.md](issues/05-miner-lamp-spec.md) | 矿灯 SPEC / D-060 | **done** |
| PE-06 | [issues/06-miner-lamp-table.md](issues/06-miner-lamp-table.md) | Equip_MinerLamp L1～5 表行 | **done** |
| PE-07 | [issues/07-miner-lamp-spawn-overlay.md](issues/07-miner-lamp-spawn-overlay.md) | 生成权重活叠加 | **done** |
| PE-08 | [issues/08-miner-lamp-gm.md](issues/08-miner-lamp-gm.md) | Dig HUD GM 矿灯手验 | **done** |

分步粘贴指令：[new-agent-prompts.md](new-agent-prompts.md)

**总约束：**

1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；设计变更先改 SPEC/Changelog 再编码。
2. 每会话 **只实现一个分片**；禁止顺手做下一片。
3. **不**改 MagicBook / SpecialEquipSlots / 材料 Warehouse / ExtraEquipment。
4. **不**实现 `SoldierManufacture` / `Combat` Token handler（域枚举可解析，效果空 apply）。
5. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。
