# Gravedigger2026 — 领域术语表 / Domain Glossary

**本文件是术语索引，不是规则权威。** 规则以 [SPEC_03_GameRules.md](SPEC_03_GameRules.md) 为准；技术约定以 [SPEC_04_Technical.md](SPEC_04_Technical.md) 为准。

| 术语 (EN) | 中文 | 定义摘要 | SPEC |
|-----------|------|----------|------|
| Gravedigger2026 | 本项目 | Unity 工程与工作区名称 | [SPEC_02](SPEC_02_GameOverview.md) |
| GameplayState | 玩法状态 | Dig / UpgradeManufacture / Defend；关卡内由阶段玩法类型驱动 | [§3.1](SPEC_03_GameRules.md)、[§3.7](SPEC_03_GameRules.md)、[§3.9](SPEC_03_GameRules.md) |
| SaveSlot | 存档槽 | 固定 3 槽本地存档位 | [§3.4](SPEC_03_GameRules.md) |
| InSaveShell | 进档壳层 | 进档后常驻壳（玩法占位 + 工具） | [§3.1](SPEC_03_GameRules.md)、[§3.3](SPEC_03_GameRules.md) |
| ToolsPanel | 工具面板 | Demo 设置/调试壳；含设置、关卡占位 | [§3.5](SPEC_03_GameRules.md) |
| Level | 关卡 | 关卡运作表驱动的多阶段流程 | [§3.1](SPEC_03_GameRules.md)、[§3.9](SPEC_03_GameRules.md) |
| LevelOperation | 关卡运作 | 关卡 ID + 阶段编号 + 玩法类型 + 玩法配置 ID | [§3.9](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| DigGameplayConfig | 挖坟配置 | 基础时长、开局坟数、过程生成速率、品质权重（零权重剔除） | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| DigMap | 挖坟地图 | 菱形外观；逻辑为整体可放置空间（非格子） | [§3.10](SPEC_03_GameRules.md) |
| Grave | 坟墓 | 挖坟可生成实体；带品质 ID；未消除时为 DigObstacle | [§3.10](SPEC_03_GameRules.md) |
| Digger | 挖坟主角 | 挖坟阶段地图中心生成；待机/挖坟循环动画 | [§3.10](SPEC_03_GameRules.md) |
| DigAction | 挖掘流程 | 0.2s 停留触发；`DigActionDuration` 帧动画后扣血；busy 不可重触 | [§3.10](SPEC_03_GameRules.md) |
| DigObstacle | 挖坟障碍物 | 仅 Digger + 未消除 Grave；圆形半径在 Prefab 上 | [§3.10](SPEC_03_GameRules.md) |
| DigProtagonistCapabilities | 挖坟主角能力 | 伤害/时长缩短和/光标半径/可挖品质/阶段时长加成；科技写入 | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| GraveHP | 坟墓血量 | maxHP 来自品质表；归 0 触发成功与奖励 | [§3.10](SPEC_03_GameRules.md) |
| GraveIconStyle | 坟墓图标样式 | 按剩余 HP%：>65%/30–65%/<30% → 样式1/2/3 | [§3.10](SPEC_03_GameRules.md) |
| GraveQualityConfig | 坟墓品质定义表 | QualityId → MaxHP、LootDrop、IconStyleSet | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| DigReward | 挖掘奖励 | HP=0 时生成；飞向主角到达后入账并消失 | [§3.10](SPEC_03_GameRules.md) |
| DigStageSummary | 挖坟阶段汇总 | 时长归零后弹窗；仅汇总本阶段已获奖励；无额外发放 | [§3.10](SPEC_03_GameRules.md)、[§3.6](SPEC_03_GameRules.md) |
| Warehouse | 仓库 | 存档槽材料仓；不限格/时长；按类型堆叠上限 10000 | [§3.10](SPEC_03_GameRules.md) |
| SpiritEssence | 精魂 | 货币；LootDrop `Spirit` + AutoConvert；造战士消耗 | [§3.10](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md) |
| MaterialConfig | 材料配置表 | MaterialId → AutoConvert、外观图、素材路径、仓库品质外轮廓 | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| CurrencyConfig | 货币配置表 | CurrencyId → 外观图、素材路径、仓库品质外轮廓；精魂=`Spirit` | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| UpgradeManufacture | 升级与制造 | 原 SewRevive；升级 + 造战士 + 布阵 | [§3.11](SPEC_03_GameRules.md) |
| Experience | 经验 | Defend 阶段胜利入账至 LifetimeExperience；失败不入账；升级不扣累计 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| LifetimeExperience | 生涯累计经验 | 存档经验总值；只增不因升级减少 | [§3.11](SPEC_03_GameRules.md) |
| ProtagonistLevelConfig | 主角升级配置表 | Level → 累计经验阈值、预留解锁、科技点、控制力上限、主角 MaxHP | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.8](SPEC_04_Technical.md) |
| TechPoint | 科技点数 | 升级获得；完整科技树另专题 | [§3.11](SPEC_03_GameRules.md) |
| Material | 材料 | 挖坟入仓库；造战士消耗（与精魂并列） | [§3.10](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md) |
| Warrior | 战士 | 独立实例（ID/名字/血量/属性构成）；非堆叠 | [§3.11](SPEC_03_GameRules.md) |
| WarriorInfo | 战士信息 | 主标签=定稿种族；不改数值 | [§3.11](SPEC_03_GameRules.md) |
| WarriorName | 战士名字 | Prefix(es)+RaceName+ClassName+Suffix | [§3.11](SPEC_03_GameRules.md) |
| ManufactureSlot | 制造槽位 | 头1/躯干1/臂2/腿2/灵魂1/宝石6/坐骑1/翅膀1 | [§3.11](SPEC_03_GameRules.md) |
| BodyPart | 躯体部位 | Head/Torso/Arm/Leg 材料；含 RaceId、SpiritCost | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.12](SPEC_04_Technical.md) |
| BodySlot | 躯体槽类型 | Head / Torso / Arm / Leg | [§3.11](SPEC_03_GameRules.md) |
| Body | 躯体 | 部位集合；换算 BaseStats；部位加权定种族 | [§3.11](SPEC_03_GameRules.md) |
| BaseStats | 基础属性 | HP/移速/力量/敏捷/智力；来自躯体部位 | [§3.11](SPEC_03_GameRules.md) |
| Race | 种族 | 部位权重1加权随机定稿；五维 RaceAdjustCoeff；主标签 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.11](SPEC_04_Technical.md) |
| RaceConfig | 种族配置表 | RaceId → 展示名、五维调整系数 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.11](SPEC_04_Technical.md) |
| RaceAdjustCoeff | 种族属性调整系数 | Base(S)×系数；缺省维=0；可正负；不计控制力 | [§3.11](SPEC_03_GameRules.md) |
| Soul | 灵魂 | 制造必注入；技能/攻击优先级/移动风格/ClassName | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md) |
| SoulConfig | 灵魂配置表 | SoulId → ClassName、Skills、AttackPriority、MoveStyle、SpiritCost、ControlPowerCost | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md) |
| ClassName | 职业名 | 灵魂字段；参与 WarriorName | [§3.11](SPEC_03_GameRules.md) |
| MoveStyle | 移动风格 | 灵魂配置的战士移动行为 | [§3.11](SPEC_03_GameRules.md) |
| ExtraEquipment | 额外装备 | 翅膀/坐骑；制造锁定；属性/技能/NamePrefix | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.13](SPEC_04_Technical.md) |
| ExtraEquipmentConfig | 额外装备配置表 | EquipSlot、NamePrefix、SpiritCost、ControlPowerCost 等 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.13](SPEC_04_Technical.md) |
| NamePrefix | 名字前缀 | 外置装备字段；两件依次拼接 | [§3.11](SPEC_03_GameRules.md) |
| SpiritCost | 精魂消耗 | 材料/灵魂/外置/宝石字段；制造总消耗=求和 | [§3.11](SPEC_03_GameRules.md) |
| Gem | 宝石 | 制造可选；6槽类型互斥；五维 GemMult（多颗Σ）；死亡全部回仓 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.10](SPEC_04_Technical.md) |
| GemType | 宝石类型 | 六类互斥（枚举名 TBD） | [§3.11](SPEC_03_GameRules.md) |
| GemConfig | 宝石配置表 | GemId → GemType、五维 GemMult、Skills、SpiritCost、ControlPowerCost | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.10](SPEC_04_Technical.md) |
| GemMult | 宝石放大系数 | 五维；多颗按维 Σ；无宝石=0；Base(S)×GemMult(S) | [§3.11](SPEC_03_GameRules.md) |
| GemSuffixNameConfig | 宝石后缀命名表 | 宝石组合 → WarriorName 后缀 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.14](SPEC_04_Technical.md) |
| ControlPowerCost | 控制力占用值 | 躯体+灵魂+额外装备+宝石；制造时定稿 | [§3.11](SPEC_03_GameRules.md) |
| SkillBuffCoeff | 技能 Buff 系数 | 仅战斗运行时；FinalStat 公式 | [§3.11](SPEC_03_GameRules.md) |
| ControlPower | 控制力 | 上阵占用；本版上限=等级行 ControlPowerCap；超额失控 | [§3.11](SPEC_03_GameRules.md) |
| LossOfControl | 失控 | 超额分档；不挡开战；战斗中生效 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| BattleFormation | 战斗布阵 | 连续坐标；§3.11 与 Prepare 同一编辑器 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| Defend | 防守 | Prepare→开战→Combat；胜负见专节 | [§3.12](SPEC_03_GameRules.md) |
| DefendPhase | 防守子状态 | Prepare / Combat / Ended | [§3.12](SPEC_03_GameRules.md) |
| StartBattle | 开战 | 准备态按钮；进入 Combat 并部署 | [§3.12](SPEC_03_GameRules.md) |
| BattleMap | 战斗地图 | 连续可走空间；与 DigMap 分离 | [§3.12](SPEC_03_GameRules.md) |
| BattleProtagonist | 战斗主角 | 地图中央；异于 Digger；MaxHP 来自等级表 | [§3.12](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md) |
| Monster | 怪物 | 防守敌方；地图外刷出 | [§3.12](SPEC_03_GameRules.md) |
| Wave | 波次 | 刷怪波次；末波清场为胜利条件之一 | [§3.12](SPEC_03_GameRules.md) |
| AttackPriority | 攻击优先级 | 战士灵魂侧 + 怪物选目标侧（分表） | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| TargetRetargetInterval | 目标修正间隔 | 重算目的地间隔；暂定 1s | [§3.12](SPEC_03_GameRules.md) |
| LevelFailure | 关卡失败 | 与 VictorySettlement 互斥；无本阶段经验/无关卡结算奖励；已获不扣 | [§3.9](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| VictorySettlement | 胜利结算 | 最后一阶段结束后的关卡级结算 | [§3.9](SPEC_03_GameRules.md) |
| Demo acceptance (D-xxx) | Demo 验收项 | D-001～D-004 最小外围壳 | [§3.8](SPEC_03_GameRules.md) |

## 维护规则

- 新增术语时同步一行；正文写在 SPEC
- 禁止在本文件写完整规则或数值表
- ADR 仅记录架构决策（`docs/adr/`）
