# 士兵技能效果 Skill_04～Skill_12（Mode2 / PushMap）

**状态：** SE-00 **done**；SE-01 **done**；SE-02 **done**；SE-03 **done**；SE-04 **done**；SE-05 **done**；SE-06 **done**；SE-07 **done**；SE-08 **done**；SE-09 **done**。

**背景：** Mode2 `Combat_SkillConfig` 中 `Skill_01`/`Skill_02`/`Skill_03` 已 `EffectImplemented=1`（D-069）。**D-073：** `Skill_04`～`Skill_12` Lv1～5 均已 `EffectImplemented=1`（UI-021 绿）。

**选定：** 难度 **3**（整包）；方案 **B+**（在方案 B 基础上补 **SkillEffectKind 登记制 + Handler 注册表 + 表驱动 EffectParams**，对齐 MagicBook `EffectPayload` 模式；**禁止**在 `PushMapSessionService` 按 `SkillId` 写 `if/switch`）。`CombatStatusService` 承载无敌 / 击晕 / 减速 / 灼烧 DoT；规则发事件、View 只表现。

**验收号：** **D-073**（Skill_04～12 PushMap 战斗效果 + `EffectImplemented` 全等级置 1）。**D-072** 仍为魔法书弹窗删除（v0.82.81）。

| 片 | 文件 | 目标 | 状态 |
|----|------|------|------|
| SE-00 | [issues/00-spec-close.md](issues/00-spec-close.md) | SPEC：EffectKind 登记表 + §9.21b 扩列 + CombatStatus + D-073 | **done** |
| SE-01 | [issues/01-skill04-first-strike.md](issues/01-skill04-first-strike.md) | `Skill_04` 先发制人 + **管线/bootstrap** | **done** |
| SE-02 | [issues/02-skill05-tenacity.md](issues/02-skill05-tenacity.md) | `Skill_05` 坚挺（致死拦截 + 无敌） | **done** |
| SE-03 | [issues/03-skill06-stun.md](issues/03-skill06-stun.md) | `Skill_06` 震晕（AOE 击晕） | **done** |
| SE-04 | [issues/04-skill07-slow.md](issues/04-skill07-slow.md) | `Skill_07` 冰冻（AOE 减速） | **done** |
| SE-05 | [issues/05-skill08-elite-bane.md](issues/05-skill08-elite-bane.md) | `Skill_08` 精英克制 | **done** |
| SE-06 | [issues/06-skill09-warming-up.md](issues/06-skill09-warming-up.md) | `Skill_09` 渐入佳境（叠层增伤） | **done** |
| SE-07 | [issues/07-skill10-pierce.md](issues/07-skill10-pierce.md) | `Skill_10` 贯穿（远程穿透） | **done** |
| SE-08 | [issues/08-skill11-burn.md](issues/08-skill11-burn.md) | `Skill_11` 灼烧（DoT 叠时） | **done** |
| SE-09 | [issues/09-skill12-blink.md](issues/09-skill12-blink.md) | `Skill_12` 瞬移（远距换目标 + 背后传送） | **done** |

分步粘贴指令：[new-agent-prompts.md](new-agent-prompts.md)

**总约束：**

1. SPEC 优先；设计变更先改 SPEC/Changelog 再编码。
2. 每会话 **只实现一个分片**；禁止顺手做下一片。
3. **PushMap only**；Defend 本片不接线；Mode1 不改。
4. **扩展性：** 新技能 = 登记表新增 `EffectKind` + 表行填 `EffectKind`/`EffectParams` + 实现一个 `ISkillEffectHandler` 并注册；Session 只调管线，**不**按 `SkillId` 分支。
5. `Skill_01`/`Skill_02`/`Skill_03` 暂保留 `SoldierSkillCast` 硬映射（D-069）；SE-00 SPEC 写清后续可迁到 Kind 的意图，**本片不强制迁移**。
6. 每片完成后：该 `SkillId` **Lv1～5** 在 Mode2 Excel `EffectImplemented=1` → Bake CSV；UI-021 指示器变绿。
7. 完成后：勾 issue 验收、更新 INDEX、回复变更文件清单。

**未实现 SkillEffectId 一览（Mode2 / EffectImplemented=0）：** 无（Skill_04～Skill_12 Lv1～5 均已 `EffectImplemented=1`）。
