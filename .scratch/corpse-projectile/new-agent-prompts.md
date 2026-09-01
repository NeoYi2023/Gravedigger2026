# D-083 尸体投射 — New Agent 分步指令

**用途：** 每个 New Agent **只粘贴一个分片**整块正文。  
**执行序：** CP-01 → CP-02 → CP-03 → CP-04 → CP-05（CP-00 已完成）。  
**权威：** `.scratch/corpse-projectile/INDEX.md` + 对应 `issues/CP-*.md`；SPEC v0.83.45。  
**工作区根：** `e:\Work\Cursor\Gravedigger2026\Gravedigger2026`  
**Unity 脚本根：** `Gravedigger2026/Assets/Scripts/`

**总约束：**

1. 先读 `unity-spec-dev-workflow` Skill + 本片 `spec_refs`；设计变更先 SPEC 再代码。
2. 本会话 **只实现本片**；禁止顺手下一片。
3. 选定 **方案 A**：Session 结算砸击；View 抛物线 + 调 Session；不新建尸体 Projectile Prefab。
4. 砸击 **不**叠 Comfort / D-073；砸击致死 **不**连锁抛物线（`knockbackDistance=0`）。
5. 完成后：勾 issue 验收、更新 INDEX、回复变更文件清单。

---

## 分片 CP-01 — 常量 + 抛物线核心

```
你正在实现 Gravedigger2026 D-083 切片 CP-01。

【边界】只做常量表三键 + CombatRuntimeTuning + MonsterDeathPresentation 抛物线采样 + 自检菜单。不接 Session 砸击、不改 AgentView。

【必读】
1. `.scratch/corpse-projectile/issues/01-constants-parabolic-core.md`
2. SPEC_04 §9.20b / §15.5
3. 现有 `MonsterDeathPresentation.cs`

【交付】见 issue 验收清单。
```

---

## 分片 CP-02 — 规则层砸击

```
你正在实现 Gravedigger2026 D-083 切片 CP-02。

【边界】PushMapSessionService + DefendSessionService `TryApplyCorpseSmashDamage`；独立伤害通道；CorpseSmash 致死不连锁（约定 knockbackDistance=0）。不改 View Tick。

【必读】
1. `.scratch/corpse-projectile/issues/02-rules-corpse-smash.md`
2. `PushMapSessionService.TryFinalizeMonsterDeath`
3. `DefendSessionService` 普攻致死路径

【编码前】AskQuestion 确认砸击致死表现用 knockbackDistance=0（推荐）。

【交付】见 issue 验收清单。
```

---

## 分片 CP-03 — PushMap 垂直

```
你正在实现 Gravedigger2026 D-083 切片 CP-03。

【边界】PushMap 抛物线尸体 + 飞行/落地砸怪 + 假死时序 + 红飘字/闪。不做 Defend。

【必读】
1. `.scratch/corpse-projectile/issues/03-pushmap-vertical.md`
2. `PushMapMonsterAgentView` / `PushMapStageController`
3. SPEC_03 §3.14 假死边界

【交付】见 issue 手验清单。
```

---

## 分片 CP-04 — Defend 垂直

```
你正在实现 Gravedigger2026 D-083 切片 CP-04。

【边界】Defend 对等接线；无假死。建议 CP-03 已完成（共享 helper）。

【必读】
1. `.scratch/corpse-projectile/issues/04-defend-vertical.md`
2. `MonsterAgentView` / `DefendStageController`

【交付】见 issue 手验清单。
```

---

## 分片 CP-05 — 自检与收尾

```
你正在实现 Gravedigger2026 D-083 切片 CP-05。

【边界】CorrectnessChecks + Editor 菜单；D-083 §3.8 标完成；INDEX 全 done。

【必读】
1. `.scratch/corpse-projectile/issues/05-correctness-handcheck.md`

【交付】见 issue 验收清单。
```
