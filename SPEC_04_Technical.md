# SPEC_04 — 技术规范 / Technical Standards（Gravedigger2026）

**关联文档 / Related:** [SPEC_00_Index.md](SPEC_00_Index.md) · [SPEC_03_GameRules.md](SPEC_03_GameRules.md)

---

## 1. 工程与环境

### 简体中文

| 属性 | 值 |
|------|-----|
| Unity 版本 | 2021.3.40f1 |
| 脚本语言 | C# |
| 渲染 | TBD |
| Cursor 工作区根 | `E:\Work\Cursor\Gravedigger2026\Gravedigger2026` |
| Unity 工程根目录 | `Gravedigger2026/`（相对工作区根） |
| 资源根目录 | `Gravedigger2026/Assets/` |
| 目标平台 | TBD |
| 平台优先级 | TBD |

### English

| Attribute | Value |
|-----------|-------|
| Unity version | 2021.3.40f1 |
| Scripting language | C# |
| Rendering | TBD |
| Cursor workspace root | `E:\Work\Cursor\Gravedigger2026\Gravedigger2026` |
| Unity project root | `Gravedigger2026/` |
| Assets root | `Gravedigger2026/Assets/` |
| Target platforms | TBD |
| Platform priority | TBD |

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
├── Sprites/
├── Localization/
│   ├── Strings/
│   └── Fonts/
├── Audio/
├── Resources/
└── Settings/
```

实际目录以工程为准；结构性变更记入 SPEC_00 Changelog。

### English

Recommended tree as above. Record structural changes in SPEC_00 Changelog.

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

**状态：已对照 SPEC_03 §3.8**

**范围内（须满足 D-001～D-004）：**

| 模块 | 要求 |
|------|------|
| 存档 Meta | 固定 3 槽；本地按槽索引；至少持久化「是否占用」；新建 / 进入 / 删除（含确认） |
| 进档壳层 | 进入后默认 `GameplayState = Dig` 占位；浮动「工具」 |
| 工具面板 | 设置、关卡占位入口；与玩法 View 分离（壳层 UI） |
| 玩法三态 | 仅可识别占位；完整规则不实现 |

**范围外：** 三玩法完整规则实现（规则正文见 SPEC_03 §3.9–§3.10）；真实关卡加载与表驱动；工具后续功能；完整存档 schema；未列入 §3.8 的需求。

**持久化意图（轻量）：** 本地、按槽索引 `0..2`；技术选型（PlayerPrefs / 文件等）**TBD**，实现时再定并回写本节。完整字段 schema **TBD**。

**架构提示：** `ToolsPanel` 属 Meta 壳层 UI；玩法状态由规则层持有，View 只订阅展示（见 §13）。挖坟：规则层负责生成、计时、DigAction 触发/忙碌锁与扣血；菱形地图与圆圈光标、帧动画、奖励飞向为主角由 View 表现；逻辑层为整体可放置空间（非格子）。

### English

**Status: Aligned with SPEC_03 §3.8**

**In scope (D-001–D-004):** 3 fixed slots with local occupied flag; InSaveShell default Dig placeholder + floating Tools; Settings/Level stubs as shell UI; gameplay states as identifiable placeholders only.

**Out of scope:** Full three-mode implementation (rules text in SPEC_03 §3.9–§3.10); real Level load / table drive; future Tools entries; full save schema; anything not in §3.8.

**Persistence intent:** Local by slot index `0..2`; tech choice (PlayerPrefs / files / etc.) **TBD**. Full field schema **TBD**.

**Architecture note:** ToolsPanel is Meta shell UI; gameplay state owned by rules layer; View subscribes only (§13). Dig: rules layer owns spawn, timer, DigAction trigger/busy lock, and damage; diamond map, circle cursor, dig frame anims, and DigReward fly-to are View; logic is continuous placeable space (not a cell grid).

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

## 9. 配置表（关卡运作 / 挖坟 / 坟墓品质 / 材料 / 货币 / 挖坟能力 / 防守 / 主角升级 / 灵魂 / 宝石 / 种族 / 制造部件）

### 简体中文

**状态：已定义字段与编码；配置载体（ScriptableObject / 外部表）TBD**，实现时按 [§13](#13-资源编排与可扩展性) 选型并回写。

规则语义权威：[SPEC_03 §3.9](SPEC_03_GameRules.md)、[§3.10](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)。

#### 加权字段通用规则

凡配置中的 **权重值（Weight）** 均适用：

| 规则 | 说明 |
|------|------|
| 非负 | `Weight` 须 ≥ 0；负值视为非法配置（加载时拒绝或忽略该段并打日志，实现时二选一并回写） |
| 零权重剔除 | `Weight = 0` → **强制认定该项不存在**：解析后剔除，不参与加权随机 |
| 有效项 | 仅 `Weight > 0` 的项进入加权池；按权重占比抽取 |
| 空有效列表 | 默认语义由各玩法写明。Dig / `GraveSpawnWeights`：过滤后为空 → **放弃该次生成**（见 [SPEC_03 §3.10](SPEC_03_GameRules.md)） |

#### 9.1 关卡运作表 `LevelOperationConfig`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| LevelId | 关卡ID | `string` 或 `int` | 同 ID 多行 = 该关全部阶段 |
| StageNumber | 阶段编号 | `int` | 同关卡内升序执行；建议同关卡内唯一 |
| GameplayType | 玩法类型 | `enum` / `string` | 如 `Dig` / `UpgradeManufacture` / `Defend` |
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | 指向对应玩法配置表主键 |

```
LevelOperationConfig {
  LevelId: Id
  StageNumber: int
  GameplayType: Dig | UpgradeManufacture | Defend | ...
  GameplayConfigId: Id
}
```

#### 9.2 挖坟配置表 `DigGameplayConfig`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | 主键；被关卡运作表引用 |
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
  LevelDurationSeconds: number
  InitialGraveCount: int
  SpawnRate: "N;M"          // every N seconds spawn M
  GraveSpawnWeights: "Q;W|Q;W|..."
}
```

**加权随机：** 先按通用规则得到有效权重列表，再做一次独立抽取（开局 N 次各抽一次；过程生成的每一座各抽一次）。抽取算法实现细节不绑定具体 RNG API。

**落点：** 在 DigMap 整体可放置区域内采样；须避开 `DigObstacle`（Digger + 未消除 Grave）的圆形障碍半径（半径在对应 Prefab 上配置）。单次生成采样失败最多重试 **32** 次，仍失败则放弃该次生成。

**Dig Prefab 约定：** `Assets/Prefabs/Dig/` 下 Digger 与各品质 Grave 预制体暴露圆形障碍半径；每种 `QualityId` 对应专属 Grave Prefab。

#### 9.3 坟墓品质定义表 `GraveQualityConfig`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| QualityId | 坟墓品质ID | `string` 或 `int` | 主键；被 `GraveSpawnWeights` 引用 |
| MaxHP | 总血量 | `int` 或 `float` | 生成时初始化坟的 maxHP / 当前 HP；具体数值 **TBD** |
| LootDrop | 掉落内容 | 见编码 | 挖掘成功（HP=0）时产出的奖励 |
| IconStyleSet | 图标样式集 | 资源引用 / Id | 可选；样式1/2/3 资源引用；载体 **TBD** |

```
GraveQualityConfig {
  QualityId: Id
  MaxHP: number              // TBD concrete values
  LootDrop: "Id_Count|Id_Count|..."
  IconStyleSet: Id?          // optional styles 1/2/3
}
```

**与规则的关系（[SPEC_03 §3.10](SPEC_03_GameRules.md)）：**

- 生成坟时按 `QualityId` 读本表初始化 `GraveHP`。
- 扣血后按剩余 HP% 切换 `GraveIconStyle`（>65% / 30%–65% / <30%）；样式资源可来自 `IconStyleSet` 或默认资源（实现时定）。
- HP 归 0 时按 `LootDrop` 生成 `DigReward` 图标并飞向主角；到达后入账。

**`LootDrop` 编码（固定）：** `Id_Count|Id_Count|...`

- 段分隔符：`|`；段内：`Id_Count`（下划线分隔）。
- `Id`：材料表主键（`MaterialConfig.MaterialId`），或 **保留精魂 Id** 字符串 **`Spirit`**（大小写敏感；不进入材料堆叠，直接加精魂）。
- `Count`：正整数（≥ 1）。
- 空串、缺下划线、`Count` 非正整数、未知非 `Spirit` 的 Id：**忽略该段并打日志**，继续解析其余段。
- 示例：`Iron_3|Spirit_10|Bone_1`

#### 9.4 材料配置表 `MaterialConfig`

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

#### 9.6 挖坟主角能力（运行时派生，非完整科技树表）

科技解锁/升级的结果写入存档主角的 `DigProtagonistCapabilities`（完整节点表与 TechPoint 消耗 **TBD**）：

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
```

#### 9.7 防守配置表 `DefendGameplayConfig`

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GameplayConfigId | 玩法配置ID | `string` 或 `int` | 主键；被关卡运作表引用 |
| BattleMapId | 战斗地图ID | `string` 或 `int` | 指向 BattleMap 资源 / 场景绑定；编码 **TBD** |
| WaveConfigId | 波次配置ID | `string` 或 `int` | 指向刷怪/波次表；表结构 **TBD**（另批） |
| TargetRetargetIntervalSeconds | 目标修正间隔 | `float` | 怪物重算可攻击目的地的间隔（秒）；默认 **1** |

```
DefendGameplayConfig {
  GameplayConfigId: Id
  BattleMapId: Id              // BattleMap binding TBD
  WaveConfigId: Id             // wave/spawn table TBD
  TargetRetargetIntervalSeconds: number  // default 1
}
```

**寻路技术约定（与 [SPEC_03 §3.12](SPEC_03_GameRules.md) 配套）：**

- 采用 Unity **NavMesh**（或项目统一封装的等价 Agent）在 BattleMap 连续可走空间上寻路。
- **规则层**只输出：当前目标实体 ID + 可攻击目的地世界坐标；**不**直接写 `Transform` / `Animator`。
- **移动层**（NavMeshAgent 或等价）执行 `SetDestination`；每隔 `TargetRetargetIntervalSeconds` 由规则层触发目的地重算并请求重寻路。
- 障碍烘焙与 NavMesh 表面范围 **TBD**（须覆盖地图内可走区，并允许从地图外刷怪点进入可走区——具体衔接 **TBD**）。

**刷怪表：** 本批不定义行结构；仅要求怪物从设定地图空间 **外** 出现（规则见 §3.12）。

#### 9.8 主角升级配置表 `ProtagonistLevelConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md)。一行 = 一个主角等级。

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| Level | 当前等级值 | `int` | 主键；≥ 1；同表内唯一 |
| RequiredTotalExperience | 升到本级需要的经验总值 | `int` 或 `long` | **生涯累计阈值**（非本级增量）；与存档 `LifetimeExperience` 比较；升级 **不**扣减已有经验；1 级行通常为 **0** |
| UnlockedFeatureIds | 升到本级解锁的功能 | 见编码 | **仅预留**；本版无运行时解锁逻辑 |
| TechPointsReward | 升到本级奖励的科技点数 | `int` | 首次进入该等级时发放（≥ 0） |
| ControlPowerCap | 升到本级控制力上限变成的值 | `int` 或 `float` | 该等级下控制力上限绝对值；本版有效上限 = 本字段（科技加成另专题） |
| ProtagonistMaxHP | 升到本级主角的生命值上限 | `int` 或 `float` | 战斗主角 MaxHP 绝对值 |

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

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 战士属性构成 / 命名。一行 = 一种灵魂。

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| SoulId | 灵魂ID | `string` 或 `int` | 主键 |
| ClassName | 职业名 | `string` | 参与 `WarriorName` 拼接；展示用 |
| Skills | 可使用技能 | 见编码 | 技能 Id + 等级列表；编码 **TBD**（建议 `SkillId;Level\|…`） |
| AttackPriority | 攻击优先级 | `string` 或 `int` / 枚举 | 战士侧攻击优先级；与怪物 AttackPriority **分表**；具体排序/编码 **TBD** |
| MoveStyle | 移动风格 | `string` 或 `int` / 枚举 | 战士移动行为风格；枚举值集 **TBD** |
| SpiritCost | 精魂消耗 | `int` 或 `float` | 制造时计入总精魂消耗（≥ 0；缺省 0） |
| ControlPowerCost | 控制力占用 | `int` 或 `float` | 该灵魂对战士 `ControlPowerCost` 的贡献（≥ 0） |

```
SoulConfig {
  SoulId: Id
  ClassName: string               // WarriorName segment
  Skills: "SkillId;Level|..."     // encoding TBD
  AttackPriority: Id | enum       // warrior-side; encoding TBD
  MoveStyle: Id | enum            // encoding TBD
  SpiritCost: number              // >= 0
  ControlPowerCost: number
}
```

**说明：** 原 `InfoTags` **不再**参与 WarriorInfo 主标签生成（主标签 = 定稿种族）。

**战士实例静态快照（制造完成时写入；非独立配置表）：**

```
WarriorInstance {
  Id: Id
  WarriorName: string             // Prefix(es)+RaceName+ClassName+Suffix
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
  }                               // from BodyPart aggregation (conversion later)
  SoulId: Id                      // FK → SoulConfig
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

- 躯体部位 / 额外装备 / 宝石后缀表见 **§9.12–§9.14**。
- 进战场最终属性（按项 `S`）：`FinalStat(S) = max(0, Base(S) + Equip(S) + Base(S)×SkillBuff(S) + Base(S)×GemMult(S) + Base(S)×RaceAdjust(S))`（先定 `S` 再取来源；`SkillBuff` 仅运行时；各维缺省 0；见 §3.11）。
- 多宝石：实例 `GemMult(S) = Σ` 已镶嵌各宝石的 `GemMult(S)`。
- 战士死亡：全部 `GemIds` 回仓库；躯体部位/灵魂/外置装备等绑定材料销毁（见 §3.11）。
- 种族 **不** 单独计入 `ControlPowerCost`。

#### 9.10 宝石配置表 `GemConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 战士属性构成 / 宝石。一行 = 一种宝石。

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| GemId | 宝石ID | `string` 或 `int` | 主键 |
| GemType | 宝石类型 | `enum` / `string` | 六类之一；制造槽 **同类型互斥**；正式枚举名 **TBD** |
| GemMult.MaxHP | 生命值放大系数 | `float` | 缺省视为 **0**；代入 `Base(MaxHP) × GemMult.MaxHP` |
| GemMult.MoveSpeed | 移动速度放大系数 | `float` | 同上 |
| GemMult.Strength | 力量放大系数 | `float` | 同上 |
| GemMult.Agility | 敏捷放大系数 | `float` | 同上 |
| GemMult.Intelligence | 智力放大系数 | `float` | 同上 |
| Skills | 额外技能 | 见编码 | 额外一套技能（SkillId + 等级）；编码 **TBD**（建议与 `SoulConfig.Skills` 同风格 `SkillId;Level\|…`） |
| SpiritCost | 精魂消耗 | `int` 或 `float` | 制造时计入总精魂消耗（≥ 0；缺省 0） |
| ControlPowerCost | 控制力占用 | `int` 或 `float` | 该宝石对战士 `ControlPowerCost` 的贡献（≥ 0） |

```
GemConfig {
  GemId: Id
  GemType: enum                   // six types; mutual exclusion in slots; names TBD
  GemMult: {
    MaxHP: number
    MoveSpeed: number
    Strength: number
    Agility: number
    Intelligence: number
  }                               // missing dim = 0
  Skills: "SkillId;Level|..."     // encoding TBD; extra skill set
  SpiritCost: number
  ControlPowerCost: number
}
```

**库存语义：** 宝石为可回仓物品；战士死亡时将实例 `GemIds` **全部归还仓库**（其余绑定材料销毁）。制造时实例五维 `GemMult(S) = Σ` 已镶嵌宝石该维（无镶嵌则五维全 0）。获取途径与具体数值 **TBD**。

#### 9.11 种族配置表 `RaceConfig`

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 战士属性构成 / 种族 / 命名。一行 = 一种种族。

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| RaceId | 种族ID | `string` 或 `int` | 主键；被躯体部位 `RaceId` 引用 |
| DisplayNameKey | 展示名 Key | `string` | UI / `WarriorName` 种族段；本地化 Key **TBD** |
| RaceAdjustCoeff.MaxHP | 生命值调整系数 | `float` | 可正可负；缺省视为 **0** |
| RaceAdjustCoeff.MoveSpeed | 移动速度调整系数 | `float` | 同上 |
| RaceAdjustCoeff.Strength | 力量调整系数 | `float` | 同上 |
| RaceAdjustCoeff.Agility | 敏捷调整系数 | `float` | 同上 |
| RaceAdjustCoeff.Intelligence | 智力调整系数 | `float` | 同上 |

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
}
```

**解析：** 制造时对已放入躯体部位（头/躯干/臂/腿）各权重 **1** 按部位 `RaceId` **加权随机**定稿 → 查本表，将五维系数写入 `WarriorInstance.RaceAdjustCoeff`；按项代入 `Base(S) × RaceAdjust(S)`。具体种族列表与数值 **TBD**。

#### 9.12 躯体部位配置表 `BodyPartConfig`（骨架）

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 制造槽位 / 种族加权 / BaseStats。一行 = 一种躯体部位材料。

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| BodyPartId | 躯体部位ID | `string` 或 `int` | 主键 |
| BodySlot | 躯体槽类型 | `enum` | `Head` / `Torso` / `Arm` / `Leg`；决定可放入的制造槽 |
| RaceId | 种族ID | `string` 或 `int` | FK → `RaceConfig`；参与加权定种族 |
| SpiritCost | 精魂消耗 | `int` 或 `float` | ≥ 0；缺省 0 |
| ControlPowerCost | 控制力占用 | `int` 或 `float` | ≥ 0 |
| BaseStatInputs | 基础属性换算输入 | **TBD** | 汇总换算五项 BaseStats 的输入；算法另专题 |

```
BodyPartConfig {
  BodyPartId: Id
  BodySlot: Head | Torso | Arm | Leg
  RaceId: Id                      // FK → RaceConfig
  SpiritCost: number
  ControlPowerCost: number
  BaseStatInputs: ...             // TBD
}
```

#### 9.13 额外装备配置表 `ExtraEquipmentConfig`（骨架）

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 外置装备 / 命名前缀。一行 = 一种外置装备。

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| EquipId | 装备ID | `string` 或 `int` | 主键 |
| EquipSlot | 装备槽 | `enum` | `Mount` / `Wing` |
| NamePrefix | 名字前缀 | `string` | 参与 `WarriorName`；两件都装备则依次拼接 |
| SpiritCost | 精魂消耗 | `int` 或 `float` | ≥ 0；缺省 0 |
| ControlPowerCost | 控制力占用 | `int` 或 `float` | ≥ 0 |
| EquipStats | 平坦属性加成 | **TBD** | 五项同名加成 |
| Skills | 额外技能 | 见编码 | 编码 **TBD** |

```
ExtraEquipmentConfig {
  EquipId: Id
  EquipSlot: Mount | Wing
  NamePrefix: string
  SpiritCost: number
  ControlPowerCost: number
  EquipStats: ...                 // TBD
  Skills: "SkillId;Level|..."     // encoding TBD
}
```

#### 9.14 宝石后缀命名表 `GemSuffixNameConfig`（骨架）

规则语义：[SPEC_03 §3.11](SPEC_03_GameRules.md) 战士命名后缀。一行 = 一种已镶嵌宝石组合 → 后缀。

| 字段 (EN) | 中文 | 类型（伪） | 说明 |
|-----------|------|------------|------|
| ComboKey | 组合键 | `string` | 已镶嵌 `GemType` 集合/有序键；**编码 TBD** |
| Suffix | 后缀 | `string` | 拼入 `WarriorName` 末段；无匹配可空 |

```
GemSuffixNameConfig {
  ComboKey: string                // encoding TBD
  Suffix: string
}
```

**解析：** 制造完成时按实例 `GemIds` 推导 `ComboKey` 查本表得后缀；无宝石或无匹配行 → 后缀为空。

### English

**Status: Fields and encodings defined; config carrier (SO / external table) TBD** — pick per §13 at implementation time.

Rules authority: [SPEC_03 §3.9](SPEC_03_GameRules.md), [§3.10](SPEC_03_GameRules.md), [§3.11](SPEC_03_GameRules.md), [§3.12](SPEC_03_GameRules.md).

#### Weighted-field common rules

All config **Weight** values follow:

| Rule | Notes |
|------|-------|
| Non-negative | `Weight` must be ≥ 0; negative = illegal (reject on load or skip segment + log; pick one at implementation and write back) |
| Zero drop | `Weight = 0` → **treat entry as absent**: strip after parse; excluded from weighted pick |
| Effective set | Only `Weight > 0` entries enter the pool; pick by weight share |
| Empty effective list | Per-mode semantics. Dig / `GraveSpawnWeights`: empty after filter → **abandon that spawn** (see [SPEC_03 §3.10](SPEC_03_GameRules.md)) |

#### 9.1 LevelOperationConfig

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| LevelId | 关卡ID | `string` or `int` | Multiple rows per Level |
| StageNumber | 阶段编号 | `int` | Ascending within Level; unique per Level recommended |
| GameplayType | 玩法类型 | `enum` / `string` | e.g. `Dig` / `UpgradeManufacture` / `Defend` |
| GameplayConfigId | 玩法配置ID | `string` or `int` | FK to mode config PK |

#### 9.2 DigGameplayConfig

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | PK; referenced by Level Operation |
| LevelDurationSeconds | 关卡时长限制 | `float` or `int` | **Base** duration (seconds); effective countdown = this field + `DigStageDurationBonus` (see [SPEC_03 §3.10](SPEC_03_GameRules.md) / §9.6) |
| InitialGraveCount | 开局基础生成坟墓数量 | `int` | N independent weighted rolls at start |
| SpawnRate | 倒计时过程中生成坟墓速率 | encoding | Every N seconds spawn M |
| GraveSpawnWeights | 坟墓出现概率权重 | encoding | Quality Id + weight list |

**`SpawnRate` encoding (fixed):** `N;M` — every N seconds spawn M (example `5;2`).

**`GraveSpawnWeights` encoding (fixed):** `QualityId;Weight|QualityId;Weight|...` (example `1;10|2;5|3;1`). Follow **Weighted-field common rules**: strip `Weight = 0`; pick among `Weight > 0`. Empty effective list → **abandon that spawn**. `QualityId` must resolve in `GraveQualityConfig` (§9.3). Example empty: `1;0|2;0`.

**Weighted pick:** filter to effective list, then one independent draw per grave (initial and ongoing). RNG API unbound.

**Placement:** sample DigMap continuous placeable space; avoid `DigObstacle` circles (Digger + uncleared Graves; radii on Prefabs). Retry up to **32** times per spawn; then abandon that spawn.

**Dig Prefab convention:** under `Assets/Prefabs/Dig/`, Digger and per-quality Grave Prefabs expose circle obstacle radius; one Grave Prefab per `QualityId`.

#### 9.3 GraveQualityConfig

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| QualityId | 坟墓品质ID | `string` or `int` | PK; referenced by `GraveSpawnWeights` |
| MaxHP | 总血量 | `int` or `float` | Init grave maxHP / current HP; concrete values **TBD** |
| LootDrop | 掉落内容 | encoding | Reward when dig succeeds (HP=0) |
| IconStyleSet | 图标样式集 | asset ref / Id | Optional; styles 1/2/3; carrier **TBD** |

```
GraveQualityConfig {
  QualityId: Id
  MaxHP: number
  LootDrop: "Id_Count|Id_Count|..."
  IconStyleSet: Id?
}
```

**Rules link ([SPEC_03 §3.10](SPEC_03_GameRules.md)):** spawn inits `GraveHP` from this table; remaining HP% drives `GraveIconStyle`; HP=0 uses `LootDrop` for `DigReward` fly-to-Digger; credit on arrival.

**`LootDrop` encoding (fixed):** `Id_Count|Id_Count|...`

- Segment separator `|`; within segment `Id_Count` (underscore).
- `Id`: `MaterialConfig.MaterialId`, or reserved Spirit Id string **`Spirit`** (case-sensitive; credits SpiritEssence, not Warehouse).
- `Count`: positive integer (≥ 1).
- Empty / missing underscore / non-positive Count / unknown non-`Spirit` Id: **skip segment and log**, continue.
- Example: `Iron_3|Spirit_10|Bone_1`

#### 9.4 MaterialConfig

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

#### 9.6 DigProtagonistCapabilities (runtime derived; not full tech-tree table)

Tech unlock/upgrade writes save-slot protagonist `DigProtagonistCapabilities` (full node table / TechPoint costs **TBD**):

```
DigProtagonistCapabilities {
  DigDamage: number
  DigDurationReductionSum: number
  DigCursorRadius: number
  DiggableQualityIds: set<QualityId>
  DigStageDurationBonus: number
}
// DigActionDuration = max(0.1, 0.8 - DigDurationReductionSum)
// EffectiveDigDuration = LevelDurationSeconds + DigStageDurationBonus
```

#### 9.7 DefendGameplayConfig

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GameplayConfigId | 玩法配置ID | `string` or `int` | PK; referenced by Level Operation |
| BattleMapId | 战斗地图ID | `string` or `int` | BattleMap asset / scene binding; encoding **TBD** |
| WaveConfigId | 波次配置ID | `string` or `int` | Wave/spawn table FK; table schema **TBD** |
| TargetRetargetIntervalSeconds | 目标修正间隔 | `float` | Seconds between attackable-destination recomputes; default **1** |

```
DefendGameplayConfig {
  GameplayConfigId: Id
  BattleMapId: Id
  WaveConfigId: Id
  TargetRetargetIntervalSeconds: number  // default 1
}
```

**Pathfinding tech (with [SPEC_03 §3.12](SPEC_03_GameRules.md)):**

- Use Unity **NavMesh** (or a project-wide equivalent Agent) on BattleMap continuous walkable space.
- **Rules layer** outputs: current target entity Id + attackable world destination; must **not** write `Transform` / `Animator` directly.
- **Movement layer** (NavMeshAgent or equiv.) runs `SetDestination`; every `TargetRetargetIntervalSeconds` the rules layer recomputes destination and requests repath.
- Obstacle bake / NavMesh surface extent **TBD** (must cover in-map walkable area; off-map spawn → walkable entry linkage **TBD**).

**Spawn tables:** not defined this batch; monsters must appear from **outside** configured map space (§3.12).

#### 9.8 ProtagonistLevelConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md). One row = one protagonist level.

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| Level | 当前等级值 | `int` | PK; ≥ 1; unique in table |
| RequiredTotalExperience | 升到本级需要的经验总值 | `int` or `long` | **Cumulative lifetime threshold** (not per-level delta); compared to save `LifetimeExperience`; level-up does **not** deduct owned Exp; level-1 row usually **0** |
| UnlockedFeatureIds | 升到本级解锁的功能 | see encoding | **Reserved only**; no runtime unlock this version |
| TechPointsReward | 升到本级奖励的科技点数 | `int` | Granted when first entering this level (≥ 0) |
| ControlPowerCap | 升到本级控制力上限变成的值 | `int` or `float` | Absolute ControlPower cap at this level; this version effective cap = this field (tech bonus later) |
| ProtagonistMaxHP | 升到本级主角的生命值上限 | `int` or `float` | Absolute BattleProtagonist MaxHP |

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

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) warrior attribute composition / naming. One row = one soul.

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| SoulId | 灵魂ID | `string` or `int` | PK |
| ClassName | 职业名 | `string` | `WarriorName` segment; display |
| Skills | 可使用技能 | encoding | Skill Id + level list; encoding **TBD** (suggested `SkillId;Level\|…`) |
| AttackPriority | 攻击优先级 | `string` or `int` / enum | Warrior-side AttackPriority; **separate** from monster table; ordering/encoding **TBD** |
| MoveStyle | 移动风格 | `string` or `int` / enum | Warrior movement style; enum set **TBD** |
| SpiritCost | 精魂消耗 | `int` or `float` | Added to manufacture Spirit total (≥ 0; default 0) |
| ControlPowerCost | 控制力占用 | `int` or `float` | This soul's contribution to warrior `ControlPowerCost` (≥ 0) |

```
SoulConfig {
  SoulId: Id
  ClassName: string
  Skills: "SkillId;Level|..."
  AttackPriority: Id | enum
  MoveStyle: Id | enum
  SpiritCost: number
  ControlPowerCost: number
}
```

**Note:** Former `InfoTags` no longer builds primary WarriorInfo (primary label = finalized Race).

**Warrior instance static snapshot (written at manufacture; not a config table):**

```
WarriorInstance {
  Id: Id
  WarriorName: string
  RemainingHP: number
  RaceId: Id
  RaceAdjustCoeff: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }
  BaseStats: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }
  SoulId: Id
  LockedEquipIds: Id[]
  GemIds: Id[]
  GemMult: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }  // Σ of socketed; 0 if none
  ControlPowerCost: number
}
```

**Related:**

- BodyPart / ExtraEquipment / GemSuffix schemas: **§9.12–§9.14**.
- Battlefield final (per attribute `S`): `FinalStat(S) = max(0, Base(S) + Equip(S) + Base(S)×SkillBuff(S) + Base(S)×GemMult(S) + Base(S)×RaceAdjust(S))` (pick `S` first; `SkillBuff` runtime only; missing dims = 0; see §3.11).
- Multi-gem: instance `GemMult(S) = Σ` of socketed gems' `GemMult(S)`.
- On warrior death: all `GemIds` return to Warehouse; BodyParts/Soul/ExtraEquipment and other bound materials are destroyed (see §3.11).
- Race does **not** add a separate ControlPowerCost term.

#### 9.10 GemConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) warrior attribute composition / Gem. One row = one gem.

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| GemId | 宝石ID | `string` or `int` | PK |
| GemType | 宝石类型 | `enum` / `string` | One of six; **type-exclusive** slots; enum names **TBD** |
| GemMult.MaxHP | 生命值放大系数 | `float` | Missing = **0**; used as `Base(MaxHP) × GemMult.MaxHP` |
| GemMult.MoveSpeed | 移动速度放大系数 | `float` | Same |
| GemMult.Strength | 力量放大系数 | `float` | Same |
| GemMult.Agility | 敏捷放大系数 | `float` | Same |
| GemMult.Intelligence | 智力放大系数 | `float` | Same |
| Skills | 额外技能 | encoding | Extra skill set; encoding **TBD** |
| SpiritCost | 精魂消耗 | `int` or `float` | ≥ 0; default 0 |
| ControlPowerCost | 控制力占用 | `int` or `float` | ≥ 0 |

```
GemConfig {
  GemId: Id
  GemType: enum
  GemMult: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }
  Skills: "SkillId;Level|..."
  SpiritCost: number
  ControlPowerCost: number
}
```

**Inventory:** Gems are Warehouse-returnable; on warrior death, **return all** instance `GemIds` to Warehouse (other bound materials destroyed). At manufacture, instance five-dim `GemMult(S) = Σ` of socketed gems (all zeros if none). Acquisition routes and concrete numbers **TBD**.

#### 9.11 RaceConfig

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) warrior attribute composition / Race / naming. One row = one race.

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| RaceId | 种族ID | `string` or `int` | PK; referenced by BodyPart `RaceId` |
| DisplayNameKey | 展示名 Key | `string` | UI / `WarriorName` race segment; localization **TBD** |
| RaceAdjustCoeff.MaxHP | 生命值调整系数 | `float` | May be +/-; missing treated as **0** |
| RaceAdjustCoeff.MoveSpeed | 移动速度调整系数 | `float` | Same |
| RaceAdjustCoeff.Strength | 力量调整系数 | `float` | Same |
| RaceAdjustCoeff.Agility | 敏捷调整系数 | `float` | Same |
| RaceAdjustCoeff.Intelligence | 智力调整系数 | `float` | Same |

```
RaceConfig {
  RaceId: Id
  DisplayNameKey: string?
  RaceAdjustCoeff: { MaxHP, MoveSpeed, Strength, Agility, Intelligence }
}
```

**Resolve:** At manufacture, weight-**1** pick among filled BodyParts' `RaceId`s (Head/Torso/Arm/Leg) → look up this table → copy five dims into `WarriorInstance.RaceAdjustCoeff`; each dim feeds `Base(S) × RaceAdjust(S)`. Concrete race list and values **TBD**.

#### 9.12 BodyPartConfig (skeleton)

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) manufacture slots / race pick / BaseStats. One row = one body-part material.

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| BodyPartId | 躯体部位ID | `string` or `int` | PK |
| BodySlot | 躯体槽类型 | `enum` | `Head` / `Torso` / `Arm` / `Leg` |
| RaceId | 种族ID | `string` or `int` | FK → `RaceConfig` |
| SpiritCost | 精魂消耗 | `int` or `float` | ≥ 0; default 0 |
| ControlPowerCost | 控制力占用 | `int` or `float` | ≥ 0 |
| BaseStatInputs | 基础属性换算输入 | **TBD** | Inputs for BaseStats aggregation |

```
BodyPartConfig {
  BodyPartId: Id
  BodySlot: Head | Torso | Arm | Leg
  RaceId: Id
  SpiritCost: number
  ControlPowerCost: number
  BaseStatInputs: ...
}
```

#### 9.13 ExtraEquipmentConfig (skeleton)

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) external gear / name prefix. One row = one ExtraEquipment.

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| EquipId | 装备ID | `string` or `int` | PK |
| EquipSlot | 装备槽 | `enum` | `Mount` / `Wing` |
| NamePrefix | 名字前缀 | `string` | `WarriorName`; concatenate if both equipped |
| SpiritCost | 精魂消耗 | `int` or `float` | ≥ 0; default 0 |
| ControlPowerCost | 控制力占用 | `int` or `float` | ≥ 0 |
| EquipStats | 平坦属性加成 | **TBD** | Flat five-stat bonuses |
| Skills | 额外技能 | encoding | **TBD** |

```
ExtraEquipmentConfig {
  EquipId: Id
  EquipSlot: Mount | Wing
  NamePrefix: string
  SpiritCost: number
  ControlPowerCost: number
  EquipStats: ...
  Skills: "SkillId;Level|..."
}
```

#### 9.14 GemSuffixNameConfig (skeleton)

Rules: [SPEC_03 §3.11](SPEC_03_GameRules.md) warrior name suffix. One row = one socketed-gem combo → suffix.

| Field (EN) | ZH | Type (pseudo) | Notes |
|------------|-----|---------------|-------|
| ComboKey | 组合键 | `string` | Socketed `GemType` set/ordered key; **encoding TBD** |
| Suffix | 后缀 | `string` | Final `WarriorName` segment; empty if no match |

```
GemSuffixNameConfig {
  ComboKey: string
  Suffix: string
}
```

**Resolve:** At manufacture complete, derive `ComboKey` from instance `GemIds` → lookup suffix; no gems / no match → empty suffix.

---

## 13. 资源编排与可扩展性

### 简体中文

**原则（强制倾向）：预制体优先（Prefab-first）。** 实际代码与场景开发中，凡会以 GameObject 层级出现的玩法实体、可复用 UI、可生成物、可摆放交互物，**默认用 Prefab + 挂载 Controller** 制作与引用，放在 `Assets/Prefabs/<模块>/`。优先在编辑器中拼装 Prefab，再由代码 `Instantiate` / 引用槽位驱动；**避免**在代码里动态 `new GameObject` 拼层级，或在多个 Scene 中手工复制同一套层级。

**适用默认 Prefab 的典型对象：** 主角/圆圈光标、坟墓（含障碍半径）、奖励飞字、工具面板与可复用面板、关卡内可生成物、战斗主角/战士/怪物等。Dig 模块建议路径：`Assets/Prefabs/Dig/`。

**可不做成 Prefab 的例外：** Scene 唯一常驻 Manager / 引导用一次性布局；纯逻辑无表现的 Service（非 MonoBehaviour 或仅场景单例入口）。

| 问题 | 是 → 做法 |
|------|-----------|
| 玩法/UI GameObject 层级（默认）？ | **Prefab** + Controller → `Assets/Prefabs/<模块>/` |
| 多次 Instantiate / 多 Scene 复用？ | 必须 Prefab；禁止 Scene 间复制层级 |
| 策划可调数值？ | ScriptableObject → `Assets/Settings/<模块>/` |
| UI 文本？ | 本地化 Key（若启用 §8） |
| 高频 spawn/destroy？ | Prefab + **对象池** |
| 玩法状态变更？ | 规则层 + 事件；View 不驱动规则 |

**禁止：** 多 Scene 复制同一层级；硬编码策划数据；`GameObject.Find`（Manager 除外）；规则层直接操作 `Transform`/`Animator`；用纯代码拼装本应以 Prefab 交付的可视层级（调试临时对象除外）。

配置表（§9）落地时优先 SO 或表驱动资源，禁止在脚本中硬编码关卡/挖坟/防守/升级数值。

### English

**Principle (strong default): Prefab-first.** For gameplay entities, reusable UI, spawnables, and placeable interactables that exist as GameObject hierarchies, **author and reference Prefabs + Controllers** under `Assets/Prefabs/<Module>/`. Prefer assembling Prefabs in the Editor and driving them via `Instantiate` / serialized slots. **Do not** build visual hierarchies with runtime `new GameObject` trees, or hand-duplicate the same hierarchy across Scenes.

**Typical Prefab targets:** Digger / circle cursor, Graves (with obstacle radius), DigReward VFX/UI, ToolsPanel and reusable panels, in-level spawnables, BattleProtagonist / Warriors / Monsters. Dig module path: `Assets/Prefabs/Dig/`.

**Exceptions:** Scene-unique Managers / one-off layout; pure logic Services (non-MonoBehaviour or single scene entry).

| Question | If yes → |
|----------|----------|
| Gameplay/UI GameObject hierarchy (default)? | **Prefab** + Controller → `Assets/Prefabs/<Module>/` |
| Multiple Instantiate / multi-Scene reuse? | Prefab required; no cross-Scene hierarchy copy |
| Designer-tunable values? | ScriptableObject → `Assets/Settings/<Module>/` |
| UI text? | Localization keys (if §8 enabled) |
| High-frequency spawn/destroy? | Prefab + **object pool** |
| Gameplay state changes? | Rules layer + events; View must not drive rules |

**Forbidden:** duplicating the same hierarchy across Scenes; hardcoding designer data; `GameObject.Find` (except Managers); rules layer directly driving `Transform`/`Animator`; assembling in code what should ship as a Prefab (except temporary debug objects).

When implementing §9 tables, prefer SO / table assets; no hardcoded Level/Dig/Defend/upgrade values in scripts.
