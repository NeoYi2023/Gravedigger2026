# AutoManufacture 0 兵（仓库有躯体仍不造）— Issue 索引

**选定方案：** A — 主要手按 BodyLevel 降序尝试；凑不齐近似套件则跳过该件试下一档（AM-CRAFT-02）。诊断日志已先行（AM-CRAFT-01 / 方案 C）。

**权威 SPEC：** [SPEC_03 §3.15](../../SPEC_03_GameRules.md) · [SPEC_03 §3.10 WarehouseHudStats](../../SPEC_03_GameRules.md) · [SPEC_03 §3.8 D-051](../../SPEC_03_GameRules.md) · [SPEC_04 §6](../../SPEC_04_Technical.md)

**难度：** 2（须拆步）

**Changelog：** SPEC_00 **v0.84.34**

**现象：** `L01_S1_Dig_1` + `L01_S2_Dig_2` 后 Warehouse HUD 像能造约 10 兵；进 AM 仅壳层 + Tips「无士兵可制造」；布阵池空。

**诊断：** HUD 种族/职业数 = **主要手件数**，不是可造套数。Dig_01 掉落全是 BodyLevel 1；Dig_02 含 Lv2/Lv3。旧实现取最高主要手当锚点失败即整批停造。

| ID | 文件 | 依赖 | 难度 | 状态 |
|----|------|------|------|------|
| AM-CRAFT-00 | [00-spec-close.md](issues/00-spec-close.md) | — | 2 | **done** |
| AM-CRAFT-01 | [01-stock-dump.md](issues/01-stock-dump.md) | AM-CRAFT-00 | 1 | **done** |
| AM-CRAFT-02 | [02-skip-unusable-primary.md](issues/02-skip-unusable-primary.md) | AM-CRAFT-01 | 2 | **done**（待 Play Mode） |
| AM-CRAFT-03 | [03-secondary-fallback.md](issues/03-secondary-fallback.md) | AM-CRAFT-02 | 2 | todo（仅当仍缺副手） |

**建议执行序：** 00 → 01 → 02（本会话）→ 03 仅当 `ArmSecondary=0` 仍 0 兵

**不做（02）：** 放宽近似品质；第二只主要手当次要手。
