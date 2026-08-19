# 战斗技能图标 CombatSkillIcon

**状态：** SI-00/01/02 **done**（方案 A：士兵子节点 SpriteRenderer + 正交相机像素换算；D-071 / UI-025）。

**背景：** PushMap Combat 显示士兵技能图标。瞬时施放（`Skill_03` 提交、`Skill_01` 格挡成功）在该兵头顶 35×35，静止 0.6s 后沿世界 +Z 上飘 0.3s 淡出；持续效果（`Skill_02` 满血）在脚下 20×20，生效瞬间同时头顶飘一次。Defend 不接线。

**选定：** 难度 **2**；方案 **A**；战场 **PushMap**；验收号 **D-071**。

| 片 | 文件 | 目标 | 状态 |
|----|------|------|------|
| SI-00 | [issues/00-spec-close.md](issues/00-spec-close.md) | SPEC 修订 + D-071 / UI-025 | **done** |
| SI-01 | [issues/01-overhead-popup.md](issues/01-overhead-popup.md) | 头顶飘：`Skill_03` / `Skill_01` + 右排 | **done** |
| SI-02 | [issues/02-persist-foot.md](issues/02-persist-foot.md) | `Skill_02` 脚下持续 + 开战/受伤/死亡 | **done** |

**总约束：**

1. SPEC 优先；设计变更先改 SPEC/Changelog 再编码。
2. 规则 Service 发 `SkillIconPopup` / `SkillPersistChanged`；View 只播表现。
3. 图标 `Resources/UI/Skills/{SkillId}`；缺图空框。
4. Defend 不接线。
