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
| 渲染 | Built-in RP（非 URP）。禁止导入仅支持 URP 的厂商包（其 shader 会 `#include Packages/com.unity.render-pipelines.core/...`，本工程未安装该包） |
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
| Rendering | Built-in RP (not URP). Do not import URP-only vendor packs (their shaders `#include Packages/com.unity.render-pipelines.core/...`, which this project does not install) |
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
│   │   ├── Graves/        # Graves/{Grave_Q*}/（Demo Q1…Q20；子文件夹名 = Prefab 逻辑名）
│   │   └── Feedback/
│   ├── Maps/              # Maps/Tiles/（Isometric Tile+Sprite）+ Maps/{Ground_0N}/
│   ├── Defend/            # 弹道、护盾等非角色表现源
│   │   ├── Projectile/
│   │   └── Shield/
│   ├── UI/                # 2D 图标统一落点（本版不另建 Sprites/）
│   │   ├── Cursor.png     # UI-024 运行时硬件光标源（PlayerPointer；非 Prefab Instantiate）
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
│   └── UI/
│       └── Skills/        # Demo 士兵技能图标按 SkillId 热加载（UI-021）；文件名 = SkillId
├── Materials/             # 运行时材质（含 AllIn1 Demo 特效材质，见 §15.2）
│   └── AllIn1/            # VisualStyle 预设 Style_*.mat（§15.2）；含 EvilMarine
├── Settings/              # 非表型 ScriptableObject（单例调参、引用槽等）
├── SmallScaleInt/         # 第三方 Character Creator 工具源（仅创作；见 §15）
└── ConfigTables/          # 配置表统一根（见 §14）
    ├── Excel/             # 人配源表（.xlsx）；运行时不加载
    └── Csv/               # 程序读表（.csv）；打表产物；运行时唯一数据源
```

**Art vs Prefabs：** `Art/` 存源素材（图、Clip、Controller、贴图等）；游戏 Instantiate / Catalog 绑定只引用 `Prefabs/<模块>/`。本版 **不** 单独落地顶层 `Sprites/`；2D 图标统一落在 `Art/UI/`（及 `Art/Placeholder/` 过渡）。**运行时光标（UI-024）：** `Art/UI/Cursor.png` 由 `PlayerSettings.defaultCursor` 引用，**不**走 Prefab Instantiate。**例外（UI-021 / UI-025 / D-065 / D-071）：** 士兵技能图标须按 `SkillId` 运行时热加载，投放目录为 `Assets/Resources/UI/Skills/`（文件名 = `SkillId`，如 `Skill_01.png`）；`Resources.Load<Sprite>("UI/Skills/"+SkillId)`；缺图则空框仍显示技能名（布阵悬浮框）或空框图标（CombatSkillIcon）。**例外（UI-022 / UI-023 / UI-026）：** 主角装备 / 魔法书 / 商店待售商品图标：投放 `Assets/Resources/UI/Equipment/{IconAssetId}.png` 与 `Assets/Resources/UI/MagicBooks/{IconAssetId}.png`；表内 `IconAssetId` 仅文件名时由 View 加前缀加载；含 `/` 则按 Resources 相对路径直载。角色烘焙细则见 [§15](#15-角色美术管线character-creator-烘焙整角)。

实际目录以工程为准；结构性变更记入 SPEC_00 Changelog。配置表路径与命名强制约定见 [§14](#14-配置表工程约定与打表工具)。角色美术路径与工具目录禁入见 [§15](#15-角色美术管线character-creator-烘焙整角)。

### English

Recommended tree as above. `Art/` holds source art (Characters per [§15](#15-角色美术管线character-creator-烘焙整角), Dig/Maps/Defend/UI/VFX/Audio, plus `Placeholder/` for Demo CSV `AssetPath`); runtime Instantiate uses `Prefabs/` only. Top-level `Sprites/` is **not** used this revision—2D icons live under `Art/UI/`. **Runtime pointer (UI-024):** `Art/UI/Cursor.png` is referenced by `PlayerSettings.defaultCursor`, **not** Prefab Instantiate. **Exception (UI-021 / UI-025 / D-065 / D-071):** soldier-skill icons load by `SkillId` at runtime from `Assets/Resources/UI/Skills/` (filename = `SkillId`; `Resources.Load<Sprite>("UI/Skills/"+SkillId)`; missing sprite still shows the skill name in the formation tooltip, or an empty-frame CombatSkillIcon). **Exception (UI-022 / UI-023 / UI-026):** protagonist equipment / MagicBook / shop-offer icons live under `Assets/Resources/UI/Equipment/{IconAssetId}.png` and `Assets/Resources/UI/MagicBooks/{IconAssetId}.png`; filename-only table ids get the folder prefix at load; ids containing `/` load as Resources-relative paths. Also includes `Materials/AllIn1/` (optional AllIn1 sprite-effect mats, §15.2), `Prefabs/Dig|Defend|Maps/...`, `SmallScaleInt/` tool source, `ConfigTables/Excel/` + `ConfigTables/Csv/`. Record structural changes in SPEC_00 Changelog. Config-table path and naming rules: [§14](#14-配置表工程约定与打表工具). Character art paths and vendor-folder ban: [§15](#15-角色美术管线character-creator-烘焙整角).

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

**初始隐藏面板：** Prefab 上 `m_IsActive=0` 即可；若 View 的 `_root` 即自身 GameObject，禁止在 `Awake` 再 `SetActive(false)`——该 Awake 往往在首次 `Show()` 才执行，会立刻抵消打开（表现为要点第二次才生效）。

审查清单见 [unity-spec-dev-workflow](.cursor/skills/unity-spec-dev-workflow/SKILL.md) §5。

### English

See naming table. Checklist: [unity-spec-dev-workflow](.cursor/skills/unity-spec-dev-workflow/SKILL.md) §5. Namespace `Gravedigger2026.<Module>` unless existing code differs.

**Initially hidden panels:** Prefer Prefab `m_IsActive=0`. If View `_root` is the same GameObject, do **not** call `SetActive(false)` in `Awake` — that Awake often runs on the first `Show()` and immediately cancels the open (looks like a double-click requirement).

---

## 4. 跨平台输入（占位）与运行时光标

### 简体中文

**输入抽象状态：未定义**

建议玩法依赖输入抽象接口，禁止玩法直接读 `Input.GetKey` / 原始触摸 API。

**运行时硬件光标（PlayerPointer / UI-024，方案 A）：**

| 项 | 约定 |
|----|------|
| 范围 | 整段 Play：Boot 存档选择起，含进档壳与全部玩法阶段 |
| 载体 | `PlayerSettings.defaultCursor` + `cursorHotspot`；**不**写 `Cursor.SetCursor`；**不**用 Overlay Image 跟随；**不**改 `Cursor.visible` |
| 源图 | `Assets/Art/UI/Cursor.png`（非 Prefab Instantiate） |
| 导入 | Texture Type = **Cursor**、Read/Write、Filter Mode = Point、Alpha is Transparency、**不压缩**、各平台 `maxTextureSize=32`（Windows 硬件光标上限） |
| hotspot | 贴图像素坐标，原点左上，对齐锁尖（约 `(0,0)`） |
| 与 Dig | `UiDigCursorRing` 仅为范围指示，叠加在 PlayerPointer 上；**不**替代 OS 指针 |

### English

**Input abstraction status: Undefined**

Prefer an input abstraction; no raw `Input.GetKey` / touch in gameplay code.

**Runtime hardware cursor (PlayerPointer / UI-024, Approach A):**

| Item | Convention |
|------|------------|
| Scope | Whole Play: from Boot save-select through InSaveShell and every gameplay stage |
| Carrier | `PlayerSettings.defaultCursor` + `cursorHotspot`; **no** `Cursor.SetCursor` script; **no** Overlay Image follow; do **not** change `Cursor.visible` |
| Source | `Assets/Art/UI/Cursor.png` (not Prefab Instantiate) |
| Import | Texture Type = **Cursor**, Read/Write, Filter Mode = Point, Alpha is Transparency, **uncompressed**, per-platform `maxTextureSize=32` (Windows hardware-cursor cap) |
| Hotspot | Texture pixel coords, origin top-left, at shovel tip (about `(0,0)`) |
| vs Dig | `UiDigCursorRing` is range overlay only on top of PlayerPointer; it does **not** replace the OS pointer |

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
| 进档壳层 | 进入后默认 `GameplayState = Dig` 占位；浮动「工具」；左下「商店」「装备」「魔法书」（UI-026 / UI-022 / UI-023）；流水线片可从壳层启动样例关卡（§3.8 D-003 / D-010） |
| 运行时光标 | UI-024：全 Play 硬件指针 `Art/UI/Cursor.png`（`PlayerSettings.defaultCursor`）；Dig 圆圈不替代 OS 指针 |
| 工具面板 | 设置、关卡入口 + Demo GM「增加主角装备」「增加魔法书」（UI-019 / D-061）+「添加士兵」（UI-020 / D-064）；与玩法 View 分离（壳层 UI） |
| 关卡驱动 | 只读 `ConfigTables/Csv/`；`LevelOperationConfig` 升序驱动 Dig → UpgradeManufacture → Defend |
| Dig | §3.10 垂直切片：`DigMapId`→`Prefabs/Maps/`；挖掘 / 奖励 / DigStageSummary（临时美术允许） |
| UpgradeManufacture | §3.11 垂直切片：升级区 / 制造 ≥1 士兵 / 布阵写回；阶段 `GameplayConfigId` **忽略**（见 §9.1） |
| Defend | §3.12 垂直切片：Prepare/开战/护盾；Demo 最小刷怪点 + NavMesh；士兵普攻；胜负/LevelFailure（临时美术允许） |

**范围外：** 解析 Description 自然语言的通用效果器；Mode1 战斗技能；怪物 `Skills` 施放；Defend 技能接线；正式美术 polish；精确 OutsideMap 几何；**完整**存档 schema（仓库/经验/科技等仍 TBD；士兵池+布阵本片已锁定）；科技树节点具体数值/图标 polish 与功能系统名完整枚举；工具面板 D-061 / D-064 以及 D-067 / D-068 / D-072 / D-073 以外的后续功能；打表全量 §9 列/类型校验（Demo 仅文件名+表头）；未列入 §3.8 的需求。

**持久化意图（轻量）：** 本地、按槽索引 `0..2` + **`CampaignMode`**。**Demo Meta 选型已锁定：`PlayerPrefs`**。键：
- `Gravedigger2026.SaveSlot.{0|1|2}.Occupied`（`0`/`1`；**按槽共享**，与模式无关）
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.WarriorPool` — JSON：`NextSerial` + `WarriorInstance` 制造静态快照数组（含 `SourceItemIds` / `SourceSpiritCost` / `EquipStats` / `BodyLife` / `SoldierSkills` / `VisualStyleId` / `VisualPriority` / `VisualIntensity` / `VisualModelScale` 等；见 §9.9；**SS-02** JsonUtility 往返；**SS-03** Mode1 制造/再造授予 `DefaultSkillIds`@Lv1；**SS-04** Mode2 授予 + `SoldierSkillLevelAdd`；VisualStyle 材质通道与放大通道仅 Mode2 书命中烘进）
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.BattleFormation` — JSON：`{WarriorId, PositionX, PositionZ, RemainingHP}[]`
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.DungeonUnlocks` — 管道分隔副本解锁 ID
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.SpecialEquipSlots` — JSON：主角魔法书 6 槽（§9.24；**AM-04 已实现**读写 + `IsUnique` 装配闸门）
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.AutoManufactureBatch` — JSON：最近一批 AutoManufacture `WarriorIds[]`（**D-054 方案 A**；空批写空数组；下一批覆盖）
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.EquipCommonExp` — 主角装备公共经验（§9.25 / [SPEC_03 §3.16](SPEC_03_GameRules.md)；**PE-02 已实现** `ProtagonistEquipmentService`）
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.ProtagonistEquipmentWarehouse` — JSON：`OwnedEquip[]` = `{ EquipId, Level, CurrentExp }[]`（§9.25 / §3.16；**PE-02 已实现**）
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.ShopProgress` — JSON：商店进度快照（`maxUnlockedLevelNumber`、`pendingOpenOnNewUnlock`、`currentRefreshCount`、`currentOffers[6]`，每项包含 `slotIndex/itemId/category(A|B)/priceSpirit/isSold` 等）。商店仅 Mode2 使用；Mode1 可忽略该键值
- **兼容：** Mode1 绑定时若新键空且旧键 `Gravedigger2026.SaveSlot.{i}.WarriorPool`（及 Formation/DungeonUnlocks）有数据 → 读旧键并可一次性迁移到新键

**士兵池 / 布阵持久化（方案 A + CampaignMode）：** `WarriorPoolService` / `BattleFormationService` / `DungeonUnlockService` / `AutoManufactureBatchRecordService` / `ProtagonistEquipmentService` 各自 `BindSlot(slot, campaignMode)` 进档加载、`ClearBound` 回档选；池/布阵/批次记录/装备仓变更立即 `PlayerPrefs` 写回；删档 `DeleteSlotData` 清 **两模式**键。进档顺序：先绑池再绑布阵；布阵加载时丢弃池中不存在的 `WarriorId`。仓库 / 经验 / 科技等完整 schema 仍 **TBD**（主角装备仓键与 Service：**PE-02 已落地**）。

**玩法模式门闩（方案 A，D-045）：** 新建/进入 → `CampaignModeSelectView`（UI-014）选 Mode1/Mode2 或取消；确认后 `CampaignModeService` 持有当前模式 → 按模式绑槽 → `ConfigCsvRepository` 按模式 CSV 根重载 → `EnterShell`。回存档选 Clear 模式。Mode2 士兵制造 = **AutoManufacture**（规则 [SPEC_03 §3.15](SPEC_03_GameRules.md)；实现 D-050～D-054 / `.scratch/mode2-auto-manufacture/issues/`）。

**士兵外观 From-Art（D-056 方案 B，WA-01 已编码）+ 职业区全覆盖（D-057，WA-02 已编码）：** 扩展 `WarriorAppearancePrefabAssembler`：Art 就绪且缺 `Warriors/{AppearanceId}.prefab` 时从 Art 创建 Visual；已有只修结构。然后用 `CloneApp02FallbackAppearances.RefreshWarriorCatalogBindings` 刷新 Defend/UM Catalog（并集 Mode1+Mode2 `BodyAppearanceConfig` 已有 Prefab 的 AppearanceId），**禁止** `GenerateAll`。`EnsureFormationClassZones` / 菜单「Ensure Formation Class Zones on Maps」以 **Mode2** `Manufacture_ClassConfig` 为权威 ClassId（读 CSV；缺补/表外删；第二前/后排及 `_0` 缺区偏移见 §13；已有区保留世界 XZ；样例 HalfExtents 锁定 `(3.85, 2)`；父/子 localRotation=identity，废止 IsoTileYaw）。issues：`.scratch/mode2-warrior-art-bind/`。

**自动制造 Stage（规则已锁；AM-03～13 闭环 + D-054/D-055 + Step2 单槽套书）：** `GameplayType=AutoManufacture` → `AutoManufactureStageModule` + `AutoManufactureService` + `AutoFormationDeployService`；`GameplayConfigId` 忽略；循环选料/职业/基础属性（**造兵时不套书**；默认定种族）→ 授予 `DefaultSkillIds` → StaticStat/外观/命名 → `TempWarriorWarehouse` → **先 Clear `BattleFormation`** → flush→`WarriorPool`（收集本批 Id）→ 批次记录 →（crafted>0）UI-016 Step2 每槽脉冲峰值调用 `SoldierManufactureMagicBookHook.ApplyEquippedBookAtSlot`（仅该书；含 `RaceWeightPick`/`StatMul`/`ForceClass`/`SoldierSkillLevelAdd`；`ForceClass` 命中重授技能）→ `RefinalizeInstance` → **再**按最终 `PlacementOrder` 落入 `FormationClassZone`；不计 Spirit/Control；不写 SoulId；演出失败/`Exit` → `ApplyRemainingSlots` 兜底；**D-054** 批末 `Replace`；**D-055** 播完再 Advance + Mode2 UM `AutoOpenFormationOnce`；0 兵 Tips + 跳过演出。UI-016 士兵行：Content 左右居中垫 + 按卡索引滚动；进入第一张从视口中心右侧一格滑入，该兵揭示后左移一格（卡宽取 `SoldierCardTemplate` 实际宽度）。士兵卡揭示（方案 B）：画外 Camera+RT；先 `Taunt` 再循环默认 Idle；传送带不等 Taunt。MetaShell 进档 Bind / 回档 Clear / 删档 Delete。

**运行时光标（UI-024，方案 A）：** `PlayerSettings.defaultCursor` = `Assets/Art/UI/Cursor.png`，`cursorHotspot` 对齐锁尖；导入约定见 [§4](#4-跨平台输入占位与运行时光标)；**不**写运行时 `Cursor.SetCursor`；Dig `UiDigCursorRing` 叠加其上。

**Meta 壳实现（方案 A，D-001～D-004 + D-045；关卡列表方案 B / UI-008；Tools GM 方案 A / UI-019 / D-061 + UI-020 / D-064；装备仓 / 魔法书弹窗方案 A / UI-022 / D-067 + UI-023 / D-068 / D-072）：** 单场景 `Assets/Scenes/Boot.unity`；`SaveSelect` / `InSaveShell` 以 Canvas Prefab 显隐切换（`Assets/Prefabs/Meta/`、`Assets/Prefabs/UI/`）。规则层：`SaveSlotService` + `GameplayStateService` + `CampaignModeService`；View 只订阅。工具「设置」→ **打开科技树画布**（见下 UI-012）；工具「关卡」→ 打开 Prefab **`LevelSelectPanel`**（InSaveShell 子级；`MetaShellAssetBuilder` 生成）：`ConfigCsvRepository.GetDistinctLevelIds()` 列出当前模式已加载 `Level_LevelOperationConfig` 的去重 `LevelId`（首次出现顺序）；点选 → `LevelOperationDriver.TryEnterLevel(levelId)`（自 StageNumber=1）；关闭按钮关掉面板。**Demo GM（ToolsPanel）：** 「增加主角装备」「增加魔法书」关闭 ToolsPanel → Prefab **`GmGrantListPanel`**（布局对齐 LevelSelectPanel）：装备列出当前模式 `ProtagonistEquipmentConfig` 去重 EquipId（Level 1 DisplayName）；点行打开嵌套 LevelPicker（该 EquipId 全部 EquipLevel 按钮）→ `ProtagonistEquipmentService.DebugGrantAtLevel`（未拥有入仓该级；已拥有覆盖 Level 且 CurrentExp=0）；Dig HUD 仍 `TryAcquire`；魔法书列出 `MagicBookConfig` 全表 → `SpecialEquipSlotsService.TryEquip`（无仓库；唯一已装/槽满失败）；Toast + 日志；列表保持打开可连点。「添加士兵」关闭 ToolsPanel → 仅 UM 布阵打开时打开左侧 Prefab **`GmAddSoldierPanel`**（职业/种族下拉、数量 1–999、自动上阵默认开）；`GmSoldierGrantService`：`BodyAppearance` 须 RaceId+ClassAffinity(ClassName) 匹配否则 Tips「找不到此种士兵！」；多匹配确定选取（`DefaultAppearanceId`∈匹配集 > `AppearanceLevel==ClassLevel` > 表序首条，禁止随机）；匹配则固定 Demo `BaseStats`（MaxHP=100、MoveSpeed=3、Strength/Agility/Intelligence=20）入池并可 `DeployBatch`；Dig HUD GM **保留**。**InSaveShell 装备 / 魔法书（方案 A / D-067 / D-068）：** 左下 `BackButton` 上方竖排「装备」「魔法书」（各 160×48，间距 8）；点击打开居中 Prefab **`EquipmentWarehousePanel`** / **`MagicBookSlotsPanel`**（对齐 LevelSelectPanel：全屏 dim + 中框 + Title + Close；`sortingOrder` ≥ 100）。装备仓只读：`EquipmentWarehousePanelView` 滚动列出 `OwnedEquips`（当前等级 `DisplayName`/`EquipId` + `Lv.{Level}` + `Description` + `IconAssetId`→`Resources.Load<Sprite>`）；空态「尚未拥有装备」；订阅 `Changed`；进档注入 Service+Configs。菜单 `Ensure EquipmentWarehouseList (UI-022)` 手术补列表。魔法书嵌套共享 `Assets/Prefabs/AutoManufacture/BookRow.prefab`（与 UI-016 演出同一份）；`SpecialEquipSlotsService.TrySwap` 交换任意两槽（含空槽）后立即 persist 并 `Changed`；`MagicBookSlotsPanelView` 点占用槽在槽下浮动「DeleteButton」，经 `ConfirmDialogView`（`sortingOrder` 110）确认后 `TryUnequip`（D-072）；演出 BookRow 订阅 `Changed` 同步。菜单 `Ensure InSaveEquipMagicBookPanels (UI-022/023)` 手术补 Prefab。菜单 `Ensure MagicBook BookRow (UI-023)` 与 `Gravedigger2026/AutoManufacture/Nest BookRow into Presentation Root` 手术嵌套共享 `BookRow.prefab`。菜单 `Gravedigger2026/Meta/Ensure GmGrantListPanel (UI-019)` / `Ensure GmAddSoldierPanel (UI-020)` 手术补 Prefab。**布局：** `InSaveShellPanel` 的 `StateLabel` / `StageInfoLabel` 顶栏水平居中（锚点顶中；`StateLabel` y≈−16、`StageInfoLabel` y≈−52），不挡中部战斗视野。壳层正式手动切三态仍 **TBD**；Demo 暂提供进档壳 **Debug「切下一态」** 仅用于手验 D-004（不得等同工具「关卡」）；另提供 **Debug「推进阶段」** 手验 D-010（占位结束当前阶段 → 下一阶段 / VictorySettlement）。

**关卡驱动（方案 A，D-010 + D-075）：** `ConfigCsvRepository` 只读 CSV（路径见 [§14.5](#145-运行时-csv-加载路径demo)）；`LevelOperationDriver` 按 `LevelId` 取行、`StageNumber` 升序运行；进入阶段时设置 `GameplayState`，经 `IStageModule` 进入/离开钩子挂各玩法（Shop / Dig / UM / Defend / PushMap / AutoManufacture）。`GameplayType=Shop` / `UpgradeManufacture` / `AutoManufacture` 时 **忽略** `GameplayConfigId`（不查 Dig/Defend 表）。Mode2 样例：`Shop` → Dig → AutoManufacture → UM → PushMap。Dig/Defend 解析对应表行后校验 `DigMapId` / `BattleMapId` ∈ `Ground_01`…`Ground_05`，逻辑路径 `Assets/Prefabs/Maps/{Id}.prefab`。UI/日志须可见 LevelId、StageNumber、GameplayType。

**Dig 垂直切片（方案 A，D-020）：** `DigStageModule`（`IStageModule`）Enter 时按 `DigMapId` Instantiate `Assets/Prefabs/Maps/{Id}.prefab`，并挂 `DigStageRoot`（`Assets/Prefabs/Dig/`）。规则层 `DigSessionService`（纯 C#）负责有效时长倒计时、开局/过程生成、DigAction 停留触发与忙碌锁、扣血、仓库/精魂入账、阶段奖励汇总；`DigProtagonistCapabilities` 由 **科技树 + 主角装备 Dig 域** 重算后注入（见 UI-012 / §3.16 / PE-03）。DigAction 候选：光标圆 ∩ 坟 `DigHitShape` 本地 XZ 凸包（世界变换后；粗筛用 `BoundingRadius`）；无凸包则回退障碍圆。表现：`DigPrefabCatalog` 绑定 Digger / `Grave_{QualityId}`（须覆盖当前模式品质表全部 Id；Sprite 来自 `Art/Dig/Graves/Grave_{QualityId}/`）/ 地图变体 / `UiDigCursorRing`；圆圈光标（Prefab 双层、描边像素恒定）、坟墓 HP 样式、DigReward 飞向 HUD 头像框、DigStageSummary 由 View 订阅；**不** Instantiate 地图 Digger。时长归零 → 取消进行中 DigAction（不结算扣血）→ DigStageSummary 确认 → `LevelOperationDriver.TryAdvanceStage`。**Demo GM（Dig HUD）：** `DigHudView` 右上「增加坟墓」「增加躯体材料」→ `DigStageController` → `DigSessionService.DebugSpawnGraves(10)`（复用加权/`TrySpawnOneGrave`）与 `DebugGrantAllBodyParts(10)`（遍历 `configs.BodyParts` → `Warehouse.AddItem`，无 AutoConvert，触发 `WarehouseChanged`）；不计入 DigStageSummary。Mode2 另有「装备战士强化」→ `SpecialEquipSlotsService.TryEquip("MagicBook_WarriorEnhance")`（D-058 手验）。主角装备手验（D-059）：「获得铁铲」→ `ProtagonistEquipmentService.TryAcquire("Equip_IronShovel")`；「装备公共经验+50」→ `DebugGrantCommonExp(50)`；「划入铁铲升级」→ `TrySpendCommonExp("Equip_IronShovel", 1)`；日志打印 Level / CurrentExp / 合并后 `DigCursorRadius`。矿灯手验（D-060）：「获得矿灯」→ `TryAcquire("Equip_MinerLamp")`；「划入矿灯升级」→ `TrySpendCommonExp("Equip_MinerLamp", 1)`；日志打印 Q4/Q5/Q6 `GraveSpawnWeightBonus`。Prefab / `DigAssetBuilder` 绑按钮。禁止运行时引用 `SmallScaleInt/`；规则层禁止读 Sprite/像素。

**科技树画布（方案 A，UI-012 可选）：** `ConfigCsvRepository` 追加加载 `Tech_TechTreeConfig.csv` / `Tech_TechEffectConfig.csv`。规则层纯 C# `TechTreeService`（存档级，挂 Meta 壳）持有已学会集合与 `UnlockedFeatureSystems`；进档/`Reset` 时对 `InitiallyUnlocked` 自动学会并应用效果；学习闸门 = 未学会 ∧ TechPoint≥LearnCost ∧ ≥1 已学会前置（由 `UnlockNextTechIds` 求逆）；学会扣点 → 标记 → 解析 `AttributeModifiers` 加法求和，并叠加仓内 Dig 域 `EquipEffect` → 写入 `DigProtagonistCapabilities`（Demo `DiggableQualityIds` 仍取全品质表，便于挖坟手验）。表现：临时 `Assets/Prefabs/Meta/TechTreeCanvasRoot.prefab`（节点坐标 Prefab 摆放；uGUI 空白处 LMB 拖移；悬停名+效果描述；连线按正向边；三态框色）；工具「设置」打开画布；Debug 可注入 TechPoints。禁止运行时引用 `SmallScaleInt/`。

**UM 升级区（方案 A，D-030；Mode2 差分方案 C / D-053 + D-054）：** `UpgradeManufactureStageModule` Enter 时按 `CampaignMode` Instantiate `Assets/Prefabs/UpgradeManufacture/UpgradeManufactureStageRoot.prefab`（Mode1：**默认全屏制造区**）或 `UpgradeManufactureStageRoot_Mode2.prefab`（Mode2：`ManufactureZone` 默认关，保留 GM 升级 Modal / 完成 / 布阵 / **制造记录**）。升级为 Modal，顶部「GM升级」打开、右上「X」关闭；布阵经「布阵」打开共享编辑器；Mode2「布阵」右「制造记录」打开只读批次 Modal（`UmAssetBuilder.BuildAndSaveMode2StageRoot` 追加；Mode1 字段可空）。`ConfigCsvRepository` 加载 `Manufacture_ProtagonistLevelConfig.csv`。规则层纯 C# `ProtagonistProgressService` 持有内存态 `Level` / `LifetimeExperience` / `TechPoints` / 生效 `ControlPowerCap` / `ProtagonistMaxHP`；累计阈值连升并应用表行奖励与上限。Debug 注入仍可用；正式 Defend 胜利入账见 D-043。底部「完成」→ `TryAdvanceStage`。禁止运行时引用 `SmallScaleInt/`。

**UM 制造区（方案 A，D-031 → UI 重做 / Pool 再造）：** `ConfigCsvRepository` 追加加载 `Manufacture_SoulConfig` / `ClassConfig` / `GemConfig` / `RaceConfig` / `BodyPartConfig` / `BodyAppearanceConfig` / `ExtraEquipmentConfig` / `GemSuffixNameConfig`。规则层纯 C# `ManufactureService` 持有 15 个严格槽位（头1/躯干1/臂2/腿2/灵魂1/宝石6/坐骑1/翅膀1），按 Id 解析所属表自动路由到合法空槽，类型不符 / 同类型宝石重复 / 库存不足即拒绝；每次槽位变化重算预览（`Base(S)=Σ StatBonus`、`Equip`、`GemMult`、`RaceAdjust`、`StaticStat`、静态 `MaxHP=ceil(BodyLife+StaticStat(Str)×MaxHpStrengthMult)`、`TotalSpiritCost`、`ControlPowerCost`、试算种族与外观）。「制造」闸门 = 最低要求（躯干+臂2+腿2；**灵魂可选**）且 `SpiritEssence ≥ TotalSpiritCost`（头/灵魂/宝石/坐骑/翅膀对**提交**均可选）。无灵魂槽：实例 `SoulId=Soul_00`，灵魂侧费用/AttackMode 等读 `Soul_00`，**强制** `ClassId=Class_Servants`（不扣仓库灵魂）。制造成功写入 `WarriorInstance.SourceItemIds` + `SourceSpiritCost`；`TryRemanufacture(sourceWarriorId)` 按配方后台校验/扣料/再跑聚合与掷种族外观流水线并 `_pool.Add` **新实例**（不改 `_slots`）；材料不足 / 精魂不足错误码供 Tips。**躯体外观可视预览闸门**（表现层）：头+躯干+臂×2+腿×2+坐骑+翅膀已填（**灵魂与宝石不参与**）→ Instantiate 试算 `AppearanceId` Prefab 并 `WarriorAnimView` 播攻击再待机；否则静态占位图。`WarehouseService` 扩展同前；Debug「注入制造套件」同前。表现：`ManufacturePanelView` — PreviewPanel 左、中心环绕槽位方格（左：头/臂1/腿1/翅膀；右：躯干/臂2/腿2/坐骑；预览内底灵魂；下排半尺寸宝石×6）、PoolPanel 右为 **ScrollRect 士兵框列表**（`PoolSoldierFrameView`；选中显「再造1个」）、UmCanvas 中上部 Tips（1s：「材料不足」/「精魂不足」）、底栏库存方格横滑 + Input 拖拽入槽、三操作钮在库存下。布局权威：`UmAssetBuilder` 重建 StageRoot Prefab。外观资源：`UpgradeManufacturePrefabCatalog` 绑定 `AppearanceId → Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`。禁止运行时引用 `SmallScaleInt/`。

**UM 布阵区（方案 A→拖拽编辑器，D-032；Mode2 差分方案 C / D-053；D-064 返回位；D-065 悬浮框）：** 纯 C# `BattleFormationService`（存档级，`BindSlot` + PlayerPrefs JSON）持有 `{WarriorId, PositionX/Z, RemainingHP}`；`TryDeployAt` / `TrySetPosition` / `TryUndeploy`；控制力占用 = Σ `ControlPowerCost` vs `ControlPowerCap`（Mode2 士兵 Cost 恒 0，且**不**按控制力拒上阵）。表现：共享 Prefab `Assets/Prefabs/Formation/FormationEditorRoot.prefab`（Mode1：士兵栏 80×80 横滑、拖拽上阵/改位/下阵、左上控制力 HUD、UM「返回」锚右下 SoldierBar 上方 `(-24,124)` / Defend「开战」）；Mode2 用 `FormationEditorRoot_Mode2.prefab`（控制力 HUD 默认关；士兵栏方格尺寸以 `SoldierSlotTemplate` RectTransform 为准，Demo **125×180**；`FormationSoldierBarView` 运行时读取模板宽高，禁止硬编码 80×80；`SoldierBar` 上方右侧常驻 `CompleteButton`（UM/Prepare 均显示，`(-24,124)`）；其正上方叠放 `StartBattleButton`（Prepare 开战）；UM「返回」在 Complete 再上方 `(-24,180)`；`FormationEditorController.CompleteRequested`；UM 宿主关编辑器后走与主屏相同的阶段结束；Catalog `ResolveEditorRoot`）。士兵栏方格文案：上行为 `ClassName`（`FormationEditorController` 经 `ConfigCsvRepository.TryGetClass`；缺行回退 `ClassId`）、下行为 `Lv.{ClassLevel}`（缺行按 0）；**不**展示 `WarriorId`。**Mode2 悬浮框（UI-021 / D-065 / D-070）：** `FormationCanvas/SoldierHoverTooltip`（`FormationSoldierHoverTooltipView`）；士兵栏沿用 Input `FindSlotAt` hover（槽位 `raycastTarget` 仍关）；有兵格 → 展示 ClassName / `{n}级` / 种族 DisplayNameKey / BaseClass / PromoteClass / 静态 MaxHP 与三维（主属性行「(主属性)」）/ `SoldierSkills` 图标+名；图标 `Resources.Load("UI/Skills/"+SkillId)`，**不**用 `IconAssetId`；每个技能 `Icon` 右上角 5×5 `EffectStatus`（最后子节点）按 `SkillConfig.EffectImplemented` 绿/红；横滑/拖起/离槽/`End` 隐藏；`CanvasGroup.blocksRaycasts=false`；Mode1 Prefab **不**挂此框。UM 主屏 Complete 右「布阵」打开编辑器；地图取关卡内下一 Defend 的 `BattleMapId`（缺省 `Ground_01`）。与 Defend / PushMap Prepare **共用**选型。禁止运行时引用 `SmallScaleInt/`。

**一键上阵（Mode2 布阵编辑器）：** `FormationEditorRoot_Mode2` 在 `CompleteButton` 左侧增加“一键上阵”按钮；点击后从当前地图收集 `FormationClassZone`，按士兵 `WarriorInstance.ClassId` 分组，并对**未上阵**的士兵进行区内随机放置（同一职业区内非重叠：候选点遍历+占用 Footprint 分离；无区/无空位则留池）；已上阵士兵不重置。实现复用 `FormationZoneSpiralSearch` 的区内安全判定与分离约束。

**战斗模式选关（方案 A，D-044）：** `DefendStageModule` Enter 先 Instantiate `Assets/Prefabs/Defend/BattleModeSelectRoot.prefab`（或运行时等价 UI）；`DefendPhase=ModeSelect`；模式1「保卫战」列出全部 `DefendGameplayConfig`；运作表 `GameplayConfigId` 作 Recommended 默认高亮；确认后用所选行覆盖 `LevelStageContext.DefendConfig` 再进入现有 Prepare。模式2「推图战」列出全部 `PushMapGameplayConfig`；确认后调用 `LevelOperationDriver.TryHandoffModeSelectToPushMap(configId)`：`Exit` 当前 Defend 模块 → 保留 `LevelId`/`StageNumber` 改写 `GameplayType=PushMap` + `PushMapConfig` + 地图路径 → `SetState(PushMap)` → `PushMapStageModule.Enter` → `StageChanged`。禁止运行时引用 `SmallScaleInt/`。

**Defend Prepare / 开战 / 护盾（方案 A→共享编辑器，D-040）：** `DefendStageModule` 在 ModeSelect 确认保卫战后 Instantiate `DefendStageRoot` + `Prefabs/Maps/{BattleMapId}`；Prepare 挂同一 `FormationEditorRoot` UI（复用本阶段地图，不双开地图）；开战 ≥1 → 销毁预览后按布阵正式部署；`Shield`/`CombatDurationSeconds` 逻辑不变。禁止运行时引用 `SmallScaleInt/`。

**Defend 刷怪与寻路（方案 A，D-041）：** `ConfigCsvRepository` 追加加载 `Defend_WaveSpawnConfig.csv` / `Defend_MonsterConfig.csv`。开战时 `DefendSessionService` 按 `WaveConfigId` 装载刷怪行；`Combat` 中每当 `RemainingCombatSeconds` 变为某整秒（含开战瞬间）时，触发尚未触发且 `SpawnRemainingSeconds` 相等的行（同秒按 `SpawnOrder` 升序），经事件交给 View Instantiate。Demo 最小出生点：地图 Prefab 上 `DefendSpawnPointSet` 固定点（`ClockDirection`→钟点位；`RegionRandom`→点池随机；Inside/Outside 本片均用固定点，精确 OutsideMap **后置**）。Instantiate 地图后 Runtime 烘焙最小可走 NavMesh（覆盖地图活动区 + 出生点）。怪物 Prefab：`Assets/Prefabs/Defend/Monsters/{ModelId}.prefab`（有 Art 则 §15.2 `Visual` 组装；否则临时立方体；Catalog 绑定）。`MonsterAgentView`（`NavMeshAgent`）按 `TargetSelect` 选目的地（本片士兵位可作为 PreferWarrior/Nearest 候选；无士兵则回退主角），按 `TargetRetargetIntervalSeconds` 重寻路；进 `AttackRange` 后按 `AttackSpeed` 普攻 → `Shield -= 1`（忽略 AttackPower）。`Shield ≤ 0` → `DefendPhase.Ended` + LevelFailure 钩子（打日志；完整关卡中止见 D-043）。士兵普攻 / 清场胜利 **不做**。禁止运行时引用 `SmallScaleInt/`。

**Defend 士兵近战（方案 A，D-042 近战片 + MP-06）：** `WarriorCombatMath` 按 `ClassConfig.PrimaryStat` + `CombatConvertCoeffs`（缺键回退 **`CombatConstantConfig`**）派生 `NormalAttackPower` / `AttackSpeed`。`DefendSessionService` 开战登记士兵 HP（`MaxHP=ceil(BodyLife+StaticStat(Str)×MaxHpStrengthMult)`，`RemainingHP` clamp）与刷怪登记怪物 HP；规则层确认近战 `HitConfirm`（前摇结束且目标仍存活、在 `AttackRange` 内）→ 怪 `HP -= NormalAttackPower`；怪对兵 `AttackPower` 直接扣 HP（无护甲）。`HP≤0` 无宝石 → `CombatDead`（停手）；有宝石 → 立即 PermanentDeath 标记（物资去向见 D-043）。表现：`WarriorAgentView` 仅在 EngageZone 内选最近存活怪；追击 `GoalKind=AttackSlot`（`AttackSlotService`+`MassMoveScheduler` Move）；无候选时非叛变士兵 `GoalKind=FormationHome`（返回途中继续选敌，发现目标即中断返回）；`AttackMode=Melee` 走近战前摇；`WarriorAnimView` 播移动/攻击/死亡（见 §15.5）。清场条件（刷怪行全触发 + 已刷怪全灭）→ `ClearVictoryConditionDetected` 事件/日志（**不**入账、**不**切胜利 Ended；见 D-043）。禁止运行时引用 `SmallScaleInt/`。

**Defend 士兵远程弹道（方案 A，D-042 远程 / 05c2）：** 开战登记同时写入 `ClassConfig.RangedProjectileSpeed` / `RangedTimeoutSeconds`。`AttackMode=Ranged` 士兵与近战共用 EngageZone 最近选敌、`AttackSpeed` 周期与无目标时返回 `FormationHome`；进 `AttackRange` 后 Instantiate 临时 `Assets/Prefabs/Defend/Projectile.prefab`（Catalog 绑定）；开火时 `WarriorAnimView` 播普攻 Trigger。`ProjectileView` 运动学飞向锁定怪 RuntimeId：**距离 ≤ hitRadius** 视为碰撞命中 → Session `TryConfirmRangedHit` → 怪 `HP -= NormalAttackPower`；**超时**销毁且不扣血。法师/射手同远程通道（仅 `PrimaryStat` 不同）。禁止运行时引用 `SmallScaleInt/`。

**Defend 失控开战 roll 与胜负结算（方案 A，D-043）：** `ConfigCsvRepository` 加载 `Combat_LossOfControlConfig.csv`。开战瞬间按布阵 `ΣCost/Cap−1` **锁定** Degree/Tier（超额不挡开战）；`Degree>0` 时对各上阵士兵用 `FinalLossChance=clamp(0,1,TierChance+RaceBonus+ΣGemBonus+ΣSkillBonus)` 独立 roll → `IsRebel`（日志可观察）。Demo `ΣSkillBonus` = 实例 `SoldierSkills` 按烘进等级查 `SkillConfig.LossOfControlChanceBonus` 之和（无技能=0；灵魂/宝石/外置 `Skills` 并行仍 TBD，本 Demo 不加）。Rebel **不受 EngageZone 限制**，就近打存活主角/其他士兵/敌人；对主角普攻 → `Shield-=1`；对兵/怪走士兵普攻通道。清场条件满足 → `DefendPhase.Ended` + PermanentDeath 最小结算（宝石回仓、清布阵、移出池）→ `ProtagonistProgressService.AddExperience(100)`（Demo 固定阶段经验）→ `LevelOperationDriver.TryAdvanceStage`。`Shield≤0` → Ended + 同 PermanentDeath 结算 → **不**入账本阶段经验 → `AbortLevelAsFailure`（无关卡胜利结算；已有资源保留）。禁止运行时引用 `SmallScaleInt/`。

**PushMap 配置表加载（方案 A，PM-02）：** `ConfigCsvRepository` 追加加载 `PushMap_PushMapGameplayConfig.csv` / `PushMap_PushMapSpawnConfig.csv`；`Defend_MonsterConfig` 解析 `AggroMode` / `AlertRadius`（缺省见 §9.19）。样例至少 1 个 `GameplayConfigId` + 多 Spawn 行（无陷阱/陷阱/BOSS）。StageModule / AI / 占领逻辑 **后置**（PM-03+）。禁止运行时引用 `SmallScaleInt/`。

**PushMap Stage 接线（方案 A，PM-03 + D-044 Mode2）：** `LevelOperationDriver.TryBuildContext` 支持 `GameplayType=PushMap`：`GameplayConfigId` → `PushMapGameplayConfig` 主键（直查），`LevelStageContext.PushMapConfig` + `MapPrefabPaths` 允许 `PushMap_*`（同解析 `Assets/Prefabs/Maps/{MapId}.prefab`）。`PushMapStageModule`（`IStageModule`；无独立 ModeSelect）：`GameplayType=PushMap` 直进 `PushMapPhase=Prepare`；亦可由 Defend `BattleModeSelect` 模式2经 `TryHandoffModeSelectToPushMap` 进入同一模块。薄规则 `PushMapSessionService`（**独立但语义对齐 §3.12**：Prepare→Combat；开战 ≥1 上阵；`Shield=ProtagonistMaxHP`；`Shield≤0`→LevelFailure；开战锁定 Degree/Tier 并对上阵士兵按 `FinalLossChance` roll→Rebel 日志可观察）+ `PushMapStageController`（Instantiate `Maps/{MapId}`，复用共享 `FormationEditorRoot` 同一 BattleFormation）。**Combat 战斗相机：** Runtime Ensure 子物体 `PushMapCamera`（与 Defend 同俯视契约：正交、`Euler(90,0,0)`、高度/`near`/`far`/开战默认 Size ← **`CombatConstantConfig`**（样例高度 `18`、Size `2`；异于 Defend 的 `max(halfExtents)−CameraOrthoSizeMargin`）；SolidColor/`depth=5`）；Prepare 关掉（用 FormationCamera），开战启用并重配；禁止开战落到 Boot 透视主相机。**PM-09 镜头跟随（方案 B / v0.81.0）：** `PushMapCameraFollowController` 挂于 `PushMapCamera`；Combat `CameraFollowMode=Auto|Manual`——Auto 跟随地图 `CameraFollowPath` 折线上存活忠诚兵的最大投影进度 `s∈[0,1]`（镜头对准折线点，不粘士兵；领头失效 SmoothDamp 回退不 Snap；全灭定格；缺轨/未烘焙 warn 并回退「距 CurrentObjective 最近忠诚兵」）；`EnterAuto`/开战启用 Snap 到当时折线点；左键拖拽（非 UI）切 Manual 并按正交像素→世界 XZ 平移；StageRoot 下 Runtime Ensure 底中「恢复跟随」按钮（锚点约 `(0.5,0.1)`，仅 Manual 显示）→ `EnterAuto`；滚轮缩放 Size（`mouseScrollDelta.y>0` 拉近变小；步进 `0.5`/档；钳制 `[0.5,20]`；指针在 UI 上跳过）；缩放不切换模式、恢复跟随不重置 Size；高度/旋转不变；规则层不参与。样例：`Level_LevelOperationConfig` `Level_01,5,PushMap,PushMap_01`（直进）与 Defend 阶段 Mode2 选 `PushMap_01`（handoff）等价进入 Prepare。禁止运行时引用 `SmallScaleInt/`。

**PushMap 刷怪与陷阱（方案 A，PM-05）：** `PushMapSessionService` 开战装载 `PushMapSpawnConfig` 行；表现层 **Bake NavMesh → 部署 → `FireStartBattleSpawns`**：无陷阱且关联目标未占 → `PushMapSpawnRequested` 事件（位置由 View 解析）；绑定 `TrapZoneId` 的点 → `TryNotifyTrapEnter` 首次触发；`ObjectiveCaptured` → 该关联点本场停刷（已刷保留）。`PushMapStageController` 收集 `SpawnPoint`/`TrapZone`，Instantiate 怪（含 Boss）入 `_monsters`（`PushMapMonsterAgentView`），Update 探测忠诚兵首次进圈。怪物 AI 暂用 Defend 默认追击语义；对主角扣盾经 `PushMapSessionService.ApplyShieldHit`。AggroMode 四态落地 **PM-06**；BOSS 通关结算后置（PM-07）；**不使用** `WaveSpawnConfig` 倒计时。运行时契约见 §9.23。样例 `TrapZoneId` 对齐为 `TZ_01`。禁止运行时引用 `SmallScaleInt/`。

**PushMap 怪物占地散开（方案 A，PM-10；v0.73.9 收紧）：** `MonsterConfig.BodyRadius`（缺省 `0.35`）；表现层按半径环形/螺旋错开同点与邻近已刷存活怪落点（`NavMesh.SamplePosition`）。**收紧：** 采样半径仅局部 ≈`max(0.75, BodyRadius×2.5)`；命中须相对刷怪点 `basePos` 在牵引上限内（≈环/螺旋半径+余量，绝对上限 ≈`max(3, BodyRadius×10)`）；失败则缩环/继续螺旋，最终允许重叠回退基点——**禁止**大半径吸附跨空气墙落到菱形外侧。`PushMapMonsterAgentView` Bind Warp 采样同为局部（≈`max(1, BodyRadius×3)`，不再用 12）。`NavMeshAgent.radius = min(BodyRadius, max(0.05, AttackRange − 0.1 − 0.05))`；`Stationary*` 仅依赖刷出占位；Defend 刷怪散开后置。**我方士兵** Demo：`NavMeshAgent.radius=0.1`、`height=0.1`。禁止运行时引用 `SmallScaleInt/`。

**PushMap AggroMode 四态（方案 A，PM-06）：** `PushMapMonsterAgentView` 按 `config.AggroMode` 分支。`ActiveChase`：忠诚士兵进 `AlertRadius` → **AttackSlot** 追击该兵直至怪死（MP-05；非中心 `SetDestination`）；`PassiveChase`：未挑衅静止，`NotifyProvoked()` 后追击；`StationaryActive`：永不移动，忠诚兵进 `AttackRange` 攻击、离开停；`StationaryPassive`：永不移动，须先 `NotifyProvoked()` 且目标仍在 `AttackRange` 才攻。主动发现与挑衅**仅**对忠诚士兵（`!IsRebel`）。挑衅 Demo 契约：`PushMapStageController` 检测忠诚 `PushMapAdvanceView` 首次进入某被动怪 `AttackRange` → 调其 `NotifyProvoked()`（等效「士兵先攻击」；士兵 HP / 命中结算后置）。命中仍 `AttackMode` 方案 D；主动态对主角不进 `AlertRadius` 主动发现，但已交战命中主角仍 `ApplyShieldHit`。普通怪真实士兵伤害 / 技能施放 / 副本玩法正文 **不做**。禁止运行时引用 `SmallScaleInt/`。

**PushMap BOSS 通关与奖励钩子（方案 A，PM-07 + UI-017/018）：** `PushMapSessionService` 对 `IsBoss` 刷出行累计待击杀数；`TryNotifyBossKilled` 递减，归零 → `Ended` + `VictorySettled(StageExpReward)`；另计战斗耗时、击杀数、CaptureLoot 展示 ledger；叛变同步 `IsRebel`；无忠诚存活 → `RequestLevelFailure`。表现：`AddExperience` → **战斗结算面板**（非立即 `_onVictoryAdvance`）；失败同弹结算。Continue：失败 → AbortLevel + LevelSelect；胜利 → 奖励弹窗（Exp+CaptureLoot）→ 结束关卡 + LevelSelect。占领仍当场入账 `CaptureLoot`。禁止运行时引用 `SmallScaleInt/`。

**PushMap 空气墙 NavMesh（方案 A，PM-08）：** 开战 Runtime Bake 在 IsoDiamond 可走面之外，收集地图 `AirWall`，以 `NavMeshBuildSourceShape.Box` + area=`Not Walkable` 注入（尺寸=`HalfExtents×2`；`Matrix4x4.TRS(position, rotation, 1)` → **含 Y 轴 45°**）。扩展 `DefendNavMeshBaker.Bake(..., notWalkableBoxes)`；`PushMapStageController` 开战传入；敌我 `NavMeshAgent`（士兵推进 / 怪物追击）均不可穿。**不做** `NavMeshObstacle` Carve、复杂多层障碍 polish。契约见 §9.22。禁止运行时引用 `SmallScaleInt/`。

**大规模战斗寻路（方案 B，MassCombatPathing / SPEC 已锁）：** 共享目标 **FlowField** + 追击 **AttackSlot** + 友军 **LocalDetour**；容量双方约 200；静态 `AirWall`/可走掩码进场；友军禁止 Carve。实现切片见 `.scratch/mass-pathing/issues/`；运行时契约见 §9.7。**MP-04：** PushMap 忠诚推进已接 FlowField+LocalDetour+`MassMoveScheduler`。**MP-05：** 交战/追击已接 `AttackSlotService`（士兵+怪；槽刷新≤50/帧；无全员每帧 `CalculatePath`）。**MP-06：** Defend `WarriorAgentView`/`MonsterAgentView` 对等接线；忠诚无 Engage 目标→`GoalKind=FormationHome`；追击走槽位；与 PushMap 共用目的地语义。**MP-07：** Debug 压测入口 `MassPathingPerfStress` / `MassPathingPerfStressView`（约 200+200 桩单位 + Stopwatch）；超预算回退见 §9.7。**士兵任务 Debug 标签（方案 A）：** Combat 中 `WarriorAgentView` / `PushMapAdvanceView` 脚下运行时 TextMesh 显示当前 `GoalKind` 中文简标；进档壳 Debug 开关，**默认开**；仅目标类。**友军脚下圈 AllyFootCircle：** 忠诚存活士兵脚下绿描边 + 内黑 α160/255（半径=`BodyRadius`，Order In Layer=`1`，localPos Y=-0.05 Z=-0.2，rotation X=-30）；`WarriorAnimView` 批量改 sortingOrder 时跳过。**CombatSkillIcon（UI-025 / D-071 / 方案 A）：** PushMap 士兵子节点 `SpriteRenderer`；`worldSize = pixelSize × 2 × camera.orthographicSize / Screen.height`；头顶 35px / 脚下 20px；Prefab `Assets/Prefabs/PushMap/SkillIconHud.prefab` 经 `DefendPrefabCatalog` 接线；规则事件 `SkillIconPopup(warriorId, skillId)` / `SkillPersistChanged(warriorId, skillId, on)`。禁止运行时引用 `SmallScaleInt/`。

**SkillEffect 管线意图（D-073 / 方案 B+）：** 纯 C# `Assets/Scripts/Core/Combat/CombatStatusService.cs`、`SkillEffectPipeline.cs`、`SkillEffects/*Handler.cs`（命名空间 `Gravedigger2026.Core.Combat`）。`PushMapSessionService` **只**在既有结算点调用 `Dispatch(TriggerHook, context)` 与 `CombatStatusService.Tick`；**禁止**按 `SkillId` `if/switch`。CombatSkillIcon 仍走 `SkillIconPopup` / `SkillPersistChanged`。Mode1 新列可空占位；Defend 不接线。issues `.scratch/soldier-skill-effects/`。**SE-07：** 远程命中 `Dispatch(OnProjectileHit)`；`ProjectileView` 为**通用穿透通道**（命中后保持当前速度方向；`alreadyHitRuntimeIds` 防重复；Handler 写 `ExtraHitsRemaining` / `DamageMul`；无弹道不触发）。禁止 View 按 `SkillId` 分支。**SE-09：** 重选目标瞬间 `Dispatch(OnWarriorTargetAcquired)`；Handler 给最远敌 + 背后落点；View 局部 `SamplePosition`+`Warp`；AttackSlot / MassMove 同步；失败不进 CD。

**架构提示：** `ToolsPanel` 属 Meta 壳层 UI；玩法状态由规则层持有，View 只订阅展示（见 §13）。挖坟：规则层负责生成、计时、DigAction 触发/忙碌锁与扣血；菱形地图与圆圈光标、帧动画、奖励飞向 HUD 头像框由 View 表现；逻辑层为整体可放置空间（非格子）。UM 阶段不查玩法配置表主键；升级进度本片内存持有。Defend：规则层输出目标/目的地；移动服务执行（规模栈见 §9.7）；Demo 最小可走面见 §9.7 / SPEC_03 §3.12。

### English

**Status: Aligned with SPEC_03 §3.8 (Meta shell + Level-pipeline vertical slice)**

**In scope (D-001–D-044):** 3 fixed slots with local occupied flag + minimal pipeline fields; InSaveShell default Dig placeholder + floating Tools + bottom-left Equipment / MagicBook (UI-022 / UI-023); sample Level start from shell (D-003/D-010); CSV-only LevelOperation drive Dig→UM→Defend; Dig / UM / Defend verticals per §3.8 (temp art OK); UM stage `GameplayConfigId` **ignored** (§9.1); Defend ModeSelect gate (D-044); Defend Demo-min spawn + NavMesh. **UI-024:** whole-Play hardware pointer `Art/UI/Cursor.png` via `PlayerSettings.defaultCursor`; Dig ring does not replace the OS pointer.

**Out of scope:** Natural-language Description effect parser; Mode1 combat skills; monster `Skills` casts; Defend skill wiring; formal art polish; exact OutsideMap geometry; **full** save schema (Warehouse/Exp/Tech still TBD; warrior pool + formation locked this slice); concrete TechTree node values/icon polish & full feature-system enum; Tools entries beyond D-061 / D-064 GM / D-067 / D-068 / D-072 / D-073; bake full §9 column/type validation (Demo: filename + header only); anything not in §3.8.

**Persistence intent:** Local by slot index `0..2` + **`CampaignMode`**. **Demo Meta locked: `PlayerPrefs`**. Keys:
- `Gravedigger2026.SaveSlot.{0|1|2}.Occupied` (`0`/`1`; **shared per slot**, mode-agnostic)
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.WarriorPool` — JSON: `NextSerial` + `WarriorInstance` manufacture snapshot array (incl. `SourceItemIds` / `SourceSpiritCost` / `EquipStats` / `BodyLife` / `SoldierSkills` / `VisualStyleId` / `VisualPriority` / `VisualIntensity` / `VisualModelScale`; see §9.9; **SS-02** JsonUtility roundtrip; **SS-03** Mode1 manufacture/remake grants `DefaultSkillIds`@Lv1; **SS-04** Mode2 grant + `SoldierSkillLevelAdd`; VisualStyle material + scale channels baked on Mode2 book hit only)
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.BattleFormation` — JSON: `{WarriorId, PositionX, PositionZ, RemainingHP}[]`
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.DungeonUnlocks` — pipe-separated dungeon unlock IDs
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.SpecialEquipSlots` — JSON: protagonist MagicBook 6 slots (§9.24; **AM-04 implemented** read/write + `IsUnique` equip gate)
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.AutoManufactureBatch` — JSON: last AutoManufacture `WarriorIds[]` (**D-054 Approach A**; empty batch writes `[]`; next batch overwrites)
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.EquipCommonExp` — protagonist equipment common Exp (§9.25 / [SPEC_03 §3.16](SPEC_03_GameRules.md); **PE-02 implemented** via `ProtagonistEquipmentService`)
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.ProtagonistEquipmentWarehouse` — JSON: `OwnedEquip[]` = `{ EquipId, Level, CurrentExp }[]` (§9.25 / §3.16; **PE-02 implemented**)
- `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.ShopProgress` — JSON: shop progress snapshot (`maxUnlockedLevelNumber`, `pendingOpenOnNewUnlock`, `currentRefreshCount`, `currentOffers[6]` with `slotIndex/itemId/category(A|B)/priceSpirit/isSold`). Mode2 only; Mode1 can ignore this key.
- **Compat:** On Mode1 bind, if new key empty and legacy `Gravedigger2026.SaveSlot.{i}.WarriorPool` (and Formation/DungeonUnlocks) has data → read legacy and optionally one-shot migrate to new key

**Warrior pool / formation persistence (Approach A + CampaignMode):** `WarriorPoolService` / `BattleFormationService` / `DungeonUnlockService` / `AutoManufactureBatchRecordService` / `ProtagonistEquipmentService` each `BindSlot(slot, campaignMode)` on enter-save, `ClearBound` on return to SaveSelect; mutate → immediate `PlayerPrefs` write; delete slot → `DeleteSlotData` clears **both** mode keys. Enter order: bind pool then formation; drop formation rows whose `WarriorId` is missing from pool. Warehouse / Exp / Tech full schema still **TBD** (protagonist equipment warehouse keys + Service: **PE-02 landed**).

**CampaignMode gate (Approach A, D-045):** Create/Enter → `CampaignModeSelectView` (UI-014) pick Mode1/Mode2 or cancel; on confirm `CampaignModeService` holds mode → bind slot by mode → `ConfigCsvRepository` reload from mode CSV root → `EnterShell`. Clear mode on return to SaveSelect. Mode2 soldier manufacture = **AutoManufacture** (rules [SPEC_03 §3.15](SPEC_03_GameRules.md); impl D-050–D-054 / `.scratch/mode2-auto-manufacture/issues/`).

**Soldier From-Art visuals (D-056 Approach B, WA-01 coded) + full class-zone cover (D-057, WA-02 coded):** Extend `WarriorAppearancePrefabAssembler`: create Visual from Art when Prefab missing and Art ready; existing Prefabs keep layout-only fix. Refresh Defend/UM catalogs via `CloneApp02FallbackAppearances.RefreshWarriorCatalogBindings` (union Mode1+Mode2 `BodyAppearanceConfig` AppearanceIds that already have Prefabs); **do not** `GenerateAll`. `EnsureFormationClassZones` / menu "Ensure Formation Class Zones on Maps" uses **Mode2** `Manufacture_ClassConfig` as authoritative ClassId list (CSV; add missing / remove orphans; second front/back + `_0` offsets: §13; existing zones keep world XZ; sample HalfExtents locked to `(3.85, 2)`; parent/child localRotation=identity, IsoTileYaw dropped). Issues: `.scratch/mode2-warrior-art-bind/`.

**AutoManufacture stage (rules locked; AM-03–13 closed + D-054/D-055 + Step2 per-slot MagicBook):** `GameplayType=AutoManufacture` → `AutoManufactureStageModule` + `AutoManufactureService` + `AutoFormationDeployService`; ignore `GameplayConfigId`; pick/class/base (**no MagicBook at craft**; default race) → grant `DefaultSkillIds` → StaticStat/appearance/name → `TempWarriorWarehouse` → **Clear `BattleFormation` first** → flush→`WarriorPool` → batch record → (crafted>0) UI-016 Step2 pulse peak calls `SoldierManufactureMagicBookHook.ApplyEquippedBookAtSlot` (that book only; incl. `RaceWeightPick`/`StatMul`/`ForceClass`/`SoldierSkillLevelAdd`; `ForceClass` hit re-grants skills) → `RefinalizeInstance` → **then** deploy by final `PlacementOrder` into `FormationClassZone`; ignore Spirit/Control; no SoulId; presentation fail/`Exit` → `ApplyRemainingSlots`; **D-054** batch `Replace`; **D-055** advance after play + Mode2 UM `AutoOpenFormationOnce`; 0 craft Tips + skip presentation. UI-016 soldier row: Content side padding + per-index scroll; enter slides first card from one pitch right of viewport center; after reveal shift left one pitch (card width from actual `SoldierCardTemplate`). Card reveal (Approach B): off-screen Camera+RT; `Taunt` then loop default Idle; conveyor does not wait for Taunt. MetaShell Bind/Clear/Delete.

**Runtime pointer (UI-024, Approach A):** `PlayerSettings.defaultCursor` = `Assets/Art/UI/Cursor.png`, `cursorHotspot` at shovel tip; import rules in [§4](#4-跨平台输入占位与运行时光标); **no** runtime `Cursor.SetCursor`; Dig `UiDigCursorRing` overlays it.

**Meta shell (Approach A, D-001–D-004 + D-045; Level list Approach B / UI-008; Tools GM Approach A / UI-019 / D-061 + UI-020 / D-064; equipment warehouse / MagicBook popup Approach A / UI-022 / D-067 + UI-023 / D-068 / D-072):** Single scene `Assets/Scenes/Boot.unity`; SaveSelect / InSaveShell via Canvas Prefab show/hide (`Assets/Prefabs/Meta/`, `Assets/Prefabs/UI/`). Rules: `SaveSlotService` + `GameplayStateService` + `CampaignModeService`; Views subscribe only. Tools Settings → **opens TechTree canvas** (UI-012 below); Tools Level → Prefab **`LevelSelectPanel`** under InSaveShell (built by `MetaShellAssetBuilder`): `ConfigCsvRepository.GetDistinctLevelIds()` lists distinct `LevelId` from loaded current-mode `Level_LevelOperationConfig` (first-seen order); pick → `LevelOperationDriver.TryEnterLevel(levelId)` (from StageNumber=1); close hides panel. **Demo GM (ToolsPanel):** Grant Protagonist Equipment / Grant MagicBook hide ToolsPanel → Prefab **`GmGrantListPanel`** (layout aligned with LevelSelectPanel): equipment = distinct EquipId from current-mode `ProtagonistEquipmentConfig` (Level 1 DisplayName); pick row → nested LevelPicker (all EquipLevel buttons) → `ProtagonistEquipmentService.DebugGrantAtLevel` (not owned → add at level; owned → overwrite Level and CurrentExp=0); Dig HUD still `TryAcquire`; MagicBook = full `MagicBookConfig` → `SpecialEquipSlotsService.TryEquip` (no warehouse; unique already equipped / slots full fail); Toast + log; list stays open. Add Soldier hide ToolsPanel → left Prefab **`GmAddSoldierPanel`** only when UM Formation is open (class/race dropdowns, count 1–999, auto-deploy default on); `GmSoldierGrantService` requires BodyAppearance RaceId+ClassAffinity(ClassName) else Tips「找不到此种士兵！」; multi-match deterministic pick (`DefaultAppearanceId` in set > `AppearanceLevel==ClassLevel` > first table order; no random); on match Demo fixed `BaseStats` (MaxHP=100, MoveSpeed=3, Str/Agi/Int=20) into pool + optional `DeployBatch`; Dig HUD GM **kept**. **InSaveShell Equipment / MagicBook (Approach A / D-067 / D-068):** above `BackButton`, vertical Equipment / MagicBook (160×48, gap 8); click opens centered Prefab **`EquipmentWarehousePanel`** / **`MagicBookSlotsPanel`** (align LevelSelectPanel: full-screen dim + box + Title + Close; `sortingOrder` ≥ 100). Warehouse read-only: `EquipmentWarehousePanelView` scrolls `OwnedEquips` (current-level `DisplayName`/`EquipId` + `Lv.{Level}` + `Description` + `IconAssetId`→`Resources.Load<Sprite>`); empty 「尚未拥有装备」; subscribes `Changed`; Service+Configs injected on enter-save. Menu `Ensure EquipmentWarehouseList (UI-022)` patches the list. MagicBook nests shared `Assets/Prefabs/AutoManufacture/BookRow.prefab` (same as UI-016 presentation); `SpecialEquipSlotsService.TrySwap` any two slots (incl. empty) persists immediately + `Changed`; `MagicBookSlotsPanelView` click occupied slot → floating `DeleteButton` under slot, confirm via `ConfirmDialogView` (`sortingOrder` 110) then `TryUnequip` (D-072); presentation BookRow subscribes `Changed`. Menu `Ensure InSaveEquipMagicBookPanels (UI-022/023)`. Menu `Ensure MagicBook BookRow (UI-023)` and `Gravedigger2026/AutoManufacture/Nest BookRow into Presentation Root` nest shared `BookRow.prefab`. Menus `Ensure GmGrantListPanel (UI-019)` / `Ensure GmAddSoldierPanel (UI-020)`. **Layout:** `InSaveShellPanel` `StateLabel` / `StageInfoLabel` top-center (top-middle anchors; `StateLabel` y≈−16, `StageInfoLabel` y≈−52) so they do not cover mid-screen combat. Formal shell three-state switch still **TBD**; Demo temp **Debug cycle** on InSaveShell for hand-checking D-004 (must not equal Tools Level); **Debug advance stage** for D-010 (placeholder end → next / VictorySettlement).

**Level driver (Approach A, D-010 + D-075):** `ConfigCsvRepository` reads CSV only (paths: [§14.5](#145-runtime-csv-load-paths-demo)); `LevelOperationDriver` loads rows by `LevelId`, runs ascending `StageNumber`; sets `GameplayState` and calls `IStageModule` enter/leave hooks (Shop / Dig / UM / Defend / PushMap / AutoManufacture). When `GameplayType=Shop` / `UpgradeManufacture` / `AutoManufacture`, **ignore** `GameplayConfigId` (no Dig/Defend lookup). Mode2 sample: Shop → Dig → AutoManufacture → UM → PushMap. Dig/Defend rows validate `DigMapId` / `BattleMapId` ∈ `Ground_01`…`Ground_05` and resolve `Assets/Prefabs/Maps/{Id}.prefab`. UI/log must show LevelId, StageNumber, GameplayType.

**Dig vertical (Approach A, D-020):** `DigStageModule` (`IStageModule`) on Enter instantiates `Assets/Prefabs/Maps/{DigMapId}.prefab` and mounts `DigStageRoot` (`Assets/Prefabs/Dig/`). Rules: pure-C# `DigSessionService` owns effective-duration countdown, initial/process spawn, DigAction dwell + busy lock, damage, Warehouse/Spirit credit, stage reward aggregate; `DigProtagonistCapabilities` injected from **TechTree + Dig-domain protagonist gear** recalc (see UI-012 / §3.16 / PE-03). DigAction candidates: cursor circle ∩ grave `DigHitShape` local-XZ convex hull (world-transformed; broadphase `BoundingRadius`); no hull → fall back to obstacle circle. Presentation: `DigPrefabCatalog` binds Digger / `Grave_{QualityId}` (must cover all current-mode quality ids; Sprite from `Art/Dig/Graves/Grave_{QualityId}/`) / map variants / `UiDigCursorRing`; circle cursor (Prefab dual-layer, fixed-pixel stroke), grave HP styles, DigReward fly-to HUD portrait (no map Digger), DigStageSummary via Views. Duration 0 → cancel in-progress DigAction (no damage) → DigStageSummary confirm → `LevelOperationDriver.TryAdvanceStage`. **Demo GM (Dig HUD):** `DigHudView` top-right "Add Graves" / "Add Body Parts" → `DigStageController` → `DigSessionService.DebugSpawnGraves(10)` (reuse weighted/`TrySpawnOneGrave`) and `DebugGrantAllBodyParts(10)` (iterate `configs.BodyParts` → `Warehouse.AddItem`, no AutoConvert, fire `WarehouseChanged`); not counted in DigStageSummary. Mode2 also "Equip Warrior Enhance" → `SpecialEquipSlotsService.TryEquip("MagicBook_WarriorEnhance")` (D-058 hand-check). Protagonist gear hand-check (D-059): "Grant Iron Shovel" → `ProtagonistEquipmentService.TryAcquire("Equip_IronShovel")`; "Equip Common Exp +50" → `DebugGrantCommonExp(50)`; "Spend Into Iron Shovel" → `TrySpendCommonExp("Equip_IronShovel", 1)`; log Level / CurrentExp / merged `DigCursorRadius`. Miner Lamp hand-check (D-060): "Grant Miner Lamp" → `TryAcquire("Equip_MinerLamp")`; "Spend Into Miner Lamp" → `TrySpendCommonExp("Equip_MinerLamp", 1)`; log Q4/Q5/Q6 `GraveSpawnWeightBonus`. Prefab / `DigAssetBuilder` wires buttons. Do not runtime-reference `SmallScaleInt/`; rules must not read Sprite/pixels.

**TechTree canvas (Approach A, UI-012 optional):** `ConfigCsvRepository` additionally loads `Tech_TechTreeConfig.csv` / `Tech_TechEffectConfig.csv`. Rules: pure-C# `TechTreeService` (save-scoped on Meta shell) holds learned set + `UnlockedFeatureSystems`; on enter-save/`Reset`, auto-learns `InitiallyUnlocked` and applies effects; learn gate = not learned ∧ TechPoint≥LearnCost ∧ ≥1 learned prerequisite (inverse of `UnlockNextTechIds`); on learn spend → mark → parse additive `AttributeModifiers` **plus** owned Dig-domain `EquipEffect` → write `DigProtagonistCapabilities` (Demo keeps `DiggableQualityIds` = all grave qualities for Dig hand-check). Presentation: temp `Assets/Prefabs/Meta/TechTreeCanvasRoot.prefab` (node positions on Prefab; uGUI LMB-drag pan; hover name+effect desc; edges from forward ids; three-state frame colors); Tools Settings opens canvas; Debug can inject TechPoints. Do not runtime-reference `SmallScaleInt/`.

**UM upgrade panel (Approach A, D-030; Mode2 diff Approach C / D-053 + D-054):** `UpgradeManufactureStageModule` on Enter instantiates by `CampaignMode` either `Assets/Prefabs/UpgradeManufacture/UpgradeManufactureStageRoot.prefab` (Mode1: **full-screen manufacture by default**) or `UpgradeManufactureStageRoot_Mode2.prefab` (Mode2: ManufactureZone off; keep GM Upgrade Modal / Complete / Formation / **Manufacture Record**). Upgrade as Modal via top "GM Upgrade", close with top-right "X"; formation via Formation button → shared editor; Mode2 "Manufacture Record" to the right of Formation opens read-only batch Modal (`UmAssetBuilder.BuildAndSaveMode2StageRoot` adds it; Mode1 fields may be null). `ConfigCsvRepository` loads `Manufacture_ProtagonistLevelConfig.csv`. Rules: pure-C# `ProtagonistProgressService` holds in-memory `Level` / `LifetimeExperience` / `TechPoints` / effective `ControlPowerCap` / `ProtagonistMaxHP`; cumulative-threshold chain level-ups apply row rewards/caps. Debug inject remains; formal Defend victory credit in D-043. Bottom Complete → `TryAdvanceStage`. Do not runtime-reference `SmallScaleInt/`.

**UM manufacture panel (Approach A, D-031 → UI redo / pool remake):** `ConfigCsvRepository` additionally loads `Manufacture_SoulConfig` / `ClassConfig` / `GemConfig` / `RaceConfig` / `BodyPartConfig` / `BodyAppearanceConfig` / `ExtraEquipmentConfig` / `GemSuffixNameConfig`. Rules: pure-C# `ManufactureService` owns the 15 strict slots (Head1/Torso1/Arm2/Leg2/Soul1/Gem6/Mount1/Wing1), routes an Id to a legal empty slot by resolving its source table, and rejects on type mismatch / duplicate `GemType` / insufficient stock; every slot change recomputes the preview (`Base(S)=Σ StatBonus`, `Equip`, `GemMult`, `RaceAdjust`, `StaticStat`, static `MaxHP=ceil(BodyLife+StaticStat(Str)×MaxHpStrengthMult)`, `TotalSpiritCost`, `ControlPowerCost`, trial Race + Appearance). Manufacture gate = min parts (Torso+2Arm+2Leg; **Soul optional**) and `SpiritEssence ≥ TotalSpiritCost` (Head/Soul/gems/Mount/Wing optional for **commit**). Empty Soul slot: instance `SoulId=Soul_00`, soul-side costs/AttackMode from `Soul_00`, **force** `ClassId=Class_Servants` (no warehouse Soul consume). On success write `WarriorInstance.SourceItemIds` + `SourceSpiritCost`; `TryRemanufacture(sourceWarriorId)` validates/consumes from recipe in background, re-runs aggregate + Race/Appearance roll, `_pool.Add` **new** instance (does not mutate `_slots`); material/Spirit shortage error codes feed Tips. **Visual BodyAppearance gate** (presentation): Head+Torso+Arm×2+Leg×2+Mount+Wing filled (**Soul and gems do not gate**) → Instantiate trial `AppearanceId` Prefab and drive `WarriorAnimView` attack-then-idle; else static placeholder. `WarehouseService` / Debug kit unchanged. Presentation: `ManufacturePanelView` — PreviewPanel left, center slot ring (left: Head/Arm1/Leg1/Wing; right: Torso/Arm2/Leg2/Mount; Soul inside preview bottom; half-size gems×6 below), PoolPanel right as **ScrollRect soldier-frame list** (`PoolSoldierFrameView`; selected shows「Remake×1」), upper-center Tips on UmCanvas (1s:「材料不足」/「精魂不足」), bottom inventory square bar + Input drag into slots, three action buttons under inventory. Layout authority: `UmAssetBuilder` rebuilds StageRoot Prefab. Appearance: `UpgradeManufacturePrefabCatalog` binds `AppearanceId → Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`. Do not runtime-reference `SmallScaleInt/`.

**UM formation panel (Approach A→drag editor, D-032; Mode2 diff Approach C / D-053):** Pure-C# `BattleFormationService` (save-scoped, `BindSlot` + PlayerPrefs JSON) holds `{WarriorId, PositionX/Z, RemainingHP}`; `TryDeployAt` / `TrySetPosition` / `TryUndeploy`; ControlPower = Σ `ControlPowerCost` vs `ControlPowerCap` (Mode2 soldier Cost always 0; **no** ControlPower deploy gate). Presentation: shared Prefab `Assets/Prefabs/Formation/FormationEditorRoot.prefab` (Mode1: 80×80 soldier bar scroll, drag deploy/reposition/undeploy, top-left ControlPower HUD, UM Return / Defend StartBattle); Mode2 uses `FormationEditorRoot_Mode2.prefab` (ControlPower HUD off; soldier-bar cell size authored on `SoldierSlotTemplate` RectTransform, Demo **125×180**; `FormationSoldierBarView` reads template width/height at runtime and must not hardcode 80×80; `CompleteButton` above `SoldierBar` on the right, visible in UM and Prepare; `StartBattleButton` stacked directly above Complete for Prepare; `FormationEditorController.CompleteRequested`; UM host closes editor then same stage end as main Complete; Catalog `ResolveEditorRoot`). Soldier-bar cell text: upper line `ClassName` via `ConfigCsvRepository.TryGetClass` (missing row → fallback `ClassId`), lower line `Lv.{ClassLevel}` (missing row → 0); does **not** show `WarriorId`. **Mode2 hover tooltip (UI-021 / D-065 / D-070):** `FormationCanvas/SoldierHoverTooltip` (`FormationSoldierHoverTooltipView`); bar uses existing Input `FindSlotAt` hover (slot `raycastTarget` stays off); occupied cell → ClassName / `{n}级` / race DisplayNameKey / BaseClass / PromoteClass / static MaxHP + three dims (「(主属性)」on PrimaryStat row) / `SoldierSkills` icon+name; icon `Resources.Load("UI/Skills/"+SkillId)`, do **not** use `IconAssetId`; each skill `Icon` top-right 5×5 `EffectStatus` (last child) green/red from `SkillConfig.EffectImplemented`; hide on scroll/lift/leave/`End`; `CanvasGroup.blocksRaycasts=false`; Mode1 Prefab has **no** tooltip. UM main Complete + Formation opens editor; map = next Defend `BattleMapId` in Level (fallback `Ground_01`). Shared resolve with Defend / PushMap Prepare. Do not runtime-reference `SmallScaleInt/`.

**Battle ModeSelect (Approach A, D-044):** `DefendStageModule` Enter first instantiates `Assets/Prefabs/Defend/BattleModeSelectRoot.prefab` (or runtime-equivalent UI); `DefendPhase=ModeSelect`; Mode1「保卫战」lists all `DefendGameplayConfig`; LevelOperation `GameplayConfigId` = Recommended default highlight; on confirm overwrite `LevelStageContext.DefendConfig` then enter existing Prepare. Mode2「推图战」lists all `PushMapGameplayConfig`; on confirm call `LevelOperationDriver.TryHandoffModeSelectToPushMap(configId)`: `Exit` current Defend module → keep `LevelId`/`StageNumber`, rewrite `GameplayType=PushMap` + `PushMapConfig` + map path → `SetState(PushMap)` → `PushMapStageModule.Enter` → `StageChanged`. Do not runtime-reference `SmallScaleInt/`.

**Defend Prepare / StartBattle / Shield (Approach A→shared editor, D-040):** After ModeSelect confirms Mode1, `DefendStageModule` instantiates `DefendStageRoot` + `Prefabs/Maps/{BattleMapId}`; Prepare hosts same `FormationEditorRoot` UI on that map (no second map); StartBattle ≥1 → destroy preview then formal deploy; Shield/countdown unchanged. Do not runtime-reference `SmallScaleInt/`.

**Defend spawn + path (Approach A, D-041):** `ConfigCsvRepository` additionally loads `Defend_WaveSpawnConfig.csv` / `Defend_MonsterConfig.csv`. On StartBattle, `DefendSessionService` loads rows for `WaveConfigId`; in `Combat`, whenever `RemainingCombatSeconds` becomes a whole second (including StartBattle instant), fires unfired rows with matching `SpawnRemainingSeconds` (`SpawnOrder` ascending within the same second) via events to View. Demo-min spawn: fixed `DefendSpawnPointSet` on map Prefab (`ClockDirection`→clock markers; `RegionRandom`→pool pick; Inside/Outside both use fixed points this slice; exact OutsideMap **deferred**). After map instantiate, runtime-bake a minimal walkable NavMesh covering activity area + spawn points. Monster Prefabs: `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab` (§15.2 `Visual` when Art ready; else temp cubes; Catalog-bound). `MonsterAgentView` (`NavMeshAgent`) picks destination by `TargetSelect` (warrior transforms are PreferWarrior/Nearest candidates; fall back to protagonist), repaths on `TargetRetargetIntervalSeconds`; when in `AttackRange`, normal-attacks at `AttackSpeed` → `Shield -= 1` (ignore AttackPower). `Shield ≤ 0` → `DefendPhase.Ended` + LevelFailure hook (log; full Level abort in D-043). Soldier attacks / clear-spawn victory **out of scope**. Do not runtime-reference `SmallScaleInt/`.

**Defend warrior melee (Approach A, D-042 melee slice + MP-06):** `WarriorCombatMath` derives `NormalAttackPower` / `AttackSpeed` from `ClassConfig.PrimaryStat` + `CombatConvertCoeffs` (missing keys → **`CombatConstantConfig`**). `DefendSessionService` registers warrior HP at StartBattle (`MaxHP=ceil(BodyLife+StaticStat(Str)×MaxHpStrengthMult)`, RemainingHP clamped) and monster HP on spawn; rules confirm melee `HitConfirm` (windup end + target alive + in `AttackRange`) → monster `HP -= NormalAttackPower`; monster→warrior uses `AttackPower` directly (no armor). `HP≤0` without gems → `CombatDead` (stop acting); with gems → immediate PermanentDeath mark (material fate in D-043). Presentation: `WarriorAgentView` picks nearest living monster inside EngageZone; chase via `GoalKind=AttackSlot` (`AttackSlotService`+`MassMoveScheduler` Move); when none, loyal soldiers use `GoalKind=FormationHome` (keep retargeting; abort return on new target); `AttackMode=Melee` uses windup; `WarriorAnimView` plays move/attack/death (§15.5). Clear condition (all wave rows fired + all spawned monsters dead) → `ClearVictoryConditionDetected` event/log (**no** Exp credit, **no** victory Ended; see D-043). Do not runtime-reference `SmallScaleInt/`.

**Defend warrior ranged projectile (Approach A, D-042 ranged / 05c2):** StartBattle registration also stores `ClassConfig.RangedProjectileSpeed` / `RangedTimeoutSeconds`. `AttackMode=Ranged` shares EngageZone nearest targeting, `AttackSpeed` cadence, and no-target return to `FormationHome` with melee; when in `AttackRange`, Instantiate temp `Assets/Prefabs/Defend/Projectile.prefab` (Catalog-bound); fire also triggers `WarriorAnimView` attack. `ProjectileView` flies kinematically toward locked monster RuntimeId: **distance ≤ hitRadius** = collision hit → Session `TryConfirmRangedHit` → monster `HP -= NormalAttackPower`; **timeout** destroys with no damage. Mage/Archer share the same ranged channel (`PrimaryStat` only differs). Do not runtime-reference `SmallScaleInt/`.

**Defend LossOfControl StartBattle roll + win/lose settle (Approach A, D-043):** `ConfigCsvRepository` loads `Combat_LossOfControlConfig.csv`. At StartBattle lock Degree/Tier from formation `ΣCost/Cap−1` (overflow does not block StartBattle); when `Degree>0`, each deployed soldier rolls once with `FinalLossChance=clamp(0,1,TierChance+RaceBonus+ΣGemBonus+ΣSkillBonus)` → `IsRebel` (logged). Demo `ΣSkillBonus` = sum of `SkillConfig.LossOfControlChanceBonus` over instance `SoldierSkills` at baked level (none → 0; Soul/Gem/ExtraEquipment `Skills` remain TBD and are not added this Demo). Rebels **ignore EngageZone**, pick nearest living protagonist / other soldiers / enemies; normal hit on protagonist → `Shield-=1`; hits on soldiers/monsters use soldier attack channel. Clear condition → `DefendPhase.Ended` + minimal PermanentDeath (gems→warehouse, clear formation, remove pool) → `ProtagonistProgressService.AddExperience(100)` (Demo fixed stage Exp) → `LevelOperationDriver.TryAdvanceStage`. `Shield≤0` → Ended + same PermanentDeath settle → **no** stage Exp → `AbortLevelAsFailure` (no VictorySettlement; keep already-owned). Do not runtime-reference `SmallScaleInt/`.

**PushMap config load (Approach A, PM-02):** `ConfigCsvRepository` additionally loads `PushMap_PushMapGameplayConfig.csv` / `PushMap_PushMapSpawnConfig.csv`; `Defend_MonsterConfig` parses `AggroMode` / `AlertRadius` (defaults §9.19). Sample ≥1 `GameplayConfigId` + spawn rows (non-trap / trap / BOSS). StageModule / AI / Capture **deferred** (PM-03+). Do not runtime-reference `SmallScaleInt/`.

**PushMap stage wiring (Approach A, PM-03 + D-044 Mode2):** `LevelOperationDriver.TryBuildContext` supports `GameplayType=PushMap`: `GameplayConfigId` → `PushMapGameplayConfig` PK (direct lookup), `LevelStageContext.PushMapConfig`, and `MapPrefabPaths` accepts `PushMap_*` (same `Assets/Prefabs/Maps/{MapId}.prefab` resolve). `PushMapStageModule` (`IStageModule`; no in-stage ModeSelect): `GameplayType=PushMap` enters `PushMapPhase=Prepare` directly; also reachable from Defend `BattleModeSelect` Mode2 via `TryHandoffModeSelectToPushMap`. Thin rule `PushMapSessionService` (**separate but semantically aligned with §3.12**: Prepare→Combat; StartBattle ≥1 deployed; `Shield=ProtagonistMaxHP`; `Shield≤0`→LevelFailure; locks Degree/Tier at StartBattle and rolls each deployed soldier's `FinalLossChance`→Rebel, log-observable) + `PushMapStageController` (instantiates `Maps/{MapId}`, reuses shared `FormationEditorRoot` on the same BattleFormation). **Combat camera:** runtime Ensure child `PushMapCamera` (same top-down contract as Defend: orthographic, `Euler(90,0,0)`, height/near/far/StartBattle Size ← **`CombatConstantConfig`** (sample height `18`, Size `2`; unlike Defend `max(halfExtents)−CameraOrthoSizeMargin`); SolidColor/`depth=5`); disable in Prepare (FormationCamera), enable+repose on StartBattle; must not fall back to Boot perspective Main Camera. **PM-09 camera follow (Approach B / v0.81.0):** `PushMapCameraFollowController` on `PushMapCamera`; Combat `CameraFollowMode=Auto|Manual` — Auto follows max projection `s∈[0,1]` of living loyal soldiers onto map `CameraFollowPath` (look-at is a rail point, not a soldier; lead drop → SmoothDamp retreat, no Snap; freeze when none; missing/empty path warns and falls back to closest loyal to CurrentObjective); `EnterAuto`/combat enable Snap to the current rail point; LMB drag (not over UI) → Manual with ortho pixel→world XZ pan; StageRoot runtime Ensure bottom-center ResumeFollow button (anchor ≈`(0.5,0.1)`, Manual-only) → `EnterAuto`; scroll-wheel zooms Size (`mouseScrollDelta.y>0` zoom-in smaller; step `0.5`/notch; clamp `[0.5,20]`; skip when pointer over UI); zoom does not switch mode; ResumeFollow does not reset Size; height/rotation unchanged; rules layer not involved. Sample: `Level_01,5,PushMap,PushMap_01` (direct) and Defend Mode2 pick `PushMap_01` (handoff) both enter Prepare. Do not runtime-reference `SmallScaleInt/`.

**PushMap spawn & trap (Approach A, PM-05):** `PushMapSessionService` loads `PushMapSpawnConfig` rows at StartBattle; View order **Bake NavMesh → deploy → `FireStartBattleSpawns`**: non-trap rows with uncaptured linked objective → `PushMapSpawnRequested` (position resolved by View); trap-bound points → `TryNotifyTrapEnter` first-enter; `ObjectiveCaptured` → linked points stop spawning (living kept). `PushMapStageController` collects `SpawnPoint`/`TrapZone`, instantiates monsters (incl. Boss) into `_monsters` (`PushMapMonsterAgentView`), and polls loyal soldiers for first trap entry. Monster AI uses Defend default-chase semantics; protagonist shield via `PushMapSessionService.ApplyShieldHit`. AggroMode four-state lands in **PM-06**; Boss-clear settlement deferred (PM-07); **no** `WaveSpawnConfig` countdown. Runtime contract: §9.23. Sample `TrapZoneId` aligned to `TZ_01`. Do not runtime-reference `SmallScaleInt/`.

**PushMap monster footprint spread (Approach A, PM-10; tightened v0.73.9):** `MonsterConfig.BodyRadius` (default `0.35`); View staggers same-point / nearby living footprints via ring/spiral + `NavMesh.SamplePosition`. **Tighten:** local sample only ≈`max(0.75, BodyRadius×2.5)`; accept hit only within leash from spawn `basePos` (≈ ring/spiral radius + slack; absolute cap ≈`max(3, BodyRadius×10)`); on failure shrink/spiral then allow overlap at base — **forbid** large-radius snaps across AirWalls onto outer diamond. `PushMapMonsterAgentView` Bind Warp uses the same local radius (≈`max(1, BodyRadius×3)`, not 12). `NavMeshAgent.radius = min(BodyRadius, max(0.05, AttackRange − 0.1 − 0.05))`; `Stationary*` rely on spawn placement; Defend spawn spread deferred. **Loyal soldiers** Demo: `NavMeshAgent.radius=0.1`, `height=0.1`. Do not runtime-reference `SmallScaleInt/`.

**PushMap AggroMode four-state (Approach A, PM-06):** `PushMapMonsterAgentView` branches on `config.AggroMode`. `ActiveChase`: loyal soldier enters `AlertRadius` → **AttackSlot** chase that soldier until monster death (MP-05; not center `SetDestination`). `PassiveChase`: idle until `NotifyProvoked()`, then chase. `StationaryActive`: never moves; attacks loyal soldier inside `AttackRange`, stops on leave. `StationaryPassive`: never moves; attacks only after `NotifyProvoked()` and target still in `AttackRange`. Active detection + provocation are **loyal-only** (`!IsRebel`). Provocation Demo contract: `PushMapStageController` fires a loyal `PushMapAdvanceView`'s first entry into a passive monster's `AttackRange` → `NotifyProvoked()` (stands in for "soldier attacks first"; soldier HP / hit settlement still deferred). Hits keep `AttackMode` scheme D; active stances do not proactively detect the protagonist via `AlertRadius`, but an engaged protagonist hit still applies `ApplyShieldHit`. Real soldier damage on normal monsters / skill casts / dungeon gameplay body **not** done. Do not runtime-reference `SmallScaleInt/`.

**PushMap Boss clear & reward hooks (Approach A, PM-07 + UI-017/018):** `PushMapSessionService` tracks pending Boss count; `TryNotifyBossKilled` → 0 → `Ended` + `VictorySettled(StageExpReward)`; also tracks combat time, kill count, CaptureLoot display ledger; sync Rebel `IsRebel`; no living loyal → `RequestLevelFailure`. Presentation: `AddExperience` → **battle settlement panel** (not immediate `_onVictoryAdvance`); failure shows the same panel. Continue: fail → AbortLevel + LevelSelect; win → reward popup (Exp+CaptureLoot) → end Level + LevelSelect. Capture still credits `CaptureLoot` immediately. Do not runtime-reference `SmallScaleInt/`.

**PushMap AirWall NavMesh (Approach A, PM-08):** StartBattle runtime bake, in addition to the IsoDiamond walkable mesh, collects map `AirWall`s and injects `NavMeshBuildSourceShape.Box` + area=`Not Walkable` (size=`HalfExtents×2`; `Matrix4x4.TRS(position, rotation, 1)` → **incl. Y 45°**). Extends `DefendNavMeshBaker.Bake(..., notWalkableBoxes)`; `PushMapStageController` passes walls at StartBattle; both factions' `NavMeshAgent`s (soldier advance / monster chase) cannot path through. **No** `NavMeshObstacle` Carve or multi-layer obstacle polish. Contract: §9.22. Do not runtime-reference `SmallScaleInt/`.

**Mass combat pathing (Approach B, MassCombatPathing / SPEC locked):** shared-goal **FlowField** + chase **AttackSlot** + friendly **LocalDetour**; ~200/side; static AirWall/walkable mask into field; no friendly Carve. Impl slices: `.scratch/mass-pathing/issues/`; runtime contract §9.7. **MP-04:** PushMap loyal advance wired to FlowField+LocalDetour+`MassMoveScheduler`. **MP-05:** chase/engage wired to `AttackSlotService` (soldiers+monsters; slot refresh ≤50/frame; no all-units per-frame `CalculatePath`). **MP-06:** Defend `WarriorAgentView`/`MonsterAgentView` parity; loyal no Engage target→`GoalKind=FormationHome`; chase uses slots; same GoalKind semantics as PushMap. **MP-07:** Debug stress entry `MassPathingPerfStress` / `MassPathingPerfStressView` (~200+200 stubs + Stopwatch); over-budget fallbacks in §9.7. **Soldier task Debug label (Approach A):** during Combat, runtime TextMesh under `WarriorAgentView` / `PushMapAdvanceView` shows current `GoalKind` short ZH label; InSaveShell Debug toggle **default on**; goal-kind only. **AllyFootCircle:** loyal living soldiers green-stroke + black fill α160/255 (radius=`BodyRadius`, Order In Layer=`1`, localPos Y=-0.05 Z=-0.2, rotation X=-30); `WarriorAnimView` skips when batching sortingOrder. **CombatSkillIcon (UI-025 / D-071 / Approach A):** PushMap soldier-child `SpriteRenderer`; `worldSize = pixelSize × 2 × camera.orthographicSize / Screen.height`; overhead 35px / persist 20px; Prefab `Assets/Prefabs/PushMap/SkillIconHud.prefab` via `DefendPrefabCatalog`; rules events `SkillIconPopup(warriorId, skillId)` / `SkillPersistChanged(warriorId, skillId, on)`. Do not runtime-reference `SmallScaleInt/`.

**SkillEffect pipeline intent (D-073 / Approach B+):** pure C# `Assets/Scripts/Core/Combat/CombatStatusService.cs`, `SkillEffectPipeline.cs`, `SkillEffects/*Handler.cs` (namespace `Gravedigger2026.Core.Combat`). `PushMapSessionService` **only** calls `Dispatch(TriggerHook, context)` and `CombatStatusService.Tick` at existing settle points; **forbid** `if/switch` on `SkillId`. CombatSkillIcon still uses `SkillIconPopup` / `SkillPersistChanged`. Mode1 new columns may stay empty; Defend not wired. Issues `.scratch/soldier-skill-effects/`. **SE-07:** ranged hits `Dispatch(OnProjectileHit)`; `ProjectileView` is a **generic pierce channel** (keep current velocity after hit; `alreadyHitRuntimeIds` block repeats; Handler writes `ExtraHitsRemaining` / `DamageMul`; no projectile → no trigger). View must not branch on `SkillId`. **SE-09:** retarget moment `Dispatch(OnWarriorTargetAcquired)`; Handler supplies farthest enemy + behind landing; View local `SamplePosition`+`Warp`; AttackSlot / MassMove sync; failure does not start CD.

**Architecture note:** ToolsPanel is Meta shell UI; gameplay state owned by rules layer; View subscribes only (§13). Dig: rules owns spawn/timer/DigAction/busy/damage; diamond map, circle cursor, dig anims, DigReward fly-to HUD portrait are View; continuous placeable space. UM stages do not resolve mode-config PKs; upgrade progress is in-memory this slice. Defend: rules outputs target/destination; move service executes (mass stack §9.7); Demo-min walkable surface in §9.7 / SPEC_03 §3.12.

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

## 9. 配置表（关卡运作 / 挖坟 / 坟墓品质 / 材料 / 货币 / 挖坟能力 / 防守 / 刷怪波次 / 怪物 / 主角升级 / 灵魂 / 宝石 / 种族 / 制造部件 / 躯体外观 / 科技树 / 失控 / 士兵技能 / 推图战）

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
| 空有效列表 | 默认语义由各玩法写明。Dig / `GraveSpawnWeights`：过滤后为空 → **放弃该次生成**（见 [SPEC_03 §3.10](SPEC_03_GameRules.md)）。Dig / `GraveQualityConfig.DropMode=2`：过滤后为空 → **无掉落** |

#### 9.1 关卡运作表 `LevelOperationConfig`

**磁盘名：**
- **Excel：** `关卡_关卡运作表_Level_LevelOperationConfig.xlsx`
- **CSV：** `Level_LevelOperationConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| LevelId | 关卡ID | `string` 或 `int` | 同 ID 多行 = 该关全部阶段 |
| StageNumber | 阶段编号 | `int` | 同关卡内升序执行；建议同关卡内唯一 |
| GameplayType | 玩法类型 | `enum` / `string` | 如 `Shop` / `Dig` / `UpgradeManufacture` / `Defend` / `PushMap` / `AutoManufacture` |
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | **Dig** → `DigGameplayConfig` 主键；**Defend** → **RecommendedConfigId**（ModeSelect 默认高亮；见 [SPEC_03 §3.12](SPEC_03_GameRules.md) D-044）；**PushMap** → `PushMapGameplayConfig` 主键；**Shop** / **UpgradeManufacture** / **AutoManufacture** → **忽略**（可不空；运行时**不**查表、**不**解析为 Dig/Defend/PushMap 行；Shop 读 `ShopProgress` + §9.27/§9.28；UM 读全局表如 `ProtagonistLevelConfig` 等）。**不另开** `ShopGameplayConfig` / `UpgradeManufactureGameplayConfig`（见 [SPEC_03 §3.9](SPEC_03_GameRules.md)） |

```
LevelOperationConfig {
  LevelId: Id
  StageNumber: int
  GameplayType: Shop | Dig | UpgradeManufacture | Defend | PushMap | AutoManufacture | ...
  GameplayConfigId: Id   // ignored when GameplayType = Shop / UpgradeManufacture / AutoManufacture; PushMap → PushMapGameplayConfig
}
```

#### 9.2 挖坟配置表 `DigGameplayConfig`

**磁盘名：**
- **Excel：** `挖坟_挖坟配置表_Dig_DigGameplayConfig.xlsx`
- **CSV：** `Dig_DigGameplayConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | 主键；被关卡运作表引用 |
| DigMapId | 挖坟地图ID | `string` | Prefab 逻辑名（无路径、无扩展名）；合法值 **`Ground_01`…`Ground_05`**；运行时解析 → `Assets/Prefabs/Maps/{DigMapId}.prefab`；与 Defend `BattleMapId` 共用同一地面变体池；表现 = **Isometric Tilemap**（Tile/Sprite 在 `Assets/Art/Maps/Tiles/`，自 Example `Environment/Tiles`+`Sprites` 复制）；禁止运行时引用 `SmallScaleInt/`，见 [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角)） |
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
- 运行时有效权重 = 本字段 **+** `DigProtagonistCapabilities.GraveSpawnWeightBonus`（按 QualityId 加法；表中缺席视为 0 再加成；加成加到该 Id **首个**段，无则插入；再套加权通用规则）。每次抽取读**活** caps。
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

**落点：** 在 DigMap 整体可放置区域内采样；须避开 `DigObstacle`（**仅**未消除 Grave）的圆形障碍半径（半径在对应 Prefab 上配置）。单次生成采样失败最多重试 **32** 次，仍失败则放弃该次生成。

**Dig Prefab 约定：** `Assets/Prefabs/Dig/` 下各品质 Grave 预制体暴露圆形障碍半径（Dig 阶段不 Instantiate 地图 Digger；HUD 左上 60x60 头像框为 DigReward 飞向目标）（`DigObstacleRadius`）；每种 `QualityId` 对应专属 Grave Prefab。`SpriteRenderer` 源图固定为 `Assets/Art/Dig/Graves/Grave_{QualityId}/Grave_{QualityId}.png`；`DigPrefabCatalog` 须覆盖当前模式 `GraveQualityConfig` 全部 QualityId（Mode2 Demo 为 Q1–Q20）；`DigAssetBuilder` / HitShape baker 品质列表与表一致。Grave 根另挂 `DigHitShape`：本地 XZ 凸包顶点（≤12）+ `BoundingRadius`，由 Editor 菜单 `Gravedigger2026/Dig/Bake All Grave Hit Shapes` 离线烘焙（优先 `Sprite.GetPhysicsShape`，否则 alpha 扫边 → 凸包 → 简化）；换图后须重烘焙。规则层只读烘焙顶点，禁止运行时读 Sprite/像素。Digger 视觉为 Character Creator **烘焙整角**，固定 Prefab 逻辑名 `Digger` → `Assets/Prefabs/Dig/Digger.prefab`；美术导出源见 [§15](#15-角色美术管线character-creator-烘焙整角)。挖坟圆圈光标 UI：`UiDigCursorRing` → `Assets/Prefabs/Dig/UiDigCursorRing.prefab`（双层圆形：Stroke 外径 + Fill 内径差固定**屏幕**像素描边；Fill 白色半透明）；由 `DigPrefabCatalog` 绑定，`DigCursorView` 在 Dig HUD Canvas 下 Instantiate：先将 `DigCursorRadius` 投影为屏幕像素直径，再 ÷ `Canvas.scaleFactor` 写入 `sizeDelta`（Scale With Screen Size 下禁止把屏幕像素当 canvas 单位）；圆形 Sprite 源 `Assets/Art/UI/Dig/Ui_DigCursor_Circle.png`。Dig 地图：`DigMapId` → `Assets/Prefabs/Maps/{DigMapId}.prefab`。

#### 9.3 坟墓品质定义表 `GraveQualityConfig`

**磁盘名：**
- **Excel：** `挖坟_坟墓品质定义表_Dig_GraveQualityConfig.xlsx`
- **CSV：** `Dig_GraveQualityConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| QualityId | 坟墓品质ID | `string` 或 `int` | 主键；被 `GraveSpawnWeights` 引用 |
| MaxHP | 总血量 | `int` 或 `float` | 生成时初始化坟的 maxHP / 当前 HP；具体数值后续填表 |
| DropMode | 掉落模式 | `int` | 决定 `LootDrop` 中权重的用法；当前 **1** / **2**；后续可扩展 |
| LootDrop | 掉落内容 | 见编码 | 挖掘成功（HP=0）时的掉落池；须先按 `DropMode` 结算 |
| IconStyleHighId | 高血量图标ID | `string` | 剩余 HP% **>65%** 用；空 = 品质默认 Prefab/图 |
| IconStyleMidId | 中血量图标ID | `string` | 剩余 HP% **30%–65%** 用；空 = 默认 |
| IconStyleLowId | 低血量图标ID | `string` | 剩余 HP% **<30%** 用；空 = 默认 |

```
GraveQualityConfig {
  QualityId: Id
  MaxHP: number
  DropMode: 1 | 2 | ...      // currently 1 and 2; more modes later
  LootDrop: "Id;Weight;Count|Id;Weight;Count|..."
  IconStyleHighId: string     // empty = quality default
  IconStyleMidId: string
  IconStyleLowId: string
}
```

**与规则的关系（[SPEC_03 §3.10](SPEC_03_GameRules.md)）：**

- 生成坟时按 `QualityId` 读本表初始化 `GraveHP`。
- 扣血后按剩余 HP% 切换 `GraveIconStyle`（>65% / 30%–65% / <30%）；样式资源分别取自 `IconStyleHighId` / `IconStyleMidId` / `IconStyleLowId`，空则用品质默认 Prefab/图。
- HP 归 0 时：规则层按 `DropMode` 对 `LootDrop` **结算**，得到已确定的 `Id_Count` 列表；再生成 `DigReward` 图标飞向 Dig HUD 左上角头像框；到达后按已结算列表入账。结算为空 → 无奖励图标、无入账。

**`DropMode`（掉落模式）：**

| 值 | 语义 |
|----|------|
| **1** | 每段**独立**判定：`Random[0, 10000) < Weight` 则掉该段 `Count`。`Weight` 为**万分比**（如 9000 = 90%）。`Weight = 0` 永不掉；`Weight ≥ 10000` 必掉。 |
| **2** | 有效段（`Weight > 0`）按权重占比抽取 **恰好 1** 段，掉其 `Count`。`Weight = 0` 按上方 **加权字段通用规则** 剔除。空有效列表 → 无掉落。 |
| 其他 | 未实现模式：整次无掉落并打日志。后续可增加新模式。 |

**`LootDrop` 编码（本表固定）：** `Id;Weight;Count|Id;Weight;Count|...`

- 段分隔符：`|`；段内从右解析两段 `;`：最右为 `Count`，其左为 `Weight`，其余为 `Id`（`Id` 可含下划线，**不可**含 `;`）。
- `Weight`：非负整数。Mode **1** = 万分比；Mode **2** = 与同字段其他段合用的权重（例：A=40、B=60 → A 概率 `40/(40+60)`）。负值非法：忽略该段并打日志。
- `Count`：正整数（≥ 1）。
- `Id` 解析顺序（入账时，对**已结算**列表）：
  1. **保留精魂 Id** 字符串 **`Spirit`**（大小写敏感）→ 不入材料堆叠，直接加精魂。
  2. **`MaterialConfig.MaterialId`** → 普通材料；`AutoConvert` / UI 图标取自材料表。
  3. **`BodyPartConfig.BodyPartId`** → 躯体材料（与 `MaterialId` **同命名空间，Id 不得冲突**）；堆叠上限同 **10000**；`AutoConvert` 取自躯体表；仓库 / DigReward 外观图可用 `ArtAssetId`。
- 空串、不足两个 `;`、`Weight` 非法、`Count` 非正整数、上述皆未命中的 Id：**忽略该段并打日志**，继续解析其余段。
- 示例（Mode 1 必掉）：`Iron;10000;3|Spirit;10000;10|Bone;9000;1`
- 示例（Mode 2 抽 1）：`Iron;40;3|Bone;60;1`

**其它表更新：** `MonsterConfig.LootDrop` 与 `PushMapGameplayConfig.CaptureLoot` 一致使用 `Id;Count|Id;Count|…`（`LootDropParser.ParseIdSemicolonCount`）。其中 `Id` **必须先命中** [§9.5a 道具汇总表 `ItemCatalogConfig`](#95a-道具汇总表-itemcatalogconfig) 再分发到具体来源表。

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

#### 9.5a 道具汇总表 `ItemCatalogConfig`

规则语义：奖励系统的统一道具入口。一行 = 一个**可被产出奖励配置直接引用**的道具定义；运行时先查本表，再按 `ItemType` / `SourceTable` 分发到具体系统。**本轮首个接入字段：** `PushMapGameplayConfig.CaptureLoot`。

**磁盘名：**
- **Excel：** `通用_道具汇总表_Item_ItemCatalogConfig.xlsx`
- **CSV：** `Item_ItemCatalogConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| ItemId | 道具ID | `string` 或 `int` | 主键；奖励系统公共 Id；`CaptureLoot` 等奖励字段只认本列 |
| DisplayName | 道具名 | `string` | 道具公共展示名；若启用 i18n 可为 Key；空则 UI 回退 `ItemId` |
| IconAssetId | 道具图标 | `string` 或 `int` | 奖励弹窗/通用掉落 UI 使用的专属图标资源 Id |
| ItemType | 道具类型 | 见枚举 | 首版枚举：`Currency` \| `Material` \| `BodyPart` \| `MagicBook` \| `ProtagonistEquipment` |
| SourceTable | 管理道具属性配置表 | `string` | 该道具权威来源表名；运行时据此校验与分发；首版见下方登记表 |
| Description | 道具描述 | `string` | 通用描述文案；奖励展示优先取本列，不强依赖来源表 |
| SellPrice | 售卖价格 | `int` | ≥ 0；商店 UI-026 出售入账精魂（`ShopSellService`）；待售商品 `priceSpirit` 亦读本列（D-076） |

**`ItemType` / `SourceTable` 首版登记（权威）：**

| ItemType | SourceTable | 主键映射 / 约束 |
|----------|-------------|-----------------|
| `Currency` | `Dig_CurrencyConfig` | `ItemId == CurrencyConfig.CurrencyId`；至少支持 `Spirit` |
| `Material` | `Dig_MaterialConfig` | `ItemId == MaterialConfig.MaterialId` |
| `BodyPart` | `Manufacture_BodyPartConfig` | `ItemId == BodyPartConfig.BodyPartId` |
| `MagicBook` | `Manufacture_MagicBookConfig` | `ItemId == MagicBookConfig.MagicBookId` |
| `ProtagonistEquipment` | `Protagonist_ProtagonistEquipmentConfig` | `ItemId == EquipId`；奖励发放获得该装备 **1 件**，装备等级仍从该表 **Level 1** 起始；`EquipLevel` **不是** 奖励公共 Id 的组成部分 |

**运行时契约：**

- 奖励字符串（如 `CaptureLoot`）统一使用 `ItemId;Count|ItemId;Count|...`；其中 `Count` 为数量；`ItemId` 可含下划线，解析时按**最后一个分号**切分数量（`LootDropParser.ParseIdSemicolonCount`）。
- 运行时先查 `ItemCatalogConfig.ItemId`；未命中 → 忽略该段并打日志。
- 命中后按 `ItemType` 分发：
  - `Currency`：进入货币/精魂入账；
  - `Material` / `BodyPart`：进入仓库堆叠与 `AutoConvert`；
  - `MagicBook`：按现有魔法书获得语义发放；
  - `ProtagonistEquipment`：按现有主角装备获得语义发放（首次得装备，重复得转化经验）。
- `SourceTable` 既是策划约束也是加载期校验项：`ItemType` 与 `SourceTable` 不匹配、或来源表找不到对应主键 → 该行配置错误，运行时不得静默改判成别的道具类型。

```
ItemCatalogConfig {
  ItemId: Id
  DisplayName: string
  IconAssetId: Id
  ItemType: Currency | Material | BodyPart | MagicBook | ProtagonistEquipment
  SourceTable: "Dig_CurrencyConfig" | "Dig_MaterialConfig" | "Manufacture_BodyPartConfig" | "Manufacture_MagicBookConfig" | "Protagonist_ProtagonistEquipmentConfig"
  Description: string
  SellPrice: int
}
```

#### 9.6 挖坟主角能力（运行时派生；由科技 + 主角装备 Dig 效果重算）

科技学会与**主角装备仓库**中 `EffectDomain` 含 `Dig` 的当前等级行效果，共同写入存档主角的 `DigProtagonistCapabilities`（科技：[§9.16](#916-科技树配置表-techtreeconfig) / [§9.17](#917-科技项效果配置表-techeffectconfig)；装备：[§9.25](#925-主角装备配置表-protagonistequipmentconfig)；规则：[SPEC_03 §3.13](SPEC_03_GameRules.md) / [§3.16](SPEC_03_GameRules.md)）：

```
DigProtagonistCapabilities {
  DigDamage: number
  DigDurationReductionSum: number   // seconds; sum of unlock shorten effects
  DigCursorRadius: number
  DiggableQualityIds: set<QualityId>
  DigStageDurationBonus: number     // seconds; additive to LevelDurationSeconds
  GraveSpawnWeightBonus: map<QualityId, number>  // additive to GraveSpawnWeights; missing Id = 0
  DigProcessSpawnCountBonus: number // additive to SpawnRate M (process spawn only; not N; not InitialGraveCount)
}
// DigActionDuration = max(0.1, 0.8 - DigDurationReductionSum)
// EffectiveDigDuration = LevelDurationSeconds + DigStageDurationBonus
// Recalc = Σ learned TechEffectConfig.AttributeModifiers
//        + Σ owned ProtagonistEquipmentConfig.EquipEffect (Dig domain; current level row)
//        (additive per key)
// Effective spawn weights = GraveSpawnWeights + GraveSpawnWeightBonus (live caps each pick)
// Effective process spawn M = max(0, tableM + DigProcessSpawnCountBonus) (live caps each tick)
```

#### 9.7 防守配置表 `DefendGameplayConfig`

**磁盘名：**
- **Excel：** `防守_防守配置表_Defend_DefendGameplayConfig.xlsx`
- **CSV：** `Defend_DefendGameplayConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | 主键；被关卡运作表引用 |
| BattleMapId | 战斗地图ID | `string` | Prefab 逻辑名（无路径、无扩展名）；合法值 **`Ground_01`…`Ground_05`**（与 Dig `DigMapId` 共用地面变体池）；运行时解析 → `Assets/Prefabs/Maps/{BattleMapId}.prefab`（含 EngageZone + Isometric Tilemap 地面 + Demo `WalkSurface`/NavMesh 可走约定，见 [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角)） |
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

**寻路技术约定（与 [SPEC_03 §3.12](SPEC_03_GameRules.md) 配套；大规模栈方案 B）：**

- **方案 B（已锁定）：** 共享目的地用 **FlowField**；追击/攻击用 **AttackSlot** + **LocalDetour**；容量双方约 200。细则见下「大规模战斗寻路运行时契约」。
- 采用 Unity **NavMesh**（或可走掩码）表达 **静态** 可走空间与 `AirWall`；**不**要求 400 个单位各自持有高频全图 `CalculatePath`。
- **规则层**只输出：当前目标实体 ID + `GoalKind`（`Objective` / `FormationHome` / `AttackSlot` / `ChaseAnchor`）+ 可选 `AttackRange`；**不**直接写 `Transform` / `Animator`。
- **移动服务**解析 `DesiredDestination`：Objective→FlowField 采样（进入 `CaptureZone` / `ObjectiveArriveRadius` 后停跟中心、软分离守备）；攻击→AttackSlot 认领；友军阻挡→LocalDetour；表现层应用位移。
- 每隔 `TargetRetargetIntervalSeconds` 由规则层触发目标重选；槽位/场采样可按更短分帧预算更新，但 **禁止** 全员每帧全图重寻路。
- **EngageZone**：挂在 BattleMap **Prefab** 上的 **IsoDiamond（XZ 菱形）选敌区**（比地图稍小；策划调位置/尺寸）；非叛变士兵仅在此区内选最近敌人。见 [SPEC_03 §3.12](SPEC_03_GameRules.md) WarriorCombat。
- 士兵命中方案 D：按 `SoulConfig.AttackMode`（制造写入实例）走近战前摇或远程弹道；`AttackRange` / 前摇 / 弹速 / 超时取自 [§9.9b `ClassConfig`](#99b-职业配置表-classconfig)（规则层确认伤害；View 播动作/弹道）。**第一版 Demo**：士兵与怪物仅普通攻击，不施放技能。
- 障碍烘焙与 NavMesh 表面范围：**Demo 最小** — 按 `DigMapBounds` **IsoDiamond** 半尺寸 Runtime 烘焙可走面；PushMap `AirWall`：开战 Bake 追加 **Not Walkable Box**（§9.22 PM-08）。静态结果写入 FlowField 障碍/可走掩码。复杂障碍与精确 OutsideMap **后置**。
- **Demo 刷怪点最小：** 地图 Prefab 上临时固定出生点或 `InsideMap` 可走面随机；精确 OutsideMap 几何后置。

**大规模战斗寻路运行时契约（方案 B / MassCombatPathing）：**

- **权威行为：** [SPEC_03 §3.12](SPEC_03_GameRules.md)「大规模战斗寻路」；PushMap 推进见 [§3.14](SPEC_03_GameRules.md)
- **选定方案：** B — FlowField（共享目标）+ 局部追击 AttackSlot + LocalDetour；难度 3；切片 `.scratch/mass-pathing/issues/`
- **模块建议路径（实现时）：**
  - `Assets/Scripts/Core/Pathing/` — 纯 C#：`FlowFieldService`、`AttackSlotService`、`SpatialHash2D`、`LocalDetourSolver`、分帧 `MassMoveScheduler`
  - `Assets/Scripts/Gameplay/Pathing/` — 表现桥：从 StageController 注册单位、应用位移；逐步替换/包裹 `PushMapAdvanceView` / `WarriorAgentView` / `*MonsterAgentView` 的重 NavMeshAgent 路径
- **FlowField：**
  - 格点分辨率 Demo 建议世界单位边长 **0.25～0.5**（可配置常量）；覆盖 `DigMapBounds` IsoDiamond
  - 障碍：Bake 后不可走（含 AirWall）→ 场内 cost=∞ / 不可达
  - 目标：PushMap `CurrentObjective` 世界点（或 CaptureZone 中心）；`CurrentObjectiveChanged` / 开战 Bake 完成 → **重建一场**
  - 查询：单位位置 → 最近格 → 读归一化方向；同目标单位共享同一场缓冲
  - **到达守备：** `MassMoveScheduler.ObjectiveArriveRadius`（Stage 取当前 `CaptureZone.Radius`，缺省 2）内：`GoalKind=Objective` **不再**用 `SampleDir` 趋近目标格（场在目标格为 0 → 全队挤停）；改 desired=0 + LocalDetour 软分离；圈外跟场；`SampleDir≈0` 且仍在圈外 → 直趋 `GoalWorld` 回退
  - **禁止** 每单位独立 Dijkstra/A* 全图
- **AttackSlot：**
  - `slotPos = targetPos + rot(k * 2π/N) * ringRadius`；`ringRadius = max(0.05, AttackRange − slotMargin)`；`slotMargin` Demo 默认 `0.05`
  - `N`：近战默认 **12**，远程默认 **8**（常量；可后置配置表）。**同一目标上近战环与远程环是两张独立认领表**；远程认领不得因 `N` 不同而重建并丢弃近战认领（反之亦然）
  - 合法性：`IAttackSlotWalkable` 钩子（Demo stub；后接 `NavMesh.SamplePosition` 或可走掩码）；与目标占地圆不严重重叠
  - 认领表：`Dictionary<targetRuntimeId, SlotClaim[]>`；释放于死亡/换目标/超时
  - 重算触发：`TargetRetargetInterval`、目标位移 > `slotReclaimMoveThreshold`（Demo 默认 `0.5` 世界单位）
  - API：`TryClaim(attackerId, targetId, attackRange, targetPos, out worldPos, attackMode?, attackerPos?, targetBodyRadius?)`；`Release` / `ReleaseAllForTarget`
- **LocalDetour：**
  - 邻域：`SpatialHash2D` cell ≈ `0.5`；查询半径 ≈ `2 * agentRadius + 0.2`；热路径复用结果列表，禁止全表 O(n²) 互扫
  - 前方扇形阻挡 → 左/右短探测（长度 ≈ `1.0`）；选净空更大侧加切向偏置
  - 可选软分离（低强度）；`separationScale` 交战圈可降（对齐既有「防 RVO 挤抖」意图）；足迹完全重合时按 Id 确定性侧推，避免零向量死锁
  - **禁止** 友军 `NavMeshObstacle.Carve`
  - API：`Steer(desiredDir, self, neighbors, separationScale?)` → `steerDir`（`self` = 自身 XZ 位置 + 半径；无邻域时 `steer ≈ desired`）
- **性能预算（验收导向）：**
  - 存活可移动单位 ≤ **400** 时：移动逻辑主线程预算目标 **≤ ~2.5 ms/帧**（60 FPS 机；Debug 可打点）
  - 分帧：路径/槽位重算每帧处理 **≤ 50** 单位（轮转）；FlowField 重建不与全员槽位重算同帧叠满
  - 空间哈希邻域；禁止双层全表距离循环
  - **压测入口（MP-07）：** `Assets/Scripts/Core/Pathing/MassPathingPerfStress.cs`（纯 C# Stopwatch，双方各约 200）+ `Assets/Scripts/Gameplay/Pathing/MassPathingPerfStressView.cs`（简化胶囊/方块桩单位）+ Editor `Gravedigger2026/Pathing/Run MassPathing 200v200 Perf Stress`；测的是 `MassMoveScheduler.Tick` + ≤50 槽位刷新，**不含** Animator/全员 `CalculatePath`
  - **超预算回退（按序尝试）：** ① 增大 FlowField `cellSize`（向 0.5）；② 降低 AttackSlot `N`（近战/远程常量）；③ 降低 `MassMoveScheduler.MaxRecalcPerFrame` / 槽刷新预算（加大分帧，接受转向滞后）
- **与现网 Demo 过渡：** MP 切片落地前，现有单 Agent NavMesh 行为仍可运行；切片验收后推进/追击须切到本契约；不得回退为全员 HighQuality RVO 作为规模方案
- **士兵任务 Debug 标签（方案 A）：**
  - 路径：`Assets/Scripts/Gameplay/Pathing/WarriorTaskDebugLabelView.cs` + `WarriorTaskLabelSettings`（静态开关，**默认 `Enabled=false`**）
  - 表现：士兵脚下运行时 `TextMesh`（俯视可读，`Euler(90,0,0)`）；localPos `(0, 0.02, -0.38)`；`Font Size=12`（`characterSize=0.12`）；只读 `MassMoveScheduler.TryGetGoal` → 中文简标：`Objective`→推进、`FormationHome`→回阵、`AttackSlot`→追击、`ChaseAnchor`→追击锚
  - 接线：`WarriorAgentView`（Defend）与 `PushMapAdvanceView`（PushMap）在 `Bind` 时 Ensure 组件
  - 开关：进档壳 Debug 按钮切换 `WarriorTaskLabelSettings.Enabled`（可运行时克隆既有 Debug 按钮，缺省 Prefab 槽亦可）
  - **不做：** 攻击前摇/开火等细态；怪物标签；正式 UI Prefab / 本地化 Key
- **友军脚下圈 AllyFootCircle（v0.75.33）：**
  - 路径：`Assets/Scripts/Gameplay/Combat/AllyFootCircleView.cs`
  - 表现：localPos `(0,-0.05,-0.2)`；rotation X=**-30**；绿描边 + 内黑 α=**160/255**；半径=`BodyRadius`；Order In Layer=`1`；`WarriorAnimView` 批量改 sortingOrder/尸体变暗时跳过
  - 接线：`WarriorAgentView` / `PushMapAdvanceView` Bind；Rebel/CombatDead 隐藏
- **边界不做（本专题）：** 完整 ORCA；动态门/可破坏墙；多层楼寻路；技能位移预测

```
// MassCombatPathing touchpoints (Approach B)
FlowFieldService.Rebuild(goal, walkableMaskInclAirWall)
FlowFieldService.SampleDir(worldPos) -> Vector2
AttackSlotService.TryClaim(attackerId, targetId, attackRange, targetPos, …) -> worldPos
LocalDetourSolver.Steer(desiredDir, self, neighbors, separationScale?) -> steerDir
MassMoveScheduler.Tick(dt) // frame-budgeted
MassPathingPerfStress.Run(perSide≈200) // MP-07 Debug Stopwatch
```

- **刷怪 / 怪物表：** 见 §9.18 `WaveSpawnConfig`、§9.19 `MonsterConfig`；失控见 §9.20 `LossOfControlConfig`、§9.21 `SkillConfig`（士兵技能权威表；PushMap `Skill_03`/`Skill_01`/`Skill_02` 见 D-069；`Skill_04`～`Skill_12` EffectKind 见 D-073）；规则见 [SPEC_03 §3.12](SPEC_03_GameRules.md)。

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
| ClassId | 职业ID | `string` 或 `int` | 必填；FK → `ClassConfig`；有灵魂槽时写入士兵实例；无灵魂槽时实例强制 `Class_Servants`（本行 `ClassId` 仍须合法 FK） |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged`；士兵普攻命中方案 D 分支（§3.12）；与怪物 `AttackMode` 同枚举。示例：战士类→Melee、射手/法师类→Ranged（法师与射手同远程通道，仅 `ClassConfig.PrimaryStat` 不同） |
| Skills | 可使用技能 | 见编码 | 技能 Id + 等级列表；编码见下；失控加成与 CD 见 [§9.21 `SkillConfig`](#921-技能配置表-skillconfig)；并行列表本 Demo **不施放**（实例 `SoldierSkills` 见 D-069） |
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

**说明：** 原 `InfoTags` **不再**参与 WarriorInfo 主标签生成（主标签 = 定稿种族）。灵魂 **不**改写力量/敏捷/智力本身；有灵魂槽时通过 `ClassId` 注入职业；无灵魂槽时实例 `SoulId=Soul_00`（系统默认行）并强制 `ClassId=Class_Servants`，其余灵魂侧字段读 `Soul_00`。通过 `AttackMode` 选择近战/远程命中分支。`ClassName` / `PrimaryStat` / 换算系数见 [§9.9b `ClassConfig`](#99b-职业配置表-classconfig)。

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
  SoulId: Id                      // FK → SoulConfig; empty slot → Soul_00
  ClassId: Id                     // placed soul ClassId, else forced Class_Servants; FK → ClassConfig
  AttackMode: Melee | Ranged      // from effective SoulConfig (placed or Soul_00)
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
  EquipStats: { … five dims }     // manufacture-locked Equip layer flats
  BodyLife: number                // Base(MaxHP)+Equip(MaxHP); HP-dim exception
  SourceItemIds: Id[]             // non-empty slot ItemIds at manufacture (remake recipe)
  SourceSpiritCost: number        // Spirit paid at manufacture (remake gate)
  SoldierSkills: {                // class DefaultSkillIds @ Lv1; Mode2 may SoldierSkillLevelAdd
    SkillId: Id                   // FK → SkillConfig.SkillId
    SkillLevel: int               // baked; lookup SkillConfig (SkillId, SkillLevel)
  }[]
  VisualStyleId: Id | ""          // Mode2 AllIn1 preset; empty = Prefab default (§3.15 6b)
  VisualPriority: int             // last winning book priority; 0 if none
  VisualIntensity: number         // stack add of VisualIntensityAdd on winning style
  VisualModelScale: number        // scale channel; default 1; *= VisualIntensityAdd on Style_ScaleModel hit
}
```

**说明（存档）：** Demo 按槽将上述快照整段序列化进 `PlayerPrefs`（§6）；`NextSerial` 与池同键，保证再进档 Id 不冲突。`SoldierSkills` 经 `WarriorSaveDto.SoldierSkills`（`SoldierSkillEntry[]`，`[Serializable]` + **public 字段**，JsonUtility）与 `WarriorInstance` 往返；缺字段 / null / 空 → 空列表，不丢其它快照字段。`VisualStyleId` / `VisualPriority` / `VisualIntensity` / `VisualModelScale` 同键往返；旧档缺字段 → 空 style / 0 / 0 / **1**（Prefab 默认材质与原尺寸）。`RepairMissingStatSnapshots` 只补 StatBlock/配方相关字段，**不得**清空已有 `SoldierSkills`、`VisualStyle*` 或 `VisualModelScale`。彻底死亡删整实例，技能与特效外观随实例消失（不另迁技能）。Mode1 制造/再造：`ManufactureService.BuildWarriorFromAggregate` 在 `ResolveInstanceClassId` 之后调用 `SoldierSkillGrant.GrantDefaultSkillsAtLevel1`（Lv1；不读魔法书；VisualStyle 空；`VisualModelScale=1`）。Mode2 AutoManufacture：造兵时按双手 `ClassId` 授予 `DefaultSkillIds`；UI-016 Step2 单槽脉冲时 `ApplyEquippedBookAtSlot`（含 `SoldierSkillLevelAdd` / `ForceClass` 命中重授；**Token 命中**才 `TryApplyVisualStyle`：材质竞争或放大连乘）。`RefinalizeInstance` 重选 `AppearanceId`，**不得**清空 `VisualStyle*` / `VisualModelScale`。
**关联说明：**

- 职业表见 **§9.9b**；躯体材料 / 躯体外观 / 额外装备 / 宝石后缀表见 **§9.12–§9.15**。
- 静态层：`StaticStat(S) = max(0, Base+Equip+Base×GemMult+Base×RaceAdjust)`（不含 Buff）；战斗层：`FinalStat(S)` 另加 `Base×SkillBuff`（先定 `S` 再取来源；各维缺省 0；见 §3.11）。
- **生命维例外**：最终士兵 `MaxHP = ceil(BodyLife + Str×MaxHpStrengthMult)`（`MaxHpStrengthMult` ← `CombatConstantConfig`），`BodyLife = Base(MaxHP)+Equip(MaxHP)`；**不**用 `FinalStat(MaxHP)`（§3.11）。
- 战斗派生：`Primary` = `ClassId` → `ClassConfig.PrimaryStat` 对应维；`NormalAttackPower` / `AttackSpeed` / `SkillCooldown` 系数取自 `ClassConfig.CombatConvertCoeffs`（缺键回退 **`CombatConstantConfig`**）；`AttackRange` 等命中参数取自同表列。
- 多宝石：实例 `GemMult(S) = Σ` 已镶嵌各宝石的 `GemMult(S)`。
- 士兵 **彻底死亡（PermanentDeath）**：全部 `GemIds` 回仓库；躯体部位/灵魂/外置装备等绑定材料销毁；`SoldierSkills` 随实例删除（无可回收技能物品）；布阵位清空（见 §3.11）。战斗死亡（无宝石）不触发物资去向、**保留** `SoldierSkills`；带宝石士兵 HP≤0 立即彻底死亡。
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
| BaseClass | 基础职业 | `enum` / `string` | CSV 中文：`战士` \| `射手` \| `法师` \| `刺客` → 运行时枚举 `Warrior`/`Archer`/`Mage`/`Thief`（加载器仍接受旧值 `盗贼`→`Thief`）；空/缺列 → `Unspecified`；非法 → Warning + `Unspecified`。**预留**后续魔法书等触发条件；**不**参与命名/外观/`PrimaryStat`/战斗派生；**不**烘进士兵实例（经 `ClassId` 查表） |
| PromoteClass | 转职职业 | `string` 或空 | 可选文字；空/缺列 = 无转职目标。本轮仅填表并加载；**不**参与命名/外观/`PrimaryStat`/战斗派生；**不**烘进士兵实例；应用点 **TBD** |
| ClassLevel | 等级 | `int` | ≥ 0；**仅 UI 展示**（UI-016 士兵卡职业名下 `Lv.{ClassLevel}`）；**不**参与战斗/制造公式；缺/空 → `0` |
| PrimaryStat | 主属性 | `enum` / `string` | `Strength` \| `Agility` \| `Intelligence`；决定普攻 `NormalAttackPower` 所用属性维（§3.12）；示例语义战士→Strength、射手→Agility、法师→Intelligence（以本字段为准，非 ClassName 硬编码） |
| CombatConvertCoeffs | 战斗换算系数 | 见编码 | 将该职业士兵的五维 StaticStat/FinalStat 转为 `NormalAttackPower` / `AttackSpeed` / `SkillCooldown` 等战斗参数时的系数集；编码见下；缺键回退 **`CombatConstantConfig`** |
| AttackRange | 攻击距离 | `float` | 进入攻击态距离（世界单位或项目统一距离单位） |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0；秒；`AttackMode=Melee` 时用 |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0；`AttackMode=Ranged` 时用 |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0；秒；超时未命中 → 未命中 |
| BaseMoveSpeed | 基础移速 | `float` | ≥ 0；世界单位/秒；士兵 **MoveSpeed** 维的 `Base` 权威来源（§3.11）；缺/≤0 → **3.5** |
| ChaseMoveSpeedMult | 追击移速倍率 | `float` | ≥ 0；`GoalKind=AttackSlot` 时 × `FinalStat(MoveSpeed)`；缺省 **1**（§3.12） |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged`；**Mode2 自动制造**无灵魂时士兵普攻分支取本列（[SPEC_03 §3.15](SPEC_03_GameRules.md)）；Mode1 仍以 `SoulConfig.AttackMode` 为准 |
| PlacementOrder | 放置排序 | `int` | ≥ 1；Mode2 AutoManufacture 自动上阵时按此 **升序** 先后放置该职业；缺省视为很大（后置） |
| DefaultAppearanceId | 职业默认外观 | `string` 或空 | Mode2：B 空，或亡灵改写后 A 仍空时优先用本 Id（FK → `BodyAppearanceConfig`）；空则继续种族 `IsFallback` |
| DefaultSkillIds | 制造默认获得技能ID | 见编码 | 该职业制造完成时写入实例 `SoldierSkills` 的技能 Id 列表；空 = 无；编码见下；FK → `SkillConfig.SkillId` |

**`CombatConvertCoeffs` 编码（固定）：** `键_数值|键_数值|…`

| 键 | 缺键回退 | 公式角色（§3.12） |
|----|----------|-------------------|
| `NormalAttackPrimaryMult` | **`CombatConstantConfig`** 同名键（样例 **15**） | `NormalAttackPower = Primary × 本系数` |
| `AttackSpeedBase` | 常量表同名键（样例 **0.5**） | `AttackSpeed = 本系数 + AttackSpeedAgiDiv/max(Agi,1)` |
| `AttackSpeedAgiDiv` | 常量表同名键（样例 **60**） | 见上 |
| `SkillCdIntDiv` | 常量表同名键（样例 **30**） | `SkillCooldown = max(SkillCdFloor, BaseCooldownSeconds − 本系数/max(Int,1))` |
| `SkillCdFloor` | 常量表同名键（样例 **0.1**） | 见上 |

- 示例：`NormalAttackPrimaryMult_15|AttackSpeedBase_0.5|AttackSpeedAgiDiv_60|SkillCdIntDiv_30|SkillCdFloor_0.1`
- 空串 = 全部回退 **`CombatConstantConfig`**；非法段跳过并打日志；常量表缺键时实现可 Warning + 安全兜底（与样例同值）
- **不**含 `AttackRange` / 前摇 / 弹速（独立列）
- 全局默认权威见 [§9.20b `CombatConstantConfig`](#920b-战斗常量表-combatconstantconfig)（**不再**以 C# 字面量为业务主路径默认）

**`DefaultSkillIds` 编码（固定）：** 空 = 无默认技能；否则 `SkillId` 或 `SkillId|SkillId|…`（管道分隔，与 `ClassRestrict` 同符）。制造时每个 Id 写入 `{ SkillId, SkillLevel=1 }`；无 `(SkillId,1)` 行 → 跳过 + Warning；重复 Id 保留首次。Demo 预期 0 或 1 个。规则见 [SPEC_03 §3.11](SPEC_03_GameRules.md) / [§3.15](SPEC_03_GameRules.md)。

```
ClassConfig {
  ClassId: Id
  ClassName: string               // WarriorName + ClassAffinity match key
  BaseClass: Warrior | Archer | Mage | Thief | Unspecified  // CSV 中文四值；预留条件；不驱动本片玩法
  PromoteClass: string | ""       // optional text; empty = none; unused this slice; application TBD
  ClassLevel: int                 // display-only; UI-016 "Lv.N"; missing → 0
  PrimaryStat: Strength | Agility | Intelligence
  CombatConvertCoeffs: "Key_Value|..."  // missing key → CombatConstantConfig
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
  BaseMoveSpeed: number           // >=0; soldier MoveSpeed Base; missing/<=0 → 3.5
  ChaseMoveSpeedMult: number      // >=0; x MoveSpeed when GoalKind=AttackSlot; default 1
  AttackMode: Melee | Ranged      // Mode2 no-soul path
  PlacementOrder: int             // >=1; Mode2 auto-deploy order
  DefaultAppearanceId: Id | ""    // Mode2 appearance fallback
  DefaultSkillIds: "SkillId|..." | ""  // grant SoldierSkills @ Lv1 after ClassId final
}
```

**解析：**

- 制造时（Mode1）：有灵魂槽 → `SoulConfig.ClassId` → `WarriorInstance.ClassId`；无灵魂槽 → 强制 `Class_Servants`。命名与外观取实例 `ClassId` 对应行的 `ClassName`。`ClassId` 定稿后由共享 `SoldierSkillGrant` 按 `DefaultSkillIds` 授予 `SoldierSkills`（Lv1；无 `(SkillId,1)` 行 → 跳过 + Warning；重复 Id 保留首次；空列 → 空列表；Mode1 不读魔法书升技能）。制造与再造共用 `BuildWarriorFromAggregate`。
- 制造时（Mode2 AutoManufacture）：`ClassId` 由双手 `ClassRestrict` 定稿（§3.15）；`AttackMode` 取本表；**不写** `SoulId`。`ForceClass` 后再按**最终**职业 `DefaultSkillIds` 授予，然后二次扫描 `SoldierSkillLevelAdd`。
- 战斗派生：先查本表取 `PrimaryStat` 与 `CombatConvertCoeffs`（缺键回退常量表）；命中参数取本行 `AttackRange` 等列；**MoveSpeed** 维 `Base` 取 `BaseMoveSpeed`。**不**读 `ClassLevel` / `BaseClass` / `PromoteClass`。
- 开战登记：`CombatConvertCoeffs.Parse(职业串, repo.GetCombatConvertCoeffDefaults())`；`MaxHP` 用常量表 `MaxHpStrengthMult`。
- UI-016：士兵卡职业名下展示 `Lv.{ClassLevel}`。
- `BaseClass`：仅配置查询预留；魔法书等条件匹配 **TBD**。
- `PromoteClass`：仅配置查询预留；应用点 **TBD**。
- 职业列表与具体数值后续填表。哪些职业默认带哪条技能 **TBD**。

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
| Skills | 额外技能 | 见编码 | 额外一套技能（SkillId + 等级）；编码同 [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig)：`SkillId;Level\|…`；技能失控加成见 [§9.21](#921-技能配置表-skillconfig) |
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

**解析：** 制造时对已放入躯体部位（头/躯干/臂/腿）收集 `RaceId`：**全部相同** → 定稿该族；否则定稿 **`Race_Undead`**。Mode2 若装备 `EffectPayload=RaceWeightPick`（「还原」）则改为各部位权重 **1** 加权随机。查本表，将五维系数写入 `WarriorInstance.RaceAdjustCoeff`；按项代入 `Base(S) × RaceAdjust(S)`。失控判定时取本行 `LossOfControlChanceBonus`。

#### 9.12 躯体材料配置表 `BodyPartConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 制造槽位 / 种族加权 / BaseStats / 仓库入账。一行 = 一种躯体部位材料。亦可称 **躯体材料表**。

**磁盘名：**
- **Excel：** `制造_躯体材料配置表_Manufacture_BodyPartConfig.xlsx`
- **CSV：** `Manufacture_BodyPartConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| BodyPartId | 躯体ID | `string` 或 `int` | 主键；可被 `LootDrop` / 仓库引用；与 `MaterialConfig.MaterialId` **同命名空间，不得冲突** |
| DisplayName | 道具名称 | `string` | 仓库 / DigStageSummary 展示名；未启用 i18n 时直接当展示串；空则 UI 回退 `BodyPartId` |
| BodyLevel | 躯体等级 | `int` 或 `float` | ≥ 0；参与制造时外观平均等级 |
| BodySlot | 躯体部位 | `enum` | `Head` / `Torso` / `Arm` / `Leg`；决定可放入的制造槽 |
| RaceId | 种族 | `string` 或 `int` | FK → `RaceConfig`；参与加权定种族 |
| ControlPowerCost | 控制力占用值 | `int` 或 `float` | ≥ 0；计入制造 `BodyCost` |
| SpiritCost | 精魂消耗 | `int` 或 `float` | ≥ 0；缺省 0；计入制造总精魂消耗 |
| StatBonus | 增加的属性值 | 见编码 | 五项基础属性平坦加成；制造时按维 **Σ** 得 `Base(S)` |
| AutoConvert | 超上限兑精魂 | `int` 或 `float` | 语义同 `MaterialConfig.AutoConvert`：每 1 个超出材料兑精魂数（≥ 0；0 = 超出丢弃且不兑） |
| Description | 文字介绍 | `string` | 展示文案；若启用 i18n 可为本地化 Key |
| ArtAssetId | 外形美术素材ID | `string` 或 `int` | 部位单件外观 / 仓库 UI 可用资源 Id |
| IsPrimaryHand | 主要手 | `int` / `0\|1` | **仅** `BodySlot=Arm` 有意义；`1`=主要手（Mode2 选料锚点）；缺省 `0`；非 Arm 须为 `0` |
| ClassRestrict | 职业限定 | 见编码 | 可产出的 `ClassId` 多值；Mode2 双手交集定职业；空=配置错误（主要手空则停造，见 §3.15） |
| BodyPrimaryStat | 躯体主属性 | `enum` / `string` | `Strength` \| `Agility` \| `Intelligence` 恰一；Mode2 选其余部位匹配键；**勿与** `ClassConfig.PrimaryStat` 混淆 |

**`ClassRestrict` 编码（固定）：** `ClassId` 或 `ClassId|ClassId|…`（管道分隔；精确匹配 `ClassConfig.ClassId`）。

**`StatBonus` 编码（固定）：** `属性项_数值|属性项_数值|…`（与 [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers` 同风格；加法；空 = 无加成）。属性项键与五项 BaseStats 对齐（如 `MaxHP` / `MoveSpeed` / `Strength` / `Agility` / `Intelligence`）。

```
BodyPartConfig {
  BodyPartId: Id                  // also warehouse / LootDrop material Id
  DisplayName: string             // item display name; empty → UI falls back to BodyPartId
  BodyLevel: number
  BodySlot: Head | Torso | Arm | Leg
  RaceId: Id                      // FK → RaceConfig
  ControlPowerCost: number
  SpiritCost: number              // >= 0; default 0; Mode2 AutoManufacture ignores
  StatBonus: "Attr_Value|..."     // additive; empty = none
  AutoConvert: number             // SpiritEssence per 1 excess unit
  Description: string
  ArtAssetId: Id
  IsPrimaryHand: 0 | 1            // Arm only; Mode2
  ClassRestrict: "ClassId|..."    // Mode2 class pool
  BodyPrimaryStat: Strength | Agility | Intelligence
}
```

**解析：**

- `Base(S) = Σ` 已放入躯体部位的 `StatBonus(S)`（缺省维 **0**）；见 [SPEC_03 §3.11](SPEC_03_GameRules.md)。
- Mode2 AutoManufacture 选料 / 职业规则见 [SPEC_03 §3.15](SPEC_03_GameRules.md)。
- 仓库入账：`BodyPartId` 按材料堆叠；超限按本行 `AutoConvert` 兑精魂。
- Mode1 表可缺新列（加载缺省：`IsPrimaryHand=0`、`ClassRestrict` 空、`BodyPrimaryStat` 可空但 Mode2 行应填；`DisplayName` 空则 UI 回退 `BodyPartId`）。
- DigStageSummary（UI-011）躯体材料行：`{DisplayName} Lv{BodyLevel} × 数量`。
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
| ClassAffinity | 职业倾向 | 见编码 | 精确匹配 `ClassConfig.ClassName`（经士兵实例 `ClassId`） |
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
3. **若 A 为空**：定稿种族改为 **`Race_Undead`**，重载 `RaceAdjustCoeff` 与命名种族段，从步骤 2 **仅重跑一轮**；本轮 A 仍空 → Mode2 先 `DefaultAppearanceId`（非空），再步骤 5→6。
4. 若 A 非空：子集 B = `ClassAffinity` 含 `ClassConfig.ClassName`（经实例 `ClassId`）的行；若 B 非空 → 在 B 中 **均匀随机**；**若 B 为空（职业不匹配）→ 不采用 A，改走步骤 5 同种族保底**（不因职业不匹配改亡灵）。Mode2：B 空时可先用 `ClassConfig.DefaultAppearanceId`（非空），再步骤 5。
5. 取**当前**定稿种族 `IsFallback == 1` 的行；有则用之。
6. 若仍无：在 **全表** 中 **均匀随机** 一行。

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
| Skills | 额外技能 | 见编码 | 编码同 [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig)；失控加成见 [§9.21](#921-技能配置表-skillconfig) |

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
| GraveSpawnWeightBonus_{QualityId} | `DigProtagonistCapabilities.GraveSpawnWeightBonus[QualityId]` | 对该品质生成权重加法；表缺席视为 0；编码例 `GraveSpawnWeightBonus_Q4_10`（末 `_` 切数值） |
| DigProcessSpawnCountBonus | `DigProtagonistCapabilities.DigProcessSpawnCountBonus` | 对过程生成 `SpawnRate` 的 **M** 加法（整数）；编码例 `DigProcessSpawnCountBonus_3`；**不**改 N |

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
| MonsterType | 怪物类型 | `int` / `enum` | `1`=普通（`Normal`）\| `2`=精英（`Elite`）\| `3`=BOSS（`Boss`）；供后续士兵技能等判定是否生效的原型标签；**异于** `PushMapSpawnConfig.IsBoss`（后者仅表示该刷怪行是否通关目标）；**本批不驱动**技能/AI；**加载缺省：** 列缺失或空 → `1`（`Normal`）；非法值 → 加载失败 |
| AggroMode | 仇恨模式 | `enum` / `string` | `ActiveChase` \| `PassiveChase` \| `StationaryActive` \| `StationaryPassive`；见 [SPEC_03 §3.14](SPEC_03_GameRules.md)；**加载缺省：** 列缺失或空单元格 → `ActiveChase`；非法值 → 加载失败 |
| AlertRadius | 警戒半径 | `float` | ≥ 0；主动发现半径；**加载缺省：** 列缺失或空 → `AttackRange`；若解析值 < 0 → 加载失败 |
| BodyRadius | 占地半径 | `float` | ≥ 0；XZ 占地圆半径（世界单位）；PushMap 刷出散开与移动怪 `NavMeshAgent.radius` 共用；**加载缺省：** 列缺失或空 → `0.35`；若解析值 < 0 → 加载失败 |
| MaxHP | 怪物血量 | `int` 或 `float` | 生成时初始化怪物 maxHP / 当前 HP |
| MoveSpeed | 怪物移动速度 | `float` | 世界单位/秒或项目统一速度单位 |
| AttackPower | 怪物攻击力 | `int` 或 `float` | **仅**攻击士兵时用于伤害结算；对主角普通攻击不用本字段 |
| AttackSpeed | 攻击速度 | `float` | 攻击频率（具体单位实现时锁定） |
| AttackRange | 攻击距离 | `float` | 进入攻击态距离 |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0；秒；`AttackMode=Melee` 时用 |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0；`AttackMode=Ranged` 时用 |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0；秒；超时未命中 → 未命中 |
| Skills | 怪物技能 | 见编码 | 技能 ID + CD 列表；技能效果列另专题；**第一版 Demo 不生效**（只打普通攻击；实现时可忽略或配空） |
| LootDrop | 怪物掉落 | 见编码 | 击杀产出；编码为 `Id;Count\|Id;Count\|...`（**不是** [§9.3](#93-坟墓品质定义表-gravequalityconfig) 的 `DropMode` / `Id;Weight;Count`） |

**`Skills` 编码（固定）：** `SkillId_CdSeconds|SkillId_CdSeconds|...`

- 段分隔符：`|`
- 段内：`技能ID_冷却秒`
- 空字符串 = 无技能
- 技能效果定义表本批 **不定**；Demo v1 即使有值也 **不施放**
- **注意：** 与士兵侧 `SkillId;Level|…` **编码不同**（怪物 CD 写在本表）

**`LootDrop` 编码：** `Id;Count|Id;Count|...`（与坟墓品质表 [§9.3](#93-坟墓品质定义表-gravequalityconfig) **不同**；本表无 `DropMode`）。段分隔 `|`；段内从右最后一个 `;` 分出 `Count`。`Id` 解析顺序同 §9.3 入账（Spirit / Material / BodyPart）。空串、缺 `;`、`Count` 非正整数：忽略该段并打日志。

```
MonsterConfig {
  MonsterId: Id
  ModelId: string                  // Prefab logical name → Assets/Prefabs/Defend/Monsters/{Id}.prefab; art §15
  DisplayName: string
  TargetSelect: Nearest | PreferWarrior | PreferProtagonist
  AttackMode: Melee | Ranged
  MonsterType: 1 | 2 | 3              // Normal | Elite | Boss; empty → 1; ≠ PushMap IsBoss
  AggroMode: ActiveChase | PassiveChase | StationaryActive | StationaryPassive  // empty → ActiveChase
  AlertRadius: number              // empty → AttackRange
  BodyRadius: number               // empty → 0.35
  MaxHP: number
  MoveSpeed: number
  AttackPower: number              // soldiers only
  AttackSpeed: number
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
  Skills: "SkillId_Cd|SkillId_Cd|..."
  LootDrop: "Id;Count|Id;Count|..."
}
```

**加载约定（`ConfigCsvRepository`）：** `MonsterType` / `AggroMode` / `AlertRadius` / `BodyRadius` 缺省如上；非法 `MonsterType` / 非法枚举或 `AlertRadius < 0` / `BodyRadius < 0` 整表加载失败（§14.5）。`MonsterType` **不**替代 `PushMapSpawnConfig.IsBoss`。PushMap 与 Defend 共用本表。

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

#### 9.20b 战斗常量表 `CombatConstantConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) / [§3.12](SPEC_03_GameRules.md) 全局战斗公式默认；另承载 **P0 镜头 / 挖坟节奏** 调参（[§3.10](SPEC_03_GameRules.md) / [§3.14](SPEC_03_GameRules.md)）。一行 = 一个常量键。职业 `CombatConvertCoeffs` **有键覆盖**、缺键/空串读本表；`MaxHP` 力量系数亦读本表。镜头与 Dig 时长由各 Stage / `DigProtagonistCapabilities` 经 `ConfigCsvRepository.GetCombatConstantOrFallback` / `GetCameraPresentationConstants` / `ApplyDigTimingConstants` 读取。

**磁盘名：**
- **Excel：** `通用_常量表_Combat_CombatConstantConfig.xlsx`
- **CSV：** `Combat_CombatConstantConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| ConstantKey | 常量键 | `string` | 主键；与 `CombatConvertCoeffs` 键名对齐，另含 `MaxHpStrengthMult` 与下表 P0 键 |
| ConstantKeyZh | 主键中文翻译 | `string` | 可选；主键展示用中文名；**运行时不读** |
| Value | 数值 | `float` | 该键默认值 |
| Comment | 备注 | `string` | 可选；英文策划备注；**运行时不读** |
| CommentZh | 备注中文解释 | `string` | 可选；中文策划说明；**运行时不读** |

**战斗公式必填键（Mode1/Mode2 样例）：**

| ConstantKey | ConstantKeyZh | Value | CommentZh（摘要） |
|-------------|---------------|-------|-------------------|
| `NormalAttackPrimaryMult` | 普攻主属性倍率 | `15` | 普攻伤害 = 主属性 × 本系数 |
| `AttackSpeedBase` | 攻击速度基础值 | `0.5` | 攻速公式常数项 |
| `AttackSpeedAgiDiv` | 攻击速度敏捷除数 | `60` | 攻速公式敏捷除数 |
| `SkillCdIntDiv` | 技能冷却智力除数 | `30` | 技能 CD 智力减项除数 |
| `SkillCdFloor` | 技能冷却下限 | `0.1` | 技能 CD 最短秒数 |
| `MaxHpStrengthMult` | 血量力量系数 | `3` | MaxHP = ceil(BodyLife + Str × 本值) |

**P0 镜头 / 挖坟节奏必填键：**

| ConstantKey | ConstantKeyZh | Value | CommentZh（摘要） |
|-------------|---------------|-------|-------------------|
| `CameraHeightY` | 镜头高度 | `18` | 俯视相机相对地图中心世界 Y |
| `CameraOrthoSizeMargin` | 地图适配Size余量 | `1.5` | Dig/Defend/布阵：Size = max(半幅) − 本值 |
| `PushMapCameraOrthoSize` | 推图开战默认Size | `2` | PushMap 开战默认正交 Size |
| `CameraNearClip` | 镜头近裁剪面 | `0.1` | 近裁剪 |
| `CameraFarClip` | 镜头远裁剪面 | `100` | 远裁剪 |
| `CameraFollowDeadzone` | 跟随死区半径 | `0.15` | PushMap Auto 跟随世界 XZ 死区 |
| `CameraFollowSmoothTime` | 跟随平滑时间 | `0.25` | PushMap Auto SmoothDamp 秒 |
| `CameraZoomStepPerNotch` | 滚轮缩放步进 | `0.5` | 滚轮每格改变 Size |
| `CameraOrthoSizeMin` | 正交Size下限 | `0.5` | Size 下限 |
| `CameraOrthoSizeMax` | 正交Size上限 | `20` | PushMap 滚轮上限 |
| `CameraDragThresholdPixels` | 拖拽启动像素阈值 | `4` | 手动拖镜头累计像素阈值 |
| `PushMapCameraIntroSpeed` | 推图镜头预览速度 | `1.5` | PushMap 开战 Intro 沿轨世界 XZ 速度（单位/秒） |
| `PushMapCameraIntroWaypointDwellSeconds` | 推图镜头预览路点停留 | `0.5` | PushMap Intro 每个作者 WP 停留秒 |
| `DigTriggerDwellSeconds` | 挖坟触发停留 | `0.2` | 触发 DigAction 前光标停留秒 |
| `BaseDigDuration` | 挖坟基础时长 | `0.8` | DigActionDuration 公式基数 |
| `DigActionDurationFloor` | 挖坟最短时长 | `0.1` | DigActionDuration 地板 |

**P1 战斗调参必填键：**

| ConstantKey | ConstantKeyZh | Value | CommentZh（摘要） |
|-------------|---------------|-------|-------------------|
| `AttackSlotMeleeCount` | 近战攻击位数量 | `12` | 近战攻击位环槽位数 |
| `AttackSlotRangedCount` | 远程攻击位数量 | `8` | 远程攻击位环槽位数 |
| `AttackSlotMargin` | 攻击位环半径余量 | `0.05` | 环半径计算余量 |
| `AttackSlotMinRingRadius` | 攻击位环最小半径 | `0.05` | 环半径下限 |
| `AttackSlotReclaimMoveThreshold` | 攻击位重算位移阈值 | `0.5` | 目标移动重算阈值 |
| `AttackSlotDefaultTargetBodyRadius` | 默认目标体半径 | `0.35` | 缺半径时默认 |
| `HitConfirmSlack` | 命中确认松弛距离 | `0.05` | HitConfirm 距离松弛 |
| `SurroundGapDegrees` | 包围缺口角度 | `60` | 近战包围缺口扇区 |
| `StuckDetectWindowSeconds` | 卡死检测窗口 | `0.5` | StuckHold 检测窗 |
| `StuckDisplacementEpsilon` | 卡死位移阈值 | `0.2` | 卡死位移 ε |
| `StuckHoldSeconds` | 卡死停顿时长 | `1` | 卡住强制 Idle 秒 |
| `ProjectileDefaultHitRadius` | 投射物默认命中半径 | `0.55` | 投射物软命中半径 |
| `DefendVictoryStageExp` | 防守胜利阶段经验 | `100` | Defend 胜场阶段 Exp |
| `NewSaveInitialSpiritCount` | 新建档初始精魂 | `30` | 新建存档入账 ItemId=Spirit 数量；≤0 不发放 |

**P2 寻路/性能必填键：**

| ConstantKey | ConstantKeyZh | Value | CommentZh（摘要） |
|-------------|---------------|-------|-------------------|
| `FlowFieldDefaultCellSize` | 流场默认格宽 | `0.5` | 流场默认格宽 |
| `FlowFieldMinCellSize` | 流场最小格宽 | `0.25` | 流场格宽下限 |
| `FlowFieldMaxCellSize` | 流场最大格宽 | `0.5` | 流场格宽上限 |
| `MassMoveMaxRecalcPerFrame` | 群体移动每帧重算上限 | `50` | 每帧重算预算 |
| `MassMoveDefaultAgentRadius` | 群体移动默认体半径 | `0.1` | 默认体半径 |
| `MassMoveArriveEpsilon` | 群体移动到达阈值 | `0.08` | 到达判定 ε |
| `MassMoveDefaultObjectiveArriveRadius` | 目标到达默认半径 | `2` | 目标到达默认半径 |
| `MassMoveAttackSlotSeparationScale` | 攻击位软分离系数 | `0.35` | 攻击位软分离缩放 |
| `SoftCollisionMaxCorrectionSpeed` | 软碰撞最大修正速度 | `2` | 软碰撞修正速度上限 |
| `LocalDetourProbeLength` | 本地绕行探测长度 | `1` | 绕行探测长 |
| `LocalDetourSoftSeparationStrength` | 本地绕行软分离强度 | `0.15` | 软分离强度 |
| `LocalDetourDetourBias` | 本地绕行偏置 | `0.85` | 绕行偏置 |
| `LocalDetourForwardConeHalfAngleDeg` | 本地绕行前向锥半角 | `50` | 前向阻挡锥半角 |
| `BossAdvanceArriveRadius` | Boss推进到达半径 | `0.35` | Boss 到达半径 |
| `EngageStickHysteresisMargin` | 接战粘滞余量 | `0.15` | 换目标粘滞余量 |
| `PushMapSpawnMinSampleDistance` | 刷怪散布最小采样距 | `0.75` | 刷怪最小采样距 |
| `PushMapSpawnSampleDistanceBodyMul` | 刷怪散布体半径倍数 | `2.5` | 采样距体半径倍数 |
| `PushMapSpawnLeashSlack` | 刷怪拴绳松弛 | `0.35` | Sample 拴绳松弛 |
| `PushMapSpawnAbsoluteLeashFloor` | 刷怪绝对拴绳下限 | `3` | 绝对拴绳下限 |
| `PushMapSpawnAbsoluteLeashBodyMul` | 刷怪绝对拴绳体倍数 | `10` | 绝对拴绳体倍数 |

```
CombatConstantConfig {
  ConstantKey: string
  ConstantKeyZh: string   // optional; display; runtime ignore
  Value: number
  Comment: string         // optional; EN note; runtime ignore
  CommentZh: string       // optional; ZH note; runtime ignore
}
```

**解析：** `ConfigCsvRepository` 按当前 CampaignMode CSV 根加载；`TryGetCombatConstant(key)`；`GetCombatConvertCoeffDefaults()` 组装五键供 `CombatConvertCoeffs.Parse` 缺键回退；`GetCameraPresentationConstants()` / `ApplyDigTimingConstants` / `GetDigTriggerDwellSeconds` 读 P0 键；**`CombatRuntimeTuning.ApplyFromRepository`** 在常量表加载末尾应用 P1/P2（及共享寻路）快照，AttackSlot / MassMove / FlowField / LocalDetour / SoftCollision / StuckHold / Projectile / Defend 胜场 Exp / PushMap 散布等读该快照。缺必填键 → Warning + 与上表样例同值的安全兜底（**非**业务权威）。Mode1/Mode2 各一份文件。新建档初始精魂：`MetaShellController.EnterShell(..., isNewSave)` → `WarehouseService.ApplyNewSaveGrants(configs)` 读 `NewSaveInitialSpiritCount` 并经 `CreditLootEntry(Spirit)` 入账（[SPEC_03 §3.4](SPEC_03_GameRules.md)）。

#### 9.21 技能配置表 `SkillConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 士兵技能 / [§3.12](SPEC_03_GameRules.md) SkillCast。**士兵技能权威表**（汇总所有属于士兵的技能）；一行 = 某一 `SkillId` 的某一等级。复合主键 `(SkillId, SkillLevel)`。怪物技能仍走 `MonsterConfig.Skills`，**不**用本表作怪物目录。灵魂 / 宝石 / 外置 `Skills` 列表仍可引用本表 `SkillId`（与实例 `SoldierSkills` 并行；同 Id 合并 **TBD**）。**Demo D-069** 驱动 PushMap `Skill_03` 施放、`Skill_01` 格挡与 `Skill_02` 舒适（硬映射）。**Demo D-073** 驱动 `Skill_04`～`Skill_12` 经 `SkillEffectConfig.EffectKind` 登记制（禁止 Session 按 `SkillId` 分支）。**禁止**解析 `Description` 自然语言作效果器。

**磁盘名：**
- **Excel：** `战斗_技能配置表_Combat_SkillConfig.xlsx`
- **CSV：** `Combat_SkillConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| SkillId | 士兵技能ID | `string` 或 `int` | 复合主键之一；被 `ClassConfig.DefaultSkillIds`、实例 `SoldierSkills`、灵魂/宝石/外置 `Skills` 引用 |
| SkillLevel | 技能等级 | `int` | 复合主键之一；从 **1** 起；实例烘进等级查本行 |
| CooldownMode | 冷却模式 | `enum` / `string` | `Mode1` \| `Mode2`；与 `CampaignMode` 对齐；缺/非法 → Warning。`Mode2`：**释放提交后**进 CD（D-069）；`Mode1` 本 Demo 不驱动 |
| CastTarget | 施放目标 | `enum` / `string` | 已出现：`CurrentNormalAttackTarget` \| `EnemySingle` \| `Self` \| `AllySingle` \| `GroundPoint` \| `EnemyAll`。Changelog「七枚举」缺项 **TBD**。`Skill_03`：`EnemySingle` = 当前交战单体 |
| ExtraActivationCondition | 额外激活条件 | `string` | 可选；空 = 无；Mode2 样例为自然语言编码。D-069 **硬匹配** `敌人普攻命中Self`（Skill_01）与 `自身血量=100%`（Skill_02）。D-073 Handler **不解析**本列正文，用 `EffectKind`/`EffectParams`/`TriggerHook` 判定（语义对齐 CSV 措辞） |
| DisplayName | 技能名称 | `string` | 展示名；若启用 i18n 可为 Key |
| Description | 技能文字描述 | `string` | 展示文案 |
| IconAssetId | 技能图标 | `string` 或 `int` | UI 图标资源 Id；缺/空 = 无图。**UI-021 士兵栏悬浮框不读本字段**：按 `SkillId` 从 `Resources/UI/Skills/{SkillId}` 加载 |
| SkillEffectId | 技能效果 | `string` 或 `int` | FK → `SkillEffectConfig`；效果正文见 §9.21b（`EffectKind`/`EffectParams`/`TriggerHook`）。Mode2 多级技能命名：`SkillEffect_{SkillNum}_{Level}`（如 `SkillEffect_01_3` ↔ `Skill_01` Lv3） |
| EffectImplemented | 效果已实现 | `0` \| `1` | Demo 战斗效果是否已接线；**不驱动战斗**，只给 UI-021 着色。`1` = 绿；`0` = 红；缺/空 → `0`。D-069 种子：`Skill_01`/`Skill_02`/`Skill_03` 全级 = `1`。D-073：`Skill_04`～`Skill_12` 各 SkillId 在 SE-01～09 落地后 Lv1～5 置 `1` |
| BaseCooldownSeconds | 基础冷却 | `float` | ≥ 0；秒；士兵实际 CD = `max(SkillCdFloor, BaseCooldownSeconds − SkillCdIntDiv/max(Int,1))`（系数见 `ClassConfig.CombatConvertCoeffs` / §3.12）；D-069 驱动 `Skill_03` |
| LossOfControlChanceBonus | 失控概率加成 | `float` | 可正可负；缺省 **0**；`ΣSkillBonus` 按实例烘进等级查本字段（§3.11） |

**复合主键规则：** `(SkillId, SkillLevel)` 唯一；同 `SkillId` 的等级行宜从 1 连续（实现加载时可校验并 Warning）。查行：用实例 `{ SkillId, SkillLevel }`；缺行 → 该技能无效 + Warning。

```
SkillConfig {
  SkillId: Id
  SkillLevel: int                 // ≥ 1; composite PK with SkillId
  CooldownMode: Mode1 | Mode2
  CastTarget: CurrentNormalAttackTarget | EnemySingle | Self | AllySingle | GroundPoint | EnemyAll | TBD
  ExtraActivationCondition: string
  DisplayName: string
  Description: string
  IconAssetId: Id | ""
  SkillEffectId: Id               // FK → SkillEffectConfig
  EffectImplemented: 0 | 1        // UI-021 tint only; missing → 0
  BaseCooldownSeconds: number     // >= 0; actual CD per §3.12; D-069 drives Skill_03
  LossOfControlChanceBonus: number  // +/- ; default 0
}
```

**说明：** 当士兵 `ΣSkillBonus ≠ 0` 时，每次释放技能额外用完整 `FinalLossChance` 再判定一次（§3.11）。Demo D-069 在 PushMap **成功提交** `Skill_03` 后触发（样例 `LossOfControlChanceBonus=0` 时本技能单独不触发）。士兵技能 **无**消耗经验升级；等级来自制造默认 1 + Mode2 `SoldierSkillLevelAdd`。

**Mode2 样例行 `Skill_01`（格挡，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description（摘要） | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|---------------------|---------------|--------------|----------------------|
| 1 | `Self` | `敌人普攻命中Self` | 格挡 | 敌人本次普通攻击有 **10%** 概率伤害变为 0（但还是判断为命中） | `SkillEffect_01_1` | `Mode2` | 0 |
| 2 | `Self` | `敌人普攻命中Self` | 格挡 | … **15%** … | `SkillEffect_01_2` | `Mode2` | 0 |
| 3 | `Self` | `敌人普攻命中Self` | 格挡 | … **20%** … | `SkillEffect_01_3` | `Mode2` | 0 |
| 4 | `Self` | `敌人普攻命中Self` | 格挡 | … **25%** … | `SkillEffect_01_4` | `Mode2` | 0 |
| 5 | `Self` | `敌人普攻命中Self` | 格挡 | … **30%** … | `SkillEffect_01_5` | `Mode2` | 0 |

公共：`IconAssetId=Skill_01`，`LossOfControlChanceBonus=0`。`CastTarget=Self`：被动受击反应作用于自身；`ExtraActivationCondition` 限定敌人普通攻击命中本士兵时方可判定。

**Skill_01 格挡编码（D-069 / SC-02 方案 B；硬映射，非通用解析器）：** `SkillEffect_01_1`～`_5` → Lv1～5 概率 **10%/15%/20%/25%/30%**（对齐 Description，不解析正文）。独立被动钩子挂在 PushMap `TryApplyMonsterDamageToWarrior` 结算前；成功则本次伤害→0（仍判命中）。**不**占用普攻通道、**不**进 CD、**不**触发失控二次 roll。不格挡远程弹道。Defend 本片不接线。见 [SPEC_03 §3.12](SPEC_03_GameRules.md) SkillCast。

**Mode2 样例行 `Skill_02`（舒适，射手 `Class_Archer.DefaultSkillIds`，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `Self` | `自身血量=100%` | 舒适 | 伤害提升 **5%** | `SkillEffect_02_1` | `Mode2` | 0 |
| 2 | `Self` | `自身血量=100%` | 舒适 | 伤害提升 **10%** | `SkillEffect_02_2` | `Mode2` | 0 |
| 3 | `Self` | `自身血量=100%` | 舒适 | 伤害提升 **15%** | `SkillEffect_02_3` | `Mode2` | 0 |
| 4 | `Self` | `自身血量=100%` | 舒适 | 伤害提升 **20%** | `SkillEffect_02_4` | `Mode2` | 0 |
| 5 | `Self` | `自身血量=100%` | 舒适 | 伤害提升 **25%** | `SkillEffect_02_5` | `Mode2` | 0 |

公共：`IconAssetId=Skill_02`，`LossOfControlChanceBonus=0`。`CastTarget=Self`：满血时作用于自身输出，非主动选敌；`ExtraActivationCondition` 限定 `RemainingHp >= MaxHp` 时才激活（条件性增伤，非常驻）。灵魂样例 `Soul_02` 仍写 `Skills=Skill_02;1`（与实例 `SoldierSkills` 并行）。

**Skill_02 舒适编码（D-069 / SC-03 方案 A；硬映射，非通用解析器）：** `SkillEffect_02_1`～`_5` → Lv1～5 Outgoing **+5%/+10%/+15%/+20%/+25%**（对齐 Description，不解析正文）。独立倍率钩子挂在 PushMap `SettleMonsterDamage`（近战/远程 HitConfirm，含 `Skill_03` 连发每一击）扣怪 HP 前。判定该次结算时 `RemainingHp >= MaxHp`。`本次伤害 = NormalAttackPower × (1 + bonus)`；**不**改写存储的 `NormalAttackPower`。**不**占用普攻通道、**不**进 CD、**不**触发失控二次 roll。连发 3 击各自独立检查满血。持有即生效（含 Rebel 对怪输出）。Defend 本片不接线。见 [SPEC_03 §3.12](SPEC_03_GameRules.md) SkillCast。

**Mode2 样例行 `Skill_03`（连发，法师 `Class_Mage.DefaultSkillIds`，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `EnemySingle` | （空） | 连发 | 攻击目标时连续攻击 **3** 次 | `SkillEffect_03_1` | `Mode2` | 50 |
| 2 | `EnemySingle` | （空） | 连发 | 攻击目标时连续攻击 **3** 次 | `SkillEffect_03_2` | `Mode2` | 40 |
| 3 | `EnemySingle` | （空） | 连发 | 攻击目标时连续攻击 **3** 次 | `SkillEffect_03_3` | `Mode2` | 30 |
| 4 | `EnemySingle` | （空） | 连发 | 攻击目标时连续攻击 **3** 次 | `SkillEffect_03_4` | `Mode2` | 20 |
| 5 | `EnemySingle` | （空） | 连发 | 攻击目标时连续攻击 **3** 次 | `SkillEffect_03_5` | `Mode2` | 10 |

公共：`IconAssetId=Skill_03`，`LossOfControlChanceBonus=0`。`CastTarget=EnemySingle`：主动对当前单体敌人施放；`ExtraActivationCondition` 空 = 无额外激活门（仅受 CD 约束，不像格挡/舒适的受击或满血条件）。五级 Description 相同（连击次数不随等级变）；等级差体现在 `BaseCooldownSeconds`。灵魂样例 `Soul_03` 仍写 `Skills=Skill_03;1`（与实例 `SoldierSkills` 并行，本 Demo 不读该并行列表）。

**Skill_03 施放编码（D-069 / 方案 C；硬映射，非通用解析器）：** `SkillEffect_03_1`～`_5` → 占用普攻通道连续 **3** 次方案 D 命中（每次 `NormalAttackPower`）；插入点 = CD 好且已进距即放；`CooldownMode=Mode2` 提交后进 CD。见 [SPEC_03 §3.12](SPEC_03_GameRules.md) SkillCast。

**Mode2 样例行 `Skill_04`（先发制人，刺客 `Class_Rogue.DefaultSkillIds`，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `EnemySingle` | `普攻攻击新目标敌人的第一次` | 先发制人 | 攻击伤害提升 **20%** | `SkillEffect_04_1` | `Mode2` | 0 |
| 2 | `EnemySingle` | `普攻攻击新目标敌人的第一次` | 先发制人 | 攻击伤害提升 **30%** | `SkillEffect_04_2` | `Mode2` | 0 |
| 3 | `EnemySingle` | `普攻攻击新目标敌人的第一次` | 先发制人 | 攻击伤害提升 **40%** | `SkillEffect_04_3` | `Mode2` | 0 |
| 4 | `EnemySingle` | `普攻攻击新目标敌人的第一次` | 先发制人 | 攻击伤害提升 **50%** | `SkillEffect_04_4` | `Mode2` | 0 |
| 5 | `EnemySingle` | `普攻攻击新目标敌人的第一次` | 先发制人 | 攻击伤害提升 **60%** | `SkillEffect_04_5` | `Mode2` | 0 |

公共：`IconAssetId=Skill_04`，`LossOfControlChanceBonus=0`。`CastTarget=EnemySingle`：增伤作用在当前普攻的单体敌人上，非 `Self` 常驻；`ExtraActivationCondition` 限定本士兵对**新选定目标**打出的**第一次普通攻击**时激活（换目标后下一次首击可再触发；对同一目标的后续普攻不加）。五级 `ExtraActivationCondition` 与目标类型相同；等级差体现在 Description 增伤幅度；`BaseCooldownSeconds=0`（无独立 CD，挂在普攻首击上）。

**Mode2 样例行 `Skill_05`（坚挺，近卫 `Class_Guardian.DefaultSkillIds`，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `Self` | `敌人的本次攻击导致Self死亡` | 坚挺 | 士兵自身生命值强制变为1点血，并无敌1秒钟 | `SkillEffect_05_1` | `Mode2` | 60 |
| 2 | `Self` | `敌人的本次攻击导致Self死亡` | 坚挺 | 士兵自身生命值强制变为1点血，并无敌2秒钟 | `SkillEffect_05_2` | `Mode2` | 60 |
| 3 | `Self` | `敌人的本次攻击导致Self死亡` | 坚挺 | 士兵自身生命值强制变为1点血，并无敌3秒钟 | `SkillEffect_05_3` | `Mode2` | 60 |
| 4 | `Self` | `敌人的本次攻击导致Self死亡` | 坚挺 | 士兵自身生命值强制变为1点血，并无敌4秒钟 | `SkillEffect_05_4` | `Mode2` | 60 |
| 5 | `Self` | `敌人的本次攻击导致Self死亡` | 坚挺 | 士兵自身生命值强制变为1点血，并无敌5秒钟 | `SkillEffect_05_5` | `Mode2` | 60 |

公共：`IconAssetId=Skill_05`，`LossOfControlChanceBonus=0`。`CastTarget=Self`：致死拦截作用于自身，不选敌；`ExtraActivationCondition` 限定**本下攻击**会使 Self 进入 HP≤0 时方可触发（措辞为「本次攻击」，**不**限定普通攻击，有别于格挡的 `敌人普攻命中Self`）。五级 `ExtraActivationCondition` 与 `CastTarget` 相同；等级差体现在 Description 无敌秒数；`BaseCooldownSeconds` 全级 **60**（CD 不随等级变）。灵魂样例 `Soul_05` 仍写 `Skills=Skill_05;1`（与实例 `SoldierSkills` 并行）；宝石样例 `Gem_Amethyst_01` 仍写 `Skills=Skill_05;1`。D-073：`EffectKind=CheatDeathInvincible`。

**Mode2 样例行 `Skill_06`（震晕，炸弹师 `Class_BombMaster.DefaultSkillIds`，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `GroundPoint` | `士兵普攻命中敌人` | 震晕 | 10% 对目标+半径1.5 击晕 **1** 秒 | `SkillEffect_06_1` | `Mode2` | 0 |
| 2–5 | 同 | 同 | 震晕 | 同；击晕 **2～5** 秒 | `SkillEffect_06_2`～`_5` | `Mode2` | 0 |

公共：`IconAssetId=Skill_06`，`LossOfControlChanceBonus=0`。`CastTarget=GroundPoint`：AOE 圆心=被命中怪位置。概率全级 **10%**；半径全级 **1.5**；等级差=击晕秒数。

**Mode2 样例行 `Skill_07`（冰冻，冰法 `Class_IceMage.DefaultSkillIds`，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `GroundPoint` | （空） | 冰冻 | 命中后半径1.5 攻/移速 −50%，持续 **2** 秒 | `SkillEffect_07_1` | `Mode2` | 10 |
| 2–5 | 同 | （空） | 冰冻 | 同；持续 **3～6** 秒 | `SkillEffect_07_2`～`_5` | `Mode2` | 10 |

公共：`IconAssetId=Skill_07`。内部 CD 权威=`BaseCooldownSeconds=10`（成功 PROC 后提交）。

**Mode2 样例行 `Skill_08`（精英克制，格斗师 `Class_Brawler.DefaultSkillIds`，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `EnemySingle` | `目标敌人的MonsterType是精英(Elite)` | 精英克制 | 伤害 +**50%** | `SkillEffect_08_1` | `Mode2` | 0 |
| 2–5 | 同 | 同 | 精英克制 | +**60%/70%/80%/90%** | `SkillEffect_08_2`～`_5` | `Mode2` | 0 |

公共：`IconAssetId=Skill_08`。判定读 `MonsterConfig.MonsterType==Elite`（不解析条件正文）。

**Mode2 样例行 `Skill_09`（渐入佳境，狂战士 `Class_Berserker.DefaultSkillIds`，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `Self` | （空） | 渐入佳境 | 攻击 +**3%**/层，最大 **60%** | `SkillEffect_09_1` | `Mode2` | 10 |
| 2–5 | 同 | （空） | 渐入佳境 | +**5%/7%/9%/12%**/层，最大仍 60% | `SkillEffect_09_2`～`_5` | `Mode2` | 10 |

公共：`IconAssetId=Skill_09`。`BaseCooldownSeconds=10` = 叠层 Tick。

**Mode2 样例行 `Skill_10`（贯穿，长弓手 `Class_Longbowman.DefaultSkillIds`，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `EnemySingle` | （空） | 贯穿 | 命中后继续飞，再命中下 **1** 名，伤害 100% | `SkillEffect_10_1` | `Mode2` | 0 |
| 2–5 | 同 | （空） | 贯穿 | 再命中下 **2～5** 名 | `SkillEffect_10_2`～`_5` | `Mode2` | 0 |

公共：`IconAssetId=Skill_10`。`ExtraHitCount`=首击之后的额外命中数。飞行：**保持当前弹道速度方向**；不每弹道 A*。

**Mode2 样例行 `Skill_11`（灼烧，火法 `Class_FireMage.DefaultSkillIds`，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `GroundPoint` | （空） | 灼烧 | 每 1s 造成普攻 **20%**，持续 **2** 秒；再施加叠时间 | `SkillEffect_11_1` | `Mode2` | 0 |
| 2–5 | 同 | （空） | 灼烧 | 同；持续 **3～6** 秒 | `SkillEffect_11_2`～`_5` | `Mode2` | 0 |

公共：`IconAssetId=Skill_11`。`StackMode=RefreshDuration`。

**Mode2 样例行 `Skill_12`（瞬移，影刃 `Class_Shadowblade.DefaultSkillIds`，Lv1～5）：**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `EnemySingle` | `当开始寻找新的攻击目标` | 瞬移 | 改选最远敌并瞬移到背后 | `SkillEffect_12_1` | `Mode2` | 60 |
| 2–5 | 同 | 同 | 瞬移 | 同；BaseCD **50/40/30/20** | `SkillEffect_12_2`～`_5` | `Mode2` | 50/40/30/20 |

公共：`IconAssetId=Skill_12`。CD 权威=`SkillConfig.BaseCooldownSeconds`（不写进 EffectParams）。

#### 9.21b 技能效果配置表 `SkillEffectConfig`

规则语义：被 `SkillConfig.SkillEffectId` 引用。一行 = 一种效果定义。**效果正文列在本表扩写**，**不**再把效果列塞回 `SkillConfig`。Demo D-069 对 `SkillEffect_01_*`/`_02_*`/`_03_*` **硬映射**（格挡 / 舒适 / 连发）；本列 `EffectKind` 对这些行可空。Demo D-073 对 `SkillEffect_04_*`～`_12_*` 走登记制 Handler。Mode1 可空列占位，本片不填战斗效果。

**磁盘名：**
- **Excel：** `战斗_技能效果配置表_Combat_SkillEffectConfig.xlsx`
- **CSV：** `Combat_SkillEffectConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| SkillEffectId | 技能效果ID | `string` 或 `int` | 主键 |
| Notes | 备注 | `string` | 可选；策划备注；**不**驱动规则 |
| EffectKind | 效果种类 | `string` | **登记制** PascalCase Token；空=未实现。禁止中文或内联参数。对齐 MagicBook `EffectPayload` |
| EffectParams | 效果参数 | `string` | Token 允许的 `Key=Value\|…`；空=无参/缺省 |
| TriggerHook | 触发钩子 | `string` | 管线插入点枚举；空=未接线 |

**`EffectKind` 编码（固定；对齐 §9.24 `EffectPayload`）：**

| 规则 | 说明 |
|------|------|
| 语法 | 单一 PascalCase Token（`^[A-Za-z][A-Za-z0-9]*$`）；空=未实现 |
| 登记 | Token **必须**出现在下方登记表；未登记 → 运行时空 apply + Warning |
| 一行一 Token | 一行恰好一个 Token；多效果用多个 SkillEffect 行 / 多个技能 |
| 可复用 | 多个 `SkillEffectId` 可共用同一 Token，用 `EffectParams` 区分数值 |
| 与展示分离 | `SkillConfig.DisplayName` / `Description` / `ExtraActivationCondition` **不**驱动规则 |

**`EffectParams` 编码（固定）：** `Key=Value` 或 `Key=Value|Key=Value|…`（管道分隔）。

| 规则 | 说明 |
|------|------|
| 空 | 无参 / 该 Token 全用缺省 |
| Key | PascalCase；**必须**为该 Token 登记表允许的 Key；未知 Key → Warning 并忽略该对 |
| Value | 加载 trim；禁止含 `=` 或 `\|`（不设转义） |
| 重复 Key | 配置错误：Warning，**后者覆盖**（Demo） |
| 缺必填 Key | 该效果记为未实现/无效：空 apply + Warning |

**`TriggerHook` 枚举（可增补；新增须先改本表）：** `OnOutgoingDamageSettle` \| `OnIncomingDamageSettle` \| `OnWarriorAaHitConfirm` \| `OnWarriorTargetAcquired` \| `OnWarriorWouldDie` \| `OnProjectileHit` \| `OnSkillInternalCooldown`。

**`EffectKind` 登记表（权威；新增 Token 须先改本表再填行/写 Handler）：**

| Token | TriggerHook | 允许 Params（示例） | 对应技能 |
|-------|-------------|---------------------|----------|
| `OutgoingMulOnNewTargetFirstHit` | `OnOutgoingDamageSettle` | **必填** `Mul` | Skill_04 |
| `CheatDeathInvincible` | `OnWarriorWouldDie` | **必填** `InvincibleSeconds` | Skill_05 |
| `OnAaHitChanceAoeStun` | `OnWarriorAaHitConfirm` | **必填** `Chance`,`Radius`,`StunSeconds` | Skill_06 |
| `OnAaHitAoeSlow` | `OnWarriorAaHitConfirm` | **必填** `Radius`,`SlowMoveMul`,`SlowAttackMul`,`DurationSeconds`；可选 `InternalCooldownSeconds`（缺省读 `SkillConfig.BaseCooldownSeconds`） | Skill_07 |
| `OutgoingMulVsMonsterType` | `OnOutgoingDamageSettle` | **必填** `MonsterType`,`Mul` | Skill_08 |
| `StackingOutgoingMulTimed` | `OnSkillInternalCooldown`（叠层）+ `OnOutgoingDamageSettle`（应用） | **必填** `StackBonus`,`MaxTotalBonus`,`TickSeconds` | Skill_09 |
| `RangedPierceExtraHits` | `OnProjectileHit` | **必填** `ExtraHitCount`,`DamageMul` | Skill_10 |
| `OnAaHitApplyBurn` | `OnWarriorAaHitConfirm` | **必填** `TickDamageMul`,`TickIntervalSeconds`,`DurationSeconds`；可选 `StackMode`（缺省 `RefreshDuration`） | Skill_11 |
| `RetargetFarthestTeleportBehind` | `OnWarriorTargetAcquired` | （无；CD 读 `SkillConfig.BaseCooldownSeconds`） | Skill_12 |

```
SkillEffectConfig {
  SkillEffectId: Id
  Notes: string
  EffectKind: string              // registered token | empty
  EffectParams: string            // Key=Value|Key=Value|… | empty
  TriggerHook: string             // enum | empty
}
```

**Mode2 样例 FK（`Skill_01`～`Skill_03`，D-069 硬映射；EffectKind 空）：**

| SkillEffectId | Notes | EffectKind | EffectParams | TriggerHook |
|---------------|-------|------------|--------------|-------------|
| `SkillEffect_01_1`～`_5` | 格挡 Lv1～5：10%～30% 伤害→0 | （空） | （空） | （空） |
| `SkillEffect_02_1`～`_5` | 舒适 Lv1～5：满血 +5%～+25% | （空） | （空） | （空） |
| `SkillEffect_03_1`～`_5` | 连发 3 次；BaseCD 50→10s | （空） | （空） | （空） |

**Mode2 样例 FK（`Skill_04` 先发制人）：**

| SkillEffectId | Notes | EffectKind | EffectParams | TriggerHook |
|---------------|-------|------------|--------------|-------------|
| `SkillEffect_04_1` | 先发制人 Lv1：新目标首击 +20% | `OutgoingMulOnNewTargetFirstHit` | `Mul=1.2` | `OnOutgoingDamageSettle` |
| `SkillEffect_04_2` | +30% | 同 | `Mul=1.3` | 同 |
| `SkillEffect_04_3` | +40% | 同 | `Mul=1.4` | 同 |
| `SkillEffect_04_4` | +50% | 同 | `Mul=1.5` | 同 |
| `SkillEffect_04_5` | +60% | 同 | `Mul=1.6` | 同 |

**Mode2 样例 FK（`Skill_05` 坚挺）：**

| SkillEffectId | Notes | EffectKind | EffectParams | TriggerHook |
|---------------|-------|------------|--------------|-------------|
| `SkillEffect_05_1` | 坚挺 Lv1：HP=1 + 无敌 1s；BaseCD=60 | `CheatDeathInvincible` | `InvincibleSeconds=1` | `OnWarriorWouldDie` |
| `SkillEffect_05_2` | 无敌 2s | 同 | `InvincibleSeconds=2` | 同 |
| `SkillEffect_05_3` | 无敌 3s | 同 | `InvincibleSeconds=3` | 同 |
| `SkillEffect_05_4` | 无敌 4s | 同 | `InvincibleSeconds=4` | 同 |
| `SkillEffect_05_5` | 无敌 5s | 同 | `InvincibleSeconds=5` | 同 |

**Mode2 样例 FK（`Skill_06` 震晕）：**

| SkillEffectId | Notes | EffectKind | EffectParams | TriggerHook |
|---------------|-------|------------|--------------|-------------|
| `SkillEffect_06_1` | 震晕 Lv1：10% AOE 击晕 1s；半径 1.5 | `OnAaHitChanceAoeStun` | `Chance=0.1\|Radius=1.5\|StunSeconds=1` | `OnWarriorAaHitConfirm` |
| `SkillEffect_06_2` | 击晕 2s | 同 | `Chance=0.1\|Radius=1.5\|StunSeconds=2` | 同 |
| `SkillEffect_06_3` | 击晕 3s | 同 | `Chance=0.1\|Radius=1.5\|StunSeconds=3` | 同 |
| `SkillEffect_06_4` | 击晕 4s | 同 | `Chance=0.1\|Radius=1.5\|StunSeconds=4` | 同 |
| `SkillEffect_06_5` | 击晕 5s | 同 | `Chance=0.1\|Radius=1.5\|StunSeconds=5` | 同 |

**Mode2 样例 FK（`Skill_07` 冰冻）：**

| SkillEffectId | Notes | EffectKind | EffectParams | TriggerHook |
|---------------|-------|------------|--------------|-------------|
| `SkillEffect_07_1` | 冰冻 Lv1：AOE 减速 2s；内置 CD 10s | `OnAaHitAoeSlow` | `Radius=1.5\|SlowMoveMul=0.5\|SlowAttackMul=0.5\|DurationSeconds=2\|InternalCooldownSeconds=10` | `OnWarriorAaHitConfirm` |
| `SkillEffect_07_2` | 持续 3s | 同 | …`DurationSeconds=3`… | 同 |
| `SkillEffect_07_3` | 持续 4s | 同 | …`DurationSeconds=4`… | 同 |
| `SkillEffect_07_4` | 持续 5s | 同 | …`DurationSeconds=5`… | 同 |
| `SkillEffect_07_5` | 持续 6s | 同 | …`DurationSeconds=6`… | 同 |

**Mode2 样例 FK（`Skill_08` 精英克制）：**

| SkillEffectId | Notes | EffectKind | EffectParams | TriggerHook |
|---------------|-------|------------|--------------|-------------|
| `SkillEffect_08_1` | 精英克制 Lv1：Elite +50% | `OutgoingMulVsMonsterType` | `MonsterType=Elite\|Mul=1.5` | `OnOutgoingDamageSettle` |
| `SkillEffect_08_2` | +60% | 同 | `MonsterType=Elite\|Mul=1.6` | 同 |
| `SkillEffect_08_3` | +70% | 同 | `MonsterType=Elite\|Mul=1.7` | 同 |
| `SkillEffect_08_4` | +80% | 同 | `MonsterType=Elite\|Mul=1.8` | 同 |
| `SkillEffect_08_5` | +90% | 同 | `MonsterType=Elite\|Mul=1.9` | 同 |

**Mode2 样例 FK（`Skill_09` 渐入佳境）：**

| SkillEffectId | Notes | EffectKind | EffectParams | TriggerHook |
|---------------|-------|------------|--------------|-------------|
| `SkillEffect_09_1` | 渐入佳境 Lv1：+3%/10s，cap 60% | `StackingOutgoingMulTimed` | `StackBonus=0.03\|MaxTotalBonus=0.6\|TickSeconds=10` | `OnSkillInternalCooldown` |
| `SkillEffect_09_2` | +5%/层 | 同 | `StackBonus=0.05\|MaxTotalBonus=0.6\|TickSeconds=10` | 同 |
| `SkillEffect_09_3` | +7%/层 | 同 | `StackBonus=0.07\|MaxTotalBonus=0.6\|TickSeconds=10` | 同 |
| `SkillEffect_09_4` | +9%/层 | 同 | `StackBonus=0.09\|MaxTotalBonus=0.6\|TickSeconds=10` | 同 |
| `SkillEffect_09_5` | +12%/层 | 同 | `StackBonus=0.12\|MaxTotalBonus=0.6\|TickSeconds=10` | 同 |

**Mode2 样例 FK（`Skill_10` 贯穿）：**

| SkillEffectId | Notes | EffectKind | EffectParams | TriggerHook |
|---------------|-------|------------|--------------|-------------|
| `SkillEffect_10_1` | 贯穿 Lv1：再穿 1 名；100% | `RangedPierceExtraHits` | `ExtraHitCount=1\|DamageMul=1` | `OnProjectileHit` |
| `SkillEffect_10_2` | 再穿 2 | 同 | `ExtraHitCount=2\|DamageMul=1` | 同 |
| `SkillEffect_10_3` | 再穿 3 | 同 | `ExtraHitCount=3\|DamageMul=1` | 同 |
| `SkillEffect_10_4` | 再穿 4 | 同 | `ExtraHitCount=4\|DamageMul=1` | 同 |
| `SkillEffect_10_5` | 再穿 5 | 同 | `ExtraHitCount=5\|DamageMul=1` | 同 |

**Mode2 样例 FK（`Skill_11` 灼烧）：**

| SkillEffectId | Notes | EffectKind | EffectParams | TriggerHook |
|---------------|-------|------------|--------------|-------------|
| `SkillEffect_11_1` | 灼烧 Lv1：20% NAP/1s × 2s；叠时 | `OnAaHitApplyBurn` | `TickDamageMul=0.2\|TickIntervalSeconds=1\|DurationSeconds=2\|StackMode=RefreshDuration` | `OnWarriorAaHitConfirm` |
| `SkillEffect_11_2` | 持续 3s | 同 | …`DurationSeconds=3`… | 同 |
| `SkillEffect_11_3` | 持续 4s | 同 | …`DurationSeconds=4`… | 同 |
| `SkillEffect_11_4` | 持续 5s | 同 | …`DurationSeconds=5`… | 同 |
| `SkillEffect_11_5` | 持续 6s | 同 | …`DurationSeconds=6`… | 同 |

**Mode2 样例 FK（`Skill_12` 瞬移）：**

| SkillEffectId | Notes | EffectKind | EffectParams | TriggerHook |
|---------------|-------|------------|--------------|-------------|
| `SkillEffect_12_1` | 瞬移 Lv1：最远敌背后；BaseCD=60 | `RetargetFarthestTeleportBehind` | （空） | `OnWarriorTargetAcquired` |
| `SkillEffect_12_2` | BaseCD=50 | 同 | （空） | 同 |
| `SkillEffect_12_3` | BaseCD=40 | 同 | （空） | 同 |
| `SkillEffect_12_4` | BaseCD=30 | 同 | （空） | 同 |
| `SkillEffect_12_5` | BaseCD=20 | 同 | （空） | 同 |

#### 9.22 推图战配置表 `PushMapGameplayConfig`

规则语义：[SPEC_03 §3.14](SPEC_03_GameRules.md)。一行 = 一个推图战关卡配置。

**磁盘名：**
- **Excel：** `推图战_推图战配置表_PushMap_PushMapGameplayConfig.xlsx`
- **CSV：** `PushMap_PushMapGameplayConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | 主键；被 `LevelOperationConfig` / ModeSelect 引用 |
| MapId | 地图编号 | `string` | Prefab 逻辑名；合法值 **`Ground_01`…`Ground_05`** 或 **`PushMap_*`**；解析 → `Assets/Prefabs/Maps/{MapId}.prefab`；**≠** LevelId |
| DisplayName | 关卡显示名 | `string` | 展示名或本地化 Key |
| StageExpReward | 阶段经验奖励 | `int` | BOSS 通关入账 `LifetimeExperience` 的数值；Demo 可固定 |
| CaptureLoot | 占领默认掉落 | 见编码 | 可选；各目标点占领时发放（若目标点无独立覆盖）；编码为 `ItemId;Count\|…`；其中 `ItemId` **必须存在于** [§9.5a 道具汇总表 `ItemCatalogConfig`](#95a-道具汇总表-itemcatalogconfig)；**不是**坟墓品质表 `DropMode` / `Id;Weight;Count` |
| DungeonUnlockIds | 副本解锁ID列表 | 见编码 | 通关或占领写入存档钩子；段分隔 `\|`；空=无；**副本玩法 TBD** |
| CaptureSeconds | 占领所需秒 | `float` | **加载缺省 5**（列缺失或空）；判定圈连续无怪秒数；< 0 → 加载失败 |
| Notes | 备注 | `string` | 可选 |

```
PushMapGameplayConfig {
  GameplayConfigId: Id
  MapId: Ground_01 | PushMap_*   // Prefabs/Maps/{MapId}.prefab
  DisplayName: string
  StageExpReward: int
  CaptureLoot: "ItemId;Count|ItemId;Count|..."
  DungeonUnlockIds: "DungeonId|DungeonId|..."
  CaptureSeconds: number         // default 5
}
```

**地图 Prefab 标记契约（与 §13 配套；方案 A / PM-01）：**

- **脚本：** `Assets/Scripts/Gameplay/PushMap/`，命名空间 `Gravedigger2026.Gameplay.PushMap`
- **样例地图：** `Assets/Prefabs/Maps/PushMap_Demo_01.prefab`（`MapId=PushMap_Demo_01`）；由 Editor 菜单 `Gravedigger2026/PushMap/Ensure Sample Map Prefab` 从 `Ground_01` 复制并挂齐标记；**不**改写 Dig/Defend 共用的 `Ground_*`。Mode2 关卡 2/3 另用作者图 `PushMap_Demo_02` / `PushMap_Demo_03`（`PushMapGameplayConfig`：`PushMap_02`/`PushMap_03`）
- **运行时绑定：** `PushMapStageController` 经 `DefendPrefabCatalog.TryGetMap(MapId)` 取 Prefab，**不**扫描 `Prefabs/Maps/` 文件夹；`DefendPrefabCatalog.Maps` 与 `DefendAssetBuilder.CatalogExtraMapIds` 须覆盖所有被引用的 `PushMap_Demo_*`（含 01–03）。菜单 `Gravedigger2026/PushMap/Ensure Catalog Map Binding` 幂等补绑，不重生 Demo_02/03
- **复用：** `EngageZone`（`Gameplay.Defend`）、`WalkSurface`（`WalkSurfaceIsoDiamond` / `Gameplay.Maps`）；地面仍为 Isometric Tilemap（可用 Tile Palette）
- **本片边界：** 仅标记契约与可 Instantiate 样例；占领/刷怪/空气墙 NavMesh **不做**（PM-04/05/08）

| 组件 | 字段（SerializeField） | 说明 |
|------|------------------------|------|
| `ObjectivePoint` | `ObjectiveOrder:int`（≥1）；引用 `CaptureZone` | 有序目标点；`CaptureZone` 可同节点或子节点 |
| `CaptureZone` | `Radius:float` 默认 **2** | XZ 圆形判定圈；`ContainsXZ`；半径可改 |
| `AirWall` | `HalfExtents:Vector3`（薄墙占位） | 阻挡体作者点；**Y 轴欧拉角**支持 0°/45°/90°…；运行时阻挡见下「空气墙 NavMesh 契约（PM-08）」 |
| `SpawnPoint` | `SpawnPointId:string` | 与 `PushMapSpawnConfig.SpawnPointId` 匹配 |
| `TrapZone` | `TrapZoneId:string`；`Radius:float` | XZ 圆形区域占位；与刷怪表 `TrapZoneId` 匹配 |
| `BossPoint` | （无必填字段；位置=`Transform`） | BOSS 生成点占位；通关逻辑见后续片 |
| `CameraFollowPath` | `_bakedPoints:Vector3[]`（局部 XZ）；子 `CameraPathWaypoint` | 镜头虚拟推进轨；作者摆 ≥2 有序路点；Bake = 相邻路点**世界 XZ 直线**按间距采样 + `InverseTransformPoint` 存局部 XZ（**不用** NavMesh / 轴对齐格心 A*）；拐弯由作者路点表达；直线可穿 AirWall；作者点可 Snap 到 Grid；Gizmo 画折线；开战空烘焙则补 Bake |

```
// Prefab marker components (authoring; no capture/spawn runtime in PM-01)
ObjectivePoint { ObjectiveOrder: int>=1; CaptureZone }
CaptureZone    { Radius: number = 2 }          // XZ circle
AirWall        { HalfExtents: Vector3 }      // Transform.eulerAngles.y = 0|45|90|…
SpawnPoint     { SpawnPointId: string }
TrapZone       { TrapZoneId: string; Radius: number }
BossPoint      { }                           // transform.position
CameraFollowPath { BakedPoints: Vector3[] }  // child CameraPathWaypoint ordered
CameraPathWaypoint { Order: int>=1 }
// + EngageZone, WalkSurface (existing)
```

**占领运行时契约（方案 A / PM-04）：**

- **规则归属：** 目标链与计时在 `PushMapSessionService`（`Core/PushMap/`）；`ObjectivePoint`/`CaptureZone` 仅标记，运行时不自管 Tick
- **Session 面：** `CaptureSecondsRequired`（`Config.CaptureSeconds`，钳 ≥0.01）；`CurrentObjectiveOrder`（未开或全占=0）；`IsObjectiveCaptured(order)`；`TryBeginObjectiveChain(IEnumerable<int> orders)`（开战调用；排序去重 → 最小未占）；`TickCapture(float dt, bool hasLivingMonsterInCurrentZone)`（有怪清零；否则累计；达标→标记已占+事件+切下一目标）
- **事件：** `ObjectiveCaptured(int order)`（停刷钩子 → PM-05）；`CurrentObjectiveChanged(int newOrder)`（推进/表现）
- **表现：** `PushMapStageController` Combat 收 `ObjectivePoint` 排序喂 Session；每秒 `Update` 探测当前圈内存活怪（默认扫描 `_monsters`：`IsAlive && CaptureZone.ContainsXZ(position)`；Rebel 不算阻挡）；探测经 `PushMapMonsterPresenceProbe`（同目录薄组件，可注入/重置验收占位）；占领日志+HUD 状态
- **推进（MP-04 / 方案 B）：** 忠诚士兵共享 `CurrentObjective` → `FlowFieldService` 单场；`PushMapAdvanceView` 采样场方向 + `MassMoveScheduler`/`LocalDetour` 友军绕行后 `NavMeshAgent.Move`（**禁止**每兵每帧 `SetDestination(Objective)`）；进入当前 `CaptureZone` 后停跟场中心、软分离守备（`ObjectiveArriveRadius`）；圈内有存活怪**不**暂停推进（探测仅喂 `TickCapture`）；Rebel 不推进
- **追击/交战（MP-05 / 方案 B）：** 忠诚兵遇敌检测内（中心距 ≤ `max(武器触及, 该怪 AlertRadius)`）→ `GoalKind=AttackSlot`（`AttackSlotService.TryClaim`）+ LocalDetour，停跟 Objective 场。**v0.82.57：** 已进 `AttackRange`（XZ）→ 停步挥刀；未进距 → 目的地=更近进距槽或内收点。离开后释放槽恢复 `Objective`。无空闲槽且未进距 → 保持 `Objective` 跟场（不硬暂停）。怪物追击目的地同为认领槽（非目标中心）；进距同样停步。`MassMoveScheduler.SetGoal`；槽位重算每帧 ≤50 轮转；死亡/`Release`/`ReleaseAllForTarget`
- **Demo 击杀（命中 polish 后置）：** 忠诚兵中心距任意存活怪 ≤ `max(怪 AttackRange, 士兵 AttackRange) + ArriveEpsilon` → `NotifyKilled`；BOSS 另 `TryNotifyBossKilled`
- **Defend 对等（MP-06 / 方案 B）：** `DefendStageController` 持有同一套 `MassMoveScheduler`+`AttackSlotService`；`WarriorAgentView` Engage 内追击→`AttackSlot`，无候选→`GoalKind=FormationHome`（直趋+LocalDetour；返回途中继续选敌即中断）；`MonsterAgentView` 追击走槽位；无全员每帧 `SetDestination`/`CalculatePath`；与 PushMap 目的地语义一致
- **FlowField 重建：** 开战 Bake（AirWall→`StaticBoxWalkableMask`）后 `Configure`+`Rebuild`；`CurrentObjectiveChanged` → 再 `Rebuild`（日志可验单次 RebuildCount）；同目标单位共享一场
- **开战顺序（表现）：** Bake NavMesh（含 AirWall）→ 建可走掩码+FlowField → `TryBeginObjectiveChain` → 部署忠诚兵（注册 Scheduler）→ `FireStartBattleSpawns`（怪注册 Scheduler+AttackSlot）→ 失控 roll
- **可走面：** 样例 `PushMap_Demo_01` 的 `DigMapBounds`/`WalkSurface`/`EngageZone` 须覆盖目标点与刷怪点（含 BossPoint）
- **边界：** 占领探测与推进解耦；无怪时连续 `CaptureSeconds` → 1→2 切换；停刷仅事件/日志；占领奖励/副本解锁见 PM-07；完整命中 polish 后置；压测入口见 MP-07 / §9.7

```
// PM-04 / MP-04+MP-05+MP-06 rules/presentation touchpoints
session.TryBeginObjectiveChain(sortedOrders)
session.TickCapture(dt, probe.HasLivingMonster(currentZone))
event ObjectiveCaptured(order)   // PM-05 stops linked spawns; PM-07 capture loot/unlock
event CurrentObjectiveChanged(order) → FlowFieldService.Rebuild(goal, airWallMask)
PushMapMonsterPresenceProbe { HasLivingMonster(CaptureZone) }  // capture timer only
FlowFieldService + StaticBoxWalkableMask(AirWall OBBs)
AttackSlotService.TryClaim / Release / ReleaseAllForTarget
MassMoveScheduler.SetGoal(Objective|AttackSlot|FormationHome) + Tick  // ≤50 steer + ≤50 slot refresh/frame
PushMapAdvanceView { field or AttackSlot Move; no SetDestination; rebel skip }
PushMapMonsterAgentView { AttackSlot chase via scheduler; AggroMode preserved }
DefendStageController { warriors+monsters on same scheduler; FormationHome return }
WarriorAgentView / MonsterAgentView { AttackSlot or FormationHome Move; no center SetDestination }
```

**BOSS 通关与奖励运行时契约（方案 A / PM-07 + UI-017/018）：**

- **规则归属：** 胜负与待击杀 BOSS 计数、战斗耗时、击杀数、CaptureLoot ledger、忠诚全灭检测在 `PushMapSessionService`；`BossPoint` 仅位置标记
- **计数：** `FireRow` 且 `IsBoss` → `_pendingBossCount += SpawnCount`；开战后若 `_pendingBossCount>0` 且表现层报告无 `BossPoint` → warn（一致性约定）
- **开战统计：** `TryStartBattle` 记录 `CombatStartRealtime`、清零 `MonstersKilled` / CaptureLoot ledger；`MonsterKilled` → `MonstersKilled++`
- **击杀：** `TryNotifyBossKilled()`（Combat 且未结算）→ 递减；归零 → `Ended` + `VictorySettled(Config.StageExpReward)` + `IsVictory=true`；通关写 `DungeonUnlockIds`
- **失败：** `Shield≤0` 或（已登记战士≥1 且无 `!IsRebel && !IsCombatDead`）→ `RequestLevelFailure` → `LevelFailureRequested`；**禁止**再发 `VictorySettled`；`IsVictory=false`
- **叛变：** 表现层 roll 后 `SetWarriorRebel(id,true)` 写入规则态，再 `TryEvaluateLoyalWipe`
- **占领奖励：** `Capture` 时解析 `Config.CaptureLoot` → 先命中 `ItemCatalogConfig` → 再按 `ItemType` 分发（仓库 / 精魂 / 魔法书 / 主角装备）；`RecordCaptureLoot` 累加展示 ledger；写 `DungeonUnlockIds`；**不**加经验
- **表现：** 订阅胜负 → 入账 Exp（仅胜）→ 显示 UI-017；Continue 路由 UI-018 / LevelSelect；**延迟** `_onVictoryAdvance` / `_onLevelFailure` 至最终 Continue

```
// PM-07 + settlement UI touchpoints
session.TryNotifyBossKilled()
session.SetWarriorRebel / TryEvaluateLoyalWipe
session.RecordCaptureLoot(entries)
event VictorySettled(stageExp)
event LevelFailureRequested
CombatElapsedSeconds / MonstersKilled / CaptureLootLedger / IsVictory
PushMapBattleSettlementView / PushMapRewardPopupView
```

**空气墙 NavMesh 契约（方案 A / PM-08）：**

- **权威阻挡：** 开战 Runtime Bake（复用 `DefendNavMeshBaker`）在 IsoDiamond 可走 `Mesh` source 之外，收集地图 `AirWall`，追加 `NavMeshBuildSource`：`shape=Box`、`size=HalfExtents×2`、`transform=TRS(position, rotation, 1)`、`area=Not Walkable`（`NavMesh.GetAreaFromName`；失败回退 `1`）
- **旋转：** Prefab `Transform.eulerAngles.y` 支持 0°/45°/90°…；样例 `AirWall_45` 为 45°
- **作用对象：** 敌我士兵与怪物凡走 `NavMeshAgent` 的路径均不可穿（含 FlowField 掩码阻挡推进方向；怪物追击仍走 NavMesh）；AirWall 同时写入 `StaticBoxWalkableMask` 供 FlowField
- **接线：** `PushMapStageController` StartBattle 收集 `GetComponentsInChildren<AirWall>` → `Bake(..., notWalkableBoxes)` + 同批 OBB 建 FlowField 掩码
- **边界：** **不做** `NavMeshObstacle` Carve、复杂多层障碍 polish；Defend 无 AirWall 时仍走无障碍重载

```
// PM-08 bake touchpoints
DefendNavMeshBaker.Bake(center, halfExtents, notWalkableBoxes)
NavMeshBoxObstacle { Center, Size=HalfExtents*2, Rotation }
AirWall { HalfExtents }  // authoring; Y euler 0|45|90|…
```

#### 9.23 推图战刷怪配置表 `PushMapSpawnConfig`

规则语义：[SPEC_03 §3.14](SPEC_03_GameRules.md)。一行 = 某关卡某刷怪点的一次刷怪定义；同点可多行（多种怪物）。

**磁盘名：**
- **Excel：** `推图战_刷怪配置表_PushMap_PushMapSpawnConfig.xlsx`
- **CSV：** `PushMap_PushMapSpawnConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | FK → `PushMapGameplayConfig` |
| SpawnPointId | 刷怪点编号 | `string` 或 `int` | 与地图 Prefab `SpawnPoint` 标记匹配 |
| MonsterId | 怪物ID | `string` 或 `int` | FK → `MonsterConfig` |
| SpawnCount | 数量 | `int` | ≥ 1 |
| LinkedObjectiveOrder | 关联目标点序号 | `int` | 可选；该目标占领后本行停刷；空/0=开战即符合「未占领」语义的全局点 |
| TrapZoneId | 陷阱区域编号 | `string` 或 `int` | 可选；空=无陷阱，开战且关联目标未占领时刷；非空=忠诚士兵进入该陷阱才刷 |
| IsBoss | 是否BOSS | `bool` / `0\|1` | 1=该刷怪为 BossPoint 通关目标（须与地图 BossPoint 一致） |
| SpawnOrder | 同点出怪顺序 | `int` | 同 SpawnPointId 多行时升序 |

```
PushMapSpawnConfig {
  GameplayConfigId: Id
  SpawnPointId: Id
  MonsterId: Id
  SpawnCount: int
  LinkedObjectiveOrder: int      // 0 = global / start-eligible
  TrapZoneId: Id | ""            // empty = non-trap StartBattle spawn
  IsBoss: 0 | 1
  SpawnOrder: int
}
```

**刷怪运行时契约（方案 A / PM-05）：**

- **规则归属：** 刷怪资格与触发状态在 `PushMapSessionService`（`Core/PushMap/`）；`SpawnPoint`/`TrapZone` 仅作者标记，运行时不自管判定
- **装载：** `TryStartBattle` 时装载本 `GameplayConfigId` 全部行；**不**立即刷怪。表现层在 Bake NavMesh + 部署完成后调用 `FireStartBattleSpawns()`：无陷阱且关联目标未占的行 → 发 `PushMapSpawnRequested`（同 `SpawnPointId` 按 `SpawnOrder` 升序）；陷阱绑定点进入 `Pending` 待触发
- **事件：** `PushMapSpawnRequested(PushMapSpawnRequest)`；负载含 `SpawnPointId` / `MonsterId` / `SpawnCount` / `LinkedObjectiveOrder` / `IsBoss` / `SpawnOrder` / `Trigger`（`StartBattle` / `Trap`）；**位置由 View 按 `SpawnPointId` / `BossPoint` 解析**，再按 `MonsterConfig.BodyRadius` 与场上存活怪占地圆做散开（环形/螺旋候选 → 局部 `NavMesh.SamplePosition` + `basePos` 牵引；PM-10 / v0.73.9）
- **陷阱触发：** `TryNotifyTrapEnter(trapZoneId)`（View 探测忠诚兵首次进圈）→ 未占领 + 未触发 → 该 `SpawnPointId` 全部行触发；每点本场仅一次
- **占领停刷：** `ObjectiveCaptured(order)` → 标记 `LinkedObjectiveOrder == order` 的点本场停刷；已触发怪的生死不受影响；`IsBoss=1` 的行 **不** 受占领停刷（BOSS 见 PM-07）
- **表现：** `PushMapStageController` 收集 `SpawnPoint`（`SpawnPointId`→`Transform.position`）与 `TrapZone`；订阅事件 Instantiate 怪 → `PushMapMonsterAgentView`（入 `_monsters`；Boss 用 `BossPoint` 位置；落点经 `PushMapSpawnSpread`）；Update 探测忠诚（`!IsRebel`）`PushMapAdvanceView` 首次进入 `TrapZone` → `TryNotifyTrapEnter`
- **占地与避障（PM-10 / v0.73.9）：** `PushMapSpawnSpread`：采样半径 ≈`max(0.75, BodyRadius×2.5)`；命中相对 `basePos` 须 ≤ 牵引上限（环/螺旋半径+余量；绝对 ≈`max(3, BodyRadius×10)`）；越界失败；挤满重叠回退基点。Bind 时 Warp 同局部半径 ≈`max(1, BodyRadius×3)`；`_agent.radius = min(BodyRadius, max(0.05, AttackRange − 士兵Demo半径0.1 − 0.05))`；刷出散开仍用完整 `BodyRadius`；移动怪靠 NavMesh RVO 互避；`Stationary*` 不主动挪位；Defend 刷怪落点散开本片 **不做**；**我方士兵** Demo：`NavMeshAgent.radius=0.1`、`height=0.1`（`WarriorAgentView` / `PushMapAdvanceView`）
- **Demo 遇敌→AttackSlot（MP-05 / v0.82.55 方案 C）：** `PushMapAdvanceView` 在忠诚兵中心距存活怪 ≤ `max(武器触及, 该怪 AlertRadius)` 时改 `GoalKind=AttackSlot`（认领环上槽 + LocalDetour `Move`；**停跟** FlowField）；武器触及 = `max(双方 AttackRange) + 双方 BodyRadius + ArriveEpsilon`。离开后释放槽并恢复 `Objective`；无空闲槽保持 `Objective` 跟场。**不是**全图 EngageZone。怪物追击同样认领槽，**禁止**全员每帧 `SetDestination`/`CalculatePath` 到目标中心。命中走 PM-12 方案 D（须已认领该怪 AttackSlot 且进入 `AttackRange`）。**D-069 SkillCast：** 忠诚兵 `Skill_03` 在 CD 好且已进距时占用该攻击通道连续 3 次方案 D；`Skill_01` 格挡为独立被动钩子（怪物普攻命中前 roll，成功伤害→0 仍判命中）；`Skill_02` 舒适为独立 Outgoing 倍率钩子（满血时 NAP×(1+5%～25%)，连发每击独立检查）；规则在 `PushMapSessionService` 提交 CD / 失控二次 roll / 格挡 / 舒适；View 不改选敌/回阵。**D-073：** Session 在既有结算点 `SkillEffectPipeline.Dispatch` + `CombatStatusService.Tick`；禁止 `SkillId` 分支；CombatSkillIcon 仍走 `SkillIconPopup` / `SkillPersistChanged`。**SE-07：** 远程 `TryConfirmRangedHit` 带 `ProjectileHitFlightContext` 时 `Dispatch(OnProjectileHit)`；`ProjectileView` 通用穿透（命中后保持当前速度方向；Handler 写 `ExtraHitsRemaining`）
- **CombatSkillIcon（D-071 / 方案 A）：** `PushMapSessionService` 发 `SkillIconPopup(warriorId, skillId)`（`TryCommitSkillBurst` 成功 → `Skill_03`；格挡成功 → `Skill_01`；`Skill_02` 生效瞬间）与 `SkillPersistChanged(warriorId, skillId, on)`（开战满血持有 `Skill_02` → on；HP 从满血变未满 → off；未满回到满血 → on+Popup）。规则不碰 Transform。`PushMapStageController` 解析士兵 View → `WarriorSkillIconHudView`（Prefab `Assets/Prefabs/PushMap/SkillIconHud.prefab`；`worldSize = pixelSize × 2 × camera.orthographicSize / Screen.height`；头顶 35px / 脚下 20px）。`CombatDead` 立即 Clear。Defend 不接线
- **怪物 AI 边界：** 本片怪用 Defend 默认追击语义（就近忠诚兵/主角；进 `AttackRange` 普攻；对主角 `ApplyShieldHit`；对兵日志）；AggroMode 四态后置 **PM-06**（见下「AggroMode 运行时契约」）；`MonsterAgentView` 仍绑定 `DefendSessionService` 不接入
- **AggroMode 运行时契约（PM-06）：** 四态在 `PushMapMonsterAgentView` 按 `config.AggroMode` 分支；`ActiveChase`：忠诚士兵进 `AlertRadius`→移动追击该兵直至怪死；`PassiveChase`：`_provoked=false` 静止，`NotifyProvoked()` 后移动追击；`StationaryActive`：永不移动，忠诚兵进 `AttackRange` 攻击、离开停；`StationaryPassive`：永不移动，须先 `NotifyProvoked()` 且目标仍在 `AttackRange` 才攻。主动发现与挑衅均**仅**对忠诚士兵（`!IsRebel`）。挑衅来源（Demo）：`PushMapStageController` 检测忠诚 `PushMapAdvanceView` 首次进入某被动怪 `AttackRange` → 调其 `NotifyProvoked()`（等效「士兵先攻击」；士兵 HP 后置）。命中仍 `AttackMode` 方案 D；主动态对主角不进 `AlertRadius` 主动发现（仅忠诚兵），但已被追击/就近规则命中主角时仍 `ApplyShieldHit`。普通怪真实士兵伤害 **不做**
- **边界：** 不使用 `WaveSpawnConfig` 倒计时；BOSS 通关 / 经验 / 占领奖励 / 副本解锁钩子见 §9.22 PM-07 契约

```
// PM-05 rules/presentation touchpoints
session.TryNotifyTrapEnter(trapZoneId)        // view first loyal-enter
event PushMapSpawnRequested(request)          // StartBattle | Trap
PushMapSpawnRequest { SpawnPointId, MonsterId, SpawnCount,
                      LinkedObjectiveOrder, IsBoss, SpawnOrder, Trigger }
PushMapMonsterAgentView { Bind(MonsterConfigRow, protagonist, warriors, onHitProtagonist) }
```

#### 9.24 魔法书配置表 `MagicBookConfig`

规则语义：[SPEC_03 §3.15](SPEC_03_GameRules.md) 魔法书 / 特殊装备槽 / EffectPhase。一行 = 一种魔法书。已实现首个具体效果「还原」。**v0.80.8：** 增列 `EffectParams`；`EffectPayload` 改为登记制 Token。

**磁盘名：**
- **Excel：** `制造_魔法书配置表_Manufacture_MagicBookConfig.xlsx`
- **CSV：** `Manufacture_MagicBookConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| MagicBookId | 魔法书ID | `string` 或 `int` | 主键 |
| IsUnique | 是否唯一 | `int` / `0\|1` | `1`=同 Id 不可叠装第二本；`0`=默认可叠（各占一槽） |
| IsProbabilistic | 概率型 | `int` / `0\|1` | `1`=概率触发魔法；`0`=无概率；空=0。`ForceClass` 的 `Chance` **真正 roll**；其它 Token 本轮仍不读本列 |
| EffectPhase | 生效环节 | 见编码 | 触发时机；可多值 |
| EffectPayload | 魔法书效果 | `string` | **登记制** PascalCase Token；空=无效果。禁止在本列写中文或内联参数 |
| EffectParams | 魔法书效果参数 | `string` | 与 Token 配套的参数；空=无参/缺省。编码见下 |
| IconAssetId | 魔法书Icon | `string` 或 `int` | UI 图标资源 Id |
| DisplayName | 魔法书名称 | `string` | 展示名；若启用 i18n 可为 Key |
| Description | 魔法书介绍 | `string` | 展示文案 |
| VisualStyleId | 特效外观ID | `string` 或空 | **不是 Token**。空=该书无特效外观。非空 → Catalog `WarriorVisualStyleCatalog` 的 StyleId。`Style_ScaleModel`（别名 `放大模型`）走 **放大通道**（不必有 `.mat`）；其它 Id 走 AllIn1 **材质通道**（`Assets/Materials/AllIn1/` 预设材质）。仅当该书 `EffectPayload` **命中**才烘进实例（skip/miss/无效不写）。**改 Excel 后 Bake Mode2 Tables**；勿只改 CSV。`ClassId` 含逗号时该格须加引号（Bake 会转义）。新增 Style 的材质/Catalog 步骤见 [§15.2](#152-项目落盘目录) |
| VisualPriority | 特效优先级 | `int` | 缺/空=0。仅材质通道：高于当前则覆盖 style 并重置 Intensity；同 StyleId 则累加 Intensity；更低则忽略。放大通道 **忽略**本列。Demo 材质：技能 10、强化 20、进阶 30 |
| VisualIntensityAdd | 特效强度加算 | `float` | 缺/空=**1**。材质通道：覆盖时写入；同 style 累加；表现层 MPB 乘到该预设登记的 float 属性。放大通道：即模型缩放系数 k；命中后 `VisualModelScale *= k`（`k≤0` 视为 1） |

**`EffectPhase` 编码（固定）：** `Phase` 或 `Phase|Phase|…`。本轮枚举至少：`SoldierManufacture` \| `Combat`（Combat 本轮不实现）。

**`EffectPayload` 编码（固定）：**

| 规则 | 说明 |
|------|------|
| 语法 | 单一 PascalCase Token（`^[A-Za-z][A-Za-z0-9]*$`）；空=无效果 |
| 登记 | Token **必须**出现在下方登记表；未登记 → 运行时空 apply + Warning，不当效果 |
| 一书一 Token | 一行恰好一个 Token；多效果用多槽多书，不在本列拼多个 Token |
| 可复用 | 多本 `MagicBookId` 可共用同一 Token，用 `EffectParams` 区分数值 |
| 与展示分离 | `DisplayName` / `Description` **不**驱动规则 |

**`EffectParams` 编码（固定，v0.80.9）：** `Key=Value` 或 `Key=Value|Key=Value|…`（管道分隔，与 `EffectPhase` 同符）。

| 规则 | 说明 |
|------|------|
| 空 | 无参 / 该 Token 全用缺省（如 `RaceWeightPick`） |
| Key | PascalCase；**必须**为该 Token 登记表允许的 Key；未知 Key → Warning 并忽略该对 |
| Value | 加载 trim；禁止含 `=` 或 `\|`（不设转义） |
| 重复 Key | 配置错误：Warning，**后者覆盖**（Demo） |
| 缺必填 Key | 该书记为未实现/无效：空 apply + Warning（具体必填 Key 见登记表） |

**`EffectPayload` 登记表（权威；新增 Token 须先改本表再填行/写 handler）：**

| Token | 适用 Phase | 允许的 EffectParams Key | 语义 |
|-------|------------|-------------------------|------|
| `RaceWeightPick` | `SoldierManufacture` | （无；须空） | 「还原」：制造时按已选头/躯干/臂×2/腿×2 的 RaceId 各权重 1 加权随机定种族 |
| `ForceRace` | `SoldierManufacture` | **必填** `RaceId`（`RaceConfig` 主键） | 强制定稿为该种族；**定稿前探测**；优先于 `RaceWeightPick`；多本按槽左→右后者覆盖；`RaceId` 缺/非法 → 该书无效并忽略 |
| `ForceClass` | `SoldierManufacture` | **必填** `ClassId`（目标，`ClassConfig` 主键）；**可选** `RequireClassId`、`Chance` | 钩子按槽左→右。改写职业：`ClassId` + 重载 `ClassName` / `AttackMode`（`DefaultAppearanceId` / `PlacementOrder` 随新 ClassId 读取）。`RequireClassId`：有则仅当前 `draft.ClassId` 精确匹配才尝试（须在 `ClassConfig`；非法→该书无效）；不匹配→跳过（不当无效）。`Chance`：\[0,1\]；缺省=1；`Random.value < Chance` 才改写；非法/越界→该书无效。缺必填 `ClassId` / 目标非法 → 该书无效，保留当前职业 |
| `StatMul` | `SoldierManufacture` | **必填** `Stat`、`Mul`；**可选** `ClassId` | 钩子按槽左→右。`Mul`≥0；缺/非法 → 该书无效。`Stat`∈`MaxHP`/`MoveSpeed`/`Strength`/`Agility`/`Intelligence`/`All`/`Primary`。可选 `ClassId`：逗号分隔多个主键（OR）；有则仅 `draft.ClassId` **命中任一**才 apply（每个 Id 须在 `ClassConfig`；任一非法→该书无效）；缺省=全兵。五维或 `All`：`Base(S)*=Mul`（All=五维都乘）。`Stat=Primary`：`S`=当前 `ClassConfig.PrimaryStat`；`BodySum(S)=Σ` 已消耗躯体 `StatBonus(S)`（不含种族/宝石/装备，不用已被前书改过的 Base）；`Base(S)+=(Mul−1)×BodySum(S)`；可叠=各书对同一 BodySum 再加一次。写入 Base 后走 StaticStat；持续至 PermanentDeath |
| `StatAdd` | `SoldierManufacture` | **必填** `Stat`、`Add` | 钩子：`Base(S) += Add`。`Stat`∈五维/`All`（**不含** `Primary`）。`Add` 须可解析为数（可负；随后 StaticStat 仍 `max(0,·)`）；缺/非法 → 该书无效 |
| `QualityDelta` | `SoldierManufacture` | **必填** `Delta`（整数，可负） | 外观定稿：`AvgLevelInt = round(mean BodyLevel) + ΣDelta`；不重选料、不改 Base；多本 Delta 相加；`Delta` 缺/非整数 → 该书无效（贡献 0） |
| `SoldierSkillLevelAdd` | `SoldierManufacture` | **必填** `SkillId`、`Delta`（整数，可负） | **二次扫描**（在 `ForceClass` 等第一次钩子与 `DefaultSkillIds` 授予 **之后**）：仅当实例 **已有** 该 `SkillId` 时 `SkillLevel += Delta`；钳制到 `SkillConfig` 中该 Id 存在的最小/最大 `SkillLevel`；无该技能则跳过（不新授）。多本按槽左→右。缺/非法 Key → 该书无效。Mode1 制造不跑本 Token |

**已定义行（示例）：**
- `MagicBook_Restore` | … | `VisualStyleId` 空（手验放大可填 `Style_ScaleModel` / Add=`1.5`） | DisplayName=`还原`
- `MagicBook_WarriorEnhance` | `ClassId=Class_BaseWarrior,Class_Warrior` | `VisualStyleId=Style_WarriorGlow` | `VisualPriority=20` | `VisualIntensityAdd=1` | DisplayName=`战士强化`
- `MagicBook_SoldierSkillLevel` | … | `VisualStyleId=Style_SkillAberration` | `VisualPriority=10` | `VisualIntensityAdd=1` | DisplayName=`士兵技能升级`
- `MagicBook_WarriorAdvance` / `ArcherAdvance` / `MageAdvance` / `RogueAdvance` | … | `VisualStyleId=Style_AdvanceOutline` | `VisualPriority=30` | `VisualIntensityAdd=1`（仅 `ForceClass` **hit**）

```
MagicBookConfig {
  MagicBookId: Id
  IsUnique: 0 | 1
  IsProbabilistic: 0 | 1          // 1 = chance-trigger; ForceClass Chance rolls
  EffectPhase: "SoldierManufacture|Combat|..."
  EffectPayload: string           // registered token | empty
  EffectParams: string            // Key=Value|Key=Value|… | empty
  IconAssetId: Id
  DisplayName: string
  Description: string
  VisualStyleId: Id | ""          // AllIn1 preset or Style_ScaleModel; empty = no visual; not a token
  VisualPriority: int             // default 0; material channel only
  VisualIntensityAdd: number      // default 1; material intensity or scale k
}
```

**主角特殊装备槽（存档意图）：**

- 默认 **6** 槽：`Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.SpecialEquipSlots` — JSON：`MagicBookId[6]`（空槽用空串）
- 装配闸门（规则）：槽未满；若目标书 `IsUnique=1` 且已装备同 Id → 拒绝；**Demo**：表行缺失时仍允许写入以便空表手验持久化（打 Warning；按可叠处理）
- AutoManufacture：**造兵时不套书**（默认定种族 + 授予双手职业 `DefaultSkillIds`）。UI-016 Step2 每槽脉冲峰值：`ApplyEquippedBookAtSlot(warrior, slotIndex)` 仅执行该槽 Token（`RaceWeightPick` / `StatMul` / `ForceClass` / `SoldierSkillLevelAdd`；`ForceClass` 命中 Clear 后重授技能；其它未实现空 apply + 日志）；每槽后 `RefinalizeInstance`；全部完成后按最终 ClassId 上阵。演出失败/`Exit`：`ApplyRemainingSlots`。取消「定稿前探测还原」与「技能二次扫描」
- **实现：** `SpecialEquipSlotsService`（`TryEquip` + `TrySwap` + `TryUnequip` + `Changed`）+ `SoldierManufactureMagicBookHook.ApplyEquippedBookAtSlot` / `ApplyRemainingSlots`；`AutoManufacturePresentationController` 脉冲峰值回调并订阅 `Changed` 刷新共享 `BookRow.prefab`；`MagicBookSlotsPanelView` 弹窗删除（D-072）；`AutoManufactureStageModule` Deploy 延后；MetaShell 进档绑定；手验 Tools「增加魔法书」+ UI-023 拖拽/删除

#### 9.25 主角装备配置表 `ProtagonistEquipmentConfig`

规则语义：[SPEC_03 §3.16](SPEC_03_GameRules.md) 主角装备仓库 / 同 Id 转化 / `EquipCommonExp` / 等级 / Dig 叠加。一行 = 某一 `EquipId` 的某一等级。**与** `MagicBookConfig`（§9.24）、`ExtraEquipmentConfig`（§9.14）、材料 `Warehouse` **并行**。

**磁盘名：**
- **Excel：** `主角_装备配置表_Protagonist_ProtagonistEquipmentConfig.xlsx`
- **CSV：** `Protagonist_ProtagonistEquipmentConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| EquipId | 装备ID | `string` 或 `int` | 复合主键之一 |
| EquipLevel | 装备等级 | `int` | 复合主键之一；从 **1** 起 |
| DisplayName | 装备名称 | `string` | 展示名；若启用 i18n 可为 Key |
| IconAssetId | 装备图标 | `string` 或 `int` | UI 图标资源 Id |
| ExpToNextLevel | 升下一级经验 | `int` | 升到 `EquipLevel+1` 所需；空或 ≤0 → 该行为满级 |
| ConvertExpValue | 转化经验值 | `int` | 再获同 `EquipId` 时转入的经验（满级时改入 `EquipCommonExp`，见 §3.16） |
| EffectDomain | 装备生效功能 | 见编码 | `Dig` \| `SoldierManufacture` \| `Combat`；可多值 |
| EquipEffect | 装备效果 | `string` | Dig 域：与 [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers` 同风格 `Attr_Value\|…`。**静态键**（`DigDamage` / `DigDurationReductionSum` / `DigCursorRadius` / `DigStageDurationBonus` / `GraveSpawnWeightBonus_{QualityId}` / `DigProcessSpawnCountBonus` 等）并入 Dig caps；**事件键** `DigOnGraveClear`（消除坟墓触发概率，`_1`=100%）与 `ExplosiveThrowRadius` / `ExplosiveBlastRadius` / `ExplosiveBlastDamage` / `ExplosiveFlightSec` / `ExplosiveFuseSec` / `ExplosiveRingSec` **不**并入 caps（由 `DigExplosiveEffectConfig` 解析，见 D-077）；`DigLightningIntervalSec` / `DigLightningFrameSec` / `DigLightningPreviewSec` **不**并入 caps（由 `DigLightningEffectConfig` 解析，见 D-078）；`SoldierManufacture` / `Combat` Token 登记表 **TBD**（另立 `ProtagonistEquipEffect`，**不**混用 MagicBook `EffectPayload`）；空 = 无效果 |
| Description | 装备描述 | `string` | 展示文案 |

**`EffectDomain` 编码（固定）：** `Domain` 或 `Domain|Domain|…`。枚举：`Dig` \| `SoldierManufacture` \| `Combat`。

**复合主键规则：** `(EquipId, EquipLevel)` 唯一；同 `EquipId` 的等级行须连续从 1 起（实现加载时可校验并 Warning）。

**Demo 样例行：** `Equip_IronShovel`（铁铲）L1～5，`EffectDomain=Dig`；相对基数 0.6 每级 +10% → L1 `DigCursorRadius_0.06` … L5 `_0.30`；`ExpToNextLevel` L1–4 = 1、L5 空；`ConvertExpValue=1`。`Equip_MinerLamp`（矿灯）L1～5，`EffectDomain=Dig`；每级 Q4/Q5/Q6 生成权重累计 +10 → L1 `GraveSpawnWeightBonus_Q4_10|GraveSpawnWeightBonus_Q5_10|GraveSpawnWeightBonus_Q6_10` … L5 `_50`；升下一级/转化经验均为 1。`Equip_Explosives`（炸药）L1～5，`EffectDomain=Dig`；`DigOnGraveClear_1|ExplosiveThrowRadius_4|ExplosiveBlastRadius_2|ExplosiveBlastDamage_{13/18/23/28/33}|ExplosiveFlightSec_0.5|ExplosiveFuseSec_0.8|ExplosiveRingSec_0.5`。`Equip_Elctr`（引雷）L1～5，`EffectDomain=Dig`；`DigLightningIntervalSec_{15/13/11/9/7}|DigLightningFrameSec_0.05|DigLightningPreviewSec_2`。`Equip_Detector`（探测器）L1～5，`EffectDomain=Dig`；L1 `DigProcessSpawnCountBonus_1` … L5 `_5`（过程生成 M 加法，不改 N）。旧样例 `Equip_DigRing` 已删除。

```
ProtagonistEquipmentConfig {
  EquipId: Id
  EquipLevel: int                 // ≥ 1
  DisplayName: string
  IconAssetId: Id
  ExpToNextLevel: int             // ≤0 or empty = max level row
  ConvertExpValue: int
  EffectDomain: "Dig|SoldierManufacture|Combat|..."
  EquipEffect: "Attr_Value|..."   // Dig static keys → caps; DigOnGraveClear / Explosive* / DigLightning* → event parser
  Description: string
}

OwnedEquip {
  EquipId: Id
  Level: int
  CurrentExp: number
}

// Persistence (SaveSlot + CampaignMode; PE-02):
//   EquipCommonExp: number
//   ProtagonistEquipmentWarehouse: OwnedEquip[]
```

#### 9.26 阵容羁绊配置表 `FormationBondConfig`

规则语义：[SPEC_03 §3.17](SPEC_03_GameRules.md) 阵容羁绊。一行 = 某一 `BondId` 的某一等级。复合主键 `(BondId, BondLevel)`。`BondBuff` FK → [§9.21b `SkillEffectConfig`](#921b-技能效果配置表-skilleffectconfig)（命名建议 `BondEffect_{Name}_{Level}`）。本 Demo 片 Handler 未接线；UI 展示 `Description` + 关联 Effect `Notes`。

**磁盘名：**
- **Excel：** `战斗_阵容羁绊配置表_Combat_FormationBondConfig.xlsx`
- **CSV：** `Combat_FormationBondConfig.csv`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| BondId | 羁绊ID | `string` | 复合主键之一 |
| BondLevel | 羁绊等级 | `int` | 复合主键之一；≥1；同 Id 多等级互斥（规则见 §3.17） |
| DisplayName | 羁绊名称 | `string` | UI 展示 |
| IconAssetId | 羁绊图标 | `string` | 运行时 `Resources/UI/Bonds/{IconAssetId}`；缺图占位 |
| Description | 羁绊介绍 | `string` | UI 只读；**不**作效果器 |
| ActivationCondition | 羁绊激活条件 | `string` | 结构化 DSL（§3.17）；加载期校验 |
| BondBuff | 羁绊Buff | `string` | FK → `SkillEffectConfig.SkillEffectId` |

**`ActivationCondition` 编码（首版 Kind 白名单）：**

| Kind | 参数 |
|------|------|
| `DeployClassCount` | `BaseClass=战士` **或** `ClassId=Class_Guardian`（二选一，不可同时）；`Min=N`（≥1） |
| `DeployRaceCount` | `RaceId=Race_Human`；`Min=N` |
| `DeployTotalCount` | `Min=N` |
| `DeployPrimaryStatCount` | `Stat=Strength\|Agility\|Intelligence`；`Min=N` |

格式：`{Kind}|Key=Value|Key=Value|…`。示例：`DeployClassCount|BaseClass=战士|Min=5`。

**Demo 样例行（Mode2）：**

| BondId | BondLevel | DisplayName | ActivationCondition | BondBuff |
|--------|-----------|-------------|---------------------|----------|
| `Bond_IronWall` | 1 | 铜墙铁壁 I | `DeployClassCount\|BaseClass=战士\|Min=3` | `BondEffect_IronWall_1` |
| `Bond_IronWall` | 2 | 铜墙铁壁 II | `DeployClassCount\|BaseClass=战士\|Min=5` | `BondEffect_IronWall_2` |
| `Bond_HumanLegion` | 1 | 人类军团 | `DeployRaceCount\|RaceId=Race_Human\|Min=4` | `BondEffect_HumanLegion_1` |
| `Bond_FullArmy` | 1 | 满编 | `DeployTotalCount\|Min=8` | `BondEffect_FullArmy_1` |
| `Bond_StrStrength` | 1 | 力之共鸣 | `DeployPrimaryStatCount\|Stat=Strength\|Min=4` | `BondEffect_StrStrength_1` |
| `Bond_PreciseClass` | 1 | 近卫专精 | `DeployClassCount\|ClassId=Class_Guardian\|Min=2` | `BondEffect_PreciseClass_1` |

```
FormationBondConfig {
  BondId: Id
  BondLevel: int              // ≥ 1
  DisplayName: string
  IconAssetId: Id
  Description: string
  ActivationCondition: string // DSL; load-time validate
  BondBuff: Id                // FK → SkillEffectConfig
}
```

### English

**Status: Fields and encodings defined; config carrier closed** — table-driven data uses **Excel source + CSV output** (paths / naming / bake: [§14](#14-配置表工程约定与打表工具)). Non-table singleton tunables may still use ScriptableObject under `Assets/Settings/<Module>/` ([§13](#13-资源编排与可扩展性)).

Rules authority: [SPEC_03 §3.9](SPEC_03_GameRules.md), [§3.10](SPEC_03_GameRules.md), [§3.11](SPEC_03_GameRules.md), [§3.12](SPEC_03_GameRules.md), [§3.13](SPEC_03_GameRules.md), [§3.14](SPEC_03_GameRules.md), [§3.15](SPEC_03_GameRules.md), [§3.16](SPEC_03_GameRules.md).

Logical short names (e.g. `DigGameplayConfig`) are for SPEC / pseudocode / type ids; **on-disk filenames** — see each subsection’s **Disk name** lines and [§14](#14-配置表工程约定与打表工具) (Excel: `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}`; CSV: `{SystemEN}_{TableEN}`).

#### Weighted-field common rules

All config **Weight** values follow:

| Rule | Notes |
|------|-------|
| Non-negative | `Weight` must be ≥ 0; negative = illegal (reject on load or skip segment + log; pick one at implementation and write back) |
| Zero drop | `Weight = 0` → **treat entry as absent**: strip after parse; excluded from weighted pick |
| Effective set | Only `Weight > 0` entries enter the pool; pick by weight share |
| Empty effective list | Per-mode semantics. Dig / `GraveSpawnWeights`: empty after filter → **abandon that spawn** (see [SPEC_03 §3.10](SPEC_03_GameRules.md)). Dig / `GraveQualityConfig.DropMode=2`: empty after filter → **no loot** |

#### 9.1 LevelOperationConfig

**Disk name:**
- **Excel:** `关卡_关卡运作表_Level_LevelOperationConfig.xlsx`
- **CSV:** `Level_LevelOperationConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| LevelId | 关卡ID | `string` or `int` | Multiple rows per Level |
| StageNumber | 阶段编号 | `int` | Ascending within Level; unique per Level recommended |
| GameplayType | 玩法类型 | `enum` / `string` | e.g. `Shop` / `Dig` / `UpgradeManufacture` / `Defend` / `PushMap` |
| GameplayConfigId | 玩法配置ID | `string` or `int` | **Dig** → `DigGameplayConfig` PK; **Defend** → **RecommendedConfigId** (ModeSelect default highlight; UM next-battle map preview; combat config = player pick — [SPEC_03 §3.12](SPEC_03_GameRules.md) D-044); **PushMap** → `PushMapGameplayConfig` PK; **Shop** / **UpgradeManufacture** → **ignore** (may be non-empty; runtime must **not** resolve against any mode config / Dig/Defend rows; Shop reads `ShopProgress` + shop tables). **No** separate `ShopGameplayConfig` / `UpgradeManufactureGameplayConfig` (see [SPEC_03 §3.9](SPEC_03_GameRules.md)) |

#### 9.2 DigGameplayConfig

**Disk name:**
- **Excel:** `挖坟_挖坟配置表_Dig_DigGameplayConfig.xlsx`
- **CSV:** `Dig_DigGameplayConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | PK; referenced by Level Operation |
| DigMapId | 挖坟地图ID | `string` | Prefab logical name (no path/ext); allowed **`Ground_01`…`Ground_05`**; resolve → `Assets/Prefabs/Maps/{DigMapId}.prefab`; shared ground-variant pool with Defend `BattleMapId`; presentation = **Isometric Tilemap** (Tiles/Sprites under `Assets/Art/Maps/Tiles/`, copied from Example `Environment/Tiles`+`Sprites`); do not runtime-reference `SmallScaleInt/` — [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角) |
| LevelDurationSeconds | 关卡时长限制 | `float` or `int` | **Base** duration (seconds); effective countdown = this field + `DigStageDurationBonus` (see [SPEC_03 §3.10](SPEC_03_GameRules.md) / §9.6) |
| InitialGraveCount | 开局基础生成坟墓数量 | `int` | N independent weighted rolls at start |
| SpawnRate | 倒计时过程中生成坟墓速率 | encoding | Every N seconds spawn M |
| GraveSpawnWeights | 坟墓出现概率权重 | encoding | Quality Id + weight list |

**`SpawnRate` encoding (fixed):** `N;M` — every N seconds spawn M (example `5;2`).

**`GraveSpawnWeights` encoding (fixed):** `QualityId;Weight|QualityId;Weight|...` (example `1;10|2;5|3;1`). Follow **Weighted-field common rules**: strip `Weight = 0`; pick among `Weight > 0`. Empty effective list → **abandon that spawn**. `QualityId` must resolve in `GraveQualityConfig` (§9.3). Runtime effective weights = this field **+** `DigProtagonistCapabilities.GraveSpawnWeightBonus` (additive per QualityId; missing table Id treated as 0 then bonus; apply to the **first** matching segment or insert; then common filter). Each pick reads **live** caps. Example empty: `1;0|2;0`.

**Weighted pick:** filter to effective list, then one independent draw per grave (initial and ongoing). RNG API unbound.

**Placement:** sample DigMap continuous placeable space; avoid `DigObstacle` circles (uncleared Graves only; radii on Prefabs). Retry up to **32** times per spawn; then abandon that spawn.

**Dig Prefab convention:** under `Assets/Prefabs/Dig/`, per-quality Grave Prefabs expose circle obstacle radius (`DigObstacleRadius`; Dig stage does not Instantiate map Digger; HUD top-left 60x60 portrait is DigReward fly target); one Grave Prefab per `QualityId`. `SpriteRenderer` source is `Assets/Art/Dig/Graves/Grave_{QualityId}/Grave_{QualityId}.png`; `DigPrefabCatalog` must cover every QualityId in the current-mode `GraveQualityConfig` (Mode2 Demo: Q1–Q20); `DigAssetBuilder` / HitShape baker quality lists match the table. Grave roots also carry `DigHitShape`: local XZ convex hull (≤12 verts) + `BoundingRadius`, offline-baked via Editor menu `Gravedigger2026/Dig/Bake All Grave Hit Shapes` (prefer `Sprite.GetPhysicsShape`, else alpha outline → hull → simplify); re-bake after art changes. Rules read baked verts only — no runtime Sprite/pixel reads. Digger visuals are Character Creator **baked whole characters**; fixed Prefab logical name `Digger` → `Assets/Prefabs/Dig/Digger.prefab`; art export sources: [§15](#15-角色美术管线character-creator-烘焙整角). Dig circle-cursor UI: `UiDigCursorRing` → `Assets/Prefabs/Dig/UiDigCursorRing.prefab` (dual circle layers: Stroke outer + Fill inner with fixed **screen**-pixel stroke gap; Fill white semi-transparent); bound on `DigPrefabCatalog`, instantiated by `DigCursorView` under Dig HUD Canvas: project `DigCursorRadius` to screen-pixel diameter, then ÷ `Canvas.scaleFactor` into `sizeDelta` (do not treat screen pixels as canvas units under Scale With Screen Size); circle Sprite source `Assets/Art/UI/Dig/Ui_DigCursor_Circle.png`. Dig map: `DigMapId` → `Assets/Prefabs/Maps/{DigMapId}.prefab`.

#### 9.3 GraveQualityConfig

**Disk name:**
- **Excel:** `挖坟_坟墓品质定义表_Dig_GraveQualityConfig.xlsx`
- **CSV:** `Dig_GraveQualityConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| QualityId | 坟墓品质ID | `string` or `int` | PK; referenced by `GraveSpawnWeights` |
| MaxHP | 总血量 | `int` or `float` | Init grave maxHP / current HP; concrete values filled later |
| DropMode | 掉落模式 | `int` | How `LootDrop` weights are used; currently **1** / **2**; more modes later |
| LootDrop | 掉落内容 | encoding | Drop pool when dig succeeds (HP=0); **resolve** via `DropMode` first |
| IconStyleHighId | 高血量图标ID | `string` | Remaining HP% **>65%**; empty = quality default Prefab/icon |
| IconStyleMidId | 中血量图标ID | `string` | Remaining HP% **30%–65%**; empty = default |
| IconStyleLowId | 低血量图标ID | `string` | Remaining HP% **<30%**; empty = default |

```
GraveQualityConfig {
  QualityId: Id
  MaxHP: number
  DropMode: 1 | 2 | ...      // currently 1 and 2; more modes later
  LootDrop: "Id;Weight;Count|Id;Weight;Count|..."
  IconStyleHighId: string
  IconStyleMidId: string
  IconStyleLowId: string
}
```

**Rules link ([SPEC_03 §3.10](SPEC_03_GameRules.md)):** spawn inits `GraveHP` from this table; remaining HP% drives `GraveIconStyle` via `IconStyleHighId` / `IconStyleMidId` / `IconStyleLowId` (empty → quality default); HP=0: rules **resolve** `LootDrop` by `DropMode` into a settled `Id_Count` list, then spawn `DigReward` fly-to Dig HUD portrait and credit that list on arrival. Empty resolve → no flyer / no credit.

**`DropMode`:**

| Value | Semantics |
|-------|-----------|
| **1** | Each segment rolls **independently**: drop that segment's `Count` if `Random[0, 10000) < Weight`. `Weight` is **per-10,000** (9000 = 90%). `Weight = 0` never drops; `Weight ≥ 10000` always drops. |
| **2** | Among effective segments (`Weight > 0`), pick **exactly 1** by weight share and drop its `Count`. `Weight = 0` stripped per **Weighted-field common rules**. Empty effective list → no loot. |
| Other | Unimplemented: no loot + log. New modes may be added later. |

**`LootDrop` encoding (this table, fixed):** `Id;Weight;Count|Id;Weight;Count|...`

- Segment separator `|`; within segment parse **two `;` from the right**: rightmost = `Count`, next = `Weight`, remainder = `Id` (`Id` may contain underscores, **must not** contain `;`).
- `Weight`: non-negative integer. Mode **1** = per-10,000 chance; Mode **2** = shared weight vs other segments in the same field (e.g. A=40, B=60 → P(A)=`40/(40+60)`). Negative = illegal: skip segment and log.
- `Count`: positive integer (≥ 1).
- `Id` resolve order (at credit time, on the **settled** list):
  1. Reserved Spirit Id string **`Spirit`** (case-sensitive) → credit SpiritEssence (not Warehouse).
  2. **`MaterialConfig.MaterialId`** → normal material; `AutoConvert` / UI icon from MaterialConfig.
  3. **`BodyPartConfig.BodyPartId`** → body material (same Id namespace as `MaterialId`; **must not collide**); stack cap **10000**; `AutoConvert` from BodyPart row; Warehouse / DigReward icon may use `ArtAssetId`.
- Empty / fewer than two `;` / illegal `Weight` / non-positive Count / Id unmatched above: **skip segment and log**, continue.
- Example (Mode 1 guaranteed): `Iron;10000;3|Spirit;10000;10|Bone;9000;1`
- Example (Mode 2 pick-one): `Iron;40;3|Bone;60;1`

**Other tables note:** `MonsterConfig.LootDrop` and `PushMapGameplayConfig.CaptureLoot` both use `Id;Count|Id;Count|…` (`LootDropParser.ParseIdSemicolonCount`). `Id` must first resolve through [§9.5a `ItemCatalogConfig`](#95a-itemcatalogconfig) before dispatch.

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

#### 9.5a ItemCatalogConfig

Rules: unified reward-item entrypoint. One row = one item definition that reward configs may reference directly; runtime resolves this table first, then dispatches by `ItemType` / `SourceTable`. **First integrated field this revision:** `PushMapGameplayConfig.CaptureLoot`.

**Disk name:**
- **Excel:** `通用_道具汇总表_Item_ItemCatalogConfig.xlsx`
- **CSV:** `Item_ItemCatalogConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| ItemId | 道具ID | `string` or `int` | PK; public reward-system Id; reward fields like `CaptureLoot` resolve this first |
| DisplayName | 道具名 | `string` | Shared display name; may be an i18n key; empty → UI falls back to `ItemId` |
| IconAssetId | 道具图标 | `string` or `int` | Dedicated icon asset Id for reward popup / generic drop UI |
| ItemType | 道具类型 | enum | Initial enum: `Currency` \| `Material` \| `BodyPart` \| `MagicBook` \| `ProtagonistEquipment` |
| SourceTable | 管理道具属性配置表 | `string` | Authoritative source-table name for validation and dispatch |
| Description | 道具描述 | `string` | Shared description text; reward UI prefers this field over source-table text |
| SellPrice | 售卖价格 | `int` | ≥ 0; credits Spirit on shop sell (UI-026 / `ShopSellService`); shop offer `priceSpirit` also reads this column (D-076) |

**Initial `ItemType` / `SourceTable` registry (authoritative):**

| ItemType | SourceTable | PK mapping / constraints |
|----------|-------------|--------------------------|
| `Currency` | `Dig_CurrencyConfig` | `ItemId == CurrencyConfig.CurrencyId`; must at least support `Spirit` |
| `Material` | `Dig_MaterialConfig` | `ItemId == MaterialConfig.MaterialId` |
| `BodyPart` | `Manufacture_BodyPartConfig` | `ItemId == BodyPartConfig.BodyPartId` |
| `MagicBook` | `Manufacture_MagicBookConfig` | `ItemId == MagicBookConfig.MagicBookId` |
| `ProtagonistEquipment` | `Protagonist_ProtagonistEquipmentConfig` | `ItemId == EquipId`; reward grant means **1 copy** of that gear; owned level still starts from source-table **Level 1**; `EquipLevel` is **not** part of the public reward Id |

**Runtime contract:**

- Reward strings (such as `CaptureLoot`) use `ItemId;Count|ItemId;Count|...`; `Count` is quantity; `ItemId` may contain underscores, so parsing splits quantity at the **last semicolon** (`LootDropParser.ParseIdSemicolonCount`).
- Runtime resolves `ItemCatalogConfig.ItemId` first; miss = skip segment + log.
- On hit, dispatch by `ItemType`:
  - `Currency`: credit currency / Spirit;
  - `Material` / `BodyPart`: warehouse stack + `AutoConvert`;
  - `MagicBook`: grant via existing magic-book acquisition semantics;
  - `ProtagonistEquipment`: grant via existing protagonist-equipment acquisition semantics (first acquire = new gear, repeat acquire = convert Exp).
- `SourceTable` is both an authoring constraint and a load-time validation key: mismatched `ItemType` / `SourceTable`, or missing source-table PK, is a config error and must not silently reclassify the item.

```
ItemCatalogConfig {
  ItemId: Id
  DisplayName: string
  IconAssetId: Id
  ItemType: Currency | Material | BodyPart | MagicBook | ProtagonistEquipment
  SourceTable: "Dig_CurrencyConfig" | "Dig_MaterialConfig" | "Manufacture_BodyPartConfig" | "Manufacture_MagicBookConfig" | "Protagonist_ProtagonistEquipmentConfig"
  Description: string
  SellPrice: int
}
```

#### 9.6 DigProtagonistCapabilities (runtime derived; recalc from tech + protagonist Dig gear)

Learned tech effects **and** owned protagonist gear whose `EffectDomain` includes `Dig` (current level row) jointly write save-slot `DigProtagonistCapabilities` (tech: [§9.16](#916-科技树配置表-techtreeconfig) / [§9.17](#917-科技项效果配置表-techeffectconfig); gear: [§9.25](#925-主角装备配置表-protagonistequipmentconfig); rules: [SPEC_03 §3.13](SPEC_03_GameRules.md) / [§3.16](SPEC_03_GameRules.md)):

```
DigProtagonistCapabilities {
  DigDamage: number
  DigDurationReductionSum: number   // seconds; sum of unlock shorten effects
  DigCursorRadius: number
  DiggableQualityIds: set<QualityId>
  DigStageDurationBonus: number     // seconds; additive to LevelDurationSeconds
  GraveSpawnWeightBonus: map<QualityId, number>  // additive to GraveSpawnWeights; missing Id = 0
  DigProcessSpawnCountBonus: number // additive to SpawnRate M (process spawn only; not N; not InitialGraveCount)
}
// DigActionDuration = max(0.1, 0.8 - DigDurationReductionSum)
// EffectiveDigDuration = LevelDurationSeconds + DigStageDurationBonus
// Recalc = Σ learned TechEffectConfig.AttributeModifiers
//        + Σ owned ProtagonistEquipmentConfig.EquipEffect (Dig domain; current level row)
//        (additive per key)
// Effective spawn weights = GraveSpawnWeights + GraveSpawnWeightBonus (live caps each pick)
// Effective process spawn M = max(0, tableM + DigProcessSpawnCountBonus) (live caps each tick)
```

#### 9.7 DefendGameplayConfig

**Disk name:**
- **Excel:** `防守_防守配置表_Defend_DefendGameplayConfig.xlsx`
- **CSV:** `Defend_DefendGameplayConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | PK; referenced by Level Operation |
| BattleMapId | 战斗地图ID | `string` | Prefab logical name (no path/ext); allowed **`Ground_01`…`Ground_05`** (shared ground-variant pool with Dig `DigMapId`); resolve → `Assets/Prefabs/Maps/{BattleMapId}.prefab` (incl. EngageZone + Isometric Tilemap ground + Demo WalkSurface/NavMesh — [§13](#13-unity-资源编排与可扩展性约定) / [§15](#15-角色美术管线character-creator-烘焙整角)) |
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

**Pathfinding tech (with [SPEC_03 §3.12](SPEC_03_GameRules.md); MassCombatPathing Approach B):**

- **Approach B (locked):** shared goals use **FlowField**; chase/attack use **AttackSlot** + **LocalDetour**; capacity ~200/side. See “Mass combat pathing runtime contract” below.
- Unity **NavMesh** (or walkable mask) expresses **static** walkability and `AirWall`; **do not** require 400 units each running high-frequency full-map `CalculatePath`.
- **Rules layer** outputs: current target entity Id + `GoalKind` (`Objective` / `FormationHome` / `AttackSlot` / `ChaseAnchor`) + optional `AttackRange`; must **not** write `Transform` / `Animator` directly.
- **Move service** resolves `DesiredDestination`: Objective→FlowField sample (inside `CaptureZone` / `ObjectiveArriveRadius` stop seeking center, soft-separation hold); attack→AttackSlot claim; friendly block→LocalDetour; presentation applies motion.
- Every `TargetRetargetIntervalSeconds` the rules layer reselects targets; slot/field samples may update on a shorter frame budget, but **forbid** full-map repath every frame for all units.
- **EngageZone**: **IsoDiamond** (XZ diamond) on the BattleMap **Prefab** (slightly smaller than the map; designer-tuned); non-Rebel soldiers pick nearest enemy only inside it. See [SPEC_03 §3.12](SPEC_03_GameRules.md) WarriorCombat.
- Soldier hit scheme D: branch by `SoulConfig.AttackMode` (copied onto instance at manufacture) — melee windup or ranged projectile; `AttackRange` / windup / projectile speed / timeout from [§9.9b `ClassConfig`](#99b-职业配置表-classconfig) (rules confirm damage; View plays anim/projectile). **Demo v1**: soldiers and monsters use normal attacks only; no skill casts.
- Obstacle bake / NavMesh surface: **Demo-min** — runtime bake from `DigMapBounds` **IsoDiamond**; PushMap `AirWall`: StartBattle bake appends **Not Walkable Box** (§9.22 PM-08). Static result feeds FlowField blockers / walkable mask. Complex obstacles and exact OutsideMap **deferred**.
- **Demo-min spawn points:** temp fixed markers on map Prefab or `InsideMap` random on walkable surface; exact OutsideMap geometry deferred.

**Mass combat pathing runtime contract (Approach B / MassCombatPathing):**

- **Behavior authority:** [SPEC_03 §3.12](SPEC_03_GameRules.md) Mass Combat Pathing; PushMap advance [§3.14](SPEC_03_GameRules.md)
- **Chosen approach:** B — FlowField (shared goals) + local chase AttackSlot + LocalDetour; difficulty 3; slices `.scratch/mass-pathing/issues/`
- **Suggested modules (on implement):**
  - `Assets/Scripts/Core/Pathing/` — pure C#: `FlowFieldService`, `AttackSlotService`, `SpatialHash2D`, `LocalDetourSolver`, frame-budgeted `MassMoveScheduler`
  - `Assets/Scripts/Gameplay/Pathing/` — view bridge: register units from StageController, apply motion; gradually replace/wrap heavy NavMeshAgent pathing in `PushMapAdvanceView` / `WarriorAgentView` / `*MonsterAgentView`
- **FlowField:** cell size Demo **0.25–0.5** world units; cover IsoDiamond; AirWall/non-walkable → impassable; rebuild on `CurrentObjectiveChanged` / StartBattle bake; units sample shared buffer — **no** per-unit full-map Dijkstra/A*. **Arrive hold:** inside `MassMoveScheduler.ObjectiveArriveRadius` (Stage sets from current `CaptureZone.Radius`, default 2), `GoalKind=Objective` stops seeking the goal cell (zero-vector pile-up) and holds via LocalDetour soft separation; outside keeps sampling; SampleDir≈0 while outside → steer toward `GoalWorld`
- **AttackSlot:** ring at `max(0.05, AttackRange − 0.05)`; N=12 melee / 8 ranged as **independent rings on the same target** (a ranged claim must not rebuild/drop melee claims); claim ≤1 per attacker; recompute on retarget / target move > 0.5; walkability via `IAttackSlotWalkable` (stub now; SamplePosition later); `TryClaim(…, targetPos, …)` / `Release` / `ReleaseAllForTarget`
- **LocalDetour:** `SpatialHash2D` cell ≈ `0.5`; query radius ≈ `2*agentRadius+0.2`; forward cone + L/R probes (~`1.0`); optional soft separation via `separationScale` (reduce in engage); coincident footprints get deterministic Id-based push; **forbid** friendly Carve; hot path reuses lists — no full-table O(n²)
- **API:** `Steer(desiredDir, self, neighbors, separationScale?)` → `steerDir` (`self` = XZ pos + radius; no neighbors → `steer ≈ desired`)
- **Perf budget:** ≤~400 movers → move logic target **≤ ~2.5 ms/frame**; ≤50 path/slot recomputes per frame (round-robin)
  - **Stress entry (MP-07):** `Assets/Scripts/Core/Pathing/MassPathingPerfStress.cs` (pure-C# Stopwatch, ~200/side) + `Assets/Scripts/Gameplay/Pathing/MassPathingPerfStressView.cs` (capsule/cube stubs) + Editor `Gravedigger2026/Pathing/Run MassPathing 200v200 Perf Stress`; measures `MassMoveScheduler.Tick` + ≤50 slot refresh — **not** Animator / all-units `CalculatePath`
  - **Over-budget fallbacks (try in order):** (1) raise FlowField `cellSize` (toward 0.5); (2) lower AttackSlot `N` (melee/ranged constants); (3) lower `MassMoveScheduler.MaxRecalcPerFrame` / slot-refresh budget (more frame-slicing; accept steer lag)
- **Transition:** until MP slices land, current single-Agent NavMesh may run; after acceptance, advance/chase must follow this contract — do not keep full HighQuality RVO as the scale solution
- **Soldier task Debug label (Approach A):**
  - Paths: `Assets/Scripts/Gameplay/Pathing/WarriorTaskDebugLabelView.cs` + `WarriorTaskLabelSettings` (static toggle, **default `Enabled=false`**)
  - Presentation: runtime `TextMesh` under soldier feet (top-down readable, `Euler(90,0,0)`); localPos `(0, 0.02, -0.38)`; `Font Size=12` (`characterSize=0.12`); read-only `MassMoveScheduler.TryGetGoal` → ZH short labels: `Objective`→推进, `FormationHome`→回阵, `AttackSlot`→追击, `ChaseAnchor`→追击锚
  - Wire: `WarriorAgentView` (Defend) and `PushMapAdvanceView` (PushMap) Ensure the component on `Bind`
  - Toggle: InSaveShell Debug button flips `WarriorTaskLabelSettings.Enabled` (may runtime-clone an existing Debug button if Prefab slot missing)
  - **Out:** attack windup/fire detail; monster labels; formal UI Prefab / i18n keys
- **AllyFootCircle (v0.75.33):** path `AllyFootCircleView.cs`; localPos `(0,-0.05,-0.2)`; rotation X=**-30**; fill α=**160/255**; Order In Layer=`1`; `WarriorAnimView` skips batch sortingOrder/corpse darken
- **Out of scope:** full ORCA; destructible doors; multi-floor pathing; skill dash prediction

```
FlowFieldService.Rebuild(goal, walkableMaskInclAirWall)
FlowFieldService.SampleDir(worldPos) -> Vector2
AttackSlotService.TryClaim(attackerId, targetId, attackRange, targetPos, …) -> worldPos
LocalDetourSolver.Steer(desiredDir, self, neighbors, separationScale?) -> steerDir
MassMoveScheduler.Tick(dt)
MassPathingPerfStress.Run(perSide≈200) // MP-07 Debug Stopwatch
```

**Spawn / monster tables:** see §9.18 `WaveSpawnConfig`, §9.19 `MonsterConfig`; LossOfControl: §9.20 `LossOfControlConfig`, §9.21 `SkillConfig` (soldier-skill authority; PushMap `Skill_03`/`Skill_01`/`Skill_02` = D-069; `Skill_04`–`Skill_12` EffectKind = D-073); rules in [SPEC_03 §3.12](SPEC_03_GameRules.md).

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
| ClassId | 职业ID | `string` or `int` | Required; FK → `ClassConfig`; written to soldier when Soul slotted; empty Soul → instance forced `Class_Servants` (row ClassId still needs valid FK) |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged`; soldier normal-attack hit scheme D branch (§3.12); same enum as monster `AttackMode`. Examples: Warrior-like→Melee; Archer/Mage-like→Ranged (Mage and Archer share Ranged channel; only `ClassConfig.PrimaryStat` differs) |
| Skills | 可使用技能 | encoding | Skill Id + level list; encoding below; LossOfControl bonus and CD in [§9.21 `SkillConfig`](#921-skillconfig); **unused cast in Demo v1** (may leave empty) |
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

**Note:** Former `InfoTags` no longer builds primary WarriorInfo (primary label = finalized Race). Soul does **not** rewrite Strength/Agility/Intelligence; when Soul slotted it injects Class via `ClassId`; when empty, instance `SoulId=Soul_00` (system default) and force `ClassId=Class_Servants`, other soul-side fields from `Soul_00`. `AttackMode` selects Melee/Ranged hit branch. `ClassName` / `PrimaryStat` / convert coeffs live in [§9.9b `ClassConfig`](#99b-classconfig).

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
  SoulId: Id                      // FK → SoulConfig; empty slot → Soul_00
  ClassId: Id                     // placed soul ClassId, else forced Class_Servants; FK → ClassConfig
  AttackMode: Melee | Ranged      // from effective SoulConfig (placed or Soul_00)
  LockedEquipIds: Id[]
  GemIds: Id[]
  GemMult: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }  // Σ of socketed; 0 if none
  ControlPowerCost: number
  EquipStats: { … five dims }     // manufacture-locked Equip layer flats
  BodyLife: number                // Base(MaxHP)+Equip(MaxHP); HP-dim exception
  SourceItemIds: Id[]             // remake recipe
  SourceSpiritCost: number        // remake Spirit gate
  SoldierSkills: { SkillId, SkillLevel }[]  // DefaultSkillIds @ Lv1; Mode2 may SoldierSkillLevelAdd
  VisualStyleId: Id | ""          // Mode2 AllIn1 preset; empty = Prefab default
  VisualPriority: int
  VisualIntensity: number
  VisualModelScale: number        // scale channel; default 1
}
```

**Save note:** Demo serializes the full snapshot per slot into `PlayerPrefs` (§6); `NextSerial` shares the pool key so re-enter does not collide Ids. `SoldierSkills` round-trips via `WarriorSaveDto.SoldierSkills` (`SoldierSkillEntry[]`, `[Serializable]` + **public fields**, JsonUtility); missing field / null / empty → empty list without dropping other snapshot fields. `VisualStyleId` / `VisualPriority` / `VisualIntensity` / `VisualModelScale` round-trip on the same DTO; missing on old saves → empty style / 0 / 0 / **1** (Prefab default mat and size). `RepairMissingStatSnapshots` only rebuilds StatBlock/recipe fields and **must not** clear existing `SoldierSkills`, `VisualStyle*`, or `VisualModelScale`. PermanentDeath deletes the whole instance (skills and visual style drop with it; no separate skill migrate). Mode1 manufacture/remake: `ManufactureService.BuildWarriorFromAggregate` calls `SoldierSkillGrant.GrantDefaultSkillsAtLevel1` after `ResolveInstanceClassId` (Lv1; no MagicBook; VisualStyle empty; `VisualModelScale=1`). Mode2 AutoManufacture: grant `DefaultSkillIds` from hand ClassId at craft; UI-016 Step2 per-slot pulse `ApplyEquippedBookAtSlot` (incl. `SoldierSkillLevelAdd` / `ForceClass` hit re-grant; `TryApplyVisualStyle` **only on token hit**: material compete or scale multiply). `RefinalizeInstance` may re-pick `AppearanceId` and **must not** clear `VisualStyle*` / `VisualModelScale`.
**Related:**

- Class schema: **§9.9b**; BodyPart / BodyAppearance / ExtraEquipment / GemSuffix schemas: **§9.12–§9.15**.
- Static layer: `StaticStat(S) = max(0, Base+Equip+Base×GemMult+Base×RaceAdjust)` (no Buff); combat layer: `FinalStat(S)` also adds `Base×SkillBuff` (pick `S` first; missing dims = 0; see §3.11).
- **HP-dim exception:** final soldier `MaxHP = ceil(BodyLife + Str×MaxHpStrengthMult)` (`MaxHpStrengthMult` ← `CombatConstantConfig`), `BodyLife = Base(MaxHP)+Equip(MaxHP)`; do **not** use `FinalStat(MaxHP)` (§3.11).
- Combat derives: `Primary` = `ClassId` → `ClassConfig.PrimaryStat` dim; `NormalAttackPower` / `AttackSpeed` / `SkillCooldown` coeffs from `ClassConfig.CombatConvertCoeffs` (missing key → **`CombatConstantConfig`**); hit params from same table columns.
- Multi-gem: instance `GemMult(S) = Σ` of socketed gems' `GemMult(S)`.
- On **PermanentDeath**: all `GemIds` return to Warehouse; BodyParts/Soul/ExtraEquipment and other bound materials are destroyed; `SoldierSkills` dropped with the instance (no recoverable skill item); formation slot cleared (see §3.11). CombatDead (no gems) does not trigger material fate and **keeps** `SoldierSkills`; gemmed soldiers PermanentDeath immediately on HP≤0.
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
| BaseClass | 基础职业 | `enum` / `string` | CSV Chinese: `战士` \| `射手` \| `法师` \| `刺客` → runtime enum `Warrior`/`Archer`/`Mage`/`Thief` (loader still accepts legacy `盗贼`→`Thief`); empty/missing column → `Unspecified`; illegal → Warning + `Unspecified`. **Reserved** for future MagicBook (etc.) conditions; **not** used in naming / appearance / `PrimaryStat` / combat derives; **not** baked onto soldier instance (look up via `ClassId`) |
| PromoteClass | 转职职业 | `string` or empty | Optional text; empty/missing column = no promote target. Fillable and loaded this slice; **not** used in naming / appearance / `PrimaryStat` / combat derives; **not** baked onto soldier instance; application **TBD** |
| ClassLevel | 等级 | `int` | ≥ 0; **UI display only** (UI-016 card under class name as `Lv.{ClassLevel}`); **not** used in combat/manufacture math; missing/empty → `0` |
| PrimaryStat | 主属性 | `enum` / `string` | `Strength` \| `Agility` \| `Intelligence`; selects dim for `NormalAttackPower` (§3.12); example semantics Warrior→Strength, Archer→Agility, Mage→Intelligence (this field wins; not ClassName hardcoding) |
| CombatConvertCoeffs | 战斗换算系数 | encoding | Coeff set for combat derives; encoding below; missing key → **`CombatConstantConfig`** |
| AttackRange | 攻击距离 | `float` | Distance to enter attack state |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0; seconds; used when `AttackMode=Melee` |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0; used when `AttackMode=Ranged` |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0; seconds; timeout → miss |
| BaseMoveSpeed | 基础移速 | `float` | ≥ 0; world units/sec; authoritative **MoveSpeed** `Base` for soldiers (§3.11); missing/≤0 → **3.5** |
| ChaseMoveSpeedMult | 追击移速倍率 | `float` | ≥ 0; × `FinalStat(MoveSpeed)` when `GoalKind=AttackSlot`; default **1** (§3.12) |
| AttackMode | 攻击模式 | `enum` / `string` | `Melee` \| `Ranged`; Mode2 AutoManufacture no-soul AttackMode source ([SPEC_03 §3.15](SPEC_03_GameRules.md)); Mode1 still uses `SoulConfig.AttackMode` |
| PlacementOrder | 放置排序 | `int` | ≥ 1; Mode2 auto-deploy ascending order; missing → large (last) |
| DefaultAppearanceId | 职业默认外观 | `string` or empty | Mode2: use when B empty, or A still empty after Undead rewrite; FK → `BodyAppearanceConfig` |
| DefaultSkillIds | 制造默认获得技能ID | encoding | Skill Ids granted into instance `SoldierSkills` at manufacture; empty = none; encoding below; FK → `SkillConfig.SkillId` |

**`CombatConvertCoeffs` encoding (fixed):** `Key_Value|Key_Value|…`

| Key | Missing-key fallback | Role in §3.12 |
|-----|----------------------|---------------|
| `NormalAttackPrimaryMult` | **`CombatConstantConfig`** same key (sample **15**) | `NormalAttackPower = Primary × coeff` |
| `AttackSpeedBase` | constants table (sample **0.5**) | `AttackSpeed = base + AttackSpeedAgiDiv/max(Agi,1)` |
| `AttackSpeedAgiDiv` | constants table (sample **60**) | see above |
| `SkillCdIntDiv` | constants table (sample **30**) | `SkillCooldown = max(SkillCdFloor, BaseCooldownSeconds − div/max(Int,1))` |
| `SkillCdFloor` | constants table (sample **0.1**) | see above |

- Example: `NormalAttackPrimaryMult_15|AttackSpeedBase_0.5|AttackSpeedAgiDiv_60|SkillCdIntDiv_30|SkillCdFloor_0.1`
- Empty string = all keys fall back to **`CombatConstantConfig`**; illegal segments skip and log; missing constants-table key → Warning + sample-value safety fallback
- Mode2：`Manufacture_ClassConfig.csv` 默认不覆盖 `NormalAttackPrimaryMult`，因此最终以 `Combat_CombatConstantConfig` 为准（通过“缺键回退”触发）。
- Does **not** include `AttackRange` / windup / projectile (separate columns)
- Global default authority: [§9.20b `CombatConstantConfig`](#920b-combatconstantconfig) (C# literals are **not** the business default path)

**`DefaultSkillIds` encoding (fixed):** empty = no default skills; else `SkillId` or `SkillId|SkillId|…` (same `|` as `ClassRestrict`). At manufacture each Id writes `{ SkillId, SkillLevel=1 }`; missing `(SkillId,1)` row → skip + Warning; duplicate Ids keep first. Demo expects 0 or 1. Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) / [§3.15](SPEC_03_GameRules.md).

```
ClassConfig {
  ClassId: Id
  ClassName: string               // WarriorName + ClassAffinity match key
  BaseClass: Warrior | Archer | Mage | Thief | Unspecified  // CSV Chinese four; reserved; unused this slice
  PromoteClass: string | ""       // optional text; empty = none; unused this slice; application TBD
  ClassLevel: int                 // display-only; UI-016 "Lv.N"; missing → 0
  PrimaryStat: Strength | Agility | Intelligence
  CombatConvertCoeffs: "Key_Value|..."  // missing key → CombatConstantConfig
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
  BaseMoveSpeed: number           // >=0; soldier MoveSpeed Base; missing/<=0 → 3.5
  ChaseMoveSpeedMult: number      // >=0; x MoveSpeed when GoalKind=AttackSlot; default 1
  AttackMode: Melee | Ranged      // Mode2 no-soul path
  PlacementOrder: int             // >=1; Mode2 auto-deploy order
  DefaultAppearanceId: Id | ""    // Mode2 appearance fallback
  DefaultSkillIds: "SkillId|..." | ""  // grant SoldierSkills @ Lv1 after ClassId final
}
```

**Parse:**

- At manufacture (Mode1): Soul slotted → `SoulConfig.ClassId` → `WarriorInstance.ClassId`; empty Soul → force `Class_Servants`. Naming / appearance use the instance `ClassId` row's `ClassName`. After `ClassId` is final, shared `SoldierSkillGrant` writes `SoldierSkills` from `DefaultSkillIds` (Lv1; missing `(SkillId,1)` → skip + Warning; duplicate Ids keep first; empty column → empty list; Mode1 ignores MagicBook skill level-up). Manufacture and remake share `BuildWarriorFromAggregate`.
- At manufacture (Mode2 AutoManufacture): `ClassId` from hand `ClassRestrict` (§3.15); `AttackMode` from this table; **no** `SoulId`. After `ForceClass`, grant from the **final** class `DefaultSkillIds`, then second-pass `SoldierSkillLevelAdd`.
- Combat derives: look up `PrimaryStat` and `CombatConvertCoeffs` (missing keys → constants table); hit params from `AttackRange` etc. columns; **MoveSpeed**-dim `Base` from `BaseMoveSpeed`. Do **not** read `ClassLevel` / `BaseClass` / `PromoteClass`.
- StartBattle: `CombatConvertCoeffs.Parse(classEncoded, repo.GetCombatConvertCoeffDefaults())`; MaxHP uses constants-table `MaxHpStrengthMult`.
- UI-016: soldier cards show `Lv.{ClassLevel}` under class name.
- `BaseClass`: config lookup only; MagicBook (etc.) condition matching **TBD**.
- `PromoteClass`: config lookup only; application **TBD**.
- Concrete class rows filled later. Which classes default to which skills **TBD**.

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
| Skills | 额外技能 | encoding | Extra skill set; same encoding as [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig): `SkillId;Level\|…`; LossOfControl bonus via [§9.21](#921-skillconfig) |
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

**Resolve:** At manufacture, collect filled BodyParts' `RaceId`s: **all identical** → that race; else **`Race_Undead`**. Mode2 with equipped `EffectPayload=RaceWeightPick` (Restore) → weight-**1** pick. Look up this table → copy five dims into `WarriorInstance.RaceAdjustCoeff`; each dim feeds `Base(S) × RaceAdjust(S)`. LossOfControl rolls read this row's `LossOfControlChanceBonus`.

#### 9.12 BodyPartConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) manufacture slots / race pick / BaseStats / Warehouse credit. One row = one body-part material (body material table).

**Disk name:**
- **Excel:** `制造_躯体材料配置表_Manufacture_BodyPartConfig.xlsx`
- **CSV:** `Manufacture_BodyPartConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| BodyPartId | 躯体ID | `string` or `int` | PK; usable by `LootDrop` / Warehouse; same Id namespace as `MaterialConfig.MaterialId` (**no collisions**) |
| DisplayName | 道具名称 | `string` | Warehouse / DigStageSummary display name; used as literal if i18n off; empty → UI falls back to `BodyPartId` |
| BodyLevel | 躯体等级 | `int` or `float` | ≥ 0; feeds appearance average level |
| BodySlot | 躯体部位 | `enum` | `Head` / `Torso` / `Arm` / `Leg` |
| RaceId | 种族 | `string` or `int` | FK → `RaceConfig` |
| ControlPowerCost | 控制力占用值 | `int` or `float` | ≥ 0; contributes to manufacture `BodyCost` |
| SpiritCost | 精魂消耗 | `int` or `float` | ≥ 0; default 0; manufacture Spirit total |
| StatBonus | 增加的属性值 | encoding | Flat BaseStat bonuses; `Base(S) = Σ` at manufacture |
| AutoConvert | 超上限兑精魂 | `int` or `float` | Same semantics as `MaterialConfig.AutoConvert` |
| Description | 文字介绍 | `string` | Display copy; localization Key if i18n |
| ArtAssetId | 外形美术素材ID | `string` or `int` | Part visual / Warehouse UI asset Id |
| IsPrimaryHand | 主要手 | `int` / `0\|1` | **Arm only**; `1` = PrimaryHand (Mode2 anchor); default `0` |
| ClassRestrict | 职业限定 | encoding | Multi-`ClassId` for Mode2 class pool; empty PrimaryHand = stop craft (§3.15) |
| BodyPrimaryStat | 躯体主属性 | `enum` / `string` | Exactly one of `Strength` \| `Agility` \| `Intelligence`; Mode2 remaining-part matcher (**not** Class `PrimaryStat`) |

**`ClassRestrict` encoding (fixed):** `ClassId` or `ClassId|ClassId|…` (pipe; exact `ClassConfig.ClassId`).

**`StatBonus` encoding (fixed):** `Attr_Value|Attr_Value|…` (same style as [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers`; additive; empty = none). Keys align with five BaseStats (`MaxHP` / `MoveSpeed` / `Strength` / `Agility` / `Intelligence`).

```
BodyPartConfig {
  BodyPartId: Id
  DisplayName: string             // item display name; empty → UI falls back to BodyPartId
  BodyLevel: number
  BodySlot: Head | Torso | Arm | Leg
  RaceId: Id
  ControlPowerCost: number
  SpiritCost: number              // Mode2 AutoManufacture ignores
  StatBonus: "Attr_Value|..."
  AutoConvert: number
  Description: string
  ArtAssetId: Id
  IsPrimaryHand: 0 | 1
  ClassRestrict: "ClassId|..."
  BodyPrimaryStat: Strength | Agility | Intelligence
}
```

**Resolve:** `Base(S) = Σ` filled parts' `StatBonus(S)` (missing dim **0**). Mode2 pick/class rules: [SPEC_03 §3.15](SPEC_03_GameRules.md). Warehouse stacks by `BodyPartId`; overflow uses row `AutoConvert`. Mode1 may omit new columns (defaults: `IsPrimaryHand=0`, empty `ClassRestrict`, empty `DisplayName` → UI falls back to `BodyPartId`). DigStageSummary (UI-011) body-part lines: `{DisplayName} Lv{BodyLevel} × count`. Concrete values **TBD**.

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
| ClassAffinity | 职业倾向 | encoding | Exact match to `ClassConfig.ClassName` (via soldier instance `ClassId`) |
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
3. **If A empty**: force race to **`Race_Undead`**, reload `RaceAdjustCoeff` + name race segment, re-run from step 2 **once**; if A still empty → Mode2 `DefaultAppearanceId` (if non-empty), then steps 5→6.
4. If A non-empty: subset B = rows whose `ClassAffinity` contains `ClassConfig.ClassName` (via instance `ClassId`); if B non-empty → uniform random in B; **if B empty (class mismatch) → do not use A; go to step 5 same-race fallback** (no Undead rewrite for class mismatch). Mode2: when B empty, try `ClassConfig.DefaultAppearanceId` first if non-empty, then step 5.
5. Use **current** finalized race row with `IsFallback == 1` if present.
6. If still none: uniform random over **entire table**.

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
| Skills | 额外技能 | encoding | Same as [§9.9 `SoulConfig.Skills`](#99-灵魂配置表-soulconfig); LossOfControl bonus via [§9.21](#921-skillconfig) |

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
| GraveSpawnWeightBonus_{QualityId} | `DigProtagonistCapabilities.GraveSpawnWeightBonus[QualityId]` | Additive spawn weight for that quality; missing table Id = 0; e.g. `GraveSpawnWeightBonus_Q4_10` (last `_` splits value) |
| DigProcessSpawnCountBonus | `DigProtagonistCapabilities.DigProcessSpawnCountBonus` | Additive to process-spawn `SpawnRate` **M** (integer); e.g. `DigProcessSpawnCountBonus_3`; **does not** change N |

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
| MonsterType | 怪物类型 | `int` / `enum` | `1`=Normal \| `2`=Elite \| `3`=Boss; archetype tag for later soldier-skill filters; **not** `PushMapSpawnConfig.IsBoss` (spawn-row clear target); **unused** for skills/AI this slice; **load default:** missing/empty → `1` (`Normal`); illegal → load fail |
| AggroMode | 仇恨模式 | `enum` / `string` | `ActiveChase` \| `PassiveChase` \| `StationaryActive` \| `StationaryPassive`; see [SPEC_03 §3.14](SPEC_03_GameRules.md); **load default:** missing/empty → `ActiveChase`; illegal → load fail |
| AlertRadius | 警戒半径 | `float` | ≥ 0; active detect radius; **load default:** missing/empty → `AttackRange`; value < 0 → load fail |
| BodyRadius | 占地半径 | `float` | ≥ 0; XZ footprint radius (world units); shared by PushMap spawn spread and moving-monster `NavMeshAgent.radius`; **load default:** missing/empty → `0.35`; value < 0 → load fail |
| MaxHP | 怪物血量 | `int` or `float` | Init monster maxHP / current HP on spawn |
| MoveSpeed | 怪物移动速度 | `float` | World units/sec or project-unified speed unit |
| AttackPower | 怪物攻击力 | `int` or `float` | Used **only** when attacking soldiers; not used for normal attacks on protagonist |
| AttackSpeed | 攻击速度 | `float` | Attack rate (unit locked at implementation) |
| AttackRange | 攻击距离 | `float` | Distance to enter attack state |
| MeleeWindupSeconds | 近战前摇 | `float` | ≥ 0; seconds; used when `AttackMode=Melee` |
| RangedProjectileSpeed | 远程弹速 | `float` | ≥ 0; used when `AttackMode=Ranged` |
| RangedTimeoutSeconds | 远程超时 | `float` | ≥ 0; seconds; timeout → miss |
| Skills | 怪物技能 | see encoding | Skill Id + CD list; skill-effect columns later topic; **unused in Demo v1** (normal attacks only; ignore or leave empty at implement time) |
| LootDrop | 怪物掉落 | see encoding | On kill; encoding `Id;Count\|Id;Count\|...` (**not** [§9.3](#93-gravequalityconfig) `DropMode` / `Id;Weight;Count`) |

**`Skills` encoding (fixed):** `SkillId_CdSeconds|SkillId_CdSeconds|...`

- Segment separator: `|`
- Segment: `SkillId_CooldownSeconds`
- Empty string = no skills
- Skill-effect definition table **not** defined this batch; Demo v1 does **not** cast even if populated
- **Note:** **Different** from soldier-side `SkillId;Level|…` (monster CD lives on this table)

**`LootDrop` encoding:** `Id;Count|Id;Count|...` (**not** GraveQuality [§9.3](#93-gravequalityconfig) `DropMode` / `Id;Weight;Count`; this table has no `DropMode`). Segment `|`; one last semicolon `;` from the right splits `Count`. `Id` credit order same as §9.3 (Spirit / Material / BodyPart). Empty / missing `;` / non-positive Count: skip segment and log.

```
MonsterConfig {
  MonsterId: Id
  ModelId: string                  // Prefab logical name → Assets/Prefabs/Defend/Monsters/{Id}.prefab; art §15
  DisplayName: string
  TargetSelect: Nearest | PreferWarrior | PreferProtagonist
  AttackMode: Melee | Ranged
  MonsterType: 1 | 2 | 3              // Normal | Elite | Boss; empty → 1; ≠ PushMap IsBoss
  AggroMode: ActiveChase | PassiveChase | StationaryActive | StationaryPassive  // empty → ActiveChase
  AlertRadius: number              // empty → AttackRange
  BodyRadius: number               // empty → 0.35
  MaxHP: number
  MoveSpeed: number
  AttackPower: number              // soldiers only
  AttackSpeed: number
  AttackRange: number
  MeleeWindupSeconds: number
  RangedProjectileSpeed: number
  RangedTimeoutSeconds: number
  Skills: "SkillId_Cd|SkillId_Cd|..."
  LootDrop: "Id;Count|Id;Count|..."
}
```

**Load rules (`ConfigCsvRepository`):** `MonsterType` / `AggroMode` / `AlertRadius` / `BodyRadius` defaults as above; illegal `MonsterType` / illegal enum or `AlertRadius < 0` / `BodyRadius < 0` fails whole-table load (§14.5). `MonsterType` does **not** replace `PushMapSpawnConfig.IsBoss`. PushMap and Defend share this table.

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

#### 9.20b CombatConstantConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) / [§3.12](SPEC_03_GameRules.md) global combat-formula defaults; also hosts **P0 camera / Dig timing** tunables ([§3.10](SPEC_03_GameRules.md) / [§3.14](SPEC_03_GameRules.md)). One row = one constant key. Class `CombatConvertCoeffs` **overrides when present**; missing key / empty string reads this table; MaxHP Strength mult also from here. Camera and Dig duration via `GetCombatConstantOrFallback` / `GetCameraPresentationConstants` / `ApplyDigTimingConstants`.

**Disk name:**
- **Excel:** `通用_常量表_Combat_CombatConstantConfig.xlsx`
- **CSV:** `Combat_CombatConstantConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| ConstantKey | 常量键 | `string` | PK; aligned with `CombatConvertCoeffs` keys, plus `MaxHpStrengthMult` and P0 keys below |
| ConstantKeyZh | 主键中文翻译 | `string` | Optional; ZH display name for the key; **runtime ignores** |
| Value | 数值 | `float` | Default value for the key |
| Comment | 备注 | `string` | Optional; EN design note; **runtime ignores** |
| CommentZh | 备注中文解释 | `string` | Optional; ZH design note; **runtime ignores** |

**Required combat-formula keys (Mode1/Mode2 samples):**

| ConstantKey | ConstantKeyZh | Value | CommentZh (summary) |
|-------------|---------------|-------|---------------------|
| `NormalAttackPrimaryMult` | 普攻主属性倍率 | `15` | Normal attack = Primary × coeff |
| `AttackSpeedBase` | 攻击速度基础值 | `0.5` | ASPD constant term |
| `AttackSpeedAgiDiv` | 攻击速度敏捷除数 | `60` | ASPD Agility divisor |
| `SkillCdIntDiv` | 技能冷却智力除数 | `30` | Skill CD Intelligence divisor |
| `SkillCdFloor` | 技能冷却下限 | `0.1` | Skill CD floor (seconds) |
| `MaxHpStrengthMult` | 血量力量系数 | `3` | MaxHP = ceil(BodyLife + Str × value) |

**Required P0 camera / Dig timing keys:**

| ConstantKey | ConstantKeyZh | Value | CommentZh (summary) |
|-------------|---------------|-------|---------------------|
| `CameraHeightY` | 镜头高度 | `18` | Top-down camera world Y above map center |
| `CameraOrthoSizeMargin` | 地图适配Size余量 | `1.5` | Dig/Defend/Formation: Size = max(half) − margin |
| `PushMapCameraOrthoSize` | 推图开战默认Size | `2` | PushMap combat default ortho Size |
| `CameraNearClip` | 镜头近裁剪面 | `0.1` | nearClipPlane |
| `CameraFarClip` | 镜头远裁剪面 | `100` | farClipPlane |
| `CameraFollowDeadzone` | 跟随死区半径 | `0.15` | PushMap Auto follow world-XZ deadzone |
| `CameraFollowSmoothTime` | 跟随平滑时间 | `0.25` | PushMap Auto SmoothDamp seconds |
| `CameraZoomStepPerNotch` | 滚轮缩放步进 | `0.5` | Scroll wheel Size step |
| `CameraOrthoSizeMin` | 正交Size下限 | `0.5` | Size floor |
| `CameraOrthoSizeMax` | 正交Size上限 | `20` | PushMap zoom ceiling |
| `CameraDragThresholdPixels` | 拖拽启动像素阈值 | `4` | Manual pan arm threshold (px) |
| `PushMapCameraIntroSpeed` | 推图镜头预览速度 | `1.5` | PushMap StartBattle Intro rail speed (world XZ units/sec) |
| `PushMapCameraIntroWaypointDwellSeconds` | 推图镜头预览路点停留 | `0.5` | PushMap Intro dwell seconds at each author WP |
| `DigTriggerDwellSeconds` | 挖坟触发停留 | `0.2` | Cursor dwell before DigAction |
| `BaseDigDuration` | 挖坟基础时长 | `0.8` | DigActionDuration base |
| `DigActionDurationFloor` | 挖坟最短时长 | `0.1` | DigActionDuration floor |

**Required P1 combat-tune keys:**

| ConstantKey | ConstantKeyZh | Value | CommentZh (summary) |
|-------------|---------------|-------|---------------------|
| `AttackSlotMeleeCount` | 近战攻击位数量 | `12` | Melee AttackSlot count |
| `AttackSlotRangedCount` | 远程攻击位数量 | `8` | Ranged AttackSlot count |
| `AttackSlotMargin` | 攻击位环半径余量 | `0.05` | Ring radius margin |
| `AttackSlotMinRingRadius` | 攻击位环最小半径 | `0.05` | Min ring radius |
| `AttackSlotReclaimMoveThreshold` | 攻击位重算位移阈值 | `0.5` | Recompute move threshold |
| `AttackSlotDefaultTargetBodyRadius` | 默认目标体半径 | `0.35` | Fallback target BodyRadius |
| `HitConfirmSlack` | 命中确认松弛距离 | `0.05` | HitConfirm slack |
| `SurroundGapDegrees` | 包围缺口角度 | `60` | Surround gap sector |
| `StuckDetectWindowSeconds` | 卡死检测窗口 | `0.5` | StuckHold detect window |
| `StuckDisplacementEpsilon` | 卡死位移阈值 | `0.2` | StuckHold displacement ε |
| `StuckHoldSeconds` | 卡死停顿时长 | `1` | StuckHold Idle seconds |
| `ProjectileDefaultHitRadius` | 投射物默认命中半径 | `0.55` | Projectile soft-hit radius |
| `DefendVictoryStageExp` | 防守胜利阶段经验 | `100` | Defend victory stage Exp |
| `NewSaveInitialSpiritCount` | 新建档初始精魂 | `30` | Spirit credited on new SaveSlot create; no grant when ≤0 |

**Required P2 pathing/perf keys:**

| ConstantKey | ConstantKeyZh | Value | CommentZh (summary) |
|-------------|---------------|-------|---------------------|
| `FlowFieldDefaultCellSize` | 流场默认格宽 | `0.5` | FlowField default cell |
| `FlowFieldMinCellSize` | 流场最小格宽 | `0.25` | FlowField min cell |
| `FlowFieldMaxCellSize` | 流场最大格宽 | `0.5` | FlowField max cell |
| `MassMoveMaxRecalcPerFrame` | 群体移动每帧重算上限 | `50` | Per-frame recalc budget |
| `MassMoveDefaultAgentRadius` | 群体移动默认体半径 | `0.1` | Default agent radius |
| `MassMoveArriveEpsilon` | 群体移动到达阈值 | `0.08` | Arrive epsilon |
| `MassMoveDefaultObjectiveArriveRadius` | 目标到达默认半径 | `2` | Default objective arrive R |
| `MassMoveAttackSlotSeparationScale` | 攻击位软分离系数 | `0.35` | AttackSlot separation scale |
| `SoftCollisionMaxCorrectionSpeed` | 软碰撞最大修正速度 | `2` | Soft collision speed cap |
| `LocalDetourProbeLength` | 本地绕行探测长度 | `1` | Detour probe length |
| `LocalDetourSoftSeparationStrength` | 本地绕行软分离强度 | `0.15` | Soft separation strength |
| `LocalDetourDetourBias` | 本地绕行偏置 | `0.85` | Detour bias |
| `LocalDetourForwardConeHalfAngleDeg` | 本地绕行前向锥半角 | `50` | Forward cone half-angle |
| `BossAdvanceArriveRadius` | Boss推进到达半径 | `0.35` | Boss advance arrive R |
| `EngageStickHysteresisMargin` | 接战粘滞余量 | `0.15` | Engage sticky hysteresis |
| `PushMapSpawnMinSampleDistance` | 刷怪散布最小采样距 | `0.75` | Spawn min sample distance |
| `PushMapSpawnSampleDistanceBodyMul` | 刷怪散布体半径倍数 | `2.5` | Sample distance body mul |
| `PushMapSpawnLeashSlack` | 刷怪拴绳松弛 | `0.35` | Sample leash slack |
| `PushMapSpawnAbsoluteLeashFloor` | 刷怪绝对拴绳下限 | `3` | Absolute leash floor |
| `PushMapSpawnAbsoluteLeashBodyMul` | 刷怪绝对拴绳体倍数 | `10` | Absolute leash body mul |

```
CombatConstantConfig {
  ConstantKey: string
  ConstantKeyZh: string   // optional; display; runtime ignore
  Value: number
  Comment: string         // optional; EN note; runtime ignore
  CommentZh: string       // optional; ZH note; runtime ignore
}
```

**Resolve:** `ConfigCsvRepository` loads from current CampaignMode CSV root; `TryGetCombatConstant(key)`; `GetCombatConvertCoeffDefaults()` builds the five-key fallback for `CombatConvertCoeffs.Parse`; `GetCameraPresentationConstants` / `ApplyDigTimingConstants` / `GetDigTriggerDwellSeconds` read P0 keys; **`CombatRuntimeTuning.ApplyFromRepository`** at end of constants load applies P1/P2 snapshot (AttackSlot / MassMove / FlowField / LocalDetour / SoftCollision / StuckHold / Projectile / Defend victory Exp / PushMap spawn spread). Missing required key → Warning + sample-value safety fallback (**not** business authority). Separate Mode1/Mode2 files.

#### 9.21 SkillConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) soldier skills / [§3.12](SPEC_03_GameRules.md) SkillCast. **Soldier-skill authority table** (catalog of all soldier skills); one row = one `SkillId` at one level. Composite PK `(SkillId, SkillLevel)`. Monster skills still use `MonsterConfig.Skills` — **not** this table as a monster catalog. Soul / Gem / ExtraEquipment `Skills` lists may still reference `SkillId` here (parallel to instance `SoldierSkills`; same-Id merge **TBD**). **Demo D-069** drives PushMap `Skill_03` casts, `Skill_01` block, and `Skill_02` Comfort (hard-map). **Demo D-073** drives `Skill_04`–`Skill_12` via the `SkillEffectConfig.EffectKind` registry (Session must not branch on `SkillId`). Do **not** parse `Description` natural language as an effect engine.

**Disk name:**
- **Excel:** `战斗_技能配置表_Combat_SkillConfig.xlsx`
- **CSV:** `Combat_SkillConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| SkillId | 士兵技能ID | `string` or `int` | Composite PK part; referenced by `ClassConfig.DefaultSkillIds`, instance `SoldierSkills`, Soul/Gem/Equip `Skills` |
| SkillLevel | 技能等级 | `int` | Composite PK part; from **1**; instance baked level looks up this row |
| CooldownMode | 冷却模式 | `enum` / `string` | `Mode1` \| `Mode2`; aligns with `CampaignMode`; missing/illegal → Warning. `Mode2`: CD starts **on cast commit** (D-069); `Mode1` unused this Demo |
| CastTarget | 施放目标 | `enum` / `string` | Present values: `CurrentNormalAttackTarget` \| `EnemySingle` \| `Self` \| `AllySingle` \| `GroundPoint` \| `EnemyAll`. Changelog "seven enums" missing name **TBD**. `Skill_03`: `EnemySingle` = current engage target |
| ExtraActivationCondition | 额外激活条件 | `string` | Optional; empty = none; Mode2 samples use natural-language encoding. D-069 **hard-matches** `敌人普攻命中Self` (Skill_01) and `自身血量=100%` (Skill_02). D-073 handlers do **not** parse this cell; they use `EffectKind`/`EffectParams`/`TriggerHook` (CSV wording is semantic alignment only) |
| DisplayName | 技能名称 | `string` | Display name; may be i18n key |
| Description | 技能文字描述 | `string` | Display copy |
| IconAssetId | 技能图标 | `string` or `int` | UI icon asset Id; missing/empty = no icon. **UI-021 soldier-bar tooltip does not read this field**: loads `Resources/UI/Skills/{SkillId}` by `SkillId` |
| SkillEffectId | 技能效果 | `string` or `int` | FK → `SkillEffectConfig`; effect body in §9.21b (`EffectKind`/`EffectParams`/`TriggerHook`). Mode2 multi-level naming: `SkillEffect_{SkillNum}_{Level}` (e.g. `SkillEffect_01_3` ↔ `Skill_01` Lv3) |
| EffectImplemented | 效果已实现 | `0` \| `1` | Whether Demo combat effect is wired; **does not drive combat**, UI-021 tint only. `1` = green; `0` = red; missing/empty → `0`. D-069 seed: `Skill_01`/`Skill_02`/`Skill_03` all levels = `1`. D-073: `Skill_04`–`Skill_12` set Lv1–5 to `1` when each SE-01–09 lands |
| BaseCooldownSeconds | 基础冷却 | `float` | ≥ 0; seconds; soldier actual CD = `max(SkillCdFloor, BaseCooldownSeconds − SkillCdIntDiv/max(Int,1))` (coeffs from `ClassConfig.CombatConvertCoeffs` / §3.12); D-069 drives `Skill_03` |
| LossOfControlChanceBonus | 失控概率加成 | `float` | May be +/-; missing = **0**; `ΣSkillBonus` looks up this field at the instance baked level (§3.11) |

**Composite PK:** `(SkillId, SkillLevel)` unique; levels for the same `SkillId` should be consecutive from 1 (loader may Warning). Lookup uses instance `{ SkillId, SkillLevel }`; missing row → skill invalid + Warning.

```
SkillConfig {
  SkillId: Id
  SkillLevel: int                 // ≥ 1; composite PK with SkillId
  CooldownMode: Mode1 | Mode2
  CastTarget: CurrentNormalAttackTarget | EnemySingle | Self | AllySingle | GroundPoint | EnemyAll | TBD
  ExtraActivationCondition: string
  DisplayName: string
  Description: string
  IconAssetId: Id | ""
  SkillEffectId: Id               // FK → SkillEffectConfig
  EffectImplemented: 0 | 1        // UI-021 tint only; missing → 0
  BaseCooldownSeconds: number     // D-069 drives Skill_03
  LossOfControlChanceBonus: number
}
```

**Note:** If soldier `ΣSkillBonus ≠ 0`, each skill cast re-rolls with full `FinalLossChance` (§3.11). Demo D-069 fires this after a PushMap **successful commit** of `Skill_03` (sample `LossOfControlChanceBonus=0` does not trigger from this skill alone). Soldier skills have **no** exp-spend upgrade; level = default 1 + Mode2 `SoldierSkillLevelAdd`.

**Mode2 sample rows `Skill_01` (Block, Lv1–5):**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description (summary) | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|----------------------|---------------|--------------|----------------------|
| 1 | `Self` | `敌人普攻命中Self` | 格挡 | Enemy normal attack this hit: **10%** chance damage → 0 (still counts as hit) | `SkillEffect_01_1` | `Mode2` | 0 |
| 2 | `Self` | `敌人普攻命中Self` | 格挡 | … **15%** … | `SkillEffect_01_2` | `Mode2` | 0 |
| 3 | `Self` | `敌人普攻命中Self` | 格挡 | … **20%** … | `SkillEffect_01_3` | `Mode2` | 0 |
| 4 | `Self` | `敌人普攻命中Self` | 格挡 | … **25%** … | `SkillEffect_01_4` | `Mode2` | 0 |
| 5 | `Self` | `敌人普攻命中Self` | 格挡 | … **30%** … | `SkillEffect_01_5` | `Mode2` | 0 |

Shared: `IconAssetId=Skill_01`, `LossOfControlChanceBonus=0`. `CastTarget=Self`: passive on-hit reaction on self; `ExtraActivationCondition` limits trigger to enemy normal attack hitting this soldier.

**Skill_01 block encoding (D-069 / SC-02 Approach B; hard-map, not a generic parser):** `SkillEffect_01_1`–`_5` → Lv1–5 **10%/15%/20%/25%/30%** (aligned with Description; do not parse body). Independent on-hit hook before PushMap `TryApplyMonsterDamageToWarrior` subtracts HP; success → this hit’s damage 0 (still a hit). Does **not** occupy the AA channel, start CD, or fire extra LOC roll. Do not block ranged projectiles. Defend not wired this slice. See [SPEC_03 §3.12](SPEC_03_GameRules.md) SkillCast.

**Mode2 sample rows `Skill_02` (Comfort, Archer `Class_Archer.DefaultSkillIds`, Lv1–5):**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `Self` | `自身血量=100%` | 舒适 | Damage +**5%** | `SkillEffect_02_1` | `Mode2` | 0 |
| 2 | `Self` | `自身血量=100%` | 舒适 | Damage +**10%** | `SkillEffect_02_2` | `Mode2` | 0 |
| 3 | `Self` | `自身血量=100%` | 舒适 | Damage +**15%** | `SkillEffect_02_3` | `Mode2` | 0 |
| 4 | `Self` | `自身血量=100%` | 舒适 | Damage +**20%** | `SkillEffect_02_4` | `Mode2` | 0 |
| 5 | `Self` | `自身血量=100%` | 舒适 | Damage +**25%** | `SkillEffect_02_5` | `Mode2` | 0 |

Shared: `IconAssetId=Skill_02`, `LossOfControlChanceBonus=0`. `CastTarget=Self`: full-HP self buff, not an external target pick; `ExtraActivationCondition` limits activation to `RemainingHp >= MaxHp` (conditional damage up, not always-on). Soul sample `Soul_02` still has `Skills=Skill_02;1` (parallel to instance `SoldierSkills`).

**Skill_02 Comfort encoding (D-069 / SC-03 Approach A; hard-map, not a generic parser):** `SkillEffect_02_1`–`_5` → Lv1–5 outgoing **+5%/+10%/+15%/+20%/+25%** (aligned with Description; do not parse body). Independent mul hook before PushMap `SettleMonsterDamage` (melee/ranged HitConfirm, including each `Skill_03` burst hit) subtracts monster HP. Gate is `RemainingHp >= MaxHp` at that settle. `this hit = NormalAttackPower × (1 + bonus)`; does **not** rewrite stored `NormalAttackPower`. Does **not** occupy the AA channel, start CD, or fire extra LOC roll. Each of 3 burst hits checks independently. Applies if held (including Rebel outgoing vs monsters). Defend not wired this slice. See [SPEC_03 §3.12](SPEC_03_GameRules.md) SkillCast.

**Mode2 sample rows `Skill_03` (Burst, Mage `Class_Mage.DefaultSkillIds`, Lv1–5):**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `EnemySingle` | (empty) | 连发 | On attack target, chain **3** hits | `SkillEffect_03_1` | `Mode2` | 50 |
| 2 | `EnemySingle` | (empty) | 连发 | On attack target, chain **3** hits | `SkillEffect_03_2` | `Mode2` | 40 |
| 3 | `EnemySingle` | (empty) | 连发 | On attack target, chain **3** hits | `SkillEffect_03_3` | `Mode2` | 30 |
| 4 | `EnemySingle` | (empty) | 连发 | On attack target, chain **3** hits | `SkillEffect_03_4` | `Mode2` | 20 |
| 5 | `EnemySingle` | (empty) | 连发 | On attack target, chain **3** hits | `SkillEffect_03_5` | `Mode2` | 10 |

Shared: `IconAssetId=Skill_03`, `LossOfControlChanceBonus=0`. `CastTarget=EnemySingle`: active single-enemy cast; empty `ExtraActivationCondition` = no extra gate (CD-only, unlike Block/Comfort on-hit or full-HP conditions). All five Description rows share the same chain count; level difference is `BaseCooldownSeconds` only. Soul sample `Soul_03` still has `Skills=Skill_03;1` (parallel list **not** read this Demo).

**Skill_03 cast encoding (D-069 / Approach C; hard-map, not a generic parser):** `SkillEffect_03_1`–`_5` → occupy the AA channel for **3** sequential scheme-D hits (`NormalAttackPower` each); insert when CD ready and in range; `CooldownMode=Mode2` CD starts on commit. See [SPEC_03 §3.12](SPEC_03_GameRules.md) SkillCast.

**Mode2 sample rows `Skill_04` (FirstStrike, Rogue `Class_Rogue.DefaultSkillIds`, Lv1–5):**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `EnemySingle` | `普攻攻击新目标敌人的第一次` | 先发制人 | Attack damage +**20%** | `SkillEffect_04_1` | `Mode2` | 0 |
| 2 | `EnemySingle` | `普攻攻击新目标敌人的第一次` | 先发制人 | Attack damage +**30%** | `SkillEffect_04_2` | `Mode2` | 0 |
| 3 | `EnemySingle` | `普攻攻击新目标敌人的第一次` | 先发制人 | Attack damage +**40%** | `SkillEffect_04_3` | `Mode2` | 0 |
| 4 | `EnemySingle` | `普攻攻击新目标敌人的第一次` | 先发制人 | Attack damage +**50%** | `SkillEffect_04_4` | `Mode2` | 0 |
| 5 | `EnemySingle` | `普攻攻击新目标敌人的第一次` | 先发制人 | Attack damage +**60%** | `SkillEffect_04_5` | `Mode2` | 0 |

Shared: `IconAssetId=Skill_04`, `LossOfControlChanceBonus=0`. `CastTarget=EnemySingle`: damage bonus on the current normal-attack single enemy, not an always-on `Self` buff; `ExtraActivationCondition` limits activation to the soldier's **first normal attack on a newly selected target** (re-triggers on next target switch; no bonus on subsequent hits on the same target). All five share the same condition and target type; level difference is damage bonus in Description; `BaseCooldownSeconds=0` (no separate CD; tied to first hit on new target).

**Mode2 sample rows `Skill_05` (Unyielding, Guardian `Class_Guardian.DefaultSkillIds`, Lv1–5):**

| SkillLevel | CastTarget | ExtraActivationCondition | DisplayName | Description | SkillEffectId | CooldownMode | BaseCooldownSeconds |
|------------|------------|--------------------------|-------------|-------------|---------------|--------------|----------------------|
| 1 | `Self` | `敌人的本次攻击导致Self死亡` | 坚挺 | Force soldier HP to 1, invulnerable **1** second | `SkillEffect_05_1` | `Mode2` | 60 |
| 2 | `Self` | `敌人的本次攻击导致Self死亡` | 坚挺 | Force soldier HP to 1, invulnerable **2** seconds | `SkillEffect_05_2` | `Mode2` | 60 |
| 3 | `Self` | `敌人的本次攻击导致Self死亡` | 坚挺 | Force soldier HP to 1, invulnerable **3** seconds | `SkillEffect_05_3` | `Mode2` | 60 |
| 4 | `Self` | `敌人的本次攻击导致Self死亡` | 坚挺 | Force soldier HP to 1, invulnerable **4** seconds | `SkillEffect_05_4` | `Mode2` | 60 |
| 5 | `Self` | `敌人的本次攻击导致Self死亡` | 坚挺 | Force soldier HP to 1, invulnerable **5** seconds | `SkillEffect_05_5` | `Mode2` | 60 |

Shared: `IconAssetId=Skill_05`, `LossOfControlChanceBonus=0`. `CastTarget=Self`: lethal-intercept on self, no external target pick; `ExtraActivationCondition` limits trigger to when **this incoming hit** would put Self at HP≤0 (wording is "this attack", **not** limited to normal attacks, unlike Block's `敌人普攻命中Self`). All five share the same condition and target type; level difference is invulnerability seconds in Description; `BaseCooldownSeconds` **60** at all levels (CD does not scale with level). Soul sample `Soul_05` still has `Skills=Skill_05;1` (parallel to instance `SoldierSkills`); gem sample `Gem_Amethyst_01` still has `Skills=Skill_05;1`. D-073: `EffectKind=CheatDeathInvincible`.

**Mode2 sample rows `Skill_06` (Stun, BombMaster `Class_BombMaster.DefaultSkillIds`, Lv1–5):** `CastTarget=GroundPoint`; `ExtraActivationCondition=士兵普攻命中敌人`; 10% AOE stun radius **1.5**; stun **1–5s** by level; `BaseCooldownSeconds=0`; `SkillEffect_06_1`–`_5`.

**Mode2 sample rows `Skill_07` (Freeze, IceMage `Class_IceMage.DefaultSkillIds`, Lv1–5):** `CastTarget=GroundPoint`; empty condition; AOE slow 50% for **2–6s**; internal CD `BaseCooldownSeconds=10`; `SkillEffect_07_1`–`_5`.

**Mode2 sample rows `Skill_08` (Elite Bane, Brawler `Class_Brawler.DefaultSkillIds`, Lv1–5):** `CastTarget=EnemySingle`; Elite filter; outgoing **+50%–+90%**; `BaseCooldownSeconds=0`; `SkillEffect_08_1`–`_5`. Gate reads `MonsterConfig.MonsterType==Elite`.

**Mode2 sample rows `Skill_09` (Warming Up, Berserker `Class_Berserker.DefaultSkillIds`, Lv1–5):** `CastTarget=Self`; +**3%/5%/7%/9%/12%** per 10s tick, cap **60%**; `BaseCooldownSeconds=10`; `SkillEffect_09_1`–`_5`.

**Mode2 sample rows `Skill_10` (Pierce, Longbowman `Class_Longbowman.DefaultSkillIds`, Lv1–5):** `CastTarget=EnemySingle`; extra hits **1–5** after first at `DamageMul=1`; keep current projectile velocity (no per-projectile A*); `BaseCooldownSeconds=0`; `SkillEffect_10_1`–`_5`.

**Mode2 sample rows `Skill_11` (Burn, FireMage `Class_FireMage.DefaultSkillIds`, Lv1–5):** `CastTarget=GroundPoint`; DoT 20% NAP / 1s for **2–6s**; `StackMode=RefreshDuration`; `BaseCooldownSeconds=0`; `SkillEffect_11_1`–`_5`.

**Mode2 sample rows `Skill_12` (Blink, Shadowblade `Class_Shadowblade.DefaultSkillIds`, Lv1–5):** `CastTarget=EnemySingle`; `ExtraActivationCondition=当开始寻找新的攻击目标`; farthest-then-teleport-behind; CD **60/50/40/30/20**; `SkillEffect_12_1`–`_5`. CD authority = `SkillConfig.BaseCooldownSeconds`.

#### 9.21b SkillEffectConfig

Rules: referenced by `SkillConfig.SkillEffectId`. One row = one effect definition. **Effect-body columns live here** — **do not** put them back on `SkillConfig`. Demo D-069 **hard-maps** `SkillEffect_01_*`/`_02_*`/`_03_*` (block / Comfort / burst); `EffectKind` may be empty on those rows. Demo D-073 drives `SkillEffect_04_*`–`_12_*` via registered handlers. Mode1 may keep empty placeholder columns; this slice does not fill Mode1 combat effects.

**Disk name:**
- **Excel:** `战斗_技能效果配置表_Combat_SkillEffectConfig.xlsx`
- **CSV:** `Combat_SkillEffectConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| SkillEffectId | 技能效果ID | `string` or `int` | PK |
| Notes | 备注 | `string` | Optional designer notes; **does not** drive rules |
| EffectKind | 效果种类 | `string` | **Registered** PascalCase token; empty = unimplemented. No Chinese / inline params. Aligns with MagicBook `EffectPayload` |
| EffectParams | 效果参数 | `string` | Allowed `Key=Value\|…` for the token; empty = none/defaults |
| TriggerHook | 触发钩子 | `string` | Pipeline insert-point enum; empty = unwired |

**`EffectKind` encoding (fixed; same pattern as §9.24 `EffectPayload`):** one PascalCase token (`^[A-Za-z][A-Za-z0-9]*$`); empty = unimplemented; token **must** appear in the catalog below; unregistered → empty apply + Warning; one token per row; multiple `SkillEffectId`s may share a token via `EffectParams`; `DisplayName` / `Description` / `ExtraActivationCondition` do **not** drive rules.

**`EffectParams` encoding (fixed):** `Key=Value` or `Key=Value|Key=Value|…` (pipe). Keys must be allowed for that token; unknown Key → Warning and skip pair; duplicate Key → latter wins; missing required Key → invalid / empty apply + Warning.

**`TriggerHook` enum (extensible):** `OnOutgoingDamageSettle` \| `OnIncomingDamageSettle` \| `OnWarriorAaHitConfirm` \| `OnWarriorTargetAcquired` \| `OnWarriorWouldDie` \| `OnProjectileHit` \| `OnSkillInternalCooldown`.

**`EffectKind` catalog (authoritative; register a token here before filling rows / writing handlers):**

| Token | TriggerHook | Allowed Params (examples) | Skill |
|-------|-------------|---------------------------|-------|
| `OutgoingMulOnNewTargetFirstHit` | `OnOutgoingDamageSettle` | required `Mul` | Skill_04 |
| `CheatDeathInvincible` | `OnWarriorWouldDie` | required `InvincibleSeconds` | Skill_05 |
| `OnAaHitChanceAoeStun` | `OnWarriorAaHitConfirm` | required `Chance`,`Radius`,`StunSeconds` | Skill_06 |
| `OnAaHitAoeSlow` | `OnWarriorAaHitConfirm` | required `Radius`,`SlowMoveMul`,`SlowAttackMul`,`DurationSeconds`; optional `InternalCooldownSeconds` (default = `SkillConfig.BaseCooldownSeconds`) | Skill_07 |
| `OutgoingMulVsMonsterType` | `OnOutgoingDamageSettle` | required `MonsterType`,`Mul` | Skill_08 |
| `StackingOutgoingMulTimed` | `OnSkillInternalCooldown` (stack) + `OnOutgoingDamageSettle` (apply) | required `StackBonus`,`MaxTotalBonus`,`TickSeconds` | Skill_09 |
| `RangedPierceExtraHits` | `OnProjectileHit` | required `ExtraHitCount`,`DamageMul` | Skill_10 |
| `OnAaHitApplyBurn` | `OnWarriorAaHitConfirm` | required `TickDamageMul`,`TickIntervalSeconds`,`DurationSeconds`; optional `StackMode` (default `RefreshDuration`) | Skill_11 |
| `RetargetFarthestTeleportBehind` | `OnWarriorTargetAcquired` | (none; CD from `SkillConfig.BaseCooldownSeconds`) | Skill_12 |

```
SkillEffectConfig {
  SkillEffectId: Id
  Notes: string
  EffectKind: string              // registered token | empty
  EffectParams: string            // Key=Value|Key=Value|… | empty
  TriggerHook: string             // enum | empty
}
```

**Mode2 sample FK (`Skill_01`–`Skill_03`, D-069 hard-map; EffectKind empty):** `SkillEffect_01_1`–`_5` block 10%–30%; `SkillEffect_02_1`–`_5` Comfort +5%–+25%; `SkillEffect_03_1`–`_5` 3-hit burst, BaseCD 50→10s.

**Mode2 sample FK (`Skill_04` FirstStrike):** `OutgoingMulOnNewTargetFirstHit` / `OnOutgoingDamageSettle` / `Mul=1.2`…`1.6`.

**Mode2 sample FK (`Skill_05` Unyielding):** `CheatDeathInvincible` / `OnWarriorWouldDie` / `InvincibleSeconds=1`…`5`.

**Mode2 sample FK (`Skill_06` Stun):** `OnAaHitChanceAoeStun` / `OnWarriorAaHitConfirm` / `Chance=0.1|Radius=1.5|StunSeconds=1`…`5`.

**Mode2 sample FK (`Skill_07` Freeze):** `OnAaHitAoeSlow` / `OnWarriorAaHitConfirm` / `Radius=1.5|SlowMoveMul=0.5|SlowAttackMul=0.5|DurationSeconds=2`…`6|InternalCooldownSeconds=10`.

**Mode2 sample FK (`Skill_08` Elite Bane):** `OutgoingMulVsMonsterType` / `OnOutgoingDamageSettle` / `MonsterType=Elite|Mul=1.5`…`1.9`.

**Mode2 sample FK (`Skill_09` Warming Up):** `StackingOutgoingMulTimed` / `OnSkillInternalCooldown` / `StackBonus=0.03`…`0.12|MaxTotalBonus=0.6|TickSeconds=10`.

**Mode2 sample FK (`Skill_10` Pierce):** `RangedPierceExtraHits` / `OnProjectileHit` / `ExtraHitCount=1`…`5|DamageMul=1`.

**Mode2 sample FK (`Skill_11` Burn):** `OnAaHitApplyBurn` / `OnWarriorAaHitConfirm` / `TickDamageMul=0.2|TickIntervalSeconds=1|DurationSeconds=2`…`6|StackMode=RefreshDuration`.

**Mode2 sample FK (`Skill_12` Blink):** `RetargetFarthestTeleportBehind` / `OnWarriorTargetAcquired` / empty Params (CD from SkillConfig 60/50/40/30/20).

#### 9.22 PushMapGameplayConfig

Rules: [SPEC_03 §3.14](SPEC_03_GameRules.md). One row = one PushMap level config.

**Disk name:**
- **Excel:** `推图战_推图战配置表_PushMap_PushMapGameplayConfig.xlsx`
- **CSV:** `PushMap_PushMapGameplayConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | PK; referenced by LevelOperation / ModeSelect |
| MapId | 地图编号 | `string` | Prefab logical name; allowed **`Ground_01`…`Ground_05`** or **`PushMap_*`**; resolve → `Assets/Prefabs/Maps/{MapId}.prefab`; **≠** LevelId |
| DisplayName | 关卡显示名 | `string` | Display or i18n key |
| StageExpReward | 阶段经验奖励 | `int` | Exp credited on Boss clear; Demo may use fixed |
| CaptureLoot | 占领默认掉落 | encoding | Optional; encoded as `ItemId;Count\|…`; `ItemId` **must exist in** [§9.5a `ItemCatalogConfig`](#95a-itemcatalogconfig); **not** GraveQuality `DropMode` / `Id;Weight;Count` |
| DungeonUnlockIds | 副本解锁ID列表 | encoding | `\|`-separated; empty=none; dungeon gameplay **TBD** |
| CaptureSeconds | Capture seconds | `float` | **load default 5** (missing/empty); < 0 → load fail |
| Notes | 备注 | `string` | Optional |

```
PushMapGameplayConfig {
  GameplayConfigId: Id
  MapId: Ground_01 | PushMap_*
  DisplayName: string
  StageExpReward: int
  CaptureLoot: "ItemId;Count|ItemId;Count|..."
  DungeonUnlockIds: "DungeonId|..."
  CaptureSeconds: number
}
```

**Map Prefab marker contract (Approach A / PM-01):**

- **Scripts:** `Assets/Scripts/Gameplay/PushMap/`, namespace `Gravedigger2026.Gameplay.PushMap`
- **Sample map:** `Assets/Prefabs/Maps/PushMap_Demo_01.prefab` (`MapId=PushMap_Demo_01`); Editor menu `Gravedigger2026/PushMap/Ensure Sample Map Prefab` copies `Ground_01` and attaches markers; **do not** rewrite Dig/Defend shared `Ground_*`. Mode2 Level 2/3 also use author maps `PushMap_Demo_02` / `PushMap_Demo_03` (`PushMapGameplayConfig`: `PushMap_02`/`PushMap_03`)
- **Runtime bind:** `PushMapStageController` resolves via `DefendPrefabCatalog.TryGetMap(MapId)` — **no** `Prefabs/Maps/` folder scan; `DefendPrefabCatalog.Maps` and `DefendAssetBuilder.CatalogExtraMapIds` must cover every referenced `PushMap_Demo_*` (incl. 01–03). Menu `Gravedigger2026/PushMap/Ensure Catalog Map Binding` upserts bindings without regenerating Demo_02/03
- **Reuse:** `EngageZone` (`Gameplay.Defend`), `WalkSurface` (`WalkSurfaceIsoDiamond` / `Gameplay.Maps`); ground remains Isometric Tilemap (Tile Palette OK)
- **Slice boundary:** marker contract + instantiable sample only; capture/spawn/AirWall NavMesh **out of scope** (PM-04/05/08)

| Component | Fields (SerializeField) | Notes |
|-----------|-------------------------|-------|
| `ObjectivePoint` | `ObjectiveOrder:int` (≥1); ref `CaptureZone` | Ordered objective; `CaptureZone` on same or child |
| `CaptureZone` | `Radius:float` default **2** | XZ circle; `ContainsXZ`; tunable |
| `AirWall` | `HalfExtents:Vector3` | Authoring blocker; **Y euler** 0°/45°/90°…; runtime block → "AirWall NavMesh contract (PM-08)" below |
| `SpawnPoint` | `SpawnPointId:string` | Matches `PushMapSpawnConfig.SpawnPointId` |
| `TrapZone` | `TrapZoneId:string`; `Radius:float` | XZ circle; matches spawn-table `TrapZoneId` |
| `BossPoint` | (none required; pose=`Transform`) | Boss spawn placeholder |
| `CameraFollowPath` | `_bakedPoints:Vector3[]` (local XZ); child `CameraPathWaypoint` | Camera rail; author ≥2 ordered waypoints; Bake = world-XZ straight samples between adjacent waypoints + `InverseTransformPoint` to local XZ (**not** NavMesh / axis-aligned cell-center A*); turns are author waypoints; straight may cross AirWall; waypoints may Snap to Grid; gizmos draw polyline; StartBattle rebakes if empty |

```
ObjectivePoint { ObjectiveOrder: int>=1; CaptureZone }
CaptureZone    { Radius: number = 2 }
AirWall        { HalfExtents: Vector3 }      // Transform.eulerAngles.y = 0|45|90|…
SpawnPoint     { SpawnPointId: string }
TrapZone       { TrapZoneId: string; Radius: number }
BossPoint      { }
CameraFollowPath { BakedPoints: Vector3[] }
CameraPathWaypoint { Order: int>=1 }
// + EngageZone, WalkSurface (existing)
```

**Capture runtime contract (Approach A / PM-04):**

- **Rules ownership:** objective chain + timer live in `PushMapSessionService` (`Core/PushMap/`); `ObjectivePoint`/`CaptureZone` are authoring markers, no runtime self-tick
- **Session surface:** `CaptureSecondsRequired` (`Config.CaptureSeconds`, clamped ≥0.01); `CurrentObjectiveOrder` (0 = none/all captured); `IsObjectiveCaptured(order)`; `TryBeginObjectiveChain(IEnumerable<int> orders)`; `TickCapture(float dt, bool hasLivingMonsterInCurrentZone)`
- **Events:** `ObjectiveCaptured(int order)` (stop-spawn hook → PM-05); `CurrentObjectiveChanged(int newOrder)`
- **Presentation:** `PushMapStageController` collects sorted `ObjectivePoint`s; per `Update` probes living monsters in current zone (default scan `_monsters`: `IsAlive && CaptureZone.ContainsXZ`); probe via `PushMapMonsterPresenceProbe` (injectable placeholder for reset acceptance); capture logs + HUD
- **Advance (MP-04 / Approach B):** loyal soldiers share `CurrentObjective` → one `FlowFieldService` field; `PushMapAdvanceView` samples field dir + `MassMoveScheduler`/`LocalDetour` then `NavMeshAgent.Move` (**no** per-soldier per-frame `SetDestination(Objective)`); inside current `CaptureZone` stop seeking field center and soft-separation hold (`ObjectiveArriveRadius`); living monsters in capture zone do **not** pause advance (probe feeds `TickCapture` only); Rebels do not advance
- **Chase/engage (MP-05 / Approach B):** loyal soldiers in engage detect (center dist ≤ `max(weapon reach, that monster's AlertRadius)`) → `GoalKind=AttackSlot` (`AttackSlotService.TryClaim`) + LocalDetour, leave Objective field. **v0.82.57:** already in `AttackRange` (XZ) → hold and swing; else dest = closer in-range slot or inward close point. On clear release slot and resume `Objective`. No free slot and not in range → keep `Objective` on field (no hard pause). Monster chase destination is claimed slot (not target center); hold when in range. `MassMoveScheduler.SetGoal`; slot refresh ≤50/frame round-robin; death/`Release`/`ReleaseAllForTarget`
- **Demo kill (hit polish deferred):** loyal center distance to any living monster ≤ `max(monster AttackRange, soldier AttackRange) + ArriveEpsilon` → `NotifyKilled`; Boss also `TryNotifyBossKilled`
- **Defend parity (MP-06 / Approach B):** `DefendStageController` owns the same `MassMoveScheduler`+`AttackSlotService`; `WarriorAgentView` Engage chase→`AttackSlot`, no candidate→`GoalKind=FormationHome` (straight+LocalDetour; abort return on new target); `MonsterAgentView` chase uses slots; no all-units per-frame `SetDestination`/`CalculatePath`; same GoalKind semantics as PushMap
- **FlowField rebuild:** after StartBattle bake (AirWall→`StaticBoxWalkableMask`) `Configure`+`Rebuild`; `CurrentObjectiveChanged` → `Rebuild` again (log-verifiable RebuildCount); same-goal units share one field
- **StartBattle order (View):** Bake NavMesh (incl. AirWall) → walkable mask+FlowField → `TryBeginObjectiveChain` → deploy loyal soldiers (register Scheduler) → `FireStartBattleSpawns` (monsters register Scheduler+AttackSlot) → LOC rolls
- **Walkable:** sample `PushMap_Demo_01` `DigMapBounds`/`WalkSurface`/`EngageZone` must cover objectives and spawn points (incl. BossPoint)
- **Boundary:** capture probe decoupled from advance; no monsters → 1→2 switch after `CaptureSeconds`; stop-spawn is event/log only; capture loot/unlock → PM-07; hit polish deferred; stress entry → MP-07 / §9.7

```
// PM-04 / MP-04+MP-05+MP-06 rules/presentation touchpoints
session.TryBeginObjectiveChain(sortedOrders)
session.TickCapture(dt, probe.HasLivingMonster(currentZone))
event ObjectiveCaptured(order)   // PM-05 stops linked spawns; PM-07 capture loot/unlock
event CurrentObjectiveChanged(order) → FlowFieldService.Rebuild(goal, airWallMask)
PushMapMonsterPresenceProbe { HasLivingMonster(CaptureZone) }  // capture timer only
FlowFieldService + StaticBoxWalkableMask(AirWall OBBs)
AttackSlotService.TryClaim / Release / ReleaseAllForTarget
MassMoveScheduler.SetGoal(Objective|AttackSlot|FormationHome) + Tick  // ≤50 steer + ≤50 slot refresh/frame
PushMapAdvanceView { field or AttackSlot Move; no SetDestination; rebel skip }
PushMapMonsterAgentView { AttackSlot chase via scheduler; AggroMode preserved }
DefendStageController { warriors+monsters on same scheduler; FormationHome return }
WarriorAgentView / MonsterAgentView { AttackSlot or FormationHome Move; no center SetDestination }
```

**Boss-clear & reward runtime contract (Approach A / PM-07 + UI-017/018):**

- **Rules ownership:** win/lose + pending Boss count, combat elapsed, kill count, CaptureLoot ledger, loyal-wipe in `PushMapSessionService`; `BossPoint` is position-only
- **Count:** `FireRow` with `IsBoss` → `_pendingBossCount += SpawnCount`; if pending > 0 and View reports no `BossPoint` → warn
- **StartBattle stats:** record combat start time; clear kill count / CaptureLoot ledger; `MonsterKilled` increments kill count
- **Kill:** `TryNotifyBossKilled()` → at 0 → `Ended` + `VictorySettled` + `IsVictory=true` + unlocks
- **Fail:** `Shield≤0` or no living loyal → `RequestLevelFailure`; **must not** also fire `VictorySettled`
- **Rebel:** View → `SetWarriorRebel` then `TryEvaluateLoyalWipe`
- **Capture rewards:** parse `Config.CaptureLoot` → resolve through `ItemCatalogConfig` → dispatch by `ItemType` (warehouse / Spirit / MagicBook / protagonist gear) + `RecordCaptureLoot` for display; **no** Exp
- **Presentation:** outcome → Exp (win) → UI-017; Continue → UI-018 / LevelSelect; defer driver callbacks

```
session.TryNotifyBossKilled()
session.SetWarriorRebel / TryEvaluateLoyalWipe
session.RecordCaptureLoot(entries)
event VictorySettled / LevelFailureRequested
PushMapBattleSettlementView / PushMapRewardPopupView
```

**AirWall NavMesh contract (Approach A / PM-08):**

- **Authoritative block:** StartBattle runtime bake (shared `DefendNavMeshBaker`) adds, besides the IsoDiamond walkable `Mesh` source, one `NavMeshBuildSource` per map `AirWall`: `shape=Box`, `size=HalfExtents×2`, `transform=TRS(position, rotation, 1)`, `area=Not Walkable` (`NavMesh.GetAreaFromName`; fallback `1`)
- **Rotation:** Prefab `Transform.eulerAngles.y` supports 0°/45°/90°…; sample `AirWall_45` is 45°
- **Applies to:** both factions' `NavMeshAgent` pathing cannot cross (FlowField mask blocks advance dirs; monster chase still uses NavMesh); AirWall also feeds `StaticBoxWalkableMask` for FlowField
- **Wiring:** `PushMapStageController` StartBattle collects `GetComponentsInChildren<AirWall>` → `Bake(..., notWalkableBoxes)` + same OBBs for FlowField mask
- **Boundary:** **no** `NavMeshObstacle` Carve or multi-layer obstacle polish; Defend without AirWalls still uses the no-obstacle overload

```
// PM-08 bake touchpoints
DefendNavMeshBaker.Bake(center, halfExtents, notWalkableBoxes)
NavMeshBoxObstacle { Center, Size=HalfExtents*2, Rotation }
AirWall { HalfExtents }  // authoring; Y euler 0|45|90|…
```

#### 9.23 PushMapSpawnConfig

Rules: [SPEC_03 §3.14](SPEC_03_GameRules.md). One row = one spawn definition; multi-rows per SpawnPointId OK.

**Disk name:**
- **Excel:** `推图战_刷怪配置表_PushMap_PushMapSpawnConfig.xlsx`
- **CSV:** `PushMap_PushMapSpawnConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | FK → PushMapGameplayConfig |
| SpawnPointId | 刷怪点编号 | `string` or `int` | Matches Prefab marker |
| MonsterId | 怪物ID | `string` or `int` | FK → MonsterConfig |
| SpawnCount | 数量 | `int` | ≥ 1 |
| LinkedObjectiveOrder | 关联目标点序号 | `int` | Optional; stop spawn after Capture; 0=global |
| TrapZoneId | 陷阱区域编号 | `string` or `int` | Empty=non-trap StartBattle; else trap-triggered |
| IsBoss | 是否BOSS | `bool` / `0\|1` | 1=Boss clear target |
| SpawnOrder | 同点出怪顺序 | `int` | Ascending within same SpawnPointId |

```
PushMapSpawnConfig {
  GameplayConfigId: Id
  SpawnPointId: Id
  MonsterId: Id
  SpawnCount: int
  LinkedObjectiveOrder: int
  TrapZoneId: Id | ""
  IsBoss: 0 | 1
  SpawnOrder: int
}
```

**Spawn runtime contract (Approach A / PM-05):**

- **Rules ownership:** spawn eligibility + trigger state live in `PushMapSessionService` (`Core/PushMap/`); `SpawnPoint`/`TrapZone` are authoring markers only
- **Load & fire:** `TryStartBattle` loads all rows for the current `GameplayConfigId` but does **not** spawn yet. View calls `FireStartBattleSpawns()` after Bake NavMesh + deploy: non-trap rows whose linked objective is uncaptured fire `PushMapSpawnRequested` (ascending `SpawnOrder` per `SpawnPointId`); trap-bound points go `Pending`
- **Event:** `PushMapSpawnRequested(PushMapSpawnRequest)`; payload carries `SpawnPointId` / `MonsterId` / `SpawnCount` / `LinkedObjectiveOrder` / `IsBoss` / `SpawnOrder` / `Trigger` (`StartBattle` / `Trap`); **position resolved by View** via `SpawnPointId` / `BossPoint`, then staggered by `MonsterConfig.BodyRadius` vs living footprint circles (ring/spiral candidates → local `NavMesh.SamplePosition` + `basePos` leash; PM-10 / v0.73.9)
- **Trap trigger:** `TryNotifyTrapEnter(trapZoneId)` (View detects first loyal-enter) → if objective uncaptured + not yet fired → all rows for that `SpawnPointId` fire; once per point per battle
- **Capture stop:** `ObjectiveCaptured(order)` → marks points with `LinkedObjectiveOrder == order` to stop; already-spawned monsters unaffected; `IsBoss=1` rows are **not** capture-stopped (Boss in PM-07)
- **Presentation:** `PushMapStageController` collects `SpawnPoint` (`SpawnPointId`→position) and `TrapZone`; subscribes to events to instantiate monsters → `PushMapMonsterAgentView` (into `_monsters`; Boss uses `BossPoint`; positions via `PushMapSpawnSpread`); Update polls loyal (`!IsRebel`) `PushMapAdvanceView` first entry into `TrapZone` → `TryNotifyTrapEnter`
- **Footprint & avoidance (PM-10 / v0.73.9):** `PushMapSpawnSpread`: sample radius ≈`max(0.75, BodyRadius×2.5)`; hit must stay within leash of `basePos` (ring/spiral radius + slack; absolute ≈`max(3, BodyRadius×10)`); over-leash fails; packed → overlap fallback at base. Bind Warp uses same local radius ≈`max(1, BodyRadius×3)`; `_agent.radius = min(BodyRadius, max(0.05, AttackRange − soldier Demo radius 0.1 − 0.05))`; spawn spread still uses full `BodyRadius`; moving monsters use NavMesh RVO; `Stationary*` do not relocate; Defend spawn spread **out of scope** this slice; **loyal soldiers** Demo: `NavMeshAgent.radius=0.1`, `height=0.1` (`WarriorAgentView` / `PushMapAdvanceView`)
- **Demo engage→AttackSlot (MP-05 / v0.82.55 Approach C):** while loyal center distance to a living monster ≤ `max(weapon reach, that monster's AlertRadius)`, `PushMapAdvanceView` switches to `GoalKind=AttackSlot` (claim ring slot + LocalDetour `Move`; **leave** FlowField); weapon reach = `max(both AttackRange) + both BodyRadius + ArriveEpsilon`. On clear release slot and resume `Objective`; no free slot keeps `Objective` on field. **Not** map-wide EngageZone. Monster chase likewise claims slots — **forbid** all-units per-frame `SetDestination`/`CalculatePath` to target center. Hits use PM-12 scheme D (claimed AttackSlot on that monster + in `AttackRange`). **D-069 SkillCast:** loyal `Skill_03` occupies that attack channel for 3× scheme D when CD ready and in range; `Skill_01` block is an independent on-hit hook (roll before monster AA subtract; success → damage 0, still a hit); `Skill_02` Comfort is an independent outgoing-mul hook (full HP → NAP×(1+5%–25%); each burst hit checks independently); `PushMapSessionService` commits CD / post-cast LOC re-roll / block / Comfort; View does not change targeting / return-home. **D-073:** Session `SkillEffectPipeline.Dispatch` + `CombatStatusService.Tick` at existing settle points; no `SkillId` branch; CombatSkillIcon still uses `SkillIconPopup` / `SkillPersistChanged`. **SE-07:** ranged `TryConfirmRangedHit` with `ProjectileHitFlightContext` dispatches `OnProjectileHit`; `ProjectileView` generic pierce (keep current velocity after hit; Handler writes `ExtraHitsRemaining`)
- **CombatSkillIcon (D-071 / Approach A):** `PushMapSessionService` fires `SkillIconPopup(warriorId, skillId)` (`TryCommitSkillBurst` success → `Skill_03`; block success → `Skill_01`; `Skill_02` activate) and `SkillPersistChanged(warriorId, skillId, on)` (StartBattle full HP with `Skill_02` → on; HP full→not → off; not→full → on+Popup). Rules do not touch Transforms. `PushMapStageController` resolves the soldier View → `WarriorSkillIconHudView` (Prefab `Assets/Prefabs/PushMap/SkillIconHud.prefab`; `worldSize = pixelSize × 2 × camera.orthographicSize / Screen.height`; overhead 35px / persist 20px). `CombatDead` Clear immediately. Defend not wired
- **Monster AI boundary:** this slice uses Defend default-chase semantics (nearest loyal warrior / protagonist; normal attack in `AttackRange`; protagonist via `ApplyShieldHit`; warrior damage logged); AggroMode four-state deferred to **PM-06** (contract below); `MonsterAgentView` (bound to `DefendSessionService`) is not wired in
- **AggroMode runtime contract (PM-06):** `PushMapMonsterAgentView` branches on `config.AggroMode`; `ActiveChase`: loyal soldier enters `AlertRadius` → chase that soldier until death; `PassiveChase`: `_provoked=false` idle, `NotifyProvoked()` → chase; `StationaryActive`: never moves, attacks loyal soldier inside `AttackRange`, stops on leave; `StationaryPassive`: never moves, attacks only after `NotifyProvoked()` and target still in `AttackRange`. Active detection and provocation are **loyal-only** (`!IsRebel`). Provocation source (Demo): `PushMapStageController` fires a loyal `PushMapAdvanceView`'s first entry into a passive monster's `AttackRange` → `NotifyProvoked()` (stands in for "soldier attacks first"; soldier HP deferred). Hits still use `AttackMode` scheme D; active stances do **not** proactively detect the protagonist via `AlertRadius` (loyal soldiers only), but a pursued/nearest-rule protagonist hit still applies `ApplyShieldHit`. Real soldier damage on normal monsters **not** done
- **Boundary:** no `WaveSpawnConfig` countdown; Boss-clear / Exp / capture loot / dungeon unlock hooks → §9.22 PM-07 contract

#### 9.24 MagicBookConfig

Rules: [SPEC_03 §3.15](SPEC_03_GameRules.md) MagicBook / SpecialEquipSlot / EffectPhase. One row = one MagicBook. First concrete effect Restore implemented. **v0.80.8:** add `EffectParams`; `EffectPayload` is a registered token.

**Disk name:**
- **Excel:** `制造_魔法书配置表_Manufacture_MagicBookConfig.xlsx`
- **CSV:** `Manufacture_MagicBookConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| MagicBookId | 魔法书ID | `string` or `int` | PK |
| IsUnique | 是否唯一 | `int` / `0\|1` | `1` = cannot equip second copy; `0` = stackable (one slot each) |
| IsProbabilistic | 概率型 | `int` / `0\|1` | `1` = chance-trigger MagicBook; `0` = not chance-based; empty = 0. `ForceClass` `Chance` **actually rolls**; other tokens still ignore this column this round |
| EffectPhase | 生效环节 | encoding | Trigger phases; multi-value OK |
| EffectPayload | 魔法书效果 | `string` | **Registered** PascalCase token; empty = none. No Chinese / inline params in this cell |
| EffectParams | 魔法书效果参数 | `string` | Params for the token; empty = none/defaults. Encoding below |
| IconAssetId | 魔法书Icon | `string` or `int` | UI icon asset Id |
| DisplayName | 魔法书名称 | `string` | Display name / i18n Key |
| Description | 魔法书介绍 | `string` | Display copy |
| VisualStyleId | 特效外观ID | `string` or empty | **Not a token.** Empty = no visual. Non-empty → `WarriorVisualStyleCatalog` StyleId. `Style_ScaleModel` (alias `放大模型`) is the **scale channel** (no `.mat` required); other Ids are AllIn1 **material** (mats under `Assets/Materials/AllIn1/`). Baked only on `EffectPayload` **hit**. **Edit Excel then Bake Mode2 Tables**; do not edit CSV only. Quote the cell if `ClassId` contains commas (Bake escapes). New Style mats/Catalog: [§15.2](#152-项目落盘目录) |
| VisualPriority | 特效优先级 | `int` | Missing/empty = 0. Material channel only: higher replaces style and resets Intensity; same StyleId adds Intensity; lower ignored. Scale channel **ignores** this column. Demo material: skill 10, enhance 20, advance 30 |
| VisualIntensityAdd | 特效强度加算 | `float` | Missing/empty = **1**. Material: written on replace; added on same style; View MPB-multiplies registered float props. Scale: the model factor k; on hit `VisualModelScale *= k` (`k≤0` treated as 1) |

**`EffectPhase` encoding (fixed):** `Phase` or `Phase|Phase|…`. Enums at least: `SoldierManufacture` \| `Combat` (Combat not implemented this round).

**`EffectPayload` encoding (fixed):**

| Rule | Notes |
|------|-------|
| Syntax | Single PascalCase token (`^[A-Za-z][A-Za-z0-9]*$`); empty = none |
| Registry | Token **must** appear in the catalog below; unknown → empty apply + Warning, not treated as an effect |
| One token per book | Exactly one token per row; multi-effects = multi books/slots, not concatenated tokens |
| Reusable | Multiple `MagicBookId`s may share a token; differ via `EffectParams` |
| Display split | `DisplayName` / `Description` do **not** drive rules |

**`EffectParams` encoding (fixed, v0.80.9):** `Key=Value` or `Key=Value|Key=Value|…` (pipe; same delimiter as `EffectPhase`).

| Rule | Notes |
|------|-------|
| Empty | No params / token defaults (e.g. `RaceWeightPick`) |
| Key | PascalCase; **must** be allowed for that token; unknown Key → Warning and skip pair |
| Value | Trim on load; must not contain `=` or `\|` (no escape) |
| Duplicate Key | Config error: Warning, **last wins** (Demo) |
| Missing required Key | Treat book as invalid: empty apply + Warning (required Keys listed per token) |

**`EffectPayload` catalog (authoritative; register a token here before filling rows / writing handlers):**

| Token | Phase | Allowed EffectParams Keys | Semantics |
|-------|-------|---------------------------|-----------|
| `RaceWeightPick` | `SoldierManufacture` | (none; must be empty) | Restore: weight-1 RaceId pick among chosen Head/Torso/2×Arm/2×Leg |
| `ForceRace` | `SoldierManufacture` | **required** `RaceId` (`RaceConfig` PK) | Force finalized race; **probe before finalize**; beats `RaceWeightPick`; multiple left→right last wins; missing/illegal `RaceId` → book invalid, skip |
| `ForceClass` | `SoldierManufacture` | **required** `ClassId` (target, `ClassConfig` PK); **optional** `RequireClassId`, `Chance` | Hook left→right. Rewrites class: `ClassId` + reload `ClassName` / `AttackMode` (`DefaultAppearanceId` / `PlacementOrder` follow new ClassId). `RequireClassId`: if set, attempt only when current `draft.ClassId` exact-matches (must exist in `ClassConfig`; illegal → book invalid); mismatch → skip (not invalid). `Chance`: [0,1]; default 1; rewrite only if `Random.value < Chance`; illegal/out of range → book invalid. Missing/illegal target `ClassId` → book invalid, keep current class |
| `StatMul` | `SoldierManufacture` | **required** `Stat`, `Mul`; **optional** `ClassId` | Hook left→right. `Mul`≥0; missing/illegal → book invalid. `Stat`∈`MaxHP`/`MoveSpeed`/`Strength`/`Agility`/`Intelligence`/`All`/`Primary`. Optional `ClassId`: comma-separated PKs (OR); if set, apply only when `draft.ClassId` **matches any** (each Id must exist in `ClassConfig`; any illegal → book invalid); omit = all soldiers. Five-dim or `All`: `Base(S)*=Mul`. `Stat=Primary`: `S`=current `ClassConfig.PrimaryStat`; `BodySum(S)=Σ` consumed BodyPart `StatBonus(S)` (no race/gem/equip; not already-mutated Base); `Base(S)+=(Mul−1)×BodySum(S)`; stackable = each book adds once against the same BodySum. Baked into Base then StaticStat; lasts until PermanentDeath |
| `StatAdd` | `SoldierManufacture` | **required** `Stat`, `Add` | Hook: `Base(S) += Add`. `Stat`∈ five dims/`All` (**not** `Primary`). `Add` must parse as a number (may be negative; StaticStat still `max(0,·)`); missing/illegal → book invalid |
| `QualityDelta` | `SoldierManufacture` | **required** `Delta` (int, may be negative) | Appearance: `AvgLevelInt = round(mean BodyLevel) + ΣDelta`; no re-pick, no Base change; Deltas sum; missing/non-int `Delta` → book invalid (contributes 0) |
| `SoldierSkillLevelAdd` | `SoldierManufacture` | **required** `SkillId`, `Delta` (int, may be negative) | **Second pass** (after first hook incl. `ForceClass` and `DefaultSkillIds` grant): if the instance **already has** that `SkillId`, `SkillLevel += Delta`; clamp to min/max `SkillLevel` rows in `SkillConfig` for that Id; if missing, skip (no new grant). Multiple books left→right. Missing/illegal keys → book invalid. Mode1 manufacture does not run this token |

**Defined rows (examples):**
- `MagicBook_Restore` | … | empty `VisualStyleId` (hand-check scale: `Style_ScaleModel` / Add=`1.5`) | DisplayName=`还原`
- `MagicBook_WarriorEnhance` | `ClassId=Class_BaseWarrior,Class_Warrior` | `VisualStyleId=Style_WarriorGlow` | `VisualPriority=20` | `VisualIntensityAdd=1` | DisplayName=`战士强化`
- `MagicBook_SoldierSkillLevel` | … | `VisualStyleId=Style_SkillAberration` | `VisualPriority=10` | `VisualIntensityAdd=1` | DisplayName=`士兵技能升级`
- `MagicBook_WarriorAdvance` / `ArcherAdvance` / `MageAdvance` / `RogueAdvance` | … | `VisualStyleId=Style_AdvanceOutline` | `VisualPriority=30` | `VisualIntensityAdd=1` (`ForceClass` **hit** only)

```
MagicBookConfig {
  MagicBookId: Id
  IsUnique: 0 | 1
  IsProbabilistic: 0 | 1          // 1 = chance-trigger; ForceClass Chance rolls
  EffectPhase: "SoldierManufacture|Combat|..."
  EffectPayload: string           // registered token | empty
  EffectParams: string            // Key=Value|Key=Value|… | empty
  IconAssetId: Id
  DisplayName: string
  Description: string
  VisualStyleId: Id | ""          // AllIn1 preset or Style_ScaleModel; empty = no visual; not a token
  VisualPriority: int             // default 0; material channel only
  VisualIntensityAdd: number      // default 1; material intensity or scale k
}
```

**Protagonist special equip slots (persistence intent):**

- Default **6** slots: `Gravedigger2026.SaveSlot.{i}.CampaignMode{1|2}.SpecialEquipSlots` — JSON `MagicBookId[6]` (empty = `""`)
- Equip gate: free slot; reject if book `IsUnique=1` and same Id already equipped; **Demo**: missing config row still allows write for empty-table persistence handcheck (Warning; treat as stackable)
- AutoManufacture: **no MagicBook at craft** (default race + hand-class `DefaultSkillIds`). UI-016 Step2 pulse peak: `ApplyEquippedBookAtSlot(warrior, slotIndex)` for that slot only (`RaceWeightPick` / `StatMul` / `ForceClass` / `SoldierSkillLevelAdd`; `ForceClass` hit Clear+re-grant skills; unimplemented empty apply + log); then `RefinalizeInstance`; deploy by final ClassId after all soldiers. Fail/`Exit`: `ApplyRemainingSlots`. Dropped pre-finalize Restore probe and skill second pass
- **Impl:** `SpecialEquipSlotsService` (`TryEquip` + `TrySwap` + `TryUnequip` + `Changed`) + `SoldierManufactureMagicBookHook.ApplyEquippedBookAtSlot` / `ApplyRemainingSlots`; `AutoManufacturePresentationController` pulse-peak callback and `Changed` refresh of shared `BookRow.prefab`; `MagicBookSlotsPanelView` popup delete (D-072); `AutoManufactureStageModule` deferred Deploy; MetaShell bind; hand-check Tools Grant MagicBook + UI-023 drag/delete

#### 9.25 ProtagonistEquipmentConfig

Rules: [SPEC_03 §3.16](SPEC_03_GameRules.md) protagonist equipment warehouse / same-Id convert / `EquipCommonExp` / levels / Dig stacking. One row = one `EquipId` at one level. **Parallel** to `MagicBookConfig` (§9.24), `ExtraEquipmentConfig` (§9.14), and material `Warehouse`.

**Disk name:**
- **Excel:** `主角_装备配置表_Protagonist_ProtagonistEquipmentConfig.xlsx`
- **CSV:** `Protagonist_ProtagonistEquipmentConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| EquipId | 装备ID | `string` or `int` | Composite PK part |
| EquipLevel | 装备等级 | `int` | Composite PK part; starts at **1** |
| DisplayName | 装备名称 | `string` | Display name / i18n Key |
| IconAssetId | 装备图标 | `string` or `int` | UI icon asset Id |
| ExpToNextLevel | 升下一级经验 | `int` | Exp needed to reach `EquipLevel+1`; empty or ≤0 → max-level row |
| ConvertExpValue | 转化经验值 | `int` | Exp granted when acquiring duplicate `EquipId` (at max level → `EquipCommonExp`, §3.16) |
| EffectDomain | 装备生效功能 | encoding | `Dig` \| `SoldierManufacture` \| `Combat`; multi-value OK |
| EquipEffect | 装备效果 | `string` | Dig domain: same style as [§9.17](#917-科技项效果配置表-techeffectconfig) `AttributeModifiers` `Attr_Value\|…`. **Static keys** (`DigDamage` / `DigDurationReductionSum` / `DigCursorRadius` / `DigStageDurationBonus` / `GraveSpawnWeightBonus_{QualityId}` / `DigProcessSpawnCountBonus`) merge into Dig caps; **event keys** `DigOnGraveClear` (clear-grave trigger chance, `_1`=100%) and `ExplosiveThrowRadius` / `ExplosiveBlastRadius` / `ExplosiveBlastDamage` / `ExplosiveFlightSec` / `ExplosiveFuseSec` / `ExplosiveRingSec` **do not** merge into caps (parsed by `DigExplosiveEffectConfig`, D-077); `DigLightningIntervalSec` / `DigLightningFrameSec` / `DigLightningPreviewSec` **do not** merge into caps (parsed by `DigLightningEffectConfig`, D-078); `SoldierManufacture` / `Combat` Token registry **TBD** (separate `ProtagonistEquipEffect`, **not** MagicBook `EffectPayload`); empty = none |
| Description | 装备描述 | `string` | Display copy |

**`EffectDomain` encoding (fixed):** `Domain` or `Domain|Domain|…`. Enums: `Dig` \| `SoldierManufacture` \| `Combat`.

**Composite PK:** `(EquipId, EquipLevel)` unique; levels for one `EquipId` should be contiguous from 1 (load may validate + Warning).

**Demo sample rows:** `Equip_IronShovel` (Iron Shovel) L1–5, `EffectDomain=Dig`; +10% of base 0.6 per level → L1 `DigCursorRadius_0.06` … L5 `_0.30`; `ExpToNextLevel` L1–4 = 1, L5 empty; `ConvertExpValue=1`. `Equip_MinerLamp` (Miner Lamp) L1–5, `EffectDomain=Dig`; Q4/Q5/Q6 spawn-weight cumulative +10 per level → L1 `GraveSpawnWeightBonus_Q4_10|GraveSpawnWeightBonus_Q5_10|GraveSpawnWeightBonus_Q6_10` … L5 `_50`; ExpToNext/ConvertExp=1. `Equip_Explosives` (Explosives) L1–5, `EffectDomain=Dig`; `DigOnGraveClear_1|ExplosiveThrowRadius_4|ExplosiveBlastRadius_2|ExplosiveBlastDamage_{13/18/23/28/33}|ExplosiveFlightSec_0.5|ExplosiveFuseSec_0.8|ExplosiveRingSec_0.5`. `Equip_Elctr` (Lightning / 引雷) L1–5, `EffectDomain=Dig`; `DigLightningIntervalSec_{15/13/11/9/7}|DigLightningFrameSec_0.05|DigLightningPreviewSec_2`. `Equip_Detector` (Detector) L1–5, `EffectDomain=Dig`; L1 `DigProcessSpawnCountBonus_1` … L5 `_5` (process-spawn M bonus; does not change N). Former sample `Equip_DigRing` **removed**.

```
ProtagonistEquipmentConfig {
  EquipId: Id
  EquipLevel: int                 // ≥ 1
  DisplayName: string
  IconAssetId: Id
  ExpToNextLevel: int             // ≤0 or empty = max level row
  ConvertExpValue: int
  EffectDomain: "Dig|SoldierManufacture|Combat|..."
  EquipEffect: "Attr_Value|..."   // Dig static keys → caps; DigOnGraveClear / Explosive* / DigLightning* → event parser
  Description: string
}

OwnedEquip {
  EquipId: Id
  Level: int
  CurrentExp: number
}

// Persistence (SaveSlot + CampaignMode; PE-02):
//   EquipCommonExp: number
//   ProtagonistEquipmentWarehouse: OwnedEquip[]
```

#### 9.26 FormationBondConfig

Rules: [SPEC_03 §3.17](SPEC_03_GameRules.md) formation bonds. One row = one `BondId` at one level. Composite PK `(BondId, BondLevel)`. `BondBuff` FK → [§9.21b `SkillEffectConfig`](#921b-技能效果配置表-skilleffectconfig) (naming: `BondEffect_{Name}_{Level}`). Demo slice: handlers not wired; UI shows `Description` + linked Effect `Notes`.

**Disk name:**
- **Excel:** `战斗_阵容羁绊配置表_Combat_FormationBondConfig.xlsx`
- **CSV:** `Combat_FormationBondConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| BondId | 羁绊ID | `string` | Composite PK part |
| BondLevel | 羁绊等级 | `int` | Composite PK part; ≥1; same Id levels mutually exclusive (§3.17) |
| DisplayName | 羁绊名称 | `string` | UI display |
| IconAssetId | 羁绊图标 | `string` | Runtime `Resources/UI/Bonds/{IconAssetId}`; placeholder if missing |
| Description | 羁绊介绍 | `string` | UI read-only; **not** an effect parser input |
| ActivationCondition | 羁绊激活条件 | `string` | Structured DSL (§3.17); validated at load |
| BondBuff | 羁绊Buff | `string` | FK → `SkillEffectConfig.SkillEffectId` |

**`ActivationCondition` encoding (v1 Kind whitelist):** `DeployClassCount` (`BaseClass` **or** `ClassId`, not both; `Min`), `DeployRaceCount`, `DeployTotalCount`, `DeployPrimaryStatCount` — see Chinese §9.26 table.

**Demo sample rows:** same as Chinese §9.26 table (`Bond_IronWall` L1–2, `Bond_HumanLegion`, `Bond_FullArmy`, `Bond_StrStrength`, `Bond_PreciseClass`).

```
FormationBondConfig {
  BondId: Id
  BondLevel: int
  DisplayName: string
  IconAssetId: Id
  Description: string
  ActivationCondition: string
  BondBuff: Id
}
```

#### 9.27 商店商品池配置表 `Shop_ShopPoolConfig`

Rules: Mode2 商店“商品池”定义。一个 `ShopPoolId` 在 `RequiredMaxLevelNumber` 解锁门槛达到后变为可用；在生成待售商品时，所有已解锁池的 `PoolItemsRaw` 会被解析为归类 A/B 候选并参与加权抽样。

**ExtraUnlockCondition** 预留字段：本片先按“总为 true”处理（空串或任意串均放行），用于后续扩展额外解锁条件。

**Disk name:**
- **Excel:** `商店_商店商品池配置表_Shop_ShopPoolConfig.xlsx`
- **CSV:** `Shop_ShopPoolConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| ShopPoolId | 商品池ID | `string` | PK |
| RequiredMaxLevelNumber | 解锁关卡进度门槛 | `int` | 与 `ShopProgress.maxUnlockedLevelNumber` 比较（>= 才解锁） |
| ExtraUnlockCondition | 额外解锁条件 | `string` | 预留；本片 always-true |
| PoolItemsRaw | 商品池道具（含归类与权重） | `string` | 编码：`itemId;category;weight|itemId;category;weight|...` |

**`category` 编码（fixed）**：`A` = 装备（对应 `ProtagonistEquipmentConfig.EquipId`），`B` = 魔法书（对应 `MagicBookConfig.MagicBookId`）。

**`PoolItemsRaw` 解析规则（fixed）**：
- 段分隔符：`|`；段内 3 元素分隔：`;`
- `weight` 须为非负；`weight=0` 视为该项不存在（忽略）
- 同一 `itemId` 与同一 `category` 可在同池内重复；生成待售商品时，权重会被汇总到临时 `byItemIdTotalWeight`（跨池也会汇总；见 §3.5 / SPEC_03）

**Demo sample rows:** 本片不额外填表；以 Excel/CSV 实际内容为准。

```
Shop_ShopPoolConfig {
  ShopPoolId: Id
  RequiredMaxLevelNumber: int
  ExtraUnlockCondition: string
  PoolItemsRaw: string           // itemId;category;weight|...
}
```

#### 9.28 刷新商品配置表 `Shop_ShopRefreshPriceConfig`

Rules: 商店手动刷新“定价表”。商店打开并发生新关卡解锁后，刷新次数从 0 开始；每次点击「刷新商品」会把 `currentRefreshCount` 推进到下一行，并按该行 `RefreshPrice` 扣除 `SpiritEssence`。

**Disk name:**
- **Excel:** `商店_刷新商品配置表_Shop_ShopRefreshPriceConfig.xlsx`
- **CSV:** `Shop_ShopRefreshPriceConfig.csv`

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| RefreshCount | 刷新次数 | `int` | 主键；建议从 1 开始（对应第一次手动刷新） |
| RefreshPrice | 刷新价格（精魂） | `int` | 扣款；`0` 允许用于 Demo 免费刷新 |

**缺行策略（fixed）**：若 `currentRefreshCount+1` 行缺失，则「刷新商品」按钮置灰/不可点击。

```
Shop_ShopRefreshPriceConfig {
  RefreshCount: int
  RefreshPrice: int
}
```

## 10. Mode2 商店系统（Shop；D-075）

### 简体中文
#### 10.1 Prefab / 运行时装配
Prefabs：`Assets/Prefabs/Shop/ShopStageRoot.prefab`  
Catalog：`Assets/Settings/Shop/ShopPrefabCatalog.asset`（`ShopAssetBuilder` 生成并接线 `MetaShellRoot`）

装配约定：
- `ShopStageRoot` 为 **全屏** Canvas Root（内容区 Rect stretch 铺满，**禁止** 980×650 居中弹窗）；`sortingOrder` ≥ 200。
- **关卡阶段：** `GameplayType=Shop` → `ShopStageModule`（`IStageModule`）Enter Instantiate Prefab、Exit Destroy。关闭/继续 → 回调 Meta → `TryAdvanceStage`（无独立阶段结算）。`GameplayConfigId` **忽略**。
- **局外 overlay：** InSaveShell 左下「商店」Instantiate **同一** Prefab；关闭仅销毁，不推进关卡。当前阶段已是 Shop 时按钮 **no-op**。
- Prefab 挂载 `ShopStageRootView`，负责：
  - 左侧“玩家信息”（SpiritEssence + `EquipSummaryText`/`MagicBookSummaryText` + 其下已拥有装备/魔法书 ICON；点占用 ICON 在下方显示「出售」+ `SellPrice`，确认后出售，D-076）
  - 右侧“待售商品”（6 项 slot0..5 + 每项道具图标/名称/价格/购买按钮；图标经 `ItemIconLoader` 按归类 A/B 分别从 `Resources/UI/Equipment/`、`Resources/UI/MagicBooks/` 加载）
  - 底部“刷新商品”按钮（显示下一次刷新价格并根据可用性置灰）
  - Shop 打开（首次渲染）前应优先调用 `ShopOfferRefreshService.TryAutoRefreshOnceIfPending(progress, configs)`
  - 关闭按钮（销毁实例；阶段路径由 Module Exit 兜底 Destroy）
  - 出售确认 `ConfirmDialog`：`overrideSorting` ≥ 201（商店 Canvas ≥ 200）

#### 10.2 脚本 / Service 分层（最小职责）
建议拆为以下 Service（纯 C# 或非 MonoBehaviour）与 View 绑定，职责与现有风格对齐（`DungeonUnlockService` / `RewardGrantService` / `ProtagonistEquipmentService`）。

1. `ShopProgressService`
   - 绑定当前 SaveSlot + CampaignMode（本片 Mode2）
   - 管理 `ShopProgress` 快照并持久化（PlayerPrefs；键见 §6/持久化意图）
   - 对外提供：是否应触发本次“新关卡解锁”开放、当前 offers、currentRefreshCount 等只读状态
   - `OnLevelCleared(newMaxLevelNumber)`：仅设置 `pendingOpenOnNewUnlock=true` 并清空/重置 offers；最终 offers 生成由 `ShopOfferRefreshService.TryAutoRefreshOnceIfPending` 完成

2. `ShopOfferGenerator`
   - 输入：`ShopProgress.maxUnlockedLevelNumber`
   - 读取 `Shop_ShopPoolConfig`，按解锁门槛得到已解锁 pools
   - 解析每个 pool 的 `PoolItemsRaw`，对归类 A/B 分别汇总 `byItemIdTotalWeight`
   - 对每个归类随机抽取 3 个不重复 `itemId`，不足则留空，不补齐

3. `ShopRefreshPriceResolver`
   - 输入：`currentRefreshCount`
   - 读取 `Shop_ShopRefreshPriceConfig` 给出下一次刷新价格
   - 若缺行则返回“不可刷新”状态供 View 置灰

4. `ShopPurchaseService`
   - 输入：slotIndex、offer itemId、category、priceSpirit
   - 校验：SpiritEssence 足够、slot 未 sold、物品 ID 与 category 映射合法
   - 扣除精魂并入账/入仓：
     - category A：调用 `ProtagonistEquipmentService.TryAcquire(itemId)`
     - category B：调用 `SpecialEquipSlotsService.TryEquip(itemId)`
   - 标记 slot 为 sold 并写回 `ShopProgress`

5. `ShopSellService`（D-076）
   - `TrySellEquipment(equipId)`：校验 `ItemCatalogConfig.SellPrice ≥ 0` → `ProtagonistEquipmentService.TryRemove` → `Warehouse.AddSpirit(SellPrice)`；整件删除，不退 `CurrentExp`/`EquipCommonExp`
   - `TrySellMagicBook(slotIndex)`：校验 catalog → `SpecialEquipSlotsService.TryUnequip` → `AddSpirit(SellPrice)`（不补位、无仓库）
   - 失败不改库存/精魂；View 只派发点击与 ConfirmDialog

6. `ShopStageModule`
   - `HandledState = GameplayState.Shop`
   - Enter：Instantiate Catalog Prefab → `Bind` + `Open`；Exit：Destroy
   - View `Closed`：阶段路径 `TryAdvanceStage`；overlay 路径仅清引用

7. 与关卡解锁的集成点
   - 在 Mode2 PushMap 的 Boss 通关结算流程中，`Meta` 层在准备进入 `LevelSelectPanel` 前，
     - 读取当前关卡 `LevelId` 对应的数字“最大通过关卡号”
     - 调用 `ShopProgressService.OnLevelCleared(levelMaxNumber)`：
      - 若该数值比已记录更大，设置 `pendingOpenOnNewUnlock=true` 且清空 offers（安全准备）
      - 然后由 `Meta` 层立刻调用 `ShopOfferRefreshService.TryAutoRefreshOnceIfPending(progress, configs)` 完成 offers 生成与 pending 消化（自动刷新不消耗刷新价格；刷新次数复位为 0）

变更原因/影响：商店从壳层 overlay 升为可配置玩法类型；offers 生成仍只走 `TryAutoRefreshOnceIfPending`。

### English
Shop StageRoot prefab: `Assets/Prefabs/Shop/ShopStageRoot.prefab` (full-screen stretch Canvas; `ShopAssetBuilder` + `ShopPrefabCatalog`). Dual entry: Level `GameplayType=Shop` via `ShopStageModule` (close → `TryAdvanceStage`; ignore `GameplayConfigId`); InSaveShell overlay instantiates the same prefab (close does not advance; no-op while Shop stage is open).

Service split (minimum responsibilities):
- `ShopProgressService`: persist snapshot (slot+Mode2) via PlayerPrefs (see §6).
- `ShopOfferGenerator`: unlock pools from `Shop_ShopPoolConfig`, parse `PoolItemsRaw`, sum weights by identical `itemId` per category (A/B), then pick 3 distinct items per category.
- `ShopRefreshPriceResolver`: read next `RefreshPrice` from `Shop_ShopRefreshPriceConfig`; missing row disables refresh.
- `ShopPurchaseService`: validate SpiritEssence and slot state, deduct Spirit, grant items by category (A→`ProtagonistEquipmentService.TryAcquire`, B→`SpecialEquipSlotsService.TryEquip`), then mark sold + persist.
- `ShopSellService` (D-076): sell owned equipment/MagicBook for `ItemCatalogConfig.SellPrice` Spirit via `TryRemove` / `TryUnequip`; View shows ICONs + floating Sell under selection; ConfirmDialog sorting ≥ 201.
- `ShopStageModule`: Instantiate/Destroy prefab; stage close advances.

Unlock integration: on Mode2 PushMap boss-clear settlement, Meta calls `ShopProgressService.OnLevelCleared(levelMaxNumber)` and (when OnLevelCleared returns true) immediately calls `ShopOfferRefreshService.TryAutoRefreshOnceIfPending(progress, configs)` to generate offers once and clear pending (free, no refresh-price deduction).

## 13. 资源编排与可扩展性

### 简体中文

**原则（强制倾向）：预制体优先（Prefab-first）。** 实际代码与场景开发中，凡会以 GameObject 层级出现的玩法实体、可复用 UI、可生成物、可摆放交互物，**默认用 Prefab + 挂载 Controller** 制作与引用，放在 `Assets/Prefabs/<模块>/`。优先在编辑器中拼装 Prefab，再由代码 `Instantiate` / 引用槽位驱动；**避免**在代码里动态 `new GameObject` 拼层级，或在多个 Scene 中手工复制同一套层级。

**适用默认 Prefab 的典型对象：** 主角/圆圈光标、坟墓（含障碍半径）、奖励飞字、工具面板与可复用面板、关卡内可生成物、战斗主角/士兵/怪物、**DigMap / BattleMap（含 EngageZone；共用 `Ground_01`…`Ground_05`）** 等。Dig 模块建议路径：`Assets/Prefabs/Dig/`；UpgradeManufacture 模块建议路径：`Assets/Prefabs/UpgradeManufacture/`（`UpgradeManufactureStageRoot`；Mode2：`UpgradeManufactureStageRoot_Mode2`）；Formation：`Assets/Prefabs/Formation/`（`FormationEditorRoot`；Mode2：`FormationEditorRoot_Mode2`）；AutoManufacture 模块建议路径：`Assets/Prefabs/AutoManufacture/`（`AutoManufacturePresentationRoot`；Catalog/`AmAssetBuilder` 或运行时 Build；UI-016 / D-055）；Shop 模块建议路径：`Assets/Prefabs/Shop/`（`ShopStageRoot`；Catalog/`ShopAssetBuilder`；UI-026 / D-075）；**地图变体**统一路径：`Assets/Prefabs/Maps/{Ground_0N}.prefab`（`DigMapId` / `BattleMapId` 均解析至此）。布阵用地图 Prefab 另须支持 **`FormationClassZone`** 标记：`ClassId:string`（精确匹配 `ClassConfig.ClassId`）+ **IsoDiamond**（与 `WalkSurface` / `EngageZone` 同形：`HalfExtents` = 菱形顶点到中心；Contains `|dx|/hx+|dz|/hz≤1`；父/子 **localRotation=identity**，废止 IsoTileYaw）（**D-057：** Demo Ensure 以 Mode2 `Manufacture_ClassConfig` 覆盖 **全部** ClassId，缺区则自动上阵留池；样例 `Ground_*` 父节点 `FormationClassZones` 与子区 identity；已有区保留世界 XZ；样例 HalfExtents 锁定 `(3.85, 2)`；表外区删除；作者向 `MeshFilter`+`MeshCollider`+`MeshRenderer.enabled=false`，Play 模式 Collider 关闭；Contains 走菱形数学、不进 NavMesh；`FormationClassZonesRoot` 选中层级画 IsoDiamond Gizmo 对齐 `DigMapBounds`；第二前排 z=−1.9：`Class_Guardian`/`Class_Brawler`/`Class_Shadowblade`（空隙补 `Class_Warrior_0`/`Class_Rogue_0`），第二后排 z=+1.7：`Class_Longbowman`/`Class_BombMaster`/`Class_IceMage`/`Class_FireMage`/`Class_DarkMage`，第三后排 z=+2.4：`Class_Archer_0`/`Class_Mage_0`；Ensure 写 identity 并补 Mesh 组件）；坐标相对 `DigMapBounds` 中心，与 `BattleFormation` 一致；Snapshot **不含** `RotationYDegrees`；Mode2 AutoManufacture（AM-06 方案 A）按 `PlacementOrder` 落入对应区并以区内螺旋 + `BodyRadius` 挤开（菱形内缩；[SPEC_03 §3.15](SPEC_03_GameRules.md)）。PushMap `MapId` 亦可为 `PushMap_*`，同目录解析；Demo 样例 **`PushMap_Demo_01`**（Editor Ensure，见 §9.22）；PushMap 地图 Prefab 另须支持标记：`ObjectivePoint`/`CaptureZone`/`AirWall`（可 45°；开战 Bake 注入 Not Walkable Box，见 §9.22 PM-08）/`SpawnPoint`/`TrapZone`/`BossPoint`/`CameraFollowPath`（见 §9.22 / [SPEC_03 §3.14](SPEC_03_GameRules.md)）。地图**表现**为 Unity **Isometric Tilemap**（Grid `CellLayout=Isometric`，Demo `CellSize≈(1,0.5,2)`，Grid 旋转使砖面落在 XZ，配合 Dig/Defend 正交顶视相机）；Tile/Sprite 源在 `Assets/Art/Maps/Tiles/`（自 Example Scene `Environment/Tiles`+`Sprites` 复制）；Prefab 另含不可见 `WalkSurface`（**IsoDiamond**：XZ 菱形薄网格，供 Demo NavMesh 约定）、`DigMapBounds` / EngageZone（同形菱形足迹；半尺寸=`PaintRadius*(cellSize.x,cellSize.y)`）及刷怪点。Editor 可用 Tile Palette 手刷，或 Builder 程序铺默认图案；**禁止**运行时直接引用 `SmallScaleInt/`，见 [§15](#15-角色美术管线character-creator-烘焙整角)。工程须含 `com.unity.2d.tilemap`（编辑器刷砖）。角色视觉 Prefab 约定：`Digger` → `Assets/Prefabs/Dig/Digger.prefab`；`BattleProtagonist` → `Assets/Prefabs/Defend/BattleProtagonist.prefab`；士兵 → `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`；怪物 → `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab`（美术管线见 [§15](#15-角色美术管线character-creator-烘焙整角)）。

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

**Typical Prefab targets:** Digger / circle cursor, Graves (with obstacle radius), DigReward VFX/UI, ToolsPanel and reusable panels, in-level spawnables, BattleProtagonist / Soldiers (Warrior) / Monsters, **DigMap / BattleMap (incl. EngageZone; shared `Ground_01`…`Ground_05`)**. Dig module path: `Assets/Prefabs/Dig/`; UpgradeManufacture module path: `Assets/Prefabs/UpgradeManufacture/` (`UpgradeManufactureStageRoot`; Mode2: `UpgradeManufactureStageRoot_Mode2`); Formation: `Assets/Prefabs/Formation/` (`FormationEditorRoot`; Mode2: `FormationEditorRoot_Mode2`); AutoManufacture module path: `Assets/Prefabs/AutoManufacture/` (`AutoManufacturePresentationRoot`; Catalog/`AmAssetBuilder` or runtime Build; UI-016 / D-055); **map variants** unified path: `Assets/Prefabs/Maps/{Ground_0N}.prefab` (`DigMapId` / `BattleMapId` both resolve here). Formation maps also support **`FormationClassZone`** markers: `ClassId:string` (exact `ClassConfig.ClassId`) + **IsoDiamond** (same as `WalkSurface` / `EngageZone`: `HalfExtents` = vertex-to-center; Contains `|dx|/hx+|dz|/hz≤1`; parent/child **localRotation=identity**, IsoTileYaw dropped) (**D-057:** Demo Ensure covers **every** Mode2 `Manufacture_ClassConfig.ClassId`; no zone → auto-deploy stays in pool; sample `Ground_*` parent `FormationClassZones` and children identity; existing zones keep world XZ; sample HalfExtents locked to `(3.85, 2)`; orphans removed; authoring `MeshFilter`+`MeshCollider`+`MeshRenderer.enabled=false`, Collider off in Play; Contains is diamond math, not NavMesh; `FormationClassZonesRoot` draws IsoDiamond gizmo in selection hierarchy matching `DigMapBounds`; second front row z=−1.9: `Class_Guardian`/`Class_Brawler`/`Class_Shadowblade` (gaps: `Class_Warrior_0`/`Class_Rogue_0`), second back row z=+1.7: `Class_Longbowman`/`Class_BombMaster`/`Class_IceMage`/`Class_FireMage`/`Class_DarkMage`, third back row z=+2.4: `Class_Archer_0`/`Class_Mage_0`; Ensure writes identity and mesh components); coords relative to `DigMapBounds` center (same as BattleFormation); Snapshot **omits** `RotationYDegrees`; Mode2 AutoManufacture (AM-06 Approach A) deploys by `PlacementOrder` with in-zone spiral + `BodyRadius` separation (diamond shrink; [SPEC_03 §3.15](SPEC_03_GameRules.md)). PushMap `MapId` may also be `PushMap_*` in the same folder; Demo sample **`PushMap_Demo_01`** (Editor Ensure; marker contract §9.22; `AirWall` StartBattle bake → Not Walkable Box, §9.22 PM-08; `CameraFollowPath` rail for Combat Auto camera). Map **presentation** is Unity **Isometric Tilemap** (`CellLayout=Isometric`, Demo `CellSize≈(1,0.5,2)`, Grid rotated onto XZ for Dig/Defend orthographic top-down); Tile/Sprite sources under `Assets/Art/Maps/Tiles/` (copied from Example Scene `Environment/Tiles`+`Sprites`); Prefab also has invisible `WalkSurface` (**IsoDiamond**: thin XZ diamond mesh for Demo NavMesh), `DigMapBounds` / EngageZone (same diamond footprint; half-extents=`PaintRadius*(cellSize.x,cellSize.y)`), spawn points. Editor: Tile Palette hand-paint or Builder default fill; **do not** runtime-reference `SmallScaleInt/` — [§15](#15-角色美术管线character-creator-烘焙整角). Require `com.unity.2d.tilemap` for editor painting. Character visual Prefabs: `Digger` → `Assets/Prefabs/Dig/Digger.prefab`; `BattleProtagonist` → `Assets/Prefabs/Defend/BattleProtagonist.prefab`; soldiers → `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`; monsters → `Assets/Prefabs/Defend/Monsters/{ModelId}.prefab` (art pipeline: [§15](#15-角色美术管线character-creator-烘焙整角)).

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

**状态：打表工具已实现（方案 A）；Excel 三行表头（方案 B）已约定；数值单元格 CSV 序列化（禁止浮点噪声）已约定。**

凡 **§9** 及后续新增的**配置表**，统一遵守本节。

#### 14.1 统一路径（两文件夹）

```
Gravedigger2026/Assets/ConfigTables/
├── Excel/           # Mode1 人配源表（.xlsx）；仅作源文件，运行时不加载
├── Csv/             # Mode1 程序读表（.csv）；打表产物；Mode1 运行时数据源
└── Mode2/
    ├── Excel/       # Mode2 人配源表（本片先整包复制自 Mode1）
    └── Csv/         # Mode2 程序读表；Mode2 运行时数据源
```

- **`CampaignMode.Mode1`** 读 `ConfigTables/Csv/`；**`CampaignMode.Mode2`** 读 `ConfigTables/Mode2/Csv/`（[§14.5](#145-运行时-csv-加载路径demo)）。
- 新配置表**禁止**散落在 `Assets/Settings/<模块>/` 或其他路径。
- Excel 导入设置：`.xlsx` 可为 DefaultAsset；SPEC 要求：Excel **不作为运行时资源**读取。

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
| `PushMap` | 推图战 | `PushMap_PushMapGameplayConfig`、`PushMap_PushMapSpawnConfig` |
| `Manufacture` | 制造 | `Manufacture_ProtagonistLevelConfig`、`Manufacture_SoulConfig`、`Manufacture_ClassConfig`、`Manufacture_GemConfig`、`Manufacture_RaceConfig`、`Manufacture_BodyPartConfig`、`Manufacture_BodyAppearanceConfig`、`Manufacture_ExtraEquipmentConfig`、`Manufacture_GemSuffixNameConfig`、`Manufacture_MagicBookConfig` |
| `Tech` | 科技 | `Tech_TechTreeConfig`、`Tech_TechEffectConfig` |
| `Combat` | 战斗 | `Combat_LossOfControlConfig`、`Combat_CombatConstantConfig`、`Combat_SkillConfig`、`Combat_FormationBondConfig` |

配置表中文名（`TableZH`）取自 §9 小节标题（如「挖坟配置表」「关卡运作表」）。各表完整 Excel / CSV 名以 §9「磁盘名」行为准；新增表须先定 `SystemZH` + `TableZH` + `SystemEN` 再落盘。

#### 14.3 双格式强制

- 每张配置表必须同时维护 **Excel 源** + **CSV 产物**（文件名按 §14.2，**不必同名**）。
- 运行时 / 加载管线**只读** `ConfigTables/Csv/`。
- 策划只改 Excel；改完必须打表。**CSV 为生成物**，禁止以手改 CSV 作为长期数据源。

#### 14.4 打表工具（Bake Tables）

| 项 | 约定 |
|----|------|
| 职责 | 一键将对应根下 `Excel/` 全部 `.xlsx` 转为 `.csv` 写入同根 `Csv/`（选中单表 **后置**） |
| 命名映射 | Excel 基名 `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}` → CSV 基名 `{SystemEN}_{TableEN}`（取英文后缀两段） |
| 入口 | Unity 顶部菜单：`Gravedigger2026/Config/Bake Tables`（Mode1 根）；`Gravedigger2026/Config/Bake Mode2 Tables`（Mode2 根） |
| 脚本 | `Assets/Editor/Config/ConfigTableBaker.cs` + `XlsxSheetReader.cs` |
| 失败策略 | 文件名不符四段规则 / 首 Sheet 无英文表头时**整批中止**；先全部解析入内存再写盘（避免半成品）；Console 报错须含**完整 Excel 名** |
| Demo 校验 | 仅文件名四段 + 非空英文表头行；§9 缺列 / 类型非法校验 **后置**（运行时 `ConfigCsvRepository` 仍做加载期校验） |
| Excel 库 | 方案 A：Editor 纯 C# 解析 Open XML（`System.IO.Compression` + XML）；零第三方包；读 workbook 第一个 worksheet；**Zip 条目路径**须同时兼容 `/` 与 `\`（部分 Windows Excel 写出反斜杠；`ZipArchive.GetEntry` 按原文精确匹配）。**打开方式：** 以 `FileShare.ReadWrite` 读入内存再 `ZipArchive`（禁止 `ZipFile.OpenRead`：其 `FileShare.Read` 在 Excel 占用时会 Sharing violation 整批中止）。打表读的是**盘上已保存**字节，不含 Excel 未保存缓冲。独占锁仍失败时 Console 须提示先保存并关闭该工作簿（可点名同目录 `~$*.xlsx` 锁文件） |
| 数值写出 | Excel 数值单元格写入 CSV 时须遵守 [§14.6](#146-数值单元格-csv-序列化禁止浮点噪声)（禁止二进制浮点噪声字面量） |
| 忽略 | `~$*.xlsx`（Excel 锁文件） |

**Excel 三行表头（方案 B，人读说明 + 机器列名）：**

| 行 | 内容 | 是否进入 CSV |
|----|------|----------------|
| 第 1 行 | 字段中文名 | **否**（打表剥离） |
| 第 2 行 | 字段中文说明 | **否**（打表剥离） |
| 第 3 行 | 字段英文名（与运行时列名一致） | **是**（成为 CSV 表头） |
| 第 4 行起 | 数据 | **是** |

- **英文表头识别：** 打表扫描首 Sheet 前至多 **3** 行，取首个「非空单元格均为合法英文列名」的行作为机器表头。合法列名格式：`^[A-Za-z_][A-Za-z0-9_.]*$`（允许点号，如 `GemMult.MaxHP`）。其上 0～2 行视为说明行并剥离；其下全部为数据行。
- **兼容：** 若第 1 行已是英文表头（旧单行格式），不要求必须有中文说明行；CSV 产物形状与现网一致（英文单行表头 + 数据）。
- **权威语义：** 字段中文名 / 说明以 [§9](#9-配置表关卡运作--挖坟--坟墓品质--材料--货币--挖坟能力--防守--刷怪波次--怪物--主角升级--灵魂--宝石--种族--制造部件--躯体外观--科技树--失控--士兵技能--推图战) 各表「字段 (EN) \| 中文 \| 类型 \| 说明」为准；Excel 第 1/2 行应与之对齐。
- **禁止：** 说明行进入 CSV；运行时改读中文表头。

```
Excel (ConfigTables/Excel/{SystemZH}_{TableZH}_{SystemEN}_{TableEN}.xlsx)
  → Editor Bake Tables（剥离至多 2 行说明，保留英文表头+数据）
  → CSV (ConfigTables/Csv/{SystemEN}_{TableEN}.csv)  // 英文单行表头
  → Runtime Config Loader
```

#### 14.5 运行时 CSV 加载路径（Demo）

| 环境 | Mode1 根路径 | Mode2 根路径 |
|------|--------------|--------------|
| Editor / 开发 Play | `{Application.dataPath}/ConfigTables/Csv/` | `{Application.dataPath}/ConfigTables/Mode2/Csv/` |
| Player 构建 | `{Application.streamingAssetsPath}/ConfigTables/Csv/` | `{Application.streamingAssetsPath}/ConfigTables/Mode2/Csv/` |

- 由当前进档 `CampaignMode` 选择相对 CSV 文件夹；加载器按序探测 dataPath 与 StreamingAssets。
- 逻辑仍只读 CSV；**禁止**运行时读 Excel。
- 进档换模式须 **重新** `ConfigCsvRepository.TryLoadAll()`，避免串表。
- 缺表 / 缺列 / 非法枚举 → 加载失败并打日志，不静默用空表推进关卡。

#### 14.6 数值单元格 CSV 序列化（禁止浮点噪声）

**适用范围：** CSV 产物、打表工具（含 Editor Bake 与对齐的脚本镜像）、Agent/脚本向配置表写出的数值字面量。

**禁止：** 在 CSV（及同源写出）中出现二进制浮点噪声字面量，例如 `0.009999999999999995`、`0.06999999999999999`、`-0.010000000000000002`。

**Excel 数值单元格（无 `t` 或可解析为数字）写出算法：**

1. 以 `InvariantCulture` 解析为 `double`；`NaN` / `Infinity` 保持原串或按现有失败策略。
2. 若 `|x - round(x)| < 1e-9` 且 `|x| < 1e15` → 输出**整数字符串**（例：`80.0` → `80`）。
3. 否则：`Round(x, 10, MidpointRounding.AwayFromZero)`，再以去尾零的固定小数格式写出（等价 `0.##########` / trim）；**禁止**科学计数法；**禁止**长尾 9/0 噪声。
4. 共享字符串 / 显式文本单元格中的**干净短小数**原样保留；若文本内嵌二进制浮点噪声字面量（如编码字段 `MoveSpeed_0.30000000000000004`），打表须按同算法改写为短小数。

**规范化示例：** `0.009999999999999995` → `0.01`；`0.06999999999999999` → `0.07`；`-0.010000000000000002` → `-0.01`；`MoveSpeed_0.30000000000000004` → `MoveSpeed_0.3`。

### English

**Status: Bake tool implemented (Approach A); Excel three-row header (Approach B) specified; numeric CSV serialization (no float noise) specified.**

All **§9** and future **config tables** must follow this section.

#### 14.1 Unified path (two folders)

```
Gravedigger2026/Assets/ConfigTables/
├── Excel/           # Mode1 human-authored (.xlsx); not loaded at runtime
├── Csv/             # Mode1 program-readable (.csv); bake output; Mode1 runtime source
└── Mode2/
    ├── Excel/       # Mode2 sources (this slice: full copy of Mode1)
    └── Csv/         # Mode2 runtime CSV
```

- **`CampaignMode.Mode1`** reads `ConfigTables/Csv/`; **`CampaignMode.Mode2`** reads `ConfigTables/Mode2/Csv/` ([§14.5](#145-runtime-csv-load-paths-demo)).
- New config tables must **not** live under `Assets/Settings/<Module>/` or other paths.
- Excel import: `.xlsx` may be DefaultAsset; Excel must **not** be a runtime load target.

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
| `PushMap` | 推图战 | `PushMap_PushMapGameplayConfig`, `PushMap_PushMapSpawnConfig` |
| `PushMap` | 推图战 | `PushMap_PushMapGameplayConfig`, `PushMap_PushMapSpawnConfig` |
| `Manufacture` | 制造 | `Manufacture_ProtagonistLevelConfig`, `Manufacture_SoulConfig`, `Manufacture_ClassConfig`, `Manufacture_GemConfig`, `Manufacture_RaceConfig`, `Manufacture_BodyPartConfig`, `Manufacture_BodyAppearanceConfig`, `Manufacture_ExtraEquipmentConfig`, `Manufacture_GemSuffixNameConfig`, `Manufacture_MagicBookConfig` |
| `Tech` | 科技 | `Tech_TechTreeConfig`, `Tech_TechEffectConfig` |
| `Combat` | 战斗 | `Combat_LossOfControlConfig`, `Combat_CombatConstantConfig`, `Combat_SkillConfig`, `Combat_SkillEffectConfig` |

`TableZH` comes from the §9 subsection title (e.g.「挖坟配置表」). Per-table full Excel/CSV names: see §9 **Disk name** lines. New tables must choose `SystemZH` + `TableZH` + `SystemEN` before landing files.

#### 14.3 Dual-format required

- Every config table must maintain **Excel source** + **CSV product** (names per §14.2; **need not match**).
- Runtime / loaders read **only** `ConfigTables/Csv/`.
- Designers edit Excel only, then bake. **CSV is generated**; do not hand-edit CSV as the long-term source of truth.

#### 14.4 Bake tool

| Item | Rule |
|------|------|
| Duty | One-click convert all `.xlsx` under the chosen root `Excel/` to matching `.csv` under that root's `Csv/` (per-selection bake **deferred**) |
| Name map | Excel `{SystemZH}_{TableZH}_{SystemEN}_{TableEN}` → CSV `{SystemEN}_{TableEN}` (last two English segments) |
| Entry | Unity menu: `Gravedigger2026/Config/Bake Tables` (Mode1 root); `Gravedigger2026/Config/Bake Mode2 Tables` (Mode2 root) |
| Scripts | `Assets/Editor/Config/ConfigTableBaker.cs` + `XlsxSheetReader.cs` |
| Failure | Abort whole batch on non-four-part Excel names / missing English header row; parse all into memory before writing (no partial writes); Console errors must include **full Excel name** |
| Demo validation | Filename four-part + non-empty English header row only; §9 missing-column / illegal-type checks **deferred** (runtime `ConfigCsvRepository` still validates on load) |
| Excel lib | Approach A: Editor pure-C# Open XML (`System.IO.Compression` + XML); zero third-party packages; first worksheet in workbook; **Zip entry paths** must accept both `/` and `\` (some Windows Excel writers store backslashes; `ZipArchive.GetEntry` matches literally). **Open:** snapshot bytes with `FileShare.ReadWrite` then `ZipArchive` (do not use `ZipFile.OpenRead` — its `FileShare.Read` aborts the batch with Sharing violation while Excel has the file open). Bake reads **last-saved disk bytes**, not unsaved Excel buffers. Exclusive lock must Console-prompt: save and close the workbook (may name sibling `~$*.xlsx`) |
| Numeric emit | When writing Excel numeric cells to CSV, follow [§14.6](#146-numeric-cell-csv-serialization-no-float-noise) (forbid binary float-noise literals) |
| Ignore | `~$*.xlsx` (Excel lock files) |

**Excel three-row header (Approach B — human ZH + machine EN):**

| Row | Content | In CSV? |
|-----|---------|---------|
| 1 | Field Chinese name | **No** (stripped on bake) |
| 2 | Field Chinese notes | **No** (stripped on bake) |
| 3 | Field English name (runtime column id) | **Yes** (CSV header) |
| 4+ | Data | **Yes** |

- **English header detection:** Bake scans at most the first **3** rows of the first sheet and picks the first row whose non-empty cells all match a legal English column id. Pattern: `^[A-Za-z_][A-Za-z0-9_.]*$` (dots allowed, e.g. `GemMult.MaxHP`). 0–2 rows above are documentation and stripped; all rows below are data.
- **Compat:** If row 1 is already the English header (legacy single-row format), ZH doc rows are optional; CSV shape stays English header + data.
- **Authority:** Field ZH name / notes follow [§9](#9-配置表关卡运作--挖坟--坟墓品质--材料--货币--挖坟能力--防守--刷怪波次--怪物--主角升级--灵魂--宝石--种族--制造部件--躯体外观--科技树--失控--士兵技能--推图战) per-table Field tables; Excel rows 1–2 should stay aligned.
- **Forbidden:** Doc rows in CSV; runtime reading Chinese headers.

```
Excel (ConfigTables/Excel/{SystemZH}_{TableZH}_{SystemEN}_{TableEN}.xlsx)
  → Editor Bake Tables (strip ≤2 doc rows; keep English header + data)
  → CSV (ConfigTables/Csv/{SystemEN}_{TableEN}.csv)  // English single-row header
  → Runtime Config Loader
```

#### 14.5 Runtime CSV load paths (Demo)

| Environment | Mode1 root | Mode2 root |
|-------------|------------|------------|
| Editor / Play Mode | `{Application.dataPath}/ConfigTables/Csv/` | `{Application.dataPath}/ConfigTables/Mode2/Csv/` |
| Player build | `{Application.streamingAssetsPath}/ConfigTables/Csv/` | `{Application.streamingAssetsPath}/ConfigTables/Mode2/Csv/` |

- Active enter-save `CampaignMode` selects the relative CSV folder; loader probes dataPath then StreamingAssets.
- Still CSV-only; **never** read Excel at runtime.
- Switching mode on enter-save must **reload** `ConfigCsvRepository.TryLoadAll()` to avoid cross-mode tables.
- Missing table / column / illegal enum → load fails with log; do not silently advance Levels on empty tables.

#### 14.6 Numeric cell CSV serialization (no float noise)

**Scope:** CSV products, bake tools (Editor Bake and aligned script mirrors), and Agent/script numeric literals written into config tables.

**Forbidden:** Binary float-noise literals in CSV (and equivalent emitters), e.g. `0.009999999999999995`, `0.06999999999999999`, `-0.010000000000000002`.

**Excel numeric cells (no `t`, or parseable as number) emit algorithm:**

1. Parse as `double` with `InvariantCulture`; keep raw string or follow existing failure policy for `NaN` / `Infinity`.
2. If `|x - round(x)| < 1e-9` and `|x| < 1e15` → emit **integer** string (e.g. `80.0` → `80`).
3. Else: `Round(x, 10, MidpointRounding.AwayFromZero)`, then emit with trailing zeros trimmed (equivalent to `0.##########` / trim); **no** scientific notation; **no** long trailing 9/0 noise.
4. Clean short decimals inside shared strings / explicit text stay as-is; if text embeds binary float-noise literals (e.g. encoded field `MoveSpeed_0.30000000000000004`), bake must rewrite them with the same algorithm.

**Normalization examples:** `0.009999999999999995` → `0.01`; `0.06999999999999999` → `0.07`; `-0.010000000000000002` → `-0.01`; `MoveSpeed_0.30000000000000004` → `MoveSpeed_0.3`.

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

**角色游戏 Prefab 组装（俯视相机，士兵 / 主角 / 怪物共用结构）：**

| 节点 | 职责 |
|------|------|
| 根 | 位移；士兵运行时挂 `WarriorAgentView` + `WarriorAnimView` + `NavMeshAgent`（可代码 Add；Demo `radius=0.1` / `height=0.1`）；Digger 根挂 `DigObstacleRadius` + `DigDiggerView` |
| 子 `Visual` | `SpriteRenderer` + `Animator`（Controller 来自 Art 烘焙）；`localEulerAngles = (90, 0, 0)`，使 Sprite 朝 +Y，对齐 Dig/Defend/PushMap 俯视相机（相机 `Euler(90,0,0)`）；`sortingOrder` 高于地面 Tilemap（Demo ≥ 200） |

- **可选 AllIn1 特效材质（Demo）：** Built-in 管线用 `AllIn1SpriteShader/AllIn1SpriteShader`；工程材质落 `Assets/Materials/AllIn1/`（含 `Style_WarriorGlow` / `Style_SkillAberration` / `Style_AdvanceOutline`；**不要**把游戏材质留在厂商 `AllIn1SpriteShader/Materials/`）。**强制落盘：** `AddAllIn1Shader` 只生成内存材质；不点 **Save Material**（或指定已有 `.mat`）就保存 Prefab，`SpriteRenderer` 槽位会变成 None，运行时角色隐身（Prefab 预览仍可能正常，因为 `ExecuteInEditMode` 会再造临时材质）。**不用** Demo 原版 ColorSwap/GlowTex/OutlineTex/OutlineDistort（整图 UV，套序列帧会花屏）。序列帧须 `ATLAS_ON`。**士兵 VisualStyle（§3.15 6b）：** `WarriorVisualStyleCatalog`（`Assets/Settings/Defend/`）绑 StyleId→Material（材质通道）或 Kind=`ScaleModel`（放大通道，无材质）。Instantiate 后：材质赋 `sharedMaterial` + MPB 强度；放大把子节点 `Visual.localScale` 设为 `(k,k,k)`（k=`VisualModelScale`）；出场 `BodyRadius` 与战斗 `AttackRange` 均 ×k。禁止运行时 `new Material` / `EnableKeyword`。士兵 Visual **必须**用项目 `AllIn1AtlasUvDriver`（MPB 写 `_Min/MaxX/YUV` 并 **`SetTexture(_MainTex, sprite.texture)`**）；**禁止**厂商 `SetAtlasUvs` 写 `sharedMaterial`（同预制体多兵抢 UV）。缺 Driver 时 `WarriorAllIn1StyleView` 运行时补。厂商 `AllIn1Shader` 可留作编辑器工具（`ExecuteInEditMode` 警告为知情项），运行时不调用。描边优先 `OUTBASEPIXELPERF`。默认仍可用 `Sprites-Default`；Assembler 不得强行覆盖已落盘的 AllIn1 材质，但 **SpriteRenderer 槽位为 None 时必须回填 `Sprites-Default`**。默认士兵 Prefab（含 `App_0_00`）**不要**挂厂商 `AllIn1Shader`：其 `ExecuteInEditMode` 会把名字含 Default 的材质换成未落盘内存材质，再保存 Prefab 就会把槽位写成 None（制造 UI 卡仍能显示 Sprite，布阵/战斗世界单位隐身或洋红）。缺 `AllIn1AtlasUvDriver` 时 `WarriorAllIn1StyleView` 运行时补。UI-016 士兵卡揭示（世界 Instantiation + Camera/RT）套 AllIn1 与缩放；布阵底栏缩略图本轮不套 AllIn1、不缩放。

**新增 VisualStyle 预设（操作流程）：** 日常只改两处——`MagicBookConfig` 定「谁在 Token **命中**后用哪套」；Catalog +（材质通道）`Assets/Materials/AllIn1/` 定「那套长什么样」。竞争 / 空列 / 放大通道 / Mode1 见 [SPEC_03 §3.15 6b](SPEC_03_GameRules.md)。

| 步 | 做什么 |
|----|--------|
| 1. 新建材质 | **材质通道：** 在 `Assets/Materials/AllIn1/` **复制**最接近的 `Style_*.mat`，改名为新 `Style_YourEffect.mat`（`StyleId` 与文件名一致）。**禁止**直接引用或改厂商 `AllIn1SpriteShader/Materials/Visual*.mat`。也可在 Visual 上用厂商 `AllIn1Shader` 的 **Create New Material** / **Save Material To Folder**，保存路径必须是本目录（不落盘就保存 Prefab 会隐身）。**放大模型不必建 `.mat`** |
| 2. 打开效果 | 材质通道：用 AllIn1 材质面板在**编辑器**勾 keyword（禁止运行时 `EnableKeyword`）。**必开** Sprite inside an atlas? → `ATLAS_ON`。描边再开 Outline + Pixel Perfect（`OUTBASEPIXELPERF`）。**不要开** Color Swap / GlowTex / Outline Texture / Outline Distort。面板上的颜色/宽度/glow 即强度 1 的基准；运行时按 `VisualIntensity` 乘 Catalog 登记的 float。放大通道跳过本步 |
| 3. Catalog | 打开 `Assets/Settings/Defend/WarriorVisualStyleCatalog.asset`，加一行：`StyleId`、材质通道填 `Material` + 可选 `IntensityFloatProperties`（空=只换预设、不叠加强度）；放大通道 Kind=`ScaleModel`、Material 可空。`DefendPrefabCatalog._visualStyleCatalog` 已绑本 SO，一般不必再绑 |
| 4. 魔法书 | 改 Mode2 Excel `Manufacture_MagicBookConfig`：`VisualStyleId` / `VisualPriority` / `VisualIntensityAdd`（空 style=该书无特效；材质 Priority 越大越优先；IntensityAdd 空=1；放大填 `Style_ScaleModel` 且 IntensityAdd=k）。然后 **Gravedigger2026 / Config / Bake Mode2 Tables**。`ClassId` 逗号 OR 时该格加引号 |
| 5. Prefab / 手验 | 世界 Instantiate 后换 `sharedMaterial` + MPB；放大另设 `Visual.localScale` 与 BodyRadius/AttackRange ×k；缺 `AllIn1AtlasUvDriver` 运行时补；**不要**挂厂商 `SetAtlasUvs`。须 **重新造兵**（旧池不补）。看布阵 / 守城 / 推图 3D 单位与 UI-016 士兵卡 RT 预览；底栏缩略图无 AllIn1、不缩放。Console 命中应有 `VisualStyle=Style_…` 或放大日志 |

Demo Catalog 强度属性：

| StyleId | IntensityFloatProperties |
|---------|--------------------------|
| `Style_WarriorGlow` | `_ColorRampBlend`、`_AlphaOutlineGlow`、`_InnerOutlineGlow` |
| `Style_SkillAberration` | `_ChromAberrAmount`、`_ChromAberrAlpha` |
| `Style_AdvanceOutline` | `_OutlineWidth`、`_OutlineGlow` |
| `Style_ScaleModel` | （无；Kind=`ScaleModel`；`VisualIntensityAdd`=k） |

Demo 优先级：士兵技能 10、战士强化 20、职业进阶 30（进阶与强化同时命中 → 描边）。放大通道不参与该优先级。

- View 层：`NavMeshAgent.updateRotation = false`（八向靠 Animator `DirIndex`，见 §15.5；士兵由 `WarriorAnimView` 驱动）。
- **禁止**用 `GenericTopDownController` 作默认玩法控制器（见 §15.4）。
- **Digger / BattleProtagonist** 已换为上述 `Visual` 结构（Art：`Protagonist/Digger`、`Protagonist/BattleProtagonist`）；**禁止** `DigAssetBuilder` / `DefendAssetBuilder` 再生成 Capsule/`Body` Mesh 占位覆盖这两 Prefab。
- **怪物（`ModelId`）**：根挂运行时 `MonsterAgentView` + `NavMeshAgent`（可代码 Add）；子 `Visual` 同上。当 `Art/Characters/Monsters/{ModelId}/` 已有烘焙 Controller（及 Idle Sprite）时，**必须**组装为 `Visual` 并**删除**占位 `Body` Mesh；无 Art 时允许保留临时立方体。本片已落地：`MonsterModel_01`…`MonsterModel_04`。
- Editor：`Tools/Gravedigger/Art/Assemble Protagonist Prefabs`（`ProtagonistPrefabAssembler`）；`Tools/Gravedigger/Art/Assemble Monster Model Prefabs`（`MonsterModelPrefabAssembler`，仅组装 Art 就绪的 `ModelId`）。`DefendAssetBuilder` 生成 Catalog 时对有 Art 的怪物调用后者，**禁止**用临时立方体覆盖已组装 Prefab。
- **士兵（`AppearanceId`，D-056 方案 B，WA-01 已编码）：** 当 `Art/Characters/Appearances/{AppearanceId}/` 已有烘焙 Controller（及 Idle Sprite）时，**必须**组装为 `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab`（根 + `Visual`，同主角/怪物）。无 Art 时允许保留已有占位（Capsule 或 `App_90`–`App_99` 的 App_02 克隆）；**禁止**用 Capsule 覆盖已组装 Visual Prefab。Editor：`Tools/Gravedigger/Defend/Assemble Warrior Appearance Prefabs`（`WarriorAppearancePrefabAssembler`）：**缺 Prefab 且 Art 就绪 → 从 Art 创建**；已有 Prefab → 仅确保 Visual 结构；**材质槽 None → 回填 `Sprites-Default` 并去掉厂商 `AllIn1Shader`**（不覆盖已落盘 AllIn1）。组装后刷新 `DefendPrefabCatalog` + `UpgradeManufacturePrefabCatalog` 士兵绑定（`CloneApp02FallbackAppearances.RefreshWarriorCatalogBindings`：并集 Mode1+Mode2 `BodyAppearanceConfig` 已有 Prefab 的 AppearanceId；**禁止**为此调用 `DefendAssetBuilder.GenerateAll`，以免覆盖 PushMap 地图绑定）。已补：`App_0_00`/`App_0_10`/`App_0_20`/`App_0_30`、`App_0_01`…`App_0_33`、`App_4_41`（圣骑士）、`App_5_51`（暗黑法师）；以及人/精/兽 3 级职业外观 `App_1_*`→`Race_Human`、`App_2_*`→`Race_Elf`、`App_3_*`→`Race_Orc`（后缀对齐 `App_0_XY`；无 `App_1_32` Art 故不生成）。

#### 15.3 导出路径改造（强制）

厂商 `SpritesheetGenerator` 默认将 `outputParent` 写到工具内 `Created Spritesheets/`。**本项目维护补丁**：把 Editor 导出根改为 `Assets/Art/Characters/...`（按角色类型分子目录；导出文件夹名与 `AppearanceId` / `ModelId` / 主角约定对齐）。Clips / Controller / 可选导出 Prefab 随同写入该 Art 子目录；再组装或复制为 §15.2 的游戏 Prefab。

**切片 / Clip 补丁（强制，否则 Windows 下 `.anim`/`.controller` 会空导出）：**

| 问题 | 现象 | 补丁 |
|------|------|------|
| 切片未设 `TextureImporter.textureType = Sprite` | meta 可有 `spriteMode: Multiple` 与 spritesheet 名，但 `textureType` 仍为 Default(0)；`LoadAllAssetRepresentationsAtPath` 得不到 Sprite → **不写任何 `.anim`**，却仍生成 **空 BlendTree**（`m_Childs: []`）的 `.controller` | 切片与 `ApplyFrameCountOption` 重导时必须 `textureType = Sprite`，并与厂商源 `spritesheets/`（`textureType: 8`）一致 |
| `AssetImporter.GetAtPath` 使用反斜杠路径 | Windows 上 importer 可能为 null，切片静默跳过 | 资产路径统一 `.Replace("\\", "/")` |
| 无 Sprite 仍建 Controller | 空状态机误当成功 | `AnimatorClipBuilder`：零 Clip 时 **LogError 并中止**，不写空 `.controller` |
| 切片用 NPOT 膨胀后的 `tex.width`（常见 2048） | 源 PNG 为 1920×1024（15×8→格宽 **128**），却按 `2048/15≈136.53` 切；换帧时角色相对中心持续左漂，循环后跳回 | 切片前设 `npotScale=None`；用 `TextureImporter.GetSourceTextureWidthAndHeight`（或重导后再读）算格宽；`CharacterCreatorExportRepair`：已有 120 格但 `rect.width` 与 `sourceWidth/columns` 差 >0.5 时 **强制重建 rect** |

**修复已坏导出：** Editor 菜单 `Tools/Gravedigger/Art/Repair Character Creator Export`（对选中或指定角色文件夹：纠正 importer → 重导 → 重建 Clips/Controller）。批量：`Tools/Gravedigger/Art/Repair All Character Creator Exports (Art/Characters)`。

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

#### 15.5 动画映射（Demo 锁定）

Creator 默认导出含 Idle / Walk / Run / Attack* / Special1 / Die 等。挖坟循环动画**无**原生 Dig clip：由**表现层**（`DigDiggerView` 序列化字段）将 Dig 语义映射到导出 Trigger **`Special1`**；厂商状态多为单次 Trigger，View 在 digging 期间于 clip 结束时重触发以实现循环。待机 = 默认 Idle（清 locomotion bool）。**禁止**规则层写死动画名字符串；**禁止**在无独立 Dig 资源时假装已有专用 Dig 资产。

**Defend 士兵（`WarriorAnimView`，Demo 锁定）：**

| 语义 | Creator 参数（默认；View 序列化可改） | 说明 |
|------|--------------------------------------|------|
| 移动 | Bool `IsRun` | MassMove **steer** 超阈值 → true；停步清 locomotion Bool → Idle |
| 普攻 | Trigger `Attack1` | 近战前摇开始 / 远程开火时触发；规则层不写动画名 |
| 死亡 | Trigger `Die` | CombatDead / PermanentDeath 后触发一次并锁存；尸体留场 |
| 朝向 | Int `DirIndex` | 按移动或瞄准 XZ 向量换算（见下表）；零向量不改 |
| 卡面嘲讽 | Trigger `Taunt` | UI-016 士兵卡揭示时播一遍；规则层不写动画名 |
| 卡面空闲 | 默认 IdleBT | 揭示：Taunt 后清 locomotion Bool 并循环 Idle |

**移动打断攻击（表现层，Demo 锁定）：** Creator `Attack1_*` 仅经 ExitTime 回 Idle，`IsRun` alone 无法中途切出。`WarriorAnimView.SetMoving(true)` 在需要时 `ResetTrigger(Attack1)` 并 `CrossFade(RunBT)`。**距离门控：** 仅当「当前 XZ → 移动目标点」平面距离 `moveTargetDistanceXZ > 0.4`（常量 `AttackInterruptMinMoveTargetDistance`）时才强制打断；≤0.4 的近距微调只写 `IsRun`、不 CrossFade（AttackSlot 旁微移不砍普攻）。`GoalKind.Objective`（FlowField）或拿不到目标点时按「足够远」处理（仍可打断）。规则前摇 / HitConfirm **不变**。

**`DirIndex`（Creator 烘焙 Controller，Demo 锁定）：**

| `DirIndex` | 朝向 |
|------------|------|
| 0 | E |
| 1 | W |
| 2 | S |
| 3 | N |
| 4 | NE |
| 5 | NW |
| 6 | SE |
| 7 | SW |

Demo：`Digger` Prefab / Art 管线保留；**Dig 阶段运行时不 Instantiate 地图 Digger**（主角为 HUD 左上 60×60 头像框）。`BattleProtagonist` 固定 **`DirIndex = 2`（南）**；士兵由 `WarriorAnimView` 按移动/瞄准动态设 `DirIndex`。BattleProtagonist 本片仅 Idle 站桩（无受击/死亡驱动）。怪物本片仍不驱动 Animator（死亡 `SetActive(false)`）。

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

**Character game Prefab assembly (top-down; soldiers, protagonists, and monsters share layout):**

| Node | Role |
|------|------|
| Root | Translation; warriors get runtime `WarriorAgentView` + `WarriorAnimView` + `NavMeshAgent` (may AddComponent; Demo `radius=0.1` / `height=0.1`); Digger root keeps `DigObstacleRadius` + `DigDiggerView` |
| Child `Visual` | `SpriteRenderer` + `Animator` (Controller from Art bake); `localEulerAngles = (90, 0, 0)` so the sprite faces +Y toward the Dig/Defend/PushMap top-down camera (`Euler(90,0,0)`); `sortingOrder` above ground Tilemap (Demo ≥ 200) |

- **Optional AllIn1 effect materials (Demo):** Built-in pipeline uses `AllIn1SpriteShader/AllIn1SpriteShader`; project mats live under `Assets/Materials/AllIn1/` (`Style_WarriorGlow` / `Style_SkillAberration` / `Style_AdvanceOutline`; **do not** leave game mats in vendor `AllIn1SpriteShader/Materials/`). **Must save to disk:** `AddAllIn1Shader` only creates an in-memory material; saving the Prefab without **Save Material** (or assigning an existing `.mat`) serializes SpriteRenderer as None and the character vanishes in Play (Prefab preview can still look fine because `ExecuteInEditMode` recreates a temp mat). **Do not** use Demo ColorSwap/GlowTex/OutlineTex/OutlineDistort (full-sprite UVs glitch on spritesheets). Spritesheets need `ATLAS_ON`. **Soldier VisualStyle (§3.15 6b):** `WarriorVisualStyleCatalog` (`Assets/Settings/Defend/`) binds StyleId→Material (material channel) or Kind=`ScaleModel` (scale channel, no mat). After Instantiate: assign `sharedMaterial` + MPB intensity; scale sets child `Visual.localScale` to `(k,k,k)` (`k=VisualModelScale`); spawn `BodyRadius` and combat `AttackRange` both ×k. No runtime `new Material` / `EnableKeyword`. Warrior Visual **must** use project `AllIn1AtlasUvDriver` (MPB `_Min/MaxX/YUV` and **`SetTexture(_MainTex, sprite.texture)`**); **forbid** vendor `SetAtlasUvs` writing `sharedMaterial`. `WarriorAllIn1StyleView` adds the driver if missing. Vendor `AllIn1Shader` may stay as an editor helper (`ExecuteInEditMode` warning is advisory) and is not called at runtime. Prefer `OUTBASEPIXELPERF`. Default remains `Sprites-Default`; Assembler must not overwrite an authored on-disk AllIn1 material, but **must restore `Sprites-Default` when the SpriteRenderer slot is None**. Default soldier Prefabs (incl. `App_0_00`) **must not** mount vendor `AllIn1Shader`: `ExecuteInEditMode` replaces materials whose name contains Default with an unsaved in-memory mat, and saving the Prefab then serializes the slot as None (manufacture UI cards still show the Sprite; formation/combat world units vanish or turn magenta). Missing `AllIn1AtlasUvDriver` is added at runtime by `WarriorAllIn1StyleView`. UI-016 card reveal (world Instantiate + Camera/RT) applies AllIn1 and scale; formation bar thumbs do not this round.

**Adding a VisualStyle preset (procedure):** two places only — `MagicBookConfig` chooses which preset after a token **hit**; Catalog + (material channel) `Assets/Materials/AllIn1/` defines how it looks. Compete / empty / scale channel / Mode1: [SPEC_03 §3.15 6b](SPEC_03_GameRules.md).

| Step | Do this |
|------|---------|
| 1. New mat | **Material:** **Duplicate** the closest `Style_*.mat` under `Assets/Materials/AllIn1/`, rename `Style_YourEffect.mat` (`StyleId` matches filename). **Do not** reference or edit vendor `AllIn1SpriteShader/Materials/Visual*.mat`. Or use vendor `AllIn1Shader` **Create New Material** / **Save Material To Folder** on Visual — save path **must** be this folder (saving the Prefab without a disk mat makes the character vanish). **Scale-model needs no `.mat`** |
| 2. Enable effects | Material: toggle keywords on the AllIn1 **material inspector** (no runtime `EnableKeyword`). **Required:** Sprite inside an atlas? → `ATLAS_ON`. For outline also Outline + Pixel Perfect (`OUTBASEPIXELPERF`). **Do not** enable Color Swap / GlowTex / Outline Texture / Outline Distort. Inspector color/width/glow are intensity=1 baselines; runtime multiplies Catalog float props by `VisualIntensity`. Scale channel skips this step |
| 3. Catalog | Open `Assets/Settings/Defend/WarriorVisualStyleCatalog.asset`; add `StyleId`; material rows: `Material` + optional `IntensityFloatProperties` (empty = swap preset only); scale rows: Kind=`ScaleModel`, Material may be null. `DefendPrefabCatalog._visualStyleCatalog` already points here |
| 4. MagicBook | Edit Mode2 Excel `Manufacture_MagicBookConfig`: `VisualStyleId` / `VisualPriority` / `VisualIntensityAdd` (empty style = no visual; material higher Priority wins; IntensityAdd default 1; scale uses `Style_ScaleModel` and IntensityAdd=k). Then **Gravedigger2026 / Config / Bake Mode2 Tables**. Quote the cell when `ClassId` is comma-OR |
| 5. Prefab / check | World Instantiate swaps `sharedMaterial` + MPB; scale also sets `Visual.localScale` and BodyRadius/AttackRange ×k; missing `AllIn1AtlasUvDriver` is added at runtime; **do not** mount vendor `SetAtlasUvs`. **Re-craft** soldiers (old pool rows are not backfilled). Inspect 3D units on formation / Defend / PushMap and UI-016 card RT preview; bar thumbs have no AllIn1 or scale. Console on hit: `VisualStyle=Style_…` or scale log |

Demo Catalog intensity props:

| StyleId | IntensityFloatProperties |
|---------|--------------------------|
| `Style_WarriorGlow` | `_ColorRampBlend`, `_AlphaOutlineGlow`, `_InnerOutlineGlow` |
| `Style_SkillAberration` | `_ChromAberrAmount`, `_ChromAberrAlpha` |
| `Style_AdvanceOutline` | `_OutlineWidth`, `_OutlineGlow` |
| `Style_ScaleModel` | (none; Kind=`ScaleModel`; `VisualIntensityAdd`=k) |

Demo priorities: skill 10, warrior enhance 20, class advance 30 (advance + enhance both hit → outline). Scale channel does not use that priority.

- View: `NavMeshAgent.updateRotation = false` (8-dir via Animator `DirIndex`, §15.5; soldiers driven by `WarriorAnimView`).
- **Do not** use `GenericTopDownController` as the default gameplay controller (§15.4).
- **Digger / BattleProtagonist** use the `Visual` layout above (Art: `Protagonist/Digger`, `Protagonist/BattleProtagonist`); **do not** let `DigAssetBuilder` / `DefendAssetBuilder` regenerate Capsule/`Body` Mesh over those Prefabs.
- **Monsters (`ModelId`)**: root gets runtime `MonsterAgentView` + `NavMeshAgent` (may AddComponent); child `Visual` as above. When `Art/Characters/Monsters/{ModelId}/` has a baked Controller (and Idle Sprite), **must** assemble `Visual` and **remove** placeholder `Body` Mesh; temp cubes remain only when Art is missing. This slice: `MonsterModel_01`…`MonsterModel_04`.
- Editor: `Tools/Gravedigger/Art/Assemble Protagonist Prefabs` (`ProtagonistPrefabAssembler`); `Tools/Gravedigger/Art/Assemble Monster Model Prefabs` (`MonsterModelPrefabAssembler`, only ModelIds with Art ready). `DefendAssetBuilder` calls the latter for Art-ready monsters when building Catalog and **must not** overwrite assembled Prefabs with temp cubes.
- **Soldiers (`AppearanceId`, D-056 Approach B, WA-01 coded):** When `Art/Characters/Appearances/{AppearanceId}/` has a baked Controller (and Idle Sprite), **must** assemble `Assets/Prefabs/Defend/Warriors/{AppearanceId}.prefab` (root + `Visual`, same as protagonist/monsters). Placeholders allowed only when Art is missing (Capsule or App_02 clones `App_90`–`App_99`); **must not** overwrite assembled Visual Prefabs with Capsules. Editor: `Tools/Gravedigger/Defend/Assemble Warrior Appearance Prefabs` (`WarriorAppearancePrefabAssembler`): **missing Prefab + Art ready → create from Art**; existing Prefab → ensure Visual layout; **material slot None → restore `Sprites-Default` and strip vendor `AllIn1Shader`** (do not overwrite on-disk AllIn1). After assemble, refresh `DefendPrefabCatalog` + `UpgradeManufacturePrefabCatalog` warrior bindings (`CloneApp02FallbackAppearances.RefreshWarriorCatalogBindings`: union Mode1+Mode2 `BodyAppearanceConfig` AppearanceIds that already have Prefabs; **do not** call `DefendAssetBuilder.GenerateAll` for this — it wipes PushMap map bindings). Added: `App_0_00`/`App_0_10`/`App_0_20`/`App_0_30`, `App_0_01`…`App_0_33`, `App_4_41` (Paladin), `App_5_51` (Dark Mage); plus Human/Elf/Orc Lv3 class looks `App_1_*`→`Race_Human`, `App_2_*`→`Race_Elf`, `App_3_*`→`Race_Orc` (suffix aligns with `App_0_XY`; no `App_1_32` Art, so not generated).

#### 15.3 Export path patch (mandatory)

Vendor `SpritesheetGenerator` defaults `outputParent` to in-package `Created Spritesheets/`. **This project maintains a patch** so Editor export roots at `Assets/Art/Characters/...` (subfolders by role; folder names align with `AppearanceId` / `ModelId` / protagonist convention). Clips / Controller / optional export Prefab land under that Art subfolder; then assemble/copy into §15.2 game Prefabs.

**Slice / Clip patch (mandatory — otherwise Windows exports empty `.anim`/`.controller`):**

| Issue | Symptom | Patch |
|-------|---------|-------|
| Slice omits `TextureImporter.textureType = Sprite` | Meta may show Multiple + spritesheet names while `textureType` stays Default(0); no Sprite sub-assets → **no `.anim`**, yet an empty BlendTree `.controller` (`m_Childs: []`) is still written | Force `textureType = Sprite` on slice and `ApplyFrameCountOption` reimport; match vendor `spritesheets/` (`textureType: 8`) |
| Backslash paths in `AssetImporter.GetAtPath` | Importer may be null on Windows; slice silently skipped | Normalize asset paths with `.Replace("\\", "/")` |
| Controller built with zero clips | Empty state machine looks “successful” | `AnimatorClipBuilder`: **LogError and abort** when clip count is zero |
| Slice uses NPOT-padded `tex.width` (often 2048) | Source PNG is 1920×1024 (15×8 → cell **128**), but sliced as `2048/15≈136.53`; frames drift left vs pivot, then jump back on loop | Set `npotScale=None` before slice; use `TextureImporter.GetSourceTextureWidthAndHeight` (or reimport then read) for cell size; `CharacterCreatorExportRepair`: if 120 cells exist but `rect.width` differs from `sourceWidth/columns` by >0.5 → **force rebuild rects** |

**Repair broken exports:** Editor menu `Tools/Gravedigger/Art/Repair Character Creator Export` (fix importer → reimport → rebuild Clips/Controller for selected/target character folder). Batch: `Tools/Gravedigger/Art/Repair All Character Creator Exports (Art/Characters)`.

**Upgrade risk:** Re-importing the `.unitypackage` overwrites the patch → after each import, `diff` `SpritesheetGenerator.cs` / `AnimatorClipBuilder.cs` (and related write paths) and re-apply.

#### 15.4 Pipeline

Same mermaid as ZH. Author in vendor folder only; export via patched path; Prefabs under Dig/Defend; **do not** treat `GenericTopDownController` as the default gameplay controller.

#### 15.5 Animation mapping (Demo lock)

Creator exports Idle / Walk / Run / Attack* / Special1 / Die, etc. Dig loop has **no** native Dig clip: **View layer** (`DigDiggerView` serialized field) maps Dig semantics to export Trigger **`Special1`**; vendor states are mostly one-shot Triggers — View re-fires when the clip ends while digging to loop. Idle = default Idle (clear locomotion bools). **No** hardcoded anim names in rules layer; **do not** pretend a dedicated Dig asset exists without one.

**Defend soldiers (`WarriorAnimView`, Demo lock):**

| Semantics | Creator param (default; View serialized) | Notes |
|-----------|------------------------------------------|-------|
| Move | Bool `IsRun` | true when MassMove **steer** above threshold; clear locomotion bools → Idle when stopped |
| Attack | Trigger `Attack1` | on melee windup start / ranged fire; rules never hardcode anim names |
| Death | Trigger `Die` | once on CombatDead / PermanentDeath, then latched; corpse stays |
| Facing | Int `DirIndex` | from move or aim XZ (table below); zero vector leaves unchanged |
| Card taunt | Trigger `Taunt` | UI-016 card reveal plays once; rules never hardcode anim names |
| Card idle | default IdleBT | After Taunt, clear locomotion bools and loop Idle |

**Move interrupts attack (presentation, Demo lock):** Creator `Attack1_*` exits only via ExitTime; `IsRun` alone cannot cut mid-attack. `WarriorAnimView.SetMoving(true)` may `ResetTrigger(Attack1)` and `CrossFade(RunBT)`. **Distance gate:** force-interrupt only when planar distance current XZ → move target `moveTargetDistanceXZ > 0.4` (`AttackInterruptMinMoveTargetDistance`); ≤0.4 near-target nudges set `IsRun` without CrossFade (AttackSlot micro-moves do not chop attack). `GoalKind.Objective` (FlowField) or missing target → treat as far enough (interrupt still allowed). Windup / HitConfirm rules **unchanged**.

**`DirIndex` (Creator bake Controller, Demo lock):**

| `DirIndex` | Facing |
|------------|--------|
| 0 | E |
| 1 | W |
| 2 | S |
| 3 | N |
| 4 | NE |
| 5 | NW |
| 6 | SE |
| 7 | SW |

Demo: `Digger` Prefab/art may remain; **Dig stage does not Instantiate map Digger** (protagonist = HUD top-left 60×60 portrait). `BattleProtagonist` fixed **`DirIndex = 2` (South)**; soldiers get dynamic `DirIndex` from `WarriorAnimView` (move/aim). BattleProtagonist this slice: Idle only (no hit/death drive). Monsters this slice: no Animator drive (death still `SetActive(false)`).

#### 15.6 Mount / Wing

See [§9.14](#914-额外装备配置表-extraequipmentconfig): bake into `AppearanceId` variants; runtime overlays later.
