# 士兵技能 SoldierSkills — NewAgent 可粘贴分步指令

**用途：** 每个 New Agent **只粘贴一个分片**整块正文（含下方 \`\`\` 内全文）。  
**执行序：** SS-01 → SS-02 → SS-03 → SS-04（**SS-00 已完成，勿再开**）。  
**权威：** `.scratch/soldier-skills/INDEX.md` + 对应 `issues/`；SPEC_00 **v0.82.16**；规则 [SPEC_03 §3.11](../../SPEC_03_GameRules.md) / [§3.15](../../SPEC_03_GameRules.md) / [SPEC_04 §9.9](../../SPEC_04_Technical.md) / [§9.9b](../../SPEC_04_Technical.md) / [§9.21](../../SPEC_04_Technical.md) / [§9.24](../../SPEC_04_Technical.md)。  
**选定方案：** **A**（扩展现有 `WarriorInstance` JSON + `ManufactureService` / `AutoManufactureService` 授予 + 魔法书钩子二次扫描）。

**总约束：**

1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；设计变更先改 SPEC/Changelog 再编码。
2. 本会话 **只实现本片**；禁止顺手做下一片。
3. **不**实现技能施放 / SkillEffect 正文 / Mode1 选技能 UI / 经验升级。
4. **不**实现 `ForceClass`（只保证授予发生在第一次钩子返回之后）。
5. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。

---

## 分片 SS-01 — 配置表 + 加载（先做）

**标题：** 士兵技能 · SS-01 · SkillConfig 加载与 ClassConfig.DefaultSkillIds

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SS-01（方案 A / 士兵技能）。

【授权与边界】
- Demo 已授权本片；本会话只做 SS-01，禁止 SS-02～SS-04。
- 不写 WarriorInstance 持久化、制造授予、魔法书 Token、技能施放。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/soldier-skills/INDEX.md
3. .scratch/soldier-skills/issues/01-config-table-loader.md
4. SPEC_03 §3.11（DefaultSkillIds / SoldierSkills 授予规则）
5. SPEC_04 §9.9b DefaultSkillIds 编码；§9.21 SkillConfig 列（含 IconAssetId）
6. SPEC_04 §14 Bake / CSV 路径
7. 参考：ProtagonistEquipmentConfigRow 复合主键加载；ConfigCsvRepository.LoadClasses / LoadProtagonistEquipment

【目标】
1) SPEC_03 §3.8 增 D-062（P1：士兵技能垂直 = 表加载 + 池持久化 + Mode1 授予 + Mode2 授予/SoldierSkillLevelAdd；状态待实现），中英同步；SPEC_00 Changelog bump。
2) Mode1+Mode2：Combat_SkillConfig 加 IconAssetId（Description 与 SkillEffectId 之间；既有行可空）。给 Skill_01 补 Level 2 行。
3) Mode1+Mode2：Manufacture_ClassConfig 加 DefaultSkillIds。仅 Class_Warrior（Mode2 另含 Class_Warrior_0）= Skill_01；其余空。勿给全职业填技能。
4) SkillConfigRow + ConfigCsvRepository.LoadSkills；TryGetSkill(skillId, level)；提供该 SkillId 的 min/max SkillLevel 查询。复合主键重复则抛。
5) ClassConfigRow 解析 DefaultSkillIds（空=无；管道分隔；重复保留首次）。加载时未知 SkillId 不抛。
6) Excel 改完 Bake CSV。

【范围】
- 改：SPEC_03 §3.8、SPEC_00 Changelog
- 改：Skill/Class Excel+CSV（Mode1+Mode2）
- 新增：SkillConfigRow；改 ConfigCsvRepository / ClassConfigRow

【不做】
- WarriorInstance / WarriorPool JSON
- ManufactureService / AutoManufacture / MagicBook handler
- SkillEffectConfig 加载、施放、选技能 UI

【流程门禁】
- 整包难度 2、方案 A 已在 INDEX 锁定；本片可直接编码。

【验收】
- Bake 无报错；运行时可查 Skill_01 Lv1 与 Lv2
- Class_Warrior.DefaultSkillIds 含 Skill_01；空列职业长度为 0
- D-062 已写入 §3.8；Changelog 已记
- 回复变更文件清单；勾 issue；INDEX SS-01→done
```

---

## 分片 SS-02 — 实例字段 + 池持久化

**标题：** 士兵技能 · SS-02 · WarriorInstance.SoldierSkills 与存档往返

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SS-02（方案 A / 士兵技能）。

【授权与边界】
- 本会话只做 SS-02；禁止 SS-03/SS-04；禁止改 SS-01 表结构（除非加载 bug）。
- 本片不在制造时授予（列表默认为空）。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/soldier-skills/issues/02-warrior-instance-persist.md
3. SPEC_04 §6 WarriorPool；§9.9 SoldierSkills
4. 参考：WarriorInstance / WarriorSaveDto / WarriorPoolService.ToDto·FromDto

【目标】
- 可序列化 SoldierSkillEntry { SkillId, SkillLevel }（JsonUtility：public 字段）
- WarriorInstance.SoldierSkills 列表；WarriorSaveDto 对应数组
- ToDto/FromDto 往返；null/缺字段 → 空列表；旧档不丢其它快照
- RepairMissingStatSnapshots 不得清空 SoldierSkills
- PermanentDeath 已删整实例，不必另写技能回收

【不做】
- ManufactureService / AutoManufacture 授予
- 改存档键名
- 技能施放

【流程门禁】
- 方案 A 已锁；可直接编码。

【验收】
- 写入 1 条 Skill_01@1 后再进档仍在
- 旧档无该字段时加载成功、列表空
- 勾 issue；INDEX SS-02→done；回复变更清单
```

---

## 分片 SS-03 — Mode1 制造授予

**标题：** 士兵技能 · SS-03 · Mode1 Manufacture 授予 DefaultSkillIds@Lv1

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SS-03（方案 A / 士兵技能）。

【授权与边界】
- 本会话只做 SS-03；禁止 SS-04（Mode2 / 魔法书二次扫描）。
- Mode1 不读魔法书、不跑 SoldierSkillLevelAdd。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/soldier-skills/issues/03-mode1-manufacture-grant.md
3. SPEC_03 §3.11 士兵技能授予（时机 = ClassId 最终定稿之后；无 (SkillId,1) → 跳过+Warning；重复保留首次）
4. 参考：ManufactureService.BuildWarriorFromAggregate / TryManufacture / TryRemanufacture

【目标】
抽共享 SoldierSkillGrant（或等价静态 helper，供 SS-04 复用）：
- 按最终 ClassId 读 DefaultSkillIds，各 Id 写入 { SkillId, SkillLevel=1 }
- 无 (SkillId,1) 行 → 跳过 + Warning；空列 → 空列表
接入 ManufactureService.BuildWarriorFromAggregate（制造与再造共用）。
制造成功日志附带 SoldierSkills 摘要。
推荐：DefendSessionService / PushMapStageController 的 skillBonusSum 改为按 SoldierSkills 查 SkillConfig.LossOfControlChanceBonus 之和（灵魂/宝石 Skills 并行仍 TBD，本片不加）。

【不做】
- AutoManufacture / MagicBook Token
- Mode1 选技能 UI、经验升级、技能施放

【流程门禁】
- 方案 A 已锁；可直接编码。

【验收】
- Mode1 战士灵魂造兵 → Skill_01@1；进档仍在
- 无灵魂 Class_Servants 且空列 → 空列表
- 再造走同一授予路径
- 勾 issue；INDEX SS-03→done；回复变更清单
```

---

## 分片 SS-04 — Mode2 授予 + 魔法书二次扫描

**标题：** 士兵技能 · SS-04 · Mode2 DefaultSkillIds 与 SoldierSkillLevelAdd

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SS-04（方案 A / 士兵技能 / D-062）。

【授权与边界】
- 本会话只做 SS-04；禁止改 SS-01 表结构（除非缺样例书/缺 Skill_01 Lv2）。
- 不实现 ForceClass / StatAdd / QualityDelta；不实现技能施放；Mode1 仍不读魔法书升技能。
- 第一次钩子 ApplySoldierManufactureEffects 内不得升技能。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/soldier-skills/issues/04-mode2-grant-and-magicbook.md
3. SPEC_03 §3.15 步骤 4b/4c；SPEC_04 §9.24 SoldierSkillLevelAdd
4. 参考：AutoManufactureService 造兵（ApplySoldierManufactureEffects → FinalizeDraft → BuildWarriorInstance）
5. 参考：SoldierManufactureMagicBookHook + MagicBookEffectParams；SS-03 SoldierSkillGrant
6. 手验：Tools「增加魔法书」（D-061）若已落地则复用；否则 Dig HUD GM 对齐战士强化

【目标】
1) AutoCraftDraft 带 SoldierSkills；钩子返回后按最终 ClassId 授予 DefaultSkillIds@Lv1；再二次扫描；再定稿/入池。
2) 第一次扫描遇到 SoldierSkillLevelAdd：跳过（deferred），勿当未知 payload 刷屏。
3) 二次扫描槽左→右：必填 SkillId、Delta；已有该技能则 Level+=Delta，钳制到 SkillConfig 该 Id min/max；无则跳过不新授；缺/非法 Key → 该书无效。
4) 样例书 MagicBook_SoldierSkillLevel：EffectPayload=SoldierSkillLevelAdd；EffectParams=SkillId=Skill_01|Delta=1。Mode1+Mode2 表都加。SPEC_04 §9.24 已定义行可补 + Changelog。
5) 手验入口：装备该书后 AutoManufacture 战士 → Skill_01@2。
6) D-062 标完成（中英）+ Changelog。

【不做】
- ForceClass 实现
- 魔法书新授予技能、经验升级、施放、正式装配 UI

【流程门禁】
- 方案 A 已锁；可直接编码。

【验收】
- 无书：战士 Skill_01@1；其它职业空
- 有书：战士 Skill_01@2（不超过表内最大级）
- 有书但职业无该技能：不新授
- 进档仍在；D-062 可勾；INDEX SS-04→done；回复变更清单
```
