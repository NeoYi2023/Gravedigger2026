# SPEC_02 — 游戏概述 / Game Overview（Gravedigger2026）

**关联文档 / Related:** [SPEC_00_Index.md](SPEC_00_Index.md) · [SPEC_03_GameRules.md](SPEC_03_GameRules.md)

---

## 1. 项目基本信息

### 简体中文

| 属性 | 值 |
|------|-----|
| 项目名称 | Gravedigger2026 |
| 游戏引擎 | Unity 2021.3.40f1 |
| 视觉维度 | TBD（当前工程为 Unity 项目，待确认 2D/3D） |
| 发布平台 | TBD |
| Cursor 工作区根 | `F:\CursorGame_Git\Gravedigger2026` |
| Unity 工程路径 | `Gravedigger2026/`（相对工作区根；含 `Assets/`、`ProjectSettings/`） |
| 套件专属 SPEC | `F:\CursorGame_Git\SPECandSKILL\Gravedigger2026\` |
| 当前阶段 | 规则录入中（Demo 外壳 + 关卡 / 挖坟 / 升级与制造框架已写入 SPEC_03） |

### English

| Attribute | Value |
|-----------|-------|
| Project name | Gravedigger2026 |
| Game engine | Unity 2021.3.40f1 |
| Visual dimension | TBD |
| Target platforms | TBD |
| Cursor workspace root | `F:\CursorGame_Git\Gravedigger2026` |
| Unity project path | `Gravedigger2026/` (relative to workspace root) |
| Kit project SPEC | `F:\CursorGame_Git\SPECandSKILL\Gravedigger2026\` |
| Current phase | Rule definition (Demo shell + Level / Dig / UpgradeManufacture framework in SPEC_03) |

---

## 2. 游戏定位

### 简体中文

| 属性 | 状态 | 说明 |
|------|------|------|
| 游戏类型 / Genre | 方向已标 | 局内三段式玩法状态（挖坟 / 升级与制造 / 防守）；完整类型标签 TBD |
| 题材 / Theme | 方向已标 | 掘墓者相关：挖坟、升级与制造、防守；细节 TBD |
| 视角 / Camera | 未定义 | TBD |
| 目标受众 | 未定义 | TBD |
| 单局时长 | 未定义 | TBD |

### English

| Attribute | Status | Notes |
|-----------|--------|-------|
| Genre | Direction noted | Three in-session gameplay states (Dig / UpgradeManufacture / Defend); full genre label TBD |
| Theme | Direction noted | Gravedigger-related: dig, upgrade-manufacture, defend; details TBD |
| Camera | Undefined | TBD |
| Target audience | Undefined | TBD |
| Session length | Undefined | TBD |

---

## 3. 核心玩法概述

### 简体中文

**状态：最小 Demo 已概述；关卡 / 挖坟 / 升级与制造 / 防守框架已指向 SPEC_03**

- **局内主循环：** 三种 `GameplayState` — 挖坟（Dig）、升级与制造（UpgradeManufacture；原 SewRevive）、防守（Defend）。关卡内由阶段玩法类型驱动；壳层手动切换 **TBD**。挖坟见 [SPEC_03 §3.10](SPEC_03_GameRules.md)；升级与制造见 [§3.11](SPEC_03_GameRules.md)；防守见 [§3.12](SPEC_03_GameRules.md)。
- **关卡阶段：** 关卡运作表按阶段编号升序执行；阶段结束（挖坟为有效时长倒计时归零 → DigStageSummary；防守见 §3.12 胜负）→ 阶段结算（若有）→ 下一阶段；无下一阶段 → **胜利结算**；关卡失败与胜利结算互斥。见 [SPEC_03 §3.9](SPEC_03_GameRules.md)。
- **外围 Meta：** 启动后先进入固定 3 槽存档；进档后为壳层，含浮动「工具」面板（设置、关卡占位）。Demo 工具「关卡」仍仅占位。详见 [SPEC_03 §3.3–§3.5](SPEC_03_GameRules.md)。
- **Demo 验收：** [SPEC_03 §3.8](SPEC_03_GameRules.md) D-001～D-004（不含完整挖坟/关卡实现）。

### English

**Status: Minimal Demo overview; Level / Dig / UpgradeManufacture / Defend framework pointed in SPEC_03**

- **In-session loop:** Three `GameplayState`s — Dig, UpgradeManufacture (was SewRevive), Defend. In Level, driven by stage gameplay type; manual shell switch **TBD**. Dig: [SPEC_03 §3.10](SPEC_03_GameRules.md); UpgradeManufacture: [§3.11](SPEC_03_GameRules.md); Defend: [§3.12](SPEC_03_GameRules.md).
- **Level stages:** Level Operation table runs stages ascending; stage end (Dig: effective duration hits 0 → DigStageSummary; Defend: win/lose per §3.12) → stage settlement if any → next stage; no next → **VictorySettlement**; LevelFailure mutually exclusive with VictorySettlement. See [SPEC_03 §3.9](SPEC_03_GameRules.md).
- **Meta shell:** Boot → 3 fixed save slots; after enter, InSaveShell with floating Tools (Settings, Level stubs). Demo Tools Level still stub-only. See [SPEC_03 §3.3–§3.5](SPEC_03_GameRules.md).
- **Demo acceptance:** [SPEC_03 §3.8](SPEC_03_GameRules.md) D-001–D-004 (full Dig/Level implementation out of Demo).

---

## 4. 设计原则（可选）

### 简体中文

- 遵循 [SPEC_01](SPEC_01_Workflow.md) SPEC 优先流程
- 资源编排遵循 [SPEC_04 §13](SPEC_04_Technical.md)（**预制体优先** / SO 配置驱动 / 规则与表现分离）

### English

- Follow SPEC-first workflow in [SPEC_01](SPEC_01_Workflow.md)
- Follow asset authoring in [SPEC_04 §13](SPEC_04_Technical.md) (**Prefab-first**, SO config-driven, rules/presentation separation)
