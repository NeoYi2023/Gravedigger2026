# Gravedigger2026 — 领域术语表 / Domain Glossary

**本文件是术语索引，不是规则权威。** 规则以 [SPEC_03_GameRules.md](SPEC_03_GameRules.md) 为准；技术约定以 [SPEC_04_Technical.md](SPEC_04_Technical.md) 为准。

| 术语 (EN) | 中文 | 定义摘要 | SPEC |
|-----------|------|----------|------|
| Gravedigger2026 | 本项目 | Unity 工程与工作区名称 | [SPEC_02](SPEC_02_GameOverview.md) |
| GameplayState | 玩法状态 | Dig / UpgradeManufacture / Defend；关卡内由阶段玩法类型驱动 | [§3.1](SPEC_03_GameRules.md)、[§3.7](SPEC_03_GameRules.md)、[§3.9](SPEC_03_GameRules.md) |
| SaveSlot | 存档槽 | 固定 3 槽本地存档位 | [§3.4](SPEC_03_GameRules.md) |
| InSaveShell | 进档壳层 | 进档后常驻壳（玩法占位 + 工具） | [§3.1](SPEC_03_GameRules.md)、[§3.3](SPEC_03_GameRules.md) |
| ToolsPanel | 工具面板 | Demo 设置/调试壳；含设置、关卡入口（流水线片可启样例关卡） | [§3.5](SPEC_03_GameRules.md) |
| Level | 关卡 | 关卡运作表驱动的多阶段流程；UM 阶段 `GameplayConfigId` 忽略 | [§3.1](SPEC_03_GameRules.md)、[§3.9](SPEC_03_GameRules.md) |
| ConfigTables | 配置表根目录 | `Assets/ConfigTables/`：Excel 源（四段中英名）+ CSV 产物（两段英文名） | [SPEC_04 §14](SPEC_04_Technical.md) |
| BakeTables | 打表 | Editor 一键 Excel→CSV（Excel 四段名映射为 CSV 英文基名）；菜单 `Gravedigger/Config/Bake Tables` | [SPEC_04 §14](SPEC_04_Technical.md) |
| CharacterArtPipeline | 角色美术管线 | Character Creator **烘焙整角**；游戏资源不得落在工具目录；导出补丁→`Art/Characters`→`Prefabs` | [SPEC_04 §15](SPEC_04_Technical.md) |
| BakedWholeCharacter | 烘焙整角 | 用 Creator 拼装后导出整角 spritesheet/Animator/Prefab；非运行时叠装 | [SPEC_04 §15](SPEC_04_Technical.md) |
| LevelOperation | 关卡运作 | 关卡 ID + 阶段编号 + 玩法类型 + 玩法配置 ID | [§3.9](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| DigGameplayConfig | 挖坟配置 | 基础时长、开局坟数、过程生成速率、品质权重（零权重剔除） | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| DigMap | 挖坟地图 | 菱形外观；逻辑为整体可放置空间（非格子）；表现 Prefab `Ground_01`…`Ground_05`（`DigMapId`） | [§3.10](SPEC_03_GameRules.md) |
| Grave | 坟墓 | 挖坟可生成实体；带品质 ID；未消除时为 DigObstacle | [§3.10](SPEC_03_GameRules.md) |
| Digger | 挖坟主角 | 挖坟阶段地图中心生成；待机/挖坟循环动画；烘焙整角 Prefab | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §15](SPEC_04_Technical.md) |
| DigAction | 挖掘流程 | 0.2s 停留触发；`DigActionDuration` 帧动画后扣血；busy 不可重触 | [§3.10](SPEC_03_GameRules.md) |
| DigObstacle | 挖坟障碍物 | 仅 Digger + 未消除 Grave；圆形半径在 Prefab 上 | [§3.10](SPEC_03_GameRules.md) |
| DigProtagonistCapabilities | 挖坟主角能力 | 伤害/时长缩短和/光标半径/可挖品质/阶段时长加成；科技树学会写入 | [§3.10](SPEC_03_GameRules.md)、[§3.13](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| GraveHP | 坟墓血量 | maxHP 来自品质表；归 0 触发成功与奖励 | [§3.10](SPEC_03_GameRules.md) |
| GraveIconStyle | 坟墓图标样式 | 按剩余 HP%：>65%/30–65%/<30% → 样式1/2/3 | [§3.10](SPEC_03_GameRules.md) |
| GraveQualityConfig | 坟墓品质定义表 | QualityId → MaxHP、LootDrop、IconStyleHighId/MidId/LowId | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| DigReward | 挖掘奖励 | HP=0 时生成；飞向主角到达后入账并消失 | [§3.10](SPEC_03_GameRules.md) |
| DigStageSummary | 挖坟阶段汇总 | 时长归零后弹窗；仅汇总本阶段已获奖励；无额外发放 | [§3.10](SPEC_03_GameRules.md)、[§3.6](SPEC_03_GameRules.md) |
| Warehouse | 仓库 | 存档槽材料仓；不限格/时长；按类型堆叠上限 10000 | [§3.10](SPEC_03_GameRules.md) |
| SpiritEssence | 精魂 | 货币；LootDrop `Spirit` + AutoConvert；造士兵消耗 | [§3.10](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md) |
| MaterialConfig | 材料配置表 | MaterialId → AutoConvert、外观图、素材路径、仓库品质外轮廓 | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| CurrencyConfig | 货币配置表 | CurrencyId → 外观图、素材路径、仓库品质外轮廓；精魂=`Spirit` | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| UpgradeManufacture | 升级与制造 | 原 SewRevive；升级 + 造士兵 + 布阵 | [§3.11](SPEC_03_GameRules.md) |
| Experience | 经验 | Defend 阶段胜利入账至 LifetimeExperience；失败不入账；升级不扣累计 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| LifetimeExperience | 生涯累计经验 | 存档经验总值；只增不因升级减少 | [§3.11](SPEC_03_GameRules.md) |
| ProtagonistLevelConfig | 主角升级配置表 | Level → 累计经验阈值、预留解锁、科技点、控制力上限、ProtagonistMaxHP（Defend 作护盾上限） | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.8](SPEC_04_Technical.md) |
| TechPoint | 科技点数 | 升级获得；科技树学习费用 | [§3.11](SPEC_03_GameRules.md)、[§3.13](SPEC_03_GameRules.md) |
| TechTree | 科技树 | 中心向外科技项图；前后置+LearnCost；设置页画布 | [§3.13](SPEC_03_GameRules.md)、[SPEC_04 §9.16](SPEC_04_Technical.md) |
| TechItem | 科技项 | 科技树节点；图标+类型框；可学会 | [§3.13](SPEC_03_GameRules.md) |
| TechEffect | 科技项效果 | 属性增量与/或功能系统解锁 | [§3.13](SPEC_03_GameRules.md)、[SPEC_04 §9.17](SPEC_04_Technical.md) |
| TechTreeConfig | 科技树配置表 | TechId → 图标/名/描述/后续ID/初始解锁/费用/TechUiFrameType（Root/Normal/Key/Capstone） | [SPEC_04 §9.16](SPEC_04_Technical.md) |
| TechEffectConfig | 科技项效果配置表 | TechId → AttributeModifiers、UnlockedFeatureSystemName | [SPEC_04 §9.17](SPEC_04_Technical.md) |
| UnlockedFeatureSystems | 已解锁功能系统 | 存档集合；科技效果写入 | [§3.13](SPEC_03_GameRules.md) |
| Material | 材料 | 挖坟入仓库；造士兵消耗（与精魂并列） | [§3.10](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md) |
| Warrior | 士兵 | 制造产出的独立实例（ID/名字/血量/属性构成）；非堆叠；中文单位称「士兵」，英文标识仍为 `Warrior`；勿与职业名「战士」混淆 | [§3.11](SPEC_03_GameRules.md) |
| WarriorInfo | 士兵信息 | 主标签=定稿种族；不改数值 | [§3.11](SPEC_03_GameRules.md) |
| WarriorName | 士兵名字 | Prefix(es)+RaceName+ClassName+Suffix | [§3.11](SPEC_03_GameRules.md) |
| ManufactureSlot | 制造槽位 | 头1/躯干1/臂2/腿2/灵魂1/宝石6/坐骑1/翅膀1 | [§3.11](SPEC_03_GameRules.md) |
| BodyPart | 躯体部位 | Head/Torso/Arm/Leg 材料；BodyPartConfig（BodyLevel/StatBonus/RaceId/SpiritCost/AutoConvert 等） | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.12](SPEC_04_Technical.md) |
| BodyPartConfig | 躯体材料配置表 | BodyPartId → 等级/部位/种族/控制力/精魂/StatBonus/AutoConvert/介绍/美术 | [SPEC_04 §9.12](SPEC_04_Technical.md) |
| BodySlot | 躯体槽类型 | Head / Torso / Arm / Leg | [§3.11](SPEC_03_GameRules.md) |
| BodyLevel | 躯体等级 | 躯体材料字段；平均后定外观等级 | [§3.11](SPEC_03_GameRules.md) |
| StatBonus | 增加的属性值 | 躯体平坦加成；Base(S)=Σ StatBonus(S) | [§3.11](SPEC_03_GameRules.md) |
| Body | 躯体 | 部位集合；Base(S)=Σ StatBonus；部位加权定种族 | [§3.11](SPEC_03_GameRules.md) |
| BaseStats | 基础属性 | HP/移速/力量/敏捷/智力；Σ StatBonus；经 StaticStat/FinalStat 派生攻/速/CD/血 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| StaticStat | 静态属性 | 制造/布阵：不含 SkillBuff 的终值 | [§3.11](SPEC_03_GameRules.md) |
| PrimaryStat | 主属性 | 职业字段 Strength/Agility/Intelligence；定普攻属性维 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md) |
| BodyLife | 躯体生命 | Base(MaxHP)+Equip(MaxHP)；代入 MaxHP=ceil(BodyLife+Str×3) | [§3.11](SPEC_03_GameRules.md) |
| NormalAttackPower | 普通攻击值 | Primary×NormalAttackPrimaryMult（缺省 1.5；见 ClassConfig.CombatConvertCoeffs） | [§3.12](SPEC_03_GameRules.md) |
| AttackSpeed | 攻击速度 | 次/秒：0.5+60/max(Agi,1)（过渡） | [§3.12](SPEC_03_GameRules.md) |
| BodyAppearance | 躯体外观 | 预设整体造型；按平均等级+种族+职业选取；烘焙整角 Prefab | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.13](SPEC_04_Technical.md)、[§15](SPEC_04_Technical.md) |
| BodyAppearanceConfig | 躯体外观配置表 | AppearanceId → Prefab 逻辑名（`Prefabs/Defend/Warriors/{Id}`）/等级/种族/职业倾向/保底 | [SPEC_04 §9.13](SPEC_04_Technical.md)、[§15](SPEC_04_Technical.md) |
| IsFallback | 保底外形 | 外观表字段；1=种族保底；每种族至多一行 | [§3.11](SPEC_03_GameRules.md) |
| Race | 种族 | 部位权重1加权随机定稿；五维 RaceAdjustCoeff；主标签 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.11](SPEC_04_Technical.md) |
| RaceConfig | 种族配置表 | RaceId → 展示名、五维调整系数、失控概率加成 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.11](SPEC_04_Technical.md) |
| RaceAdjustCoeff | 种族属性调整系数 | Base(S)×系数；缺省维=0；可正负；不计控制力 | [§3.11](SPEC_03_GameRules.md) |
| Soul | 灵魂 | 制造必注入；ClassId/技能/AttackMode/攻击优先级/移动风格；不改写三维；Demo 不施放技能 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md) |
| SoulConfig | 灵魂配置表 | SoulId → ClassId、AttackMode、Skills（`SkillId;Level|…`）、AttackPriority（同 TargetSelect）、MoveStyle、SpiritCost、ControlPowerCost | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md) |
| Class | 职业 | 灵魂 ClassId 提供；ClassName/PrimaryStat/五维→战斗参数换算系数 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md) |
| ClassId | 职业ID | 职业主键；灵魂必填；写入士兵实例 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md) |
| ClassConfig | 职业配置表 | ClassId → ClassName、PrimaryStat、CombatConvertCoeffs（`键_数值|…`）、AttackRange / 前摇 / 弹速 / 超时 | [SPEC_04 §9.9b](SPEC_04_Technical.md) |
| ClassName | 职业名 | 职业表字段；参与 WarriorName 与外观 ClassAffinity；可为「战士」等，**不是**单位称谓「士兵」 | [§3.11](SPEC_03_GameRules.md) |
| MoveStyle | 移动风格 | `Normal` \| `Aggressive` \| `Cautious` | [§3.11](SPEC_03_GameRules.md) |
| ExtraEquipment | 额外装备 | 翅膀/坐骑；制造锁定；属性/技能/NamePrefix | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.14](SPEC_04_Technical.md) |
| ExtraEquipmentConfig | 额外装备配置表 | EquipSlot、NamePrefix、EquipStats（`Attr_Value|…`）、Skills（`SkillId;Level|…`）、SpiritCost、ControlPowerCost | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.14](SPEC_04_Technical.md) |
| NamePrefix | 名字前缀 | 外置装备字段；两件依次拼接 | [§3.11](SPEC_03_GameRules.md) |
| SpiritCost | 精魂消耗 | 材料/灵魂/外置/宝石字段；制造总消耗=求和 | [§3.11](SPEC_03_GameRules.md) |
| Gem | 宝石 | 制造可选；6槽类型互斥；五维 GemMult（多颗Σ）；彻底死亡全部回仓；带宝石士兵 HP≤0 立即彻底死亡 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.10](SPEC_04_Technical.md) |
| GemType | 宝石类型 | 六类互斥：`Ruby` / `Sapphire` / `Emerald` / `Topaz` / `Amethyst` / `Diamond` | [§3.11](SPEC_03_GameRules.md) |
| GemConfig | 宝石配置表 | GemId → GemType、五维 GemMult、Skills、SpiritCost、ControlPowerCost、失控概率加成 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.10](SPEC_04_Technical.md) |
| GemMult | 宝石放大系数 | 五维；多颗按维 Σ；无宝石=0；Base(S)×GemMult(S) | [§3.11](SPEC_03_GameRules.md) |
| GemSuffixNameConfig | 宝石后缀命名表 | 已镶嵌 GemType 排序拼接 ComboKey → WarriorName 后缀 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.15](SPEC_04_Technical.md) |
| ControlPowerCost | 控制力占用值 | 躯体+灵魂+额外装备+宝石；制造时定稿 | [§3.11](SPEC_03_GameRules.md) |
| SkillBuffCoeff | 技能 Buff 系数 | 仅战斗运行时；FinalStat 公式 | [§3.11](SPEC_03_GameRules.md) |
| ControlPower | 控制力 | 上阵占用；本版上限=等级行 ControlPowerCap；超额失控 | [§3.11](SPEC_03_GameRules.md) |
| LossOfControl | 失控 | Degree 分档；开战锁定；各士兵独立 roll；成功→叛变 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| LossOfControlDegree | 失控程度 | ΣCost/Cap−1；≤0 未失控；开战锁定 | [§3.11](SPEC_03_GameRules.md) |
| LossOfControlTier | 失控程度段 | 1~4（轻度/中度/重度/完全） | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.20](SPEC_04_Technical.md) |
| LossOfControlConfig | 失控配置表 | TierId→名称/描述/基础失控概率 | [SPEC_04 §9.20](SPEC_04_Technical.md) |
| Rebel | 叛变 | 失控成功状态；就近打主角/士兵/敌人；至死亡 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| SkillConfig | 技能配置表 | 骨架；BaseCooldownSeconds + 失控概率加成；效果列另专题（同文件扩写）；**Demo 不施放技能** | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.21](SPEC_04_Technical.md) |
| BattleFormation | 战斗布阵 | 连续坐标；§3.11 与 Prepare 同一编辑器 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| Defend | 防守 | Prepare→开战→Combat；胜负见专节 | [§3.12](SPEC_03_GameRules.md) |
| DefendPhase | 防守子状态 | Prepare / Combat / Ended | [§3.12](SPEC_03_GameRules.md) |
| StartBattle | 开战 | 准备态按钮；进入 Combat 并部署 | [§3.12](SPEC_03_GameRules.md) |
| BattleMap | 战斗地图 | 连续可走空间；与 DigMap 阶段分离；表现可共用 `Ground_*`（`BattleMapId`）；Prefab 含 EngageZone | [§3.12](SPEC_03_GameRules.md) |
| EngageZone | 选敌区 | 地图 Prefab 上比地图稍小的轴对齐方形；非叛变士兵仅区内选最近敌人 | [§3.12](SPEC_03_GameRules.md) |
| AttackMode | 攻击模式 | Melee/Ranged；士兵 SoulConfig / 怪物 MonsterConfig；普攻命中分支 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md)、[§9.19](SPEC_04_Technical.md) |
| AttackRange | 攻击距离 | 士兵 ClassConfig / 怪物 MonsterConfig；进入距内才攻击 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md)、[§9.19](SPEC_04_Technical.md) |
| CombatDead | 战斗死亡 | 无宝石士兵 HP≤0；可复活；不触发物资去向 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| PermanentDeath | 彻底死亡 | 物资去向+清实例/布阵位；Ended/LevelFailure 结算或宝石特例立即 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| AttackWindup | 攻击前摇 | 近战命中确认前计时 | [§3.12](SPEC_03_GameRules.md) |
| HitConfirm | 命中确认 | 规则层结算伤害时刻（近战前摇结束 / 远程弹道命中） | [§3.12](SPEC_03_GameRules.md) |
| BattleProtagonist | 战斗主角 | 地图中央；异于 Digger；Defend 用护盾承受普通攻击；烘焙整角 Prefab | [§3.12](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md)、[SPEC_04 §15](SPEC_04_Technical.md) |
| Shield | 护盾 | 普通攻击承受次数（敌人或叛变士兵）；开战 = ProtagonistMaxHP；归零 LevelFailure | [§3.12](SPEC_03_GameRules.md) |
| Monster | 怪物 | 防守敌方；InsideMap/OutsideMap；ModelId 烘焙整角 Prefab | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.19](SPEC_04_Technical.md)、[§15](SPEC_04_Technical.md) |
| MonsterConfig | 怪物配置表 | MonsterId → ModelId/目标选择/攻模/血量/移速/攻力/攻速/AttackRange 等/技能/掉落；Demo 技能不生效 | [SPEC_04 §9.19](SPEC_04_Technical.md) |
| Wave | 波次 | WaveConfigId 下刷怪行集合；全触发且全灭为胜利条件之一 | [§3.12](SPEC_03_GameRules.md) |
| WaveSpawnConfig | 刷怪波次配置表 | WaveConfigId + 顺序/剩余秒/怪物/数量/位置/方式 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.18](SPEC_04_Technical.md) |
| WaveConfigId | 波次配置ID | DefendGameplayConfig → WaveSpawnConfig 分组键 | [SPEC_04 §9.7](SPEC_04_Technical.md) |
| RemainingCombatSeconds | 战斗剩余秒 | 开战倒计时剩余整秒；等于 SpawnRemainingSeconds 时刷怪 | [§3.12](SPEC_03_GameRules.md) |
| TargetSelect | 目标选择 | Nearest / PreferWarrior / PreferProtagonist | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.19](SPEC_04_Technical.md) |
| AttackPriority | 攻击优先级 | 灵魂字段；与 TargetSelect 同枚举；本批不驱动选目标（默认 EngageZone 内最近） | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| TargetRetargetInterval | 目标修正间隔 | 怪物与士兵重算目的地间隔；暂定 1s | [§3.12](SPEC_03_GameRules.md) |
| LevelFailure | 关卡失败 | 护盾归零等；与 VictorySettlement 互斥；无本阶段经验/无关卡结算奖励；已获不扣 | [§3.9](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| VictorySettlement | 胜利结算 | 最后一阶段结束后的关卡级结算 | [§3.9](SPEC_03_GameRules.md) |
| Demo acceptance (D-xxx) | Demo 验收项 | D-001～D-004 Meta 壳；D-010～D-043 Dig→UM→Defend 流水线垂直切片 | [§3.8](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |

## 维护规则

- 新增术语时同步一行；正文写在 SPEC
- 禁止在本文件写完整规则或数值表
- ADR 仅记录架构决策（`docs/adr/`）
