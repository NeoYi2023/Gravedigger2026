# 战术阵型多组与等级（D-093）— Issue 索引

**选定方案：** 手动建组 + 复合主键 `(FormationId, FormationLevel)` + 组跟布阵存档走 + 每组独立虚拟中心  
**权威 SPEC：** [SPEC_03 §3.18](../../SPEC_03_GameRules.md) · [SPEC_04 §9.30](../../SPEC_04_Technical.md) · UI-030 / UI-035 · §3.8 D-093  
**难度：** 3（须拆步；一次会话只做下面一片）  
**Changelog：** SPEC_00 v0.84.70（TFG-00 规则、TFG-01 表与加载器、TFG-02 手动建组与存档、TFG-03 目录条建组、TFG-04 顶中卡牌、TFG-05 按组战斗已编码；待验收）

| ID | 文件 | 状态 | 说明 |
|----|------|------|------|
| TFG-00 | [00-spec-close.md](issues/00-spec-close.md) | done | SPEC 规则关闭 |
| TFG-01 | [01-config-formation-level.md](issues/01-config-formation-level.md) | done | 表增 `FormationLevel` + 复合键加载 |
| TFG-02 | [02-group-session.md](issues/02-group-session.md) | done | 手动建组 / 解散 / 裁剪 / 存档 |
| TFG-03 | [03-catalog-bar.md](issues/03-catalog-bar.md) | done | 左缘按钮改为创建一组 |
| TFG-04 | [04-group-cards.md](issues/04-group-cards.md) | done | 顶中卡牌、头顶图标、解散 |
| TFG-05 | [05-combat-per-group.md](issues/05-combat-per-group.md) | done | 战斗每组一中心、死亡重算等级 |

**建议执行序：** TFG-00 → TFG-01 → TFG-02 → TFG-03 与 TFG-04（都依赖 02）→ TFG-05（依赖 01+02）
