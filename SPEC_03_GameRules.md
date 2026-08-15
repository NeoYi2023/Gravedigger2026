# SPEC_03 — 游戏规则 / Game Rules（Gravedigger2026）

**关联文档 / Related:** [SPEC_00_Index.md](SPEC_00_Index.md) · [SPEC_02_GameOverview.md](SPEC_02_GameOverview.md) · [SPEC_04_Technical.md](SPEC_04_Technical.md)

> Demo 验收已扩大为「Meta 壳 + 一条关卡流水线垂直切片」（§3.8）；关卡阶段 / 挖坟 / 升级与制造 / 防守 / 科技树 / 推图战 / 自动制造 / 主角装备规则见 §3.9–§3.16。Unity 编码须负责人明确授权 Demo 开发。

---

## 3.1 术语与实体

### 简体中文

| 术语 (EN) | 中文 | 定义 |
|-----------|------|------|
| GameplayState | 玩法状态 | 局内主状态枚举：`Dig`（挖坟）、`AutoManufacture`（自动制造；Mode2 流水线）、`UpgradeManufacture`（升级与制造；原占位名 `SewRevive`）、`Defend`（防守）、`PushMap`（推图战）。关卡运行时由当前阶段的玩法类型决定（§3.9）；壳层默认占位仍为 Dig。 |
| SaveSlot | 存档槽 | 固定数量的本地存档位；本版 **3 槽**（索引 0–2）。空槽可新建，占用槽可进入或删除。占用旗按槽共享；士兵池/布阵/副本解锁等进度按槽 **且按 `CampaignMode`** 隔离（§3.4）。 |
| CampaignMode | 玩法模式 | 存档级玩法门闩：`Mode1` / `Mode2`。每次新建或进入均经 `CampaignModeSelect` 选择；同槽两模式进度完全隔离；Mode2 使用独立配置表根（[SPEC_04 §14](SPEC_04_Technical.md)）。Mode2 与 Mode1 共用战斗与挖坟机制；**士兵制造**：Mode1 手动（§3.11），Mode2 自动制造（§3.15）。**勿与** `BattleMode`（保卫战/推图战）混淆。 |
| AutoManufacture | 自动制造 | Mode2 关卡玩法类型 / `GameplayState`：DigStageSummary 确认后进入；按规则自动选料→造兵→临时仓库→清空布阵后按职业区上阵；结束后进 `UpgradeManufacture`（§3.15）。 |
| TempWarriorWarehouse | 临时仓库 | AutoManufacture 阶段批内缓冲：造好的士兵先入此仓，全部造完后再入 `WarriorPool` 并自动上阵（§3.15）。 |
| PrimaryHand | 主要手 | `BodyPartConfig.IsPrimaryHand=1` 的手臂材料；Mode2 自动制造的选料锚点与职业限定主源（§3.15）。 |
| SecondaryHand | 次要手 | `IsPrimaryHand=0` 的手臂材料；与主要手组成双臂；职业取双手 `ClassRestrict` 交集（无交集则仅主要手池）（§3.15）。 |
| ClassRestrict | 职业限定 | 躯体材料可产出的 `ClassId` 多值列表（`\|` 分隔）；Mode2 职业由双手交集/回退决定（§3.15，[SPEC_04 §9.12](SPEC_04_Technical.md)）。 |
| BodyPrimaryStat | 躯体主属性 | 躯体材料字段：`Strength` / `Agility` / `Intelligence` 恰一；Mode2 选其余部位时的匹配键（**勿与** 职业 `PrimaryStat` 混淆）（§3.15）。 |
| ApproxBodyLevel | 近似品质 | Mode2 选料：相对锚点 `|ΔBodyLevel| ≤ 1`；候选排序更高 → 相同 → 低 1 级（§3.15）。 |
| PlacementOrder | 放置排序 | `ClassConfig` 字段（≥1）；AutoManufacture 自动上阵时按此升序先后放置各职业（§3.15，[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| FormationClassZone | 职业布阵区 | 布阵地图 Prefab 上按职业标定的空间区域（**IsoDiamond**：`HalfExtents` 为菱形顶点到中心；与 WalkSurface 同形；无 Y 旋转）；自动上阵落入对应区并做碰撞挤开（§3.15，[SPEC_04 §13](SPEC_04_Technical.md)）。 |
| MagicBook | 魔法书 | 主角特殊装备；效果库；Mode2 在 UI-016 Step2 **单槽脉冲峰值**触发；含「还原」（`RaceWeightPick`）、「战士强化」（`StatMul`/`Primary`）、「士兵技能升级」（`SoldierSkillLevelAdd`）、「职业进阶」（`ForceClass`）等（§3.15，[SPEC_04 §9.24](SPEC_04_Technical.md)）。**勿与** §3.16 `ProtagonistEquipment` 混淆。 |
| MagicBookConfig | 魔法书配置表 | MagicBookId → IsUnique、IsProbabilistic（概率型）、EffectPhase、EffectPayload、EffectParams、Icon、名称、介绍（§3.15，[SPEC_04 §9.24](SPEC_04_Technical.md)）。 |
| SpecialEquipSlot | 特殊装备槽 | 主角默认 **6** 槽装配魔法书；同书默认可叠，`IsUnique=1` 不可（§3.15）。 |
| ProtagonistEquipment | 主角装备 | 主角成长型装备；仓内拥有即按当前等级生效；同 Id 转化经验 / 公共经验升级；与 MagicBook、材料 Warehouse、士兵 ExtraEquipment **并行**（§3.16，[SPEC_04 §9.25](SPEC_04_Technical.md)）。 |
| ProtagonistEquipmentWarehouse | 主角装备仓库 | 存档级状态仓：存 `OwnedEquip[]`；不限种类总数；每种 `EquipId` 至多 1 件（§3.16）。 |
| ProtagonistEquipmentConfig | 主角装备配置表 | 复合主键 `EquipId`+`EquipLevel` → 名/图标/升下一级经验/转化经验/生效域/效果/描述（§3.16，[SPEC_04 §9.25](SPEC_04_Technical.md)）。 |
| EquipCommonExp | 装备公共经验 | 独立经验池，专供主角装备升级；与 `LifetimeExperience` 无关（§3.16）。 |
| OwnedEquip | 已拥有装备实例 | 仓内一件：`EquipId`、`Level`、`CurrentExp`（§3.16）。 |
| EquipEffectDomain | 装备生效功能 | 装备效果域枚举：`Dig` \| `SoldierManufacture` \| `Combat`（可多值；§3.16）。 |
| EffectPhase | 生效环节 | 魔法书触发时机枚举：至少含 `SoldierManufacture` / `Combat`；Mode2 制造书在 UI-016 Step2 单槽节拍 apply（§3.15）。 |
| EffectPayload | 魔法书效果编码 | 已登记 PascalCase Token（如 `RaceWeightPick`）；空=无效果；未登记 Token 空 apply + 警告（§3.15，[SPEC_04 §9.24](SPEC_04_Technical.md)）。 |
| EffectParams | 魔法书效果参数 | 与 Token 配套：`Key=Value` 或 `Key=Value\|…`；空=无参/缺省（§3.15，[SPEC_04 §9.24](SPEC_04_Technical.md)）。 |
| ManufactureRecord | 制造记录 | Mode2 UM 只读弹窗：展示**最近一批** AutoManufacture 造出的士兵摘要（名字/种族/职业）；入口在「布阵」右侧（UI-015 / §3.15）。 |
| AutoManufactureBatchRecord | 自动制造批次记录 | 存档级最近一批 `WarriorId` 列表；下一批覆盖；按槽 + CampaignMode 持久化（§3.15，[SPEC_04 §6](SPEC_04_Technical.md)）。 |
| AutoManufacturePresentation | 自动制造演出 | Mode2 AutoManufacture 阶段表现层（UI-016）：规则跑批后播 Step1–2，再进 UM 并自动开布阵（§3.15）。 |
| CampaignModeSelect | 玩法模式选择 | 点击「新建」或「进入」后弹出的选模式 UI（UI-014）；取消则留在存档界面（§3.2、§3.6）。 |
| InSaveShell | 进档壳层 | 选定存档 **且选定 `CampaignMode`** 进入后的常驻壳：承载当前 `GameplayState` 占位与浮动「工具」入口。 |
| ToolsPanel | 工具面板 | Demo 调试/设置壳层 UI；由浮动「工具」按钮打开。本期含「设置」「关卡」入口（关卡→列表选关），以及 Demo GM「增加主角装备」「增加魔法书」（→ GmGrantListPanel，见 §3.5 / UI-019 / D-061）。 |
| Level | 关卡 | 由「关卡运作表」定义的多阶段流程实体；每阶段指定玩法类型与玩法配置 ID（§3.9；UM 阶段 ConfigId **忽略**）。工具「关卡」打开列表选关（去重 LevelId → Stage 1）；场景绑定 **TBD**。 |
| LevelOperation | 关卡运作 | 关卡运作表一行：关卡 ID + 阶段编号 + 玩法类型 + 玩法配置 ID。 |
| DigGameplayConfig | 挖坟配置 | 挖坟配置表一行：时长、开局坟数、过程生成速率、品质权重（零权重项剔除）等（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| Grave | 坟墓 | 挖坟地图上的可生成实体；带坟墓品质 ID；落点须避开已有坟与障碍物。 |
| VictorySettlement | 胜利结算 | 关卡**最后一阶段**结束后触发的关卡级结算反馈。 |
| DigMap | 挖坟地图 | 表现上用 Unity **Isometric Tilemap** 铺斜 45° 菱形地板（正交/无透视）；**逻辑足迹 IsoDiamond**（XZ 曼哈顿菱形，与砖面外轮廓对齐；连续可放置，非格子网格）；表现 Prefab 逻辑名 `Ground_01`…`Ground_05`（`DigMapId`）。 |
| Digger | 挖坟主角 | Dig 阶段**不**在地图生成 3D/整角模型；主角表现为 Dig HUD **左上角 60×60** 头像框（§3.10）；`Digger` Prefab 仍可保留于 Catalog/美术管线，见 [SPEC_04 §15](SPEC_04_Technical.md)。 |
| DigAction | 挖掘流程 | 圆圈光标与坟 DigHitShape 相交且停留 ≥0.2s 触发；半径内全部满足条件的坟**同时**各启 DigAction；每坟独立 `DigActionDuration` 后结算扣血；该坟挖掘中不可重复触发（§3.10）。 |
| DigObstacle | 挖坟障碍物 | Dig 阶段**仅**未消除 Grave；圆形障碍半径在坟预制体上配置（§3.10）。**不含**地图中心主角。 |
| DigHitShape | 挖坟命中形 | Grave Prefab 离线烘焙本地 XZ 凸包（贴近精灵轮廓）；光标圆相交判定；与 DigObstacle 分离（§3.10）。 |
| DigProtagonistCapabilities | 挖坟主角能力 | 存档主角派生：挖坟伤害、挖坟阶段时长加成、时长缩短和、光标半径、可挖品质集合、坟墓生成权重加成；由**科技树**学会与**主角装备**（`EffectDomain` 含 `Dig`）效果按键加法重算（§3.10、§3.13、§3.16）。 |
| GraveHP | 坟墓血量 | 坟墓当前/最大生命；maxHP 来自坟墓品质定义表；扣至 0 触发挖掘成功与奖励（§3.10）。 |
| GraveIconStyle | 坟墓图标样式 | 按剩余 HP% 切换：>65% 样式1；30%–65% 样式2；<30% 样式3（§3.10）。 |
| GraveQualityConfig | 坟墓品质定义表 | 品质 ID → maxHP、掉落等；被挖坟权重引用（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| DigReward | 挖掘奖励 | 坟 HP 归 0 时在成功动画中心生成的奖励图标；飞向 Dig HUD 左上角主角头像框，到达后入账并消失（§3.10）。 |
| DigStageSummary | 挖坟阶段汇总 | Dig 有效时长归零后弹出的汇总弹窗：仅展示本阶段已获奖励按类型汇总，无额外发放；躯体材料行 `{DisplayName} Lv{BodyLevel} × 数量`；右上「X」确认关闭（§3.10，UI-011）。 |
| Warehouse | 仓库 | 按存档槽持久的材料仓库；不限格数与存储时长；材料按类型堆叠上限 10000（§3.10）。 |
| SpiritEssence | 精魂 | 货币；挖坟获得（LootDrop 保留 Id + 堆叠超限自动兑换）；制造士兵时消耗（§3.10、§3.11）。 |
| MaterialConfig | 材料配置表 | MaterialId → AutoConvert、AppearanceIconId、AssetPath、WarehouseQualityOutlineId；堆叠超限时按 AutoConvert 兑精魂（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| CurrencyConfig | 货币配置表 | CurrencyId → 外观图/素材路径/仓库品质外轮廓；精魂保留 Id=`Spirit`（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| UpgradeManufacture | 升级与制造 | 阶段玩法类型（原占位 `SewRevive`）：角色升级 + 制造士兵 + 战斗布阵；见 §3.11。 |
| Experience | 经验 | Defend 或 PushMap **阶段胜利**结算时加算至 `LifetimeExperience`；关卡失败不入账；达累计阈值升级（§3.11、§3.12、§3.14）。 |
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
| BodyPartConfig | 躯体材料配置表 | BodyPartId → 道具名称（`DisplayName`）/等级/部位/种族/控制力/精魂消耗/StatBonus/AutoConvert/介绍/美术素材（§3.11，[SPEC_04 §9.12](SPEC_04_Technical.md)）。 |
| BodySlot | 躯体槽类型 | `Head` / `Torso` / `Arm` / `Leg`（§3.11）。 |
| BodyLevel | 躯体等级 | 躯体材料字段；制造时对已放部位取平均后定外观等级（§3.11）。 |
| StatBonus | 增加的属性值 | 躯体材料平坦属性加成串；`Base(S)=Σ StatBonus(S)`（§3.11）。 |
| Body | 躯体 | 制造所用躯体部位集合；`Base(S)=Σ StatBonus(S)`；各部位 `RaceId` 加权定种族；贡献控制力占用（§3.11）。 |
| BaseStats | 基础属性 | 由已放躯体部位 `StatBonus` 按维求和：生命值、移动速度、力量、敏捷、智力；经 StaticStat/FinalStat 后派生攻/速/CD/血（§3.11、§3.12）。 |
| StaticStat | 静态属性 | 制造/布阵展示用：`max(0, Base+Equip+Base×GemMult+Base×RaceAdjust)`；不含 `SkillBuff`（§3.11）。 |
| PrimaryStat | 主属性 | 职业配置字段：`Strength` / `Agility` / `Intelligence`；决定普攻攻击值所用属性维（§3.11、§3.12，[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| Class | 职业 | 由实例 `ClassId` 提供（有灵魂取自该灵魂；无灵魂强制 `Class_Servants`）；决定 `ClassName`、`PrimaryStat`，以及对五维→战斗参数的换算系数调整（§3.11、§3.12，[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| ClassId | 职业ID | 职业主键；有灵魂时取自灵魂 `ClassId`；无灵魂时强制 `Class_Servants`；制造时写入士兵实例（§3.11，[SPEC_04 §9.9](SPEC_04_Technical.md) / [§9.9b](SPEC_04_Technical.md)）。 |
| ClassConfig | 职业配置表 | ClassId → ClassName、ClassLevel（展示用等级）、BaseClass（基础职业，预留）、PrimaryStat、CombatConvertCoeffs（`键_数值|…`）、AttackRange / 前摇 / 弹速 / 超时、`DefaultSkillIds`（制造默认士兵技能）（§3.11，[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| ClassLevel | 职业等级 | `ClassConfig` 展示字段（品质等级）；UI-016 士兵卡职业名下显示 `Lv.{ClassLevel}`；**不**进战斗/制造公式（[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| BaseClass | 基础职业 | `ClassConfig` 字段；CSV 中文 `战士`/`射手`/`法师`/`盗贼`；空或非法→`Unspecified`；**预留**后续魔法书等条件；**不**参与命名/外观/`PrimaryStat`/战斗（[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| DefaultSkillIds | 制造默认获得技能ID | `ClassConfig` 列；空=无；否则 `SkillId` 或 `SkillId\|SkillId`（FK → `SkillConfig.SkillId`）。`ClassId` 最终定稿后写入实例 `SoldierSkills`，初始等级 **1**（§3.11、§3.15）。 |
| SoldierSkill | 士兵技能 | 绑定在士兵**实例**上的技能；制造时由职业默认授予；Mode2 可由魔法书改等级；**无**消耗经验升级；`PermanentDeath` 随实例删除；权威表 `SkillConfig`（§3.11，[SPEC_04 §9.21](SPEC_04_Technical.md)）。**勿与**灵魂/宝石/外置 `Skills` 列表混淆（并行；同 Id 合并 **TBD**）。 |
| SoldierSkills | 士兵技能列表 | 实例字段 `{ SkillId, SkillLevel }[]`；制造烘进快照；`CombatDead` 保留；`PermanentDeath` 删除（§3.11，[SPEC_04 §9.9](SPEC_04_Technical.md)）。 |
| SkillConfig | 技能配置表 | 士兵技能权威表；复合主键 `(SkillId, SkillLevel)` → 名称/图标/描述/`SkillEffectId`/CD/失控加成等；怪物仍走 `MonsterConfig.Skills`；**Demo 不施放**（§3.11、§3.12，[SPEC_04 §9.21](SPEC_04_Technical.md)）。 |
| SkillEffectConfig | 技能效果配置表 | `SkillEffectId` 主键；效果正文列仍骨架；被 `SkillConfig.SkillEffectId` 引用（[SPEC_04 §9.21b](SPEC_04_Technical.md)）。 |
| BodyLife | 躯体生命 | `Base(MaxHP)+Equip(MaxHP)`；制造锁定；不含宝石/种族/Buff 对生命维放大；代入士兵 `MaxHP` 公式（§3.11）。 |
| CombatConstantConfig | 战斗常量表 | 全局战斗公式默认键值（`ConstantKey`→`Value`）；含 `NormalAttackPrimaryMult` 等与 `MaxHpStrengthMult`；职业 `CombatConvertCoeffs` 缺键回退本表（§3.11、§3.12，[SPEC_04 §9.20b](SPEC_04_Technical.md)）。 |
| NormalAttackPower | 普通攻击值 | `Primary × NormalAttackPrimaryMult`（职业覆盖，否则常量表；样例默认 15）；命中后对怪物直接扣血（本批无护甲）（§3.12）。 |
| AttackSpeed | 攻击速度 | 次/秒：`AttackSpeedBase+AttackSpeedAgiDiv/max(Agi,1)`（系数同上）；攻击开始间隔=`1/AttackSpeed`（§3.12）。 |
| MaxHpStrengthMult | 血量力量系数 | 常量表键；`MaxHP=ceil(BodyLife+Str×本值)`；样例默认 **3**（§3.11）。 |
| BodyAppearance | 躯体外观 | 预设整体外观造型；制造时按平均躯体等级+定稿种族+职业名选取（§3.11，[SPEC_04 §9.13](SPEC_04_Technical.md)）；资源为 Character Creator **烘焙整角** Prefab，见 [SPEC_04 §15](SPEC_04_Technical.md)。 |
| BodyAppearanceConfig | 躯体外观配置表 | AppearanceId → 外观等级/隶属种族/职业倾向/介绍/保底外形/`BodyRadius`（§3.11，[SPEC_04 §9.13](SPEC_04_Technical.md)）。 |
| IsFallback | 保底外形 | 外观表字段；`1`=该种族保底外观；每种族至多一行；等级+种族命中但职业倾向无匹配时走保底；等级+种族候选集 A 为空时先改写为 `Race_Undead` 再选外观（§3.11）。 |
| Race | 种族 | 默认：参与部位全部 `RaceId` 相同 → 该族，否则 `Race_Undead`；Mode2 装备「还原」时改回部位权重 1 加权随机；一士兵一族；提供五维 `RaceAdjustCoeff`；配置见 `RaceConfig`（§3.11/§3.15，[SPEC_04 §9.11](SPEC_04_Technical.md)）。 |
| RaceConfig | 种族配置表 | RaceId → 展示名、五维种族属性调整系数（§3.11，[SPEC_04 §9.11](SPEC_04_Technical.md)）。 |
| RaceAdjustCoeff | 种族属性调整系数 | 五维（对应五项基础属性）；缺省维为 0；可正可负；代入 `BaseStat × RaceAdjustCoeff`；**不**单独计入控制力占用（§3.11）。 |
| Soul | 灵魂 | 制造槽位 **可选**；有灵魂则消耗该行；无灵魂则实例 `SoulId=Soul_00`（系统默认行），`AttackMode`/技能/优先级/移动风格/灵魂侧 Spirit·控制力费用读 `Soul_00`，且 **强制** `ClassId=Class_Servants`；**不**改写力量/敏捷/智力本身；配置见 `SoulConfig`（§3.11，[SPEC_04 §9.9](SPEC_04_Technical.md)）。 |
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
| BattleFormation | 战斗布阵 | 安排士兵上阵；持久化士兵 ID、位置、剩余血量；可在 §3.11 与 Defend / PushMap `Prepare` 编辑同一套数据（§3.11、§3.12、§3.14）。 |
| Defend | 防守 / 保卫战 | 关卡玩法类型 / `GameplayState`；亦为战斗模式1「保卫战」；进入阶段可经 ModeSelect 选关，再 Prepare→开战→战斗；见 §3.12。 |
| BattleMode | 战斗模式 | 战斗阶段可选模式：`Defend`（保卫战，模式1）/ `PushMap`（推图战，模式2）；模式2 规则见 §3.14。 |
| BattleModeSelect | 战斗模式选关 | 进入 Defend 阶段后的选模式+选关 UI（UI-013）；模式1→§3.12；模式2确认后→§3.14 Prepare（见 §3.8 D-044）。 |
| PushMap | 推图战 | 关卡玩法类型 / `GameplayState`；亦可作战斗模式2；目标点链占领 + 刷怪点/陷阱/BOSS 通关；复用 Defend 布阵/护盾/失控/士兵战斗；见 §3.14。 |
| PushMapPhase | 推图战子状态 | 阶段内子状态：`Prepare` → `Combat` → `Ended`（与 DefendPhase 对齐；见 §3.14）。 |
| PushMapBattleSettlement | 推图战斗结算 | 胜负均弹（UI-017）：上部胜利/失败、中部耗时与击杀、底中继续；见 §3.14。 |
| PushMapRewardPopup | 推图奖励弹窗 | UI-018：展示本场已入账 Exp+CaptureLoot；继续后打开关卡选择；见 §3.14。 |
| MapId | 地图编号 | 推图战地图 Prefab 逻辑名（≠ LevelId）；多关卡可共用；合法池见 [SPEC_04](SPEC_04_Technical.md)；解析 → `Assets/Prefabs/Maps/{MapId}.prefab`（§3.14）。 |
| ObjectivePoint | 目标点 | 推图战有序推进点（1→2→3…）；士兵自动前往当前目标；见 §3.14。 |
| CaptureZone | 判定圈 | 目标点固定半径占领判定范围；默认半径 2（Prefab 可改）；任一忠诚兵进入当前目标圈 → 立即占领（§3.14）。 |
| Capture | 占领 | 目标点本场判定成功状态；已占领则关联刷怪点本场停刷；可发配置奖励与解锁副本钩子（§3.14）。 |
| AirWall | 空气墙 | 地图 Prefab 阻挡体；敌我士兵均不可进入；支持绕 Y 轴旋转 45°（及轴对齐）（§3.14）。 |
| SpawnPoint | 刷怪点 | 地图 Prefab 上独立编号出生点；怪物种类/数量由 PushMap 关卡刷怪表驱动（§3.14）。 |
| BodyRadius | 占地半径 | 单位 XZ 占地圆半径（世界单位）。怪物来自 `MonsterConfig`；士兵来自 `BodyAppearanceConfig`（按 `AppearanceId`，缺省 `0.1`）。PushMap 刷出散开与 `NavMeshAgent` / MassMove 避障半径共用（§3.12/§3.14，[SPEC_04 §9.13](SPEC_04_Technical.md)/[§9.19](SPEC_04_Technical.md)）。 |
| TrapZone | 陷阱区域 | 地图 Prefab 上独立编号触发区；我方忠诚士兵进入后可激活绑定刷怪点（§3.14）。 |
| BossPoint | BOSS 点 | 地图 Prefab 标记；击杀该点生成的 BOSS 怪物 → PushMap 阶段通关（§3.14）。 |
| AggroMode | 仇恨模式 | 怪物主动/被动 × 移动/原地四态；**异于** `AttackMode`（Melee/Ranged）；见 §3.14、[SPEC_04 §9.19](SPEC_04_Technical.md)。 |
| AlertRadius | 警戒半径 | `AggroMode` 主动态用于发现我方士兵的半径；与 `AttackRange` 并列（§3.14）。 |
| DungeonUnlock | 副本解锁 | PushMap 占领/通关配置的副本 ID 写入存档钩子；副本玩法正文 **TBD**（§3.14）。 |
| CameraFollowMode | 镜头跟随模式 | PushMap Combat 表现层：`Auto`（沿 `CameraFollowPath` 最大投影进度）/ `Manual`（拖拽平移）；见 §3.14。 |
| CameraFollowPath | 镜头跟随轨 | PushMap 地图 Prefab 上的虚拟推进折线；作者摆起点/拐弯/终点，相邻路点间按世界 XZ 直线按间距采样；镜头对准折线上的点，不对准士兵 Transform；见 §3.14。 |
| CameraPathProgress | 镜头轨进度 | 折线弧长参数 `s∈[0,1]`；Auto 取存活忠诚兵投影最大值；领头失效则回退；见 §3.14。 |
| ResumeFollow | 恢复跟随 | PushMap 手动模式下底中按钮；点击回到 `Auto`；见 §3.14。 |
| FollowDeadzone | 跟随死区 | Auto 世界 XZ 半径 0.15；圈内忽略目标小幅位移；见 §3.14。 |
| FollowSmoothTime | 跟随缓动时间 | Auto 超出死区后 XZ SmoothDamp 时间 0.25s；见 §3.14。 |
| DamagePopup | 伤害飘字 | PushMap 命中成功后在**被击目标**头顶显示单次伤害文本（格式 `-受伤值`）；敌我字号均为 **12**（怪红 / 兵白）；0.5s 内 `position.z` 相对起点 +0→+0.5 后销毁；见 §3.14。 |
| HitFlash | 受伤闪烁 | PushMap 命中成功后目标模型临时亮色闪烁；怪亮红、兵亮白；共 2 次×0.1s 紧接中间不灭（≈连续亮 0.2s）；过程中再受伤则刷新；见 §3.14。 |
| AllyFootCircle | 友军脚下圈 | Defend / PushMap Combat 中**忠诚存活**士兵脚下绿色描边圆 + 内部黑色半透明（α=**160/255**）；世界半径=`BodyRadius`；localPos Y=-0.05 Z=-0.2；rotation X=**-30**；Order In Layer=`1`；随士兵移动；叛变/死亡隐藏；见 §3.12 / §3.14、[SPEC_04 §9.7](SPEC_04_Technical.md)。 |
| DefendPhase | 防守子状态 | 阶段内子状态：`ModeSelect`（选模式/关卡，若启用）→ `Prepare`（准备）→ `Combat`（战斗中）→ `Ended`（已结束）。 |
| StartBattle | 开战 | 准备态 UI 按钮；点击后进入 `Combat` 并部署单位（§3.12）。 |
| BattleMap | 战斗地图 | 防守阶段地图；逻辑为连续可走空间（非格子）；表现与 DigMap 同为 Isometric Tilemap，可共用 `Ground_*`（§3.12）。 |
| BattleProtagonist | 战斗主角 | 战斗中地图中央的主角实体；与挖坟 `Digger` 区分；Defend 中以 **护盾（Shield）** 代替 HP 承受普通攻击（§3.12）；外观为 Character Creator **烘焙整角**，见 [SPEC_04 §15](SPEC_04_Technical.md)。 |
| Shield | 护盾 | Defend 战斗中主角可承受 **普通攻击** 的次数；开战时 `Shield =` 当前等级行 `ProtagonistMaxHP`；归零 → LevelFailure（§3.12）。 |
| Monster | 怪物 | 防守战斗敌方单位；参数见 `MonsterConfig`；出现位置可为地图内或外围（§3.12，[SPEC_04 §9.19](SPEC_04_Technical.md)）；外观为 Character Creator **烘焙整角**（`ModelId` Prefab），见 [SPEC_04 §15](SPEC_04_Technical.md)。 |
| MonsterConfig | 怪物配置表 | 怪物 ID → 模型/名称/目标选择/AttackMode/`MonsterType`/AggroMode/警戒半径/血量/移速/攻击力/攻速/技能/掉落（§3.12、§3.14，[SPEC_04 §9.19](SPEC_04_Technical.md)）。 |
| MonsterType | 怪物类型 | `MonsterConfig` 原型标签：`1`=普通 / `2`=精英 / `3`=BOSS；供后续士兵技能等判定；**异于** PushMap 刷怪行 `IsBoss`（通关目标）；本批不驱动（[SPEC_04 §9.19](SPEC_04_Technical.md)）。 |
| Wave | 波次 | 防守刷怪：由 `WaveSpawnConfig` 在同一 `WaveConfigId` 下的刷怪行集合定义；全部行触发且全灭为阶段胜利条件之一（§3.12）。 |
| WaveSpawnConfig | 刷怪波次配置表 | WaveConfigId + 出怪顺序/剩余秒/怪物/数量/位置/方式（§3.12，[SPEC_04 §9.18](SPEC_04_Technical.md)）。 |
| WaveConfigId | 波次配置ID | 防守玩法配置指向的刷怪表分组键（§3.12，[SPEC_04 §9.7](SPEC_04_Technical.md)）。 |
| RemainingCombatSeconds | 战斗剩余秒 | Defend 开战倒计时剩余整秒；与刷怪行 `SpawnRemainingSeconds` 相等时激活该行（§3.12）。 |
| TargetSelect | 目标选择 | 怪物选目标模式：`Nearest` / `PreferWarrior` / `PreferProtagonist`（§3.12 / `MonsterConfig`）。 |
| AttackPriority | 攻击优先级 | **士兵灵魂**配置字段（§3.11 / `SoulConfig`）；枚举与怪物 `TargetSelect` 对齐：`Nearest` \| `PreferWarrior` \| `PreferProtagonist`；**本批不驱动**选目标（默认见 `EngageZone` 内最近敌人）。怪物侧选目标用 `TargetSelect`（§3.12）。 |
| EngageZone | 选敌区 | BattleMap 预制体上比地图稍小的 **IsoDiamond**（XZ 菱形）；非叛变士兵仅在此区内选最近敌人；区外不可选（§3.12）。 |
| FormationHome | 布阵原点 | 开战部署锁定的该士兵布阵世界坐标；无 EngageZone 目标时非叛变士兵自动返回此处（§3.12）。 |
| AttackRange | 攻击距离 | 近战/远程均有；须进入目标攻击距离内才开始攻击动作（§3.12）。 |
| CombatDead | 战斗死亡 | 士兵 HP≤0 且无宝石时的战场状态；可被战斗中复活技能拉起；**不**触发物资去向（§3.11、§3.12）。 |
| PermanentDeath | 彻底死亡 | 实例移除（含 `SoldierSkills`）+ 布阵位空 + 执行物资去向；结算于阶段胜利 `Ended` / LevelFailure，或带宝石士兵 HP≤0 立即触发（§3.11、§3.12）。 |
| AttackWindup | 攻击前摇 | 近战命中确认前的计时阶段；结束时若目标仍有效且在距内则结算（§3.12）。 |
| HitConfirm | 命中确认 | 规则层确认伤害结算的时刻（近战=前摇结束；远程=弹道命中）（§3.12）。 |
| TargetRetargetInterval | 目标修正间隔 | 怪物与士兵重算可攻击目的地 / AttackSlot 的间隔；暂定 **1s**，可配置（§3.12）。 |
| MassCombatPathing | 大规模战斗寻路 | 双方约 200 人量级移动栈：共享目标用 **FlowField**；追击/攻击用 **AttackSlot** + 本地左右绕行；静态障碍（含 AirWall）进场；见 §3.12「大规模战斗寻路」、[SPEC_04 §9.7](SPEC_04_Technical.md)。 |
| FlowField | 流场 | 针对共享目的地（如 PushMap `CurrentObjective`）预计算的格点方向场；同目标单位采样同一场，禁止每人独立全图 A*（§3.12）。 |
| AttackSlot | 攻击槽位 | 围绕被攻击目标、落在 `AttackRange` 环上的可站立世界坐标；单位认领后作为到达点；目标移动/槽失效时按 `TargetRetargetInterval` 重算（§3.12）。 |
| LocalDetour | 本地绕行 | 默认直线趋近 `DesiredDestination`；前方被友军阻挡时左/右短探测选一侧绕行；**不**把友军 Bake/Carve 进 NavMesh（§3.12）。 |
| DesiredDestination | 期望目的地 | 移动层当前趋近的世界坐标：`Objective` / `FormationHome` / `AttackSlot` / 流场采样引导点之一（§3.12）。 |
| GoalKind | 目的地种类 | `Objective` \| `FormationHome` \| `AttackSlot` \| `ChaseAnchor`；规则层输出目标实体 + GoalKind，移动服务解析坐标（§3.12）。 |
| CombatMoveMode | 战斗移动模式 | `Chase` \| `Surround` \| `Sweep`（**无 Follow**）；叠在 GoalKind 上的走法策略（§3.12 方案 B+）。 |
| SoftCollision | 单位软碰撞 | XZ 圆足迹 + 邻域排斥；集中服务解算，替代规模向硬刚体/全员 RVO（§3.12 方案 B+）。 |
| SurroundGap | 包围缺口 | Surround 模式下 AttackSlot 环上跳过的扇区（方向+角宽）（§3.12）。 |
| LevelFailure | 关卡失败 | Defend 中护盾归零等触发的关卡级失败；与 VictorySettlement 互斥（§3.12）。 |

新增术语同步一行到 [CONTEXT.md](CONTEXT.md)。

### English

| Term (EN) | ZH | Definition |
|-----------|-----|------------|
| GameplayState | 玩法状态 | In-session main state enum: `Dig`, `AutoManufacture` (Mode2 pipeline), `UpgradeManufacture` (was placeholder `SewRevive`), `Defend`, `PushMap`. During a Level, set by the current stage's gameplay type (§3.9); shell default placeholder remains Dig. |
| SaveSlot | 存档槽 | Fixed local slots; this version **3 slots** (indices 0–2). Empty → create; occupied → enter or delete. Occupied flag is shared per slot; WarriorPool / BattleFormation / DungeonUnlocks progress is isolated per slot **and** `CampaignMode` (§3.4). |
| CampaignMode | 玩法模式 | Save-level play gate: `Mode1` / `Mode2`. Every create/enter goes through `CampaignModeSelect`; progress fully isolated per mode in the same slot; Mode2 uses a separate config-table root ([SPEC_04 §14](SPEC_04_Technical.md)). Mode2 shares Dig/Defend mechanics with Mode1; **soldier manufacture**: Mode1 manual (§3.11), Mode2 AutoManufacture (§3.15). **Do not confuse** with `BattleMode` (Defend/PushMap). |
| AutoManufacture | 自动制造 | Mode2 stage type / `GameplayState`: after DigStageSummary confirm; auto pick parts → craft → temp warehouse → clear formation then deploy by class zones; then enter `UpgradeManufacture` (§3.15). |
| TempWarriorWarehouse | 临时仓库 | AutoManufacture batch buffer: crafted soldiers enter here first; after the batch finishes, flush to `WarriorPool` and auto-deploy (§3.15). |
| PrimaryHand | 主要手 | Arm BodyPart with `IsPrimaryHand=1`; Mode2 selection anchor and primary ClassRestrict source (§3.15). |
| SecondaryHand | 次要手 | Arm with `IsPrimaryHand=0`; pairs with PrimaryHand; class from ClassRestrict intersection (else PrimaryHand pool only) (§3.15). |
| ClassRestrict | 职业限定 | Multi-`ClassId` list on BodyPart (`\|`-separated); Mode2 class from hand intersection/fallback (§3.15, [SPEC_04 §9.12](SPEC_04_Technical.md)). |
| BodyPrimaryStat | 躯体主属性 | BodyPart field: exactly one of `Strength` / `Agility` / `Intelligence`; Mode2 matcher when picking remaining parts (**not** Class `PrimaryStat`) (§3.15). |
| ApproxBodyLevel | 近似品质 | Mode2 pick: `|ΔBodyLevel| ≤ 1` vs anchor; sort higher → same → lower-by-1 (§3.15). |
| PlacementOrder | 放置排序 | `ClassConfig` field (≥1); AutoManufacture deploys classes in ascending order (§3.15, [SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| FormationClassZone | 职业布阵区 | Authoring zone on formation map Prefab per ClassId (**IsoDiamond**: `HalfExtents` = vertex-to-center; same shape as WalkSurface; no Y rotation); auto-deploy lands there with separation (§3.15, [SPEC_04 §13](SPEC_04_Technical.md)). |
| MagicBook | 魔法书 | Protagonist special equipment; Mode2 applies at UI-016 Step2 **per-slot pulse peak**; includes Restore (`RaceWeightPick`), Warrior Enhance (`StatMul`/`Primary`), Soldier skill level (`SoldierSkillLevelAdd`), class advance (`ForceClass`) (§3.15, [SPEC_04 §9.24](SPEC_04_Technical.md)). **Distinct from** §3.16 `ProtagonistEquipment`. |
| MagicBookConfig | 魔法书配置表 | MagicBookId → IsUnique, IsProbabilistic (chance-trigger marker), EffectPhase, EffectPayload, EffectParams, Icon, name, description (§3.15, [SPEC_04 §9.24](SPEC_04_Technical.md)). |
| SpecialEquipSlot | 特殊装备槽 | Default **6** protagonist slots for MagicBooks; same book stackable unless `IsUnique=1` (§3.15). |
| ProtagonistEquipment | 主角装备 | Leveling protagonist gear; owned-in-warehouse applies at current level; same-Id convert Exp / common Exp upgrade; **parallel** to MagicBook, material Warehouse, soldier ExtraEquipment (§3.16, [SPEC_04 §9.25](SPEC_04_Technical.md)). |
| ProtagonistEquipmentWarehouse | 主角装备仓库 | Save-scoped status warehouse of `OwnedEquip[]`; unlimited distinct kinds; at most one entry per `EquipId` (§3.16). |
| ProtagonistEquipmentConfig | 主角装备配置表 | Composite PK `EquipId`+`EquipLevel` → name/icon/ExpToNext/ConvertExp/domain/effect/desc (§3.16, [SPEC_04 §9.25](SPEC_04_Technical.md)). |
| EquipCommonExp | 装备公共经验 | Independent Exp pool for protagonist gear upgrades only; unrelated to `LifetimeExperience` (§3.16). |
| OwnedEquip | 已拥有装备实例 | One warehouse entry: `EquipId`, `Level`, `CurrentExp` (§3.16). |
| EquipEffectDomain | 装备生效功能 | Gear effect domain enum: `Dig` \| `SoldierManufacture` \| `Combat` (multi-value ok; §3.16). |
| EffectPhase | 生效环节 | MagicBook trigger enum: at least `SoldierManufacture` / `Combat`; Mode2 manufacture books apply on UI-016 Step2 per-slot beat (§3.15). |
| EffectPayload | MagicBook effect code | Registered PascalCase token (e.g. `RaceWeightPick`); empty = none; unknown token empty-apply + warn (§3.15, [SPEC_04 §9.24](SPEC_04_Technical.md)). |
| EffectParams | MagicBook effect params | `Key=Value` or `Key=Value\|…`; empty = none/defaults (§3.15, [SPEC_04 §9.24](SPEC_04_Technical.md)). |
| ManufactureRecord | 制造记录 | Mode2 UM read-only popup: last AutoManufacture batch soldier summaries (name/race/class); entry to the right of Formation (UI-015 / §3.15). |
| AutoManufactureBatchRecord | 自动制造批次记录 | Save-scoped last-batch `WarriorId` list; next batch overwrites; persist per slot + CampaignMode (§3.15, [SPEC_04 §6](SPEC_04_Technical.md)). |
| AutoManufacturePresentation | AutoManufacture presentation | Mode2 AutoManufacture stage presentation (UI-016): after rule batch play Step1–2, then UM + auto-open Formation (§3.15). |
| CampaignModeSelect | 玩法模式选择 | Mode-pick UI after Create/Enter (UI-014); cancel stays on save select (§3.2, §3.6). |
| InSaveShell | 进档壳层 | Persistent shell after entering a save **with a chosen `CampaignMode`**: hosts current `GameplayState` placeholder and floating Tools entry. |
| ToolsPanel | 工具面板 | Demo settings/debug shell UI opened by floating Tools. This version: Settings + Level (Level → pick list) + Demo GM Grant Protagonist Equipment / Grant MagicBook (→ GmGrantListPanel, §3.5 / UI-019 / D-061). |
| Level | 关卡 | Multi-stage flow defined by Level Operation table; each stage has gameplay type + config ID (§3.9; UM stage ConfigId **ignored**). Tools Level opens LevelSelectPanel (distinct LevelIds → Stage 1); scene binding **TBD**. |
| LevelOperation | 关卡运作 | One Level Operation row: LevelId + StageNumber + GameplayType + GameplayConfigId. |
| DigGameplayConfig | 挖坟配置 | One Dig config row: duration, initial grave count, spawn rate, quality weights (zero-weight entries dropped) (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| Grave | 坟墓 | Spawnable Dig-map entity with Grave Quality Id; placement must avoid existing graves and obstacles. |
| VictorySettlement | 胜利结算 | Level-level settlement feedback after the **last** stage ends. |
| DigMap | 挖坟地图 | Presentation uses Unity **Isometric Tilemap** diamond floor tiles (orthographic / no perspective); **logic footprint IsoDiamond** (XZ Manhattan diamond aligned to tile silhouette; continuous placeable, not a cell grid); presentation Prefab logical names `Ground_01`…`Ground_05` (`DigMapId`). |
| Digger | 挖坟主角 | Dig stage does **not** spawn a map avatar; protagonist is shown as Dig HUD **top-left 60×60** portrait (§3.10); `Digger` Prefab may remain in Catalog/art pipeline — [SPEC_04 §15](SPEC_04_Technical.md). |
| DigAction | 挖掘流程 | Circle cursor dwell ≥0.2s over diggable graves intersecting DigHitShape triggers dig; **all** eligible graves start DigAction **in parallel**; each resolves damage after its own `DigActionDuration`; busy grave cannot re-trigger (§3.10). |
| DigObstacle | 挖坟障碍物 | Dig-stage obstacles are **only** uncleared Graves; circle obstacle radius on Grave Prefabs (§3.10). **No** map-center protagonist obstacle. |
| DigHitShape | Dig hit shape | Offline-baked local-XZ convex hull on Grave Prefab (silhouette-approx); cursor-circle intersection; separate from DigObstacle (§3.10). |
| DigProtagonistCapabilities | 挖坟主角能力 | Save-slot protagonist derived stats: dig damage, Dig stage duration bonus, duration-reduction sum, cursor radius, diggable quality set, grave spawn-weight bonuses; recalculated additively from **tech-tree** learns **and** protagonist gear whose `EffectDomain` includes `Dig` (§3.10, §3.13, §3.16). |
| GraveHP | 坟墓血量 | Current/max HP; maxHP from GraveQualityConfig; 0 HP → dig success + reward (§3.10). |
| GraveIconStyle | 坟墓图标样式 | By remaining HP%: >65% style1; 30%–65% style2; <30% style3 (§3.10). |
| GraveQualityConfig | 坟墓品质定义表 | Quality Id → maxHP, loot, etc.; referenced by Dig spawn weights (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| DigReward | 挖掘奖励 | Reward icon spawned at dig-success anim center when HP hits 0; flies to Dig HUD top-left protagonist portrait, credits on arrival, then disappears (§3.10). |
| DigStageSummary | 挖坟阶段汇总 | Popup after Dig effective duration hits 0: aggregate rewards earned this stage by type only; no extra grants; body-part lines `{DisplayName} Lv{BodyLevel} × count`; top-right "X" confirms (§3.10, UI-011). |
| Warehouse | 仓库 | Per-SaveSlot material warehouse; unlimited slots and retention; materials stack by type up to 10000 (§3.10). |
| SpiritEssence | 精魂 | Currency; from Dig (LootDrop reserved Id + stack overflow AutoConvert); spent when manufacturing soldiers (§3.10, §3.11). |
| MaterialConfig | 材料配置表 | MaterialId → AutoConvert, AppearanceIconId, AssetPath, WarehouseQualityOutlineId; overflow converts to SpiritEssence via AutoConvert (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| CurrencyConfig | 货币配置表 | CurrencyId → appearance icon / asset path / warehouse quality outline; Spirit reserved Id=`Spirit` (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| UpgradeManufacture | 升级与制造 | Stage gameplay type (formerly `SewRevive`): level-up + manufacture soldiers + battle formation; §3.11. |
| Experience | 经验 | Added to `LifetimeExperience` on **Defend or PushMap stage victory** settlement; not credited on LevelFailure; cumulative threshold → level up (§3.11, §3.12, §3.14). |
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
| BodyPartConfig | 躯体材料配置表 | BodyPartId → item name (`DisplayName`) / level/slot/race/ControlPower/SpiritCost/StatBonus/AutoConvert/desc/art (§3.11, [SPEC_04 §9.12](SPEC_04_Technical.md)). |
| BodySlot | 躯体槽类型 | `Head` / `Torso` / `Arm` / `Leg` (§3.11). |
| BodyLevel | 躯体等级 | BodyPart field; mean of filled parts drives appearance level (§3.11). |
| StatBonus | 增加的属性值 | BodyPart flat-stat string; `Base(S)=Σ StatBonus(S)` (§3.11). |
| Body | 躯体 | Set of BodyParts at manufacture; `Base(S)=Σ StatBonus(S)`; part `RaceId`s weight-pick Race; contributes ControlPowerCost (§3.11). |
| BaseStats | 基础属性 | Sum of filled BodyPart `StatBonus` per dim: HP, MoveSpeed, Strength, Agility, Intelligence; after StaticStat/FinalStat, derives attack / ASPD / CD / MaxHP (§3.11, §3.12). |
| StaticStat | 静态属性 | Manufacture / formation UI: `max(0, Base+Equip+Base×GemMult+Base×RaceAdjust)`; excludes `SkillBuff` (§3.11). |
| PrimaryStat | 主属性 | Class field: `Strength` / `Agility` / `Intelligence`; selects which dim feeds NormalAttackPower (§3.11, §3.12, [SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| Class | 职业 | From instance `ClassId` (placed soul's ClassId when present; else forced `Class_Servants`); supplies `ClassName`, `PrimaryStat`, and five-dim→combat-param convert coeffs (§3.11, §3.12, [SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| ClassId | 职业ID | Class primary key; from placed soul when present; else forced `Class_Servants`; written to soldier instance at manufacture (§3.11, [SPEC_04 §9.9](SPEC_04_Technical.md) / [§9.9b](SPEC_04_Technical.md)). |
| ClassConfig | 职业配置表 | ClassId → ClassName, ClassLevel (display grade), BaseClass (reserved), PrimaryStat, CombatConvertCoeffs (`Key_Value|…`), AttackRange / windup / projectile / timeout (§3.11, [SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| ClassLevel | Class level | `ClassConfig` display field (quality grade); UI-016 soldier card shows `Lv.{ClassLevel}` under class name; **not** used in combat/manufacture math ([SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| BaseClass | Base class | `ClassConfig` field; CSV Chinese Warrior/Archer/Mage/Thief literals; empty/illegal → `Unspecified`; **reserved** for future MagicBook conditions; **not** used in naming / appearance / `PrimaryStat` / combat ([SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| BodyLife | 躯体生命 | `Base(MaxHP)+Equip(MaxHP)`; locked at manufacture; no Gem/Race/Buff amplify on HP dim; feeds soldier MaxHP formula (§3.11). |
| CombatConstantConfig | 战斗常量表 | Global combat-formula defaults (`ConstantKey`→`Value`); incl. `NormalAttackPrimaryMult` etc. and `MaxHpStrengthMult`; Class `CombatConvertCoeffs` missing keys fall back here (§3.11, §3.12, [SPEC_04 §9.20b](SPEC_04_Technical.md)). |
| NormalAttackPower | 普通攻击值 | `Primary × NormalAttackPrimaryMult` (class override else constants table; sample default 15); on hit, subtract from monster HP directly (no armor this batch) (§3.12). |
| AttackSpeed | 攻击速度 | Attacks/sec: `AttackSpeedBase+AttackSpeedAgiDiv/max(Agi,1)` (same coeff source); attack-start interval = `1/AttackSpeed` (§3.12). |
| MaxHpStrengthMult | 血量力量系数 | Constants-table key; `MaxHP=ceil(BodyLife+Str×this)`; sample default **3** (§3.11). |
| BodyAppearance | 躯体外观 | Preset overall look; picked by avg BodyLevel + finalized Race + class ClassName (§3.11, [SPEC_04 §9.13](SPEC_04_Technical.md)); assets are Character Creator **baked whole-character** Prefabs — [SPEC_04 §15](SPEC_04_Technical.md). |
| BodyAppearanceConfig | 躯体外观配置表 | AppearanceId → AppearanceLevel / RaceId / ClassAffinity / Description / IsFallback / `BodyRadius` (§3.11, [SPEC_04 §9.13](SPEC_04_Technical.md)). |
| IsFallback | 保底外形 | Appearance field; `1` = race fallback; at most one per RaceId; used when level+race matches but class affinity does not; when set A (level+race) is empty, rewrite to `Race_Undead` then re-pick appearance (§3.11). |
| Race | 种族 | Default: all filled BodyPart `RaceId`s identical → that race, else `Race_Undead`; Mode2 with Restore book → weight-1 pick; one race per soldier; five-dim `RaceAdjustCoeff`; config via `RaceConfig` (§3.11/§3.15, [SPEC_04 §9.11](SPEC_04_Technical.md)). |
| RaceConfig | 种族配置表 | RaceId → display name, five-dimensional race adjust coeffs (§3.11, [SPEC_04 §9.11](SPEC_04_Technical.md)). |
| RaceAdjustCoeff | 种族属性调整系数 | Five dims (one per BaseStat); missing dim = 0; may be +/-; used as `BaseStat × RaceAdjustCoeff`; does **not** add to ControlPowerCost alone (§3.11). |
| Soul | 灵魂 | Manufacture slot **optional**; if filled, consume that row; if empty, instance `SoulId=Soul_00` (system default), AttackMode/skills/priority/MoveStyle/soul Spirit·Control costs from `Soul_00`, and **force** `ClassId=Class_Servants`; does **not** rewrite Strength/Agility/Intelligence; config via `SoulConfig` (§3.11, [SPEC_04 §9.9](SPEC_04_Technical.md)). |
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
| BattleFormation | 战斗布阵 | Assign soldiers to battlefield; persists soldier Id, position, remaining HP; editable in §3.11 and Defend / PushMap `Prepare` on the same dataset (§3.11, §3.12, §3.14). |
| Defend | 防守 / 保卫战 | Stage type / `GameplayState`; also BattleMode 1「保卫战」; enter stage may ModeSelect then Prepare → StartBattle → Combat; §3.12. |
| BattleMode | 战斗模式 | Battle-stage modes: `Defend` (Mode1) / `PushMap` (Mode2); Mode2 rules in §3.14. |
| BattleModeSelect | 战斗模式选关 | Mode+Level select UI after entering Defend (UI-013); Mode1→§3.12; Mode2 confirm→§3.14 Prepare (§3.8 D-044). |
| PushMap | 推图战 | Stage type / `GameplayState`; also BattleMode 2; objective capture + spawn/trap/Boss clear; reuses Defend formation/Shield/LOC/WarriorCombat; §3.14. |
| PushMapPhase | 推图战子状态 | In-stage phases: `Prepare` → `Combat` → `Ended` (aligned with DefendPhase; §3.14). |
| PushMapBattleSettlement | PushMap battle settlement | Always on win/lose (UI-017): result + time + kills + Continue; §3.14. |
| PushMapRewardPopup | PushMap reward popup | UI-018: show credited Exp+CaptureLoot; Continue → LevelSelect; §3.14. |
| MapId | 地图编号 | PushMap map Prefab logical name (≠ LevelId); shared across levels; resolve → `Assets/Prefabs/Maps/{MapId}.prefab` (§3.14). |
| ObjectivePoint | 目标点 | Ordered PushMap push points (1→2→3…); soldiers auto-advance to current; §3.14. |
| CaptureZone | 判定圈 | Fixed-radius capture circle; default radius 2; any loyal soldier entering current zone → immediate Capture (§3.14). |
| Capture | 占领 | Objective captured this battle; linked spawns stop; may grant loot + dungeon unlock hook (§3.14). |
| AirWall | 空气墙 | Prefab blocker; neither faction may enter; Y-rotation 45° supported (§3.14). |
| SpawnPoint | 刷怪点 | Numbered Prefab spawn; monsters from PushMap spawn table (§3.14). |
| BodyRadius | 占地半径 | Unit XZ footprint radius (world units). Monsters: `MonsterConfig`; soldiers: `BodyAppearanceConfig` (by `AppearanceId`, default `0.1`). Shared by PushMap spawn spread and `NavMeshAgent` / MassMove avoidance (§3.12/§3.14, [SPEC_04 §9.13](SPEC_04_Technical.md)/[§9.19](SPEC_04_Technical.md)). |
| TrapZone | 陷阱区域 | Numbered Prefab zone; loyal soldier enter triggers bound SpawnPoints (§3.14). |
| BossPoint | BOSS 点 | Prefab marker; kill Boss spawned here → PushMap stage clear (§3.14). |
| AggroMode | 仇恨模式 | Monster active/passive × chase/stationary; **not** AttackMode (Melee/Ranged); §3.14 / SPEC_04 §9.19. |
| AlertRadius | 警戒半径 | AggroMode active detect radius; alongside AttackRange (§3.14). |
| DungeonUnlock | 副本解锁 | Save-slot unlock hook from PushMap config; dungeon gameplay **TBD** (§3.14). |
| CameraFollowMode | 镜头跟随模式 | PushMap Combat presentation: `Auto` (max projection on `CameraFollowPath`) / `Manual` (drag pan); §3.14. |
| CameraFollowPath | 镜头跟随轨 | Virtual advance polyline on the PushMap map Prefab; author start/turns/end, bake world-XZ straight samples between adjacent waypoints; camera looks at a point on the rail, not a soldier Transform; §3.14. |
| CameraPathProgress | 镜头轨进度 | Polyline arc-length `s∈[0,1]`; Auto = max projection of living loyal soldiers; retreats if the lead drops; §3.14. |
| ResumeFollow | 恢复跟随 | PushMap Manual-only bottom-center button → back to `Auto`; §3.14. |
| FollowDeadzone | Follow deadzone | Auto world-XZ radius 0.15; ignore small target motion inside; §3.14. |
| FollowSmoothTime | Follow smooth time | Auto XZ SmoothDamp time 0.25s when outside deadzone; §3.14. |
| DamagePopup | 伤害飘字 | PushMap floating damage text above the **hit target** after a successful hit (format `-damage`); font size **12** for both sides (monster red / soldier white); over **0.5s** world `position.z` rises relative start +0→+0.5 then despawn; §3.14. |
| HitFlash | 受伤闪烁 | PushMap hit-flash on the target model after a successful hit; monster bright red, soldier bright white; 2×0.1s pulses back-to-back with no off gap (≈0.2s continuous); refresh if hit again mid-flash; §3.14. |
| AllyFootCircle | 友军脚下圈 | During Defend / PushMap Combat, **loyal living** soldiers show a green-stroke foot circle with black fill α=**160/255**; world radius=`BodyRadius`; localPos Y=-0.05 Z=-0.2; rotation X=**-30**; Order In Layer=`1`; follows the soldier; hide on Rebel / CombatDead; §3.12 / §3.14, [SPEC_04 §9.7](SPEC_04_Technical.md). |
| DefendPhase | 防守子状态 | In-stage phases: `ModeSelect` (if enabled) → `Prepare` → `Combat` → `Ended`. |
| StartBattle | 开战 | Prepare-phase UI button; click → `Combat` and deploy units (§3.12). |
| BattleMap | 战斗地图 | Defend-stage map; continuous walkable space (not a grid); presentation shares DigMap Isometric Tilemap via `Ground_*` (§3.12). |
| BattleProtagonist | 战斗主角 | Protagonist entity at BattleMap center; distinct from Dig `Digger`; in Defend uses **Shield** instead of HP for normal attacks (§3.12); visuals are Character Creator **baked whole characters** — [SPEC_04 §15](SPEC_04_Technical.md). |
| Shield | 护盾 | Hit-count capacity for **normal attacks** on the protagonist in Defend; on StartBattle `Shield =` current level row `ProtagonistMaxHP`; `Shield ≤ 0` → LevelFailure (§3.12). |
| Monster | 怪物 | Defend enemy unit; params in `MonsterConfig`; appear location InsideMap or OutsideMap (§3.12, [SPEC_04 §9.19](SPEC_04_Technical.md)); visuals are Character Creator **baked whole characters** (`ModelId` Prefab) — [SPEC_04 §15](SPEC_04_Technical.md). |
| MonsterConfig | 怪物配置表 | MonsterId → model/name/target select/AttackMode/`MonsterType`/AggroMode/AlertRadius/HP/move/attack power/speed/skills/loot (§3.12, §3.14, [SPEC_04 §9.19](SPEC_04_Technical.md)). |
| MonsterType | 怪物类型 | `MonsterConfig` archetype tag: `1`=Normal / `2`=Elite / `3`=Boss; for later soldier-skill filters; **not** PushMap spawn-row `IsBoss` (clear target); unused this batch ([SPEC_04 §9.19](SPEC_04_Technical.md)). |
| Wave | 波次 | Defend spawn set: all `WaveSpawnConfig` rows under one `WaveConfigId`; all rows fired + all killed is part of stage victory (§3.12). |
| WaveSpawnConfig | 刷怪波次配置表 | WaveConfigId + spawn order / remaining seconds / monster / count / location / mode (§3.12, [SPEC_04 §9.18](SPEC_04_Technical.md)). |
| WaveConfigId | 波次配置ID | Grouping key for spawn rows referenced by DefendGameplayConfig (§3.12, [SPEC_04 §9.7](SPEC_04_Technical.md)). |
| RemainingCombatSeconds | 战斗剩余秒 | Whole-second Defend combat countdown remaining; activates spawn rows when equal to `SpawnRemainingSeconds` (§3.12). |
| TargetSelect | 目标选择 | Monster targeting mode: `Nearest` / `PreferWarrior` / `PreferProtagonist` (§3.12 / `MonsterConfig`). |
| AttackPriority | 攻击优先级 | **Soldier Soul** field (§3.11 / `SoulConfig`); same enum as monster `TargetSelect`: `Nearest` \| `PreferWarrior` \| `PreferProtagonist`; **does not drive** targeting this batch (default = nearest enemy inside `EngageZone`). Monster targeting uses `TargetSelect` (§3.12). |
| EngageZone | 选敌区 | **IsoDiamond** (XZ diamond) on BattleMap Prefab, slightly smaller than the map; non-Rebel soldiers pick nearest enemy **only inside** this zone; outside = not selectable (§3.12). |
| FormationHome | Formation home | World position locked at StartBattle deploy for that soldier; loyal soldiers auto-return here when EngageZone has no target (§3.12). |
| AttackRange | 攻击距离 | Both Melee and Ranged; must enter target AttackRange before starting attack action (§3.12). |
| CombatDead | 战斗死亡 | Battlefield state when soldier HP≤0 and has no gems; revivable by in-combat revive skills; **does not** trigger material fate (§3.11, §3.12). |
| PermanentDeath | 彻底死亡 | Remove instance + clear formation slot + run material fate; settled on stage victory `Ended` / LevelFailure, or immediately when a gemmed soldier hits HP≤0 (§3.11, §3.12). |
| AttackWindup | 攻击前摇 | Timed phase before melee HitConfirm; on end, settle if target still valid and in range (§3.12). |
| HitConfirm | 命中确认 | Rules-layer moment damage settles (melee = windup end; ranged = projectile hit) (§3.12). |
| TargetRetargetInterval | 目标修正间隔 | Interval for monsters **and soldiers** to recompute attackable destination / AttackSlot; provisional **1s**, configurable (§3.12). |
| MassCombatPathing | 大规模战斗寻路 | ~200-per-side move stack: shared goals use **FlowField**; chase/attack use **AttackSlot** + local L/R detour; static blockers (incl. AirWall) baked into field; §3.12 Mass Combat Pathing, [SPEC_04 §9.7](SPEC_04_Technical.md). |
| FlowField | 流场 | Grid direction field for a shared destination (e.g. PushMap `CurrentObjective`); same-goal units sample one field — no per-unit full-map A* (§3.12). |
| AttackSlot | 攻击槽位 | Standable world point on the `AttackRange` ring around the attack target; claimed as arrival; recomputed on move/invalid at `TargetRetargetInterval` (§3.12). |
| LocalDetour | 本地绕行 | Default straight-line toward `DesiredDestination`; on friendly block, short L/R probes pick a side; friendlies are **not** NavMesh-baked/carved (§3.12). |
| DesiredDestination | 期望目的地 | World point the move layer seeks: `Objective` / `FormationHome` / `AttackSlot` / flow-field sample (§3.12). |
| GoalKind | 目的地种类 | `Objective` \| `FormationHome` \| `AttackSlot` \| `ChaseAnchor`; rules emit target entity + GoalKind; move service resolves coords (§3.12). |
| CombatMoveMode | 战斗移动模式 | `Chase` \| `Surround` \| `Sweep` (**no Follow**); steer policy layered on GoalKind (§3.12 Approach B+). |
| SoftCollision | 单位软碰撞 | XZ circle footprints + neighbor repulsion; centralized resolve instead of hard RB / all-unit RVO at scale (§3.12 Approach B+). |
| SurroundGap | 包围缺口 | Fan sector skipped on AttackSlot ring under Surround (direction + degrees) (§3.12). |
| LevelFailure | 关卡失败 | Level-level failure (e.g. Shield reaches 0 in Defend); mutually exclusive with VictorySettlement (§3.12). |

Sync glossary rows to [CONTEXT.md](CONTEXT.md).

---

## 3.2 玩家输入与操作（占位）

### 简体中文

**状态：部分定义（Meta 壳）**

| 场景 | 操作 | 说明 |
|------|------|------|
| 存档选择 | 点击空槽「新建」 | 弹出 `CampaignModeSelect`；选定模式后占用该槽并进入进档壳层；取消则不占用、留在存档界面 |
| 存档选择 | 点击占用槽「进入」 | 弹出 `CampaignModeSelect`；选定模式后加载该槽**该模式**进度并进入进档壳层；取消则留在存档界面 |
| 存档选择 | 点击占用槽「删除」 | 须二次确认后清空槽位（含两模式全部进度键），停留在存档界面 |
| 玩法模式选择 | 选「模式1」或「模式2」 | 确认后按所选 `CampaignMode` 进档；两模式同槽进度隔离 |
| 玩法模式选择 | 取消 | 关闭弹窗，不进壳 |
| 进档壳层 | 点击浮动「工具」 | 打开 / 关闭工具面板 |
| 工具面板 | 点击「设置」「关卡」 | 设置→科技树；关卡→LevelSelectPanel |
| 工具面板 | 点击「增加主角装备」「增加魔法书」 | 关闭 ToolsPanel → GmGrantListPanel（UI-019）；点一次发放（装备 `TryAcquire` / 魔法书 `TryEquip`） |
| 三玩法状态 | — | **TBD**（后续专门补充） |

### English

**Status: Partially defined (Meta shell)**

| Context | Action | Notes |
|---------|--------|-------|
| Save select | Create on empty slot | Open `CampaignModeSelect`; on confirm occupy slot and enter InSaveShell; cancel → stay |
| Save select | Enter occupied slot | Open `CampaignModeSelect`; on confirm load **that mode's** slot progress and enter InSaveShell; cancel → stay |
| Save select | Delete occupied slot | Confirm, then clear slot (both modes' keys); stay on save UI |
| CampaignModeSelect | Pick Mode1 or Mode2 | Enter with chosen `CampaignMode`; progress isolated per mode in the same slot |
| CampaignModeSelect | Cancel | Close popup; do not enter shell |
| InSaveShell | Floating Tools | Open / close ToolsPanel |
| ToolsPanel | Settings / Level | Settings → TechTree; Level → LevelSelectPanel (distinct LevelIds) |
| ToolsPanel | Grant Protagonist Equipment / Grant MagicBook | Hide ToolsPanel → GmGrantListPanel (UI-019); one click grants (`TryAcquire` / `TryEquip`) |
| Three gameplay states | — | **TBD** |

---

## 3.3 核心循环

### 简体中文

| 阶段 | 说明 |
|------|------|
| 1. 启动 | 进入存档选择界面（非直接进局） |
| 2. Meta 存档 | 对 3 个固定槽执行新建 / 选择进入 / 删除；新建与进入均经 `CampaignModeSelect`（见 §3.4） |
| 3. 进档壳层 | 选定槽与 `CampaignMode` 后默认 `GameplayState = Dig`（挖坟占位）；显示浮动「工具」（§3.5）；运行时 CSV 根随模式切换（Mode2→`ConfigTables/Mode2/Csv`） |
| 4. 玩法状态 | 当前状态以占位表现可识别；关卡内由阶段玩法类型驱动（§3.9）；壳层内手动切换 **TBD** |
| 5. 关卡 | 规则见 §3.9；按 `LevelOperationConfig` 驱动真实阶段（§3.8 D-010）；工具「关卡」打开列表选关（UI-008） |

交叉引用：[SPEC_02 §3](SPEC_02_GameOverview.md)。

### English

| Stage | Description |
|-------|-------------|
| 1. Boot | Open save-select UI (not direct into gameplay) |
| 2. Meta saves | Create / enter / delete on 3 fixed slots; create/enter always via `CampaignModeSelect` (§3.4) |
| 3. InSaveShell | After slot + `CampaignMode`: default `GameplayState = Dig`; show floating Tools (§3.5); runtime CSV root follows mode (Mode2→`ConfigTables/Mode2/Csv`) |
| 4. Gameplay states | Placeholder must identify current state; in Level, driven by stage gameplay type (§3.9); manual shell switch **TBD** |
| 5. Level | Rules in §3.9; drive real stages via `LevelOperationConfig` (§3.8 D-010); Tools Level opens pick list (UI-008) |

Cross-ref: [SPEC_02 §3](SPEC_02_GameOverview.md).

---

## 3.4 Meta / 存档

### 简体中文

**槽位规则**

| 规则 | 值 |
|------|-----|
| 槽位数量 | 固定 **3**（索引 0、1、2） |
| 空槽 | 可「新建」→ 弹出 `CampaignModeSelect` → 选定后标记占用并进入进档壳层 |
| 占用槽 | 可「选择进入」（同样先 `CampaignModeSelect`）或「删除」 |
| 玩法模式 | 每次新建/进入均须选择 `CampaignMode`（Mode1/Mode2）；取消不进壳 |
| 同槽隔离 | Mode1 与 Mode2 的士兵池 / 布阵 / 副本解锁等进度键**完全隔离**；`Occupied` 按槽共享（任一模式玩过即占用） |
| 删除 | **必须二次确认**；确认后槽变空并清除**两模式**全部进度键；不可恢复（本版） |
| 持久化 | 本地、按槽索引 + `CampaignMode`；至少持久化「是否占用」。**本片已锁定：** 士兵可上阵池（`WarriorPool`）+ 战斗布阵（`BattleFormation`）随槽**与模式**读写（见 [SPEC_04 §6](SPEC_04_Technical.md)）；其余字段（仓库 / 经验 / 科技等）schema 仍 **TBD** |
| Mode2 合成 | Mode2 士兵制造 = **自动制造**（§3.15）；Mode2 UM 关闭手动制造 |

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
| Empty | Create → `CampaignModeSelect` → on confirm mark occupied and enter InSaveShell |
| Occupied | Enter (also via `CampaignModeSelect`) or Delete |
| CampaignMode | Every create/enter must pick Mode1/Mode2; cancel stays on save UI |
| Per-slot isolation | Mode1 vs Mode2 WarriorPool / BattleFormation / DungeonUnlocks keys are **fully isolated**; `Occupied` is shared per slot |
| Delete | **Confirm required**; slot becomes empty and **both modes'** progress keys cleared; no undo (this version) |
| Persistence | Local, by slot index + `CampaignMode`; at least occupied flag. **This slice locked:** deployable soldier pool (`WarriorPool`) + `BattleFormation` read/write per slot **and mode** ([SPEC_04 §6](SPEC_04_Technical.md)); other fields (Warehouse / Exp / Tech, …) schema still **TBD** |
| Mode2 manufacture | Mode2 soldier craft = **AutoManufacture** (§3.15); Mode2 UM hides manual manufacture |

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
| 本期条目 | **设置**（含科技树画布入口，见 §3.13 / UI-012）、**关卡**（打开关卡列表，见 UI-008）、**增加主角装备**、**增加魔法书**（Demo GM，见 UI-019 / D-061） |
| 关卡语义 | 工具「关卡」入口 **不等于** 直接切换三种 `GameplayState`；关卡多阶段规则见 §3.9。点击「关卡」→ 打开 **LevelSelectPanel**（Prefab）：列出当前 `CampaignMode` 已加载的 `Level_LevelOperationConfig` 中全部 **去重 `LevelId`**（同 Id 只显示一行）；点选某行 → `LevelOperationDriver.TryEnterLevel(levelId)`，从该关 **`StageNumber=1`**（升序第一阶段）进入。关列表空则 Toast 提示。 |
| Demo GM：增加主角装备 | 点击 → 关闭 ToolsPanel → 打开 **GmGrantListPanel**：列出当前模式 `ProtagonistEquipmentConfig` **按 EquipId 去重**（取 Level 1 行 `DisplayName`，空则 Id）。点一次 → `ProtagonistEquipmentService.TryAcquire(equipId)`（首次入仓 L1；重复=转化经验）。成功/失败 Toast + 日志；列表保持打开可连点。 |
| Demo GM：增加魔法书 | 点击 → 关闭 ToolsPanel → 同一 **GmGrantListPanel**：列出当前模式 `MagicBookConfig` 全表（`DisplayName`，空则 Id）。点一次 → `SpecialEquipSlotsService.TryEquip(magicBookId)`（装入第一个空槽；**无**独立仓库）。`IsUnique=1` 已装或 6 槽满 → 失败 Toast。Dig HUD「装备战士强化」等 GM **保留**。 |
| Demo Debug：士兵任务标签 | 进档壳 **Debug** 区提供开关（**默认开**）：Defend / PushMap Combat 中士兵脚下 TextMesh 显示当前 `GoalKind` 中文简标（推进 / 回阵 / 追击 / 追击锚）；仅目标类，不含攻击前摇等细态；见 [SPEC_04 §9.7](SPEC_04_Technical.md) |
| 后续条目 | 正式装备仓 UI / 魔法书装配 UI 另专题；其余 TBD。本两项 GM 纳入 §3.8 D-061（P1） |

点击「设置」：进入设置页并承载科技树画布（§3.13）；其它设置项清单仍 **TBD**；科技树画布完整验收本版可选后置。点击「关卡」：关闭工具面板 → 打开 LevelSelectPanel → 点选进入对应关卡 Stage 1。点击「增加主角装备」/「增加魔法书」：关闭工具面板 → GmGrantListPanel → 点一次发放。

### English

| Rule | Notes |
|------|-------|
| Visibility | Floating Tools only inside InSaveShell |
| Open / close | Toggle ToolsPanel via button |
| This version | **Settings** (hosts TechTree canvas, §3.13 / UI-012), **Level** (opens level list, UI-008), **Grant Protagonist Equipment**, **Grant MagicBook** (Demo GM, UI-019 / D-061) |
| Level meaning | Tools Level entry is **not** a direct three-state switch; multi-stage Level rules in §3.9. Level click → **LevelSelectPanel** Prefab: lists all **distinct `LevelId`** from the current `CampaignMode`'s loaded `Level_LevelOperationConfig` (one row per Id); picking a row → `LevelOperationDriver.TryEnterLevel(levelId)` starting at **`StageNumber=1`** (first ascending stage). Empty list → Toast. |
| Demo GM: Grant Protagonist Equipment | Click → hide ToolsPanel → **GmGrantListPanel**: distinct `EquipId` from current-mode `ProtagonistEquipmentConfig` (Level 1 `DisplayName`, else Id). One click → `ProtagonistEquipmentService.TryAcquire(equipId)` (first acquire L1; duplicate converts Exp). Success/fail Toast + log; list stays open. |
| Demo GM: Grant MagicBook | Click → hide ToolsPanel → same **GmGrantListPanel**: all current-mode `MagicBookConfig` rows (`DisplayName`, else Id). One click → `SpecialEquipSlotsService.TryEquip(magicBookId)` (first empty slot; **no** warehouse). Unique already equipped or 6 slots full → fail Toast. Dig HUD GM (e.g. Equip Warrior Enhance) **kept**. |
| Demo Debug: soldier task label | InSaveShell **Debug** toggle (**default on**): during Defend / PushMap Combat, TextMesh under each soldier shows current `GoalKind` short ZH label (advance / home / chase / chase-anchor); goal-kind only — no attack windup detail; see [SPEC_04 §9.7](SPEC_04_Technical.md) |
| Future entries | Formal equipment warehouse UI / MagicBook equip UI later; other TBD. These two GM entries are §3.8 D-061 (P1) |

Settings click → Settings page hosting TechTree canvas (§3.13); other settings items still **TBD**; full TechTree canvas acceptance optional this Demo. Level click → hide Tools → LevelSelectPanel → pick enters that level at Stage 1. Grant Equipment / Grant MagicBook → hide Tools → GmGrantListPanel → one click grants.

---

## 3.6 UI 清单

### 简体中文

| ID | 名称 | 状态 | 说明 |
|----|------|------|------|
| UI-001 | 存档选择 | 已定义（Demo） | 3 槽：新建 / 进入 / 删除（含确认） |
| UI-002 | 浮动工具按钮 | 已定义（Demo） | 进档壳层常驻 |
| UI-003 | 工具面板 | 已定义（Demo） | 含设置、关卡、增加主角装备、增加魔法书（后两项→ UI-019） |
| UI-004 | 挖坟占位屏 | 占位 | 可识别当前为 Dig |
| UI-005 | 升级与制造占位屏 | 占位 | 可识别当前为 UpgradeManufacture（原 SewRevive） |
| UI-006 | 防守占位屏 | 占位 | 可识别当前为 Defend；完整 UI 见 §3.12 |
| UI-007 | 设置页 | 已实现（方案 A） | 自工具面板进入；承载科技树画布（UI-012）；其它设置项 TBD |
| UI-008 | 关卡选择面板 | 已实现（方案 B） | Prefab `LevelSelectPanel`（InSaveShell 子级）；列出当前模式 `LevelOperationConfig` 去重 `LevelId`；点选进入 Stage 1；关闭按钮；验收见 §3.8 D-003 |
| UI-009 | 开战按钮 | 已定义（Demo 流水线） | Defend 准备态；点击 → StartBattle（§3.12）；验收见 §3.8 D-040 |
| UI-010 | 升级与制造主屏 | 已定义（Demo 流水线） | 默认全屏制造区；顶部「GM升级」打开升级 Modal（右上 X 关闭）；底栏库存方格拖拽 +「完成」与其右「布阵」；布阵打开共享 FormationEditor；验收见 §3.8 D-030～D-032 |
| UI-011 | 挖坟阶段汇总 | 已定义（Demo 流水线） | DigStageSummary：本阶段已获奖励按类型汇总；无额外发放；`SummaryRoot` **1020×1150**、`Body` **920×1030**；躯体材料行 `{DisplayName} Lv{BodyLevel} × 数量`（精魂/非躯体仍 `{Id} × 数量`）；`ConfirmButton` 文案「X」、锚 `SummaryRoot` 右上角（语义仍为确认）；确认后接 §3.9；验收见 §3.8 D-020 |
| UI-012 | 科技树画布 | 已实现（方案 A，可选） | 2D 可拖动画布；节点图标+类型框；连线；悬停描述；学习点击；见 §3.13；非 §3.8 P0；学会后 Dig 能力可验 |
| UI-013 | 战斗模式选关 | 已定义（Demo 流水线） | 进入 Defend 阶段后：选 `BattleMode` + 关卡（该模式全部玩法配置）；模式1进保卫战 Prepare；模式2选 `PushMapGameplayConfig` 后进 §3.14 Prepare；验收见 §3.8 D-044 |
| UI-014 | 玩法模式选择 | 已定义（Demo） | 新建/进入存档前：选 `CampaignMode` Mode1/Mode2 或取消；**勿与** UI-013 混淆；验收见 §3.8 D-045 |
| UI-015 | 制造记录弹窗 | 已定义（Demo / Mode2） | Mode2 UM：「布阵」右侧「制造记录」打开只读 Modal；最近一批士兵摘要（名字/种族/职业）；空态「本批无士兵」；Mode1 **无**此入口；验收见 §3.8 D-054 |
| UI-016 | 自动制造演出 | 已定义（Demo / Mode2） | AutoManufacture 阶段：Step1 中央士兵行（150×200，「?」42 + 职业名 32 + 其下 `Lv.{ClassLevel}` 24，横滑）+ 上方 6 魔法书槽（120×160）；Step2 逐兵加强动画/Idle 揭示/每 3 兵加速；Step3 进 UM 后自动开布阵；0 兵跳过；Mode1 **无**；验收见 §3.8 D-055 |
| UI-017 | 推图战斗结算 | 已定义（Demo / PushMap） | 胜负均弹：上部「胜利/失败」；中部「战斗耗时」「击杀怪物总数」；底中「继续」。失败 Continue → LevelSelectPanel；胜利 Continue → UI-018；见 §3.14 |
| UI-018 | 推图奖励弹窗 | 已定义（Demo / PushMap） | 仅展示本场已入账：`StageExpReward` + 占领 `CaptureLoot` 汇总；无额外发放；底中「继续」→ 关闭后打开 LevelSelectPanel；见 §3.14 |
| UI-019 | GM 发放列表 | 已定义（Demo GM） | Prefab `GmGrantListPanel`（InSaveShell 子级；布局对齐 UI-008）；Tools「增加主角装备」/「增加魔法书」打开；按钮文案 DisplayName（空则 Id）；点一次发放；关闭按钮；验收见 §3.8 D-061 |

### English

| ID | Name | Status | Notes |
|----|------|--------|-------|
| UI-001 | Save select | Defined (Demo) | 3 slots: create / enter / delete (confirm) |
| UI-002 | Floating Tools button | Defined (Demo) | InSaveShell |
| UI-003 | ToolsPanel | Defined (Demo) | Settings + Level + Grant Protagonist Equipment + Grant MagicBook (last two → UI-019) |
| UI-004 | Dig placeholder | Placeholder | Identifiable Dig |
| UI-005 | UpgradeManufacture placeholder | Placeholder | Identifiable UpgradeManufacture (was SewRevive) |
| UI-006 | Defend placeholder | Placeholder | Identifiable Defend; full UI in §3.12 |
| UI-007 | Settings page | Done (Approach A) | From Tools; hosts TechTree canvas (UI-012); other settings TBD |
| UI-008 | Level select panel | Done (Approach B) | Prefab `LevelSelectPanel` under InSaveShell; distinct `LevelId` from current-mode `LevelOperationConfig`; pick → Stage 1; close button; accept §3.8 D-003 |
| UI-009 | StartBattle button | Defined (Demo pipeline) | Defend Prepare; click → StartBattle (§3.12); accept §3.8 D-040 |
| UI-010 | UpgradeManufacture main screen | Defined (Demo pipeline) | Full-screen manufacture by default; top "GM Upgrade" opens upgrade Modal (top-right X closes); bottom inventory square bar + drag + Complete with Formation to its right; opens shared FormationEditor; accept §3.8 D-030–D-032 |
| UI-011 | Dig stage summary | Defined (Demo pipeline) | DigStageSummary aggregate only; `SummaryRoot` **1020×1150**, `Body` **920×1030**; body-part lines `{DisplayName} Lv{BodyLevel} × count` (Spirit/non-body still `{Id} × count`); `ConfirmButton` label "X", top-right of `SummaryRoot` (still confirms); confirm → §3.9; accept §3.8 D-020 |
| UI-012 | TechTree canvas | Done (Approach A, optional) | 2D pannable canvas; §3.13; not §3.8 P0; Dig caps verifiable after learn |
| UI-013 | Battle mode/level select | Defined (Demo pipeline) | After entering Defend: pick `BattleMode` + level (all configs for mode); Mode1 → Defend Prepare; Mode2 pick `PushMapGameplayConfig` → §3.14 Prepare; accept §3.8 D-044 |
| UI-014 | Campaign mode select | Defined (Demo) | Before create/enter save: pick `CampaignMode` Mode1/Mode2 or cancel; **not** UI-013; accept §3.8 D-045 |
| UI-015 | Manufacture record popup | Defined (Demo / Mode2) | Mode2 UM: "Manufacture Record" to the right of Formation opens read-only Modal; last-batch summaries (name/race/class); empty 「本批无士兵」; Mode1 has **no** entry; accept §3.8 D-054 |
| UI-016 | AutoManufacture presentation | Defined (Demo / Mode2) | AutoManufacture stage: Step1 center soldier row (150×200, "?" 42 + class name 32 + `Lv.{ClassLevel}` 24 below, horizontal scroll) + 6 MagicBook slots above (120×160); Step2 per-soldier amplify / Idle reveal / +25% speed every 3; Step3 enter UM then auto-open Formation; 0 craft skips; Mode1 **none**; accept §3.8 D-055 |
| UI-017 | PushMap battle settlement | Defined (Demo / PushMap) | Always on win/lose: top Victory/Defeat; mid combat time + monsters killed; bottom Continue. Fail Continue → LevelSelectPanel; Win Continue → UI-018; §3.14 |
| UI-018 | PushMap reward popup | Defined (Demo / PushMap) | Show already-credited StageExpReward + CaptureLoot aggregate only; no extra grants; bottom Continue → LevelSelectPanel; §3.14 |
| UI-019 | GM grant list | Defined (Demo GM) | Prefab `GmGrantListPanel` under InSaveShell (layout aligned with UI-008); Tools Grant Equipment / Grant MagicBook; label DisplayName (else Id); one click grants; close button; accept §3.8 D-061 |

---

## 3.7 玩法状态占位

### 简体中文

进档后须存在可识别的当前状态表现；默认进入 **挖坟（Dig）**。挖坟完整规则见 §3.10；升级与制造框架见 §3.11；防守框架见 §3.12。Meta 壳仅需占位可识别；流水线垂直切片须按 §3.8 对应验收项可玩。

| 状态 | 中文 | Demo 要求 | 范围 / 输入 / 胜负 |
|------|------|-----------|-------------------|
| Dig | 挖坟 | Meta：可识别占位；流水线：§3.10 垂直切片可玩（§3.8 D-020） | 规则见 §3.10（交互 / 扣血 / 奖励 / 无胜负 / DigStageSummary） |
| AutoManufacture | 自动制造 | Mode2 流水线：规则见 §3.15；实现见 §3.8 D-050～D-055（AM-03～08 + 制造记录 + 演出 UI-016） | Dig 后自动造兵+上阵+演出；无玩家确认结束；UM 可开制造记录 |
| UpgradeManufacture | 升级与制造 | Meta：可识别占位；流水线：§3.11 垂直切片可玩（§3.8 D-030～D-032）；Mode2 差分见 §3.15 / D-054 | 框架见 §3.11（原占位名 SewRevive） |
| Defend | 防守 | Meta：可识别占位；流水线：§3.12 垂直切片可玩（§3.8 D-040～D-043） | 框架见 §3.12（准备/开战/护盾/刷怪/寻路/胜负；Demo 最小刷怪点/NavMesh 见本节配套 §3.12） |
| PushMap | 推图战 | Meta：可识别占位即可；完整垂直切片 **非** 当前 §3.8 P0（规则见 §3.14） | 框架见 §3.14（复用 Defend 布阵/护盾/失控/士兵战斗 + 目标点占领/刷怪点/陷阱/BOSS） |

壳层内手动切换玩法状态方式 **TBD**（不得将工具「关卡」入口隐式等同为五态手动切换）。关卡运行时由阶段玩法类型驱动，见 §3.9。

### English

After enter, current state must be identifiable; default **Dig**. Dig: §3.10; UpgradeManufacture: §3.11; Defend: §3.12. Meta shell needs identifiable placeholders; pipeline vertical slices must be playable per §3.8.

| State | ZH | Demo requirement | Scope / input / win-lose |
|-------|-----|------------------|---------------------------|
| Dig | 挖坟 | Meta: identifiable placeholder; pipeline: playable §3.10 vertical (§3.8 D-020) | Rules in §3.10 (dig / HP / rewards / no win-lose / DigStageSummary) |
| AutoManufacture | 自动制造 | Mode2 pipeline: rules §3.15; impl §3.8 D-050–D-055 (AM-03–08 + manufacture record + presentation UI-016) | After Dig: auto craft + deploy + presentation; no player confirm; UM can open ManufactureRecord |
| UpgradeManufacture | 升级与制造 | Meta: identifiable placeholder; pipeline: playable §3.11 vertical (§3.8 D-030–D-032); Mode2 diffs §3.15 / D-054 | Framework in §3.11 (was SewRevive) |
| Defend | 防守 | Meta: identifiable placeholder; pipeline: playable §3.12 vertical (§3.8 D-040–D-043) | Framework in §3.12 (Prepare/StartBattle/Shield/spawn/pathing/win-lose; Demo-min spawn/NavMesh in §3.12) |
| PushMap | 推图战 | Meta: identifiable placeholder OK; full vertical **not** current §3.8 P0 (rules §3.14) | Framework in §3.14 (reuse Defend formation/Shield/LOC/WarriorCombat + objectives/spawns/traps/Boss) |

Manual shell state switch is **TBD** (must not equate Tools Level entry to a five-state manual switch). During Level, stage gameplay type drives state — §3.9.

---

## 3.8 Demo 验收标准

### 简体中文

**状态：已定义（Meta 壳 + 一条关卡流水线垂直切片）**

实现顺序建议：先 D-001～D-004（Meta 壳），再 D-010（关卡驱动），再 Dig → UpgradeManufacture → Defend（D-020～D-043）。临时美术允许（Prefab 路径须符合 [SPEC_04 §13](SPEC_04_Technical.md) / §15；正式资源后换）。

| ID | 验收项 | 优先级 | 状态 |
|----|--------|--------|------|
| D-001 | 可打开存档界面，对 3 槽执行新建 / 选择进入 / 删除（删除含二次确认） | P0 | Meta 壳已实现（Boot） |
| D-002 | 进入存档后可见浮动「工具」，可打开 / 关闭工具面板 | P0 | Meta 壳已实现 |
| D-003 | 工具面板可见「设置」「关卡」入口；「关卡」打开列表选关（当前模式 `LevelOperationConfig` 去重 LevelId → Stage 1） | P0 | **关卡**→ LevelSelectPanel（方案 B / UI-008）；设置→科技树画布 |
| D-004 | 进档后可识别当前处于三种玩法状态之一；默认进档为挖坟占位；关卡内由阶段玩法类型驱动 | P0 | Meta 占位+Debug 切态保留；关卡内由 `LevelOperationDriver` 按阶段 `GameplayType` 驱动 |
| D-010 | 运行时只读 `ConfigTables/Csv/`；按 `LevelOperationConfig` 升序驱动至少一条含 Dig → UpgradeManufacture → Defend 的样例关卡；UI/日志可见 LevelId、StageNumber、GameplayType | P0 | 已实现（方案 A；手验：Tools 关卡 + Debug 推进阶段） |
| D-020 | Dig 垂直切片可玩：按 `DigMapId` 实例化 `Assets/Prefabs/Maps/{Id}.prefab`；坟墓可挖可掉落；有效时长归零 → DigStageSummary 确认 → 交还关卡驱动 | P0 | 已实现（方案 A：`DigStageModule` + `DigSessionService`） |
| D-030 | UpgradeManufacture 升级区：可读 `ProtagonistLevelConfig`；注入/入账经验后可连升并看到表字段生效（TechPoints / ControlPowerCap / ProtagonistMaxHP） | P0 | 已实现（方案 A：`UpgradeManufactureStageModule` + `ProtagonistProgressService`；正式入账见 D-043） |
| D-031 | UpgradeManufacture 制造：至少可制造 1 名士兵实例并入池（临时 Prefab 可；技能不施放） | P0 | 已实现（方案 A：`ManufactureService` + `WarriorPoolService`；严格槽位 / 精魂闸门 / 种族与外观定稿 / 命名；临时 `Prefabs/Defend/Warriors/{AppearanceId}`） |
| D-032 | UpgradeManufacture 布阵：连续坐标布阵可写回；与 Defend Prepare 共用同一套 BattleFormation | P0 | 已实现（共享 `FormationEditorRoot` 拖拽编辑器；UM「布阵/返回」；士兵栏；控制力 HUD；`TryDeployAt`） |
| D-040 | Defend Prepare / 开战 / 护盾：加载 `BattleMapId`→`Prefabs/Maps/`；开战须 ≥1 上阵；`Shield` 初值=主角等级行 `ProtagonistMaxHP` | P0 | 已实现（Prepare 复用同一 `FormationEditorRoot`+「开战」；地图按 `BattleMapId`；开战 ≥1；护盾/倒计时不变） |
| D-041 | Defend 刷怪与寻路：样例波次能出怪；Demo 最小出生点（临时固定点或地图内随机）；怪物 NavMesh 接近并以普攻扣主角护盾；精确 OutsideMap 几何 **后置** | P0 | 已实现（方案 A：Session 按剩余秒激活 WaveSpawn + Runtime NavMesh + `MonsterAgentView` 扣盾；`Shield≤0`→Ended 钩子） |
| D-042 | Defend 士兵战斗：EngageZone 内普攻可选敌并造成伤害（第一版不施放技能） | P0 | 已实现（方案 A：近战前摇 + 远程 `ProjectileView` 软碰撞命中/超时未命中；清场可检测；胜负入账见 D-043） |
| D-043 | Defend 胜负与结算：清场胜利可入账阶段经验并交还关卡驱动；`Shield ≤ 0` → LevelFailure（可验） | P0 | 已实现（方案 A：开战 Degree/Tier 锁定 + `FinalLossChance` roll→Rebel 就近扣盾；清场 Ended 入账 Demo Exp=100→`TryAdvanceStage`；护盾归零 LevelFailure 不入账并 `AbortLevel`） |
| D-044 | 战斗模式选关闸门：进入 Defend 须先 `ModeSelect`；可选保卫战全部 `DefendGameplayConfig`；模式2列出全部 `PushMapGameplayConfig`，确认后交接进 §3.14 Prepare；任一模式通关→`TryAdvanceStage` | P0 | 已实现（方案 A：`BattleModeSelectRoot` + Mode2→`TryHandoffModeSelectToPushMap`→`PushMapStageModule`） |
| D-045 | 玩法模式门闩：新建/进入均弹出 `CampaignModeSelect`（Mode1/Mode2/取消）；同槽两模式进度隔离；Mode2 只读 `ConfigTables/Mode2/Csv`；删档清双模数据；**勿与** D-044 混淆 | P0 | 本片实现（方案 A） |
| D-050 | Mode2：样例关卡运作含 Dig → **AutoManufacture** → UpgradeManufacture；DigStageSummary 确认后进入自动制造；阶段无玩家确认、自动交还驱动 | P1 | 已实现（AM-03～08：`AutoManufactureStageModule` 自动 `TryAdvanceStage`；Mode2 `Level_01` Dig→AutoManufacture→UM→PushMap；Excel/CSV 对齐；0 兵 Tips「无士兵可制造」；手验清单 `.scratch/mode2-auto-manufacture/issues/08-level-sample-handcheck.md`） |
| D-051 | Mode2 AutoManufacture：按最低配方（头+躯干+臂×2含主要手+腿×2）循环造兵入临时仓库；不计 Spirit/Control；职业由双手 ClassRestrict；余料留仓库 | P1 | 已实现（AM-03～06：选料/职业/属性→钩子→外观+命名→临时仓 flush→`WarriorPool`→清阵上阵；SoulId 空、Control=0、AttackMode←ClassConfig；手验见 AM-08） |
| D-052 | Mode2：批结束后清空布阵，按 `PlacementOrder` + `FormationClassZone` 自动上阵（碰撞挤开）；再进 UM | P1 | 已实现（AM-06 方案 A：区内螺旋采样 + BodyRadius；`FormationClassZone` **IsoDiamond**（同 WalkSurface；废止 OBB/IsoTileYaw）；仅本批 Id；手验见 AM-08 / FZ-01～02） |
| D-053 | Mode2 UM：隐藏手动制造；保留升级 Modal 与可编辑布阵；控制力 HUD 屏蔽；布阵内 `CompleteButton`（SoldierBar 上右，UM/Prepare 均显示；UM 接线结束阶段） | P1 | 已实现（方案 C：`UpgradeManufactureStageRoot_Mode2` + `FormationEditorRoot_Mode2`；Catalog 按 CampaignMode Resolve；手验见 AM-08；Complete 见 Mode2 差分） |
| D-054 | Mode2 UM：布阵右侧「制造记录」打开只读弹窗；展示最近一批 AutoManufacture 士兵摘要（名字/种族/职业）；0 兵空态「本批无士兵」；下一批覆盖；同档再进仍可见；Mode1 无此按钮 | P1 | 已实现（方案 A：`AutoManufactureBatchRecordService` + Mode2 Modal；`UmAssetBuilder` Mode2 追加 / 运行时 Ensure） |
| D-055 | Mode2 AutoManufacture 演出（UI-016）：批末可见 Step1 士兵行+6 书槽；Step2 逐兵加强/单槽脉冲套该书/Idle 揭示/每 3 兵加速；播完后按最终 ClassId 上阵再进 UM 并自动开布阵；0 兵 Tips+跳过演出且不自动开布阵；Mode1 无此 UI | P1 | **更新**（单槽节拍：`ApplyEquippedBookAtSlot` 于脉冲峰值；Deploy 延后） |
| D-056 | 士兵外观：`BodyAppearanceConfig.AppearanceId` 在 `Art/Characters/Appearances/{Id}/` **Art 就绪**（Controller + Idle Sprite）时，须有游戏 Prefab `Prefabs/Defend/Warriors/{Id}.prefab`（根+`Visual`）并绑定 Defend/UM Catalog；缺绑定则布阵/战斗/演出不显示该外观 | P1 | Done（WA-01 / 方案 B：`WarriorAppearancePrefabAssembler` From-Art + Catalog 并集刷新；已补 `App_0_00`/`App_0_10`/`App_0_20`/`App_0_30`、`App_0_01`…`App_0_33`、`App_4_41`、`App_5_51`） |
| D-057 | 样例 `Ground_*` 的 `FormationClassZone` 覆盖当前模式 `ClassConfig` **全部** ClassId（缺区→自动上阵留池）；Mode2 须含 `Class_DarkMage`/`Class_Guardian` 等 | P1 | 已实现（全量 ClassId 同步 + 样例 HalfExtents 锁定 `(3.85, 2)`：Ensure 读 Mode2 `Manufacture_ClassConfig`；已有区保留世界 XZ；缺补/表外删；未调用 GenerateAll） |
| D-058 | Mode2 魔法书「战士强化」：装备 `MagicBook_WarriorEnhance` 后 AutoManufacture **Step2 该书槽脉冲**仅对 `Class_Warrior` 将主属性 Base 增加躯体该维 Σ StatBonus 的 15%（可叠；种族不过滤）；非战士不变；写入实例至彻底死亡；Dig HUD GM 可装备 | P1 | **更新**（生效点改为 Step2 单槽脉冲） |
| D-059 | 主角装备 Dig 垂直：`ProtagonistEquipmentConfig` 表加载（Mode1+Mode2）+ 装备仓 Service/存档 + Dig caps 科技与装备加法合并 + Dig HUD GM 手验（发放 `Equip_IronShovel` / 公共经验）；正式装备 UI / 制造·战斗 Token **后置** | P1 | **完成**（PE-01～PE-04；方案 A；issues `.scratch/protagonist-equipment/`；Demo 装备=`Equip_IronShovel` 铁铲） |
| D-060 | 主角装备「矿灯」`Equip_MinerLamp`：表 5 级（升下一级/转化经验均为 1）+ Q4/Q5/Q6 生成权重按当前行累计 +10（缺席视为 0）+ Dig HUD GM 发放/划入手验 | P1 | **完成**（PE-05～PE-08；方案 A；issues `.scratch/protagonist-equipment/`） |
| D-061 | ToolsPanel Demo GM：增加主角装备（当前模式表按 EquipId 去重，点一次 `TryAcquire`）+ 增加魔法书（MagicBookConfig 全表，点一次 `TryEquip`；唯一已装/槽满失败）；GmGrantListPanel（UI-019）；Dig HUD GM 保留；正式仓/装配 UI **后置** | P1 | **完成**（TP-00～02；方案 A；issues `.scratch/tools-panel-gm-grant/`） |
| D-062 | 士兵技能垂直：`SkillConfig` 表加载（Mode1+Mode2，含 `IconAssetId`）+ `ClassConfig.DefaultSkillIds` + 池持久化 + Mode1 制造授予 + Mode2 造兵授予/`SoldierSkillLevelAdd`（Step2 该书槽脉冲）；Demo **不施放** | P1 | **更新**（取消二次扫描；单槽脉冲立刻升技能） |
| D-063 | Mode2 魔法书职业进阶：装备 `MagicBook_WarriorAdvance` 等四本后 AutoManufacture **Step2 该书槽脉冲**仅对对应 `Class_*_0` 以 25% 改为 `Class_*`（精确 ClassId；日志 hit/miss）；命中后重授 `DefaultSkillIds`；外观/命名/上阵区跟最终职业；其它职业不变；手验 Tools「增加魔法书」 | P1 | **更新**（生效点改为 Step2 单槽脉冲；Deploy 用最终 ClassId） |

**Demo 范围外（仍排除）：**

- Mode2 魔法书装备 UI 与其余具体效果行（「还原」`RaceWeightPick`、「战士强化」`StatMul`/`Primary`、职业进阶 `ForceClass` 已实现；正式装备 UI / 其它书另专题）
- 推图战（PushMap）完整 polish / 副本玩法正文（规则与 ModeSelect 模式2入口已落地 §3.14 / D-044；细节见 `.scratch/push-map/issues/`）
- 完整技能施放与技能效果表驱动（士兵/怪物第一版仅普通攻击；`SkillConfig` / CD 公式保留不驱动）
- 正式美术与动画 polish（临时 Prefab / 占位资源允许；禁止运行时引用 `SmallScaleInt/`）
- 完整存档序列化 schema（超出槽占用、士兵池、布阵及流水线所需的最小持久化字段；仓库/经验/科技等仍 TBD）
- 精确 OutsideMap 出生几何、完整障碍烘焙细则（Demo 最小约定见 §3.12 / [SPEC_04 §9.7](SPEC_04_Technical.md)）
- 科技树节点具体数值/图标 polish 与功能系统名完整枚举（§3.13；画布方案 A 已落地，非本表 P0）
- 工具面板「设置」「关卡」及 D-061 GM 发放以外的后续功能；完整 polish；未写入本表的需求
- 打表全量 §9 列/类型校验（[SPEC_04 §14](SPEC_04_Technical.md) Demo 仅文件名+表头；schema 校验后置）

实现边界对照：[SPEC_04 §6](SPEC_04_Technical.md)。

### English

**Status: Defined (Meta shell + one Level-pipeline vertical slice)**

Suggested order: D-001–D-004 (Meta) → D-010 (Level driver) → Dig → UpgradeManufacture → Defend (D-020–D-043). Temp art allowed (Prefab paths must follow [SPEC_04 §13](SPEC_04_Technical.md) / §15; swap formal art later).

| ID | Criterion | Priority | Status |
|----|-----------|----------|--------|
| D-001 | Save UI with 3 slots: create / enter / delete (delete confirms) | P0 | Meta shell done (Boot) |
| D-002 | After enter: floating Tools; open / close ToolsPanel | P0 | Meta shell done |
| D-003 | Tools shows Settings + Level; Level opens pick list (distinct LevelId from current-mode `LevelOperationConfig` → Stage 1) | P0 | **Level** → LevelSelectPanel (Approach B / UI-008); Settings → TechTree canvas |
| D-004 | Identifiable gameplay state; default Dig placeholder; in-Level driven by stage GameplayType | P0 | Meta placeholders + Debug cycle kept; in-Level driven by `LevelOperationDriver` via stage `GameplayType` |
| D-010 | Runtime reads `ConfigTables/Csv/` only; `LevelOperationConfig` drives at least one sample Level with Dig → UpgradeManufacture → Defend; UI/log shows LevelId, StageNumber, GameplayType | P0 | Done (Approach A; hand-check: Tools Level + Debug advance stage) |
| D-020 | Dig vertical playable: instantiate `Assets/Prefabs/Maps/{DigMapId}.prefab`; dig + loot; duration → DigStageSummary confirm → return to Level driver | P0 | Done (Approach A: `DigStageModule` + `DigSessionService`) |
| D-030 | UM upgrade panel: read `ProtagonistLevelConfig`; inject/credit Exp → multi-level up; TechPoints / ControlPowerCap / ProtagonistMaxHP visible | P0 | Done (Approach A: `UpgradeManufactureStageModule` + `ProtagonistProgressService`; formal credit in D-043) |
| D-031 | UM manufacture: craft ≥1 soldier instance into pool (temp Prefab OK; no skill casts) | P0 | Done (Approach A: `ManufactureService` + `WarriorPoolService`; strict slots / Spirit gate / Race + Appearance finalize / naming; temp `Prefabs/Defend/Warriors/{AppearanceId}`) |
| D-032 | UM formation: continuous-coord formation writable; shared BattleFormation with Defend Prepare | P0 | Done (shared `FormationEditorRoot` drag editor; UM Formation/Return; soldier bar; ControlPower HUD; `TryDeployAt`) |
| D-040 | Defend Prepare / StartBattle / Shield: load `BattleMapId`→`Prefabs/Maps/`; StartBattle requires ≥1 deployed; Shield init = level-row `ProtagonistMaxHP` | P0 | Done (Prepare reuses same `FormationEditorRoot`+StartBattle; map by `BattleMapId`; StartBattle ≥1; Shield/countdown unchanged) |
| D-041 | Defend spawn + path: sample waves spawn; Demo-min spawn (fixed points or in-map random); monsters NavMesh approach and normal-attack Shield; exact OutsideMap geometry **deferred** | P0 | Done (Approach A: Session activates WaveSpawn by remaining seconds + runtime NavMesh + `MonsterAgentView` hits Shield; `Shield≤0`→Ended hook) |
| D-042 | Defend WarriorCombat: EngageZone normal-attack targeting + damage (no skill casts in v1) | P0 | Done (Approach A: melee windup + ranged `ProjectileView` soft-hit/timeout miss; clear detectable; win/lose credit in D-043) |
| D-043 | Defend win/lose: clear-spawn victory credits stage Exp and returns to Level driver; `Shield ≤ 0` → LevelFailure (verifiable) | P0 | Done (Approach A: StartBattle Degree/Tier lock + `FinalLossChance`→Rebel nearest Shield hit; clear Ended credits Demo Exp=100→`TryAdvanceStage`; Shield 0 LevelFailure no Exp + `AbortLevel`) |
| D-044 | Battle ModeSelect gate: entering Defend requires `ModeSelect` first; list all DefendGameplayConfig for Mode1; Mode2 lists all PushMapGameplayConfig then handoff → §3.14 Prepare; either-mode clear→`TryAdvanceStage` | P0 | Done (Approach A: `BattleModeSelectRoot` + Mode2→`TryHandoffModeSelectToPushMap`→`PushMapStageModule`) |
| D-045 | CampaignMode gate: create/enter always shows `CampaignModeSelect` (Mode1/Mode2/cancel); per-slot progress isolated by mode; Mode2 reads only `ConfigTables/Mode2/Csv`; delete clears both modes; **not** D-044 | P0 | This slice (Approach A) |
| D-050 | Mode2: sample LevelOperation Dig → **AutoManufacture** → UpgradeManufacture; after DigStageSummary enter auto craft; stage ends without player confirm | P1 | Done (AM-03–08: `AutoManufactureStageModule` auto `TryAdvanceStage`; Mode2 `Level_01` Dig→AutoManufacture→UM→PushMap; Excel/CSV aligned; zero-craft Tips「无士兵可制造」; handcheck `.scratch/mode2-auto-manufacture/issues/08-level-sample-handcheck.md`) |
| D-051 | Mode2 AutoManufacture: loop craft into temp warehouse with min recipe (Head+Torso+2Arm incl. PrimaryHand+2Leg); no Spirit/Control; class from hand ClassRestrict; leftovers stay in Warehouse | P1 | Done (AM-03–06: pick/class/base→hook→appearance+name→temp flush→`WarriorPool`→clear+deploy; empty SoulId, Control=0, AttackMode←ClassConfig; handcheck AM-08) |
| D-052 | Mode2: after batch, clear formation; auto-deploy by `PlacementOrder` + `FormationClassZone` (separation); then enter UM | P1 | Done (AM-06 Approach A: in-zone spiral + BodyRadius; `FormationClassZone` **IsoDiamond** (same as WalkSurface; drop OBB/IsoTileYaw); batch Ids only; handcheck AM-08 / FZ-01–02) |
| D-053 | Mode2 UM: hide manual manufacture; keep upgrade Modal + editable formation; hide ControlPower HUD; in-editor `CompleteButton` (above SoldierBar right; visible UM+Prepare; UM wires stage end) | P1 | Done (Approach C: `UpgradeManufactureStageRoot_Mode2` + `FormationEditorRoot_Mode2`; Catalog Resolve by CampaignMode; handcheck AM-08; Complete in Mode2 diffs) |
| D-054 | Mode2 UM: "Manufacture Record" to the right of Formation opens read-only popup; last AutoManufacture batch summaries (name/race/class); empty 「本批无士兵」; next batch overwrites; survives re-enter save; Mode1 has no button | P1 | Done (Approach A: `AutoManufactureBatchRecordService` + Mode2 Modal; `UmAssetBuilder` Mode2 append / runtime Ensure) |
| D-055 | Mode2 AutoManufacture presentation (UI-016): after batch show Step1 soldier row + 6 book slots; Step2 per-soldier amplify / per-slot pulse applies that book / Idle reveal / +25% speed every 3; then deploy by final ClassId → UM + auto-open Formation; 0 craft Tips + skip presentation and no auto-open; Mode1 has no UI | P1 | **Updated** (per-slot beat: `ApplyEquippedBookAtSlot` at pulse peak; Deploy deferred) |
| D-056 | Soldier visuals: when `BodyAppearanceConfig.AppearanceId` has Art-ready bake under `Art/Characters/Appearances/{Id}/` (Controller + Idle Sprite), game Prefab `Prefabs/Defend/Warriors/{Id}.prefab` (root+`Visual`) must exist and bind Defend/UM catalogs; missing bind → no visual in formation/combat/presentation | P1 | Done (WA-01 / Approach B: `WarriorAppearancePrefabAssembler` From-Art + union catalog refresh; added `App_0_00`/`App_0_10`/`App_0_20`/`App_0_30`, `App_0_01`…`App_0_33`, `App_4_41`, `App_5_51`) |
| D-057 | Sample `Ground_*` `FormationClassZone` covers **every** current-mode `ClassConfig.ClassId` (no zone → auto-deploy stays in pool); Mode2 must include `Class_DarkMage`/`Class_Guardian` etc. | P1 | Done (full ClassId sync + sample HalfExtents locked `(3.85, 2)`: Ensure reads Mode2 `Manufacture_ClassConfig`; existing zones keep world XZ; add missing / remove orphans; no GenerateAll) |
| D-058 | Mode2 MagicBook Warrior Enhance: equipped `MagicBook_WarriorEnhance` on AutoManufacture **Step2 that slot's pulse** adds 15% of body Σ StatBonus(class PrimaryStat) to Base for `Class_Warrior` only (stackable; no race filter); other classes unchanged; baked until PermanentDeath; Dig HUD GM can equip | P1 | **Updated** (apply point → Step2 per-slot pulse) |
| D-059 | ProtagonistEquipment Dig vertical: load `ProtagonistEquipmentConfig` (Mode1+Mode2) + warehouse Service/persist + Dig caps tech+equip additive merge + Dig HUD GM handcheck (grant `Equip_IronShovel` / common Exp); formal equip UI / Manufacture·Combat tokens **deferred** | P1 | **Done** (PE-01–PE-04; Approach A; issues `.scratch/protagonist-equipment/`; Demo gear=`Equip_IronShovel` Iron Shovel) |
| D-060 | ProtagonistEquipment Miner Lamp `Equip_MinerLamp`: 5-level table (ExpToNext/ConvertExp=1) + Q4/Q5/Q6 spawn-weight cumulative +10 at current row (absent treated as 0) + Dig HUD GM grant/spend handcheck | P1 | **Done** (PE-05–PE-08; Approach A; issues `.scratch/protagonist-equipment/`) |
| D-061 | ToolsPanel Demo GM: Grant Protagonist Equipment (distinct EquipId, one click `TryAcquire`) + Grant MagicBook (full MagicBookConfig, one click `TryEquip`; unique already equipped / slots full fail); GmGrantListPanel (UI-019); Dig HUD GM kept; formal warehouse/equip UI **deferred** | P1 | **Done** (TP-00–02; Approach A; issues `.scratch/tools-panel-gm-grant/`) |
| D-062 | Soldier-skill vertical: load `SkillConfig` (Mode1+Mode2, incl. `IconAssetId`) + `ClassConfig.DefaultSkillIds` + pool persist + Mode1 manufacture grant + Mode2 craft grant/`SoldierSkillLevelAdd` (Step2 that slot's pulse); Demo **no cast** | P1 | **Updated** (no second pass; immediate on-slot pulse) |
| D-063 | Mode2 MagicBook class advance: equipped `MagicBook_WarriorAdvance` (and siblings) on AutoManufacture **Step2 that slot's pulse** promotes matching `Class_*_0` to `Class_*` at 25% (exact ClassId; log hit/miss); on hit re-grant `DefaultSkillIds`; appearance/name/deploy zone follow final class; other classes unchanged; hand-check Tools Grant MagicBook | P1 | **Updated** (apply point → Step2 per-slot pulse; Deploy uses final ClassId) |

**Out of Demo scope (still excluded):**

- Mode2 MagicBook equip UI and remaining concrete effects (Restore `RaceWeightPick`, Warrior Enhance `StatMul`/`Primary`, and class-advance `ForceClass` done; formal equip UI / other books later)
- PushMap polish / dungeon gameplay body (rules + ModeSelect Mode2 entry landed §3.14 / D-044; details in `.scratch/push-map/issues/`)
- Full skill casts / skill-effect table drive (soldiers/monsters: normal attacks only in v1; `SkillConfig` / CD formula retained unused)
- Formal art / animation polish (temp Prefabs OK; **no** runtime refs to `SmallScaleInt/`)
- Full save schema beyond occupied flag + warrior pool + BattleFormation + minimal pipeline fields (Warehouse / Exp / Tech still TBD)
- Exact OutsideMap spawn geometry / full obstacle-bake detail (Demo-min in §3.12 / [SPEC_04 §9.7](SPEC_04_Technical.md))
- Full TechTree node values/icon polish & full feature-system enum (§3.13; canvas Approach A landed; not P0 here)
- Tools entries beyond Settings / Level / D-061 GM grants; full polish; anything not in this table
- Bake full §9 column/type validation ([SPEC_04 §14](SPEC_04_Technical.md) Demo: filename + header only; schema validation deferred)

Boundary: [SPEC_04 §6](SPEC_04_Technical.md).

---

## 3.9 关卡阶段流水线

### 简体中文

**状态：已定义（规则库；Demo 流水线垂直切片须实现，见 §3.8 D-010）**

关卡由「关卡运作表」驱动。同一 `关卡ID` 的多行按 `阶段编号` **升序**执行。每阶段以 `玩法类型` 设置当前 `GameplayState`，并以 `玩法配置ID` 加载对应玩法配置（挖坟见 §3.10；自动制造见 §3.15；升级与制造见 §3.11；防守见 §3.12；推图战见 §3.14；配置编码见 [SPEC_04 §9](SPEC_04_Technical.md)）。

**表 1 — 关卡运作表字段（规则语义）**

| 字段 | 说明 |
|------|------|
| 关卡ID | 关卡标识；同 ID 多行组成该关的全部阶段 |
| 阶段编号 | 同关卡内执行顺序（升序） |
| 玩法类型 | 本阶段玩法（如 `Dig` / `AutoManufacture` / `UpgradeManufacture` / `Defend` / `PushMap`）；映射到 `GameplayState` |
| 玩法配置ID | **Dig** → 查 `DigGameplayConfig` 主键；**Defend** → **RecommendedConfigId**（选关默认高亮；UM 下一战斗地图预览仍可解析；**不**强制为唯一开战配置——开战配置由玩家在 `ModeSelect` 从该模式全部玩法配置中选择，见 §3.12）；**PushMap** → 查 `PushMapGameplayConfig` 主键（亦可经 ModeSelect 模式2 选关，见 §3.12 / §3.14）；**UpgradeManufacture** / **AutoManufacture** → **忽略**（可不空；运行时**不**查任何玩法配置表、不解析为 Dig/Defend/PushMap 行；本阶段读全局表如 `ProtagonistLevelConfig` / `BodyPartConfig` 等，见 §3.11 / §3.15 / [SPEC_04 §9.1](SPEC_04_Technical.md)）。**本版不另开** `UpgradeManufactureGameplayConfig` / `AutoManufactureGameplayConfig` |

**阶段流转**

1. 进入关卡：按 `关卡ID` 加载关卡运作行 → 按阶段编号升序排序。
2. 运行当前阶段：应用玩法类型与玩法配置 ID。
3. 阶段结束：由该玩法的结束条件触发。挖坟阶段：有效挖坟时长倒计时归零 → 本阶段结束（§3.10；**无胜负**）。**自动制造**：算法跑完（造到不能再造 + 自动上阵）后播演出（UI-016；0 兵跳过），再 **自动**结束（§3.15；无玩家确认；**无独立阶段结算**）。升级与制造阶段：玩家主动确认「完成 / 进入下一阶段」→ 本阶段结束（§3.11；无强制倒计时；**无独立阶段结算**）。防守/战斗阶段：玩家在 `ModeSelect` 所选模式的关卡胜利 → 阶段结束（§3.12；保卫战护盾归零 → **关卡失败**）；`GameplayType=PushMap` 或模式2：BOSS 通关 → 阶段结束；护盾归零 **或** 无忠诚存活士兵 → **关卡失败**（§3.14）。
4. 阶段结算：若该玩法定义了阶段结算则触发（挖坟：**DigStageSummary** 仅汇总本阶段已获奖励、无额外发放，玩家确认后继续；**自动制造跳过**；升级与制造 **跳过**；防守阶段胜利时至少含 **经验入账**，其余 **TBD**；推图战：先 **战斗结算（UI-017）**，胜利再 **奖励弹窗（UI-018）** 后打开关卡选择（**不**自动 `TryAdvanceStage` / VictorySettlement 占位），见 §3.14），再进入下一阶段（非 PushMap Demo 路由时）。
5. **无下一阶段**（已是最后一阶段结束后）：触发关卡级 **胜利结算（VictorySettlement）**（PushMap Demo 胜负后改走 UI-017/018 → LevelSelectPanel，见 §3.14）。
6. **关卡失败（LevelFailure）**：任意阶段触发关卡失败（如 Defend 中护盾归零；PushMap 护盾归零或无忠诚存活）→ **结束关卡**；**不**触发 VictorySettlement / **无关卡结算奖励**；**不**入账本阶段失败阶段经验；此前已入账的 Experience、材料/精魂、士兵、TechPoint 等 **不扣除**；PushMap Demo：先 UI-017 再 Continue → LevelSelectPanel（§3.14）；Defend 失败结算 UI 仍 **TBD**。

```
EnterLevel
  → Load LevelOperation rows by LevelId
  → Sort by StageNumber ascending
  → Run stage (GameplayType + GameplayConfigId)
  → Stage end condition
       Dig: EffectiveDigDuration countdown = 0 (no win/lose)
       AutoManufacture: algo done + presentation (UI-016; skip if 0); no player confirm (§3.15)
       UpgradeManufacture: player confirm
       Defend: stage victory per §3.12  OR  LevelFailure → abort Level
       PushMap: Boss clear per §3.14  OR  LevelFailure → abort Level
  → If LevelFailure → no VictorySettlement / no stage Exp credit; keep already-owned; LevelFailure settlement UI TBD; stop
  → Stage settlement if any
       Dig: DigStageSummary (aggregate only; no extra grants) → player confirm
       AutoManufacture: skip
       UpgradeManufacture: skip
       Defend: at least Experience credit; other TBD
       PushMap: Boss-clear Experience credit; capture loot separate; §3.14
  → If next stage exists → run next
  → Else → VictorySettlement
```
### English

**Status: Defined (rules library; Demo pipeline vertical must implement — §3.8 D-010)**

A Level is driven by the Level Operation table. Rows sharing a `LevelId` run in ascending `StageNumber`. Each stage sets `GameplayState` from `GameplayType` and loads config via `GameplayConfigId` (Dig: §3.10; AutoManufacture: §3.15; UpgradeManufacture: §3.11; Defend: §3.12; PushMap: §3.14; encodings: [SPEC_04 §9](SPEC_04_Technical.md)).

**Table 1 — Level Operation fields (rules semantics)**

| Field | Notes |
|-------|-------|
| LevelId | Level id; multiple rows = all stages |
| StageNumber | Execution order within the Level (ascending) |
| GameplayType | Stage mode (e.g. `Dig` / `AutoManufacture` / `UpgradeManufacture` / `Defend` / `PushMap`) → `GameplayState` |
| GameplayConfigId | **Dig** → lookup `DigGameplayConfig` PK; **Defend** → **RecommendedConfigId** (default highlight in ModeSelect; UM next-battle map preview may still resolve; **not** the sole start config — player picks from all mode configs in `ModeSelect`, §3.12); **PushMap** → lookup `PushMapGameplayConfig` PK (or ModeSelect Mode2 pick, §3.12 / §3.14); **UpgradeManufacture** / **AutoManufacture** → **ignore** (may be non-empty; runtime must **not** resolve against any mode config table / Dig/Defend/PushMap rows; stage reads global tables such as `ProtagonistLevelConfig` / `BodyPartConfig` — §3.11 / §3.15 / [SPEC_04 §9.1](SPEC_04_Technical.md)). **No** separate `UpgradeManufactureGameplayConfig` / `AutoManufactureGameplayConfig` this version |

**Stage flow**

1. Enter Level: load rows by LevelId → sort by StageNumber ascending.
2. Run current stage: apply GameplayType + GameplayConfigId.
3. Stage end: per-mode end condition. Dig: effective Dig duration countdown hits 0 → stage ends (§3.10; **no win/lose**). **AutoManufacture**: algorithm finishes (craft until cannot + auto-deploy) → play presentation (UI-016; skip if 0 craft) → stage ends automatically (§3.15; no player confirm; **no independent stage settlement**). UpgradeManufacture: player confirms "Complete / Next stage" → stage ends (§3.11; no forced countdown; **no independent stage settlement**). Defend: see §3.12 (clear-spawn victory → stage end; Shield reaches 0 → **LevelFailure**, no next stage). PushMap: see §3.14 (Boss clear → stage end; Shield reaches 0 → **LevelFailure**).
4. Stage settlement: if the mode defines one, run it (Dig: **DigStageSummary** — aggregate rewards earned this stage only, no extra grants, then player confirm; **AutoManufacture skips**; UpgradeManufacture **skips**; Defend victory at least **credits Experience**, other content **TBD**), then advance.
5. **No next stage** (after last stage ends): trigger level-level **VictorySettlement**.
6. **LevelFailure**: any stage that triggers LevelFailure (e.g. Shield reaches 0 in Defend) → **abort the Level immediately**; **no** VictorySettlement / **no level settlement rewards**; **no** Defend stage Exp credit for the failed stage; already-owned Experience, materials/SpiritEssence, soldiers, TechPoints, etc. are **not clawed back**; failure settlement UI/fields **TBD**.

```
EnterLevel
  → Load LevelOperation rows by LevelId
  → Sort by StageNumber ascending
  → Run stage (GameplayType + GameplayConfigId)
  → Stage end condition
       Dig: EffectiveDigDuration countdown = 0 (no win/lose)
       AutoManufacture: algo done + presentation (UI-016; skip if 0); no player confirm (§3.15)
       UpgradeManufacture: player confirm
       Defend: stage victory per §3.12  OR  LevelFailure → abort Level
       PushMap: Boss clear per §3.14  OR  LevelFailure → abort Level
  → If LevelFailure → no VictorySettlement / no stage Exp credit; keep already-owned; LevelFailure settlement UI TBD; stop
  → Stage settlement if any
       Dig: DigStageSummary (aggregate only; no extra grants) → player confirm
       AutoManufacture: skip
       UpgradeManufacture: skip
       Defend: at least Experience credit; other TBD
       PushMap: Boss-clear Experience credit; capture loot separate; §3.14
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
| 表现 | Unity **Isometric Tilemap** 斜 45° 菱形地板拼贴；相机正交（无透视）；玩法坐标系仍为 XZ 顶视 |
| 表现资产 | 本阶段 `DigGameplayConfig.DigMapId` → Prefab 逻辑名 `Ground_01`…`Ground_05`（与 Defend 的 `BattleMapId` **共用**同一地面变体池）；Tile/Sprite 落 `Assets/Art/Maps/Tiles/`（自 Example Scene `Environment/Tiles`+`Sprites` 复制）；运行时只引用 `Assets/Prefabs/Maps/{Id}.prefab`，**禁止**引用 `SmallScaleInt/`，见 [SPEC_04 §9.2 / §13 / §15](SPEC_04_Technical.md) |
| 逻辑 | **整体可放置空间**（**IsoDiamond** XZ 曼哈顿菱形，与 Isometric 砖面外轮廓对齐），不是一堆格子；落点在连续菱形内选取；Tilemap 仅表现，不驱动规则网格；`DigMapBounds` 半尺寸 = `PaintRadius*(cellSize.x,cellSize.y)`（可各向异性；Demo ≈`(5,2.5)`） |
| 可放置 | 候选位置须在 IsoDiamond 内，且不得与任何 **挖坟障碍物（DigObstacle）** 的圆形区域相交 |

**障碍物（DigObstacle）**

本阶段障碍物 **仅** 以下一类（暂不引入其他类型）：

| 类型 | 说明 |
|------|------|
| Grave | 已生成且尚未消除（HP > 0）的坟；障碍区域大小在 **该品质对应坟预制体**上配置（每种坟品质专属预制体；圆形障碍半径） |

- **不**生成地图中心 Digger 实体，**无**主角圆形障碍。
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
2. **独立进行 N 次**尝试：每次先取表 `GraveSpawnWeights`，再按 `DigProtagonistCapabilities.GraveSpawnWeightBonus` **按 QualityId 加法**得到有效权重（表中缺席该 Id 视为权重 **0** 再加成；加成加到该 Id **首个**段，无段则插入）；再按 [SPEC_04 §9 加权字段通用规则](SPEC_04_Technical.md) 过滤（`Weight ≤ 0` 剔除）。若有效列表为空 → **放弃该次生成**（不抽品质、不生成实体）。否则按有效权重加权抽取一个坟墓品质 ID。
3. 每次抽中后，在地图可放置区域内随机选位置生成一座坟墓；该坟 `maxHP` / 当前 HP 按品质定义表初始化。
4. 落点采样须避开未消除 Grave 的圆形障碍；单次生成最多重试 **32** 次，仍失败则 **放弃该次生成**。

**过程生成**

- 倒计时进行中，按「倒计时过程中生成坟墓速率」：每 N 秒尝试生成 M 座坟。
- 每一座仍：表权重 + caps 加成 → 过滤 →（空有效列表则放弃）→ 加权抽品质 ID → 可放置区随机落点（同上重试规则）→ 按品质表初始化 HP。
- 与开局共用同一套有效权重算法；**每次抽取读活 caps**（本阶段中装备升级立即影响后续刷坟）。

**主角（Digger）表现**

| 规则 | 说明 |
|------|------|
| 地图实体 | Dig 阶段 **不** Instantiate 地图中心 Digger 模型（无整角 Visual、无待机/挖坟动画驱动） |
| HUD 头像 | Dig HUD **左上角** 固定 **60×60**（Canvas 参考分辨率单位）方框，展示主角头像；Demo 可用占位图/色块，正式头像资源后换 |
| Prefab | `Digger` Prefab / Art 管线可保留（[SPEC_04 §15](SPEC_04_Technical.md)），但本阶段运行时 **不**作为场上实体 |

**挖坟主角能力（DigProtagonistCapabilities）**

绑定在 **存档主角** 上，由科技树学会写入（规则与表结构见 [§3.13](#313-科技树techtree) / [SPEC_04 §9.16–§9.17](SPEC_04_Technical.md)；本批只定能力语义与算法）：

| 能力 | 说明 |
|------|------|
| DigDamage | 单次 DigAction 结束时对该坟的扣血数值；Demo 初始值 **25**，由默认解锁科技项提供 |
| DigDurationReductionSum | 所有已解锁「缩短单次挖坟时长」科技效果之和（秒） |
| DigCursorRadius | 圆圈光标半径（世界单位）；Demo 初始值 **0.6**，由默认解锁科技项提供 |
| DiggableQualityIds | 已解锁、可触发挖掘的坟墓品质 ID 集合 |
| DigStageDurationBonus | 挖坟阶段有效时长的科技加成（秒，加法；见「有效挖坟时长」） |
| GraveSpawnWeightBonus | 按 QualityId 的生成权重加法；编码键 `GraveSpawnWeightBonus_{QualityId}`（见 [SPEC_04 §9.17](SPEC_04_Technical.md)）；表 `GraveSpawnWeights` 缺席该 Id 视为 0 再加成 |

**单次挖掘时长（挖坟单次速度）：**

`DigActionDuration = max(0.1, BaseDigDuration − DigDurationReductionSum)`，其中 `BaseDigDuration = 0.8`（秒）。最短挖坟时间不得小于 **0.1s**。

（与「有效挖坟时长」不同：后者是阶段倒计时总长；本项是单次 DigAction 动画/结算时长。）

**光标与挖掘触发**

| 规则 | 值 |
|------|-----|
| 光标形态 | 进入挖坟阶段后，鼠标指针变为「圆圈范围」；半径 = `DigCursorRadius`（圆，非方） |
| DigHitShape | 每品质 Grave Prefab 上离线烘焙的 **本地 XZ 凸包**（≤12 顶点，贴近精灵轮廓）；与 `DigObstacle` 圆半径分离。无有效凸包时回退为该 Prefab `DigObstacle` 半径的圆近似 |
| 命中判定 | 光标圆与坟 `DigHitShape`（世界 XZ）**相交**的未清除坟为候选；规则层纯几何，**禁止**运行时读 Sprite/像素。Busy 视觉缩放 **不**放大命中形 |
| 光标表现 | 屏幕空间 UI Prefab `UiDigCursorRing`（`Assets/Prefabs/Dig/`）：外圈描边 + 内区白色半透明填充；圆直径 = `DigCursorRadius` 的屏幕投影像素 ÷ Dig HUD `Canvas.scaleFactor`（写入 `sizeDelta`，避免 CanvasScaler 二次放大），**描边屏幕像素粗细不随半径/分辨率缩放** |
| 触发条件 | 与命中形相交的「可挖且非忙碌」坟 ≥1，圆圈 **连续停留 ≥ 0.2 秒** → 对当时**所有**满足条件的坟**同时**各启一次 DigAction |
| 可挖类型门禁 | 若该坟品质 ID **不在** `DiggableQualityIds` 内 → **不**纳入本次触发（该类坟仍可按配置生成） |
| 忙碌锁 | **按坟**：该坟处于「挖掘中」则不可再触发，直至本次 DigAction 结束；**无**「场上已有任意 DigAction 则禁止新触发」的全局锁。挖进行中若光标又盖住新的空闲可挖坟，可再积 0.2s 后对其启动；离开半径**不**中断已开始的 DigAction |

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
2. 成功动画播放的同时，规则层按该坟品质的 `DropMode` 对 `LootDrop` **结算**（编码与模式见 [SPEC_04 §9.3](SPEC_04_Technical.md)），得到已确定的 `Id_Count` 列表；在动画 **中心点** 出现本次获得的奖励图标。结算为空则不生成奖励图标。
3. 随后奖励图标 **飞向 Dig HUD 左上角主角头像框中心**；**到达瞬间**按下方规则入账（对已结算列表），然后图标消失。

**仓库（Warehouse）与精魂（SpiritEssence）入账**

| 规则 | 说明 |
|------|------|
| 仓库 | 按 **存档槽** 持久；**不限格数、不限存储时长** |
| 材料堆叠 | 非货币奖励按 **材料类型（MaterialId）** 堆叠；单类型上限常量 **10000** |
| 精魂 | 货币；**不**进入材料堆叠；挖坟获得（`LootDrop` 保留 Id 直接掉落 + 堆叠超限自动兑换）；在 **制造士兵** 时消耗（§3.11） |
| 入账时机 | DigReward 飞到 **头像框中心** **到达瞬间** |

解析已结算的每一段 `Id_Count`（**不是**表内原始 `Id;Weight;Count`）：

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
| 阶段结算 | 弹出 **DigStageSummary**（UI-011）：仅展示 **本阶段已获得** 奖励的按类型汇总；**不额外发放**任何奖励（与关卡级 `VictorySettlement` 区分）。躯体材料行 `{DisplayName} Lv{BodyLevel} × 数量`（`DisplayName` 空则回退 `BodyPartId`）；精魂与非躯体材料仍 `{Id} × 数量`。Demo 面板 `SummaryRoot` **1020×1150**、`Body` **920×1030**；关闭为右上「X」（`ConfirmButton`） |
| 确认后 | 玩家点右上「X」关闭弹窗 → 进入 §3.9 下一阶段 /（若末阶段）`VictorySettlement` |

**Demo GM（Dig HUD）**

| 按钮 | 行为 |
|------|------|
| 增加坟墓 | 点一次：按**当前有效权重**（表 `GraveSpawnWeights` + caps `GraveSpawnWeightBonus`）加权抽品质，落点/避障/32 次重试规则同开局与过程生成；循环尝试 **10** 次；空间不足或有效权重为空时该次放弃，实际生成可少于 10 |
| 增加躯体材料 | 点一次：对当前已加载 `Manufacture_BodyPartConfig` **全部行**各 `Warehouse.AddItem(BodyPartId, 10)`（堆叠上限 10000 钳制；**不**走 LootDrop / AutoConvert） |
| 装备战士强化 | **仅 Mode2**：点一次 `SpecialEquipSlotsService.TryEquip("MagicBook_WarriorEnhance")`（可叠；槽满则 Tips/日志失败）。正式装备 UI 另专题；手验 D-058 |
| 获得铁铲 | 点一次：`ProtagonistEquipmentService.TryAcquire("Equip_IronShovel")`（首获 / 同 Id 转化连升 / 满级转公共池）；日志打印 Level / CurrentExp / `DigCursorRadius`。正式装备 UI 后置；手验 D-059 |
| 装备公共经验+50 | 点一次：GM 注入 `EquipCommonExp += 50`（`DebugGrantCommonExp`）；日志打印公共池与仓状态 |
| 划入铁铲升级 | 点一次：`TrySpendCommonExp("Equip_IronShovel", 1)`（池不足或未拥有则日志失败）；与每级 `ExpToNextLevel=1` 对齐，便于手验公共经验→升级 |
| 获得矿灯 | 点一次：`TryAcquire("Equip_MinerLamp")`（首获 / 同 Id 转化连升 / 满级转公共池）；日志打印 Level / CurrentExp / Q4·Q5·Q6 `GraveSpawnWeightBonus`。正式装备 UI 后置；手验 D-060 |
| 划入矿灯升级 | 点一次：`TrySpendCommonExp("Equip_MinerLamp", 1)`（池不足或未拥有则日志失败）；与每级 `ExpToNextLevel=1` 对齐 |

- 仅 Dig 进行中（未归零 / 未弹 Summary）可用；为 Demo/手验工具。
- GM 直接写入仓库的躯体材料 **不**计入 DigStageSummary「本阶段已获奖励」。
- 实现见 [SPEC_04 §6 Dig 垂直切片](SPEC_04_Technical.md)。

```
EffectiveDigDuration countdown → 0
  → Stop spawn; cancel in-progress DigAction (no damage)
  → DigStageSummary popup (aggregate rewards earned this Dig stage; no extra grants)
  → Player confirm (top-right X) → §3.9 next stage / VictorySettlement
```

### English

**Status: Defined (spawn / effective duration / dig interaction & reward credit / obstacle geometry / four Dig tech-bound capabilities / no win-lose / DigStageSummary; TechTree framework in §3.13; concrete node values still TBD)**

When the current Level stage has `GameplayType = Dig`, use the DigGameplayConfig row matching `GameplayConfigId`. Grave `maxHP` and loot come from GraveQualityConfig ([SPEC_04 §9.3](SPEC_04_Technical.md)).

**Map**

| Rule | Notes |
|------|-------|
| Presentation | Unity **Isometric Tilemap** diamond floor tiles; orthographic camera (no perspective); gameplay remains XZ top-down |
| Visual asset | Stage `DigGameplayConfig.DigMapId` → Prefab logical name `Ground_01`…`Ground_05` (**shared** ground-variant pool with Defend `BattleMapId`); Tile/Sprite under `Assets/Art/Maps/Tiles/` (copied from Example Scene `Environment/Tiles`+`Sprites`); runtime only `Assets/Prefabs/Maps/{Id}.prefab`; **do not** reference `SmallScaleInt/` — [SPEC_04 §9.2 / §13 / §15](SPEC_04_Technical.md) |
| Logic | **One continuous placeable space** (**IsoDiamond** XZ Manhattan diamond aligned to Isometric tile silhouette), not a cell grid; sample inside the diamond; Tilemap is presentation-only; `DigMapBounds` half-extents = `PaintRadius*(cellSize.x,cellSize.y)` (anisotropic OK; Demo ≈`(5,2.5)`) |
| Placeable | Candidate must be **inside IsoDiamond** and **not** intersect any **DigObstacle** circle |

**Obstacles (DigObstacle)**

Only this type this stage (no other obstacle types yet):

| Type | Notes |
|------|-------|
| Grave | Spawned and not yet cleared (HP > 0); obstacle size on **that quality's Grave Prefab** (one Prefab per quality; circle radius) |

- Dig stage does **not** spawn a map-center Digger entity and has **no** protagonist obstacle circle.
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
4. Placement must avoid uncleared Grave obstacle circles; retry up to **32** times per spawn attempt, then **abandon that spawn**.

**Ongoing spawn**

- While countdown runs, every N seconds attempt to spawn M graves per the rate field.
- Each grave: filter weights → (abandon if effective list empty) → weighted quality pick → random placeable position (same retry rule) → HP from quality table.
- Same weight field and zero-weight drop rule as initial spawn.

**Digger presentation**

| Rule | Notes |
|------|-------|
| Map entity | Dig stage does **not** Instantiate a map-center Digger model (no whole-character Visual, no idle/dig anim drive) |
| HUD portrait | Dig HUD **top-left** fixed **60×60** (canvas reference-resolution units) frame showing protagonist portrait; Demo may use placeholder tint/sprite; swap formal art later |
| Prefab | `Digger` Prefab / art pipeline may remain ([SPEC_04 §15](SPEC_04_Technical.md)) but is **not** a runtime Dig-stage world entity |

**DigProtagonistCapabilities**

Bound to the **save-slot protagonist**; written by tech-tree learns (rules & tables: [§3.13](#313-科技树techtree) / [SPEC_04 §9.16–§9.17](SPEC_04_Technical.md); this batch defines capability semantics and formulas only):

| Capability | Notes |
|------------|-------|
| DigDamage | Per-DigAction damage to the grave; Demo initial value **25**, provided by default-unlocked tech |
| DigDurationReductionSum | Sum of all unlocked dig-action-duration shorten effects (seconds) |
| DigCursorRadius | Circle cursor radius (world units); Demo initial value **0.6**, provided by default-unlocked tech |
| DiggableQualityIds | Set of Grave Quality Ids that may trigger DigAction |
| DigStageDurationBonus | Additive Dig-stage effective-duration bonus (seconds; see Effective Dig duration) |
| GraveSpawnWeightBonus | Per-QualityId additive spawn-weight map; attr key `GraveSpawnWeightBonus_{QualityId}` (see [SPEC_04 §9.17](SPEC_04_Technical.md)); missing Id in table `GraveSpawnWeights` treated as 0 then bonus |

**Dig action duration (dig speed):**

`DigActionDuration = max(0.1, BaseDigDuration − DigDurationReductionSum)` where `BaseDigDuration = 0.8` (seconds). Minimum dig time is **0.1s**.

(Distinct from Effective Dig duration: that is the stage countdown length; this is single DigAction anim/resolve duration.)

**Cursor & dig trigger**

| Rule | Value |
|------|-------|
| Cursor | On Dig stage enter, pointer becomes a **circle range**; radius = `DigCursorRadius` (circle, not square) |
| DigHitShape | Per-quality Grave Prefab: offline-baked **local XZ convex hull** (≤12 verts, silhouette-approx); separate from `DigObstacle` circle radius. If no valid hull, fall back to that Prefab's `DigObstacle` radius as a circle |
| Hit test | Uncleared graves whose `DigHitShape` (world XZ) **intersects** the cursor circle are candidates; rules use pure geometry — **no** runtime Sprite/pixel reads. Busy visual scale does **not** enlarge the hit shape |
| Cursor visuals | Screen-space UI Prefab `UiDigCursorRing` (`Assets/Prefabs/Dig/`): outer stroke + inner white semi-transparent fill; diameter = screen projection of `DigCursorRadius` in pixels ÷ Dig HUD `Canvas.scaleFactor` (written to `sizeDelta`, avoiding CanvasScaler double-scale); **stroke thickness stays constant in screen pixels** |
| Trigger | While ≥1 diggable non-busy grave intersects the hit shape, circle continuously dwells **≥ 0.2s** → start one DigAction on **each** currently eligible grave **in parallel** |
| Diggable gate | If that grave's Quality Id is **not** in `DiggableQualityIds` → **exclude** from this trigger (such graves may still spawn) |
| Busy lock | **Per grave**: if that grave is already in DigAction, **do not** re-trigger until it ends; **no** global lock that blocks new DigActions while any dig is active. New idle diggable graves entering the radius may start after another 0.2s dwell; leaving the radius does **not** cancel in-progress DigActions |

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
2. While that anim plays, rules **resolve** that quality's `DropMode` + `LootDrop` (encoding: [SPEC_04 §9.3](SPEC_04_Technical.md)) into a settled `Id_Count` list, then spawn the reward icon at the anim **center**. Empty resolve → no reward icon.
3. Reward icon then **flies to the Digger**; **on arrival** credit the settled list per rules below, then the icon disappears.

**Warehouse & SpiritEssence credit**

| Rule | Notes |
|------|-------|
| Warehouse | Persist per **SaveSlot**; **unlimited slots and retention time** |
| Material stacks | Non-currency rewards stack by **MaterialId**; per-type cap constant **10000** |
| SpiritEssence | Currency; **not** stacked as material; from Dig (LootDrop reserved Id + overflow AutoConvert); spent when **manufacturing soldiers** (§3.11) |
| Credit timing | When DigReward **arrives** at the **portrait frame center** |

For each settled `Id_Count` (not the raw table `Id;Weight;Count`):

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
| Stage settlement | Show **DigStageSummary** (UI-011): aggregate **rewards already earned this stage** by type; **no extra grants** (distinct from level `VictorySettlement`). Body-part lines `{DisplayName} Lv{BodyLevel} × count` (empty `DisplayName` → `BodyPartId`); Spirit / non-body still `{Id} × count`. Demo panel `SummaryRoot` **1020×1150**, `Body` **920×1030**; dismiss via top-right "X" (`ConfirmButton`) |
| After confirm | Player taps top-right "X" → §3.9 next stage / (if last) `VictorySettlement` |

**Demo GM (Dig HUD)**

| Button | Behavior |
|--------|----------|
| Add Graves | One click: weighted pick via **current effective weights** (table `GraveSpawnWeights` + caps `GraveSpawnWeightBonus`); placement / obstacle / 32-retry same as initial & process spawn; attempt **10** times; fewer than 10 if no space or empty effective weights |
| Add Body Parts | One click: for **every** loaded `Manufacture_BodyPartConfig` row, `Warehouse.AddItem(BodyPartId, 10)` (stack cap 10000; **no** LootDrop / AutoConvert) |
| Equip Warrior Enhance | **Mode2 only**: one click `SpecialEquipSlotsService.TryEquip("MagicBook_WarriorEnhance")` (stackable; full slots → fail log/Tips). Formal equip UI later; hand-check D-058 |
| Grant Iron Shovel | One click: `ProtagonistEquipmentService.TryAcquire("Equip_IronShovel")` (first / same-Id convert level-up / maxed→common pool); log Level / CurrentExp / `DigCursorRadius`. Formal equip UI deferred; hand-check D-059 |
| Equip Common Exp +50 | One click: GM inject `EquipCommonExp += 50` (`DebugGrantCommonExp`); log pool + warehouse |
| Spend Into Iron Shovel | One click: `TrySpendCommonExp("Equip_IronShovel", 1)` (fail log if pool short / not owned); matches per-level `ExpToNextLevel=1`; hand-check common Exp → level-up |
| Grant Miner Lamp | One click: `TryAcquire("Equip_MinerLamp")` (first / same-Id convert level-up / maxed→common pool); log Level / CurrentExp / Q4·Q5·Q6 `GraveSpawnWeightBonus`. Formal equip UI deferred; hand-check D-060 |
| Spend Into Miner Lamp | One click: `TrySpendCommonExp("Equip_MinerLamp", 1)` (fail log if pool short / not owned); matches per-level `ExpToNextLevel=1` |

- Available only while Dig is active (before duration zero / Summary). Demo / hand-check tools.
- Body parts granted via GM **do not** count toward DigStageSummary “rewards earned this stage”.
- Impl: [SPEC_04 §6 Dig vertical](SPEC_04_Technical.md).

```
EffectiveDigDuration countdown → 0
  → Stop spawn; cancel in-progress DigAction (no damage)
  → DigStageSummary popup (aggregate rewards earned this Dig stage; no extra grants)
  → Player confirm (top-right X) → §3.9 next stage / VictorySettlement
```

---

## 3.11 升级与制造（UpgradeManufacture）

### 简体中文

**状态：框架已关闭（规则库）；升级配置表结构、关卡失败经验边界、士兵属性构成（含宝石五维、种族、按项 FinalStat+下限、StaticStat 分层、职业 ClassId/ClassConfig（含 PrimaryStat、CombatConvertCoeffs 编码、AttackRange 等命中列、`DefaultSkillIds`）、士兵技能 `SoldierSkills`（职业默认 Lv1 烘进实例；Mode1 不读魔法书升技能；PermanentDeath 删除）、生命维例外 MaxHP=ceil(BodyLife+Str×MaxHpStrengthMult)（系数见 `CombatConstantConfig`）、士兵制造流程/槽位/命名、躯体材料表与 Base(S)=Σ StatBonus、躯体外观选取（含保底外形）、失控程度/四档/叛变判定与概率公式、士兵死亡分层（CombatDead / PermanentDeath / 宝石特例）已关闭；科技树框架见 §3.13；士兵战斗选敌/攻击距离/命中/普攻·攻速·技能CD 派生见 §3.12（换算缺键回退常量表）；躯体/外观/灵魂·职业·宝石·种族表具体数值 / 失控与技能效果表具体数值行仍 TBD。Mode2 士兵制造见 §3.15（自动制造）；本节为 Mode1 手动制造权威。**

**Mode2 差分（进入本阶段时）**

| 规则 | 说明 |
|------|------|
| 前置 | Mode2 样例关卡在 Dig 与本阶段之间插入 `AutoManufacture`（§3.15）；本阶段开始时士兵已由自动制造入池并可已上阵 |
| 手动制造 | **关闭 / 隐藏** ManufactureZone 与制造按钮；**不可**手动拖料造兵（Demo：`UpgradeManufactureStageRoot_Mode2.prefab` 上 ManufactureZone 默认关；Catalog 按 CampaignMode 选型，见 [SPEC_04 §6](SPEC_04_Technical.md)） |
| 升级 | 保留「GM升级」Modal（与 Mode1 同） |
| 布阵 | 保留「布阵」打开共享 FormationEditor；可再编辑自动上阵结果 |
| 布阵内完成 | Mode2 `FormationEditorRoot_Mode2`：`SoldierBar` **上方右侧**近屏边常驻 `CompleteButton`（文案同主屏「完成 / 进入下一阶段」）；**UM / Defend·PushMap Prepare 均显示**；点击语义同主屏完成 → **结束本 UM 阶段**（仅 UM 宿主接线；Prepare 宿主不订阅，按钮仍可见） |
| 制造记录 | 「布阵」**右侧**「制造记录」打开只读 Modal（UI-015）；展示最近一批 AutoManufacture 士兵摘要；详见 §3.15 |
| Spirit / Control | Mode2 **屏蔽**：制造不计 `SpiritCost`；布阵 HUD **不**显示控制力占用（失控专题另议；本轮不按 ControlPower 拦上阵） |
| 灵魂 | 自动造兵路径 **不写** `SoulId`；灵魂手动装配 **后续需求**（§3.15） |

当关卡当前阶段 `玩法类型 = UpgradeManufacture` 时进入本阶段。本阶段包含三条并列能力：**升级**、**制造士兵**、**战斗布阵**。配置表载体与字段编码见 [SPEC_04 §9](SPEC_04_Technical.md)（升级表见 **§9.8 `ProtagonistLevelConfig`**；灵魂表见 **§9.9 `SoulConfig`**；职业表见 **§9.9b `ClassConfig`**；宝石表见 **§9.10 `GemConfig`**；种族表见 **§9.11 `RaceConfig`**；躯体材料见 **§9.12 `BodyPartConfig`**；躯体外观见 **§9.13 `BodyAppearanceConfig`**；额外装备 / 宝石后缀见 **§9.14–§9.15**；失控表见 **§9.20 `LossOfControlConfig`**；完整数值仍 **TBD**）。

**界面组织（UI）**

| 规则 | 说明 |
|------|------|
| 布局 | **默认全屏制造区（ManufactureZone）**；升级区为 **Modal 弹窗**（非同屏并列、非 Tab）；布阵 **不** 同屏嵌入，由「布阵」按钮打开共享编辑器 |
| 升级入口 | 主屏 **顶部左侧**「GM升级」打开升级 Modal；Modal **右上角「X」** 关闭；Modal 内为升级状态与 Debug 注入等控件 |
| 完成入口 | 屏幕 **底部** 常驻「完成 / 进入下一阶段」按钮（与制造操作钮同底栏分区）；点击即触发阶段结束（§3.11 阶段结束） |
| 布阵入口 | 「完成」按钮 **右侧**「布阵」；点击打开 **FormationEditor**（见下「战斗布阵」）；编辑器内「返回」关闭编辑器回到本主屏 |
| 布阵编辑器 | 与 Defend `Prepare` **共用同一套** `FormationEditor` Prefab / 逻辑（写同一 BattleFormation） |
| 制造区控件 | 见下「制造区布局」与「制造士兵」；Prefab：`Assets/Prefabs/UpgradeManufacture/UpgradeManufactureStageRoot.prefab` |
| UI 清单 | 见 §3.6 `UI-010` |

**制造区布局（ManufactureZone）**

| 区域 | 说明 |
|------|------|
| PreviewPanel | 界面 **最左侧**（库存栏左侧）：属性/精魂等 **文本预览** |
| 中心槽位环 | 中部：中心为「士兵预览」；周围方格为各部位 `SlotRowTemplate` |
| 槽位环方位 | **左**（上→下）：头、手臂1、腿1、**翅膀**；**右**（上→下）：躯干、手臂2、腿2、**坐骑**；**预览区内底部**：灵魂；**预览下方**：6 宝石格（边长为其它部位格的 **一半**） |
| PoolPanel | 界面 **最右侧**（库存栏右侧）：士兵池 **可滚动士兵框列表**（每兵一框，自上而下；点击选中后框内出现「再造1个」） |
| InventoryColumn | **底部** 横滑方格栏（交互/尺寸对齐布阵 `SoldierBar`）；每项一格 |
| 操作钮 | `GrantKitButton` / `ClearSlotsButton` / `ManufactureButton` 在库存栏 **下方**；再与「完成 / 布阵」同属底栏分区 |
| 交互 | 库存 → 槽位为 **拖拽**（对齐 Formation 士兵栏 Input 驱动）；类型不符拒绝；可从已填槽位移出 |

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
材料按槽拖入 → 每次成功拖入/移除后刷新文本预览（角色信息、属性变更、精魂消耗）
→（可选）头+躯干+臂×2+腿×2+坐骑+翅膀已填时展示躯体外观可视预览（灵魂/宝石不参与闸门）
→ 玩家点「制造」（最低材料齐 + 精魂足够）→ 播放制造动画 → 生成士兵实例
```

| 步骤 | 规则 |
|------|------|
| 拖入 | 仅接受对应槽位类型的材料；类型不符 → 拒绝 |
| 文本预览 | 每次槽位变化后在 PreviewPanel 展示：角色信息、相对当前方案的属性变更、**当前总精魂消耗**、试算种族/外观 Id / 命名 |
| 躯体外观可视预览 | **闸门**：头+躯干+臂×2+腿×2+坐骑+翅膀已填（**灵魂与宝石不参与闸门**）。未满足 → 显示静态占位图（资源可后换）；满足 → 按试算 `AppearanceId` 展示士兵外观，先播一遍攻击再循环待机（无 Animator 则静态降级） |
| 制造按钮 | 最低材料要求满足 **且** `SpiritEssence ≥` 总精魂消耗 → 可点；否则 **不可制造**（按钮禁用或点击无效，二选一即可）。**制造闸门不变**（头/宝石/坐骑/翅膀对提交仍可选） |
| 动画 | 制造动画为表现层；规则层在确认消耗后提交生成 |
| 完成时 | 扣除材料与精魂；定稿种族与 **躯体外观**；写入属性快照、`AppearanceId` 与 `WarriorName`；按最终 `ClassId` 授予 `SoldierSkills`（见下「士兵技能授予」）；写入 **消耗材料配方**（各非空槽 `ItemId` 列表）与当时 **精魂总消耗**；实例进入可上阵池；**池与布阵均按存档槽立即持久化**（进档加载、删档清空；见 [SPEC_04 §6](SPEC_04_Technical.md)） |
| 士兵框 | PoolPanel 内每名士兵一框：展示 `Id`、名称、剩余 HP（已上阵可标〔上阵〕）；点击选中 → 框内显「再造1个」（无配方快照的旧实例不可再造） |
| 再造 | 以该兵配方 **后台** 再走制造流水线（不改动当前制造槽）；成功则 **新增** 池内士兵（原兵保留）；失败不扣料 |
| 再造不足 Tips | 材料同 Id 数量不足 → 取消，屏幕中上部 Tips「材料不足」停留 **1 秒**；精魂不够 → Tips「精魂不足」停留 **1 秒** |

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

必填：**1 躯干 + 2 手臂 + 2 腿**。头部、灵魂、宝石、坐骑、翅膀均为 **可选**。无灵魂时：实例 `SoulId = Soul_00`；`AttackMode` / 技能 / 攻击优先级 / 移动风格 / 灵魂侧 `SpiritCost`·`ControlPowerCost` 读 `Soul_00`；**强制** `ClassId = Class_Servants`（不扣仓库灵魂）。有灵魂时：消耗该灵魂；`ClassId` 取自该灵魂。

**精魂消耗闸门**

| 规则 | 说明 |
|------|------|
| 总消耗 | `TotalSpiritCost = Σ SpiritCost`（已放入的躯体部位、灵魂、外置装备、宝石；缺省项为 0；**无灵魂槽时仍计入 `Soul_00.SpiritCost`**） |
| 字段来源 | 各材料/灵魂/外置/宝石配置表的 `SpiritCost`（[SPEC_04 §9](SPEC_04_Technical.md)）；具体数值 **TBD** |
| 不足 | 材料齐但精魂不够 → **不能制造** |

**种族定稿（默认同族 / 否则亡灵）**

| 规则 | 说明 |
|------|------|
| 参与部位 | 已放入的 **头部、躯干、手臂×2、腿×2**；空槽 **不**参与 |
| 默认定稿 | 参与部位 **全部** `RaceId` 相同 → 定稿为该族；否则定稿为 **`Race_Undead`**（不论各部位等级） |
| Mode2「还原」 | 若特殊装备槽已装备 `EffectPayload=RaceWeightPick` 的魔法书（「还原」），则改为各部位权重 **1** 加权随机（旧规则）；**Mode1 不读**魔法书，始终用默认定稿 |
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
| 2b. A 空→亡灵 | 若 A **为空**：将定稿种族改为 **`Race_Undead`**，重载 `RaceAdjustCoeff`，重建 `WarriorName` 种族段，再从步骤 2 **仅重跑一轮**（防循环）；本轮 A 仍空 → 走步骤 4→5 |
| 3. 职业倾向 | 若 A 非空：子集 B = `ClassAffinity` 含 `ClassConfig.ClassName`（经实例 `ClassId`：有灵魂取自该灵魂，无灵魂为 `Class_Servants`）的行；B 非空 → 在 B 中均匀随机；**B 为空（职业不匹配）→ 不采用 A，改走步骤 4 同种族保底**（**不**因职业不匹配改亡灵） |
| 4. 保底外形 | A 非空但 B 为空，或亡灵重跑后 A 仍空：取**当前**定稿种族 `IsFallback == 1` 的行（每种族至多配置 1 个；常规行为空/`0`） |
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
| 职业名 | 实例 `ClassId` → `ClassConfig.ClassName`（有灵魂取自该灵魂；无灵魂为 `Class_Servants`） |
| 后缀 Suffix | 无宝石可空；有宝石时由 **`GemSuffixNameConfig`** 按已镶嵌 `GemType` 排序拼接 `ComboKey` 解析 |

**士兵属性构成**

士兵属性由下列部件构成：**士兵信息**、**基础属性**、**种族**、**灵魂**、**职业**、**士兵技能**、**额外装备属性**、**宝石**、**控制力占用值**。进入战场时的最终单项数值另叠加 **技能 Buff 系数**（仅运行时）、**宝石放大**与 **种族调整**。实例 **职业（ClassId）**：有灵魂取自该灵魂；无灵魂强制 `Class_Servants`。职业定 **ClassName**、**主属性（PrimaryStat）**，以及对五维→战斗参数的 **换算系数调整**（`ClassConfig.CombatConvertCoeffs`；编码与公式见 [SPEC_04 §9.9b](SPEC_04_Technical.md) / §3.12）。三维经 StaticStat / FinalStat 后派生战斗数值（见下与 §3.12）。

| 部件 | 规则 |
|------|------|
| 士兵信息（WarriorInfo） | 主标签 = 定稿 **种族**；仅标签 / 展示 / 分类，**不**直接改变数值。数值调整 **仅** 走「种族」与 `RaceAdjustCoeff` |
| 基础属性（BaseStats） | 由制造所用 **躯体部位** `StatBonus` 按维求和：`Base(S)=Σ StatBonus(S)`（见上）。固定五项：**生命值、移动速度、力量、敏捷、智力**。选敌/攻击距离/命中/死亡见 §3.12；普攻/攻速/技能CD/最终血量派生见下与 §3.12 |
| 种族（Race） | 由躯体部位加权随机定稿（见上）；数据来自 **`RaceConfig`**（[SPEC_04 §9.11](SPEC_04_Technical.md)）。提供 **五维** `RaceAdjustCoeff`（缺省维 **0**；可正可负）。**不**单独计入 `ControlPowerCost` |
| 灵魂（Soul） | 槽位 **可选**；数据来自 **`SoulConfig`**（[SPEC_04 §9.9](SPEC_04_Technical.md)）。有灵魂：消耗该行，写入其 `SoulId`/`ClassId`/`AttackMode`/技能/优先级/`MoveStyle`/SpiritCost/ControlPowerCost。无灵魂：不扣仓库；`SoulId=Soul_00`；其余灵魂侧字段读 `Soul_00`；**强制** `ClassId=Class_Servants`。`AttackMode ∈ { Melee, Ranged }`。**不**改写三维属性本身；**第一版 Demo 不施放技能**（见 §3.12） |
| 职业（Class） | 由实例 `ClassId` 解析 **`ClassConfig`**（[SPEC_04 §9.9b](SPEC_04_Technical.md)）。提供：`ClassName`（命名与外观 `ClassAffinity`）、`BaseClass`（基础职业：`战士`/`射手`/`法师`/`盗贼`；**预留**后续魔法书等条件，**不**参与命名/外观/`PrimaryStat`/战斗派生）、`PrimaryStat ∈ { Strength, Agility, Intelligence }`、`CombatConvertCoeffs`（`键_数值|…`；缺键/空串回退 **`CombatConstantConfig`**）、以及 `AttackRange` / `MeleeWindupSeconds` / `RangedProjectileSpeed` / `RangedTimeoutSeconds`、`DefaultSkillIds`（制造默认士兵技能）。示例语义：战士→Strength、射手→Agility、法师→Intelligence、仆从（`Class_Servants`）→与战士同主属性样例（以 `PrimaryStat` 为准，非 ClassName 硬编码） |
| 士兵技能（SoldierSkills） | 实例绑定列表 `{ SkillId, SkillLevel }[]`；权威表 **`SkillConfig`**（[SPEC_04 §9.21](SPEC_04_Technical.md)）。制造时由最终 `ClassId` 的 `DefaultSkillIds` 授予（见下）；**无**消耗经验升级。灵魂/宝石/外置 `Skills` **并行**（同 Id 合并 **TBD**）。**第一版 Demo 不施放**（§3.12） |
| 额外装备属性 | 外置装备提供的同名平坦属性加成与/或额外技能；制造时写入实例并锁定；并提供 `NamePrefix` |
| 宝石（Gem） | 可选；最多 6 颗（类型互斥）；数据来自 **`GemConfig`**（[SPEC_04 §9.10](SPEC_04_Technical.md)）。提供：**五维** `GemMult` + **额外技能**（各宝石技能集合并与灵魂技能 **并存**；冲突/覆盖 **TBD**）。无宝石时五维皆 **0**；多颗时实例各维 `GemMult(S) = Σ` 已镶嵌宝石的 `GemMult(S)` |
| 控制力占用值（ControlPowerCost） | 制造完成时定稿：`ControlPowerCost = BodyCost + SoulCost + EquipCost + GemCost`（无装备/无宝石则对应项为 0；多宝石 `GemCost` 为各宝石占用之和；种族与职业不另加项） |

**士兵技能授予（制造时；Mode1 权威）**

| 规则 | 说明 |
|------|------|
| 时机 | 实例 `ClassId` **最终定稿之后**（有灵魂取自该灵魂；无灵魂 `Class_Servants`）。Mode1 **不读**魔法书（与种族定稿一致），不跑 `SoldierSkillLevelAdd` |
| 来源 | 最终职业行 `ClassConfig.DefaultSkillIds`（[SPEC_04 §9.9b](SPEC_04_Technical.md)）。空 = 无技能；否则 `SkillId` 或 `SkillId\|SkillId`（Demo 预期 0 或 1 个） |
| 初始等级 | 每个授予的 `SkillId` 写入 `{ SkillId, SkillLevel=1 }`。无 `(SkillId, 1)` 行 → 跳过该 Id + Warning。重复 Id **保留首次** |
| 写入 | `WarriorInstance.SoldierSkills`；随士兵池快照持久化 |
| 升级 | **无**消耗经验升级（对比 §3.16 主角装备）。等级只来自：默认 1；Mode2 另见 §3.15 `SoldierSkillLevelAdd` |
| Mode1 UI | **不做**制造时手动选/加技能 |
| 再造 | `TryRemanufacture` 产出**新**实例，按新实例当时的最终 `ClassId` 重新授予（Mode1 仍为 Lv1） |

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
MaxHP = ceil(BodyLife + Str × MaxHpStrengthMult)
```

| 规则 | 说明 |
|------|------|
| BodyLife | 制造时锁定；**不含** GemMult / RaceAdjust / SkillBuff 对生命维的放大 |
| Str | 静态展示用 `StaticStat(Strength)`；战斗运行时用 `FinalStat(Strength)` |
| MaxHpStrengthMult | 读 **`CombatConstantConfig`** 键 `MaxHpStrengthMult`（样例默认 **3**）；缺表键实现可 Warning + 兜底 3 |
| SkillBuff(MaxHP) | **本批不读**；Buff 改力量则经 `Str×MaxHpStrengthMult` 间接影响血量 |
| RemainingHP 上限 | 开战时算出的 `MaxHP`；若布阵已存 `RemainingHP` 超过新上限 → **钳制**为新上限 |
| 静态展示 MaxHP | `ceil(BodyLife + StaticStat(Strength)×MaxHpStrengthMult)` |

**士兵实例静态快照（制造完成时写入；伪结构）：** 见 [SPEC_04 §9.9](SPEC_04_Technical.md) / [§9.10](SPEC_04_Technical.md) / [§9.11](SPEC_04_Technical.md)。

**士兵死亡与材料去向**

| 规则 | 说明 |
|------|------|
| 分层 | **战斗死亡（CombatDead）** ≠ **彻底死亡（PermanentDeath）**；物资与实例清除 **仅** 在彻底死亡时执行（判定细节见 §3.12） |
| 战斗死亡 | 无宝石士兵 `HP ≤ 0` → 进入 `CombatDead`：停用战场行为；**可**被战斗中复活技能拉起（技能专题 TBD）；**不**回仓、**不**毁材料、**不**移除实例；`SoldierSkills` **保留** |
| 彻底死亡触发 | ① 本阶段胜利进入 `Ended`，或 **LevelFailure** 结算时：仍为 `CombatDead` 且无「战斗结束复活」类技能 → PermanentDeath；② **宝石特例**：实例 `GemIds` **非空** 时，`HP ≤ 0` → **立即** PermanentDeath（跳过可复活的战斗死亡态） |
| 宝石 | 彻底死亡时：实例 `GemIds` 中全部宝石 → **自动回主角仓库**；**不**随死亡销毁 |
| 其余材料 | 彻底死亡时：躯体部位、灵魂、外置装备，以及制造时绑定到该士兵的其它材料 → **全部销毁**，**不**回仓 |
| 实例与布阵 | 彻底死亡时：该士兵实例从可上阵池移除（`SoldierSkills` **一并消失**，无可回收技能物品）；BattleFormation 中对应站位 **清空**（无士兵 ID / 视为空位） |

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
| 来源拆解 | `TierChance` = 当前锁定档 `LossOfControlConfig.LossOfControlChance`；`RaceBonus` = 定稿种族 `RaceConfig.LossOfControlChanceBonus`；`ΣGemBonus` = 实例已镶嵌各宝石该字段之和；`ΣSkillBonus` = 实例 `SoldierSkills` 按烘进等级查 `SkillConfig.LossOfControlChanceBonus` **之和**，再 **加** 灵魂/宝石/额外装备 `Skills` 列表解析之和（同 Id 是否去重 **TBD**；缺省 0） |
| 技能二次判定 | 仅当该士兵 `ΣSkillBonus ≠ 0` 时：每次 **释放技能** 后再用 **同一完整最终失控率** 独立 roll 一次；已叛变则跳过；**第一版 Demo 不施放技能，故不触发本判定** |
| 叛变（Rebel） | roll 成功 → 该士兵进入 **叛变**；持续至 **该士兵死亡**；细则见 §3.12 |
| 与开战关系 | 失控 **不阻止** Defend「开战」；开战门槛仅「上阵士兵 ≥ 1」（§3.12） |
| 布阵预览 | Prepare / 制造布阵区上下阵变更后立即重算 **当前** Degree/档次（供 UI）；真正判定仍以开战锁定值为准 |

**战斗布阵（BattleFormation）**

| 规则 | 说明 |
|------|------|
| 功能 | 安排已制造的士兵进入战场 |
| 持久化字段 | 至少保存：上阵士兵 **ID**、**位置**、**剩余血量**；与士兵池同槽本地持久化（变更立即写回；加载时丢弃池中不存在的孤儿站位） |
| 坐标系 | **BattleMap 连续坐标**（与 §3.12 连续可走空间一致；非格子） |
| 载体 | 共享 Prefab `FormationEditorRoot`（非独立 `.unity`）；画面与战斗地图一致（`Ground_*`） |
| UM 地图 | 当前关卡内查找 **下一** `GameplayType=Defend` 的 `BattleMapId`；找不到则 Demo 回退 `Ground_01` |
| Defend 地图 | 使用本阶段 `BattleMapId` 已 Instantiate 的地图实例（编辑器挂 UI，不重复造地图） |
| 可编辑时机 | **两处**写同一套数据：① UM「布阵」编辑器；② 防守 `Prepare` |
| 编辑器复用 | 两处 **同一套** `FormationEditor` UI / 逻辑 |
| 士兵栏 | 画面底部 UI：池内士兵以 **80×80** 方格左对齐向右排列（**已上阵也保留在栏内**）；栏内按住左右拖 = 横滑；方格文案上行为 `ClassName`（经 `ClassId` → `Manufacture_ClassConfig`；缺行回退 `ClassId`）、下行 `Lv.{ClassLevel}`（缺行按 0） |
| 上阵操作 | 左键按住方格 **向上拖** → 该格 **变亮**；拖出士兵栏后光标处出现 **Idle 待机模型** 跟手；在地图内松手 → `TryDeployAt` 写坐标（放下即存）；上阵后栏内该格 **保持变亮且不隐藏** |
| 改位 / 下阵 | 已上阵可在战场再拖改位（`TrySetPosition`）；拖回士兵栏或松手在 **地图外**（`DigMapBounds` 外）→ `TryUndeploy` / 取消上阵并回栏，同时 **关闭** 该格变亮 |
| 控制力 HUD | 画面左上角显示 `ΣControlPowerCost / ControlPowerCap` |
| 离开 | UM：「返回」关编辑器回主屏；Defend：「开战」（UI-009，≥1）关编辑器进 Combat |
| Mode2 完成钮 | 仅 `FormationEditorRoot_Mode2`：`SoldierBar` 上方右侧 `CompleteButton`（UM/Prepare **均显示**）；UM 宿主点击 = 关编辑器并触发与主屏相同的阶段结束；Mode1 Prefab **无**此钮 |
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
  → Manufacture: slots (Head/Torso/Arm×2/Leg×2/Soul/Gem×6 type-exclusive/Mount/Wing); min = Torso+2Arm+2Leg (Soul optional; empty → Soul_00 + Class_Servants)
       → preview on drag; TotalSpiritCost = Σ SpiritCost; gate on SpiritEssence
       → Race: same-race else Race_Undead (Mode2 Restore → weight-1); RaceConfig; write RaceId + RaceAdjustCoeff (5D)
       → Base(S)=Σ StatBonus(S); AppearanceId via BodyAppearanceConfig (avg BodyLevel→round; A empty→Race_Undead once; class affinity else race IsFallback; else table-random)
       → Gem: GemIds[]; GemMult(S)=Σ socketed GemMult(S) (5D; all 0 if none)
       → WarriorName = Prefix(es)+RaceName+ClassName+Suffix; WarriorInfo primary = Race
       → Grant SoldierSkills from ClassConfig.DefaultSkillIds at Level 1 (Mode1: no MagicBook level-up)
       → Warrior instance {Id, WarriorName, RemainingHP, RaceId, RaceAdjustCoeff, BaseStats, AppearanceId, SoulId, ClassId, AttackMode, LockedEquipIds, GemIds[], GemMult(5D), ControlPowerCost, SoldierSkills[]}
       → BodyPart/Appearance concrete value rows TBD
  → Formation: shared editor; BattleMap continuous coords; persist {WarriorId, Position, RemainingHP}
  → Deploy control: Cap = level-row ControlPowerCap (+ tech later); cost = instance ControlPowerCost; Degree = ΣCost/Cap − 1; tiers 1–4 + Rebel rolls (§3.11 / §3.12 / SPEC_04 §9.20); does not block StartBattle
  → Combat: StaticStat(S)=max(0, Base+Equip+Base×GemMult+Base×RaceAdjust); FinalStat adds Base×SkillBuff
       → MaxHP=ceil(BodyLife+Str×MaxHpStrengthMult); BodyLife=Base(MaxHP)+Equip(MaxHP); ClassId (soul or Class_Servants) → ClassConfig.PrimaryStat → attack/ASPD/CD (§3.12)
       → RemainingHP clamp to MaxHP on StartBattle
  → On PermanentDeath: all GemIds → Warehouse; BodyParts/Soul/ExtraEquipment/other bound materials destroyed; SoldierSkills dropped with instance; clear formation slot
  → CombatDead (no gems): no material fate until PermanentDeath (§3.12)
  → Gem exception: GemIds non-empty + HP≤0 → immediate PermanentDeath
  → Player confirms "Complete / Next stage" → no stage settlement → §3.9 next / VictorySettlement
```

### English

**Status: Framework closed (rules library); upgrade table schema, LevelFailure Exp boundary, soldier attribute composition (incl. five-dim Gem, Race; per-stat FinalStat + floor; StaticStat layer; Class ClassId/ClassConfig (incl. PrimaryStat, CombatConvertCoeffs encoding, AttackRange hit columns, `DefaultSkillIds`); soldier skills `SoldierSkills` (class default Lv1 baked; Mode1 ignores MagicBook skill level-up; dropped on PermanentDeath); HP-dim exception MaxHP=ceil(BodyLife+Str×MaxHpStrengthMult) (coeff from `CombatConstantConfig`)), soldier manufacture flow/slots/naming, BodyPartConfig + Base(S)=Σ StatBonus, BodyAppearance pick (incl. IsFallback), LossOfControlDegree / four tiers / Rebel rolls & chance formula, soldier death layers (CombatDead / PermanentDeath / gem exception) closed; TechTree framework in §3.13; WarriorCombat targeting / AttackRange / hit / NormalAttack·ASPD·SkillCD derives in §3.12 (missing convert keys → constants table); concrete Body/Appearance/Soul/Class/Gem/Race numbers / LossOfControl & skill-effect table concrete rows still TBD. Mode2 soldier manufacture is §3.15 (AutoManufacture); this section is Mode1 manual-manufacture authority.**

**Mode2 diffs (when entering this stage)**

| Rule | Notes |
|------|-------|
| Prefaced by | Mode2 sample Levels insert `AutoManufacture` between Dig and this stage (§3.15); soldiers may already be in pool/formation |
| Manual manufacture | **Hide/disable** ManufactureZone and craft button; **no** manual drag-craft (Demo: `UpgradeManufactureStageRoot_Mode2.prefab` has ManufactureZone off; Catalog resolves by CampaignMode — [SPEC_04 §6](SPEC_04_Technical.md)) |
| Upgrade | Keep "GM Upgrade" Modal (same as Mode1) |
| Formation | Keep Formation button → shared FormationEditor; auto-deploy results remain editable |
| Complete in editor | Mode2 `FormationEditorRoot_Mode2`: `CompleteButton` above `SoldierBar` on the **right**, near screen edge (same label as main Complete); **visible in UM and Defend/PushMap Prepare**; click = same as main Complete → **end UM stage** (only UM host wires it; Prepare hosts do not subscribe; button still visible) |
| Manufacture record | "Manufacture Record" to the **right** of Formation opens read-only Modal (UI-015); last AutoManufacture batch summaries; see §3.15 |
| Spirit / Control | Mode2 **shielded**: manufacture ignores `SpiritCost`; formation HUD **hides** ControlPower (LOC later; this round does not gate deploy by ControlPower) |
| Soul | Auto-craft path writes **no** `SoulId`; manual soul attach is a **later** topic (§3.15) |

Entered when Level stage `GameplayType = UpgradeManufacture`. Three parallel capabilities: **Upgrade**, **Manufacture soldiers**, **BattleFormation**. Config encodings: [SPEC_04 §9](SPEC_04_Technical.md) (**§9.8 `ProtagonistLevelConfig`**; **§9.9 `SoulConfig`**; **§9.9b `ClassConfig`**; **§9.10 `GemConfig`**; **§9.11 `RaceConfig`**; **§9.12 `BodyPartConfig`**; **§9.13 `BodyAppearanceConfig`**; ExtraEquipment / gem-suffix **§9.14–§9.15**; **§9.20 `LossOfControlConfig`**; soldier skills **§9.21 `SkillConfig`** / **§9.21b `SkillEffectConfig`**; concrete numbers still **TBD**).

**UI layout**

| Rule | Notes |
|------|-------|
| Layout | **Full-screen ManufactureZone by default**; Upgrade is a **Modal** (not side-by-side, not tabs); formation is **not** embedded — opened via Formation button |
| Upgrade entry | Top-left **"GM Upgrade"** opens upgrade Modal; Modal **top-right "X"** closes; Modal holds upgrade status + Debug inject |
| Complete entry | **Bottom** "Complete / Next stage" (same bottom band as manufacture action buttons); ends stage |
| Formation entry | "Formation" button to the **right** of Complete; opens **FormationEditor**; "Return" closes editor back to this main screen |
| Formation editor | **Same** `FormationEditor` Prefab/logic shared with Defend `Prepare` (same BattleFormation) |
| Manufacture widgets | See 「ManufactureZone layout」 and manufacture rules below; Prefab: `Assets/Prefabs/UpgradeManufacture/UpgradeManufactureStageRoot.prefab` |
| UI inventory | §3.6 `UI-010` |

**ManufactureZone layout**

| Region | Notes |
|--------|-------|
| PreviewPanel | **Far left** (left of inventory bar): attribute / Spirit **text preview** |
| Center slot ring | Middle: center **soldier visual preview**; surrounding squares are slot cells |
| Slot ring positions | **Left** (top→bottom): Head, Arm1, Leg1, **Wing**; **Right** (top→bottom): Torso, Arm2, Leg2, **Mount**; **bottom inside preview**: Soul; **below preview**: 6 gem cells (half the side length of other slot cells) |
| PoolPanel | **Far right** (right of inventory bar): scrollable **soldier frame list** (one frame per warrior, top→bottom; tap to select → show「Remake×1」in frame) |
| InventoryColumn | **Bottom** horizontal square bar (interaction/size aligned with Formation `SoldierBar`); one cell per item |
| Action buttons | `GrantKit` / `ClearSlots` / `Manufacture` **below** inventory; Complete / Formation share the bottom band |
| Interaction | Inventory → slots via **drag** (Formation soldier-bar Input style); reject type mismatch; can remove from filled slots |

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
Drag materials into slots → on each successful add/remove, refresh text preview (info, stat delta, Spirit cost)
→ (optional) when Head+Torso+Arm×2+Leg×2+Mount+Wing filled (Soul/gems optional), show BodyAppearance visual preview
→ player taps Manufacture (min parts filled + enough Spirit) → manufacture VFX → create soldier instance
```

| Step | Rules |
|------|-------|
| Drag | Accept only matching slot type; reject mismatches |
| Text preview | After each slot change, PreviewPanel shows character info, attribute deltas, **total Spirit cost**, trial Race / Appearance Id / name |
| Visual BodyAppearance preview | **Gate**: Head+Torso+Arm×2+Leg×2+Mount+Wing filled (**Soul and gems do not gate**). Else → static placeholder image (art swappable later); when met → show trial `AppearanceId` warrior, play attack once then loop idle (static fallback if no Animator) |
| Manufacture button | Enabled only if min requirements met **and** `SpiritEssence ≥` total Spirit cost; else **cannot manufacture**. **Manufacture commit gate unchanged** (Head/gems/Mount/Wing still optional for submit) |
| VFX | Presentation only; rules commit after cost confirmation |
| On complete | Deduct materials + Spirit; finalize Race and **BodyAppearance**; write snapshot, `AppearanceId`, `WarriorName`; grant `SoldierSkills` from final `ClassId` (see 「Soldier skill grant」 below); write **consumed material recipe** (non-empty slot `ItemId` list) and **Spirit cost at manufacture**; add to deployable pool; **pool + formation persist per SaveSlot immediately** (load on enter, clear on delete; [SPEC_04 §6](SPEC_04_Technical.md)) |
| Soldier frame | One frame per pool warrior in PoolPanel: show `Id`, name, remaining HP (〔Deployed〕 if on formation); tap to select → show「Remake×1」(no remake if recipe snapshot missing) |
| Remake | Re-run manufacture pipeline **in background** from that warrior's recipe (do **not** change current slots); success → **add** a new pool warrior (original kept); failure → no consume |
| Remake shortage Tips | Insufficient same-Id materials → cancel, upper-center Tips「材料不足」for **1s**; insufficient Spirit → Tips「精魂不足」for **1s** |

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

Required: **1 Torso + 2 Arms + 2 Legs**. Head, Soul, gems, mount, wings are **optional**. No Soul slotted: instance `SoulId = Soul_00`; AttackMode / skills / AttackPriority / MoveStyle / soul-side SpiritCost·ControlPowerCost from `Soul_00`; **force** `ClassId = Class_Servants` (do not consume warehouse Soul). Soul slotted: consume that Soul; `ClassId` from that Soul.

**Spirit cost gate**

| Rule | Notes |
|------|-------|
| Total | `TotalSpiritCost = Σ SpiritCost` of filled BodyParts, Soul, ExtraEquipment, Gems (missing = 0; **empty Soul slot still adds `Soul_00.SpiritCost`**) |
| Field source | `SpiritCost` on each config row ([SPEC_04 §9](SPEC_04_Technical.md)); concrete numbers **TBD** |
| Insufficient | Parts OK but Spirit short → **cannot manufacture** |

**Race finalization (same-race / else Undead)**

| Rule | Notes |
|------|-------|
| Participants | Filled **Head, Torso, Arm×2, Leg×2**; empty slots excluded |
| Default | All participant `RaceId`s **identical** → that race; else finalize **`Race_Undead`** (ignore part levels) |
| Mode2 Restore | If a MagicBook with `EffectPayload=RaceWeightPick` (Restore) is equipped → weight-**1** pick among parts (legacy); **Mode1 ignores** MagicBooks and always uses default |
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
| 2b. A empty → Undead | If A **empty**: force race to **`Race_Undead`**, reload `RaceAdjustCoeff`, rebuild WarriorName race segment, re-run from step 2 **once** only; if A still empty → steps 4→5 |
| 3. Class affinity | If A non-empty: subset B = rows whose `ClassAffinity` contains `ClassConfig.ClassName` (via instance `ClassId`: placed soul when present, else `Class_Servants`); if B non-empty → uniform random in B; **if B empty (class mismatch) → do not use A; go to step 4 same-race fallback** (class mismatch does **not** rewrite to Undead) |
| 4. Fallback | A non-empty but B empty, or Undead re-run still has empty A: current-race row with `IsFallback == 1` (at most one per race; normal rows empty/`0`) |
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
| Class name | Instance `ClassId` → `ClassConfig.ClassName` (placed soul when present; else `Class_Servants`) |
| Suffix | Empty if no gems; else **`GemSuffixNameConfig`** by sorted socketed `GemType` `ComboKey` |

**Soldier attribute composition**

A soldier is composed of: **WarriorInfo**, **BaseStats**, **Race**, **Soul**, **Class**, **SoldierSkills**, **ExtraEquipment stats**, **Gem**, and **ControlPowerCost**. Battlefield final per-stat values additionally apply **SkillBuffCoeff** (runtime only), **GemMult**, and **RaceAdjustCoeff**. Instance **Class (ClassId)**: from placed soul when present; else forced `Class_Servants`. Class supplies **ClassName**, **PrimaryStat**, and five-dim→combat-param **convert coeffs** (`ClassConfig.CombatConvertCoeffs`; encoding and formulas in [SPEC_04 §9.9b](SPEC_04_Technical.md) / §3.12). Those dims feed combat derives via StaticStat / FinalStat (below and §3.12).

| Part | Rules |
|------|-------|
| WarriorInfo | Primary label = finalized **Race**; display/taxonomy only (no numeric effect). Numeric adjust uses **Race** / `RaceAdjustCoeff` only |
| BaseStats | Sum of filled BodyPart `StatBonus` per dim: `Base(S)=Σ StatBonus(S)` (above). Fixed five: **HP, MoveSpeed, Strength, Agility, Intelligence**. Targeting / AttackRange / hit / death in §3.12; NormalAttack / ASPD / SkillCD / final MaxHP derives below and in §3.12 |
| Race | Weighted pick from BodyParts (above); data from **`RaceConfig`** ([SPEC_04 §9.11](SPEC_04_Technical.md)). Five-dim `RaceAdjustCoeff` (missing dim = **0**; may be +/-). No separate ControlPowerCost term |
| Soul | Slot **optional**; **`SoulConfig`** ([SPEC_04 §9.9](SPEC_04_Technical.md)). If filled: consume that row; write its SoulId/ClassId/AttackMode/skills/priority/MoveStyle/SpiritCost/ControlPowerCost. If empty: no warehouse consume; `SoulId=Soul_00`; other soul-side fields from `Soul_00`; **force** `ClassId=Class_Servants`. `AttackMode ∈ { Melee, Ranged }`. Does **not** rewrite the three dims; **Demo v1 does not cast skills** (see §3.12) |
| Class | Resolved from instance `ClassId` via **`ClassConfig`** ([SPEC_04 §9.9b](SPEC_04_Technical.md)): `ClassName` (naming + appearance `ClassAffinity`), `BaseClass` (base class: Warrior/Archer/Mage/Thief; **reserved** for future MagicBook conditions; **not** used in naming / appearance / `PrimaryStat` / combat derives), `PrimaryStat ∈ { Strength, Agility, Intelligence }`, `CombatConvertCoeffs` (`Key_Value|…`; missing key / empty → **`CombatConstantConfig`**), plus `AttackRange` / `MeleeWindupSeconds` / `RangedProjectileSpeed` / `RangedTimeoutSeconds`, `DefaultSkillIds` (default soldier skills at manufacture). Example semantics: Warrior→Strength, Archer→Agility, Mage→Intelligence, Servants (`Class_Servants`)→same PrimaryStat sample as Warrior (`PrimaryStat` wins; not ClassName hardcoding) |
| SoldierSkills | Instance list `{ SkillId, SkillLevel }[]`; catalog **`SkillConfig`** ([SPEC_04 §9.21](SPEC_04_Technical.md)). Granted at manufacture from final `ClassId` `DefaultSkillIds` (below); **no** exp-spend upgrade. Soul/Gem/ExtraEquipment `Skills` remain **parallel** (same-Id merge **TBD**). **Demo v1 does not cast** (§3.12) |
| ExtraEquipment stats | Flat same-named bonuses and/or extra skills; locked at manufacture; also supplies `NamePrefix` |
| Gem | Optional; up to 6 (type-exclusive); **`GemConfig`** ([SPEC_04 §9.10](SPEC_04_Technical.md)): **five-dim** `GemMult` + extra skills (union with Soul skills; conflict **TBD**). No gems → all dims **0**; multi-gem → instance `GemMult(S) = Σ` socketed `GemMult(S)` |
| ControlPowerCost | Finalized at manufacture: `BodyCost + SoulCost + EquipCost + GemCost` (0 for missing; multi-gem GemCost = sum; Race and Class add no term) |

**Soldier skill grant (at manufacture; Mode1 authority)**

| Rule | Notes |
|------|-------|
| When | After instance `ClassId` is **final** (from soul, or `Class_Servants`). Mode1 **ignores** MagicBooks (same as race finalize) and does **not** run `SoldierSkillLevelAdd` |
| Source | Final class row `ClassConfig.DefaultSkillIds` ([SPEC_04 §9.9b](SPEC_04_Technical.md)). Empty = none; else `SkillId` or `SkillId\|SkillId` (Demo expects 0 or 1) |
| Initial level | Each granted `SkillId` writes `{ SkillId, SkillLevel=1 }`. Missing `(SkillId, 1)` row → skip that Id + Warning. Duplicate Ids: **keep first** |
| Write | `WarriorInstance.SoldierSkills`; persist with WarriorPool snapshot |
| Upgrade | **No** exp-spend upgrade (contrast §3.16 ProtagonistEquipment). Level comes from default 1 only in Mode1; Mode2 also §3.15 `SoldierSkillLevelAdd` |
| Mode1 UI | **No** manual pick/add skills at manufacture |
| Remake | `TryRemanufacture` creates a **new** instance and re-grants from that instance's final `ClassId` (Mode1 still Lv1) |

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
MaxHP = ceil(BodyLife + Str × MaxHpStrengthMult)
```

| Rule | Notes |
|------|-------|
| BodyLife | Locked at manufacture; **excludes** GemMult / RaceAdjust / SkillBuff amplify on the HP dim |
| Str | Static UI uses `StaticStat(Strength)`; combat uses `FinalStat(Strength)` |
| MaxHpStrengthMult | From **`CombatConstantConfig`** key `MaxHpStrengthMult` (sample default **3**); missing key → Warning + fallback 3 |
| SkillBuff(MaxHP) | **Not read this batch**; Buffs that change Strength affect MaxHP via `Str×MaxHpStrengthMult` |
| RemainingHP cap | Combat `MaxHP` at StartBattle; if persisted `RemainingHP` exceeds new cap → **clamp** to new cap |
| Static MaxHP UI | `ceil(BodyLife + StaticStat(Strength)×MaxHpStrengthMult)` |

**Soldier instance static snapshot (written at manufacture):** see [SPEC_04 §9.9](SPEC_04_Technical.md) / [§9.10](SPEC_04_Technical.md) / [§9.11](SPEC_04_Technical.md).

**Soldier death & material fate**

| Rule | Notes |
|------|-------|
| Layers | **CombatDead** ≠ **PermanentDeath**; material fate and instance removal run **only** on PermanentDeath (criteria in §3.12) |
| CombatDead | Soldier with **no** gems and `HP ≤ 0` → `CombatDead`: battlefield actions disabled; **may** be revived by in-combat revive skills (skills topic TBD); **no** Warehouse return, **no** material destroy, **no** instance removal; `SoldierSkills` **kept** |
| PermanentDeath triggers | ① On stage victory enter `Ended`, or on **LevelFailure** settlement: still `CombatDead` and no end-of-battle revive skill → PermanentDeath; ② **Gem exception**: if instance `GemIds` **non-empty**, `HP ≤ 0` → PermanentDeath **immediately** (skip revivable CombatDead) |
| Gem | On PermanentDeath: all gems in `GemIds` → **auto-return to protagonist Warehouse**; **not** destroyed |
| Other materials | On PermanentDeath: BodyParts, Soul, ExtraEquipment, and other materials bound at manufacture → **all destroyed**; **not** returned |
| Instance & formation | On PermanentDeath: remove from deployable pool (`SoldierSkills` **dropped with the instance**; no recoverable skill item); clear that BattleFormation slot (empty / no soldier Id) |

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
| Sources | `TierChance` = locked tier `LossOfControlConfig.LossOfControlChance`; `RaceBonus` = finalized race `RaceConfig.LossOfControlChanceBonus`; `ΣGemBonus` = sum of socketed gems' field; `ΣSkillBonus` = sum of `SkillConfig.LossOfControlChanceBonus` over instance `SoldierSkills` at baked level, **plus** Soul/Gem/ExtraEquipment `Skills` lists (same-Id dedupe **TBD**; missing = 0) |
| Extra skill rolls | Only if this soldier's `ΣSkillBonus ≠ 0`: on **each skill cast**, roll again with the **same full FinalLossChance**; skip if already Rebel; **Demo v1 does not cast skills → this roll never fires** |
| Rebel | Successful roll → **Rebel** until **that soldier dies**; combat AI in §3.12 |
| vs StartBattle | Does **not** block StartBattle; only gate is ≥1 soldier (§3.12) |
| Formation preview | After deploy edits, recalc **current** Degree/tier for UI; combat rolls use the StartBattle-locked values |

**BattleFormation**

| Rule | Notes |
|------|-------|
| Function | Assign soldier instances onto the battlefield |
| Persisted fields | Warrior **Id**, **position**, **remaining HP**; same-slot local persistence with soldier pool (write on change; drop orphan slots whose WarriorId is not in pool on load) |
| Coordinates | **BattleMap continuous space** (same as §3.12; not a cell grid) |
| Carrier | Shared Prefab `FormationEditorRoot` (not a separate `.unity`); visuals match battle map (`Ground_*`) |
| UM map | Look up **next** `GameplayType=Defend` `BattleMapId` in the current Level; Demo fallback `Ground_01` |
| Defend map | Reuse this stage's already-instantiated `BattleMapId` map (editor hosts UI only) |
| Editable in | UM Formation editor **and** Defend `Prepare` (one dataset) |
| Editor reuse | **Same** `FormationEditor` UI/logic in both places |
| Soldier bar | Bottom UI: pool soldiers as **80×80** cells left-aligned (**deployed cells remain in bar**); horizontal drag inside bar = scroll; cell text: upper line `ClassName` (via `ClassId` → `Manufacture_ClassConfig`; missing row → fallback `ClassId`), lower line `Lv.{ClassLevel}` (missing row → 0) |
| Deploy | LMB hold cell, drag **up** → cell **highlights**; after leaving bar, Idle model follows cursor; release on map → `TryDeployAt` (persist immediately); deployed cell **stays highlighted and visible** |
| Reposition / undeploy | Drag deployed units to move (`TrySetPosition`); drag back to bar or release **outside map** (`DigMapBounds`) → `TryUndeploy` / cancel and **clear** cell highlight |
| ControlPower HUD | Top-left: `ΣControlPowerCost / ControlPowerCap` |
| Leave | UM: Return closes editor; Defend: StartBattle (UI-009, ≥1) closes editor → Combat |
| Mode2 Complete | `FormationEditorRoot_Mode2` only: `CompleteButton` above `SoldierBar` (right); **visible in UM and Prepare**; UM host click = close editor + same stage end as main Complete; Mode1 Prefab has **no** button |
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
  → Manufacture: slots (Head/Torso/Arm×2/Leg×2/Soul/Gem×6 type-exclusive/Mount/Wing); min = Torso+2Arm+2Leg (Soul optional; empty → Soul_00 + Class_Servants)
       → preview on drag; TotalSpiritCost = Σ SpiritCost; gate on SpiritEssence
       → Race: same-race else Race_Undead (Mode2 Restore → weight-1); RaceConfig; write RaceId + RaceAdjustCoeff (5D)
       → Base(S)=Σ StatBonus(S); AppearanceId via BodyAppearanceConfig (avg BodyLevel→round; A empty→Race_Undead once; class affinity else race IsFallback; else table-random)
       → Gem: GemIds[]; GemMult(S)=Σ socketed GemMult(S) (5D; all 0 if none)
       → WarriorName = Prefix(es)+RaceName+ClassName+Suffix; WarriorInfo primary = Race
       → Grant SoldierSkills from ClassConfig.DefaultSkillIds at Level 1 (Mode1: no MagicBook level-up)
       → Warrior instance {Id, WarriorName, RemainingHP, RaceId, RaceAdjustCoeff, BaseStats, AppearanceId, SoulId, ClassId, AttackMode, LockedEquipIds, GemIds[], GemMult(5D), ControlPowerCost, SoldierSkills[]}
       → BodyPart/Appearance concrete value rows TBD
  → Formation: shared editor; BattleMap continuous coords; persist {WarriorId, Position, RemainingHP}
  → Deploy control: Cap = level-row ControlPowerCap (+ tech later); cost = instance ControlPowerCost; Degree = ΣCost/Cap − 1; tiers 1–4 + Rebel rolls (§3.11 / §3.12 / SPEC_04 §9.20); does not block StartBattle
  → Combat: StaticStat(S)=max(0, Base+Equip+Base×GemMult+Base×RaceAdjust); FinalStat adds Base×SkillBuff
       → MaxHP=ceil(BodyLife+Str×MaxHpStrengthMult); BodyLife=Base(MaxHP)+Equip(MaxHP); ClassId (soul or Class_Servants) → ClassConfig.PrimaryStat → attack/ASPD/CD (§3.12)
       → RemainingHP clamp to MaxHP on StartBattle
  → On PermanentDeath: all GemIds → Warehouse; BodyParts/Soul/ExtraEquipment/other bound materials destroyed; SoldierSkills dropped with instance; clear formation slot
  → CombatDead (no gems): no material fate until PermanentDeath (§3.12)
  → Gem exception: GemIds non-empty + HP≤0 → immediate PermanentDeath
  → Player confirms "Complete / Next stage" → no stage settlement → §3.9 next / VictorySettlement
```

---

## 3.12 防守（Defend）

### 简体中文

**状态：框架已定义（ModeSelect 选模式/关卡 / 准备可改布阵/开战/部署/护盾/倒计时刷怪/寻路/胜负/失控叛变/士兵战斗选敌·AttackMode·攻击距离·命中方案D·死亡分层·普攻攻击值·攻速；Primary 取自 ClassConfig；CombatConvertCoeffs 与 AttackRange 等命中列见 ClassConfig / MonsterConfig；**第一版 Demo：士兵与怪物仅普通攻击、不施放技能**；SkillCooldown 公式与表结构保留但不驱动）；技能效果表、怪物→士兵伤害细节仍 TBD；**出生点 / NavMesh：Demo 最小约定已关闭（见下），精确 OutsideMap 几何后置****

当关卡当前阶段 `玩法类型 = Defend` 时进入本阶段。依赖 §3.11 **战斗布阵（BattleFormation）** 持久化数据。配置表载体见 [SPEC_04 §9.7](SPEC_04_Technical.md) `DefendGameplayConfig`、[§9.18](SPEC_04_Technical.md) `WaveSpawnConfig`、[§9.19](SPEC_04_Technical.md) `MonsterConfig`、[§9.20](SPEC_04_Technical.md) `LossOfControlConfig`。

**战斗模式与选关（BattleMode / BattleModeSelect，UI-013 / D-044）**

| 规则 | 说明 |
|------|------|
| 进入 | 进入 Defend 阶段后 **必须** 先 `DefendPhase = ModeSelect`，**不可**直接进入 `Prepare` |
| 模式1 | `BattleMode = Defend`，玩家可见名「**保卫战**」；规则 = 本节 Prepare→Combat 全套；关卡列表 = `DefendGameplayConfig` **全部**主键行 |
| 模式2 | `BattleMode = PushMap`，玩家可见名「**推图战**」；规则权威见 **§3.14**；关卡列表 = `PushMapGameplayConfig` **全部**主键行；确认后按所选行进入 §3.14 Prepare |
| 关卡 | 列表随当前模式切换；保卫战列出全部 `DefendGameplayConfig`；推图战列出全部 `PushMapGameplayConfig`；运作表 `GameplayConfigId` 仅作保卫战 **Recommended** 默认高亮；UM 布阵预览地图仍可读 Recommended→`BattleMapId`（可与玩家最终所选关卡不一致，本版可接受） |
| 确认 | 模式1 + 已选关卡 → 用所选行覆盖本阶段开战配置 → 进入保卫战 `Prepare`；模式2 + 已选关卡 → `LevelOperationDriver.TryHandoffModeSelectToPushMap` 卸 Defend ModeSelect，改写上下文为 PushMap，进入 `PushMapStageModule` §3.14 Prepare |
| 通关 | **任一模式**关卡胜利 → 本阶段胜利结算 → §3.9 `TryAdvanceStage`；失败：保卫战与推图战均为 Shield≤0→LevelFailure（推图战胜负见 §3.14） |

**阶段内子状态（DefendPhase）**

| 子状态 | 说明 |
|--------|------|
| `ModeSelect` | 进入 Defend 后的默认态：展示战斗模式与关卡选择（UI-013）；确认保卫战后进入保卫战 `Prepare`；确认推图战后交接离开本模块 |
| `Prepare` | 加载布阵、展示准备 UI（含「开战」）；**可编辑布阵**（与 §3.11 **同一套**布阵 UI/逻辑）；写回同一 BattleFormation；不可制造新士兵 |
| `Combat` | 点击「开战」后：按**当前**布阵部署单位、护盾与战斗倒计时、刷怪、寻路与战斗结算运行中 |
| `Ended` | 本阶段已因胜利结束，或因关卡失败中止 |

**准备态布阵编辑**

| 规则 | 说明 |
|------|------|
| 数据 | 与升级与制造共用 **同一套** BattleFormation 持久化 |
| 编辑器 | 与 §3.11 **同一套** `FormationEditor` Prefab / 逻辑（士兵栏拖拽；含「开战」） |
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
| 逻辑 | **连续可走空间**（**IsoDiamond** XZ 菱形足迹，非格子网格）；与 DigMap **阶段分离**（不同阶段实例），表现资产可与 Dig **共用** `Ground_01`…`Ground_05` |
| 表现资产 | `DefendGameplayConfig.BattleMapId` → 同 Dig 的地面变体池（合法值 `Ground_01`…`Ground_05`）；解析见 [SPEC_04 §9.7 / §13](SPEC_04_Technical.md) |
| 障碍 | Demo 最小：地图 Prefab 可走面须可烘焙 NavMesh（同形旋转盒 / `WalkSurface`）；复杂障碍几何 **后置** |
| EngageZone | 地图 **Prefab** 上挂载比 BattleMap 稍小的 **IsoDiamond（XZ 菱形）选敌区**；位置与尺寸由策划在预制体上调节；规则层只读该区域（见下「士兵战斗」） |

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
| 掉落 | 击杀时按 `LootDrop`（**仍为**旧编码 `Id_Count|…`；**不是**坟墓品质表的 `DropMode` / `Id;Weight;Count`） |

**目标选择与寻路（怪物）**

| 规则 | 说明 |
|------|------|
| 选目标 | 按 `MonsterConfig.TargetSelect`（见上） |
| 目的地 | 前往该目标的 **AttackSlot**（落在 `AttackRange` 环上的可站立点；见下「大规模战斗寻路」） |
| 修正间隔 | 每 **TargetRetargetInterval**（暂定 **1s**，可配置）重选目标并重算 AttackSlot；**禁止**全员每帧全图重寻路 |
| 技术约定 | 规则层输出目标实体 ID + `GoalKind`；移动服务解析 `DesiredDestination` 并执行移动；规则层不直接驱动 `Transform`。见 [SPEC_04 §9.7](SPEC_04_Technical.md) |
| Demo 最小 NavMesh | 在 `Prefabs/Maps/{BattleMapId}` 可走面上烘焙（或运行时等价）**最小可走 NavMesh**；须覆盖地图内主角/士兵活动区，并允许从 Demo 固定出生点走到可走区。精确外围衔接与障碍细则 **后置**。大规模栈中 NavMesh/可走掩码主要用于 **FlowField 障碍** 与槽位合法性，而非 400 个独立重路径 |

**大规模战斗寻路（MassCombatPathing，方案 B）**

**状态：规则已锁定（方案 B）；实现见 `.scratch/mass-pathing/issues/`；须支持双方约 200 人同场；编码前按切片授权。**

适用于 Defend / PushMap 中 **士兵与怪物** 的战斗移动（PushMap 忠诚推进优先走 FlowField；追击/交战走 AttackSlot）。

| 规则 | 说明 |
|------|------|
| 目标规模 | 设计容量 **双方各约 200**（合计约 400）存活可移动单位同场；移动逻辑须分帧，禁止 O(n²) 全表互扫、禁止全员每帧 `CalculatePath` |
| 默认运动 | 确定 `DesiredDestination` 后 **默认直线趋近**（XZ）；仅当直线被挡才绕行 |
| 静态障碍 | 地图边界、`AirWall`、不可走区 → 写入 FlowField / 可走掩码（开战 Bake 一次；目标切换时按需重建场）；单位 **不可穿** |
| 动态障碍（友军） | 友军 / 同阵营单位 **不** NavMesh Carve、**不** 写入 FlowField 障碍格；以 **LocalDetour**（前方扇形 + 左/右短探测）选一侧绕行，可叠加软分离 |
| 共享目标 → FlowField | PushMap **全队共 `CurrentObjective`**（及同类「多人同一世界点」）：构建/采样 **一条** 流向该点的 FlowField；同目标单位只读场向量 + 本地绕行，**禁止**每人独立全图 A*。进入该目标 `CaptureZone` 后 **停跟场趋近中心**，改 LocalDetour 软分离守备（见 §3.14「到达」） |
| 追击/攻击 → AttackSlot | 目标为敌人（或可攻击实体）时：`DesiredDestination` = 认领的 **AttackSlot**（见下），非目标中心 |
| FormationHome | Defend 无 Engage 候选时：目的地=`FormationHome`；**MP-06 已接线** — `MassMoveScheduler.SetGoal(FormationHome)` 直趋 + LocalDetour（人多聚类短命场后置） |
| 与遇敌暂停关系 | PushMap：**MP-05 已接线** — 忠诚兵进入遇敌检测（距存活怪 ≤ `max(怪 AttackRange, 士兵 AttackRange, 怪BodyRadius+士兵BodyRadius) + ArriveEpsilon`）时改 `GoalKind=AttackSlot`（认领槽 + LocalDetour），**停跟** Objective FlowField；离开后释放槽并恢复 `GoalKind=Objective`。无空闲槽时**不**硬暂停：保持 `GoalKind=Objective` 继续跟场/绕行。完整 WarriorCombat 命中 polish 仍后置；Demo 近距击杀见 §3.14（须已认领该怪 AttackSlot，禁止 Objective 路过即杀） |
| 规则/表现分离 | 规则层：目标 ID + `GoalKind`（+ 可选 AttackRange）；**移动服务**（可纯 C# + 表现桥）：FlowField / AttackSlot / LocalDetour / 分帧预算；View 只应用位移与动画 |

**AttackSlot（攻击槽位）**

| 规则 | 说明 |
|------|------|
| 几何 | 候选点在目标周围半径 ≈ `max(ε, AttackRange − margin)` 的环上；须可走且不与目标 `BodyRadius`（怪表或士兵 `BodyAppearanceConfig.BodyRadius`）严重重叠 |
| 认领 | 每单位至多认领 1 槽；优先：相对当前朝向/来向夹角小、槽空闲、可走 |
| 失效重算 | 目标死亡/换目标；目标位移超过阈值；槽被占或变为不可走；周期 ≤ `TargetRetargetInterval` |
| 近战/远程 | 均用槽位；远程可用更大环半径或更疏角度步进（实现常量见 SPEC_04） |
| 多打一 | 同目标槽位表 + 空间哈希分配，避免全体挤同一世界点 |

**LocalDetour（本地左右绕行）**

| 规则 | 说明 |
|------|------|
| 触发 | 直线前方短距内存在阻挡友军（或软重叠超阈） |
| 决策 | 左、右各做短探测（射线或采样点）；选通行更好一侧作为绕行偏置，直至重新看见 `DesiredDestination` 或超时回退直线 |
| 禁止 | 因友军阻挡而触发全图重寻路；友军做 `NavMeshObstacle.Carve` |

**方案 B+：战斗移动模式与软碰撞（MassCombatSoftCollision；BMH 借鉴）**

**状态：规则草案已录入；叠在方案 B 之上，不替换 FlowField / AttackSlot / LocalDetour。实现切片见 `.scratch/mass-soft-collision/`；编码前按切片授权。**

借鉴自同类大规模群体战斗（Be My Horde）的可观测实现：自定义移动态 + 集中软碰撞排斥；**适配到本项目已锁定的方案 B**。

| 规则 | 说明 |
|------|------|
| 定位 | **增强层**：目的地仍由 `GoalKind` + FlowField / AttackSlot 解析；本层只规定「如何朝目的地走」与「单位间如何互推」 |
| **明确不做（跟随）** | **不**引入「跟随主角 / 军队半径聚团」作为默认移动态（对标 BMH `EMoveType::NORMAL` + `ArmyRadius` + `MinionFollowSpeed`）。士兵无目标时仍走既有 `FormationHome` / PushMap `Objective` FlowField，**不是**粘随主角 |
| 共享推进 | PushMap `Objective` FlowField **保留**（多人同一世界点，非跟随模式） |
| 容量 / 性能 | 同方案 B：双方约 200；邻域仅 `SpatialHash2D`；禁止 O(n²) 全表互扫与全员每帧 `CalculatePath` |

**CombatMoveMode（战斗移动模式；不含 Follow）**

规则层在已有 `GoalKind` 之外，可为单位附带可选 `CombatMoveMode`（缺省由 GoalKind 推导）。**枚举不含 Follow / NormalFollow。**

| 模式 | 与 GoalKind 关系 | 行为 |
|------|------------------|------|
| `Chase` | 默认对应 `AttackSlot` / `ChaseAnchor` | 直线（+ LocalDetour）趋近认领槽；可配置追击加速曲线（后置） |
| `Surround` | `AttackSlot` 的槽位分配策略 | 环上认领，但按 `SurroundGapDirection` + `SurroundGapDegrees` **留出缺口**（便于后排/远程/主角视线）；多打一不挤成实心环 |
| `Sweep` | 可选新 `GoalKind=Sweep` 或技能驱动（**P2 后置**） | 沿 `SweeperDir` / 波次切线推进，用于 Boss 波、冲锋类；Demo 可不实现 |
| （推导）Objective / FormationHome | 无独立 MoveMode | 仍采样 FlowField 或直趋 Home；到达圈内软分离守备（已有） |

**Surround（包围缺口）细则**

| 规则 | 说明 |
|------|------|
| 缺口方向 | `Left` / `Right` / `Top` / `Bottom` / `Random`（相对目标→进攻方来向或世界轴；实现常量见 SPEC_04） |
| 缺口角宽 | `SurroundGapDegrees`（Demo 建议默认 **60°**，可配置） |
| 槽位 | 在 AttackSlot 环上 **跳过**缺口扇区内的角度步进；其余角位照常认领 |
| 触发 | Demo：**近战多打一**默认开 Surround；远程可用更疏环且缺口更大或关闭（常量） |
| 与平衡 | 包围越完整雪球越快；缺口与后置威胁用于对冲（规则可调，不在本片改数值表） |

**SoftCollision（单位软碰撞）**

| 规则 | 说明 |
|------|------|
| 模型 | 单位 = XZ **圆**（半径 = `BodyRadius`：怪表或士兵 `BodyAppearanceConfig`）；**硬刚体互卡不做**为规模方案 |
| 集中登记 | `SoftCollisionService`（对标 BMH `PhysicsManager`）开战注册/死亡注销全部可移动单位足迹；Tick 分帧 |
| 解算 | 邻域软重叠 → **排斥位移/速度偏置**（升格 LocalDetour 可选软分离为默认）；`ResolveCollisions` 可关（Debug） |
| 与 RVO | 交战/守备时 **关闭** `NavMeshAgent` ObstacleAvoidance（对齐既有防挤抖）；规模分离以本服务为准 |
| 静态障碍 | 仍由 NavMesh / FlowField 掩码负责；软碰撞 **不**替代 AirWall |
| 强度 | `repulsionScale`；交战圈可降；足迹完全重合时按 RuntimeId 确定性侧推，避免零向量死锁 |

**士兵战斗（WarriorCombat）**

| 规则 | 说明 |
|------|------|
| Demo 边界 | **第一版 Demo**：士兵 **仅普通攻击**；不读/不施放 `SoldierSkills` 以及灵魂、宝石、额外装备的技能列表；**不**触发「释放技能后失控二次 roll」。`SkillCooldown` / `SkillConfig` / `Skills` / `SoldierSkills` 字段 **保留** 供后续扩展，本版 **不驱动施放** |
| 适用范围 | 非叛变士兵在 `Combat` 中的普攻 / 攻速流程；技能**效果**（含复活）仍 **TBD**（Demo 不施放） |
| EngageZone | 候选敌人 = 存活且 **位置在 EngageZone 内** 的怪物；区外（含仍在 `OutsideMap` 外围、尚未进入选敌区的怪）**不可选** |
| 选目标 | **默认**：EngageZone 内 **距离最近** 的存活敌人 |
| FormationHome | 开战部署时锁定的布阵世界坐标（该士兵 `BattleFormation` 上阵位）；战斗中不随 Prepare 再编辑变化 |
| 无目标 → 自动返回 | **非叛变**士兵：当前目标死亡（或其它原因）后若 EngageZone **无**下一可选目标 → **自动返回** `FormationHome`（`GoalKind=FormationHome`；大规模栈下直趋或轻量路径，见上 MassCombatPathing）；抵达后无目标则在该点待机；**不**追区外目标 |
| 返回途中选敌 | 自动返回过程中仍按 `TargetRetargetInterval` **继续搜索** EngageZone；一旦出现可选目标 → **立即中断返回**，改为追击 / 进入攻击流程（改 `AttackSlot`） |
| 叛变与返回 | **Rebel 不**自动返回布阵点（仍就近打主角/兵/怪） |
| AttackPriority | `SoulConfig.AttackPriority` **本批不参与**选目标；枚举与 `TargetSelect` 对齐，字段保留 |
| AttackMode | 取自 `SoulConfig.AttackMode`（`Melee` / `Ranged`）；决定普攻走方案 D 的近战或远程分支。配置示例（非 ClassName 硬编码）：战士类→`Melee`+`Strength`；射手类→`Ranged`+`Agility`；法师类→`Ranged`+`Intelligence`（主属性维取自 `ClassConfig.PrimaryStat`） |
| 法师与射手 | **同为远程通道**（进距 → 弹道 → 碰撞命中/超时未命中）；规则层 **唯一差异** 是 `NormalAttackPower` 所用 `PrimaryStat` 维（法师智力 / 射手数敏捷）；不另做法师技能或不同弹道规则；View 特效可区分，**不**改变结算 |
| AttackRange | 近战与远程均有攻击距离（士兵取 `ClassConfig.AttackRange`；怪物取 `MonsterConfig.AttackRange`）；须先移动至认领 **AttackSlot**（落在目标 `AttackRange` 内）后，再进入攻击态并播放攻击动作 |
| 重选 / 寻路 | 与怪物共用 `TargetRetargetInterval`：周期性在 EngageZone 内重选最近敌人并 **重算/换认领 AttackSlot**；无候选时 `GoalKind=FormationHome`；共享推进目标走 FlowField（PushMap） |
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

下列系数取自 `ClassConfig.CombatConvertCoeffs`（有键覆盖）；**缺键 / 空串**回退 **`CombatConstantConfig`**（[SPEC_04 §9.20b](SPEC_04_Technical.md)）。样例常量表：`NormalAttackPrimaryMult=15`、`AttackSpeedBase=0.5`、`AttackSpeedAgiDiv=60`、`SkillCdIntDiv=30`、`SkillCdFloor=0.1`。命中参数（`AttackRange` / 前摇 / 弹速 / 超时）取自职业表独立列（怪物取 `MonsterConfig` 同名列）。

```
NormalAttackPower = Primary × NormalAttackPrimaryMult

AttackSpeed = AttackSpeedBase + AttackSpeedAgiDiv / max(Agi, 1)
  // 单位：次/秒；攻击开始间隔 = 1 / AttackSpeed

SkillCooldown = max(SkillCdFloor, SkillConfig.BaseCooldownSeconds - SkillCdIntDiv / max(Int, 1))
  // 单位：秒；SkillConfig 见 SPEC_04 §9.21；第一版 Demo 不施放技能，本式不驱动战斗
```

| 派生项 | 静态展示 | 战斗运行时 |
|--------|----------|------------|
| Primary / Str / Agi / Int | `StaticStat` | `FinalStat` |
| MaxHP | §3.11：`ceil(BodyLife + StaticStat(Strength)×MaxHpStrengthMult)` | `ceil(BodyLife + FinalStat(Strength)×MaxHpStrengthMult)`；`BodyLife` 不变 |
| NormalAttackPower / AttackSpeed / SkillCooldown | 上式 + 静态属性 | 上式 + 战斗属性（Demo 不展示/不驱动 SkillCooldown 亦可） |

士兵攻击状态机要点：

```
Idle/Move → (target in EngageZone) → Move to AttackRange
  → wait until attack-start interval elapsed (1/AttackSpeed)
  → AttackWindup (within interval)
  → AttackMode=Melee: HitConfirm → monster HP -= NormalAttackPower (if still valid + in range) → Recovery
  → AttackMode=Ranged: spawn projectile → hit: HP -= NormalAttackPower; or timeout miss → Recovery
  → no EngageZone target (loyal) → ReturnToFormationHome (keep retargeting; abort on new target)
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
  → DefendPhase = ModeSelect
  → Player picks BattleMode + GameplayConfigId (Mode1 lists all DefendGameplayConfig)
  → Mode1 confirm → DefendPhase = Prepare (selected config)
  → Load BattleFormation {WarriorId, Position, RemainingHP}
  → Player may edit formation (positions / deploy / undeploy) → write back same BattleFormation
  → StartBattle requires deployed soldiers ≥ 1 (else button disabled / hint)
  → Player clicks StartBattle
  → DefendPhase = Combat
  → Spawn BattleProtagonist at BattleMap center
  → Shield = ProtagonistMaxHP (current level row)
  → RemainingCombatSeconds = CombatDurationSeconds
  → Deploy soldiers at **current** formation positions; MaxHP=ceil(BodyLife+FinalStat(Str)×MaxHpStrengthMult); RemainingHP clamp to MaxHP
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
       if no candidate → NavMesh to FormationHome (StartBattle deploy pos); keep retargeting; abort return on new target
       AttackMode from SoulConfig; Primary=FinalStat(ClassConfig.PrimaryStat via ClassId); NormalAttackPower=Primary×NormalAttackPrimaryMult
       AttackSpeed=0.5+60/max(Agi,1); interval=1/AttackSpeed; windup within interval
       // Demo: no skill cast; SkillCooldown formula retained but unused
       move into AttackRange → AttackWindup
       Melee: HitConfirm → monster HP -= NormalAttackPower; Ranged: projectile hit same / timeout miss
       HP≤0 + no gems → CombatDead; HP≤0 + gems → immediate PermanentDeath (§3.11)
       every TargetRetargetInterval: reselect nearest in EngageZone / repath (or FormationHome if none)
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

**Status: Framework defined (ModeSelect mode/level pick / Prepare / StartBattle / deploy / Shield / countdown spawn / pathing / win-lose / LossOfControl Rebel / WarriorCombat EngageZone·AttackMode·AttackRange·hit scheme D·death layers·NormalAttackPower·AttackSpeed; Primary from ClassConfig; CombatConvertCoeffs and AttackRange hit columns on ClassConfig / MonsterConfig; **Demo v1: soldiers and monsters use normal attacks only — no skill casts**; SkillCooldown formula/schema retained but unused); skill-effect table, monster→soldier damage edge cases still TBD; **spawn / NavMesh Demo-min closed below; exact OutsideMap geometry deferred****

Entered when Level stage `GameplayType = Defend`. Depends on §3.11 **BattleFormation** persistence. Config: [SPEC_04 §9.7](SPEC_04_Technical.md) `DefendGameplayConfig`, [§9.18](SPEC_04_Technical.md) `WaveSpawnConfig`, [§9.19](SPEC_04_Technical.md) `MonsterConfig`, [§9.20](SPEC_04_Technical.md) `LossOfControlConfig`.

**BattleMode / BattleModeSelect (UI-013 / D-044)**

| Rule | Notes |
|------|-------|
| Enter | After entering Defend, **must** start at `DefendPhase = ModeSelect`; **must not** jump straight to `Prepare` |
| Mode1 | `BattleMode = Defend`, player label「保卫战」; rules = this section Prepare→Combat; level list = **all** `DefendGameplayConfig` rows |
| Mode2 | `BattleMode = PushMap`, player label「推图战」; rules authority **§3.14**; level list = **all** `PushMapGameplayConfig` rows; confirm → §3.14 Prepare |
| Levels | List follows current mode; Defend lists all `DefendGameplayConfig`; PushMap lists all `PushMapGameplayConfig`; LevelOperation `GameplayConfigId` is Defend **Recommended** default highlight only; UM formation preview map may still read Recommended→`BattleMapId` (may differ from player's final pick; OK this version) |
| Confirm | Mode1 + selected level → overwrite stage start config with selected row → Defend `Prepare`; Mode2 + selected level → `LevelOperationDriver.TryHandoffModeSelectToPushMap` exits Defend ModeSelect, rewrites context to PushMap, enters `PushMapStageModule` §3.14 Prepare |
| Clear | Victory in **either** mode → stage settlement → §3.9 `TryAdvanceStage`; failure: both modes Shield≤0→LevelFailure (PushMap win/lose §3.14) |

**In-stage phases (DefendPhase)**

| Phase | Notes |
|-------|-------|
| `ModeSelect` | Default on enter: show mode + level select (UI-013); Mode1 confirm → Defend `Prepare`; Mode2 confirm → handoff leave this module |
| `Prepare` | Load formation, show prepare UI (incl. StartBattle); **may edit** formation with the **same** UI/logic as §3.11; write back same BattleFormation; cannot manufacture |
| `Combat` | After StartBattle: deploy from **current** formation, Shield + combat countdown, spawn, pathing, combat resolution |
| `Ended` | Stage ended by victory, or aborted by LevelFailure |

**Prepare formation editing**

| Rule | Notes |
|------|-------|
| Data | Shared **same** BattleFormation persistence as UpgradeManufacture |
| Editor | **Same** `FormationEditor` Prefab/logic as §3.11 (soldier-bar drag; includes StartBattle) |
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
| Logic | **Continuous walkable space** (**IsoDiamond** XZ footprint, not a cell grid); **stage-separate** from DigMap (different stage instances); presentation assets **may share** Dig’s `Ground_01`…`Ground_05` pool |
| Visual asset | `DefendGameplayConfig.BattleMapId` → same ground-variant pool as Dig (allowed `Ground_01`…`Ground_05`); resolve via [SPEC_04 §9.7 / §13](SPEC_04_Technical.md) |
| Obstacles | Demo-min: map Prefab walkable surface must bake NavMesh (same-shape rotated box / `WalkSurface`); complex obstacle geometry **deferred** |
| EngageZone | **IsoDiamond** (XZ diamond) **slightly smaller** than BattleMap, authored on the map **Prefab**; position/size tuned by designers; rules layer reads the zone (see WarriorCombat below) |

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
| Loot | On kill, `LootDrop` (**legacy** `Id_Count|…`; **not** GraveQuality `DropMode` / `Id;Weight;Count`) |

**Targeting & pathfinding (monsters)**

| Rule | Notes |
|------|-------|
| Select target | Per `MonsterConfig.TargetSelect` (above) |
| Destination | That target’s **AttackSlot** (standable point on the `AttackRange` ring; see Mass Combat Pathing below) |
| Retarget interval | Every **TargetRetargetInterval** (provisional **1s**, configurable) reselect target and recompute AttackSlot; **forbid** full-map repath every frame for all units |
| Tech | Rules layer outputs target entity id + `GoalKind`; move service resolves `DesiredDestination` and moves; rules must not drive `Transform`. See [SPEC_04 §9.7](SPEC_04_Technical.md) |
| Demo-min NavMesh | Bake (or runtime-equivalent) a **minimal walkable NavMesh** on `Prefabs/Maps/{BattleMapId}`; must cover in-map protagonist/soldier area and allow pathing from Demo fixed spawn points onto walkable surface. Exact off-map linkage and obstacle detail **deferred**. In the mass stack, NavMesh/walkable mask mainly feeds **FlowField blockers** and slot legality — not 400 independent full paths |

**Mass combat pathing (MassCombatPathing, Approach B)**

**Status: Rules locked (Approach B); implementation in `.scratch/mass-pathing/issues/`; must support ~200 per side; authorize per slice before coding.**

Applies to **soldier and monster** combat movement in Defend / PushMap (PushMap loyal advance prefers FlowField; chase/engage uses AttackSlot).

| Rule | Notes |
|------|-------|
| Scale | Design capacity **~200 per side** (~400 movable living units); movement must be frame-budgeted; forbid O(n²) all-pairs scans and per-frame `CalculatePath` for all |
| Default motion | After `DesiredDestination` is set, **default straight-line** (XZ); detour only when blocked |
| Static blockers | Map bounds, `AirWall`, non-walkable → written into FlowField / walkable mask (bake once at StartBattle; rebuild field when goal changes); units **cannot** cross |
| Dynamic blockers (friendlies) | Friendlies / same-faction units are **not** NavMesh-carved and **not** FlowField obstacle cells; use **LocalDetour** (forward cone + L/R probes) plus optional soft separation |
| Shared goal → FlowField | PushMap **shared `CurrentObjective`** (and similar many-to-one world points): build/sample **one** FlowField toward that point; same-goal units read field vectors + local detour — **no** per-unit full-map A*. Once inside that objective's `CaptureZone`, **stop seeking the goal cell center** and hold via LocalDetour soft separation (see §3.14 Arrive) |
| Chase/attack → AttackSlot | When target is an enemy (attackable entity): `DesiredDestination` = claimed **AttackSlot**, not entity center |
| FormationHome | Defend with no Engage candidate: goal=`FormationHome`; **MP-06 wired** — `MassMoveScheduler.SetGoal(FormationHome)` straight-line + LocalDetour (clustered short-lived fields deferred) |
| Vs engage pause | PushMap: **MP-05 wired** — loyal soldiers entering engage detect (dist to living monster ≤ `max(monster AttackRange, soldier AttackRange, monsterBodyRadius+soldierBodyRadius) + ArriveEpsilon`) switch to `GoalKind=AttackSlot` (claim slot + LocalDetour) and **leave** Objective FlowField; on clear, release slot and resume `GoalKind=Objective`. If no free slot: **do not** hard-pause — keep `GoalKind=Objective` and continue field/detour. Full WarriorCombat hit polish still deferred; Demo proximity kill → §3.14 (requires claimed AttackSlot on that monster; forbid Objective drive-by kill) |
| Rules / presentation | Rules: target id + `GoalKind` (+ optional AttackRange); **move service** (pure C# + view bridge): FlowField / AttackSlot / LocalDetour / frame budget; View only applies motion/anim |

**AttackSlot**

| Rule | Notes |
|------|-------|
| Geometry | Candidates on a ring radius ≈ `max(ε, AttackRange − margin)` around target; must be walkable and not heavily overlap target `BodyRadius` (monster table or soldier `BodyAppearanceConfig.BodyRadius`) |
| Claim | ≤1 slot per unit; prefer small angle vs facing/approach, free, walkable |
| Invalidate | Target dead/changed; target moved past threshold; slot occupied/unwalkable; period ≤ `TargetRetargetInterval` |
| Melee/Ranged | Both use slots; ranged may use larger ring / coarser angle step (SPEC_04 constants) |
| Many-vs-one | Per-target slot table + spatial hash — do not pile all on one world point |

**LocalDetour**

| Rule | Notes |
|------|-------|
| Trigger | Friendly blocker in short forward range (or soft overlap over threshold) |
| Decide | Short L/R probes; pick clearer side as detour bias until `DesiredDestination` is clear again or timeout → straight |
| Forbid | Full-map repath solely because of friendlies; `NavMeshObstacle.Carve` for friendlies |

**Approach B+: combat move modes & soft collision (MassCombatSoftCollision; BMH-inspired)**

**Status: Rules drafted; layered on Approach B — does not replace FlowField / AttackSlot / LocalDetour. Impl slices: `.scratch/mass-soft-collision/`; authorize per slice before coding.**

Inspired by observed large-horde combat (Be My Horde): custom move modes + centralized soft repulsion; **adapted to this project's locked Approach B**.

| Rule | Notes |
|------|-------|
| Role | **Enhancement layer**: destinations still from `GoalKind` + FlowField / AttackSlot; this layer defines steer-toward-goal + unit-unit push |
| **Explicitly out (Follow)** | **Do not** add “follow protagonist / army-radius blob” as default locomotion (BMH `EMoveType::NORMAL` + `ArmyRadius` + follow-speed curves). No-target soldiers still use `FormationHome` / PushMap `Objective` FlowField — **not** sticky follow |
| Shared advance | PushMap `Objective` FlowField **kept** (many-to-one world point, not follow mode) |
| Scale / perf | Same as B: ~200/side; neighbors via `SpatialHash2D` only; forbid O(n²) all-pairs and per-frame `CalculatePath` for all |

**CombatMoveMode (no Follow)**

Optional `CombatMoveMode` beside `GoalKind` (default derived from GoalKind). **Enum has no Follow / NormalFollow.**

| Mode | vs GoalKind | Behavior |
|------|-------------|----------|
| `Chase` | Default for `AttackSlot` / `ChaseAnchor` | Straight (+ LocalDetour) to claimed slot; optional chase accel curve (deferred) |
| `Surround` | AttackSlot allocation policy | Ring claims with `SurroundGapDirection` + `SurroundGapDegrees` **gap** (rear/ranged/protagonist LOS); multi-vs-one not a solid ring |
| `Sweep` | Optional `GoalKind=Sweep` or skill-driven (**P2 deferred**) | Advance along `SweeperDir` / wave tangent (Boss wave / charge); Demo may skip |
| (derived) Objective / FormationHome | No separate mode | FlowField sample or home straight-line; soft-separation hold inside arrive radius (existing) |

**Surround gap**

| Rule | Notes |
|------|-------|
| Direction | `Left` / `Right` / `Top` / `Bottom` / `Random` (vs approach or world axes; constants in SPEC_04) |
| Width | `SurroundGapDegrees` (Demo default **60°**, configurable) |
| Slots | Skip angle steps inside gap sector on AttackSlot ring; claim the rest normally |
| When | Demo: **melee multi-vs-one** Surround on by default; ranged may use sparser ring / larger gap / off (constants) |
| Balance | Fuller surround snowballs faster; gaps + later threats counterbalance (tunable; no table rewrite this slice) |

**SoftCollision**

| Rule | Notes |
|------|-------|
| Model | Unit = XZ **circle** (`BodyRadius`: monster table or soldier `BodyAppearanceConfig`); **hard rigidbody pile-up is not** the scale solution |
| Registry | `SoftCollisionService` (cf. BMH `PhysicsManager`) registers/unregisters movable footprints at battle start/death; frame-budgeted Tick |
| Resolve | Soft overlap → **repulsion offset/velocity bias** (promote LocalDetour optional separation to default); `ResolveCollisions` toggle (Debug) |
| vs RVO | Disable `NavMeshAgent` ObstacleAvoidance in engage/hold (existing anti-jitter); scale separation owned by this service |
| Static blockers | Still NavMesh / FlowField mask; soft collision **does not** replace AirWall |
| Strength | `repulsionScale`; lower in engage; coincident footprints → deterministic side push by RuntimeId |

**Warrior combat (WarriorCombat)**

| Rule | Notes |
|------|-------|
| Demo scope | **Demo v1**: soldiers use **normal attacks only**; do not read/cast `SoldierSkills` or Soul/Gem/ExtraEquipment skill lists; **no** skill-cast LossOfControl re-roll. `SkillCooldown` / `SkillConfig` / `Skills` / `SoldierSkills` fields **kept** for later; **unused cast** this Demo |
| Scope | Non-Rebel soldiers’ normal-attack / ASPD flow in `Combat`; skill **effects** (incl. revive) still **TBD** (not cast in Demo) |
| EngageZone | Candidate enemies = living monsters **inside EngageZone**; outside (incl. still-`OutsideMap` spawns not yet in zone) **not selectable** |
| Target select | **Default**: nearest living enemy inside EngageZone |
| FormationHome | World position locked at StartBattle deploy (that soldier’s `BattleFormation` slot); does not change from Prepare edits mid-combat |
| No target → auto-return | **Loyal** (non-Rebel) soldiers: after current target dies (or otherwise) if EngageZone has **no** next candidate → **auto-return** to `FormationHome` (`GoalKind=FormationHome`; straight or light path under MassCombatPathing); idle there if still no target; **do not** chase outside zone |
| Retarget while returning | During auto-return, still search EngageZone every `TargetRetargetInterval`; on a new candidate → **abort return** and chase / attack (switch to `AttackSlot`) |
| Rebel vs return | **Rebels do not** auto-return to formation (keep nearest protagonist / soldiers / enemies) |
| AttackPriority | `SoulConfig.AttackPriority` **unused** for targeting this batch; same enum as `TargetSelect`; field kept |
| AttackMode | From `SoulConfig.AttackMode` (`Melee` / `Ranged`); selects Melee vs Ranged branch of scheme D. Config examples (not ClassName hardcoding): Warrior-like→`Melee`+`Strength`; Archer-like→`Ranged`+`Agility`; Mage-like→`Ranged`+`Intelligence` (PrimaryStat dim from `ClassConfig.PrimaryStat`) |
| Mage vs Archer | **Same Ranged channel** (enter range → projectile → collision hit / timeout miss); rules-layer **only** difference is which `PrimaryStat` feeds `NormalAttackPower` (Mage Intelligence / Archer Agility); no separate mage skill or different projectile rules; View VFX may differ without changing settlement |
| AttackRange | Both Melee and Ranged have AttackRange (soldiers: `ClassConfig.AttackRange`; monsters: `MonsterConfig.AttackRange`); must move to claimed **AttackSlot** (inside target `AttackRange`) before attack state / attack anim |
| Retarget / path | Same `TargetRetargetInterval` as monsters: periodically reselect nearest in EngageZone and **reclaim/recompute AttackSlot**; if none, `GoalKind=FormationHome`; shared advance goals use FlowField (PushMap) |
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

Coeffs from `ClassConfig.CombatConvertCoeffs` (present keys override); **missing key / empty** fall back to **`CombatConstantConfig`** ([SPEC_04 §9.20b](SPEC_04_Technical.md)). Sample constants: `NormalAttackPrimaryMult=15`, `AttackSpeedBase=0.5`, `AttackSpeedAgiDiv=60`, `SkillCdIntDiv=30`, `SkillCdFloor=0.1`. Hit params (`AttackRange` / windup / projectile / timeout) from the same table's separate columns (monsters: `MonsterConfig` same-named columns).

```
NormalAttackPower = Primary × NormalAttackPrimaryMult

AttackSpeed = AttackSpeedBase + AttackSpeedAgiDiv / max(Agi, 1)
  // attacks per second; attack-start interval = 1 / AttackSpeed

SkillCooldown = max(SkillCdFloor, SkillConfig.BaseCooldownSeconds - SkillCdIntDiv / max(Int, 1))
  // seconds; SkillConfig in SPEC_04 §9.21; Demo v1 does not cast skills — unused in combat
```

| Derive | Static UI | Combat runtime |
|--------|-----------|----------------|
| Primary / Str / Agi / Int | `StaticStat` | `FinalStat` |
| MaxHP | §3.11: `ceil(BodyLife + StaticStat(Strength)×MaxHpStrengthMult)` | `ceil(BodyLife + FinalStat(Strength)×MaxHpStrengthMult)`; `BodyLife` unchanged |
| NormalAttackPower / AttackSpeed / SkillCooldown | formulas + static attrs | formulas + combat attrs (Demo may omit SkillCooldown display/drive) |

Soldier attack state machine (sketch):

```
Idle/Move → (target in EngageZone) → Move to AttackRange
  → wait until attack-start interval elapsed (1/AttackSpeed)
  → AttackWindup (within interval)
  → AttackMode=Melee: HitConfirm → monster HP -= NormalAttackPower (if still valid + in range) → Recovery
  → AttackMode=Ranged: spawn projectile → hit: HP -= NormalAttackPower; or timeout miss → Recovery
  → no EngageZone target (loyal) → ReturnToFormationHome (keep retargeting; abort on new target)
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
  → DefendPhase = ModeSelect
  → Player picks BattleMode + GameplayConfigId (Mode1 lists all DefendGameplayConfig)
  → Mode1 confirm → DefendPhase = Prepare (selected config)
  → Load BattleFormation {WarriorId, Position, RemainingHP}
  → Player may edit formation (positions / deploy / undeploy) → write back same BattleFormation
  → StartBattle requires deployed soldiers ≥ 1 (else button disabled / hint)
  → Player clicks StartBattle
  → DefendPhase = Combat
  → Spawn BattleProtagonist at BattleMap center
  → Shield = ProtagonistMaxHP (current level row)
  → RemainingCombatSeconds = CombatDurationSeconds
  → Deploy soldiers at **current** formation positions; MaxHP=ceil(BodyLife+FinalStat(Str)×MaxHpStrengthMult); RemainingHP clamp to MaxHP
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
       if no candidate → NavMesh to FormationHome (StartBattle deploy pos); keep retargeting; abort return on new target
       AttackMode from SoulConfig; Primary=FinalStat(ClassConfig.PrimaryStat via ClassId); NormalAttackPower=Primary×NormalAttackPrimaryMult
       AttackSpeed=0.5+60/max(Agi,1); interval=1/AttackSpeed; windup within interval
       // Demo: no skill cast; SkillCooldown formula retained but unused
       move into AttackRange → AttackWindup
       Melee: HitConfirm → monster HP -= NormalAttackPower; Ranged: projectile hit same / timeout miss
       HP≤0 + no gems → CombatDead; HP≤0 + gems → immediate PermanentDeath (§3.11)
       every TargetRetargetInterval: reselect nearest in EngageZone / repath (or FormationHome if none)
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

科技树为 **中心向外** 扩展的科技项图。配置表载体见 [SPEC_04 §9.16 `TechTreeConfig`](SPEC_04_Technical.md)、[§9.17 `TechEffectConfig`](SPEC_04_Technical.md)。经验入账来自 Defend 或 PushMap **阶段胜利**（§3.11 / §3.12 / §3.14）；击杀怪物 **不** 直接给经验。

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

TechTree is a **center-out** graph of TechItems. Config tables: [SPEC_04 §9.16 `TechTreeConfig`](SPEC_04_Technical.md), [§9.17 `TechEffectConfig`](SPEC_04_Technical.md). Experience from Defend or PushMap **stage victory** (§3.11 / §3.12 / §3.14); killing monsters does **not** grant Exp directly.

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

## 3.14 推图战（PushMap）

### 简体中文

**状态：框架已定义（规则库）；Prepare/开战/护盾/失控/士兵战斗复用 §3.12；目标点链/判定圈占领/空气墙/刷怪点/陷阱/BOSS 通关/AggroMode 已锁定；副本玩法正文 TBD（仅解锁钩子）；Demo 实现须另授权并拆切片（见 `.scratch/push-map/issues/`）**

当关卡当前阶段 `玩法类型 = PushMap` 时进入本阶段。亦可通过 Defend 阶段 `BattleModeSelect` 选模式2「推图战」经 `TryHandoffModeSelectToPushMap` 进入本规则（见 §3.8 D-044；规则以本节为准）。依赖 §3.11 **战斗布阵**。配置见 [SPEC_04 §9.22](SPEC_04_Technical.md) `PushMapGameplayConfig`、[§9.23](SPEC_04_Technical.md) `PushMapSpawnConfig`、[§9.19](SPEC_04_Technical.md) `MonsterConfig`（含 `AggroMode` / `AlertRadius` / `BodyRadius`）、[§9.20](SPEC_04_Technical.md) `LossOfControlConfig`。

**与 Defend 的关系（复用边界，方案 2C）**

| 复用 | 说明 |
|------|------|
| 子状态 | `Prepare` → `Combat` → `Ended`（`PushMapPhase`；语义对齐 DefendPhase，无独立 ModeSelect） |
| 布阵 / 开战 | 同一 `FormationEditor`；开战须 ≥1 上阵；控制力超额允许开战 |
| 护盾 / 失控 | 同 §3.12：`Shield` 初值=`ProtagonistMaxHP`；`Shield ≤ 0` → **LevelFailure**；开战锁定 Degree/Tier + Rebel |
| 士兵战斗 | 同 §3.12 WarriorCombat（EngageZone / AttackMode Melee\|Ranged / 命中方案 D）；**第一版仅普攻** |
| 不复用 | Defend 倒计时刷怪（`WaveSpawnConfig` / `SpawnRemainingSeconds`）；清场胜利条件（刷怪行全触发+全灭） |
| 表现机位 | 与 Defend **同为正交俯视**（`Euler(90,0,0)`）；Combat 须启用专用战斗相机，不得落到场景透视主相机（见 [SPEC_04 §6](SPEC_04_Technical.md)） |
| 镜头跟随 | Combat 专属（Prepare 仍用 FormationCamera）。`CameraFollowMode`：`Auto`（默认）= 跟随地图 **`CameraFollowPath`** 折线上的机位，**不**粘士兵 Transform。进度 `s∈[0,1]` = 场上**忠诚存活**（`!IsRebel` 且非 `CombatDead`）士兵把世界 XZ **投影到折线**后的**最大值**；领头兵死亡/叛变/失活 → `s` 变小 → `SmoothDamp` **回退**（不 Snap）。无可跟随忠诚兵 → **定格**最后机位（不跟主角、不回地图中心）。缺 `CameraFollowPath` 或未烘焙折线 → warn + **回退**旧行为（粘随距 **CurrentObjective** 最近忠诚兵）。Auto 表现：世界 XZ 圆形死区 **`FollowDeadzone=0.15`** 内忽略目标小幅位移（镜头不动）；超出后以 **`FollowSmoothTime=0.25`** 对 XZ 做 `SmoothDamp` 缓动追赶（Y/旋转不变）；`EnterAuto` / 开战启用 → **立刻 Snap** 到**当时**的折线点（或回退士兵）XZ（清零 damp 速度）；进度回退**不** Snap。`Manual` = 左键拖动画布，镜头 XZ 平移；底中「恢复跟随」（`ResumeFollow`）**仅手动态显示**，点击 → `Auto` 并隐藏。机位高度不变；开战默认 `orthographicSize=2`；Combat 滚轮缩放 Size，钳制 **`[0.5, 20]`**（前滚拉近变小、后滚拉远变大）；缩放不切换跟随模式；恢复跟随不重置 Size |

**阶段内子状态（PushMapPhase）**

| 子状态 | 说明 |
|--------|------|
| `Prepare` | 加载地图与布阵；可编辑布阵；含「开战」；不可制造 |
| `Combat` | 部署单位；目标点推进；刷怪点/陷阱；AggroMode；护盾与失控运行中 |
| `Ended` | BOSS 通关胜利，或 LevelFailure（护盾归零 / 无忠诚存活士兵） |

**地图（MapId）**

| 规则 | 说明 |
|------|------|
| 标识 | `PushMapGameplayConfig.MapId`；**≠** `LevelId`；多关卡可共用同一地图 |
| 合法池 | `Ground_01`…`Ground_05` **或** `PushMap_*` 前缀逻辑名 |
| 路径 | 解析 → `Assets/Prefabs/Maps/{MapId}.prefab` |
| 地面 | Unity **Isometric Tilemap** + Tile Palette 手刷（约定同 Dig/Defend，[SPEC_04 §13](SPEC_04_Technical.md)） |
| 与 Dig/Defend 分离 | 阶段实例分离；勿复用未销毁的 Dig/Defend 地图实例 |

**地图 Prefab 标记（须支持）**

| 标记 | 说明 |
|------|------|
| ObjectivePoint | 有序目标点；字段含 `ObjectiveOrder`（1,2,3…）与挂载 `CaptureZone` |
| CaptureZone | 判定圈；圆形；**默认半径 2**（世界单位）；Prefab 可改 |
| AirWall | 空气墙；阻挡 **敌我双方** 士兵与怪物；支持绕 Y 轴 **0°/45°/90°…** 旋转；Demo NavMesh 见下「Demo 空气墙边界」 |
| SpawnPoint | 刷怪点；独立 `SpawnPointId`；与配置表行匹配 |
| TrapZone | 陷阱区域；独立 `TrapZoneId`；我方 **忠诚** 士兵进入触发绑定刷怪 |
| BossPoint | BOSS 点；关联刷怪生成的 BOSS；击杀 → 阶段通关 |
| CameraFollowPath | 镜头跟随轨；子物体有序路点（起点/拐弯/终点，≥2）；烘焙折线存 Prefab；作者路点可 Snap 到 Tilemap Grid；相邻路点之间按**世界 XZ 直线**按间距采样（倾斜已在路点世界坐标内）；拐弯由作者路点表达；镜头轨**不是**士兵寻路，直线可穿 AirWall（须作者加转弯点）；Gizmo 可见；开战若未烘焙则补 Bake 一次 |
| EngageZone / WalkSurface | 复用 §3.12 约定（选敌区 / NavMesh 可走面） |
| Demo 空气墙边界 | PM-08（方案 A）：开战 Runtime Bake 收集地图 `AirWall`，以 `NavMeshBuildSource` **Box + Not Walkable** 注入可走面烘焙（`HalfExtents×2` 尺寸；`Transform` 含 Y 旋转 → **含 45°**）；敌我士兵与怪物 `NavMeshAgent` 均不可穿；**不做** NavMeshObstacle Carve、复杂多层障碍 polish。**作者硬约束：** `ObjectivePoint` / `SpawnPoint` / `BossPoint` 的世界 XZ **不得**落在任一 `AirWall` OBB 内（加厚/平移墙体时须复核）；目标落墙内 → FlowField 目标格不可走、到达圈内守备后无法再向下一目标推进 |

**目标点链与占领**

| 规则 | 说明 |
|------|------|
| 顺序 | 按 `ObjectiveOrder` 升序；开战后当前目标 = 最小未占领 Order |
| 士兵推进 | **全队共当前目标**：所有忠诚士兵以 `CurrentObjective` 为共享目的地；移动走 **FlowField**（§3.12 MassCombatPathing 方案 B）；可途中被 EngageZone 内敌人打断选敌（改 AttackSlot）；无候选则继续采样流向当前目标的场 |
| 推进与占领 | 占领仅看「是否到达」；圈内有存活怪物 **不**阻止占领，也 **不**单独暂停推进（遇敌改 AttackSlot 见下） |
| Demo 遇敌暂停推进 | 完整 §3.12 WarriorCombat 接入前：忠诚兵中心距任一存活怪 ≤ `max(AttackRange, 怪BodyRadius+士兵BodyRadius)` 时 **暂停推进**（停跟场、清本地速度；勿依赖全开 RVO）；离开后恢复采样 FlowField。正式 Engage 选敌后改 `GoalKind=AttackSlot`；命中仍后置完整 WarriorCombat |
| FlowField 重建 | `CurrentObjective` 切换、开战 Bake（含 AirWall 不可走）、可走面变更 → 重建指向新目标的场；同目标单位共享一场 |
| 到达＝占领 | **任一**忠诚士兵进入当前目标 `CaptureZone` → **立即占领（Capture）**；无需全部到达；**无**计时、**无**「圈内无怪」附加条件。**移动：** 进入圈后 **不再** 采样 FlowField 趋近目标格中心（避免全队挤死零向量格）；圈内以 LocalDetour **软分离** 维持站位间距；圈外继续跟场；场向量≈0 且仍在圈外时直趋目标回退 |
| 占领效果 | ① 该目标点本场标记已占领；② **关联该目标的刷怪点本场不再刷怪**；③ 已刷出且仍存活的怪物 **保留** 直至击杀；④ 可发放配置奖励并写入副本解锁钩子（见下）；⑤ 当前目标切换为下一未占领目标；**若无下一目标且地图有 `BossPoint` → 全队推进目标改指 BossPoint（v0.74.10 BOSS 引导，见下「Demo BOSS 引导边界」）**；无 `BossPoint` 时士兵保持当前位置/就近守备直至 BOSS 通关或失败 |
| 占领奖励 | 来自 `PushMapGameplayConfig` 或目标点绑定配置的物资/精魂等；**不**入账阶段经验 |
| 副本解锁 | 配置的 `DungeonUnlockIds` 写入存档集合钩子；**副本玩法正文 TBD** |
| 规则归属 | `CurrentObjective` 链与占领判定/事件归属 **规则层**（`PushMapSessionService`）；地图 `ObjectivePoint`/`CaptureZone` 仅作者标记，运行时不自管 Tick；表现层每帧上报「是否有忠诚兵在当前圈内」 |
| Demo 到达边界 | 表现层扫描忠诚 `PushMapAdvanceView`：`!IsRebel && CaptureZone.ContainsXZ` → `TickCapture(true)`；Rebel 不触发占领；`CaptureSeconds` 配置列可加载但 **规则忽略** |
| Demo 刷怪 AI 边界 | PM-05 怪物 AI 用 Defend 默认追击语义（就近忠诚兵/主角；进 `AttackRange` 普攻；对主角扣盾）；AggroMode 四态后置 **PM-06**（见下「Demo AggroMode 边界」）；BOSS 通关结算见「Demo BOSS 通关边界」 |
| Demo AggroMode 边界 | PM-06 四态实现：主动态（`ActiveChase`/`StationaryActive`）发现半径用 `AlertRadius`（与 `AttackRange` 并列）且**仅**对忠诚士兵触发主动发现；被动态（`PassiveChase`/`StationaryPassive`）须先被攻击→挑衅优先＝对该怪的士兵 **真实 HitConfirm**（PM-12）；可保留「忠诚兵首次进入该怪 `AttackRange`」兜底，以免远程未命中永远不激怒；原地态（`Stationary*`）不移动，仅 `AttackRange` 内攻击。技能施放 / 副本玩法正文 **不做** |
| Demo BOSS 通关边界 | PM-07：击杀 `IsBoss` 行生成且落于 `BossPoint` 的 BOSS → `Ended` → 入账 `StageExpReward`（`AddExperience`）；**不**立即 `TryAdvanceStage`。`Shield≤0` **或** 已登记士兵≥1 且场上无忠诚存活（`!IsRebel && !CombatDead`）→ LevelFailure **不**入账本阶段经验。叛变写入规则层 `IsRebel` 供判定。**击杀契约（PM-12 起）：** 怪物 `RemainingHp≤0` → 表现层 `NotifyKilled`；BOSS 另 `TryNotifyBossKilled`。占领时发放 `CaptureLoot`（**不含**经验）并累加本场展示 ledger；写 `DungeonUnlockIds`；通关同样写解锁钩子。`IsBoss` 与 `BossPoint`：缺标记 warn |
| Demo 战斗结算 / 奖励 UI | **UI-017 / UI-018（方案 A）：** 胜负均先弹战斗结算（上部胜利/失败；中部战斗耗时 `mm:ss`、击杀怪物总数；底中「继续」）。失败 Continue → `AbortLevel` + 打开 `LevelSelectPanel`。胜利 Continue → 奖励弹窗（仅展示已入账 Exp + CaptureLoot 汇总，无额外发放）→ Continue → 结束关卡（无 VictorySettlement 占位 toast）+ `LevelSelectPanel`。Defend 同款 UI **不做** |
| Demo 士兵攻击 / WarriorCombat 边界 | **PM-12（方案 B）：** PushMap 忠诚士兵战斗对齐 §3.12 方案 D——`AttackMode=Melee`：`AttackWindup` 结束 → `HitConfirm`（目标仍存活且在 `AttackRange`）→ 怪 `HP -= NormalAttackPower`；`AttackMode=Ranged`：生成弹道（复用 Defend `ProjectileView`）→ 软碰撞命中再结算 / 超时未命中不结算。`NormalAttackPower` / `AttackSpeed` / 前摇 / 弹速取自开战登记（`WarriorCombatMath` + `ClassConfig`，镜像 Defend）。保留 FlowField / AttackSlot / 粘滞选敌；**移除**固定 `PushMapAttackAnimSeconds` 仅播动作、无结算的旧边界。怪物本片仍可不驱动 Animator（§15.5） |
| Demo 伤害飘字边界 | **DamagePopup（PM-12/13）：** 规则层命中成功后，在**被击目标**头顶显示 `-受伤值`（数值与本次结算伤害一致）。敌方怪物：红色；我方士兵：白色；敌我字号均为 **12**。出现后 **0.5s** 内世界坐标 `position.z` 相对起点从 **+0** 线性增至 **+0.5**，随后销毁（不做 Y 轴持续上浮）。主角护盾受击**不要求**飘字。防守战本需求 **不做** |
| Demo 受伤闪烁边界 | **HitFlash（PM-12/13）：** 与飘字同一命中成功事件。目标子树 Renderer 临时亮色（`MaterialPropertyBlock`，勿永久改共享材质）。怪物亮**红**；士兵亮**白**。共 **2** 次脉冲（立即 1 + 再 1），每次持续 **0.1s**，**中间不灭**（紧接）→ 视觉连续亮约 **0.2s** 后恢复本色。闪烁未结束再次受伤 → 从头刷新。未命中/挥空不闪。防守战本需求 **不做** |
| Demo 友军脚下圈边界 | **AllyFootCircle：** Defend/PushMap Combat **忠诚存活**士兵脚下绿描边 + 内黑 α=**160/255**；半径=`BodyRadius`；localPos `(0,-0.05,-0.2)`；rotation X=**-30**；Order In Layer=`1`；跟随移动；叛变/死亡隐藏；见 [SPEC_04 §9.7](SPEC_04_Technical.md) |
| Demo 选敌粘滞边界 | v0.74.10：遇敌选敌带**粘滞迟滞**——已认领怪仍存活且仍在其遇敌检测半径内时，仅当新候选中心距比已认领目标近超过 **`EngageStickHysteresisMargin`（默认 0.15 世界单位，常量）** 才切换认领，否则保持原认领。原因：密集怪群叠加 SoftCollision 微推使「严格最近」目标逐帧翻飞，破坏 AttackSlot / 前摇稳定性。粘滞不改变检测半径与槽位合法性；目标死亡/出圈仍正常切换（**不再**绑定已废止的 `DemoKillEngageSeconds`） |
| Demo BOSS 引导边界 | v0.74.10：目标链耗尽（`CurrentObjectiveOrder=0`，全部占领）且地图有 `BossPoint` → `FlowField` 重建指向 BossPoint 世界 XZ（`CurrentObjectiveChanged(0)` 触发一次；无目标点地图开战后立即重建）；此时 `ObjectiveArriveRadius` 收紧为 **`BossAdvanceArriveRadius`（默认 0.35，常量）**，保证士兵进入遇敌检测半径（现配置 ≈0.38+）转 `AttackSlot` 并靠 HitConfirm 打空 BOSS HP；`Stationary*` BOSS 不移动时本引导是唯一接近手段；无 `BossPoint` → 维持「保持当前位置/就近守备」原语义；镜头跟随仍走 `CameraFollowPath` 最大投影（不跟士兵本人） |
| Demo 士兵受击边界 | **PM-13：** 怪物对忠诚士兵按 `MonsterConfig.AttackPower` 扣士兵 `RemainingHp`（无护甲）；命中成功 → 士兵头顶白飘字 + 白 HitFlash。`HP≤0` → `CombatDead`（停手；宝石/PermanentDeath 对齐 §3.12 Demo 最小即可）。对主角仍 `Shield -= 1`（忽略 AttackPower；不要求主角飘字/闪烁） |
|| Demo 怪物朝向稳定边界 | v0.75.10：推图怪追击朝向带**迟滞 + 最短保持**（`FacingHysteresisDegrees`=12°、`FacingSwitchMinDwellSeconds`=0.12s，常量；详见 SPEC_04 §15.5）；被堵**停滞**（0.25s 滑窗 XZ 位移 < 0.05 且 steer 非零）时停播 Run、面向当前追击目标，位移恢复或 steer 归零即退出。仅 `PushMapMonsterAgentView` 表现层；不改攻击判定 / 槽位认领 / 寻路规则；士兵与防守战怪 **不做** |

**刷怪（非 WaveSpawn 倒计时）**

| 规则 | 说明 |
|------|------|
| 驱动表 | `PushMapSpawnConfig`：按 `GameplayConfigId` + `SpawnPointId`；一点可多行（多种怪物） |
| 无陷阱刷怪 | 开战瞬间：若该刷怪点 **未** 绑定陷阱，且其 **关联目标点尚未占领**（或未关联目标=全局，开战即符合）→ 按行生成 |
| 陷阱刷怪 | 绑定 `TrapZoneId` 的刷怪点：我方忠诚士兵 **首次**进入该陷阱区且关联目标未占领 → 生成；本场每点默认触发一次（重复进入不重复刷，除非配置另开 **TBD**） |
| 占领停刷 | 关联目标已占领 → 该点不再新刷；场上已有怪保留 |
| 占地散开 | 同点 `SpawnCount>1` 或邻近已刷存活怪：按各自 `MonsterConfig.BodyRadius` 在可走面上错开落点，使 XZ 占地圆不重叠（NavMesh 采样失败时可略收缩环半径，最终可回退采样点）；陷阱后刷同样避让场上已有怪。**Demo 收紧（v0.73.9）：** `SamplePosition` 仅局部吸附（≈`max(0.75, BodyRadius×2.5)`）；命中点相对刷怪点 `basePos` 的 XZ 距离不得超过牵引上限（≈当前环/螺旋半径 + 半径余量，绝对上限约 `max(3, BodyRadius×10)`）；越界命中视为失败。挤满时 **优先重叠回退基点**，禁止为避让而跨空气墙吸附到菱形外侧空白 NavMesh |
| 持续避让 | Combat 中 **移动**怪：刷出散开仍按 `BodyRadius`；`NavMeshAgent.radius = min(BodyRadius, max(0.05, AttackRange − BodyAppearanceConfig.DefaultBodyRadius(0.1) − 0.05))`，保证中心距可进入 `AttackRange`（避免 RVO 半径大于攻击距导致永远无法交战）；`Stationary*` 不主动位移避让；PushMap 怪物 Demo：`NavMeshAgent.height=0.1`（`PushMapMonsterAgentView`）；**我方士兵**：`NavMeshAgent.radius` 取自 `BodyAppearanceConfig.BodyRadius`（缺省 `0.1`）、`height=0.1` |
| 禁止 | PushMap **不**使用 `RemainingCombatSeconds` / `WaveSpawnConfig` 倒计时激活 |
| Demo 实现边界 | PM-05（刷怪/陷阱）：怪物 AI 暂用 Defend 默认追击（非四态，见 §9.23 契约）；AggroMode 四态与 BOSS 通关结算 **后置**（PM-06/07）；刷怪资格 / 触发状态归 `PushMapSessionService`，位置由表现层按 `SpawnPointId` / `BossPoint` 解析；**开战顺序**：Bake NavMesh → 部署忠诚兵 → `FireStartBattleSpawns`（避免先刷怪后 Bake 导致 Agent 未上网格）；占地散开与 Agent 半径见 **PM-10** |
| Demo 可走面边界 | `DigMapBounds`（及 `WalkSurface` / `EngageZone`）须覆盖样例图上目标点 / 刷怪点 / BossPoint；NavMesh Runtime Bake 以 `DigMapBounds` 为准；标记落在界外会导致推进/刷怪无法上网格。样例 `PushMap_Demo_01` 空气墙须保持目标点可走（见上「Demo 空气墙边界」作者硬约束） |

**BOSS 与胜负**

| 规则 | 说明 |
|------|------|
| 通关 | 击杀 **BossPoint** 生成的 BOSS 怪物（`PushMapSpawnConfig.IsBoss=1`）→ `PushMapPhase=Ended` → 阶段胜利 → 入账 `StageExpReward` → UI-017 → UI-018 → 结束关卡并打开 LevelSelectPanel（Demo；不自动下一阶段） |
| 失败 | `Shield ≤ 0` **或** 无忠诚存活士兵 → LevelFailure；**不**入账本阶段经验 → UI-017 → Continue → AbortLevel + LevelSelectPanel |
| 清场 | **不**以「全部刷怪点刷完+全灭」为通关条件 |
| 规则归属 | 胜负结算归 `PushMapSessionService`（`TryNotifyBossKilled` / `VictorySettled` / `RequestLevelFailure` / 忠诚全灭检测）；经验入账由表现层调用 `ProtagonistProgressService.AddExperience`；驱动交还延迟至结算 UI Continue |

**怪物 AggroMode（仇恨模式；≠ AttackMode）**

| AggroMode | 中文 | 行为 |
|-----------|------|------|
| `ActiveChase` | 主动移动攻击 | 我方忠诚士兵进入 `AlertRadius` → 主动移动追击攻击直至该怪死亡 |
| `PassiveChase` | 被动移动攻击 | 仅被我方先攻击后进入攻击态并移动追击，直至死亡 |
| `StationaryActive` | 原地主动攻击 | 不主动移动；忠诚士兵进入 `AttackRange` 则攻击，离开则停止 |
| `StationaryPassive` | 原地被动攻击 | 不主动移动；须先被攻击且目标仍在 `AttackRange` 内才攻击，离开则停止 |

| 字段 | 说明 |
|------|------|
| AttackMode | 仍为 `Melee` / `Ranged`（命中方案 D） |
| AlertRadius | 主动发现半径；缺省可 = `AttackRange`（实现时锁定） |
| BodyRadius | XZ 占地半径；缺省 `0.35`；PushMap 刷出散开与移动怪 `NavMeshAgent.radius` 共用（[SPEC_04 §9.19](SPEC_04_Technical.md)） |
| TargetSelect | 仍可参考 §3.12；PushMap 默认优先当前威胁士兵 |

**经验与科技点边界**

| 规则 | 说明 |
|------|------|
| 阶段经验 | **仅** BOSS 通关胜利入账 `LifetimeExperience`；占领奖励 **不含** 经验 |
| 击杀普通怪 | **不**直接给经验（同 Defend） |

```
Enter PushMap (GameplayType=PushMap OR BattleModeSelect Mode2 → §3.14)
  → PushMapPhase = Prepare
  → Instantiate Prefabs/Maps/{MapId}; load markers
  → Edit BattleFormation (shared)
  → StartBattle (≥1 deployed)
       → Shield = ProtagonistMaxHP; lock LossOfControl; deploy
       → CurrentObjective = min uncaptured ObjectiveOrder
       → Loyal soldiers path toward CurrentObjective (EngageZone combat may interrupt)
       → Fire non-trap PushMapSpawn rows for uncaptured-linked SpawnPoints
  → Combat loop:
       → Trap enter → fire bound SpawnPoint once if objective uncaptured
       → CaptureZone: any loyal soldier inside → immediate Capture
            → stop future spawns for linked points; keep living; grant capture loot/unlock hook
            → advance CurrentObjective
       → AggroMode AI + WarriorCombat (§3.12 hit scheme D)
       → Shield ≤ 0 OR no living loyal → LevelFailure → UI-017 → LevelSelect
       → Boss from BossPoint killed → Ended → credit Exp → UI-017 → UI-018 → LevelSelect
```

**待实现优先级（规则库；非 Demo §3.8）**

| 优先级 | 内容 |
|--------|------|
| P0 | 地图标记契约、配置表、Stage 接线、目标点占领、刷怪/陷阱、BOSS 通关、护盾失败（已落地 PM-01～05/07）；战斗结算/奖励 UI（UI-017/018） |
| P1 | AggroMode 四态、空气墙 NavMesh（已落地 PM-06 / PM-08）；Combat 镜头双模式跟随（已落地 PM-09）；怪物占地散开 / BodyRadius（已落地 PM-10） |
| P2 | 副本解锁 UI；副本玩法另专题 |

### English

**Status: Framework defined (rules library); Prepare/StartBattle/Shield/LossOfControl/WarriorCombat reuse §3.12; objective chain / CaptureZone / AirWall / SpawnPoint / TrapZone / Boss clear / AggroMode locked; Dungeon gameplay body TBD (unlock hook only); Demo implementation requires separate authorization and issue splits (see `.scratch/push-map/issues/`)**

Entered when Level stage `GameplayType = PushMap`. May also be entered via Defend `BattleModeSelect` Mode2「推图战」through `TryHandoffModeSelectToPushMap` (see §3.8 D-044; rules authority is this section). Depends on §3.11 **BattleFormation**. Config: [SPEC_04 §9.22](SPEC_04_Technical.md) `PushMapGameplayConfig`, [§9.23](SPEC_04_Technical.md) `PushMapSpawnConfig`, [§9.19](SPEC_04_Technical.md) `MonsterConfig` (`AggroMode` / `AlertRadius` / `BodyRadius`), [§9.20](SPEC_04_Technical.md) `LossOfControlConfig`.

**Relation to Defend (reuse boundary, Approach 2C)**

| Reuse | Notes |
|-------|-------|
| Phases | `Prepare` → `Combat` → `Ended` (`PushMapPhase`; aligned with DefendPhase; no separate ModeSelect) |
| Formation / StartBattle | Same `FormationEditor`; StartBattle requires ≥1 deployed; ControlPower overflow allowed |
| Shield / LOC | Same as §3.12: Shield init = `ProtagonistMaxHP`; `Shield ≤ 0` → **LevelFailure**; StartBattle locks Degree/Tier + Rebel |
| WarriorCombat | Same §3.12 (EngageZone / AttackMode Melee\|Ranged / hit scheme D); **v1 normal attacks only** |
| Not reused | Defend countdown spawns (`WaveSpawnConfig` / `SpawnRemainingSeconds`); clear-all victory (all rows fired + all killed) |
| Presentation camera | Same orthographic top-down as Defend (`Euler(90,0,0)`); Combat must enable a dedicated battle camera — must not fall back to scene perspective Main Camera (see [SPEC_04 §6](SPEC_04_Technical.md)) |
| Camera follow | Combat only (Prepare keeps FormationCamera). `CameraFollowMode`: `Auto` (default) = follow a look-at on map **`CameraFollowPath`**, **not** a soldier Transform. Progress `s∈[0,1]` = **max** polyline projection of living **loyal** soldiers (`!IsRebel` and not `CombatDead`); lead death/rebel/inactive → `s` shrinks → `SmoothDamp` **retreats** (no Snap). No followable loyal → **freeze** last pose (not protagonist, not map center). Missing `CameraFollowPath` or empty bake → warn + **fallback** to sticky-follow closest loyal to **CurrentObjective**. Auto presentation: ignore small target motion inside world-XZ circular deadzone **`FollowDeadzone=0.15`** (camera holds); outside → XZ `SmoothDamp` with **`FollowSmoothTime=0.25`** (Y/rotation unchanged); `EnterAuto` / combat enable → **immediate Snap** to the **current** rail point (or fallback soldier) XZ (clear damp velocity); progress retreat does **not** Snap. `Manual` = LMB drag pans camera XZ; bottom-center `ResumeFollow` **Manual-only**, click → `Auto` and hide. Height unchanged; StartBattle default `orthographicSize=2`; Combat scroll-wheel zooms Size clamped **`[0.5, 20]`** (forward zoom-in smaller / back zoom-out larger); zoom does not switch follow mode; ResumeFollow does not reset Size |

**PushMapPhase**

| Phase | Notes |
|-------|-------|
| `Prepare` | Load map + formation; editable formation; StartBattle; no manufacture |
| `Combat` | Deploy; objective push; spawn/trap; AggroMode; Shield + LOC running |
| `Ended` | Boss-clear victory, or LevelFailure (Shield≤0 / no living loyal soldiers) |

**Map (MapId)**

| Rule | Notes |
|------|-------|
| Id | `PushMapGameplayConfig.MapId`; **≠** `LevelId`; multiple levels may share one map |
| Allowed | `Ground_01`…`Ground_05` **or** `PushMap_*` logical names |
| Path | Resolve → `Assets/Prefabs/Maps/{MapId}.prefab` |
| Ground | Unity **Isometric Tilemap** + Tile Palette ([SPEC_04 §13](SPEC_04_Technical.md)) |
| Isolation | Stage instances separate from Dig/Defend |

**Map Prefab markers (required)**

| Marker | Notes |
|--------|-------|
| ObjectivePoint | Ordered objectives; `ObjectiveOrder` (1,2,3…) + `CaptureZone` |
| CaptureZone | Capture circle; default radius **2**; Prefab-tunable |
| AirWall | Blocks **both** factions; Y-rotation **0°/45°/90°…**; Demo NavMesh → "Demo AirWall edge" |
| SpawnPoint | Unique `SpawnPointId`; matched by config rows |
| TrapZone | Unique `TrapZoneId`; loyal soldier enter triggers bound spawns |
| BossPoint | Boss spawn marker; kill → stage clear |
| CameraFollowPath | Camera rail; ordered child waypoints (start/turns/end, ≥2); baked polyline stored on Prefab; author waypoints may Snap to Tilemap Grid; adjacent waypoints filled by **world-XZ straight** samples (tilt is in waypoint world poses); turns are author waypoints; rail is **not** soldier pathing and may cross AirWall (author must add turn waypoints); gizmos visible; StartBattle rebakes if empty |
| EngageZone / WalkSurface | Reuse §3.12 |
| Demo AirWall edge | PM-08 (Approach A): StartBattle runtime bake collects map `AirWall`s and injects `NavMeshBuildSource` **Box + Not Walkable** into the walkable bake (`HalfExtents×2`; `Transform` Y rotation → **incl. 45°**); both soldiers and monsters (`NavMeshAgent`) cannot path through; **no** NavMeshObstacle Carve or multi-layer obstacle polish. **Authoring hard rule:** world XZ of `ObjectivePoint` / `SpawnPoint` / `BossPoint` must **not** fall inside any `AirWall` OBB (re-check after thickening/moving walls); goal-inside-wall → FlowField goal cell non-walkable and advance can stall after CaptureZone hold |

**Objective chain & Capture**

| Rule | Notes |
|------|-------|
| Order | Ascending `ObjectiveOrder`; current = min uncaptured |
| Advance | **Shared current objective** for all loyal soldiers; movement via **FlowField** (§3.12 MassCombatPathing Approach B); may interrupt for EngageZone enemies (`AttackSlot`); if none, keep sampling field toward current objective |
| Advance vs Capture | Capture depends only on **arrive**; living monsters in zone do **not** block Capture and do **not** alone pause advance (engage → AttackSlot below) |
| Demo engage pause | Until full §3.12 WarriorCombat is wired: loyal soldiers **pause advance** when center distance to any living monster ≤ `max(AttackRange, monsterBodyRadius+soldierBodyRadius)` (stop following field / clear local velocity; do not rely on full RVO); resume FlowField sampling when clear. Formal Engage → `GoalKind=AttackSlot`; hits still deferred to full WarriorCombat |
| FlowField rebuild | On `CurrentObjective` change, StartBattle bake (incl. AirWall non-walkable), or walkable change → rebuild field toward new goal; same-goal units share one field |
| Arrive = Capture | **Any** loyal soldier entering current `CaptureZone` → **immediate Capture**; not all need arrive; **no** timer; **no** “no monsters in zone” extra condition. **Move:** once inside the zone, **stop** sampling FlowField toward the goal cell center (avoids pile-up on the zero-vector cell); inside zone use LocalDetour **soft separation** for spacing; outside keep following the field; if SampleDir≈0 while still outside, fall back to steer toward the goal |
| On Capture | Mark captured; **stop future spawns** for linked points; **keep** living spawned monsters; grant configured loot + dungeon unlock hook; advance current objective; **if none remains and the map has a `BossPoint` → shared advance goal redirects to the BossPoint (v0.74.10 Boss guidance, see "Demo Boss guidance edge" below)**; without a `BossPoint` soldiers hold position / guard nearby until Boss clear or failure |
| Capture loot | Materials/Spirit from config; **no** stage Exp |
| Dungeon unlock | Write `DungeonUnlockIds` to save-slot set hook; **dungeon gameplay TBD** |
| Rules ownership | Objective chain + capture check/events live in **rules layer** (`PushMapSessionService`); map `ObjectivePoint`/`CaptureZone` are authoring markers only, no runtime self-ticking; presentation reports each frame whether any loyal is in the current zone |
| Demo arrive edge | Presentation scans loyal `PushMapAdvanceView`: `!IsRebel && CaptureZone.ContainsXZ` → `TickCapture(true)`; Rebels do not trigger Capture; `CaptureSeconds` config column may load but is **ignored by rules** |
| Demo spawn AI edge | PM-05 monster AI uses Defend default-chase semantics (nearest loyal soldier / protagonist; attack in `AttackRange`; Shield hit on protagonist); AggroMode four-state deferred to **PM-06** (see "Demo AggroMode edge" below); Boss-clear settlement see "Demo Boss-clear edge" |
| Demo AggroMode edge | PM-06 four-state: active stances (`ActiveChase`/`StationaryActive`) detect via `AlertRadius` (alongside `AttackRange`) and **only** on loyal soldiers; passive stances (`PassiveChase`/`StationaryPassive`) must be attacked first → provoke prefers a real soldier **HitConfirm** on that monster (PM-12); may keep first loyal entry into `AttackRange` as fallback so a ranged miss cannot forever leave the passive idle; stationary stances (`Stationary*`) never move, attack only inside `AttackRange`. Skill casts / dungeon gameplay body **not** done |
| Demo Boss-clear edge | PM-07: kill Boss from `IsBoss` row at `BossPoint` → `Ended` → credit `StageExpReward` (`AddExperience`); **do not** immediately `TryAdvanceStage`. `Shield≤0` **or** registered warriors≥1 with no living loyal (`!IsRebel && !CombatDead`) → LevelFailure with **no** stage Exp. Sync Rebel into rules `IsRebel`. **Kill contract (from PM-12):** monster `RemainingHp≤0` → View `NotifyKilled`; Boss also `TryNotifyBossKilled`. Capture grants `CaptureLoot` (**no** Exp), accumulates display ledger, writes `DungeonUnlockIds`; Boss-clear also writes unlocks. Missing `BossPoint` with `IsBoss` → warn |
| Demo battle settlement / reward UI | **UI-017 / UI-018 (Approach A):** always show settlement on win/lose (top Victory/Defeat; mid combat time `mm:ss` + monsters killed; bottom Continue). Fail Continue → `AbortLevel` + `LevelSelectPanel`. Win Continue → reward popup (already-credited Exp + CaptureLoot only; no extra grants) → Continue → end Level (no VictorySettlement placeholder toast) + `LevelSelectPanel`. Defend counterpart **not** done |
| Demo soldier attack / WarriorCombat edge | **PM-12 (Approach B):** PushMap loyal WarriorCombat aligns with §3.12 scheme D — `AttackMode=Melee`: `AttackWindup` end → `HitConfirm` (target alive + in `AttackRange`) → monster `HP -= NormalAttackPower`; `AttackMode=Ranged`: spawn projectile (reuse Defend `ProjectileView`) → soft-collision hit settles / timeout miss does not. `NormalAttackPower` / `AttackSpeed` / windup / projectile params from StartBattle registry (`WarriorCombatMath` + `ClassConfig`, mirrored from Defend). Keep FlowField / AttackSlot / sticky engage; **remove** the old anim-only `PushMapAttackAnimSeconds` loop with no settlement. Monsters may still skip Animator this slice (§15.5) |
| Demo DamagePopup edge | **DamagePopup (PM-12/13):** after rules confirm a hit, show `-damage` above the **hit target** (value matches settled damage). Enemy monsters: red; loyal soldiers: white; font size **12** for both. Over **0.5s**, world `position.z` lerps relative start **+0→+0.5**, then despawn (no sustained Y rise). Protagonist Shield hits **do not** require a popup. Defend mode out of scope for this request |
| Demo HitFlash edge | **HitFlash (PM-12/13):** same successful-hit event as DamagePopup. Temporarily tint target subtree Renderers (MaterialPropertyBlock; do not permanently mutate shared materials). Monster bright **red**; soldier bright **white**. **2** pulses (immediate + one more), each **0.1s**, **no off gap** between them → ≈**0.2s** continuous tint then restore. Hit again mid-flash → restart from t=0. Miss / whiff → no flash. Defend mode out of scope |
| Demo AllyFootCircle edge | **AllyFootCircle:** Defend/PushMap Combat **loyal living** soldiers: green-stroke foot circle + black fill α=**160/255**; radius=`BodyRadius`; localPos `(0,-0.05,-0.2)`; rotation X=**-30**; Order In Layer=`1`; follows movement; hide on Rebel/CombatDead; see [SPEC_04 §9.7](SPEC_04_Technical.md) |
| Demo engage stickiness edge | v0.74.10: engage target selection carries **sticky hysteresis** — while the claimed monster is alive and still inside its engage detect radius, switch claims only if a new candidate's center distance is closer by more than **`EngageStickHysteresisMargin` (default 0.15 world units, constant)**; otherwise keep the current claim. Rationale: dense packs + SoftCollision micro-pushes flip-flop the strictly-nearest target per frame and destabilize AttackSlot / windup. Stickiness changes neither detect radius nor slot legality; target death / leaving range still switches normally (**no longer** tied to retired `DemoKillEngageSeconds`) |
| Demo Boss guidance edge | v0.74.10: when the objective chain is exhausted (`CurrentObjectiveOrder=0`, all captured) and the map has a `BossPoint` → rebuild the `FlowField` toward the BossPoint world XZ (fired once by `CurrentObjectiveChanged(0)`; maps with no objectives rebuild right after StartBattle); `ObjectiveArriveRadius` tightens to **`BossAdvanceArriveRadius` (default 0.35, constant)** so soldiers enter engage detect (≈0.38+ on current configs), convert to `AttackSlot`, and empty Boss HP via HitConfirm; for `Stationary*` bosses this guidance is the only approach means; no `BossPoint` → keep the original "hold position / guard nearby" semantics; camera follow still uses `CameraFollowPath` max projection (does not follow the soldier) |
| Demo soldier-hit edge | **PM-13:** monsters subtract soldier `RemainingHp` by `MonsterConfig.AttackPower` (no armor); successful hit → white DamagePopup + white HitFlash on the soldier. `HP≤0` → `CombatDead` (stop acting; gems / PermanentDeath follow §3.12 Demo-min). Protagonist hits still `Shield -= 1` (ignore AttackPower; no protagonist popup/flash required) |
|| Demo monster facing stabilization edge | v0.75.10: PushMap monster chase facing carries **hysteresis + min dwell** (`FacingHysteresisDegrees`=12°, `FacingSwitchMinDwellSeconds`=0.12s, constants; see SPEC_04 §15.5); when **stuck** (XZ displacement over a 0.25s sliding window < 0.05 while steer is non-zero) stop Run and face the current chase target; exit on displacement recovery or zero steer. `PushMapMonsterAgentView` presentation only; attack checks / slot claims / pathing rules unchanged; soldiers and Defend monsters **out of scope** |

**Spawning (not WaveSpawn countdown)**

| Rule | Notes |
|------|-------|
| Table | `PushMapSpawnConfig` by `GameplayConfigId` + `SpawnPointId`; multi-rows per point OK |
| Non-trap | At StartBattle: if no trap bind and linked objective uncaptured → spawn rows |
| Trap | Bound `TrapZoneId`: first loyal enter while objective uncaptured → spawn once per point this battle (re-enter no re-spawn unless config TBD) |
| Capture stop | Captured linked objective → no new spawns; living remain |
| Footprint spread | Same-point `SpawnCount>1` or nearby living monsters: stagger on walkable NavMesh by each `MonsterConfig.BodyRadius` so XZ footprint circles do not overlap (may shrink ring on SamplePosition failure; final fallback to sampled base); trap spawns likewise avoid existing living monsters. **Demo tighten (v0.73.9):** `SamplePosition` is local only (≈`max(0.75, BodyRadius×2.5)`); accept hit only if XZ distance from spawn `basePos` ≤ leash (≈ current ring/spiral radius + radius slack; absolute cap ≈`max(3, BodyRadius×10)`); over-leash hits fail. When packed, **prefer overlap at base** — do not snap across AirWalls onto empty outer-diamond NavMesh |
| Ongoing avoidance | **Moving** monsters in Combat: spawn spread still uses `BodyRadius`; `NavMeshAgent.radius = min(BodyRadius, max(0.05, AttackRange − BodyAppearanceConfig.DefaultBodyRadius(0.1) − 0.05))` so centers can enter `AttackRange` (RVO radius must not exceed attack reach); `Stationary*` do not relocate; PushMap monsters Demo: `NavMeshAgent.height=0.1` (`PushMapMonsterAgentView`); **loyal soldiers:** `NavMeshAgent.radius` from `BodyAppearanceConfig.BodyRadius` (default `0.1`), `height=0.1` |
| Forbidden | PushMap does **not** use `RemainingCombatSeconds` / `WaveSpawnConfig` activation |
| Demo impl boundary | PM-05 (spawn/trap): monsters use Defend default-chase AI (not four-state, §9.23 contract); AggroMode four-state and Boss-clear settlement **deferred** (PM-06/07); spawn eligibility/trigger state in `PushMapSessionService`; position resolved by View via `SpawnPointId` / `BossPoint`; **StartBattle order**: Bake NavMesh → deploy loyal soldiers → `FireStartBattleSpawns` (avoids agents off-mesh when spawning before bake); footprint spread + agent radius → **PM-10** |
| Demo walkable edge | `DigMapBounds` (and `WalkSurface` / `EngageZone`) must cover sample objectives / spawn points / BossPoint; runtime NavMesh bake uses `DigMapBounds`; markers outside the diamond leave advance/spawns unable to sit on NavMesh. Sample `PushMap_Demo_01` AirWalls must keep objectives walkable (see Demo AirWall edge authoring hard rule) |

**Boss & win/lose**

| Rule | Notes |
|------|-------|
| Clear | Kill Boss from **BossPoint** (`IsBoss=1`) → Ended → credit `StageExpReward` → UI-017 → UI-018 → end Level + LevelSelectPanel (Demo; no auto next stage) |
| Fail | `Shield ≤ 0` **or** no living loyal soldiers → LevelFailure; **no** stage Exp → UI-017 → Continue → AbortLevel + LevelSelectPanel |
| Not used | “All spawn rows fired + all killed” clear condition |
| Rules ownership | Outcome in `PushMapSessionService` (`TryNotifyBossKilled` / `VictorySettled` / `RequestLevelFailure` / loyal-wipe check); Exp via presentation `AddExperience`; driver handoff deferred until settlement Continue |

**Monster AggroMode (≠ AttackMode)**

| AggroMode | Behavior |
|-----------|----------|
| `ActiveChase` | Loyal soldier enters `AlertRadius` → chase-attack until death |
| `PassiveChase` | Only after attacked → chase-attack until death |
| `StationaryActive` | No move; attack while loyal in `AttackRange`; stop when leave |
| `StationaryPassive` | No move; only after attacked and target still in `AttackRange` |

`AttackMode` remains Melee/Ranged (hit scheme D). `AlertRadius` defaults may equal `AttackRange`. `BodyRadius` defaults to `0.35`; PushMap spawn spread and moving-monster `NavMeshAgent.radius` share it ([SPEC_04 §9.19](SPEC_04_Technical.md)).

**Exp boundary:** Only Boss-clear credits `LifetimeExperience`; capture loot has no Exp; normal kills grant no Exp.

```
Enter PushMap (GameplayType=PushMap OR BattleModeSelect Mode2 → §3.14)
  → Prepare → StartBattle → shared CurrentObjective push
  → Non-trap spawns; trap-triggered spawns; Capture on arrive; AggroMode + WarriorCombat
  → Shield≤0 OR no living loyal → LevelFailure → UI-017 → LevelSelect
  → Boss kill → Ended + Exp → UI-017 → UI-018 → LevelSelect
```

---

---

## 3.15 自动制造（AutoManufacture；Mode2）

### 简体中文

**状态：规则库已关闭（本轮）；Demo 实现见 §3.8 D-050～D-055 / D-058 / `.scratch/mode2-auto-manufacture/issues/`；其余魔法书效果行与正式装备 UI 另专题**

当关卡当前阶段 `玩法类型 = AutoManufacture` 时进入本阶段（Mode2 样例运作表：Dig → **本阶段** → UpgradeManufacture）。配置表见 [SPEC_04 §9.9b / §9.12 / §9.24](SPEC_04_Technical.md)。Mode1 **不**进入本阶段。

**阶段边界**

| 规则 | 说明 |
|------|------|
| 进入 | 上一阶段 Dig 经 DigStageSummary 玩家确认后，关卡驱动进入本阶段 |
| 结束 | 算法跑完（造到不能再造 + 自动上阵）后播 **AutoManufacturePresentation**（UI-016；本批 0 兵则跳过），再 **自动**交还 §3.9；**无**玩家确认 |
| 结算 | **无**独立阶段结算 |
| 材料不足 | 进入时仓库连 1 套最低配方都没有 → 0 兵入临时仓库；仍执行「清空布阵」再进下一阶段 |
| 0 兵 Tips | 本批造兵数 = 0（含最低配方不足、无主要手等停造且未造出任何兵）→ 屏幕中上部 Tips「无士兵可制造」，停留约 **1 秒**；**不**阻塞阶段推进 |
| 余料 | 造完后剩余躯体材料 **留在普通仓库**，供下次 Dig 后继续使用 |

**费用屏蔽（Mode2）**

| 规则 | 说明 |
|------|------|
| SpiritCost | 自动制造 **不扣** 精魂；材料行 `SpiritCost` 本流程忽略 |
| ControlPowerCost | 士兵实例 `ControlPowerCost` **恒写 0**；布阵不按控制力拦上阵；UM 控制力 HUD 屏蔽（§3.11 Mode2 差分） |

**最低配方（可造 1 兵）**

必填：**头 1 + 躯干 1 + 臂 2（须含 ≥1 主要手）+ 腿 2**。翼 / 坐骑 / 宝石 **永不**参与本流程。灵魂 **非必要**；本流程 **不消耗、不写入** `SoulId`（手动加灵魂后续需求）。

**单兵流水线（循环）**

```
while 仓库满足最低配方:
  1. 自动选择躯体材料（主要手 → 次要手 → 头/躯干/腿）
  2. 生成职业（双手 ClassRestrict）
  3. 生成基础属性 Base(S)=Σ StatBonus；种族默认定稿（§3.11；造兵时不读魔法书）
  4. 按双手 ClassId 授予 DefaultSkillIds（Lv1）
  5. 士兵最终静态属性定稿并记录（含 SoldierSkills）
  6. 士兵外观定稿
  7. 扣仓库已选躯体 → 入临时仓库 → flush→WarriorPool
清空布阵 →（造兵数>0）UI-016 Step2 单槽节拍套魔法书 → 按职业区自动上阵 → 阶段结束
```

**1. 自动选择躯体材料**

| 步骤 | 规则 |
|------|------|
| 候选池 | 仓库中 `BodySlot ∈ {Head,Torso,Arm,Leg}`；忽略翼/坐骑及 `ExtraEquipment` |
| 近似品质 | 相对锚点 `|ΔBodyLevel| ≤ 1`；同档内排序：**更高 BodyLevel → 相同 → 低 1 级** |
| 主要手 | `BodySlot=Arm` 且 `IsPrimaryHand=1`；按 `BodyLevel` **降序**取一件作锚点；若无 → **停造** |
| 次要手 | `IsPrimaryHand=0` 的 Arm；先滤近似品质；优先 `ClassRestrict` 与主要手有交集；同级随机；**必须**选到，否则停造 |
| 其余部位 | 顺序固定：**头 → 躯干 → 腿1 → 腿2**；锚点 = 主要手的 `BodyLevel` / `BodyPrimaryStat` / `RaceId`；优先级：近似品质 → `BodyPrimaryStat` 相同 → `RaceId` 相同 → 在满足近似品质的剩余中随机；满足近似但无主属性相同则在近似集内随机 |
| 主要手 ClassRestrict 空 | 视为配置错误：**停造**并打日志（不静默跳过坏主要手以免掩盖配表问题） |

**2. 生成职业**

| 规则 | 说明 |
|------|------|
| 来源 | **手**（非灵魂） |
| 交集 | `交集 = Primary.ClassRestrict ∩ Secondary.ClassRestrict` |
| 抽取 | 交集非空 → 均匀随机；空 → **仅**主要手 `ClassRestrict` 均匀随机 |
| 写入 | `WarriorInstance.ClassId`；**不**写 `SoulId` |
| AttackMode | 取 `ClassConfig.AttackMode`（Mode2 扩列） |
| MoveStyle / AttackPriority | 本轮全局默认 `Normal` / `Nearest`（不强制扩表） |

**3. 生成属性（基础）**

对已确定部位：`Base(S) = Σ StatBonus(S)`（同 §3.11）。**造兵时不读魔法书**：种族同 §3.11 默认定稿（全同族→该族，否则 `Race_Undead`）。`ForceRace` / `RaceWeightPick` 等在 UI-016 Step2 单槽脉冲时生效（见下）。

**4. 魔法书触发效果（UI-016 Step2 单槽节拍）**

| 规则 | 说明 |
|------|------|
| 槽位 | 主角默认 **6** 特殊装备槽（一书占 1 槽） |
| 唯一 | 同 `MagicBookId` 默认可叠装；`IsUnique=1` 不可再装第二本 |
| 概率型 | `IsProbabilistic=1` 标记该书为概率触发魔法。`ForceClass` 的 `Chance` **真正 roll**；其它 Token 本轮仍不读本列。`0`=无概率 |
| 触发 | **不在造兵循环内套书。** 本批 flush→池且造兵数>0 后，UI-016 Step2：聚焦该兵 → 6 槽自左→右伸缩（空槽照跳、无效果）；**仅当前缩放到峰值的那一槽**对聚焦兵执行其 `EffectPayload`（须 `EffectPhase` 含 `SoldierManufacture`）。表现层只回调槽索引，规则在钩子内解析 Token |
| 「还原」 | `MagicBook_Restore`：`EffectPayload=RaceWeightPick`、`EffectParams` 空；**该书槽脉冲时**用实例 `SourceItemIds` 对应部位 `RaceId` 权重 **1** 重抽种族并重载 `RaceAdjustCoeff`；`IsUnique=1` |
| 「战士强化」 | `MagicBook_WarriorEnhance`：`EffectPayload=StatMul`、`EffectParams=Stat=Primary\|Mul=1.15\|ClassId=Class_Warrior`；`IsUnique=0` 可叠；仅 Mode2；种族不过滤；职业须为战士；见下 `StatMul` / `Primary` |
| 「士兵技能升级」 | `MagicBook_SoldierSkillLevel`：`EffectPayload=SoldierSkillLevelAdd`、`EffectParams=SkillId=Skill_01\|Delta=1`；`IsUnique=0` 可叠；**该书槽脉冲时**立刻升已有技能（无二次扫描）；见下 |
| 「职业进阶」 | 四本 `IsUnique=1` `IsProbabilistic=1` `EffectPayload=ForceClass` `Chance=0.25`：`MagicBook_WarriorAdvance`（`RequireClassId=Class_Warrior_0`→`ClassId=Class_Warrior`）等。精确 ClassId；仅 Mode2 |
| 指定种族 | `ForceRace`：必填 `RaceId`；**该书槽脉冲时**改写种族（未实现 Token 仍空 apply） |
| 指定职业 | `ForceClass`：必填 `ClassId`；可选 `RequireClassId` / `Chance`（语义同前）。**该书槽脉冲时**判定；命中则改写 `ClassId`/`AttackMode`，并 **Clear 后重授** 新职业 `DefaultSkillIds`@Lv1。若「技能升级」在「进阶」左边，进阶会清掉已加等级 |
| 属性倍率 | `StatMul`：语义同前；`BodySum` 用实例 `SourceItemIds` 反查躯体 `StatBonus`；**该书槽脉冲时**写入 Base |
| 属性加算 / 品质偏移 | `StatAdd` / `QualityDelta`：登记语义同前；未实现则空 apply |
| 士兵技能升级 | `SoldierSkillLevelAdd`：必填 `SkillId`、`Delta`；**该书槽脉冲时**立刻 apply；仅当实例 **已有** 该 `SkillId` 时 `SkillLevel += Delta`（钳制 SkillConfig 最小/最大级）；无该技能则跳过（**不新授**）。**无**消耗经验升级 |
| 每槽后定稿 | apply 后立刻重算 StaticStat / MaxHP / 外观 / 命名；`RemainingHP=MaxHP`；士兵卡立刻刷新职业名与 `Lv.N`；Idle 仍在该兵 6 槽全部结束后揭示 |
| Fallback | 演出未能启动或阶段 `Exit` 中断 → 对未处理槽按左→右瞬时 apply 剩余书，再 persist + 上阵 |
| 编码 | `EffectPayload` 须为 [SPEC_04 §9.24](SPEC_04_Technical.md) 已登记 Token；一书一 Token |
| 其它书 | 未登记 / 未实现 Token 仍空 apply + 警告日志 |
| UI | 本轮 **不做** 装备/卸下 UI |
| Combat 环节 | 枚举预留；本轮不实现 |

**5. 士兵最终属性确认（造兵时）**

造兵循环内：按双手 `ClassId` 授予 `DefaultSkillIds`（Lv1）→ 定稿 StaticStat / BodyLife / MaxHP（同 §3.11）→ 写入实例。魔法书导致的属性/职业/种族变更在 Step2 单槽 apply 后再定稿。

**6. 士兵外观确定**

1. 平均 `BodyLevel` → 保留 1 位小数 → 四舍五入得基础 `AvgLevelInt`（同 Mode1）；若已装备 `QualityDelta` → `AvgLevelInt += ΣDelta`
2. 候选 A：`AppearanceLevel==AvgLevelInt` 且 `RaceId==` 定稿种族
3. **若 A 为空**：定稿种族改为 `Race_Undead`，重载系数与命名种族段，从步骤 2 **仅重跑一轮**（同 §3.11）
4. 子集 B：`ClassAffinity` 含本兵 `ClassName`；B 非空 → 均匀随机
5. 若 A 非空但 B 空，**或** 亡灵改写后 A 仍空 → **`ClassConfig.DefaultAppearanceId`**（非空则用之）
6. 仍无 → 同种族 `IsFallback==1`
7. 仍无 → 全表均匀随机

（说明：A 空仍先亡灵改写，**改写前**不吃 `DefaultAppearanceId`；改写后 A 仍空或 B 空时用默认外形。避免混族 3 级兵因无「亡灵+该等级」行掉到 `App_94` 等保底。）

**7. 完成制造放入临时仓库**

扣已选躯体材料；实例入 **临时仓库**（批内缓冲，尚未改布阵）。命名：`RaceDisplayName + ClassName`（无外置前缀、无宝石后缀）。

**8. 循环判定**

剩余材料不满足最低配方 → 结束循环。

**9. 临时仓库士兵自动上阵**

| 规则 | 说明 |
|------|------|
| 清空 | Enter 时 **先清空**当前 `BattleFormation` 全部上阵位 |
| 入池 | 临时仓库士兵（无书定稿）写入 `WarriorPool` 并持久化；批次记录可在 flush 后立刻写入 |
| 上阵时机 | **推迟到** UI-016 Step2 全部士兵单槽套书完成（或 fallback 瞬时套完）之后，再按**最终** `ClassId` 落入职业区（避免 `ForceClass` 进错区） |
| 排序 | 按士兵 `ClassId` → `ClassConfig.PlacementOrder` **升序**；同序稳定按实例 Id |
| 区域 | 布阵地图 Prefab 上 `FormationClassZone`（绑定 `ClassId`）；体积 = **IsoDiamond**（同 WalkSurface；无 Y 旋转） |
| 放置 | 区内螺旋 + `BodyRadius`；失败留池 |
| 失败 | 仍放不下 / 无匹配区 → 该兵 **留在池、不上阵**（UM 可手布） |
| 旧兵 | 池内本批之前的旧兵 **不**自动再上（仅本批 Id 上阵） |
| 读区 | 无头 Stage 可短时 Instantiate 下一关 BattleMap 采集区快照后销毁 |
| 批次记录 | flush 后把本批 `WarriorId` **整表替换**写入 `AutoManufactureBatchRecord`（含 0 兵空列表） |

**10. 制造记录（UM 只读弹窗；UI-015 / D-054）**

| 规则 | 说明 |
|------|------|
| 入口 | **仅 Mode2** UM；「布阵」**右侧**「制造记录」；Mode1 **无**此按钮 |
| 范围 | **仅最近一批** AutoManufacture flush 的 Id；不是全池、不是多批历史 |
| 展示 | 只读列表：`WarriorName` + 种族展示名 + `ClassName`（`｜` 分隔）；不点开详情、不再造 |
| 空态 | 本批 0 兵，或 Id 在 `WarriorPool` 中全部缺失 → 文案「本批无士兵」 |
| 缺兵 | 个别 Id 已不在池（死亡/移除）→ **跳过该行**，仍展示其余 |
| 覆盖 | 下一次 AutoManufacture 批末覆盖记录；同档退出再进仍可见上一批 |
| 删档 | 清该槽两模式 `AutoManufactureBatch` 键 |

**11. 自动制造演出（阶段表现；UI-016 / D-055；方案 A + 单槽节拍套书）**

规则层同步跑批（选料→无书造兵→flush→批次记录；**此时不上阵**）。本批造兵数 **>0** 时挂表现 Prefab：Step2 单槽脉冲即套该书 → 全部播完再自动上阵 → 交还驱动；**0 兵**仅 Tips「无士兵可制造」，跳过演出/套书，进 UM **不**自动开布阵。

| 步骤 | 规则 |
|------|------|
| Step1 | 画面中央横滑士兵行；每卡 **宽 150 × 高 200**；卡中央「?」字号 **42** + 其下职业名（造兵时双手职业，尚未套书）字号 **32** + 再下 `Lv.{ClassLevel}` 字号 **24**；士兵行**上方** 6 个魔法书方框（各 **120×160**） |
| Step2 | 以单兵为单位：目标卡移至画面中央；6 书框**依次**伸缩；**缩放到峰值时**对该兵执行**仅该槽**魔法书；士兵卡立刻刷新职业名/`Lv.N`；卡上临时字「加强」；该兵 6 槽结束后「?」→ Idle（读套书后外观）；每完成 **3** 兵速率 × `1.25^floor(completed/3)` |
| Step3 | 全部士兵 Step2 完成后 **先按最终 ClassId 自动上阵**，再进 `UpgradeManufacture` 并**自动打开**布阵编辑器 |

```
AutoManufacture stage
  → Clear BattleFormation
  → while warehouse can craft min recipe:
       pick parts; ClassId from hands; Base=Σ StatBonus; Race default (§3.11)
       Grant DefaultSkillIds@Lv1; finalize StaticStat/Appearance; flush → WarriorPool
  → Replace AutoManufactureBatchRecord
  → if crafted>0: play UI-016 Step1–2 (each book pulse peak → ApplyAtSlot only that book)
  → DeployBatch by final ClassId into FormationClassZone
  → Auto return → UpgradeManufacture (+ auto-open Formation if presentation ran)
```

**待实现优先级（规则已锁；编码另切片）**

| 优先级 | 内容 |
|--------|------|
| P0 | 表扩列 + AutoManufacture 阶段 + 选料/职业/属性循环 + 临时仓库 + 自动上阵 + Mode2 UM 差分 |
| P1 | 魔法书 6 槽存档 + 空钩子；制造记录弹窗（最近一批）；自动制造演出 UI-016 |
| P2 | 其余魔法书效果 / 正式装备 UI / 灵魂手动装配 |

### English

**Status: Rules library closed (this round); Demo impl §3.8 D-050–D-055 / D-058 / `.scratch/mode2-auto-manufacture/issues/`; remaining MagicBook effect rows and formal equip UI later**

Entered when Level stage `GameplayType = AutoManufacture` (Mode2 sample LevelOperation: Dig → **this stage** → UpgradeManufacture). Config: [SPEC_04 §9.9b / §9.12 / §9.24](SPEC_04_Technical.md). Mode1 does **not** enter this stage.

**Stage boundary**

| Rule | Notes |
|------|-------|
| Enter | After Dig DigStageSummary player confirm |
| End | When algo finishes (craft until cannot + auto-deploy) → play **AutoManufacturePresentation** (UI-016; skip if batch 0) → **automatic** return to §3.9; **no** player confirm |
| Settlement | **No** independent stage settlement |
| Insufficient stock | If warehouse cannot craft even one min recipe → 0 soldiers in temp warehouse; still **clear formation** then advance |
| Zero-craft Tips | Batch crafted count = 0 (incl. min-recipe short / no PrimaryHand stop with zero crafts) → upper-center Tips「无士兵可制造」for ~**1s**; does **not** block stage advance |
| Leftovers | Remaining body parts stay in normal Warehouse for the next Dig cycle |

**Cost shield (Mode2)**

| Rule | Notes |
|------|-------|
| SpiritCost | AutoManufacture does **not** spend Spirit; ignore part `SpiritCost` |
| ControlPowerCost | Instance `ControlPowerCost` **always 0**; deploy not gated by ControlPower; UM ControlPower HUD hidden (§3.11 Mode2 diffs) |

**Min recipe (one soldier)**

Required: **Head 1 + Torso 1 + Arm 2 (incl. ≥1 PrimaryHand) + Leg 2**. Wing / Mount / Gems **never** participate. Soul is **optional** and **not** consumed/written this flow (manual soul later).

**Per-soldier pipeline (loop)**

```
while warehouse meets min recipe:
  1. Auto-pick body parts (PrimaryHand → SecondaryHand → Head/Torso/Legs)
  2. Generate Class (hand ClassRestrict)
  3. Base stats Base(S)=Σ StatBonus; race default finalize (§3.11; no MagicBook at craft)
  4. Grant DefaultSkillIds at Lv1 from hand ClassId
  5. Finalize static stats snapshot (incl. SoldierSkills)
  6. Finalize Appearance
  7. Consume warehouse parts → TempWarriorWarehouse → flush→WarriorPool
Clear formation → (if crafted>0) UI-016 Step2 per-slot MagicBook apply → auto-deploy by class zone → stage end
```

**1. Auto-pick body parts**

| Step | Rules |
|------|-------|
| Pool | Warehouse `BodySlot ∈ {Head,Torso,Arm,Leg}`; ignore Wing/Mount/`ExtraEquipment` |
| Approx quality | `|ΔBodyLevel| ≤ 1` vs anchor; within band sort **higher → same → lower-by-1** |
| PrimaryHand | `Arm` + `IsPrimaryHand=1`; pick max `BodyLevel` as anchor; if none → **stop crafting** |
| SecondaryHand | `IsPrimaryHand=0` Arms; filter approx; prefer ClassRestrict overlap with Primary; random among ties; **must** pick one else stop |
| Remaining | Fixed order **Head → Torso → Leg1 → Leg2**; anchor = PrimaryHand BodyLevel / BodyPrimaryStat / RaceId; priority: approx → same BodyPrimaryStat → same RaceId → random among approx; if approx but no BodyPrimaryStat match → random in approx set |
| Empty Primary ClassRestrict | Config error: **stop crafting** and log |

**2. Generate Class**

| Rule | Notes |
|------|-------|
| Source | **Hands** (not Soul) |
| Intersect | `Primary.ClassRestrict ∩ Secondary.ClassRestrict` |
| Roll | Non-empty → uniform; empty → **PrimaryHand ClassRestrict only** |
| Write | `WarriorInstance.ClassId`; **no** `SoulId` |
| AttackMode | From `ClassConfig.AttackMode` |
| MoveStyle / AttackPriority | This round global defaults `Normal` / `Nearest` |

**3. Base stats**

`Base(S)=Σ StatBonus(S)` over chosen parts (§3.11). **No MagicBook at craft**: race uses §3.11 default (same-race / `Race_Undead`). `ForceRace` / `RaceWeightPick` apply later at UI-016 Step2 per-slot pulse.

**4. MagicBook effects (UI-016 Step2 per-slot beat)**

| Rule | Notes |
|------|-------|
| Slots | Default **6** special slots (one book per slot) |
| Unique | Same MagicBookId stackable unless `IsUnique=1` |
| Probabilistic | `IsProbabilistic=1` marks chance-trigger. `ForceClass` `Chance` **rolls**; other tokens ignore this column this round |
| Trigger | **Not during the craft loop.** After flush→pool with crafted>0, UI-016 Step2: focus soldier → 6 slots pulse left→right (empty slots pulse, no effect); **only the slot at peak scale** applies its `EffectPayload` (`EffectPhase` must include `SoldierManufacture`). View callbacks slot index only; Core parses tokens |
| Restore | `MagicBook_Restore`: `RaceWeightPick`; **on that slot's pulse**, weight-1 re-pick race from instance `SourceItemIds` part RaceIds and reload `RaceAdjustCoeff`; `IsUnique=1` |
| Warrior Enhance | `MagicBook_WarriorEnhance`: `StatMul` / `Stat=Primary` / `ClassId=Class_Warrior`; stackable; Mode2 only |
| Soldier skill level | `MagicBook_SoldierSkillLevel`: `SoldierSkillLevelAdd`; **on that slot's pulse** immediately; no second pass |
| Class advance | Four `ForceClass` books with `Chance=0.25`; Mode2 only |
| Force race | `ForceRace`: required `RaceId`; apply **on that slot's pulse** (unimplemented → empty apply) |
| Force class | `ForceClass`: on pulse; on hit rewrite class and **Clear then re-grant** new `DefaultSkillIds`@Lv1. Skill-level books left of advance are wiped by promotion |
| Stat mul | `StatMul`: `BodySum` from `SourceItemIds`; apply on that slot's pulse |
| Stat add / Quality | Registered; unimplemented → empty apply |
| Soldier skill level | `SoldierSkillLevelAdd`: on pulse; only if instance already has `SkillId`; clamp to SkillConfig range; **no** new grant |
| After each slot | Refinalize StaticStat / MaxHP / appearance / name; `RemainingHP=MaxHP`; card refreshes class name / `Lv.N`; Idle still after that soldier's 6 slots |
| Fallback | Presentation fail or stage Exit → instant `ApplyRemaining` left→right, then persist + deploy |
| Encoding | Registered `EffectPayload` tokens ([SPEC_04 §9.24](SPEC_04_Technical.md)) |
| Other books | Unregistered / unimplemented → empty apply + warn |
| UI | **No** equip/unequip UI this round |
| Combat phase | Enum reserved; not implemented |

**5. Final soldier stats (at craft)**

In craft loop: grant `DefaultSkillIds` (Lv1) from hand `ClassId` → finalize StaticStat / BodyLife / MaxHP → write instance. MagicBook mutations refinalize after each Step2 slot apply.

**6. Appearance**

1. Mean BodyLevel → 1 decimal → round base `AvgLevelInt` (Mode1); if `QualityDelta` equipped → `AvgLevelInt += ΣDelta`
2. Set A: level + race match
3. **If A empty**: force `Race_Undead`, reload coeffs + name race segment, re-run from step 2 **once** (§3.11)
4. Subset B: ClassAffinity contains ClassName; if non-empty uniform pick
5. If A non-empty but B empty, **or** A still empty after Undead rewrite → **`ClassConfig.DefaultAppearanceId`** if non-empty
6. Else race `IsFallback==1`
7. Else full-table uniform

(Note: A-empty still rewrites to Undead first — do **not** eat `DefaultAppearanceId` before the rewrite; after rewrite, use default when A is still empty or B is empty. Prevents mixed-race Lv3 soldiers falling to `App_94` when no Undead+level row exists.)

**7. Temp warehouse**

Consume chosen parts; instance → **TempWarriorWarehouse**. Name: `RaceDisplayName + ClassName` (no prefixes/suffixes).

**8. Loop check**

Stop when remaining stock cannot satisfy min recipe.

**9. Auto-deploy**

| Rule | Notes |
|------|-------|
| Clear | On Enter, **clear** all BattleFormation slots first |
| Pool | Flush pre-book soldiers → WarriorPool + persist; batch record may write right after flush |
| Deploy timing | **Deferred** until UI-016 Step2 finishes all per-slot applies (or fallback instant apply), then deploy by **final** ClassId |
| Order | By `ClassConfig.PlacementOrder` ascending; tie-break instance Id |
| Zones | Map Prefab `FormationClassZone` IsoDiamond (same as WalkSurface; no Y rotation) |
| Place | In-zone spiral + BodyRadius; fail → stay in pool |
| Old pool | Prior pool soldiers are **not** auto-redeployed (only this batch Ids) |
| Batch record | After flush, **replace** `AutoManufactureBatchRecord` with this batch Ids |

**10. Manufacture record (UM read-only popup; UI-015 / D-054)**

| Rule | Notes |
|------|-------|
| Entry | **Mode2 UM only**; "Manufacture Record" to the **right** of Formation; Mode1 has **no** button |
| Scope | **Last AutoManufacture batch** Ids only; not full pool, not multi-batch history |
| Display | Read-only: `WarriorName` + race display name + `ClassName` (`｜`-separated); no detail tap, no remake |
| Empty | Batch 0 soldiers, or all Ids missing from `WarriorPool` → 「本批无士兵」 |
| Missing | Individual Id gone from pool (death/remove) → **skip that row**, still show the rest |
| Overwrite | Next AutoManufacture batch replaces the record; survives leave/re-enter same save |
| Delete save | Clear both-mode `AutoManufactureBatch` keys for that slot |

**11. AutoManufacture presentation (stage View; UI-016 / D-055; Approach A + per-slot MagicBook)**

Rules run the batch synchronously (pick→craft without books→flush→batch record; **no deploy yet**). When crafted **>0**, mount presentation: Step2 per-slot pulse applies that book → then deploy by final ClassId → advance; **0 craft** Tips only, skip presentation/books, UM does **not** auto-open Formation.

| Step | Rules |
|------|-------|
| Step1 | Center soldier row 150×200; "?" 42 + hand ClassName 32 + `Lv.{ClassLevel}` 24 (pre-book); 6 MagicBook frames 120×160 above |
| Step2 | Per soldier: focus card; pulse 6 books left→right; **at peak scale** apply **only that slot** to the focused soldier; refresh card class/`Lv.N`; 「加强」 label; after 6 slots "?" → Idle (post-book appearance); every 3 soldiers rate × `1.25^floor(completed/3)` |
| Step3 | After all Step2 → **DeployBatch by final ClassId** → enter UM and auto-open FormationEditor |

```
AutoManufacture stage
  → Clear BattleFormation
  → craft without MagicBook → flush → WarriorPool; Replace batch record
  → if crafted>0: UI-016 Step1–2 (pulse peak → ApplyAtSlot)
  → DeployBatch by final ClassId
  → UpgradeManufacture (+ auto-open Formation if presentation ran)
```

**Impl priority (rules locked; coding in separate slices)**

| Priority | Content |
|----------|---------|
| P0 | Table columns + AutoManufacture stage + pick/class/stat loop + temp warehouse + auto-deploy + Mode2 UM diffs |
| P1 | MagicBook 6-slot save + empty hook; ManufactureRecord popup (last batch); AutoManufacture presentation UI-016 |
| P2 | Remaining MagicBook effects / formal equip UI / manual soul |

---

## 3.16 主角装备（ProtagonistEquipment）

### 简体中文

**状态：规则库已关闭（本轮）；实现与 Demo 验收另授权；获取来源 / 制造·战斗 Token 登记表仍 TBD**

与 `MagicBook`（§3.15 特殊装备槽装配）、材料 `Warehouse`（§3.10）、士兵 `ExtraEquipment`（§3.11）**并行**，互不替代。配置表见 [SPEC_04 §9.25](SPEC_04_Technical.md)。

**定位**

| 规则 | 说明 |
|------|------|
| 状态仓 | 主角装备仓库是存档级「状态系统」：存储已获得装备实例；**不**占用材料仓库格 |
| 种类上限 | **不限制**已拥有装备种类总数 |
| 同 Id | 每种 `EquipId` 仓内 **至多 1 件**（`OwnedEquip`） |
| 生效 | **无需**再装配到槽；仓内拥有即按该件**当前等级**对应表行效果生效 |
| 公共经验 | `EquipCommonExp` 独立池；**不**与 `LifetimeExperience` / 主角等级互通 |

**获得与同 Id 转化**

| 步骤 | 规则 |
|------|------|
| 首次获得 | 仓内无该 `EquipId` → 新建 `OwnedEquip{ EquipId, Level=1, CurrentExp=0 }`，立即按 Lv1 行重算效果 |
| 再获同 Id | **不**增加件数；按该件**当前等级**表行 `ConvertExpValue` 加入 `CurrentExp`，再尝试升级 |
| 满级再获 | 若已满级（无下一 `EquipLevel` 行，或当前行 `ExpToNextLevel` 空/≤0）→ 同 Id 转化经验 **改入 `EquipCommonExp`**（不废弃） |
| 获取来源 | Dig 掉落 / GM / 商店等 **TBD**；本轮只锁入账算法 |

**升级**

| 规则 | 说明 |
|------|------|
| 阈值 | 升到下一级需 `CurrentExp ≥` 当前行 `ExpToNextLevel`（`ExpToNextLevel` 空或 ≤0 → 已满级，不可再升） |
| 经验来源 | （1）同 Id 转化累计进 `CurrentExp`；（2）从 `EquipCommonExp` **划入**该件 `CurrentExp`（划入即扣公共池，数量由玩家/UI 指定，本轮不锁 UI） |
| 连升 | 足够则连升；每升一级：`Level += 1`，`CurrentExp -= ExpToNextLevel`（扣的是升前所在行阈值）；切到下一等级行效果 |
| 溢出 | 满级后：`CurrentExp` 可保留已划入但未消费的部分；满级后同 Id 转化见上表「改入公共池」 |

**效果与重算**

| 规则 | 说明 |
|------|------|
| 效果域 | 表列 `EffectDomain`：`Dig` \| `SoldierManufacture` \| `Combat`（可多值 `\|`） |
| Dig | `EffectDomain` 含 `Dig` 时，解析当前等级行 `EquipEffect`（编码同科技 `AttributeModifiers`：`Attr_Value\|…`；键见 [SPEC_04 §9.17](SPEC_04_Technical.md)，含 `DigCursorRadius` 等） |
| 能力叠加 | `DigProtagonistCapabilities` 重算 = Σ 已学会科技 `AttributeModifiers` **+** Σ 仓内各装备当前行 Dig 域 `EquipEffect`（**按键加法**） |
| 制造 / 战斗 | 域枚举已锁；`SoldierManufacture` / `Combat` 的 Token 登记表与 handler **TBD**（**不**复用 MagicBook `EffectPayload` 列名空间；另立 `ProtagonistEquipEffect` Token 表） |
| 触发时机 | 获得 / 转化 / 划入公共经验升级 / 卸除（若日后支持）后重算；进档加载后亦重算 |

**持久化意图（SaveSlot + CampaignMode）**

| 字段 | 说明 |
|------|------|
| `EquipCommonExp` | 非负整数（或 number） |
| `OwnedEquip[]` | `{ EquipId, Level, CurrentExp }[]` |

键名意图见 [SPEC_04 §6](SPEC_04_Technical.md)；**PE-02 已实现**（`ProtagonistEquipmentService` + PlayerPrefs）。Dig caps 合并 **PE-03 已实现**（`TechTreeService` 科技+Dig 装备加法）。

**Demo 装备目录**

仅当前等级行生效，故 `EquipEffect` 为该级**累计**加成。Demo 基数 `DigCursorRadius=0.6`（`Tech_Root`）。旧样例 `Equip_DigRing`（挖坟之环）已删除。

| EquipId | DisplayName | 等级 | EffectDomain | EquipEffect（当前行） | ExpToNextLevel | ConvertExpValue |
|---------|-------------|------|--------------|----------------------|----------------|-----------------|
| `Equip_IronShovel` | 铁铲 | 1～5 | `Dig` | 相对基数每级 +10%：L1 `DigCursorRadius_0.06` … L5 `_0.30`（满级 +科技根项 = **0.90**） | L1–4 = **1**；L5 空 | 每级 **1** |
| `Equip_MinerLamp` | 矿灯 | 1～5 | `Dig` | 每级 Q4/Q5/Q6 生成权重累计 +10：L1 `GraveSpawnWeightBonus_Q4_10\|…_Q5_10\|…_Q6_10` … L5 `_50`；表中缺席视为 0 再加成 | L1–4 = **1**；L5 空 | 每级 **1** |

**明确非范围（本轮规则录入）**

- 不改 MagicBook / SpecialEquipSlots / 材料 Warehouse / ExtraEquipment
- 正式装备 UI / 商店 / Dig 掉落入账后置；Demo 垂直见 §3.8 **D-059**（表加载 + 仓 + Dig caps + GM）

```
Acquire(EquipId)
  → if no OwnedEquip: create Level=1 CurrentExp=0 → RecalcCaps
  → else if not max level: CurrentExp += ConvertExpValue(current row) → TryLevelUp → RecalcCaps
  → else: EquipCommonExp += ConvertExpValue(current row)

TryLevelUp(owned)
  → while next row exists AND ExpToNextLevel > 0 AND CurrentExp >= ExpToNextLevel:
       CurrentExp -= ExpToNextLevel; Level += 1

SpendCommonExp(owned, amount)
  → deduct EquipCommonExp; owned.CurrentExp += amount → TryLevelUp → RecalcCaps

RecalcCaps
  → DigProtagonistCapabilities = TechSum + EquipDigSum (additive per key)
```

### English

**Status: Rules closed this pass; implementation / Demo acceptance require separate authorization; acquire sources and Manufacture/Combat token registry still TBD**

**Parallel** to MagicBook (§3.15 slotted), material Warehouse (§3.10), and soldier ExtraEquipment (§3.11). Table: [SPEC_04 §9.25](SPEC_04_Technical.md).

**Positioning**

| Rule | Notes |
|------|-------|
| Status warehouse | Protagonist equipment warehouse is save-scoped status storage for owned gear; **not** material Warehouse slots |
| Kind cap | **Unlimited** distinct owned `EquipId`s |
| Same Id | At most **one** `OwnedEquip` per `EquipId` |
| Apply | **No** extra equip-to-slot step; owned applies at **current level** row |
| Common Exp | `EquipCommonExp` independent; **no** interchange with `LifetimeExperience` / protagonist level |

**Acquire & same-Id convert**

| Step | Rules |
|------|-------|
| First acquire | No owned → create `OwnedEquip{ EquipId, Level=1, CurrentExp=0 }`; recalc from Lv1 |
| Duplicate Id | **No** stack count; add current-level row `ConvertExpValue` to `CurrentExp`, then TryLevelUp |
| Maxed duplicate | If maxed (no next `EquipLevel` row, or current `ExpToNextLevel` empty/≤0) → convert Exp goes to **`EquipCommonExp`** |
| Sources | Dig loot / GM / shop **TBD**; this pass locks credit algorithm only |

**Level-up**

| Rule | Notes |
|------|-------|
| Threshold | Need `CurrentExp ≥` current row `ExpToNextLevel` (empty/≤0 → maxed) |
| Sources | (1) same-Id convert into `CurrentExp`; (2) transfer from `EquipCommonExp` into that piece (deducts pool; UI amount **TBD**) |
| Chain | Level up while possible; each step: `Level += 1`, subtract prior row `ExpToNextLevel` from `CurrentExp` |
| Overflow | After max: leftover `CurrentExp` may remain; further same-Id converts → common pool |

**Effects & recalc**

| Rule | Notes |
|------|-------|
| Domains | `EffectDomain`: `Dig` \| `SoldierManufacture` \| `Combat` (multi `\|`) |
| Dig | When domain includes `Dig`, parse current-row `EquipEffect` (same encoding as tech `AttributeModifiers`; keys in [SPEC_04 §9.17](SPEC_04_Technical.md), e.g. `DigCursorRadius`) |
| Cap stack | `DigProtagonistCapabilities` = Σ learned tech modifiers **+** Σ owned current-row Dig `EquipEffect` (**additive per key**) |
| Manufacture / Combat | Domain enum locked; Token registry / handlers **TBD** (**not** MagicBook `EffectPayload` namespace; separate `ProtagonistEquipEffect` token table) |
| When | Recalc after acquire / convert / common-Exp spend / (future unequip); also after save load |

**Persistence intent (SaveSlot + CampaignMode)**

| Field | Notes |
|-------|-------|
| `EquipCommonExp` | Non-negative |
| `OwnedEquip[]` | `{ EquipId, Level, CurrentExp }[]` |

Key intent: [SPEC_04 §6](SPEC_04_Technical.md); **PE-02 implemented** (`ProtagonistEquipmentService` + PlayerPrefs). Dig caps merge **PE-03 implemented** (`TechTreeService` tech + Dig gear additive).

**Demo gear catalog**

Only the current-level row applies, so `EquipEffect` is the **cumulative** bonus at that level. Demo base `DigCursorRadius=0.6` (`Tech_Root`). Former sample `Equip_DigRing` (Dig Ring) **removed**.

| EquipId | DisplayName | Levels | EffectDomain | EquipEffect (current row) | ExpToNextLevel | ConvertExpValue |
|---------|-------------|--------|--------------|---------------------------|----------------|-----------------|
| `Equip_IronShovel` | Iron Shovel | 1–5 | `Dig` | +10% of base per level: L1 `DigCursorRadius_0.06` … L5 `_0.30` (max + tech root = **0.90**) | L1–4 = **1**; L5 empty | **1** each level |
| `Equip_MinerLamp` | Miner Lamp | 1–5 | `Dig` | Q4/Q5/Q6 spawn-weight cumulative +10 per level: L1 `GraveSpawnWeightBonus_Q4_10\|…_Q5_10\|…_Q6_10` … L5 `_50`; missing table Id treated as 0 then bonus | L1–4 = **1**; L5 empty | **1** each level |

**Out of scope this rules pass**

- No MagicBook / SpecialEquipSlots / material Warehouse / ExtraEquipment changes
- Formal equip UI / shop / Dig loot credit deferred; Demo vertical = §3.8 **D-059** (table load + warehouse + Dig caps + GM)

```
Acquire(EquipId)
  → if no OwnedEquip: create Level=1 CurrentExp=0 → RecalcCaps
  → else if not max level: CurrentExp += ConvertExpValue(current row) → TryLevelUp → RecalcCaps
  → else: EquipCommonExp += ConvertExpValue(current row)

TryLevelUp(owned)
  → while next row exists AND ExpToNextLevel > 0 AND CurrentExp >= ExpToNextLevel:
       CurrentExp -= ExpToNextLevel; Level += 1

SpendCommonExp(owned, amount)
  → deduct EquipCommonExp; owned.CurrentExp += amount → TryLevelUp → RecalcCaps

RecalcCaps
  → DigProtagonistCapabilities = TechSum + EquipDigSum (additive per key)
```

---

## 待澄清清单

### 简体中文

- [ ] 壳层内三种 `GameplayState` 的手动切换触发
- [ ] 关卡场景绑定与从工具/流程进入真实关卡的路径
- [x] 挖坟障碍物类型与几何、以及「可放置」判定细节（仅未消除 Grave；圆形半径在预制体上；见 §3.10）
- [x] 玩家挖坟交互与单坟奖励产出表现及入账（见 §3.10；Warehouse / SpiritEssence）
- [x] 挖坟阶段结束与结算：无胜负；有效时长=配置基础+科技时长加成；DigStageSummary 仅汇总无额外发放（见 §3.10 / UI-011）
- [ ] 胜利结算 UI / 字段
- [x] 坟墓品质定义表字段与 `LootDrop` 编码（见 SPEC_04 §9.3：`DropMode` + `Id;Weight;Count`；MaxHP 具体数值仍 TBD）
- [x] 权重零值剔除与 Dig 空有效权重列表放弃该次生成（见 SPEC_04 §9 通用规则 / §3.10）
- [ ] 坟墓品质表 `MaxHP` 具体数值
- [x] 挖坟四项科技绑定能力算法（伤害 / 单次速度 / 光标半径 / 可挖类型；见 §3.10 `DigProtagonistCapabilities`）
- [x] 科技树框架：中心向外 / InitiallyUnlocked 默认学会 / 前后置与 LearnCost / 画布交互（§3.13；UI-012）
- [x] 科技树配置表 `TechTreeConfig` + 效果表 `TechEffectConfig` 字段（SPEC_04 §9.16–§9.17）
- [ ] 科技树各节点具体数值、图标资源与功能系统名完整枚举
- [ ] 挖坟帧动画具体数量与资源命名清单
- [x] 升级与制造框架（§3.11；原 SewRevive 更名 UpgradeManufacture）— **框架已关闭**
- [x] 升级与制造阶段结束=玩家确认；**无独立阶段结算**
- [x] 升级与制造主屏布局（默认全屏制造 + 升级 Modal「GM升级」+ 底部完成/布阵；UI-010）；制造区方格拖拽与外观可视预览见 §3.11
- [x] BattleFormation：§3.11 与 Defend Prepare **同一 FormationEditor**；连续坐标；拖拽士兵栏；Prepare 不可制造
- [x] 经验：Defend 阶段胜利统一入账至 `LifetimeExperience`；升级不扣减累计经验；科技树消费见 §3.13
- [x] 士兵=独立实例；**士兵制造流程/槽位/最低要求/精魂闸门/命名已关闭**（§3.11）；**士兵技能授予框架已关闭**（`DefaultSkillIds` → `SoldierSkills` Lv1；Mode1 不读魔法书升技能；PermanentDeath 删除）
- [x] 躯体材料表 `BodyPartConfig` 完整字段 + `Base(S)=Σ StatBonus`；躯体外观表与选取/保底算法（§3.11 / SPEC_04 §9.12–§9.13）；具体数值行仍 TBD
- [x] 控制力上限=当前等级行 `ControlPowerCap`（科技加成另专题）；失控程度/四档/叛变判定与概率公式已关闭（§3.11 / §3.12 / SPEC_04 §9.20）；失控不挡开战
- [x] 无上阵士兵时不允许开战（须 ≥1）
- [x] 关卡失败：不入账本阶段经验、无关卡结算奖励；已获得不扣除
- [x] 主角升级配置表 `ProtagonistLevelConfig` 字段与累计阈值语义（SPEC_04 §9.8）；各行具体数值仍 TBD
- [x] 士兵控制力占用值 = 躯体+灵魂+额外装备+宝石叠加（制造时定稿）；灵魂配置表 `SoulConfig`（SPEC_04 §9.9）；职业配置表 `ClassConfig`（SPEC_04 §9.9b）；宝石配置表 `GemConfig`（SPEC_04 §9.10）；种族配置表 `RaceConfig`（SPEC_04 §9.11）
- [x] 宝石：制造可选镶嵌（**6 槽、类型互斥**）；五维 `GemMult`（多颗按维 **Σ**）；**彻底死亡**全部回仓库，其余绑定材料销毁；带宝石士兵 HP≤0 立即彻底死亡
- [x] 种族：默认同族否则 `Race_Undead`；Mode2「还原」→权重 1 加权随机；五维 `RaceAdjustCoeff`；不另计控制力；为主标签来源
- [x] 外观：A 空→改写 `Race_Undead` 再选；B 空或改写后 A 仍空→Mode2 DefaultAppearanceId / 同族 IsFallback；全表兜底
- [x] FinalStat 按单项属性汇总（先定 `S` 再取来源）；`FinalStat(S)=max(0, …)` 下限保护
- [x] 士兵命名：`Prefix(es)+RaceName+ClassName+Suffix`（外置前缀 / 种族 / 职业 ClassConfig.ClassName / 宝石后缀表）
- [ ] 躯体/外观具体数值与美术资源清单（另专题）
- [ ] 额外装备完整配置数值（表结构已锁：SPEC_04 §9.14）
- [ ] 失控配置表 / 种族·宝石·技能失控加成具体数值行（另专题）
- [ ] 灵魂 / 职业表具体数值（结构与编码已锁：CombatConvertCoeffs / MoveStyle / AttackPriority；本批 AttackPriority 不驱动选目标）
- [ ] 宝石获取途径、五维 GemMult/技能具体数值、镶嵌 UI 与回仓表现（另专题；GemType 六类与 ComboKey 编码已锁）
- [ ] 种族列表与各维 RaceAdjustCoeff 具体数值（另专题）
- [x] 升级 Modal（GM升级 / X）与制造区布局（方格拖拽、环绕槽、外观可视预览闸门）；升级区数值展示 polish 仍可迭代
- [x] 防守（Defend）框架：准备/开战/部署/NavMesh 寻路/阶段胜利与关卡失败（§3.12）
- [x] 防守刷怪波次表、倒计时激活节奏与出现位置/方式（§3.12 / SPEC_04 §9.18）；**Demo 最小刷怪点/NavMesh 已关闭**；精确 OutsideMap 几何后置
- [x] Demo 验收扩大：Meta 壳 + Dig→UM→Defend 流水线（SPEC_03 §3.8 D-001～D-043）；UM `GameplayConfigId`=忽略
- [x] 怪物配置表与目标选择（§3.12 / SPEC_04 §9.19）；怪物对士兵：`AttackPower` 直接扣 HP（本批无护甲）；AttackRange 等命中列已锁
- [x] 士兵战斗派生：ClassId→ClassConfig.PrimaryStat / NormalAttackPower / AttackSpeed / SkillCooldown / MaxHP=ceil(BodyLife+Str×MaxHpStrengthMult)；CombatConvertCoeffs + CombatConstantConfig 已锁（§3.11 / §3.12 / SPEC_04 §9.9b / §9.20b）
- [x] 士兵战斗（WarriorCombat）：EngageZone 最近选敌、AttackMode（SoulConfig）、AttackRange（ClassConfig）、命中方案 D、CombatDead / PermanentDeath / 宝石特例（§3.12）；**第一版 Demo 仅普攻**（士兵与怪物不施放技能；法师=远程+Intelligence，同射手通道）
- [x] 护盾（Shield）：开战取 `ProtagonistMaxHP`；普通攻击命中 −1（含叛变士兵）；归零 LevelFailure（§3.12）
- [x] 失控判定时机与叛变 AI（开战锁定 Degree；就近目标；技能二次完整率 roll——**Demo 无技能施放故不触发二次 roll**）（§3.11 / §3.12）
- [ ] 防守阶段结算其余字段；关卡失败结算 UI / 字段
- [ ] 怪物技能效果表；技能命中主角是否扣盾；士兵技能效果与复活技能（另专题；**Demo 不实现**）；精确 OutsideMap 出生几何（Demo 后置）
- [x] 科技树画布 Demo 垂直（方案 A：学习扣点 + Dig 能力重算可验；非 §3.8 P0）
- [ ] 科技树节点具体数值/图标 polish 与功能系统名完整枚举
- [ ] 设置项清单（科技树入口已定；其它设置项 TBD）
- [ ] 存档完整字段（显示名、时间戳、局内进度等）
- [x] ToolsPanel Demo GM：增加主角装备 / 增加魔法书（D-061 / UI-019）
- [ ] 工具面板其余后续功能 / polish
- [x] 推图战（PushMap）框架：GameplayType、目标点/判定圈占领、空气墙、刷怪点/陷阱、BOSS 通关、AggroMode、复用 Defend 护盾/失控（§3.14）
- [x] Mode2 自动制造（AutoManufacture）规则关闭：流水线 Dig→AutoManufacture→UM；最低配方头+躯干+双臂（含主要手）+双腿；近似品质 |Δ|≤1；职业由双手 ClassRestrict；不计 Spirit/Control；无 SoulId；魔法书表+6槽+钩子骨架；清空布阵后按 PlacementOrder/职业区上阵（§3.15）
- [x] Mode2 制造记录弹窗（UI-015 / D-054）：最近一批只读摘要；布阵右侧入口；方案 A `AutoManufactureBatchRecordService`
- [x] Mode2 自动制造演出（UI-016 / D-055）：Step1–3；方案 A `AutoManufacturePresentationController` + UM 自动开布阵
- [x] Mode2 魔法书「还原」`RaceWeightPick`（种族定稿前探测）
- [x] Mode2 魔法书「战士强化」`StatMul`/`Primary`（D-058；可叠；Dig HUD GM 装备）
- [x] 士兵技能体系框架关闭：`SkillConfig` 为士兵技能权威表；职业 `DefaultSkillIds`；实例 `SoldierSkills`；Mode2 `SoldierSkillLevelAdd` 只升已有技能（§3.11 / §3.15 / SPEC_04 §9.21）
- [x] 士兵技能垂直 D-062（SS-01～04：表加载 + 池持久化 + Mode1/Mode2 授予 + `SoldierSkillLevelAdd` 二次扫描；Demo 不施放）
- [x] Mode2 魔法书职业进阶 D-063（`ForceClass` + `RequireClassId`/`Chance`；四本 `MagicBook_*Advance`）
- [ ] 全职业 DefaultSkillIds（Demo 样例仅战士=`Skill_01`，见 SS-01）；CastTarget 第七枚举名（样例书 `MagicBook_SoldierSkillLevel` 已由 SS-04 落地）
- [ ] `SoldierSkills` 与灵魂/宝石 `Skills` 同 Id 时的合并规则
- [ ] Mode2 魔法书正式装备 UI 与其余效果行；灵魂手动装配（§3.15 另专题）
- [x] 主角装备（ProtagonistEquipment）规则关闭：装备仓库 / 同 Id 转化 / EquipCommonExp / 等级 / Dig 与科技加法叠加（§3.16 / SPEC_04 §9.25）；获取来源与制造·战斗 Token **TBD**
- [x] 主角装备 Dig 垂直 D-059（PE-01～PE-04：表加载 + 仓/存档 + Dig caps 合并 + Dig HUD GM）
- [x] 主角装备矿灯 D-060（PE-05～PE-08：SPEC + 5 级表行 + 生成权重活叠加 + Dig HUD GM）
- [ ] 主角装备获取来源（Dig 掉落 / 商店等）与正式装备 UI（GM 手验属 D-059 / D-060）
- [ ] 主角装备 `SoldierManufacture` / `Combat` 效果 Token 登记表
- [x] PushMap 边界锁定：到达 `CaptureZone` 即占领（无计时/无「无怪」条件）；占领后已刷怪保留；全队共当前目标；无陷阱开战刷；无倒计时刷怪；仅 BOSS 通关入账经验；MapId=`Ground_*`|`PushMap_*`
- [x] 大规模战斗寻路（方案 B）规则锁定：FlowField（共享目标）+ AttackSlot（追击/攻击）+ LocalDetour（友军左右绕）；容量双方约 200；实现见 `.scratch/mass-pathing/issues/`（§3.12 / SPEC_04 §9.7）
- [ ] 推图战副本玩法正文
- [ ] 陷阱刷怪是否允许配置「可重复触发」
- [x] 大规模寻路实现切片（MP-01～MP-07）与 200v200 压测验收（入口已落地；约定机型 Stopwatch 数字见 `.scratch/mass-pathing/issues/07-perf-stress-200.md`）

### English

- [ ] Manual shell `GameplayState` switch triggers
- [ ] Level scene binding and real Level entry path
- [x] Dig obstacle types/geometry and placeable checks (uncleared Graves only; circle radius on Prefabs; §3.10)
- [x] Player dig interaction, per-grave rewards, and inventory credit (§3.10; Warehouse / SpiritEssence)
- [x] Dig stage end & settlement: no win/lose; effective duration = config base + tech duration bonus; DigStageSummary aggregate only, no extra grants (§3.10 / UI-011)
- [ ] VictorySettlement UI / fields
- [x] GraveQualityConfig fields and `LootDrop` encoding (SPEC_04 §9.3: `DropMode` + `Id;Weight;Count`; MaxHP concrete values still TBD)
- [x] Zero-weight drop and Dig empty effective weight list → abandon that spawn (SPEC_04 §9 common rules / §3.10)
- [ ] GraveQualityConfig MaxHP concrete values
- [x] Four Dig tech-bound capability formulas (damage / dig speed / cursor radius / diggable types; §3.10 `DigProtagonistCapabilities`)
- [x] TechTree framework: center-out / InitiallyUnlocked default learn / prereqs & LearnCost / canvas UI (§3.13; UI-012)
- [x] `TechTreeConfig` + `TechEffectConfig` schemas (SPEC_04 §9.16–§9.17)
- [ ] Concrete tech-node values, icons, and full feature-system enum
- [ ] Dig frame-anim count and asset naming list
- [x] UpgradeManufacture framework closed (§3.11)
- [x] UpgradeManufacture: player confirm end; **no** independent stage settlement
- [x] UI-010 full-screen manufacture + upgrade Modal + Complete/Formation; square drag inventory + visual appearance gate (§3.11)
- [x] BattleFormation: shared FormationEditor; continuous coords; soldier-bar drag; no manufacture in Prepare
- [x] Exp: Defend victory → `LifetimeExperience`; level-up does not deduct cumulative Exp; TechTree spend in §3.13
- [x] Warrior = instance; **manufacture flow/slots/min requirements/Spirit gate/naming closed** (§3.11); **soldier-skill grant framework closed** (`DefaultSkillIds` → `SoldierSkills` Lv1; Mode1 ignores MagicBook skill level-up; dropped on PermanentDeath)
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
- [x] Upgrade Modal (GM Upgrade / X) + ManufactureZone layout (square drag, slot ring, visual appearance gate); upgrade numeric polish still iterable
- [x] Defend framework (§3.12)
- [x] Defend wave spawn table, countdown activation, appear location/mode (§3.12 / SPEC_04 §9.18); **Demo-min spawn/NavMesh closed**; exact OutsideMap geometry deferred
- [x] MonsterConfig + TargetSelect (§3.12 / SPEC_04 §9.19); monster vs soldier: `AttackPower` subtracts HP directly (no armor this batch); AttackRange hit columns locked
- [x] Soldier combat derives: ClassId→ClassConfig.PrimaryStat / NormalAttackPower / AttackSpeed / SkillCooldown / MaxHP=ceil(BodyLife+Str×MaxHpStrengthMult); CombatConvertCoeffs + CombatConstantConfig locked (§3.11 / §3.12 / SPEC_04 §9.9b / §9.20b)
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
- [x] ToolsPanel Demo GM: Grant Protagonist Equipment / Grant MagicBook (D-061 / UI-019)
- [ ] Remaining ToolsPanel entries / polish
- [x] PushMap framework: GameplayType, objectives/CaptureZone, AirWall, SpawnPoint/Trap, Boss clear, AggroMode, reuse Defend Shield/LOC (§3.14)
- [x] Mode2 AutoManufacture rules closed: Dig→AutoManufacture→UM; min recipe Head+Torso+2Arm(incl PrimaryHand)+2Leg; approx |Δ|≤1; class from hand ClassRestrict; no Spirit/Control; no SoulId; MagicBook schema+6 slots+hook stub; clear formation then PlacementOrder/class-zone deploy (§3.15)
- [x] Mode2 ManufactureRecord popup (UI-015 / D-054): last-batch read-only summary; entry right of Formation; Approach A `AutoManufactureBatchRecordService`
- [x] Mode2 AutoManufacture presentation (UI-016 / D-055): Step1–3; Approach A `AutoManufacturePresentationController` + UM auto-open Formation
- [x] Mode2 MagicBook Restore `RaceWeightPick` (probe before race finalize)
- [x] Mode2 MagicBook Warrior Enhance `StatMul`/`Primary` (D-058; stackable; Dig HUD GM equip)
- [x] Soldier-skill framework closed: `SkillConfig` is soldier-skill authority; class `DefaultSkillIds`; instance `SoldierSkills`; Mode2 `SoldierSkillLevelAdd` only raises existing skills (§3.11 / §3.15 / SPEC_04 §9.21)
- [x] Soldier-skill vertical D-062 (SS-01–04: table load + pool persist + Mode1/Mode2 grant + `SoldierSkillLevelAdd` second pass; Demo no cast)
- [x] Mode2 MagicBook class advance D-063 (`ForceClass` + `RequireClassId`/`Chance`; four `MagicBook_*Advance`)
- [ ] Full-class DefaultSkillIds (Demo sample Warrior=`Skill_01` only, SS-01); 7th CastTarget enum name (sample book `MagicBook_SoldierSkillLevel` landed in SS-04)
- [ ] Same-Id merge between `SoldierSkills` and Soul/Gem `Skills`
- [ ] Mode2 MagicBook formal equip UI + remaining effects; manual soul attach (§3.15 later)
- [x] ProtagonistEquipment rules closed: equipment warehouse / same-Id convert / EquipCommonExp / levels / Dig additive with tech (§3.16 / SPEC_04 §9.25); acquire sources and Manufacture/Combat tokens **TBD**
- [x] ProtagonistEquipment Dig vertical D-059 (PE-01–PE-04: table load + warehouse/persist + Dig caps merge + Dig HUD GM)
- [x] ProtagonistEquipment Miner Lamp D-060 (PE-05–PE-08: SPEC + 5-level rows + live spawn-weight overlay + Dig HUD GM)
- [ ] ProtagonistEquipment acquire sources (Dig loot / shop) and formal equip UI (GM handcheck is D-059 / D-060)
- [ ] ProtagonistEquipment `SoldierManufacture` / `Combat` effect Token registry
- [x] PushMap boundary locks: Capture on arrive to `CaptureZone` (no timer / no “clear monsters” condition); keep living after Capture; shared current objective; non-trap spawn at StartBattle; no countdown spawn; Exp only on Boss clear; MapId=`Ground_*`|`PushMap_*`
- [x] Mass combat pathing (Approach B) rules locked: FlowField (shared goals) + AttackSlot (chase/attack) + LocalDetour (friendly L/R); ~200/side capacity; impl `.scratch/mass-pathing/issues/` (§3.12 / SPEC_04 §9.7)
- [ ] PushMap dungeon gameplay body
- [ ] Whether trap spawns may be configured as re-triggerable
- [x] Mass pathing implementation slices (MP-01–MP-07) and 200v200 stress acceptance (entry shipped; agreed-machine Stopwatch numbers in `.scratch/mass-pathing/issues/07-perf-stress-200.md`)

---

## 维护说明

### 简体中文

- 新模块从下一个可用 `## 3.x` 节起写；大节变更记入 SPEC_00 Changelog。
- 中英文双块同步；未决标 `TBD` / `未定义`。

### English

- Add new modules as the next `## 3.x` section; log major changes in SPEC_00 Changelog.
- Keep bilingual blocks in sync; mark open items `TBD` / `Undefined`.
