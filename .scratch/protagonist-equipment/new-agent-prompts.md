# 主角装备 ProtagonistEquipment — NewAgent 可粘贴分步指令

**用途：** 每个 New Agent **只粘贴一个分片**整块正文（含下方 \`\`\` 内全文）。  
**执行序：** PE-01 → PE-02 → PE-03 → PE-04（**PE-00 已完成，勿再开**）。  
**权威：** `.scratch/protagonist-equipment/INDEX.md` + 对应 `issues/`；SPEC_00 **v0.82.5**；规则 [SPEC_03 §3.16](../../SPEC_03_GameRules.md) / [SPEC_04 §9.25](../../SPEC_04_Technical.md)。  
**选定方案：** **A**（纯 C# Service + PlayerPrefs；Dig caps = 科技 + 装备 Dig 按键加法；最小 Dig GM 手验）。

**总约束：**

1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；设计变更先改 SPEC/Changelog 再编码。
2. 本会话 **只实现本片**；禁止顺手做下一片。
3. **不**改 MagicBook / SpecialEquipSlots / 材料 Warehouse / ExtraEquipment。
4. **不**实现 SoldierManufacture / Combat Token handler。
5. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。

---

## 分片 PE-01 — 配置表 + 加载（先做）

**标题：** 主角装备 · PE-01 · ProtagonistEquipmentConfig 表与 ConfigCsvRepository

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 PE-01（方案 A / 主角装备）。

【授权与边界】
- Demo 已授权本片；本会话只做 PE-01，禁止 PE-02～PE-04。
- 不改 MagicBook / Warehouse / ExtraEquipment。
- 不写 Service / 持久化 / Dig caps / GM。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/protagonist-equipment/INDEX.md
3. .scratch/protagonist-equipment/issues/01-config-table-loader.md
4. SPEC_03 §3.16
5. SPEC_04 §9.25（磁盘名、字段、EffectDomain/EquipEffect 编码）
6. SPEC_04 §14 Bake / CSV 路径
7. 参考：MagicBookConfigRow + ConfigCsvRepository.LoadMagicBooks

【目标】
1) SPEC_03 §3.8 增 D-059（主角装备 Dig 垂直：表加载+仓+caps+GM；状态待实现），SPEC_00 Changelog bump。
2) Mode1+Mode2 新建 Excel/CSV：主角_装备配置表_Protagonist_ProtagonistEquipmentConfig.xlsx / Protagonist_ProtagonistEquipmentConfig.csv
3) ProtagonistEquipmentConfigRow + ConfigCsvRepository 加载；复合主键 EquipId+EquipLevel。
4) 样例至少：Equip_DigRing Level 1～3；EffectDomain=Dig；EquipEffect 含 DigCursorRadius 递增值；填 ExpToNextLevel / ConvertExpValue；满级行 ExpToNextLevel 空或 ≤0。

【范围】
- 改：SPEC_03 §3.8、SPEC_00 Changelog
- 新增：Config 行类型 + 表文件（Mode1+Mode2）
- 改：ConfigCsvRepository 加载/查询 API

【不做】
- ProtagonistEquipmentService、MetaShell 接线、Dig caps、GM、正式 UI

【流程门禁】
- 整包难度 2、方案 A 已在 INDEX 锁定；本片可直接编码。

【验收】
- Bake 无报错；运行时可查到 Equip_DigRing 各级行
- D-059 已写入 §3.8；Changelog 已记
- 回复变更文件清单；勾 issue；INDEX PE-01→done
```

---

## 分片 PE-02 — 装备仓 Service + 持久化

**标题：** 主角装备 · PE-02 · ProtagonistEquipmentService 与存档键

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 PE-02（方案 A / 主角装备）。

【授权与边界】
- 本会话只做 PE-02；禁止 PE-03/PE-04；禁止改 PE-01 表结构（除非加载 bug）。
- 不改材料 Warehouse / MagicBook。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/protagonist-equipment/issues/02-equipment-service-persist.md
3. SPEC_03 §3.16（Acquire / TryLevelUp / SpendCommonExp / 满级转公共池）
4. SPEC_04 §6 键：EquipCommonExp、ProtagonistEquipmentWarehouse
5. 参考：SpecialEquipSlotsService（Bind/Clear/Delete）+ MetaShellController 进档/回档/删档
6. PE-01 加载 API / Equip_DigRing 样例 Id

【目标】
纯 C# ProtagonistEquipmentService：
- BindSlot(slot, campaignMode) / ClearBound / DeleteSlotData（两模式）
- TryAcquire(EquipId)：首获 Lv1；同 Id 加 ConvertExpValue 并连升；满级同 Id → EquipCommonExp
- TrySpendCommonExp(EquipId, amount)：扣公共池、加 CurrentExp、连升
- 只读 OwnedEquips / EquipCommonExp；变更写回 PlayerPrefs
- MetaShell 进档 Bind、回档 Clear、删档清键

【不做】
- DigProtagonistCapabilities 合并（PE-03）
- Dig GM（PE-04）
- 正式装备 UI

【流程门禁】
- 方案 A 已锁；可直接编码。

【验收】
- 单元路径或 Play 日志可验证：首获 / 同 Id 转化连升 / 满级进公共池 / SpendCommonExp
- 进档恢复、回档 Clear、删档双模式键清除
- 勾 issue；INDEX PE-02→done；回复变更清单
```

---

## 分片 PE-03 — Dig 能力科技+装备叠加

**标题：** 主角装备 · PE-03 · DigProtagonistCapabilities 合并重算

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 PE-03（方案 A / 主角装备）。

【授权与边界】
- 本会话只做 PE-03；禁止 PE-04；禁止正式 UI。
- 不改 MagicBook 钩子。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/protagonist-equipment/issues/03-dig-caps-merge.md
3. SPEC_03 §3.16 效果与重算；SPEC_04 §9.6 / §9.17 AttributeModifiers 键
4. TechTreeService 现有重算路径；DigStageModule 如何取 caps
5. ProtagonistEquipmentService（PE-02）OwnedEquips 当前等级行

【目标】
DigProtagonistCapabilities 重算 = Σ 科技 AttributeModifiers + Σ 仓内装备当前行且 EffectDomain 含 Dig 的 EquipEffect（按键加法）。
获得/升级/SpendCommonExp/进档 Bind 后重算；Dig 阶段读合并后 caps（如 DigCursorRadius）。
Manufacture/Combat 域本片空 apply。

【范围】
- 改：TechTreeService 和/或抽出 DigCapsRecalc；DigStageModule / Meta 接线
- 装备变更 → 触发重算（事件或直接调用）

【不做】
- Dig GM 按钮（PE-04）
- SoldierManufacture / Combat Token 登记表

【流程门禁】
- 方案 A 已锁；可直接编码。

【验收】
- 无装备时 caps 与改前科技结果一致
- 拥有/升级 Equip_DigRing 后 DigCursorRadius = 科技和 + 装备和
- Dig 阶段光标半径可见变化（或日志确认 Session caps）
- 勾 issue；INDEX PE-03→done；回复变更清单
```

---

## 分片 PE-04 — Dig HUD GM 手验

**标题：** 主角装备 · PE-04 · Dig GM 发放装备与公共经验

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 PE-04（方案 A / 主角装备）。

【授权与边界】
- 本会话只做 PE-04；禁止正式装备仓库 UI / 商店 / Dig 掉落入账。
- 可对齐现有 Dig HUD GM（增加坟墓 / 增加躯体材料 / Mode2 装备战士强化）。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/protagonist-equipment/issues/04-dig-gm-handcheck.md
3. SPEC_03 §3.8 D-059；§3.16
4. DigHudView / DigStageController / DigAssetBuilder 现有 GM 接线
5. ProtagonistEquipmentService API（PE-02）

【目标】
Dig HUD 增加最小 GM：
- 「获得挖坟圈装备」→ TryAcquire("Equip_DigRing")（或 PE-01 实际样例 Id）
- 「装备公共经验+N」→ 注入 EquipCommonExp（或再提供「划入当前装备升级」调用 TrySpendCommonExp）
- 日志打印 Level / CurrentExp / 合并后 DigCursorRadius（便于手验）

【不做】
- 正式 UI、掉落表、制造/战斗效果

【流程门禁】
- 难度 1；可直接编码。

【验收】
- Play Mode Dig：GM 获得 → 光标/caps 变；再获同 Id → 升级；公共经验可用
- D-059 手验可勾；issue/INDEX PE-04→done；回复变更清单
```
