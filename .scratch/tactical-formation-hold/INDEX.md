# 战术阵型守槽不散 — Issue 索引

**选定方案：** A — 守槽挥刀（人不离槽；只打阵型朝向前方 90° 且进入该兵攻击距离的敌人；守槽不受软碰撞挤离；死亡后最大 `Slot_*` 编号补到空出的小编号）
**眼前判定：** 阵型朝向前方 90° 扇形（左右各 45°）+ 该兵 `AttackRange`
**难度：** 3（须拆步；一次会话只做下面一片；本批发布 issues 时不编码）
**权威 SPEC：** 规则尚未写入。TFH-00 关闭前，战斗仍以 [SPEC_03 §3.18](../../SPEC_03_GameRules.md) 的 leash + AttackSlot 为准。关闭后以 §3.18 / [SPEC_04 §9.7](../../SPEC_04_Technical.md) / §3.8 D-094 为准。
**现状：** 接敌进入 leash 后成员改走 `AttackSlot` 围敌，所以阵型被拉开。软碰撞不在敌我之间传递。死亡不换槽。`StatModifiers` overlay 已存在。

| ID | 文件 | 状态 | 说明 |
|----|------|------|------|
| TFH-00 | [00-spec-close.md](issues/00-spec-close.md) | done | SPEC 把守槽规则写死 |
| TFH-01 | [01-hold-front-attack.md](issues/01-hold-front-attack.md) | done | 三处战斗：守槽，只打眼前 |
| TFH-02 | [02-slot-anchor.md](issues/02-slot-anchor.md) | done | 守槽时不受软碰撞挤离 |
| TFH-03 | [03-slot-fill.md](issues/03-slot-fill.md) | done | 最大编号补到死亡小编号 |
| TFH-04 | [04-stat-overlay-check.md](issues/04-stat-overlay-check.md) | done | 核对现有阵型属性，不改表数值 |

**建议执行序：** TFH-00 → TFH-01 → TFH-02 → TFH-03 → TFH-04

未入组士兵、羁绊、布阵拖拽与卡牌不在本目录。
