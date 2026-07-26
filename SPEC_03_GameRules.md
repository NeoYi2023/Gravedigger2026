# SPEC_03 — 游戏规则 / Game Rules（Gravedigger2026）

**关联文档 / Related:** [SPEC_00_Index.md](SPEC_00_Index.md) · [SPEC_02_GameOverview.md](SPEC_02_GameOverview.md) · [SPEC_04_Technical.md](SPEC_04_Technical.md)

> Demo 验收已扩大为「Meta 壳 + 一条关卡流水线垂直切片」（§3.8）；关卡阶段 / 挖坟 / 升级与制造 / 防守 / 科技树规则见 §3.9–§3.13。Unity 编码须负责人明确授权 Demo 开发。

---

## 3.1 术语与实体

### 简体中文

| 术语 (EN) | 中文 | 定义 |
|-----------|------|------|
| GameplayState | 玩法状态 | 局内主状态枚举：`Dig`（挖坟）、`UpgradeManufacture`（升级与制造；原占位名 `SewRevive`）、`Defend`（防守）。关卡运行时由当前阶段的玩法类型决定（§3.9）；壳层默认占位仍为 Dig。 |
| SaveSlot | 存档槽 | 固定数量的本地存档位；本版 **3 槽**（索引 0–2）。空槽可新建，占用槽可进入或删除。 |
| InSaveShell | 进档壳层 | 选定存档进入后的常驻壳：承载当前 `GameplayState` 占位与浮动「工具」入口。 |
| ToolsPanel | 工具面板 | Demo 调试/设置壳层 UI；由浮动「工具」按钮打开。本期含「设置」「关卡」占位，其余后续补充。 |
| Level | 关卡 | 由「关卡运作表」定义的多阶段流程实体；每阶段指定玩法类型与玩法配置 ID（§3.9；UM 阶段 ConfigId **忽略**）。流水线片由工具「关卡」或等价入口启动样例关卡；场景绑定 **TBD**。 |
| LevelOperation | 关卡运作 | 关卡运作表一行：关卡 ID + 阶段编号 + 玩法类型 + 玩法配置 ID。 |
| DigGameplayConfig | 挖坟配置 | 挖坟配置表一行：时长、开局坟数、过程生成速率、品质权重（零权重项剔除）等（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| Grave | 坟墓 | 挖坟地图上的可生成实体；带坟墓品质 ID；落点须避开已有坟与障碍物。 |
| VictorySettlement | 胜利结算 | 关卡**最后一阶段**结束后触发的关卡级结算反馈。 |
| DigMap | 挖坟地图 | 表现上为旋转 45° 正方形（菱形外观）组成的地图；**逻辑层为整体可放置空间，非格子网格**；表现 Prefab 逻辑名 `Ground_01`…`Ground_05`（`DigMapId`）。 |
| Digger | 挖坟主角 | 挖坟阶段在地图中心点生成的角色；待机 / 挖坟循环动画由场上是否有坟正在被挖驱动（§3.10）；外观为 Character Creator **烘焙整角**，见 [SPEC_04 §15](SPEC_04_Technical.md)。 |
| DigAction | 挖掘流程 | 圆圈光标在坟上停留 ≥0.2s 触发；坟上播放 `DigActionDuration` 挖掘帧动画后结算扣血；该坟挖掘中不可重复触发（§3.10）。 |
| DigObstacle | 挖坟障碍物 | Dig 阶段仅两类：Digger 与未消除 Grave；圆形障碍半径在各自预制体上配置（§3.10）。 |
| DigProtagonistCapabilities | 挖坟主角能力 | 存档主角派生：挖坟伤害、挖坟阶段时长加成、时长缩短和、光标半径、可挖品质集合；由科技树学会写入（§3.10、§3.13）。 |
| GraveHP | 坟墓血量 | 坟墓当前/最大生命；maxHP 来自坟墓品质定义表；扣至 0 触发挖掘成功与奖励（§3.10）。 |
| GraveIconStyle | 坟墓图标样式 | 按剩余 HP% 切换：>65% 样式1；30%–65% 样式2；<30% 样式3（§3.10）。 |
| GraveQualityConfig | 坟墓品质定义表 | 品质 ID → maxHP、掉落等；被挖坟权重引用（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| DigReward | 挖掘奖励 | 坟 HP 归 0 时在成功动画中心生成的奖励图标；飞向主角到达后入账并消失（§3.10）。 |
| DigStageSummary | 挖坟阶段汇总 | Dig 有效时长归零后弹出的汇总弹窗：仅展示本阶段已获奖励按类型汇总，无额外发放（§3.10，UI-011）。 |
| Warehouse | 仓库 | 按存档槽持久的材料仓库；不限格数与存储时长；材料按类型堆叠上限 10000（§3.10）。 |
| SpiritEssence | 精魂 | 货币；挖坟获得（LootDrop 保留 Id + 堆叠超限自动兑换）；制造士兵时消耗（§3.10、§3.11）。 |
| MaterialConfig | 材料配置表 | MaterialId → AutoConvert、AppearanceIconId、AssetPath、WarehouseQualityOutlineId；堆叠超限时按 AutoConvert 兑精魂（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| CurrencyConfig | 货币配置表 | CurrencyId → 外观图/素材路径/仓库品质外轮廓；精魂保留 Id=`Spirit`（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| UpgradeManufacture | 升级与制造 | 阶段玩法类型（原占位 `SewRevive`）：角色升级 + 制造士兵 + 战斗布阵；见 §3.11。 |
| Experience | 经验 | Defend **阶段胜利**结算时加算至 `LifetimeExperience`；关卡失败不入账；达累计阈值升级（§3.11）。 |
| LifetimeExperience | 生涯累计经验 | 存档持有的经验总值；只增不因升级减少；与 `ProtagonistLevelConfig.RequiredTotalExperience` 比较（§3.11）。 |
| ProtagonistLevelConfig | 主角升级配置表 | 等级行：累计经验阈值、预留解锁功能、科技点奖励、控制力上限、`ProtagonistMaxHP`（Defend 开战时作护盾上限）（§3.11，[SPEC_04 §9.8](SPEC_04_Technical.md)）。 |
| TechPoint | 科技点数 | 升级获得；用于科技树学习费用（§3.11、§3.13）。 |
| TechTree | 科技树 | 中心向外扩展的科技项图；前后置由配置正向边定义；见 §3.13。 |
| TechItem | 科技项 | 科技树上一节点；图标+类型框展示；可学会并应用效果（§3.13）。 |
| TechEffect | 科技项效果 | 学会后应用的属性增量与/或功能系统解锁（§3.13，[SPEC_04 §9.17](SPEC_04_Technical.md)）。 |
| TechTreeConfig | 科技树配置表 | TechId → 图标/名/描述/后续 ID/初始解锁/学习费用/UI 框类型（§3.13，[SPEC_04 §9.16](SPEC_04_Technical.md)）。 |
| TechEffectConfig | 科技项效果配置表 | TechId → 属性增量串、解锁功能系统名（§3.13，[SPEC_04 §9.17](SPEC_04_Technical.md)）。 |
| UnlockedFeatureSystems | 已解锁功能系统 | 存档集合；由科技效果 `UnlockedFeatureSystemName` 写入（§3.13）。 |
| Material | 材料 | 挖坟入仓库；造士兵消耗（与精魂并列；配方另专题）（§3.10、§3.11）。 |
| Warrior | 士兵 | 制造产出的 **独立实例**（ID/血量/属性构成等）；防守上阵；中文单位称「士兵」，英文标识仍为 `Warrior`；勿与职业名「战士」混淆（§3.11）。 |
| WarriorInfo | 士兵信息 | 主标签来源为定稿 **种族（Race）**；仅展示/分类，**不**直接改数值（数值调整走 `RaceAdjustCoeff`）（§3.11）。 |
| WarriorName | 士兵名字 | 制造完成时生成：`Prefix(es) + RaceName + ClassName + Suffix`（§3.11）。 |
| ManufactureSlot | 制造槽位 | 制造区严格槽位：头1/躯干1/臂2/腿2/灵魂1/宝石6（类型互斥）/坐骑1/翅膀1（§3.11）。 |
| BodyPart | 躯体部位 | 可拖入头部/躯干/手臂/腿部槽的躯体材料；配置见 `BodyPartConfig`（含 `BodyLevel`、`StatBonus`、`RaceId`、`SpiritCost`、`AutoConvert` 等）（§3.11，[SPEC_04 §9.12](SPEC_04_Technical.md)）。 |
| BodyPartConfig | 躯体材料配置表 | BodyPartId → 等级/部位/种族/控制力/精魂消耗/StatBonus/AutoConvert/介绍/美术素材（§3.11，[SPEC_04 §9.12](SPEC_04_Technical.md)）。 |
| BodySlot | 躯体槽类型 | `Head` / `Torso` / `Arm` / `Leg`（§3.11）。 |
| BodyLevel | 躯体等级 | 躯体材料字段；制造时对已放部位取平均后定外观等级（§3.11）。 |
| StatBonus | 增加的属性值 | 躯体材料平坦属性加成串；`Base(S)=Σ StatBonus(S)`（§3.11）。 |
| Body | 躯体 | 制造所用躯体部位集合；`Base(S)=Σ StatBonus(S)`；各部位 `RaceId` 加权定种族；贡献控制力占用（§3.11）。 |
| BaseStats | 基础属性 | 由已放躯体部位 `StatBonus` 按维求和：生命值、移动速度、力量、敏捷、智力；经 StaticStat/FinalStat 后派生攻/速/CD/血（§3.11、§3.12）。 |
| StaticStat | 静态属性 | 制造/布阵展示用：`max(0, Base+Equip+Base×GemMult+Base×RaceAdjust)`；不含 `SkillBuff`（§3.11）。 |
| PrimaryStat | 主属性 | 职业配置字段：`Strength` / `Agility` / `Intelligence`；决定普攻攻击值所用属性维（§3.11、§3.12，[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| Class | 职业 | 由灵魂 `ClassId` 提供；决定 `ClassName`、`PrimaryStat`，以及对五维→战斗参数的换算系数调整（§3.11、§3.12，[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| ClassId | 职业ID | 职业主键；灵魂必填引用；制造时写入士兵实例（§3.11，[SPEC_04 §9.9](SPEC_04_Technical.md) / [§9.9b](SPEC_04_Technical.md)）。 |
| ClassConfig | 职业配置表 | ClassId → ClassName、PrimaryStat、CombatConvertCoeffs（`键_数值|…`）、AttackRange / 前摇 / 弹速 / 超时（§3.11，[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| BodyLife | 躯体生命 | `Base(MaxHP)+Equip(MaxHP)`；制造锁定；不含宝石/种族/Buff 对生命维放大；代入士兵 `MaxHP` 公式（§3.11）。 |
| NormalAttackPower | 普通攻击值 | `Primary × 1.5`；命中后对怪物直接扣血（本批无护甲）（§3.12）。 |
| AttackSpeed | 攻击速度 | 次/秒：`0.5+60/max(Agi,1)`；攻击开始间隔=`1/AttackSpeed`（§3.12）。 |
| BodyAppearance | 躯体外观 | 预设整体外观造型；制造时按平均躯体等级+定稿种族+职业名选取（§3.11，[SPEC_04 §9.13](SPEC_04_Technical.md)）；资源为 Character Creator **烘焙整角** Prefab，见 [SPEC_04 §15](SPEC_04_Technical.md)。 |
| BodyAppearanceConfig | 躯体外观配置表 | AppearanceId → 外观等级/隶属种族/职业倾向/介绍/保底外形（§3.11，[SPEC_04 §9.13](SPEC_04_Technical.md)）。 |
| IsFallback | 保底外形 | 外观表字段；`1`=该种族保底外观；每种族至多一行（§3.11）。 |
| Race | 种族 | 由已放入躯体部位（头/躯干/臂/腿）各权重 1 **加权随机**定稿；一士兵一族；提供五维 `RaceAdjustCoeff`；配置见 `RaceConfig`（§3.11，[SPEC_04 §9.11](SPEC_04_Technical.md)）。 |
| RaceConfig | 种族配置表 | RaceId → 展示名、五维种族属性调整系数（§3.11，[SPEC_04 §9.11](SPEC_04_Technical.md)）。 |
| RaceAdjustCoeff | 种族属性调整系数 | 五维（对应五项基础属性）；缺省维为 0；可正可负；代入 `BaseStat × RaceAdjustCoeff`；**不**单独计入控制力占用（§3.11）。 |
| Soul | 灵魂 | 制造时必须注入；无灵魂不可成士兵；提供 **职业（ClassId）**、技能（含等级）、**攻击模式（AttackMode）**、攻击优先级、移动风格；**不**改写力量/敏捷/智力本身；配置见 `SoulConfig`（§3.11，[SPEC_04 §9.9](SPEC_04_Technical.md)）。 |
| SoulConfig | 灵魂配置表 | 灵魂行：ClassId、AttackMode、技能列表与等级、攻击优先级、移动风格、SpiritCost、控制力占用等（§3.11，[SPEC_04 §9.9](SPEC_04_Technical.md)）。 |
| AttackMode | 攻击模式 | `Melee` / `Ranged`；士兵取自 `SoulConfig`，怪物取自 `MonsterConfig`；决定普攻走命中方案 D 的近战或远程分支（§3.12）。 |
| ClassName | 职业名 | 职业配置字段（`ClassConfig`）；参与 `WarriorName` 拼接与外观 `ClassAffinity` 匹配；配置值可为「战士」等职业名，**不是**单位称谓「士兵」（§3.11）。 |
| MoveStyle | 移动风格 | 灵魂配置的士兵移动行为风格：`Normal` \| `Aggressive` \| `Cautious`（§3.11）。 |
| ExtraEquipment | 额外装备 | 外置装备（翅膀、坐骑）；制造时选定并 **锁定**；提供额外属性与/或技能、命名前缀、控制力占用（§3.11）。 |
| ExtraEquipmentConfig | 额外装备配置表 | EquipSlot（Mount/Wing）、NamePrefix、属性/技能、SpiritCost、ControlPowerCost 等（§3.11，[SPEC_04 §9.14](SPEC_04_Technical.md)）。 |
| NamePrefix | 名字前缀 | 外置装备配置字段；两件都装备则依次拼入 `WarriorName`（§3.11）。 |
| SpiritCost | 精魂消耗 | 材料/灵魂/外置/宝石配置字段；制造总消耗 = 已放入项之和（§3.11）。 |
| Gem | 宝石 | 制造可选镶嵌；**6 槽、不同类型各 1**；提供 `GemMult` + 额外技能；贡献控制力占用；士兵 **彻底死亡** 后 **全部回仓库**；带宝石士兵 HP≤0 时 **立即** 彻底死亡（§3.11、§3.12，[SPEC_04 §9.10](SPEC_04_Technical.md)）。 |
| GemType | 宝石类型 | 六类互斥：`Ruby` / `Sapphire` / `Emerald` / `Topaz` / `Amethyst` / `Diamond`；同类型不可叠放两颗（§3.11）。 |
| GemConfig | 宝石配置表 | GemId → GemType、五维 GemMult、Skills、SpiritCost、ControlPowerCost（§3.11，[SPEC_04 §9.10](SPEC_04_Technical.md)）。 |
| GemMult | 宝石放大系数 | **五维**（对应五项基础属性）；缺省维为 0；多颗时实例各维 = **Σ** 已镶嵌宝石该维；无宝石五维皆 0；代入 `Base(S) × GemMult(S)`（§3.11）。 |
| GemSuffixNameConfig | 宝石后缀命名表 | 按已镶嵌 `GemType` 排序拼接 `ComboKey`（`A|B|C`）→ 名字后缀（§3.11，[SPEC_04 §9.15](SPEC_04_Technical.md)）。 |
| ControlPowerCost | 控制力占用值 | 单士兵上阵占用；制造完成时 = 躯体 + 灵魂 + 额外装备 + 宝石占用之和（§3.11）。 |
| SkillBuffCoeff | 技能 Buff 系数 | 战斗运行时 Buff 对基础属性的系数；仅进战场最终属性公式使用；制造静态不含（§3.11）。 |
| ControlPower | 控制力 | 主角属性；上阵占用；本版上限取当前等级行 `ControlPowerCap`（科技加成另专题）；超额失控（§3.11）。 |
| LossOfControl | 失控 | 上阵占用超过控制力上限时，按失控程度分档；开战倒计时开始时各士兵独立判定；效果为叛变（§3.11、§3.12）。 |
| LossOfControlDegree | 失控程度 | `Σ上阵 ControlPowerCost / ControlPowerCapEffective − 1`；≤0 为未失控；开战时锁定（§3.11）。 |
| LossOfControlTier | 失控程度段 | 按 Degree 分四段（1 轻度 / 2 中度 / 3 重度 / 4 完全）；查 `LossOfControlConfig`（§3.11，[SPEC_04 §9.20](SPEC_04_Technical.md)）。 |
| LossOfControlConfig | 失控配置表 | TierId 1~4 → 名称、描述、基础失控概率（§3.11，[SPEC_04 §9.20](SPEC_04_Technical.md)）。 |
| Rebel | 叛变 | 失控成功后的士兵状态；就近攻击主角/其他士兵/敌人；持续至该士兵死亡（§3.11、§3.12）。 |
| BattleFormation | 战斗布阵 | 安排士兵上阵；持久化士兵 ID、位置、剩余血量；可在 §3.11 与 Defend `Prepare` 编辑同一套数据（§3.11、§3.12）。 |
| Defend | 防守 | 关卡玩法类型 / `GameplayState`：准备态→开战→战斗；见 §3.12。 |
| DefendPhase | 防守子状态 | 阶段内子状态：`Prepare`（准备）→ `Combat`（战斗中）→ `Ended`（已结束）。 |
| StartBattle | 开战 | 准备态 UI 按钮；点击后进入 `Combat` 并部署单位（§3.12）。 |
| BattleMap | 战斗地图 | 防守阶段地图；逻辑为连续可走空间（非格子）；与 DigMap 阶段分离，表现可共用 `Ground_*`（§3.12）。 |
| BattleProtagonist | 战斗主角 | 战斗中地图中央的主角实体；与挖坟 `Digger` 区分；Defend 中以 **护盾（Shield）** 代替 HP 承受普通攻击（§3.12）；外观为 Character Creator **烘焙整角**，见 [SPEC_04 §15](SPEC_04_Technical.md)。 |
| Shield | 护盾 | Defend 战斗中主角可承受 **普通攻击** 的次数；开战时 `Shield =` 当前等级行 `ProtagonistMaxHP`；归零 → LevelFailure（§3.12）。 |
| Monster | 怪物 | 防守战斗敌方单位；参数见 `MonsterConfig`；出现位置可为地图内或外围（§3.12，[SPEC_04 §9.19](SPEC_04_Technical.md)）；外观为 Character Creator **烘焙整角**（`ModelId` Prefab），见 [SPEC_04 §15](SPEC_04_Technical.md)。 |
| MonsterConfig | 怪物配置表 | 怪物 ID → 模型/名称/目标选择/攻击模式/血量/移速/攻击力/攻速/技能/掉落（§3.12，[SPEC_04 §9.19](SPEC_04_Technical.md)）。 |
| Wave | 波次 | 防守刷怪：由 `WaveSpawnConfig` 在同一 `WaveConfigId` 下的刷怪行集合定义；全部行触发且全灭为阶段胜利条件之一（§3.12）。 |
| WaveSpawnConfig | 刷怪波次配置表 | WaveConfigId + 出怪顺序/剩余秒/怪物/数量/位置/方式（§3.12，[SPEC_04 §9.18](SPEC_04_Technical.md)）。 |
| WaveConfigId | 波次配置ID | 防守玩法配置指向的刷怪表分组键（§3.12，[SPEC_04 §9.7](SPEC_04_Technical.md)）。 |
| RemainingCombatSeconds | 战斗剩余秒 | Defend 开战倒计时剩余整秒；与刷怪行 `SpawnRemainingSeconds` 相等时激活该行（§3.12）。 |
| TargetSelect | 目标选择 | 怪物选目标模式：`Nearest` / `PreferWarrior` / `PreferProtagonist`（§3.12 / `MonsterConfig`）。 |
| AttackPriority | 攻击优先级 | **士兵灵魂**配置字段（§3.11 / `SoulConfig`）；枚举与怪物 `TargetSelect` 对齐：`Nearest` \| `PreferWarrior` \| `PreferProtagonist`；**本批不驱动**选目标（默认见 `EngageZone` 内最近敌人）。怪物侧选目标用 `TargetSelect`（§3.12）。 |
| EngageZone | 选敌区 | BattleMap 预制体上比地图稍小的轴对齐方形；非叛变士兵仅在此区内选最近敌人；区外不可选（§3.12）。 |
| AttackRange | 攻击距离 | 近战/远程均有；须进入目标攻击距离内才开始攻击动作（§3.12）。 |
| CombatDead | 战斗死亡 | 士兵 HP≤0 且无宝石时的战场状态；可被战斗中复活技能拉起；**不**触发物资去向（§3.11、§3.12）。 |
| PermanentDeath | 彻底死亡 | 实例移除 + 布阵位空 + 执行物资去向；结算于阶段胜利 `Ended` / LevelFailure，或带宝石士兵 HP≤0 立即触发（§3.11、§3.12）。 |
| AttackWindup | 攻击前摇 | 近战命中确认前的计时阶段；结束时若目标仍有效且在距内则结算（§3.12）。 |
| HitConfirm | 命中确认 | 规则层确认伤害结算的时刻（近战=前摇结束；远程=弹道命中）（§3.12）。 |
| TargetRetargetInterval | 目标修正间隔 | 怪物与士兵重算可攻击目的地的间隔；暂定 **1s**，可配置（§3.12）。 |
| LevelFailure | 关卡失败 | Defend 中护盾归零等触发的关卡级失败；与 VictorySettlement 互斥（§3.12）。 |

新增术语同步一行到 [CONTEXT.md](CONTEXT.md)。

### English

| Term (EN) | ZH | Definition |
|-----------|-----|------------|
| GameplayState | 玩法状态 | In-session main state enum: `Dig`, `UpgradeManufacture` (was placeholder `SewRevive`), `Defend`. During a Level, set by the current stage's gameplay type (§3.9); shell default placeholder remains Dig. |
| SaveSlot | 存档槽 | Fixed local slots; this version **3 slots** (indices 0–2). Empty → create; occupied → enter or delete. |
| InSaveShell | 进档壳层 | Persistent shell after entering a save: hosts current `GameplayState` placeholder and floating Tools entry. |
| ToolsPanel | 工具面板 | Demo settings/debug shell UI opened by floating Tools. This version: Settings + Level stubs; more later. |
| Level | 关卡 | Multi-stage flow defined by Level Operation table; each stage has gameplay type + config ID (§3.9; UM stage ConfigId **ignored**). Pipeline slice starts sample Level from Tools Level or equiv. entry; scene binding **TBD**. |
| LevelOperation | 关卡运作 | One Level Operation row: LevelId + StageNumber + GameplayType + GameplayConfigId. |
| DigGameplayConfig | 挖坟配置 | One Dig config row: duration, initial grave count, spawn rate, quality weights (zero-weight entries dropped) (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| Grave | 坟墓 | Spawnable Dig-map entity with Grave Quality Id; placement must avoid existing graves and obstacles. |
| VictorySettlement | 胜利结算 | Level-level settlement feedback after the **last** stage ends. |
| DigMap | 挖坟地图 | Visually composed of 45°-rotated squares (diamond look); **logically one continuous placeable space, not a cell grid**; presentation Prefab logical names `Ground_01`…`Ground_05` (`DigMapId`). |
| Digger | 挖坟主角 | Avatar spawned at DigMap center when Dig stage starts; idle vs looping dig anim driven by whether ≥1 grave is being dug (§3.10); visuals are Character Creator **baked whole characters** — [SPEC_04 §15](SPEC_04_Technical.md). |
| DigAction | 挖掘流程 | Circle cursor dwell ≥0.2s on a grave triggers dig; dig frame anim for `DigActionDuration` then damage resolve; busy grave cannot re-trigger (§3.10). |
| DigObstacle | 挖坟障碍物 | Dig-stage obstacles only: Digger and uncleared Graves; circle obstacle radius on each Prefab (§3.10). |
| DigProtagonistCapabilities | 挖坟主角能力 | Save-slot protagonist derived stats: dig damage, Dig stage duration bonus, duration-reduction sum, cursor radius, diggable quality set; written by tech-tree learns (§3.10, §3.13). |
| GraveHP | 坟墓血量 | Current/max HP; maxHP from GraveQualityConfig; 0 HP → dig success + reward (§3.10). |
| GraveIconStyle | 坟墓图标样式 | By remaining HP%: >65% style1; 30%–65% style2; <30% style3 (§3.10). |
| GraveQualityConfig | 坟墓品质定义表 | Quality Id → maxHP, loot, etc.; referenced by Dig spawn weights (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| DigReward | 挖掘奖励 | Reward icon spawned at dig-success anim center when HP hits 0; flies to Digger, credits on arrival, then disappears (§3.10). |
| DigStageSummary | 挖坟阶段汇总 | Popup after Dig effective duration hits 0: aggregate rewards earned this stage by type only; no extra grants (§3.10, UI-011). |
| Warehouse | 仓库 | Per-SaveSlot material warehouse; unlimited slots and retention; materials stack by type up to 10000 (§3.10). |
| SpiritEssence | 精魂 | Currency; from Dig (LootDrop reserved Id + stack overflow AutoConvert); spent when manufacturing soldiers (§3.10, §3.11). |
| MaterialConfig | 材料配置表 | MaterialId → AutoConvert, AppearanceIconId, AssetPath, WarehouseQualityOutlineId; overflow converts to SpiritEssence via AutoConvert (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| CurrencyConfig | 货币配置表 | CurrencyId → appearance icon / asset path / warehouse quality outline; Spirit reserved Id=`Spirit` (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| UpgradeManufacture | 升级与制造 | Stage gameplay type (formerly `SewRevive`): level-up + manufacture soldiers + battle formation; §3.11. |
| Experience | 经验 | Added to `LifetimeExperience` on **Defend stage victory** settlement; not credited on LevelFailure; cumulative threshold → level up (§3.11). |
| LifetimeExperience | 生涯累计经验 | Save-slot total Exp; never decreases on level-up; compared to `ProtagonistLevelConfig.RequiredTotalExperience` (§3.11). |
| ProtagonistLevelConfig | 主角升级配置表 | Level rows: cumulative Exp threshold, reserved unlock features, TechPoint reward, ControlPower cap, `ProtagonistMaxHP` (Defend Shield cap on StartBattle) (§3.11, [SPEC_04 §9.8](SPEC_04_Technical.md)). |
| TechPoint | 科技点数 | Granted on level-up; spent as tech-tree learn cost (§3.11, §3.13). |
| TechTree | 科技树 | Center-out tech graph; prerequisites = inverse of configured forward edges; §3.13. |
| TechItem | 科技项 | One node on the tree; icon + frame type; learnable with effects (§3.13). |
| TechEffect | 科技项效果 | Attribute deltas and/or feature-system unlocks applied on learn (§3.13, [SPEC_04 §9.17](SPEC_04_Technical.md)). |
| TechTreeConfig | 科技树配置表 | TechId → icon/name/desc/next IDs/initial unlock/learn cost/UI frame type (§3.13, [SPEC_04 §9.16](SPEC_04_Technical.md)). |
| TechEffectConfig | 科技项效果配置表 | TechId → attribute-modifier string, unlocked feature system name (§3.13, [SPEC_04 §9.17](SPEC_04_Technical.md)). |
| UnlockedFeatureSystems | 已解锁功能系统 | Save-slot set; written by tech effect `UnlockedFeatureSystemName` (§3.13). |
| Material | 材料 | Credited to Warehouse from Dig; spent to manufacture (alongside SpiritEssence; recipes later) (§3.10, §3.11). |
| Warrior | 士兵 | Manufactured **instance** (Id/HP/attribute composition/…); deployed in Defend; CN unit name「士兵」, EN id remains `Warrior`; do **not** confuse with ClassName profession「战士」(§3.11). |
| WarriorInfo | 士兵信息 | Primary label = finalized **Race**; display/taxonomy only (no numeric effect; numeric adjust uses `RaceAdjustCoeff`) (§3.11). |
| WarriorName | 士兵名字 | Generated at manufacture: `Prefix(es) + RaceName + ClassName + Suffix` (§3.11). |
| ManufactureSlot | 制造槽位 | Strict slots: Head1 / Torso1 / Arm2 / Leg2 / Soul1 / Gem6 (type-exclusive) / Mount1 / Wing1 (§3.11). |
| BodyPart | 躯体部位 | Body materials for Head/Torso/Arm/Leg slots; config `BodyPartConfig` (`BodyLevel`, `StatBonus`, `RaceId`, `SpiritCost`, `AutoConvert`, …) (§3.11, [SPEC_04 §9.12](SPEC_04_Technical.md)). |
| BodyPartConfig | 躯体材料配置表 | BodyPartId → level/slot/race/ControlPower/SpiritCost/StatBonus/AutoConvert/desc/art (§3.11, [SPEC_04 §9.12](SPEC_04_Technical.md)). |
| BodySlot | 躯体槽类型 | `Head` / `Torso` / `Arm` / `Leg` (§3.11). |
| BodyLevel | 躯体等级 | BodyPart field; mean of filled parts drives appearance level (§3.11). |
| StatBonus | 增加的属性值 | BodyPart flat-stat string; `Base(S)=Σ StatBonus(S)` (§3.11). |
| Body | 躯体 | Set of BodyParts at manufacture; `Base(S)=Σ StatBonus(S)`; part `RaceId`s weight-pick Race; contributes ControlPowerCost (§3.11). |
| BaseStats | 基础属性 | Sum of filled BodyPart `StatBonus` per dim: HP, MoveSpeed, Strength, Agility, Intelligence; after StaticStat/FinalStat, derives attack / ASPD / CD / MaxHP (§3.11, §3.12). |
| StaticStat | 静态属性 | Manufacture / formation UI: `max(0, Base+Equip+Base×GemMult+Base×RaceAdjust)`; excludes `SkillBuff` (§3.11). |
| PrimaryStat | 主属性 | Class field: `Strength` / `Agility` / `Intelligence`; selects which dim feeds NormalAttackPower (§3.11, §3.12, [SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| Class | 职业 | From soul `ClassId`; supplies `ClassName`, `PrimaryStat`, and five-dim→combat-param convert coeffs (§3.11, §3.12, [SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| ClassId | 职业ID | Class primary key; required on Soul; written to soldier instance at manufacture (§3.11, [SPEC_04 §9.9](SPEC_04_Technical.md) / [§9.9b](SPEC_04_Technical.md)). |
| ClassConfig | 职业配置表 | ClassId → ClassName, PrimaryStat, CombatConvertCoeffs (`Key_Value|…`), AttackRange / windup / projectile / timeout (§3.11, [SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| BodyLife | 躯体生命 | `Base(MaxHP)+Equip(MaxHP)`; locked at manufacture; no Gem/Race/Buff amplify on HP dim; feeds soldier MaxHP formula (§3.11). |
| NormalAttackPower | 普通攻击值 | `Primary × 1.5`; on hit, subtract from monster HP directly (no armor this batch) (§3.12). |
| AttackSpeed | 攻击速度 | Attacks/sec: `0.5+60/max(Agi,1)`; attack-start interval = `1/AttackSpeed` (§3.12). |
| BodyAppearance | 躯体外观 | Preset overall look; picked by avg BodyLevel + finalized Race + class ClassName (§3.11, [SPEC_04 §9.13](SPEC_04_Technical.md)); assets are Character Creator **baked whole-character** Prefabs — [SPEC_04 §15](SPEC_04_Technical.md). |
| BodyAppearanceConfig | 躯体外观配置表 | AppearanceId → AppearanceLevel / RaceId / ClassAffinity / Description / IsFallback (§3.11, [SPEC_04 §9.13](SPEC_04_Technical.md)). |
| IsFallback | 保底外形 | Appearance field; `1` = race fallback; at most one per RaceId (§3.11). |
| Race | 种族 | Finalized by **weighted random** over filled BodyParts (Head/Torso/Arm/Leg), weight **1** each; one race per soldier; five-dim `RaceAdjustCoeff`; config via `RaceConfig` (§3.11, [SPEC_04 §9.11](SPEC_04_Technical.md)). |
| RaceConfig | 种族配置表 | RaceId → display name, five-dimensional race adjust coeffs (§3.11, [SPEC_04 §9.11](SPEC_04_Technical.md)). |
| RaceAdjustCoeff | 种族属性调整系数 | Five dims (one per BaseStat); missing dim = 0; may be +/-; used as `BaseStat × RaceAdjustCoeff`; does **not** add to ControlPowerCost alone (§3.11). |
| Soul | 灵魂 | Must be injected at manufacture; no soul → cannot create soldier; provides **Class (ClassId)**, skills (+levels), **AttackMode**, AttackPriority, MoveStyle; does **not** rewrite Strength/Agility/Intelligence; config via `SoulConfig` (§3.11, [SPEC_04 §9.9](SPEC_04_Technical.md)). |
| SoulConfig | 灵魂配置表 | Soul rows: ClassId, AttackMode, skills+levels, AttackPriority, MoveStyle, SpiritCost, ControlPowerCost, etc. (§3.11, [SPEC_04 §9.9](SPEC_04_Technical.md)). |
| AttackMode | 攻击模式 | `Melee` / `Ranged`; soldiers from `SoulConfig`, monsters from `MonsterConfig`; selects Melee vs Ranged branch of hit scheme D for normal attacks (§3.12). |
| ClassName | 职业名 | Class config field (`ClassConfig`); used in `WarriorName` and appearance `ClassAffinity` match; may be profession「战士」, **not** the unit name「士兵」(§3.11). |
| MoveStyle | 移动风格 | Soldier movement behavior style from Soul: `Normal` \| `Aggressive` \| `Cautious` (§3.11). |
| ExtraEquipment | 额外装备 | External gear (wings, mount); chosen and **locked** at manufacture; grants extra stats and/or skills, name prefix, ControlPowerCost (§3.11). |
| ExtraEquipmentConfig | 额外装备配置表 | EquipSlot (Mount/Wing), NamePrefix, stats/skills, SpiritCost, ControlPowerCost (§3.11, [SPEC_04 §9.14](SPEC_04_Technical.md)). |
| NamePrefix | 名字前缀 | ExtraEquipment field; if both equipped, concatenate into `WarriorName` in order (§3.11). |
| SpiritCost | 精魂消耗 | Per BodyPart/Soul/Equip/Gem config field; total manufacture cost = sum of filled items (§3.11). |
| Gem | 宝石 | Optional sockets at manufacture; **6 slots, one per GemType**; grants `GemMult` + extra skills; ControlPowerCost; on **PermanentDeath** **all return to Warehouse**; soldiers with gems transition to PermanentDeath **immediately** on HP≤0 (§3.11, §3.12, [SPEC_04 §9.10](SPEC_04_Technical.md)). |
| GemType | 宝石类型 | Six mutually exclusive types: `Ruby` / `Sapphire` / `Emerald` / `Topaz` / `Amethyst` / `Diamond`; at most one gem per type (§3.11). |
| GemConfig | 宝石配置表 | GemId → GemType, five-dim GemMult, Skills, SpiritCost, ControlPowerCost (§3.11, [SPEC_04 §9.10](SPEC_04_Technical.md)). |
| GemMult | 宝石放大系数 | **Five dims** (one per BaseStat); missing dim = 0; multi-gem instance dim = **Σ** of socketed gems for that dim; all zeros if none; used as `Base(S) × GemMult(S)` (§3.11). |
| GemSuffixNameConfig | 宝石后缀命名表 | Socketed `GemType` sorted join `ComboKey` (`A|B|C`) → name suffix (§3.11, [SPEC_04 §9.15](SPEC_04_Technical.md)). |
| ControlPowerCost | 控制力占用值 | Per-soldier deploy cost; finalized at manufacture = Body + Soul + ExtraEquipment + Gem costs (§3.11). |
| SkillBuffCoeff | 技能 Buff 系数 | Runtime combat Buff coefficient on BaseStats; used only in battlefield final-stat formula; excluded from manufacture static snapshot (§3.11). |
| ControlPower | 控制力 | Protagonist attribute; deploy cost; this version cap = current level row `ControlPowerCap` (tech bonus later); overflow → LossOfControl (§3.11). |
| LossOfControl | 失控 | When deployed cost exceeds cap, tier by LossOfControlDegree; each soldier rolls once when combat countdown starts; success → Rebel (§3.11, §3.12). |
| LossOfControlDegree | 失控程度 | `Σ deployed ControlPowerCost / ControlPowerCapEffective − 1`; ≤0 = not out of control; locked at StartBattle (§3.11). |
| LossOfControlTier | 失控程度段 | Four tiers by Degree (1 Mild / 2 Moderate / 3 Severe / 4 Full); lookup `LossOfControlConfig` (§3.11, [SPEC_04 §9.20](SPEC_04_Technical.md)). |
| LossOfControlConfig | 失控配置表 | TierId 1~4 → name, description, base LossOfControl chance (§3.11, [SPEC_04 §9.20](SPEC_04_Technical.md)). |
| Rebel | 叛变 | Soldier state after a successful LossOfControl roll; nearest-target attacks on protagonist / other soldiers / enemies until death (§3.11, §3.12). |
| BattleFormation | 战斗布阵 | Assign soldiers to battlefield; persists soldier Id, position, remaining HP; editable in §3.11 and Defend `Prepare` on the same dataset (§3.11, §3.12). |
| Defend | 防守 | Stage type / `GameplayState`: Prepare → StartBattle → Combat; §3.12. |
| DefendPhase | 防守子状态 | In-stage phases: `Prepare` → `Combat` → `Ended`. |
| StartBattle | 开战 | Prepare-phase UI button; click → `Combat` and deploy units (§3.12). |
| BattleMap | 战斗地图 | Defend-stage map; continuous walkable space (not a grid); stage-separate from DigMap; may share `Ground_*` presentation (§3.12). |
| BattleProtagonist | 战斗主角 | Protagonist entity at BattleMap center; distinct from Dig `Digger`; in Defend uses **Shield** instead of HP for normal attacks (§3.12); visuals are Character Creator **baked whole characters** — [SPEC_04 §15](SPEC_04_Technical.md). |
| Shield | 护盾 | Hit-count capacity for **normal attacks** on the protagonist in Defend; on StartBattle `Shield =` current level row `ProtagonistMaxHP`; `Shield ≤ 0` → LevelFailure (§3.12). |
| Monster | 怪物 | Defend enemy unit; params in `MonsterConfig`; appear location InsideMap or OutsideMap (§3.12, [SPEC_04 §9.19](SPEC_04_Technical.md)); visuals are Character Creator **baked whole characters** (`ModelId` Prefab) — [SPEC_04 §15](SPEC_04_Technical.md). |
| MonsterConfig | 怪物配置表 | MonsterId → model/name/target select/attack mode/HP/move/attack power/speed/skills/loot (§3.12, [SPEC_04 §9.19](SPEC_04_Technical.md)). |
| Wave | 波次 | Defend spawn set: all `WaveSpawnConfig` rows under one `WaveConfigId`; all rows fired + all killed is part of stage victory (§3.12). |
| WaveSpawnConfig | 刷怪波次配置表 | WaveConfigId + spawn order / remaining seconds / monster / count / location / mode (§3.12, [SPEC_04 §9.18](SPEC_04_Technical.md)). |
| WaveConfigId | 波次配置ID | Grouping key for spawn rows referenced by DefendGameplayConfig (§3.12, [SPEC_04 §9.7](SPEC_04_Technical.md)). |
| RemainingCombatSeconds | 战斗剩余秒 | Whole-second Defend combat countdown remaining; activates spawn rows when equal to `SpawnRemainingSeconds` (§3.12). |
| TargetSelect | 目标选择 | Monster targeting mode: `Nearest` / `PreferWarrior` / `PreferProtagonist` (§3.12 / `MonsterConfig`). |
| AttackPriority | 攻击优先级 | **Soldier Soul** field (§3.11 / `SoulConfig`); same enum as monster `TargetSelect`: `Nearest` \| `PreferWarrior` \| `PreferProtagonist`; **does not drive** targeting this batch (default = nearest enemy inside `EngageZone`). Monster targeting uses `TargetSelect` (§3.12). |
| EngageZone | 选敌区 | Axis-aligned square on BattleMap Prefab, slightly smaller than the map; non-Rebel soldiers pick nearest enemy **only inside** this zone; outside = not selectable (§3.12). |
| AttackRange | 攻击距离 | Both Melee and Ranged; must enter target AttackRange before starting attack action (§3.12). |
| CombatDead | 战斗死亡 | Battlefield state when soldier HP≤0 and has no gems; revivable by in-combat revive skills; **does not** trigger material fate (§3.11, §3.12). |
| PermanentDeath | 彻底死亡 | Remove instance + clear formation slot + run material fate; settled on stage victory `Ended` / LevelFailure, or immediately when a gemmed soldier hits HP≤0 (§3.11, §3.12). |
| AttackWindup | 攻击前摇 | Timed phase before melee HitConfirm; on end, settle if target still valid and in range (§3.12). |
| HitConfirm | 命中确认 | Rules-layer moment damage settles (melee = windup end; ranged = projectile hit) (§3.12). |
| TargetRetargetInterval | 目标修正间隔 | Interval for monsters **and soldiers** to recompute attackable destination; provisional **1s**, configurable (§3.12). |
| LevelFailure | 关卡失败 | Level-level failure (e.g. Shield reaches 0 in Defend); mutually exclusive with VictorySettlement (§3.12). |

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
| 5. 关卡 | 规则见 §3.9；流水线片须按 `LevelOperationConfig` 驱动真实阶段（§3.8 D-010）；Meta 片工具「关卡」可仍为占位 |

交叉引用：[SPEC_02 §3](SPEC_02_GameOverview.md)。

### English

| Stage | Description |
|-------|-------------|
| 1. Boot | Open save-select UI (not direct into gameplay) |
| 2. Meta saves | Create / enter / delete on 3 fixed slots (§3.4) |
| 3. InSaveShell | Default `GameplayState = Dig`; show floating Tools (§3.5) |
| 4. Gameplay states | Placeholder must identify current state; in Level, driven by stage gameplay type (§3.9); manual shell switch **TBD** |
| 5. Level | Rules in §3.9; pipeline slice must drive real stages via `LevelOperationConfig` (§3.8 D-010); Meta-slice Tools Level may remain stub |

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
| 本期条目 | **设置**（含科技树画布入口，见 §3.13 / UI-012）、**关卡**（入口） |
| 关卡语义 | 工具「关卡」入口 **不等于** 直接切换三种 `GameplayState`；关卡多阶段规则见 §3.9。**Meta 已锁定 Toast 占位**；流水线片须能启动样例关卡（或等价 Debug 入口，§3.8 D-003 / D-010） |
| 后续条目 | 标 TBD；「设置」「关卡」以外不纳入本版 §3.8 P0 |

点击「设置」：进入设置页并承载科技树画布（§3.13）；其它设置项清单仍 **TBD**；科技树画布完整验收本版可选后置。点击「关卡」：Meta 片空页或 Toast；流水线片启动样例关卡运作。

### English

| Rule | Notes |
|------|-------|
| Visibility | Floating Tools only inside InSaveShell |
| Open / close | Toggle ToolsPanel via button |
| This version | **Settings** (hosts TechTree canvas, §3.13 / UI-012), **Level** (entry) |
| Level meaning | Tools Level entry is **not** a direct three-state switch; multi-stage Level rules in §3.9. **Meta locked to Toast stubs**; pipeline slice must start sample Level (or equiv. Debug entry — §3.8 D-003 / D-010) |
| Future entries | TBD; beyond Settings/Level not §3.8 P0 |

Settings click → Settings page hosting TechTree canvas (§3.13); other settings items still **TBD**; full TechTree canvas acceptance optional this Demo. Level click → Meta: empty/Toast; pipeline: start sample Level Operation.

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
| UI-007 | 设置页 | 已实现（方案 A） | 自工具面板进入；承载科技树画布（UI-012）；其它设置项 TBD |
| UI-008 | 关卡占位页 | 占位 | 自工具面板进入；非玩法三态 |
| UI-009 | 开战按钮 | 已定义（Demo 流水线） | Defend 准备态；点击 → StartBattle（§3.12）；验收见 §3.8 D-040 |
| UI-010 | 升级与制造主屏 | 已定义（Demo 流水线） | 同屏三区（升级/制造/布阵）+ 底部「完成」；细则控件可简陋；验收见 §3.8 D-030～D-032 |
| UI-011 | 挖坟阶段汇总 | 已定义（Demo 流水线） | DigStageSummary：本阶段已获奖励按类型汇总；无额外发放；确认后接 §3.9；验收见 §3.8 D-020 |
| UI-012 | 科技树画布 | 已实现（方案 A，可选） | 2D 可拖动画布；节点图标+类型框；连线；悬停描述；学习点击；见 §3.13；非 §3.8 P0；学会后 Dig 能力可验 |

### English

| ID | Name | Status | Notes |
|----|------|--------|-------|
| UI-001 | Save select | Defined (Demo) | 3 slots: create / enter / delete (confirm) |
| UI-002 | Floating Tools button | Defined (Demo) | InSaveShell |
| UI-003 | ToolsPanel | Defined (Demo) | Settings + Level entry |
| UI-004 | Dig placeholder | Placeholder | Identifiable Dig |
| UI-005 | UpgradeManufacture placeholder | Placeholder | Identifiable UpgradeManufacture (was SewRevive) |
| UI-006 | Defend placeholder | Placeholder | Identifiable Defend; full UI in §3.12 |
| UI-007 | Settings page | Done (Approach A) | From Tools; hosts TechTree canvas (UI-012); other settings TBD |
| UI-008 | Level stub page | Placeholder | From Tools; Meta may Toast; pipeline must start sample Level (§3.8 D-003/D-010) |
| UI-009 | StartBattle button | Defined (Demo pipeline) | Defend Prepare; click → StartBattle (§3.12); accept §3.8 D-040 |
| UI-010 | UpgradeManufacture main screen | Defined (Demo pipeline) | Three panels + bottom Complete; widgets may be rough; accept §3.8 D-030–D-032 |
| UI-011 | Dig stage summary | Defined (Demo pipeline) | DigStageSummary aggregate only; confirm → §3.9; accept §3.8 D-020 |
| UI-012 | TechTree canvas | Done (Approach A, optional) | 2D pannable canvas; §3.13; not §3.8 P0; Dig caps verifiable after learn |

---

## 3.7 玩法状态占位

### 简体中文

进档后须存在可识别的当前状态表现；默认进入 **挖坟（Dig）**。挖坟完整规则见 §3.10；升级与制造框架见 §3.11；防守框架见 §3.12。Meta 壳仅需占位可识别；流水线垂直切片须按 §3.8 对应验收项可玩。

| 状态 | 中文 | Demo 要求 | 范围 / 输入 / 胜负 |
|------|------|-----------|-------------------|
| Dig | 挖坟 | Meta：可识别占位；流水线：§3.10 垂直切片可玩（§3.8 D-020） | 规则见 §3.10（交互 / 扣血 / 奖励 / 无胜负 / DigStageSummary） |
| UpgradeManufacture | 升级与制造 | Meta：可识别占位；流水线：§3.11 垂直切片可玩（§3.8 D-030～D-032） | 框架见 §3.11（原占位名 SewRevive） |
| Defend | 防守 | Meta：可识别占位；流水线：§3.12 垂直切片可玩（§3.8 D-040～D-043） | 框架见 §3.12（准备/开战/护盾/刷怪/寻路/胜负；Demo 最小刷怪点/NavMesh 见本节配套 §3.12） |

壳层内手动切换玩法状态方式 **TBD**（不得将工具「关卡」入口隐式等同为三态手动切换）。关卡运行时由阶段玩法类型驱动，见 §3.9。

### English

After enter, current state must be identifiable; default **Dig**. Dig: §3.10; UpgradeManufacture: §3.11; Defend: §3.12. Meta shell needs identifiable placeholders; pipeline vertical slices must be playable per §3.8.

| State | ZH | Demo requirement | Scope / input / win-lose |
|-------|-----|------------------|---------------------------|
| Dig | 挖坟 | Meta: identifiable placeholder; pipeline: playable §3.10 vertical (§3.8 D-020) | Rules in §3.10 (dig / HP / rewards / no win-lose / DigStageSummary) |
| UpgradeManufacture | 升级与制造 | Meta: identifiable placeholder; pipeline: playable §3.11 vertical (§3.8 D-030–D-032) | Framework in §3.11 (was SewRevive) |
| Defend | 防守 | Meta: identifiable placeholder; pipeline: playable §3.12 vertical (§3.8 D-040–D-043) | Framework in §3.12 (Prepare/StartBattle/Shield/spawn/pathing/win-lose; Demo-min spawn/NavMesh in §3.12) |

Manual shell state switch is **TBD** (must not equate Tools Level entry to a three-state manual switch). During Level, stage gameplay type drives state — §3.9.

---

## 3.8 Demo 验收标准

### 简体中文

**状态：已定义（Meta 壳 + 一条关卡流水线垂直切片）**

实现顺序建议：先 D-001～D-004（Meta 壳），再 D-010（关卡驱动），再 Dig → UpgradeManufacture → Defend（D-020～D-043）。临时美术允许（Prefab 路径须符合 [SPEC_04 §13](SPEC_04_Technical.md) / §15；正式资源后换）。

| ID | 验收项 | 优先级 | 状态 |
|----|--------|--------|------|
| D-001 | 可打开存档界面，对 3 槽执行新建 / 选择进入 / 删除（删除含二次确认） | P0 | Meta 壳已实现（Boot） |
| D-002 | 进入存档后可见浮动「工具」，可打开 / 关闭工具面板 | P0 | Meta 壳已实现 |
| D-003 | 工具面板可见「设置」「关卡」入口；**Meta 已锁定 Toast 占位**；流水线片须能从壳层启动样例关卡（或等价 Debug 入口，实现时锁定并回写） | P0 | 设置仍 Toast；**关卡**→启动样例 `Level_01`（方案 A） |
| D-004 | 进档后可识别当前处于三种玩法状态之一；默认进档为挖坟占位；关卡内由阶段玩法类型驱动 | P0 | Meta 占位+Debug 切态保留；关卡内由 `LevelOperationDriver` 按阶段 `GameplayType` 驱动 |
| D-010 | 运行时只读 `ConfigTables/Csv/`；按 `LevelOperationConfig` 升序驱动至少一条含 Dig → UpgradeManufacture → Defend 的样例关卡；UI/日志可见 LevelId、StageNumber、GameplayType | P0 | 已实现（方案 A；手验：Tools 关卡 + Debug 推进阶段） |
| D-020 | Dig 垂直切片可玩：按 `DigMapId` 实例化 `Assets/Prefabs/Maps/{Id}.prefab`；坟墓可挖可掉落；有效时长归零 → DigStageSummary 确认 → 交还关卡驱动 | P0 | 已实现（方案 A：`DigStageModule` + `DigSessionService`） |
| D-030 | UpgradeManufacture 升级区：可读 `ProtagonistLevelConfig`；注入/入账经验后可连升并看到表字段生效（TechPoints / ControlPowerCap / ProtagonistMaxHP） | P0 | 已实现（方案 A：`UpgradeManufactureStageModule` + `ProtagonistProgressService`；正式入账见 D-043） |
| D-031 | UpgradeManufacture 制造：至少可制造 1 名士兵实例并入池（临时 Prefab 可；技能不施放） | P0 | 已实现（方案 A：`ManufactureService` + `WarriorPoolService`；严格槽位 / 精魂闸门 / 种族与外观定稿 / 命名；临时 `Prefabs/Defend/Warriors/{AppearanceId}`） |
| D-032 | UpgradeManufacture 布阵：连续坐标布阵可写回；与 Defend Prepare 共用同一套 BattleFormation | P0 | 已实现（方案 A：`BattleFormationService` + `FormationPanelView`；按钮上阵/下阵/微调坐标；控制力占用展示；存档级持有供 Prepare） |
| D-040 | Defend Prepare / 开战 / 护盾：加载 `BattleMapId`→`Prefabs/Maps/`；开战须 ≥1 上阵；`Shield` 初值=主角等级行 `ProtagonistMaxHP` | P0 | 已实现（方案 A：`DefendStageModule` + `DefendSessionService`；倒计时可跑；刷怪/战斗见后续片） |
| D-041 | Defend 刷怪与寻路：样例波次能出怪；Demo 最小出生点（临时固定点或地图内随机）；怪物 NavMesh 接近并以普攻扣主角护盾；精确 OutsideMap 几何 **后置** | P0 | 已实现（方案 A：Session 按剩余秒激活 WaveSpawn + Runtime NavMesh + `MonsterAgentView` 扣盾；`Shield≤0`→Ended 钩子） |
| D-042 | Defend 士兵战斗：EngageZone 内普攻可选敌并造成伤害（第一版不施放技能） | P0 | 已实现（方案 A：近战前摇 + 远程 `ProjectileView` 软碰撞命中/超时未命中；清场可检测；胜负入账见 D-043） |
| D-043 | Defend 胜负与结算：清场胜利可入账阶段经验并交还关卡驱动；`Shield ≤ 0` → LevelFailure（可验） | P0 | 已实现（方案 A：开战 Degree/Tier 锁定 + `FinalLossChance` roll→Rebel 就近扣盾；清场 Ended 入账 Demo Exp=100→`TryAdvanceStage`；护盾归零 LevelFailure 不入账并 `AbortLevel`） |

**Demo 范围外（仍排除）：**

- 完整技能施放与技能效果表驱动（士兵/怪物第一版仅普通攻击；`SkillConfig` / CD 公式保留不驱动）
- 正式美术与动画 polish（临时 Prefab / 占位资源允许；禁止运行时引用 `SmallScaleInt/`）
- 完整存档序列化 schema（超出槽占用及流水线所需的最小持久化字段）
- 精确 OutsideMap 出生几何、完整障碍烘焙细则（Demo 最小约定见 §3.12 / [SPEC_04 §9.7](SPEC_04_Technical.md)）
- 科技树节点具体数值/图标 polish 与功能系统名完整枚举（§3.13；画布方案 A 已落地，非本表 P0）
- 工具面板「设置」「关卡」以外的后续功能；完整 polish；未写入本表的需求
- Editor 打表工具实现（[SPEC_04 §14](SPEC_04_Technical.md) 约定已锁；实现另开）

实现边界对照：[SPEC_04 §6](SPEC_04_Technical.md)。

### English

**Status: Defined (Meta shell + one Level-pipeline vertical slice)**

Suggested order: D-001–D-004 (Meta) → D-010 (Level driver) → Dig → UpgradeManufacture → Defend (D-020–D-043). Temp art allowed (Prefab paths must follow [SPEC_04 §13](SPEC_04_Technical.md) / §15; swap formal art later).

| ID | Criterion | Priority | Status |
|----|-----------|----------|--------|
| D-001 | Save UI with 3 slots: create / enter / delete (delete confirms) | P0 | Meta shell done (Boot) |
| D-002 | After enter: floating Tools; open / close ToolsPanel | P0 | Meta shell done |
| D-003 | Tools shows Settings + Level; **Meta locked to Toast stubs**; pipeline must start sample Level from shell (or equiv. Debug entry) | P0 | Settings still Toast; **Level** → starts sample `Level_01` (Approach A) |
| D-004 | Identifiable gameplay state; default Dig placeholder; in-Level driven by stage GameplayType | P0 | Meta placeholders + Debug cycle kept; in-Level driven by `LevelOperationDriver` via stage `GameplayType` |
| D-010 | Runtime reads `ConfigTables/Csv/` only; `LevelOperationConfig` drives at least one sample Level with Dig → UpgradeManufacture → Defend; UI/log shows LevelId, StageNumber, GameplayType | P0 | Done (Approach A; hand-check: Tools Level + Debug advance stage) |
| D-020 | Dig vertical playable: instantiate `Assets/Prefabs/Maps/{DigMapId}.prefab`; dig + loot; duration → DigStageSummary confirm → return to Level driver | P0 | Done (Approach A: `DigStageModule` + `DigSessionService`) |
| D-030 | UM upgrade panel: read `ProtagonistLevelConfig`; inject/credit Exp → multi-level up; TechPoints / ControlPowerCap / ProtagonistMaxHP visible | P0 | Done (Approach A: `UpgradeManufactureStageModule` + `ProtagonistProgressService`; formal credit in D-043) |
| D-031 | UM manufacture: craft ≥1 soldier instance into pool (temp Prefab OK; no skill casts) | P0 | Done (Approach A: `ManufactureService` + `WarriorPoolService`; strict slots / Spirit gate / Race + Appearance finalize / naming; temp `Prefabs/Defend/Warriors/{AppearanceId}`) |
| D-032 | UM formation: continuous-coord formation writable; shared BattleFormation with Defend Prepare | P0 | Done (Approach A: `BattleFormationService` + `FormationPanelView`; button deploy/undeploy/nudge coords; ControlPower usage; save-scoped for Prepare) |
| D-040 | Defend Prepare / StartBattle / Shield: load `BattleMapId`→`Prefabs/Maps/`; StartBattle requires ≥1 deployed; Shield init = level-row `ProtagonistMaxHP` | P0 | Done (Approach A: `DefendStageModule` + `DefendSessionService`; countdown runs; spawn/combat in later slices) |
| D-041 | Defend spawn + path: sample waves spawn; Demo-min spawn (fixed points or in-map random); monsters NavMesh approach and normal-attack Shield; exact OutsideMap geometry **deferred** | P0 | Done (Approach A: Session activates WaveSpawn by remaining seconds + runtime NavMesh + `MonsterAgentView` hits Shield; `Shield≤0`→Ended hook) |
| D-042 | Defend WarriorCombat: EngageZone normal-attack targeting + damage (no skill casts in v1) | P0 | Done (Approach A: melee windup + ranged `ProjectileView` soft-hit/timeout miss; clear detectable; win/lose credit in D-043) |
| D-043 | Defend win/lose: clear-spawn victory credits stage Exp and returns to Level driver; `Shield ≤ 0` → LevelFailure (verifiable) | P0 | Done (Approach A: StartBattle Degree/Tier lock + `FinalLossChance`→Rebel nearest Shield hit; clear Ended credits Demo Exp=100→`TryAdvanceStage`; Shield 0 LevelFailure no Exp + `AbortLevel`) |

**Out of Demo scope (still excluded):**

- Full skill casts / skill-effect table drive (soldiers/monsters: normal attacks only in v1; `SkillConfig` / CD formula retained unused)
- Formal art / animation polish (temp Prefabs OK; **no** runtime refs to `SmallScaleInt/`)
- Full save schema beyond occupied flag + minimal fields needed by the pipeline
- Exact OutsideMap spawn geometry / full obstacle-bake detail (Demo-min in §3.12 / [SPEC_04 §9.7](SPEC_04_Technical.md))
- Full TechTree node values/icon polish & full feature-system enum (§3.13; canvas Approach A landed; not P0 here)
- Tools entries beyond Settings / Level; full polish; anything not in this table
- Editor bake-tool implementation ([SPEC_04 §14](SPEC_04_Technical.md) rules locked; implement separately)

Boundary: [SPEC_04 §6](SPEC_04_Technical.md).

---

## 3.9 关卡阶段流水线

### 简体中文

**状态：已定义（规则库；Demo 流水线垂直切片须实现，见 §3.8 D-010）**

关卡由「关卡运作表」驱动。同一 `关卡ID` 的多行按 `阶段编号` **升序**执行。每阶段以 `玩法类型` 设置当前 `GameplayState`，并以 `玩法配置ID` 加载对应玩法配置（挖坟见 §3.10；升级与制造见 §3.11；防守见 §3.12；配置编码见 [SPEC_04 §9](SPEC_04_Technical.md)）。

**表 1 — 关卡运作表字段（规则语义）**

| 字段 | 说明 |
|------|------|
| 关卡ID | 关卡标识；同 ID 多行组成该关的全部阶段 |
| 阶段编号 | 同关卡内执行顺序（升序） |
| 玩法类型 | 本阶段玩法（如 `Dig` / `UpgradeManufacture` / `Defend`）；映射到 `GameplayState` |
| 玩法配置ID | **Dig** → 查 `DigGameplayConfig` 主键；**Defend** → 查 `DefendGameplayConfig` 主键；**UpgradeManufacture** → **忽略**（可不空；运行时**不**查任何玩法配置表、不解析为 Dig/Defend 行；本阶段读全局表如 `ProtagonistLevelConfig` 等，见 §3.11 / [SPEC_04 §9.1](SPEC_04_Technical.md)）。**本版不另开** `UpgradeManufactureGameplayConfig` |

**阶段流转**

1. 进入关卡：按 `关卡ID` 加载关卡运作行 → 按阶段编号升序排序。
2. 运行当前阶段：应用玩法类型与玩法配置 ID。
3. 阶段结束：由该玩法的结束条件触发。挖坟阶段：有效挖坟时长倒计时归零 → 本阶段结束（§3.10；**无胜负**）。升级与制造阶段：玩家主动确认「完成 / 进入下一阶段」→ 本阶段结束（§3.11；无强制倒计时；**无独立阶段结算**）。防守阶段：见 §3.12（清场胜利 → 阶段结束；护盾归零 → **关卡失败**，不进入下一阶段）。
4. 阶段结算：若该玩法定义了阶段结算则触发（挖坟：**DigStageSummary** 仅汇总本阶段已获奖励、无额外发放，玩家确认后继续；升级与制造 **跳过**；防守阶段胜利时至少含 **经验入账**，其余 **TBD**），再进入下一阶段。
5. **无下一阶段**（已是最后一阶段结束后）：触发关卡级 **胜利结算（VictorySettlement）**。
6. **关卡失败（LevelFailure）**：任意阶段触发关卡失败（如 Defend 中护盾归零）→ **立即结束关卡**；**不**触发 VictorySettlement / **无关卡结算奖励**；**不**入账本阶段 Defend 经验；此前已入账的 Experience、材料/精魂、士兵、TechPoint 等 **不扣除**；失败结算 UI / 字段 **TBD**。

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

**Status: Defined (rules library; Demo pipeline vertical must implement — §3.8 D-010)**

A Level is driven by the Level Operation table. Rows sharing a `LevelId` run in ascending `StageNumber`. Each stage sets `GameplayState` from `GameplayType` and loads config via `GameplayConfigId` (Dig: §3.10; UpgradeManufacture: §3.11; Defend: §3.12; encodings: [SPEC_04 §9](SPEC_04_Technical.md)).

**Table 1 — Level Operation fields (rules semantics)**

| Field | Notes |
|-------|-------|
| LevelId | Level id; multiple rows = all stages |
| StageNumber | Execution order within the Level (ascending) |
| GameplayType | Stage mode (e.g. `Dig` / `UpgradeManufacture` / `Defend`) → `GameplayState` |
| GameplayConfigId | **Dig** → lookup `DigGameplayConfig` PK; **Defend** → lookup `DefendGameplayConfig` PK; **UpgradeManufacture** → **ignore** (may be non-empty; runtime must **not** resolve against any mode config table / Dig/Defend rows; stage reads global tables such as `ProtagonistLevelConfig` — §3.11 / [SPEC_04 §9.1](SPEC_04_Technical.md)). **No** separate `UpgradeManufactureGameplayConfig` this version |

**Stage flow**

1. Enter Level: load rows by LevelId → sort by StageNumber ascending.
2. Run current stage: apply GameplayType + GameplayConfigId.
3. Stage end: per-mode end condition. Dig: effective Dig duration countdown hits 0 → stage ends (§3.10; **no win/lose**). UpgradeManufacture: player confirms "Complete / Next stage" → stage ends (§3.11; no forced countdown; **no independent stage settlement**). Defend: see §3.12 (clear-spawn victory → stage end; Shield reaches 0 → **LevelFailure**, no next stage).
4. Stage settlement: if the mode defines one, run it (Dig: **DigStageSummary** — aggregate rewards earned this stage only, no extra grants, then player confirm; UpgradeManufacture **skips**; Defend victory at least **credits Experience**, other content **TBD**), then advance.
5. **No next stage** (after last stage ends): trigger level-level **VictorySettlement**.
6. **LevelFailure**: any stage that triggers LevelFailure (e.g. Shield reaches 0 in Defend) → **abort the Level immediately**; **no** VictorySettlement / **no level settlement rewards**; **no** Defend stage Exp credit for the failed stage; already-owned Experience, materials/SpiritEssence, soldiers, TechPoints, etc. are **not clawed back**; failure settlement UI/fields **TBD**.

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

**状态：已定义（生成 / 有效时长 / 玩家挖掘交互与奖励入账 / 障碍物几何 / 挖坟四项科技绑定能力 / 无胜负 / DigStageSummary；科技树框架见 §3.13，节点具体数值仍 TBD）**

当关卡当前阶段 `玩法类型 = Dig` 时，使用「挖坟配置表」中对应 `玩法配置ID` 的行。坟墓 `maxHP` 与掉落内容来自「坟墓品质定义表」（[SPEC_04 §9.3](SPEC_04_Technical.md)）。

**地图**

| 规则 | 说明 |
|------|------|
| 表现 | 由旋转 45° 的正方形组成，外观呈菱形拼贴 |
| 表现资产 | 本阶段 `DigGameplayConfig.DigMapId` → Prefab 逻辑名 `Ground_01`…`Ground_05`（与 Defend 的 `BattleMapId` **共用**同一地面变体池）；源参考 Example Scene `Grid`/`Ground (1)`…`Ground (5)`，运行时须用项目 Prefab，见 [SPEC_04 §9.2 / §13 / §15](SPEC_04_Technical.md) |
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
| 挖坟地图ID | Prefab 逻辑名；合法值 `Ground_01`…`Ground_05`（见 [SPEC_04 §9.2](SPEC_04_Technical.md)） |
| 关卡时长限制 | **基础**时长（秒）；实际倒计时用 **有效挖坟时长**（见下） |
| 开局基础生成坟墓数量 | 开局独立加权随机的次数 N |
| 倒计时过程中生成坟墓速率 | 每 N 秒生成 M 个（编码见 [SPEC_04 §9](SPEC_04_Technical.md)） |
| 坟墓出现概率权重 | 各坟墓品质 ID 的出现权重；`Weight = 0` 项剔除（编码与通用规则见 SPEC_04 §9） |

**有效挖坟时长**

| 规则 | 说明 |
|------|------|
| 公式 | `EffectiveDigDuration = DigGameplayConfig.LevelDurationSeconds + DigStageDurationBonus`（秒，加法） |
| 科技来源 | `DigStageDurationBonus` 由 **存档主角** 科技树学会写入 `DigProtagonistCapabilities`；规则见 §3.13；节点具体数值 **TBD** |
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

绑定在 **存档主角** 上，由科技树学会写入（规则与表结构见 [§3.13](#313-科技树techtree) / [SPEC_04 §9.16–§9.17](SPEC_04_Technical.md)；本批只定能力语义与算法）：

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
| 精魂 | 货币；**不**进入材料堆叠；挖坟获得（`LootDrop` 保留 Id 直接掉落 + 堆叠超限自动兑换）；在 **制造士兵** 时消耗（§3.11） |
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

**Status: Defined (spawn / effective duration / dig interaction & reward credit / obstacle geometry / four Dig tech-bound capabilities / no win-lose / DigStageSummary; TechTree framework in §3.13; concrete node values still TBD)**

When the current Level stage has `GameplayType = Dig`, use the DigGameplayConfig row matching `GameplayConfigId`. Grave `maxHP` and loot come from GraveQualityConfig ([SPEC_04 §9.3](SPEC_04_Technical.md)).

**Map**

| Rule | Notes |
|------|-------|
| Presentation | Composed of 45°-rotated squares (diamond look) |
| Visual asset | Stage `DigGameplayConfig.DigMapId` → Prefab logical name `Ground_01`…`Ground_05` (**shared** ground-variant pool with Defend `BattleMapId`); source ref Example Scene `Grid`/`Ground (1)`…`Ground (5)`; runtime must use project Prefabs — [SPEC_04 §9.2 / §13 / §15](SPEC_04_Technical.md) |
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
| DigMapId | Prefab logical name; allowed `Ground_01`…`Ground_05` ([SPEC_04 §9.2](SPEC_04_Technical.md)) |
| Level duration limit | **Base** duration (seconds); actual countdown uses **effective Dig duration** (below) |
| Initial grave count | N independent weighted rolls at start |
| In-countdown spawn rate | Every N seconds spawn M graves (encoding: [SPEC_04 §9](SPEC_04_Technical.md)) |
| Grave spawn weights | Weights per Grave Quality Id; `Weight = 0` entries dropped (encoding + common rules: SPEC_04 §9) |

**Effective Dig duration**

| Rule | Notes |
|------|-------|
| Formula | `EffectiveDigDuration = DigGameplayConfig.LevelDurationSeconds + DigStageDurationBonus` (seconds, additive) |
| Tech source | `DigStageDurationBonus` written into `DigProtagonistCapabilities` by **save-slot protagonist** tech learns; rules in §3.13; concrete node values **TBD** |
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

Bound to the **save-slot protagonist**; written by tech-tree learns (rules & tables: [§3.13](#313-科技树techtree) / [SPEC_04 §9.16–§9.17](SPEC_04_Technical.md); this batch defines capability semantics and formulas only):

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
| SpiritEssence | Currency; **not** stacked as material; from Dig (LootDrop reserved Id + overflow AutoConvert); spent when **manufacturing soldiers** (§3.11) |
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

**状态：框架已关闭（规则库）；升级配置表结构、关卡失败经验边界、士兵属性构成（含宝石五维、种族、按项 FinalStat+下限、StaticStat 分层、职业 ClassId/ClassConfig（含 PrimaryStat、CombatConvertCoeffs 编码、AttackRange 等命中列）、生命维例外 MaxHP=ceil(BodyLife+Str×3)）、士兵制造流程/槽位/命名、躯体材料表与 Base(S)=Σ StatBonus、躯体外观选取（含保底外形）、失控程度/四档/叛变判定与概率公式、士兵死亡分层（CombatDead / PermanentDeath / 宝石特例）已关闭；科技树框架见 §3.13；士兵战斗选敌/攻击距离/命中/普攻·攻速·技能CD 派生见 §3.12；躯体/外观/灵魂·职业·宝石·种族表具体数值 / 失控与技能效果表具体数值行仍 TBD**

当关卡当前阶段 `玩法类型 = UpgradeManufacture` 时进入本阶段。本阶段包含三条并列能力：**升级**、**制造士兵**、**战斗布阵**。配置表载体与字段编码见 [SPEC_04 §9](SPEC_04_Technical.md)（升级表见 **§9.8 `ProtagonistLevelConfig`**；灵魂表见 **§9.9 `SoulConfig`**；职业表见 **§9.9b `ClassConfig`**；宝石表见 **§9.10 `GemConfig`**；种族表见 **§9.11 `RaceConfig`**；躯体材料见 **§9.12 `BodyPartConfig`**；躯体外观见 **§9.13 `BodyAppearanceConfig`**；额外装备 / 宝石后缀见 **§9.14–§9.15**；失控表见 **§9.20 `LossOfControlConfig`**；完整数值仍 **TBD**）。

**界面组织（UI）**

| 规则 | 说明 |
|------|------|
| 布局 | **同一屏三区并列**：升级区 / 制造区 / 布阵区（可同时看见与操作，非 Tab、非线性向导） |
| 完成入口 | 屏幕 **底部** 常驻「完成 / 进入下一阶段」按钮；点击即触发阶段结束（§3.11 阶段结束） |
| 布阵编辑器 | 与 Defend `Prepare` **共用同一套**布阵 UI / 逻辑（写同一 BattleFormation） |
| 区内外细节控件 | 升级区内具体控件 **TBD**；制造区槽位与预览规则见下「制造士兵」 |
| UI 清单 | 见 §3.6 `UI-010` |

**资源依赖**

| 子系统 | 依赖资源 | 来源 |
|--------|----------|------|
| 升级 | 经验（Experience）→ `LifetimeExperience` | **Defend 阶段胜利**结算时统一加算（非击杀即时）；关卡失败不入账 |
| 制造士兵 | 材料（Material）+ 精魂（SpiritEssence） | **挖坟（Dig）** 入仓库 / 精魂；见 §3.10 |
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
| 科技树范围 | 完整规则见 [§3.13](#313-科技树techtree)；挖坟能力绑定见 §3.10 `DigProtagonistCapabilities`；花费 TechPoint 学习科技项 |
| 等级具体数值 | 各行 `RequiredTotalExperience` / 奖励 / 上限数值 **TBD**（1 级行通常 `RequiredTotalExperience = 0`） |

**制造士兵**

| 规则 | 说明 |
|------|------|
| 目的 | 制造 **士兵（Warrior）**，供防守阶段上阵，抵御敌人对主角的进攻 |
| 库存模型 | 每个士兵为 **独立实例**（自有 ID、名字、剩余血量、属性构成快照等）；**非**种类×数量堆叠 |
| 消耗 | 从仓库扣除已放入的 **材料**，并从货币扣除 **精魂**（总消耗见下） |
| 产出 | 可上阵的士兵实例；属性构成见「士兵属性构成」；`Base(S)=Σ` 已放部位 `StatBonus(S)`；外观见「躯体外观定稿」；具体数值行 **TBD** |

**制造步骤（流水线）**

```
材料按槽拖入 → 每次成功拖入/移除后刷新预览（角色信息、属性变更、精魂消耗）
→ 玩家点「制造」（最低材料齐 + 精魂足够）→ 播放制造动画 → 生成士兵实例
```

| 步骤 | 规则 |
|------|------|
| 拖入 | 仅接受对应槽位类型的材料；类型不符 → 拒绝 |
| 预览刷新 | 每次槽位变化后展示：角色信息（含可预览字段）、相对当前方案的属性变更、**当前总精魂消耗**、按同算法试算的 **躯体外观** |
| 制造按钮 | 最低材料要求满足 **且** `SpiritEssence ≥` 总精魂消耗 → 可点；否则 **不可制造**（按钮禁用或点击无效，二选一即可） |
| 动画 | 制造动画为表现层；规则层在确认消耗后提交生成 |
| 完成时 | 扣除材料与精魂；定稿种族与 **躯体外观**；写入属性快照、`AppearanceId` 与 `WarriorName`；实例进入可上阵池 |

**制造槽位（ManufactureSlot；严格类型）**

| 槽组 | 数量 / 约束 |
|------|-------------|
| 头部 | 1（`BodySlot = Head`） |
| 躯干 | 1（`BodySlot = Torso`） |
| 手臂 | 2（不分左右；两槽均为 `Arm`） |
| 腿部 | 2（不分左右；两槽均为 `Leg`） |
| 灵魂 | 1 |
| 宝石 | 6（**不同类型各 1**；`GemType` 互斥：`Ruby` / `Sapphire` / `Emerald` / `Topaz` / `Amethyst` / `Diamond`） |
| 外置装备 | 坐骑 1（`Mount`）+ 翅膀 1（`Wing`） |

**最低制造要求**

必填：**1 躯干 + 2 手臂 + 2 腿 + 1 灵魂**。头部、宝石、坐骑、翅膀均为 **可选**。

**精魂消耗闸门**

| 规则 | 说明 |
|------|------|
| 总消耗 | `TotalSpiritCost = Σ SpiritCost`（已放入的躯体部位、灵魂、外置装备、宝石；缺省项为 0） |
| 字段来源 | 各材料/灵魂/外置/宝石配置表的 `SpiritCost`（[SPEC_04 §9](SPEC_04_Technical.md)）；具体数值 **TBD** |
| 不足 | 材料齐但精魂不够 → **不能制造** |

**种族定稿（加权随机）**

| 规则 | 说明 |
|------|------|
| 参与部位 | 已放入的 **头部、躯干、手臂×2、腿×2**；空槽 **不**参与 |
| 权重 | 每部位权重 **1**（相同权重） |
| 抽取 | 按各部位配置的 `RaceId` 加权随机 → 定稿 `RaceId` |
| 数值 | 定稿后查 `RaceConfig`，将五维 `RaceAdjustCoeff` 写入实例 |
| 标签 | 定稿种族为 **WarriorInfo 主标签来源**；**不再**用「躯体 + 灵魂 InfoTags 拼接」生成主标签 |

**基础属性汇总（BaseStats）**

| 规则 | 说明 |
|------|------|
| 公式 | 对属性项 `S`：`Base(S) = Σ` 已放入躯体部位的 `StatBonus(S)`（缺省维 **0**） |
| 参与部位 | 已放入的全部躯体槽（含可选头部）；空槽不计 |
| 字段来源 | `BodyPartConfig.StatBonus`（编码见 [SPEC_04 §9.12](SPEC_04_Technical.md)） |
| 数值 | 各材料具体 `StatBonus` / `BodyLevel` 行 **TBD** |

**躯体外观定稿**

躯体外观是预设好的 **整体外观造型**。制造定稿（与定稿种族同批；预览用当前槽位按同算法试算）：

| 步骤 | 规则 |
|------|------|
| 1. 平均等级 | 对已放入全部躯体槽的 `BodyLevel` 取算术平均 → **保留 1 位小数** → 再 **四舍五入为整数** `AvgLevelInt`（空槽不计） |
| 2. 等级+种族 | 候选集 A = `BodyAppearanceConfig` 中 `AppearanceLevel == AvgLevelInt` **且** `RaceId ==` 定稿种族 |
| 3. 职业倾向 | 若 A 非空：子集 B = `ClassAffinity` 含 `ClassConfig.ClassName`（经灵魂 `ClassId`）的行；B 非空 → 在 B 中均匀随机；否则在 A 中均匀随机 |
| 4. 保底外形 | 若 A 为空：取同种族 `IsFallback == 1` 的行（每种族至多配置 1 个；常规行为空/`0`） |
| 5. 全表随机 | 若仍无匹配 → 在 **全表** 中均匀随机一行 |
| 写入 | 定稿 `AppearanceId` 写入士兵实例 |

**士兵命名（制造完成时）**

```
WarriorName = Prefix(es) + RaceDisplayName + ClassName + Suffix
```

| 段 | 来源 |
|----|------|
| 前缀 Prefix(es) | 每件已装备外置装备的 `NamePrefix`；两件都有则 **依次拼接**；皆无则可空 |
| 种族名 | 定稿 `RaceId` → `RaceConfig.DisplayNameKey`（或展示名） |
| 职业名 | 所用灵魂 `ClassId` → `ClassConfig.ClassName` |
| 后缀 Suffix | 无宝石可空；有宝石时由 **`GemSuffixNameConfig`** 按已镶嵌 `GemType` 排序拼接 `ComboKey` 解析 |

**士兵属性构成**

士兵属性由下列部件构成：**士兵信息**、**基础属性**、**种族**、**灵魂**、**职业**、**额外装备属性**、**宝石**、**控制力占用值**。进入战场时的最终单项数值另叠加 **技能 Buff 系数**（仅运行时）、**宝石放大**与 **种族调整**。灵魂注入 **职业（ClassId）**；职业定 **ClassName**、**主属性（PrimaryStat）**，以及对五维→战斗参数的 **换算系数调整**（`ClassConfig.CombatConvertCoeffs`；编码与公式见 [SPEC_04 §9.9b](SPEC_04_Technical.md) / §3.12）。三维经 StaticStat / FinalStat 后派生战斗数值（见下与 §3.12）。

| 部件 | 规则 |
|------|------|
| 士兵信息（WarriorInfo） | 主标签 = 定稿 **种族**；仅标签 / 展示 / 分类，**不**直接改变数值。数值调整 **仅** 走「种族」与 `RaceAdjustCoeff` |
| 基础属性（BaseStats） | 由制造所用 **躯体部位** `StatBonus` 按维求和：`Base(S)=Σ StatBonus(S)`（见上）。固定五项：**生命值、移动速度、力量、敏捷、智力**。选敌/攻击距离/命中/死亡见 §3.12；普攻/攻速/技能CD/最终血量派生见下与 §3.12 |
| 种族（Race） | 由躯体部位加权随机定稿（见上）；数据来自 **`RaceConfig`**（[SPEC_04 §9.11](SPEC_04_Technical.md)）。提供 **五维** `RaceAdjustCoeff`（缺省维 **0**；可正可负）。**不**单独计入 `ControlPowerCost` |
| 灵魂（Soul） | 制造时注入；数据来自 **`SoulConfig`**（[SPEC_04 §9.9](SPEC_04_Technical.md)）。功能：必填 **ClassId**（→ 职业）、可使用技能（含技能等级）、**攻击模式 AttackMode**、**攻击优先级**、**移动风格（MoveStyle）**、SpiritCost、ControlPowerCost。`AttackMode ∈ { Melee, Ranged }`（示例语义与职业搭配：战士类→Melee、射手/法师类→Ranged；以配置字段为准）。**不**改写三维属性本身；**第一版 Demo 不施放技能**（见 §3.12） |
| 职业（Class） | 由灵魂 `ClassId` 解析 **`ClassConfig`**（[SPEC_04 §9.9b](SPEC_04_Technical.md)）。提供：`ClassName`（命名与外观 `ClassAffinity`）、`PrimaryStat ∈ { Strength, Agility, Intelligence }`、`CombatConvertCoeffs`（`键_数值|…`；缺键回退全局默认）、以及 `AttackRange` / `MeleeWindupSeconds` / `RangedProjectileSpeed` / `RangedTimeoutSeconds`。示例语义：战士→Strength、射手→Agility、法师→Intelligence（以 `PrimaryStat` 为准，非 ClassName 硬编码） |
| 额外装备属性 | 外置装备提供的同名平坦属性加成与/或额外技能；制造时写入实例并锁定；并提供 `NamePrefix` |
| 宝石（Gem） | 可选；最多 6 颗（类型互斥）；数据来自 **`GemConfig`**（[SPEC_04 §9.10](SPEC_04_Technical.md)）。提供：**五维** `GemMult` + **额外技能**（各宝石技能集合并与灵魂技能 **并存**；冲突/覆盖 **TBD**）。无宝石时五维皆 **0**；多颗时实例各维 `GemMult(S) = Σ` 已镶嵌宝石的 `GemMult(S)` |
| 控制力占用值（ControlPowerCost） | 制造完成时定稿：`ControlPowerCost = BodyCost + SoulCost + EquipCost + GemCost`（无装备/无宝石则对应项为 0；多宝石 `GemCost` 为各宝石占用之和；种族与职业不另加项） |

**静态属性与最终属性（分层）：**

| 层 | 公式 | 用途 |
|----|------|------|
| 静态层 `StaticStat(S)` | `max(0, Base(S)+Equip(S)+Base(S)×GemMult(S)+Base(S)×RaceAdjust(S))` | 制造 / 布阵展示；须标明未含运行时 Buff |
| 战斗层 `FinalStat(S)` | 见下通式（含 `SkillBuff`） | 开战部署与战斗中；Buff 变更时重算 |

公式始终针对 **某一个目标属性项** `S`。汇总时：**先选定 `S`，再取该属性对应的各来源**，不得跨属性混加。

```
FinalStat(S) = max(0,
  Base(S) + Equip(S)
  + Base(S) × SkillBuff(S)
  + Base(S) × GemMult(S)
  + Base(S) × RaceAdjust(S)
)
```

**力量例：**

```
力量最终 = max(0,
  力量基础 + 装备对力量的增加值
  + 力量基础 × 技能对力量的增强系数
  + 力量基础 × 宝石对力量的增强系数
  + 力量基础 × 种族对力量的增强系数
)
```

| 规则 | 说明 |
|------|------|
| 汇总步骤 | ① 选定目标属性 `S` → ② 取 `Base(S)`、`Equip(S)`、`SkillBuff(S)`、`GemMult(S)`、`RaceAdjust(S)` → ③ 代入通式 → ④ `max(0, raw)` |
| `Equip(S)` | 额外装备对该属性的平坦加成；无则 **0** |
| `SkillBuff(S)` | **仅**战斗运行时 Buff 对该属性的系数；制造静态快照 **不含** |
| `GemMult(S)` | 实例五维中对应 `S` 的系数 = **Σ** 已镶嵌各宝石的 `GemMult(S)`；无宝石或该维缺省为 **0**；制造时写入实例 |
| `RaceAdjust(S)` | 定稿种族五维中对应 `S` 的系数；缺省为 **0**；制造时写入实例 |
| 下限保护 | 最终属性 **最小为 0**；算式结果为负时钳制为 0 |
| 继续走 FinalStat 的维 | **力量、敏捷、智力、移动速度**（及生命维的 Base/Equip 取材，但最终血量见下例外） |
| 重算时机 | 开战部署及战斗中 Buff 变更时，对力量/敏捷/智力/移速按当前 `SkillBuff` 重算 `FinalStat`；并重算派生的 MaxHP / 普攻 / 攻速 / 技能 CD（§3.12） |

**士兵最终血量（生命维例外）：**

最终士兵血量 **不再**使用 `FinalStat(MaxHP)`；改用派生公式。`Base(MaxHP)` / `Equip(MaxHP)` 仍由躯体部位与额外装备提供。

```
BodyLife = Base(MaxHP) + Equip(MaxHP)
MaxHP = ceil(BodyLife + Str × 3)
```

| 规则 | 说明 |
|------|------|
| BodyLife | 制造时锁定；**不含** GemMult / RaceAdjust / SkillBuff 对生命维的放大 |
| Str | 静态展示用 `StaticStat(Strength)`；战斗运行时用 `FinalStat(Strength)` |
| SkillBuff(MaxHP) | **本批不读**；Buff 改力量则经 `Str×3` 间接影响血量 |
| RemainingHP 上限 | 开战时算出的 `MaxHP`；若布阵已存 `RemainingHP` 超过新上限 → **钳制**为新上限 |
| 静态展示 MaxHP | `ceil(BodyLife + StaticStat(Strength)×3)` |

**士兵实例静态快照（制造完成时写入；伪结构）：** 见 [SPEC_04 §9.9](SPEC_04_Technical.md) / [§9.10](SPEC_04_Technical.md) / [§9.11](SPEC_04_Technical.md)。

**士兵死亡与材料去向**

| 规则 | 说明 |
|------|------|
| 分层 | **战斗死亡（CombatDead）** ≠ **彻底死亡（PermanentDeath）**；物资与实例清除 **仅** 在彻底死亡时执行（判定细节见 §3.12） |
| 战斗死亡 | 无宝石士兵 `HP ≤ 0` → 进入 `CombatDead`：停用战场行为；**可**被战斗中复活技能拉起（技能专题 TBD）；**不**回仓、**不**毁材料、**不**移除实例 |
| 彻底死亡触发 | ① 本阶段胜利进入 `Ended`，或 **LevelFailure** 结算时：仍为 `CombatDead` 且无「战斗结束复活」类技能 → PermanentDeath；② **宝石特例**：实例 `GemIds` **非空** 时，`HP ≤ 0` → **立即** PermanentDeath（跳过可复活的战斗死亡态） |
| 宝石 | 彻底死亡时：实例 `GemIds` 中全部宝石 → **自动回主角仓库**；**不**随死亡销毁 |
| 其余材料 | 彻底死亡时：躯体部位、灵魂、外置装备，以及制造时绑定到该士兵的其它材料 → **全部销毁**，**不**回仓 |
| 实例与布阵 | 彻底死亡时：该士兵实例从可上阵池移除；BattleFormation 中对应站位 **清空**（无士兵 ID / 视为空位） |

**控制力与失控**

| 规则 | 说明 |
|------|------|
| 占用时机 | 士兵 **上战场时** 占用主角的控制力（制造本身 **不**耗控制力） |
| 单兵占用 | 取该士兵实例的 **`ControlPowerCost`**（制造时已按躯体+灵魂+额外装备+宝石叠加定稿） |
| 上限成长 | 本版：`ControlPowerCapEffective =` 当前等级行 `ControlPowerCap`；科技对上限的加成 **另专题**（生效后为「等级表上限 + 科技加成」） |
| 失控程度 | `LossOfControlDegree = (Σ 当前上阵士兵 ControlPowerCost) / ControlPowerCapEffective − 1` |
| 未失控 | `Degree ≤ 0` → **未失控**：不触发任何负面效果、不进入可失控池、不 roll |
| 可失控 | `Degree > 0` → 全体上阵士兵进入 **可能失控** 状态；再按下方判定各自独立 roll |
| 程度锁定 | **开战瞬间**（进入 `Combat`、倒计时开始时）计算并 **锁定** Degree 与档次；战斗中士兵阵亡 **不**重算 Degree/档次 |
| 四档（TierId） | 轻度 `(0, 0.35]` = 1；中度 `(0.35, 0.7]` = 2；重度 `(0.7, 1]` = 3；完全 `> 1` = 4；查 [SPEC_04 §9.20](SPEC_04_Technical.md) `LossOfControlConfig` |
| 开战判定时机 | 倒计时开始时：每个上阵士兵 **独立判定 1 次**（见下「最终失控率」） |
| 最终失控率 | `FinalLossChance = clamp(0, 1, TierChance + RaceBonus + ΣGemBonus + ΣSkillBonus)`；各来源可正可负；缺省视为 0 |
| 来源拆解 | `TierChance` = 当前锁定档 `LossOfControlConfig.LossOfControlChance`；`RaceBonus` = 定稿种族 `RaceConfig.LossOfControlChanceBonus`；`ΣGemBonus` = 实例已镶嵌各宝石该字段之和；`ΣSkillBonus` = 该士兵全部技能（灵魂/宝石/额外装备技能列表解析）的 `SkillConfig.LossOfControlChanceBonus` 之和 |
| 技能二次判定 | 仅当该士兵 `ΣSkillBonus ≠ 0` 时：每次 **释放技能** 后再用 **同一完整最终失控率** 独立 roll 一次；已叛变则跳过；**第一版 Demo 不施放技能，故不触发本判定** |
| 叛变（Rebel） | roll 成功 → 该士兵进入 **叛变**；持续至 **该士兵死亡**；细则见 §3.12 |
| 与开战关系 | 失控 **不阻止** Defend「开战」；开战门槛仅「上阵士兵 ≥ 1」（§3.12） |
| 布阵预览 | Prepare / 制造布阵区上下阵变更后立即重算 **当前** Degree/档次（供 UI）；真正判定仍以开战锁定值为准 |

**战斗布阵（BattleFormation）**

| 规则 | 说明 |
|------|------|
| 功能 | 安排已制造的士兵进入战场 |
| 持久化字段 | 至少保存：上阵士兵 **ID**、**位置**、**剩余血量** |
| 坐标系 | **BattleMap 连续坐标**（与 §3.12 连续可走空间一致；非格子） |
| 可编辑时机 | **两处**写同一套数据：① 升级与制造布阵区；② 防守 `Prepare` |
| 编辑器复用 | 两处 **同一套**布阵 UI / 逻辑 |
| 准备态可做 | 调整位置、上下阵（从已有士兵实例池选入/撤下）；**不可**在 Prepare 制造新士兵 |
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
       → UnlockedFeatureIds reserved only; TechTree learn/spend → §3.13
  → Manufacture: slots (Head/Torso/Arm×2/Leg×2/Soul/Gem×6 type-exclusive/Mount/Wing); min = Torso+2Arm+2Leg+Soul
       → preview on drag; TotalSpiritCost = Σ SpiritCost; gate on SpiritEssence
       → Race: weight-1 pick from filled BodyParts → RaceConfig; write RaceId + RaceAdjustCoeff (5D)
       → Base(S)=Σ StatBonus(S); AppearanceId via BodyAppearanceConfig (avg BodyLevel→round; class affinity; IsFallback; else table-random)
       → Gem: GemIds[]; GemMult(S)=Σ socketed GemMult(S) (5D; all 0 if none)
       → WarriorName = Prefix(es)+RaceName+ClassName+Suffix; WarriorInfo primary = Race
       → Warrior instance {Id, WarriorName, RemainingHP, RaceId, RaceAdjustCoeff, BaseStats, AppearanceId, SoulId, ClassId, AttackMode, LockedEquipIds, GemIds[], GemMult(5D), ControlPowerCost}
       → BodyPart/Appearance concrete value rows TBD
  → Formation: shared editor; BattleMap continuous coords; persist {WarriorId, Position, RemainingHP}
  → Deploy control: Cap = level-row ControlPowerCap (+ tech later); cost = instance ControlPowerCost; Degree = ΣCost/Cap − 1; tiers 1–4 + Rebel rolls (§3.11 / §3.12 / SPEC_04 §9.20); does not block StartBattle
  → Combat: StaticStat(S)=max(0, Base+Equip+Base×GemMult+Base×RaceAdjust); FinalStat adds Base×SkillBuff
       → MaxHP=ceil(BodyLife+Str×3); BodyLife=Base(MaxHP)+Equip(MaxHP); ClassId from Soul → ClassConfig.PrimaryStat → attack/ASPD/CD (§3.12)
       → RemainingHP clamp to MaxHP on StartBattle
  → On PermanentDeath: all GemIds → Warehouse; BodyParts/Soul/ExtraEquipment/other bound materials destroyed; clear formation slot
  → CombatDead (no gems): no material fate until PermanentDeath (§3.12)
  → Gem exception: GemIds non-empty + HP≤0 → immediate PermanentDeath
  → Player confirms "Complete / Next stage" → no stage settlement → §3.9 next / VictorySettlement
```

### English

**Status: Framework closed (rules library); upgrade table schema, LevelFailure Exp boundary, soldier attribute composition (incl. five-dim Gem, Race; per-stat FinalStat + floor; StaticStat layer; Class ClassId/ClassConfig (incl. PrimaryStat, CombatConvertCoeffs encoding, AttackRange hit columns); HP-dim exception MaxHP=ceil(BodyLife+Str×3)), soldier manufacture flow/slots/naming, BodyPartConfig + Base(S)=Σ StatBonus, BodyAppearance pick (incl. IsFallback), LossOfControlDegree / four tiers / Rebel rolls & chance formula, soldier death layers (CombatDead / PermanentDeath / gem exception) closed; TechTree framework in §3.13; WarriorCombat targeting / AttackRange / hit / NormalAttack·ASPD·SkillCD derives in §3.12; concrete Body/Appearance/Soul/Class/Gem/Race numbers / LossOfControl & skill-effect table concrete rows still TBD**

Entered when Level stage `GameplayType = UpgradeManufacture`. Three parallel capabilities: **Upgrade**, **Manufacture soldiers**, **BattleFormation**. Config encodings: [SPEC_04 §9](SPEC_04_Technical.md) (**§9.8 `ProtagonistLevelConfig`**; **§9.9 `SoulConfig`**; **§9.9b `ClassConfig`**; **§9.10 `GemConfig`**; **§9.11 `RaceConfig`**; **§9.12 `BodyPartConfig`**; **§9.13 `BodyAppearanceConfig`**; ExtraEquipment / gem-suffix **§9.14–§9.15**; **§9.20 `LossOfControlConfig`**; concrete numbers still **TBD**).

**UI layout**

| Rule | Notes |
|------|-------|
| Layout | **One screen, three side-by-side panels**: Upgrade / Manufacture / Formation |
| Complete entry | **Bottom** "Complete / Next stage"; ends stage |
| Formation editor | **Same** UI/logic shared with Defend `Prepare` (same BattleFormation) |
| In-panel controls | Upgrade widgets **TBD**; Manufacture slots & preview below |
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
| Tech tree scope | Full rules in [§3.13](#313-科技树techtree); Dig capability bindings in §3.10 `DigProtagonistCapabilities`; spend TechPoints to learn tech items |
| Concrete numbers | Per-row thresholds / rewards / caps **TBD** (level-1 row usually `RequiredTotalExperience = 0`) |

**Manufacture soldiers**

| Rule | Notes |
|------|-------|
| Purpose | Create **Warrior** instances for Defend |
| Inventory model | Each soldier is an **independent instance** (own Id, name, remaining HP, attribute snapshot, …); **not** stack-by-kind |
| Cost | Deduct filled **materials** from Warehouse and **SpiritEssence** for total Spirit cost |
| Output | Deployable instances; attribute composition below; `Base(S)=Σ` filled `StatBonus(S)`; appearance via「Body appearance finalize」; concrete value rows **TBD** |

**Manufacture pipeline**

```
Drag materials into slots → on each successful add/remove, refresh preview (info, stat delta, Spirit cost)
→ player taps Manufacture (min parts filled + enough Spirit) → manufacture VFX → create soldier instance
```

| Step | Rules |
|------|-------|
| Drag | Accept only matching slot type; reject mismatches |
| Preview | After each slot change: character info, attribute deltas for current plan, **total Spirit cost**, **BodyAppearance** trial-pick with same algorithm |
| Manufacture button | Enabled only if min requirements met **and** `SpiritEssence ≥` total Spirit cost; else **cannot manufacture** (disable or no-op) |
| VFX | Presentation only; rules commit after cost confirmation |
| On complete | Deduct materials + Spirit; finalize Race and **BodyAppearance**; write snapshot, `AppearanceId`, `WarriorName`; add to deployable pool |

**Manufacture slots (strict typing)**

| Group | Count / constraint |
|-------|--------------------|
| Head | 1 (`BodySlot = Head`) |
| Torso | 1 (`BodySlot = Torso`) |
| Arms | 2 (no L/R; both `Arm`) |
| Legs | 2 (no L/R; both `Leg`) |
| Soul | 1 |
| Gems | 6 (**one per GemType**; mutually exclusive: `Ruby` / `Sapphire` / `Emerald` / `Topaz` / `Amethyst` / `Diamond`) |
| ExtraEquipment | Mount 1 + Wing 1 |

**Minimum requirements**

Required: **1 Torso + 2 Arms + 2 Legs + 1 Soul**. Head, gems, mount, wings are **optional**.

**Spirit cost gate**

| Rule | Notes |
|------|-------|
| Total | `TotalSpiritCost = Σ SpiritCost` of filled BodyParts, Soul, ExtraEquipment, Gems (missing = 0) |
| Field source | `SpiritCost` on each config row ([SPEC_04 §9](SPEC_04_Technical.md)); concrete numbers **TBD** |
| Insufficient | Parts OK but Spirit short → **cannot manufacture** |

**Race finalization (weighted pick)**

| Rule | Notes |
|------|-------|
| Participants | Filled **Head, Torso, Arm×2, Leg×2**; empty slots excluded |
| Weight | **1** per part |
| Pick | Weighted random by each part's `RaceId` → finalized `RaceId` |
| Numerics | Lookup `RaceConfig`; copy five-dim `RaceAdjustCoeff` into instance |
| Labels | Finalized Race is **primary WarriorInfo label**; **no** Body+Soul `InfoTags` merge for primary tags |

**BaseStats aggregation**

| Rule | Notes |
|------|-------|
| Formula | Per attribute `S`: `Base(S) = Σ` filled BodyParts' `StatBonus(S)` (missing dim **0**) |
| Participants | All filled body slots (incl. optional Head); empty slots excluded |
| Field source | `BodyPartConfig.StatBonus` (encoding: [SPEC_04 §9.12](SPEC_04_Technical.md)) |
| Numbers | Concrete `StatBonus` / `BodyLevel` rows **TBD** |

**Body appearance finalize**

BodyAppearance is a preset **overall look**. Finalized at manufacture (same batch as Race; preview trials current slots with the same algorithm):

| Step | Rules |
|------|-------|
| 1. Average level | Mean `BodyLevel` over filled body slots → keep **1 decimal** → **round half-up to int** `AvgLevelInt` (empty slots excluded) |
| 2. Level + race | Set A = `BodyAppearanceConfig` rows with `AppearanceLevel == AvgLevelInt` **and** `RaceId ==` finalized race |
| 3. Class affinity | If A non-empty: subset B = rows whose `ClassAffinity` contains `ClassConfig.ClassName` (via soul `ClassId`); if B non-empty → uniform random in B; else uniform random in A |
| 4. Fallback | If A empty: same-race row with `IsFallback == 1` (at most one per race; normal rows empty/`0`) |
| 5. Table random | If still none → uniform random over **entire table** |
| Write | Final `AppearanceId` onto soldier instance |

**Soldier naming (at manufacture complete)**

```
WarriorName = Prefix(es) + RaceDisplayName + ClassName + Suffix
```

| Segment | Source |
|---------|--------|
| Prefix(es) | Each equipped ExtraEquipment `NamePrefix`; concatenate in order if both; empty if none |
| Race name | Finalized `RaceId` → `RaceConfig.DisplayNameKey` (or display name) |
| Class name | Soul `ClassId` → `ClassConfig.ClassName` |
| Suffix | Empty if no gems; else **`GemSuffixNameConfig`** by sorted socketed `GemType` `ComboKey` |

**Soldier attribute composition**

A soldier is composed of: **WarriorInfo**, **BaseStats**, **Race**, **Soul**, **Class**, **ExtraEquipment stats**, **Gem**, and **ControlPowerCost**. Battlefield final per-stat values additionally apply **SkillBuffCoeff** (runtime only), **GemMult**, and **RaceAdjustCoeff**. Soul injects **Class (ClassId)**; Class supplies **ClassName**, **PrimaryStat**, and five-dim→combat-param **convert coeffs** (`ClassConfig.CombatConvertCoeffs`; encoding and formulas in [SPEC_04 §9.9b](SPEC_04_Technical.md) / §3.12). Those dims feed combat derives via StaticStat / FinalStat (below and §3.12).

| Part | Rules |
|------|-------|
| WarriorInfo | Primary label = finalized **Race**; display/taxonomy only (no numeric effect). Numeric adjust uses **Race** / `RaceAdjustCoeff` only |
| BaseStats | Sum of filled BodyPart `StatBonus` per dim: `Base(S)=Σ StatBonus(S)` (above). Fixed five: **HP, MoveSpeed, Strength, Agility, Intelligence**. Targeting / AttackRange / hit / death in §3.12; NormalAttack / ASPD / SkillCD / final MaxHP derives below and in §3.12 |
| Race | Weighted pick from BodyParts (above); data from **`RaceConfig`** ([SPEC_04 §9.11](SPEC_04_Technical.md)). Five-dim `RaceAdjustCoeff` (missing dim = **0**; may be +/-). No separate ControlPowerCost term |
| Soul | Injected at manufacture; **`SoulConfig`** ([SPEC_04 §9.9](SPEC_04_Technical.md)): required **ClassId** (→ Class), skills (+levels), **AttackMode**, **AttackPriority**, **MoveStyle**, SpiritCost, ControlPowerCost. `AttackMode ∈ { Melee, Ranged }` (example pairing with Class: Warrior-like→Melee; Archer/Mage-like→Ranged; config field wins). Does **not** rewrite the three dims; **Demo v1 does not cast skills** (see §3.12) |
| Class | Resolved from soul `ClassId` via **`ClassConfig`** ([SPEC_04 §9.9b](SPEC_04_Technical.md)): `ClassName` (naming + appearance `ClassAffinity`), `PrimaryStat ∈ { Strength, Agility, Intelligence }`, `CombatConvertCoeffs` (`Key_Value|…`; missing key → global defaults), plus `AttackRange` / `MeleeWindupSeconds` / `RangedProjectileSpeed` / `RangedTimeoutSeconds`. Example semantics: Warrior→Strength, Archer→Agility, Mage→Intelligence (`PrimaryStat` wins; not ClassName hardcoding) |
| ExtraEquipment stats | Flat same-named bonuses and/or extra skills; locked at manufacture; also supplies `NamePrefix` |
| Gem | Optional; up to 6 (type-exclusive); **`GemConfig`** ([SPEC_04 §9.10](SPEC_04_Technical.md)): **five-dim** `GemMult` + extra skills (union with Soul skills; conflict **TBD**). No gems → all dims **0**; multi-gem → instance `GemMult(S) = Σ` socketed `GemMult(S)` |
| ControlPowerCost | Finalized at manufacture: `BodyCost + SoulCost + EquipCost + GemCost` (0 for missing; multi-gem GemCost = sum; Race and Class add no term) |

**Static vs final stats (layers):**

| Layer | Formula | Use |
|-------|---------|-----|
| Static `StaticStat(S)` | `max(0, Base(S)+Equip(S)+Base(S)×GemMult(S)+Base(S)×RaceAdjust(S))` | Manufacture / formation UI; note runtime Buffs excluded |
| Combat `FinalStat(S)` | Full formula below (includes `SkillBuff`) | StartBattle deploy and in combat; recalc on Buff change |

The formula always targets **one attribute** `S`. Aggregation: **pick `S` first, then gather sources for that attribute only** — never mix across attributes.

```
FinalStat(S) = max(0,
  Base(S) + Equip(S)
  + Base(S) × SkillBuff(S)
  + Base(S) × GemMult(S)
  + Base(S) × RaceAdjust(S)
)
```

**Strength example:**

```
FinalStrength = max(0,
  BaseStrength + EquipStrengthBonus
  + BaseStrength × SkillBuffStrength
  + BaseStrength × GemMultStrength
  + BaseStrength × RaceAdjustStrength
)
```

| Rule | Notes |
|------|-------|
| Steps | ① Choose target `S` → ② Load `Base(S)`, `Equip(S)`, `SkillBuff(S)`, `GemMult(S)`, `RaceAdjust(S)` → ③ Apply formula → ④ `max(0, raw)` |
| `Equip(S)` | Flat bonus to that attribute from ExtraEquipment; else **0** |
| `SkillBuff(S)` | Runtime combat Buff coeff for that attribute only; excluded from manufacture static snapshot |
| `GemMult(S)` | Instance five-dim coeff for `S` = **Σ** of socketed gems' `GemMult(S)`; **0** if none / missing dim; written at manufacture |
| `RaceAdjust(S)` | Finalized race five-dim coeff for `S`; **0** if missing; written at manufacture |
| Floor | Final attribute **minimum 0**; negative raw results clamp to 0 |
| Dims that keep FinalStat | **Strength, Agility, Intelligence, MoveSpeed** (HP Base/Equip still sourced, but final MaxHP is the exception below) |
| Recalc | On StartBattle deploy and when Buffs change, recompute `FinalStat` for Str/Agi/Int/MoveSpeed; also recompute derived MaxHP / NormalAttack / ASPD / SkillCD (§3.12) |

**Soldier final MaxHP (HP-dim exception):**

Final soldier MaxHP does **not** use `FinalStat(MaxHP)`; use the derived formula. `Base(MaxHP)` / `Equip(MaxHP)` still come from BodyParts and ExtraEquipment.

```
BodyLife = Base(MaxHP) + Equip(MaxHP)
MaxHP = ceil(BodyLife + Str × 3)
```

| Rule | Notes |
|------|-------|
| BodyLife | Locked at manufacture; **excludes** GemMult / RaceAdjust / SkillBuff amplify on the HP dim |
| Str | Static UI uses `StaticStat(Strength)`; combat uses `FinalStat(Strength)` |
| SkillBuff(MaxHP) | **Not read this batch**; Buffs that change Strength affect MaxHP via `Str×3` |
| RemainingHP cap | Combat `MaxHP` at StartBattle; if persisted `RemainingHP` exceeds new cap → **clamp** to new cap |
| Static MaxHP UI | `ceil(BodyLife + StaticStat(Strength)×3)` |

**Soldier instance static snapshot (written at manufacture):** see [SPEC_04 §9.9](SPEC_04_Technical.md) / [§9.10](SPEC_04_Technical.md) / [§9.11](SPEC_04_Technical.md).

**Soldier death & material fate**

| Rule | Notes |
|------|-------|
| Layers | **CombatDead** ≠ **PermanentDeath**; material fate and instance removal run **only** on PermanentDeath (criteria in §3.12) |
| CombatDead | Soldier with **no** gems and `HP ≤ 0` → `CombatDead`: battlefield actions disabled; **may** be revived by in-combat revive skills (skills topic TBD); **no** Warehouse return, **no** material destroy, **no** instance removal |
| PermanentDeath triggers | ① On stage victory enter `Ended`, or on **LevelFailure** settlement: still `CombatDead` and no end-of-battle revive skill → PermanentDeath; ② **Gem exception**: if instance `GemIds` **non-empty**, `HP ≤ 0` → PermanentDeath **immediately** (skip revivable CombatDead) |
| Gem | On PermanentDeath: all gems in `GemIds` → **auto-return to protagonist Warehouse**; **not** destroyed |
| Other materials | On PermanentDeath: BodyParts, Soul, ExtraEquipment, and other materials bound at manufacture → **all destroyed**; **not** returned |
| Instance & formation | On PermanentDeath: remove from deployable pool; clear that BattleFormation slot (empty / no soldier Id) |

**ControlPower & LossOfControl**

| Rule | Notes |
|------|------|
| When cost applies | On **deployment** (manufacture does **not** cost ControlPower) |
| Per-soldier cost | Instance **`ControlPowerCost`** (Body+Soul+ExtraEquipment+Gem sum finalized at manufacture) |
| Cap growth | This version: `ControlPowerCapEffective =` current level row `ControlPowerCap`; tech bonus to cap **later** (then «level-table cap + tech») |
| LossOfControlDegree | `LossOfControlDegree = (Σ deployed ControlPowerCost) / ControlPowerCapEffective − 1` |
| Not out of control | `Degree ≤ 0` → **no** negative effects, no roll pool |
| At risk | `Degree > 0` → all deployed soldiers enter **possible LossOfControl**; each rolls independently below |
| Degree lock | Compute and **lock** Degree + tier at **StartBattle** (Combat countdown start); soldier deaths mid-combat do **not** recalc Degree/tier |
| Four tiers (TierId) | Mild `(0, 0.35]` = 1; Moderate `(0.35, 0.7]` = 2; Severe `(0.7, 1]` = 3; Full `> 1` = 4; lookup [SPEC_04 §9.20](SPEC_04_Technical.md) `LossOfControlConfig` |
| StartBattle roll | When countdown starts: each deployed soldier rolls **once** (see FinalLossChance) |
| FinalLossChance | `FinalLossChance = clamp(0, 1, TierChance + RaceBonus + ΣGemBonus + ΣSkillBonus)`; sources may be +/−; missing = 0 |
| Sources | `TierChance` = locked tier `LossOfControlConfig.LossOfControlChance`; `RaceBonus` = finalized race `RaceConfig.LossOfControlChanceBonus`; `ΣGemBonus` = sum of socketed gems' field; `ΣSkillBonus` = sum of `SkillConfig.LossOfControlChanceBonus` over all skills from Soul/Gem/ExtraEquipment skill lists |
| Extra skill rolls | Only if this soldier's `ΣSkillBonus ≠ 0`: on **each skill cast**, roll again with the **same full FinalLossChance**; skip if already Rebel; **Demo v1 does not cast skills → this roll never fires** |
| Rebel | Successful roll → **Rebel** until **that soldier dies**; combat AI in §3.12 |
| vs StartBattle | Does **not** block StartBattle; only gate is ≥1 soldier (§3.12) |
| Formation preview | After deploy edits, recalc **current** Degree/tier for UI; combat rolls use the StartBattle-locked values |

**BattleFormation**

| Rule | Notes |
|------|-------|
| Function | Assign soldier instances onto the battlefield |
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
       → UnlockedFeatureIds reserved only; TechTree learn/spend → §3.13
  → Manufacture: slots (Head/Torso/Arm×2/Leg×2/Soul/Gem×6 type-exclusive/Mount/Wing); min = Torso+2Arm+2Leg+Soul
       → preview on drag; TotalSpiritCost = Σ SpiritCost; gate on SpiritEssence
       → Race: weight-1 pick from filled BodyParts → RaceConfig; write RaceId + RaceAdjustCoeff (5D)
       → Base(S)=Σ StatBonus(S); AppearanceId via BodyAppearanceConfig (avg BodyLevel→round; class affinity; IsFallback; else table-random)
       → Gem: GemIds[]; GemMult(S)=Σ socketed GemMult(S) (5D; all 0 if none)
       → WarriorName = Prefix(es)+RaceName+ClassName+Suffix; WarriorInfo primary = Race
       → Warrior instance {Id, WarriorName, RemainingHP, RaceId, RaceAdjustCoeff, BaseStats, AppearanceId, SoulId, ClassId, AttackMode, LockedEquipIds, GemIds[], GemMult(5D), ControlPowerCost}
       → BodyPart/Appearance concrete value rows TBD
  → Formation: shared editor; BattleMap continuous coords; persist {WarriorId, Position, RemainingHP}
  → Deploy control: Cap = level-row ControlPowerCap (+ tech later); cost = instance ControlPowerCost; Degree = ΣCost/Cap − 1; tiers 1–4 + Rebel rolls (§3.11 / §3.12 / SPEC_04 §9.20); does not block StartBattle
  → Combat: StaticStat(S)=max(0, Base+Equip+Base×GemMult+Base×RaceAdjust); FinalStat adds Base×SkillBuff
       → MaxHP=ceil(BodyLife+Str×3); BodyLife=Base(MaxHP)+Equip(MaxHP); ClassId from Soul → ClassConfig.PrimaryStat → attack/ASPD/CD (§3.12)
       → RemainingHP clamp to MaxHP on StartBattle
  → On PermanentDeath: all GemIds → Warehouse; BodyParts/Soul/ExtraEquipment/other bound materials destroyed; clear formation slot
  → CombatDead (no gems): no material fate until PermanentDeath (§3.12)
  → Gem exception: GemIds non-empty + HP≤0 → immediate PermanentDeath
  → Player confirms "Complete / Next stage" → no stage settlement → §3.9 next / VictorySettlement
```

---

## 3.12 防守（Defend）

### 简体中文

**状态：框架已定义（准备可改布阵/开战/部署/护盾/倒计时刷怪/寻路/胜负/失控叛变/士兵战斗选敌·AttackMode·攻击距离·命中方案D·死亡分层·普攻攻击值·攻速；Primary 取自 ClassConfig；CombatConvertCoeffs 与 AttackRange 等命中列见 ClassConfig / MonsterConfig；**第一版 Demo：士兵与怪物仅普通攻击、不施放技能**；SkillCooldown 公式与表结构保留但不驱动）；技能效果表、怪物→士兵伤害细节仍 TBD；**出生点 / NavMesh：Demo 最小约定已关闭（见下），精确 OutsideMap 几何后置****

当关卡当前阶段 `玩法类型 = Defend` 时进入本阶段。依赖 §3.11 **战斗布阵（BattleFormation）** 持久化数据。配置表载体见 [SPEC_04 §9.7](SPEC_04_Technical.md) `DefendGameplayConfig`、[§9.18](SPEC_04_Technical.md) `WaveSpawnConfig`、[§9.19](SPEC_04_Technical.md) `MonsterConfig`、[§9.20](SPEC_04_Technical.md) `LossOfControlConfig`。

**阶段内子状态（DefendPhase）**

| 子状态 | 说明 |
|--------|------|
| `Prepare` | 进入 Defend 后的默认态：加载布阵、展示准备 UI（含「开战」）；**可编辑布阵**（与 §3.11 **同一套**布阵 UI/逻辑）；写回同一 BattleFormation；不可制造新士兵 |
| `Combat` | 点击「开战」后：按**当前**布阵部署单位、护盾与战斗倒计时、刷怪、寻路与战斗结算运行中 |
| `Ended` | 本阶段已因胜利结束，或因关卡失败中止 |

**准备态布阵编辑**

| 规则 | 说明 |
|------|------|
| 数据 | 与升级与制造共用 **同一套** BattleFormation 持久化 |
| 编辑器 | 与 §3.11 布阵区 **同一套** UI / 逻辑 |
| 坐标系 | BattleMap **连续坐标**（§3.11 / §3.12） |
| 允许 | 调整上阵士兵 **位置**；从已有士兵 **实例**池 **上阵 / 下阵** |
| 禁止 | 在 `Prepare` **制造**新士兵（制造仅 §3.11） |
| 写回时机 | 每次有效编辑立即写回（或等价于开战前保证已持久化）；开战读的是最新布阵 |
| 控制力 | 编辑后立即重算占用与失控档次（§3.11） |

**开战（StartBattle）**

| 规则 | 说明 |
|------|------|
| 触发 | 仅在 `Prepare`：玩家点击 UI「开战」（UI-009） |
| 效果 | `Prepare` → `Combat`；按下方规则部署单位并开始倒计时与刷怪流程 |
| 无上阵士兵 | **不允许开战**：当前布阵上阵士兵数须 **≥ 1**；否则「开战」按钮 **禁用**，或点击时提示不可开战（二选一实现即可，语义相同） |
| 控制力超额 | **允许开战**；失控不挡开战；开战倒计时开始时锁定 Degree/档次并做叛变判定（§3.11） |

**开战瞬间部署**

| 单位 | 落点 / 状态 |
|------|-------------|
| 战斗主角（BattleProtagonist） | **BattleMap 中央**；与挖坟 `Digger` 为不同阶段实体；初始化 **护盾**（见下） |
| 上阵士兵（Warrior） | 按布阵持久化的 **位置** 生成；**剩余血量** 自布阵读取 |

**失控判定与叛变（LossOfControl / Rebel）**

| 规则 | 说明 |
|------|------|
| 程度与档次 | 进入 `Combat`、倒计时开始瞬间：按 §3.11 计算并 **锁定** `LossOfControlDegree` 与 `LossOfControlTier` |
| 未失控 | `Degree ≤ 0` → 本场无失控负面、不 roll |
| 开战 roll | `Degree > 0` 时，每个上阵士兵按 `FinalLossChance` **独立判定 1 次**（公式见 §3.11） |
| 技能二次 roll | 该士兵 `ΣSkillBonus ≠ 0` 时，每次释放技能再用完整 `FinalLossChance` roll；已叛变跳过；**第一版 Demo 不施放技能，故不触发** |
| 叛变效果 | 成功 → 状态 **Rebel**，持续至该士兵死亡 |
| 叛变选目标 | **就近**：存活主角 + 其他存活士兵（含已叛变）+ 存活敌人；**排除自身** |
| 叛变对主角 | **普通攻击** 命中 → `Shield -= 1`（与怪物普攻破盾相同；不用攻击力字段） |
| 叛变对士兵/怪物 | 走士兵既有攻击结算通道（普攻伤害 = `NormalAttackPower`；见下「战斗派生公式」） |
| 与胜负 | 清场胜利条件 **不变**（刷怪行全触发 + 已刷怪物全灭）；叛变士兵 **不**单独阻挡阶段胜利 |

**护盾（Shield）**

| 规则 | 说明 |
|------|------|
| 语义 | Defend 战斗中主角的「生命」= **护盾**：可承受敌人 **普通攻击** 的次数（非传统 HP 扣减） |
| 初值 | 开战时 `Shield =` 当前主角等级行 `ProtagonistMaxHP`（字段名保留；本阶段语义为护盾上限） |
| 扣减 | 敌人或 **叛变士兵** 的 **普通攻击** 每次命中主角 → `Shield -= 1`；**忽略** 攻击力数值字段 |
| 技能命中 | 怪物技能命中主角是否扣盾 **TBD**（本批仅锁定普通攻击） |
| 失败 | `Shield ≤ 0` → **LevelFailure**（§3.9） |

**战斗倒计时**

| 规则 | 说明 |
|------|------|
| 初值 | 开战时 `RemainingCombatSeconds = DefendGameplayConfig.CombatDurationSeconds`（整秒） |
| 递减 | `Combat` 中按整秒递减 |
| 归零 | **不单独判胜负**；战斗可继续，直至清场胜利或护盾失败 |
| 与刷怪 | 剩余秒等于刷怪行 `SpawnRemainingSeconds` 时激活该行（见下） |

**战斗地图（BattleMap）**

| 规则 | 说明 |
|------|------|
| 逻辑 | **连续可走空间**（非格子网格）；与 DigMap **阶段分离**（不同阶段实例），表现资产可与 Dig **共用** `Ground_01`…`Ground_05` |
| 表现资产 | `DefendGameplayConfig.BattleMapId` → 同 Dig 的地面变体池（合法值 `Ground_01`…`Ground_05`）；解析见 [SPEC_04 §9.7 / §13](SPEC_04_Technical.md) |
| 障碍 | Demo 最小：地图 Prefab 可走面须可烘焙 NavMesh；复杂障碍几何 **后置** |
| EngageZone | 地图 **Prefab** 上挂载比 BattleMap 稍小的 **轴对齐方形选敌区**；位置与尺寸由策划在预制体上调节；规则层只读该区域（见下「士兵战斗」） |

**刷怪（WaveSpawnConfig）**

| 规则 | 说明 |
|------|------|
| 表 | 本阶段 `WaveConfigId` 下全部 `WaveSpawnConfig` 行（[SPEC_04 §9.18](SPEC_04_Technical.md)） |
| 激活条件 | 每当 `RemainingCombatSeconds` 变为某整秒值时，触发所有 `SpawnRemainingSeconds == RemainingCombatSeconds` 且尚未触发的行 |
| 出怪顺序 | `SpawnOrder` **仅**在同一 `SpawnRemainingSeconds` 的多行之间生效：按 **升序** 依次刷出 |
| 数量 | 每行按 `SpawnCount` 生成该行 `MonsterId` 对应怪物 |
| 出现位置 | `AppearLocation`：`InsideMap` / `OutsideMap`。**Demo 最小：** 使用地图 Prefab 上 **临时固定出生点**（SerializeField / 子节点标记即可），或 `InsideMap` **地图内随机**于可走 NavMesh 点；`ClockDirection` 可简化为固定点映射。**精确 OutsideMap 外围几何与钟点方位后置**（正式规则仍保留字段语义） |
| 出怪方式 | `SpawnMode`：`RegionRandom`（区域内随机）或 `ClockDirection`（几点钟方向；须配 `SpawnClockHour` 1–12）；Demo 可按上行最小约定简化 |
| 倒计时已过 | 未匹配到的未来剩余秒不再触发；`SpawnRemainingSeconds = 0` 且开战瞬间尚未处理的行，在剩余秒首次为 0 时触发一次 |

**怪物参数与攻击**

| 规则 | 说明 |
|------|------|
| 参数表 | `MonsterConfig`（[SPEC_04 §9.19](SPEC_04_Technical.md)） |
| 选目标 | 按该怪 `TargetSelect`：`Nearest`（就近）/ `PreferWarrior`（优先士兵）/ `PreferProtagonist`（优先主角） |
| 攻击模式 | `AttackMode`：`Melee` / `Ranged`；怪物侧 `AttackRange` 与命中参数取自 `MonsterConfig`（可复用士兵命中方案 D 语义） |
| 对士兵 | 使用 `AttackPower` **直接扣士兵当前 HP**（本批无护甲/减伤） |
| 对主角 | 普通攻击只扣护盾 1 点（见上）；不用 `AttackPower` |
| 技能 | `Skills` 字段引用技能 ID+CD；技能效果表 **TBD**；**第一版 Demo 不生效**（只打普通攻击；实现时可忽略或配空） |
| 掉落 | 击杀时按 `LootDrop`（编码同 Dig：`Id_Count|…`） |

**目标选择与寻路（怪物）**

| 规则 | 说明 |
|------|------|
| 选目标 | 按 `MonsterConfig.TargetSelect`（见上） |
| 目的地 | 前往能够对该目标施展攻击的坐标（攻击距离 **TBD**） |
| 修正间隔 | 每 **TargetRetargetInterval**（暂定 **1s**，可配置）重选/重算可攻击坐标，并请求 **NavMesh** 重寻路 |
| 技术约定 | 规则层输出目标与目的地；移动由 NavMeshAgent（或等价）执行；规则层不直接驱动 `Transform`。见 [SPEC_04 §9.7](SPEC_04_Technical.md) |
| Demo 最小 NavMesh | 在 `Prefabs/Maps/{BattleMapId}` 可走面上烘焙（或运行时等价）**最小可走 NavMesh**；须覆盖地图内主角/士兵活动区，并允许从 Demo 固定出生点走到可走区。精确外围衔接与障碍细则 **后置** |

**士兵战斗（WarriorCombat）**

| 规则 | 说明 |
|------|------|
| Demo 边界 | **第一版 Demo**：士兵 **仅普通攻击**；不读/不施放灵魂、宝石、额外装备的技能列表；**不**触发「释放技能后失控二次 roll」。`SkillCooldown` / `SkillConfig` / `Skills` 字段 **保留** 供后续扩展，本版 **不驱动** |
| 适用范围 | 非叛变士兵在 `Combat` 中的普攻 / 攻速流程；技能**效果**（含复活）仍 **TBD**（Demo 不施放） |
| EngageZone | 候选敌人 = 存活且 **位置在 EngageZone 内** 的怪物；区外（含仍在 `OutsideMap` 外围、尚未进入选敌区的怪）**不可选** |
| 选目标 | **默认**：EngageZone 内 **距离最近** 的存活敌人；无候选则待机 / 不追区外目标 |
| AttackPriority | `SoulConfig.AttackPriority` **本批不参与**选目标；枚举与 `TargetSelect` 对齐，字段保留 |
| AttackMode | 取自 `SoulConfig.AttackMode`（`Melee` / `Ranged`）；决定普攻走方案 D 的近战或远程分支。配置示例（非 ClassName 硬编码）：战士类→`Melee`+`Strength`；射手类→`Ranged`+`Agility`；法师类→`Ranged`+`Intelligence`（主属性维取自 `ClassConfig.PrimaryStat`） |
| 法师与射手 | **同为远程通道**（进距 → 弹道 → 碰撞命中/超时未命中）；规则层 **唯一差异** 是 `NormalAttackPower` 所用 `PrimaryStat` 维（法师智力 / 射手数敏捷）；不另做法师技能或不同弹道规则；View 特效可区分，**不**改变结算 |
| AttackRange | 近战与远程均有攻击距离（士兵取 `ClassConfig.AttackRange`；怪物取 `MonsterConfig.AttackRange`）；须先移动至目标 `AttackRange` 内，再进入攻击态并播放攻击动作 |
| 重选 / 寻路 | 与怪物共用 `TargetRetargetInterval`：周期性在 EngageZone 内重选最近敌人，并重算可攻击点 + NavMesh 重寻路 |
| 命中方案 D | **近战**（`AttackMode=Melee`）：`AttackWindup` 计时结束 → `HitConfirm`：若目标仍存活且仍在 `AttackRange` 内则结算伤害，否则挥空；**远程**（`AttackMode=Ranged`）：生成弹道，**碰撞命中** 或 **超时未命中** 后再结算 / 判定未命中；规则层确认伤害，View 只播动作与弹道 |
| 普攻伤害 | `HitConfirm`（或远程命中）后：对怪物 `HP -= NormalAttackPower`（本批无护甲）；见下公式 |
| 攻速 | 两次攻击**开始**间隔 = `1 / AttackSpeed`；`AttackWindup` **计入**该周期内（不另加在周期外） |
| 技能 CD | 实际冷却见下式；技能效果正文 **TBD**（含战斗中复活、战斗结束复活）；**第一版 Demo 不施放技能，本式不驱动战斗** |
| 战斗死亡 | 无宝石士兵 `HP ≤ 0` → `CombatDead`（可被战斗中复活技能拉起；**TBD**）；不触发 §3.11 物资去向 |
| 宝石特例 | `GemIds` 非空且 `HP ≤ 0` → **立即** `PermanentDeath`（§3.11 物资去向） |
| 彻底死亡结算 | 本阶段胜利 `Ended` **或** LevelFailure 时：仍为 `CombatDead` 且无「战斗结束复活」类技能 → `PermanentDeath`（实例消失、布阵位空） |
| 叛变 | **Rebel 不受 EngageZone 限制**；选目标仍为就近主角 / 其他士兵 / 敌人（见上）；攻击距离与命中走士兵通道（方案 D，按该兵 `AttackMode`）；对士兵/怪物普攻同用 `NormalAttackPower` |

**战斗派生公式（士兵）：**

令 `Primary` = 士兵 `ClassId` → `ClassConfig.PrimaryStat` 对应维的属性值；`Str` / `Agi` / `Int` 分别为力量 / 敏捷 / 智力。制造/布阵静态展示取 §3.11 `StaticStat`；开战与战斗中取 `FinalStat`；Buff 变更时重算下列派生项。分母用 `max(·, 1)`。

下列全局常量（如 `1.5` / `0.5` / `60` / `30` / `0.1`）为 **缺键回退默认**；正式系数取自 `ClassConfig.CombatConvertCoeffs`（编码见 [SPEC_04 §9.9b](SPEC_04_Technical.md)）。命中参数（`AttackRange` / 前摇 / 弹速 / 超时）取自同表独立列（怪物取 `MonsterConfig` 同名列）。

```
NormalAttackPower = Primary × 1.5

AttackSpeed = 0.5 + 60 / max(Agi, 1)
  // 单位：次/秒；攻击开始间隔 = 1 / AttackSpeed

SkillCooldown = max(0.1, SkillConfig.BaseCooldownSeconds - 30 / max(Int, 1))
  // 单位：秒；SkillConfig 见 SPEC_04 §9.21；第一版 Demo 不施放技能，本式不驱动战斗
```

| 派生项 | 静态展示 | 战斗运行时 |
|--------|----------|------------|
| Primary / Str / Agi / Int | `StaticStat` | `FinalStat` |
| MaxHP | §3.11：`ceil(BodyLife + StaticStat(Strength)×3)` | `ceil(BodyLife + FinalStat(Strength)×3)`；`BodyLife` 不变 |
| NormalAttackPower / AttackSpeed / SkillCooldown | 上式 + 静态属性 | 上式 + 战斗属性（Demo 不展示/不驱动 SkillCooldown 亦可） |

士兵攻击状态机要点：

```
Idle/Move → (target in EngageZone) → Move to AttackRange
  → wait until attack-start interval elapsed (1/AttackSpeed)
  → AttackWindup (within interval)
  → AttackMode=Melee: HitConfirm → monster HP -= NormalAttackPower (if still valid + in range) → Recovery
  → AttackMode=Ranged: spawn projectile → hit: HP -= NormalAttackPower; or timeout miss → Recovery
  // Demo: no skill cast; SkillCooldown / skill effects deferred
  → HP≤0 + no gems → CombatDead（revivable TBD）
  → HP≤0 + has gems → PermanentDeath（immediate）
  → On Ended / LevelFailure: CombatDead without end-battle revive → PermanentDeath
```

**胜负**

| 结果 | 判定 |
|------|------|
| **关卡失败（LevelFailure）** | `Combat` 中 **护盾 Shield ≤ 0** → 立即关卡失败；**不**走 VictorySettlement / **无关卡结算奖励**（§3.9）；结算时对仍 `CombatDead` 的士兵按上表执行彻底死亡（无结束复活则 PermanentDeath） |
| **本阶段胜利** | **同时**满足：① 本 `WaveConfigId` 下 **全部刷怪行均已触发**；② **全部已刷出怪物均已被击杀**（场上无存活敌、无待触发行） |
| 倒计时归零 | **不**单独构成胜或负 |
| 阶段胜利之后 | `Combat` → `Ended` → 结算彻底死亡（见士兵战斗）→ **统一入账本阶段经验（Experience）**（§3.11；**Demo 固定 +100** `LifetimeExperience`，正式表字段后置）→ §3.9 阶段结算（其余内容 **TBD**）→ 下一阶段 /（若末阶段）VictorySettlement |
| 关卡失败与经验 | LevelFailure **不**入账本阶段经验；此前已入账的 Experience 与其它已获资源 **不扣除** |
| 叛变与胜利 | 存活的叛变士兵 **不**单独阻挡阶段胜利（胜利仍只看刷怪行与怪物） |

```
Defend stage
  → DefendPhase = Prepare
  → Load BattleFormation {WarriorId, Position, RemainingHP}
  → Player may edit formation (positions / deploy / undeploy) → write back same BattleFormation
  → StartBattle requires deployed soldiers ≥ 1 (else button disabled / hint)
  → Player clicks StartBattle
  → DefendPhase = Combat
  → Spawn BattleProtagonist at BattleMap center
  → Shield = ProtagonistMaxHP (current level row)
  → RemainingCombatSeconds = CombatDurationSeconds
  → Deploy soldiers at **current** formation positions; MaxHP=ceil(BodyLife+FinalStat(Str)×3); RemainingHP clamp to MaxHP
  → Lock LossOfControlDegree = ΣCost / Cap − 1 and Tier; if Degree > 0, each soldier rolls FinalLossChance once (§3.11)
  → Each whole second (and on StartBattle for matching Remaining):
       fire WaveSpawnConfig rows where SpawnRemainingSeconds == RemainingCombatSeconds
       same-second rows ordered by SpawnOrder ascending
  → Each Monster:
       select target by TargetSelect
       set NavMesh destination = attackable position
       every TargetRetargetInterval (default 1s): recompute destination / repath
       normal hit on protagonist → Shield -= 1 (ignore AttackPower)
       hit on soldier → soldier HP -= AttackPower (no armor this batch)
       // Demo: MonsterConfig.Skills unused — normal attacks only
  → Each non-Rebel soldier (WarriorCombat):
       candidates = living monsters inside EngageZone (outside not selectable)
       target = nearest candidate (AttackPriority unused this batch)
       AttackMode from SoulConfig; Primary=FinalStat(ClassConfig.PrimaryStat via ClassId); NormalAttackPower=Primary×1.5
       AttackSpeed=0.5+60/max(Agi,1); interval=1/AttackSpeed; windup within interval
       // Demo: no skill cast; SkillCooldown formula retained but unused
       move into AttackRange → AttackWindup
       Melee: HitConfirm → monster HP -= NormalAttackPower; Ranged: projectile hit same / timeout miss
       HP≤0 + no gems → CombatDead; HP≤0 + gems → immediate PermanentDeath (§3.11)
       every TargetRetargetInterval: reselect nearest in EngageZone / repath
  → // Demo: no skill-cast LossOfControl re-roll (skills not cast)
  → Each Rebel soldier:
       nearest target among living protagonist / other soldiers / enemies (exclude self; **no EngageZone limit**)
       normal hit on protagonist → Shield -= 1
       hit on soldier/monster → soldier channel (scheme D; NormalAttackPower)
  → If Shield ≤ 0 → LevelFailure → settle PermanentDeath for remaining CombatDead (no end-battle revive)
       → no stage Exp / no VictorySettlement; keep already-owned; abort Level (§3.9)
  → If all WaveSpawnConfig rows for WaveConfigId fired AND all spawned monsters killed
       → DefendPhase = Ended → settle PermanentDeath for remaining CombatDead (no end-battle revive)
       → credit Experience → §3.9 settlement / next / VictorySettlement
       (living Rebels do not alone block stage victory)
  → RemainingCombatSeconds == 0 does NOT alone end Combat
```

### English

**Status: Framework defined (Prepare / StartBattle / deploy / Shield / countdown spawn / pathing / win-lose / LossOfControl Rebel / WarriorCombat EngageZone·AttackMode·AttackRange·hit scheme D·death layers·NormalAttackPower·AttackSpeed; Primary from ClassConfig; CombatConvertCoeffs and AttackRange hit columns on ClassConfig / MonsterConfig; **Demo v1: soldiers and monsters use normal attacks only — no skill casts**; SkillCooldown formula/schema retained but unused); skill-effect table, monster→soldier damage edge cases still TBD; **spawn / NavMesh Demo-min closed below; exact OutsideMap geometry deferred****

Entered when Level stage `GameplayType = Defend`. Depends on §3.11 **BattleFormation** persistence. Config: [SPEC_04 §9.7](SPEC_04_Technical.md) `DefendGameplayConfig`, [§9.18](SPEC_04_Technical.md) `WaveSpawnConfig`, [§9.19](SPEC_04_Technical.md) `MonsterConfig`, [§9.20](SPEC_04_Technical.md) `LossOfControlConfig`.

**In-stage phases (DefendPhase)**

| Phase | Notes |
|-------|-------|
| `Prepare` | Default on enter: load formation, show prepare UI (incl. StartBattle); **may edit** formation with the **same** UI/logic as §3.11; write back same BattleFormation; cannot manufacture |
| `Combat` | After StartBattle: deploy from **current** formation, Shield + combat countdown, spawn, pathing, combat resolution |
| `Ended` | Stage ended by victory, or aborted by LevelFailure |

**Prepare formation editing**

| Rule | Notes |
|------|-------|
| Data | Shared **same** BattleFormation persistence as UpgradeManufacture |
| Editor | **Same** UI/logic as §3.11 formation panel |
| Coordinates | BattleMap **continuous** space (§3.11 / §3.12) |
| Allowed | Change soldier **positions**; **deploy / undeploy** from existing soldier **instance** pool |
| Forbidden | **Manufacture** new soldiers in `Prepare` (manufacture only in §3.11) |
| Write-back | Persist on each valid edit (or guarantee persisted before StartBattle); StartBattle uses latest |
| ControlPower | Recalculate cost / LossOfControl tier after edits (§3.11) |

**StartBattle**

| Rule | Notes |
|------|-------|
| Trigger | Only in `Prepare`: player clicks UI StartBattle (UI-009) |
| Effect | `Prepare` → `Combat`; deploy units and start countdown + spawn flow |
| Empty formation | **StartBattle forbidden**: deployed soldier count must be **≥ 1**; otherwise StartBattle is **disabled**, or click shows a cannot-start hint (either UX is fine; same rule) |
| Over ControlPower | **StartBattle allowed**; LossOfControl does not block; Degree/tier locked and Rebel rolls fire when countdown starts (§3.11) |

**Deploy on StartBattle**

| Unit | Placement / state |
|------|-------------------|
| BattleProtagonist | **BattleMap center**; distinct from Dig `Digger`; init **Shield** (below) |
| Soldiers (Warrior) | Spawn at persisted formation **positions**; **remaining HP** from formation |

**LossOfControl & Rebel**

| Rule | Notes |
|------|-------|
| Degree & tier | On enter `Combat` / countdown start: compute and **lock** `LossOfControlDegree` + `LossOfControlTier` (§3.11) |
| Not out of control | `Degree ≤ 0` → no LossOfControl negatives, no rolls |
| StartBattle roll | If `Degree > 0`, each deployed soldier rolls `FinalLossChance` **once** (§3.11) |
| Extra skill rolls | If soldier `ΣSkillBonus ≠ 0`, on **each skill cast** roll again with full `FinalLossChance`; skip if already Rebel; **Demo v1 does not cast skills → never fires** |
| Rebel effect | Success → **Rebel** until that soldier dies |
| Rebel targeting | **Nearest** among living protagonist + other living soldiers (incl. Rebels) + living enemies; **exclude self** |
| Rebel vs protagonist | **Normal attack** hit → `Shield -= 1` (same as monster normal hit) |
| Rebel vs soldier/monster | Existing soldier attack channel (normal damage = `NormalAttackPower`; see combat derives below) |
| vs victory | Stage victory conditions **unchanged**; living Rebels do **not** alone block stage victory |

**Shield**

| Rule | Notes |
|------|-------|
| Meaning | Protagonist “life” in Defend = **Shield**: count of **normal attacks** that can be absorbed (not traditional HP damage) |
| Init | On StartBattle `Shield =` current level row `ProtagonistMaxHP` (field name kept; Defend semantics = Shield cap) |
| Decrement | Each enemy or **Rebel** **normal attack** hit on protagonist → `Shield -= 1`; **ignore** attack-power fields |
| Skill hits | Whether monster skills reduce Shield **TBD** (this batch locks normal attacks only) |
| Failure | `Shield ≤ 0` → **LevelFailure** (§3.9) |

**Combat countdown**

| Rule | Notes |
|------|-------|
| Init | On StartBattle `RemainingCombatSeconds = DefendGameplayConfig.CombatDurationSeconds` (whole seconds) |
| Tick | Decrements by whole seconds during `Combat` |
| Hits zero | **Does not alone decide win/lose**; combat may continue until clear victory or Shield failure |
| Spawn link | When remaining seconds equal a row’s `SpawnRemainingSeconds`, that row activates (below) |

**BattleMap**

| Rule | Notes |
|------|-------|
| Logic | **Continuous walkable space** (not a cell grid); **stage-separate** from DigMap (different stage instances); presentation assets **may share** Dig’s `Ground_01`…`Ground_05` pool |
| Visual asset | `DefendGameplayConfig.BattleMapId` → same ground-variant pool as Dig (allowed `Ground_01`…`Ground_05`); resolve via [SPEC_04 §9.7 / §13](SPEC_04_Technical.md) |
| Obstacles | Demo-min: map Prefab walkable surface must bake NavMesh; complex obstacle geometry **deferred** |
| EngageZone | Axis-aligned square **slightly smaller** than BattleMap, authored on the map **Prefab**; position/size tuned by designers; rules layer reads the zone (see WarriorCombat below) |

**Spawn (WaveSpawnConfig)**

| Rule | Notes |
|------|-------|
| Table | All `WaveSpawnConfig` rows under this stage’s `WaveConfigId` ([SPEC_04 §9.18](SPEC_04_Technical.md)) |
| Activate | When `RemainingCombatSeconds` becomes a whole-second value, fire all not-yet-fired rows with `SpawnRemainingSeconds == RemainingCombatSeconds` |
| SpawnOrder | `SpawnOrder` applies **only** among rows sharing the same `SpawnRemainingSeconds`: ascending order |
| Count | Each row spawns `SpawnCount` instances of `MonsterId` |
| AppearLocation | `InsideMap` or `OutsideMap`. **Demo-min:** temp **fixed spawn points** on map Prefab (SerializeField / child markers OK), or `InsideMap` **random on walkable NavMesh**; `ClockDirection` may map to fixed points. **Exact OutsideMap perimeter geometry and clock bearings deferred** (formal field semantics retained) |
| SpawnMode | `RegionRandom` or `ClockDirection` (requires `SpawnClockHour` 1–12); Demo may simplify per row above |
| Past times | Future remaining-second matches no longer fire after countdown passes them; rows with `SpawnRemainingSeconds = 0` fire once when remaining first hits 0 |

**Monster params & attack**

| Rule | Notes |
|------|-------|
| Table | `MonsterConfig` ([SPEC_04 §9.19](SPEC_04_Technical.md)) |
| TargetSelect | `Nearest` / `PreferWarrior` / `PreferProtagonist` |
| AttackMode | `Melee` / `Ranged`; monster `AttackRange` and hit params from `MonsterConfig` (may reuse soldier hit scheme D) |
| Vs soldier | Use `AttackPower` to **subtract from soldier current HP** directly (no armor/mitigation this batch) |
| Vs protagonist | Normal attack reduces Shield by 1 only (above); do not use `AttackPower` |
| Skills | `Skills` references SkillId+CD; skill-effect table **TBD**; **unused in Demo v1** (normal attacks only; ignore or leave empty at implement time) |
| Loot | On kill, `LootDrop` (same encoding as Dig: `Id_Count|…`) |

**Targeting & pathfinding (monsters)**

| Rule | Notes |
|------|-------|
| Select target | Per `MonsterConfig.TargetSelect` (above) |
| Destination | Position from which the monster can attack the target (range **TBD**) |
| Retarget interval | Every **TargetRetargetInterval** (provisional **1s**, configurable) recompute attackable point and request **NavMesh** repath |
| Tech | Rules layer outputs target + destination; movement via NavMeshAgent (or equiv.); rules must not drive `Transform`. See [SPEC_04 §9.7](SPEC_04_Technical.md) |
| Demo-min NavMesh | Bake (or runtime-equivalent) a **minimal walkable NavMesh** on `Prefabs/Maps/{BattleMapId}`; must cover in-map protagonist/soldier area and allow pathing from Demo fixed spawn points onto walkable surface. Exact off-map linkage and obstacle detail **deferred** |

**Warrior combat (WarriorCombat)**

| Rule | Notes |
|------|-------|
| Demo scope | **Demo v1**: soldiers use **normal attacks only**; do not read/cast Soul/Gem/ExtraEquipment skill lists; **no** skill-cast LossOfControl re-roll. `SkillCooldown` / `SkillConfig` / `Skills` fields **kept** for later; **unused** this Demo |
| Scope | Non-Rebel soldiers’ normal-attack / ASPD flow in `Combat`; skill **effects** (incl. revive) still **TBD** (not cast in Demo) |
| EngageZone | Candidate enemies = living monsters **inside EngageZone**; outside (incl. still-`OutsideMap` spawns not yet in zone) **not selectable** |
| Target select | **Default**: nearest living enemy inside EngageZone; if none, idle / do not chase outside |
| AttackPriority | `SoulConfig.AttackPriority` **unused** for targeting this batch; same enum as `TargetSelect`; field kept |
| AttackMode | From `SoulConfig.AttackMode` (`Melee` / `Ranged`); selects Melee vs Ranged branch of scheme D. Config examples (not ClassName hardcoding): Warrior-like→`Melee`+`Strength`; Archer-like→`Ranged`+`Agility`; Mage-like→`Ranged`+`Intelligence` (PrimaryStat dim from `ClassConfig.PrimaryStat`) |
| Mage vs Archer | **Same Ranged channel** (enter range → projectile → collision hit / timeout miss); rules-layer **only** difference is which `PrimaryStat` feeds `NormalAttackPower` (Mage Intelligence / Archer Agility); no separate mage skill or different projectile rules; View VFX may differ without changing settlement |
| AttackRange | Both Melee and Ranged have AttackRange (soldiers: `ClassConfig.AttackRange`; monsters: `MonsterConfig.AttackRange`); must move into target `AttackRange` before attack state / attack anim |
| Retarget / path | Same `TargetRetargetInterval` as monsters: periodically reselect nearest in EngageZone and recompute attackable point + NavMesh repath |
| Hit scheme D | **Melee** (`AttackMode=Melee`): end of `AttackWindup` → `HitConfirm` if target still alive and in `AttackRange`, else miss; **Ranged** (`AttackMode=Ranged`): spawn projectile, settle on **collision hit** or **timeout miss**; rules layer confirms damage; View plays anim/projectile only |
| Normal damage | On `HitConfirm` (or ranged hit): monster `HP -= NormalAttackPower` (no armor this batch); see formulas below |
| Attack speed | Interval between attack **starts** = `1 / AttackSpeed`; `AttackWindup` is **inside** that interval (not added outside) |
| Skill CD | Actual cooldown per formula below; skill-effect text **TBD** (incl. in-combat revive, end-of-battle revive); **Demo v1 does not cast skills — formula unused in combat** |
| CombatDead | Soldier with no gems and `HP ≤ 0` → `CombatDead` (revivable by in-combat revive skills; **TBD**); no §3.11 material fate |
| Gem exception | Non-empty `GemIds` and `HP ≤ 0` → **immediate** `PermanentDeath` (§3.11 material fate) |
| PermanentDeath settle | On stage victory `Ended` **or** LevelFailure: still `CombatDead` and no end-of-battle revive skill → `PermanentDeath` (instance gone; formation slot empty) |
| Rebel | **Rebels ignore EngageZone**; targeting remains nearest protagonist / other soldiers / enemies (above); AttackRange and hit use soldier channel (scheme D per that soldier’s `AttackMode`); vs soldier/monster also uses `NormalAttackPower` |

**Combat derive formulas (soldiers):**

Let `Primary` = the attribute dim selected by soldier `ClassId` → `ClassConfig.PrimaryStat`; `Str` / `Agi` / `Int` = Strength / Agility / Intelligence. Manufacture / formation UI uses §3.11 `StaticStat`; StartBattle and combat use `FinalStat`; recalc derives when Buffs change. Denominators use `max(·, 1)`.

Global constants below (e.g. `1.5` / `0.5` / `60` / `30` / `0.1`) are **missing-key defaults**; formal coeffs from `ClassConfig.CombatConvertCoeffs` (encoding: [SPEC_04 §9.9b](SPEC_04_Technical.md)). Hit params (`AttackRange` / windup / projectile / timeout) from the same table's separate columns (monsters: `MonsterConfig` same-named columns).

```
NormalAttackPower = Primary × 1.5

AttackSpeed = 0.5 + 60 / max(Agi, 1)
  // attacks per second; attack-start interval = 1 / AttackSpeed

SkillCooldown = max(0.1, SkillConfig.BaseCooldownSeconds - 30 / max(Int, 1))
  // seconds; SkillConfig in SPEC_04 §9.21; Demo v1 does not cast skills — unused in combat
```

| Derive | Static UI | Combat runtime |
|--------|-----------|----------------|
| Primary / Str / Agi / Int | `StaticStat` | `FinalStat` |
| MaxHP | §3.11: `ceil(BodyLife + StaticStat(Strength)×3)` | `ceil(BodyLife + FinalStat(Strength)×3)`; `BodyLife` unchanged |
| NormalAttackPower / AttackSpeed / SkillCooldown | formulas + static attrs | formulas + combat attrs (Demo may omit SkillCooldown display/drive) |

Soldier attack state machine (sketch):

```
Idle/Move → (target in EngageZone) → Move to AttackRange
  → wait until attack-start interval elapsed (1/AttackSpeed)
  → AttackWindup (within interval)
  → AttackMode=Melee: HitConfirm → monster HP -= NormalAttackPower (if still valid + in range) → Recovery
  → AttackMode=Ranged: spawn projectile → hit: HP -= NormalAttackPower; or timeout miss → Recovery
  // Demo: no skill cast; SkillCooldown / skill effects deferred
  → HP≤0 + no gems → CombatDead (revivable TBD)
  → HP≤0 + has gems → PermanentDeath (immediate)
  → On Ended / LevelFailure: CombatDead without end-battle revive → PermanentDeath
```

**Win / lose**

| Outcome | Condition |
|---------|-----------|
| **LevelFailure** | In `Combat`, **Shield ≤ 0** → immediate LevelFailure; **no** VictorySettlement / **no level settlement rewards** (§3.9); settle PermanentDeath for remaining `CombatDead` soldiers (no end-battle revive → PermanentDeath) |
| **Stage victory** | **All** of: ① **all** `WaveSpawnConfig` rows for this `WaveConfigId` **have fired**; ② **all** spawned monsters **have been killed** (no living enemies; no pending spawn rows) |
| Countdown = 0 | **Does not** alone win or lose |
| After stage victory | `Combat` → `Ended` → settle PermanentDeath (see WarriorCombat) → **credit stage Experience** (§3.11; **Demo fixed +100** to `LifetimeExperience`; formal table field deferred) → §3.9 stage settlement (other content **TBD**) → next stage / (if last) VictorySettlement |
| LevelFailure & Exp | LevelFailure **does not** credit stage Exp; already-owned Experience and other assets are **not clawed back** |
| Rebels & victory | Living Rebel soldiers do **not** alone block stage victory (victory still only checks spawn rows + monsters) |

```
Defend stage
  → DefendPhase = Prepare
  → Load BattleFormation {WarriorId, Position, RemainingHP}
  → Player may edit formation (positions / deploy / undeploy) → write back same BattleFormation
  → StartBattle requires deployed soldiers ≥ 1 (else button disabled / hint)
  → Player clicks StartBattle
  → DefendPhase = Combat
  → Spawn BattleProtagonist at BattleMap center
  → Shield = ProtagonistMaxHP (current level row)
  → RemainingCombatSeconds = CombatDurationSeconds
  → Deploy soldiers at **current** formation positions; MaxHP=ceil(BodyLife+FinalStat(Str)×3); RemainingHP clamp to MaxHP
  → Lock LossOfControlDegree = ΣCost / Cap − 1 and Tier; if Degree > 0, each soldier rolls FinalLossChance once (§3.11)
  → Each whole second (and on StartBattle for matching Remaining):
       fire WaveSpawnConfig rows where SpawnRemainingSeconds == RemainingCombatSeconds
       same-second rows ordered by SpawnOrder ascending
  → Each Monster:
       select target by TargetSelect
       set NavMesh destination = attackable position
       every TargetRetargetInterval (default 1s): recompute destination / repath
       normal hit on protagonist → Shield -= 1 (ignore AttackPower)
       hit on soldier → soldier HP -= AttackPower (no armor this batch)
       // Demo: MonsterConfig.Skills unused — normal attacks only
  → Each non-Rebel soldier (WarriorCombat):
       candidates = living monsters inside EngageZone (outside not selectable)
       target = nearest candidate (AttackPriority unused this batch)
       AttackMode from SoulConfig; Primary=FinalStat(ClassConfig.PrimaryStat via ClassId); NormalAttackPower=Primary×1.5
       AttackSpeed=0.5+60/max(Agi,1); interval=1/AttackSpeed; windup within interval
       // Demo: no skill cast; SkillCooldown formula retained but unused
       move into AttackRange → AttackWindup
       Melee: HitConfirm → monster HP -= NormalAttackPower; Ranged: projectile hit same / timeout miss
       HP≤0 + no gems → CombatDead; HP≤0 + gems → immediate PermanentDeath (§3.11)
       every TargetRetargetInterval: reselect nearest in EngageZone / repath
  → // Demo: no skill-cast LossOfControl re-roll (skills not cast)
  → Each Rebel soldier:
       nearest target among living protagonist / other soldiers / enemies (exclude self; **no EngageZone limit**)
       normal hit on protagonist → Shield -= 1
       hit on soldier/monster → soldier channel (scheme D; NormalAttackPower)
  → If Shield ≤ 0 → LevelFailure → settle PermanentDeath for remaining CombatDead (no end-battle revive)
       → no stage Exp / no VictorySettlement; keep already-owned; abort Level (§3.9)
  → If all WaveSpawnConfig rows for WaveConfigId fired AND all spawned monsters killed
       → DefendPhase = Ended → settle PermanentDeath for remaining CombatDead (no end-battle revive)
       → credit Experience → §3.9 settlement / next / VictorySettlement
       (living Rebels do not alone block stage victory)
  → RemainingCombatSeconds == 0 does NOT alone end Combat
```

---

## 3.13 科技树（TechTree）

### 简体中文

**状态：框架已关闭（规则库）；表结构 / 学习前置与费用 / 中心默认学会 / 画布交互已定义；各节点具体数值与图标、功能系统名完整枚举、学习失败提示文案仍 TBD**

科技树为 **中心向外** 扩展的科技项图。配置表载体见 [SPEC_04 §9.16 `TechTreeConfig`](SPEC_04_Technical.md)、[§9.17 `TechEffectConfig`](SPEC_04_Technical.md)。经验入账仍仅来自 Defend **阶段胜利**（§3.11 / §3.12）；击杀怪物 **不** 直接给经验。

**结构与前后置**

| 规则 | 说明 |
|------|------|
| 形态 | 中心根项向外扩展；玩家默认学会中心科技项（见下「初始学会」） |
| 正向边 | `TechTreeConfig.UnlockNextTechIds`：本项学会后可解锁的后续科技项 ID 列表 |
| 前置 | 逆边：某项的前置 = 所有把该项写在 `UnlockNextTechIds` 中的科技项 |
| 空间位置 | **不**写在配置表；由设置页科技树 Prefab 摆放节点世界/画布坐标 |
| 表第一行 | `TechTreeConfig` **第一行** = 画布默认镜头焦点节点 |

**科技点与经验（边界）**

| 规则 | 说明 |
|------|------|
| 经验 | 仅 Defend 阶段胜利结算 → `LifetimeExperience`（§3.11）；本专题不改 |
| 科技点来源 | 主角升级发放 `TechPointsReward`（§3.11） |
| 余额 | 存档持有可花费 `TechPoint` 余额；学习扣减；本版 **不可退点 / 不可遗忘** |

**初始学会（新建档）**

| 规则 | 说明 |
|------|------|
| 触发 | 新建存档时 |
| 条件 | 凡 `InitiallyUnlocked = true` 的科技项 **自动学会**（通常为中心根项；`LearnCost` 通常为 0） |
| 效果 | 立即应用对应 `TechEffectConfig`（含初始 `DigDamage`、挖坟单次速度相关 `DigDurationReductionSum` 等） |
| 镜头 | 打开科技树时默认对准表第一行节点（与是否中心项无关；中心项应同时为第一行且 `InitiallyUnlocked = true`） |

**学习条件与结算**

```
可学习 ⟺ 未学会
      ∧ TechPoint余额 ≥ LearnCost
      ∧ 至少有一个「把本项写在 UnlockNextTechIds 里」的已学会科技项
         （InitiallyUnlocked 已自动学会的项不再走本判定）
学会时：扣 LearnCost → 标记已学会 → 应用 TechEffectConfig
失败：科技点不足或前置未满足 → 不扣点、不学会；提示文案 TBD
```

**效果应用**

| 规则 | 说明 |
|------|------|
| 属性增量 | 解析 `AttributeModifiers`（`属性项_数值\|…`）；同属性多科技 **加法求和** |
| 挖坟能力 | 至少写入 `DigProtagonistCapabilities`：本版文档化键 `DigDamage`、`DigDurationReductionSum`（挖坟单次速度）；其它键后续补充 |
| 功能系统 | `UnlockedFeatureSystemName` 非空 → 加入存档 `UnlockedFeatureSystems`；系统名完整枚举 **TBD** |
| 重算 | 学会后重算派生能力；规则层写能力，View 只展示 |

**UI / 画布（UI-012）**

| 规则 | 说明 |
|------|------|
| 入口 | 工具面板 → **设置**（UI-007）→ 科技树画布 |
| 形态 | 2D 画布 |
| 节点展示 | 仅 **图标 + 科技项类型框**（`TechUiFrameType`）；不在节点上常驻名称正文 |
| 连线 | 按 `UnlockNextTechIds` 从本项画线至后续项 UI 框 |
| 默认镜头 | 对准 `TechTreeConfig` 第一行对应节点 |
| 平移 | 空白处 **按住鼠标左键拖动** 移动镜头 |
| 悬停 | 指针停留在科技项上 → 弹出描述：**科技项名字** + **科技项效果描述** |
| 学习操作 | 点击可学习节点 → 尝试学习（见上） |
| 视觉三态 | 已学会 / 可学 / 锁定（样式由类型框 + Prefab；细则 TBD） |

```
New save
  → Auto-learn all InitiallyUnlocked → apply TechEffect → DigProtagonistCapabilities / UnlockedFeatureSystems
Open Settings → TechTree canvas (camera on TechTreeConfig row 1)
  → Pan on empty LMB-drag; hover → name + effect desc; edges from UnlockNextTechIds
  → Click learnable node
       → if TechPoint ≥ LearnCost AND ≥1 learned prerequisite → spend → learn → apply effect
       → else fail (copy TBD)
Level-up (Defend Exp path) → TechPointsReward → spendable balance for learn
```

### English

**Status: Framework closed (rules library); schema / learn prereqs & cost / center default learn / canvas interaction defined; concrete node values & icons, full feature-system enum, fail-hint copy still TBD**

TechTree is a **center-out** graph of TechItems. Config tables: [SPEC_04 §9.16 `TechTreeConfig`](SPEC_04_Technical.md), [§9.17 `TechEffectConfig`](SPEC_04_Technical.md). Experience still only from Defend **stage victory** (§3.11 / §3.12); killing monsters does **not** grant Exp directly.

**Structure & prerequisites**

| Rule | Notes |
|------|-------|
| Shape | Center root expands outward; player default-learns the center item (see Initial learn) |
| Forward edges | `TechTreeConfig.UnlockNextTechIds`: subsequent TechIds unlocked after this item is learned |
| Prerequisites | Inverse edges: parents = all items that list this TechId in `UnlockNextTechIds` |
| Layout | **Not** in config; Prefab places node world/canvas positions on the Settings TechTree |
| First row | `TechTreeConfig` **first row** = default camera focus node |

**TechPoints & Experience (boundary)**

| Rule | Notes |
|------|-------|
| Experience | Only Defend stage victory → `LifetimeExperience` (§3.11); unchanged here |
| TechPoint source | Level-up grants `TechPointsReward` (§3.11) |
| Balance | Save-slot spendable `TechPoint`; learn deducts; this version **no refund / no unlearn** |

**Initial learn (new save)**

| Rule | Notes |
|------|-------|
| When | On create SaveSlot |
| Which | All `InitiallyUnlocked = true` items are **auto-learned** (typically center root; `LearnCost` usually 0) |
| Effects | Immediately apply matching `TechEffectConfig` (incl. initial `DigDamage`, dig-speed via `DigDurationReductionSum`, etc.) |
| Camera | Opening TechTree focuses first table row (center root should be row 1 and `InitiallyUnlocked = true`) |

**Learn conditions & resolve**

```
Learnable ⟺ not yet learned
        ∧ TechPoint balance ≥ LearnCost
        ∧ ≥1 learned item lists this TechId in UnlockNextTechIds
           (InitiallyUnlocked auto-learned items skip this gate)
On learn: deduct LearnCost → mark learned → apply TechEffectConfig
On fail: insufficient TechPoints or missing prereq → no spend; hint copy TBD
```

**Effect application**

| Rule | Notes |
|------|-------|
| Attribute deltas | Parse `AttributeModifiers` (`Attr_Value\|…`); same attr across techs **sums additively** |
| Dig caps | At least write `DigProtagonistCapabilities`: documented keys `DigDamage`, `DigDurationReductionSum` (dig action speed); more keys later |
| Feature systems | Non-empty `UnlockedFeatureSystemName` → add to save `UnlockedFeatureSystems`; full enum **TBD** |
| Recalc | Recalc derived caps after learn; rules write caps, View displays only |

**UI / canvas (UI-012)**

| Rule | Notes |
|------|-------|
| Entry | Tools → **Settings** (UI-007) → TechTree canvas |
| Shape | 2D canvas |
| Node display | **Icon + TechUiFrameType frame** only; no persistent name body on node |
| Edges | Draw lines from item to each `UnlockNextTechIds` target frame |
| Default camera | Focus `TechTreeConfig` first-row node |
| Pan | **Hold LMB on empty space** and drag |
| Hover | Pointer over item → tooltip: **DisplayName** + **EffectDescription** |
| Learn | Click learnable node → attempt learn (above) |
| Visual states | Learned / Learnable / Locked (frame + Prefab; polish TBD) |

```
New save
  → Auto-learn all InitiallyUnlocked → apply TechEffect → DigProtagonistCapabilities / UnlockedFeatureSystems
Open Settings → TechTree canvas (camera on TechTreeConfig row 1)
  → Pan on empty LMB-drag; hover → name + effect desc; edges from UnlockNextTechIds
  → Click learnable node
       → if TechPoint ≥ LearnCost AND ≥1 learned prerequisite → spend → learn → apply effect
       → else fail (copy TBD)
Level-up (Defend Exp path) → TechPointsReward → spendable balance for learn
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
- [x] 科技树框架：中心向外 / InitiallyUnlocked 默认学会 / 前后置与 LearnCost / 画布交互（§3.13；UI-012）
- [x] 科技树配置表 `TechTreeConfig` + 效果表 `TechEffectConfig` 字段（SPEC_04 §9.16–§9.17）
- [ ] 科技树各节点具体数值、图标资源与功能系统名完整枚举
- [ ] 挖坟帧动画具体数量与资源命名清单
- [x] 升级与制造框架（§3.11；原 SewRevive 更名 UpgradeManufacture）— **框架已关闭**
- [x] 升级与制造阶段结束=玩家确认；**无独立阶段结算**
- [x] 升级与制造主屏布局（同屏三区 + 底部完成；UI-010）；升级/制造区控件仍 TBD
- [x] BattleFormation：§3.11 与 Defend Prepare **同一编辑器**；连续坐标；Prepare 不可制造
- [x] 经验：Defend 阶段胜利统一入账至 `LifetimeExperience`；升级不扣减累计经验；科技树消费见 §3.13
- [x] 士兵=独立实例；**士兵制造流程/槽位/最低要求/精魂闸门/命名已关闭**（§3.11）
- [x] 躯体材料表 `BodyPartConfig` 完整字段 + `Base(S)=Σ StatBonus`；躯体外观表与选取/保底算法（§3.11 / SPEC_04 §9.12–§9.13）；具体数值行仍 TBD
- [x] 控制力上限=当前等级行 `ControlPowerCap`（科技加成另专题）；失控程度/四档/叛变判定与概率公式已关闭（§3.11 / §3.12 / SPEC_04 §9.20）；失控不挡开战
- [x] 无上阵士兵时不允许开战（须 ≥1）
- [x] 关卡失败：不入账本阶段经验、无关卡结算奖励；已获得不扣除
- [x] 主角升级配置表 `ProtagonistLevelConfig` 字段与累计阈值语义（SPEC_04 §9.8）；各行具体数值仍 TBD
- [x] 士兵控制力占用值 = 躯体+灵魂+额外装备+宝石叠加（制造时定稿）；灵魂配置表 `SoulConfig`（SPEC_04 §9.9）；职业配置表 `ClassConfig`（SPEC_04 §9.9b）；宝石配置表 `GemConfig`（SPEC_04 §9.10）；种族配置表 `RaceConfig`（SPEC_04 §9.11）
- [x] 宝石：制造可选镶嵌（**6 槽、类型互斥**）；五维 `GemMult`（多颗按维 **Σ**）；**彻底死亡**全部回仓库，其余绑定材料销毁；带宝石士兵 HP≤0 立即彻底死亡
- [x] 种族：躯体部位权重 1 加权随机定稿；五维 `RaceAdjustCoeff`；不另计控制力；为主标签来源
- [x] FinalStat 按单项属性汇总（先定 `S` 再取来源）；`FinalStat(S)=max(0, …)` 下限保护
- [x] 士兵命名：`Prefix(es)+RaceName+ClassName+Suffix`（外置前缀 / 种族 / 职业 ClassConfig.ClassName / 宝石后缀表）
- [ ] 躯体/外观具体数值与美术资源清单（另专题）
- [ ] 额外装备完整配置数值（表结构已锁：SPEC_04 §9.14）
- [ ] 失控配置表 / 种族·宝石·技能失控加成具体数值行（另专题）
- [ ] 灵魂 / 职业表具体数值（结构与编码已锁：CombatConvertCoeffs / MoveStyle / AttackPriority；本批 AttackPriority 不驱动选目标）
- [ ] 宝石获取途径、五维 GemMult/技能具体数值、镶嵌 UI 与回仓表现（另专题；GemType 六类与 ComboKey 编码已锁）
- [ ] 种族列表与各维 RaceAdjustCoeff 具体数值（另专题）
- [ ] 升级区内具体控件与数值展示；制造区控件细节（槽位规则已定）
- [x] 防守（Defend）框架：准备/开战/部署/NavMesh 寻路/阶段胜利与关卡失败（§3.12）
- [x] 防守刷怪波次表、倒计时激活节奏与出现位置/方式（§3.12 / SPEC_04 §9.18）；**Demo 最小刷怪点/NavMesh 已关闭**；精确 OutsideMap 几何后置
- [x] Demo 验收扩大：Meta 壳 + Dig→UM→Defend 流水线（SPEC_03 §3.8 D-001～D-043）；UM `GameplayConfigId`=忽略
- [x] 怪物配置表与目标选择（§3.12 / SPEC_04 §9.19）；怪物对士兵：`AttackPower` 直接扣 HP（本批无护甲）；AttackRange 等命中列已锁
- [x] 士兵战斗派生：ClassId→ClassConfig.PrimaryStat / NormalAttackPower / AttackSpeed / SkillCooldown / MaxHP=ceil(BodyLife+Str×3)；CombatConvertCoeffs 编码已锁（§3.11 / §3.12 / SPEC_04 §9.9b）
- [x] 士兵战斗（WarriorCombat）：EngageZone 最近选敌、AttackMode（SoulConfig）、AttackRange（ClassConfig）、命中方案 D、CombatDead / PermanentDeath / 宝石特例（§3.12）；**第一版 Demo 仅普攻**（士兵与怪物不施放技能；法师=远程+Intelligence，同射手通道）
- [x] 护盾（Shield）：开战取 `ProtagonistMaxHP`；普通攻击命中 −1（含叛变士兵）；归零 LevelFailure（§3.12）
- [x] 失控判定时机与叛变 AI（开战锁定 Degree；就近目标；技能二次完整率 roll——**Demo 无技能施放故不触发二次 roll**）（§3.11 / §3.12）
- [ ] 防守阶段结算其余字段；关卡失败结算 UI / 字段
- [ ] 怪物技能效果表；技能命中主角是否扣盾；士兵技能效果与复活技能（另专题；**Demo 不实现**）；精确 OutsideMap 出生几何（Demo 后置）
- [x] 科技树画布 Demo 垂直（方案 A：学习扣点 + Dig 能力重算可验；非 §3.8 P0）
- [ ] 科技树节点具体数值/图标 polish 与功能系统名完整枚举
- [ ] 设置项清单（科技树入口已定；其它设置项 TBD）
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
- [x] TechTree framework: center-out / InitiallyUnlocked default learn / prereqs & LearnCost / canvas UI (§3.13; UI-012)
- [x] `TechTreeConfig` + `TechEffectConfig` schemas (SPEC_04 §9.16–§9.17)
- [ ] Concrete tech-node values, icons, and full feature-system enum
- [ ] Dig frame-anim count and asset naming list
- [x] UpgradeManufacture framework closed (§3.11)
- [x] UpgradeManufacture: player confirm end; **no** independent stage settlement
- [x] UI-010 three panels + Complete; Upgrade widgets still TBD; Manufacture slots/preview closed
- [x] BattleFormation: shared editor; continuous coords; no manufacture in Prepare
- [x] Exp: Defend victory → `LifetimeExperience`; level-up does not deduct cumulative Exp; TechTree spend in §3.13
- [x] Warrior = instance; **manufacture flow/slots/min requirements/Spirit gate/naming closed** (§3.11)
- [x] BodyPartConfig full schema + `Base(S)=Σ StatBonus`; BodyAppearance pick/fallback (§3.11 / SPEC_04 §9.12–§9.13); concrete value rows still TBD
- [x] ControlPower cap = level-row `ControlPowerCap` (tech bonus later); LossOfControlDegree / four tiers / Rebel rolls & chance formula closed (§3.11 / §3.12 / SPEC_04 §9.20); does not block StartBattle
- [x] StartBattle requires ≥1 deployed soldier
- [x] LevelFailure: no stage Exp / no level settlement rewards; already-owned not clawed back
- [x] `ProtagonistLevelConfig` schema + cumulative threshold semantics (SPEC_04 §9.8); concrete row numbers still TBD
- [x] ControlPowerCost = Body+Soul+ExtraEquipment+Gem (finalized at manufacture); `SoulConfig` (SPEC_04 §9.9); `ClassConfig` (SPEC_04 §9.9b); `GemConfig` (SPEC_04 §9.10); `RaceConfig` (SPEC_04 §9.11)
- [x] Gem: optional sockets (**6, type-exclusive**); five-dim `GemMult` (multi-gem **Σ** per dim); on **PermanentDeath** all return to Warehouse; other bound materials destroyed; gemmed soldiers PermanentDeath immediately on HP≤0
- [x] Race: weight-1 pick from filled BodyParts; five-dim `RaceAdjustCoeff`; no separate ControlPower term; primary WarriorInfo label
- [x] FinalStat per-attribute aggregation (pick `S` then sources); `FinalStat(S)=max(0, …)` floor
- [x] WarriorName = Prefix(es)+RaceName+ClassName+Suffix (prefix / Race / ClassConfig.ClassName / gem suffix)
- [ ] Concrete Body/Appearance numbers & art list (later topic)
- [ ] ExtraEquipment concrete numbers (schema locked: SPEC_04 §9.14)
- [ ] LossOfControlConfig / Race·Gem·Skill chance-bonus concrete rows (later topic)
- [ ] Soul / Class table concrete numbers (schema/encodings locked: CombatConvertCoeffs / MoveStyle / AttackPriority; AttackPriority unused for targeting this batch)
- [ ] Gem acquisition, five-dim GemMult/skills, socket UI & return VFX (later topic; GemType six types + ComboKey encoding locked)
- [ ] Race list and concrete per-dim RaceAdjustCoeff values (later topic)
- [ ] Upgrade in-panel widgets; Manufacture widget polish (slot rules closed)
- [x] Defend framework (§3.12)
- [x] Defend wave spawn table, countdown activation, appear location/mode (§3.12 / SPEC_04 §9.18); **Demo-min spawn/NavMesh closed**; exact OutsideMap geometry deferred
- [x] MonsterConfig + TargetSelect (§3.12 / SPEC_04 §9.19); monster vs soldier: `AttackPower` subtracts HP directly (no armor this batch); AttackRange hit columns locked
- [x] Soldier combat derives: ClassId→ClassConfig.PrimaryStat / NormalAttackPower / AttackSpeed / SkillCooldown / MaxHP=ceil(BodyLife+Str×3); CombatConvertCoeffs encoding locked (§3.11 / §3.12 / SPEC_04 §9.9b)
- [x] WarriorCombat: EngageZone nearest target, AttackMode (SoulConfig), AttackRange (ClassConfig), hit scheme D, CombatDead / PermanentDeath / gem exception (§3.12); **Demo v1 normal attacks only** (soldiers & monsters; Mage = Ranged+Intelligence, same channel as Archer)
- [x] Shield: init from `ProtagonistMaxHP`; normal hit −1 (incl. Rebel soldiers); Shield ≤ 0 → LevelFailure (§3.12)
- [x] LossOfControl roll timing & Rebel AI (Degree locked at StartBattle; nearest target; skill-cast full-chance re-roll — **Demo does not cast skills so no secondary roll**) (§3.11 / §3.12)
- [x] Demo acceptance expanded: Meta shell + Dig→UM→Defend pipeline (SPEC_03 §3.8 D-001～D-043); UM `GameplayConfigId` = ignore
- [ ] Defend settlement other fields; LevelFailure settlement UI / fields
- [ ] Monster skill-effect table; whether skill hits reduce Shield; soldier skill effects & revive skills (later topic; **not in Demo**); exact OutsideMap spawn geometry (post-Demo)
- [x] TechTree canvas Demo vertical (Approach A: spend+learn + Dig caps recalc verifiable; not §3.8 P0)
- [ ] Concrete tech-node values/icon polish & full feature-system enum
- [ ] Settings item list (TechTree entry closed; other settings TBD)
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
