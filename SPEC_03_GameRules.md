# SPEC_03 — 游戏规则 / Game Rules（Gravedigger2026）

**关联文档 / Related:** [SPEC_00_Index.md](SPEC_00_Index.md) · [SPEC_02_GameOverview.md](SPEC_02_GameOverview.md) · [SPEC_04_Technical.md](SPEC_04_Technical.md)

> 最小 Demo 外围壳已录入；关卡阶段流水线、挖坟、升级与制造、防守框架见 §3.9–§3.12。规则录入阶段禁止写 Unity 代码。

---

## 3.1 术语与实体

### 简体中文

| 术语 (EN) | 中文 | 定义 |
|-----------|------|------|
| GameplayState | 玩法状态 | 局内主状态枚举：`Dig`（挖坟）、`UpgradeManufacture`（升级与制造；原占位名 `SewRevive`）、`Defend`（防守）。关卡运行时由当前阶段的玩法类型决定（§3.9）；壳层默认占位仍为 Dig。 |
| SaveSlot | 存档槽 | 固定数量的本地存档位；本版 **3 槽**（索引 0–2）。空槽可新建，占用槽可进入或删除。 |
| InSaveShell | 进档壳层 | 选定存档进入后的常驻壳：承载当前 `GameplayState` 占位与浮动「工具」入口。 |
| ToolsPanel | 工具面板 | Demo 调试/设置壳层 UI；由浮动「工具」按钮打开。本期含「设置」「关卡」占位，其余后续补充。 |
| Level | 关卡 | 由「关卡运作表」定义的多阶段流程实体；每阶段指定玩法类型与玩法配置 ID（§3.9）。Demo 工具面板仍仅占位入口；场景绑定 **TBD**。 |
| LevelOperation | 关卡运作 | 关卡运作表一行：关卡 ID + 阶段编号 + 玩法类型 + 玩法配置 ID。 |
| DigGameplayConfig | 挖坟配置 | 挖坟配置表一行：时长、开局坟数、过程生成速率、品质权重（零权重项剔除）等（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| Grave | 坟墓 | 挖坟地图上的可生成实体；带坟墓品质 ID；落点须避开已有坟与障碍物。 |
| VictorySettlement | 胜利结算 | 关卡**最后一阶段**结束后触发的关卡级结算反馈。 |
| DigMap | 挖坟地图 | 表现上为旋转 45° 正方形（菱形外观）组成的地图；**逻辑层为整体可放置空间，非格子网格**。 |
| Digger | 挖坟主角 | 挖坟阶段在地图中心点生成的角色；待机 / 挖坟循环动画由场上是否有坟正在被挖驱动（§3.10）。 |
| DigAction | 挖掘流程 | 圆圈光标在坟上停留 ≥0.2s 触发；坟上播放 `DigActionDuration` 挖掘帧动画后结算扣血；该坟挖掘中不可重复触发（§3.10）。 |
| DigObstacle | 挖坟障碍物 | Dig 阶段仅两类：Digger 与未消除 Grave；圆形障碍半径在各自预制体上配置（§3.10）。 |
| DigProtagonistCapabilities | 挖坟主角能力 | 存档主角派生：挖坟伤害、挖坟阶段时长加成、时长缩短和、光标半径、可挖品质集合；由科技解锁/升级写入（§3.10）。 |
| GraveHP | 坟墓血量 | 坟墓当前/最大生命；maxHP 来自坟墓品质定义表；扣至 0 触发挖掘成功与奖励（§3.10）。 |
| GraveIconStyle | 坟墓图标样式 | 按剩余 HP% 切换：>65% 样式1；30%–65% 样式2；<30% 样式3（§3.10）。 |
| GraveQualityConfig | 坟墓品质定义表 | 品质 ID → maxHP、掉落等；被挖坟权重引用（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| DigReward | 挖掘奖励 | 坟 HP 归 0 时在成功动画中心生成的奖励图标；飞向主角到达后入账并消失（§3.10）。 |
| DigStageSummary | 挖坟阶段汇总 | Dig 有效时长归零后弹出的汇总弹窗：仅展示本阶段已获奖励按类型汇总，无额外发放（§3.10，UI-011）。 |
| Warehouse | 仓库 | 按存档槽持久的材料仓库；不限格数与存储时长；材料按类型堆叠上限 10000（§3.10）。 |
| SpiritEssence | 精魂 | 货币；挖坟获得（LootDrop 保留 Id + 堆叠超限自动兑换）；制造战士时消耗（§3.10、§3.11）。 |
| MaterialConfig | 材料配置表 | MaterialId → AutoConvert、AppearanceIconId、AssetPath、WarehouseQualityOutlineId；堆叠超限时按 AutoConvert 兑精魂（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| CurrencyConfig | 货币配置表 | CurrencyId → 外观图/素材路径/仓库品质外轮廓；精魂保留 Id=`Spirit`（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| UpgradeManufacture | 升级与制造 | 阶段玩法类型（原占位 `SewRevive`）：角色升级 + 制造战士 + 战斗布阵；见 §3.11。 |
| Experience | 经验 | Defend **阶段胜利**结算时加算至 `LifetimeExperience`；关卡失败不入账；达累计阈值升级（§3.11）。 |
| LifetimeExperience | 生涯累计经验 | 存档持有的经验总值；只增不因升级减少；与 `ProtagonistLevelConfig.RequiredTotalExperience` 比较（§3.11）。 |
| ProtagonistLevelConfig | 主角升级配置表 | 等级行：累计经验阈值、预留解锁功能、科技点奖励、控制力上限、主角生命上限（§3.11，[SPEC_04 §9.8](SPEC_04_Technical.md)）。 |
| TechPoint | 科技点数 | 升级获得；用于科技树（完整树另专题）（§3.11）。 |
| Material | 材料 | 挖坟入仓库；造战士消耗（与精魂并列；配方另专题）（§3.10、§3.11）。 |
| Warrior | 战士 | 制造产出的 **独立实例**（ID/血量等）；防守上阵（§3.11）。 |
| ControlPower | 控制力 | 主角属性；上阵占用；本版上限取当前等级行 `ControlPowerCap`（科技加成另专题）；超额失控（§3.11）。 |
| LossOfControl | 失控 | 上阵占用超过控制力上限时，按超额档次在战斗中生效（细则 TBD）。 |
| BattleFormation | 战斗布阵 | 安排战士上阵；持久化战士 ID、位置、剩余血量；可在 §3.11 与 Defend `Prepare` 编辑同一套数据（§3.11、§3.12）。 |
| Defend | 防守 | 关卡玩法类型 / `GameplayState`：准备态→开战→战斗；见 §3.12。 |
| DefendPhase | 防守子状态 | 阶段内子状态：`Prepare`（准备）→ `Combat`（战斗中）→ `Ended`（已结束）。 |
| StartBattle | 开战 | 准备态 UI 按钮；点击后进入 `Combat` 并部署单位（§3.12）。 |
| BattleMap | 战斗地图 | 防守阶段地图；逻辑为连续可走空间（非格子）；与 DigMap 分离（§3.12）。 |
| BattleProtagonist | 战斗主角 | 战斗中地图中央的主角实体；与挖坟 `Digger` 区分（§3.12）。 |
| Monster | 怪物 | 防守战斗敌方单位；从地图空间外刷出（刷怪细则 TBD）。 |
| Wave | 波次 | 防守刷怪的波次单位；最后一波清场为阶段胜利条件之一（§3.12）。 |
| AttackPriority | 攻击优先级 | 怪物选目标的预设优先级规则（具体排序表 TBD）。 |
| TargetRetargetInterval | 目标修正间隔 | 怪物重算可攻击目的地的间隔；暂定 **1s**，可配置（§3.12）。 |
| LevelFailure | 关卡失败 | 战斗中主角阵亡等触发的关卡级失败；与 VictorySettlement 互斥（§3.12）。 |

新增术语同步一行到 [CONTEXT.md](CONTEXT.md)。

### English

| Term (EN) | ZH | Definition |
|-----------|-----|------------|
| GameplayState | 玩法状态 | In-session main state enum: `Dig`, `UpgradeManufacture` (was placeholder `SewRevive`), `Defend`. During a Level, set by the current stage's gameplay type (§3.9); shell default placeholder remains Dig. |
| SaveSlot | 存档槽 | Fixed local slots; this version **3 slots** (indices 0–2). Empty → create; occupied → enter or delete. |
| InSaveShell | 进档壳层 | Persistent shell after entering a save: hosts current `GameplayState` placeholder and floating Tools entry. |
| ToolsPanel | 工具面板 | Demo settings/debug shell UI opened by floating Tools. This version: Settings + Level stubs; more later. |
| Level | 关卡 | Multi-stage flow defined by Level Operation table; each stage has gameplay type + config ID (§3.9). Demo Tools still stub-only; scene binding **TBD**. |
| LevelOperation | 关卡运作 | One Level Operation row: LevelId + StageNumber + GameplayType + GameplayConfigId. |
| DigGameplayConfig | 挖坟配置 | One Dig config row: duration, initial grave count, spawn rate, quality weights (zero-weight entries dropped) (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| Grave | 坟墓 | Spawnable Dig-map entity with Grave Quality Id; placement must avoid existing graves and obstacles. |
| VictorySettlement | 胜利结算 | Level-level settlement feedback after the **last** stage ends. |
| DigMap | 挖坟地图 | Visually composed of 45°-rotated squares (diamond look); **logically one continuous placeable space, not a cell grid**. |
| Digger | 挖坟主角 | Avatar spawned at DigMap center when Dig stage starts; idle vs looping dig anim driven by whether ≥1 grave is being dug (§3.10). |
| DigAction | 挖掘流程 | Circle cursor dwell ≥0.2s on a grave triggers dig; dig frame anim for `DigActionDuration` then damage resolve; busy grave cannot re-trigger (§3.10). |
| DigObstacle | 挖坟障碍物 | Dig-stage obstacles only: Digger and uncleared Graves; circle obstacle radius on each Prefab (§3.10). |
| DigProtagonistCapabilities | 挖坟主角能力 | Save-slot protagonist derived stats: dig damage, Dig stage duration bonus, duration-reduction sum, cursor radius, diggable quality set; written by tech unlock/upgrade (§3.10). |
| GraveHP | 坟墓血量 | Current/max HP; maxHP from GraveQualityConfig; 0 HP → dig success + reward (§3.10). |
| GraveIconStyle | 坟墓图标样式 | By remaining HP%: >65% style1; 30%–65% style2; <30% style3 (§3.10). |
| GraveQualityConfig | 坟墓品质定义表 | Quality Id → maxHP, loot, etc.; referenced by Dig spawn weights (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| DigReward | 挖掘奖励 | Reward icon spawned at dig-success anim center when HP hits 0; flies to Digger, credits on arrival, then disappears (§3.10). |
| DigStageSummary | 挖坟阶段汇总 | Popup after Dig effective duration hits 0: aggregate rewards earned this stage by type only; no extra grants (§3.10, UI-011). |
| Warehouse | 仓库 | Per-SaveSlot material warehouse; unlimited slots and retention; materials stack by type up to 10000 (§3.10). |
| SpiritEssence | 精魂 | Currency; from Dig (LootDrop reserved Id + stack overflow AutoConvert); spent when manufacturing warriors (§3.10, §3.11). |
| MaterialConfig | 材料配置表 | MaterialId → AutoConvert, AppearanceIconId, AssetPath, WarehouseQualityOutlineId; overflow converts to SpiritEssence via AutoConvert (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| CurrencyConfig | 货币配置表 | CurrencyId → appearance icon / asset path / warehouse quality outline; Spirit reserved Id=`Spirit` (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| UpgradeManufacture | 升级与制造 | Stage gameplay type (formerly `SewRevive`): level-up + manufacture warriors + battle formation; §3.11. |
| Experience | 经验 | Added to `LifetimeExperience` on **Defend stage victory** settlement; not credited on LevelFailure; cumulative threshold → level up (§3.11). |
| LifetimeExperience | 生涯累计经验 | Save-slot total Exp; never decreases on level-up; compared to `ProtagonistLevelConfig.RequiredTotalExperience` (§3.11). |
| ProtagonistLevelConfig | 主角升级配置表 | Level rows: cumulative Exp threshold, reserved unlock features, TechPoint reward, ControlPower cap, protagonist MaxHP (§3.11, [SPEC_04 §9.8](SPEC_04_Technical.md)). |
| TechPoint | 科技点数 | Granted on level-up; spent on tech tree (full tree later) (§3.11). |
| Material | 材料 | Credited to Warehouse from Dig; spent to manufacture (alongside SpiritEssence; recipes later) (§3.10, §3.11). |
| Warrior | 战士 | Manufactured **instance** (Id/HP/…); deployed in Defend (§3.11). |
| ControlPower | 控制力 | Protagonist attribute; deploy cost; this version cap = current level row `ControlPowerCap` (tech bonus later); overflow → LossOfControl (§3.11). |
| LossOfControl | 失控 | When deployed control cost exceeds cap, tiered battle effects apply (details TBD). |
| BattleFormation | 战斗布阵 | Assign warriors to battlefield; persists warrior Id, position, remaining HP; editable in §3.11 and Defend `Prepare` on the same dataset (§3.11, §3.12). |
| Defend | 防守 | Stage type / `GameplayState`: Prepare → StartBattle → Combat; §3.12. |
| DefendPhase | 防守子状态 | In-stage phases: `Prepare` → `Combat` → `Ended`. |
| StartBattle | 开战 | Prepare-phase UI button; click → `Combat` and deploy units (§3.12). |
| BattleMap | 战斗地图 | Defend-stage map; continuous walkable space (not a grid); separate from DigMap (§3.12). |
| BattleProtagonist | 战斗主角 | Protagonist entity at BattleMap center; distinct from Dig `Digger` (§3.12). |
| Monster | 怪物 | Defend enemy unit; spawns outside map space (spawn rules TBD). |
| Wave | 波次 | Spawn-wave unit; clearing the last wave is part of stage victory (§3.12). |
| AttackPriority | 攻击优先级 | Preset monster target-selection priority (ordering table TBD). |
| TargetRetargetInterval | 目标修正间隔 | Interval to recompute attackable destination; provisional **1s**, configurable (§3.12). |
| LevelFailure | 关卡失败 | Level-level failure (e.g. protagonist death in Combat); mutually exclusive with VictorySettlement (§3.12). |

Sync glossary rows to [CONTEXT.md](CONTEXT.md).

---

## 3.2 玩家输入与操作（占位）

### 简体中文

**状态：部分定义（Meta 壳）**

| 场景 | 操作 | 说明 |
|------|------|------|
| 存档选择 | 点击空槽「新建」 | 占用该槽并进入进档壳层 |
| 存档选择 | 点击占用槽「进入」 | 加载该槽并进入进档壳层 |
| 存档选择 | 点击占用槽「删除」 | 须二次确认后清空槽位，停留在存档界面 |
| 进档壳层 | 点击浮动「工具」 | 打开 / 关闭工具面板 |
| 工具面板 | 点击「设置」「关卡」 | 进入占位页或等价反馈（Toast / 空页） |
| 三玩法状态 | — | **TBD**（后续专门补充） |

### English

**Status: Partially defined (Meta shell)**

| Context | Action | Notes |
|---------|--------|-------|
| Save select | Create on empty slot | Occupy slot and enter InSaveShell |
| Save select | Enter occupied slot | Load slot and enter InSaveShell |
| Save select | Delete occupied slot | Confirm, then clear; stay on save UI |
| InSaveShell | Floating Tools | Open / close ToolsPanel |
| ToolsPanel | Settings / Level | Placeholder page or equivalent feedback |
| Three gameplay states | — | **TBD** |

---

## 3.3 核心循环

### 简体中文

| 阶段 | 说明 |
|------|------|
| 1. 启动 | 进入存档选择界面（非直接进局） |
| 2. Meta 存档 | 对 3 个固定槽执行新建 / 选择进入 / 删除（见 §3.4） |
| 3. 进档壳层 | 进入后默认 `GameplayState = Dig`（挖坟占位）；显示浮动「工具」（§3.5） |
| 4. 玩法状态 | 当前状态以占位表现可识别；关卡内由阶段玩法类型驱动（§3.9）；壳层内手动切换 **TBD** |
| 5. 关卡 | 规则见 §3.9；Demo 工具面板仍仅占位，不驱动真实关卡加载 |

交叉引用：[SPEC_02 §3](SPEC_02_GameOverview.md)。

### English

| Stage | Description |
|-------|-------------|
| 1. Boot | Open save-select UI (not direct into gameplay) |
| 2. Meta saves | Create / enter / delete on 3 fixed slots (§3.4) |
| 3. InSaveShell | Default `GameplayState = Dig`; show floating Tools (§3.5) |
| 4. Gameplay states | Placeholder must identify current state; in Level, driven by stage gameplay type (§3.9); manual shell switch **TBD** |
| 5. Level | Rules in §3.9; Demo Tools still stub-only (no real Level load) |

Cross-ref: [SPEC_02 §3](SPEC_02_GameOverview.md).

---

## 3.4 Meta / 存档

### 简体中文

**槽位规则**

| 规则 | 值 |
|------|-----|
| 槽位数量 | 固定 **3**（索引 0、1、2） |
| 空槽 | 可「新建」→ 标记占用并进入进档壳层 |
| 占用槽 | 可「选择进入」或「删除」 |
| 删除 | **必须二次确认**；确认后槽变空；不可恢复（本版） |
| 持久化 | 本地、按槽索引；至少持久化「是否占用」。完整存档 schema **TBD**（见 [SPEC_04 §6](SPEC_04_Technical.md)） |

**槽位展示（最小）**

| 字段 | 要求 |
|------|------|
| 槽号 | 必须（1–3 或 0–2，UI 一致即可） |
| 是否占用 | 必须 |
| 显示名 / 时间戳 | 可选；未定时标 TBD |

### English

**Slot rules**

| Rule | Value |
|------|-------|
| Slot count | Fixed **3** (indices 0, 1, 2) |
| Empty | Create → mark occupied and enter InSaveShell |
| Occupied | Enter or Delete |
| Delete | **Confirm required**; slot becomes empty; no undo (this version) |
| Persistence | Local, by slot index; at least occupied flag. Full schema **TBD** ([SPEC_04 §6](SPEC_04_Technical.md)) |

**Minimal display**

| Field | Requirement |
|-------|-------------|
| Slot id | Required |
| Occupied | Required |
| Display name / timestamp | Optional; TBD if unused |

---

## 3.5 工具面板

### 简体中文

| 规则 | 说明 |
|------|------|
| 可见时机 | 仅在进档壳层常驻浮动「工具」按钮 |
| 打开 / 关闭 | 点击按钮切换工具面板 |
| 本期条目 | **设置**（占位）、**关卡**（占位） |
| 关卡语义 | 工具「关卡」入口 **不等于** 直接切换三种 `GameplayState`；关卡多阶段规则见 §3.9。Demo 仍仅占位，不加载真实关卡 |
| 后续条目 | 标 TBD，不纳入本版 Demo 验收 |

点击「设置」或「关卡」：进入空页或 Toast 等等价占位反馈即可。

### English

| Rule | Notes |
|------|-------|
| Visibility | Floating Tools only inside InSaveShell |
| Open / close | Toggle ToolsPanel via button |
| This version | **Settings** (stub), **Level** (stub) |
| Level meaning | Tools Level entry is **not** a direct three-state switch; multi-stage Level rules in §3.9. Demo still stub-only |
| Future entries | TBD; out of this Demo acceptance |

Settings / Level click → empty page or Toast-equivalent stub.

---

## 3.6 UI 清单

### 简体中文

| ID | 名称 | 状态 | 说明 |
|----|------|------|------|
| UI-001 | 存档选择 | 已定义（Demo） | 3 槽：新建 / 进入 / 删除（含确认） |
| UI-002 | 浮动工具按钮 | 已定义（Demo） | 进档壳层常驻 |
| UI-003 | 工具面板 | 已定义（Demo） | 含设置、关卡占位入口 |
| UI-004 | 挖坟占位屏 | 占位 | 可识别当前为 Dig |
| UI-005 | 升级与制造占位屏 | 占位 | 可识别当前为 UpgradeManufacture（原 SewRevive） |
| UI-006 | 防守占位屏 | 占位 | 可识别当前为 Defend；完整 UI 见 §3.12 |
| UI-007 | 设置占位页 | 占位 | 自工具面板进入 |
| UI-008 | 关卡占位页 | 占位 | 自工具面板进入；非玩法三态 |
| UI-009 | 开战按钮 | 规则已定义 | Defend 准备态；点击 → StartBattle（§3.12）；本 Demo **不实现** |
| UI-010 | 升级与制造主屏 | 已定义（规则库） | 同屏三区（升级/制造/布阵）+ 底部「完成」；细则控件 TBD |
| UI-011 | 挖坟阶段汇总 | 已定义（规则库） | DigStageSummary：本阶段已获奖励按类型汇总；无额外发放；确认后接 §3.9；本 Demo **不实现** |

### English

| ID | Name | Status | Notes |
|----|------|--------|-------|
| UI-001 | Save select | Defined (Demo) | 3 slots: create / enter / delete (confirm) |
| UI-002 | Floating Tools button | Defined (Demo) | InSaveShell |
| UI-003 | ToolsPanel | Defined (Demo) | Settings + Level stubs |
| UI-004 | Dig placeholder | Placeholder | Identifiable Dig |
| UI-005 | UpgradeManufacture placeholder | Placeholder | Identifiable UpgradeManufacture (was SewRevive) |
| UI-006 | Defend placeholder | Placeholder | Identifiable Defend; full UI in §3.12 |
| UI-007 | Settings stub page | Placeholder | From Tools |
| UI-008 | Level stub page | Placeholder | From Tools; not gameplay states |
| UI-009 | StartBattle button | Rules defined | Defend Prepare; click → StartBattle (§3.12); **not** in this Demo |
| UI-010 | UpgradeManufacture main screen | Defined (rules library) | Three panels + bottom Complete; in-panel widgets TBD |
| UI-011 | Dig stage summary | Defined (rules library) | DigStageSummary: aggregate rewards earned this Dig stage; no extra grants; confirm → §3.9; **not** in this Demo |

---

## 3.7 玩法状态占位

### 简体中文

进档后须存在可识别的当前状态表现；默认进入 **挖坟（Dig）**。挖坟完整规则见 §3.10；升级与制造框架见 §3.11；防守框架见 §3.12。

| 状态 | 中文 | Demo 要求 | 范围 / 输入 / 胜负 |
|------|------|-----------|-------------------|
| Dig | 挖坟 | 可识别占位 | 规则见 §3.10（含交互 / 扣血 / 奖励 / 无胜负 / DigStageSummary；本 Demo **不实现**） |
| UpgradeManufacture | 升级与制造 | 可识别占位 | 框架见 §3.11（原占位名 SewRevive；细则后续补充；本 Demo **不实现**） |
| Defend | 防守 | 可识别占位 | 框架见 §3.12（准备/开战/寻路/胜负；刷怪细则 TBD；本 Demo **不实现**） |

壳层内手动切换玩法状态方式 **TBD**（不得将工具「关卡」占位隐式等同为状态切换）。关卡运行时由阶段玩法类型驱动，见 §3.9。

### English

After enter, current state must be identifiable; default **Dig**. Full Dig rules: §3.10. UpgradeManufacture framework: §3.11. Defend framework: §3.12.

| State | ZH | Demo requirement | Scope / input / win-lose |
|-------|-----|------------------|---------------------------|
| Dig | 挖坟 | Identifiable placeholder | Rules in §3.10 (incl. dig interaction / HP / rewards / no win-lose / DigStageSummary; **not** implemented in this Demo) |
| UpgradeManufacture | 升级与制造 | Identifiable placeholder | Framework in §3.11 (was SewRevive; details later; **not** implemented in this Demo) |
| Defend | 防守 | Identifiable placeholder | Framework in §3.12 (Prepare/StartBattle/pathing/win-lose; spawn details TBD; **not** implemented in this Demo) |

Manual shell state switch is **TBD** (must not equate Tools Level stub to a state switch). During Level, stage gameplay type drives state — §3.9.

---

## 3.8 Demo 验收标准

### 简体中文

**状态：已定义（最小 Demo 外围壳）**

| ID | 验收项 | 优先级 | 状态 |
|----|--------|--------|------|
| D-001 | 可打开存档界面，对 3 槽执行新建 / 选择进入 / 删除（删除含二次确认） | P0 | 待实现 |
| D-002 | 进入存档后可见浮动「工具」，可打开 / 关闭工具面板 | P0 | 待实现 |
| D-003 | 工具面板可见「设置」「关卡」占位入口；点击可进入空页或 Toast | P0 | 待实现 |
| D-004 | 进档后可识别当前处于三种玩法状态之一的占位表现；默认进档为挖坟占位 | P0 | 待实现 |

**Demo 范围外：**

- 挖坟 / 升级与制造 / 防守的完整规则实现、输入与胜负（规则正文见 §3.9–§3.12，本 Demo **不实现**）
- 真实关卡加载、关卡运作表驱动、关卡与场景绑定
- 工具面板「设置」「关卡」以外的后续功能
- 完整存档序列化 schema（超出「槽占用」最小持久化）
- 完整 polish、未写入本表的需求

实现边界对照：[SPEC_04 §6](SPEC_04_Technical.md)。

### English

**Status: Defined (minimal Demo Meta shell)**

| ID | Criterion | Priority | Status |
|----|-----------|----------|--------|
| D-001 | Save UI with 3 slots: create / enter / delete (delete confirms) | P0 | Pending |
| D-002 | After enter: floating Tools; open / close ToolsPanel | P0 | Pending |
| D-003 | Tools shows Settings + Level stubs; click → empty page or Toast | P0 | Pending |
| D-004 | After enter: identifiable gameplay-state placeholder; default Dig | P0 | Pending |

**Out of Demo scope:**

- Full Dig / UpgradeManufacture / Defend implementation, input, win-lose (rules text in §3.9–§3.12; **not** implemented in this Demo)
- Real Level load, Level Operation table drive, scene binding
- Tools entries beyond Settings / Level stubs
- Full save schema beyond minimal occupied flag
- Full polish; anything not in this table

Boundary: [SPEC_04 §6](SPEC_04_Technical.md).

---

## 3.9 关卡阶段流水线

### 简体中文

**状态：已定义（规则库；非本版 Demo 实现）**

关卡由「关卡运作表」驱动。同一 `关卡ID` 的多行按 `阶段编号` **升序**执行。每阶段以 `玩法类型` 设置当前 `GameplayState`，并以 `玩法配置ID` 加载对应玩法配置（挖坟见 §3.10；升级与制造见 §3.11；防守见 §3.12；配置编码见 [SPEC_04 §9](SPEC_04_Technical.md)）。

**表 1 — 关卡运作表字段（规则语义）**

| 字段 | 说明 |
|------|------|
| 关卡ID | 关卡标识；同 ID 多行组成该关的全部阶段 |
| 阶段编号 | 同关卡内执行顺序（升序） |
| 玩法类型 | 本阶段玩法（如 `Dig` / `UpgradeManufacture` / `Defend`）；映射到 `GameplayState` |
| 玩法配置ID | 指向该玩法配置表行（挖坟 → 挖坟配置表；防守 → 防守配置表） |

**阶段流转**

1. 进入关卡：按 `关卡ID` 加载关卡运作行 → 按阶段编号升序排序。
2. 运行当前阶段：应用玩法类型与玩法配置 ID。
3. 阶段结束：由该玩法的结束条件触发。挖坟阶段：有效挖坟时长倒计时归零 → 本阶段结束（§3.10；**无胜负**）。升级与制造阶段：玩家主动确认「完成 / 进入下一阶段」→ 本阶段结束（§3.11；无强制倒计时；**无独立阶段结算**）。防守阶段：见 §3.12（清场胜利 → 阶段结束；战斗主角阵亡 → **关卡失败**，不进入下一阶段）。
4. 阶段结算：若该玩法定义了阶段结算则触发（挖坟：**DigStageSummary** 仅汇总本阶段已获奖励、无额外发放，玩家确认后继续；升级与制造 **跳过**；防守阶段胜利时至少含 **经验入账**，其余 **TBD**），再进入下一阶段。
5. **无下一阶段**（已是最后一阶段结束后）：触发关卡级 **胜利结算（VictorySettlement）**。
6. **关卡失败（LevelFailure）**：任意阶段触发关卡失败（如 Defend 中战斗主角阵亡）→ **立即结束关卡**；**不**触发 VictorySettlement / **无关卡结算奖励**；**不**入账本阶段 Defend 经验；此前已入账的 Experience、材料/精魂、战士、TechPoint 等 **不扣除**；失败结算 UI / 字段 **TBD**。

```
EnterLevel
  → Load LevelOperation rows by LevelId
  → Sort by StageNumber ascending
  → Run stage (GameplayType + GameplayConfigId)
  → Stage end condition
       Dig: EffectiveDigDuration countdown = 0 (no win/lose)
       UpgradeManufacture: player confirm
       Defend: stage victory per §3.12  OR  LevelFailure → abort Level
  → If LevelFailure → no VictorySettlement / no stage Exp credit; keep already-owned; LevelFailure settlement UI TBD; stop
  → Stage settlement if any
       Dig: DigStageSummary (aggregate only; no extra grants) → player confirm
       UpgradeManufacture: skip
       Defend: at least Experience credit; other TBD
  → If next stage exists → run next
  → Else → VictorySettlement
```
### English

**Status: Defined (rules library; not this Demo implementation)**

A Level is driven by the Level Operation table. Rows sharing a `LevelId` run in ascending `StageNumber`. Each stage sets `GameplayState` from `GameplayType` and loads config via `GameplayConfigId` (Dig: §3.10; UpgradeManufacture: §3.11; Defend: §3.12; encodings: [SPEC_04 §9](SPEC_04_Technical.md)).

**Table 1 — Level Operation fields (rules semantics)**

| Field | Notes |
|-------|-------|
| LevelId | Level id; multiple rows = all stages |
| StageNumber | Execution order within the Level (ascending) |
| GameplayType | Stage mode (e.g. `Dig` / `UpgradeManufacture` / `Defend`) → `GameplayState` |
| GameplayConfigId | Points to that mode's config row (Dig → DigGameplayConfig; Defend → DefendGameplayConfig) |

**Stage flow**

1. Enter Level: load rows by LevelId → sort by StageNumber ascending.
2. Run current stage: apply GameplayType + GameplayConfigId.
3. Stage end: per-mode end condition. Dig: effective Dig duration countdown hits 0 → stage ends (§3.10; **no win/lose**). UpgradeManufacture: player confirms "Complete / Next stage" → stage ends (§3.11; no forced countdown; **no independent stage settlement**). Defend: see §3.12 (clear-wave victory → stage end; BattleProtagonist death → **LevelFailure**, no next stage).
4. Stage settlement: if the mode defines one, run it (Dig: **DigStageSummary** — aggregate rewards earned this stage only, no extra grants, then player confirm; UpgradeManufacture **skips**; Defend victory at least **credits Experience**, other content **TBD**), then advance.
5. **No next stage** (after last stage ends): trigger level-level **VictorySettlement**.
6. **LevelFailure**: any stage that triggers LevelFailure (e.g. BattleProtagonist death in Defend) → **abort the Level immediately**; **no** VictorySettlement / **no level settlement rewards**; **no** Defend stage Exp credit for the failed stage; already-owned Experience, materials/SpiritEssence, warriors, TechPoints, etc. are **not clawed back**; failure settlement UI/fields **TBD**.

```
EnterLevel
  → Load LevelOperation rows by LevelId
  → Sort by StageNumber ascending
  → Run stage (GameplayType + GameplayConfigId)
  → Stage end condition
       Dig: EffectiveDigDuration countdown = 0 (no win/lose)
       UpgradeManufacture: player confirm
       Defend: stage victory per §3.12  OR  LevelFailure → abort Level
  → If LevelFailure → no VictorySettlement / no stage Exp credit; keep already-owned; LevelFailure settlement UI TBD; stop
  → Stage settlement if any
       Dig: DigStageSummary (aggregate only; no extra grants) → player confirm
       UpgradeManufacture: skip
       Defend: at least Experience credit; other TBD
  → If next stage exists → run next
  → Else → VictorySettlement
```

---

## 3.10 挖坟（Dig）玩法

### 简体中文

**状态：已定义（生成 / 有效时长 / 玩家挖掘交互与奖励入账 / 障碍物几何 / 挖坟四项科技绑定能力 / 无胜负 / DigStageSummary；完整科技节点表仍 TBD）**

当关卡当前阶段 `玩法类型 = Dig` 时，使用「挖坟配置表」中对应 `玩法配置ID` 的行。坟墓 `maxHP` 与掉落内容来自「坟墓品质定义表」（[SPEC_04 §9.3](SPEC_04_Technical.md)）。

**地图**

| 规则 | 说明 |
|------|------|
| 表现 | 由旋转 45° 的正方形组成，外观呈菱形拼贴 |
| 逻辑 | **整体可放置空间**，不是一堆格子；落点在连续空间中选取 |
| 可放置 | 候选位置上不得与任何 **挖坟障碍物（DigObstacle）** 的圆形区域相交 |

**障碍物（DigObstacle）**

本阶段障碍物 **仅** 以下两类（暂不引入其他类型）：

| 类型 | 说明 |
|------|------|
| Digger | 地图中心的挖坟主角；障碍区域大小在 **Digger 预制体**上配置（圆形障碍半径，世界单位） |
| Grave | 已生成且尚未消除（HP > 0）的坟；障碍区域大小在 **该品质对应坟预制体**上配置（每种坟品质专属预制体；圆形障碍半径） |

- 规则层用圆形障碍半径做相交判定：候选落点与任一障碍圆相交 → 不可放置。
- 坟 HP 归 0 消除后，其障碍 **立即失效**。
- Prefab 路径约定见 [SPEC_04 §9 / §13](SPEC_04_Technical.md)。

**表 2 — 挖坟配置表字段（规则语义）**

| 字段 | 说明 |
|------|------|
| 玩法配置ID | 与关卡运作表关联 |
| 关卡时长限制 | **基础**时长（秒）；实际倒计时用 **有效挖坟时长**（见下） |
| 开局基础生成坟墓数量 | 开局独立加权随机的次数 N |
| 倒计时过程中生成坟墓速率 | 每 N 秒生成 M 个（编码见 [SPEC_04 §9](SPEC_04_Technical.md)） |
| 坟墓出现概率权重 | 各坟墓品质 ID 的出现权重；`Weight = 0` 项剔除（编码与通用规则见 SPEC_04 §9） |

**有效挖坟时长**

| 规则 | 说明 |
|------|------|
| 公式 | `EffectiveDigDuration = DigGameplayConfig.LevelDurationSeconds + DigStageDurationBonus`（秒，加法） |
| 科技来源 | `DigStageDurationBonus` 由 **存档主角** 科技树解锁/升级写入 `DigProtagonistCapabilities`；节点 / 数值 **另专题** |
| 倒计时 | 进入 Dig 阶段时按有效时长启动倒计时；归零 → 阶段结束（见下） |

**开局生成**

1. 读取「开局基础生成坟墓数量」= N。
2. **独立进行 N 次**尝试：每次先按 [SPEC_04 §9 加权字段通用规则](SPEC_04_Technical.md) 过滤 `GraveSpawnWeights`（`Weight = 0` 剔除）；若有效列表为空 → **放弃该次生成**（不抽品质、不生成实体）。否则按有效权重加权抽取一个坟墓品质 ID。
3. 每次抽中后，在地图可放置区域内随机选位置生成一座坟墓；该坟 `maxHP` / 当前 HP 按品质定义表初始化。
4. 落点采样须避开 Digger 与未消除 Grave 的圆形障碍；单次生成最多重试 **32** 次，仍失败则 **放弃该次生成**。

**过程生成**

- 倒计时进行中，按「倒计时过程中生成坟墓速率」：每 N 秒尝试生成 M 座坟。
- 每一座仍：过滤权重 →（空有效列表则放弃）→ 加权抽品质 ID → 可放置区随机落点（同上重试规则）→ 按品质表初始化 HP。
- 与开局共用同一套权重字段与零权重剔除规则。

**主角（Digger）**

| 规则 | 说明 |
|------|------|
| 生成 | 进入挖坟阶段时，在 **地图中心点** 生成主角（Digger） |
| 默认动画 | 待机动作 |
| 挖坟动画 | 当场上 **至少有 1 座坟** 处于「挖掘中」时，主角在 **原地** 播放可循环的挖坟动画；否则回到待机 |

**挖坟主角能力（DigProtagonistCapabilities）**

绑定在 **存档主角** 上，由科技树解锁/升级写入（**完整科技节点表与 TechPoint 消耗另专题**；本批只定能力语义与算法）：

| 能力 | 说明 |
|------|------|
| DigDamage | 单次 DigAction 结束时对该坟的扣血数值；初始默认值来自默认解锁科技项 |
| DigDurationReductionSum | 所有已解锁「缩短单次挖坟时长」科技效果之和（秒） |
| DigCursorRadius | 圆圈光标半径（世界单位） |
| DiggableQualityIds | 已解锁、可触发挖掘的坟墓品质 ID 集合 |
| DigStageDurationBonus | 挖坟阶段有效时长的科技加成（秒，加法；见「有效挖坟时长」） |

**单次挖掘时长（挖坟单次速度）：**

`DigActionDuration = max(0.1, BaseDigDuration − DigDurationReductionSum)`，其中 `BaseDigDuration = 0.8`（秒）。最短挖坟时间不得小于 **0.1s**。

（与「有效挖坟时长」不同：后者是阶段倒计时总长；本项是单次 DigAction 动画/结算时长。）

**光标与挖掘触发**

| 规则 | 值 |
|------|-----|
| 光标形态 | 进入挖坟阶段后，鼠标指针变为「圆圈范围」；半径 = `DigCursorRadius` |
| 触发条件 | 圆圈范围在地图内某座坟上方 **连续停留 ≥ 0.2 秒** → 对该坟触发一次挖掘 |
| 可挖类型门禁 | 若该坟品质 ID **不在** `DiggableQualityIds` 内 → **不触发** DigAction（该类坟仍可按配置生成） |
| 忙碌锁 | 若该坟当前处于「挖掘中」，**不刷新 / 不重复触发**，直至本次挖掘流程结束 |

**单次挖掘流程（DigAction）**

1. 将该坟标记为「挖掘中」。
2. 在坟的图标素材 **上方** 播放挖掘帧动画，持续 **`DigActionDuration` 秒**，并同时播放挖掘反馈特效。
3. 同一座坟被持续挖掘时，挖掘帧动画按 **固定顺序循环** 播放（如 动画1→动画2→动画3→动画4→…）；动画具体数量与资源清单 **TBD**。
4. **`DigActionDuration` 播放完毕并完成扣血计算** 后，本次挖掘流程结束，清除该坟的「挖掘中」标记。

**扣血、图标样式与伤害来源**

| 规则 | 说明 |
|------|------|
| 扣血时机 | 每次 DigAction 结束时，对该坟结算 **一次** 扣血 |
| 伤害来源 | 单次扣血数值 = 存档主角的 `DigDamage`（科技绑定，见上） |
| 图标样式 | 按 **剩余 HP / maxHP** 百分比切换坟图标样式（端点归属如下） |

| 剩余 HP% | 样式 |
|----------|------|
| **> 65%** | 样式 1 |
| **≥ 30% 且 ≤ 65%** | 样式 2 |
| **< 30%** | 样式 3 |

**坟墓消除与奖励（DigReward）**

1. 当坟的当前 HP **变为 0** 时：播放「坟挖掘成功」动画；该坟障碍立即失效。
2. 成功动画播放的同时，在动画 **中心点** 出现本次获得的奖励图标（掉落内容取自该坟品质在「坟墓品质定义表」中的 `LootDrop`；编码见 [SPEC_04 §9.3](SPEC_04_Technical.md)）。
3. 随后奖励图标 **飞向主角**；**到达瞬间**按下方规则入账，然后图标消失。

**仓库（Warehouse）与精魂（SpiritEssence）入账**

| 规则 | 说明 |
|------|------|
| 仓库 | 按 **存档槽** 持久；**不限格数、不限存储时长** |
| 材料堆叠 | 非货币奖励按 **材料类型（MaterialId）** 堆叠；单类型上限常量 **10000** |
| 精魂 | 货币；**不**进入材料堆叠；挖坟获得（`LootDrop` 保留 Id 直接掉落 + 堆叠超限自动兑换）；在 **制造战士** 时消耗（§3.11） |
| 入账时机 | DigReward 飞到 Digger **到达瞬间** |

解析 `LootDrop` 每一段 `Id_Count`：

1. 若 `Id` 为保留精魂 Id（`Spirit`，见 SPEC_04 §9.3）→ 增加 `Count` 点精魂。
2. 若 `Id` 为材料 Id → 尝试写入仓库：
   - 令 `space = 10000 − 当前堆叠数量`；`toStack = min(Count, space)`；`excess = Count − toStack`。
   - `toStack` 加入该材料堆叠。
   - `excess > 0` 时：按材料配置表 `AutoConvert`（每 1 个超出材料兑换的精魂数，≥ 0）兑换精魂：`SpiritGain = excess × AutoConvert`；`AutoConvert = 0` 时超出部分不入堆且不兑精魂。

**阶段结束与结算（无胜负）**

| 规则 | 说明 |
|------|------|
| 胜负 | Dig 阶段 **无胜 / 负**；**不**触发 `LevelFailure` |
| 唯一结束条件 | **有效挖坟时长**倒计时归零 |
| 归零瞬间 | 停止过程生成；**取消**所有进行中的 `DigAction`（**不**结算本次扣血）；不可再触发挖掘 |
| 阶段结算 | 弹出 **DigStageSummary**（UI-011）：仅展示 **本阶段已获得** 奖励的按类型汇总；**不额外发放**任何奖励（与关卡级 `VictorySettlement` 区分） |
| 确认后 | 玩家确认关闭弹窗 → 进入 §3.9 下一阶段 /（若末阶段）`VictorySettlement` |

```
EffectiveDigDuration countdown → 0
  → Stop spawn; cancel in-progress DigAction (no damage)
  → DigStageSummary popup (aggregate rewards earned this Dig stage; no extra grants)
  → Player confirm → §3.9 next stage / VictorySettlement
```

### English

**Status: Defined (spawn / effective duration / dig interaction & reward credit / obstacle geometry / four Dig tech-bound capabilities / no win-lose / DigStageSummary; full tech-node table still TBD)**

When the current Level stage has `GameplayType = Dig`, use the DigGameplayConfig row matching `GameplayConfigId`. Grave `maxHP` and loot come from GraveQualityConfig ([SPEC_04 §9.3](SPEC_04_Technical.md)).

**Map**

| Rule | Notes |
|------|-------|
| Presentation | Composed of 45°-rotated squares (diamond look) |
| Logic | **One continuous placeable space**, not a cell grid; pick positions in continuous space |
| Placeable | Candidate must **not** intersect any **DigObstacle** circle |

**Obstacles (DigObstacle)**

Only these two types this stage (no other obstacle types yet):

| Type | Notes |
|------|-------|
| Digger | Dig protagonist at map center; obstacle size on **Digger Prefab** (circle radius, world units) |
| Grave | Spawned and not yet cleared (HP > 0); obstacle size on **that quality's Grave Prefab** (one Prefab per quality; circle radius) |

- Rules layer uses circle–circle intersection for placeable checks.
- When a grave is cleared (HP = 0), its obstacle **clears immediately**.
- Prefab path conventions: [SPEC_04 §9 / §13](SPEC_04_Technical.md).

**Table 2 — DigGameplayConfig fields (rules semantics)**

| Field | Notes |
|-------|-------|
| GameplayConfigId | Links from Level Operation |
| Level duration limit | **Base** duration (seconds); actual countdown uses **effective Dig duration** (below) |
| Initial grave count | N independent weighted rolls at start |
| In-countdown spawn rate | Every N seconds spawn M graves (encoding: [SPEC_04 §9](SPEC_04_Technical.md)) |
| Grave spawn weights | Weights per Grave Quality Id; `Weight = 0` entries dropped (encoding + common rules: SPEC_04 §9) |

**Effective Dig duration**

| Rule | Notes |
|------|-------|
| Formula | `EffectiveDigDuration = DigGameplayConfig.LevelDurationSeconds + DigStageDurationBonus` (seconds, additive) |
| Tech source | `DigStageDurationBonus` written into `DigProtagonistCapabilities` by **save-slot protagonist** tech unlock/upgrade; nodes / values **later** |
| Countdown | On Dig stage enter, start countdown from effective duration; hits 0 → stage ends (below) |

**Initial spawn**

1. Read initial grave count = N.
2. Perform **N independent** attempts: each time filter `GraveSpawnWeights` per [SPEC_04 §9 weighted-field common rules](SPEC_04_Technical.md) (drop `Weight = 0`); if the effective list is empty → **abandon that spawn** (no quality pick, no entity). Otherwise weighted-pick one Grave Quality Id from the effective list.
3. For each pick, choose a random placeable position and spawn a Grave; init `maxHP` / current HP from GraveQualityConfig.
4. Placement must avoid Digger and uncleared Grave obstacle circles; retry up to **32** times per spawn attempt, then **abandon that spawn**.

**Ongoing spawn**

- While countdown runs, every N seconds attempt to spawn M graves per the rate field.
- Each grave: filter weights → (abandon if effective list empty) → weighted quality pick → random placeable position (same retry rule) → HP from quality table.
- Same weight field and zero-weight drop rule as initial spawn.

**Digger**

| Rule | Notes |
|------|-------|
| Spawn | On Dig stage enter, spawn Digger at **DigMap center** |
| Default anim | Idle |
| Dig anim | While **≥1 grave** is in DigAction (busy), Digger plays a **looping** dig anim **in place**; otherwise return to idle |

**DigProtagonistCapabilities**

Bound to the **save-slot protagonist**; written by tech unlock/upgrade (**full tech-node table and TechPoint costs deferred**; this batch defines capability semantics and formulas only):

| Capability | Notes |
|------------|-------|
| DigDamage | Per-DigAction damage to the grave; initial default from default-unlocked tech |
| DigDurationReductionSum | Sum of all unlocked dig-action-duration shorten effects (seconds) |
| DigCursorRadius | Circle cursor radius (world units) |
| DiggableQualityIds | Set of Grave Quality Ids that may trigger DigAction |
| DigStageDurationBonus | Additive Dig-stage effective-duration bonus (seconds; see Effective Dig duration) |

**Dig action duration (dig speed):**

`DigActionDuration = max(0.1, BaseDigDuration − DigDurationReductionSum)` where `BaseDigDuration = 0.8` (seconds). Minimum dig time is **0.1s**.

(Distinct from Effective Dig duration: that is the stage countdown length; this is single DigAction anim/resolve duration.)

**Cursor & dig trigger**

| Rule | Value |
|------|-------|
| Cursor | On Dig stage enter, pointer becomes a **circle range**; radius = `DigCursorRadius` |
| Trigger | Circle continuously dwells on a map grave for **≥ 0.2s** → start one DigAction on that grave |
| Diggable gate | If that grave's Quality Id is **not** in `DiggableQualityIds` → **do not** start DigAction (such graves may still spawn) |
| Busy lock | If that grave is already in DigAction, **do not refresh / re-trigger** until the current DigAction ends |

**Single DigAction**

1. Mark the grave busy (DigAction in progress).
2. Play dig frame animation **above** the grave icon for **`DigActionDuration` seconds**, plus dig feedback VFX.
3. While the same grave is dug repeatedly, dig frame anims play in a **fixed cyclic order** (e.g. anim1→2→3→4→…); anim count and asset list **TBD**.
4. DigAction ends only after **`DigActionDuration` finishes and damage is resolved**; then clear busy.

**Damage, icon styles, damage source**

| Rule | Notes |
|------|-------|
| Damage timing | On each DigAction end, apply **one** damage hit to the grave |
| Damage source | Per-hit dig damage = save-slot protagonist `DigDamage` (tech-bound, above) |
| Icon style | Switch grave icon by **remaining HP / maxHP** % (endpoint rules below) |

| Remaining HP% | Style |
|---------------|-------|
| **> 65%** | Style 1 |
| **≥ 30% and ≤ 65%** | Style 2 |
| **< 30%** | Style 3 |

**Grave clear & DigReward**

1. When current HP hits **0**: play dig-success animation; grave obstacle clears immediately.
2. While that anim plays, spawn the reward icon at the anim **center** (loot from GraveQualityConfig `LootDrop`; encoding: [SPEC_04 §9.3](SPEC_04_Technical.md)).
3. Reward icon then **flies to the Digger**; **on arrival** credit per rules below, then the icon disappears.

**Warehouse & SpiritEssence credit**

| Rule | Notes |
|------|-------|
| Warehouse | Persist per **SaveSlot**; **unlimited slots and retention time** |
| Material stacks | Non-currency rewards stack by **MaterialId**; per-type cap constant **10000** |
| SpiritEssence | Currency; **not** stacked as material; from Dig (LootDrop reserved Id + overflow AutoConvert); spent when **manufacturing warriors** (§3.11) |
| Credit timing | When DigReward **arrives** at the Digger |

For each `LootDrop` segment `Id_Count`:

1. If `Id` is the reserved Spirit Id (`Spirit`, SPEC_04 §9.3) → add `Count` SpiritEssence.
2. If `Id` is a Material Id → credit Warehouse:
   - `space = 10000 − currentStack`; `toStack = min(Count, space)`; `excess = Count − toStack`.
   - Add `toStack` to that material stack.
   - If `excess > 0`: convert via MaterialConfig `AutoConvert` (SpiritEssence per 1 excess unit, ≥ 0): `SpiritGain = excess × AutoConvert`; if `AutoConvert = 0`, excess is discarded and yields no Spirit.

**Stage end & settlement (no win/lose)**

| Rule | Notes |
|------|-------|
| Win/lose | Dig stage has **no win / lose**; does **not** trigger `LevelFailure` |
| Sole end condition | **Effective Dig duration** countdown hits 0 |
| On zero | Stop ongoing spawn; **cancel** all in-progress `DigAction`s (**no** damage resolve); no further dig triggers |
| Stage settlement | Show **DigStageSummary** (UI-011): aggregate **rewards already earned this stage** by type; **no extra grants** (distinct from level `VictorySettlement`) |
| After confirm | Player confirms/dismisses popup → §3.9 next stage / (if last) `VictorySettlement` |

```
EffectiveDigDuration countdown → 0
  → Stop spawn; cancel in-progress DigAction (no damage)
  → DigStageSummary popup (aggregate rewards earned this Dig stage; no extra grants)
  → Player confirm → §3.9 next stage / VictorySettlement
```

---

## 3.11 升级与制造（UpgradeManufacture）

### 简体中文

**状态：框架已关闭（规则库）；升级配置表结构与关卡失败经验边界已关闭；配方 / 失控档次效果 / 完整科技树 / 等级具体数值另专题补录**

当关卡当前阶段 `玩法类型 = UpgradeManufacture` 时进入本阶段。本阶段包含三条并列能力：**升级**、**制造战士**、**战斗布阵**。配置表载体与字段编码见 [SPEC_04 §9](SPEC_04_Technical.md)（升级表见 **§9.8 `ProtagonistLevelConfig`**；制造配方等仍 **TBD**）。

**界面组织（UI）**

| 规则 | 说明 |
|------|------|
| 布局 | **同一屏三区并列**：升级区 / 制造区 / 布阵区（可同时看见与操作，非 Tab、非线性向导） |
| 完成入口 | 屏幕 **底部** 常驻「完成 / 进入下一阶段」按钮；点击即触发阶段结束（§3.11 阶段结束） |
| 布阵编辑器 | 与 Defend `Prepare` **共用同一套**布阵 UI / 逻辑（写同一 BattleFormation） |
| 区内外细节控件 | 升级/制造区内具体控件与数值展示 **TBD**（后续按子系统补） |
| UI 清单 | 见 §3.6 `UI-010` |

**资源依赖**

| 子系统 | 依赖资源 | 来源 |
|--------|----------|------|
| 升级 | 经验（Experience）→ `LifetimeExperience` | **Defend 阶段胜利**结算时统一加算（非击杀即时）；关卡失败不入账 |
| 制造战士 | 材料（Material）+ 精魂（SpiritEssence） | **挖坟（Dig）** 入仓库 / 精魂；见 §3.10 |
| 上阵 / 受控 | 控制力（ControlPower） | 主角属性；上限成长见下 |

**升级**

| 规则 | 说明 |
|------|------|
| 配置表 | `ProtagonistLevelConfig`（[SPEC_04 §9.8](SPEC_04_Technical.md)）：一行一个等级 |
| 存档字段 | 至少持有 `Level` 与 `LifetimeExperience`（生涯累计经验） |
| 模式 | 累计阈值制：`LifetimeExperience >=` 下一档 `RequiredTotalExperience` → 连升 |
| 经验入账 | 仅在 **Defend 阶段胜利**结算路径加算本阶段应得经验至 `LifetimeExperience` |
| 关卡失败 | LevelFailure **不**入账本阶段经验；**无关卡结算奖励**；已入账经验与其它已获资源 **不扣除**（§3.9、§3.12） |
| 溢出经验 | 升级 **不**清零 / **不**扣减 `LifetimeExperience`；「溢出保留」= 累计模型自然结果 |
| 升级时应用 | 每升入等级 N：发放该行 `TechPointsReward`；应用 `ControlPowerCap`、`ProtagonistMaxHP` |
| 解锁功能字段 | `UnlockedFeatureIds` **仅预留**；本版无运行时解锁逻辑 |
| 科技树范围 | **完整科技树另专题录入**；本版仅定挖坟能力绑定（`DigProtagonistCapabilities`，见 §3.10）+ 占位「可花费 TechPoint 升级科技」 |
| 等级具体数值 | 各行 `RequiredTotalExperience` / 奖励 / 上限数值 **TBD**（1 级行通常 `RequiredTotalExperience = 0`） |

**制造战士**

| 规则 | 说明 |
|------|------|
| 目的 | 制造 **战士（Warrior）**，供防守阶段上阵，抵御敌人对主角的进攻 |
| 库存模型 | 每个战士为 **独立实例**（自有 ID、剩余血量等）；**非**种类×数量堆叠 |
| 消耗 | 消耗仓库中的 **材料** 与/或 **精魂**；本批仅框架「耗资源 → 出战士」；材料种类 / 配方表（含精魂消耗量）**另专题** |
| 产出 | 可上阵的战士实例（属性、品质 **TBD**） |

**控制力与失控**

| 规则 | 说明 |
|------|------|
| 占用时机 | 战士 **上战场时** 占用主角的控制力（制造本身 **不**耗控制力） |
| 上限成长 | 本版：`ControlPowerCapEffective =` 当前等级行 `ControlPowerCap`；科技对上限的加成 **另专题**（生效后为「等级表上限 + 科技加成」） |
| 受控判定 | 若所有已上阵战士占用之和 **≤** 上限 → 这些战士 **永久受控**（除非死亡） |
| 失控判定 | 若占用之和 **>** 上限 → 按 **超过额度** 分档触发 **失控（LossOfControl）** |
| 与开战关系 | 失控 **不阻止** Defend「开战」；开战门槛仅「上阵战士 ≥ 1」（§3.12）。失控效果在 **战斗中** 按档次生效 |
| 失控档次效果 | 本批仅保留「按超额分档」占位；档位阈值与战斗效果 **另专题** |

**战斗布阵（BattleFormation）**

| 规则 | 说明 |
|------|------|
| 功能 | 安排已制造的战士进入战场 |
| 持久化字段 | 至少保存：上阵战士 **ID**、**位置**、**剩余血量** |
| 坐标系 | **BattleMap 连续坐标**（与 §3.12 连续可走空间一致；非格子） |
| 可编辑时机 | **两处**写同一套数据：① 升级与制造布阵区；② 防守 `Prepare` |
| 编辑器复用 | 两处 **同一套**布阵 UI / 逻辑 |
| 准备态可做 | 调整位置、上下阵（从已有战士实例池选入/撤下）；**不可**在 Prepare 制造新战士 |
| 与防守关系 | `Prepare` 加载并允许改写布阵；开战瞬间按**当前**布阵部署（见 §3.12） |
| 控制力 | 上下阵变更后立即重算控制力占用 / 失控档次 |

**阶段结束与结算**

| 规则 | 说明 |
|------|------|
| 结束条件 | **玩家主动确认**「完成 / 进入下一阶段」→ 本阶段结束 |
| 倒计时 | **无**强制倒计时（与 Dig 不同） |
| 前置门槛 | **无强制门槛**（允许空布阵确认结束） |
| 阶段结算 | **无独立阶段结算**；确认后直接进入 §3.9 下一阶段 /（若末阶段）VictorySettlement |
| 确认后 | 跳过本玩法阶段结算 → 下一阶段 / 胜利结算 |

```
UpgradeManufacture stage
  → Upgrade: LifetimeExperience (from Defend victory credit) ≥ next RequiredTotalExperience
       → LevelUp (Exp pool not reset) → TechPointsReward + apply ControlPowerCap / ProtagonistMaxHP
       → UnlockedFeatureIds reserved only; TechTree full tree later
  → Manufacture: spend Materials and/or SpiritEssence → create Warrior instance {Id, HP, ...} (recipes later)
  → Formation: shared editor; BattleMap continuous coords; persist {WarriorId, Position, RemainingHP}
  → Deploy control: Cap = level-row ControlPowerCap (+ tech later); overflow → LossOfControl tiers (effects later); does not block StartBattle
  → Player confirms "Complete / Next stage" → no stage settlement → §3.9 next / VictorySettlement
```

### English

**Status: Framework closed (rules library); upgrade table schema and LevelFailure Exp boundary closed; recipes / LossOfControl tier effects / full tech tree / concrete level numbers deferred**

Entered when Level stage `GameplayType = UpgradeManufacture`. Three parallel capabilities: **Upgrade**, **Manufacture warriors**, **BattleFormation**. Config encodings: [SPEC_04 §9](SPEC_04_Technical.md) (**§9.8 `ProtagonistLevelConfig`** for upgrade; manufacture recipes still **TBD**).

**UI layout**

| Rule | Notes |
|------|-------|
| Layout | **One screen, three side-by-side panels**: Upgrade / Manufacture / Formation |
| Complete entry | **Bottom** "Complete / Next stage"; ends stage |
| Formation editor | **Same** UI/logic shared with Defend `Prepare` (same BattleFormation) |
| In-panel controls | Upgrade/Manufacture widgets **TBD** |
| UI inventory | §3.6 `UI-010` |

**Resource dependencies**

| Subsystem | Resource | Source |
|-----------|----------|--------|
| Upgrade | Experience → `LifetimeExperience` | Credited on **Defend stage victory** (not on kill); not on LevelFailure |
| Manufacture | Material + SpiritEssence | Dig → Warehouse / SpiritEssence; see §3.10 |
| Deploy / control | ControlPower | Protagonist; cap growth below |

**Upgrade**

| Rule | Notes |
|------|-------|
| Config | `ProtagonistLevelConfig` ([SPEC_04 §9.8](SPEC_04_Technical.md)): one row per level |
| Save fields | At least `Level` and `LifetimeExperience` |
| Model | Cumulative threshold: `LifetimeExperience >=` next row `RequiredTotalExperience` → chain level-ups |
| Exp credit | Only on **Defend stage victory** settlement → add to `LifetimeExperience` |
| LevelFailure | **No** stage Exp credit; **no** level settlement rewards; already-owned Exp and other assets **not clawed back** (§3.9, §3.12) |
| Overflow Exp | Level-up does **not** reset / deduct `LifetimeExperience`; overflow kept is the natural cumulative model |
| On level-up | Entering level N: grant row `TechPointsReward`; apply `ControlPowerCap`, `ProtagonistMaxHP` |
| Unlock field | `UnlockedFeatureIds` **reserved only**; no runtime unlock this version |
| Tech tree scope | **Full tree later**; this version defines Dig capability bindings (`DigProtagonistCapabilities`, §3.10) + placeholder spend TechPoints |
| Concrete numbers | Per-row thresholds / rewards / caps **TBD** (level-1 row usually `RequiredTotalExperience = 0`) |

**Manufacture warriors**

| Rule | Notes |
|------|-------|
| Purpose | Create **Warrior** instances for Defend |
| Inventory model | Each warrior is an **independent instance** (own Id, remaining HP, …); **not** stack-by-kind |
| Cost | **Materials** and/or **SpiritEssence** from Warehouse / currency; framework only «spend resources → warrior»; kinds/recipes (incl. Spirit costs) **later** |
| Output | Deployable instances (stats/quality **TBD**) |

**ControlPower & LossOfControl**

| Rule | Notes |
|------|-------|
| When cost applies | On **deployment** (manufacture does **not** cost ControlPower) |
| Cap growth | This version: `ControlPowerCapEffective =` current level row `ControlPowerCap`; tech bonus to cap **later** (then «level-table cap + tech») |
| Controlled | Sum cost **≤** cap → permanently controlled (unless dead) |
| LossOfControl | Sum **>** cap → tiered by overflow |
| vs StartBattle | Does **not** block StartBattle; only gate is ≥1 warrior (§3.12); effects in **combat** |
| Tier effects | Placeholder only this batch; thresholds/effects **later** |

**BattleFormation**

| Rule | Notes |
|------|-------|
| Function | Assign warrior instances onto the battlefield |
| Persisted fields | Warrior **Id**, **position**, **remaining HP** |
| Coordinates | **BattleMap continuous space** (same as §3.12; not a cell grid) |
| Editable in | UpgradeManufacture panel **and** Defend `Prepare` (one dataset) |
| Editor reuse | **Same** formation UI/logic in both places |
| Prepare may | Positions + deploy/undeploy from instance pool; **no** manufacture |
| Defend link | StartBattle deploys from **current** formation |
| ControlPower | Recalculate immediately after deploy changes |

**Stage end & settlement**

| Rule | Notes |
|------|-------|
| End condition | Player confirms "Complete / Next stage" |
| Countdown | **None** |
| Preconditions | **None** (empty formation allowed) |
| Stage settlement | **None**; skip to §3.9 next stage / VictorySettlement |
| After confirm | No mode settlement → next / VictorySettlement |

```
UpgradeManufacture stage
  → Upgrade: LifetimeExperience (from Defend victory credit) ≥ next RequiredTotalExperience
       → LevelUp (Exp pool not reset) → TechPointsReward + apply ControlPowerCap / ProtagonistMaxHP
       → UnlockedFeatureIds reserved only; TechTree full tree later
  → Manufacture: spend Materials and/or SpiritEssence → create Warrior instance {Id, HP, ...} (recipes later)
  → Formation: shared editor; BattleMap continuous coords; persist {WarriorId, Position, RemainingHP}
  → Deploy control: Cap = level-row ControlPowerCap (+ tech later); overflow → LossOfControl tiers (effects later); does not block StartBattle
  → Player confirms "Complete / Next stage" → no stage settlement → §3.9 next / VictorySettlement
```

---

## 3.12 防守（Defend）

### 简体中文

**状态：框架已定义（准备可改布阵/开战/部署/寻路/胜负）；刷怪细则、攻击优先级表、攻击与伤害数值 TBD**

当关卡当前阶段 `玩法类型 = Defend` 时进入本阶段。依赖 §3.11 **战斗布阵（BattleFormation）** 持久化数据。配置表载体见 [SPEC_04 §9.7](SPEC_04_Technical.md)。刷怪波次细则 **另批阐述**。

**阶段内子状态（DefendPhase）**

| 子状态 | 说明 |
|--------|------|
| `Prepare` | 进入 Defend 后的默认态：加载布阵、展示准备 UI（含「开战」）；**可编辑布阵**（与 §3.11 **同一套**布阵 UI/逻辑）；写回同一 BattleFormation；不可制造新战士 |
| `Combat` | 点击「开战」后：按**当前**布阵部署单位、刷怪、寻路与战斗结算运行中 |
| `Ended` | 本阶段已因胜利结束，或因关卡失败中止 |

**准备态布阵编辑**

| 规则 | 说明 |
|------|------|
| 数据 | 与升级与制造共用 **同一套** BattleFormation 持久化 |
| 编辑器 | 与 §3.11 布阵区 **同一套** UI / 逻辑 |
| 坐标系 | BattleMap **连续坐标**（§3.11 / §3.12） |
| 允许 | 调整上阵战士 **位置**；从已有战士 **实例**池 **上阵 / 下阵** |
| 禁止 | 在 `Prepare` **制造**新战士（制造仅 §3.11） |
| 写回时机 | 每次有效编辑立即写回（或等价于开战前保证已持久化）；开战读的是最新布阵 |
| 控制力 | 编辑后立即重算占用与失控档次（§3.11） |

**开战（StartBattle）**

| 规则 | 说明 |
|------|------|
| 触发 | 仅在 `Prepare`：玩家点击 UI「开战」（UI-009） |
| 效果 | `Prepare` → `Combat`；按下方规则部署单位并开始刷怪流程 |
| 无上阵战士 | **不允许开战**：当前布阵上阵战士数须 **≥ 1**；否则「开战」按钮 **禁用**，或点击时提示不可开战（二选一实现即可，语义相同） |
| 控制力超额 | **允许开战**；失控不挡开战，效果在战斗中按档次生效（§3.11） |

**开战瞬间部署**

| 单位 | 落点 / 状态 |
|------|-------------|
| 战斗主角（BattleProtagonist） | **BattleMap 中央**；与挖坟 `Digger` 为不同阶段实体 |
| 上阵战士（Warrior） | 按布阵持久化的 **位置** 生成；**剩余血量** 自布阵读取 |

**战斗地图（BattleMap）**

| 规则 | 说明 |
|------|------|
| 逻辑 | **连续可走空间**（非格子网格）；与 DigMap 分离 |
| 障碍 | 几何与阻挡 **TBD**（须可烘焙 / 可用于 NavMesh） |

**刷怪（占位）**

| 规则 | 说明 |
|------|------|
| 出现位置 | 怪物从设定的 **地图空间之外** 出现 |
| 波次 / 表 | 波次定义、出生点、数量与节奏 **TBD**（另节） |

**目标选择与寻路**

| 规则 | 说明 |
|------|------|
| 选目标 | 怪物按 **攻击优先级（AttackPriority）** 选择目标（排序表 **TBD**） |
| 目的地 | 前往能够对该目标施展攻击的坐标（攻击距离 **TBD**） |
| 修正间隔 | 每 **TargetRetargetInterval**（暂定 **1s**，可配置）重选/重算可攻击坐标，并请求 **NavMesh** 重寻路 |
| 技术约定 | 规则层输出目标与目的地；移动由 NavMeshAgent（或等价）执行；规则层不直接驱动 `Transform`。见 [SPEC_04 §9.7](SPEC_04_Technical.md) |

**胜负**

| 结果 | 判定 |
|------|------|
| **关卡失败（LevelFailure）** | `Combat` 中 **战斗主角 HP ≤ 0** → 立即关卡失败；**不**走 VictorySettlement / **无关卡结算奖励**（§3.9） |
| **本阶段胜利** | **同时**满足：① 当前为 **最后一波**；② 该防守配置下 **全部怪物均已出现过**；③ **全部怪物均已被击杀**（场上无存活敌、待刷队列为空） |
| 阶段胜利之后 | `Combat` → `Ended` → **统一入账本阶段经验（Experience）**（§3.11）→ §3.9 阶段结算（其余内容 **TBD**）→ 下一阶段 /（若末阶段）VictorySettlement |
| 关卡失败与经验 | LevelFailure **不**入账本阶段经验；此前已入账的 Experience 与其它已获资源 **不扣除** |

```
Defend stage
  → DefendPhase = Prepare
  → Load BattleFormation {WarriorId, Position, RemainingHP}
  → Player may edit formation (positions / deploy / undeploy) → write back same BattleFormation
  → StartBattle requires deployed warriors ≥ 1 (else button disabled / hint)
  → Player clicks StartBattle
  → DefendPhase = Combat
  → Spawn BattleProtagonist at BattleMap center
  → Deploy Warriors at **current** formation positions (RemainingHP)
  → Spawner starts (outside map; wave rules TBD)
  → Each Monster:
       select target by AttackPriority (table TBD)
       set NavMesh destination = attackable position
       every TargetRetargetInterval (default 1s): recompute destination / repath
  → If BattleProtagonist HP ≤ 0 → LevelFailure → no stage Exp / no VictorySettlement; keep already-owned; abort Level (§3.9)
  → If last wave AND all monsters spawned AND all monsters killed
       → DefendPhase = Ended → credit Experience → §3.9 settlement / next / VictorySettlement
```

### English

**Status: Framework defined (Prepare / StartBattle / deploy / pathing / win-lose); Prepare may edit shared BattleFormation; StartBattle requires ≥1 warrior; spawn details, AttackPriority table, attack/damage numbers TBD**

Entered when Level stage `GameplayType = Defend`. Depends on §3.11 **BattleFormation** persistence. Config: [SPEC_04 §9.7](SPEC_04_Technical.md). Wave spawn rules **later**.

**In-stage phases (DefendPhase)**

| Phase | Notes |
|-------|-------|
| `Prepare` | Default on enter: load formation, show prepare UI (incl. StartBattle); **may edit** formation with the **same** UI/logic as §3.11; write back same BattleFormation; cannot manufacture |
| `Combat` | After StartBattle: deploy from **current** formation, spawn, pathing, combat resolution |
| `Ended` | Stage ended by victory, or aborted by LevelFailure |

**Prepare formation editing**

| Rule | Notes |
|------|-------|
| Data | Shared **same** BattleFormation persistence as UpgradeManufacture |
| Editor | **Same** UI/logic as §3.11 formation panel |
| Coordinates | BattleMap **continuous** space (§3.11 / §3.12) |
| Allowed | Change warrior **positions**; **deploy / undeploy** from existing warrior **instance** pool |
| Forbidden | **Manufacture** new warriors in `Prepare` (manufacture only in §3.11) |
| Write-back | Persist on each valid edit (or guarantee persisted before StartBattle); StartBattle uses latest |
| ControlPower | Recalculate cost / LossOfControl tier after edits (§3.11) |

**StartBattle**

| Rule | Notes |
|------|-------|
| Trigger | Only in `Prepare`: player clicks UI StartBattle (UI-009) |
| Effect | `Prepare` → `Combat`; deploy units and start spawn flow |
| Empty formation | **StartBattle forbidden**: deployed warrior count must be **≥ 1**; otherwise StartBattle is **disabled**, or click shows a cannot-start hint (either UX is fine; same rule) |
| Over ControlPower | **StartBattle allowed**; LossOfControl does not block; tier effects apply in combat (§3.11) |

**Deploy on StartBattle**

| Unit | Placement / state |
|------|-------------------|
| BattleProtagonist | **BattleMap center**; distinct from Dig `Digger` |
| Warriors | Spawn at persisted formation **positions**; **remaining HP** from formation |

**BattleMap**

| Rule | Notes |
|------|-------|
| Logic | **Continuous walkable space** (not a cell grid); separate from DigMap |
| Obstacles | Geometry / blocking **TBD** (must support NavMesh bake/use) |

**Spawn (placeholder)**

| Rule | Notes |
|------|-------|
| Entry | Monsters appear from **outside** the configured map space |
| Waves / tables | Wave defs, spawn points, counts, timing **TBD** |

**Targeting & pathfinding**

| Rule | Notes |
|------|-------|
| Select target | By **AttackPriority** (ordering table **TBD**) |
| Destination | Position from which the monster can attack the target (range **TBD**) |
| Retarget interval | Every **TargetRetargetInterval** (provisional **1s**, configurable) recompute attackable point and request **NavMesh** repath |
| Tech | Rules layer outputs target + destination; movement via NavMeshAgent (or equiv.); rules must not drive `Transform`. See [SPEC_04 §9.7](SPEC_04_Technical.md) |

**Win / lose**

| Outcome | Condition |
|---------|-----------|
| **LevelFailure** | In `Combat`, **BattleProtagonist HP ≤ 0** → immediate LevelFailure; **no** VictorySettlement / **no level settlement rewards** (§3.9) |
| **Stage victory** | **All** of: ① current wave is the **last** wave; ② **all** monsters for this Defend config **have spawned**; ③ **all** monsters **have been killed** (no living enemies; spawn queue empty) |
| After stage victory | `Combat` → `Ended` → **credit stage Experience** (§3.11) → §3.9 stage settlement (other content **TBD**) → next stage / (if last) VictorySettlement |
| LevelFailure & Exp | LevelFailure **does not** credit stage Exp; already-owned Experience and other assets are **not clawed back** |

```
Defend stage
  → DefendPhase = Prepare
  → Load BattleFormation {WarriorId, Position, RemainingHP}
  → Player may edit formation (positions / deploy / undeploy) → write back same BattleFormation
  → StartBattle requires deployed warriors ≥ 1 (else button disabled / hint)
  → Player clicks StartBattle
  → DefendPhase = Combat
  → Spawn BattleProtagonist at BattleMap center
  → Deploy Warriors at **current** formation positions (RemainingHP)
  → Spawner starts (outside map; wave rules TBD)
  → Each Monster:
       select target by AttackPriority (table TBD)
       set NavMesh destination = attackable position
       every TargetRetargetInterval (default 1s): recompute destination / repath
  → If BattleProtagonist HP ≤ 0 → LevelFailure → no stage Exp / no VictorySettlement; keep already-owned; abort Level (§3.9)
  → If last wave AND all monsters spawned AND all monsters killed
       → DefendPhase = Ended → credit Experience → §3.9 settlement / next / VictorySettlement
```

---

## 待澄清清单

### 简体中文

- [ ] 壳层内三种 `GameplayState` 的手动切换触发
- [ ] 关卡场景绑定与从工具/流程进入真实关卡的路径
- [x] 挖坟障碍物类型与几何、以及「可放置」判定细节（Digger + 未消除 Grave；圆形半径在预制体上；见 §3.10）
- [x] 玩家挖坟交互与单坟奖励产出表现及入账（见 §3.10；Warehouse / SpiritEssence）
- [x] 挖坟阶段结束与结算：无胜负；有效时长=配置基础+科技时长加成；DigStageSummary 仅汇总无额外发放（见 §3.10 / UI-011）
- [ ] 胜利结算 UI / 字段
- [x] 坟墓品质定义表字段与 `LootDrop` 编码（见 SPEC_04 §9.3；MaxHP 具体数值仍 TBD）
- [x] 权重零值剔除与 Dig 空有效权重列表放弃该次生成（见 SPEC_04 §9 通用规则 / §3.10）
- [ ] 坟墓品质表 `MaxHP` 具体数值
- [x] 挖坟四项科技绑定能力算法（伤害 / 单次速度 / 光标半径 / 可挖类型；见 §3.10 `DigProtagonistCapabilities`）
- [ ] 完整科技节点表 / TechPoint 消耗与默认解锁项（另专题）
- [ ] 挖坟帧动画具体数量与资源命名清单
- [x] 升级与制造框架（§3.11；原 SewRevive 更名 UpgradeManufacture）— **框架已关闭**
- [x] 升级与制造阶段结束=玩家确认；**无独立阶段结算**
- [x] 升级与制造主屏布局（同屏三区 + 底部完成；UI-010）；升级/制造区控件仍 TBD
- [x] BattleFormation：§3.11 与 Defend Prepare **同一编辑器**；连续坐标；Prepare 不可制造
- [x] 经验：Defend 阶段胜利统一入账至 `LifetimeExperience`；升级不扣减累计经验；完整科技树另专题
- [x] 战士=独立实例；制造仅框架（配方另专题）
- [x] 控制力上限=当前等级行 `ControlPowerCap`（科技加成另专题）；失控分档占位（效果另专题）；失控不挡开战
- [x] 无上阵战士时不允许开战（须 ≥1）
- [x] 关卡失败：不入账本阶段经验、无关卡结算奖励；已获得不扣除
- [x] 主角升级配置表 `ProtagonistLevelConfig` 字段与累计阈值语义（SPEC_04 §9.8）；各行具体数值仍 TBD
- [ ] 完整科技树节点与 TechPoint 消耗（另专题）
- [ ] 材料种类、制造配方与战士属性（另专题）
- [ ] 战士控制力占用值；失控档位阈值与战斗效果（另专题）
- [ ] 升级 / 制造区内具体控件与数值展示
- [x] 防守（Defend）框架：准备/开战/部署/NavMesh 寻路/阶段胜利与关卡失败（§3.12）
- [ ] 防守刷怪波次表、出生点几何与节奏
- [ ] 攻击优先级（AttackPriority）排序表；攻击距离与伤害
- [ ] 防守阶段结算其余字段；关卡失败结算 UI / 字段
- [ ] 设置项清单
- [ ] 存档完整字段（显示名、时间戳、局内进度等）
- [ ] 工具面板后续功能列表

### English

- [ ] Manual shell `GameplayState` switch triggers
- [ ] Level scene binding and real Level entry path
- [x] Dig obstacle types/geometry and placeable checks (Digger + uncleared Grave; circle radius on Prefabs; §3.10)
- [x] Player dig interaction, per-grave rewards, and inventory credit (§3.10; Warehouse / SpiritEssence)
- [x] Dig stage end & settlement: no win/lose; effective duration = config base + tech duration bonus; DigStageSummary aggregate only, no extra grants (§3.10 / UI-011)
- [ ] VictorySettlement UI / fields
- [x] GraveQualityConfig fields and `LootDrop` encoding (SPEC_04 §9.3; MaxHP concrete values still TBD)
- [x] Zero-weight drop and Dig empty effective weight list → abandon that spawn (SPEC_04 §9 common rules / §3.10)
- [ ] GraveQualityConfig MaxHP concrete values
- [x] Four Dig tech-bound capability formulas (damage / dig speed / cursor radius / diggable types; §3.10 `DigProtagonistCapabilities`)
- [ ] Full tech-node table / TechPoint costs and default unlocks (later topic)
- [ ] Dig frame-anim count and asset naming list
- [x] UpgradeManufacture framework closed (§3.11)
- [x] UpgradeManufacture: player confirm end; **no** independent stage settlement
- [x] UI-010 three panels + Complete; Upgrade/Manufacture widgets still TBD
- [x] BattleFormation: shared editor; continuous coords; no manufacture in Prepare
- [x] Exp: Defend victory → `LifetimeExperience`; level-up does not deduct cumulative Exp; full tech tree later
- [x] Warrior = instance; manufacture framework only (recipes later)
- [x] ControlPower cap = level-row `ControlPowerCap` (tech bonus later); LossOfControl tier placeholder; does not block StartBattle
- [x] StartBattle requires ≥1 deployed warrior
- [x] LevelFailure: no stage Exp / no level settlement rewards; already-owned not clawed back
- [x] `ProtagonistLevelConfig` schema + cumulative threshold semantics (SPEC_04 §9.8); concrete row numbers still TBD
- [ ] Full tech-tree nodes and TechPoint costs (later topic)
- [ ] Material kinds, recipes, warrior stats (later topic)
- [ ] Per-warrior control cost; LossOfControl tier thresholds & effects (later topic)
- [ ] Upgrade / Manufacture in-panel widgets
- [x] Defend framework (§3.12)
- [ ] Defend wave tables, spawn-point geometry, timing
- [ ] AttackPriority ordering table; attack range and damage
- [ ] Defend settlement other fields; LevelFailure settlement UI / fields
- [ ] Settings item list
- [ ] Full save fields (name, timestamp, progress, etc.)
- [ ] Future ToolsPanel entries

---

## 维护说明

### 简体中文

- 新模块从下一个可用 `## 3.x` 节起写；大节变更记入 SPEC_00 Changelog。
- 中英文双块同步；未决标 `TBD` / `未定义`。

### English

- Add new modules as the next `## 3.x` section; log major changes in SPEC_00 Changelog.
- Keep bilingual blocks in sync; mark open items `TBD` / `Undefined`.
