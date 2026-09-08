# AutoManufacture 尸骸雨 + 法阵复活（UI-016 / D-055）— Issue 索引

**选定方案：** A — Overlay 保留背景/书槽；独立正交相机 + Physics2D 播躯体/法阵/复活

**权威 SPEC：** [SPEC_03 §3.15 §11](../../SPEC_03_GameRules.md) · [SPEC_03 §3.6 UI-016](../../SPEC_03_GameRules.md) · [SPEC_03 §3.8 D-055](../../SPEC_03_GameRules.md) · [SPEC_04 §9.20b](../../SPEC_04_Technical.md) `AutoMfg*` · [SPEC_04 §2/§6/§13](../../SPEC_04_Technical.md)

**难度：** 2（须拆步）

**Changelog：** SPEC_00 **v0.84.29**

| ID | 文件 | 依赖 | 难度 | 状态 |
|----|------|------|------|------|
| AM-RAIN-00 | [00-spec-close.md](issues/00-spec-close.md) | — | 2 | **done** |
| AM-RAIN-01 | [01-constants-art.md](issues/01-constants-art.md) | AM-RAIN-00 | 1 | **done** |
| AM-RAIN-02 | [02-physics-rain.md](issues/02-physics-rain.md) | AM-RAIN-01 | 2 | **done** |
| AM-RAIN-03 | [03-revive-wire.md](issues/03-revive-wire.md) | AM-RAIN-02 | 2 | **done** |

**建议执行序：** AM-RAIN-00 → 01 → 02 → 03

**不做：** 改选料/造兵/套书规则；删士兵传送带代码；落下仓库余料。

**实现摘要：** `AutoManufacturePresentationController.CoPlayBodyRain`；`AmPresentationWorld` / `AmBodyRainPresentation`；常量 `AutoMfg*`；`Resources/UI/Dig`。Play Mode 手验见 [03-revive-wire.md](issues/03-revive-wire.md)。尸骸碰撞（v0.84.33）：可转 OBB + SAT + `AutoMfgColliderInset`。
