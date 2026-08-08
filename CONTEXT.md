# Gravedigger2026 — 领域术语表 / Domain Glossary

**本文件是术语索引，不是规则权威。** 规则以 [SPEC_03_GameRules.md](SPEC_03_GameRules.md) 为准；技术约定以 [SPEC_04_Technical.md](SPEC_04_Technical.md) 为准。

| 术语 (EN) | 中文 | 定义摘要 | SPEC |
|-----------|------|----------|------|
| Gravedigger2026 | 本项目 | Unity 工程与工作区名称 | [SPEC_02](SPEC_02_GameOverview.md) |
| GameplayState | 玩法状态 | Dig / UpgradeManufacture / Defend / PushMap；关卡内由阶段玩法类型驱动 | [§3.1](SPEC_03_GameRules.md)、[§3.7](SPEC_03_GameRules.md)、[§3.9](SPEC_03_GameRules.md) |
| SaveSlot | 存档槽 | 固定 3 槽本地存档位；占用旗 + 士兵池/布阵/副本解锁等按槽 PlayerPrefs | [§3.4](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |
| InSaveShell | 进档壳层 | 进档后常驻壳（玩法占位 + 工具） | [§3.1](SPEC_03_GameRules.md)、[§3.3](SPEC_03_GameRules.md) |
| ToolsPanel | 工具面板 | Demo 设置/调试壳；含设置、关卡入口（流水线片可启样例关卡） | [§3.5](SPEC_03_GameRules.md) |
| Level | 关卡 | 关卡运作表驱动的多阶段流程；UM 阶段 `GameplayConfigId` 忽略 | [§3.1](SPEC_03_GameRules.md)、[§3.9](SPEC_03_GameRules.md) |
| ConfigTables | 配置表根目录 | `Assets/ConfigTables/`：Excel 源（四段中英名）+ CSV 产物（两段英文名） | [SPEC_04 §14](SPEC_04_Technical.md) |
| BakeTables | 打表 | Editor 一键 Excel→CSV（Excel 四段名映射为 CSV 英文基名）；菜单 `Gravedigger2026/Config/Bake Tables` | [SPEC_04 §14](SPEC_04_Technical.md) |
| CharacterArtPipeline | 角色美术管线 | Character Creator **烘焙整角**；游戏资源不得落在工具目录；导出补丁→`Art/Characters`→`Prefabs` | [SPEC_04 §15](SPEC_04_Technical.md) |
| BakedWholeCharacter | 烘焙整角 | 用 Creator 拼装后导出整角 spritesheet/Animator/Prefab；非运行时叠装 | [SPEC_04 §15](SPEC_04_Technical.md) |
| LevelOperation | 关卡运作 | 关卡 ID + 阶段编号 + 玩法类型 + 玩法配置 ID | [§3.9](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| DigGameplayConfig | 挖坟配置 | 基础时长、开局坟数、过程生成速率、品质权重（零权重剔除） | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| DigMap | 挖坟地图 | 菱形外观；逻辑为整体可放置空间（非格子）；表现 Prefab `Ground_01`…`Ground_05`（`DigMapId`） | [§3.10](SPEC_03_GameRules.md) |
| Grave | 坟墓 | 挖坟可生成实体；带品质 ID；未消除时为 DigObstacle | [§3.10](SPEC_03_GameRules.md) |
| Digger | 挖坟主角 | 挖坟阶段地图中心生成；待机/挖坟循环动画；烘焙整角 Prefab | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §15](SPEC_04_Technical.md) |
| DigAction | 挖掘流程 | 0.2s 停留触发；`DigActionDuration` 帧动画后扣血；busy 不可重触 | [§3.10](SPEC_03_GameRules.md) |
| DigObstacle | 挖坟障碍物 | 仅 Digger + 未消除 Grave；圆形半径在 Prefab 上 | [§3.10](SPEC_03_GameRules.md) |
| DigHitShape | 挖坟命中形 | Grave Prefab 离线烘焙本地 XZ 凸包；光标圆相交触发挖掘；与障碍圆分离 | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9.2](SPEC_04_Technical.md) |
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
| Experience | 经验 | Defend 或 PushMap 阶段胜利入账至 LifetimeExperience；失败不入账；升级不扣累计 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[§3.14](SPEC_03_GameRules.md) |
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
| WarriorAnimView | 士兵/怪物动画表现 | 表现层：驱动 Creator Animator（`IsRun`/`Attack1`/`Die`/`DirIndex`+`Direction` 同值）；可选 `FacingYawFlip`；士兵与怪物（Defend/PushMap）共用 | [SPEC_04 §15.5](SPEC_04_Technical.md) |
| FacingYawFlip | 朝向整圈翻转 | 配表 0\|1；写入 Animator 前 `(DirIndex+4)%8`（180°）；士兵=`BodyAppearanceConfig`，怪=`MonsterConfig`；缺省 0 | [SPEC_04 §15.5](SPEC_04_Technical.md)/[§9.13](SPEC_04_Technical.md)/[§9.19](SPEC_04_Technical.md) |
| FacingHysteresis | 朝向迟滞 | 推图怪八向切换迟滞：候选扇区越过当前边界 +12° 且过最短保持 0.12s 才切换；仅 `PushMapMonsterAgentView` | [SPEC_04 §15.5](SPEC_04_Technical.md) |
| StuckHold | 受堵停滞 | 推图怪 steer 非零但 0.25s 滑窗 XZ 位移 < 0.05 → 停播 Run 面向追击目标；位移恢复或 steer 归零即退出 | [SPEC_04 §15.5](SPEC_04_Technical.md)、[§3.14](SPEC_03_GameRules.md) |
| WarriorInfo | 士兵信息 | 主标签=定稿种族；不改数值 | [§3.11](SPEC_03_GameRules.md) |
| WarriorName | 士兵名字 | Prefix(es)+RaceName+ClassName+Suffix | [§3.11](SPEC_03_GameRules.md) |
| ManufactureSlot | 制造槽位 | 头1/躯干1/臂2/腿2/灵魂1/宝石6/坐骑1/翅膀1 | [§3.11](SPEC_03_GameRules.md) |
| Remanufacture | 再造 | 按士兵实例配方快照后台再走制造流水线，成功则新增池内士兵；不足弹 Tips | [§3.11](SPEC_03_GameRules.md) |
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
| NormalAttackPower | 普通攻击值 | Primary×NormalAttackPrimaryMult（缺省 15；见 ClassConfig.CombatConvertCoeffs） | [§3.12](SPEC_03_GameRules.md) |
| AttackSpeed | 攻击速度 | 次/秒：0.5+60/max(Agi,1)（过渡） | [§3.12](SPEC_03_GameRules.md) |
| BodyAppearance | 躯体外观 | 预设整体造型；按平均等级+种族+职业选取；烘焙整角 Prefab | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.13](SPEC_04_Technical.md)、[§15](SPEC_04_Technical.md) |
| BodyAppearanceConfig | 躯体外观配置表 | AppearanceId → Prefab 逻辑名（`Prefabs/Defend/Warriors/{Id}`）/等级/种族/职业倾向/保底/`BodyRadius`（士兵占地；缺省 0.1）/`FacingYawFlip` | [SPEC_04 §9.13](SPEC_04_Technical.md)、[§15](SPEC_04_Technical.md) |
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
| BattleFormation | 战斗布阵 | 连续坐标；共享 `FormationEditor`；与士兵池同槽 PlayerPrefs 持久化 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[§3.14](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |
| WarriorPool | 士兵可上阵池 | 存档级已造士兵实例集合；制造入池；布阵/Defend/PushMap 共用；按槽持久化 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |
| FormationEditor | 布阵编辑器 | Prefab `FormationEditorRoot`；底栏士兵格（上阵保留+变亮）+ Idle 跟手拖放；UM 返回 / Defend 开战 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| Defend | 防守 / 保卫战 | Prepare→开战→Combat；亦可作战斗模式1；见专节 | [§3.12](SPEC_03_GameRules.md) |
| BattleMode | 战斗模式 | Defend（保卫战）/ PushMap（推图战；规则 §3.14） | [§3.12](SPEC_03_GameRules.md)、[§3.14](SPEC_03_GameRules.md) |
| BattleModeSelect | 战斗模式选关 | 进入 Defend 后选模式+关卡（UI-013 / D-044；模式2确认→§3.14） | [§3.12](SPEC_03_GameRules.md)、[§3.6](SPEC_03_GameRules.md) |
| PushMap | 推图战 | GameplayType/GameplayState；亦可作战斗模式2；目标点占领+刷怪/陷阱/BOSS；复用 Defend 布阵/护盾/失控 | [§3.14](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| PushMapPhase | 推图战子状态 | Prepare / Combat / Ended | [§3.14](SPEC_03_GameRules.md) |
| MapId | 地图编号 | PushMap 地图 Prefab 逻辑名（≠ LevelId）；`Ground_*` 或 `PushMap_*`（Demo 样例 `PushMap_Demo_01`）→ `Prefabs/Maps/` | [§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.22](SPEC_04_Technical.md) |
| ObjectivePoint | 目标点 | 有序推进点 1→2→3…；全队共当前目标 | [§3.14](SPEC_03_GameRules.md) |
| CaptureZone | 判定圈 | 默认半径 2；任一忠诚兵进入当前圈 → 立即占领 | [§3.14](SPEC_03_GameRules.md) |
| Capture | 占领 | 目标点本场已占领；关联刷怪停刷；可发奖励/副本解锁钩子 | [§3.14](SPEC_03_GameRules.md) |
| AirWall | 空气墙 | 阻挡敌我；支持 Y 轴 45° 旋转 | [§3.14](SPEC_03_GameRules.md) |
| SpawnPoint | 刷怪点 | 地图独立编号；由 PushMapSpawnConfig 驱动 | [§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.23](SPEC_04_Technical.md) |
| TrapZone | 陷阱区域 | 忠诚士兵进入触发绑定刷怪点 | [§3.14](SPEC_03_GameRules.md) |
| BossPoint | BOSS 点 | 击杀该点 BOSS → PushMap 阶段通关 | [§3.14](SPEC_03_GameRules.md) |
| AggroMode | 仇恨模式 | ActiveChase/PassiveChase/StationaryActive/StationaryPassive；异于 AttackMode | [§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.19](SPEC_04_Technical.md) |
| AlertRadius | 警戒半径 | AggroMode 主动发现半径 | [§3.14](SPEC_03_GameRules.md) |
| BodyRadius | 占地半径 | 单位 XZ 占地圆；怪物=`MonsterConfig`；士兵=`BodyAppearanceConfig`（按 AppearanceId，缺省 0.1）；PushMap 刷出散开与 NavMeshAgent/MassMove 避障 | [§3.12](SPEC_03_GameRules.md)/[§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.13](SPEC_04_Technical.md)/[§9.19](SPEC_04_Technical.md) |
| DungeonUnlock | 副本解锁 | 存档钩子；副本玩法 TBD | [§3.14](SPEC_03_GameRules.md) |
| CameraFollowMode | 镜头跟随模式 | PushMap Combat：`Auto` / `Manual` | [§3.14](SPEC_03_GameRules.md) |
| ResumeFollow | 恢复跟随 | 手动模式底中按钮 → 回 Auto | [§3.14](SPEC_03_GameRules.md) |
| DamagePopup | 伤害飘字 | PushMap 命中后头顶 `-受伤值`；怪红/兵白字号 12；0.5s Z +0→+0.5 后销毁 | [§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.22](SPEC_04_Technical.md) |
| HitFlash | 受伤闪烁 | PushMap 命中后模型亮色；怪红/兵白；2×0.1s 紧接不灭；重伤刷新 | [§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.22](SPEC_04_Technical.md) |
| PushMapGameplayConfig | 推图战配置表 | MapId/经验/占领掉落/副本解锁等 | [SPEC_04 §9.22](SPEC_04_Technical.md) |
| PushMapSpawnConfig | 推图战刷怪表 | SpawnPointId+MonsterId+陷阱/目标关联 | [SPEC_04 §9.23](SPEC_04_Technical.md) |
| DefendPhase | 防守子状态 | ModeSelect / Prepare / Combat / Ended | [§3.12](SPEC_03_GameRules.md) |
| StartBattle | 开战 | 准备态按钮；进入 Combat 并部署 | [§3.12](SPEC_03_GameRules.md) |
| BattleMap | 战斗地图 | 连续可走空间；与 DigMap 阶段分离；表现可共用 `Ground_*`（`BattleMapId`）；Prefab 含 EngageZone | [§3.12](SPEC_03_GameRules.md) |
| EngageZone | 选敌区 | 地图 Prefab 上比地图稍小的 IsoDiamond（XZ 菱形）；非叛变士兵仅区内选最近敌人 | [§3.12](SPEC_03_GameRules.md) |
| FormationHome | 布阵原点 | 开战部署锁定的布阵世界坐标；无 EngageZone 目标时非叛变士兵自动返回 | [§3.12](SPEC_03_GameRules.md) |
| MassCombatPathing | 大规模战斗寻路 | 双方约 200：FlowField + AttackSlot + LocalDetour（方案 B）；B+ 叠 SoftCollision / CombatMoveMode | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.7](SPEC_04_Technical.md) |
| FlowField | 流场 | 共享目的地格点方向场；同目标单位共享采样 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.7](SPEC_04_Technical.md) |
| AttackSlot | 攻击槽位 | `AttackRange` 环上可站立到达点；单位认领 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.7](SPEC_04_Technical.md) |
| LocalDetour | 本地绕行 | 默认直线；遇友军左/右短探测绕行；友军不 Carve | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.7](SPEC_04_Technical.md) |
| CombatMoveMode | 战斗移动模式 | Chase \| Surround \| Sweep（**无 Follow**）；叠在 GoalKind 上 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.7](SPEC_04_Technical.md) |
| SoftCollision | 单位软碰撞 | XZ 圆足迹 + 邻域排斥；集中服务解算 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.7](SPEC_04_Technical.md) |
| SurroundGap | 包围缺口 | Surround 下 AttackSlot 环跳过的扇区 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.7](SPEC_04_Technical.md) |
| DesiredDestination | 期望目的地 | 移动层趋近的世界坐标（Objective/Home/Slot 等） | [§3.12](SPEC_03_GameRules.md) |
| GoalKind | 目的地种类 | Objective \| FormationHome \| AttackSlot \| ChaseAnchor | [§3.12](SPEC_03_GameRules.md) |
| IsoDiamond | 地图菱形足迹 | XZ 曼哈顿菱形（`|dx|/hx+|dz|/hz≤1`）；半尺寸 = `PaintRadius*(cellSize.x,cellSize.y)`，可随 iso 高宽比各向异性（Demo `(5,2.5)`）；`DigMapBounds`/`EngageZone`/`WalkSurface`/NavMesh 共用 | [§3.10](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.7](SPEC_04_Technical.md) |
| AttackMode | 攻击模式 | Melee/Ranged；士兵 SoulConfig / 怪物 MonsterConfig；普攻命中分支；**异于** AggroMode | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md)、[§9.19](SPEC_04_Technical.md) |
| AttackRange | 攻击距离 | 士兵 ClassConfig / 怪物 MonsterConfig；进入距内才攻击 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md)、[§9.19](SPEC_04_Technical.md) |
| CombatDead | 战斗死亡 | 无宝石士兵 HP≤0；可复活；不触发物资去向 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| PermanentDeath | 彻底死亡 | 物资去向+清实例/布阵位；Ended/LevelFailure 结算或宝石特例立即 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| AttackWindup | 攻击前摇 | 近战命中确认前计时 | [§3.12](SPEC_03_GameRules.md) |
| HitConfirm | 命中确认 | 规则层结算伤害时刻（近战前摇结束 / 远程弹道命中） | [§3.12](SPEC_03_GameRules.md) |
| BattleProtagonist | 战斗主角 | 地图中央；异于 Digger；Defend 用护盾承受普通攻击；烘焙整角 Prefab | [§3.12](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md)、[SPEC_04 §15](SPEC_04_Technical.md) |
| Shield | 护盾 | 普通攻击承受次数（敌人或叛变士兵）；开战 = ProtagonistMaxHP；归零 LevelFailure | [§3.12](SPEC_03_GameRules.md) |
| Monster | 怪物 | 防守敌方；InsideMap/OutsideMap；ModelId 烘焙整角 Prefab | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.19](SPEC_04_Technical.md)、[§15](SPEC_04_Technical.md) |
| MonsterConfig | 怪物配置表 | MonsterId → ModelId/目标选择/AttackMode/AggroMode/AlertRadius/BodyRadius/FacingYawFlip/血量/移速/攻力/攻速/AttackRange 等/技能/掉落；Demo 技能不生效 | [SPEC_04 §9.19](SPEC_04_Technical.md)、[§3.14](SPEC_03_GameRules.md) |
| Wave | 波次 | WaveConfigId 下刷怪行集合；全触发且全灭为胜利条件之一 | [§3.12](SPEC_03_GameRules.md) |
| WaveSpawnConfig | 刷怪波次配置表 | WaveConfigId + 顺序/剩余秒/怪物/数量/位置/方式 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.18](SPEC_04_Technical.md) |
| WaveConfigId | 波次配置ID | DefendGameplayConfig → WaveSpawnConfig 分组键 | [SPEC_04 §9.7](SPEC_04_Technical.md) |
| RemainingCombatSeconds | 战斗剩余秒 | 开战倒计时剩余整秒；等于 SpawnRemainingSeconds 时刷怪 | [§3.12](SPEC_03_GameRules.md) |
| TargetSelect | 目标选择 | Nearest / PreferWarrior / PreferProtagonist | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.19](SPEC_04_Technical.md) |
| AttackPriority | 攻击优先级 | 灵魂字段；与 TargetSelect 同枚举；本批不驱动选目标（默认 EngageZone 内最近） | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| TargetRetargetInterval | 目标修正间隔 | 怪物与士兵重算目的地间隔；暂定 1s | [§3.12](SPEC_03_GameRules.md) |
| LevelFailure | 关卡失败 | 护盾归零等（Defend/PushMap）；与 VictorySettlement 互斥；无本阶段经验/无关卡结算奖励；已获不扣 | [§3.9](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[§3.14](SPEC_03_GameRules.md) |
| VictorySettlement | 胜利结算 | 最后一阶段结束后的关卡级结算 | [§3.9](SPEC_03_GameRules.md) |
| Demo acceptance (D-xxx) | Demo 验收项 | D-001～D-004 Meta 壳；D-010～D-044 Dig→UM→Defend（含 ModeSelect）流水线垂直切片 | [§3.8](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |

## 维护规则

- 新增术语时同步一行；正文写在 SPEC
- 禁止在本文件写完整规则或数值表
- ADR 仅记录架构决策（`docs/adr/`）
