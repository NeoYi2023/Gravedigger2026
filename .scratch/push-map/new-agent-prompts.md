# PushMap WarriorCombat / DamagePopup / HitFlash — NewAgent 可粘贴分步指令

**用途：** 每个 New Agent **只粘贴一个分片**整块正文（含标题行）。  
**执行序：** PM-12 → PM-13（**PM-11 已由文档 Agent 完成，勿再开**）。  
**权威：** `.scratch/push-map/INDEX.md` + 对应 `issues/1N-*.md`；规则见 SPEC_03 §3.14、SPEC_04 §6/§9.22（v0.75.0）。  
**工作区根：** `f:\CursorGame_Git\Gravedigger2026`  
**Unity 脚本根：** `Gravedigger2026/Assets/Scripts/`

**总约束（每个分片都适用）：**
1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；SPEC 优先，设计变更先改 SPEC/Changelog 再编码。
2. 本会话 **只实现本片**；禁止顺手做下一片。
3. 难度 3：先 AskQuestion（或不可用时短文本）确认难度与方案比选，**选定后再编码**。选定方案记录为 **方案 B**（整包已定）；本片只需确认实现子细节（若有分歧）。
4. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。
5. **仅推图战**；不要做 Defend 飘字/闪烁。

**HitFlash 锁定时间轴：** 共 2 次脉冲（立即 + 再 1 次），每次亮 0.1s，**中间不灭** → 视觉连续亮 ≈0.2s；过程中再受伤从头刷新。怪亮红 / 兵亮白。

**DamagePopup 锁定：** 格式 `-受伤值`；怪红字号 28；兵白字号 24；挂被击目标头顶。

---

## 分片 PM-12 — 士兵→怪 HitConfirm + 红飘字红闪（先做）

**标题：** PushMap · PM-12 · 士兵→怪 WarriorCombat + 红 DamagePopup + 红 HitFlash

**粘贴正文：**

```
你正在实现 Gravedigger2026 推图战切片 PM-12（方案 B）。

【授权与边界】
- Demo 已授权；本会话只做 PM-12，禁止 PM-13（怪→兵白字白闪）、禁止改 Defend 飘字。
- 废止 DemoKillEngageSeconds / PollMonsterDemoKill；击杀改 RemainingHp≤0。
- PushMapSessionService 独立镜像 Defend HitConfirm，禁止绑定 DefendSessionService 生命周期。

【必读】
1. `.cursor/skills/unity-spec-dev-workflow/SKILL.md`
2. `.scratch/push-map/INDEX.md`
3. `.scratch/push-map/issues/12-soldier-hit-monster-fx.md`
4. SPEC_03 §3.14「Demo 士兵攻击 / WarriorCombat」「DamagePopup」「HitFlash」「Demo BOSS 通关」边界
5. SPEC_04 §6 PM-12 段 + §9.22「WarriorCombat / DamagePopup / HitFlash 运行时契约」
6. 参考：DefendSessionService HitConfirm、WarriorAgentView 前摇/弹道、WarriorCombatMath、ProjectileView、DigGraveView PropertyBlock、WarriorTaskDebugLabelView TextMesh

【目标】
开战登记士兵/怪物 HP；士兵近战前摇 + 远程弹道 HitConfirm 扣怪血；怪头顶红飘字 -N（字号28）+ 怪红 HitFlash（2×0.1s 紧接不灭，可刷新）；BOSS HP≤0 → TryNotifyBossKilled。

【范围】
- 改：PushMapSessionService、PushMapAdvanceView、PushMapStageController（去 DemoKill）、必要时 PushMapMonsterAgentView
- 新建：DamagePopupView + Prefab；HitFlashView
- 复用：WarriorCombatMath、Projectile prefab/View、WarriorAnimView

【不做】
- 怪→兵扣血与兵白 FX（PM-13）
- 技能 / 护甲 / 副本正文

【流程门禁】
- 难度 3；用 AskQuestion 确认难度；子方案比选（例如：伤害事件用 C# event 还是 StageController 回调；飘字 TextMesh vs TMP）后选定再编码
- AskQuestion 不可用时短文本确认并注明

【验收】
- 对齐 issue 验收清单；INDEX 中 PM-12 → done
- 回复：选定子方案、新建/改动文件路径、如何在 PushMap Demo 目视验证
```

---

## 分片 PM-13 — 怪→兵扣血 + 白飘字白闪（PM-12 之后）

**标题：** PushMap · PM-13 · 怪→兵扣血 + 白 DamagePopup + 白 HitFlash

**前置：** PM-12 status=done。

**粘贴正文：**

```
你正在实现 Gravedigger2026 推图战切片 PM-13（方案 B）。

【授权与边界】
- 本会话只做 PM-13；禁止重做 PM-12 命中链（除非修 PM-12 阻塞 bug）。
- 仅推图；主角 Shield 命中不要求飘字/闪烁。

【必读】
1. `.cursor/skills/unity-spec-dev-workflow/SKILL.md`
2. `.scratch/push-map/INDEX.md`
3. `.scratch/push-map/issues/13-monster-hit-soldier-fx.md`
4. SPEC_03 §3.14「Demo 士兵受击」「DamagePopup」「HitFlash」「AggroMode 挑衅」
5. SPEC_04 §9.22 运行时契约（TryApplyMonsterDamageToWarrior）
6. 复用 PM-12 的 DamagePopupView / HitFlashView（换白样式）

【目标】
怪对忠诚兵按 AttackPower 扣 RemainingHp；兵头顶白 -N（字号24）+ 白 HitFlash（同 2×0.1s 紧接节奏，可刷新）；HP≤0 → CombatDead 停手；被动挑衅优先真实 HitConfirm（可保留进距兜底）。

【范围】
- PushMapSessionService.TryApplyMonsterDamageToWarrior
- PushMapMonsterAgentView 对兵命中接线（替换仅日志）
- 士兵白飘字/白闪；最小 CombatDead 表现
- 挑衅接线优先 HitConfirm

【不做】
- 完整 PermanentDeath 物资去向 polish；Defend FX；技能

【流程门禁】
- 难度 3；AskQuestion 确认难度与死亡表现最小方案后再编码

【验收】
- 对齐 issue 验收清单；INDEX 中 PM-13 → done
- 回复：变更文件、目视验证步骤
```
