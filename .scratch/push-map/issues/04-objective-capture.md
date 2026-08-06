---
title: PushMap — 目标点链与判定圈占领
status: done
difficulty: 3
demo_scope: authorized
approach: A
spec_refs:
  - SPEC_03 §3.14 目标点链与占领
  - SPEC_04 §9.22 CaptureSeconds / 占领运行时契约
---

## 目标

全队共当前目标；CaptureZone 连续 5s 无存活怪物 → 占领 → 切换下一目标；Rebel 不阻挡占领。

## 范围

- CurrentObjective = min uncaptured ObjectiveOrder
- 忠诚士兵向当前目标推进（EngageZone 战斗可打断）
- 占领事件：标记已占领；通知停刷（接 PM-05）

## 不做

- 刷怪生成本身（PM-05）
- 占领物资入账表现 polish（可日志）

## 验收

- [x] 1→2 目标切换可验（无怪连续 CaptureSeconds → 占领 → CurrentObjective 切换；日志/HUD 可见）
- [x] 圈内有怪时计时重置；无怪 5s 占领（`TickCapture` 有怪清零；探测可注入 `PushMapMonsterPresenceProbe`）

## 依赖

- [01](01-map-prefab-markers.md)
- [03](03-stage-module-wire.md)

## 编码前

- 难度 3 方案比选；须 Demo 授权

## 本会话交付（方案 A）

- SPEC：SPEC_03 §3.14 增「规则归属 / Demo 无刷怪边界」（中英）；SPEC_04 §9.22 增「占领运行时契约」（中英）；SPEC_00 v0.57.0
- 规则：`Assets/Scripts/Core/PushMap/PushMapSessionService.cs` 扩展 `CaptureSecondsRequired` / `CurrentObjectiveOrder` / `IsObjectiveCaptured` / `TryBeginObjectiveChain` / `TickCapture`；事件 `ObjectiveCaptured`（停刷钩子→PM-05）+ `CurrentObjectiveChanged`
- 表现：`Assets/Scripts/Gameplay/PushMap/` 新增 `PushMapMonsterPresenceProbe`（默认扫 `MonsterAgentView.IsAlive && ContainsXZ`；可注入/重置占位）与 `PushMapAdvanceView`（NavMeshAgent 向当前目标推进；探测到怪暂停=Engage 打断占位；Rebel 不推进）；`PushMapStageController` Combat 收集 `ObjectivePoint` 排序喂 Session、`Update` 喂探测、开战 Bake NavMesh + 部署主角/忠诚兵、占领日志与 HUD 当前目标
- 边界：不生成刷怪；无怪时 1→2 切换；停刷仅事件/日志；占领奖励/副本解锁不入账表现
