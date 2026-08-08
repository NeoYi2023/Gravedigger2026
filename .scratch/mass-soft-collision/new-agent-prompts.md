# MassCombat SoftCollision — NewAgent 可粘贴分步指令

**用途：** 每个 New Agent **只粘贴一个分片**整块正文（含「标题」行）。  
**执行序：** SC-01 → SC-02 → SC-03 → SC-04（已定；目标④「流体感」后置，本文件不含）。  
**权威：** `.scratch/mass-soft-collision/INDEX.md` + 对应 `issues/0N-*.md`；规则见 SPEC_03 §3.12 B+、SPEC_04 §9.7。  
**工作区根：** `f:\CursorGame_Git\Gravedigger2026`  
**Unity 脚本根：** `Gravedigger2026/Assets/Scripts/`

**总约束（每个分片都适用）：**
1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；SPEC 优先，设计变更先改 SPEC/Changelog 再编码。
2. 本会话 **只实现本片**；禁止顺手做下一片或 Follow/Sweep。
3. 难度 ≥2：先 AskQuestion（或不可用时短文本）确认难度与方案比选，**选定后再编码**。
4. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。

---

## 分片 SC-01 — SoftCollision 核心（先做）

**标题：** MassCombat B+ · SC-01 · SoftCollisionService 核心（纯 C#；无 Stage 接线）

**粘贴正文：**

```
你正在实现 Gravedigger2026 大规模战斗寻路方案 B+ 的切片 SC-01。

【授权与边界】
- Demo 范围内；本会话只做 SC-01，禁止 SC-02/03/04、禁止 Follow、禁止改 FlowField 语义。
- 叠在已有方案 B（FlowField + AttackSlot + LocalDetour）之上，不替换它们。
- 执行序已定：SC-01 → SC-02 → SC-03 → SC-04。

【必读】
1. `.cursor/skills/unity-spec-dev-workflow/SKILL.md`
2. `.scratch/mass-soft-collision/INDEX.md`
3. `.scratch/mass-soft-collision/issues/01-soft-collision-core.md`
4. SPEC_03 §3.12「方案 B+」SoftCollision；SPEC_04 §9.7 B+ 运行时契约
5. 复用：`Assets/Scripts/Core/Pathing/SpatialHash2D.cs`（MP-03）

【目标】
实现纯 C# SoftCollisionService：登记/注销足迹，分帧邻域排斥，写出 CorrectionXz。

【范围】
- 新建：`Gravedigger2026/Assets/Scripts/Core/Pathing/SoftCollisionService.cs`
- API：Register / Unregister / Tick(dt, maxBodiesPerFrame) / TryGetCorrection(id, out Vector2)
- 复用 SpatialHash2D；禁止 O(n²)；热路径无每帧托管分配
- 常量对齐 SPEC_04 §9.7：哈希 cell≈0.5；查询半径≈2*r+0.2；repulsionScale 默认 1.0；ResolveCollisions 默认 true
- 完全重叠时按稳定 RuntimeId 确定性侧推
- 正确性自检（类比 FlowFieldCorrectnessChecks / AttackSlotCorrectnessChecks）

【不做】
- Stage/View 接线（SC-03）
- Surround 槽位（SC-02）
- 替代 NavMesh/AirWall

【流程门禁】
- 自评难度 3、须拆步已满足（本片即第一片）
- 用 AskQuestion 确认难度；再比选 2～3 种排斥实现方案（例如：位置冲量 vs 速度偏置；分帧轮转策略），选定后再编码
- AskQuestion 不可用时短文本确认并注明

【验收】
- 对齐 issue 验收清单；更新 INDEX 中 SC-01 状态为 done
- 回复：选定方案、新建/改动文件路径、如何跑自检
```

---

## 分片 SC-02 — Surround 缺口 AttackSlot

**标题：** MassCombat B+ · SC-02 · SurroundGap AttackSlot（依赖 SC-00/MP-02；可与 SC-01 并行，但推荐在 SC-01 之后）

**前置：** 建议 SC-01 done；与 SC-01 无代码依赖时可并行开第二 Agent。

**粘贴正文：**

```
你正在实现 Gravedigger2026 大规模战斗寻路方案 B+ 的切片 SC-02。

【授权与边界】
- 本会话只做 SC-02；禁止 SoftCollision 接线、禁止 SC-03/04、禁止 Sweep/Follow。
- 不破坏既有无 surround 的 TryClaim 调用语义（MP-02/MP-05）。

【必读】
1. `.cursor/skills/unity-spec-dev-workflow/SKILL.md`
2. `.scratch/mass-soft-collision/issues/02-surround-gap-slots.md`
3. SPEC_03 §3.12 SurroundGap / CombatMoveMode=Surround；SPEC_04 §9.7 Surround 常量
4. 现有：`Assets/Scripts/Core/Pathing/AttackSlotService.cs`

【目标】
扩展 AttackSlotService.TryClaim(..., surround?: SurroundParams)：环上跳过 SurroundGapDegrees 扇区。

【范围】
- SurroundGapDegrees Demo 默认 60
- 默认缺口方向：相对「目标←进攻方质心」背侧扇区
- 近战多打一默认 Surround；远程默认不传 surround（Chase）
- 纯 C# 正确性自检：同目标多认领者角度不落入缺口；缺口外仍可认领；释放再认领

【不做】
- SoftCollision、Stage 接线、Sweep、Follow

【流程门禁】
- 难度 3：AskQuestion 确认难度 + 方案比选（缺口方向定义 / API 形状：可选参数 vs 重载），选定后再编码

【验收】
- issue 清单全过；INDEX SC-02 → done
- 回复：选定方案、diff 文件、自检入口
```

---

## 分片 SC-03 — Scheduler / Stage 接线

**标题：** MassCombat B+ · SC-03 · SoftCollision+Surround 接线 MassMoveScheduler / PushMap / Defend

**前置（硬阻塞）：** SC-01 与 SC-02 均为 **done**。

**粘贴正文：**

```
你正在实现 Gravedigger2026 大规模战斗寻路方案 B+ 的切片 SC-03。

【授权与边界】
- 前置必须已完成 SC-01 SoftCollisionService + SC-02 SurroundGap。
- 本会话只做接线与手玩可验证行为；禁止 SC-04 压测大改、禁止 Follow、禁止改 FlowField 共享目标语义、禁止 Sweep。

【必读】
1. `.cursor/skills/unity-spec-dev-workflow/SKILL.md`
2. `.scratch/mass-soft-collision/issues/03-scheduler-wire.md`
3. SPEC_03 §3.12 B+、§3.14 遇敌/到达守备；SPEC_04 §9.7 管线：
   desiredDir → LocalDetour → + SoftCollision.Correction → View.Move
4. 现有：MassMoveScheduler、PushMapStageController、DefendStageController、*AgentView / PushMapAdvanceView

【目标】
把 SoftCollision 与 Surround 接入运行栈；近战多打一认领带 Surround。

【范围】
- PushMap / Defend：开战 Register、死亡/卸载 Unregister SoftCollision
- MassMoveScheduler.Tick 叠加 Correction；交战 GoalKind=AttackSlot 时 repulsionScale 降至约 0.35～0.5
- 保持 NavMeshAgent ObstacleAvoidance 关闭（既有）
- CombatMoveMode 推导：AttackSlot+近战→Surround；否则 Chase；Objective/FormationHome 无 Follow
- 规则/表现分离：规则仍出 GoalKind；View 只应用位移

【不做】
- 200v200 正式压测数字（→ SC-04）
- 引入 Follow / Sweep

【流程门禁】
- 难度 3：AskQuestion 确认难度 + 方案比选（Correction 加在 steer 还是最终位移；谁持有 SoftCollision 生命周期），选定后再编码

【验收】
- 手玩：多近战围怪可见环上缺口；重叠减轻；无全队粘随主角；CaptureZone 到达守备语义不变
- INDEX SC-03 → done；回复文件清单与手测步骤
```

---

## 分片 SC-04 — 200v200 性能回归

**标题：** MassCombat B+ · SC-04 · SoftCollision 并入后 200v200 性能回归

**前置（硬阻塞）：** SC-03 **done**。

**粘贴正文：**

```
你正在实现 Gravedigger2026 大规模战斗寻路方案 B+ 的切片 SC-04。

【授权与边界】
- 前置 SC-03 已接线 SoftCollision。
- 本会话只做性能回归与必要回退旋钮；禁止新玩法、禁止 Follow。

【必读】
1. `.cursor/skills/unity-spec-dev-workflow/SKILL.md`
2. `.scratch/mass-soft-collision/issues/04-perf-regression.md`
3. SPEC_04 §9.7 性能预算（≤~2.5 ms/帧，存活可移动 ≤400）
4. 现有：MassPathingPerfStress / MassPathingPerfStressView / Editor Menu（MP-07）

【目标】
SoftCollision.Tick 计入压测；导向仍 ≤~2.5 ms/帧；超预算记录并实现/启用回退（降邻域半径、加大 SoftCollision 分帧等）。

【范围】
- 扩展压测入口，含 SoftCollision
- 记录含/不含 SoftCollision 对比数字（可粘贴到 issue 或 SPEC 备注）
- 不改玩法语义

【流程门禁】
- 难度 2：AskQuestion 确认难度；若需改架构级分帧策略再比选方案，否则可在确认后直接扩展压测

【验收】
- Editor Menu 可跑；数字可复现
- INDEX SC-04 → done
- 回复：数字、回退项（若触发）、文件清单
- 若本机无 Unity：交付代码与跑法，数字栏标注「待负责人手验粘贴」
```

---

## 使用提示

| 场景 | 做法 |
|------|------|
| 串行最稳 | 开 New Agent → 只贴 SC-01 → 完成后再开 Agent 贴 SC-02… |
| 抢时间 | SC-01 与 SC-02 可两个 Agent 并行（无互相依赖）；**SC-03 必须等两者 done** |
| Agent 跑偏 | 把对应 `issues/0N-*.md` 路径再贴一次，并强调「本会话只做本片」 |
