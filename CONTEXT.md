# Gravedigger2026 — 领域术语表 / Domain Glossary

**本文件是术语索引，不是规则权威。** 规则以 [SPEC_03_GameRules.md](SPEC_03_GameRules.md) 为准；技术约定以 [SPEC_04_Technical.md](SPEC_04_Technical.md) 为准。

| 术语 (EN) | 中文 | 定义摘要 | SPEC |
|-----------|------|----------|------|
| Gravedigger2026 | 本项目 | Unity 工程与工作区名称 | [SPEC_02](SPEC_02_GameOverview.md) |
| GameplayState | 玩法状态 | Dig / AutoManufacture / UpgradeManufacture / Defend / PushMap / **Shop**（Mode2 商店阶段）；关卡内由阶段玩法类型驱动 | [§3.1](SPEC_03_GameRules.md)、[§3.7](SPEC_03_GameRules.md)、[§3.9](SPEC_03_GameRules.md) |
| SaveSlot | 存档槽 | 固定 3 槽本地存档位；占用旗共享；士兵池/布阵/副本解锁等按槽 **且按 CampaignMode** PlayerPrefs | [§3.4](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |
| CampaignMode | 玩法模式 | 存档进出门闩：`Mode1` / `Mode2`；同槽进度隔离；Mode2 读独立配置根；**勿与** BattleMode（保卫/推图）混淆；Mode1 手动制造 §3.11，Mode2 自动制造 §3.15 | [§3.1](SPEC_03_GameRules.md)、[§3.4](SPEC_03_GameRules.md)、[§3.15](SPEC_03_GameRules.md) |
| AutoManufacture | 自动制造 | Mode2 阶段：Dig 后自动选料造兵→临时仓库→清空布阵按职业区上阵→再进 UM | [§3.15](SPEC_03_GameRules.md) |
| TempWarriorWarehouse | 临时仓库 | AutoManufacture 批内缓冲；造完后入 WarriorPool | [§3.15](SPEC_03_GameRules.md) |
| PrimaryHand | 主要手 | `IsPrimaryHand=1` 的 Arm；Mode2 选料锚点 | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.12](SPEC_04_Technical.md) |
| SecondaryHand | 次要手 | `IsPrimaryHand=0` 的 Arm；与主要手定职业 | [§3.15](SPEC_03_GameRules.md) |
| ClassRestrict | 职业限定 | BodyPart 多 ClassId；Mode2 双手交集定职业 | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.12](SPEC_04_Technical.md) |
| BodyPrimaryStat | 躯体主属性 | BodyPart 上 Strength/Agility/Intelligence 恰一；Mode2 选料匹配（≠职业 PrimaryStat） | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.12](SPEC_04_Technical.md) |
| ApproxBodyLevel | 近似品质 | `|ΔBodyLevel|≤1`；更高→相同→低1 | [§3.15](SPEC_03_GameRules.md) |
| PlacementOrder | 放置排序 | ClassConfig；自动上阵职业先后 | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md) |
| ClassLevel | 职业等级 | ClassConfig 展示用品质等级；UI-016 士兵卡 `Lv.N`；不进战斗公式 | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md) |
| FormationClassZone | 职业布阵区 | 地图 Prefab 按 ClassId 标定 **IsoDiamond**（HalfExtents = 顶点到中心；与 WalkSurface 同形；无 Y 旋转）；作者 MeshCollider；自动上阵螺旋落入 | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §13](SPEC_04_Technical.md) |
| IsoTileYaw | 等距砖轴偏航 | `Y=-atan2(cellSize.y,cellSize.x)`，使 Transform 本地 +X 对齐 Isometric Grid 在 XZ 上的砖轴（Demo ≈ -26.57°）；**职业布阵区已废止此偏航**（改无旋转 IsoDiamond，同 WalkSurface） | [SPEC_04 §13](SPEC_04_Technical.md) |
| MagicBook | 魔法书 | 主角特殊装备效果库；Mode2 在 UI-016 Step2 **单槽脉冲峰值**触发（一槽一书）；含「还原」`RaceWeightPick`、「战士强化」`StatMul`、「士兵技能升级」`SoldierSkillLevelAdd`、「职业进阶」`ForceClass`；命中时可烘进 `VisualStyle`（材质和/或放大）；**勿与** ProtagonistEquipment 混淆 | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.24](SPEC_04_Technical.md) |
| MagicBookConfig | 魔法书配置表 | MagicBookId→唯一/概率型/环节/EffectPayload/EffectParams/VisualStyleId/Priority/IntensityAdd/Icon/名/介绍 | [SPEC_04 §9.24](SPEC_04_Technical.md) |
| SpecialEquipSlot | 特殊装备槽 | 主角默认 6 槽装魔法书；下标 0→5=左→右；可 TrySwap（含空槽）；IsUnique 限制叠装；无独立仓库 | [§3.15](SPEC_03_GameRules.md) |
| EquipmentWarehousePanel | 装备仓弹窗 | InSaveShell 左下「装备」打开的只读 Modal；展示 OwnedEquip（名/等级/描述/图标）；不升级、不卸下（UI-022 / D-067） | [§3.6](SPEC_03_GameRules.md)、[§3.16](SPEC_03_GameRules.md) |
| MagicBookSlotsPanel | 魔法书槽弹窗 | InSaveShell 左下「魔法书」打开；共享 BookRow；拖拽 TrySwap 含空槽；点占用槽槽下「删除」须确认后 TryUnequip（UI-023 / D-068 / D-072） | [§3.6](SPEC_03_GameRules.md)、[§3.15](SPEC_03_GameRules.md) |
| BookRow | 魔法书槽行 | 6×BookSlot 共享 Prefab；AM 演出与魔法书弹窗嵌套同一份 | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |
| ProtagonistEquipment | 主角装备 | 成长型装备；仓内拥有即生效；同 Id 转化经验 / EquipCommonExp 升级；并行于 MagicBook / 材料仓 / ExtraEquipment；Dig 事件型样例 `Equip_Explosives`（D-077）、`Equip_Elctr`（D-078） | [§3.16](SPEC_03_GameRules.md)、[SPEC_04 §9.25](SPEC_04_Technical.md) |
| FormationBond | 阵容羁绊 | 上阵士兵属性统计激活的战斗增益；同 BondId 多等级互斥；Buff→SkillEffectConfig | [§3.17](SPEC_03_GameRules.md)、[SPEC_04 §9.26](SPEC_04_Technical.md) |
| BondBuff | 羁绊Buff | FormationBondConfig.BondBuff FK→SkillEffectConfig；本 Demo 片仅配置与 UI | [§3.17](SPEC_03_GameRules.md) |
| ProtagonistEquipmentWarehouse | 主角装备仓库 | 存档状态仓；不限种类；每 EquipId 至多 1 件 OwnedEquip | [§3.16](SPEC_03_GameRules.md) |
| ProtagonistEquipmentConfig | 主角装备配置表 | EquipId+EquipLevel → 名/图标/升下一级经验/转化经验/生效域/效果/描述 | [SPEC_04 §9.25](SPEC_04_Technical.md) |
| EquipCommonExp | 装备公共经验 | 独立池，专供主角装备升级；≠ LifetimeExperience | [§3.16](SPEC_03_GameRules.md) |
| OwnedEquip | 已拥有装备实例 | EquipId + Level + CurrentExp | [§3.16](SPEC_03_GameRules.md) |
| EquipEffectDomain | 装备生效功能 | Dig \| SoldierManufacture \| Combat（可多值） | [§3.16](SPEC_03_GameRules.md) |
| EffectPhase | 生效环节 | SoldierManufacture / Combat 等；Mode2 制造书在 Step2 单槽节拍 apply | [§3.15](SPEC_03_GameRules.md) |
| EffectPayload | 魔法书效果编码 | 已登记 PascalCase Token；空=无效果；未登记空 apply | [SPEC_04 §9.24](SPEC_04_Technical.md) |
| EffectParams | 魔法书效果参数 | `Key=Value` 或 `Key=Value\|…`；空=无参 | [SPEC_04 §9.24](SPEC_04_Technical.md) |
| ManufactureRecord | 制造记录 | Mode2 UM 只读弹窗：最近一批自动制造士兵摘要（名字/种族/职业）；布阵右侧入口（UI-015 / D-054） | [§3.15](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md) Mode2 差分 |
| AutoManufactureBatchRecord | 自动制造批次记录 | 存档级最近一批 WarriorId；下一批覆盖；PlayerPrefs 按槽+CampaignMode | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |
| AutoManufacturePresentation | 自动制造演出 | Mode2 AutoManufacture 阶段表现：Step1 士兵行+书槽 → Step2 加强动画 → Step3 进 UM 自动开布阵（UI-016 / D-055） | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) / §13 |
| ShopSystem | 商店系统 | Mode2 全屏商店：关卡 `GameplayType=Shop`（Stage1）与局外 InSaveShell 左下入口共用 Prefab `ShopStageRoot`；左侧可展示已拥有装备/魔法书 ICON 并出售换精魂（D-076） | [§3.5](SPEC_03_GameRules.md)、[§3.9](SPEC_03_GameRules.md)、[SPEC_04 §10](SPEC_04_Technical.md) |
| ShopSellService | 商店出售服务 | 商店 UI 出售已拥有装备（`TryRemove`）/魔法书（`TryUnequip`），按 `ItemCatalog.SellPrice` 入账精魂 | [§3.5](SPEC_03_GameRules.md)、[SPEC_04 §10](SPEC_04_Technical.md) |
| ShopProgress | 商店进度 | 存档商店快照：解锁关卡号、pending 开放、刷新次数、6 项 offers | [§3.5](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |
| CampaignModeSelect | 玩法模式选择 | 新建/进入存档前弹窗选 CampaignMode（UI-014 / D-045） | [§3.2](SPEC_03_GameRules.md)、[§3.6](SPEC_03_GameRules.md) |
| InSaveShell | 进档壳层 | 进档后常驻壳（玩法占位 + 工具 + 左下商店/装备/魔法书入口） | [§3.1](SPEC_03_GameRules.md)、[§3.3](SPEC_03_GameRules.md)、[§3.5](SPEC_03_GameRules.md) |
| ToolsPanel | 工具面板 | Demo 设置/调试壳；含设置、关卡（→LevelSelectPanel）及 GM「增加主角装备」（→GmGrantListPanel 嵌套选等级 / D-061）、「增加魔法书」（→GmGrantListPanel / D-061）、「添加士兵」（→GmAddSoldierPanel / D-064） | [§3.5](SPEC_03_GameRules.md) |
| PlayerPointer | 运行时光标 | 整段 Play 硬件鼠标外观（UI-024）；`Art/UI/Cursor.png`；勿与 Dig 圆圈混淆 | [§3.6](SPEC_03_GameRules.md)、[SPEC_04 §4](SPEC_04_Technical.md) |
| Level | 关卡 | 关卡运作表驱动的多阶段流程；UM 阶段 `GameplayConfigId` 忽略 | [§3.1](SPEC_03_GameRules.md)、[§3.9](SPEC_03_GameRules.md) |
| ConfigTables | 配置表根目录 | Mode1：`Assets/ConfigTables/{Excel,Csv}`；Mode2：`Assets/ConfigTables/Mode2/{Excel,Csv}` | [SPEC_04 §14](SPEC_04_Technical.md) |
| BakeTables | 打表 | Editor Excel→CSV；`Bake Tables`（Mode1）+ `Bake Mode2 Tables` | [SPEC_04 §14](SPEC_04_Technical.md) |
| CharacterArtPipeline | 角色美术管线 | Character Creator **烘焙整角**；游戏资源不得落在工具目录；导出补丁→`Art/Characters`→`Prefabs` | [SPEC_04 §15](SPEC_04_Technical.md) |
| BakedWholeCharacter | 烘焙整角 | 用 Creator 拼装后导出整角 spritesheet/Animator/Prefab；非运行时叠装 | [SPEC_04 §15](SPEC_04_Technical.md) |
| LevelOperation | 关卡运作 | 关卡 ID + 阶段编号 + 玩法类型 + 玩法配置 ID | [§3.9](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| DigGameplayConfig | 挖坟配置 | 基础时长、开局坟数、过程生成速率、品质权重（零权重剔除） | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| DigMap | 挖坟地图 | 菱形外观；逻辑为整体可放置空间（非格子）；表现 Prefab `Ground_01`…`Ground_05`（`DigMapId`） | [§3.10](SPEC_03_GameRules.md) |
| Grave | 坟墓 | 挖坟可生成实体；带品质 ID；未消除时为 DigObstacle | [§3.10](SPEC_03_GameRules.md) |
| Digger | 挖坟主角 | Dig 阶段不生成地图模型；HUD 左上 60×60 头像框；Prefab 可保留于 Catalog/Art | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §15](SPEC_04_Technical.md) |
| DigAction | 挖掘流程 | 0.2s 停留触发；`DigActionDuration` 帧动画后扣血；busy 不可重触 | [§3.10](SPEC_03_GameRules.md) |
| DigObstacle | 挖坟障碍物 | 仅未消除 Grave；圆形半径在 Prefab 上 | [§3.10](SPEC_03_GameRules.md) |
| DigHitShape | 挖坟命中形 | Grave Prefab 离线烘焙本地 XZ 凸包；光标圆相交触发挖掘；与障碍圆分离 | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9.2](SPEC_04_Technical.md) |
| DigProtagonistCapabilities | 挖坟主角能力 | 伤害/时长缩短和/光标半径/可挖品质/阶段时长加成/坟墓生成权重加成/过程生成数量加成；科技树 + 主角装备 Dig 效果按键加法重算 | [§3.10](SPEC_03_GameRules.md)、[§3.13](SPEC_03_GameRules.md)、[§3.16](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| GraveSpawnWeightBonus | 坟墓生成权重加成 | 按 QualityId 加法叠到 `GraveSpawnWeights`；表中缺席视为 0；键 `GraveSpawnWeightBonus_{QualityId}` | [§3.10](SPEC_03_GameRules.md)、[§3.16](SPEC_03_GameRules.md)、[SPEC_04 §9.6](SPEC_04_Technical.md) |
| GraveHP | 坟墓血量 | maxHP 来自品质表；归 0 触发成功与奖励 | [§3.10](SPEC_03_GameRules.md) |
| GraveIconStyle | 坟墓图标样式 | 按剩余 HP%：>65%/30–65%/<30% → 样式1/2/3 | [§3.10](SPEC_03_GameRules.md) |
| GraveQualityConfig | 坟墓品质定义表 | QualityId → MaxHP、DropMode、LootDrop、IconStyleHighId/MidId/LowId | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| DigReward | 挖掘奖励 | HP=0 时生成；飞向 Dig HUD 左上头像框，到达后入账并消失 | [§3.10](SPEC_03_GameRules.md) |
| DigStageSummary | 挖坟阶段汇总 | 时长归零后弹窗；仅汇总本阶段已获奖励；躯体行 DisplayName+BodyLevel；右上 X 确认 | [§3.10](SPEC_03_GameRules.md)、[§3.6](SPEC_03_GameRules.md) |
| Warehouse | 仓库 | 存档槽材料仓；不限格/时长；按类型堆叠上限 10000 | [§3.10](SPEC_03_GameRules.md) |
| SpiritEssence | 精魂 | 货币；LootDrop `Spirit` + AutoConvert；造士兵消耗 | [§3.10](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md) |
| MaterialConfig | 材料配置表 | MaterialId → AutoConvert、外观图、素材路径、仓库品质外轮廓 | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| CurrencyConfig | 货币配置表 | CurrencyId → 外观图、素材路径、仓库品质外轮廓；精魂=`Spirit` | [§3.10](SPEC_03_GameRules.md)、[SPEC_04 §9](SPEC_04_Technical.md) |
| UpgradeManufacture | 升级与制造 | 原 SewRevive；升级 + 造士兵 + 布阵；Mode2 关闭手动制造（§3.15） | [§3.11](SPEC_03_GameRules.md)、[§3.15](SPEC_03_GameRules.md) |
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
| WarriorAnimView | 士兵/怪物动画表现 | 表现层：驱动 Creator Animator（`IsRun`/`Attack1`/`Die`/`Taunt`/`DirIndex`+`Direction` 同值）；可选 `FacingYawFlip`；`SetMoving(true)` 仅当移动目标 XZ 距 >0.4 才强制 Attack→Run；士兵死亡：锁存 Die **最后非空**精灵（跳过末尾 null 关键帧）+ RGB×`CorpseDarkenMul` 变暗 + `sortingOrder`→100（低于存活单位 200）；士兵与怪物（Defend/PushMap）共用；UI-016 卡面揭示：Taunt 一遍后循环默认 Idle（Camera+RT） | [SPEC_04 §15.5](SPEC_04_Technical.md) |
| FacingYawFlip | 朝向整圈翻转 | 配表 0\|1；写入 Animator 前 `(DirIndex+4)%8`（180°）；士兵=`BodyAppearanceConfig`，怪=`MonsterConfig`；缺省 0 | [SPEC_04 §15.5](SPEC_04_Technical.md)/[§9.13](SPEC_04_Technical.md)/[§9.19](SPEC_04_Technical.md) |
| FacingHysteresis | 朝向迟滞 | 推图怪八向切换迟滞：候选扇区越过当前边界 +12° 且过最短保持 0.12s 才切换；仅 `PushMapMonsterAgentView` | [SPEC_04 §15.5](SPEC_04_Technical.md) |
| StuckHold | 受堵停滞 | 推图怪 steer 非零但 0.25s 滑窗 XZ 位移 < 0.05 → 停播 Run 面向追击目标；位移恢复或 steer 归零即退出 | [SPEC_04 §15.5](SPEC_04_Technical.md)、[§3.14](SPEC_03_GameRules.md) |
| WarriorInfo | 士兵信息 | 主标签=定稿种族；不改数值 | [§3.11](SPEC_03_GameRules.md) |
| WarriorName | 士兵名字 | Prefix(es)+RaceName+ClassName+Suffix | [§3.11](SPEC_03_GameRules.md) |
| ManufactureSlot | 制造槽位 | 头1/躯干1/臂2/腿2/灵魂1/宝石6/坐骑1/翅膀1 | [§3.11](SPEC_03_GameRules.md) |
| Remanufacture | 再造 | 按士兵实例配方快照后台再走制造流水线，成功则新增池内士兵；不足弹 Tips | [§3.11](SPEC_03_GameRules.md) |
| BodyPart | 躯体部位 | Head/Torso/Arm/Leg 材料；BodyPartConfig（BodyLevel/StatBonus/RaceId/SpiritCost/AutoConvert 等） | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.12](SPEC_04_Technical.md) |
| BodyPartConfig | 躯体材料配置表 | BodyPartId → DisplayName（道具名称）/等级/部位/种族/控制力/精魂/StatBonus/AutoConvert/介绍/美术/IsPrimaryHand/ClassRestrict/BodyPrimaryStat | [SPEC_04 §9.12](SPEC_04_Technical.md) |
| BodySlot | 躯体槽类型 | Head / Torso / Arm / Leg | [§3.11](SPEC_03_GameRules.md) |
| BodyLevel | 躯体等级 | 躯体材料字段；平均后定外观等级 | [§3.11](SPEC_03_GameRules.md) |
| StatBonus | 增加的属性值 | 躯体平坦加成；Base(S)=Σ StatBonus(S) | [§3.11](SPEC_03_GameRules.md) |
| Body | 躯体 | 部位集合；Base(S)=Σ StatBonus；默认同族否则亡灵定种族 | [§3.11](SPEC_03_GameRules.md) |
| BaseStats | 基础属性 | HP/移速/力量/敏捷/智力；Σ StatBonus；经 StaticStat/FinalStat 派生攻/速/CD/血 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| StaticStat | 静态属性 | 制造/布阵：不含 SkillBuff 的终值 | [§3.11](SPEC_03_GameRules.md) |
| PrimaryStat | 主属性 | 职业字段 Strength/Agility/Intelligence；定普攻属性维 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md) |
| BodyLife | 躯体生命 | Base(MaxHP)+Equip(MaxHP)；代入 MaxHP=ceil(BodyLife+Str×MaxHpStrengthMult) | [§3.11](SPEC_03_GameRules.md) |
| NormalAttackPower | 普通攻击值 | Primary×NormalAttackPrimaryMult（职业覆盖，否则 CombatConstantConfig；样例 15） | [§3.12](SPEC_03_GameRules.md) |
| CombatConstantConfig | 战斗常量表 | 全局战斗公式默认键值；CombatConvertCoeffs 缺键回退；含 MaxHpStrengthMult | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.20b](SPEC_04_Technical.md) |
| MaxHpStrengthMult | 血量力量系数 | 常量表键；MaxHP=ceil(BodyLife+Str×本值)；样例 3 | [§3.11](SPEC_03_GameRules.md) |
| AttackSpeed | 攻击速度 | 次/秒：0.5+60/max(Agi,1)（过渡） | [§3.12](SPEC_03_GameRules.md) |
| BodyAppearance | 躯体外观 | 预设整体造型；按平均等级+种族+职业选取；烘焙整角 Prefab | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.13](SPEC_04_Technical.md)、[§15](SPEC_04_Technical.md) |
| VisualStyle | 特效外观 | Mode2 书命中后烘进：AllIn1 材质通道（优先级赢家）+ 放大通道 `Style_ScaleModel`（`VisualModelScale` 连乘 k，可与材质共存）；世界 Instantiate 换材质/改 Visual Scale，BodyRadius 与 AttackRange ×k | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.24](SPEC_04_Technical.md)/[§15.2](SPEC_04_Technical.md) |
| VisualModelScale | 模型缩放系数 | 放大通道连乘系数 k（缺省 1）；`VisualIntensityAdd` 即该书 k | [§3.15](SPEC_03_GameRules.md) 6b、[SPEC_04 §9.9](SPEC_04_Technical.md) |
| BodyAppearanceConfig | 躯体外观配置表 | AppearanceId → Prefab 逻辑名（`Prefabs/Defend/Warriors/{Id}`）/等级/种族/职业倾向/保底/`BodyRadius`（士兵占地；缺省 0.1）/`FacingYawFlip` | [SPEC_04 §9.13](SPEC_04_Technical.md)、[§15](SPEC_04_Technical.md) |
| IsFallback | 保底外形 | 外观表字段；1=种族保底；每种族至多一行；职业不匹配时走保底；A 空先改亡灵 | [§3.11](SPEC_03_GameRules.md) |
| DefaultAppearanceId | 职业默认外形 | Mode2 ClassConfig 字段；B 空或亡灵改写后 A 仍空时优先于 IsFallback | [§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md) |
| DefaultSkillIds | 制造默认获得技能ID | ClassConfig 列；空=无；否则 SkillId 或 `SkillId\|…`；ClassId 定稿后写入 SoldierSkills Lv1 | [§3.11](SPEC_03_GameRules.md)、[§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md) |
| Race | 种族 | 默认同族否则 Race_Undead；Mode2「还原」→权重1加权随机；五维 RaceAdjustCoeff；主标签 | [§3.11](SPEC_03_GameRules.md)、[§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.11](SPEC_04_Technical.md) |
| RaceConfig | 种族配置表 | RaceId → 展示名、五维调整系数、失控概率加成 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.11](SPEC_04_Technical.md) |
| RaceAdjustCoeff | 种族属性调整系数 | Base(S)×系数；缺省维=0；可正负；不计控制力 | [§3.11](SPEC_03_GameRules.md) |
| Soul | 灵魂 | 槽位可选；有灵魂消耗该行；无灵魂→Soul_00 + 强制 Class_Servants；AttackMode/技能等；不改写三维；Demo 不施放技能 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md) |
| SoulConfig | 灵魂配置表 | SoulId → ClassId、AttackMode、Skills（`SkillId;Level|…`）、AttackPriority（同 TargetSelect）、MoveStyle、SpiritCost、ControlPowerCost；含系统默认 `Soul_00` | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md) |
| Class | 职业 | 实例 ClassId（有灵魂取自灵魂；无灵魂 Class_Servants）；ClassName/PrimaryStat/五维→战斗参数换算系数 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md) |
| ClassId | 职业ID | 职业主键；有灵魂取自灵魂；无灵魂强制 Class_Servants；写入士兵实例 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md) |
| ClassConfig | 职业配置表 | ClassId → ClassName、ClassLevel、BaseClass（基础职业，预留）、PromoteClass（转职职业，可选文字，预留）、PrimaryStat、CombatConvertCoeffs、BaseMoveSpeed（基础移速）、AttackRange/前摇/弹速/超时、ChaseMoveSpeedMult、AttackMode、PlacementOrder、DefaultAppearanceId、DefaultSkillIds | [SPEC_04 §9.9b](SPEC_04_Technical.md) |
| BaseMoveSpeed | 基础移速 | ClassConfig 列；士兵 MoveSpeed 维 Base；缺/≤0 → 3.5 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md) |
| ClassName | 职业名 | 职业表字段；参与 WarriorName 与外观 ClassAffinity；可为「战士」等，**不是**单位称谓「士兵」 | [§3.11](SPEC_03_GameRules.md) |
| BaseClass | 基础职业 | ClassConfig 列；CSV 中文战士/射手/法师/刺客（加载器仍接受旧值盗贼）；空或非法→Unspecified；预留魔法书等条件；不参与命名/外观/战斗；不烘进实例 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md) |
| PromoteClass | 转职职业 | ClassConfig 可选文字列；空=无；本轮仅填表/加载；不参与命名/外观/战斗；应用点 TBD | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md) |
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
| SoldierSkill | 士兵技能 | 绑定士兵实例；职业默认授予；Mode2 魔法书可改等级；无经验升级；PermanentDeath 删除 | [§3.11](SPEC_03_GameRules.md)、[§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.21](SPEC_04_Technical.md) |
| SoldierSkills | 士兵技能列表 | 实例 `{SkillId,SkillLevel}[]`；制造烘进；CombatDead 保留 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md) |
| SkillCast | 士兵技能施放 | Combat 内按 SoldierSkills+SkillConfig 自动施放；PushMap `Skill_03` 占用普攻通道 3×方案 D；`Skill_01` 独立被动格挡钩子；`Skill_02` 满血 Outgoing 倍率；`Skill_04`～`Skill_12` 走 EffectKind 管线（D-073）；Mode2 提交后进 CD（仅 BaseCD>0） | [§3.12](SPEC_03_GameRules.md) SkillCast、[SPEC_04 §9.21](SPEC_04_Technical.md) |
| CombatSkillIcon | 战斗技能图标 | PushMap 头顶 35×35 静止 0.6s 后 +Z 上飘 0.3s；持续脚下 20×20；`Skill_03`/`Skill_01` 头顶；`Skill_02` 满血脚下+生效头顶飘；D-071 / UI-025；D-073 Handler 复用同一对事件 | [§3.12](SPEC_03_GameRules.md) SkillCast、[SPEC_04 §9.22](SPEC_04_Technical.md) |
| EffectKind | 技能效果种类 | `SkillEffectConfig` 登记制 PascalCase Token（对齐 MagicBook `EffectPayload`）；空=未实现；Session 禁止按 SkillId 硬分支 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.21b](SPEC_04_Technical.md) |
| CombatStatusService | 战斗状态服务 | 无敌 / 击晕 / 减速 / 灼烧 DoT 的统一 Tick + 查询；士兵与怪物分 bucket | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |
| SkillConfig | 技能配置表 | 士兵技能权威表；复合主键 (SkillId,SkillLevel)；名/图标/描述/SkillEffectId/`EffectImplemented`(UI-021 绿/红)/CD/失控加成；PushMap `Skill_03`/`Skill_01`/`Skill_02` 见 D-069；`Skill_04`～`Skill_12` 见 D-073 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.21](SPEC_04_Technical.md) |
| SkillEffectConfig | 技能效果配置表 | SkillEffectId 主键；`EffectKind`/`EffectParams`/`TriggerHook` 登记制（D-073）；`Skill_01`～`Skill_03` 仍 D-069 硬映射（Kind 可空） | [SPEC_04 §9.21b](SPEC_04_Technical.md) |
| ControlPower | 控制力 | 上阵占用；本版上限=等级行 ControlPowerCap；超额失控 | [§3.11](SPEC_03_GameRules.md) |
| LossOfControl | 失控 | Degree 分档；开战锁定；各士兵独立 roll；成功→叛变 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| LossOfControlDegree | 失控程度 | ΣCost/Cap−1；≤0 未失控；开战锁定 | [§3.11](SPEC_03_GameRules.md) |
| LossOfControlTier | 失控程度段 | 1~4（轻度/中度/重度/完全） | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §9.20](SPEC_04_Technical.md) |
| LossOfControlConfig | 失控配置表 | TierId→名称/描述/基础失控概率 | [SPEC_04 §9.20](SPEC_04_Technical.md) |
| Rebel | 叛变 | 失控成功状态；就近打主角/士兵/敌人；至死亡 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| BattleFormation | 战斗布阵 | 连续坐标；共享 `FormationEditor`；与士兵池同槽 PlayerPrefs 持久化 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[§3.14](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |
| WarriorPool | 士兵可上阵池 | 存档级已造士兵实例集合；制造入池；布阵/Defend/PushMap 共用；按槽持久化 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |
| FormationEditor | 布阵编辑器 | Prefab `FormationEditorRoot`；底栏士兵格（上阵保留+变亮）+ Idle 跟手拖放；UM 返回 / Defend 开战 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| SoldierHoverTooltip | 士兵栏悬浮框 | Mode2 布阵：指针停在有兵 `SoldierSlot` 上展示职业/种族/静态属性/技能（UI-021 / D-065）；图标 `Resources/UI/Skills/{SkillId}`；`Icon` 右上角 5×5 按 `EffectImplemented` 绿/红（D-070）；Mode1 无 | [§3.11](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |
| Defend | 防守 / 保卫战 | Prepare→开战→Combat；亦可作战斗模式1；见专节 | [§3.12](SPEC_03_GameRules.md) |
| BattleMode | 战斗模式 | Defend（保卫战）/ PushMap（推图战；规则 §3.14） | [§3.12](SPEC_03_GameRules.md)、[§3.14](SPEC_03_GameRules.md) |
| BattleModeSelect | 战斗模式选关 | 进入 Defend 后选模式+关卡（UI-013 / D-044；模式2确认→§3.14） | [§3.12](SPEC_03_GameRules.md)、[§3.6](SPEC_03_GameRules.md) |
| PushMap | 推图战 | GameplayType/GameplayState；亦可作战斗模式2；目标点占领+刷怪/陷阱/BOSS；复用 Defend 布阵/护盾/失控 | [§3.14](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| PushMapPhase | 推图战子状态 | Prepare / Combat / Ended | [§3.14](SPEC_03_GameRules.md) |
| MapId | 地图编号 | PushMap 地图 Prefab 逻辑名（≠ LevelId）；`Ground_*` 或 `PushMap_*`（Demo：`PushMap_Demo_01`–`03`）→ `Prefabs/Maps/`；运行时经 `DefendPrefabCatalog.Maps` 绑定 | [§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.22](SPEC_04_Technical.md) |
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
| CameraFollowMode | 镜头跟随模式 | PushMap Combat：`Intro`（开战扫镜）/ `Auto`（`CameraFollowPath` 最大投影）/ `Manual` | [§3.14](SPEC_03_GameRules.md) |
| CameraFollowPath | 镜头跟随轨 | 地图 Prefab 虚拟推进折线；作者路点 + 相邻点世界 XZ 直线烘焙；镜头对准折线点 | [§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.22](SPEC_04_Technical.md) |
| IsCombatIntroActive | 开战镜头预览门闩 | PushMap Combat 内标志：单位已部署 Idle、计时冻结、镜头 Intro；结束后正常玩法 | [§3.14](SPEC_03_GameRules.md) |
| CameraPathProgress | 镜头轨进度 | 折线弧长 `s∈[0,1]`；Auto=存活忠诚兵投影最大值；领头失效回退 | [§3.14](SPEC_03_GameRules.md) |
| ResumeFollow | 恢复跟随 | 手动模式底中按钮 → 回 Auto | [§3.14](SPEC_03_GameRules.md) |
| FollowDeadzone | 跟随死区 | Auto 世界 XZ 半径 0.15；圈内忽略目标小幅位移 | [§3.14](SPEC_03_GameRules.md) |
| FollowSmoothTime | 跟随缓动时间 | Auto 超出死区后 XZ SmoothDamp 时间 0.25s | [§3.14](SPEC_03_GameRules.md) |
| DamagePopup | 伤害飘字 | PushMap 命中后头顶 `-受伤值`（`RoundToInt` 实际伤害，无下限 1）；怪红/兵白字号 12；0.5s Z +0→+0.5 后销毁 | [§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.22](SPEC_04_Technical.md) |
| HitFlash | 受伤闪烁 | PushMap 命中后模型亮色；怪红/兵白；2×0.1s 紧接不灭；重伤刷新 | [§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.22](SPEC_04_Technical.md) |
| AllyFootCircle | 友军脚下圈 | Defend/PushMap Combat 忠诚存活士兵脚下绿描边圆 + 内黑 α160/255；半径=`BodyRadius`；localPos Y=-0.05 Z=-0.2；rotation X=-30；跟随；叛变/死亡隐藏；Order In Layer=`1` | [§3.12](SPEC_03_GameRules.md)/[§3.14](SPEC_03_GameRules.md)、[SPEC_04 §9.7](SPEC_04_Technical.md) |
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
| IsoDiamond | 地图菱形足迹 | XZ 曼哈顿菱形（`|dx|/hx+|dz|/hz≤1`）；半尺寸 = `PaintRadius*(cellSize.x,cellSize.y)`，可随 iso 高宽比各向异性（Demo `(5,2.5)`）；`DigMapBounds`/`EngageZone`/`WalkSurface`/`FormationClassZone`/NavMesh 共用（职业区半尺寸更小，Sanitize 下限 0.05） | [§3.10](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[§3.15](SPEC_03_GameRules.md)、[SPEC_04 §9.7](SPEC_04_Technical.md)/[§13](SPEC_04_Technical.md) |
| AttackMode | 攻击模式 | Melee/Ranged；士兵 SoulConfig / 怪物 MonsterConfig；普攻命中分支；**异于** AggroMode | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.9](SPEC_04_Technical.md)、[§9.19](SPEC_04_Technical.md) |
| AttackRange | 攻击距离 | 士兵 ClassConfig / 怪物 MonsterConfig；进入距内才攻击 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.9b](SPEC_04_Technical.md)、[§9.19](SPEC_04_Technical.md) |
| CombatDead | 战斗死亡 | 无宝石士兵 HP≤0；可复活；不触发物资去向 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| PermanentDeath | 彻底死亡 | 物资去向+清实例/布阵位；Ended/LevelFailure 结算或宝石特例立即 | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| AttackWindup | 攻击前摇 | 近战命中确认前计时 | [§3.12](SPEC_03_GameRules.md) |
| HitConfirm | 命中确认 | 规则层结算伤害时刻（近战前摇结束 / 远程弹道命中） | [§3.12](SPEC_03_GameRules.md) |
| BattleProtagonist | 战斗主角 | 地图中央；异于 Digger；Defend 用护盾承受普通攻击；烘焙整角 Prefab | [§3.12](SPEC_03_GameRules.md)、[§3.11](SPEC_03_GameRules.md)、[SPEC_04 §15](SPEC_04_Technical.md) |
| Shield | 护盾 | 普通攻击承受次数（敌人或叛变士兵）；开战 = ProtagonistMaxHP；归零 LevelFailure | [§3.12](SPEC_03_GameRules.md) |
| Monster | 怪物 | 防守敌方；InsideMap/OutsideMap；ModelId 烘焙整角 Prefab | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.19](SPEC_04_Technical.md)、[§15](SPEC_04_Technical.md) |
| MonsterConfig | 怪物配置表 | MonsterId → ModelId/目标选择/AttackMode/MonsterType/AggroMode/AlertRadius/BodyRadius/FacingYawFlip/血量/移速/攻力/攻速/AttackRange 等/技能/掉落；Demo 技能不生效 | [SPEC_04 §9.19](SPEC_04_Technical.md)、[§3.14](SPEC_03_GameRules.md) |
| MonsterType | 怪物类型 | `1`=普通 / `2`=精英 / `3`=BOSS；MonsterConfig 原型标签；异于 PushMapSpawnConfig.IsBoss；本批不驱动技能 | [SPEC_04 §9.19](SPEC_04_Technical.md)、[SPEC_03](SPEC_03_GameRules.md) |
| Wave | 波次 | WaveConfigId 下刷怪行集合；全触发且全灭为胜利条件之一 | [§3.12](SPEC_03_GameRules.md) |
| WaveSpawnConfig | 刷怪波次配置表 | WaveConfigId + 顺序/剩余秒/怪物/数量/位置/方式 | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.18](SPEC_04_Technical.md) |
| WaveConfigId | 波次配置ID | DefendGameplayConfig → WaveSpawnConfig 分组键 | [SPEC_04 §9.7](SPEC_04_Technical.md) |
| RemainingCombatSeconds | 战斗剩余秒 | 开战倒计时剩余整秒；等于 SpawnRemainingSeconds 时刷怪 | [§3.12](SPEC_03_GameRules.md) |
| TargetSelect | 目标选择 | Nearest / PreferWarrior / PreferProtagonist | [§3.12](SPEC_03_GameRules.md)、[SPEC_04 §9.19](SPEC_04_Technical.md) |
| AttackPriority | 攻击优先级 | 灵魂字段；与 TargetSelect 同枚举；本批不驱动选目标（默认 EngageZone 内最近） | [§3.11](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md) |
| TargetRetargetInterval | 目标修正间隔 | 怪物与士兵重算目的地间隔；暂定 1s | [§3.12](SPEC_03_GameRules.md) |
| LevelFailure | 关卡失败 | 护盾归零等（Defend/PushMap）；PushMap 另含无忠诚存活；与 VictorySettlement 互斥；无本阶段经验/无关卡结算奖励；已获不扣；PushMap Demo 经 UI-017 → LevelSelect | [§3.9](SPEC_03_GameRules.md)、[§3.12](SPEC_03_GameRules.md)、[§3.14](SPEC_03_GameRules.md) |
| PushMapBattleSettlement | 推图战斗结算 | 胜负均弹（UI-017）：胜利/失败 + 耗时 + 击杀数；Continue 路由选关或奖励弹窗 | [§3.14](SPEC_03_GameRules.md)、[§3.6](SPEC_03_GameRules.md) |
| PushMapRewardPopup | 推图奖励弹窗 | UI-018：展示本场已入账 Exp+CaptureLoot；Continue → LevelSelect | [§3.14](SPEC_03_GameRules.md)、[§3.6](SPEC_03_GameRules.md) |
| VictorySettlement | 胜利结算 | 最后一阶段结束后的关卡级结算 | [§3.9](SPEC_03_GameRules.md) |
| Demo acceptance (D-xxx) | Demo 验收项 | D-001～D-004 Meta 壳；D-010～D-044 Dig→UM→Defend（含 ModeSelect）流水线垂直切片 | [§3.8](SPEC_03_GameRules.md)、[SPEC_04 §6](SPEC_04_Technical.md) |

## 维护规则

- 新增术语时同步一行；正文写在 SPEC
- 禁止在本文件写完整规则或数值表
- ADR 仅记录架构决策（`docs/adr/`）
