# SearchExtract HoldFraming（搜打撤守点镜头 / P1）— Issue 索引

**选定方案：** B — 视口包围 + 迟滞（外框立刻拉远；内框滞留后慢收紧）

**权威 SPEC：** [SPEC_03 §3.19](../../SPEC_03_GameRules.md) 倒计时战斗镜头 · [SPEC_04 §9.20b](../../SPEC_04_Technical.md) `SearchExtractHold*` · [SPEC_04 §9.32](../../SPEC_04_Technical.md) 开战镜头 · [SPEC_04 §6](../../SPEC_04_Technical.md)

**难度：** 2（须拆步）；末片 SE-CAM-03 = 难度 1

**Changelog：** SPEC_00 v0.84.27（规则） / **v0.84.28**（样例锁定）

| ID | 文件 | 依赖 | 难度 | 状态 |
|----|------|------|------|------|
| SE-CAM-00 | [00-spec-close.md](issues/00-spec-close.md) | — | 2 | **done** |
| SE-CAM-01 | [01-constants-solver.md](issues/01-constants-solver.md) | SE-CAM-00 | 2 | **done** |
| SE-CAM-02 | [02-stage-wire.md](issues/02-stage-wire.md) | SE-CAM-01 | 2 | **done** |
| SE-CAM-03 | [03-tune-handcheck.md](issues/03-tune-handcheck.md) | SE-CAM-02 | 1 | **done** |

**编号约定：** 文件名前缀 `NN-` 与 **SE-CAM-NN** 一一对应。

**建议执行序：** SE-CAM-00 → SE-CAM-01 → SE-CAM-02 → SE-CAM-03

**不做：** 改 PushMap 默认轨跟随；框选怪物；改搜集/刷怪规则。

**样例锁定：** Size `3`～`8`、半径 `8`、滞留 `0.8`、Out `0.2` / In `0.55`（与 v0.84.27 初值相同）。Play Mode 手验由负责人勾选 [03-tune-handcheck.md](issues/03-tune-handcheck.md)。
