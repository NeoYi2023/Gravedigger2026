---
title: TFH-00 阵型守槽不散 SPEC 关闭
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 TacticalFormation
  - SPEC_03 §3.12 FormationSlot
  - SPEC_03 §3.14 士兵推进
  - SPEC_04 §9.7 战术阵型运行时契约
  - SPEC_03 §3.8 D-093
approach: A
---

## 目标

把「阵型不散」写成 SPEC，新增验收 **D-094**。本片只改文档（双语 + Changelog），不写玩法代码。

选定方案 A。整组仍跟虚拟中心走（推图流场 / 防守守点）。「不散」是相对槽位不散，不是冻在世界坐标上。

## 需求指令

修订 SPEC_03 §3.18 战斗段与 SPEC_04 §9.7 战术阵型契约（中英同步），并在 §3.8 增加 D-094。D-093 的多组、等级、卡牌保持不变；本片只改**已入组成员的接敌与站位**。

必须写明：

- 入组成员目的地始终是 `GoalKind=FormationSlot`（中心 + 旋转后的 `Slot_*` 本地偏移）。接敌**不再** `TryClaim` AttackSlot，也不再把人钳到 leash 圆周。
- 未走到 `SlotArriveEpsilon`：继续走向槽位，即使敌人已在射程内。
- 已在槽上，且敌人中心落在**该组朝向**前方 **90°** 扇形（左右各 45°）内，并且进入该兵 `AttackRange`：暂停位移并沿用现有命中。多名候选人取扇形内最近。扇形外或不在射程：不追、不改 Goal。
- `LeashRadius` 字段保留，本规则不再用它决定离槽或是否开打。
- 守槽期间该成员受到的软碰撞修正为 0（敌我本来就不同 `DetourGroup`）。细节实现留给 TFH-02，SPEC 只锁结果：守槽成员不会被挤离槽位。
- 死亡或 Rebel 腾出槽位后，若该组仍 ≥ `MinMemberCount`：活着成员里**编号最大**的一人换到这个更小的编号上（编号 = `Slot_*` 名字排序后的下标，`Slot_00` = 0）。最大编号自己死亡则高位留空，不把中间的人依次前移。同一帧多个空槽按编号从小到大依次补。低于 Min 仍只解散该组。不写回布阵坐标。等级 overlay 仍按剩余成员重算。
- 属性继续用现有 `StatModifiers` → StatMul overlay，不新做 Buff 系统。攻击力不单独乘；近战走力量等主属性派生。`MaxHP` / `Strength` / `Agility` / `Intelligence` / `MoveSpeed` / `All` 已支持。
- 未入组士兵仍走原来的 AttackSlot / Objective / Home。

Changelog 记入 SPEC_00。CONTEXT 若需新术语只收「守槽 / 前方扇形 / 槽位递补」这类词，规则正文留在 SPEC。

## 不做

- C#、Prefab、Excel、CSV
- 改未入组士兵的追击
- 给楔阵填新的血量或攻击倍率（没有负责人给的数值就不改表）

## 验收

- [ ] §3.18 与 §9.7 中英都改成守槽挥刀，不再写「接敌走 AttackSlot 再 leash 钳制」作为成员行为
- [ ] 扇形写明 90°、轴是组朝向、须已到槽才停步挥刀
- [ ] 递补写明：最大编号补到更小的死亡编号，不整体压缩
- [ ] D-094 出现在 §3.8，并指向本目录 issues
- [ ] 未入组士兵与 FormationBond 的句子仍在

## 依赖

- 无（D-093 / TFG-01～05 已编码）
