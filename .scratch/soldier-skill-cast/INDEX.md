# 士兵战斗技能施放 SoldierSkillCast

**状态：** SC-00/01/02/03 **done**（方案 C + 格挡方案 B + 舒适方案 A；D-069）。

**背景：** D-062（SS-01～04）已完成表加载 / 授予 / 持久化 / UI 展示。D-069：PushMap 忠诚兵自动施放 `Skill_03` 连发（占用普攻通道 3×方案 D）+ `Skill_01` 格挡 + `Skill_02` 舒适。

**选定：** 难度 **3**（首片）/ 后续片 **2**；方案 **C**（连发）+ **B**（格挡）+ **A**（舒适）；战场 **PushMap**；插入点 **CD 好且已进距即放**。验收号 **D-069**（勿与装备仓 D-067 混淆）。

| 片 | 文件 | 目标 | 状态 |
|----|------|------|------|
| SC-00 | [issues/00-spec-close.md](issues/00-spec-close.md) | SPEC 修订 + D-069 验收项 | **done** |
| SC-01 | [issues/01-cast-pipeline-skill03.md](issues/01-cast-pipeline-skill03.md) | 施放管线 + `Skill_03` 连发（主动） | **done** |
| SC-02 | [issues/02-skill01-block-passive.md](issues/02-skill01-block-passive.md) | `Skill_01` 格挡（被动） | **done** |
| SC-03 | [issues/03-skill02-comfort.md](issues/03-skill02-comfort.md) | `Skill_02` 舒适（条件增伤） | **done** |

**总约束：**

1. SPEC 优先；设计变更先改 SPEC/Changelog 再编码。
2. 每会话 **只实现一个分片**。
3. 规则 Service 结算；View 只播表现。
4. 配置驱动；禁止硬编码策划数值（连发次数硬映射 `SkillEffect_03_*` = 3；格挡概率硬映射 `SkillEffect_01_*` = 10/15/20/25/30；舒适倍率硬映射 `SkillEffect_02_*` = 5/10/15/20/25，已写入 SPEC）。
