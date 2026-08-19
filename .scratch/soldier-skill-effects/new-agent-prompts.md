# 士兵技能效果 Skill_04～12 — NewAgent 可粘贴分步指令

**用途：** 每个 New Agent **只粘贴一个分片**整块正文（含下方 ``` 内全文）。

**执行序：** SE-00 → SE-01 → … → SE-09（SE-05/SE-08 可与 SE-03/04 并行度低，但仍建议按序以便 Status/弹道依赖就绪）。

**权威：** [.scratch/soldier-skill-effects/INDEX.md](INDEX.md) + 对应 `issues/`。

**选定方案：** **B+** — `SkillEffectKind` 登记制 + `EffectParams` 表驱动 + `CombatStatusService` + `ISkillEffectHandler` 注册表；`PushMapSessionService` **禁止** `if (skillId == "Skill_XX")`。

**总约束：**

1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；设计变更先改 SPEC/Changelog 再编码。
2. 本会话 **只实现本片**；禁止顺手做下一片。
3. PushMap only；Defend 不接线；Mode1 不改。
4. 新技能扩展路径：**登记表加 Token → 表行填 Kind/Params/Hook → 新 Handler 类 Register**；不改 Session 分支。
5. 本片完成后：该 `SkillId` Lv1～5 Mode2 Excel `EffectImplemented=1` → **Bake Mode2 Tables**。
6. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。

---

## 分片 SE-00 — SPEC 关闭（先做）

**标题：** SkillEffects · SE-00 · SPEC D-073 + EffectKind 框架

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SE-00（方案 B+ / Skill_04～12 效果框架 SPEC）。

【授权与边界】
- 本会话只做 SE-00 SPEC，禁止 SE-01～09 任何编码或改 Excel。
- CONTEXT 仅补术语（EffectKind / CombatStatusService），不写规则正文。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/soldier-skill-effects/INDEX.md
3. .scratch/soldier-skill-effects/issues/00-spec-close.md
4. SPEC_03 §3.12 SkillCast / §3.8
5. SPEC_04 §9.21 / §9.21b / §9.22
6. SPEC_04 §9.24 MagicBook EffectPayload/EffectParams（对齐登记制模式）
7. Gravedigger2026/Assets/ConfigTables/Mode2/Csv/Combat_SkillConfig.csv（Skill_04～12 行）
8. SPEC_00 Changelog（现行版本 v0.82.80+）

【目标】
1) SPEC_03 §3.12：SkillEffectKind 登记制 + TriggerHook + CombatStatusService + Pipeline 原则（禁止 SkillId 硬分支）；9 技能规则摘要；D-069 三技能保留 SoldierSkillCast 的迁移意图。
2) SPEC_04 §9.21b：扩列 EffectKind / EffectParams / TriggerHook；登记表 + SkillEffect_04～12 FK 样例。
3) SPEC_03 §3.8：新增 D-073（P1；D-072 已被魔法书删除占用）。
4) SPEC_04 §6 / §9.22：脚本路径意图。
5) SPEC_00 Changelog；spec-map.md；CONTEXT 术语。

【范围】
- 改：SPEC_03、SPEC_04 §9.21b/§6/§9.22、SPEC_00、spec-map.md、（可选）CONTEXT

【不做】
- C# / Prefab / Excel / CSV

【验收】
- 中英双块同步
- D-073 + 登记表 + 9 技能摘要已写
- 勾 issue；INDEX SE-00→done
```

---

## 分片 SE-01 — Skill_04 + 管线 bootstrap

**标题：** SkillEffects · SE-01 · Skill_04 先发制人 + Pipeline

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SE-01（方案 B+ / D-073 首条 + 框架 bootstrap）。

【授权与边界】
- Demo 已授权；本会话只做 SE-01，禁止 SE-02～09。
- SE-00 SPEC 须 done。
- PushMapSessionService 禁止新增 SkillId 分支；只调 SkillEffectPipeline.Dispatch。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/soldier-skill-effects/INDEX.md
3. .scratch/soldier-skill-effects/issues/01-skill04-first-strike.md
4. SE-00 定稿的 SPEC_03 §3.12 / SPEC_04 §9.21b
5. 参考：Core/Defend/SoldierSkillCast.cs（D-069 旧路径，勿复制 SkillId switch）
6. 参考：Core/Config/MagicBookEffectParams.cs（Params 解析风格）
7. Core/PushMap/PushMapSessionService.cs SettleMonsterDamage / Skill_02 钩子

【目标】
A) Bootstrap：SkillEffectPipeline + ISkillEffectHandler 注册 + SkillEffectParams.Parse + ConfigCsvRepository 读 EffectKind/Params/Hook；CombatStatusService 空壳+Tick。
B) Skill_04 OutgoingMulOnNewTargetFirstHit：新目标首击 Mul=1.2～1.6。
C) Mode2 表 SkillEffect_04_1～5 + Skill_04 EffectImplemented=1 → Bake。

【建议路径】
- Assets/Scripts/Core/Combat/SkillEffectPipeline.cs
- Assets/Scripts/Core/Combat/CombatStatusService.cs
- Assets/Scripts/Core/Combat/SkillEffects/OutgoingMulOnNewTargetFirstHitHandler.cs
- ConfigTables/Mode2/Excel/战斗_技能效果配置表_Combat_SkillEffectConfig.xlsx
- ConfigTables/Mode2/Excel/战斗_技能配置表_Combat_SkillConfig.xlsx

【不做】
- Skill_05+
- SoldierSkillCast 迁移
- Defend

【验收】
- grep 无 PushMapSessionService 内 "Skill_04"
- PushMap 刺客换目标首击增伤可验
- UI-021 绿；勾 issue；INDEX SE-01→done
```

---

## 分片 SE-02 — Skill_05 坚挺

**标题：** SkillEffects · SE-02 · Skill_05 致死拦截 + 无敌

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SE-02（方案 B+ / CheatDeathInvincible）。

【授权与边界】
- 本会话只做 SE-02；SE-01 Pipeline 须 done。
- 无敌逻辑在 CombatStatusService + Handler，非 Session 硬写。

【必读】
1. .scratch/soldier-skill-effects/issues/02-skill05-tenacity.md
2. SPEC_04 §9.21 Skill_05 样例
3. PushMapSessionService TryApplyMonsterDamageToWarrior（致死判定点）
4. D-071 SkillIconPopup 事件

【目标】
OnWarriorWouldDie：HP→1 + Invincible 1～5s；CD 60s；Skill_05 EffectImplemented=1。

【验收】
- 近卫致死拦截 + 无敌；CD 内不重复
- 勾 issue；INDEX SE-02→done
```

---

## 分片 SE-03 — Skill_06 震晕

**标题：** SkillEffects · SE-03 · Skill_06 AOE 击晕

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SE-03（方案 B+ / OnAaHitChanceAoeStun）。

【授权与边界】
- 本会话只做 SE-03；CombatStatusService 须有 Stun API。
- 击晕 gate 怪物 AI（PushMapMonsterAgentView 或 Session 查询），勿在 View 写 Skill_06。

【必读】
1. .scratch/soldier-skill-effects/issues/03-skill06-stun.md
2. Gameplay/PushMap/PushMapMonsterAgentView.cs
3. PushMapSessionService 士兵普攻 HitConfirm 入口

【目标】
10% 概率；半径 1.5；Stun 1～5s；Skill_06 EffectImplemented=1。

【验收】
- 炸弹师 AOE 击晕；Stun 中怪不动不攻
- 勾 issue；INDEX SE-03→done
```

---

## 分片 SE-04 — Skill_07 冰冻

**标题：** SkillEffects · SE-04 · Skill_07 AOE 减速

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SE-04（方案 B+ / OnAaHitAoeSlow）。

【授权与边界】
- 本会话只做 SE-04。
- 技能 CD 10s 走士兵 Skill CD 字段或 InternalCooldown 状态，与 Skill_03 独立。

【必读】
1. .scratch/soldier-skill-effects/issues/04-skill07-slow.md
2. SE-03 AOE 命中模式（可复用半径查询）

【目标】
攻速移速 -50%；持续 2～6s；10s 内部 CD；Skill_07 EffectImplemented=1。

【验收】
- 冰法减速 AOE + CD
- 勾 issue；INDEX SE-04→done
```

---

## 分片 SE-05 — Skill_08 精英克制

**标题：** SkillEffects · SE-05 · Skill_08 Elite 增伤

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SE-05（方案 B+ / OutgoingMulVsMonsterType）。

【授权与边界】
- 本会话只做 SE-05；难度 1；改动应集中。

【必读】
1. .scratch/soldier-skill-effects/issues/05-skill08-elite-bane.md
2. Core/Config/MonsterType.cs / MonsterConfigRow

【目标】
Elite 目标 Outgoing Mul 1.5～1.9；Skill_08 EffectImplemented=1。

【验收】
- Elite vs Normal 对比可验
- 勾 issue；INDEX SE-05→done
```

---

## 分片 SE-06 — Skill_09 渐入佳境

**标题：** SkillEffects · SE-06 · Skill_09 叠层增伤

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SE-06（方案 B+ / StackingOutgoingMulTimed）。

【授权与边界】
- 本会话只做 SE-06。
- 叠层状态放士兵运行时（可泛化为 EffectStackState），避免 Skill_09 专用全局变量。

【必读】
1. .scratch/soldier-skill-effects/issues/06-skill09-warming-up.md
2. DefendCombatWarriorState（或 PushMap 士兵 state）

【目标】
每 10s +3%～+12%/层，cap 60%；Outgoing 读取；Skill_09 EffectImplemented=1。

【验收】
- 狂战士越打越痛，上限 60%
- 勾 issue；INDEX SE-06→done
```

---

## 分片 SE-07 — Skill_10 贯穿

**标题：** SkillEffects · SE-07 · Skill_10 远程穿透

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SE-07（方案 B+ / RangedPierceExtraHits）。

【授权与边界】
- 本会话只做 SE-07；难度 3。
- 扩展 ProjectileView 为**通用穿透通道**（Handler 决定剩余穿透次数），禁止 if Skill_10。

【必读】
1. .scratch/soldier-skill-effects/issues/07-skill10-pierce.md
2. Gameplay/Defend/ProjectileView.cs
3. PushMapAdvanceView 远程开火路径
4. SE-00 SPEC 锁定的穿透飞行规则

【目标】
ExtraHitCount 1～5；100% 伤害；Skill_10 EffectImplemented=1。

【验收】
- 长弓手一箭多怪
- Projectile 可复用于未来 Kind
- 勾 issue；INDEX SE-07→done
```

---

## 分片 SE-08 — Skill_11 灼烧

**标题：** SkillEffects · SE-08 · Skill_11 DoT 灼烧

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SE-08（方案 B+ / OnAaHitApplyBurn）。

【授权与边界】
- 本会话只做 SE-08。
- DoT Tick 在 CombatStatusService；规则层扣 HP。

【必读】
1. .scratch/soldier-skill-effects/issues/08-skill11-burn.md
2. CombatStatusService Tick（SE-02/03 已用）

【目标】
每秒 20% NAP；持续 2～6s；RefreshDuration；Skill_11 EffectImplemented=1。

【验收】
- 火法灼烧 + 叠时
- 勾 issue；INDEX SE-08→done
```

---

## 分片 SE-09 — Skill_12 瞬移

**标题：** SkillEffects · SE-09 · Skill_12 远距瞬移

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 SE-09（方案 B+ / RetargetFarthestTeleportBehind）。

【授权与边界】
- 本会话只做 SE-09；难度 3。
- 规则层算最远敌+落点；View Warp；AttackSlot 同步。

【必读】
1. .scratch/soldier-skill-effects/issues/09-skill12-blink.md
2. Gameplay/PushMap/PushMapAdvanceView.cs 选目标
3. MassMoveScheduler / AttackSlotService

【目标】
OnWarriorTargetAcquired：最远敌 + 背后 SamplePosition + CD 60～20s；Skill_12 EffectImplemented=1。

【验收】
- 影刃瞬移可验；CD 随等级
- D-073 全勾选；勾 issue；INDEX SE-09→done
```

---

## 附录：新技能接入 checklist（给后续策划/Agent）

1. **SPEC**：在 §9.21b `EffectKind` 登记表新增 Token + 允许 Params + TriggerHook。
2. **表**：`Combat_SkillEffectConfig` 行填 `EffectKind` / `EffectParams` / `TriggerHook`；`Combat_SkillConfig` 挂 FK。
3. **代码**：新建 `Assets/Scripts/Core/Combat/SkillEffects/{Token}Handler.cs` 实现 `ISkillEffectHandler`；在 Pipeline 启动时 `Register`。
4. **Session**：若现有 Hook 不够，**只增 Hook 枚举 + 一处 Dispatch 调用**，不按 SkillId 分支。
5. **验收**：PushMap 手验 → `EffectImplemented=1` → Bake → UI-021 绿。
