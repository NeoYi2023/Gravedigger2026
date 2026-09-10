# 离屏刷怪边缘提示（OffScreenSpawnHint / UI-034 / D-090）— Issue 索引

**选定方案：** A — 共享 `OffScreenSpawnHintView`（ScreenSpaceOverlay）

**权威 SPEC：** [SPEC_03 §3.14 / §3.19](../../SPEC_03_GameRules.md) · [SPEC_04 §6 / §9.20b](../../SPEC_04_Technical.md)

**难度：** 2（须拆步）

**Changelog：** SPEC_00 **v0.84.47**（闪红 2 次 + 常亮 2s + scale 1.3；前序落地 v0.84.46）

| ID | 文件 | 依赖 | 难度 | 状态 |
|----|------|------|------|------|
| OSH-00 | [00-spec-close.md](issues/00-spec-close.md) | — | 2 | **done** |
| OSH-01 | [01-constants-view.md](issues/01-constants-view.md) | OSH-00 | 2 | **done** |
| OSH-02 | [02-stage-wire.md](issues/02-stage-wire.md) | OSH-01 | 2 | **done** |
| OSH-03 | [03-handcheck.md](issues/03-handcheck.md) | OSH-02 | 1 | **open**（手验项已按 v0.84.47 更新） |

**编号约定：** 文件名前缀 `NN-` 与 **OSH-NN** 一一对应。

**建议执行序：** OSH-00 → OSH-01 → OSH-02 → OSH-03

**不做：** Defend 接线；改刷怪规则/波次表；Prepare 预览提示；跟踪单个怪移动。
