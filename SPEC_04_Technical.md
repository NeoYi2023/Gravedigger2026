# SPEC_04 — 技术规范 / Technical Standards（Gravedigger2026）

**关联文档 / Related:** [SPEC_00_Index.md](SPEC_00_Index.md) · [SPEC_03_GameRules.md](SPEC_03_GameRules.md)

---

## 1. 工程与环境

### 简体中文

| 属性 | 值 |
|------|-----|
| Unity 版本 | 2021.3.40f1 |
| Unity 编辑器（本机） | `F:\Unity\Unity 2021.3.40f1\Editor\Unity.exe` |
| 脚本语言 | C# |
| 渲染 | TBD |
| Cursor 工作区根 | `F:\CursorGame_Git\Gravedigger2026` |
| Unity 工程根目录 | `Gravedigger2026/`（相对工作区根） |
| 资源根目录 | `Gravedigger2026/Assets/` |
| 目标平台 | TBD |
| 平台优先级 | TBD |

批处理 / CLI 示例：`"<Unity编辑器>" -batchmode -quit -projectPath "<工作区根>\Gravedigger2026" …`（具体参数按任务另定）。

### English

| Attribute | Value |
|-----------|-------|
| Unity version | 2021.3.40f1 |
| Unity Editor (this machine) | `F:\Unity\Unity 2021.3.40f1\Editor\Unity.exe` |
| Scripting language | C# |
| Rendering | TBD |
| Cursor workspace root | `F:\CursorGame_Git\Gravedigger2026` |
| Unity project root | `Gravedigger2026/` |
| Assets root | `Gravedigger2026/Assets/` |
| Target platforms | TBD |
| Platform priority | TBD |

CLI example: `"<UnityEditor>" -batchmode -quit -projectPath "<workspaceRoot>\Gravedigger2026" …` (args per task).

---

## 2. 目录结构（建议）

### 简体中文

```
Gravedigger2026/Assets/
├── Scenes/
├── Scripts/
│   ├── Core/
│   ├── Gameplay/
│   ├── UI/
│   ├── Input/
│   └── Localization/
├── Prefabs/
│   ├── Dig/               # Digger、Grave 等
│   ├── Defend/
│   │   ├── Warriors/      # 士兵 AppearanceId Prefab
│   │   └── Monsters/      # 怪物 ModelId Prefab
│   └── Maps/              # Dig/Defend 共用地面变体 Ground_01…Ground_05
├── Art/                   # 美术源素材（非运行时 Instantiate 入口）
│   ├── Characters/        # Character Creator 烘焙导出源（见 §15）
│   │   ├── Protagonist/   # Digger / BattleProtagonist
│   │   ├── Appearances/   # Appearances/{AppearanceId}/
│   │   └── Monsters/      # Monsters/{ModelId}/
│   ├── Dig/               # 坟墓、挖坟反馈等非角色源
│   │   ├── Graves/        # Graves/{Grave_Q*}/
│   │   └── Feedback/
│   ├── Maps/              # Maps/{Ground_0N}/ 贴图等
│   ├── Defend/            # 弹道、护盾等非角色表现源
│   │   ├── Projectile/
│   │   └── Shield/
│   ├── UI/                # 2D 图标统一落点（本版不另建 Sprites/）
│   │   ├── Currency/
│   │   ├── Icons/
│   │   ├── Outlines/
│   │   ├── Tech/
│   │   └── Meta/
│   ├── Placeholder/       # Demo CSV AssetPath 过渡落点
│   ├── VFX/
│   └── Audio/             # 玩法音效源约定
├── Localization/
│   ├── Strings/
│   └── Fonts/
├── Resources/
├── Settings/              # 非表型 ScriptableObject（单例调参、引用槽等）
├── SmallScaleInt/         # 第三方 Character Creator 工具源（仅创作；见 §15）
└── ConfigTables/          # 配置表统一根（见 §14）
    ├── Excel/             # 人配源表（.xlsx）；运行时不加载
    └── Csv/               # 程序读表（.csv）；打表产物；运行时唯一数据源
```

**Art vs Prefabs：** `Art/` 存源素材（图、Clip、Controller、贴图等）；游戏 Instantiate / Catalog 绑定只引用 `Prefabs/<模块>/`。本版 **不** 单独落地顶层 `Sprites/`；2D 图标统一落在 `Art/UI/`（及 `Art/Placeholder/` 过渡）。角色烘焙细则见 [§15](#15-角色美术管线character-creator-烘焙整角)。

实际目录以工程为准；结构性变更记入 SPEC_00 Changelog。配置表路径与命名强制约定见 [§14](#14-配置表工程约定与打表工具)。角色美术路径与工具目录禁入见 [§15](#15-角色美术管线character-creator-烘焙整角)。

### English

Recommended tree as above. `Art/` holds source art (Characters per [§15](#15-角色美术管线character-creator-烘焙整角), Dig/Maps/Defend/UI/VFX/Audio, plus `Placeholder/` for Demo CSV `AssetPath`); runtime Instantiate uses `Prefabs/` only. Top-level `Sprites/` is **not** used this revision—2D icons live under `Art/UI/`. Also includes `Prefabs/Dig|Defend|Maps/...`, `SmallScaleInt/` tool source, `ConfigTables/Excel/` + `ConfigTables/Csv/`. Record structural changes in SPEC_00 Changelog. Config-table path and naming rules: [§14](#14-配置表工程约定与打表工具). Character art paths and vendor-folder ban: [§15](#15-角色美术管线character-creator-烘焙整角).

---

## 3. 代码规范（摘要）

### 简体中文

| 类别 | 约定 |
|------|------|
| 类 / 方法 / 公共成员 | PascalCase |
| 私有字段 | camelCase，可选 `_` 前缀 |
| 接口 | `I` + PascalCase |
| 命名空间 | `Gravedigger2026.<模块>`（待代码出现后与现有一致） |
| 文件名 | 与主类名一致 |

审查清单见 [unity-spec-dev-workflow](.cursor/skills/unity-spec-dev-workflow/SKILL.md) §5。

### English

See naming table. Checklist: [unity-spec-dev-workflow](.cursor/skills/unity-spec-dev-workflow/SKILL.md) §5. Namespace `Gravedigger2026.<Module>` unless existing code differs.

---

## 4. 跨平台输入（占位）

### 简体中文

**状态：未定义**

建议玩法依赖输入抽象接口，禁止玩法直接读 `Input.GetKey` / 原始触摸 API。

### English

**Status: Undefined**

Prefer an input abstraction; no raw `Input.GetKey` / touch in gameplay code.

---

## 5. 性能与资源（底线）

### 简体中文

- 缓存 `GetComponent`；`Update` 避免分配；高频对象池化；主线程访问 Unity API

### English

- Cache components; avoid allocations in `Update`; pool frequent spawns; main-thread Unity API only

---

## 6. Demo 实现边界

### 简体中文

**状态：已对照 SPEC_03 §3.8（Meta 壳 + 关卡流水线垂直切片）**

**范围内（须满足 D-001～D-043）：**

| 模块 | 要求 |
|------|------|
| 存档 Meta | 固定 3 槽；本地按槽索引；至少持久化「是否占用」及流水线所需最小字段；新建 / 进入 / 删除（含确认） |
| 进档壳层 | 进入后默认 `GameplayState = Dig` 占位；浮动「工具」；流水线片可从壳层启动样例关卡（§3.8 D-003 / D-010） |
| 工具面板 | 设置、关卡入口；与玩法 View 分离（壳层 UI） |
| 关卡驱动 | 只读 `ConfigTables/Csv/`；`LevelOperationConfig` 升序驱动 Dig → UpgradeManufacture → Defend |
| Dig | §3.10 垂直切片：`DigMapId`→`Prefabs/Maps/`；挖掘 / 奖励 / DigStageSummary（临时美术允许） |
| UpgradeManufacture | §3.11 垂直切片：升级区 / 制造 ≥1 士兵 / 布阵写回；阶段 `GameplayConfigId` **忽略**（见 §9.1） |
| Defend | §3.12 垂直切片：Prepare/开战/护盾；Demo 最小刷怪点 + NavMesh；士兵普攻；胜负/LevelFailure（临时美术允许） |

**范围外：** 完整技能施放与效果表；正式美术 polish；精确 OutsideMap 几何；完整存档 schema；科技树节点具体数值/图标 polish 与功能系统名完整枚举；工具后续功能；Editor 打表工具实现；未列入 §3.8 的需求。

**持久化意图（轻量）：** 本地、按槽索引 `0..2`。**Demo Meta 选型已锁定：`PlayerPrefs`**；键 `Gravedigger2026.SaveSlot.{0|1|2}.Occupied`（`0`/`1`）。本期仅持久化「是否占用」；完整字段 schema **TBD**；流水线所需最小字段随后续片回写。

**Meta 壳实现（方案 A，D-001～D-004）：** 单场景 `Assets/Scenes/Boot.unity`；`SaveSelect` / `InSaveShell` 以 Canvas Prefab 显隐切换（`Assets/Prefabs/Meta/`、`Assets/Prefabs/UI/`）。规则层：`SaveSlotService` + `GameplayStateService`；View 只订阅。工具「设置」→ **打开科技树画布**（见下 UI-012）；工具「关卡」→ **启动样例 `Level_01`**（见下「关卡驱动」）。壳层正式手动切三态仍 **TBD**；Demo 暂提供进档壳 **Debug「切下一态」** 仅用于手验 D-004（不得等同工具「关卡」）；另提供 **Debug「推进阶段」** 手验 D-010（占位结束当前阶段 → 下一阶段 / VictorySettlement）。

**关卡驱动（方案 A，D-010）：** `ConfigCsvRepository` 只读 CSV（路径见 [§14.5](#145-运行时-csv-加载路径demo)）；`LevelOperationDriver` 按 `LevelId` 取行、`StageNumber` 升序运行；进入阶段时设置 `GameplayState`，经 `IStageModule` 进入/离开钩子挂各玩法（Dig / UM / Defend 见下；士兵战斗与胜负仍待后续片）。`GameplayType=UpgradeManufacture` 时 **忽略** `GameplayConfigId`（不查 Dig/Defend 表）。Dig/Defend 解析对应表行后校验 `DigMapId` / `BattleMapId` ∈ `Ground_01`…`Ground_05`，逻辑路径 `Assets/Prefabs/Maps/{Id}.prefab`。UI/日志须可见 LevelId、StageNumber、GameplayType。

**Dig 垂直切片（方案 A，D-020）：** `DigStageModule`（`IStageModule`）Enter 时按 `DigMapId` Instantiate `Assets/Prefabs/Maps/{Id}.prefab`，并挂 `DigStageRoot`（`Assets/Prefabs/Dig/`）。规则层 `DigSessionService`（纯 C#）负责有效时长倒计时、开局/过程生成、DigAction 停留触发与忙碌锁、扣血、仓库/精魂入账、阶段奖励汇总；`DigProtagonistCapabilities` 由 **科技树** 重算后注入（见 UI-012）。表现：`DigPrefabCatalog` 绑定 Digger / `Grave_{QualityId}` / 地图变体；圆圈光标、坟墓 HP 样式、DigReward 飞向、DigStageSummary 由 View 订阅。时长归零 → 取消进行中 DigAction（不结算扣血）→ DigStageSummary 确认 → `LevelOperationDriver.TryAdvanceStage`。禁止运行时引用 `SmallScaleInt/`。

**科技树画布（方案 A，UI-012 可选）：** `ConfigCsvRepository` 追加加载 `Tech_TechTreeConfig.csv` / `Tech_TechEffectConfig.csv`。规则层纯 C# `TechTreeService`（存档级，挂 Meta 壳）持有已学会集合与 `UnlockedFeatureSystems`；进档/`Reset` 时对 `InitiallyUnlocked` 自动学会并应用效果；学习闸门 = 未学会 ∧ TechPoint≥LearnCost ∧ ≥1 已学会前置（由 `UnlockNextTechIds` 求逆）；学会扣点 → 标记 → 解析 `AttributeModifiers` 加法求和 → 写入 `DigProtagonistCapabilities`（Demo `DiggableQualityIds` 仍取全品质表，便于挖坟手验）。表现：临时 `Assets/Prefabs/Meta/TechTreeCanvasRoot.prefab`（节点坐标 Prefab 摆放；uGUI 空白处 LMB 拖移；悬停名+效果描述；连线按正向边；三态框色）；工具「设置」打开画布；Debug 可注入 TechPoints。禁止运行时引用 `SmallScaleInt/`。

**UM 升级区（方案 A，D-030）：** `UpgradeManufactureStageModule` Enter 时 Instantiate `Assets/Prefabs/UpgradeManufacture/UpgradeManufactureStageRoot.prefab`（同屏三区：升级 / 制造 / 布阵均可操作）。`ConfigCsvRepository` 加载 `Manufacture_ProtagonistLevelConfig.csv`。规则层纯 C# `ProtagonistProgressService` 持有内存态 `Level` / `LifetimeExperience` / `TechPoints` / 生效 `ControlPowerCap` / `ProtagonistMaxHP`；累计阈值连升并应用表行奖励与上限。Debug 注入仍可用；正式 Defend 胜利入账见 D-043。底部「完成」→ `TryAdvanceStage`。禁止运行时引用 `SmallScaleInt/`。

**UM 制造区（方案 A，D-031）：** `ConfigCsvRepository` 追加加载 `Manufacture_SoulConfig` / `ClassConfig` / `GemConfig` / `RaceConfig` / `BodyPartConfig` / `BodyAppearanceConfig` / `ExtraEquipmentConfig` / `GemSuffixNameConfig`。规则层纯 C# `ManufactureService` 持有 15 个严格槽位（头1/躯干1/臂2/腿2/灵魂1/宝石6/坐骑1/翅膀1），按 Id 解析所属表自动路由到合法空槽，类型不符 / 同类型宝石重复 / 库存不足即拒绝；每次槽位变化重算预览（`Base(S)=Σ StatBonus`、`Equip`、`GemMult`、`RaceAdjust`、`StaticStat`、静态 `MaxHP=ceil(BodyLife+StaticStat(Str)×3)`、`TotalSpiritCost`、`ControlPowerCost`、试算种族与外观）。「制造」闸门 = 最低要求（躯干+臂2+腿2+灵魂）且 `SpiritEssence ≥ TotalSpiritCost`；提交时按 Id 逐件扣仓、扣精魂，定稿种族（各部位权重 1 加权随机）与外观（§9.13 算法），拼装 `WarriorName`，写 `WarriorInstance` 快照入纯 C# `WarriorPoolService`（存档级持有，供 04c 布阵 / 05x 战斗）。`WarehouseService` 扩展：`BodyPartId` 与 `MaterialId` 同命名空间入账（`AutoConvert` 取躯体表）、按 Id 扣减、精魂扣减；制造用灵魂/宝石/外置装备 Demo 期同存该物品仓（获取途径 **TBD**），本片以制造区 **Debug「注入制造套件」** 补齐。外观资源：`UpgradeManufacturePrefabCatalog` 增 `AppearanceId → Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab` 绑定（临时胶囊体；制造完成时解析并打日志，实际实例化留待 04c/05b）。种族展示名未启用 i18n，按 §9.11 回退直接使用 `DisplayNameKey`。禁止运行时引用 `SmallScaleInt/`。

**UM 布阵区（方案 A，D-032）：** 纯 C# `BattleFormationService`（存档级，与 `WarriorPoolService` 同挂 Meta 壳）持有上阵条目 `{WarriorId, PositionX/Z, RemainingHP}`；坐标系为 BattleMap **连续坐标**（非格子）。上阵从士兵池选入并写入实例当前 `RemainingHP`；下阵写回实例 HP 并清空条目；改位按步进微调 X/Z。控制力占用 = Σ 上阵 `ControlPowerCost`，对照 `ProtagonistProgressService.ControlPowerCap` 展示 Degree（`ΣCost/Cap − 1`，可粗）。表现：`FormationPanelView`（按钮上阵/下阵/微调；无真拖拽）挂入同屏三区布阵区；与 Defend `Prepare` **共用同一 Service**（Prepare UI 已在 D-040 复用）。本片不强制 Instantiate `Ground_*` 预览（正式战斗地图仍按 `BattleMapId`→`Prefabs/Maps/`）。底部「完成」→ `TryAdvanceStage`。禁止运行时引用 `SmallScaleInt/`。

**Defend Prepare / 开战 / 护盾（方案 A，D-040）：** `DefendStageModule`（`IStageModule`）Enter 时 Instantiate `Assets/Prefabs/Defend/DefendStageRoot.prefab`，并按 `BattleMapId` Instantiate **独立** `Assets/Prefabs/Maps/{Id}.prefab`（与 Dig 地图实例分离；Exit 销毁本阶段根）。规则层纯 C# `DefendSessionService` 持有 `DefendPhase`（Prepare→Combat）、`Shield`、`RemainingCombatSeconds`；开战闸门 = 布阵上阵数 ≥ 1。Prepare 复用存档级 `BattleFormationService` + `FormationPanelView` + UI-009「开战」。开战：`Shield = ProtagonistProgressService.ProtagonistMaxHP`；`RemainingCombatSeconds = DefendGameplayConfig.CombatDurationSeconds`（整秒递减；归零不单独判胜负）；地图中央部署临时 `Assets/Prefabs/Defend/BattleProtagonist.prefab`；按布阵坐标部署士兵（外观取 `Prefabs/Defend/Warriors/{AppearanceId}`）。`DefendPrefabCatalog` 绑定 StageRoot / BattleProtagonist / `Ground_*` / 士兵外观。EngageZone 挂在地图 Prefab 上（本片不驱动选敌）。禁止运行时引用 `SmallScaleInt/`。

**Defend 刷怪与寻路（方案 A，D-041）：** `ConfigCsvRepository` 追加加载 `Defend_WaveSpawnConfig.csv` / `Defend_MonsterConfig.csv`。开战时 `DefendSessionService` 按 `WaveConfigId` 装载刷怪行；`Combat` 中每当 `RemainingCombatSeconds` 变为某整秒（含开战瞬间）时，触发尚未触发且 `SpawnRemainingSeconds` 相等的行（同秒按 `SpawnOrder` 升序），经事件交给 View Instantiate。Demo 最小出生点：地图 Prefab 上 `DefendSpawnPointSet` 固定点（`ClockDirection`→钟点位；`RegionRandom`→点池随机；Inside/Outside 本片均用固定点，精确 OutsideMap **后置**）。Instantiate 地图后 Runtime 烘焙最小可走 NavMesh（覆盖地图活动区 + 出生点）。怪物 Prefab：`Assets/Prefabs/Defend/Monsters/{ModelId}.prefab`（临时立方体；Catalog 绑定）。`MonsterAgentView`（`NavMeshAgent`）按 `TargetSelect` 选目的地（本片士兵位可作为 PreferWarrior/Nearest 候选；无士兵则回退主角），按 `TargetRetargetIntervalSeconds` 重寻路；进 `AttackRange` 后按 `AttackSpeed` 普攻 → `Shield -= 1`（忽略 AttackPower）。`Shield ≤ 0` → `DefendPhase.Ended` + LevelFailure 钩子（打日志；完整关卡中止见 D-043）。士兵普攻 / 清场胜利 **不做**。禁止运行时引用 `SmallScaleInt/`。

**Defend 士兵近战（方案 A，D-042 近战片）：** `WarriorCombatMath` 按 `ClassConfig.PrimaryStat` + `CombatConvertCoeffs`（缺键回退 §3.12 默认）派生 `NormalAttackPower` / `AttackSpeed`。`DefendSessionService` 开战登记士兵 HP（`MaxHP=ceil(BodyLife+StaticStat(Str)×3)`，`RemainingHP` clamp）与刷怪登记怪物 HP；规则层确认近战 `HitConfirm`（前摇结束且目标仍存活、在 `AttackRange` 内）→ 怪 `HP -= NormalAttackPower`；怪对兵 `AttackPower` 直接扣 HP（无护甲）。`HP≤0` 无宝石 → `CombatDead`（停手）；有宝石 → 立即 PermanentDeath 标记（物资去向见 D-043）。表现：`WarriorAgentView`（`NavMeshAgent`）仅在 EngageZone 内选最近存活怪；`AttackMode=Melee` 走近战前摇。清场条件（刷怪行全触发 + 已刷怪全灭）→ `ClearVictoryConditionDetected` 事件/日志（**不**入账、**不**切胜利 Ended；见 D-043）。禁止运行时引用 `SmallScaleInt/`。

**Defend 士兵远程弹道（方案 A，D-042 远程 / 05c2）：** 开战登记同时写入 `ClassConfig.RangedProjectileSpeed` / `RangedTimeoutSeconds`。`AttackMode=Ranged` 士兵与近战共用 EngageZone 最近选敌与 `AttackSpeed` 周期；进 `AttackRange` 后 Instantiate 临时 `Assets/Prefabs/Defend/Projectile.prefab`（Catalog 绑定）。`ProjectileView` 运动学飞向锁定怪 RuntimeId：**距离 ≤ hitRadius** 视为碰撞命中 → Session `TryConfirmRangedHit` → 怪 `HP -= NormalAttackPower`；**超时**销毁且不扣血。法师/射手同远程通道（仅 `PrimaryStat` 不同）。禁止运行时引用 `SmallScaleInt/`。

**Defend 失控开战 roll 与胜负结算（方案 A，D-043）：** `ConfigCsvRepository` 加载 `Combat_LossOfControlConfig.csv`。开战瞬间按布阵 `ΣCost/Cap−1` **锁定** Degree/Tier（超额不挡开战）；`Degree>0` 时对各上阵士兵用 `FinalLossChance=clamp(0,1,TierChance+RaceBonus+ΣGemBonus)`（Demo `ΣSkillBonus=0`）独立 roll → `IsRebel`（日志可观察）。Rebel **不受 EngageZone 限制**，就近打存活主角/其他士兵/敌人；对主角普攻 → `Shield-=1`；对兵/怪走士兵普攻通道。清场条件满足 → `DefendPhase.Ended` + PermanentDeath 最小结算（宝石回仓、清布阵、移出池）→ `ProtagonistProgressService.AddExperience(100)`（Demo 固定阶段经验）→ `LevelOperationDriver.TryAdvanceStage`。`Shield≤0` → Ended + 同 PermanentDeath 结算 → **不**入账本阶段经验 → `AbortLevelAsFailure`（无关卡胜利结算；已有资源保留）。禁止运行时引用 `SmallScaleInt/`。

**架构提示：** `ToolsPanel` 属 Meta 壳层 UI；玩法状态由规则层持有，View 只订阅展示（见 §13）。挖坟：规则层负责生成、计时、DigAction 触发/忙碌锁与扣血；菱形地图与圆圈光标、帧动画、奖励飞向为主角由 View 表现；逻辑层为整体可放置空间（非格子）。UM 阶段不查玩法配置表主键；升级进度本片内存持有。Defend：规则层输出目标/目的地；NavMeshAgent 移动；Demo 最小可走面见 §9.7 / SPEC_03 §3.12。

### English

**Status: Aligned with SPEC_03 §3.8 (Meta shell + Level-pipeline vertical slice)**

**In scope (D-001–D-043):** 3 fixed slots with local occupied flag + minimal pipeline fields; InSaveShell default Dig placeholder + floating Tools; sample Level start from shell (D-003/D-010); CSV-only LevelOperation drive Dig→UM→Defend; Dig / UM / Defend verticals per §3.8 (temp art OK); UM stage `GameplayConfigId` **ignored** (§9.1); Defend Demo-min spawn + NavMesh.

**Out of scope:** Full skill casts / effect tables; formal art polish; exact OutsideMap geometry; full save schema; concrete TechTree node values/icon polish & full feature-system enum; future Tools entries; Editor bake-tool impl; anything not in §3.8.

**Persistence intent:** Local by slot index `0..2`. **Demo Meta locked: `PlayerPrefs`**; keys `Gravedigger2026.SaveSlot.{0|1|2}.Occupied` (`0`/`1`). Occupied flag only this slice; full schema **TBD**; pipeline minimal fields backfilled in later slices.

**Meta shell (Approach A, D-001–D-004):** Single scene `Assets/Scenes/Boot.unity`; SaveSelect / InSaveShell via Canvas Prefab show/hide (`Assets/Prefabs/Meta/`, `Assets/Prefabs/UI/`). Rules: `SaveSlotService` + `GameplayStateService`; Views subscribe only. Tools Settings → **opens TechTree canvas** (UI-012 below); Tools Level → **starts sample `Level_01`** (see Level driver below). Formal shell three-state switch still **TBD**; Demo temp **Debug cycle** on InSaveShell for hand-checking D-004 (must not equal Tools Level); **Debug advance stage** for D-010 (placeholder end → next / VictorySettlement).

**Level driver (Approach A, D-010):** `ConfigCsvRepository` reads CSV only (paths: [§14.5](#145-runtime-csv-load-paths-demo)); `LevelOperationDriver` loads rows by `LevelId`, runs ascending `StageNumber`; sets `GameplayState` and calls `IStageModule` enter/leave hooks (Dig / UM / Defend below; warrior combat and win/lose still later). When `GameplayType=UpgradeManufacture`, **ignore** `GameplayConfigId` (no Dig/Defend lookup). Dig/Defend rows validate `DigMapId` / `BattleMapId` ∈ `Ground_01`…`Ground_05` and resolve `Assets/Prefabs/Maps/{Id}.prefab`. UI/log must show LevelId, StageNumber, GameplayType.

**Dig vertical (Approach A, D-020):** `DigStageModule` (`IStageModule`) on Enter instantiates `Assets/Prefabs/Maps/{DigMapId}.prefab` and mounts `DigStageRoot` (`Assets/Prefabs/Dig/`). Rules: pure-C# `DigSessionService` owns effective-duration countdown, initial/process spawn, DigAction dwell + busy lock, damage, Warehouse/Spirit credit, stage reward aggregate; `DigProtagonistCapabilities` injected from **TechTree** recalc (see UI-012). Presentation: `DigPrefabCatalog` binds Digger / `Grave_{QualityId}` / map variants; circle cursor, grave HP styles, DigReward fly-to, DigStageSummary via Views. Duration 0 → cancel in-progress DigAction (no damage) → DigStageSummary confirm → `LevelOperationDriver.TryAdvanceStage`. Do not runtime-reference `SmallScaleInt/`.

**TechTree canvas (Approach A, UI-012 optional):** `ConfigCsvRepository` additionally loads `Tech_TechTreeConfig.csv` / `Tech_TechEffectConfig.csv`. Rules: pure-C# `TechTreeService` (save-scoped on Meta shell) holds learned set + `UnlockedFeatureSystems`; on enter-save/`Reset`, auto-learns `InitiallyUnlocked` and applies effects; learn gate = not learned ∧ TechPoint≥LearnCost ∧ ≥1 learned prerequisite (inverse of `UnlockNextTechIds`); on learn spend → mark → parse additive `AttributeModifiers` → write `DigProtagonistCapabilities` (Demo keeps `DiggableQualityIds` = all grave qualities for Dig hand-check). Presentation: temp `Assets/Prefabs/Meta/TechTreeCanvasRoot.prefab` (node positions on Prefab; uGUI LMB-drag pan; hover name+effect desc; edges from forward ids; three-state frame colors); Tools Settings opens canvas; Debug can inject TechPoints. Do not runtime-reference `SmallScaleInt/`.

**UM upgrade panel (Approach A, D-030):** `UpgradeManufactureStageModule` on Enter instantiates `Assets/Prefabs/UpgradeManufacture/UpgradeManufactureStageRoot.prefab` (three panels: upgrade / manufacture / formation all operable). `ConfigCsvRepository` loads `Manufacture_ProtagonistLevelConfig.csv`. Rules: pure-C# `ProtagonistProgressService` holds in-memory `Level` / `LifetimeExperience` / `TechPoints` / effective `ControlPowerCap` / `ProtagonistMaxHP`; cumulative-threshold chain level-ups apply row rewards/caps. Debug inject remains; formal Defend victory credit in D-043. Bottom Complete → `TryAdvanceStage`. Do not runtime-reference `SmallScaleInt/`.

**UM manufacture panel (Approach A, D-031):** `ConfigCsvRepository` additionally loads `Manufacture_SoulConfig` / `ClassConfig` / `GemConfig` / `RaceConfig` / `BodyPartConfig` / `BodyAppearanceConfig` / `ExtraEquipmentConfig` / `GemSuffixNameConfig`. Rules: pure-C# `ManufactureService` owns the 15 strict slots (Head1/Torso1/Arm2/Leg2/Soul1/Gem6/Mount1/Wing1), routes an Id to a legal empty slot by resolving its source table, and rejects on type mismatch / duplicate `GemType` / insufficient stock; every slot change recomputes the preview (`Base(S)=Σ StatBonus`, `Equip`, `GemMult`, `RaceAdjust`, `StaticStat`, static `MaxHP=ceil(BodyLife+StaticStat(Str)×3)`, `TotalSpiritCost`, `ControlPowerCost`, trial Race + Appearance). Manufacture gate = min parts (Torso+2Arm+2Leg+Soul) and `SpiritEssence ≥ TotalSpiritCost`; commit deducts each placed Id and the Spirit, finalizes Race (weight-1 pick) and Appearance (§9.13), builds `WarriorName`, and writes the `WarriorInstance` snapshot into pure-C# `WarriorPoolService` (save-scoped; consumed by 04c formation / 05x combat). `WarehouseService` extended: `BodyPartId` credited in the same Id namespace as `MaterialId` (`AutoConvert` from the BodyPart row), per-Id consume, Spirit spend; Souls / Gems / ExtraEquipment share the same item store for Demo (acquisition **TBD**) and are provided by a manufacture-panel **Debug "grant starter kit"** this slice. Appearance assets: `UpgradeManufacturePrefabCatalog` gains `AppearanceId → Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab` bindings (temp capsules; resolved and logged at manufacture, actual instantiation deferred to 04c/05b). Race display name falls back to `DisplayNameKey` per §9.11 while i18n is off. Do not runtime-reference `SmallScaleInt/`.

**UM formation panel (Approach A, D-032):** Pure-C# `BattleFormationService` (save-scoped beside `WarriorPoolService` on Meta shell) holds deploy entries `{WarriorId, PositionX/Z, RemainingHP}` on **BattleMap continuous coordinates** (not a grid). Deploy copies the pool instance's current `RemainingHP`; undeploy writes HP back and clears the entry; reposition nudges X/Z by a step. ControlPower usage = Σ deployed `ControlPowerCost` vs `ProtagonistProgressService.ControlPowerCap` (Degree = `ΣCost/Cap − 1`, rough UI OK). Presentation: `FormationPanelView` (button deploy/undeploy/nudge; no real drag) in the third on-screen panel; **same Service** shared with Defend `Prepare` (Prepare UI reused in D-040). This slice does not require instantiating `Ground_*` preview (formal battle maps still resolve `BattleMapId`→`Prefabs/Maps/`). Bottom Complete → `TryAdvanceStage`. Do not runtime-reference `SmallScaleInt/`.

**Defend Prepare / StartBattle / Shield (Approach A, D-040):** `DefendStageModule` (`IStageModule`) on Enter instantiates `Assets/Prefabs/Defend/DefendStageRoot.prefab` and a **stage-separate** `Assets/Prefabs/Maps/{BattleMapId}.prefab` (not reusing Dig's map instance; Exit destroys this stage root). Rules: pure-C# `DefendSessionService` owns `DefendPhase` (Prepare→Combat), `Shield`, `RemainingCombatSeconds`; StartBattle gate = deployed count ≥ 1. Prepare reuses save-scoped `BattleFormationService` + `FormationPanelView` + UI-009 StartBattle. On StartBattle: `Shield = ProtagonistProgressService.ProtagonistMaxHP`; `RemainingCombatSeconds = DefendGameplayConfig.CombatDurationSeconds` (whole-second countdown; zero alone does not end); spawn temp `Assets/Prefabs/Defend/BattleProtagonist.prefab` at map center; deploy warriors at formation coords (`Prefabs/Defend/Warriors/{AppearanceId}`). `DefendPrefabCatalog` binds StageRoot / BattleProtagonist / `Ground_*` / warrior appearances. EngageZone lives on the map Prefab (targeting not driven this slice). Do not runtime-reference `SmallScaleInt/`.

**Defend spawn + path (Approach A, D-041):** `ConfigCsvRepository` additionally loads `Defend_WaveSpawnConfig.csv` / `Defend_MonsterConfig.csv`. On StartBattle, `DefendSessionService` loads rows for `WaveConfigId`; in `Combat`, whenever `RemainingCombatSeconds` becomes a whole second (including StartBattle instant), fires unfired rows with matching `SpawnRemainingSeconds` (`SpawnOrder` ascending within the same second) via events to View. Demo-min spawn: fixed `DefendSpawnPointSet` on map Prefab (`ClockDirection`→clock markers; `RegionRandom`→pool pick; Inside/Outside both use fixed points this slice; exact OutsideMap **deferred**). After map instantiate, runtime-bake a minimal walkable NavMesh covering activity area + spawn points. Monster Prefabs: `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab` (temp cubes; Catalog-bound). `MonsterAgentView` (`NavMeshAgent`) picks destination by `TargetSelect` (warrior transforms are PreferWarrior/Nearest candidates; fall back to protagonist), repaths on `TargetRetargetIntervalSeconds`; when in `AttackRange`, normal-attacks at `AttackSpeed` → `Shield -= 1` (ignore AttackPower). `Shield ≤ 0` → `DefendPhase.Ended` + LevelFailure hook (log; full Level abort in D-043). Soldier attacks / clear-spawn victory **out of scope**. Do not runtime-reference `SmallScaleInt/`.

**Defend warrior melee (Approach A, D-042 melee slice):** `WarriorCombatMath` derives `NormalAttackPower` / `AttackSpeed` from `ClassConfig.PrimaryStat` + `CombatConvertCoeffs` (missing keys → §3.12 defaults). `DefendSessionService` registers warrior HP at StartBattle (`MaxHP=ceil(BodyLife+StaticStat(Str)×3)`, RemainingHP clamped) and monster HP on spawn; rules confirm melee `HitConfirm` (windup end + target alive + in `AttackRange`) → monster `HP -= NormalAttackPower`; monster→warrior uses `AttackPower` directly (no armor). `HP≤0` without gems → `CombatDead` (stop acting); with gems → immediate PermanentDeath mark (material fate in D-043). Presentation: `WarriorAgentView` (`NavMeshAgent`) picks nearest living monster inside EngageZone; `AttackMode=Melee` uses windup. Clear condition (all wave rows fired + all spawned monsters dead) → `ClearVictoryConditionDetected` event/log (**no** Exp credit, **no** victory Ended; see D-043). Do not runtime-reference `SmallScaleInt/`.

**Defend warrior ranged projectile (Approach A, D-042 ranged / 05c2):** StartBattle registration also stores `ClassConfig.RangedProjectileSpeed` / `RangedTimeoutSeconds`. `AttackMode=Ranged` shares EngageZone nearest targeting and `AttackSpeed` cadence with melee; when in `AttackRange`, Instantiate temp `Assets/Prefabs/Defend/Projectile.prefab` (Catalog-bound). `ProjectileView` flies kinematically toward locked monster RuntimeId: **distance ≤ hitRadius** = collision hit → Session `TryConfirmRangedHit` → monster `HP -= NormalAttackPower`; **timeout** destroys with no damage. Mage/Archer share the same ranged channel (`PrimaryStat` only differs). Do not runtime-reference `SmallScaleInt/`.

**Defend LossOfControl StartBattle roll + win/lose settle (Approach A, D-043):** `ConfigCsvRepository` loads `Combat_LossOfControlConfig.csv`. At StartBattle lock Degree/Tier from formation `ΣCost/Cap−1` (overflow does not block StartBattle); when `Degree>0`, each deployed soldier rolls once with `FinalLossChance=clamp(0,1,TierChance+RaceBonus+ΣGemBonus)` (Demo `ΣSkillBonus=0`) → `IsRebel` (logged). Rebels **ignore EngageZone**, pick nearest living protagonist / other soldiers / enemies; normal hit on protagonist → `Shield-=1`; hits on soldiers/monsters use soldier attack channel. Clear condition → `DefendPhase.Ended` + minimal PermanentDeath (gems→warehouse, clear formation, remove pool) → `ProtagonistProgressService.AddExperience(100)` (Demo fixed stage Exp) → `LevelOperationDriver.TryAdvanceStage`. `Shield≤0` → Ended + same PermanentDeath settle → **no** stage Exp → `AbortLevelAsFailure` (no VictorySettlement; keep already-owned). Do not runtime-reference `SmallScaleInt/`.

**Architecture note:** ToolsPanel is Meta shell UI; gameplay state owned by rules layer; View subscribes only (§13). Dig: rules owns spawn/timer/DigAction/busy/damage; diamond map, circle cursor, dig anims, DigReward fly-to are View; continuous placeable space. UM stages do not resolve mode-config PKs; upgrade progress is in-memory this slice. Defend: rules outputs target/destination; NavMeshAgent moves; Demo-min walkable surface in §9.7 / SPEC_03 §3.12.

---

## 7. 版本控制

### 简体中文

- Unity 适用 `.gitignore`；不提交 `Library/`、密钥、本地用户设置

### English

- Unity-appropriate `.gitignore`; never commit secrets or `Library/`

---

## 8. 运行时本地化（建议）

### 简体中文

**状态：可选 / 建议启用（待项目确认）**

### English

**Status: Optional / recommended (pending project decision)**

---

## 9. 配置表（关卡运作 / 挖坟 / 坟墓品质 / 材料 / 货币 / 挖坟能力 / 防守 / 刷怪波次 / 怪物 / 主角升级 / 灵魂 / 宝石 / 种族 / 制造部件 / 躯体外观 / 科技树 / 失控 / 技能骨架）

### 简体中文

**状态：已定义字段与编码；配置载体已关闭** — 表驱动统一为 **Excel 源 + CSV 产物**（路径 / 命名 / 打表见 [§14](#14-配置表工程约定与打表工具)）。非表型单例调参仍可用 ScriptableObject（`Assets/Settings/<模块>/`，见 [§13](#13-资源编排与可扩展性)）。

规则语义权威：[SPEC_03 §3.9](SPEC_03_GameRules.md)、[§3.10](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[§3.13](SPEC_03_GameRules.md)。

逻辑表名（短名，如 `DigGameplayConfig`）用于 SPEC / 伪代码 / 类型标识；**磁盘文件名**见各小节「磁盘名」行与 [§14](#14-配置表工程约定与打表工具)（Excel：`系统中文_表中文_系统英文_表英文`；CSV：`系统英文_表英文`）。

#### 加权字段通用规则

凡配置中的 **权重值（Weight）** 均适用：

| 规则 | 说明 |
|------|------|
| 非负 | `Weight` 须 ≥ 0；负值视为非法配置（加载时拒绝或忽略该段并打日志，实现时二选一并回写） |
| 零权重剔除 | `Weight = 0` → **强制认定该项不存在**：解析后剔除，不参与加权随机 |
| 有效项 | 仅 `Weight > 0` 的项进入加权池；按权重占比抽取 |
| 空有效列表 | 默认语义由各玩法写明。Dig / `GraveSpawnWeights`：过滤后为空 → **放弃该次生成**（见 [SPEC_03 §3.10](SPEC_03_GameRules.md)） |

#### 9.1 关卡运作表 `LevelOperationConfig`

**磁盘名：**
- **Excel：** `关卡_关卡运作表_Level_LevelOperationConfig.xlsx`
- **CSV：** `Level_LevelOperationConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| LevelId | 关卡ID | `string` 或 `int` | 同 ID 多行 = 该关全部阶段 |
| StageNumber | 阶段编号 | `int` | 同关卡内升序执行；建议同关卡内唯一 |
| GameplayType | 玩法类型 | `enum` / `string` | 如 `Dig` / `UpgradeManufacture` / `Defend` |
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | **Dig** → `DigGameplayConfig` 主键；**Defend** → `DefendGameplayConfig` 主键；**UpgradeManufacture** → **忽略**（可不空；运行时**不**查表、**不**解析为 Dig/Defend 行；本阶段读全局表如 `ProtagonistLevelConfig` 等）。**不另开** `UpgradeManufactureGameplayConfig`（见 [SPEC_03 §3.9](SPEC_03_GameRules.md)） |

```
LevelOperationConfig {
  LevelId: Id
  StageNumber: int
  GameplayType: Dig | UpgradeManufacture | Defend | ...
  GameplayConfigId: Id   // ignored when GameplayType = UpgradeManufacture
}
```

#### 9.2 挖坟配置表 `DigGameplayConfig`

**磁盘名：**
- **Excel：** `挖坟_挖坟配置表_Dig_DigGameplayConfig.xlsx`
- **CSV：** `Dig_DigGameplayConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | 主键；被关卡运作表引用 |
| DigMapId | 挖坟地图ID | `string` | Prefab 逻辑名（无路径、无扩展名）；合法值 **`Ground_01`…`Ground_05`**；运行时解析 → `Assets/Prefabs/Maps/{DigMapId}.prefab`；与 Defend `BattleMapId` 共用同一地面变体池（源参考 Example Scene `Ground (1)`…`Ground (5)`，须复制为项目 Prefab，见 [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角)） |
| LevelDurationSeconds | 关卡时长限制 | `float` 或 `int` | **基础**时长（秒）；有效倒计时 = 本字段 + `DigStageDurationBonus`（见 [SPEC_03 §3.10](SPEC_03_GameRules.md) / §9.6） |
| InitialGraveCount | 开局基础生成坟墓数量 | `int` | 开局独立加权随机次数 N（≥ 0） |
| SpawnRate | 倒计时过程中生成坟墓速率 | 见编码 | 每 N 秒生成 M 个 |
| GraveSpawnWeights | 坟墓出现概率权重 | 见编码 | 品质 ID + 权重列表 |

**`SpawnRate` 编码（固定）：** `N;M`

- `N`：间隔秒数（> 0）
- `M`：该间隔内生成数量（≥ 0）
- 示例：`5;2` → 每 5 秒生成 2 座坟

**`GraveSpawnWeights` 编码（固定）：** `QualityId;Weight|QualityId;Weight|...`

- 段分隔符：`|`
- 段内：`坟墓品质ID;出现权重`
- 权重遵循上方 **加权字段通用规则**：`Weight = 0` 的段剔除；仅对 `Weight > 0` 按占比抽取
- 过滤后有效列表为空 → **放弃该次生成**（开局少一座 / 过程跳过一座；不打断阶段）
- `QualityId` 须能在 `GraveQualityConfig`（§9.3）中解析
- 示例：`1;10|2;5|3;1`；`1;0|2;0` → 有效列表空 → 放弃该次生成

```
DigGameplayConfig {
  GameplayConfigId: Id
  DigMapId: string             // Ground_01…Ground_05 → Assets/Prefabs/Maps/{Id}.prefab
  LevelDurationSeconds: number
  InitialGraveCount: int
  SpawnRate: "N;M"          // every N seconds spawn M
  GraveSpawnWeights: "Q;W|Q;W|..."
}
```

**加权随机：** 先按通用规则得到有效权重列表，再做一次独立抽取（开局 N 次各抽一次；过程生成的每一座各抽一次）。抽取算法实现细节不绑定具体 RNG API。

**落点：** 在 DigMap 整体可放置区域内采样；须避开 `DigObstacle`（Digger + 未消除 Grave）的圆形障碍半径（半径在对应 Prefab 上配置）。单次生成采样失败最多重试 **32** 次，仍失败则放弃该次生成。

**Dig Prefab 约定：** `Assets/Prefabs/Dig/` 下 Digger 与各品质 Grave 预制体暴露圆形障碍半径；每种 `QualityId` 对应专属 Grave Prefab。Digger 视觉为 Character Creator **烘焙整角**，固定 Prefab 逻辑名 `Digger` → `Assets/Prefabs/Dig/Digger.prefab`；美术导出源见 [§15](#15-角色美术管线character-creator-烘焙整角)。Dig 地图：`DigMapId` → `Assets/Prefabs/Maps/{DigMapId}.prefab`。

#### 9.3 坟墓品质定义表 `GraveQualityConfig`

**磁盘名：**
- **Excel：** `挖坟_坟墓品质定义表_Dig_GraveQualityConfig.xlsx`
- **CSV：** `Dig_GraveQualityConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| QualityId | 坟墓品质ID | `string` 或 `int` | 主键；被 `GraveSpawnWeights` 引用 |
| MaxHP | 总血量 | `int` 或 `float` | 生成时初始化坟的 maxHP / 当前 HP；具体数值后续填表 |
| LootDrop | 掉落内容 | 见编码 | 挖掘成功（HP=0）时产出的奖励 |
| IconStyleHighId | 高血量图标ID | `string` | 剩余 HP% **>65%** 用；空 = 品质默认 Prefab/图 |
| IconStyleMidId | 中血量图标ID | `string` | 剩余 HP% **30%–65%** 用；空 = 默认 |
| IconStyleLowId | 低血量图标ID | `string` | 剩余 HP% **<30%** 用；空 = 默认 |

```
GraveQualityConfig {
  QualityId: Id
  MaxHP: number
  LootDrop: "Id_Count|Id_Count|..."
  IconStyleHighId: string     // empty = quality default
  IconStyleMidId: string
  IconStyleLowId: string
}
```

**与规则的关系（[SPEC_03 §3.10](SPEC_03_GameRules.md)）：**

- 生成坟时按 `QualityId` 读本表初始化 `GraveHP`。
- 扣血后按剩余 HP% 切换 `GraveIconStyle`（>65% / 30%–65% / <30%）；样式资源分别取自 `IconStyleHighId` / `IconStyleMidId` / `IconStyleLowId`，空则用品质默认 Prefab/图。
- HP 归 0 时按 `LootDrop` 生成 `DigReward` 图标并飞向主角；到达后入账。

**`LootDrop` 编码（固定）：** `Id_Count|Id_Count|...`

- 段分隔符：`|`；段内：`Id_Count`（下划线分隔）。
- `Id` 解析顺序：
  1. **保留精魂 Id** 字符串 **`Spirit`**（大小写敏感）→ 不入材料堆叠，直接加精魂。
  2. **`MaterialConfig.MaterialId`** → 普通材料；`AutoConvert` / UI 图标取自材料表。
  3. **`BodyPartConfig.BodyPartId`** → 躯体材料（与 `MaterialId` **同命名空间，Id 不得冲突**）；堆叠上限同 **10000**；`AutoConvert` 取自躯体表；仓库 / DigReward 外观图可用 `ArtAssetId`。
- `Count`：正整数（≥ 1）。
- 空串、缺下划线、`Count` 非正整数、上述皆未命中的 Id：**忽略该段并打日志**，继续解析其余段。
- 示例：`Iron_3|Spirit_10|Bone_1`

#### 9.4 材料配置表 `MaterialConfig`

**磁盘名：**
- **Excel：** `挖坟_材料配置表_Dig_MaterialConfig.xlsx`
- **CSV：** `Dig_MaterialConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| MaterialId | 材料ID | `string` 或 `int` | 主键；被 `LootDrop` 引用 |
| AutoConvert | 自动兑换 | `int` 或 `float` | 堆叠超上限时，**每 1 个超出材料**兑换的精魂数量（≥ 0；0 = 超出部分丢弃且不兑精魂） |
| AppearanceIconId | 外观图ID | `string` 或 `int` | 仓库 / DigReward 等 UI 用的外观图 Id |
| AssetPath | 素材路径 | `string` | 主素材资源路径（加载约定实现时定） |
| WarehouseQualityOutlineId | 仓库品质外轮廓ID | `string` 或 `int` | **仓库格子**专用品质外轮廓美术资源 Id（专用套，非地图坟图标） |

堆叠上限常量（规则常量，非表字段）：**10000**（见 [SPEC_03 §3.10](SPEC_03_GameRules.md)）。

```
MaterialConfig {
  MaterialId: Id
  AutoConvert: number   // SpiritEssence per 1 excess unit
  AppearanceIconId: Id
  AssetPath: string
  WarehouseQualityOutlineId: Id
}
```

#### 9.5 货币配置表 `CurrencyConfig`

**磁盘名：**
- **Excel：** `挖坟_货币配置表_Dig_CurrencyConfig.xlsx`
- **CSV：** `Dig_CurrencyConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| CurrencyId | 货币ID | `string` 或 `int` | 主键；精魂保留 Id 为字符串 **`Spirit`**（与 `LootDrop` / [SPEC_03 §3.10](SPEC_03_GameRules.md) 对齐） |
| AppearanceIconId | 外观图ID | `string` 或 `int` | UI 外观图 Id |
| AssetPath | 素材路径 | `string` | 主素材资源路径 |
| WarehouseQualityOutlineId | 仓库品质外轮廓ID | `string` 或 `int` | 若货币以同类格子展示，复用仓库格品质外轮廓资源 Id |

```
CurrencyConfig {
  CurrencyId: Id          // e.g. "Spirit"
  AppearanceIconId: Id
  AssetPath: string
  WarehouseQualityOutlineId: Id
}
```

本版至少须有一行 `CurrencyId = Spirit`（精魂）。

#### 9.6 挖坟主角能力（运行时派生；由科技效果重算）

科技学会的结果写入存档主角的 `DigProtagonistCapabilities`（节点表与费用见 [§9.16](#916-科技树配置表-techtreeconfig) / [§9.17](#917-科技项效果配置表-techeffectconfig)；规则见 [SPEC_03 §3.13](SPEC_03_GameRules.md)）：

```
DigProtagonistCapabilities {
  DigDamage: number
  DigDurationReductionSum: number   // seconds; sum of unlock shorten effects
  DigCursorRadius: number
  DiggableQualityIds: set<QualityId>
  DigStageDurationBonus: number     // seconds; additive to LevelDurationSeconds
}
// DigActionDuration = max(0.1, 0.8 - DigDurationReductionSum)
// EffectiveDigDuration = LevelDurationSeconds + DigStageDurationBonus
// Recalc from sum of learned TechEffectConfig.AttributeModifiers (additive per key)
```

#### 9.7 防守配置表 `DefendGameplayConfig`

**磁盘名：**
- **Excel：** `防守_防守配置表_Defend_DefendGameplayConfig.xlsx`
- **CSV：** `Defend_DefendGameplayConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | 主键；被关卡运作表引用 |
| BattleMapId | 战斗地图ID | `string` | Prefab 逻辑名（无路径、无扩展名）；合法值 **`Ground_01`…`Ground_05`**（与 Dig `DigMapId` 共用地面变体池）；运行时解析 → `Assets/Prefabs/Maps/{BattleMapId}.prefab`（含 EngageZone；Demo 时从 Example Scene `Ground (N)` 复制为项目 Prefab，见 [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角)） |
| WaveConfigId | 波次配置ID | `string` 或 `int` | FK → `WaveSpawnConfig`（§9.18）分组键 |
| CombatDurationSeconds | 战斗总时长 | `int` | 开战倒计时初值（整秒）；剩余秒用于刷怪激活；归零不单独判胜负 |
| TargetRetargetIntervalSeconds | 目标修正间隔 | `float` | 怪物与士兵重算可攻击目的地的间隔（秒）；默认 **1** |

```
DefendGameplayConfig {
  GameplayConfigId: Id
  BattleMapId: string          // Ground_01…Ground_05 → Assets/Prefabs/Maps/{Id}.prefab
  WaveConfigId: Id             // FK → WaveSpawnConfig
  CombatDurationSeconds: int   // whole seconds; countdown init
  TargetRetargetIntervalSeconds: number  // default 1
}
```

**寻路技术约定（与 [SPEC_03 §3.12](SPEC_03_GameRules.md) 配套）：**

- 采用 Unity **NavMesh**（或项目统一封装的等价 Agent）在 BattleMap 连续可走空间上寻路。
- **规则层**只输出：当前目标实体 ID + 可攻击目的地世界坐标；**不**直接写 `Transform` / `Animator`。
- **移动层**（NavMeshAgent 或等价）执行 `SetDestination`；每隔 `TargetRetargetIntervalSeconds` 由规则层触发目的地重算并请求重寻路（怪物与士兵均适用）。
- **EngageZone**：挂在 BattleMap **Prefab** 上的轴对齐方形选敌区（比地图稍小；策划调位置/尺寸）；非叛变士兵仅在此区内选最近敌人。见 [SPEC_03 §3.12](SPEC_03_GameRules.md) WarriorCombat。
- 士兵命中方案 D：按 `SoulConfig.AttackMode`（制造写入实例）走近战前摇或远程弹道；`AttackRange` / 前摇 / 弹速 / 超时取自 [§9.9b `ClassConfig`](#99b-职业配置表-classconfig)（规则层确认伤害；View 播动作/弹道）。**第一版 Demo**：士兵与怪物仅普通攻击，不施放技能。
- 障碍烘焙与 NavMesh 表面范围：**Demo 最小** — 在 `Prefabs/Maps/{BattleMapId}` 可走面烘焙（或运行时等价）最小可走 NavMesh；须覆盖地图内主角/士兵活动区，并允许从 Demo 固定出生点进入可走区。复杂障碍与精确 OutsideMap 外围衔接 **后置**（规则见 [SPEC_03 §3.12](SPEC_03_GameRules.md)）。
- **Demo 刷怪点最小：** 地图 Prefab 上临时固定出生点（SerializeField / 子节点标记）或 `InsideMap` 可走面随机；`ClockDirection` 可映射到固定点；精确 OutsideMap 几何后置。

**刷怪 / 怪物表：** 见 §9.18 `WaveSpawnConfig`、§9.19 `MonsterConfig`；失控见 §9.20 `LossOfControlConfig`、§9.21 `SkillConfig`（骨架）；规则见 [SPEC_03 §3.12](SPEC_03_GameRules.md)。

#### 9.8 主角升级配置表 `ProtagonistLevelConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md)。一行 = 一个主角等级。

**磁盘名：**
- **Excel：** `制造_主角升级配置表_Manufacture_ProtagonistLevelConfig.xlsx`
- **CSV：** `Manufacture_ProtagonistLevelConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| Level | 当前等级值 | `int` | 主键；≥ 1；同表内唯一 |
| RequiredTotalExperience | 升到本级需要的经验总值 | `int` 或 `long` | **生涯累计阈值**（非本级增量）；与存档 `LifetimeExperience` 比较；升级 **不**扣减已有经验；1 级行通常为 **0** |
| UnlockedFeatureIds | 升到本级解锁的功能 | 见编码 | **仅预留**；本版无运行时解锁逻辑 |
| TechPointsReward | 升到本级奖励的科技点数 | `int` | 首次进入该等级时发放（≥ 0） |
| ControlPowerCap | 升到本级控制力上限变成的值 | `int` 或 `float` | 该等级下控制力上限绝对值；本版有效上限 = 本字段（科技加成另专题） |
| ProtagonistMaxHP | 升到本级主角的生命值上限 | `int` 或 `float` | 字段名保留；**Defend 开战时作护盾上限**（`Shield` 初值，见 [SPEC_03 §3.12](SPEC_03_GameRules.md)） |

**`UnlockedFeatureIds` 编码（固定）：** `FeatureId|FeatureId|...`

- 段分隔符：`|`
- 空字符串 = 无预留项
- 本版解析后可忽略；不得据此驱动玩法解锁

```
ProtagonistLevelConfig {
  Level: int                              // PK, >= 1
  RequiredTotalExperience: number         // cumulative lifetime threshold
  UnlockedFeatureIds: "Id|Id|..."         // reserved; no runtime unlock this version
  TechPointsReward: int
  ControlPowerCap: number
  ProtagonistMaxHP: number
}
```

**升级解析（与 §3.11 配套）：**

- 存档持有 `Level`、`LifetimeExperience`。
- 当存在 `Level+1` 行且 `LifetimeExperience >= RequiredTotalExperience(Level+1)` 时连升；每升一级应用该行奖励与属性。
- 各行具体数值 **TBD**（本批只定表结构与语义）。

#### 9.9 灵魂配置表 `SoulConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵属性构成 / 命名。一行 = 一种灵魂。

**磁盘名：**
- **Excel：** `制造_灵魂配置表_Manufacture_SoulConfig.xlsx`
- **CSV：** `Manufacture_SoulConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| SoulId | 灵魂ID | `string` 或 `int` | 主键 |
| ClassId | 职业ID | `string` 或 `int` | 必填；FK → `ClassConfig`；制造时写入士兵实例 |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged`；士兵普攻命中方案 D 分支（§3.12）；与怪物 `AttackMode` 同枚举。示例：战士类→Melee、射手/法师类→Ranged（法师与射手同远程通道，仅 `ClassConfig.PrimaryStat` 不同） |
| Skills | 可使用技能 | 见编码 | 技能 Id + 等级列表；编码见下；失控加成与 CD 见 [§9.21 `SkillConfig`](#921-技能配置表骨架-skillconfig)；**第一版 Demo 不施放**（字段可留空） |
| AttackPriority | 攻击优先级 | `enum` / `string` | 与怪物 `TargetSelect` **同枚举**：`Nearest` \| `PreferWarrior` \| `PreferProtagonist`；**本批不驱动**选目标（默认 EngageZone 内最近敌人，见 §3.12） |
| MoveStyle | 移动风格 | `enum` / `string` | `Normal` \| `Aggressive` \| `Cautious`；未知值当 `Normal`；本批可不驱动 AI |
| SpiritCost | 精魂消耗 | `int` 或 `float` | 制造时计入总精魂消耗（≥ 0；缺省 0） |
| ControlPowerCost | 控制力占用 | `int` 或 `float` | 该灵魂对士兵 `ControlPowerCost` 的贡献（≥ 0） |

**`Skills` 编码（固定；士兵侧 Soul / Gem / ExtraEquipment 共用）：** `SkillId;Level|SkillId;Level|…`

- 段分隔符：`|`；段内：`技能ID;等级`
- `Level`：正整数（≥ 1）；非法段 **跳过并打日志**
- 空字符串 = 无技能
- 与怪物 `MonsterConfig.Skills`（`SkillId_CdSeconds|…`）**编码不同**（怪物 CD 写在怪物表）

```
SoulConfig {
  SoulId: Id
  ClassId: Id                     // FK → ClassConfig; required
  AttackMode: Melee | Ranged      // soldier normal-attack hit branch (§3.12)
  Skills: "SkillId;Level|..."     // soldier-side; unused cast in Demo v1
  AttackPriority: Nearest | PreferWarrior | PreferProtagonist  // unused for targeting this batch
  MoveStyle: Normal | Aggressive | Cautious  // unknown → Normal
  SpiritCost: number              // >= 0
  ControlPowerCost: number
}
```

**说明：** 原 `InfoTags` **不再**参与 WarriorInfo 主标签生成（主标签 = 定稿种族）。灵魂 **不**改写力量/敏捷/智力本身；通过 `ClassId` 注入职业；通过 `AttackMode` 选择近战/远程命中分支。`ClassName` / `PrimaryStat` / 换算系数见 [§9.9b `ClassConfig`](#99b-职业配置表-classconfig)。

**士兵实例静态快照（制造完成时写入；非独立配置表）：**

```
WarriorInstance {
  Id: Id
  WarriorName: string             // Prefix(es)+RaceName+ClassName+Suffix; ClassName via ClassId → ClassConfig
  RemainingHP: number
  RaceId: Id                      // weight-1 pick from filled BodyParts → RaceConfig
  RaceAdjustCoeff: {              // five dims; missing = 0; may be +/-
    MaxHP: number
    MoveSpeed: number
    Strength: number
    Agility: number
    Intelligence: number
  }                               // copied from RaceConfig at manufacture
  BaseStats: {
    MaxHP: number
    MoveSpeed: number
    Strength: number
    Agility: number
    Intelligence: number
  }                               // Base(S) = Σ StatBonus(S) of filled BodyParts
  AppearanceId: Id                // FK → BodyAppearanceConfig; pick rules §3.11 / §9.13
  SoulId: Id                      // FK → SoulConfig
  ClassId: Id                     // copied from SoulConfig at manufacture; FK → ClassConfig
  AttackMode: Melee | Ranged      // copied from SoulConfig at manufacture
  LockedEquipIds: Id[]            // ExtraEquipment chosen & locked at manufacture
  GemIds: Id[]                    // 0..6; FK → GemConfig; type-exclusive
  GemMult: {                      // five dims; Σ of socketed gems; all 0 if none
    MaxHP: number
    MoveSpeed: number
    Strength: number
    Agility: number
    Intelligence: number
  }
  ControlPowerCost: number        // BodyCost + SoulCost + EquipCost + GemCost at manufacture
}
```

**关联说明：**

- 职业表见 **§9.9b**；躯体材料 / 躯体外观 / 额外装备 / 宝石后缀表见 **§9.12–§9.15**。
- 静态层：`StaticStat(S) = max(0, Base+Equip+Base×GemMult+Base×RaceAdjust)`（不含 Buff）；战斗层：`FinalStat(S)` 另加 `Base×SkillBuff`（先定 `S` 再取来源；各维缺省 0；见 §3.11）。
- **生命维例外**：最终士兵 `MaxHP = ceil(BodyLife + Str×3)`，`BodyLife = Base(MaxHP)+Equip(MaxHP)`；**不**用 `FinalStat(MaxHP)`（§3.11）。
- 战斗派生：`Primary` = `ClassId` → `ClassConfig.PrimaryStat` 对应维；`NormalAttackPower` / `AttackSpeed` / `SkillCooldown` 系数取自 `ClassConfig.CombatConvertCoeffs`（缺键回退 §3.12 全局默认）；`AttackRange` 等命中参数取自同表列。
- 多宝石：实例 `GemMult(S) = Σ` 已镶嵌各宝石的 `GemMult(S)`。
- 士兵 **彻底死亡（PermanentDeath）**：全部 `GemIds` 回仓库；躯体部位/灵魂/外置装备等绑定材料销毁；布阵位清空（见 §3.11）。战斗死亡（无宝石）不触发物资去向；带宝石士兵 HP≤0 立即彻底死亡。
- 种族与职业 **不** 单独计入 `ControlPowerCost`。

#### 9.9b 职业配置表 `ClassConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵职业 / 命名 / 主属性 / 五维→战斗参数换算。一行 = 一种职业。插在 §9.9 与 §9.10 之间；后续节号不变。

**磁盘名：**
- **Excel：** `制造_职业配置表_Manufacture_ClassConfig.xlsx`
- **CSV：** `Manufacture_ClassConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| ClassId | 职业ID | `string` 或 `int` | 主键；被 `SoulConfig.ClassId` 引用 |
| ClassName | 职业名 | `string` | 参与 `WarriorName` 拼接；外观 `ClassAffinity` 精确匹配键；展示用；可为「战士」等，**不是**单位称谓「士兵」 |
| PrimaryStat | 主属性 | `enum` / `string` | `Strength` \| `Agility` \| `Intelligence`；决定普攻 `NormalAttackPower` 所用属性维（§3.12）；示例语义战士→Strength、射手→Agility、法师→Intelligence（以本字段为准，非 ClassName 硬编码） |
| CombatConvertCoeffs | 战斗换算系数 | 见编码 | 将该职业士兵的五维 StaticStat/FinalStat 转为 `NormalAttackPower` / `AttackSpeed` / `SkillCooldown` 等战斗参数时的系数集；编码见下；缺键回退全局默认 |
| AttackRange | 攻击距离 | `float` | 进入攻击态距离（世界单位或项目统一距离单位） |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0；秒；`AttackMode=Melee` 时用 |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0；`AttackMode=Ranged` 时用 |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0；秒；超时未命中 → 未命中 |

**`CombatConvertCoeffs` 编码（固定）：** `键_数值|键_数值|…`

| 键 | 默认（缺键回退） | 公式角色（§3.12） |
|----|------------------|-------------------|
| `NormalAttackPrimaryMult` | `1.5` | `NormalAttackPower = Primary × 本系数` |
| `AttackSpeedBase` | `0.5` | `AttackSpeed = 本系数 + AttackSpeedAgiDiv/max(Agi,1)` |
| `AttackSpeedAgiDiv` | `60` | 见上 |
| `SkillCdIntDiv` | `30` | `SkillCooldown = max(SkillCdFloor, BaseCooldownSeconds − 本系数/max(Int,1))` |
| `SkillCdFloor` | `0.1` | 见上 |

- 示例：`NormalAttackPrimaryMult_1.5|AttackSpeedBase_0.5|AttackSpeedAgiDiv_60|SkillCdIntDiv_30|SkillCdFloor_0.1`
- 空串 = 全部回退默认；非法段跳过并打日志
- **不**含 `AttackRange` / 前摇 / 弹速（独立列）

```
ClassConfig {
  ClassId: Id
  ClassName: string               // WarriorName + ClassAffinity match key
  PrimaryStat: Strength | Agility | Intelligence
  CombatConvertCoeffs: "Key_Value|..."  // missing key → global default
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
}
```

**解析：**

- 制造时：`SoulConfig.ClassId` → 写入 `WarriorInstance.ClassId`；命名与外观取本行 `ClassName`。
- 战斗派生：先查本表取 `PrimaryStat` 与 `CombatConvertCoeffs`；命中参数取本行 `AttackRange` 等列。
- 职业列表与具体数值后续填表。

#### 9.10 宝石配置表 `GemConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵属性构成 / 宝石。一行 = 一种宝石。

**磁盘名：**
- **Excel：** `制造_宝石配置表_Manufacture_GemConfig.xlsx`
- **CSV：** `Manufacture_GemConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GemId | 宝石ID | `string` 或 `int` | 主键 |
| GemType | 宝石类型 | `enum` / `string` | 六类之一：`Ruby` \| `Sapphire` \| `Emerald` \| `Topaz` \| `Amethyst` \| `Diamond`；制造槽 **同类型互斥**；展示名可后补本地化 |
| GemMult.MaxHP | 生命值放大系数 | `float` | 缺省视为 **0**；代入 `Base(MaxHP) × GemMult.MaxHP` |
| GemMult.MoveSpeed | 移动速度放大系数 | `float` | 同上 |
| GemMult.Strength | 力量放大系数 | `float` | 同上 |
| GemMult.Agility | 敏捷放大系数 | `float` | 同上 |
| GemMult.Intelligence | 智力放大系数 | `float` | 同上 |
| Skills | 额外技能 | 见编码 | 额外一套技能（SkillId + 等级）；编码同 [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig)：`SkillId;Level\|…`；技能失控加成见 [§9.21](#921-技能配置表骨架-skillconfig) |
| SpiritCost | 精魂消耗 | `int` 或 `float` | 制造时计入总精魂消耗（≥ 0；缺省 0） |
| ControlPowerCost | 控制力占用 | `int` 或 `float` | 该宝石对士兵 `ControlPowerCost` 的贡献（≥ 0） |
| LossOfControlChanceBonus | 失控概率加成 | `float` | 可正可负；缺省 **0**；多宝石时对实例已镶嵌宝石该字段 **求和**（§3.11） |

```
GemConfig {
  GemId: Id
  GemType: Ruby | Sapphire | Emerald | Topaz | Amethyst | Diamond
  GemMult: {
    MaxHP: number
    MoveSpeed: number
    Strength: number
    Agility: number
    Intelligence: number
  }                               // missing dim = 0
  Skills: "SkillId;Level|..."     // same as SoulConfig.Skills
  SpiritCost: number
  ControlPowerCost: number
  LossOfControlChanceBonus: number // +/- ; default 0; Σ across socketed gems
}
```

**库存语义：** 宝石为可回仓物品；士兵 **彻底死亡** 时将实例 `GemIds` **全部归还仓库**（其余绑定材料销毁）；带宝石士兵 HP≤0 立即彻底死亡。制造时实例五维 `GemMult(S) = Σ` 已镶嵌宝石该维（无镶嵌则五维全 0）。获取途径与具体数值后续填表。

#### 9.11 种族配置表 `RaceConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵属性构成 / 种族 / 命名。一行 = 一种种族。

**磁盘名：**
- **Excel：** `制造_种族配置表_Manufacture_RaceConfig.xlsx`
- **CSV：** `Manufacture_RaceConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| RaceId | 种族ID | `string` 或 `int` | 主键；被躯体部位 `RaceId` 引用 |
| DisplayNameKey | 展示名 Key | `string` | UI / `WarriorName` 种族段；约定格式 **`Race.{RaceId}.Name`**；未启用 i18n 时可将本字段直接当展示字符串回退 |
| RaceAdjustCoeff.MaxHP | 生命值调整系数 | `float` | 可正可负；缺省视为 **0** |
| RaceAdjustCoeff.MoveSpeed | 移动速度调整系数 | `float` | 同上 |
| RaceAdjustCoeff.Strength | 力量调整系数 | `float` | 同上 |
| RaceAdjustCoeff.Agility | 敏捷调整系数 | `float` | 同上 |
| RaceAdjustCoeff.Intelligence | 智力调整系数 | `float` | 同上 |
| LossOfControlChanceBonus | 失控概率加成 | `float` | 可正可负；缺省 **0**；代入士兵最终失控率（§3.11） |

```
RaceConfig {
  RaceId: Id
  DisplayNameKey: string?
  RaceAdjustCoeff: {
    MaxHP: number
    MoveSpeed: number
    Strength: number
    Agility: number
    Intelligence: number
  }
  LossOfControlChanceBonus: number   // +/- ; default 0
}
```

**解析：** 制造时对已放入躯体部位（头/躯干/臂/腿）各权重 **1** 按部位 `RaceId` **加权随机**定稿 → 查本表，将五维系数写入 `WarriorInstance.RaceAdjustCoeff`；按项代入 `Base(S) × RaceAdjust(S)`。失控判定时取本行 `LossOfControlChanceBonus`。具体种族列表与数值后续填表。

#### 9.12 躯体材料配置表 `BodyPartConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 制造槽位 / 种族加权 / BaseStats / 仓库入账。一行 = 一种躯体部位材料。亦可称 **躯体材料表**。

**磁盘名：**
- **Excel：** `制造_躯体材料配置表_Manufacture_BodyPartConfig.xlsx`
- **CSV：** `Manufacture_BodyPartConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| BodyPartId | 躯体ID | `string` 或 `int` | 主键；可被 `LootDrop` / 仓库引用；与 `MaterialConfig.MaterialId` **同命名空间，不得冲突** |
| BodyLevel | 躯体等级 | `int` 或 `float` | ≥ 0；参与制造时外观平均等级 |
| BodySlot | 躯体部位 | `enum` | `Head` / `Torso` / `Arm` / `Leg`；决定可放入的制造槽 |
| RaceId | 种族 | `string` 或 `int` | FK → `RaceConfig`；参与加权定种族 |
| ControlPowerCost | 控制力占用值 | `int` 或 `float` | ≥ 0；计入制造 `BodyCost` |
| SpiritCost | 精魂消耗 | `int` 或 `float` | ≥ 0；缺省 0；计入制造总精魂消耗 |
| StatBonus | 增加的属性值 | 见编码 | 五项基础属性平坦加成；制造时按维 **Σ** 得 `Base(S)` |
| AutoConvert | 超上限兑精魂 | `int` 或 `float` | 语义同 `MaterialConfig.AutoConvert`：每 1 个超出材料兑精魂数（≥ 0；0 = 超出丢弃且不兑） |
| Description | 文字介绍 | `string` | 展示文案；若启用 i18n 可为本地化 Key |
| ArtAssetId | 外形美术素材ID | `string` 或 `int` | 部位单件外观 / 仓库 UI 可用资源 Id |

**`StatBonus` 编码（固定）：** `属性项_数值|属性项_数值|…`（与 [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers` 同风格；加法；空 = 无加成）。属性项键与五项 BaseStats 对齐（如 `MaxHP` / `MoveSpeed` / `Strength` / `Agility` / `Intelligence`）。

```
BodyPartConfig {
  BodyPartId: Id                  // also warehouse / LootDrop material Id
  BodyLevel: number
  BodySlot: Head | Torso | Arm | Leg
  RaceId: Id                      // FK → RaceConfig
  ControlPowerCost: number
  SpiritCost: number              // >= 0; default 0
  StatBonus: "Attr_Value|..."     // additive; empty = none
  AutoConvert: number             // SpiritEssence per 1 excess unit
  Description: string
  ArtAssetId: Id
}
```

**解析：**

- `Base(S) = Σ` 已放入躯体部位的 `StatBonus(S)`（缺省维 **0**）；见 [SPEC_03 §3.11](SPEC_03_GameRules.md)。
- 仓库入账：`BodyPartId` 按材料堆叠；超限按本行 `AutoConvert` 兑精魂。
- 具体数值行 **TBD**。

#### 9.13 躯体外观配置表 `BodyAppearanceConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵整体外观选取。一行 = 一种预设整体外观造型。

**磁盘名：**
- **Excel：** `制造_躯体外观配置表_Manufacture_BodyAppearanceConfig.xlsx`
- **CSV：** `Manufacture_BodyAppearanceConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| AppearanceId | 外观ID | `string` 或 `int` | 主键；写入 `WarriorInstance.AppearanceId`；**Prefab 逻辑名**（无路径、无扩展名）；运行时解析 → `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`；美术源 `Assets/Art/Characters/Appearances/{AppearanceId}/`（见 [§15](#15-角色美术管线character-creator-烘焙整角)） |
| AppearanceLevel | 外观等级 | `int` | 与制造时平均躯体等级（保留 1 位小数后再四舍五入为整数）匹配 |
| RaceId | 隶属种族 | `string` 或 `int` | FK → `RaceConfig` |
| ClassAffinity | 职业倾向 | 见编码 | 精确匹配 `ClassConfig.ClassName`（经士兵灵魂 `ClassId`） |
| Description | 文字介绍 | `string` | 展示文案；若启用 i18n 可为本地化 Key |
| IsFallback | 保底外形 | `int` 或空 | 空 / `0` = 常规；**`1` = 该种族保底**；**每种族至多 1 行** 为 `1` |

**`ClassAffinity` 编码（固定）：** `ClassName` 或 `ClassName|ClassName|…`（管道分隔；精确匹配；空 = 无职业倾向，仅能靠随机进入候选后的回退路径）。

```
BodyAppearanceConfig {
  AppearanceId: Id                 // Prefab logical name → Assets/Prefabs/Defend/Warriors/{Id}.prefab
  AppearanceLevel: int
  RaceId: Id                      // FK → RaceConfig
  ClassAffinity: "Class|Class|..." // empty = no class affinity
  Description: string
  IsFallback: 0 | 1 | empty       // 1 = race fallback; ≤1 per RaceId
}
```

**选取算法（制造定稿；预览用当前槽位按同算法试算）：**

1. 对已放入全部躯体槽（含可选头部；空槽不计）取 `BodyLevel` 算术平均 → **保留 1 位小数** → 再 **四舍五入为整数** `AvgLevelInt`。
2. 候选集 A = `AppearanceLevel == AvgLevelInt` **且** `RaceId ==` 定稿种族。
3. 若 A 非空：子集 B = `ClassAffinity` 含 `ClassConfig.ClassName`（经灵魂 `ClassId`）的行；若 B 非空 → 在 B 中 **均匀随机**；否则在 A 中 **均匀随机**。
4. 若 A 为空：取同种族 `IsFallback == 1` 的行；有则用之。
5. 若仍无：在 **全表** 中 **均匀随机** 一行。

#### 9.14 额外装备配置表 `ExtraEquipmentConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 外置装备 / 命名前缀。一行 = 一种外置装备。

**磁盘名：**
- **Excel：** `制造_额外装备配置表_Manufacture_ExtraEquipmentConfig.xlsx`
- **CSV：** `Manufacture_ExtraEquipmentConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| EquipId | 装备ID | `string` 或 `int` | 主键 |
| EquipSlot | 装备槽 | `enum` | `Mount` / `Wing` |
| NamePrefix | 名字前缀 | `string` | 参与 `WarriorName`；两件都装备则依次拼接 |
| SpiritCost | 精魂消耗 | `int` 或 `float` | ≥ 0；缺省 0 |
| ControlPowerCost | 控制力占用 | `int` 或 `float` | ≥ 0 |
| EquipStats | 平坦属性加成 | 见编码 | 五项同名平坦加成；加法进 Equip 层 |
| Skills | 额外技能 | 见编码 | 编码同 [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig)；失控加成见 [§9.21](#921-技能配置表骨架-skillconfig) |

**`EquipStats` 编码（固定）：** 与 [§9.12](#912-躯体材料配置表-bodypartconfig) `StatBonus` / [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers` 同风格：`属性项_数值|属性项_数值|…`（键：`MaxHP` / `MoveSpeed` / `Strength` / `Agility` / `Intelligence`；空 = 无；加法）。

**视觉（Mount / Wing）：** 本批默认将坐骑/翅膀外观 **打进** 对应士兵 `AppearanceId` 的烘焙整角变体；**不**做运行时叠装层。运行时模块化叠装另专题。数值与命名字段仍按本表生效。

```
ExtraEquipmentConfig {
  EquipId: Id
  EquipSlot: Mount | Wing
  NamePrefix: string
  SpiritCost: number
  ControlPowerCost: number
  EquipStats: "Attr_Value|..."    // same style as StatBonus
  Skills: "SkillId;Level|..."     // same as SoulConfig.Skills
}
```

#### 9.15 宝石后缀命名表 `GemSuffixNameConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵命名后缀。一行 = 一种已镶嵌宝石组合 → 后缀。

**磁盘名：**
- **Excel：** `制造_宝石后缀命名表_Manufacture_GemSuffixNameConfig.xlsx`
- **CSV：** `Manufacture_GemSuffixNameConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| ComboKey | 组合键 | `string` | 主键；见编码 |
| Suffix | 后缀 | `string` | 拼入 `WarriorName` 末段；无匹配可空 |

**`ComboKey` 编码（固定）：** 取实例已镶嵌宝石的 `GemType` 集合 → **按枚举名字典序排序** → 用 `|` 拼接。例：`Amethyst|Diamond|Ruby`。无宝石 → 不查表，后缀为空。

```
GemSuffixNameConfig {
  ComboKey: string                // sorted GemType names joined by |
  Suffix: string
}
```

**解析：** 制造完成时按实例 `GemIds` → 各 `GemType` → 推导 `ComboKey` 查本表得后缀；无宝石或无匹配行 → 后缀为空。

#### 9.16 科技树配置表 `TechTreeConfig`

规则语义：[SPEC_03 §3.13](SPEC_03_GameRules.md)。一行 = 一个科技项。

**磁盘名：**
- **Excel：** `科技_科技树配置表_Tech_TechTreeConfig.xlsx`
- **CSV：** `Tech_TechTreeConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| TechId | 科技项ID | `string` 或 `int` | 主键 |
| IconId | 科技项图标 | `string` 或 `int` | 图标资源 / 图集键；具体资源路径实现时定 |
| DisplayName | 科技项名字 | `string` | 展示名；若启用 i18n 则为本地化 Key |
| EffectDescription | 科技项效果描述 | `string` | 悬停/详情文案；若启用 i18n 则为本地化 Key |
| UnlockNextTechIds | 解锁后续科技项ID | `string` | 编码 **`Id\|Id\|Id`**（管道分隔）；空 = 叶节点 |
| InitiallyUnlocked | 初始是否解锁 | `bool` | `true` → 新建档 **自动学会** 并应用效果；中心根项须为 `true` |
| LearnCost | 学习费用 | `int` | 科技点；≥ 0；自动学会项通常为 **0** |
| TechUiFrameType | 科技项类型 | `enum` / `string` | `Root` \| `Normal` \| `Key` \| `Capstone`；驱动科技树界面 UI 框样式 |

```
TechTreeConfig {
  TechId: Id
  IconId: Id
  DisplayName: string
  EffectDescription: string
  UnlockNextTechIds: "Id|Id|Id"   // empty = leaf
  InitiallyUnlocked: bool
  LearnCost: int                  // TechPoints; usually 0 if InitiallyUnlocked
  TechUiFrameType: Root | Normal | Key | Capstone
}
```

**约定：**

| 规则 | 说明 |
|------|------|
| 默认镜头 | 表 **第一行** 对应节点 = 打开画布时默认镜头焦点 |
| 中心根项 | 须 `InitiallyUnlocked = true`；建议同时为表第一行 |
| 空间坐标 | **不**在本表；由设置页科技树 Prefab 摆放 |
| 前置 | 不存表；由 `UnlockNextTechIds` 正向边求逆 |

#### 9.17 科技项效果配置表 `TechEffectConfig`

规则语义：[SPEC_03 §3.13](SPEC_03_GameRules.md)。学会某 `TechId` 时应用本表效果。

**磁盘名：**
- **Excel：** `科技_科技项效果配置表_Tech_TechEffectConfig.xlsx`
- **CSV：** `Tech_TechEffectConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| TechId | 科技项ID | `string` 或 `int` | 关联 `TechTreeConfig.TechId`；本版建议 **1:1**；同 ID 多行则效果合并 |
| AttributeModifiers | 增加的属性 | `string` | 编码 **`属性项_数值\|属性项_数值\|…`**（加法）；空 = 无属性增量 |
| UnlockedFeatureSystemName | 解锁的功能系统名 | `string` | 非空 → 写入存档 `UnlockedFeatureSystems`；空 = 无；**开放名单**（PascalCase）；实现时校验已知名，未知名打日志并仍写入集合；名单随玩法扩展回写 SPEC。属性增量（如 `DigDamage`）走 `AttributeModifiers`，**不**走本字段 |

```
TechEffectConfig {
  TechId: Id
  AttributeModifiers: "Attr_Value|Attr_Value|..."  // additive
  UnlockedFeatureSystemName: string               // empty = none; open PascalCase list
}
```

**本版文档化属性键（可扩展）：**

| 属性键 | 写入目标 | 说明 |
|--------|----------|------|
| DigDamage | `DigProtagonistCapabilities.DigDamage` | 挖坟单次伤害 |
| DigDurationReductionSum | `DigProtagonistCapabilities.DigDurationReductionSum` | 缩短单次挖坟时长（秒）；挖坟单次速度 |
| DigCursorRadius | `DigProtagonistCapabilities.DigCursorRadius` | 光标半径（若效果使用） |
| DigStageDurationBonus | `DigProtagonistCapabilities.DigStageDurationBonus` | 挖坟阶段时长加成（秒） |

其它属性键（如控制力上限加成）后续补充；同键多科技 **加法求和** 后写入派生能力。

**功能系统名示例（开放，非封闭）：** `FeatureDigAdvancedQualities` 等；以非空写入存档为准。

#### 9.18 刷怪波次配置表 `WaveSpawnConfig`

规则语义：[SPEC_03 §3.12](SPEC_03_GameRules.md)。一行 = 一次刷怪事件；按 `WaveConfigId` 分组，由 `DefendGameplayConfig.WaveConfigId` 引用。

**磁盘名：**
- **Excel：** `防守_刷怪波次配置表_Defend_WaveSpawnConfig.xlsx`
- **CSV：** `Defend_WaveSpawnConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| WaveConfigId | 波次配置ID | `string` 或 `int` | 分组键；同 ID 多行 = 该防守配置的全部刷怪事件 |
| SpawnOrder | 出怪顺序 | `int` | **仅**当多行 `SpawnRemainingSeconds` 相同时，按升序决定先后；不同剩余秒时忽略相对顺序 |
| SpawnRemainingSeconds | 出怪时间（剩余秒） | `int` | 激活条件：`RemainingCombatSeconds ==` 本字段（整秒） |
| MonsterId | 怪物ID | `string` 或 `int` | FK → `MonsterConfig`（§9.19） |
| SpawnCount | 出现数量 | `int` | ≥ 1 |
| AppearLocation | 出现位置 | `enum` / `string` | `InsideMap` \| `OutsideMap` |
| SpawnMode | 出怪方式 | `enum` / `string` | `RegionRandom` \| `ClockDirection` |
| SpawnClockHour | 几点钟方向 | `int` | 1–12；仅 `SpawnMode = ClockDirection` 时有效；其它方式可空 |

```
WaveSpawnConfig {
  WaveConfigId: Id
  SpawnOrder: int
  SpawnRemainingSeconds: int
  MonsterId: Id
  SpawnCount: int                    // >= 1
  AppearLocation: InsideMap | OutsideMap
  SpawnMode: RegionRandom | ClockDirection
  SpawnClockHour: int?               // 1–12; required when ClockDirection
}
```

**激活规则（与 §3.12 配套）：** `Combat` 中每当剩余整秒等于某行 `SpawnRemainingSeconds` 时触发尚未触发的匹配行；同秒多行按 `SpawnOrder` 升序。出生点精确几何 **TBD**。

#### 9.19 怪物配置表 `MonsterConfig`

规则语义：[SPEC_03 §3.12](SPEC_03_GameRules.md)。一行 = 一种怪物。

**磁盘名：**
- **Excel：** `防守_怪物配置表_Defend_MonsterConfig.xlsx`
- **CSV：** `Defend_MonsterConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| MonsterId | 怪物ID | `string` 或 `int` | 主键；被 `WaveSpawnConfig` 引用 |
| ModelId | 怪物模型ID | `string` | Prefab 逻辑名（无路径、无扩展名）；运行时解析 → `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab`；美术源 `Assets/Art/Characters/Monsters/{ModelId}/`（Character Creator 烘焙整角，见 [§15](#15-角色美术管线character-creator-烘焙整角)） |
| DisplayName | 怪物名称 | `string` | 展示名或本地化 Key（若启用 i18n） |
| TargetSelect | 目标选择 | `enum` / `string` | `Nearest` \| `PreferWarrior` \| `PreferProtagonist`（与 `SoulConfig.AttackPriority` 同枚举） |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged` |
| MaxHP | 怪物血量 | `int` 或 `float` | 生成时初始化怪物 maxHP / 当前 HP |
| MoveSpeed | 怪物移动速度 | `float` | 世界单位/秒或项目统一速度单位 |
| AttackPower | 怪物攻击力 | `int` 或 `float` | **仅**攻击士兵时用于伤害结算；对主角普通攻击不用本字段 |
| AttackSpeed | 攻击速度 | `float` | 攻击频率（具体单位实现时锁定） |
| AttackRange | 攻击距离 | `float` | 进入攻击态距离 |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0；秒；`AttackMode=Melee` 时用 |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0；`AttackMode=Ranged` 时用 |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0；秒；超时未命中 → 未命中 |
| Skills | 怪物技能 | 见编码 | 技能 ID + CD 列表；技能效果列另专题；**第一版 Demo 不生效**（只打普通攻击；实现时可忽略或配空） |
| LootDrop | 怪物掉落 | 见编码 | 击杀产出；编码同 Dig `LootDrop` |

**`Skills` 编码（固定）：** `SkillId_CdSeconds|SkillId_CdSeconds|...`

- 段分隔符：`|`
- 段内：`技能ID_冷却秒`
- 空字符串 = 无技能
- 技能效果定义表本批 **不定**；Demo v1 即使有值也 **不施放**
- **注意：** 与士兵侧 `SkillId;Level|…` **编码不同**（怪物 CD 写在本表）

**`LootDrop` 编码：** 与 [§9.3](#93-坟墓品质定义表-gravequalityconfig) 相同：`Id_Count|Id_Count|...`

```
MonsterConfig {
  MonsterId: Id
  ModelId: string                  // Prefab logical name → Assets/Prefabs/Defend/Monsters/{Id}.prefab; art §15
  DisplayName: string
  TargetSelect: Nearest | PreferWarrior | PreferProtagonist
  AttackMode: Melee | Ranged
  MaxHP: number
  MoveSpeed: number
  AttackPower: number              // soldiers only
  AttackSpeed: number
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
  Skills: "SkillId_Cd|SkillId_Cd|..."
  LootDrop: "Id_Count|Id_Count|..."
}
```

#### 9.20 失控配置表 `LossOfControlConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) / [§3.12](SPEC_03_GameRules.md) 失控程度段与基础失控概率。一行 = 一个失控程度段（固定 4 行：TierId 1~4）。

**磁盘名：**
- **Excel：** `战斗_失控配置表_Combat_LossOfControlConfig.xlsx`
- **CSV：** `Combat_LossOfControlConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| TierId | 失控程度段 | `int` | 主键；**1~4**（轻度 / 中度 / 重度 / 完全）；与 Degree 区间映射见 SPEC_03 §3.11 |
| DisplayName | 名称 | `string` | 展示名或本地化 Key（若启用 i18n） |
| Description | 描述 | `string` | 档位说明文案或本地化 Key |
| LossOfControlChance | 失控概率 | `float` | 该档 **基础**失控概率；取值 **\[0, 1\]**（0=0%，1=100%）；具体数值 **TBD** |

```
LossOfControlConfig {
  TierId: 1 | 2 | 3 | 4
  DisplayName: string
  Description: string
  LossOfControlChance: number     // [0, 1]
}
```

**解析：** 开战锁定 `LossOfControlDegree` 后映射 `TierId` → 读本表 `LossOfControlChance` 作为 `TierChance`。最终失控率与叛变见 SPEC_03 §3.11 / §3.12。

#### 9.21 技能配置表骨架 `SkillConfig`

规则语义：士兵/怪物技能定义表完整效果字段 **另专题**；本批锁定失控加成与 **基础 CD**，供 Soul / Gem / ExtraEquipment 的 `Skills` 列表解析；实际冷却见 §3.12 公式。**第一版 Demo 不施放技能**，本表与 CD 公式均不驱动战斗。**本批不扩效果列**；完整效果列后续沿用同一磁盘文件名扩写。

**磁盘名：**
- **Excel：** `战斗_技能配置表_Combat_SkillConfig.xlsx`
- **CSV：** `Combat_SkillConfig.csv`（完整技能效果列后续扩写时沿用本文件）

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| SkillId | 技能ID | `string` 或 `int` | 主键；被灵魂/宝石/装备/怪物 `Skills` 引用 |
| BaseCooldownSeconds | 基础冷却 | `float` | ≥ 0；单位秒；士兵实际 CD = `max(SkillCdFloor, BaseCooldownSeconds − SkillCdIntDiv/max(Int,1))`（系数见 `ClassConfig.CombatConvertCoeffs` / §3.12） |
| LossOfControlChanceBonus | 失控概率加成 | `float` | 可正可负；缺省 **0**；士兵最终率中 `ΣSkillBonus` = 其全部技能本字段之和（§3.11） |

```
SkillConfig {
  SkillId: Id
  BaseCooldownSeconds: number     // >= 0; actual CD per §3.12
  LossOfControlChanceBonus: number  // +/- ; default 0
  // skill-effect columns deferred; same file when expanded
}
```

**说明：** 当士兵 `ΣSkillBonus ≠ 0` 时，每次释放技能额外用完整 `FinalLossChance` 再判定一次（§3.11）。技能效果、等级成长等其余列 **另专题**（文件名不变）。

### English

**Status: Fields and encodings defined; config carrier closed** — table-driven data uses **Excel source + CSV output** (paths / naming / bake: [§14](#14-配置表工程约定与打表工具)). Non-table singleton tunables may still use ScriptableObject under `Assets/Settings/<Module>/` ([§13](#13-资源编排与可扩展性)).

Rules authority: [SPEC_03 §3.9](SPEC_03_GameRules.md), [§3.10](SPEC_03_GameRules.md), [§3.11](SPEC_03_GameRules.md), [§3.12](SPEC_03_GameRules.md), [§3.13](SPEC_03_GameRules.md).

Logical short names (e.g. `DigGameplayConfig`) are for SPEC / pseudocode / type ids; **on-disk filenames** — see each subsection’s **Disk name** lines and [§14](#14-配置表工程约定与打表工具) (Excel: `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}`; CSV: `{SystemEN}_{TableEN}`).

#### Weighted-field common rules

All config **Weight** values follow:

| Rule | Notes |
|------|-------|
| Non-negative | `Weight` must be ≥ 0; negative = illegal (reject on load or skip segment + log; pick one at implementation and write back) |
| Zero drop | `Weight = 0` → **treat entry as absent**: strip after parse; excluded from weighted pick |
| Effective set | Only `Weight > 0` entries enter the pool; pick by weight share |
| Empty effective list | Per-mode semantics. Dig / `GraveSpawnWeights`: empty after filter → **abandon that spawn** (see [SPEC_03 §3.10](SPEC_03_GameRules.md)) |

#### 9.1 LevelOperationConfig

**Disk name:**
- **Excel:** `关卡_关卡运作表_Level_LevelOperationConfig.xlsx`
- **CSV:** `Level_LevelOperationConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| LevelId | 关卡ID | `string` or `int` | Multiple rows per Level |
| StageNumber | 阶段编号 | `int` | Ascending within Level; unique per Level recommended |
| GameplayType | 玩法类型 | `enum` / `string` | e.g. `Dig` / `UpgradeManufacture` / `Defend` |
| GameplayConfigId | 玩法配置ID | `string` or `int` | **Dig** → `DigGameplayConfig` PK; **Defend** → `DefendGameplayConfig` PK; **UpgradeManufacture** → **ignore** (may be non-empty; runtime must **not** resolve against any mode config / Dig/Defend rows; stage reads global tables such as `ProtagonistLevelConfig`). **No** separate `UpgradeManufactureGameplayConfig` (see [SPEC_03 §3.9](SPEC_03_GameRules.md)) |

#### 9.2 DigGameplayConfig

**Disk name:**
- **Excel:** `挖坟_挖坟配置表_Dig_DigGameplayConfig.xlsx`
- **CSV:** `Dig_DigGameplayConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | PK; referenced by Level Operation |
| DigMapId | 挖坟地图ID | `string` | Prefab logical name (no path/ext); allowed **`Ground_01`…`Ground_05`**; resolve → `Assets/Prefabs/Maps/{DigMapId}.prefab`; shared ground-variant pool with Defend `BattleMapId` (source ref Example Scene `Ground (1)`…`Ground (5)`; copy to project Prefabs — [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角)) |
| LevelDurationSeconds | 关卡时长限制 | `float` or `int` | **Base** duration (seconds); effective countdown = this field + `DigStageDurationBonus` (see [SPEC_03 §3.10](SPEC_03_GameRules.md) / §9.6) |
| InitialGraveCount | 开局基础生成坟墓数量 | `int` | N independent weighted rolls at start |
| SpawnRate | 倒计时过程中生成坟墓速率 | encoding | Every N seconds spawn M |
| GraveSpawnWeights | 坟墓出现概率权重 | encoding | Quality Id + weight list |

**`SpawnRate` encoding (fixed):** `N;M` — every N seconds spawn M (example `5;2`).

**`GraveSpawnWeights` encoding (fixed):** `QualityId;Weight|QualityId;Weight|...` (example `1;10|2;5|3;1`). Follow **Weighted-field common rules**: strip `Weight = 0`; pick among `Weight > 0`. Empty effective list → **abandon that spawn**. `QualityId` must resolve in `GraveQualityConfig` (§9.3). Example empty: `1;0|2;0`.

**Weighted pick:** filter to effective list, then one independent draw per grave (initial and ongoing). RNG API unbound.

**Placement:** sample DigMap continuous placeable space; avoid `DigObstacle` circles (Digger + uncleared Graves; radii on Prefabs). Retry up to **32** times per spawn; then abandon that spawn.

**Dig Prefab convention:** under `Assets/Prefabs/Dig/`, Digger and per-quality Grave Prefabs expose circle obstacle radius; one Grave Prefab per `QualityId`. Digger visuals are Character Creator **baked whole characters**; fixed Prefab logical name `Digger` → `Assets/Prefabs/Dig/Digger.prefab`; art export sources: [§15](#15-角色美术管线character-creator-烘焙整角). Dig map: `DigMapId` → `Assets/Prefabs/Maps/{DigMapId}.prefab`.

#### 9.3 GraveQualityConfig

**Disk name:**
- **Excel:** `挖坟_坟墓品质定义表_Dig_GraveQualityConfig.xlsx`
- **CSV:** `Dig_GraveQualityConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| QualityId | 坟墓品质ID | `string` or `int` | PK; referenced by `GraveSpawnWeights` |
| MaxHP | 总血量 | `int` or `float` | Init grave maxHP / current HP; concrete values filled later |
| LootDrop | 掉落内容 | encoding | Reward when dig succeeds (HP=0) |
| IconStyleHighId | 高血量图标ID | `string` | Remaining HP% **>65%**; empty = quality default Prefab/icon |
| IconStyleMidId | 中血量图标ID | `string` | Remaining HP% **30%–65%**; empty = default |
| IconStyleLowId | 低血量图标ID | `string` | Remaining HP% **<30%**; empty = default |

```
GraveQualityConfig {
  QualityId: Id
  MaxHP: number
  LootDrop: "Id_Count|Id_Count|..."
  IconStyleHighId: string
  IconStyleMidId: string
  IconStyleLowId: string
}
```

**Rules link ([SPEC_03 §3.10](SPEC_03_GameRules.md)):** spawn inits `GraveHP` from this table; remaining HP% drives `GraveIconStyle` via `IconStyleHighId` / `IconStyleMidId` / `IconStyleLowId` (empty → quality default); HP=0 uses `LootDrop` for `DigReward` fly-to-Digger; credit on arrival.

**`LootDrop` encoding (fixed):** `Id_Count|Id_Count|...`

- Segment separator `|`; within segment `Id_Count` (underscore).
- `Id` resolve order:
  1. Reserved Spirit Id string **`Spirit`** (case-sensitive) → credit SpiritEssence (not Warehouse).
  2. **`MaterialConfig.MaterialId`** → normal material; `AutoConvert` / UI icon from MaterialConfig.
  3. **`BodyPartConfig.BodyPartId`** → body material (same Id namespace as `MaterialId`; **must not collide**); stack cap **10000**; `AutoConvert` from BodyPart row; Warehouse / DigReward icon may use `ArtAssetId`.
- `Count`: positive integer (≥ 1).
- Empty / missing underscore / non-positive Count / Id unmatched above: **skip segment and log**, continue.
- Example: `Iron_3|Spirit_10|Bone_1`

#### 9.4 MaterialConfig

**Disk name:**
- **Excel:** `挖坟_材料配置表_Dig_MaterialConfig.xlsx`
- **CSV:** `Dig_MaterialConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| MaterialId | 材料ID | `string` or `int` | PK; referenced by `LootDrop` |
| AutoConvert | 自动兑换 | `int` or `float` | SpiritEssence per **1 excess** material over stack cap (≥ 0; 0 = discard excess, no Spirit) |
| AppearanceIconId | 外观图ID | `string` or `int` | Icon Id for Warehouse / DigReward UI |
| AssetPath | 素材路径 | `string` | Primary asset path (load convention at implementation) |
| WarehouseQualityOutlineId | 仓库品质外轮廓ID | `string` or `int` | Quality outline asset Id for **warehouse slot** display (dedicated set; not DigMap grave icons) |

Stack cap constant (rules, not a table field): **10000** ([SPEC_03 §3.10](SPEC_03_GameRules.md)).

```
MaterialConfig {
  MaterialId: Id
  AutoConvert: number
  AppearanceIconId: Id
  AssetPath: string
  WarehouseQualityOutlineId: Id
}
```

#### 9.5 CurrencyConfig

**Disk name:**
- **Excel:** `挖坟_货币配置表_Dig_CurrencyConfig.xlsx`
- **CSV:** `Dig_CurrencyConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| CurrencyId | 货币ID | `string` or `int` | PK; Spirit reserved Id string **`Spirit`** (aligned with `LootDrop` / [SPEC_03 §3.10](SPEC_03_GameRules.md)) |
| AppearanceIconId | 外观图ID | `string` or `int` | UI icon Id |
| AssetPath | 素材路径 | `string` | Primary asset path |
| WarehouseQualityOutlineId | 仓库品质外轮廓ID | `string` or `int` | If shown in slot-like UI, reuse warehouse quality outline Id |

```
CurrencyConfig {
  CurrencyId: Id          // e.g. "Spirit"
  AppearanceIconId: Id
  AssetPath: string
  WarehouseQualityOutlineId: Id
}
```

This version requires at least one row with `CurrencyId = Spirit`.

#### 9.6 DigProtagonistCapabilities (runtime derived; recalc from tech effects)

Learned tech results write save-slot `DigProtagonistCapabilities` (node table & costs: [§9.16](#916-科技树配置表-techtreeconfig) / [§9.17](#917-科技项效果配置表-techeffectconfig); rules: [SPEC_03 §3.13](SPEC_03_GameRules.md)):

```
DigProtagonistCapabilities {
  DigDamage: number
  DigDurationReductionSum: number   // seconds; sum of unlock shorten effects
  DigCursorRadius: number
  DiggableQualityIds: set<QualityId>
  DigStageDurationBonus: number     // seconds; additive to LevelDurationSeconds
}
// DigActionDuration = max(0.1, 0.8 - DigDurationReductionSum)
// EffectiveDigDuration = LevelDurationSeconds + DigStageDurationBonus
// Recalc from sum of learned TechEffectConfig.AttributeModifiers (additive per key)
```

#### 9.7 DefendGameplayConfig

**Disk name:**
- **Excel:** `防守_防守配置表_Defend_DefendGameplayConfig.xlsx`
- **CSV:** `Defend_DefendGameplayConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | PK; referenced by Level Operation |
| BattleMapId | 战斗地图ID | `string` | Prefab logical name (no path/ext); allowed **`Ground_01`…`Ground_05`** (shared ground-variant pool with Dig `DigMapId`); resolve → `Assets/Prefabs/Maps/{BattleMapId}.prefab` (incl. EngageZone; Demo: copy Example Scene `Ground (N)` into project Prefabs — [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角)) |
| WaveConfigId | 波次配置ID | `string` or `int` | FK → `WaveSpawnConfig` (§9.18) group key |
| CombatDurationSeconds | 战斗总时长 | `int` | Combat countdown init (whole seconds); remaining seconds drive spawn; hitting 0 does not alone decide win/lose |
| TargetRetargetIntervalSeconds | 目标修正间隔 | `float` | Seconds between attackable-destination recomputes for monsters **and soldiers**; default **1** |

```
DefendGameplayConfig {
  GameplayConfigId: Id
  BattleMapId: string          // Ground_01…Ground_05 → Assets/Prefabs/Maps/{Id}.prefab
  WaveConfigId: Id             // FK → WaveSpawnConfig
  CombatDurationSeconds: int   // whole seconds; countdown init
  TargetRetargetIntervalSeconds: number  // default 1
}
```

**Pathfinding tech (with [SPEC_03 §3.12](SPEC_03_GameRules.md)):**

- Use Unity **NavMesh** (or a project-wide equivalent Agent) on BattleMap continuous walkable space.
- **Rules layer** outputs: current target entity Id + attackable world destination; must **not** write `Transform` / `Animator` directly.
- **Movement layer** (NavMeshAgent or equiv.) runs `SetDestination`; every `TargetRetargetIntervalSeconds` the rules layer recomputes destination and requests repath (monsters **and soldiers**).
- **EngageZone**: axis-aligned square on the BattleMap **Prefab** (slightly smaller than the map; designer-tuned); non-Rebel soldiers pick nearest enemy only inside it. See [SPEC_03 §3.12](SPEC_03_GameRules.md) WarriorCombat.
- Soldier hit scheme D: branch by `SoulConfig.AttackMode` (copied onto instance at manufacture) — melee windup or ranged projectile; `AttackRange` / windup / projectile speed / timeout from [§9.9b `ClassConfig`](#99b-职业配置表-classconfig) (rules confirm damage; View plays anim/projectile). **Demo v1**: soldiers and monsters use normal attacks only; no skill casts.
- Obstacle bake / NavMesh surface: **Demo-min** — bake (or runtime-equivalent) a minimal walkable NavMesh on `Prefabs/Maps/{BattleMapId}`; must cover in-map protagonist/soldier area and allow pathing from Demo fixed spawn points onto walkable surface. Complex obstacles and exact OutsideMap perimeter linkage **deferred** (rules: [SPEC_03 §3.12](SPEC_03_GameRules.md)).
- **Demo-min spawn points:** temp fixed markers on map Prefab (SerializeField / child markers) or `InsideMap` random on walkable surface; `ClockDirection` may map to fixed points; exact OutsideMap geometry deferred.

**Spawn / monster tables:** see §9.18 `WaveSpawnConfig`, §9.19 `MonsterConfig`; LossOfControl: §9.20 `LossOfControlConfig`, §9.21 `SkillConfig` (skeleton); rules in [SPEC_03 §3.12](SPEC_03_GameRules.md).

#### 9.8 ProtagonistLevelConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md). One row = one protagonist level.

**Disk name:**
- **Excel:** `制造_主角升级配置表_Manufacture_ProtagonistLevelConfig.xlsx`
- **CSV:** `Manufacture_ProtagonistLevelConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| Level | 当前等级值 | `int` | PK; ≥ 1; unique in table |
| RequiredTotalExperience | 升到本级需要的经验总值 | `int` or `long` | **Cumulative lifetime threshold** (not per-level delta); compared to save `LifetimeExperience`; level-up does **not** deduct owned Exp; level-1 row usually **0** |
| UnlockedFeatureIds | 升到本级解锁的功能 | see encoding | **Reserved only**; no runtime unlock this version |
| TechPointsReward | 升到本级奖励的科技点数 | `int` | Granted when first entering this level (≥ 0) |
| ControlPowerCap | 升到本级控制力上限变成的值 | `int` or `float` | Absolute ControlPower cap at this level; this version effective cap = this field (tech bonus later) |
| ProtagonistMaxHP | 升到本级主角的生命值上限 | `int` or `float` | Field name kept; **Defend StartBattle uses as Shield cap** (`Shield` init, see [SPEC_03 §3.12](SPEC_03_GameRules.md)) |

**`UnlockedFeatureIds` encoding (fixed):** `FeatureId|FeatureId|...`

- Segment separator: `|`
- Empty string = no reserved entries
- May be ignored at runtime this version; must not drive unlocks

```
ProtagonistLevelConfig {
  Level: int
  RequiredTotalExperience: number
  UnlockedFeatureIds: "Id|Id|..."
  TechPointsReward: int
  ControlPowerCap: number
  ProtagonistMaxHP: number
}
```

**Level-up resolution (with §3.11):**

- Save holds `Level`, `LifetimeExperience`.
- While a `Level+1` row exists and `LifetimeExperience >= RequiredTotalExperience(Level+1)`, chain level-ups; each step applies that row's reward and attributes.
- Concrete per-row numbers **TBD** (schema/semantics only this batch).

#### 9.9 SoulConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) soldier attribute composition / naming. One row = one soul.

**Disk name:**
- **Excel:** `制造_灵魂配置表_Manufacture_SoulConfig.xlsx`
- **CSV:** `Manufacture_SoulConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| SoulId | 灵魂ID | `string` or `int` | PK |
| ClassId | 职业ID | `string` or `int` | Required; FK → `ClassConfig`; written to soldier instance at manufacture |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged`; soldier normal-attack hit scheme D branch (§3.12); same enum as monster `AttackMode`. Examples: Warrior-like→Melee; Archer/Mage-like→Ranged (Mage and Archer share Ranged channel; only `ClassConfig.PrimaryStat` differs) |
| Skills | 可使用技能 | encoding | Skill Id + level list; encoding below; LossOfControl bonus and CD in [§9.21 `SkillConfig`](#921-skillconfig-skeleton); **unused cast in Demo v1** (may leave empty) |
| AttackPriority | 攻击优先级 | `enum` / `string` | Same enum as monster `TargetSelect`: `Nearest` \| `PreferWarrior` \| `PreferProtagonist`; **does not drive** targeting this batch (default nearest in EngageZone, §3.12) |
| MoveStyle | 移动风格 | `enum` / `string` | `Normal` \| `Aggressive` \| `Cautious`; unknown → `Normal`; AI drive optional this batch |
| SpiritCost | 精魂消耗 | `int` or `float` | Added to manufacture Spirit total (≥ 0; default 0) |
| ControlPowerCost | 控制力占用 | `int` or `float` | This soul's contribution to soldier `ControlPowerCost` (≥ 0) |

**`Skills` encoding (fixed; shared by Soul / Gem / ExtraEquipment):** `SkillId;Level|SkillId;Level|…`

- Segment separator `|`; within segment `SkillId;Level`
- `Level`: positive integer (≥ 1); illegal segments **skip and log**
- Empty string = no skills
- **Different** from monster `MonsterConfig.Skills` (`SkillId_CdSeconds|…`)

```
SoulConfig {
  SoulId: Id
  ClassId: Id                     // FK → ClassConfig; required
  AttackMode: Melee | Ranged      // soldier normal-attack hit branch (§3.12)
  Skills: "SkillId;Level|..."     // soldier-side; unused cast in Demo v1
  AttackPriority: Nearest | PreferWarrior | PreferProtagonist
  MoveStyle: Normal | Aggressive | Cautious
  SpiritCost: number
  ControlPowerCost: number
}
```

**Note:** Former `InfoTags` no longer builds primary WarriorInfo (primary label = finalized Race). Soul does **not** rewrite Strength/Agility/Intelligence; it injects Class via `ClassId` and selects `AttackMode` for Melee/Ranged hit branch. `ClassName` / `PrimaryStat` / convert coeffs live in [§9.9b `ClassConfig`](#99b-classconfig).

**Soldier instance static snapshot (written at manufacture; not a config table):**

```
WarriorInstance {
  Id: Id
  WarriorName: string             // Prefix(es)+RaceName+ClassName+Suffix; ClassName via ClassId → ClassConfig
  RemainingHP: number
  RaceId: Id
  RaceAdjustCoeff: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }
  BaseStats: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }  // Base(S)=Σ StatBonus(S)
  AppearanceId: Id                // FK → BodyAppearanceConfig
  SoulId: Id
  ClassId: Id                     // copied from SoulConfig; FK → ClassConfig
  AttackMode: Melee | Ranged      // copied from SoulConfig at manufacture
  LockedEquipIds: Id[]
  GemIds: Id[]
  GemMult: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }  // Σ of socketed; 0 if none
  ControlPowerCost: number
}
```

**Related:**

- Class schema: **§9.9b**; BodyPart / BodyAppearance / ExtraEquipment / GemSuffix schemas: **§9.12–§9.15**.
- Static layer: `StaticStat(S) = max(0, Base+Equip+Base×GemMult+Base×RaceAdjust)` (no Buff); combat layer: `FinalStat(S)` also adds `Base×SkillBuff` (pick `S` first; missing dims = 0; see §3.11).
- **HP-dim exception:** final soldier `MaxHP = ceil(BodyLife + Str×3)`, `BodyLife = Base(MaxHP)+Equip(MaxHP)`; do **not** use `FinalStat(MaxHP)` (§3.11).
- Combat derives: `Primary` = `ClassId` → `ClassConfig.PrimaryStat` dim; `NormalAttackPower` / `AttackSpeed` / `SkillCooldown` coeffs from `ClassConfig.CombatConvertCoeffs` (missing key → §3.12 global defaults); hit params from same table columns.
- Multi-gem: instance `GemMult(S) = Σ` of socketed gems' `GemMult(S)`.
- On **PermanentDeath**: all `GemIds` return to Warehouse; BodyParts/Soul/ExtraEquipment and other bound materials are destroyed; formation slot cleared (see §3.11). CombatDead (no gems) does not trigger material fate; gemmed soldiers PermanentDeath immediately on HP≤0.
- Race and Class do **not** add a separate ControlPowerCost term.

#### 9.9b ClassConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) soldier Class / naming / PrimaryStat / five-dim→combat-param convert. One row = one Class. Inserted between §9.9 and §9.10; subsequent section numbers unchanged.

**Disk name:**
- **Excel:** `制造_职业配置表_Manufacture_ClassConfig.xlsx`
- **CSV:** `Manufacture_ClassConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| ClassId | 职业ID | `string` or `int` | PK; referenced by `SoulConfig.ClassId` |
| ClassName | 职业名 | `string` | `WarriorName` segment; exact-match key for appearance `ClassAffinity`; display; may be profession「战士」, **not** unit name「士兵」 |
| PrimaryStat | 主属性 | `enum` / `string` | `Strength` \| `Agility` \| `Intelligence`; selects dim for `NormalAttackPower` (§3.12); example semantics Warrior→Strength, Archer→Agility, Mage→Intelligence (this field wins; not ClassName hardcoding) |
| CombatConvertCoeffs | 战斗换算系数 | encoding | Coeff set for combat derives; encoding below; missing key → global defaults |
| AttackRange | 攻击距离 | `float` | Distance to enter attack state |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0; seconds; used when `AttackMode=Melee` |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0; used when `AttackMode=Ranged` |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0; seconds; timeout → miss |

**`CombatConvertCoeffs` encoding (fixed):** `Key_Value|Key_Value|…`

| Key | Default (missing) | Role in §3.12 |
|-----|-------------------|---------------|
| `NormalAttackPrimaryMult` | `1.5` | `NormalAttackPower = Primary × coeff` |
| `AttackSpeedBase` | `0.5` | `AttackSpeed = base + AttackSpeedAgiDiv/max(Agi,1)` |
| `AttackSpeedAgiDiv` | `60` | see above |
| `SkillCdIntDiv` | `30` | `SkillCooldown = max(SkillCdFloor, BaseCooldownSeconds − div/max(Int,1))` |
| `SkillCdFloor` | `0.1` | see above |

- Example: `NormalAttackPrimaryMult_1.5|AttackSpeedBase_0.5|AttackSpeedAgiDiv_60|SkillCdIntDiv_30|SkillCdFloor_0.1`
- Empty string = all defaults; illegal segments skip and log
- Does **not** include `AttackRange` / windup / projectile (separate columns)

```
ClassConfig {
  ClassId: Id
  ClassName: string
  PrimaryStat: Strength | Agility | Intelligence
  CombatConvertCoeffs: "Key_Value|..."
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
}
```

**Parse:**

- At manufacture: `SoulConfig.ClassId` → write `WarriorInstance.ClassId`; naming / appearance use this row's `ClassName`.
- Combat derives: look up `PrimaryStat` and `CombatConvertCoeffs`; hit params from `AttackRange` etc. columns.
- Class list / concrete numbers filled later.

#### 9.10 GemConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) soldier attribute composition / Gem. One row = one gem.

**Disk name:**
- **Excel:** `制造_宝石配置表_Manufacture_GemConfig.xlsx`
- **CSV:** `Manufacture_GemConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GemId | 宝石ID | `string` or `int` | PK |
| GemType | 宝石类型 | `enum` / `string` | One of: `Ruby` \| `Sapphire` \| `Emerald` \| `Topaz` \| `Amethyst` \| `Diamond`; **type-exclusive** slots; display names may be localized later |
| GemMult.MaxHP | 生命值放大系数 | `float` | Missing = **0**; used as `Base(MaxHP) × GemMult.MaxHP` |
| GemMult.MoveSpeed | 移动速度放大系数 | `float` | Same |
| GemMult.Strength | 力量放大系数 | `float` | Same |
| GemMult.Agility | 敏捷放大系数 | `float` | Same |
| GemMult.Intelligence | 智力放大系数 | `float` | Same |
| Skills | 额外技能 | encoding | Extra skill set; same encoding as [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig): `SkillId;Level\|…`; LossOfControl bonus via [§9.21](#921-skillconfig-skeleton) |
| SpiritCost | 精魂消耗 | `int` or `float` | ≥ 0; default 0 |
| ControlPowerCost | 控制力占用 | `int` or `float` | ≥ 0 |
| LossOfControlChanceBonus | 失控概率加成 | `float` | May be +/-; missing = **0**; **Σ** across socketed gems on the instance (§3.11) |

```
GemConfig {
  GemId: Id
  GemType: Ruby | Sapphire | Emerald | Topaz | Amethyst | Diamond
  GemMult: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }
  Skills: "SkillId;Level|..."
  SpiritCost: number
  ControlPowerCost: number
  LossOfControlChanceBonus: number
}
```

**Inventory:** Gems are Warehouse-returnable; on **PermanentDeath**, **return all** instance `GemIds` to Warehouse (other bound materials destroyed); gemmed soldiers PermanentDeath immediately on HP≤0. At manufacture, instance five-dim `GemMult(S) = Σ` of socketed gems (all zeros if none). Acquisition routes and concrete numbers filled later.

#### 9.11 RaceConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) soldier attribute composition / Race / naming. One row = one race.

**Disk name:**
- **Excel:** `制造_种族配置表_Manufacture_RaceConfig.xlsx`
- **CSV:** `Manufacture_RaceConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| RaceId | 种族ID | `string` or `int` | PK; referenced by BodyPart `RaceId` |
| DisplayNameKey | 展示名 Key | `string` | UI / `WarriorName` race segment; convention **`Race.{RaceId}.Name`**; if i18n off, may use field as display fallback |
| RaceAdjustCoeff.MaxHP | 生命值调整系数 | `float` | May be +/-; missing treated as **0** |
| RaceAdjustCoeff.MoveSpeed | 移动速度调整系数 | `float` | Same |
| RaceAdjustCoeff.Strength | 力量调整系数 | `float` | Same |
| RaceAdjustCoeff.Agility | 敏捷调整系数 | `float` | Same |
| RaceAdjustCoeff.Intelligence | 智力调整系数 | `float` | Same |
| LossOfControlChanceBonus | 失控概率加成 | `float` | May be +/-; missing = **0**; feeds soldier FinalLossChance (§3.11) |

```
RaceConfig {
  RaceId: Id
  DisplayNameKey: string?
  RaceAdjustCoeff: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }
  LossOfControlChanceBonus: number
}
```

**Resolve:** At manufacture, weight-**1** pick among filled BodyParts' `RaceId`s (Head/Torso/Arm/Leg) → look up this table → copy five dims into `WarriorInstance.RaceAdjustCoeff`; each dim feeds `Base(S) × RaceAdjust(S)`. LossOfControl rolls read this row's `LossOfControlChanceBonus`. Concrete race list and values filled later.

#### 9.12 BodyPartConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) manufacture slots / race pick / BaseStats / Warehouse credit. One row = one body-part material (body material table).

**Disk name:**
- **Excel:** `制造_躯体材料配置表_Manufacture_BodyPartConfig.xlsx`
- **CSV:** `Manufacture_BodyPartConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| BodyPartId | 躯体ID | `string` or `int` | PK; usable by `LootDrop` / Warehouse; same Id namespace as `MaterialConfig.MaterialId` (**no collisions**) |
| BodyLevel | 躯体等级 | `int` or `float` | ≥ 0; feeds appearance average level |
| BodySlot | 躯体部位 | `enum` | `Head` / `Torso` / `Arm` / `Leg` |
| RaceId | 种族 | `string` or `int` | FK → `RaceConfig` |
| ControlPowerCost | 控制力占用值 | `int` or `float` | ≥ 0; contributes to manufacture `BodyCost` |
| SpiritCost | 精魂消耗 | `int` or `float` | ≥ 0; default 0; manufacture Spirit total |
| StatBonus | 增加的属性值 | encoding | Flat BaseStat bonuses; `Base(S) = Σ` at manufacture |
| AutoConvert | 超上限兑精魂 | `int` or `float` | Same semantics as `MaterialConfig.AutoConvert` |
| Description | 文字介绍 | `string` | Display copy; localization Key if i18n |
| ArtAssetId | 外形美术素材ID | `string` or `int` | Part visual / Warehouse UI asset Id |

**`StatBonus` encoding (fixed):** `Attr_Value|Attr_Value|…` (same style as [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers`; additive; empty = none). Keys align with five BaseStats (`MaxHP` / `MoveSpeed` / `Strength` / `Agility` / `Intelligence`).

```
BodyPartConfig {
  BodyPartId: Id
  BodyLevel: number
  BodySlot: Head | Torso | Arm | Leg
  RaceId: Id
  ControlPowerCost: number
  SpiritCost: number
  StatBonus: "Attr_Value|..."
  AutoConvert: number
  Description: string
  ArtAssetId: Id
}
```

**Resolve:** `Base(S) = Σ` filled parts' `StatBonus(S)` (missing dim **0**). Warehouse stacks by `BodyPartId`; overflow uses row `AutoConvert`. Concrete values **TBD**.

#### 9.13 BodyAppearanceConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) whole-body visual pick. One row = one preset overall appearance.

**Disk name:**
- **Excel:** `制造_躯体外观配置表_Manufacture_BodyAppearanceConfig.xlsx`
- **CSV:** `Manufacture_BodyAppearanceConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| AppearanceId | 外观ID | `string` or `int` | PK; written to `WarriorInstance.AppearanceId`; **Prefab logical name** (no path/ext); resolve → `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`; art source `Assets/Art/Characters/Appearances/{AppearanceId}/` (see [§15](#15-角色美术管线character-creator-烘焙整角)) |
| AppearanceLevel | 外观等级 | `int` | Matches rounded average BodyLevel |
| RaceId | 隶属种族 | `string` or `int` | FK → `RaceConfig` |
| ClassAffinity | 职业倾向 | encoding | Exact match to `ClassConfig.ClassName` (via soldier soul `ClassId`) |
| Description | 文字介绍 | `string` | Display copy; localization Key if i18n |
| IsFallback | 保底外形 | `int` or empty | Empty/`0` = normal; **`1` = race fallback**; **at most one `1` per RaceId** |

**`ClassAffinity` encoding (fixed):** `ClassName` or `ClassName|ClassName|…` (pipe; exact match; empty = no class affinity).

```
BodyAppearanceConfig {
  AppearanceId: Id                 // Prefab logical name → Assets/Prefabs/Defend/Warriors/{Id}.prefab
  AppearanceLevel: int
  RaceId: Id
  ClassAffinity: "Class|Class|..."
  Description: string
  IsFallback: 0 | 1 | empty
}
```

**Pick algorithm (manufacture finalize; preview uses same algo on current slots):**

1. Mean `BodyLevel` over all filled body slots (incl. optional Head; empty slots excluded) → keep **1 decimal** → **round half-up to int** `AvgLevelInt`.
2. Set A = rows with `AppearanceLevel == AvgLevelInt` **and** `RaceId ==` finalized race.
3. If A non-empty: subset B = rows whose `ClassAffinity` contains `ClassConfig.ClassName` (via soul `ClassId`); if B non-empty → uniform random in B; else uniform random in A.
4. If A empty: use same-race row with `IsFallback == 1` if present.
5. If still none: uniform random over **entire table**.

#### 9.14 ExtraEquipmentConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) external gear / name prefix. One row = one ExtraEquipment.

**Disk name:**
- **Excel:** `制造_额外装备配置表_Manufacture_ExtraEquipmentConfig.xlsx`
- **CSV:** `Manufacture_ExtraEquipmentConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| EquipId | 装备ID | `string` or `int` | PK |
| EquipSlot | 装备槽 | `enum` | `Mount` / `Wing` |
| NamePrefix | 名字前缀 | `string` | `WarriorName`; concatenate if both equipped |
| SpiritCost | 精魂消耗 | `int` or `float` | ≥ 0; default 0 |
| ControlPowerCost | 控制力占用 | `int` or `float` | ≥ 0 |
| EquipStats | 平坦属性加成 | encoding | Flat five-stat bonuses; additive into Equip layer |
| Skills | 额外技能 | encoding | Same as [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig); LossOfControl bonus via [§9.21](#921-skillconfig-skeleton) |

**`EquipStats` encoding (fixed):** same style as [§9.12](#912-躯体材料配置表-bodypartconfig) `StatBonus` / [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers`: `Attr_Value|Attr_Value|…` (keys: `MaxHP` / `MoveSpeed` / `Strength` / `Agility` / `Intelligence`; empty = none; additive).

**Visuals (Mount / Wing):** This batch bakes mount/wing looks **into** the corresponding soldier `AppearanceId` whole-character export; **no** runtime layered overlays. Runtime modular overlays are a later topic. Stat/name fields on this table still apply.

```
ExtraEquipmentConfig {
  EquipId: Id
  EquipSlot: Mount | Wing
  NamePrefix: string
  SpiritCost: number
  ControlPowerCost: number
  EquipStats: "Attr_Value|..."
  Skills: "SkillId;Level|..."
}
```

#### 9.15 GemSuffixNameConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) soldier name suffix. One row = one socketed-gem combo → suffix.

**Disk name:**
- **Excel:** `制造_宝石后缀命名表_Manufacture_GemSuffixNameConfig.xlsx`
- **CSV:** `Manufacture_GemSuffixNameConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| ComboKey | 组合键 | `string` | PK; encoding below |
| Suffix | 后缀 | `string` | Final `WarriorName` segment; empty if no match |

**`ComboKey` encoding (fixed):** take socketed gems' `GemType` set → **sort by enum name** → join with `|`. Example: `Amethyst|Diamond|Ruby`. No gems → skip table; empty suffix.

```
GemSuffixNameConfig {
  ComboKey: string                // sorted GemType names joined by |
  Suffix: string
}
```

**Resolve:** At manufacture complete, derive `ComboKey` from instance `GemIds` → each `GemType` → lookup suffix; no gems / no match → empty suffix.

#### 9.16 TechTreeConfig

Rules: [SPEC_03 §3.13](SPEC_03_GameRules.md). One row = one TechItem.

**Disk name:**
- **Excel:** `科技_科技树配置表_Tech_TechTreeConfig.xlsx`
- **CSV:** `Tech_TechTreeConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| TechId | 科技项ID | `string` or `int` | PK |
| IconId | 科技项图标 | `string` or `int` | Icon asset / atlas key; concrete paths at implementation |
| DisplayName | 科技项名字 | `string` | Display name; localization Key if i18n enabled |
| EffectDescription | 科技项效果描述 | `string` | Hover/detail copy; localization Key if i18n enabled |
| UnlockNextTechIds | 解锁后续科技项ID | `string` | Encoding **`Id\|Id\|Id`** (pipe-separated); empty = leaf |
| InitiallyUnlocked | 初始是否解锁 | `bool` | `true` → **auto-learn** on new save and apply effects; center root must be `true` |
| LearnCost | 学习费用 | `int` | TechPoints; ≥ 0; usually **0** if InitiallyUnlocked |
| TechUiFrameType | 科技项类型 | `enum` / `string` | `Root` \| `Normal` \| `Key` \| `Capstone`; drives TechTree UI frame style |

```
TechTreeConfig {
  TechId: Id
  IconId: Id
  DisplayName: string
  EffectDescription: string
  UnlockNextTechIds: "Id|Id|Id"   // empty = leaf
  InitiallyUnlocked: bool
  LearnCost: int
  TechUiFrameType: Root | Normal | Key | Capstone
}
```

**Conventions:**

| Rule | Notes |
|------|-------|
| Default camera | Table **first row** node = default canvas camera focus |
| Center root | Must have `InitiallyUnlocked = true`; recommended as first row |
| Layout | **Not** in this table; Prefab places positions on Settings TechTree |
| Prerequisites | Not stored; invert `UnlockNextTechIds` forward edges |

#### 9.17 TechEffectConfig

Rules: [SPEC_03 §3.13](SPEC_03_GameRules.md). Applied when a `TechId` is learned.

**Disk name:**
- **Excel:** `科技_科技项效果配置表_Tech_TechEffectConfig.xlsx`
- **CSV:** `Tech_TechEffectConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| TechId | 科技项ID | `string` or `int` | FK to `TechTreeConfig.TechId`; this version prefer **1:1**; multi-rows same Id merge |
| AttributeModifiers | 增加的属性 | `string` | Encoding **`Attr_Value\|Attr_Value\|…`** (additive); empty = no attr deltas |
| UnlockedFeatureSystemName | 解锁的功能系统名 | `string` | Non-empty → write save `UnlockedFeatureSystems`; empty = none; **open list** (PascalCase); validate known names at implement time, log unknown but still write; extend SPEC as features grow. Attr deltas (e.g. `DigDamage`) use `AttributeModifiers`, **not** this field |

```
TechEffectConfig {
  TechId: Id
  AttributeModifiers: "Attr_Value|Attr_Value|..."  // additive
  UnlockedFeatureSystemName: string               // empty = none; open PascalCase list
}
```

**Documented attribute keys this version (extensible):**

| Attr key | Writes | Notes |
|----------|--------|-------|
| DigDamage | `DigProtagonistCapabilities.DigDamage` | Per DigAction damage |
| DigDurationReductionSum | `DigProtagonistCapabilities.DigDurationReductionSum` | Shorten DigAction duration (seconds); dig speed |
| DigCursorRadius | `DigProtagonistCapabilities.DigCursorRadius` | Cursor radius (if used) |
| DigStageDurationBonus | `DigProtagonistCapabilities.DigStageDurationBonus` | Dig stage duration bonus (seconds) |

Further keys (e.g. ControlPower cap bonus) later; same key across techs **sums additively** into derived caps.

**Feature system name examples (open, not closed):** `FeatureDigAdvancedQualities`, etc.; non-empty write to save is authoritative.

#### 9.18 WaveSpawnConfig

Rules: [SPEC_03 §3.12](SPEC_03_GameRules.md). One row = one spawn event; grouped by `WaveConfigId`, referenced by `DefendGameplayConfig.WaveConfigId`.

**Disk name:**
- **Excel:** `防守_刷怪波次配置表_Defend_WaveSpawnConfig.xlsx`
- **CSV:** `Defend_WaveSpawnConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| WaveConfigId | 波次配置ID | `string` or `int` | Group key; multi-rows = all spawn events for that Defend config |
| SpawnOrder | 出怪顺序 | `int` | Applies **only** when multiple rows share the same `SpawnRemainingSeconds`; ascending order |
| SpawnRemainingSeconds | 出怪时间（剩余秒） | `int` | Activate when `RemainingCombatSeconds ==` this field (whole seconds) |
| MonsterId | 怪物ID | `string` or `int` | FK → `MonsterConfig` (§9.19) |
| SpawnCount | 出现数量 | `int` | ≥ 1 |
| AppearLocation | 出现位置 | `enum` / `string` | `InsideMap` \| `OutsideMap` |
| SpawnMode | 出怪方式 | `enum` / `string` | `RegionRandom` \| `ClockDirection` |
| SpawnClockHour | 几点钟方向 | `int` | 1–12; required when `SpawnMode = ClockDirection`; else optional/empty |

```
WaveSpawnConfig {
  WaveConfigId: Id
  SpawnOrder: int
  SpawnRemainingSeconds: int
  MonsterId: Id
  SpawnCount: int
  AppearLocation: InsideMap | OutsideMap
  SpawnMode: RegionRandom | ClockDirection
  SpawnClockHour: int?               // 1–12; required when ClockDirection
}
```

**Activation (with §3.12):** During `Combat`, when remaining whole seconds equal a row’s `SpawnRemainingSeconds`, fire matching not-yet-fired rows; same-second rows by `SpawnOrder` ascending. Exact spawn geometry **TBD**.

#### 9.19 MonsterConfig

Rules: [SPEC_03 §3.12](SPEC_03_GameRules.md). One row = one monster type.

**Disk name:**
- **Excel:** `防守_怪物配置表_Defend_MonsterConfig.xlsx`
- **CSV:** `Defend_MonsterConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| MonsterId | 怪物ID | `string` or `int` | PK; referenced by `WaveSpawnConfig` |
| ModelId | 怪物模型ID | `string` | Prefab logical name (no path/ext); resolve → `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab`; art source `Assets/Art/Characters/Monsters/{ModelId}/` (Character Creator baked whole characters; see [§15](#15-角色美术管线character-creator-烘焙整角)) |
| DisplayName | 怪物名称 | `string` | Display name or localization key (if i18n enabled) |
| TargetSelect | 目标选择 | `enum` / `string` | `Nearest` \| `PreferWarrior` \| `PreferProtagonist` (same enum as `SoulConfig.AttackPriority`) |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged` |
| MaxHP | 怪物血量 | `int` or `float` | Init monster maxHP / current HP on spawn |
| MoveSpeed | 怪物移动速度 | `float` | World units/sec or project-unified speed unit |
| AttackPower | 怪物攻击力 | `int` or `float` | Used **only** when attacking soldiers; not used for normal attacks on protagonist |
| AttackSpeed | 攻击速度 | `float` | Attack rate (unit locked at implementation) |
| AttackRange | 攻击距离 | `float` | Distance to enter attack state |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0; seconds; used when `AttackMode=Melee` |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0; used when `AttackMode=Ranged` |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0; seconds; timeout → miss |
| Skills | 怪物技能 | see encoding | Skill Id + CD list; skill-effect columns later topic; **unused in Demo v1** (normal attacks only; ignore or leave empty at implement time) |
| LootDrop | 怪物掉落 | see encoding | On kill; same encoding as Dig `LootDrop` |

**`Skills` encoding (fixed):** `SkillId_CdSeconds|SkillId_CdSeconds|...`

- Segment separator: `|`
- Segment: `SkillId_CooldownSeconds`
- Empty string = no skills
- Skill-effect definition table **not** defined this batch; Demo v1 does **not** cast even if populated
- **Note:** **Different** from soldier-side `SkillId;Level|…` (monster CD lives on this table)

**`LootDrop` encoding:** same as [§9.3](#93-gravequalityconfig): `Id_Count|Id_Count|...`

```
MonsterConfig {
  MonsterId: Id
  ModelId: string                  // Prefab logical name → Assets/Prefabs/Defend/Monsters/{Id}.prefab; art §15
  DisplayName: string
  TargetSelect: Nearest | PreferWarrior | PreferProtagonist
  AttackMode: Melee | Ranged
  MaxHP: number
  MoveSpeed: number
  AttackPower: number              // soldiers only
  AttackSpeed: number
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
  Skills: "SkillId_Cd|SkillId_Cd|..."
  LootDrop: "Id_Count|Id_Count|..."
}
```

#### 9.20 LossOfControlConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) / [§3.12](SPEC_03_GameRules.md) LossOfControl tiers and base chance. One row = one tier (fixed 4 rows: TierId 1–4).

**Disk name:**
- **Excel:** `战斗_失控配置表_Combat_LossOfControlConfig.xlsx`
- **CSV:** `Combat_LossOfControlConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| TierId | 失控程度段 | `int` | PK; **1–4** (Mild / Moderate / Severe / Full); Degree→tier mapping in SPEC_03 §3.11 |
| DisplayName | 名称 | `string` | Display name or localization Key |
| Description | 描述 | `string` | Tier description or localization Key |
| LossOfControlChance | 失控概率 | `float` | Tier **base** chance; range **\[0, 1\]**; concrete values filled later |

```
LossOfControlConfig {
  TierId: 1 | 2 | 3 | 4
  DisplayName: string
  Description: string
  LossOfControlChance: number     // [0, 1]
}
```

**Resolve:** After StartBattle locks `LossOfControlDegree` → map to `TierId` → read `LossOfControlChance` as `TierChance`. Final chance and Rebel: SPEC_03 §3.11 / §3.12.

#### 9.21 SkillConfig (skeleton)

Rules: Full soldier/monster skill-effect schema **later**; this batch locks LossOfControl bonus and **base cooldown** so Soul / Gem / ExtraEquipment `Skills` lists can resolve; actual CD uses §3.12 formula. **Demo v1 does not cast skills** — this table and CD formula do not drive combat. **No effect columns this batch**; future columns extend the same on-disk filenames.

**Disk name:**
- **Excel:** `战斗_技能配置表_Combat_SkillConfig.xlsx`
- **CSV:** `Combat_SkillConfig.csv` (future skill-effect columns extend this file)

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| SkillId | 技能ID | `string` or `int` | PK; referenced by Soul/Gem/Equip/Monster `Skills` |
| BaseCooldownSeconds | 基础冷却 | `float` | ≥ 0; seconds; soldier actual CD = `max(SkillCdFloor, BaseCooldownSeconds − SkillCdIntDiv/max(Int,1))` (coeffs from `ClassConfig.CombatConvertCoeffs` / §3.12) |
| LossOfControlChanceBonus | 失控概率加成 | `float` | May be +/-; missing = **0**; soldier `ΣSkillBonus` = sum of this field over all their skills (§3.11) |

```
SkillConfig {
  SkillId: Id
  BaseCooldownSeconds: number
  LossOfControlChanceBonus: number
  // skill-effect columns deferred; same file when expanded
}
```

**Note:** If soldier `ΣSkillBonus ≠ 0`, each skill cast re-rolls with full `FinalLossChance` (§3.11). Other skill columns (effects, level growth) **later topic** (filenames unchanged).

---

## 13. 资源编排与可扩展性

### 简体中文

**原则（强制倾向）：预制体优先（Prefab-first）。** 实际代码与场景开发中，凡会以 GameObject 层级出现的玩法实体、可复用 UI、可生成物、可摆放交互物，**默认用 Prefab + 挂载 Controller** 制作与引用，放在 `Assets/Prefabs/<模块>/`。优先在编辑器中拼装 Prefab，再由代码 `Instantiate` / 引用槽位驱动；**避免**在代码里动态 `new GameObject` 拼层级，或在多个 Scene 中手工复制同一套层级。

**适用默认 Prefab 的典型对象：** 主角/圆圈光标、坟墓（含障碍半径）、奖励飞字、工具面板与可复用面板、关卡内可生成物、战斗主角/士兵/怪物、**DigMap / BattleMap（含 EngageZone；共用 `Ground_01`…`Ground_05`）** 等。Dig 模块建议路径：`Assets/Prefabs/Dig/`；UpgradeManufacture 模块建议路径：`Assets/Prefabs/UpgradeManufacture/`（`UpgradeManufactureStageRoot`）；**地图变体**统一路径：`Assets/Prefabs/Maps/{Ground_0N}.prefab`（`DigMapId` / `BattleMapId` 均解析至此；源参考 Example Scene `Grid`/`Ground (1)`…`Ground (5)`，Demo 时复制进项目，**禁止**运行时直接引用 `SmallScaleInt/`，见 [§15](#15-角色美术管线character-creator-烘焙整角)）。角色视觉 Prefab 约定：`Digger` → `Assets/Prefabs/Dig/Digger.prefab`；`BattleProtagonist` → `Assets/Prefabs/Defend/BattleProtagonist.prefab`；士兵 → `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`；怪物 → `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab`（美术管线见 [§15](#15-角色美术管线character-creator-烘焙整角)）。

**可不做成 Prefab 的例外：** Scene 唯一常驻 Manager / 引导用一次性布局；纯逻辑无表现的 Service（非 MonoBehaviour 或仅场景单例入口）。

| 问题 | 是 → 做法 |
|------|-----------|
| 玩法/UI GameObject 层级（默认）？ | **Prefab** + Controller → `Assets/Prefabs/<模块>/` |
| 多次 Instantiate / 多 Scene 复用？ | 必须 Prefab；禁止 Scene 间复制层级 |
| 策划**配置表**行数据？ | Excel + CSV → `Assets/ConfigTables/`（见 [§14](#14-配置表工程约定与打表工具)） |
| 策划可调**非表型**单例/引用槽？ | ScriptableObject → `Assets/Settings/<模块>/` |
| UI 文本？ | 本地化 Key（若启用 §8） |
| 高频 spawn/destroy？ | Prefab + **对象池** |
| 玩法状态变更？ | 规则层 + 事件；View 不驱动规则 |

**禁止：** 多 Scene 复制同一层级；硬编码策划数据；`GameObject.Find`（Manager 除外）；规则层直接操作 `Transform`/`Animator`；用纯代码拼装本应以 Prefab 交付的可视层级（调试临时对象除外）；将新配置表散落在 `Settings/` 或其他非 `ConfigTables/` 路径。

配置表（§9）落地须遵循 [§14](#14-配置表工程约定与打表工具)：运行时只读 CSV；禁止在脚本中硬编码关卡/挖坟/防守/升级/科技等表数值。

### English

**Principle (strong default): Prefab-first.** For gameplay entities, reusable UI, spawnables, and placeable interactables that exist as GameObject hierarchies, **author and reference Prefabs + Controllers** under `Assets/Prefabs/<Module>/`. Prefer assembling Prefabs in the Editor and driving them via `Instantiate` / serialized slots. **Do not** build visual hierarchies with runtime `new GameObject` trees, or hand-duplicate the same hierarchy across Scenes.

**Typical Prefab targets:** Digger / circle cursor, Graves (with obstacle radius), DigReward VFX/UI, ToolsPanel and reusable panels, in-level spawnables, BattleProtagonist / Soldiers (Warrior) / Monsters, **DigMap / BattleMap (incl. EngageZone; shared `Ground_01`…`Ground_05`)**. Dig module path: `Assets/Prefabs/Dig/`; UpgradeManufacture module path: `Assets/Prefabs/UpgradeManufacture/` (`UpgradeManufactureStageRoot`); **map variants** unified path: `Assets/Prefabs/Maps/{Ground_0N}.prefab` (`DigMapId` / `BattleMapId` both resolve here; source ref Example Scene `Grid`/`Ground (1)`…`Ground (5)`; copy into project at Demo time; **do not** runtime-reference `SmallScaleInt/` — [§15](#15-角色美术管线character-creator-烘焙整角)). Character visual Prefabs: `Digger` → `Assets/Prefabs/Dig/Digger.prefab`; `BattleProtagonist` → `Assets/Prefabs/Defend/BattleProtagonist.prefab`; soldiers → `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`; monsters → `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab` (art pipeline: [§15](#15-角色美术管线character-creator-烘焙整角)).

**Exceptions:** Scene-unique Managers / one-off layout; pure logic Services (non-MonoBehaviour or single scene entry).

| Question | If yes → |
|----------|----------|
| Gameplay/UI GameObject hierarchy (default)? | **Prefab** + Controller → `Assets/Prefabs/<Module>/` |
| Multiple Instantiate / multi-Scene reuse? | Prefab required; no cross-Scene hierarchy copy |
| Designer **config-table** rows? | Excel + CSV → `Assets/ConfigTables/` (see [§14](#14-配置表工程约定与打表工具)) |
| Designer-tunable **non-table** singleton / ref slots? | ScriptableObject → `Assets/Settings/<Module>/` |
| UI text? | Localization keys (if §8 enabled) |
| High-frequency spawn/destroy? | Prefab + **object pool** |
| Gameplay state changes? | Rules layer + events; View must not drive rules |

**Forbidden:** duplicating the same hierarchy across Scenes; hardcoding designer data; `GameObject.Find` (except Managers); rules layer directly driving `Transform`/`Animator`; assembling in code what should ship as a Prefab (except temporary debug objects); placing new config tables outside `ConfigTables/`.

When implementing §9 tables, follow [§14](#14-配置表工程约定与打表工具): runtime reads CSV only; no hardcoded Level/Dig/Defend/upgrade/tech table values in scripts.

---

## 14. 配置表工程约定与打表工具

### 简体中文

**状态：工程约定已关闭；Editor 打表工具实现待 Demo 开发授权后另开任务。**

凡 **§9** 及后续新增的**配置表**，统一遵守本节。

#### 14.1 统一路径（两文件夹）

```
Gravedigger2026/Assets/ConfigTables/
├── Excel/     # 人配源表（.xlsx）；仅作源文件，运行时不加载
└── Csv/       # 程序读表（.csv）；打表产物；运行时唯一数据源
```

- 新配置表**禁止**散落在 `Assets/Settings/<模块>/` 或其他路径。
- Excel 导入设置在实现阶段再定；SPEC 要求：Excel **不作为运行时资源**读取。

#### 14.2 命名（Excel 四段 / CSV 两段）

- **Excel 磁盘名（无扩展名）** = `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}`  
  例：`挖坟_挖坟配置表_Dig_DigGameplayConfig.xlsx`
- **CSV 磁盘名（无扩展名）** = `{SystemEN}_{TableEN}`（**不变**）  
  例：`Dig_DigGameplayConfig.csv`
- 逻辑短名（如 `DigGameplayConfig`）可用于类型/伪代码；**CSV 必须带系统英文前缀**；**Excel 必须在其前再加系统中文名 + 配置表中文名**。
- `SystemZH` / `TableZH` **禁止**含 `_`（避免打表解析歧义）。

| SystemEN | SystemZH | 适用范围（现有 §9 CSV 基名示例） |
|----------|----------|----------------------------------|
| `Level` | 关卡 | `Level_LevelOperationConfig` |
| `Dig` | 挖坟 | `Dig_DigGameplayConfig`、`Dig_GraveQualityConfig`、`Dig_MaterialConfig`、`Dig_CurrencyConfig` |
| `Defend` | 防守 | `Defend_DefendGameplayConfig`、`Defend_WaveSpawnConfig`、`Defend_MonsterConfig` |
| `Manufacture` | 制造 | `Manufacture_ProtagonistLevelConfig`、`Manufacture_SoulConfig`、`Manufacture_ClassConfig`、`Manufacture_GemConfig`、`Manufacture_RaceConfig`、`Manufacture_BodyPartConfig`、`Manufacture_BodyAppearanceConfig`、`Manufacture_ExtraEquipmentConfig`、`Manufacture_GemSuffixNameConfig` |
| `Tech` | 科技 | `Tech_TechTreeConfig`、`Tech_TechEffectConfig` |
| `Combat` | 战斗 | `Combat_LossOfControlConfig`、`Combat_SkillConfig` |

配置表中文名（`TableZH`）取自 §9 小节标题（如「挖坟配置表」「关卡运作表」）。各表完整 Excel / CSV 名以 §9「磁盘名」行为准；新增表须先定 `SystemZH` + `TableZH` + `SystemEN` 再落盘。

#### 14.3 双格式强制

- 每张配置表必须同时维护 **Excel 源** + **CSV 产物**（文件名按 §14.2，**不必同名**）。
- 运行时 / 加载管线**只读** `ConfigTables/Csv/`。
- 策划只改 Excel；改完必须打表。**CSV 为生成物**，禁止以手改 CSV 作为长期数据源。

#### 14.4 打表工具（Bake Tables）

| 项 | 约定 |
|----|------|
| 职责 | 一键将 `ConfigTables/Excel/` 下全部（或选中）`.xlsx` 转为对应 `.csv` 写入 `ConfigTables/Csv/` |
| 命名映射 | Excel 基名 `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}` → CSV 基名 `{SystemEN}_{TableEN}`（取英文后缀两段） |
| 入口 | Unity 顶部菜单：`Gravedigger/Config/Bake Tables`（中文菜单可用「打表」） |
| 失败策略 | 缺列 / 类型非法 / 文件名不符四段规则时**中止**，Console 报错（**完整 Excel 名** + 行 + 字段）；不静默写出半成品 |
| Excel 库 | 实现时选用 Editor 可读 `.xlsx` 方案并回写本 SPEC |
| 实现时机 | Demo 开发授权后另开任务；本节只锁接口级约定 |

```
Excel (ConfigTables/Excel/{SystemZH}_{TableZH}_{SystemEN}_{TableEN}.xlsx)
  → Editor Bake Tables
  → CSV (ConfigTables/Csv/{SystemEN}_{TableEN}.csv)
  → Runtime Config Loader
```

#### 14.5 运行时 CSV 加载路径（Demo）

| 环境 | 根路径 | 说明 |
|------|--------|------|
| Editor / 开发 Play | `{Application.dataPath}/ConfigTables/Csv/` | 即工程内 `Assets/ConfigTables/Csv/` |
| Player 构建 | `{Application.streamingAssetsPath}/ConfigTables/Csv/` | 须在构建前将同名 CSV 镜像到 `Assets/StreamingAssets/ConfigTables/Csv/`（拷贝工具可另开；本片加载器按序探测两根路径） |

- 逻辑仍只读 CSV；**禁止**运行时读 Excel。
- 缺表 / 缺列 / 非法枚举 → 加载失败并打日志，不静默用空表推进关卡。

### English

**Status: Engineering rules closed; Editor bake-tool implementation deferred until Demo-dev authorization.**

All **§9** and future **config tables** must follow this section.

#### 14.1 Unified path (two folders)

```
Gravedigger2026/Assets/ConfigTables/
├── Excel/     # Human-authored source (.xlsx); not loaded at runtime
└── Csv/       # Program-readable (.csv); bake output; sole runtime data source
```

- New config tables must **not** live under `Assets/Settings/<Module>/` or other paths.
- Excel import settings TBD at implementation; Excel must **not** be a runtime load target.

#### 14.2 Naming (Excel four-part / CSV two-part)

- **Excel basename** = `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}`  
  e.g. `挖坟_挖坟配置表_Dig_DigGameplayConfig.xlsx`
- **CSV basename** = `{SystemEN}_{TableEN}` (**unchanged**)  
  e.g. `Dig_DigGameplayConfig.csv`
- Logical short names (e.g. `DigGameplayConfig`) may be used for types/pseudocode; **CSV requires the English system prefix**; **Excel requires SystemZH + TableZH before that**.
- `SystemZH` / `TableZH` must **not** contain `_` (bake parse safety).

| SystemEN | SystemZH | Scope (existing §9 CSV basename examples) |
|----------|----------|-------------------------------------------|
| `Level` | 关卡 | `Level_LevelOperationConfig` |
| `Dig` | 挖坟 | `Dig_DigGameplayConfig`, `Dig_GraveQualityConfig`, `Dig_MaterialConfig`, `Dig_CurrencyConfig` |
| `Defend` | 防守 | `Defend_DefendGameplayConfig`, `Defend_WaveSpawnConfig`, `Defend_MonsterConfig` |
| `Manufacture` | 制造 | `Manufacture_ProtagonistLevelConfig`, `Manufacture_SoulConfig`, `Manufacture_ClassConfig`, `Manufacture_GemConfig`, `Manufacture_RaceConfig`, `Manufacture_BodyPartConfig`, `Manufacture_BodyAppearanceConfig`, `Manufacture_ExtraEquipmentConfig`, `Manufacture_GemSuffixNameConfig` |
| `Tech` | 科技 | `Tech_TechTreeConfig`, `Tech_TechEffectConfig` |
| `Combat` | 战斗 | `Combat_LossOfControlConfig`, `Combat_SkillConfig` |

`TableZH` comes from the §9 subsection title (e.g.「挖坟配置表」). Per-table full Excel/CSV names: see §9 **Disk name** lines. New tables must choose `SystemZH` + `TableZH` + `SystemEN` before landing files.

#### 14.3 Dual-format required

- Every config table must maintain **Excel source** + **CSV product** (names per §14.2; **need not match**).
- Runtime / loaders read **only** `ConfigTables/Csv/`.
- Designers edit Excel only, then bake. **CSV is generated**; do not hand-edit CSV as the long-term source of truth.

#### 14.4 Bake tool

| Item | Rule |
|------|------|
| Duty | One-click convert all (or selected) `.xlsx` under `ConfigTables/Excel/` to matching `.csv` under `ConfigTables/Csv/` |
| Name map | Excel `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}` → CSV `{SystemEN}_{TableEN}` (last two English segments) |
| Entry | Unity menu: `Gravedigger/Config/Bake Tables` (ZH label may be「打表」) |
| Failure | Abort on missing columns / illegal types / non-four-part Excel names; Console error (**full Excel name** + row + field); no silent partial writes |
| Excel lib | Pick an Editor-readable `.xlsx` approach at implementation and write back here |
| Timing | Separate task after Demo-dev authorization; this section locks interface-level rules only |

```
Excel (ConfigTables/Excel/{SystemZH}_{TableZH}_{SystemEN}_{TableEN}.xlsx)
  → Editor Bake Tables
  → CSV (ConfigTables/Csv/{SystemEN}_{TableEN}.csv)
  → Runtime Config Loader
```

#### 14.5 Runtime CSV load paths (Demo)

| Environment | Root path | Notes |
|-------------|-----------|-------|
| Editor / Play Mode | `{Application.dataPath}/ConfigTables/Csv/` | i.e. project `Assets/ConfigTables/Csv/` |
| Player build | `{Application.streamingAssetsPath}/ConfigTables/Csv/` | Mirror same CSV files under `Assets/StreamingAssets/ConfigTables/Csv/` before build (copy tool may be separate; this slice’s loader probes both roots in order) |

- Still CSV-only; **never** read Excel at runtime.
- Missing table / column / illegal enum → load fails with log; do not silently advance Levels on empty tables.

---

## 15. 角色美术管线（Character Creator 烘焙整角）

### 简体中文

**状态：管线约定已关闭** — 全角色视觉采用 SmallScaleInt **Character Creator - Fantasy** 烘焙整角；游戏引用资源不得落在工具包目录。

#### 15.1 工具与禁入

| 项 | 约定 |
|----|------|
| 工具根 | `Assets/SmallScaleInt/Character creator - Fantasy/` |
| 允许内容 | 厂商源部件（`spritesheets` 等）、Editor 示例场景/脚本、文档、厂商示例 |
| **禁止** | 任何**游戏实际引用**的角色 spritesheet、`.anim`、`.controller`、游戏 Prefab、运行时材质/图集 落在该目录（**含**其下 `Created Spritesheets/`） |
| 形态 | **烘焙整角**（拼装 → 导出整角 spritesheet + AnimationClips + Animator + 可选厂商 Prefab）；**不**以运行时多层换装为默认方案 |
| 覆盖角色 | Digger、BattleProtagonist、士兵（`AppearanceId`）、怪物（`ModelId`） |

#### 15.2 项目落盘目录

```
Assets/Art/Characters/
  Protagonist/                 # 挖坟/战斗主角导出源（spritesheet + clips + animator）
  Appearances/{AppearanceId}/  # 士兵外观
  Monsters/{ModelId}/          # 怪物
Assets/Prefabs/Dig/Digger.prefab
Assets/Prefabs/Defend/BattleProtagonist.prefab
Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab
Assets/Prefabs/Defend/Monsters/{ModelId}.prefab
```

- Art 目录存烘焙产物（图、Clip、Controller）；游戏 Instantiate 只引用 `Prefabs/` 下游戏 Prefab。
- 主角固定逻辑名：`Digger`、`BattleProtagonist`（本版不另开多皮肤表）。

**士兵游戏 Prefab 组装（`Warriors/{AppearanceId}`，俯视相机）：**

| 节点 | 职责 |
|------|------|
| 根 | 位移；运行时挂 `WarriorAgentView` + `NavMeshAgent`（可代码 Add） |
| 子 `Visual` | `SpriteRenderer` + `Animator`（Controller 来自 Art 烘焙）；`localEulerAngles = (90, 0, 0)`，使 Sprite 朝 +Y，对齐 Defend/Dig 俯视相机（相机 `Euler(90,0,0)`） |

- View 层：`NavMeshAgent.updateRotation = false`（八向靠 Animator `DirIndex`，见 §15.5；本约定不要求本片已驱动参数）。
- **禁止**用 `GenericTopDownController` 作默认玩法控制器（见 §15.4）。
- 怪物 / `BattleProtagonist` 若仍用 Mesh 占位，不强制本结构；换 Sprite 时沿用同一约定。

#### 15.3 导出路径改造（强制）

厂商 `SpritesheetGenerator` 默认将 `outputParent` 写到工具内 `Created Spritesheets/`。**本项目维护补丁**：把 Editor 导出根改为 `Assets/Art/Characters/...`（按角色类型分子目录；导出文件夹名与 `AppearanceId` / `ModelId` / 主角约定对齐）。Clips / Controller / 可选导出 Prefab 随同写入该 Art 子目录；再组装或复制为 §15.2 的游戏 Prefab。

**切片 / Clip 补丁（强制，否则 Windows 下 `.anim`/`.controller` 会空导出）：**

| 问题 | 现象 | 补丁 |
|------|------|------|
| 切片未设 `TextureImporter.textureType = Sprite` | meta 可有 `spriteMode: Multiple` 与 spritesheet 名，但 `textureType` 仍为 Default(0)；`LoadAllAssetRepresentationsAtPath` 得不到 Sprite → **不写任何 `.anim`**，却仍生成 **空 BlendTree**（`m_Childs: []`）的 `.controller` | 切片与 `ApplyFrameCountOption` 重导时必须 `textureType = Sprite`，并与厂商源 `spritesheets/`（`textureType: 8`）一致 |
| `AssetImporter.GetAtPath` 使用反斜杠路径 | Windows 上 importer 可能为 null，切片静默跳过 | 资产路径统一 `.Replace("\\", "/")` |
| 无 Sprite 仍建 Controller | 空状态机误当成功 | `AnimatorClipBuilder`：零 Clip 时 **LogError 并中止**，不写空 `.controller` |

**修复已坏导出：** Editor 菜单 `Tools/Gravedigger/Art/Repair Character Creator Export`（对选中或指定角色文件夹：纠正 importer → 重导 → 重建 Clips/Controller）。

**升级风险：** 重新导入 `.unitypackage` 会覆盖补丁脚本 → 每次导入后须 `diff` `SpritesheetGenerator.cs` / `AnimatorClipBuilder.cs`（及相关写盘路径）并重打补丁。

#### 15.4 流水线

```mermaid
flowchart LR
  VendorTool[Vendor_Creator_Source] --> BakeExport[Patched_Export]
  BakeExport --> ArtFolder[Art_Characters_Id]
  ArtFolder --> GamePrefab[Prefabs_Dig_or_Defend]
  GamePrefab --> ConfigBind[AppearanceId_or_ModelId]
```

| 阶段 | 说明 |
|------|------|
| 创作 | 仅在工具目录内拼装部件/配色；不把成品当作游戏引用 |
| 导出 | 经补丁路径写入 `Art/Characters/...` |
| Prefab | 组装到 `Prefabs/Dig` 或 `Prefabs/Defend/...`；挂项目玩法 Controller（**不用** `GenericTopDownController` 作默认玩法控制器；厂商脚本可作视觉壳参考） |
| 配置 | `AppearanceId` / `ModelId` / 固定主角 Prefab 名绑定；规则层不硬编码动画资源路径 |

#### 15.5 动画映射（占位）

Creator 默认导出含 Idle / Walk / Run / Attack* / Die 等。挖坟循环动画**无**原生 Dig clip 时：由**表现层映射表**将 Dig 语义映射到某一导出 clip（如 `Special1`）；具体映射 Demo 实现时锁定。**禁止**规则层写死动画名字符串；**禁止**在无独立 Dig 资源时假装已有专用 Dig 资产。

8 向 `DirIndex` 与菱形/等距地图朝向在 View 层统一约定（实现时锁定）。

#### 15.6 Mount / Wing

见 [§9.14](#914-额外装备配置表-extraequipmentconfig)：视觉打进对应 `AppearanceId` 烘焙变体；运行时叠装另专题。

### English

**Status: Pipeline rules closed** — All character visuals use SmallScaleInt **Character Creator - Fantasy** baked whole characters; game-referenced assets must **not** live under the vendor tool folder.

#### 15.1 Tool folder & ban

| Item | Rule |
|------|------|
| Tool root | `Assets/SmallScaleInt/Character creator - Fantasy/` |
| Allowed | Vendor source parts (`spritesheets`, etc.), Editor sample scenes/scripts, docs, vendor samples |
| **Forbidden** | Any **game-referenced** character spritesheets, `.anim`, `.controller`, game Prefabs, runtime materials/atlases under that folder (**including** `Created Spritesheets/`) |
| Form | **Baked whole characters** (assemble → export full spritesheets + AnimationClips + Animator + optional vendor Prefab); runtime layered gear is **not** the default |
| Covers | Digger, BattleProtagonist, soldiers (`AppearanceId`), monsters (`ModelId`) |

#### 15.2 Project paths

Same tree as ZH §15.2. Art holds bake outputs; runtime Instantiate uses `Prefabs/` only. Fixed protagonist logical names: `Digger`, `BattleProtagonist`.

**Soldier game Prefab assembly (`Warriors/{AppearanceId}`, top-down camera):**

| Node | Role |
|------|------|
| Root | Translation; runtime `WarriorAgentView` + `NavMeshAgent` (may be AddComponent) |
| Child `Visual` | `SpriteRenderer` + `Animator` (Controller from Art bake); `localEulerAngles = (90, 0, 0)` so the sprite faces +Y toward the Defend/Dig top-down camera (`Euler(90,0,0)`) |

- View: `NavMeshAgent.updateRotation = false` (8-dir via Animator `DirIndex`, §15.5; this slice need not drive those params yet).
- **Do not** use `GenericTopDownController` as the default gameplay controller (§15.4).
- Monsters / `BattleProtagonist` still on Mesh placeholders need not follow this yet; adopt the same layout when switching to sprites.

#### 15.3 Export path patch (mandatory)

Vendor `SpritesheetGenerator` defaults `outputParent` to in-package `Created Spritesheets/`. **This project maintains a patch** so Editor export roots at `Assets/Art/Characters/...` (subfolders by role; folder names align with `AppearanceId` / `ModelId` / protagonist convention). Clips / Controller / optional export Prefab land under that Art subfolder; then assemble/copy into §15.2 game Prefabs.

**Slice / Clip patch (mandatory — otherwise Windows exports empty `.anim`/`.controller`):**

| Issue | Symptom | Patch |
|-------|---------|-------|
| Slice omits `TextureImporter.textureType = Sprite` | Meta may show Multiple + spritesheet names while `textureType` stays Default(0); no Sprite sub-assets → **no `.anim`**, yet an empty BlendTree `.controller` (`m_Childs: []`) is still written | Force `textureType = Sprite` on slice and `ApplyFrameCountOption` reimport; match vendor `spritesheets/` (`textureType: 8`) |
| Backslash paths in `AssetImporter.GetAtPath` | Importer may be null on Windows; slice silently skipped | Normalize asset paths with `.Replace("\\", "/")` |
| Controller built with zero clips | Empty state machine looks “successful” | `AnimatorClipBuilder`: **LogError and abort** when clip count is zero |

**Repair broken exports:** Editor menu `Tools/Gravedigger/Art/Repair Character Creator Export` (fix importer → reimport → rebuild Clips/Controller for selected/target character folder).

**Upgrade risk:** Re-importing the `.unitypackage` overwrites the patch → after each import, `diff` `SpritesheetGenerator.cs` / `AnimatorClipBuilder.cs` (and related write paths) and re-apply.

#### 15.4 Pipeline

Same mermaid as ZH. Author in vendor folder only; export via patched path; Prefabs under Dig/Defend; **do not** treat `GenericTopDownController` as the default gameplay controller.

#### 15.5 Animation mapping (placeholder)

Creator exports Idle / Walk / Run / Attack* / Die, etc. Dig loop has **no** native Dig clip: View-layer mapping table maps Dig semantics to an exported clip (e.g. `Special1`); lock at Demo implement time. **No** hardcoded anim names in rules layer; **do not** pretend a dedicated Dig asset exists without one.

8-dir `DirIndex` vs diamond/isometric facing: unify in View (lock at implement time).

#### 15.6 Mount / Wing

See [§9.14](#914-额外装备配置表-extraequipmentconfig): bake into `AppearanceId` variants; runtime overlays later.
