# SPEC_03 — 游戏规则 / Game Rules（Gravedigger2026）

**关联文档 / Related:** [SPEC_00_Index.md](SPEC_00_Index.md) · [SPEC_02_GameOverview.md](SPEC_02_GameOverview.md) · [SPEC_04_Technical.md](SPEC_04_Technical.md)

> Demo 验收已扩大为「Meta 壳 + 一条关卡流水线垂直切片」（§3.8）；关卡阶段 / 挖坟 / 升级与制造 / 防守 / 科技树 / 推图战 / 自动制造 / 主角装备 / 战术阵型 / **搜打撤** / **沙盘** / **COC 战斗** 规则见 §3.9–§3.21。Unity 编码须负责人明确授权 Demo 开发。

---

## 3.1 术语与实体

### 简体中文

| 术语 (EN) | 中文 | 定义 |
|-----------|------|------|
| GameplayState | 玩法状态 | 局内主状态枚举：`Shop`（商店；Mode2 关卡第一阶段）、`Dig`（挖坟）、`AutoManufacture`（自动制造；Mode2 流水线）、`UpgradeManufacture`（升级与制造；原占位名 `SewRevive`）、`Defend`（防守）、`PushMap`（推图战）、`SearchExtract`（搜打撤；Mode2 子关卡玩法，§3.19）、`CocCombat`（COC 战斗；沙盘进入，§3.21）。关卡运行时由当前阶段的玩法类型决定（§3.9）；壳层默认占位仍为 Dig。 |
| SaveSlot | 存档槽 | 固定数量的本地存档位；本版 **3 槽**（索引 0–2）。空槽可新建，占用槽可进入或删除。占用旗按槽共享；士兵池/布阵/副本解锁等进度按槽 **且按 `CampaignMode`** 隔离（§3.4）。 |
| CampaignMode | 玩法模式 | 存档级玩法门闩：`Mode1` / `Mode2`。**本 Demo 进档路径：** 新建/进入**跳过** `CampaignModeSelect`，一律 `Mode2`（UI-014 组件保留，Mode1 入口后置）。同槽两模式进度完全隔离；Mode2 使用独立配置表根（[SPEC_04 §14](SPEC_04_Technical.md)）。Mode2 与 Mode1 共用战斗与挖坟机制；**士兵制造**：Mode1 手动（§3.11），Mode2 自动制造（§3.15）。**勿与** `BattleMode`（保卫战/推图战）混淆。 |
| AutoManufacture | 自动制造 | Mode2 关卡玩法类型 / `GameplayState`：DigStageSummary 确认后进入；按规则自动选料→造兵→临时仓库→清空布阵后按职业区上阵；结束后进 `UpgradeManufacture`（§3.15）。 |
| TempWarriorWarehouse | 临时仓库 | AutoManufacture 阶段批内缓冲：造好的士兵先入此仓，全部造完后再入 `WarriorPool` 并自动上阵（§3.15）。 |
| PrimaryHand | 主要手 | `BodyPartConfig.IsPrimaryHand=1` 的手臂材料；Mode2 自动制造的选料锚点与职业限定主源（§3.15）。 |
| SecondaryHand | 次要手 | `IsPrimaryHand=0` 的手臂材料；与主要手组成双臂；职业取双手 `ClassRestrict` 交集（无交集则仅主要手池）（§3.15）。 |
| ClassRestrict | 职业限定 | 躯体材料可产出的 `ClassId` 多值列表（`\|` 分隔）；Mode2 职业由双手交集/回退决定（§3.15，[SPEC_04 §9.12](SPEC_04_Technical.md)）。 |
| BodyPrimaryStat | 躯体主属性 | 躯体材料字段：`Strength` / `Agility` / `Intelligence` 恰一；Mode2 选其余部位时的匹配键（**勿与** 职业 `PrimaryStat` 混淆）（§3.15）。 |
| ApproxBodyLevel | 近似品质 | Mode2 选料：相对锚点 `|ΔBodyLevel| ≤ 1`；候选排序更高 → 相同 → 低 1 级（§3.15）。 |
| PlacementOrder | 放置排序 | `ClassConfig` 字段（≥1）；AutoManufacture 自动上阵时按此升序先后放置各职业（§3.15，[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| FormationClassZone | 职业布阵区 | 布阵地图 Prefab 上按职业标定的空间区域（**IsoDiamond**：`HalfExtents` 为菱形顶点到中心；与 WalkSurface 同形；无 Y 旋转）；自动上阵落入对应区并做碰撞挤开（§3.15，[SPEC_04 §13](SPEC_04_Technical.md)）。 |
| MagicBook | 魔法书 | 主角特殊装备；效果库；Mode2 在 UI-016 Step2 **单槽脉冲峰值**触发；含「还原」（`RaceWeightPick`）、「战士强化」（`StatMul`/`Primary`）、「士兵技能升级」（`SoldierSkillLevelAdd`）、「职业进阶」（`ForceClass`）等；命中时可烘进 `VisualStyle`（材质和/或放大）（§3.15，[SPEC_04 §9.24](SPEC_04_Technical.md)）。**勿与** §3.16 `ProtagonistEquipment` 混淆。 |
| MagicBookConfig | 魔法书配置表 | MagicBookId → IsUnique、IsProbabilistic、EffectPhase、EffectPayload、EffectParams、VisualStyleId/VisualPriority/VisualIntensityAdd、Icon、名称、介绍（§3.15，[SPEC_04 §9.24](SPEC_04_Technical.md)）。 |
| VisualStyle | 特效外观 | Mode2 魔法书 Token **命中**后烘进实例的特效：AllIn1 **材质通道**（每兵一套优先级赢家）与 **放大通道**（`Style_ScaleModel`，可与材质共存）；同次命中另附带体型步进（§3.15，[SPEC_04 §15.2](SPEC_04_Technical.md)）。 |
| VisualModelScale | 模型缩放系数 | 实例连乘系数 k（缺省 1）：每次 Token **命中**先 ×`WarriorVisualModelScalePerHit`（样例 1.15），若该书为 `Style_ScaleModel` 再 ×`VisualIntensityAdd`，再夹 `WarriorVisualModelScaleMax`（样例 3）；世界 `Visual.localScale=(k,k,k)`，`BodyRadius`/`AttackRange` 均 ×k（§3.15 6b）。 |
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
| CampaignModeSelect | 玩法模式选择 | UI-014：选 Mode1/Mode2 或取消。**本 Demo 进档路径不调用**（新建/进入直进 Mode2）；组件保留供后置 Mode1 入口（§3.2、§3.6）。 |
| DifficultyConfig | 关卡难度表 | `Level_DifficultyConfig`：难度解锁链、显示名、文字介绍、通关一次性奖励（§3.9，[SPEC_04 §9.1a](SPEC_04_Technical.md)）。 |
| DifficultyId | 难度ID | 难度表主键；运作表行归属字段；样例 `Diff_Normal` / `Diff_Hard` / `Diff_Hell`。 |
| UnlockRequireDifficultyId | 解锁所需难度ID | 难度表字段：空=初始解锁；填入存在的 DifficultyId=等该难度通关后解锁；找不到对应行=不可解锁。 |
| ClearReward | 难度通关奖励 | 难度表字段：`ItemId;Count\|…`（经 §9.5a）；首次判定难度通关时一次性发放；空=无奖。 |
| DifficultySelectHost | 难度选择宿主 | **新建进档** / 工具「关卡」先打开（UI-029）：普通/困难/地狱三栏**等宽同屏**（各约 1/3 视口，左右排列）；悬停 Column 显示该难度描述（**Demo 仍写死**；表 `Description` 已定义，接线后置）；**三栏点击都进入沙盘**（UI-036，按 `DifficultyId` 过滤）；**进入占用档**与玩法结束回沙盘（默认 `Diff_Normal`）；**不**在栏内嵌 LevelSelect；难度**不**改玩法数值（§3.5 / §3.20）。无中央 MapHost 地图底图。 |
| Sandbox | 沙盘界面 | 难度之后的关卡入口（UI-036 / D-098 / D-099）：横向方格，显示玩法名与剩余进入次数；次数写入存档并拦截进入；无解锁前置；点击进挖坟 / 商店 / 自动造兵 / COC 战斗（§3.20 / §3.21）。 |
| SandboxNode | 沙盘节点 | `Level_SandboxNodeConfig` 一行：`DifficultyId` + 排序 + 显示名 + `GameplayType` + `RepeatEnterCount`（首次见到时的初始剩余次数）+ 可选 `GameplayConfigId`（§3.20，[SPEC_04 §9.1c](SPEC_04_Technical.md)）。 |
| CocCombat | COC战斗 | 沙盘玩法类型 / `GameplayState`。洒兵代替布阵；击杀唯一最终 BOSS 通关。规则 §3.21。 |
| CocDeploy | 洒兵 | COC 战斗中从士兵库按 `ClassId` 分组，把一名士兵放到可洒区域（§3.21）。 |
| CocFog | 战场迷雾 | COC 地图上由策划多边形盖住的黑雾；无士兵信息 / 临时亮起 / 变暗三态，外加占领点永久亮起（§3.21）。与镜头滤镜、地图外缘雾分开。 |
| CocCapturePoint | COC占领点 | 地图上配置的区域；我方士兵进入后激活并写入存档，永久亮起且可给同一难度的其他沙盘玩法加次数（§3.21）。 |
| InSaveShell | 进档壳层 | 选定存档后进入的常驻壳（Demo 默认 `CampaignMode=Mode2`）：默认打开 `DifficultySelectHost`（三栏同屏）；另承载 `GameplayState` 占位、浮动「工具」，以及左下「商店」（Mode2 / UI-026）、「装备」「魔法书」（UI-022 / UI-023）。独立 Prefab `InSaveShellPanel`。 |
| ToolsPanel | 工具面板 | Demo 调试/设置壳层 UI；由浮动「工具」按钮打开。本期含「设置」「关卡」入口（关卡→列表选关），以及 Demo GM「增加主角装备」「增加魔法书」（→ GmGrantListPanel，UI-019 / D-061）与「添加士兵」（→ GmAddSoldierPanel，UI-020 / D-064）。 |
| ShopSystem | 商店系统 | Mode2 全屏商店：既是关卡 `GameplayType=Shop`（样例 Stage1），也可由 InSaveShell 左下按钮作为局外 overlay 打开；共用 Prefab `ShopStageRoot`。展示玩家信息与 6 项待售商品（归类 A=装备、归类 B=魔法书），支持基于关卡解锁的开放、自动刷新/手动刷新，以及点击购买扣精魂并入账。 |
| ShopProgress | 商店进度 | 存档持久化状态：最高解锁关卡号、是否触发本次开放、当前刷新次数、当前待售商品 6 项及其已售/空状态。 |
| LevelRouteProgress | 关卡路线进度 | 存档持久化：已通关 `GameplayOptionId` 集合（扁平；OptionId 不跨 LevelId 复用）；进关派生解锁；按槽 + CampaignMode（§3.9，[SPEC_04 §6](SPEC_04_Technical.md)）。 |
| ShopOffer | 待售商品条目 | 单项待售信息：slotIndex（0..5）、itemId、归类（A|B）、精魂售价、可购买/已售/空状态；用于展示与购买扣款。 |
| ShopPoolConfig | 商店商品池配置表 | `Shop_ShopPoolConfig.csv` 行：ShopPoolId、RequiredMaxLevelNumber（关卡进度门槛）、ExtraUnlockCondition（预留）、以及 PoolItemsRaw（商品池道具串：`itemId;归类;出现权重|...`）。 |
| ShopRefreshPriceConfig | 刷新商品配置表 | `Shop_ShopRefreshPriceConfig.csv` 行：RefreshCount（刷新次数）与 RefreshPrice（刷新精魂价格）；用于手动刷新递进定价。 |
| ShopCategory | 商品归类 | A 类=装备（对应主角装备 EquipId），B 类=魔法书（对应魔法书 MagicBookId）；同归类内按“道具ID相同→出现权重相加”聚合后做加权抽样。 |
| PlayerPointer | 运行时光标 | 整段 Play 的系统硬件鼠标外观（UI-024）；源图 `Art/UI/Cursor.png`；点击热点为锁尖。**勿与** Dig 圆圈范围（`DigCursorRadius` / `UiDigCursorRing`）混淆。 |
| Level | 关卡 | 由「关卡运作表」定义的多阶段流程实体；每 Stage 挂最多 5 套玩法选项（多选一）；选项详情见子关卡表（§3.9；`Shop` / UM / AutoManufacture 的 ConfigId **忽略**）。进档 Hub / 工具「关卡」→ `DifficultySelectHost`；任意难度点击 → 沙盘 UI-036（§3.20）。路线选择 UI-031 **保留**，不再作为这些入口的目的地。 |
| LevelOperation | 关卡运作 | 关卡运作表一行：关卡 ID + 阶段编号 + `GameplayOptionId1..5` + 可选 `DifficultyId`（归属难度）+ 可选 `RouteMapAssetId`。 |
| GameplayOption | 玩法选项 | 子关卡表一行；玩家在同 Stage 内多选一；通关后按 `UnlockNextOptionIds` 解锁下一 Stage 选项。 |
| RouteSelect | 关卡路线选择 | **保留**；玩家从难度 / 占用档 / 结束不再打开。关卡内 Prefab（UI-031）：`Box` 全屏；`MapScroll` 竖向铺满、宽 1920 水平居中（地图内容 1450 在视口内居中）；`Title`/页签叠在地图上；Box 顶部 LevelId 页签；有 `LevelRouteMap_{LevelId}` 时竖滑该关地图 Prefab（宽 1450、高按比例）+ 选项钉在 Prefab 内同名 `GameplayOptionId` 子节点（场景仅 Icon；地图 Icon 三态：已通关 Checkmark / 可选择慢闪缩放 / 未解锁变暗；悬停 Tips 按 `GameplayType` 分型——Dig：`TipMessages`（类型名/图标/存量尺度箭头）+Description；Shop/AutoManufacture/UpgradeManufacture：`IconAssetId2`+Description；PushMap/SearchExtract/Defend：`IconAssetId2`+Reward 图标行+Description；空字段对应区块隐藏）；无地图 Prefab（或无 `RouteMapAssetId`）时回退竖版自下而上 Stage + 横向完整选项卡；跨 Stage 连线。 |
| DigGameplayConfig | 挖坟配置 | 挖坟配置表一行：时长、开局坟数、过程生成速率、品质权重（零权重项剔除）等（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| Grave | 坟墓 | 挖坟地图上的可生成实体；带坟墓品质 ID；落点须避开已有坟与障碍物。 |
| SpiritCrystal | 精魂结晶 | 坟墓品质 `QualityId=Q101`；交互/障碍/DigAction 同普通坟；掉落仅精魂（`Spirit`），无尸体残骸（§3.10）。 |
| VictorySettlement | 胜利结算 | 关卡**最后一阶段**结束后触发的关卡级结算反馈。 |
| DigMap | 挖坟地图 | 表现上用 Unity **Isometric Tilemap** 铺斜 45° 菱形地板（正交/无透视）；**逻辑足迹 IsoDiamond**（XZ 曼哈顿菱形，与砖面外轮廓对齐；连续可放置，非格子网格）；表现 Prefab 逻辑名 `Ground_01`…`Ground_05`（`DigMapId`）。 |
| Digger | 挖坟主角 | Dig 阶段**不**在地图生成 3D/整角模型；主角表现为 Dig HUD **左上角 60×60** 头像框（§3.10）；`Digger` Prefab 仍可保留于 Catalog/美术管线，见 [SPEC_04 §15](SPEC_04_Technical.md)。 |
| DigAction | 挖掘流程 | 圆圈光标与坟 DigHitShape 相交且停留 ≥0.2s 触发；半径内全部满足条件的坟**同时**各启 DigAction；每坟独立 `DigActionDuration` 后结算扣血；该坟挖掘中不可重复触发（§3.10）。 |
| DigObstacle | 挖坟障碍物 | Dig 阶段**仅**未消除 Grave；圆形障碍半径在坟预制体上配置（§3.10）。**不含**地图中心主角。 |
| DigHitShape | 挖坟命中形 | Grave Prefab 离线烘焙本地 XZ 凸包（贴近精灵轮廓）；光标圆相交判定；与 DigObstacle 分离（§3.10）。 |
| DigProtagonistCapabilities | 挖坟主角能力 | 存档主角派生：挖坟伤害、挖坟阶段时长加成、时长缩短和、光标半径、可挖品质集合、坟墓生成权重加成、过程生成数量加成；由**科技树**学会与**主角装备**（`EffectDomain` 含 `Dig`）效果按键加法重算（§3.10、§3.13、§3.16）。 |
| GraveHP | 坟墓血量 | 坟墓当前/最大生命；maxHP 来自坟墓品质定义表；扣至 0 触发挖掘成功与奖励（§3.10）。 |
| GraveIconStyle | 坟墓图标样式 | 按剩余 HP% 切换：>65% 样式1；30%–65% 样式2；<30% 样式3（§3.10）。 |
| GraveQualityConfig | 坟墓品质定义表 | 品质 ID → maxHP、掉落等；被挖坟权重引用（§3.10，[SPEC_04 §9](SPEC_04_Technical.md)）。 |
| DigReward | 挖掘奖励 | 坟 HP 归 0 时在成功动画中心生成的奖励图标；飞向 Dig HUD 左上角主角头像框，到达后入账并消失（§3.10）。 |
| DigStageSummary | 挖坟阶段汇总 | Dig 有效时长归零后弹出的汇总弹窗：仅展示本阶段已获奖励按类型汇总，无额外发放；每条奖励为格子（图标+名称+数量），每行最多 5；`Body` 约三行可视，超出可竖滑；右上「X」确认关闭（§3.10，UI-011）。 |
| Warehouse | 仓库 | 按存档槽持久的材料仓库；不限格数与存储时长；材料按类型堆叠上限 10000（§3.10）。 |
| WarehouseHudStats | Dig HUD 仓库统计 | Dig HUD `Warehouse` 三行图标统计：精魂、非主要手残骸总数、按种族/基础职业划分的主要手数量；数量为 0 不显示；Hover Tips 文案 ← `LocalizedDescriptionConfig`（§3.10）。 |
| LocalizedDescriptionConfig | 多语言描述表 | `TextKey` → 多语言描述文案（Demo 读 `TextZh`；`TextEn` 预留）；Dig Warehouse Hover Tips Key=`DigWarehouseHoverTips`；子关卡 Tips 类型名 Key=`TipMsg_*`（§3.9 / UI-031，[SPEC_04 §9.34](SPEC_04_Technical.md)）。 |
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
| BodyPartConfig | 躯体材料配置表 | BodyPartId → 道具名称（`DisplayName`）/等级/部位/种族/控制力/精魂消耗/StatBonus/AutoConvert/介绍/美术素材/`IsPrimaryHand`/`ClassRestrict`/`BaseClass`/`BodyPrimaryStat`（§3.11，[SPEC_04 §9.12](SPEC_04_Technical.md)）。 |
| BodySlot | 躯体槽类型 | `Head` / `Torso` / `Arm` / `Leg`（§3.11）。 |
| BodyLevel | 躯体等级 | 躯体材料字段；制造时对已放部位取平均后定外观等级（§3.11）。 |
| StatBonus | 增加的属性值 | 躯体材料平坦属性加成串；`Base(S)=Σ StatBonus(S)`（§3.11）。 |
| Body | 躯体 | 制造所用躯体部位集合；`Base(S)=Σ StatBonus(S)`；各部位 `RaceId` 加权定种族；贡献控制力占用（§3.11）。 |
| BaseStats | 基础属性 | 由已放躯体部位 `StatBonus` 按维求和：生命值、移动速度、力量、敏捷、智力；经 StaticStat/FinalStat 后派生攻/速/CD/血（§3.11、§3.12）。 |
| StaticStat | 静态属性 | 制造/布阵展示用：`max(0, Base+Equip+Base×GemMult+Base×RaceAdjust)`；不含 `SkillBuff`（§3.11）。 |
| PrimaryStat | 主属性 | 职业配置字段：`Strength` / `Agility` / `Intelligence`；决定普攻攻击值所用属性维（§3.11、§3.12，[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| Class | 职业 | 由实例 `ClassId` 提供（有灵魂取自该灵魂；无灵魂强制 `Class_Servants`）；决定 `ClassName`、`PrimaryStat`，以及对五维→战斗参数的换算系数调整（§3.11、§3.12，[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| ClassId | 职业ID | 职业主键；有灵魂时取自灵魂 `ClassId`；无灵魂时强制 `Class_Servants`；制造时写入士兵实例（§3.11，[SPEC_04 §9.9](SPEC_04_Technical.md) / [§9.9b](SPEC_04_Technical.md)）。 |
| ClassConfig | 职业配置表 | ClassId → ClassName、ClassLevel（展示用等级）、BaseClass（基础职业，预留）、PromoteClass（转职职业，可选文字，预留）、PrimaryStat、CombatConvertCoeffs（`键_数值|…`）、AttackRange / 前摇 / 弹速 / 超时、`DefaultSkillIds`（制造默认士兵技能）（§3.11，[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| ClassLevel | 职业等级 | `ClassConfig` 展示字段（品质等级）；UI-016 士兵卡职业名下显示 `Lv.{ClassLevel}`；**不**进战斗/制造公式（[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| BaseClass | 基础职业 | `ClassConfig` 字段；CSV 中文 `战士`/`射手`/`法师`/`刺客`（加载器仍接受旧值 `盗贼`）；空或非法→`Unspecified`；**预留**后续魔法书等条件；**不**参与命名/外观/`PrimaryStat`/战斗（[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| PromoteClass | 转职职业 | `ClassConfig` 可选文字列；空=无转职目标；本轮仅填表/加载；**不**参与命名/外观/`PrimaryStat`/战斗；应用点 **TBD**（[SPEC_04 §9.9b](SPEC_04_Technical.md)）。 |
| DefaultSkillIds | 制造默认获得技能ID | `ClassConfig` 列；空=无；否则 `SkillId` 或 `SkillId\|SkillId`（FK → `SkillConfig.SkillId`）。`ClassId` 最终定稿后写入实例 `SoldierSkills`，初始等级 **1**（§3.11、§3.15）。 |
| SoldierSkill | 士兵技能 | 绑定在士兵**实例**上的技能；制造时由职业默认授予；Mode2 可由魔法书改等级；**无**消耗经验升级；`PermanentDeath` 随实例删除；权威表 `SkillConfig`（§3.11，[SPEC_04 §9.21](SPEC_04_Technical.md)）。**勿与**灵魂/宝石/外置 `Skills` 列表混淆（并行；同 Id 合并 **TBD**）。 |
| SoldierSkills | 士兵技能列表 | 实例字段 `{ SkillId, SkillLevel }[]`；制造烘进快照；`CombatDead` 保留；`PermanentDeath` 删除（§3.11，[SPEC_04 §9.9](SPEC_04_Technical.md)）。 |
| SkillConfig | 技能配置表 | 士兵技能权威表；复合主键 `(SkillId, SkillLevel)` → 名称/图标/描述/`SkillEffectId`/`EffectImplemented`/CD/失控加成等；怪物仍走 `MonsterConfig.Skills`；Demo PushMap 施放见 §3.12 SkillCast / D-069 / D-073（[SPEC_04 §9.21](SPEC_04_Technical.md)）。 |
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
| AttackMode | 攻击模式 | `Melee` / `Ranged` / `Parabola`；士兵取自 `SoulConfig`（Mode2 无灵魂取 `ClassConfig`），怪物仍仅 `Melee` / `Ranged`；`Parabola` 仅射手士兵的抛物线射击分支（§3.12）。 |
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
| FormationBond | 阵容羁绊 | 按上阵士兵属性统计激活的战斗增益；同 `BondId` 多等级互斥；Buff FK `SkillEffectConfig`（§3.17）。**勿与** §3.18 战术阵型混淆。 |
| BondActivationCondition | 羁绊激活条件 | `FormationBondConfig.ActivationCondition` 结构化 DSL；首版四类计数条件（§3.17）。 |
| TacticalFormation | 战术阵型 | 拥有阵型技能的士兵经左缘按钮**手动**建成空间编队（已上阵优先，不足时从士兵栏未上阵补人并上阵）；同 `FormationId` 可多组，每组独立虚拟中心 + 槽位；战斗中成员守槽（D-094）；与 `BattleFormation`（上阵名单）、`FormationBond`（属性计数 Buff）**并行**（§3.18 / D-093）。 |
| TacticalFormationGroup | 战术阵型组 | 一次按钮点击产生的一组；`GroupInstanceId` 唯一；成员同时只属于一个组；等级由组内 `ClassLevel` 平均向下取整（§3.18 / D-093）。 |
| FormationLevel | 阵型等级 | `TacticalFormationConfig` 复合主键之一；不由玩家点选；查属性时取 ≤ 计算等级的最大表等级（§3.18 / D-093）。 |
| FormationSkill | 阵型技能 | 魔法书 `GrantFormationSkill` 授予、写入实例 `SoldierSkills` 的标记技能；`SkillConfig.FormationId` FK → `TacticalFormationConfig`（§3.18）。 |
| FormationSlot | 阵型槽位 | 战术阵型 Prefab 内相对中心的士兵站位；可挂 `PreferredClass`（优选职业=`BaseClass`）供建组分槽软匹配；战斗中 `GoalKind=FormationSlot` 趋近「中心+旋转偏移」（§3.18）。 |
| FormationLeash | 阵型拴绳 | Pattern 上保留的半径字段。**D-094：** 不再把接敌目的地投影回圆周，也不再用它决定是否离槽（§3.18）。 |
| BondBuff | 羁绊Buff | 羁绊激活后引用的技能效果；`BondBuff` → `SkillEffectConfig`（§3.17）。 |
| Defend | 防守 / 保卫战 | 关卡玩法类型 / `GameplayState`；亦为战斗模式1「保卫战」；进入阶段可经 ModeSelect 选关，再 Prepare→开战→战斗；见 §3.12。 |
| BattleMode | 战斗模式 | 战斗阶段可选模式：`Defend`（保卫战，模式1）/ `PushMap`（推图战，模式2）；模式2 规则见 §3.14。 |
| BattleModeSelect | 战斗模式选关 | 进入 Defend 阶段后的选模式+选关 UI（UI-013）；模式1→§3.12；模式2确认后→§3.14 Prepare（见 §3.8 D-044）。 |
| PushMap | 推图战 | 关卡玩法类型 / `GameplayState`；亦可作战斗模式2；目标点链占领 + 刷怪点/陷阱/BOSS 通关；复用 Defend 布阵/护盾/失控/士兵战斗；见 §3.14。 |
| SearchExtract | 搜打撤 | 关卡玩法类型 / `GameplayState`（Mode2 子关卡）；有序搜集点链 + 进圈倒计时守点 + 方向波次刷怪 + 单点胜利后「继续搜集/离开」；**非** `CampaignMode`；见 §3.19。 |
| GatherPoint | 搜集点 | 搜打撤有序目标；地图 `ObjectivePoint`+`CaptureZone`；进圈激活搜集倒计时与刷怪；**勿与** PushMap 即时占领混淆（§3.19）。 |
| GatherCountdown | 搜集倒计时 | 单搜集点激活后规则层倒计时；归零且仍有忠诚存活 → 单点胜利（§3.19）。 |
| SearchExtractPhase | 搜打撤子状态 | `Prepare` → `Combat` → `Ended`（§3.19）。 |
| PushMapPhase | 推图战子状态 | 阶段内子状态：`Prepare` → `Combat` → `Ended`（与 DefendPhase 对齐；见 §3.14）。 |
| PushMapBattleSettlement | 战斗结算（推图/搜打撤） | 胜负均弹（UI-017，PushMap + SearchExtract 共用）：胜利=标题+耗时/击杀（PushMap）+阵亡总数+四职业阵亡+继续；失败=标题「失败」+阵亡总数+「返回主界面」「重新开始」；见 §3.14 / §3.19。 |
| BattleCasualtyStats | 阵亡统计 | 本场已登记非 Rebel 士兵中 `CombatDead`/`PermanentDead`/`HP≤0` 的总数，及按 `BaseClass`（战士/射手/法师/刺客）细分；`Unspecified` 只进总数；见 §3.14。 |
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
| AlertRadius | 警戒半径 | `AggroMode` 主动态用于发现我方士兵的半径；与 `AttackRange` 并列（§3.14）。D-074：`MonsterSelfReviveOnDeath` 首次复活可将实例警戒半径改为 EffectParams `AlertRadius`（后续复活不再改）。 |
| DungeonUnlock | 副本解锁 | PushMap 占领/通关配置的副本 ID 写入存档钩子；副本玩法正文 **TBD**（§3.14）。 |
| CameraFollowMode | 镜头跟随模式 | PushMap Combat 表现层：`Auto`（沿 `CameraFollowPath` 最大投影进度）/ `Manual`（拖拽平移）；见 §3.14。 |
| CameraFollowPath | 镜头跟随轨 | PushMap 地图 Prefab 上的虚拟推进折线；作者摆起点/拐弯/终点，相邻路点间按世界 XZ 直线按间距采样；镜头对准折线上的点，不对准士兵 Transform；见 §3.14。 |
| CameraPathProgress | 镜头轨进度 | 折线弧长参数 `s∈[0,1]`；Prepare 滑动条 / 快速预览与 Combat Auto 共用；Auto 取存活忠诚兵投影最大值；领头失效则回退；见 §3.14。 |
| PreparePathPreview | 布阵路径预览 | PushMap Prepare：可选「快速预览」沿 `CameraFollowPath` 从 `WP_End` 反向扫到 `WP_Start`；默认不播；见 §3.14。 |
| CameraPathSlider | 路径滑动条 | PushMap Prepare：左=`WP_Start`(s=0)、右=`WP_End`(s=1)，按折线弧长均匀映射；拖动即时定位 FormationCamera；见 §3.14。 |
| PrepareSpawnPreview | 布阵开战刷怪预览 | PushMap Prepare：按开战非陷阱行刷出 Idle 怪供预览；开战销毁后正式重刷；见 §3.14。 |
| ResumeFollow | 恢复跟随 | PushMap 手动模式下底中按钮；点击回到 `Auto`；SearchExtract Hold 期间回到 HoldFraming（非轨）；见 §3.14 / §3.19。 |
| FollowDeadzone | 跟随死区 | Auto 世界 XZ 半径 0.15；圈内忽略目标小幅位移；见 §3.14。 |
| FollowSmoothTime | 跟随缓动时间 | Auto 超出死区后 XZ SmoothDamp 时间 0.25s；见 §3.14。 |
| SearchExtractHoldCamera | 搜打撤守点镜头 | SearchExtract 点激活后的 Combat 镜头模式：视口包围忠诚兵 + 迟滞拉近；钳 Size/半径；见 §3.19。 |
| HoldFraming | 守点包围取景 | Hold 表现算法：相机视口 AABB、外框立刻拉远/平移、内框滞留后慢收紧；锚点=当前 Objective；见 §3.19。 |
| DamagePopup | 伤害飘字 | PushMap 命中成功后在**被击目标**头顶显示单次伤害文本（格式 `-受伤值`）；敌我字号均为 **12**（怪红 / 兵白）；0.5s 内 `position.z` 相对起点 +0→+0.5 后销毁；见 §3.14。 |
| CombatSkillIcon | 战斗技能图标 | PushMap Combat 士兵技能图标（UI-025 / D-071）：瞬时头顶 35×35 静止 0.6s 后沿世界 +Z 上飘 0.3s 淡出；持续效果脚下 20×20；同列向屏幕右侧排开；见 §3.12 SkillCast / §3.14。 |
| CombatIndicator | 战斗指示器 | PushMap / SearchExtract **Combat** 顶中「血条」HUD（UI-033 / D-089）：中央敌我存活数 + 两侧单位格（职业/怪物简画 + HP% 着色）；低频轮询；死亡格 0.5s 后移除；见 §3.14 / §3.19。 |
| OffScreenSpawnHint | 离屏刷怪边缘提示 | PushMap / SearchExtract **Combat** 真实刷怪时：若该组 `SpawnPoint`/`basePos` 在战斗相机视口外，于最靠近方向的屏幕边缘显示 `EnemyAttack_1`，开场 `OffScreenSpawnHintIntroBlinkSeconds`（默认 0.4s）内闪红 `OffScreenSpawnHintIntroBlinkCount`（默认 2）次，再常亮 `OffScreenSpawnHintHoldSeconds`（默认 2s）；显示缩放 `OffScreenSpawnHintDisplayScale`（默认 1.3）；Prepare 预览 / 屏内 / 非 Combat 不出；见 §3.14 / §3.19、UI-034 / D-090。 |
| EffectKind | 技能效果种类 | `SkillEffectConfig` 登记制 PascalCase Token（对齐 MagicBook `EffectPayload`）；空=未实现；Session/View **禁止**按 `SkillId` 硬分支（§3.12，[SPEC_04 §9.21b](SPEC_04_Technical.md)）。 |
| TriggerHook | 技能效果钩子 | 管线插入点枚举（如 `OnOutgoingDamageSettle`）；Handler 按 Hook 注册（§3.12）。 |
| SkillEffectPipeline | 技能效果管线 | 查实例 `SoldierSkills` → `SkillConfig.SkillEffectId` → `SkillEffectConfig` → 按 `TriggerHook` 调度已注册 Handler（§3.12）。 |
| CombatStatusService | 战斗状态服务 | 无敌 / 击晕 / 减速 / 灼烧 DoT 的统一 Tick + 查询；士兵与怪物分 bucket（§3.12）。 |
| HitFlash | 受伤闪烁 | PushMap 命中成功后目标模型临时亮色闪烁；怪亮红、兵亮白；共 2 次×0.1s 紧接中间不灭（≈连续亮 0.2s）；过程中再受伤则刷新；见 §3.14。 |
| AllyFootCircle | 友军脚下圈 | Defend / PushMap Combat 中**忠诚存活**士兵脚下绿色描边圆 + 内部黑色半透明（α=**160/255**）；世界半径=`BodyRadius`；localPos Y=-0.05 Z=-0.2；rotation X=**-30**；Order In Layer=`50`；随士兵移动；叛变/死亡隐藏；见 §3.12 / §3.14、[SPEC_04 §9.7](SPEC_04_Technical.md)。 |
| DefendPhase | 防守子状态 | 阶段内子状态：`ModeSelect`（选模式/关卡，若启用）→ `Prepare`（准备）→ `Combat`（战斗中）→ `Ended`（已结束）。 |
| StartBattle | 开战 | 准备态 UI 按钮；点击后进入 `Combat` 并部署单位（§3.12）。 |
| BattleMap | 战斗地图 | 防守阶段地图；逻辑为连续可走空间（非格子）；表现与 DigMap 同为 Isometric Tilemap，可共用 `Ground_*`（§3.12）。 |
| BattleProtagonist | 战斗主角 | 战斗中地图中央的主角实体；与挖坟 `Digger` 区分；Defend 中以 **护盾（Shield）** 代替 HP 承受普通攻击（§3.12）；外观为 Character Creator **烘焙整角**，见 [SPEC_04 §15](SPEC_04_Technical.md)。 |
| Shield | 护盾 | Defend 战斗中主角可承受 **普通攻击** 的次数；开战时 `Shield =` 当前等级行 `ProtagonistMaxHP`；归零 → LevelFailure（§3.12）。 |
| Monster | 怪物 | 防守战斗敌方单位；参数见 `MonsterConfig`；出现位置可为地图内或外围（§3.12，[SPEC_04 §9.19](SPEC_04_Technical.md)）；外观为 Character Creator **烘焙整角**（`ModelId` Prefab），见 [SPEC_04 §15](SPEC_04_Technical.md)。 |
| MonsterConfig | 怪物配置表 | 怪物 ID → 模型/名称/目标选择/AttackMode/`MonsterType`/AggroMode/警戒半径/血量/移速/攻击力/攻速/技能/掉落/普攻·走·跑动作池（§3.12、§3.14，[SPEC_04 §9.19](SPEC_04_Technical.md)）。 |
| MonsterType | 怪物类型 | `MonsterConfig` 原型标签：`1`=普通 / `2`=精英 / `3`=BOSS；**异于** PushMap 刷怪行 `IsBoss`（通关目标）；D-073 `Skill_08` 对 `Elite` 增伤读取本字段（[SPEC_04 §9.19](SPEC_04_Technical.md)）。 |
| Wave | 波次 | 防守刷怪：由 `WaveSpawnConfig` 在同一 `WaveConfigId` 下的刷怪行集合定义；全部行触发且全灭为阶段胜利条件之一（§3.12）。 |
| WaveSpawnConfig | 刷怪波次配置表 | WaveConfigId + 出怪顺序/剩余秒/怪物/数量/位置/方式（§3.12，[SPEC_04 §9.18](SPEC_04_Technical.md)）。 |
| WaveConfigId | 波次配置ID | 防守玩法配置指向的刷怪表分组键（§3.12，[SPEC_04 §9.7](SPEC_04_Technical.md)）。 |
| RemainingCombatSeconds | 战斗剩余秒 | Defend 开战倒计时剩余整秒；与刷怪行 `SpawnRemainingSeconds` 相等时激活该行（§3.12）。 |
| TargetSelect | 目标选择 | 怪物选目标模式：`Nearest` / `PreferWarrior` / `PreferProtagonist`（§3.12 / `MonsterConfig`）。士兵侧「最近」见 `NearestTargetBand`。 |
| NearestTargetBand | 最近目标带 | 怪物挑士兵时：先求合法候选最小距 `dMin`，带内 `d ≤ dMin×(1+NearestTargetBandRelative)+NearestTargetBandSlack`；带内优先无 `TargetFocus` 的空闲兵，再取最近；同档粘滞当前聚焦。常量见 [SPEC_04 §9.20b](SPEC_04_Technical.md)（§3.12）。 |
| TargetFocus | 目标聚焦 | 怪物当前锁定的攻击目标登记（`TargetFocusRegistry`）；用于最近带内「空闲优先」。进距后释放 AttackSlot **不**等于失去聚焦（§3.12）。 |
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
| GameplayState | 玩法状态 | In-session main state enum: `Shop` (shop; Mode2 Level stage 1), `Dig`, `AutoManufacture` (Mode2 pipeline), `UpgradeManufacture` (was placeholder `SewRevive`), `Defend`, `PushMap`, `SearchExtract` (search-fight-extract; Mode2 SubLevel; §3.19), `CocCombat` (COC combat; entered from the Sandbox; §3.21). During a Level, set by the current stage's gameplay type (§3.9); shell default placeholder remains Dig. |
| SaveSlot | 存档槽 | Fixed local slots; this version **3 slots** (indices 0–2). Empty → create; occupied → enter or delete. Occupied flag is shared per slot; WarriorPool / BattleFormation / DungeonUnlocks progress is isolated per slot **and** `CampaignMode` (§3.4). |
| CampaignMode | 玩法模式 | Save-level play gate: `Mode1` / `Mode2`. **This Demo enter path:** create/enter **skip** `CampaignModeSelect` and always use `Mode2` (UI-014 retained; Mode1 entry deferred). Progress fully isolated per mode in the same slot; Mode2 uses a separate config-table root ([SPEC_04 §14](SPEC_04_Technical.md)). Mode2 shares Dig/Defend mechanics with Mode1; **soldier manufacture**: Mode1 manual (§3.11), Mode2 AutoManufacture (§3.15). **Do not confuse** with `BattleMode` (Defend/PushMap). |
| AutoManufacture | 自动制造 | Mode2 stage type / `GameplayState`: after DigStageSummary confirm; auto pick parts → craft → temp warehouse → clear formation then deploy by class zones; then enter `UpgradeManufacture` (§3.15). |
| TempWarriorWarehouse | 临时仓库 | AutoManufacture batch buffer: crafted soldiers enter here first; after the batch finishes, flush to `WarriorPool` and auto-deploy (§3.15). |
| PrimaryHand | 主要手 | Arm BodyPart with `IsPrimaryHand=1`; Mode2 selection anchor and primary ClassRestrict source (§3.15). |
| SecondaryHand | 次要手 | Arm with `IsPrimaryHand=0`; pairs with PrimaryHand; class from ClassRestrict intersection (else PrimaryHand pool only) (§3.15). |
| ClassRestrict | 职业限定 | Multi-`ClassId` list on BodyPart (`\|`-separated); Mode2 class from hand intersection/fallback (§3.15, [SPEC_04 §9.12](SPEC_04_Technical.md)). |
| BodyPrimaryStat | 躯体主属性 | BodyPart field: exactly one of `Strength` / `Agility` / `Intelligence`; Mode2 matcher when picking remaining parts (**not** Class `PrimaryStat`) (§3.15). |
| ApproxBodyLevel | 近似品质 | Mode2 pick: `|ΔBodyLevel| ≤ 1` vs anchor; sort higher → same → lower-by-1 (§3.15). |
| PlacementOrder | 放置排序 | `ClassConfig` field (≥1); AutoManufacture deploys classes in ascending order (§3.15, [SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| FormationClassZone | 职业布阵区 | Authoring zone on formation map Prefab per ClassId (**IsoDiamond**: `HalfExtents` = vertex-to-center; same shape as WalkSurface; no Y rotation); auto-deploy lands there with separation (§3.15, [SPEC_04 §13](SPEC_04_Technical.md)). |
| MagicBook | 魔法书 | Protagonist special equipment; Mode2 applies at UI-016 Step2 **per-slot pulse peak**; includes Restore (`RaceWeightPick`), Warrior Enhance (`StatMul`/`Primary`), Soldier skill level (`SoldierSkillLevelAdd`), class advance (`ForceClass`); a **hit** may bake `VisualStyle` (material and/or scale) (§3.15, [SPEC_04 §9.24](SPEC_04_Technical.md)). **Distinct from** §3.16 `ProtagonistEquipment`. |
| MagicBookConfig | 魔法书配置表 | MagicBookId → IsUnique, IsProbabilistic, EffectPhase, EffectPayload, EffectParams, VisualStyleId/VisualPriority/VisualIntensityAdd, Icon, name, description (§3.15, [SPEC_04 §9.24](SPEC_04_Technical.md)). |
| VisualStyle | 特效外观 | Mode2 visual baked on MagicBook token **hit**: AllIn1 **material channel** (one winner by priority) plus **scale channel** (`Style_ScaleModel`, coexists with material); same hit also applies the body-scale step (§3.15, [SPEC_04 §15.2](SPEC_04_Technical.md)). |
| VisualModelScale | 模型缩放系数 | Instance stacked multiplier k (default 1): each token **hit** first ×`WarriorVisualModelScalePerHit` (sample 1.15), then if book is `Style_ScaleModel` ×`VisualIntensityAdd`, then clamp `WarriorVisualModelScaleMax` (sample 3); world `Visual.localScale=(k,k,k)`; `BodyRadius`/`AttackRange` both ×k (§3.15 6b). |
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
| CampaignModeSelect | 玩法模式选择 | UI-014 Mode1/Mode2/cancel. **Not called on this Demo enter path** (create/enter go straight to Mode2); component retained for deferred Mode1 entry (§3.2, §3.6). |
| DifficultyConfig | 关卡难度表 | `Level_DifficultyConfig`: unlock chain, display name, description, one-shot clear reward (§3.9, [SPEC_04 §9.1a](SPEC_04_Technical.md)). |
| DifficultyId | 难度ID | Difficulty table PK; Level Operation ownership field; samples `Diff_Normal` / `Diff_Hard` / `Diff_Hell`. |
| UnlockRequireDifficultyId | 解锁所需难度ID | Difficulty field: empty = initially unlocked; existing DifficultyId = unlock after that difficulty clears; missing row = never unlockable. |
| ClearReward | 难度通关奖励 | Difficulty field: `ItemId;Count\|…` (via §9.5a); granted once on first difficulty-clear; empty = none. |
| DifficultySelectHost | 难度选择宿主 | **Create enter** / Tools Level open this first (UI-029): equal-width Normal/Hard/Hell columns **same-screen** (~1/3 viewport each, left-to-right); hover Column shows difficulty description (**Demo still hardcoded**; table `Description` defined, wiring deferred); **any column click** opens the Sandbox (UI-036, filtered by `DifficultyId`); **Enter occupied** and gameplay-end return to the Sandbox (default `Diff_Normal`); **no** in-column LevelSelect; difficulty does **not** change gameplay numbers (§3.5 / §3.20). No central MapHost map image. |
| Sandbox | 沙盘界面 | Level entry after difficulty (UI-036 / D-098 / D-099): horizontal cells show gameplay name and remaining enter count; the count is saved and gates entry; no unlock gates; click enters Dig / Shop / AutoManufacture / COC combat (§3.20 / §3.21). |
| SandboxNode | 沙盘节点 | One `Level_SandboxNodeConfig` row: `DifficultyId` + sort + display name + `GameplayType` + `RepeatEnterCount` (initial remaining count the first time the save sees the node) + optional `GameplayConfigId` (§3.20, [SPEC_04 §9.1c](SPEC_04_Technical.md)). |
| CocCombat | COC战斗 | Sandbox gameplay type / `GameplayState`. Deploy-soldiers replaces formation; killing the single Final Boss clears the node. Rules §3.21. |
| CocDeploy | 洒兵 | In COC combat, place one soldier from the warrior pool, grouped by `ClassId`, into an allowed area (§3.21). |
| CocFog | 战场迷雾 | Black fog authored as polygons on the COC map: unseen / temporary reveal / explored-dark, plus permanent light from capture points (§3.21). Separate from the camera fog filter and map-edge fog. |
| CocCapturePoint | COC占领点 | A map-authored area. A friendly soldier entering it activates it into the save, lights it permanently, and can add enter counts to other Sandbox nodes of the same difficulty (§3.21). |
| InSaveShell | 进档壳层 | Persistent shell after entering a save (Demo default `CampaignMode=Mode2`): default `DifficultySelectHost` (three columns same-screen); also hosts `GameplayState` placeholder, floating Tools, bottom-left Shop (Mode2 / UI-026) / Equipment / MagicBook (UI-022 / UI-023). Standalone Prefab `InSaveShellPanel`. |
| ToolsPanel | 工具面板 | Demo settings/debug shell UI opened by floating Tools. This version: Settings + Level (Level → pick list) + Demo GM Grant Protagonist Equipment / Grant MagicBook (→ GmGrantListPanel, §3.5 / UI-019 / D-061) + Add Soldier (→ GmAddSoldierPanel, UI-020 / D-064). |
| ShopSystem | 商店系统 | Mode2 full-screen shop: a Level `GameplayType=Shop` (sample Stage1) **and** an out-of-level InSaveShell overlay from the bottom-left button; both instantiate the same Prefab `ShopStageRoot`. Shows player info and 6 offers (category A=equipment, category B=magicbooks). Supports level-unlock gating, auto/manual refresh, and click-to-buy that deducts Spirit and grants inventory/equipment. |
| ShopProgress | 商店进度 | Save-persisted state: max unlocked level marker, whether this unlock triggers current opening, current refresh count, current 6 offers and their sold/empty status. |
| LevelRouteProgress | 关卡路线进度 | Save-persisted cleared `GameplayOptionId` set (flat; OptionIds must not reuse across LevelIds); unlocks derived on enter; per slot + CampaignMode (§3.9, [SPEC_04 §6](SPEC_04_Technical.md)). |
| ShopOffer | 待售商品条目 | Single offer info: slotIndex (0..5), itemId, category (A|B), Spirit price, purchasable/sold/empty state; used for display and Spirit deduction. |
| ShopPoolConfig | 商店商品池配置表 | `Shop_ShopPoolConfig.csv` row: ShopPoolId, RequiredMaxLevelNumber (level progress gate), ExtraUnlockCondition (reserved), and PoolItemsRaw (pool items string: `itemId;category;weight|...`). |
| ShopRefreshPriceConfig | 刷新商品配置表 | `Shop_ShopRefreshPriceConfig.csv` row: RefreshCount (how many manual refreshes) and RefreshPrice (Spirit cost); used for manual refresh pricing progression. |
| ShopCategory | 商品归类 | Category A=equipment (protagonist gear EquipId), category B=magicbooks (MagicBookId). Inside a category, identical itemId weights are summed before weighted sampling. |
| PlayerPointer | 运行时光标 | Whole-Play hardware mouse look (UI-024); source `Art/UI/Cursor.png`; hotspot = shovel tip. **Distinct from** Dig circle range (`DigCursorRadius` / `UiDigCursorRing`). |
| Level | 关卡 | Multi-stage flow defined by Level Operation table; each Stage mounts up to 5 gameplay options (pick-one); option details in SubLevel table (§3.9; `Shop` / UM / AutoManufacture ConfigId **ignored**). Enter-shell Hub / Tools Level → `DifficultySelectHost`; any difficulty click → Sandbox UI-036 (§3.20). RouteSelect UI-031 **remains**, and is no longer the destination of these entries. |
| LevelOperation | 关卡运作 | One Level Operation row: LevelId + StageNumber + `GameplayOptionId1..5` + optional `DifficultyId` + optional `RouteMapAssetId`. |
| GameplayOption | 玩法选项 | One SubLevel row; pick-one within Stage; clear unlocks next-Stage options via `UnlockNextOptionIds`. |
| RouteSelect | 关卡路线选择 | **Retained**; difficulty / occupied enter / gameplay end no longer open it. In-Level Prefab (UI-031): `Box` fullscreen; `MapScroll` full viewport height, width 1920, horizontally centered (map content 1450 centered in viewport); `Title`/tabs overlay the map; LevelId tabs atop Box; with `LevelRouteMap_{LevelId}` → scroll that map Prefab (width 1450, height by aspect) + options pinned to child nodes named `GameplayOptionId` (Icon only on map; map Icon tri-state: Cleared Checkmark / Selectable pulse / Locked dim; hover Tips by `GameplayType` — Dig: `TipMessages` (type name/icon/stock-scale arrows)+Description; Shop/AutoManufacture/UpgradeManufacture: `IconAssetId2`+Description; PushMap/SearchExtract/Defend: `IconAssetId2`+Reward icon row+Description; empty fields hide their block); without map Prefab (or no `RouteMapAssetId`) → bottom-up Stages + full horizontal option cards; cross-Stage edges. |
| DigGameplayConfig | 挖坟配置 | One Dig config row: duration, initial grave count, spawn rate, quality weights (zero-weight entries dropped) (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| Grave | 坟墓 | Spawnable Dig-map entity with Grave Quality Id; placement must avoid existing graves and obstacles. |
| SpiritCrystal | 精魂结晶 | Grave quality `QualityId=Q101`; same dig/obstacle/DigAction as normal graves; loot is Spirit only (no body-part wrecks) (§3.10). |
| VictorySettlement | 胜利结算 | Level-level settlement feedback after the **last** stage ends. |
| DigMap | 挖坟地图 | Presentation uses Unity **Isometric Tilemap** diamond floor tiles (orthographic / no perspective); **logic footprint IsoDiamond** (XZ Manhattan diamond aligned to tile silhouette; continuous placeable, not a cell grid); presentation Prefab logical names `Ground_01`…`Ground_05` (`DigMapId`). |
| Digger | 挖坟主角 | Dig stage does **not** spawn a map avatar; protagonist is shown as Dig HUD **top-left 60×60** portrait (§3.10); `Digger` Prefab may remain in Catalog/art pipeline — [SPEC_04 §15](SPEC_04_Technical.md). |
| DigAction | 挖掘流程 | Circle cursor dwell ≥0.2s over diggable graves intersecting DigHitShape triggers dig; **all** eligible graves start DigAction **in parallel**; each resolves damage after its own `DigActionDuration`; busy grave cannot re-trigger (§3.10). |
| DigObstacle | 挖坟障碍物 | Dig-stage obstacles are **only** uncleared Graves; circle obstacle radius on Grave Prefabs (§3.10). **No** map-center protagonist obstacle. |
| DigHitShape | Dig hit shape | Offline-baked local-XZ convex hull on Grave Prefab (silhouette-approx); cursor-circle intersection; separate from DigObstacle (§3.10). |
| DigProtagonistCapabilities | 挖坟主角能力 | Save-slot protagonist derived stats: dig damage, Dig stage duration bonus, duration-reduction sum, cursor radius, diggable quality set, grave spawn-weight bonuses, process-spawn count bonus; recalculated additively from **tech-tree** learns **and** protagonist gear whose `EffectDomain` includes `Dig` (§3.10, §3.13, §3.16). |
| GraveHP | 坟墓血量 | Current/max HP; maxHP from GraveQualityConfig; 0 HP → dig success + reward (§3.10). |
| GraveIconStyle | 坟墓图标样式 | By remaining HP%: >65% style1; 30%–65% style2; <30% style3 (§3.10). |
| GraveQualityConfig | 坟墓品质定义表 | Quality Id → maxHP, loot, etc.; referenced by Dig spawn weights (§3.10, [SPEC_04 §9](SPEC_04_Technical.md)). |
| DigReward | 挖掘奖励 | Reward icon spawned at dig-success anim center when HP hits 0; flies to Dig HUD top-left protagonist portrait, credits on arrival, then disappears (§3.10). |
| DigStageSummary | 挖坟阶段汇总 | Popup after Dig effective duration hits 0: aggregate rewards earned this stage by type only; no extra grants; each reward is a cell (icon+name+qty), max 5 per row; `Body` shows ~3 rows then vertical scroll; top-right "X" confirms (§3.10, UI-011). |
| Warehouse | 仓库 | Per-SaveSlot material warehouse; unlimited slots and retention; materials stack by type up to 10000 (§3.10). |
| WarehouseHudStats | Dig HUD warehouse stats | Dig HUD `Warehouse` three-row icon stats: Spirit, non-primary-hand wreck total, primary-hand counts by race / base class; hide zero counts; hover tips ← `LocalizedDescriptionConfig` (§3.10). |
| LocalizedDescriptionConfig | Localized description table | `TextKey` → localized copy (Demo reads `TextZh`; `TextEn` reserved); Dig Warehouse hover Key=`DigWarehouseHoverTips`; SubLevel Tips type-name Keys=`TipMsg_*` (§3.9 / UI-031, [SPEC_04 §9.34](SPEC_04_Technical.md)). |
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
| ClassConfig | 职业配置表 | ClassId → ClassName, ClassLevel (display grade), BaseClass (reserved), PromoteClass (optional promote-class text, reserved), PrimaryStat, CombatConvertCoeffs (`Key_Value|…`), AttackRange / windup / projectile / timeout (§3.11, [SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| ClassLevel | Class level | `ClassConfig` display field (quality grade); UI-016 soldier card shows `Lv.{ClassLevel}` under class name; **not** used in combat/manufacture math ([SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| BaseClass | Base class | `ClassConfig` field; CSV Chinese `战士`/`射手`/`法师`/`刺客` (loader still accepts legacy `盗贼`); empty/illegal → `Unspecified`; **reserved** for future MagicBook conditions; **not** used in naming / appearance / `PrimaryStat` / combat ([SPEC_04 §9.9b](SPEC_04_Technical.md)). |
| PromoteClass | 转职职业 | Optional `ClassConfig` text; empty = no promote target; fill/load this slice; **not** used in naming / appearance / `PrimaryStat` / combat; application **TBD** ([SPEC_04 §9.9b](SPEC_04_Technical.md)). |
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
| AttackMode | 攻击模式 | `Melee` / `Ranged` / `Parabola`; soldiers from `SoulConfig` (Mode2 no-soul from `ClassConfig`), monsters stay `Melee` / `Ranged` only; `Parabola` is the archer-only arc-shot branch of hit scheme D (§3.12). |
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
| FormationBond | 阵容羁绊 | Combat buff activated by deployed-soldier stat counts; same `BondId` levels mutually exclusive; Buff FK `SkillEffectConfig` (§3.17). **Distinct from** §3.18 TacticalFormation. |
| BondActivationCondition | 羁绊激活条件 | Structured DSL on `FormationBondConfig.ActivationCondition`; v1 four count kinds (§3.17). |
| TacticalFormation | 战术阵型 | Soldiers with a Formation Skill are **manually** grouped from the left-edge button (deployed first; shortfall filled from the undeployed SoldierBar); the same `FormationId` may have many groups, each with its own virtual center + slots; combat members hold those slots (D-094); **parallel** to `BattleFormation` and `FormationBond` (§3.18 / D-093). |
| TacticalFormationGroup | 战术阵型组 | One button click creates one group; `GroupInstanceId` is unique; a soldier belongs to at most one group; level = floor of members' mean `ClassLevel` (§3.18 / D-093). |
| FormationLevel | 阵型等级 | Composite-key part of `TacticalFormationConfig`; not player-picked; stat lookup uses the greatest table level ≤ the computed level (§3.18 / D-093). |
| FormationSkill | 阵型技能 | Marker skill granted by MagicBook `GrantFormationSkill` into instance `SoldierSkills`; `SkillConfig.FormationId` FK → `TacticalFormationConfig` (§3.18). |
| FormationSlot | 阵型槽位 | Soldier offset from formation center authored on the pattern Prefab; optional `PreferredClass` (`BaseClass`) soft-matches members at group create; in combat `GoalKind=FormationSlot` seeks center + rotated offset (§3.18). |
| FormationLeash | 阵型拴绳 | Radius field kept on the Pattern. **D-094:** it no longer clamps engage destinations onto the circle and no longer decides whether a member leaves the slot (§3.18). |
| BondBuff | 羁绊Buff | Skill effect referenced when bond active; `BondBuff` → `SkillEffectConfig` (§3.17). |
| Defend | 防守 / 保卫战 | Stage type / `GameplayState`; also BattleMode 1「保卫战」; enter stage may ModeSelect then Prepare → StartBattle → Combat; §3.12. |
| BattleMode | 战斗模式 | Battle-stage modes: `Defend` (Mode1) / `PushMap` (Mode2); Mode2 rules in §3.14. |
| BattleModeSelect | 战斗模式选关 | Mode+Level select UI after entering Defend (UI-013); Mode1→§3.12; Mode2 confirm→§3.14 Prepare (§3.8 D-044). |
| PushMap | 推图战 | Stage type / `GameplayState`; also BattleMode 2; objective capture + spawn/trap/Boss clear; reuses Defend formation/Shield/LOC/WarriorCombat; §3.14. |
| SearchExtract | 搜打撤 | Stage type / `GameplayState` (Mode2 SubLevel); ordered gather-point chain + zone countdown hold + directional wave spawns + continue/leave after point clear; **not** `CampaignMode`; §3.19. |
| GatherPoint | 搜集点 | SearchExtract ordered objective; map `ObjectivePoint`+`CaptureZone`; enter zone starts gather countdown + spawns; **not** PushMap instant Capture (§3.19). |
| GatherCountdown | 搜集倒计时 | Per-point countdown after activation; zero + living loyal → point success (§3.19). |
| SearchExtractPhase | 搜打撤子状态 | `Prepare` → `Combat` → `Ended` (§3.19). |
| PushMapPhase | 推图战子状态 | In-stage phases: `Prepare` → `Combat` → `Ended` (aligned with DefendPhase; §3.14). |
| PushMapBattleSettlement | Battle settlement (PushMap / SearchExtract) | Always on win/lose (UI-017, shared): victory = title + time/kills (PushMap) + casualty total + four BaseClass counts + Continue; defeat = title Defeat + casualty total + Return to Title / Restart; §3.14 / §3.19. |
| BattleCasualtyStats | Casualty stats | Registered non-Rebel soldiers with `CombatDead`/`PermanentDead`/`HP≤0`: total + by `BaseClass` (Warrior/Archer/Mage/Thief); `Unspecified` in total only; §3.14. |
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
| AlertRadius | 警戒半径 | AggroMode active detect radius; alongside AttackRange (§3.14). D-074: first `MonsterSelfReviveOnDeath` revive may override instance AlertRadius from EffectParams `AlertRadius` (later revives do not change it). |
| DungeonUnlock | 副本解锁 | Save-slot unlock hook from PushMap config; dungeon gameplay **TBD** (§3.14). |
| CameraFollowMode | 镜头跟随模式 | PushMap Combat presentation: `Auto` (max projection on `CameraFollowPath`) / `Manual` (drag pan); §3.14. |
| CameraFollowPath | 镜头跟随轨 | Virtual advance polyline on the PushMap map Prefab; author start/turns/end, bake world-XZ straight samples between adjacent waypoints; camera looks at a point on the rail, not a soldier Transform; §3.14. |
| CameraPathProgress | 镜头轨进度 | Polyline arc-length `s∈[0,1]`; shared by Prepare slider / quick preview and Combat Auto; Auto = max projection of living loyal soldiers; retreats if the lead drops; §3.14. |
| PreparePathPreview | 布阵路径预览 | PushMap Prepare: optional Quick Preview reverse-sweeps `CameraFollowPath` WP_End→WP_Start; off by default; §3.14. |
| CameraPathSlider | 路径滑动条 | PushMap Prepare: left=WP_Start (s=0), right=WP_End (s=1), uniform arc-length; drag snaps FormationCamera; §3.14. |
| PrepareSpawnPreview | 布阵开战刷怪预览 | PushMap Prepare: Idle monsters from StartBattle non-trap rows for preview; destroyed then formal respawn on StartBattle; §3.14. |
| ResumeFollow | 恢复跟随 | PushMap Manual-only bottom-center button → back to `Auto`; during SearchExtract Hold → HoldFraming (not rail); §3.14 / §3.19. |
| FollowDeadzone | Follow deadzone | Auto world-XZ radius 0.15; ignore small target motion inside; §3.14. |
| FollowSmoothTime | Follow smooth time | Auto XZ SmoothDamp time 0.25s when outside deadzone; §3.14. |
| SearchExtractHoldCamera | SearchExtract hold camera | Combat camera mode after gather-point activation: viewport framing of loyal soldiers + delayed zoom-in; Size/radius clamps; §3.19. |
| HoldFraming | Hold framing | Hold presentation solver: camera-viewport AABB; expand/pan immediately on outer pad breach; slow tighten after inner-pad dwell; anchor = current Objective; §3.19. |
| DamagePopup | 伤害飘字 | PushMap floating damage text above the **hit target** after a successful hit (format `-damage`); font size **12** for both sides (monster red / soldier white); over **0.5s** world `position.z` rises relative start +0→+0.5 then despawn; §3.14. |
| CombatSkillIcon | 战斗技能图标 | PushMap Combat soldier-skill presentation: instant casts show the skill icon **35×35 screen px** above **that soldier’s head** (hold 0.6s then world +Z rise 0.3s and fade); persistent effects sit at **that soldier’s feet** at 20×20; extras stack screen-right (4px gap); icons `Resources/UI/Skills/{SkillId}`; §3.12 SkillCast / UI-025 / D-071. |
| CombatIndicator | 战斗指示器 | PushMap / SearchExtract **Combat** top-center unit-status HUD (UI-033 / D-089): center alive counts + per-unit slots (class/monster silhouette + HP% tint); low-frequency poll; dead slot removed after 0.5s; §3.14 / §3.19. |
| OffScreenSpawnHint | 离屏刷怪边缘提示 | PushMap / SearchExtract **Combat** real spawns: if the group `SpawnPoint`/`basePos` is outside the combat camera viewport, show `EnemyAttack_1` at the nearest screen edge, 2 red blinks in `OffScreenSpawnHintIntroBlinkSeconds` (default 0.4s, count `OffScreenSpawnHintIntroBlinkCount`=2), then solid hold `OffScreenSpawnHintHoldSeconds` (default 2s); display scale `OffScreenSpawnHintDisplayScale` (default 1.3); skip Prepare preview / on-screen / non-Combat; §3.14 / §3.19, UI-034 / D-090. |
| EffectKind | 技能效果种类 | Registered PascalCase token on `SkillEffectConfig` (same pattern as MagicBook `EffectPayload`); empty = unimplemented; Session/View **must not** branch on `SkillId` (§3.12, [SPEC_04 §9.21b](SPEC_04_Technical.md)). |
| TriggerHook | 技能效果钩子 | Pipeline insert-point enum (e.g. `OnOutgoingDamageSettle`); handlers register by hook (§3.12). |
| SkillEffectPipeline | 技能效果管线 | Lookup instance `SoldierSkills` → `SkillConfig.SkillEffectId` → `SkillEffectConfig` → dispatch registered handlers by `TriggerHook` (§3.12). |
| CombatStatusService | 战斗状态服务 | Unified Tick + query for Invincible / Stun / Slow / Burn DoT; separate warrior vs monster buckets (§3.12). |
| HitFlash | 受伤闪烁 | PushMap hit-flash on the target model after a successful hit; monster bright red, soldier bright white; 2×0.1s pulses back-to-back with no off gap (≈0.2s continuous); refresh if hit again mid-flash; §3.14. |
| AllyFootCircle | 友军脚下圈 | During Defend / PushMap Combat, **loyal living** soldiers show a green-stroke foot circle with black fill α=**160/255**; world radius=`BodyRadius`; localPos Y=-0.05 Z=-0.2; rotation X=**-30**; Order In Layer=`50`; follows the soldier; hide on Rebel / CombatDead; §3.12 / §3.14, [SPEC_04 §9.7](SPEC_04_Technical.md). |
| DefendPhase | 防守子状态 | In-stage phases: `ModeSelect` (if enabled) → `Prepare` → `Combat` → `Ended`. |
| StartBattle | 开战 | Prepare-phase UI button; click → `Combat` and deploy units (§3.12). |
| BattleMap | 战斗地图 | Defend-stage map; continuous walkable space (not a grid); presentation shares DigMap Isometric Tilemap via `Ground_*` (§3.12). |
| BattleProtagonist | 战斗主角 | Protagonist entity at BattleMap center; distinct from Dig `Digger`; in Defend uses **Shield** instead of HP for normal attacks (§3.12); visuals are Character Creator **baked whole characters** — [SPEC_04 §15](SPEC_04_Technical.md). |
| Shield | 护盾 | Hit-count capacity for **normal attacks** on the protagonist in Defend; on StartBattle `Shield =` current level row `ProtagonistMaxHP`; `Shield ≤ 0` → LevelFailure (§3.12). |
| Monster | 怪物 | Defend enemy unit; params in `MonsterConfig`; appear location InsideMap or OutsideMap (§3.12, [SPEC_04 §9.19](SPEC_04_Technical.md)); visuals are Character Creator **baked whole characters** (`ModelId` Prefab) — [SPEC_04 §15](SPEC_04_Technical.md). |
| MonsterConfig | 怪物配置表 | MonsterId → model/name/target select/AttackMode/`MonsterType`/AggroMode/AlertRadius/HP/move/attack power/speed/skills/loot/normal-attack·walk·run anim pools (§3.12, §3.14, [SPEC_04 §9.19](SPEC_04_Technical.md)). |
| MonsterType | 怪物类型 | `MonsterConfig` archetype tag: `1`=Normal / `2`=Elite / `3`=Boss; **not** PushMap spawn-row `IsBoss` (clear target); D-073 `Skill_08` reads `Elite` for outgoing mul ([SPEC_04 §9.19](SPEC_04_Technical.md)). |
| Wave | 波次 | Defend spawn set: all `WaveSpawnConfig` rows under one `WaveConfigId`; all rows fired + all killed is part of stage victory (§3.12). |
| WaveSpawnConfig | 刷怪波次配置表 | WaveConfigId + spawn order / remaining seconds / monster / count / location / mode (§3.12, [SPEC_04 §9.18](SPEC_04_Technical.md)). |
| WaveConfigId | 波次配置ID | Grouping key for spawn rows referenced by DefendGameplayConfig (§3.12, [SPEC_04 §9.7](SPEC_04_Technical.md)). |
| RemainingCombatSeconds | 战斗剩余秒 | Whole-second Defend combat countdown remaining; activates spawn rows when equal to `SpawnRemainingSeconds` (§3.12). |
| TargetSelect | 目标选择 | Monster targeting mode: `Nearest` / `PreferWarrior` / `PreferProtagonist` (§3.12 / `MonsterConfig`). Soldier-side “nearest” uses `NearestTargetBand`. |
| NearestTargetBand | 最近目标带 | When a monster picks soldiers: compute min legal distance `dMin`; band = `d ≤ dMin×(1+NearestTargetBandRelative)+NearestTargetBandSlack`; inside band prefer soldiers with no `TargetFocus`, then nearest; same-tier sticky on current focus. Constants: [SPEC_04 §9.20b](SPEC_04_Technical.md) (§3.12). |
| TargetFocus | 目标聚焦 | Registry of each monster’s current attack lock (`TargetFocusRegistry`); drives free-first inside the nearest band. Releasing AttackSlot in range does **not** clear focus (§3.12). |
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
| 登录主界面（UI-027） | 点击主按钮 | 无存档时文案「开始游戏」、任一槽占用时「继续游戏」；点击 → 打开存档选择（UI-001） |
| 登录主界面 | 点击「设置」 | 打开登录设置面板（UI-028）；关闭回登录主界面 |
| 登录主界面 | 点击「读取存档」「开发者介绍」 | Toast「还未制作」 |
| 登录主界面 | 版本号 | 右下只读 Text，显示 `Application.version` |
| 存档选择 | 点击「返回」 | 回到登录主界面（UI-027）；Title BGM 保持（同 Context 幂等） |
| 存档选择 | 点击空槽「新建」 | **跳过** `CampaignModeSelect`；占用该槽并以 `Mode2` 进入进档壳层 |
| 存档选择 | 点击占用槽「进入」 | **跳过** `CampaignModeSelect`；加载该槽 **Mode2** 进度并进入进档壳层；**跳过** `DifficultySelectHost`，直接打开沙盘 UI-036（默认难度 `Diff_Normal`） |
| 存档选择 | 点击占用槽「删除」 | 须二次确认后清空槽位（含两模式全部进度键），停留在存档界面 |
| 玩法模式选择 | （Demo 旁路） | UI-014 保留；本 Demo 新建/进入路径不弹出；Mode1 入口后置 |
| 进档壳层 | 默认打开 | **新建**：打开 `DifficultySelectHost`（三栏同屏）；点任意难度 → 该难度沙盘 UI-036。**进入占用档**：跳过 Hub，打开沙盘（默认 `Diff_Normal`）。玩法结束回到同一难度沙盘。困难/地狱不再 Toast |
| 进档壳层 | 悬停难度栏 | 显示该难度描述（Demo 写死） |
| 进档壳层 | 点任意难度 | 打开该 `DifficultyId` 的沙盘（UI-036）；不改玩法数值 |
| 进档壳层 | 点击浮动「工具」 | 打开 / 关闭工具面板 |
| 进档壳层 | 点击左下「装备」 | 打开装备仓只读弹窗（UI-022 / D-067） |
| 进档壳层 | 点击左下「魔法书」 | 打开魔法书 6 槽弹窗（UI-023 / D-068 / D-072）；可拖拽排序；点占用槽 → 槽下「删除」→ 二次确认后清槽 |
| 工具面板 | 点击「设置」「关卡」 | 设置→科技树；关卡→同一 DifficultySelectHost（三栏同屏） |
| 工具面板 | 点击「增加主角装备」「增加魔法书」 | 关闭 ToolsPanel → GmGrantListPanel（UI-019）；装备点行→嵌套选等级→`DebugGrantAtLevel`；魔法书点一次 `TryEquip` |
| 三玩法状态 | — | **TBD**（后续专门补充） |

### English

**Status: Partially defined (Meta shell)**

| Context | Action | Notes |
|---------|--------|-------|
| Title menu (UI-027) | Primary button | Label「开始游戏」when no occupied slot;「继续游戏」when any slot occupied; click → Save select (UI-001) |
| Title menu | Settings | Open title settings panel (UI-028); close returns to title |
| Title menu | Load save / Credits | Toast「还未制作」 |
| Title menu | Version text | Bottom-right read-only `Application.version` |
| Save select | Back | Return to title menu (UI-027); Title BGM continues (same-Context idempotent) |
| Save select | Create on empty slot | **Skip** `CampaignModeSelect`; occupy slot and enter InSaveShell as `Mode2` |
| Save select | Enter occupied slot | **Skip** `CampaignModeSelect`; load slot **Mode2** progress and enter InSaveShell; **skip** `DifficultySelectHost` and open Sandbox UI-036 (default difficulty `Diff_Normal`) |
| Save select | Delete occupied slot | Confirm, then clear slot (both modes' keys); stay on save UI |
| CampaignModeSelect | (Demo bypass) | UI-014 retained; not shown on this Demo create/enter path; Mode1 entry deferred |
| InSaveShell | Default open | **Create:** show `DifficultySelectHost` (three columns); any difficulty → that difficulty's Sandbox UI-036. **Enter occupied:** skip Hub and open the Sandbox (default `Diff_Normal`). Gameplay end returns to the same difficulty's Sandbox. Hard/Hell no longer Toast |
| InSaveShell | Hover difficulty column | Show that difficulty description (Demo hardcoded) |
| InSaveShell | Any difficulty click | Open that `DifficultyId`'s Sandbox (UI-036); no gameplay number change |
| InSaveShell | Floating Tools | Open / close ToolsPanel |
| InSaveShell | Bottom-left Equipment | Open read-only warehouse popup (UI-022 / D-067) |
| InSaveShell | Bottom-left MagicBook | Open 6-slot popup (UI-023 / D-068 / D-072); drag to reorder; click occupied slot → Delete under slot → confirm then clear |
| ToolsPanel | Settings / Level | Settings → TechTree; Level → same DifficultySelectHost (three columns same-screen) |
| ToolsPanel | Grant Protagonist Equipment / Grant MagicBook | Hide ToolsPanel → GmGrantListPanel (UI-019); equipment: pick row → nested level picker → `DebugGrantAtLevel`; MagicBook: one click `TryEquip` |
| Three gameplay states | — | **TBD** |

---

## 3.3 核心循环

### 简体中文

| 阶段 | 说明 |
|------|------|
| 1. 启动 | 进入登录主界面（UI-027）；主按钮 → 存档选择（UI-001）（非直接进局） |
| 2. Meta 存档 | 对 3 个固定槽执行新建 / 选择进入 / 删除；新建与进入**跳过** `CampaignModeSelect`，一律 `Mode2`（见 §3.4） |
| 3. 进档壳层 | **新建**进档默认打开 `DifficultySelectHost`（三栏同屏）；点任意难度进沙盘 UI-036。**进入占用档**跳过 Hub 直开沙盘（默认 `Diff_Normal`，§3.2 / §3.20）；壳层 `GameplayState` 占位仍为 Dig；显示浮动「工具」与左下「装备」「魔法书」（§3.5）；运行时 CSV 根为 Mode2（`ConfigTables/Mode2/Csv`） |
| 4. 玩法状态 | 当前状态以占位表现可识别；关卡内由阶段玩法类型驱动（§3.9）；壳层内手动切换 **TBD** |
| 5. 关卡 | 规则见 §3.9；按 `LevelOperationConfig` 驱动真实阶段（§3.8 D-010）；工具「关卡」打开列表选关（UI-008） |

交叉引用：[SPEC_02 §3](SPEC_02_GameOverview.md)。

### English

| Stage | Description |
|-------|-------------|
| 1. Boot | Open title menu (UI-027); primary button → save select (UI-001) (not direct into gameplay) |
| 2. Meta saves | Create / enter / delete on 3 fixed slots; create/enter **skip** `CampaignModeSelect`, always `Mode2` (§3.4) |
| 3. InSaveShell | **Create:** default `DifficultySelectHost` (three columns); any difficulty → Sandbox UI-036. **Enter occupied:** skip Hub → Sandbox (default `Diff_Normal`, §3.2 / §3.20); shell `GameplayState` placeholder remains Dig; show floating Tools and bottom-left Equipment / MagicBook (§3.5); runtime CSV root is Mode2 (`ConfigTables/Mode2/Csv`) |
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
| 空槽 | 可「新建」→ **跳过** `CampaignModeSelect` → 以 `Mode2` 标记占用并进入进档壳层 |
| 占用槽 | 可「选择进入」（同样跳过弹窗、直进 Mode2）或「删除」 |
| 玩法模式 | **本 Demo：** 新建/进入一律 `Mode2`；UI-014 保留、进档路径不调用；Mode1 入口后置 |
| 同槽隔离 | Mode1 与 Mode2 的士兵池 / 布阵 / 副本解锁等进度键**完全隔离**；`Occupied` 按槽共享（任一模式玩过即占用） |
| 删除 | **必须二次确认**；确认后槽变空并清除**两模式**全部进度键；不可恢复（本版） |
| 持久化 | 本地、按槽索引 + `CampaignMode`；至少持久化「是否占用」。**本片已锁定：** 士兵可上阵池（`WarriorPool`）+ 战斗布阵（`BattleFormation`）+ **关卡路线进度（`LevelRouteProgress`：已通关 `GameplayOptionId`）** 随槽**与模式**读写（见 [SPEC_04 §6](SPEC_04_Technical.md)）；其余字段（仓库 / 经验 / 科技等）schema 仍 **TBD** |
| Mode2 合成 | Mode2 士兵制造 = **自动制造**（§3.15）；Mode2 UM 关闭手动制造 |

**新建档初始资源**

| 规则 | 说明 |
|------|------|
| 触发 | **新建**进档（非进入已有档；本 Demo 在旁路 Mode2 进壳时发放） |
| 发放顺序 | ① 精魂 → ② 主角装备仓 → ③ 魔法书槽（均在 Service 已 `BindSlot` 且 CSV 已加载之后） |
| 精魂 | 向当前存档仓库入账 `ItemId=Spirit`，数量 ← **`CombatConstantConfig.NewSaveInitialSpiritCount`**（样例 **30**）；`Value ≤ 0` 不发放 |
| 装备 | 解析 **`CombatConstantConfig.NewSaveInitialEquipments`**（文本 Value）：`EquipId;Level\|EquipId;Level`；空串不发；`Level≥1` 且表内有行才入仓（`GrantAtLevel`，`CurrentExp=0`）；非法段 Warning 跳过；同 `EquipId` **先出现为准**（后者跳过） |
| 魔法书 | 解析 **`CombatConstantConfig.NewSaveInitialMagicBooks`**（文本 Value）：`MagicBookId\|MagicBookId`；空串不发；左→右依次 `TryEquip`；非法 / Unique 冲突 / 槽满 → Warning 跳过该项并继续 |
| 边界 | 进入已有档不重复发放；编码与加载见 [SPEC_04 §9.20b](SPEC_04_Technical.md) |
| Demo | 仓库/精魂尚未持久化；装备仓与魔法书槽已持久化；本规则保证新建档首次进壳即可消费精魂并拥有配置的初始装备/书 |

**槽位展示（最小）**

| 字段 | 要求 |
|------|------|
| 槽号 | 必须（1–3 或 0–2，UI 一致即可） |
| 是否占用 | 必须 |
| 显示名 / 时间戳 | 可选；未定时标 TBD |

**背景音乐（BGM）**

| 规则 | 说明 |
|------|------|
| 配置 | 曲目池由表 `Audio_BgmConfig` 驱动（[SPEC_04 §9.29](SPEC_04_Technical.md)）；同 `Context` 多行按 `Weight` 加权随机选 1 首；`Loop` 默认循环（可设只播一遍） |
| Title | 登录主界面（`TitleMenu` / UI-027）**与**存档选择（`SaveSelect` / UI-001）播放 `Context=Title`；共享 `TitleScreenBackground` |
| Dig | Dig 阶段 Enter 播放 `Context=Dig`；Exit / 离开关卡 → 停止 |
| Combat | **Defend Combat** 与 **PushMap Combat** 共用 `Context=Combat` 池；Prepare / Ended / 非战斗阶段 → 停止 |
| 静音 | 进档壳、Shop / AutoManufacture / UM、Defend·PushMap Prepare、结算后回壳等非上列时机 **不播 BGM**（`Stop`） |
| 幂等 | 同一 `Context` 已在播放时不重抽、不重开；离开该 Context 后再进入才重新随机 |

### English

**Slot rules**

| Rule | Value |
|------|-------|
| Slot count | Fixed **3** (indices 0, 1, 2) |
| Empty | Create → **skip** `CampaignModeSelect` → mark occupied as `Mode2` and enter InSaveShell |
| Occupied | Enter (also skip popup → Mode2) or Delete |
| CampaignMode | **This Demo:** create/enter always `Mode2`; UI-014 retained but unused on enter path; Mode1 entry deferred |
| Per-slot isolation | Mode1 vs Mode2 WarriorPool / BattleFormation / DungeonUnlocks keys are **fully isolated**; `Occupied` is shared per slot |
| Delete | **Confirm required**; slot becomes empty and **both modes'** progress keys cleared; no undo (this version) |
| Persistence | Local, by slot index + `CampaignMode`; at least occupied flag. **This slice locked:** deployable soldier pool (`WarriorPool`) + `BattleFormation` + **level route progress (`LevelRouteProgress`: cleared `GameplayOptionId`s)** read/write per slot **and mode** ([SPEC_04 §6](SPEC_04_Technical.md)); other fields (Warehouse / Exp / Tech, …) schema still **TBD** |
| Mode2 manufacture | Mode2 soldier craft = **AutoManufacture** (§3.15); Mode2 UM hides manual manufacture |

**Initial resources (new save)**

| Rule | Notes |
|------|-------|
| When | **Create** enter (not enter existing slot; this Demo grants on Mode2 bypass EnterShell) |
| Order | ① Spirit → ② protagonist equipment warehouse → ③ MagicBook slots (after Services `BindSlot` and CSV loaded) |
| Spirit | Credit warehouse `ItemId=Spirit`; count ← **`CombatConstantConfig.NewSaveInitialSpiritCount`** (sample **30**); no grant when `Value ≤ 0` |
| Equipment | Parse **`CombatConstantConfig.NewSaveInitialEquipments`** (text Value): `EquipId;Level\|EquipId;Level`; empty = none; grant only when `Level≥1` and config row exists (`GrantAtLevel`, `CurrentExp=0`); illegal segment → Warning skip; duplicate `EquipId` → **first wins** (later skipped) |
| MagicBook | Parse **`CombatConstantConfig.NewSaveInitialMagicBooks`** (text Value): `MagicBookId\|MagicBookId`; empty = none; left→right `TryEquip`; unknown / Unique conflict / full → Warning skip that id and continue |
| Boundary | No repeat grant on enter existing slot; encoding/load: [SPEC_04 §9.20b](SPEC_04_Technical.md) |
| Demo | Warehouse/Spirit not yet persisted; equipment warehouse and MagicBook slots are persisted; ensures new save has Spirit plus configured starter gear/books on first InSaveShell |

**Minimal display**

| Field | Requirement |
|-------|-------------|
| Slot id | Required |
| Occupied | Required |
| Display name / timestamp | Optional; TBD if unused |

**Background music (BGM)**

| Rule | Notes |
|------|-------|
| Config | Track pools driven by `Audio_BgmConfig` ([SPEC_04 §9.29](SPEC_04_Technical.md)); multiple rows per `Context` → weighted-random pick one by `Weight`; `Loop` defaults to loop (can play once) |
| Title | Title menu (`TitleMenu` / UI-027) **and** save select (`SaveSelect` / UI-001) play `Context=Title`; shared `TitleScreenBackground` |
| Dig | Dig stage Enter plays `Context=Dig`; Exit / leave level → stop |
| Combat | **Defend Combat** and **PushMap Combat** share `Context=Combat` pool; Prepare / Ended / non-combat → stop |
| Silence | InSaveShell, Shop / AutoManufacture / UM, Defend·PushMap Prepare, post-settlement shell, etc. play **no** BGM (`Stop`) |
| Idempotent | While the same `Context` is already playing, do not re-roll or restart; re-enter after leave re-rolls |

---

## 3.5 工具面板

### 简体中文

| 规则 | 说明 |
|------|------|
| 可见时机 | 仅在进档壳层常驻浮动「工具」按钮 |
| 打开 / 关闭 | 点击按钮切换工具面板 |
| 进档壳左下入口 | 左下 `BackButton`（「返回存档」）正上方竖排：**上「商店」、中「装备」、下「魔法书」、最下「返回存档」**。各 **160×48**，间距 **8**；Mode1/Mode2 均显示「返回存档」；商店为 Mode2（见下方规则）。点「商店」→ UI-026；点「装备」→ UI-022；点「魔法书」→ UI-023。弹窗/全屏对齐 UI-008 的遮罩风格（全屏遮罩 + 中框/全屏框 + 关闭）；`sortingOrder` ≥ 100 以盖住 AM 演出。Tools GM（UI-019）与 Dig HUD GM **保留**。 |
| 本期条目 | **设置**（含科技树画布入口，见 §3.13 / UI-012）、**关卡**（打开关卡列表，见 UI-008）、**商店**（Mode2 全屏商店：开放/自动刷新/刷新价格递进/购买扣精魂）、**增加主角装备**、**增加魔法书**（Demo GM，见 UI-019 / D-061）、**添加士兵**（Demo GM，见 UI-020 / D-064） |
| 关卡语义 | 工具「关卡」入口 **不等于** 直接切换三种 `GameplayState`。**新建进档** / 点击「关卡」→ 打开 **DifficultySelectHost**（三栏等宽同屏，各约 1/3；悬停显示难度描述）：**任意难度**点击 → 沙盘 UI-036（§3.20），**不**再打开 `LevelRouteSelectRoot`（UI-031）。**进入占用档** → **跳过** Hub，打开沙盘（默认 `Diff_Normal`）。从沙盘进入的挖坟 / 商店 / 自动造兵结束后回到**同一难度**沙盘，不调用 `TryAdvanceStage`。旧 `LevelEnded` 同样回到最近沙盘难度（缺省 `Diff_Normal`）。困难/地狱不再 Toast。无栏内 LevelSelect；无中央 MapHost。UI-031 与子关卡路线**保留**，不从这些入口进入。**规则已定义、Demo 接线后置：** 难度栏描述/解锁/通关奖取自 `DifficultyConfig`（§3.9）；当前难度栏文案仍写死。 |
| Demo GM：增加主角装备 | 点击 → 关闭 ToolsPanel → 打开 **GmGrantListPanel**：列出当前模式 `ProtagonistEquipmentConfig` **按 EquipId 去重**（取 Level 1 行 `DisplayName`，空则 Id）。点行 → **嵌套 LevelPicker**（该 EquipId 全部 `EquipLevel` 升序按钮，文案 `Lv.{n}`）→ `ProtagonistEquipmentService.DebugGrantAtLevel(equipId, level)`（未拥有则入仓该级 `CurrentExp=0`；已拥有则覆盖 `Level` 且 `CurrentExp=0`）。成功/失败 Toast + 日志；关 LevelPicker；**列表保持打开**。Dig HUD「获得铁铲/矿灯/炸药」仍 `TryAcquire`。 |
| Demo GM：增加魔法书 | 点击 → 关闭 ToolsPanel → 同一 **GmGrantListPanel**：列出当前模式 `MagicBookConfig` 全表（`DisplayName`，空则 Id）。点一次 → `SpecialEquipSlotsService.TryEquip(magicBookId)`（装入第一个空槽；**无**独立仓库）。`IsUnique=1` 已装或 6 槽满 → 失败 Toast。Dig HUD Mode2 `GmMenuPanel` 魔法书全表 GM **保留**。 |
| Demo GM：添加士兵 | 点击 → 关闭 ToolsPanel。**可用门闩（方案 A）：**（1）UM「布阵」编辑器已打开（`FormationEditorMode.UpgradeManufacture`，Mode1/Mode2 均可）；或（2）`CampaignMode=Mode2` 且任一战斗 Prepare 布阵已打开（`FormationEditorRoot_Mode2`：Defend / PushMap / SearchExtract Prepare）。否则 Toast「请先打开布阵界面」、不打开面板。**Mode1** Defend Prepare **仍不可用**。可用时打开左侧 **GmAddSoldierPanel**（UI-020）：职业下拉=`ClassConfig` 全表；种族下拉=`RaceConfig` 全表；数量输入（默认 1，钳制 1～999）；「自动上阵」默认勾选；底「关闭」「添加」。「添加」**不关面板**：在当前模式 `BodyAppearanceConfig` 中查找 `RaceId` 精确匹配 **且** `ClassAffinity` 含该职业 `ClassName`（`|` 分隔，与制造亲和一致）的外观行；**无匹配** → Toast「找不到此种士兵！」且不入池（**不**回退 `DefaultAppearanceId`）。**多条匹配不得均匀随机**：优先匹配集内 `AppearanceId` 等于该职业 `DefaultAppearanceId` 的行；否则 `AppearanceLevel` 等于该职业 `ClassLevel` 的行；再否则取表内首次出现。有匹配 → 不耗材料/精魂，由 `GmSoldierGrantService` 按 Demo 固定 `BaseStats` + 职业/种族行构造实例入 `WarriorPool`（授予 `DefaultSkillIds`@Lv1；若已装备 `GrantFormationSkill` 书则 **仅**应用该 Token，不跑 `StatMul`/`ForceClass`/`SoldierSkillLevelAdd`/`RaceWeightPick`）；若勾选自动上阵 → 对本批 Id 调 `AutoFormationDeployService.DeployBatch`（缺职业区则留池，不弹「找不到士兵」；zones 取自当前打开的布阵宿主）。 |
| Demo Debug：士兵任务标签 | 进档壳 **Debug** 区提供开关（**默认开**）：Defend / PushMap / SearchExtract Combat 中士兵脚下 TextMesh 显示当前 `GoalKind` 中文简标（推进 / 回阵 / 追击 / 追击锚 / 阵型）+ **`(有效移速)`**（与位移同口径，含追击倍率等）；仅目标类，不含攻击前摇等细态；见 [SPEC_04 §9.7](SPEC_04_Technical.md) |
| 后续条目 | 装备升级 / 划入 `EquipCommonExp` / 卸下 UI、魔法书从弹窗装入，其余 TBD。装备仓只读见 D-067；魔法书排序见 D-068；弹窗删除见 D-072；GM 见 D-061 / D-064（P1） |

点击「设置」：进入设置页并承载科技树画布（§3.13）；其它设置项清单仍 **TBD**；科技树画布完整验收本版可选后置。点击「关卡」：关闭工具面板 → 打开 LevelSelectPanel → 点选进入对应关卡 Stage 1。点击「商店」：打开 Mode2 全屏商店（UI-026；**局外 overlay**；商店开放门闩见下方规则）。点击「增加主角装备」：关闭工具面板 → GmGrantListPanel → 点行选等级后 `DebugGrantAtLevel`。点击「增加魔法书」：关闭工具面板 → GmGrantListPanel → 点一次 `TryEquip`。点击「添加士兵」：UM 布阵打开，或 Mode2 下 Defend/PushMap/SearchExtract Prepare 布阵打开时 → GmAddSoldierPanel。点击进档壳左下「装备」/「魔法书」：打开对应居中 Modal（不关 ToolsPanel）。

### Mode2 商店系统（本片规则）

1) 双入口与全屏 UI
- 仅在 `CampaignMode=Mode2` 时「商店」按钮有意义。
- **关卡阶段入口：** Mode2 样例运作表 Stage1 `GameplayType=Shop`。`ShopStageModule` Instantiate 全屏 Prefab `Assets/Prefabs/Shop/ShopStageRoot.prefab`（内容区 stretch 铺满，**不是** 980×650 居中弹窗）。阶段内点关闭/继续 → **无独立阶段结算** → `LevelOperationDriver.TryAdvanceStage`（进入 Dig）。
- **局外 overlay 入口：** InSaveShell 左下「商店」在关卡未运行、或当前阶段 **不是** Shop 时，Instantiate **同一** Prefab 盖在壳上。关闭销毁实例并返回 InSaveShell，**不**推进关卡。若当前关卡已处于 Shop 阶段（全屏已开），按钮 **no-op**。
- 打开后（首次渲染前）先调用 `ShopOfferRefreshService.TryEnsureOffersIfAllEmpty(progress, configs, activeLevelNumberFloor)`（方案 B：6 槽 `itemId` 皆空时补一次货），再调用 `TryAutoRefreshOnceIfPending` 消化仍残留的 pending（若解锁回调已消化则不会重复生成）。
- 布局：左侧“玩家信息”、右侧“待售商品信息”，按当前 `ShopProgress` 显示精魂总值、装备栏、魔法书栏、以及 6 项待售商品。左侧保留 `EquipSummaryText` / `MagicBookSummaryText`（`装备：n/6`、`魔法书槽：n/6`），其下分别展示已拥有装备图标与已装备魔法书图标（见第 6 条）。
- `ShopProgress` 必须持久化；下次打开仍可复用当前商品栏（直到下一次新关卡解锁触发自动刷新一次）。

2) 商店开放规则与自动刷新
- 商店“可打开”的门闩：`CampaignMode=Mode2` 时按钮可用；**新的关卡解锁**仍会置 `pendingOpenOnNewUnlock=true` 并由 `TryAutoRefreshOnceIfPending` 生成/重置；另增 **空栏保底（方案 B）**：任一入口打开商店时，若当前 6 槽 `itemId` **全部为空**（含新建档、未通关过推图即进路线商店），则调用 `TryEnsureOffersIfAllEmpty`：
  - `floor = max(1, activeLevelNumberFloor)`（关卡阶段入口：当前 `LevelId` 数字后缀；局外 overlay：有 `ActiveLevelId` 则取其后缀，否则 `0`→floor=1）
  - 若 `maxUnlockedLevelNumber < floor`：走 `OnLevelCleared(floor)`（抬门槛、清栏、pending=true、refreshCount=0）
  - 若门槛已够但仍全空：仅 `ForcePendingOpenForEmptyOffers`（不抬门槛、不清栏）
  - 再 `TryAutoRefreshOnceIfPending` 生成 6 项；**任一槽已有 `itemId`（含已售）则不重刷**
- 本片将“新的关卡解锁”定义为：玩家最高通过关卡进度（由 `Level_LevelOperationConfig` 的 `LevelId` 数字后缀映射得到）从 `maxUnlockedLevelNumber` 提升到更高值 `newMaxUnlockedLevelNumber` 时。
- 当 `newMaxUnlockedLevelNumber > maxUnlockedLevelNumber`：
  - 更新 `ShopProgress.maxUnlockedLevelNumber = newMaxUnlockedLevelNumber`
  - 重置 `currentRefreshCount = 0`
  - 置 `pendingOpenOnNewUnlock=true`（由 `ShopProgressService.OnLevelCleared` 完成）
  - 通过 `ShopOfferRefreshService.TryAutoRefreshOnceIfPending(progress, configs)` 生成一次新的 6 项待售商品并写回 `ShopProgress.currentOffers`（pending 被清除后不会重复生成）
  - 之后的商品“刷新价格递进”和“手动刷新次数”从 0 开始。

变更原因/影响：路线上第一次进商店（推图通关前）即可按 `Pool_01`（门槛 1）出货；生成仍只走 pending→`TryAutoRefreshOnceIfPending`，避免与已有货/已售栏冲突。

3) 刷新商品按钮与价格递进
- `ShopRefreshPriceConfig.csv` 行字段：`RefreshCount` 与 `RefreshPrice`。
- 当当前 `currentRefreshCount = x`（初始 0）时，点击一次「刷新商品」：
  - 读取 `RefreshCount = x+1` 对应行得到 `RefreshPrice`
  - 支付该 `RefreshPrice`（扣精魂）
  - 成功后 `currentRefreshCount++`，并按商品生成算法重新生成 6 项待售商品
- 若配置表缺少 `RefreshCount=x+1` 行，则「刷新商品」不可点击（不改变商品栏）。

4) 待售商品生成算法（严格按你的流程）
- 输入：`highestClearedMaxLevelNumber` 来自 `ShopProgress.maxUnlockedLevelNumber`。
- 步骤1：读取 `Shop_ShopPoolConfig.csv`，根据 `RequiredMaxLevelNumber` 与（预留的）`ExtraUnlockCondition` 找出满足条件的 `ShopPoolId` 列表。
- 步骤2：遍历所有已解锁的 `ShopPoolId`，把每行的 `PoolItemsRaw` 拆分并按 `归类` A/B 汇总到临时集合：
  - 归类 A：A 类装备候选（itemId）
  - 归类 B：B 类魔法书候选（itemId）
- 步骤3：在每个归类内执行“同道具权重相加”，即对相同 `itemId` 的出现权重做求和；然后对每个归类随机抽取 3 个不同的 `itemId` 生成当前的 3 项待售商品。
- 如果某归类可抽的不同 `itemId` 数量不足 3，则只填充已有数量，剩余 slot 为空，不补齐。

5) 购买闭环（点击购买）
- 一次购买的输入：当前 slot 的 `itemId`、`category`（A|B）、以及 `priceSpirit`（精魂价格）。
- 校验：玩家 `SpiritEssence >= priceSpirit` 且该 slot 未标记为 sold。
- 支付：扣除 `priceSpirit`。
- 入账/入仓：
  - A 类（装备）：调用 `ProtagonistEquipmentService.TryAcquire(itemId)`
  - B 类（魔法书）：调用 `SpecialEquipSlotsService.TryEquip(itemId)`
- 更新 slot：标记为已售并清空/禁用该 slot 的购买入口；不自动触发刷新（只能由「刷新商品」或下一次“新关卡解锁自动刷新”改变商品栏）。

6) 已拥有展示与出售闭环（D-076）
- 装备图标：读取 `ProtagonistEquipmentService.OwnedEquips`（每种 `EquipId` 至多 1 件，最多 6）；魔法书图标：读取 `SpecialEquipSlotsService` 6 槽，空槽不显示可点图标。
- 图标优先 `Item_ItemCatalogConfig.IconAssetId`；缺则回退 `ProtagonistEquipmentConfig` / `MagicBookConfig` 的 `IconAssetId`。加载：仅文件名时装备走 `Resources/UI/Equipment/{IconAssetId}`、魔法书走 `Resources/UI/MagicBooks/{IconAssetId}`（与 UI-022 / UI-023 同目录）；含 `/` 则按 Resources 相对路径直载；缺图空框，名称/价格仍显示。
- 点击占用图标：该图标正下方出现「出售」按钮与 `Item_ItemCatalogConfig.SellPrice`（获得精魂）；再点同一图标收起；点另一图标切换；空槽不出现。
- 点击「出售」须 `ConfirmDialog`（文案含道具名与获得精魂）；商店 Canvas `sortingOrder` ≥ 200，确认框 `overrideSorting` ≥ 201。取消则库存与精魂不变。
- 确认后由 `ShopSellService` 执行：先校验 catalog 行且 `SellPrice ≥ 0`（缺行或负值失败 Toast、不移除）；再移除道具；成功后 `Warehouse.AddSpirit(SellPrice)`。`SellPrice=0` 允许出售（入账 0）。
- 装备：`ProtagonistEquipmentService.TryRemove` 整件删除；**不**退 `CurrentExp` / `EquipCommonExp`；`Changed` 触发 Dig caps 重算。UI-022 仓只读弹窗**不**提供出售。
- 魔法书：`SpecialEquipSlotsService.TryUnequip(slotIndex)`（不补位、无仓库=从当前存档删除）。与 UI-023 删除的差异是商店会发精魂。
- 购买仍可用 catalog `SellPrice` 作为待售 `priceSpirit`（本片不拆买卖价字段）。

### English

| Rule | Notes |
|------|-------|
| Visibility | Floating Tools only inside InSaveShell |
| Open / close | Toggle ToolsPanel via button |
| InSaveShell bottom-left | Above `BackButton` ("Return to saves"), vertical stack: **Shop (top, Mode2), Equipment (next), MagicBook (next), Back (bottom)**. Each **160×48**, gap **8**; Mode1/Mode2 show Back button; Shop enabled for Mode2 (see rules below). Shop → UI-026; Equipment → UI-022; MagicBook → UI-023. Popups/full-screen align with UI-008 dim style (full-screen dim + center/full-screen frame + close); `sortingOrder` ≥ 100 to cover AM presentation. Tools GM (UI-019) and Dig HUD GM **kept**. |
| This version | **Settings** (hosts TechTree canvas, §3.13 / UI-012), **Level** (opens level list, UI-008), **Shop** (Mode2 full-screen shop: unlock/open + auto refresh + refresh price progression + buy with Spirit cost), **Grant Protagonist Equipment**, **Grant MagicBook** (Demo GM, UI-019 / D-061), **Add Soldier** (Demo GM, UI-020 / D-064) |
| Level meaning | Tools Level entry is **not** a direct three-state switch. **Create enter** / Level click → **DifficultySelectHost** (equal-width same-screen columns ~1/3 each; hover shows difficulty description): **any difficulty** click → Sandbox UI-036 (§3.20), and does **not** open `LevelRouteSelectRoot` (UI-031). **Enter occupied** → **skip** Hub and open the Sandbox (default `Diff_Normal`). Dig / Shop / AutoManufacture entered from the Sandbox return to the **same difficulty** Sandbox and do not call `TryAdvanceStage`. Legacy `LevelEnded` also returns to the last Sandbox difficulty (default `Diff_Normal`). Hard/Hell no longer Toast. No in-column LevelSelect; no central MapHost. UI-031 and the SubLevel route **remain**, and are not opened from these entries. **Rules defined, Demo wiring deferred:** Hub description/unlock/clear reward come from `DifficultyConfig` (§3.9); difficulty column copy is still hardcoded. |
| Demo GM: Grant Protagonist Equipment | Click → hide ToolsPanel → **GmGrantListPanel**: distinct `EquipId` from current-mode `ProtagonistEquipmentConfig` (Level 1 `DisplayName`, else Id). Pick a row → nested **LevelPicker** (all `EquipLevel` rows for that Id, ascending, label `Lv.{n}`) → `ProtagonistEquipmentService.DebugGrantAtLevel(equipId, level)` (not owned → add at that level `CurrentExp=0`; owned → overwrite `Level` and `CurrentExp=0`). Success/fail Toast + log; close LevelPicker; **list stays open**. Dig HUD Grant Iron Shovel / Miner Lamp / Explosives still `TryAcquire`. |
| Demo GM: Grant MagicBook | Click → hide ToolsPanel → same **GmGrantListPanel**: all current-mode `MagicBookConfig` rows (`DisplayName`, else Id). One click → `SpecialEquipSlotsService.TryEquip(magicBookId)` (first empty slot; **no** warehouse). Unique already equipped or 6 slots full → fail Toast. Dig HUD Mode2 `GmMenuPanel` full MagicBook GM **kept**. |
| Demo GM: Add Soldier | Click → hide ToolsPanel. **Open gate (Approach A):** (1) UM Formation editor open (`FormationEditorMode.UpgradeManufacture`, Mode1/Mode2); or (2) `CampaignMode=Mode2` and any combat Prepare formation open (`FormationEditorRoot_Mode2`: Defend / PushMap / SearchExtract Prepare). Else Toast「请先打开布阵界面」and do not open panel. **Mode1** Defend Prepare **still blocked**. When allowed → left **GmAddSoldierPanel** (UI-020): class dropdown = full `ClassConfig`; race dropdown = full `RaceConfig`; count input (default 1, clamp 1–999); Auto-deploy default on; bottom Close / Add. Add **keeps panel open**: find current-mode `BodyAppearanceConfig` rows with exact `RaceId` **and** `ClassAffinity` containing that class `ClassName` (`|`-split, same as manufacture affinity); **no match** → Toast「找不到此种士兵！」and no pool add (**no** `DefaultAppearanceId` fallback). **If several rows match, do not pick uniformly at random**: prefer the match whose `AppearanceId` equals that class's `DefaultAppearanceId`; else `AppearanceLevel` equals `ClassLevel`; else first table order. On match → no material/Spirit cost; `GmSoldierGrantService` builds instances with Demo fixed `BaseStats` + class/race rows into `WarriorPool` (`DefaultSkillIds`@Lv1; if a `GrantFormationSkill` book is equipped, apply **that token only** — not `StatMul`/`ForceClass`/`SoldierSkillLevelAdd`/`RaceWeightPick`); if Auto-deploy → `AutoFormationDeployService.DeployBatch` for batch Ids (missing class zone → leave in pool; not「找不到士兵」; zones from the open formation host). |
| Demo Debug: soldier task label | InSaveShell **Debug** toggle (**default on**): during Defend / PushMap / SearchExtract Combat, TextMesh under each soldier shows current `GoalKind` short ZH label (advance / home / chase / chase-anchor / formation) + **`(effective move speed)`** (same value as displacement, incl. chase mult); goal-kind only — no attack windup detail; see [SPEC_04 §9.7](SPEC_04_Technical.md) |
| Future entries | Equipment level-up / spend `EquipCommonExp` / unequip UI, MagicBook grant-from-popup, other TBD. Warehouse read-only = D-067; MagicBook reorder = D-068; popup delete = D-072; GM = D-061 / D-064 (P1) |

Settings click → Settings page hosting TechTree canvas (§3.13); other settings items still **TBD**; full TechTree canvas acceptance optional this Demo. Level click → hide Tools → LevelSelectPanel → pick enters that level at Stage 1. Shop click → open Mode2 full-screen shop as an **out-of-level overlay** (UI-026; open-gate rules below). Grant Equipment → hide Tools → GmGrantListPanel → pick row then level → `DebugGrantAtLevel`. Grant MagicBook → hide Tools → GmGrantListPanel → one click `TryEquip`. Add Soldier → when UM Formation open, or Mode2 Defend/PushMap/SearchExtract Prepare formation open → GmAddSoldierPanel. InSaveShell bottom-left Equipment / MagicBook → open the matching centered Modal (does not hide ToolsPanel).

### Mode2 Shop System (This Slice Rules)

1) Dual entry & full-screen UI
- Shop button is meaningful only for `CampaignMode=Mode2`.
- **Level-stage entry:** Mode2 sample LevelOperation Stage1 `GameplayType=Shop`. `ShopStageModule` instantiates full-screen Prefab `Assets/Prefabs/Shop/ShopStageRoot.prefab` (content stretch-fills the screen; **not** a 980×650 centered dialog). In-stage Close/Continue → **no independent stage settlement** → `LevelOperationDriver.TryAdvanceStage` (enters Dig).
- **Out-of-level overlay entry:** InSaveShell bottom-left Shop, when no Level is running **or** the current stage is **not** Shop, instantiates the **same** Prefab over the shell. Close destroys the instance and returns to InSaveShell without advancing the Level. If the Shop stage is already open, the button is a **no-op**.
- After open (before first render) call `ShopOfferRefreshService.TryEnsureOffersIfAllEmpty(progress, configs, activeLevelNumberFloor)` (Approach B: seed once when all 6 slots have empty `itemId`), then `TryAutoRefreshOnceIfPending` for any remaining pending (no second generate if unlock callback already consumed it).
- Layout: left “Player Info”, right “Shop Offers”, driven by current `ShopProgress`. Left keeps `EquipSummaryText` / `MagicBookSummaryText` (`装备：n/6`, `魔法书槽：n/6`) and, below each, owned equipment icons and equipped MagicBook icons (see rule 6).
- Persist `ShopProgress` so offers remain until the next “new level unlock” auto refresh.

2) Shop open rule & auto refresh
- Shop button is enabled for `CampaignMode=Mode2`. **New level unlock** still sets `pendingOpenOnNewUnlock=true` and is consumed by `TryAutoRefreshOnceIfPending`. **Empty-shelf ensure (Approach B):** on either shop entry, if all 6 offer `itemId`s are empty (new save / route Shop before any PushMap clear), call `TryEnsureOffersIfAllEmpty`:
  - `floor = max(1, activeLevelNumberFloor)` (stage entry: trailing digits of current `LevelId`; overlay: `ActiveLevelId` suffix if any, else `0`→floor=1)
  - if `maxUnlockedLevelNumber < floor`: `OnLevelCleared(floor)` (raise gate, clear, pending=true, refreshCount=0)
  - else if still all-empty: `ForcePendingOpenForEmptyOffers` only (no raise/clear)
  - then `TryAutoRefreshOnceIfPending`; **do not re-roll if any slot already has an `itemId` (including sold)**
- In this slice, “new level unlock” is when the player’s highest cleared level progress (derived from numeric suffix of `LevelId` in `Level_LevelOperationConfig`) increases from `maxUnlockedLevelNumber` to `newMaxUnlockedLevelNumber`.
- When `newMaxUnlockedLevelNumber > maxUnlockedLevelNumber`:
  - update `ShopProgress.maxUnlockedLevelNumber`
  - reset `currentRefreshCount=0`
  - set `pendingOpenOnNewUnlock=true` (done by `ShopProgressService.OnLevelCleared`)
  - generate one fresh set of 6 offers and store to `ShopProgress.currentOffers` via `ShopOfferRefreshService.TryAutoRefreshOnceIfPending(progress, configs)` (no repeat after pending is cleared)

3) Refresh button pricing progression
- `ShopRefreshPriceConfig.csv` provides (`RefreshCount`, `RefreshPrice`).
- When `currentRefreshCount=x` (initial 0), one manual refresh:
  - pays `RefreshPrice` from `RefreshCount=x+1`
  - then `currentRefreshCount++` and re-generates offers
- If `RefreshCount=x+1` row is missing: refresh button is disabled.

4) Offer generation algorithm (strictly following your flow)
- Input: `highestClearedMaxLevelNumber = ShopProgress.maxUnlockedLevelNumber`.
- Step1: read `Shop_ShopPoolConfig.csv`, find unlocked `ShopPoolId` by `RequiredMaxLevelNumber <= highestClearedMaxLevelNumber` (+ reserved `ExtraUnlockCondition`).
- Step2: for all unlocked pools, parse `PoolItemsRaw` into temporary A/B candidates; accumulate `byItemIdTotalWeight` later.
- Step3: within each category (A and B), sum weights for identical `itemId`, then randomly pick 3 distinct `itemId`.
- If a category can pick fewer than 3 distinct ids: fill only available offers; remaining slots stay empty.

5) Purchase close-loop (click to buy)
- Validate Spirit cost (`SpiritEssence >= priceSpirit`) and slot not sold.
- Pay `priceSpirit`.
- Grant:
  - category A: `ProtagonistEquipmentService.TryAcquire(itemId)`
  - category B: `SpecialEquipSlotsService.TryEquip(itemId)`
- Mark slot sold/empty and disable purchase for that slot; do not auto-trigger refresh.

6) Owned display & sell close-loop (D-076)
- Equipment icons: `ProtagonistEquipmentService.OwnedEquips` (at most one per `EquipId`, max 6). MagicBook icons: 6 `SpecialEquipSlotsService` slots; empty slots are not clickable icons.
- Icon: prefer `Item_ItemCatalogConfig.IconAssetId`; else fall back to `ProtagonistEquipmentConfig` / `MagicBookConfig` `IconAssetId`. Load: filename-only equipment → `Resources/UI/Equipment/{IconAssetId}`, MagicBook → `Resources/UI/MagicBooks/{IconAssetId}` (same folders as UI-022 / UI-023); if the id contains `/`, load as a Resources-relative path; missing sprite = empty frame, name/price still shown.
- Click occupied icon → “Sell” + `Item_ItemCatalogConfig.SellPrice` (Spirit gained) under that icon; click same again to hide; click another to switch; empty slots never show it.
- Sell requires `ConfirmDialog` (name + Spirit gain). Shop Canvas `sortingOrder` ≥ 200; confirm overlay `overrideSorting` ≥ 201. Cancel leaves inventory and Spirit unchanged.
- On confirm `ShopSellService`: require catalog row with `SellPrice ≥ 0` (missing/negative → Toast, no remove); then remove; then `Warehouse.AddSpirit(SellPrice)`. `SellPrice=0` is allowed (gain 0).
- Equipment: `TryRemove` deletes the whole piece; **no** refund of `CurrentExp` / `EquipCommonExp`; `Changed` recalcs Dig caps. UI-022 warehouse stays read-only (no sell there).
- MagicBook: `TryUnequip(slotIndex)` (no compact; no warehouse = delete from save). Unlike UI-023 delete, shop sell credits Spirit.
- Shop buy may still use catalog `SellPrice` as offer `priceSpirit` (this slice does not split buy/sell columns).

---

## 3.6 UI 清单

### 简体中文

| ID | 名称 | 状态 | 说明 |
|----|------|------|------|
| UI-001 | 存档选择 | 已定义（Demo） | 3 槽：新建 / 进入 / 删除（含确认） |
| UI-002 | 浮动工具按钮 | 已定义（Demo） | 进档壳层常驻 |
| UI-003 | 工具面板 | 已定义（Demo） | 含设置、关卡、增加主角装备、增加魔法书、添加士兵（后三项→ UI-019 / UI-020） |
| UI-004 | 挖坟占位屏 | 占位 | 可识别当前为 Dig |
| UI-005 | 升级与制造占位屏 | 占位 | 可识别当前为 UpgradeManufacture（原 SewRevive） |
| UI-006 | 防守占位屏 | 占位 | 可识别当前为 Defend；完整 UI 见 §3.12 |
| UI-007 | 设置页（进档） | 已实现（方案 A） | **仅**自工具面板进入；承载科技树画布（UI-012）；**不**与 UI-028 合并；其它进档设置项 TBD |
| UI-008 | 关卡选择面板 | 已实现（保留 Prefab；Hub 主路径不再嵌入） | Prefab `LevelSelectPanel`（可仍挂 InSaveShell 子级，默认隐藏）；去重 `LevelId` 选关由 UI-031 页签承担；验收见 §3.8 D-003 / D-081 |
| UI-009 | 开战按钮 | 已定义（Demo 流水线） | Defend 准备态；点击 → StartBattle（§3.12）；验收见 §3.8 D-040 |
| UI-010 | 升级与制造主屏 | 已定义（Demo 流水线） | 默认全屏制造区；顶部「GM升级」打开升级 Modal（右上 X 关闭）；底栏库存方格拖拽 +「完成」与其右「布阵」；布阵打开共享 FormationEditor；验收见 §3.8 D-030～D-032 |
| UI-011 | 挖坟阶段汇总 | 已定义（Demo 流水线） | DigStageSummary：本阶段已获奖励按类型汇总；无额外发放；`SummaryRoot` **1103×796**（Image Sprite=`Art/UI/Meta/Title/UI_Kuang_09.png`、Color 纯白）、`Title` 文字纯黑；`Body` **920×620** 为竖向 `ScrollRect` 视口（约三行可视，超出可上下滑动；无强制滚动条）；`Body/Viewport`（`RectMask2D`）→ `Content`（`GridLayoutGroup` `FixedColumnCount=5`，`cellSize` **168×200**，`spacing` **12×12**，`ContentSizeFitter` 竖向 Preferred）；每条奖励为 `DigSummaryItemCell`（方图标 + 右下数量 + 下方名称）：躯体名=`DisplayName`（空则 `BodyPartId`），图标=`DigBodyArtLoader`；精魂/非躯体名=`Id`，图标=`ItemIconLoader.LoadFromCatalog`（缺图空 Icon）；空账本「本阶段未获得奖励。」；`ConfirmButton` 文案「X」、锚 `SummaryRoot` 右上角（语义仍为确认）；确认后接 §3.9；验收见 §3.8 D-020 |
| UI-012 | 科技树画布 | 已实现（方案 A，可选） | 2D 可拖动画布；节点图标+类型框；连线；悬停描述；学习点击；见 §3.13；非 §3.8 P0；学会后 Dig 能力可验 |
| UI-013 | 战斗模式选关 | 已定义（Demo 流水线） | 进入 Defend 阶段后：选 `BattleMode` + 关卡（该模式全部玩法配置）；模式1进保卫战 Prepare；模式2选 `PushMapGameplayConfig` 后进 §3.14 Prepare；验收见 §3.8 D-044 |
| UI-014 | 玩法模式选择 | Demo 旁路 | 组件保留；**本 Demo 新建/进入不弹出**（直进 Mode2）；Mode1 入口后置；**勿与** UI-013 混淆；验收见 §3.8 D-045 |
| UI-015 | 制造记录弹窗 | 已定义（Demo / Mode2） | Mode2 UM：「布阵」右侧「制造记录」打开只读 Modal；最近一批士兵摘要（名字/种族/职业）；空态「本批无士兵」；Mode1 **无**此入口；验收见 §3.8 D-054 |
| UI-016 | 自动制造演出 | 已定义（Demo / Mode2） | AutoManufacture 阶段：底层全屏背景 `Title_AutoManufacture_1` + Dim + 上方 6 魔法书槽（120×160）。**默认流程（方案 A）：** StepA 落下本批消耗躯体（`SourceItemIds`→`BodyPartConfig.ArtAssetId`，`Art/UI/Dig`→`Resources/UI/Dig`；每 0.3s 随机 3～5 件；适配最长边 `AutoMfgBodyMaxEdgePx` 样例 **49**；绝对转角夹紧 ±`AutoMfgBodyAngleMaxDeg` 样例 **270**；`AmBodyRainLayer` 本地 Y=`AutoMfgBodyRainLayerYPx` 样例 **−340**；堆地板 `AutoMfgPileFloorYPx` 样例 **−300**（相对 Layer）；2D 重力 + 可转 OBB 弱碰撞堆叠；法阵 `MagicCircle_1` 本地 Y=`AutoMfgMagicCircleYPx` 样例 **−50**，绘制层级低于躯体）→ StepB 逐兵：法阵持续闪红 + 谜底 `UnknownSoldier_1` 从法阵中心出现并与 6 书槽脉冲**并行**（每槽上升全程 1/6；峰值套书+谜底闪烁；空槽仍占一拍）→ **上升期间**该兵 StepA 堆中躯体（按 `WarriorId`）错开飞入谜底视觉中心并闪红消失（`AutoMfgBodyAbsorb*`）→ 第 6 槽结束脚底到 `AutoMfgSoldierLandYPx` 变兵 Idle **定住**（sizeDelta 最长边 `AutoMfgSoldierMaxEdgePx` 样例 **64** × `AutoMfgSoldierVisualScale` 样例 **8**；**底部 pivot**；不再重力下落）；脚下影 Demo 隐藏（`AutoMfgSoldierShadowAlpha` 样例 **0**）；多兵成一行（**新兵水平居中**；已落地兵先向左平移一个 pitch，短时插值样例 **0.2s**；间距=`MaxEdge`×`VisualScale`×`PitchFactor`，样例 Factor **0.5** → **256**）；速率 ×`1.25^floor(completed/3)` → 循环下一兵 → StepC 上阵+UM 自动开布阵。**备选：** 中央士兵行传送带（卡+Camera+RT）默认隐藏、不删除；`AutoMfgUseLegacySoldierRow=1` 可切回。0 兵跳过；Mode1 **无**；参数见 `CombatConstantConfig` `AutoMfg*`；验收见 §3.8 D-055 |
| UI-017 | 战斗结算（推图/搜打撤） | 已定义（Demo / PushMap + SearchExtract） | **胜利：**上部「胜利」；中部战斗耗时/击杀（PushMap；SE 可隐藏击杀）、**阵亡士兵总数**、四基础职业阵亡（战士/射手/法师/刺客，图标+数量）；底中「继续」。PushMap Continue → UI-018；SE Leave 后 Continue → §3.9 推进。**失败：**上部「失败」；中部阵亡士兵总数；底栏「返回主界面」（→ TitleMenu / UI-027）、「重新开始」（Abort 后重进同 LevelId+OptionId → Prepare）。Defend **不做**；见 §3.14 / §3.19 |
| UI-018 | 推图奖励弹窗 | 已定义（Demo / PushMap） | 仅展示本场已入账：`StageExpReward` + 占领 `CaptureLoot` 汇总；`CaptureLoot` 先经 `ItemCatalogConfig` 解析为统一道具展示名/图标来源；无额外发放；底中「继续」→ 关闭后打开 LevelSelectPanel；见 §3.14 |
| UI-019 | GM 发放列表 | 已定义（Demo GM） | Prefab `GmGrantListPanel`（InSaveShell 子级；布局对齐 UI-008）；Tools「增加主角装备」/「增加魔法书」打开；按钮文案 DisplayName（空则 Id）；装备点行→嵌套 LevelPicker（等级按钮）→`DebugGrantAtLevel`；魔法书点一次 `TryEquip`；关闭按钮；验收见 §3.8 D-061 |
| UI-020 | GM 添加士兵 | 已定义（Demo GM） | Prefab/`Ensure` `GmAddSoldierPanel`（InSaveShell 子级；画面左侧靠边）；Tools「添加士兵」打开；职业/种族下拉 + 数量 + 自动上阵 + 关闭/添加；UM 布阵 **或** Mode2 Prepare（Defend/PushMap/SE）布阵打开可用；根节点自挂 Canvas `overrideSorting`，`sortingOrder` ≥ **100**（高于布阵 `FormationCanvas`=70 / `TacticalFormationSquadBarRoot`）；验收见 §3.8 D-064 |
| UI-021 | 士兵栏悬浮框 | 已定义（Demo / Mode2） | 仅 `FormationEditorRoot_Mode2`：指针停在有兵 `SoldierSlot` 上展示职业信息/静态属性/技能图标与名；每个技能 `Icon` 右上角 5×5 指示器按 `SkillConfig.EffectImplemented` 绿/红；离槽、横滑、拖起上阵则隐藏；Mode1 **无**；验收见 §3.8 D-065 / D-070 |
| UI-022 | 主角装备仓弹窗 | 已定义（Demo） | InSaveShell 左下「装备」打开居中 Modal（对齐 UI-008：全屏遮罩 + 中框 + 关闭；`sortingOrder` ≥ 100）；只读 `OwnedEquips`：`DisplayName`（空则 `EquipId`）+ `Lv.{Level}` + 当前等级行 `Description` + `IconAssetId`（有则 `Resources.Load<Sprite>("UI/Equipment/"+IconAssetId)`，含 `/` 则按相对路径直载）；空态「尚未拥有装备」；订阅 `Changed`；**不**升级、**不**划 `EquipCommonExp`、**不**卸下；Mode1/Mode2 均有；验收见 §3.8 D-067 |
| UI-023 | 魔法书槽弹窗 | 已定义（Demo） | InSaveShell 左下「魔法书」打开居中 Modal（同 UI-022 壳）；嵌套共享 `Assets/Prefabs/AutoManufacture/BookRow.prefab`（6×`BookSlot_0`…`_5`，120×160）；下标 **0→5 = 左→右**（与 UI-016 Step2 启动顺序相同）；每个槽位 `Icon` 从 `Resources/UI/MagicBooks/{IconAssetId}` 加载（`Resources.Load<Sprite>`，缺/空则不显示）；左键拖拽任意两槽 `SpecialEquipSlotsService.TrySwap`（含空槽=搬书）；成功立即 persist + `Changed`；左键点占用槽 → 该槽正下方浮动「删除」（空槽不出现；再点同一槽收起；拖拽开始隐藏）；点「删除」须二次确认（`ConfirmDialog`，`sortingOrder` 110）；确认后 `TryUnequip` 清该槽（**不补位**；无仓库=从当前存档删除；不可恢复）立即 persist + `Changed`；AM 演出 BookRow 订阅 `Changed` 同步；**无**独立魔法书仓库；装入仍 Tools GM `TryEquip`（UI-019）；Mode1/Mode2 均有；验收见 §3.8 D-068 / D-072 |
| UI-024 | 运行时光标 | 已定义（Demo） | 整段 Play（Boot 登录主界面起）硬件指针 = `Assets/Art/UI/Cursor.png`；`PlayerSettings.defaultCursor` + hotspot 对齐锁尖（贴图左上）；**不**隐藏系统鼠标；Dig 圆圈仅作范围指示 |
| UI-025 | 战斗技能图标 | 已定义（Demo / PushMap） | CombatSkillIcon：头顶瞬时 35×35 静止 0.6s 后世界 +Z 上飘 0.3s 淡出；同兵右排间距 4px；脚下持续 20×20；图标 `Resources/UI/Skills/{SkillId}`（缺图空框）；验收 D-071；Defend **无** |
| UI-026 | Mode2 商店全屏界面 | 已定义（Demo / Mode2） | 全屏 Prefab `Assets/Prefabs/Shop/ShopStageRoot.prefab`（内容区 stretch 铺满；运行时实例化/销毁）。底层全屏背景 `Title_Shop_1`（`AspectRatioFitter` EnvelopeParent 锁定长宽比铺满；其上半透明 `ShopBackdrop` Dim；`ShopBox` 为透明内容容器）。**双入口：** ① Mode2 关卡 Stage1 `GameplayType=Shop`（`ShopStageModule`；关闭 → `TryAdvanceStage`）；② InSaveShell 左下「商店」局外 overlay（关闭回壳、不推进阶段；Shop 阶段已开则 no-op）。布局：左侧「玩家信息」（精魂总值 `SpiritEssence`、`EquipSummaryText`/`MagicBookSummaryText` 占用摘要、其下已拥有装备/魔法书 ICON；点占用 ICON 在图标下方出现「出售」+ `SellPrice`，确认后出售入账精魂，见 D-076）；右侧「待售商品」（共 6 项：slot0..2 归类 A 装备、slot3..5 归类 B 魔法书；每项显示道具图标+道具名+精魂售价，支持点击购买；图标加载同 §3.5：A→`Resources/UI/Equipment/{IconAssetId}`、B→`Resources/UI/MagicBooks/{IconAssetId}`；购买成功后该 slot 标记已售/禁用且不自动刷新）。**OfferSlot 着色：** 未购根 Image 恒为纯白不透明 `Color.white`；已购仅通过 ColorTint 变色（`DisabledColor` alpha=1），**禁止**半透明淡出。下侧「刷新商品」按钮显示当前刷新价格并支持手动刷新递进定价；每次刷新重新生成 6 项（不足留空、不补齐）。商店在「新关卡解锁」时自动刷新一次并重置刷新价格进度。验收见 §3.8 D-075 / D-076。 |
| UI-027 | 登录主界面 | 已定义（Demo） | Prefab `TitleMenuPanel`（`MetaCanvas` 子级）；共享 `TitleScreenBackground`（Sprite 源 `Art/UI/Meta/Title/`）；顶中游戏名 Image `GameName`（`Title_GameName.png`）；主按钮双态（开始/继续）→ SaveSelect；**设置 → UI-028**；读取存档/开发者介绍 → Tips「还未制作」；右下版本 Text；Title BGM 随机 |
| UI-028 | 登录设置面板 | 已定义（Demo） | Prefab `TitleSettingsPanel`（`MetaCanvas` 子级；对齐 UI-008：全屏遮罩 + 中框 + 关闭；`sortingOrder` ≥ 100）；页签首期仅 **「显示」**：分辨率列表（`Screen.resolutions` 按宽×高去重降序，文案 `1920 × 1080`）+ 显示模式三选一（窗口 / 无边框全屏 / 独占全屏）；点 **「应用」** 才 `Screen.SetResolution` 并写机台级 PlayerPrefs（关闭不提交草稿）；Boot 读盘应用；**不**挂科技树；与 UI-007 分离 |
| UI-029 | 难度选择宿主 | 已定义（Demo） | Prefab 内 `DifficultySelectHost`（`InSaveShellPanel` 中心面）；三栏等宽**同屏**（各约 1/3）；悬停显示难度描述（Demo 写死；表 `Description` 接线后置）；**三栏点击均进入沙盘 UI-036**；无栏内 UI-008；无 MapHost；难度不改玩法数值；**新建进档** / 工具「关卡」仍先打开本界面；**进入占用档**与玩法结束打开沙盘（默认 `Diff_Normal`）；独立壳 Prefab `Assets/Prefabs/Meta/InSaveShellPanel.prefab`；验收 D-081 / D-098；难度解锁/通关奖规则见 §3.9（接线后置） |
| UI-030 | 战术阵型目录条 | 已定义（Demo） | 共享 `FormationEditorRoot` / `_Mode2`：`FormationCanvas` **左缘**竖排阵型目录按钮（`TacticalFormationSquadBarView`；数据=表内去重 `FormationId`，同阵型各 `FormationLevel` **共用 1 个按钮**）；常显，不随已建组出现或消失；图标=`Resources/UI/Formations/{IconAssetId}`（缺图空框+短 DisplayName）；点击 → **尝试新建一组**：未入任何组且技能中带有该 `FormationId` 的士兵，**已上阵优先**，不足 `min(MaxMemberCount, 槽位数)` 时按士兵库顺序从士兵栏**未上阵**补人并上阵（合计 ≥`MinMemberCount` 才 snap；不足 Min 不动坐标、不上阵）。阵心 = 本次纳入的已上阵成员质心；一个已上阵都没有时 = 镜头画面中心地面点（无效则失败且不动）。**不再**用按钮选中小队。选中、头顶图标与解散见 UI-035。Mode1/Mode2 均有；验收 §3.8 D-093（D-085 的点选高亮改由卡牌驱动） |
| UI-035 | 战术阵型组卡牌 | 已定义（Demo） | `FormationEditorRoot` 与 `_Mode2` **顶中**横向卡牌条（视觉规格以 Mode2 为准；Mode1 复用同一组件，否则无法选中/解散）：一组一张；视口最多同时看见 **8** 张，超出可左右滑动；卡面含图标、名称、阵型等级、人数；点击进入选中：士兵栏**仅**高亮该组成员，布阵预览里组成员头顶显示该阵型图标（**不**进战斗）；**选中卡下方**出现「解散」（成员退回职业区并删除该组）。验收 §3.8 D-093 |
| UI-036 | 沙盘界面 | 已定义（Demo） | Prefab `Assets/Prefabs/Sandbox/SandboxRoot.prefab`（运行时 `Resources/Prefabs/Sandbox/SandboxRoot`；缺则运行时拼装）。全屏；横向 `ScrollRect`；方格按 `SortOrder` 从左到右，显示 `DisplayName` 与剩余次数（未通关文案「可进入 N 次」；已通关文案「已通关」且不可进入）。剩余次数写入存档：挖坟 / 商店 / 自动造兵进入时扣 1，剩余 0 不可进入；COC 进入不扣，单局失败才扣（§3.21）。无解锁前置。点击：`Dig` → 现有 `DigStageModule`（须解析 `GameplayConfigId`）；`Shop` / `AutoManufacture` → 现有模块（ConfigId 忽略）；`CocCombat` → UI-037（须解析 `CocGameplayConfig`）。挖坟 / 商店 / 自动造兵结束，以及 COC 胜利或剩余仍大于 0 的单局失败，回同一难度沙盘，不走 `TrySelectGameplayOption` / `TryAdvanceStage`。COC 最终失败回 Title。返回按钮回 UI-029。验收 D-098 / D-099；规则 §3.20 / §3.21 |
| UI-037 | COC战斗 | 已定义（Demo） | COC 进图即战斗，无布阵 Prepare。底部职业卡：按 `ClassId` 一组一张，显示职业名、组内数量、组内第一名外形；点击选中后，在可洒区域左键放走该组最前一名，按住左键按间隔重复。平移镜头按住鼠标右键拖动（推图仍是左键拖动）。含「退出」（无二次确认，即单局失败）。验收 D-099；规则 §3.21
| UI-038 | COC组内展开 | 已定义（Demo） | 选中或未选中均可（不需先点击选中）：在职业卡上按住左键达到 `CardHoldSeconds`（默认 1 秒）后，于该卡上方用同样卡牌样式横排展开组内全部士兵（一行，可左右滑）。只查看，不改变洒出顺序，不单独放走某一名。松手收起。验收 D-099；规则 §3.21 |
| UI-031 | 关卡路线选择 | 已定义（Demo，入口已让出） | **玩家难度 / 占用档 / 结束不再打开。** Prefab `Assets/Prefabs/Level/LevelRouteSelectRoot.prefab`（壳层；**`Box` 全屏 stretch**；**`MapScroll` 竖向铺满与屏幕同高、宽 1920 水平居中**（地图内容展示宽仍 **1450**、在视口内水平居中；Stage 回退 `StageScroll` 仍铺满）；**`Title` / `LevelTabBar` 叠在地图之上**，不占 MapScroll 顶边留白）；进关后 / 选项通关后显示；**Box 顶部 LevelId 页签**（去重列表；页签 Label / `Box/Title` 显示运作表 `LevelName`，空则回退 `LevelId`；默认末项；切换=`TryEnterLevel`）；有 `Assets/Prefabs/Level/LevelRouteMap_{LevelId}.prefab` 时：Box 内竖滑该关地图（展示宽 **1450**、高按底图比例；底图源 `Art/UI/SubLevelMaps/`，运行时 `Resources/UI/SubLevelMaps/`，`RouteMapAssetId` 仍表驱动文件名）；选项中心钉在地图 Prefab 内子节点名=`GameplayOptionId` 的 `anchoredPosition`（底图左下角原点、Y 向上）；**打开/切页签后竖滑初始 Y 滚到「最新已解锁」钉点居中**（优先 Selectable/Running 最大 StageNumber，否则 Cleared；同 Stage 取最大钉点 Y；无目标则底部）；**选项通关返回地图模式时**：`JustClearedOptionId` 非空 → 先瞬时对准刚通关钉点，停顿 **0.5s**，再约 **0.5s** 平滑滚向当前「最新已解锁」前沿（只动 Y）；缺刚通关钉则直接对准前沿；**只动 Y、X 不变**；**地图模式场景仅显示 Icon**（约 80×80）；**悬停**独立 Prefab `Assets/Prefabs/Level/OptionHoverTips.prefab`（嵌套于壳层 `Box`；运行时 SerializeField，禁止代码拼装）按 GameplayType 分型展示（Dig=TipMessages；Shop/AM/UM=IconAssetId2+Description；PushMap/SE/Defend=IconAssetId2+Reward 图标行；均含 Title）；缺钉 Warning、图标仍生成于 `(0,0)`；无地图 Prefab（或无 `RouteMapAssetId`）时回退竖版**自下而上** Stage 行 + 横向**完整选项卡**；运行时仍 Instantiate 选项节点（地图 Prefab 只提供底图+钉点 Transform）；**SearchExtract** 另展示点奖励摘要与子关卡 `Reward` **分离显示**；按 `UnlockNextOptionIds` 画相邻 Stage 连线；状态：锁定 / 可点 / 已通关；**地图模式 Icon 三态（仅地图模式；Stage 行仍用卡片底色）**：Cleared=正常色+Icon 下方内侧 Checkmark（源 `Art/UI/Icons/Checkmark.png`，运行时 `Resources/UI/Icons/Checkmark`）；Selectable/Running=慢速 alpha 闪烁+缩放 0.9↔1.1（周期约 1.6s）；Locked=Icon RGB×0.4 变暗；点击可选项 → `TrySelectGameplayOption`；验收 D-086 |
| UI-032 | 搜打撤单点决策 | 已定义（规则库 / SearchExtract） | 单搜集点倒计时结束且仍有忠诚存活时弹出；底中「继续搜集」「离开」；最后一点仅「离开」；弹出后忠诚兵延迟 `SearchExtractDecisionIdleDelaySeconds`（默认 1s）停步并 Idle；**仅当**本关 `GatherPointCount=1` 时 LeaveButton 文案倒计时 `SearchExtractDecisionAutoLeaveSeconds`（默认 3s）后自动 Leave（可手动抢先）；Continue → 解除无敌、推进下一 `ObjectiveOrder`；Leave → 子关卡通关（`TryAdvanceStage` 或 §3.9 通关链）；验收 D-087 |
| UI-033 | 战斗指示器 | 已定义（Demo / PushMap + SearchExtract） | Combat 顶中 HUD（锚点 `(0.5,1)`，`anchoredPosition.y=-10`，根 `localScale=0.75`，参考分辨率 1920×1080）：中央 `HPPK_UI_1`（`CenterBg`）**子节点**左右半区=敌我**当前存活数**（字号 BestFit 26～42）；敌方本场尚未出现过 `>0` 且当前为 0 时显示 **`?`**，一旦现身过则后续含清零均显示数字；左侧我方格 `HPPK_UI_2`（右对齐靠中心）、右侧敌方格 `HPPK_UI_3`（左对齐靠中心）；简画图标来自 `ClassConfig` / `MonsterConfig` 的 `SilhouetteIconAssetId`（`Resources/UI/Icons/{Id}`）；HP% 着色 ≥66% 绿 / 33%～66% 橙 / ≤33% 红 / 永久死亡灰+`HPPK_UI_4`（0.5s 后移除并重对齐）；单行超出半屏可用宽则**截断**（不显示第 2～N 行）；低频 0.2s 轮询；Prepare/Ended/结算弹窗期间隐藏；Defend **无**；验收 D-089 |
| UI-034 | 离屏刷怪边缘提示 | 已定义（Demo / PushMap + SearchExtract） | Combat 真实刷怪一组完成后：以该次 `basePos`（SpawnPoint / `ResolveSpawnPosition`）相对战斗相机 `WorldToViewportPoint`；视口外则在最靠近方向的屏幕边缘显示 `EnemyAttack_1`（`Art/UI/Icons/`→`Resources/UI/Icons/EnemyAttack_1`；缺图空框仍闪）；图标不旋转；开场 0.4s 内闪红 2 次（`Image` 白↔红，alpha=1），再常亮 2s；显示 `localScale=1.3`；同帧多组可各出一枚；离开 Combat / Ended / UI-017 / UI-032 清掉进行中提示；PushMap **跳过** `PreparePreview`；Defend **无**；验收 D-090 |

### English

| ID | Name | Status | Notes |
|----|------|--------|-------|
| UI-001 | Save select | Defined (Demo) | 3 slots: create / enter / delete (confirm) |
| UI-002 | Floating Tools button | Defined (Demo) | InSaveShell |
| UI-003 | ToolsPanel | Defined (Demo) | Settings + Level + Grant Protagonist Equipment + Grant MagicBook + Add Soldier (last three → UI-019 / UI-020) |
| UI-004 | Dig placeholder | Placeholder | Identifiable Dig |
| UI-005 | UpgradeManufacture placeholder | Placeholder | Identifiable UpgradeManufacture (was SewRevive) |
| UI-006 | Defend placeholder | Placeholder | Identifiable Defend; full UI in §3.12 |
| UI-007 | Settings page (in-save) | Done (Approach A) | **Only** from Tools; hosts TechTree canvas (UI-012); **not** merged with UI-028; other in-save settings TBD |
| UI-008 | Level select panel | Done (Prefab retained; Hub no longer embeds) | Prefab `LevelSelectPanel` (may remain under InSaveShell, hidden by default); LevelId pick moves to UI-031 tabs; accept §3.8 D-003 / D-081 |
| UI-009 | StartBattle button | Defined (Demo pipeline) | Defend Prepare; click → StartBattle (§3.12); accept §3.8 D-040 |
| UI-010 | UpgradeManufacture main screen | Defined (Demo pipeline) | Full-screen manufacture by default; top "GM Upgrade" opens upgrade Modal (top-right X closes); bottom inventory square bar + drag + Complete with Formation to its right; opens shared FormationEditor; accept §3.8 D-030–D-032 |
| UI-011 | Dig stage summary | Defined (Demo pipeline) | DigStageSummary aggregate only; `SummaryRoot` **1103×796** (Image Sprite=`Art/UI/Meta/Title/UI_Kuang_09.png`, Color white), `Title` text black; `Body` **920×620** vertical `ScrollRect` viewport (~3 rows visible; overflow scrolls; no forced scrollbar); `Body/Viewport` (`RectMask2D`) → `Content` (`GridLayoutGroup` `FixedColumnCount=5`, `cellSize` **168×200**, `spacing` **12×12**, vertical Preferred `ContentSizeFitter`); each reward is `DigSummaryItemCell` (square icon + bottom-right qty + name below): body name=`DisplayName` (empty → `BodyPartId`), icon=`DigBodyArtLoader`; Spirit/non-body name=`Id`, icon=`ItemIconLoader.LoadFromCatalog` (missing → empty Icon); empty ledger 「本阶段未获得奖励。」; `ConfirmButton` label "X", top-right of `SummaryRoot` (still confirms); confirm → §3.9; accept §3.8 D-020 |
| UI-012 | TechTree canvas | Done (Approach A, optional) | 2D pannable canvas; §3.13; not §3.8 P0; Dig caps verifiable after learn |
| UI-013 | Battle mode/level select | Defined (Demo pipeline) | After entering Defend: pick `BattleMode` + level (all configs for mode); Mode1 → Defend Prepare; Mode2 pick `PushMapGameplayConfig` → §3.14 Prepare; accept §3.8 D-044 |
| UI-014 | Campaign mode select | Demo bypass | Component retained; **not shown** on this Demo create/enter (straight Mode2); Mode1 entry deferred; **not** UI-013; accept §3.8 D-045 |
| UI-015 | Manufacture record popup | Defined (Demo / Mode2) | Mode2 UM: "Manufacture Record" to the right of Formation opens read-only Modal; last-batch summaries (name/race/class); empty 「本批无士兵」; Mode1 has **no** entry; accept §3.8 D-054 |
| UI-016 | AutoManufacture presentation | Defined (Demo / Mode2) | AutoManufacture stage: full-screen `Title_AutoManufacture_1` + Dim + 6 MagicBook slots (120×160) above. **Default (Approach A):** StepA rain consumed body parts (`SourceItemIds`→`ArtAssetId`, `Art/UI/Dig`→`Resources/UI/Dig`; every 0.3s drop 3–5; fit max-edge `AutoMfgBodyMaxEdgePx` sample **49**; Z-angle clamp ±`AutoMfgBodyAngleMaxDeg` sample **270**; `AmBodyRainLayer` local Y=`AutoMfgBodyRainLayerYPx` sample **−340**; pile floor `AutoMfgPileFloorYPx` sample **−300** (vs Layer); 2D gravity + rotatable OBB soft pile; circle `MagicCircle_1` local Y=`AutoMfgMagicCircleYPx` sample **−50**, draws under bodies) → StepB per soldier: circle flash red + mystery `UnknownSoldier_1` spawns at circle center and rises **in parallel** with 6 book pulses (1/6 of rise per slot; peak applies book + mystery flash; empty slots still take a beat) → **during rise** that soldier's StepA pile pieces (by `WarriorId`) stagger-fly into mystery visual center and vanish with red flash (`AutoMfgBodyAbsorb*`) → at end of slot 6 feet at `AutoMfgSoldierLandYPx` then morph Idle **in place** (sizeDelta max-edge `AutoMfgSoldierMaxEdgePx` sample **64** × `AutoMfgSoldierVisualScale` sample **8**; **bottom pivot**; no gravity fall); foot shadow Demo-hidden (`AutoMfgSoldierShadowAlpha` sample **0**); multi-soldier one row (**new at horizontal center**; already-landed shift left by one pitch, short lerp sample **0.2s**; pitch=`MaxEdge`×`VisualScale`×`PitchFactor`, sample Factor **0.5** → **256**); rate ×`1.25^floor(completed/3)` → next soldier → StepC deploy + UM auto-open Formation. **Legacy:** central soldier conveyor (card+Camera+RT) hidden by default, not deleted; `AutoMfgUseLegacySoldierRow=1` to restore. Skip if 0 craft; Mode1 **none**; tunables `CombatConstantConfig` `AutoMfg*`; accept §3.8 D-055 |
| UI-017 | Battle settlement (PushMap / SearchExtract) | Defined (Demo / PushMap + SearchExtract) | **Victory:** top Victory; mid combat time/kills (PushMap; SE may hide kills), **loyal casualty total**, four BaseClass casualty rows (icons+counts); bottom Continue. PushMap Continue → UI-018; SE after Leave Continue → §3.9 advance. **Defeat:** top Defeat; mid casualty total; bottom Return to Title (→ TitleMenu / UI-027) / Restart (Abort then re-enter same LevelId+OptionId → Prepare). Defend **not** done; §3.14 / §3.19 |
| UI-018 | PushMap reward popup | Defined (Demo / PushMap) | Show already-credited StageExpReward + CaptureLoot aggregate only; `CaptureLoot` first resolves through `ItemCatalogConfig` for shared display-name/icon sourcing; no extra grants; bottom Continue → LevelSelectPanel; §3.14 |
| UI-019 | GM grant list | Defined (Demo GM) | Prefab `GmGrantListPanel` under InSaveShell (layout aligned with UI-008); Tools Grant Equipment / Grant MagicBook; label DisplayName (else Id); equipment pick → nested LevelPicker (level buttons) → `DebugGrantAtLevel`; MagicBook one click `TryEquip`; close button; accept §3.8 D-061 |
| UI-020 | GM add soldier | Defined (Demo GM) | Prefab/`Ensure` `GmAddSoldierPanel` under InSaveShell (left dock); Tools Add Soldier; class/race dropdowns + count + auto-deploy + Close/Add; UM Formation **or** Mode2 Prepare (Defend/PushMap/SE); root Canvas `overrideSorting`, `sortingOrder` ≥ **100** (above formation `FormationCanvas`=70 / `TacticalFormationSquadBarRoot`); accept §3.8 D-064 |
| UI-021 | Soldier-bar hover tooltip | Defined (Demo / Mode2) | `FormationEditorRoot_Mode2` only: pointer over occupied `SoldierSlot` shows class info / static stats / skill icons+names; each skill `Icon` top-right 5×5 indicator green/red from `SkillConfig.EffectImplemented`; hide on leave, horizontal scroll, or lift-to-deploy; Mode1 **none**; accept §3.8 D-065 / D-070 |
| UI-022 | Protagonist equipment warehouse popup | Defined (Demo) | InSaveShell bottom-left Equipment opens centered Modal (align UI-008: full-screen dim + center box + close; `sortingOrder` ≥ 100); read-only `OwnedEquips`: `DisplayName` (else `EquipId`) + `Lv.{Level}` + current-level `Description` + `IconAssetId` (`Resources.Load<Sprite>("UI/Equipment/"+IconAssetId)` if set; slash in id → load as Resources-relative path); empty 「尚未拥有装备」; subscribe `Changed`; **no** level-up / spend `EquipCommonExp` / unequip; Mode1 and Mode2; accept §3.8 D-067 |
| UI-023 | MagicBook slots popup | Defined (Demo) | InSaveShell bottom-left MagicBook opens centered Modal (same shell as UI-022); nested shared `Assets/Prefabs/AutoManufacture/BookRow.prefab` (6×`BookSlot_0`…`_5`, 120×160); index **0→5 = left→right** (same start order as UI-016 Step2); each slot `Icon` loaded from `Resources/UI/MagicBooks/{IconAssetId}` (`Resources.Load<Sprite>`; missing/empty => hidden); LMB-drag any two slots `SpecialEquipSlotsService.TrySwap` (empty slot = move book); success persists immediately + `Changed`; LMB click occupied slot → floating Delete under that slot (empty slots hide it; click same slot again hides; BeginDrag hides); Delete requires confirm (`ConfirmDialog`, `sortingOrder` 110); confirm → `TryUnequip` clears that index (**no compact**; no warehouse = delete from current save; irreversible) persist immediately + `Changed`; AM presentation BookRow subscribes `Changed`; **no** MagicBook warehouse; grant still Tools GM `TryEquip` (UI-019); Mode1 and Mode2; accept §3.8 D-068 / D-072 |
| UI-024 | Runtime pointer | Defined (Demo) | Whole-Play (from Boot title menu) hardware cursor = `Assets/Art/UI/Cursor.png`; `PlayerSettings.defaultCursor` + hotspot at shovel tip (texture top-left); do **not** hide OS cursor; Dig ring is range overlay only |
| UI-025 | Combat skill icon | Defined (Demo / PushMap) | CombatSkillIcon: overhead 35×35 holds 0.6s then world +Z rise 0.3s fade; stack screen-right 4px; persist 20×20 at feet; icons `Resources/UI/Skills/{SkillId}` (missing → empty frame); accept D-071; Defend **none** |
| UI-026 | Mode2 shop full-screen UI | Defined (Demo / Mode2) | Full-screen Prefab `Assets/Prefabs/Shop/ShopStageRoot.prefab` (content stretch-fills; runtime instantiate/destroy). Bottom full-screen background `Title_Shop_1` (`AspectRatioFitter` EnvelopeParent keep-aspect cover; semi-transparent `ShopBackdrop` Dim above; `ShopBox` is a transparent content host). **Dual entry:** (1) Mode2 Level Stage1 `GameplayType=Shop` (`ShopStageModule`; close → `TryAdvanceStage`); (2) InSaveShell bottom-left Shop overlay (close returns to shell, does not advance; no-op while Shop stage is open). Layout: left "Player Info" (`SpiritEssence`, `EquipSummaryText`/`MagicBookSummaryText`, owned equipment/MagicBook ICONs below; click occupied ICON → Sell + `SellPrice` under it, confirm to sell for Spirit, D-076); right "Shop Offers" (6 offers total: slot0..2 category A equipment, slot3..5 category B magicbooks; each shows item icon + item name + Spirit price and supports click-to-buy; successful purchase marks that slot sold/disabled and does not auto-refresh). **OfferSlot tint:** unsold root Image stays opaque pure white (`Color.white`); sold uses ColorTint RGB only (`DisabledColor` alpha=1) — **no** semi-transparent fade. Bottom "Refresh Offers" button shows the current refresh price and supports manual refresh-price progression; each refresh regenerates all 6 offers (leave empty if insufficient, no backfill). The shop auto-refreshes once on each "new level unlock" and resets the refresh-price progression. Accept §3.8 D-075 / D-076. |
| UI-027 | Title / login menu | Defined (Demo) | Prefab `TitleMenuPanel` under `MetaCanvas`; shared `TitleScreenBackground` (sprites under `Art/UI/Meta/Title/`); top-center game-name Image `GameName` (`Title_GameName.png`); primary dual-state (Start/Continue) → SaveSelect; **Settings → UI-028**; Load save / Credits → Tips「还未制作」; bottom-right version Text; random Title BGM |
| UI-028 | Title settings panel | Defined (Demo) | Prefab `TitleSettingsPanel` under `MetaCanvas` (align UI-008: full-screen dim + center box + close; `sortingOrder` ≥ 100); first tab **Display** only: resolution list (`Screen.resolutions` dedupe by WxH descending, label `1920 × 1080`) + window mode tri-state (Windowed / Borderless / Exclusive); **Apply** commits `Screen.SetResolution` + machine-level PlayerPrefs (Close discards draft); Boot applies saved; **no** TechTree; separate from UI-007 |
| UI-029 | Difficulty select host | Defined (Demo) | `DifficultySelectHost` inside `InSaveShellPanel`; equal-width **same-screen** columns (~1/3 each); hover shows difficulty description (Demo hardcoded; table `Description` wiring deferred); **any column click opens Sandbox UI-036**; no in-column UI-008; no MapHost; difficulty does not change gameplay numbers; **Create enter** / Tools Level still open this host first; **Enter occupied** and gameplay end open the Sandbox (default `Diff_Normal`); standalone Prefab `Assets/Prefabs/Meta/InSaveShellPanel.prefab`; accept D-081 / D-098; unlock/clear-reward rules in §3.9 (wiring deferred) |
| UI-030 | Tactical formation catalog strip | Defined (Demo) | Shared `FormationEditorRoot` / `_Mode2`: left-edge vertical **catalog** buttons (`TacticalFormationSquadBarView`; data = distinct `FormationId` rows; all `FormationLevel`s of one formation **share 1 button**); always visible, not tied to existing groups; icon=`Resources/UI/Formations/{IconAssetId}` (missing → empty frame + short DisplayName); click **tries to create one new group** from soldiers in no group whose skills include that `FormationId`: **deployed first**, then undeployed SoldierBar soldiers in pool order until `min(MaxMemberCount, slot count)` (snap only if the total ≥`MinMemberCount`; below Min does not move or deploy). Center = centroid of the deployed members taken this click; if none are deployed, the ground point at the view center (invalid → fail, no moves). Buttons **no longer select** a squad. Selection, overhead icons, and disband are UI-035. Mode1+Mode2; accept §3.8 D-093 (D-085 soldier-bar highlight moves to the card) |
| UI-035 | Tactical formation group cards | Defined (Demo) | Top-center horizontal card strip on `FormationEditorRoot` and `_Mode2` (visual spec authored for Mode2; Mode1 mounts the same component, otherwise Mode1 cannot select or disband): one card per group; viewport shows at most **8** cards, horizontal scroll both ways when more; card shows icon, name, formation level, member count; click selects that group: soldier bar highlights **only** its members, and formation-preview puppets show that formation’s icon overhead (**not** in combat); **Disband** appears under the selected card (members return to class zones and the group is removed). Accept §3.8 D-093 |
| UI-036 | Sandbox | Defined (Demo) | Prefab `Assets/Prefabs/Sandbox/SandboxRoot.prefab` (runtime `Resources/Prefabs/Sandbox/SandboxRoot`; runtime build if missing). Full screen; horizontal `ScrollRect`; cells left-to-right by `SortOrder`, showing `DisplayName` and remaining count (uncleared label 「可进入 N 次」; cleared label 「已通关」 and not enterable). Remaining count is saved: Dig / Shop / AutoManufacture decrement by 1 on enter and block at 0; COC does not decrement on enter, only on a round loss (§3.21). No unlock gates. Click: `Dig` → existing `DigStageModule` (`GameplayConfigId` must resolve); `Shop` / `AutoManufacture` → existing modules (ConfigId ignored); `CocCombat` → UI-037 (`CocGameplayConfig` must resolve). Dig / Shop / AutoManufacture end, and COC victory or a round loss that still leaves remaining count above 0, return to the same difficulty Sandbox, not via `TrySelectGameplayOption` / `TryAdvanceStage`. COC final loss returns to Title. Back returns to UI-029. Accept D-098 / D-099; rules §3.20 / §3.21 |
| UI-037 | COC combat | Defined (Demo) | COC enters combat immediately; no formation Prepare. Bottom class cards: one card per `ClassId`, showing class name, count in the group, and the appearance of the first soldier in the group. Click selects the card; left-click on an allowed area places the front soldier of that group; holding left-click repeats on the interval. Pan the camera by holding the right mouse button (PushMap still pans with the left button). Includes Quit (no confirm dialog; that is a round loss). Accept D-099; rules §3.21
| UI-038 | COC group expand | Defined (Demo) | Whether or not the card is selected (no prior click-select required): holding left-click on a class card for `CardHoldSeconds` (default 1s) expands every soldier in that group in one horizontal, scrollable row of the same card style above the card. Inspect only; it does not change deploy order and does not place a chosen soldier. Release dismisses the row. Accept D-099; rules §3.21 |
| UI-031 | Level route select | Defined (Demo, entry retired) | **Difficulty / occupied enter / gameplay end no longer open this.** Prefab `Assets/Prefabs/Level/LevelRouteSelectRoot.prefab` (chrome; **`Box` stretch fullscreen**; **`MapScroll` height matches screen, width 1920, horizontally centered** (map content still display width **1450**, centered in viewport; Stage fallback `StageScroll` still stretch-full); **`Title` / `LevelTabBar` overlay the map**, no top inset reserved for chrome); shown after enter Level / after option clear; **LevelId tabs atop Box** (distinct list; tab Label / `Box/Title` show Operation `LevelName`, empty→`LevelId`; default last; switch=`TryEnterLevel`); with `Assets/Prefabs/Level/LevelRouteMap_{LevelId}.prefab`: vertical-scroll that map (display width **1450**, height by bg aspect; art source `Art/UI/SubLevelMaps/`, runtime `Resources/UI/SubLevelMaps/`; `RouteMapAssetId` still names the file); option centers pinned to child nodes named `GameplayOptionId` (`anchoredPosition`; map bottom-left origin, Y up); **on open / tab switch, initial scroll Y centers the latest unlocked pin** (prefer max StageNumber among Selectable/Running, else Cleared; same Stage → max pin Y; else bottom); **on clear-return in map mode**: non-empty `JustClearedOptionId` → snap to just-cleared pin, hold **0.5s**, then ~**0.5s** smooth scroll to current latest-unlocked frontier (Y only); missing cleared pin → frontier directly; **Y only, X unchanged**; **map mode shows Icon only** (~80×80); **hover** standalone Prefab `Assets/Prefabs/Level/OptionHoverTips.prefab` (nested under chrome `Box`; runtime SerializeField only — no code-built hierarchy) by GameplayType (Dig=TipMessages; Shop/AM/UM=IconAssetId2+Description; PushMap/SE/Defend=IconAssetId2+Reward icons; Title always); missing pin → Warning, icon at `(0,0)`; without map Prefab (or no `RouteMapAssetId`) → bottom-up Stage rows + horizontal **full cards**; runtime still Instantiates option nodes (map Prefab = bg + pin Transforms only); **SearchExtract** dual summaries; edges from `UnlockNextOptionIds`; states locked / selectable / cleared; **map-mode Icon visuals (map mode only; Stage rows keep card tint)**: Cleared=normal + bottom-inner Checkmark (art `Art/UI/Icons/Checkmark.png`, runtime `Resources/UI/Icons/Checkmark`); Selectable/Running=slow alpha blink + scale 0.9↔1.1 (~1.6s period); Locked=Icon RGB×0.4 dim; click → `TrySelectGameplayOption`; accept D-086 |
| UI-032 | SearchExtract point decision | Defined (rules library / SearchExtract) | After gather countdown ends with ≥1 living loyal: bottom-center **Continue Gather** / **Leave**; last point shows **Leave** only; after show, loyals delay `SearchExtractDecisionIdleDelaySeconds` (default 1s) then stop + Idle; **only if** stage `GatherPointCount=1`, LeaveButton label countdown `SearchExtractDecisionAutoLeaveSeconds` (default 3s) then auto-Leave (manual Leave may preempt); Continue → drop invincibility, advance next `ObjectiveOrder`; Leave → SubLevel clear (§3.9 chain); accept D-087 |
| UI-033 | Combat indicator | Defined (Demo / PushMap + SearchExtract) | Combat top-center HUD (anchor `(0.5,1)`, `anchoredPosition.y=-10`, root `localScale=0.75`, ref 1920×1080): center `HPPK_UI_1` (`CenterBg`) **child** left/right-half = **current alive counts** (BestFit font 26–42); enemy shows **`?`** while this battle has never seen `EnemyAliveCount>0` and current is 0, then always numeric (incl. 0); left ally slots `HPPK_UI_2` (right-align toward center), right enemy slots `HPPK_UI_3` (left-align toward center); silhouettes from `ClassConfig` / `MonsterConfig` `SilhouetteIconAssetId` (`Resources/UI/Icons/{Id}`); HP% tint ≥66% green / 33%–66% orange / ≤33% red / permanent-dead gray+`HPPK_UI_4` (remove after 0.5s and re-align); one row exceeding half-screen usable width → **truncate** (do not show rows 2…N); 0.2s poll; hide in Prepare/Ended/settlement; Defend **none**; accept D-089 |
| UI-034 | Off-screen spawn edge hint | Defined (Demo / PushMap + SearchExtract) | After each Combat real spawn group: test `basePos` (SpawnPoint / `ResolveSpawnPosition`) via combat camera `WorldToViewportPoint`; if outside viewport, show `EnemyAttack_1` at nearest screen edge (`Art/UI/Icons/`→`Resources/UI/Icons/EnemyAttack_1`; missing → empty frame still blinks); icon does not rotate; 2 red blinks in 0.4s (white↔red, alpha=1) then solid 2s; display `localScale=1.3`; multiple groups may each show one; clear on leave Combat / Ended / UI-017 / UI-032; PushMap **skips** `PreparePreview`; Defend **none**; accept D-090 |

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
| D-003 | 工具面板可见「设置」「关卡」入口；「关卡」打开难度 Hub（三栏同屏）；任意难度点击 → 沙盘 UI-036 | P0 | **关卡**→ DifficultySelectHost → Sandbox（UI-029/UI-036）；设置→科技树画布 |
| D-004 | 进档后可识别当前处于三种玩法状态之一；默认进档为挖坟占位；关卡内由阶段玩法类型驱动 | P0 | Meta 占位+Debug 切态保留；关卡内由 `LevelOperationDriver` 按阶段 `GameplayType` 驱动；进档默认中心面为难度 Hub（D-081） |
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
| D-045 | 玩法模式门闩：**本 Demo** 新建/进入跳过 `CampaignModeSelect`、一律 Mode2；UI-014 保留；同槽两模式进度隔离；Mode2 只读 `ConfigTables/Mode2/Csv`；删档清双模数据；**勿与** D-044 混淆 | P0 | Demo 旁路（方案 A Hub 片） |
| D-050 | Mode2：样例关卡运作含 **Shop** → Dig → **AutoManufacture** → UpgradeManufacture → PushMap；DigStageSummary 确认后进入自动制造；自动制造阶段无玩家确认、自动交还驱动 | P1 | 已实现（AM-03～08：`AutoManufactureStageModule` 自动 `TryAdvanceStage`；Mode2 `Level_01` Shop→Dig→AutoManufacture→UM→PushMap；Excel/CSV 对齐；0 兵 Tips「无士兵可制造」；手验清单 `.scratch/mode2-auto-manufacture/issues/08-level-sample-handcheck.md`；Shop 阶段见 D-075） |
| D-051 | Mode2 AutoManufacture：按最低配方（头+躯干+臂×2含主要手+腿×2）循环造兵入临时仓库；不计 Spirit/Control；职业由双手 ClassRestrict；余料留仓库 | P1 | 已实现（AM-03～06：选料/职业/属性→钩子→外观+命名→临时仓 flush→`WarriorPool`→清阵上阵；SoulId 空、Control=0、AttackMode←ClassConfig；手验见 AM-08） |
| D-052 | Mode2：批结束后清空布阵，按 `PlacementOrder` + `FormationClassZone` 自动上阵（碰撞挤开）；再进 UM | P1 | 已实现（AM-06 方案 A：区内螺旋采样 + BodyRadius；`FormationClassZone` **IsoDiamond**（同 WalkSurface；废止 OBB/IsoTileYaw）；仅本批 Id；手验见 AM-08 / FZ-01～02） |
| D-053 | Mode2 UM：隐藏手动制造；保留升级 Modal 与可编辑布阵；控制力 HUD 屏蔽；布阵内 `CompleteButton`（SoldierBar 上右，UM/Prepare 均显示；UM 接线结束阶段）；Prepare `StartBattleButton` 叠于 Complete 正上方 | P1 | 已实现（方案 C：`UpgradeManufactureStageRoot_Mode2` + `FormationEditorRoot_Mode2`；Catalog 按 CampaignMode Resolve；手验见 AM-08；Complete 见 Mode2 差分） |
| D-054 | Mode2 UM：布阵右侧「制造记录」打开只读弹窗；展示最近一批 AutoManufacture 士兵摘要（名字/种族/职业）；0 兵空态「本批无士兵」；下一批覆盖；同档再进仍可见；Mode1 无此按钮 | P1 | 已实现（方案 A：`AutoManufactureBatchRecordService` + Mode2 Modal；`UmAssetBuilder` Mode2 追加 / 运行时 Ensure） |
| D-055 | Mode2 AutoManufacture 演出（UI-016）：批末播 StepA 落下本批消耗躯体（ArtAssetId；0.3s×3～5；2D 堆叠）→ StepB 逐兵法阵闪红 + 谜底从法阵中心与 6 书脉冲并行上升（每槽 1/6 + 峰值闪烁）+ 该兵堆中躯体错开飞入谜底中心闪红消失 → 脚底到 LandY 变兵定住 Idle → 循环 → StepC 按 final ClassId 上阵再进 UM 并自动开布阵；中央士兵行默认隐藏保留；0 兵 Tips+跳过且不自动开布阵；Mode1 无此 UI | P1 | **更新**（v0.84.51 躯体飞入谜底；1A+2A） |
| D-056 | 士兵外观：`BodyAppearanceConfig.AppearanceId` 在 `Art/Characters/Appearances/{Id}/` **Art 就绪**（Controller + Idle Sprite）时，须有游戏 Prefab `Prefabs/Defend/Warriors/{Id}.prefab`（根+`Visual`）并绑定 Defend/UM Catalog；缺绑定则布阵/战斗/演出不显示该外观 | P1 | Done（WA-01 / 方案 B：`WarriorAppearancePrefabAssembler` From-Art + Catalog 并集刷新；已补 `App_0_*`/`App_4_41`/`App_5_51`，并扩展人/精/兽 `App_1_*`/`App_2_*`/`App_3_*`） |
| D-057 | 样例 `Ground_*` **与** `PushMap_Demo_*` 的 `FormationClassZone` 覆盖当前模式 `ClassConfig` **全部** ClassId（缺区→自动上阵/一键上阵留池）；Mode2 须含 `Class_DarkMage`/`Class_Guardian` 等；推图样例区锚点=`CameraFollowPath/WP_Start`（缺则 DigMapBounds.Center） | P1 | **更新**（全量 ClassId + HalfExtents `(3.85, 2)`；Ground Ensure 不变；PushMap Ensure 菜单锚定 WP_Start） |
| D-058 | Mode2 魔法书「战士强化」：装备 `MagicBook_WarriorEnhance` 后 AutoManufacture **Step2 该书槽脉冲**仅对 `Class_BaseWarrior` / `Class_Warrior` 将主属性 Base 增加躯体该维 Σ StatBonus 的 15%（可叠；种族不过滤）；其它职业不变；写入实例至彻底死亡；Dig HUD GM 可装备 | P1 | **更新**（生效点改为 Step2 单槽脉冲；职业含枯骨战士） |
| D-059 | 主角装备 Dig 垂直：`ProtagonistEquipmentConfig` 表加载（Mode1+Mode2）+ 装备仓 Service/存档 + Dig caps 科技与装备加法合并 + Dig HUD GM 手验（发放 `Equip_IronShovel` / 公共经验）；仓只读 UI 见 D-067；升级/划公共经验/卸下 UI 与制造·战斗 Token **后置** | P1 | **完成**（PE-01～PE-04；方案 A；issues `.scratch/protagonist-equipment/`；Demo 装备=`Equip_IronShovel` 铁铲） |
| D-060 | 主角装备「矿灯」`Equip_MinerLamp`：表 5 级（升下一级/转化经验均为 1）+ Q4/Q5/Q6 生成权重按当前行累计 +10（缺席视为 0）+ Dig HUD GM 发放/划入手验 | P1 | **完成**（PE-05～PE-08；方案 A；issues `.scratch/protagonist-equipment/`） |
| D-061 | ToolsPanel Demo GM：增加主角装备（当前模式表按 EquipId 去重；点行嵌套选等级 → `DebugGrantAtLevel` 写死等级/`CurrentExp=0`）+ 增加魔法书（MagicBookConfig 全表，点一次 `TryEquip`；唯一已装/槽满失败）；GmGrantListPanel + LevelPicker（UI-019）；Dig HUD GM 仍 `TryAcquire`；仓只读 / 魔法书排序见 D-067 / D-068 | P1 | **完成**（TP-00～02 + 等级弹窗迭代；方案 A） |
| D-062 | 士兵技能垂直：`SkillConfig` 表加载（Mode1+Mode2，含 `IconAssetId`）+ `ClassConfig.DefaultSkillIds` + 池持久化 + Mode1 制造授予 + Mode2 造兵授予/`SoldierSkillLevelAdd`（Step2 该书槽脉冲）；战斗施放见 **D-069** | P1 | **完成**（SS-01～04；施放垂直 = D-069） |
| D-063 | Mode2 魔法书职业进阶：装备 `MagicBook_WarriorAdvance` 等四本后 AutoManufacture **Step2 该书槽脉冲**仅对对应 `Class_*_0` 以 25% 改为 `Class_*`（精确 ClassId；日志 hit/miss）；命中后重授 `DefaultSkillIds`；外观/命名/上阵区跟最终职业；其它职业不变；手验 Tools「增加魔法书」 | P1 | **更新**（生效点改为 Step2 单槽脉冲；Deploy 用最终 ClassId） |
| D-064 | ToolsPanel Demo GM「添加士兵」（UI-020）：UM 布阵 **或** Mode2 Prepare（Defend/PushMap/SearchExtract，`FormationEditorRoot_Mode2`）打开可用；Mode1 Defend Prepare 仍不可用；左侧面板选职业/种族/数量(1–999)/自动上阵；`BodyAppearance` 无 RaceId+ClassAffinity(ClassName) 匹配 → Tips「找不到此种士兵！」且不关面板、不入池；多匹配确定选取（`DefaultAppearanceId`∈匹配集 > `AppearanceLevel==ClassLevel` > 表序首条，禁止匹配集内随机）；匹配则免材料入池并可 DeployBatch；UM「返回」在右下（Mode2 在 Complete 上方） | P1 | **完成**（方案 A；Mode2 Prepare 门闩扩展） |
| D-065 | Mode2 士兵栏悬浮框（UI-021）：指针停在有兵 `SoldierSlot` 上展示 ClassName、`{ClassLevel}级`、种族 `DisplayNameKey`、`BaseClass`/`PromoteClass`（空则隐藏该标）、静态 MaxHP 与力量/敏捷/智力（`PrimaryStat` 行标「(主属性)」）、实例 `SoldierSkills` 图标（`Resources/UI/Skills/{SkillId}`）与 `DisplayName`；离槽/横滑/拖起隐藏；UM 与 Defend/PushMap Prepare 均可用；Mode1 无框 | P1 | **完成**（方案 A；`FormationSoldierHoverTooltipView`） |
| D-066 | Mode2 放大模型：命中 `VisualStyleId=Style_ScaleModel`（或别名 `放大模型`）的书后，实例 `VisualModelScale` 连乘 `VisualIntensityAdd`（k）；世界单位 `Visual.localScale=(k,k,k)` 且 `BodyRadius`/`AttackRange` ×k（可与 AllIn1 材质共存）；UI-016 士兵卡世界预览 **套用** k；布阵底栏缩略图 **不**缩放；Mode1/GM 添加士兵 k=1 | P1 | **完成**（VS-00～03；方案 A；issues `.scratch/visual-scale-model/`；卡面 RT 预览跟世界 Instantiation） |
| D-067 | InSaveShell 装备仓只读弹窗（UI-022）：左下「装备」打开居中 Modal；列出 `OwnedEquips` 名/等级/描述/图标；空态「尚未拥有装备」；GM 发放后 `Changed` 刷新；不升级、不划公共经验、不卸下 | P1 | **完成**（EM-01～02；方案 A；issues `.scratch/insave-equip-magicbook-ui/`） |
| D-068 | InSaveShell 魔法书槽弹窗（UI-023）：左下「魔法书」打开；共享 `BookRow.prefab`；任意两槽 `TrySwap`（含空槽）立即 persist + `Changed`；AM 演出 BookRow 同步；槽 0→5 左→右即 Step2 启动顺序；无仓库；装入仍 GM `TryEquip`；弹窗删除见 D-072 | P1 | **完成**（EM-01/03；方案 A；issues `.scratch/insave-equip-magicbook-ui/`） |
| D-069 | 士兵战斗技能施放垂直：PushMap 忠诚兵按 `SoldierSkills`+`SkillConfig` 自动施放 `Skill_03` 连发（占用普攻通道走方案 D 连续 3 次）；`SkillCooldown` 公式驱动；Mode2 释放后进 CD；`ΣSkillBonus≠0` 时施放后再 roll；Rebel 不施放主动技；`Skill_01` 格挡（SC-02 方案 B：独立被动钩子，怪物普攻命中按等级 10%～30% 伤害→0 仍判命中）；`Skill_02` 舒适（SC-03 方案 A：满血时 Outgoing +5%～+25%，连发每击独立检查） | P1 | **完成**（SC-00/01/02/03；方案 C+B+A；issues `.scratch/soldier-skill-cast/`） |
| D-070 | Mode2 士兵栏悬浮框技能效果指示器（UI-021）：每个技能 `Icon` 右上角常驻 5×5 方块；绿 = `SkillConfig.EffectImplemented=1`，红 = `0`（缺行/缺列当 0）；不挡射线；Mode1 无框。Demo 初值 `Skill_01`/`Skill_02`/`Skill_03`=1，其余=0 | P1 | **完成**（方案 A：`Icon/EffectStatus` 子节点） |
| D-071 | 战斗技能图标（UI-025）：PushMap 士兵 `Skill_03` 提交与 `Skill_01` 格挡成功在该兵头顶显示 35×35 图标（静止 0.6s 后世界 +Z 上飘 0.3s 淡出；未结束图标向屏幕右排、间距 4px、消失后左移靠齐）；`Skill_02` 满血脚下 20×20 持续且生效瞬间同时头顶飘一次，受伤收起；同 SkillId 脚下只保留 1 个；`CombatDead` 立即清该兵图标；相机变焦屏幕像素不变；缺图空框；Defend 不接线 | P1 | **完成**（方案 A：士兵子节点 SpriteRenderer + 正交相机像素换算；issues `.scratch/combat-skill-icon/`） |
| D-072 | InSaveShell 魔法书弹窗删除（UI-023）：点占用 `BookSlot` 在该槽正下方出现「删除」；空槽不出现；点「删除」须二次确认；确认后 `TryUnequip` 清该槽（不补位、无仓库=从当前存档删除、不可恢复）立即 persist + `Changed`；AM 演出 BookRow 同步；拖拽开始隐藏按钮 | P1 | **完成**（方案 A：弹窗浮动按钮；不改共享 BookRow） |
| D-073 | PushMap 士兵技能 `Skill_04`～`Skill_12` 战斗效果（方案 B+）：`SkillEffectKind` 登记制 + `EffectParams` 表驱动 + `CombatStatusService` + `SkillEffectPipeline`；Session **禁止**按 `SkillId` `if/switch`；9 技能 Lv1～5 可手验；Mode2 `EffectImplemented=1`（UI-021 绿）；与 D-069 / D-071 共存；`Skill_01`/`Skill_02`/`Skill_03` 本片保留 `SoldierSkillCast` 硬映射 | P1 | **完成**（SE-00～09；方案 B+；issues `.scratch/soldier-skill-effects/`） |

| D-074 | Mode2 布阵编辑器内“一键上阵”：`FormationEditorRoot_Mode2` 左侧 `CompleteButton` 左邻按钮；点击后收集当前地图 `FormationClassZone`；将**尚未上阵**的士兵按其 `WarriorInstance.ClassId` 分组并随机放入对应职业区（区内非重叠，按区内候选点遍历+占用 Footprint 分离；遇无区/无空位则留池）；已在战场上的士兵不被重置；**PushMap/SearchExtract Prepare** 样例图须含区（推图锚 `WP_Start`） | P1 | **完成**（方案 B：按钮 + `OneClickFormationDeployService`；PushMap_Demo_* 补区） |
| D-097 | Mode2 布阵编辑器内“一键下阵”：`FormationEditorRoot_Mode2` 在「一键上阵」**正上方**增加按钮；点击后对当前全部已上阵士兵逐一 `TryUndeploy` 退回 SoldierBar（栏内格取消变亮）；无已上阵则 no-op；随后裁剪战术组（成员移出；剩余 &lt; Min 解散）。不改地图区、不改士兵池。UM / Defend·PushMap·SearchExtract Prepare 均可用 | P1 | **完成**（运行时按钮 + `FormationEditorController` 批量下阵） |
| D-075 | Mode2 商店关卡玩法类型：样例运作表 Stage1 `GameplayType=Shop`；全屏 Prefab `ShopStageRoot`；`ShopStageModule` 关闭 → `TryAdvanceStage` 进入 Dig；InSaveShell 左下「商店」保留局外 overlay（Shop 阶段已开 no-op）；`GameplayConfigId` 忽略 | P1 | **完成**（方案 A：Prefab-first + `IStageModule`） |
| D-076 | Mode2 商店左侧已拥有 ICON + 出售：摘要文本下展示装备/魔法书图标；点占用图标出现「出售」+ `ItemCatalog.SellPrice`；`ConfirmDialog`（sorting ≥ 201）确认后 `ShopSellService` 移除并发精魂；装备整件删除不退经验；魔法书 `TryUnequip` 不补位；UI-022 仍只读 | P1 | **完成**（方案 B：`ShopSellService`） |
| D-077 | 主角装备「炸药」`Equip_Explosives`：5 级表行 + Dig 事件 Token（玩家 DigAction 消除坟墓 100% 投掷炸药桶；**爆炸清坟不连锁**）+ 抛物线 0.5s / 引信 0.8s / 半径 2 伤害 13/18/23/28/33 + `ZYT_1` 精灵 + 贴地红圈 0.5s + Dig HUD GM 发放/划入 | P1 | **完成**（方案 B：`DigExplosiveScheduler` + Dig Views） |
| D-078 | 主角装备「引雷」`Equip_Elctr`：5 级表行 + Dig 定时事件 Token（间隔 15/13/11/9/7s，阶段开始后先等完整间隔）+ 随机坟或随机可放点落雷 + 命中坟无 LootDrop / 不触发炸药 + LootDrop 扫描 `IsPrimaryHand=1` 等权随机 → `ClassRestrict`+`RaceId` 入士兵池 + `Elctr_0`～`Elctr_3` 序列帧（0.05s/帧）+ 坟位待机 2s + Dig HUD GM 发放/划入 | P1 | **完成**（方案 A：`DigLightningScheduler` + Dig Views） |
| D-079 | 主角装备「探测器」`Equip_Detector`：5 级表行 + 静态键 `DigProcessSpawnCountBonus`（L1～5 = +1～+5，并入 Dig caps）+ 过程生成 `SpawnRate` 的 **M** 加法（**不**改 N、**不**改开局坟数）+ ItemCatalog / Mode2 商店池 + Dig HUD GM 发放/划入 | P1 | **完成**（方案 A：caps 静态键） |
| D-080 | 主角装备种族信物 `Equip_HumanToken` / `Equip_ElfToken` / `Equip_OrcToken`：各 5 级表行 + 静态键 `GraveSpawnWeightBonus`（L1～5 对应品质带权重累计 10/15/20/25/30；表缺席视为 0 再插入）+ ItemCatalog / Mode2 商店池 + Dig HUD GM 发放/划入；Mode2 Prefab/Catalog 覆盖 Q16–Q27（Q21–Q27 可占位） | P1 | **完成**（方案 A：复用矿灯 `GraveSpawnWeightBonus`） |
| D-081 | 进档难度 Hub（UI-029）：**新建**进档打开三栏等宽**同屏**（各约 1/3）；悬停显示难度描述；**任意难度**点击 → 沙盘 UI-036（D-098）。**进入占用档**跳过 Hub，直开沙盘（默认 `Diff_Normal`）。困难/地狱不再 Toast。无栏内 LevelSelect；无 MapHost；左下/右上按钮位置不变；`InSaveShellPanel` 独立 Prefab。**旁注：** `DifficultyConfig` 表驱动解锁/描述/通关奖已入 SPEC，Demo Hub 文案接线后置 | P0 | **更新**（入口改沙盘；方案 C Hub 仅新建/工具关卡） |
| D-098 | 沙盘入口（UI-036 / 方案 A）：表 `Level_SandboxNodeConfig` 按 `DifficultyId` 横向排方格（玩法名 + 剩余次数）。点击进现有挖坟 `DigStageRoot` / 商店 / 自动造兵；结束回同一难度沙盘，不经 `TrySelectGameplayOption` / `TryAdvanceStage`，不改各玩法模块内部与 UI-031。无解锁前置。占用档与 `LevelEnded` 回沙盘（默认 `Diff_Normal`）。**次数只展示、COC 占位**已由 **D-099** 改为存档剩余次数与 COC 战斗 | P0 | **入口已落地**（issues `.scratch/coc-sandbox/`）；次数与 COC 战斗见 D-099 |
| D-099 | COC 战斗（UI-037 / UI-038 / §3.21）：沙盘 `CocCombat` 进入即战斗（`GameplayState.CocCombat`）。底部按 `ClassId` 洒兵；战场迷雾三态 + 占领点永久亮起；击杀唯一最终 BOSS 通关回沙盘；单局失败（退出，或场上全死且库空）扣 1 次，扣到 0 回 Title，否则回沙盘。已洒出的士兵不回库。占领点激活与剩余次数写入存档；重进重置怪、迷雾探索与场上单位。不布阵、不护盾、不失控、不发经验、不弹推图结算 | P1 | 次数存档切片 01 已落地；空地图进入切片 02 已落地；刷怪待机切片 03a（方案 A）已落地；洒兵战斗切片 03b（方案 A）已落地；走向最终 BOSS 切片 03c（方案 A）已落地；胜负切片 03d（方案 A）已落地；迷雾静态片 04a（方案 A）已落地；临时亮起 / 变暗切片 04b（方案 A）已落地；占领切片 05（方案 A，同格第四态）已落地；长按展开切片 06 已落地（issues `.scratch/coc-combat/`） |
| D-082 | Mode2 魔法书 Token **命中**附带体型放大：`VisualModelScale` ×`WarriorVisualModelScalePerHit`（样例 1.15，可叠）后夹 `WarriorVisualModelScaleMax`（样例 3）；可与 `Style_ScaleModel` 通道同次再 ×`VisualIntensityAdd`；`BodyRadius`/`AttackRange` 仍 ×k；空 `VisualStyleId` 命中也放大；存档→布阵/Defend/PushMap | P1 | **完成**（选定方案：命中步进 + Style_ScaleModel 并存夹紧；Correctness 菜单 `Run Warrior VisualModelScale Correctness (D-082)`） |
| D-083 | 怪物尸体投射（抛物线击飞+砸击合一）：`distance≥DeathDie2KnockbackThreshold` 时飞行扫掠+落地砸其它存活怪；`OutgoingDamage×DeathCorpseSmashDamageMul`；同目标只结算一次；砸死不连锁；`MonsterCombatDead` 亦飞砸后 Delay→倒放；Defend+PushMap | P1 | **完成**（方案 A：Session `TryApplyCorpseSmashDamage` + View 抛物线/扫掠；Correctness 菜单 `Run Corpse Projectile Correctness Checks (D-083)`；issues `.scratch/corpse-projectile/`） |
| D-084 | 战术阵型 TacticalFormation（方案 A）：魔法书 `GrantFormationSkill` 授予阵型技能；Pattern 槽位 + 整阵拖拽；Defend+PushMap 虚拟中心 + `FormationSlot` + 接敌 leash；阵亡 &lt;Min 运行时解散；属性/专属技能 overlay；专表 `TacticalFormationConfig`。**成员关系**（自动、每种最多 1 组）由 **D-093** 修订 | P1 | **完成**（方案 A；TF-01～06；框架已落地。自动单实例组阵见 D-093，规则已锁、编码未做） |
| D-085 | 布阵战术阵型小队条（UI-030）：原为左侧已 snap 小队点选高亮。**D-093** 改为阵型目录创建按钮；高亮改由 UI-035 卡牌驱动 | P1 | **完成**（原方案 A 已接线；行为以 D-093 / UI-030 / UI-035 为准） |
| D-086 | 关卡 Stage 多选一 + 路线选择（UI-031）：运作表挂 `GameplayOptionId1..5` + 可选 `RouteMapAssetId` + Stage1 `UnlockLevelId`；子关卡表承载 Type/Config/图标/文案/Reward/UnlockNext（**不含**地图坐标）；每关地图 Prefab `LevelRouteMap_{LevelId}` 承载底图+`GameplayOptionId` 钉点；进关先开路线图；**Box 顶全量 LevelId 页签**（未解锁灰禁+Toast；切换已解锁=`TryEnterLevel`；默认已解锁末项）；有地图 Prefab 时选项钉坐标、**场景仅 Icon、悬停 Tips 按 GameplayType 分型（Dig=TipMessages；Shop/AM/UM=IconAssetId2+Description；PushMap/SE/Defend=IconAssetId2+Reward 图标）**；**地图 Icon 三态**（Cleared Checkmark / Selectable·Running 慢闪+±10% 缩放 / Locked 变暗）；**通关返回地图：对准刚通关→停顿 0.5s→平滑至新解锁前沿**；同 Stage 多选一；**同 Stage 已通关后兄弟 Locked 变暗不可选**（快照 UiState + TrySelect 双重门闩）；通关发奖并解锁下一 Stage 选项；空 UnlockNext → 关卡胜利；壳 Prefab + 每关地图 Prefab | P0 | **更新**（同 Stage 兄弟锁定；地图 Icon 三态；LevelId 门闩；通关返回镜头；地图 Icon+Tips；方案 C 钉点；Play Mode 由负责人勾选） |
| D-087 | Mode2 搜打撤 SearchExtract（方案 A）：子关卡 `GameplayType=SearchExtract`；独立 `SearchExtractStageModule`+Session；有序搜集点进圈倒计时+方向波次刷怪+布阵中心重定位；单点胜利无敌停刷清怪+UI-032；全灭整关 LevelFailure；每点奖励+子关卡 Reward 分离；**非** CampaignMode | P1 | TBD（规则库 SE-00～SE-09 已关；**字段工作坊已签字**；SE-01～SE-09 已落地；全灭 AbortLevel 可复现；**P1 HoldFraming SE-CAM-00～03 已关**（方案 B；样例锁定初值；手验 `.scratch/search-extract-hold-camera/issues/03-tune-handcheck.md`）；**D-087 Demo 验收须负责人 Play Mode 勾选手验**；issues `.scratch/mode3-search-extract/`） |
| D-088 | 关卡路线进度存档（方案 A）：按槽+CampaignMode 持久化已通关 `GameplayOptionId`；`TryEnterLevel` 水合 Cleared 并派生 Unlocked；通关立即写回；进行中选项不存；整关胜利保留 Cleared；删档清键 | P0 | **完成**（`LevelRouteProgressService` + Driver 水合；Play Mode 由负责人勾选） |
| D-089 | 战斗指示器（UI-033 / 方案 A）：PushMap + SearchExtract **仅 Combat** 显示顶中敌我单位格 HUD（`y=-10`，`scale=0.75`）；`CenterBg` 内左右存活数（BestFit 26～42；敌方开场未现身前为 `?`）；简画按职业/怪物表；HP% 四色；永久死亡灰+X 0.5s 后移除重对齐；可复活怪算活着；叛变不入两侧；单行截断（不换行）；0.2s 轮询 Snapshot（不改战斗热路事件）；Defend 不接线 | P1 | TBD（issues `.scratch/combat-indicator/`） |
| D-090 | 离屏刷怪边缘提示（UI-034 / 方案 A）：PushMap + SearchExtract **仅 Combat 真实刷怪**；判定=该组 `basePos` 视口外；边缘 `EnemyAttack_1` 开场闪红 2 次（0.4s）后常亮 2s、放大 1.3；Prepare 预览不出；同帧多组独立；离 Combat 清理；Defend 不接线 | P1 | TBD（issues `.scratch/offscreen-spawn-hint/`） |
| D-091 | 主角装备「复活铲」`Equip_ReviveShovel`（Mode2）：5 级表行 + 玩家 DigAction 清坟按级 20/40/60/80/100% 掷 `DigOnGraveClear`；复用引雷主要手扫描→入士兵池 + `DigLightningPreviewSec_2` 预览（**不**播闪电、**不**跳过掉落）；爆炸/引雷清坟不触发；Dig HUD GM 发放 | P1 | **完成**（方案 A：`DigReviveShovelEffectConfig` + `DigSessionService`） |
| D-092 | 布阵战术阵型旋转与滚轮缩放（方案 A）：整阵左键按住/拖动时 `Q`/`E` 绕阵心即时 ±15°；已有组再裁剪时保留玩家朝向；布阵滚轮缩放步进/夹限同 Combat（`CameraZoomStepPerNotch` / `CameraOrthoSizeMin`/`Max`）；指针在士兵栏等 UI 上忽略 | P1 | **完成**（方案 A：`TryApplySquadYawDelta` + `FormationEditorController`） |
| D-093 | 战术阵型多组与等级：左缘按钮手动建组（同 `FormationId` 可多组；一人一组；不满 Max 也可建）；等级=`Floor(组内 ClassLevel 平均)`，属性取 ≤ 该值的最大 `FormationLevel` 行；组写入布阵存档；顶中卡牌（最多 8 张可见、左右滑）选中后头顶图标 + 卡下解散；战斗每组独立虚拟中心，死亡后仍 ≥Min 则重算等级。不再自动成组。目录按钮已上阵不足时从士兵栏未上阵补人；搜打撤倒计时各组阵心相对全体上阵质心平移到采集点。**接敌离槽由 D-094 修订** | P1 | TBD（TFG-01～05 已编码；待验收） |
| D-094 | 战术阵型守槽（方案 A）：入组成员 `GoalKind` 保持 `FormationSlot`，接敌不再认领 AttackSlot。未到 `SlotArriveEpsilon` 先回槽。已在槽上且敌人中心在**组朝向**前方 `FrontArcDegrees`（缺省 90）内并进入该兵 `AttackRange` 才暂停挥刀；扇形外不追。守槽时该成员软碰撞 incoming 修正为 0。`LeashRadius` 保留但不再驱动离槽。死亡或 Rebel 腾槽后，若仍 ≥Min，活着成员中槽下标最大者补到更小的空编号（不整体压缩）；低于 Min 仍只解散该组。属性继续 `StatModifiers` StatMul。未入组士兵仍追击。PushMap + Defend + SearchExtract | P1 | **完成**（方案 A；TFH-00～04；issues `.scratch/tactical-formation-hold/`） |
| D-095 | 阵型默认八向（方案 A）：已入组成员的默认八向 = 该组 `FacingYawDegrees` 量化到 45° 扇区（0°=世界 +Z=北）。布阵预览在入组与 `Q`/`E` 改朝向后跟着换；未跨扇区不换精灵；未入组预览与士兵栏缩略图仍朝南。战斗中走路与待机都跟该朝向（PushMap `FacingTurnRate` 转向时一起转；Defend / 搜打撤速率 0 则保持布阵朝向）。开始攻击仍朝敌人一次并冻结到本次攻击结束，结束后回到阵型朝向。走跑仍由 steer 决定。未入组战斗移动仍跟 `LastDesired`。不改寻路、槽位、扇形攻击。PushMap + Defend + SearchExtract | P1 | **完成**（方案 A） |
| D-096 | 阵型贴身可打（方案 A）：已在槽上时，若敌人中心在前方扇形外，但双方身体贴靠（XZ 中心距 ≤ 双方 `BodyRadius` 之和），仍可原地暂停挥刀；不改 `GoalKind`、不追击、不认领 AttackSlot。开始攻击仍朝敌人一次（同 D-095）。扇形内仍按 `AttackRange`。多名候选取最近（扇形内进距与贴身扇外合并比较）。未贴身且扇外仍不追。PushMap + Defend + SearchExtract | P1 | **完成**（方案 A） |

**Demo 范围外（仍排除）：**

- 魔法书从弹窗装入（装入仍 Tools GM `TryEquip`；槽排序见 D-068；弹窗删除见 D-072）；其余未实现效果行（「还原」`RaceWeightPick`、「战士强化」`StatMul`/`Primary`、职业进阶 `ForceClass` 已实现）
- 推图战（PushMap）完整 polish / 副本玩法正文（规则与 ModeSelect 模式2入口已落地 §3.14 / D-044；细节见 `.scratch/push-map/issues/`）
- 完整技能效果表**解析 Description 自然语言**；Mode1 战斗技能；怪物 `Skills` 施放；Defend 技能接线（本 Demo PushMap `Skill_03` 连发 + `Skill_01` 格挡 + `Skill_02` 舒适见 D-069；`Skill_04`～`Skill_12` 见 D-073 EffectKind 登记制）
- 正式美术与动画 polish（临时 Prefab / 占位资源允许；禁止运行时引用 `SmallScaleInt/`）
- 完整存档序列化 schema（超出槽占用、士兵池、布阵、**关卡路线进度**及流水线所需的最小持久化字段；仓库/经验/科技等仍 TBD）
- 精确 OutsideMap 出生几何、完整障碍烘焙细则（Demo 最小约定见 §3.12 / [SPEC_04 §9.7](SPEC_04_Technical.md)）
- 科技树节点具体数值/图标 polish 与功能系统名完整枚举（§3.13；画布方案 A 已落地，非本表 P0）
- 工具面板「设置」「关卡」及 D-061 / D-064 GM、D-067 / D-068 / D-069 / D-070 / D-071 / D-072 / D-073 / D-075 / D-076 / D-077 / D-078 / D-079 / D-080 / D-091 以外的后续功能；完整 polish；未写入本表的需求
- 打表全量 §9 列/类型校验（[SPEC_04 §14](SPEC_04_Technical.md) Demo 仅文件名+表头；schema 校验后置）

实现边界对照：[SPEC_04 §6](SPEC_04_Technical.md)。

### English

**Status: Defined (Meta shell + one Level-pipeline vertical slice)**

Suggested order: D-001–D-004 (Meta) → D-010 (Level driver) → Dig → UpgradeManufacture → Defend (D-020–D-043). Temp art allowed (Prefab paths must follow [SPEC_04 §13](SPEC_04_Technical.md) / §15; swap formal art later).

| ID | Criterion | Priority | Status |
|----|-----------|----------|--------|
| D-001 | Save UI with 3 slots: create / enter / delete (delete confirms) | P0 | Meta shell done (Boot) |
| D-002 | After enter: floating Tools; open / close ToolsPanel | P0 | Meta shell done |
| D-003 | Tools shows Settings + Level; Level opens difficulty Hub (three columns same-screen); any difficulty click → Sandbox UI-036 | P0 | **Level** → DifficultySelectHost → Sandbox (UI-029/UI-036); Settings → TechTree canvas |
| D-004 | Identifiable gameplay state; default Dig placeholder; in-Level driven by stage GameplayType | P0 | Meta placeholders + Debug cycle kept; in-Level driven by `LevelOperationDriver` via stage `GameplayType`; enter default center = difficulty Hub (D-081) |
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
| D-045 | CampaignMode gate: **this Demo** create/enter skip `CampaignModeSelect`, always Mode2; UI-014 retained; per-slot progress isolated by mode; Mode2 reads only `ConfigTables/Mode2/Csv`; delete clears both modes; **not** D-044 | P0 | Demo bypass (Approach A Hub slice) |
| D-050 | Mode2: sample LevelOperation **Shop** → Dig → **AutoManufacture** → UpgradeManufacture → PushMap; after DigStageSummary enter auto craft; AutoManufacture ends without player confirm | P1 | Done (AM-03–08: `AutoManufactureStageModule` auto `TryAdvanceStage`; Mode2 `Level_01` Shop→Dig→AutoManufacture→UM→PushMap; Excel/CSV aligned; zero-craft Tips「无士兵可制造」; handcheck `.scratch/mode2-auto-manufacture/issues/08-level-sample-handcheck.md`; Shop stage = D-075) |
| D-051 | Mode2 AutoManufacture: loop craft into temp warehouse with min recipe (Head+Torso+2Arm incl. PrimaryHand+2Leg); no Spirit/Control; class from hand ClassRestrict; leftovers stay in Warehouse | P1 | Done (AM-03–06: pick/class/base→hook→appearance+name→temp flush→`WarriorPool`→clear+deploy; empty SoulId, Control=0, AttackMode←ClassConfig; handcheck AM-08) |
| D-052 | Mode2: after batch, clear formation; auto-deploy by `PlacementOrder` + `FormationClassZone` (separation); then enter UM | P1 | Done (AM-06 Approach A: in-zone spiral + BodyRadius; `FormationClassZone` **IsoDiamond** (same as WalkSurface; drop OBB/IsoTileYaw); batch Ids only; handcheck AM-08 / FZ-01–02) |
| D-053 | Mode2 UM: hide manual manufacture; keep upgrade Modal + editable formation; hide ControlPower HUD; in-editor `CompleteButton` (above SoldierBar right; visible UM+Prepare; UM wires stage end); Prepare `StartBattleButton` stacked above Complete | P1 | Done (Approach C: `UpgradeManufactureStageRoot_Mode2` + `FormationEditorRoot_Mode2`; Catalog Resolve by CampaignMode; handcheck AM-08; Complete in Mode2 diffs) |
| D-054 | Mode2 UM: "Manufacture Record" to the right of Formation opens read-only popup; last AutoManufacture batch summaries (name/race/class); empty 「本批无士兵」; next batch overwrites; survives re-enter save; Mode1 has no button | P1 | Done (Approach A: `AutoManufactureBatchRecordService` + Mode2 Modal; `UmAssetBuilder` Mode2 append / runtime Ensure) |
| D-055 | Mode2 AutoManufacture presentation (UI-016): after batch play StepA rain of consumed body parts (ArtAssetId; 0.3s×3–5; 2D pile) → StepB per soldier circle flash + mystery rises from circle center in parallel with 6 book pulses (1/6 per slot + flash at peak) + that soldier's pile pieces stagger-fly into mystery center with red flash → morph Idle in place at LandY → loop → StepC deploy by final ClassId → UM + auto-open Formation; center soldier row hidden by default (kept); 0 craft Tips + skip and no auto-open; Mode1 has no UI | P1 | **Updated** (v0.84.51 body absorb fly-in; 1A+2A) |
| D-056 | Soldier visuals: when `BodyAppearanceConfig.AppearanceId` has Art-ready bake under `Art/Characters/Appearances/{Id}/` (Controller + Idle Sprite), game Prefab `Prefabs/Defend/Warriors/{Id}.prefab` (root+`Visual`) must exist and bind Defend/UM catalogs; missing bind → no visual in formation/combat/presentation | P1 | Done (WA-01 / Approach B: `WarriorAppearancePrefabAssembler` From-Art + union catalog refresh; added `App_0_*`/`App_4_41`/`App_5_51`, extended Human/Elf/Orc `App_1_*`/`App_2_*`/`App_3_*`) |
| D-057 | Sample `Ground_*` **and** `PushMap_Demo_*` `FormationClassZone` cover **every** current-mode `ClassConfig.ClassId` (no zone → auto/one-click deploy stays in pool); Mode2 must include `Class_DarkMage`/`Class_Guardian` etc.; PushMap sample zones anchor at `CameraFollowPath/WP_Start` (else DigMapBounds.Center) | P1 | **Updated** (full ClassId + HalfExtents `(3.85, 2)`; Ground Ensure unchanged; PushMap Ensure menu anchors WP_Start) |
| D-058 | Mode2 MagicBook Warrior Enhance: equipped `MagicBook_WarriorEnhance` on AutoManufacture **Step2 that slot's pulse** adds 15% of body Σ StatBonus(class PrimaryStat) to Base for `Class_BaseWarrior` / `Class_Warrior` only (stackable; no race filter); other classes unchanged; baked until PermanentDeath; Dig HUD GM can equip | P1 | **Updated** (apply point → Step2 per-slot pulse; includes bone warrior) |
| D-059 | ProtagonistEquipment Dig vertical: load `ProtagonistEquipmentConfig` (Mode1+Mode2) + warehouse Service/persist + Dig caps tech+equip additive merge + Dig HUD GM handcheck (grant `Equip_IronShovel` / common Exp); warehouse read-only UI = D-067; level-up / spend common Exp / unequip UI and Manufacture·Combat tokens **deferred** | P1 | **Done** (PE-01–PE-04; Approach A; issues `.scratch/protagonist-equipment/`; Demo gear=`Equip_IronShovel` Iron Shovel) |
| D-060 | ProtagonistEquipment Miner Lamp `Equip_MinerLamp`: 5-level table (ExpToNext/ConvertExp=1) + Q4/Q5/Q6 spawn-weight cumulative +10 at current row (absent treated as 0) + Dig HUD GM grant/spend handcheck | P1 | **Done** (PE-05–PE-08; Approach A; issues `.scratch/protagonist-equipment/`) |
| D-061 | ToolsPanel Demo GM: Grant Protagonist Equipment (distinct EquipId; pick row → nested level picker → `DebugGrantAtLevel` overwrite level / `CurrentExp=0`) + Grant MagicBook (full MagicBookConfig, one click `TryEquip`; unique already equipped / slots full fail); GmGrantListPanel + LevelPicker (UI-019); Dig HUD GM still `TryAcquire`; warehouse read-only / MagicBook reorder = D-067 / D-068 | P1 | **Done** (TP-00–02 + level-picker iteration; Approach A) |
| D-062 | Soldier-skill vertical: load `SkillConfig` (Mode1+Mode2, incl. `IconAssetId`) + `ClassConfig.DefaultSkillIds` + pool persist + Mode1 manufacture grant + Mode2 craft grant/`SoldierSkillLevelAdd` (Step2 that slot's pulse); combat cast = **D-069** | P1 | **Done** (SS-01–04; cast vertical = D-069) |
| D-063 | Mode2 MagicBook class advance: equipped `MagicBook_WarriorAdvance` (and siblings) on AutoManufacture **Step2 that slot's pulse** promotes matching `Class_*_0` to `Class_*` at 25% (exact ClassId; log hit/miss); on hit re-grant `DefaultSkillIds`; appearance/name/deploy zone follow final class; other classes unchanged; hand-check Tools Grant MagicBook | P1 | **Updated** (apply point → Step2 per-slot pulse; Deploy uses final ClassId) |
| D-064 | ToolsPanel Demo GM Add Soldier (UI-020): UM Formation **or** Mode2 Prepare (Defend/PushMap/SearchExtract, `FormationEditorRoot_Mode2`); Mode1 Defend Prepare still blocked; left panel class/race/count(1–999)/auto-deploy; no BodyAppearance RaceId+ClassAffinity(ClassName) match → Tips「找不到此种士兵！」keep panel / no pool; multi-match deterministic pick (`DefaultAppearanceId` in set > `AppearanceLevel==ClassLevel` > first table order; no uniform random in set); match → free grant + optional DeployBatch; UM Return bottom-right (Mode2 above Complete) | P1 | **Done** (Approach A; Mode2 Prepare gate) |
| D-065 | Mode2 soldier-bar hover tooltip (UI-021): pointer over occupied `SoldierSlot` shows ClassName, `{ClassLevel}级`, race `DisplayNameKey`, `BaseClass`/`PromoteClass` (hide badge if empty), static MaxHP + Str/Agi/Int with「(主属性)」on `PrimaryStat` row, instance `SoldierSkills` icons (`Resources/UI/Skills/{SkillId}`) + `DisplayName`; hide on leave/scroll/lift; UM and Defend/PushMap Prepare; Mode1 none | P1 | **Done** (Approach A; `FormationSoldierHoverTooltipView`) |
| D-066 | Mode2 scale-model visual: on hit of `VisualStyleId=Style_ScaleModel` (alias `放大模型`), instance `VisualModelScale` multiplies by `VisualIntensityAdd` (k); world `Visual.localScale=(k,k,k)` and `BodyRadius`/`AttackRange` ×k (coexists with AllIn1 material); UI-016 live card preview **does** apply k; formation bar thumbs **do not**; Mode1/GM grant k=1 | P1 | **Done** (VS-00–03; Approach A; issues `.scratch/visual-scale-model/`; card RT follows world Instantiate) |
| D-067 | InSaveShell equipment warehouse read-only popup (UI-022): bottom-left Equipment opens centered Modal; list `OwnedEquips` name/level/description/icon; empty 「尚未拥有装备」; GM grant refreshes via `Changed`; no level-up / spend common Exp / unequip | P1 | **Done** (EM-01–02; Approach A; issues `.scratch/insave-equip-magicbook-ui/`) |
| D-068 | InSaveShell MagicBook slots popup (UI-023): bottom-left MagicBook; shared `BookRow.prefab`; any two slots `TrySwap` (incl. empty) persist immediately + `Changed`; AM presentation BookRow syncs; index 0→5 left→right = Step2 start order; no warehouse; grant still GM `TryEquip`; popup delete = D-072 | P1 | **Done** (EM-01/03; Approach A; issues `.scratch/insave-equip-magicbook-ui/`) |
| D-069 | Soldier combat skill-cast vertical: PushMap loyal soldiers auto-cast `Skill_03` burst from `SoldierSkills`+`SkillConfig` (occupies AA channel; 3× scheme D); `SkillCooldown` formula drives CD; Mode2 CD starts after cast commit; post-cast LOC re-roll if `ΣSkillBonus≠0`; Rebels do not cast actives; `Skill_01` block (SC-02 Approach B: independent on-hit hook; enemy AA hit → 10%–30% by level, damage→0 still a hit); `Skill_02` Comfort (SC-03 Approach A: full-HP outgoing +5%–+25%; each burst hit checks independently) | P1 | **Done** (SC-00/01/02/03; Approaches C+B+A; issues `.scratch/soldier-skill-cast/`) |
| D-070 | Mode2 soldier-bar tooltip skill-effect indicator (UI-021): each skill `Icon` keeps a 5×5 square at top-right; green = `SkillConfig.EffectImplemented=1`, red = `0` (missing row/column → 0); does not block raycasts; Mode1 has no tooltip. Demo seed: `Skill_01`/`Skill_02`/`Skill_03`=1, others=0 | P1 | **Done** (Approach A: `Icon/EffectStatus` child) |
| D-071 | CombatSkillIcon (UI-025): PushMap `Skill_03` commit and `Skill_01` block success show a 35×35 icon above that soldier (hold 0.6s then world +Z rise 0.3s fade; live icons stack screen-right, 4px gap, compact left on despawn); `Skill_02` full-HP persist 20×20 at feet plus one overhead popup on activate, hide when damaged; same SkillId persist keeps 1; `CombatDead` clears that soldier’s icons immediately; zoom keeps screen pixels; missing sprite → empty frame; Defend not wired | P1 | **Done** (Approach A: soldier-child SpriteRenderer + ortho pixel scale; issues `.scratch/combat-skill-icon/`) |
| D-072 | InSaveShell MagicBook popup delete (UI-023): click occupied `BookSlot` → Delete under that slot; empty slots hide it; Delete requires confirm; confirm → `TryUnequip` clears that index (no compact; no warehouse = delete from current save; irreversible) persist immediately + `Changed`; AM presentation BookRow syncs; BeginDrag hides the button | P1 | **Done** (Approach A: panel-local floating button; shared BookRow unchanged) |
| D-073 | PushMap soldier skills `Skill_04`–`Skill_12` combat effects (Approach B+): `SkillEffectKind` registry + table-driven `EffectParams` + `CombatStatusService` + `SkillEffectPipeline`; Session **must not** `if/switch` on `SkillId`; all 9 skills Lv1–5 hand-checkable; Mode2 `EffectImplemented=1` (UI-021 green); coexists with D-069 / D-071; `Skill_01`/`Skill_02`/`Skill_03` keep `SoldierSkillCast` hard-map this slice | P1 | **Done** (SE-00–09; Approach B+; issues `.scratch/soldier-skill-effects/`) |

| D-074 | Mode2 formation editor “One-click deploy”: the `FormationEditorRoot_Mode2` button placed left of `CompleteButton`; click collects current-map `FormationClassZone`; deploy only not-yet deployed soldiers grouped by their `WarriorInstance.ClassId` and randomly place them into the matching class zone (in-zone non-overlap via candidate-point traversal + occupied Footprint separation; no zone / no free slot leaves them in pool); already-deployed soldiers are not reset; **PushMap/SearchExtract Prepare** sample maps must include zones (PushMap anchor `WP_Start`) | P1 | **Done** (Approach B: button + `OneClickFormationDeployService`; PushMap_Demo_* zones added) |
| D-097 | Mode2 formation editor “One-click undeploy”: `FormationEditorRoot_Mode2` adds a button **directly above** One-click deploy; click `TryUndeploy`s every currently deployed soldier back to the SoldierBar (cell highlight clears); no-op if none deployed; then prune tactical groups (drop members; remaining &lt; Min disbands). Does not alter class zones or the warrior pool. Available in UM and Defend/PushMap/SearchExtract Prepare | P1 | **Done** (runtime button + batch undeploy in `FormationEditorController`) |
| D-075 | Mode2 shop as Level gameplay type: sample Stage1 `GameplayType=Shop`; full-screen Prefab `ShopStageRoot`; `ShopStageModule` close → `TryAdvanceStage` into Dig; InSaveShell Shop button remains overlay (no-op while Shop stage open); ignore `GameplayConfigId` | P1 | **Done** (Approach A: Prefab-first + `IStageModule`) |
| D-076 | Mode2 shop owned ICON + sell on left panel: ICONs under summary; click → Sell + `ItemCatalog.SellPrice`; ConfirmDialog (sorting ≥ 201) → `ShopSellService` removes item and credits Spirit; equipment `TryRemove` without Exp refund; MagicBook `TryUnequip` no compact; UI-022 stays read-only | P1 | **Done** (Approach B: `ShopSellService`) |
| D-077 | ProtagonistEquipment Explosives `Equip_Explosives`: 5-level rows + Dig event token (100% throw barrel on player DigAction grave clear; **no chain from blast clears**) + parabola 0.5s / fuse 0.8s / radius 2 damage 13/18/23/28/33 + `ZYT_1` sprite + ground red ring 0.5s + Dig HUD GM grant/spend | P1 | **Done** (Approach B: `DigExplosiveScheduler` + Dig Views) |
| D-078 | ProtagonistEquipment Lightning `Equip_Elctr`: 5-level rows + Dig timed event tokens (interval 15/13/11/9/7s, first strike after a full wait) + random grave or random placeable point + hit grave skips LootDrop and explosives + scan LootDrop `IsPrimaryHand=1` equal-random → `ClassRestrict`+`RaceId` into WarriorPool + `Elctr_0`–`Elctr_3` (0.05s/frame) + 2s idle preview + Dig HUD GM grant/spend | P1 | **Done** (Approach A: `DigLightningScheduler` + Dig Views) |
| D-079 | ProtagonistEquipment Detector `Equip_Detector`: 5-level rows + static key `DigProcessSpawnCountBonus` (L1–5 = +1–+5, merges into Dig caps) + additive to process-spawn `SpawnRate` **M** (**not** N, **not** initial grave count) + ItemCatalog / Mode2 shop pool + Dig HUD GM grant/spend | P1 | **Done** (Approach A: static cap key) |
| D-080 | ProtagonistEquipment race tokens `Equip_HumanToken` / `Equip_ElfToken` / `Equip_OrcToken`: 5-level rows each + static `GraveSpawnWeightBonus` (L1–5 cumulative weights 10/15/20/25/30 on quality bands; missing table Id = 0 then insert) + ItemCatalog / Mode2 shop pool + Dig HUD GM grant/spend; Mode2 Prefab/Catalog covers Q16–Q27 (Q21–Q27 may be placeholders) | P1 | **Done** (Approach A: reuse Miner Lamp `GraveSpawnWeightBonus`) |
| D-081 | Enter-shell difficulty Hub (UI-029): **Create** opens equal-width **same-screen** columns (~1/3 each); hover description; **any difficulty** → Sandbox UI-036 (D-098). **Enter occupied** skips Hub → Sandbox (default `Diff_Normal`). Hard/Hell no longer Toast. No in-column LevelSelect; no MapHost; chrome unchanged; standalone `InSaveShellPanel` Prefab. **Note:** `DifficultyConfig` unlock/description/clear-reward rules are in SPEC; Demo Hub copy wiring deferred | P0 | **Updated** (entry is Sandbox; Approach C Hub for Create / Tools Level) |
| D-098 | Sandbox entry (UI-036 / Approach A): `Level_SandboxNodeConfig` lays out cells horizontally per `DifficultyId` (gameplay name + remaining count). Click enters existing Dig `DigStageRoot` / Shop / AutoManufacture; end returns to the same difficulty Sandbox, not via `TrySelectGameplayOption` / `TryAdvanceStage`, and does not change gameplay module internals or UI-031. No unlock order. Occupied enter and `LevelEnded` open the Sandbox (default `Diff_Normal`). **Display-only counts and the COC placeholder** are replaced by saved remaining counts and COC combat in **D-099** | P0 | **Entry landed** (issues `.scratch/coc-sandbox/`); counts and COC combat are D-099 |
| D-099 | COC combat (UI-037 / UI-038 / §3.21): Sandbox `CocCombat` enters combat immediately (`GameplayState.CocCombat`). Bottom cards deploy soldiers grouped by `ClassId`; battlefield fog has three states plus permanent capture light; killing the single Final Boss clears the node and returns to the Sandbox; a round loss (quit, or all field soldiers dead and the pool empty) decrements by 1, and reaching 0 returns to Title, otherwise to the Sandbox. Deployed soldiers do not return to the pool. Capture activation and remaining counts persist; re-entry resets monsters, fog exploration, and units on the field. No formation, shield, loss-of-control, experience, or PushMap settlement popup | P1 | Remaining-count slice 01 landed; empty-map enter slice 02 landed; spawn-idle slice 03a (Approach A) landed; deploy-fight slice 03b (Approach A) landed; Final Boss march slice 03c (Approach A) landed; win/loss slice 03d (Approach A) landed; fog / capture: static fog slice 04a (Approach A) landed; reveal / explored-dark slice 04b (Approach A) landed; capture slice 05 (Approach A, fourth grid state) landed; card-hold expand slice 06 landed (issues `.scratch/coc-combat/`) |
| D-082 | Mode2 MagicBook token **hit** appends body scale: `VisualModelScale` ×`WarriorVisualModelScalePerHit` (sample 1.15, stackable) then clamp `WarriorVisualModelScaleMax` (sample 3); same hit may also ×`VisualIntensityAdd` via `Style_ScaleModel`; `BodyRadius`/`AttackRange` still ×k; empty `VisualStyleId` hit still scales; persist → formation/Defend/PushMap | P1 | **Done** (chosen: hit step + Style_ScaleModel coexist + clamp; menu `Run Warrior VisualModelScale Correctness (D-082)`) |
| D-083 | Monster corpse projectile (parabolic knockback + smash unified): when `distance≥DeathDie2KnockbackThreshold`, flight sweep + landing smash other living monsters; `OutgoingDamage×DeathCorpseSmashDamageMul`; once per target; smash kills no chain; `MonsterCombatDead` also flies/smashes then Delay→reverse revive; Defend+PushMap | P1 | **Done** (Approach A: Session `TryApplyCorpseSmashDamage` + View parabolic/sweep; menu `Run Corpse Projectile Correctness Checks (D-083)`; issues `.scratch/corpse-projectile/`) |
| D-084 | TacticalFormation (Approach A): MagicBook `GrantFormationSkill`; Pattern slots + whole-squad drag; Defend+PushMap virtual center + `FormationSlot` + engage leash; dissolve when living &lt;Min; stat/exclusive-skill overlay; table `TacticalFormationConfig`. **Membership** (auto, max one group per formation) is revised by **D-093** | P1 | **Done** (Approach A; TF-01–06; framework landed. Auto single-instance grouping → D-093, rules locked, not coded) |
| D-085 | Formation tactical-squad strip (UI-030): originally left-edge snapped-squad select + soldier-bar highlight. **D-093** turns it into a catalog create button; highlight moves to UI-035 cards | P1 | **Done** (original Approach A wired; behavior follows D-093 / UI-030 / UI-035) |
| D-086 | Level Stage multi-pick + route select (UI-031): Operation mounts `GameplayOptionId1..5` + optional `RouteMapAssetId` + Stage1 `UnlockLevelId`; SubLevel holds Type/Config/icon/copy/Reward/UnlockNext (**no** map coords); per-level map Prefab `LevelRouteMap_{LevelId}` holds bg + `GameplayOptionId` pins; enter opens route; **all LevelId tabs atop Box** (locked gray+Toast; unlocked switch=`TryEnterLevel`; default last unlocked); with map Prefab, options pin to coordinates, **Icon only on map, hover Tips by GameplayType (Dig=TipMessages; Shop/AM/UM=IconAssetId2+Description; PushMap/SE/Defend=IconAssetId2+Reward icons); **map Icon tri-state** (Cleared Checkmark / Selectable·Running slow blink+±10% scale / Locked dim)**; **clear-return map: snap just-cleared → hold 0.5s → smooth to new frontier**; pick-one per Stage; **same-Stage Cleared locks uncleared siblings (dim + TrySelect/UiState dual gate)**; clear grants reward + unlocks next-Stage options; empty UnlockNext → victory; chrome Prefab + per-level map Prefab | P0 | **Updated** (same-Stage sibling lock; map Icon tri-state; LevelId gate; clear-return camera; map Icon+Tips; Approach C pins; Play Mode checkboxes for owner) |
| D-087 | Mode2 SearchExtract (Approach A): SubLevel `GameplayType=SearchExtract`; independent `SearchExtractStageModule`+Session; ordered gather points with zone countdown + directional wave spawns + formation-center relocate; point success invincible/stop-spawn/clear monsters + UI-032; wipe → Level failure; per-point loot separate from SubLevel Reward; **not** CampaignMode | P1 | TBD (rules SE-00–SE-09 closed; **field workshop signed**; SE-01–SE-09 landed; wipe AbortLevel reproducible; **P1 HoldFraming SE-CAM-00–03 closed** (Approach B; samples locked at initials; handcheck `.scratch/search-extract-hold-camera/issues/03-tune-handcheck.md`); **D-087 Demo accept needs owner Play Mode handcheck**; issues `.scratch/mode3-search-extract/`) |
| D-088 | Level route progress save (Approach A): persist cleared `GameplayOptionId`s per slot+CampaignMode; `TryEnterLevel` hydrates Cleared and derives Unlocked; write on clear; no in-progress option; keep Cleared after level victory; delete slot clears keys | P0 | **Done** (`LevelRouteProgressService` + Driver hydrate; Play Mode checkboxes for owner) |
| D-089 | Combat indicator (UI-033 / Approach A): PushMap + SearchExtract **Combat only** top-center ally/enemy unit-slot HUD (`y=-10`, `scale=0.75`); alive counts inside `CenterBg` left/right halves (BestFit 26–42; enemy `?` until first `>0` this battle); silhouettes from class/monster tables; HP% four tints; permanent-dead gray+X then remove after 0.5s and re-align; revivable monsters count as alive; Rebels excluded from both sides; single-row truncate (no wrap); 0.2s Snapshot poll (no combat hot-path events); Defend unwired | P1 | TBD (issues `.scratch/combat-indicator/`) |
| D-090 | Off-screen spawn edge hint (UI-034 / Approach A): PushMap + SearchExtract **Combat real spawns only**; gate = group `basePos` outside viewport; edge `EnemyAttack_1` 2 red blinks in 0.4s then hold 2s at scale 1.3; Prepare preview skipped; multi-group independent; clear on leave Combat; Defend unwired | P1 | TBD (issues `.scratch/offscreen-spawn-hint/`) |
| D-091 | ProtagonistEquipment Revive Shovel `Equip_ReviveShovel` (Mode2): 5-level rows + player DigAction grave-clear roll `DigOnGraveClear` 20/40/60/80/100% by level; reuse Lightning primary-hand scan → WarriorPool + `DigLightningPreviewSec_2` preview (**no** bolt VFX, **no** loot skip); blast/lightning clears do not trigger; Dig HUD GM grant | P1 | **Done** (Approach A: `DigReviveShovelEffectConfig` + `DigSessionService`) |
| D-092 | Formation tactical-squad rotate + scroll zoom (Approach A): while LMB holding/dragging a squad, `Q`/`E` instantly rotate ±15° about center; prune of an existing group keeps player facing; formation scroll zoom step/clamp same as Combat (`CameraZoomStepPerNotch` / `CameraOrthoSizeMin`/`Max`); ignore when pointer over soldier bar / blocking UI | P1 | **Done** (Approach A: `TryApplySquadYawDelta` + `FormationEditorController`) |
| D-093 | Tactical formation multi-group + level: left-edge button manually creates a group (many groups per `FormationId`; one group per soldier; partial Max allowed); level=`Floor(mean ClassLevel)`; stats from the greatest `FormationLevel` row ≤ that value; groups persist with the battle-formation save; top-center cards (at most 8 visible, scroll both ways) select a group → overhead icon + Disband under the card; combat gives each group its own virtual center and recomputes level after a death while still ≥Min. No auto-grouping. Catalog click fills a shortfall from undeployed SoldierBar soldiers; SearchExtract countdown shifts each group center by its offset from the army centroid onto the gather point. **Leaving the slot to engage is revised by D-094** | P1 | TBD (TFG-01–05 coded; pending playtest) |
| D-094 | Tactical formation hold (Approach A): grouped members keep `GoalKind=FormationSlot` and do not claim AttackSlot. They return to the slot until `SlotArriveEpsilon`. Once there, they pause and swing only when the enemy center is inside the **group facing** forward `FrontArcDegrees` (default 90) and within that soldier's `AttackRange`; outside the arc they do not chase. Incoming soft-collision correction is 0 while holding. `LeashRadius` remains but no longer pulls members off the slot. After death or Rebel, if the group is still ≥Min, the living member with the highest slot index steps into the lower vacated index (no compact); below Min still dissolves only that group. Stats stay on `StatModifiers` StatMul. Ungrouped soldiers still chase. PushMap + Defend + SearchExtract | P1 | **Done** (Approach A; TFH-00–04; issues `.scratch/tactical-formation-hold/`) |
| D-095 | Formation default 8-dir (Approach A): a grouped member's default 8-dir equals that group's `FacingYawDegrees` quantized to 45° sectors (0°=world +Z=north). The formation preview updates on join and when `Q`/`E` changes facing; the sprite does not change until a sector boundary is crossed; ungrouped previews and soldier-bar thumbnails stay south. In combat, walk and idle both follow that facing (they turn with PushMap `FacingTurnRate`; Defend / SearchExtract with rate 0 keep the editor facing). Attack start still snaps toward the enemy once and freezes until that swing ends, then returns to formation facing. Gait still comes from steer. Ungrouped combat movement still follows `LastDesired`. Pathing, slots, and the front arc are unchanged. PushMap + Defend + SearchExtract | P1 | **Done** (Approach A) |
| D-096 | Formation body-contact swing (Approach A): once on the slot, if the enemy center is outside the forward arc but the bodies touch (XZ center distance ≤ sum of both `BodyRadius`), the member may pause and swing in place; keep `GoalKind`, no chase, no AttackSlot claim. Attack start still snaps toward the enemy once (same as D-095). Inside the arc still uses `AttackRange`. Several candidates → nearest among in-arc-in-range and contact-outside-arc. Outside the arc without contact: still no chase. PushMap + Defend + SearchExtract | P1 | **Done** (Approach A) |

**Out of Demo scope (still excluded):**

- MagicBook grant-from-popup (grant still Tools GM `TryEquip`; slot reorder = D-068; popup delete = D-072); remaining unimplemented effect rows (Restore `RaceWeightPick`, Warrior Enhance `StatMul`/`Primary`, and class-advance `ForceClass` done)
- PushMap polish / dungeon gameplay body (rules + ModeSelect Mode2 entry landed §3.14 / D-044; details in `.scratch/push-map/issues/`)
- Full skill-effect table **natural-language Description parser**; Mode1 combat skills; monster `Skills` casts; Defend skill wiring (this Demo PushMap `Skill_03` burst + `Skill_01` block + `Skill_02` Comfort = D-069; `Skill_04`–`Skill_12` = D-073 EffectKind registry)
- Formal art / animation polish (temp Prefabs OK; **no** runtime refs to `SmallScaleInt/`)
- Full save schema beyond occupied flag + warrior pool + BattleFormation + **LevelRouteProgress** + minimal pipeline fields (Warehouse / Exp / Tech still TBD)
- Exact OutsideMap spawn geometry / full obstacle-bake detail (Demo-min in §3.12 / [SPEC_04 §9.7](SPEC_04_Technical.md))
- Full TechTree node values/icon polish & full feature-system enum (§3.13; canvas Approach A landed; not P0 here)
- Tools entries beyond Settings / Level / D-061 / D-064 GM / D-067 / D-068 / D-069 / D-070 / D-071 / D-072 / D-073 / D-075 / D-076 / D-077 / D-078 / D-079 / D-080 / D-091; full polish; anything not in this table
- Bake full §9 column/type validation ([SPEC_04 §14](SPEC_04_Technical.md) Demo: filename + header only; schema validation deferred)

Boundary: [SPEC_04 §6](SPEC_04_Technical.md).

---

## 3.9 关卡阶段流水线

### 简体中文

**状态：已定义（规则库；Demo 见 §3.8 D-010 / D-086）**

关卡由「关卡运作表」+「子关卡表」驱动。同一 `关卡ID` 的运作表行按 `阶段编号` **升序**组织 Stage。每 Stage 挂最多 **5** 套玩法选项 ID（`GameplayOptionId1..5`，空列忽略）。选项详情在子关卡表（`Level_SubLevelConfig`）：`GameplayType` / `GameplayConfigId` / 图标 / 标题 / 描述 / 奖励 / 解锁下一阶段选项。玩家在**同 Stage 内多选一**；通关后按 `UnlockNextOptionIds` 解锁 **仅下一 Stage（StageNumber+1）** 的选项，并返回路线选择界面（UI-031）。**可选条件：** 选项须在解锁派生集合内（Stage1 全部 ∪ 本 Level 已通关项的 `UnlockNextOptionIds`），且**所属 Stage 尚无任一已通关兄弟**。**同 Stage 排他：** 某 `StageNumber` 一旦有选项进入 Cleared，其余同 Stage 未通关选项 → `Locked`（地图 Icon RGB×0.4；不可点）；已通关项仍为 `Cleared`（不可重打）。`TrySelectGameplayOption` 与快照 `UiState` 双重门闩（不改 `_unlockedOptions` 派生集合）。

**表 1 — 关卡运作表字段（规则语义）**

| 字段 | 说明 |
|------|------|
| 关卡ID | 关卡标识；同 ID 多行 = 该关全部 Stage |
| 阶段编号 | 同关卡内顺序（升序）；一行 = 一个 Stage |
| 玩法选项ID1…5 | 本 Stage 挂载的子关卡主键；空=无；最多 5 套 |
| 难度ID | `DifficultyId`；FK → 关卡难度表；同 LevelId 多行须同值；空=不归属任何难度（不参与难度通关统计） |
| 路线底图资源ID | 可选；文件名无扩展名（例 `SubLevel_001`）；同 LevelId 多行取**首个非空**；空=该关无底图意图、走旧 Stage 行布局；有值时配合 Prefab `LevelRouteMap_{LevelId}` |
| 解锁 LevelId | `UnlockLevelId`；**仅读 StageNumber=1 行**（其它 Stage 行同列忽略）。空=本 LevelId **默认解锁**；填入 `Level_SubLevelConfig` 存在的 `GameplayOptionId` → 该子关卡已通关则解锁本 LevelId（跨 Level 允许）；填入 SubLevel 搜不到的值 → **永不可解锁**（正式入口永远进不去） |

**表 0 — 关卡难度表字段（规则语义；Demo Hub 接线后置）**

| 字段 | 说明 |
|------|------|
| 难度ID | 主键；样例 `Diff_Normal` / `Diff_Hard` / `Diff_Hell` |
| 显示名 | UI 栏标题 |
| 解锁所需难度ID | `UnlockRequireDifficultyId`：空=**初始解锁**；填入本表存在的 DifficultyId → 等该难度**已通关**后解锁本行；填入本表找不到的 Id → **不可解锁**（加载可 Warning） |
| 文字介绍 | 悬停/说明文案（将来替代 Demo 写死描述） |
| 难度通关奖励 | `ClearReward`：`ItemId;Count\|…`（经道具汇总表）；可空；**首次**判定难度通关时一次性发放 |

**难度解锁与通关（相对 LevelId 门闩 / 选项 UnlockNext 独立）**

| 规则 | 说明 |
|------|------|
| 难度已通关 | 归属该 `DifficultyId` 的**全部**去重 `LevelId` 均已达成关卡胜利（空 `UnlockNextOptionIds` → VictorySettlement）；无归属 LevelId 的难度行永不因关卡胜利而通关 |
| 难度解锁 | 空前置 → 初始解锁；前置难度已通关 → 解锁；前置 Id 不存在 → 永久不可解锁 |
| 通关奖励 | 首次从「未通关」变为「已通关」时发 `ClearReward`；空=无奖；已发放标记存档 schema **接线后置** |
| Demo | 表与规则已落地；`DifficultySelectHost` / 发奖 / 存档 **接线后置**（仍写死三栏与 Toast） |

**LevelId 解锁门闩（相对子关卡 `UnlockNextOptionIds` 独立）**

| 规则 | 说明 |
|------|------|
| 判定时机 | 正式进关 / UI-031 页签切换前；派生自 Stage1 `UnlockLevelId` + `LevelRouteProgress` 已通关集合 |
| 正式入口 | 普通难度 / UI-031 页签：`TryEnterLevel` **enforce**；默认进关 = **已解锁**去重 LevelId 列表末项；全无解锁 → Toast |
| UI-031 页签 | **全量**去重 LevelId 仍显示；未解锁 / 永不可解锁 → 灰禁，点击 Toast「关卡未解锁」；已解锁可切 |
| Tools「关卡」 | `LevelSelectPanel` **可 bypass** 门闩（GM 强制进任意 LevelId） |
| 商店 | `maxUnlockedLevelNumber`（LevelId 尾数）**本片不改** |

**表 2 — 子关卡表字段（规则语义）**

| 字段 | 说明 |
|------|------|
| 玩法选项ID | 主键；**不得跨 LevelId 复用**（钉点挂在所属关 `LevelRouteMap_{LevelId}`） |
| 玩法类型 | `Shop` / `Dig` / `AutoManufacture` / `UpgradeManufacture` / `Defend` / `PushMap` / `SearchExtract` → `GameplayState` |
| 玩法配置ID | 语义同旧运作表：Dig/Defend/PushMap/**SearchExtract** 查对应表；Shop/UM/AM **忽略** |
| 玩法图标 | 路线图展示；Demo `Resources/UI/Levels/{IconAssetId}` |
| 标题文字 / 描述文字 | 路线图展示 |
| 奖励 | `ItemId;Count\|…`（经道具汇总表）；可空；**通关时发放**且路线图展示 |
| 解锁下一阶段选项 | `OptId\|OptId`；目标须属于 StageNumber+1；**空 = 通关后关卡胜利** |

**路线地图 Prefab（方案 C，UI-031）：** 路径 `Assets/Prefabs/Level/LevelRouteMap_{LevelId}.prefab`。子节点名 = `GameplayOptionId`；`RectTransform.anchoredPosition` = 选项中心（底图左下 `(0,0)`，Y 向上；单位=展示宽 1450 UI 像素）。玩法选项 ID **只**来自运作表+子关卡表，Prefab **不**发明 ID。Editor 菜单（Mode2 CSV）：`Gravedigger2026/Level（关卡）/Ensure LevelRouteMap Prefabs (UI-031)（确保关卡路线地图预制体）` 建/贴底图并补缺钉；`Gravedigger2026/Level（关卡）/Sync LevelRouteMap Pins (UI-031)（同步关卡路线地图钉点）` 从表同步缺钉、警告表外多余钉（**不**覆盖已摆坐标）。钉点仅为占位 Transform，不是选项节点。运行时 Instantiate 选项节点，地图 Prefab 只提供底图 + 钉点 Transform。**地图模式：场景仅显示 Icon**（约 80×80）；悬停 Tips 为独立 Prefab `Assets/Prefabs/Level/OptionHoverTips.prefab`（嵌套于 `LevelRouteSelectRoot/Box`；Ensure 同步 `Resources/Prefabs/Level/`；运行时禁止 `BuildHierarchy`）按 GameplayType 分型（Dig=`TipMessages`；Shop/AM/UM=`IconAssetId2`+Description；PushMap/SE/Defend=`IconAssetId2`+Reward 图标行）。**打开/切页签后 `LevelRouteSelectView` 将 `MapContent` 竖滑 Y 定位到最新已解锁钉点居中（只动 Y）**。**选项通关返回地图模式：** Snapshot 一次性携带 `JustClearedOptionId` → View 先瞬时对准刚通关钉点，停顿 0.5s，再约 0.5s 平滑滚向当前最新解锁前沿（只动 Y；Driver 不写 Transform）。缺钉 → Warning，图标仍生成于 `(0,0)`。无该 Prefab（或无 `RouteMapAssetId`）→ 旧 Stage 行布局（完整选项卡）。

**阶段流转**

1. 进入关卡：先过 **LevelId 解锁门闩**（正式路径；Tools 可 bypass）→ 加载运作表 Stage → 解析选项 → **自 `LevelRouteProgress` 水合已通关并派生解锁** → **打开路线选择**（Stage1 全部非空选项默认可选，加上已派生解锁；**同 Stage 已有 Cleared 的兄弟除外**；UI-031 Box 顶展示**全量**去重 LevelId 页签，默认**已解锁末项**，未解锁页签灰禁+Toast，已解锁切换 = `TryEnterLevel`）；**不**立刻 Enter 玩法模块。
2. 玩家点选可点选项 → 应用该选项的 GameplayType + GameplayConfigId → `IStageModule.Enter`。
3. 选项结束条件：同旧各玩法（Shop 关闭、Dig 倒计时、AM 自动、UM 确认、Defend/PushMap 胜负、**SearchExtract「离开」或 LevelFailure** 等）。
4. **关卡失败**：同旧 → AbortLevel（**不**清除已存 Cleared）。
5. **通关成功**：发放子关卡 `Reward`（若有）→ 标记已通关 → **立即写入 `LevelRouteProgress`** → 解锁 `UnlockNextOptionIds` → **同 Stage 兄弟变 Locked** → 若解锁列表为空 → **VictorySettlement**；否则 **返回路线选择**。
6. 路线图：有 `LevelRouteMap_{LevelId}` 时竖滑该关地图 Prefab + 钉点选项（**仅 Icon**；悬停 Tips 按 GameplayType 分型（Dig=TipMessages；Shop/AM/UM=IconAssetId2+Description；PushMap/SE/Defend=IconAssetId2+Reward 图标））；**打开/切 LevelId 页签后初始竖滑 Y 滚到「最新已解锁」钉点居中**（优先 Selectable/Running 最大 Stage，否则 Cleared；同 Stage 取最大钉点 Y；无目标则底部；**只动 Y**）；**通关返回地图：对准刚通关钉点 → 停顿 0.5s → 平滑至新解锁前沿**；无地图 Prefab（或无 `RouteMapAssetId`）时竖版自下而上 Stage、横向完整选项卡；按解锁字段画相邻 Stage 连线；状态锁定/可点/已通关（**同 Stage 已通关后兄弟 Locked 变暗**）。

**路线进度持久化（`LevelRouteProgress`，方案 A / D-088）**

| 规则 | 说明 |
|------|------|
| 存什么 | 仅已通关 `GameplayOptionId` 集合（扁平 JSON；OptionId **不跨 LevelId 复用**） |
| 不存 | 进行中的选项 / 局内细节（Dig 半截、战斗 HP 等）；重进回到路线图 |
| 解锁派生 | `TryEnterLevel`：Stage1 全部 ∪ 每个已通关项的 `UnlockNextOptionIds`（须挂在本 LevelId） |
| 同 Stage 兄弟锁定 | 某 Stage 已有 Cleared → 其余同 Stage 未通关选项 `UiState=Locked` 且 `TrySelect` 拒绝；不从解锁派生集合移除 |
| 写回时机 | 选项通关（`TryAdvanceStage` / 推图结算 `CompleteLevelAfterBattleSettlement`）立即 `PlayerPrefs` |
| 整关胜利 | 保留 Cleared；再进该 LevelId 仍显示已通关、不可重打 |
| 失败/中止 | **不**清除已存 Cleared |
| 键空间 | 按槽 + `CampaignMode`（[SPEC_04 §6](SPEC_04_Technical.md)）；删档清两模式 |

```
EnterLevel
  → Load LevelOperation stages + SubLevel options
  → Hydrate Cleared from LevelRouteProgress → derive Unlocked
  → Show RouteSelect
  → Player picks unlocked option
  → Run option (GameplayType + GameplayConfigId)
  → On LevelFailure → abort (Cleared kept)
  → On clear → Grant Reward → Persist Cleared → Unlock next → empty? VictorySettlement : RouteSelect
```
### English

**Status: Defined (rules library; Demo — §3.8 D-010 / D-086)**

A Level is driven by Level Operation + SubLevel tables. Operation rows share `LevelId`, sorted by `StageNumber`. Each Stage mounts up to **5** option IDs (`GameplayOptionId1..5`). Option details live in `Level_SubLevelConfig`. Player **picks one** option per Stage visit; clear unlocks **only StageNumber+1** targets via `UnlockNextOptionIds`, then returns to RouteSelect (UI-031). **Selectable iff** in unlock-derived set (all Stage1 ∪ cleared options' `UnlockNextOptionIds`) **and** no sibling on the same Stage is Cleared. **Same-Stage exclusivity:** once any option on a `StageNumber` is Cleared, other uncleared options on that Stage → `Locked` (map Icon RGB×0.4; not clickable); the cleared option stays `Cleared` (no replay). Enforced by both snapshot `UiState` and `TrySelectGameplayOption` (unlock-derived set unchanged).

**Table 1 — Level Operation fields**

| Field | Notes |
|-------|-------|
| LevelId | Level id |
| StageNumber | Ascending; one row = one Stage |
| GameplayOptionId1…5 | SubLevel PKs; empty ignored; max 5 |
| DifficultyId | FK → DifficultyConfig; same value across rows of one LevelId; empty → not owned by any difficulty (excluded from difficulty-clear) |
| RouteMapAssetId | Optional filename without ext (e.g. `SubLevel_001`); first non-empty among LevelId rows; empty → no map intent / legacy Stage-row layout; when set, pairs with Prefab `LevelRouteMap_{LevelId}` |
| UnlockLevelId | Read **only StageNumber=1** (ignore other Stage rows). Empty → LevelId **default unlocked**; value found in `Level_SubLevelConfig` as `GameplayOptionId` → unlock this LevelId when that option is cleared (cross-Level OK); value not in SubLevel → **never unlockable** via formal entry |

**Table 0 — DifficultyConfig fields (rules; Demo Hub wiring deferred)**

| Field | Notes |
|-------|-------|
| DifficultyId | PK; samples `Diff_Normal` / `Diff_Hard` / `Diff_Hell` |
| DisplayName | UI column title |
| UnlockRequireDifficultyId | Empty = **initially unlocked**; existing DifficultyId → unlock after that difficulty is cleared; Id missing from table → **never unlockable** (load may Warning) |
| Description | Hover / help copy (replaces Demo hardcoded text when wired) |
| ClearReward | `ItemId;Count\|…` (via ItemCatalog); empty OK; granted **once** on first difficulty-clear |

**Difficulty unlock & clear (independent of LevelId gate / option UnlockNext)**

| Rule | Notes |
|------|-------|
| Difficulty cleared | **All** distinct `LevelId`s owned by that `DifficultyId` have reached level victory (empty `UnlockNextOptionIds` → VictorySettlement); a difficulty row with no owned LevelIds never clears via level victory |
| Unlock | Empty prerequisite → initial; prerequisite cleared → unlock; missing prerequisite Id → permanently locked |
| Clear reward | First transition uncleared→cleared grants `ClearReward`; empty = none; granted flag save schema **wiring deferred** |
| Demo | Table + rules landed; `DifficultySelectHost` / grant / save **wiring deferred** (still hardcodes three columns + Toast) |

**LevelId unlock gate (independent of option `UnlockNextOptionIds`)**

| Rule | Notes |
|------|-------|
| When | Before formal enter / UI-031 tab switch; derived from Stage1 `UnlockLevelId` + `LevelRouteProgress` cleared set |
| Formal entry | Normal difficulty / UI-031 tabs: `TryEnterLevel` **enforces**; default enter = last **unlocked** distinct LevelId; none unlocked → Toast |
| UI-031 tabs | Show **all** distinct LevelIds; locked / never → gray, click Toast「关卡未解锁」; unlocked tabs switch OK |
| Tools Level | `LevelSelectPanel` may **bypass** gate (GM force-enter any LevelId) |
| Shop | `maxUnlockedLevelNumber` (LevelId trailing digits) **unchanged this slice** |

**Table 2 — SubLevel fields**

| Field | Notes |
|-------|-------|
| GameplayOptionId | PK; **must not reuse across LevelIds** (pins live on that level's `LevelRouteMap_{LevelId}`) |
| GameplayType | → `GameplayState` |
| GameplayConfigId | Dig/Defend/PushMap/**SearchExtract** lookup; Shop/UM/AM **ignored** |
| IconAssetId / Title / Description | Route UI |
| Reward | `ItemId;Count\|…` via ItemCatalog; grant on clear |
| UnlockNextOptionIds | `OptId\|…` must be Stage+1; **empty → level victory on clear** |

**Route map Prefab (Approach C, UI-031):** path `Assets/Prefabs/Level/LevelRouteMap_{LevelId}.prefab`. Child name = `GameplayOptionId`; `RectTransform.anchoredPosition` = option center (map bottom-left `(0,0)`, Y up; units = UI px at display width 1450). Option IDs come **only** from Operation + SubLevel tables; Prefab does **not** invent IDs. Editor menus (Mode2 CSV): `Gravedigger2026/Level（关卡）/Ensure LevelRouteMap Prefabs (UI-031)（确保关卡路线地图预制体）` creates/paints Background and fills missing pins; `Gravedigger2026/Level（关卡）/Sync LevelRouteMap Pins (UI-031)（同步关卡路线地图钉点）` syncs missing pins from tables and warns on extras (**does not** overwrite authored positions). Pins are placeholder Transforms, not option nodes. Runtime Instantiates option nodes; map Prefab supplies bg + pin Transforms only. **Map mode: Icon only on scene** (~80×80); hover Tips = standalone Prefab `Assets/Prefabs/Level/OptionHoverTips.prefab` (nested under `LevelRouteSelectRoot/Box`; Ensure copies to `Resources/Prefabs/Level/`; no runtime `BuildHierarchy`) by GameplayType (Dig=`TipMessages`; Shop/AM/UM=`IconAssetId2`+Description; PushMap/SE/Defend=`IconAssetId2`+Reward icons). **On open / LevelId tab switch, `LevelRouteSelectView` scrolls `MapContent` Y to center the latest unlocked pin (Y only).** **On clear-return in map mode:** Snapshot one-shot `JustClearedOptionId` → View snaps to just-cleared pin, holds 0.5s, then ~0.5s smooth scrolls to current latest-unlocked frontier (Y only; Driver does not write Transforms). Missing pin → Warning, icon at `(0,0)`. Without that Prefab (or no `RouteMapAssetId`) → legacy Stage-row layout (full cards).

**Flow:** EnterLevel (formal: LevelId unlock gate first; Tools may bypass) → hydrate Cleared from `LevelRouteProgress` → derive Unlocked → RouteSelect (all LevelId tabs; locked gray+Toast; default last unlocked; Stage1 + unlocked next **except same-Stage siblings of Cleared**; map Prefab when present; map mode initial scroll Y → latest unlocked pin centered; clear-return → just-cleared hold then frontier) → pick → run module → fail aborts (**Cleared kept**); clear grants Reward, **persists Cleared**, unlocks next, **locks same-Stage siblings**; empty UnlockNext → VictorySettlement (Cleared retained on re-enter) else RouteSelect.

**Route progress persistence (`LevelRouteProgress`, Approach A / D-088)**

| Rule | Notes |
|------|-------|
| Persist | Cleared `GameplayOptionId` set only (flat JSON; OptionIds **must not** reuse across LevelIds) |
| Do not persist | In-progress option / in-option details; re-enter returns to RouteSelect |
| Unlock derive | `TryEnterLevel`: all Stage1 ∪ each cleared option's `UnlockNextOptionIds` (mounted on this LevelId) |
| Same-Stage sibling lock | Stage has any Cleared → other uncleared options on that Stage `UiState=Locked` and `TrySelect` rejects; do **not** remove from unlock-derived set |
| Write timing | On option clear (`TryAdvanceStage` / PushMap `CompleteLevelAfterBattleSettlement`) immediate `PlayerPrefs` |
| Level victory | Keep Cleared; re-enter shows Cleared; cannot re-pick cleared |
| Failure/abort | **Do not** wipe persisted Cleared |
| Key space | Per slot + `CampaignMode` ([SPEC_04 §6](SPEC_04_Technical.md)); delete slot clears both modes |

```
EnterLevel
  → Load stages + options
  → Hydrate Cleared → derive Unlocked
  → Show RouteSelect
  → Pick unlocked option → run
  → LevelFailure → abort (Cleared kept)
  → Clear → Grant → Persist Cleared → Unlock → empty? Victory : RouteSelect
```

---

## 3.10 挖坟（Dig）玩法

### 简体中文

**状态：已定义（生成 / 有效时长 / 玩家挖掘交互与奖励入账 / 障碍物几何 / 挖坟四项科技绑定能力 / 无胜负 / DigStageSummary；科技树框架见 §3.13，节点具体数值仍 TBD）**

当关卡当前阶段 `玩法类型 = Dig` 时，使用「挖坟配置表」中对应 `玩法配置ID` 的行。坟墓 `maxHP` 与掉落内容来自「坟墓品质定义表」（[SPEC_04 §9.3](SPEC_04_Technical.md)）。

**精魂结晶（SpiritCrystal / `QualityId=Q101`）**

| 规则 | 说明 |
|------|------|
| 身份 | 普通坟墓品质之一（**不是**独立实体类型）；Prefab `Grave_Q101`；须纳入 `DigPrefabCatalog` |
| 交互 | DigAction / 障碍圆 / DigHitShape / HP 色阶与普通坟一致 |
| Mode2 样例 | `MaxHP=38`；`DropMode=2`；`LootDrop=Spirit;6000;2\|Spirit;2000;4\|Spirit;1500;8\|Spirit;500;16`（加权抽恰好 1 段精魂；**无** BodyPart） |
| 生成 | Mode2 `Dig_01`/`Dig_02`/`Dig_03` 的 `GraveSpawnWeights` 含 `Q101;10`（可再调表） |

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

- 倒计时进行中，按「倒计时过程中生成坟墓速率」`SpawnRate`（编码 `N;M`）：每 **N** 秒尝试生成 **effectiveM** 座坟。
- **`effectiveM = max(0, tableM + DigProtagonistCapabilities.DigProcessSpawnCountBonus)`**（整数加法；**不**改 N；**不**影响开局 `InitialGraveCount`）。
- 每一座仍：表权重 + caps 加成 → 过滤 →（空有效列表则放弃）→ 加权抽品质 ID → 可放置区随机落点（同上重试规则）→ 按品质表初始化 HP。
- 与开局共用同一套有效权重算法；**每次抽取 / 每次过程生成 tick 读活 caps**（本阶段中装备升级立即影响后续刷坟数量与权重）。

**主角（Digger）表现**

| 规则 | 说明 |
|------|------|
| 地图实体 | Dig 阶段 **不** Instantiate 地图中心 Digger 模型（无整角 Visual、无待机/挖坟动画驱动） |
| HUD 头像 | Dig HUD **左上角** 固定 **60×60**（Canvas 参考分辨率单位）方框，展示主角头像；Demo 可用占位图/色块，正式头像资源后换 |
| 镜头迷雾滤镜 | 全局 **`CameraFogService`**（Meta 壳常驻）拥有独立 **`DigFogCanvas`**（`sortingOrder=10`）全屏 **`CameraFogOverlay`**：Sprite = `Assets/Art/Maps/Fogs/Fog_1.png`；`raycastTarget=false`；**高于**世界地图/Sprite，**低于** Meta 壳 `MetaCanvas`（`20`）与 **DigHudCanvas**（`30`）。**显隐（权威）：** **仅** Dig 会话进行中（含 DigStageSummary）与 PushMap **`Combat`** 显示；Shop / AutoManufacture / UpgradeManufacture / Defend / PushMap **Prepare**/`Ended` / 进档壳其它界面 **不**显示。Dig 或 PushMap Combat 中打开装备仓 / 魔法书槽 / 商店 overlay / Tools / 关卡选择 / GM 弹窗 / 科技树等 Meta 弹窗时 **主动 `SetActive(false)` 隐藏** `DigFogCanvas`，关闭后若仍处合法玩法窗口则恢复。呼吸动效：`DigCameraFogOverlayView` 中心 pivot 缩放 1.0↔1.05 各 5s；Dig 倒计时进行中与 PushMap Combat 可见时 `Play`，倒计时归零 / 阶段 End / 隐藏时 `Stop`。禁止把 `DigFogCanvas` 挂到 `transform.root` 以致阶段销毁后残留。`DigUiLayering` 进 Dig 仅调 Meta/HUD 排序。**GM** 折叠：`GmToggleButton` + `GmMenuPanel` |
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
| DigProcessSpawnCountBonus | 对过程生成 `SpawnRate` 的 **M** 加法（整数）；编码键 `DigProcessSpawnCountBonus`（见 [SPEC_04 §9.17](SPEC_04_Technical.md)）；**不**改 N；**不**影响开局坟数 |

**单次挖掘时长（挖坟单次速度）：**

`DigActionDuration = max(DigActionDurationFloor, BaseDigDuration − DigDurationReductionSum)`，其中 **`BaseDigDuration` / `DigActionDurationFloor` ← `CombatConstantConfig`**（样例 `0.8` / `0.1` 秒）。光标触发停留 **`DigTriggerDwellSeconds` ← 同表**（样例 `0.2s`）。

（与「有效挖坟时长」不同：后者是阶段倒计时总长；本项是单次 DigAction 动画/结算时长。）

**光标与挖掘触发**

| 规则 | 值 |
|------|-----|
| 光标形态 | 进入挖坟阶段后叠加「圆圈范围」指示；半径 = `DigCursorRadius`（圆，非方）。OS 指针仍为 UI-024 `PlayerPointer`，**不**隐藏 |
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

**Dig HUD 仓库统计（WarehouseHudStats）**

Dig HUD 左上 `Warehouse` **不再**展示具体道具名 / raw Id 文本链，改为三行图标+数量统计（近似「可造士兵种族/职业」的提示，非材料明细）：

| 行 | 内容 | 图标（`Assets/Art/UI/Icons/`） |
|----|------|-------------------------------|
| 1 | 精魂（`SpiritEssence`）+ **尸体残骸总数** | `Currency_Spirit` / `WreckWarehouse` |
| 2 | 四种族「主要手」数量（序：亡灵→兽人→精灵→人类） | `AllRacesIcon_1` / `OrcIcon_1` / `ElvesIcon_1` / `HumansIcon_1` |
| 3 | 四基础职业「主要手」数量（序：战士→射手→法师→刺客） | `WarriorIcon` / `ArcherIcon` / `MageIcon` / `AssassinIcon` |

| 规则 | 说明 |
|------|------|
| 布局 | 左上对齐；图标 **60×60**；数值字号 **24**（图标下方）；单元左右间距 **10**；行距 **20**；每行左对齐 |
| 未获得 | 数量 = 0（精魂同）→ **不显示** 该单元图标与数值 |
| 残骸总数 | 仓库中所有 `BodyPartConfig` 且 `IsPrimaryHand ≠ 1` 的堆叠数量之和（不含精魂、不含 Dig Material、不含主要手） |
| 主要手·种族 | `IsPrimaryHand == 1`，按 `RaceId` 归桶（展示仅 `Race_Undead` / `Race_Orc` / `Race_Elf` / `Race_Human`） |
| 主要手·职业 | `IsPrimaryHand == 1`，按 `BodyPartConfig.BaseClass` 归桶（`战士\|射手\|法师\|刺客`；与 `ClassConfig.BaseClass` 同枚举） |
| Hover Tips | 鼠标指向 Warehouse 区域旁显示 Tips；文案 ← `LocalizedDescriptionConfig` Key=`DigWarehouseHoverTips`（Demo 中文：`此处统计可大致制造的士兵种族与职业数量`） |
| 刷新 | `WarehouseChanged` → 重算统计并刷新 HUD |

**阶段结束与结算（无胜负）**

| 规则 | 说明 |
|------|------|
| 胜负 | Dig 阶段 **无胜 / 负**；**不**触发 `LevelFailure` |
| 唯一结束条件 | **有效挖坟时长**倒计时归零 |
| 归零瞬间 | 停止过程生成；**取消**所有进行中的 `DigAction`（**不**结算本次扣血）；不可再触发挖掘 |
| 阶段结算 | 弹出 **DigStageSummary**（UI-011）：仅展示 **本阶段已获得** 奖励的按类型汇总；**不额外发放**任何奖励（与关卡级 `VictorySettlement` 区分）。每条奖励为格子（方图标 + 右下数量 + 下方名称）；躯体名称=`DisplayName`（空则 `BodyPartId`），精魂/非躯体名称=`Id`；**每行最多 5**（`GridLayoutGroup` FixedColumnCount）。Demo 面板 `SummaryRoot` **1103×796**（`UI_Kuang_09`、纯白）、`Title` 纯黑、`Body` **920×620** 竖向 `ScrollRect`（约三行可视，超出可滑，`Content` 承载网格）；关闭为右上「X」（`ConfirmButton`） |
| 确认后 | 玩家点右上「X」关闭弹窗 → 进入 §3.9 下一阶段 /（若末阶段）`VictorySettlement` |

**Demo GM（Dig HUD）**

`GmMenuPanel`（`GmToggleButton` 折叠）：**两层两列**网格（列宽 172、列间距 8、行步进 48）。**上层** `GmLayerDigMagic`：Dig 通用 + Mode2 魔法书；**下层** `GmLayerEquip`：主角装备「获得」左列 / 「划入升级」右列配对。面板右上锚点，宽约 360。

| 区域 | 按钮 | 行为 |
|------|------|------|
| 上层 | 增加坟墓 | 点一次：按**当前有效权重**（表 `GraveSpawnWeights` + caps `GraveSpawnWeightBonus`）加权抽品质，落点/避障/32 次重试规则同开局与过程生成；循环尝试 **10** 次；空间不足或有效权重为空时该次放弃，实际生成可少于 10 |
| 上层 | 增加躯体材料 | 点一次：对当前已加载 `Manufacture_BodyPartConfig` **全部行**各 `Warehouse.AddItem(BodyPartId, 10)`（堆叠上限 10000 钳制；**不**走 LootDrop / AutoConvert） |
| 上层 | 少量增加躯体材料 | 点一次：对当前已加载 `Manufacture_BodyPartConfig` **全部行**各 `Warehouse.AddItem(BodyPartId, 2)`（堆叠上限 10000 钳制；**不**走 LootDrop / AutoConvert；与「增加躯体材料」同 API，仅数量不同） |
| 上层 | 装备公共经验+50 | 点一次：GM 注入 `EquipCommonExp += 50`（`DebugGrantCommonExp`）；日志打印公共池与仓状态 |
| 上层 | 装备魔法书（全表） | **仅 Mode2**：列出当前模式 `MagicBookConfig` **全表**（两列网格；文案=`DisplayName`，空则 `MagicBookId`）。点一次 `SpecialEquipSlotsService.TryEquip(magicBookId)`（装入第一个空槽；与 UI-019 / D-061 一致；唯一已装或 6 槽满 → 失败日志）。Mode1 **不**显示。槽排序见 UI-023 / D-068 |
| 下层左 | 获得铁铲 / 矿灯 / 炸药 / 引雷 / 探测器 / 人类信物 / 精灵信物 / 兽人信物 | 各点一次：`TryAcquire` 对应 EquipId；日志打印 Level / CurrentExp / 相关 caps。仓只读见 UI-022 / D-067；手验 D-059 / D-060 / D-077～D-080 |
| 下层右 | 划入对应升级 | 各点一次：`TrySpendCommonExp(equipId, 1)`（池不足或未拥有则日志失败）；与每级 `ExpToNextLevel=1` 对齐 |

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

**Spirit Crystal (`QualityId=Q101`)**

| Rule | Notes |
|------|-------|
| Identity | A normal grave quality (**not** a separate entity type); Prefab `Grave_Q101`; must be in `DigPrefabCatalog` |
| Interaction | Same DigAction / obstacle / DigHitShape / HP tint as normal graves |
| Mode2 sample | `MaxHP=38`; `DropMode=2`; `LootDrop=Spirit;6000;2\|Spirit;2000;4\|Spirit;1500;8\|Spirit;500;16` (weighted pick exactly one Spirit count; **no** BodyPart) |
| Spawn | Mode2 `Dig_01`/`Dig_02`/`Dig_03` `GraveSpawnWeights` include `Q101;10` (tunable) |

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

- While countdown runs, every N seconds attempt to spawn **effectiveM** graves per `SpawnRate` (`N;M`).
- **`effectiveM = max(0, tableM + DigProtagonistCapabilities.DigProcessSpawnCountBonus)`** (integer add; **does not** change N; **does not** affect initial `InitialGraveCount`).
- Each grave: table weights + caps bonus → filter → (abandon if effective list empty) → weighted quality pick → random placeable position (same retry rule) → HP from quality table.
- Same weight field and zero-weight drop rule as initial spawn; **each pick / each process-spawn tick reads live caps**.

**Digger presentation**

| Rule | Notes |
|------|-------|
| Map entity | Dig stage does **not** Instantiate a map-center Digger model (no whole-character Visual, no idle/dig anim drive) |
| HUD portrait | Dig HUD **top-left** fixed **60×60** (canvas reference-resolution units) frame showing protagonist portrait; Demo may use placeholder tint/sprite; swap formal art later |
| Camera fog filter | Global **`CameraFogService`** (Meta-resident) owns **`DigFogCanvas`** (`sortingOrder=10`) full-screen **`CameraFogOverlay`** (Sprite = `Assets/Art/Maps/Fogs/Fog_1.png`; `raycastTarget=false`); **above** world map/sprites, **below** Meta shell `MetaCanvas` (`20`) and **DigHudCanvas** (`30`). **Visibility (authority):** show **only** during Dig session (incl. DigStageSummary) and PushMap **`Combat`**; **never** for Shop / AutoManufacture / UpgradeManufacture / Defend / PushMap **Prepare**/`Ended` / other shell UIs. While Dig or PushMap Combat is eligible, opening Equipment / MagicBook / Shop overlay / Tools / LevelSelect / GM / TechTree Meta overlays **actively hides** `DigFogCanvas` (`SetActive(false)`); restore when overlays close if still eligible. Pulse: `DigCameraFogOverlayView` 1.0↔1.05 / 5s; `Play` while Dig countdown or PushMap Combat fog is shown, `Stop` on Dig time-up / stage End / hide. Do **not** parent `DigFogCanvas` to `transform.root` (leaks after Dig destroy). `DigUiLayering` only adjusts Meta/HUD order on Dig enter. **GM** foldout: `GmToggleButton` + `GmMenuPanel` |
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
| DigProcessSpawnCountBonus | Additive to process-spawn `SpawnRate` **M** (integer); attr key `DigProcessSpawnCountBonus` (see [SPEC_04 §9.17](SPEC_04_Technical.md)); **does not** change N; **does not** affect initial grave count |

**Dig action duration (dig speed):**

`DigActionDuration = max(DigActionDurationFloor, BaseDigDuration − DigDurationReductionSum)` where **`BaseDigDuration` / `DigActionDurationFloor` ← `CombatConstantConfig`** (sample `0.8` / `0.1` s). Cursor dwell **`DigTriggerDwellSeconds` ← same table** (sample `0.2s`).

(Distinct from Effective Dig duration: that is the stage countdown length; this is single DigAction anim/resolve duration.)

**Cursor & dig trigger**

| Rule | Value |
|------|-------|
| Cursor | On Dig stage enter, a **circle range** overlay appears; radius = `DigCursorRadius` (circle, not square). OS pointer stays UI-024 `PlayerPointer` — do **not** hide it |
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

**Dig HUD warehouse stats (WarehouseHudStats)**

Dig HUD top-left `Warehouse` **no longer** shows item names / raw Id text chains. It shows three icon+count rows (approximate “craftable race/class” hint, not material detail):

| Row | Content | Icons (`Assets/Art/UI/Icons/`) |
|-----|---------|--------------------------------|
| 1 | SpiritEssence + **wreck total** | `Currency_Spirit` / `WreckWarehouse` |
| 2 | Primary-hand counts by race (Undead→Orc→Elf→Human) | `AllRacesIcon_1` / `OrcIcon_1` / `ElvesIcon_1` / `HumansIcon_1` |
| 3 | Primary-hand counts by base class (Warrior→Archer→Mage→Thief) | `WarriorIcon` / `ArcherIcon` / `MageIcon` / `AssassinIcon` |

| Rule | Notes |
|------|-------|
| Layout | Top-left align; icon **60×60**; value font size **24** (below icon); cell gap **10**; row gap **20**; each row left-aligned |
| Not owned | Count = 0 (incl. Spirit) → **hide** that cell icon and value |
| Wreck total | Sum of warehouse stacks for all `BodyPartConfig` with `IsPrimaryHand ≠ 1` (excludes Spirit, Dig Material, primary hands) |
| Primary hand · race | `IsPrimaryHand == 1`, bucket by `RaceId` (display only `Race_Undead` / `Race_Orc` / `Race_Elf` / `Race_Human`) |
| Primary hand · class | `IsPrimaryHand == 1`, bucket by `BodyPartConfig.BaseClass` (`战士\|射手\|法师\|刺客`; same enum as `ClassConfig.BaseClass`) |
| Hover tips | Hover Warehouse area → side tips; copy ← `LocalizedDescriptionConfig` Key=`DigWarehouseHoverTips` (Demo ZH: `此处统计可大致制造的士兵种族与职业数量`) |
| Refresh | `WarehouseChanged` → rebuild stats and refresh HUD |

**Stage end & settlement (no win/lose)**

| Rule | Notes |
|------|-------|
| Win/lose | Dig stage has **no win / lose**; does **not** trigger `LevelFailure` |
| Sole end condition | **Effective Dig duration** countdown hits 0 |
| On zero | Stop ongoing spawn; **cancel** all in-progress `DigAction`s (**no** damage resolve); no further dig triggers |
| Stage settlement | Show **DigStageSummary** (UI-011): aggregate **rewards already earned this stage** by type; **no extra grants** (distinct from level `VictorySettlement`). Each reward is a cell (square icon + bottom-right qty + name below); body name=`DisplayName` (empty → `BodyPartId`), Spirit/non-body name=`Id`; **max 5 per row** (`GridLayoutGroup` FixedColumnCount). Demo panel `SummaryRoot` **1103×796** (`UI_Kuang_09`, white), `Title` black, `Body` **920×620** vertical `ScrollRect` (~3 rows visible; overflow scrolls; grid on `Content`); dismiss via top-right "X" (`ConfirmButton`) |
| After confirm | Player taps top-right "X" → §3.9 next stage / (if last) `VictorySettlement` |

**Demo GM (Dig HUD)**

`GmMenuPanel` (foldout via `GmToggleButton`): **two layers × two columns** (col width 172, gap 8, row step 48). **Top** `GmLayerDigMagic`: Dig commons + Mode2 MagicBooks; **bottom** `GmLayerEquip`: protagonist gear Grant (left) / Spend-into-upgrade (right) pairs. Top-right anchor; panel width ≈360.

| Zone | Button | Behavior |
|------|--------|----------|
| Top | Add Graves | One click: weighted pick via **current effective weights** (table `GraveSpawnWeights` + caps `GraveSpawnWeightBonus`); placement / obstacle / 32-retry same as initial & process spawn; attempt **10** times; fewer than 10 if no space or empty effective weights |
| Top | Add Body Parts | One click: for **every** loaded `Manufacture_BodyPartConfig` row, `Warehouse.AddItem(BodyPartId, 10)` (stack cap 10000; **no** LootDrop / AutoConvert) |
| Top | Add Few Body Parts | One click: for **every** loaded `Manufacture_BodyPartConfig` row, `Warehouse.AddItem(BodyPartId, 2)` (stack cap 10000; **no** LootDrop / AutoConvert; same API as Add Body Parts, count only) |
| Top | Equip Common Exp +50 | One click: GM inject `EquipCommonExp += 50` (`DebugGrantCommonExp`); log pool + warehouse |
| Top | Equip MagicBook (full table) | **Mode2 only**: list all current-mode `MagicBookConfig` rows (two-col grid; label=`DisplayName`, else `MagicBookId`). One click `SpecialEquipSlotsService.TryEquip(magicBookId)` (first empty slot; same as UI-019 / D-061; unique already equipped or 6 slots full → fail log). **Hidden** in Mode1. Slot reorder = UI-023 / D-068 |
| Bottom left | Grant Iron Shovel / Miner Lamp / Explosives / Lightning / Detector / Human / Elf / Orc Token | One click each: `TryAcquire` matching EquipId; log Level / CurrentExp / related caps. Warehouse read-only = UI-022 / D-067; hand-check D-059 / D-060 / D-077–D-080 |
| Bottom right | Spend Into matching upgrade | One click each: `TrySpendCommonExp(equipId, 1)` (fail log if pool short / not owned); matches per-level `ExpToNextLevel=1` |

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
| 一键上阵 / 一键下阵 | Mode2：`CompleteButton` **左侧**「一键上阵」（D-074）；其**正上方**「一键下阵」（D-097）— 将全部已上阵士兵退回 SoldierBar 并裁剪战术组；UM / Prepare 均显示 |
| 布阵内开战位 | Mode2：`StartBattleButton`（开战）在同一右下角叠放于 `CompleteButton` **正上方**（Defend / PushMap Prepare 显示；UM 宿主可隐藏） |
| 布阵路径预览控件 | Mode2 **仅 PushMap Prepare**：`StartBattleButton` 正上方「快速预览」；再上方 `CameraPathSlider`（左 WP_Start / 右 WP_End）；Defend / UM **不**显示；缺 `CameraFollowPath` 则隐藏或禁用 |
| 士兵栏悬浮框 | Mode2 布阵编辑器内：指针停有兵格 → UI-021 悬浮框（见上「战斗布阵」）；Prepare 同样可用 |
| 士兵栏方格尺寸 | Mode2 `SoldierSlotTemplate` 的 RectTransform 宽高为权威（Demo **125×180**，可改预制体）；`FormationSoldierBarView` 运行时读取模板尺寸并应用到克隆槽与 Content；**不得**覆盖为硬编码 80×80 |
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
| 公式 | 对属性项 `S`：`Base(S) = Σ` 已放入躯体部位的 `StatBonus(S)`（缺省维 **0**）；**例外**：`S=MoveSpeed` 时 `Base(MoveSpeed) = ClassConfig.BaseMoveSpeed`（经实例 `ClassId` 查表；缺/≤0 → **3.5**），**不**累加躯体 `StatBonus(MoveSpeed)` |
| 参与部位 | 已放入的全部躯体槽（含可选头部）；空槽不计（**MoveSpeed 维除外**，见上） |
| 字段来源 | `BodyPartConfig.StatBonus`（编码见 [SPEC_04 §9.12](SPEC_04_Technical.md)）；MoveSpeed 另见 [§9.9b `ClassConfig.BaseMoveSpeed`](SPEC_04_Technical.md) |
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
| 基础属性（BaseStats） | 由制造所用 **躯体部位** `StatBonus` 按维求和：`Base(S)=Σ StatBonus(S)`（见上；**MoveSpeed 维例外**：`Base(MoveSpeed)` 取 `ClassConfig.BaseMoveSpeed`）。固定五项：**生命值、移动速度、力量、敏捷、智力**。选敌/攻击距离/命中/死亡见 §3.12；普攻/攻速/技能CD/最终血量派生见下与 §3.12 |
| 种族（Race） | 由躯体部位加权随机定稿（见上）；数据来自 **`RaceConfig`**（[SPEC_04 §9.11](SPEC_04_Technical.md)）。提供 **五维** `RaceAdjustCoeff`（缺省维 **0**；可正可负）。**不**单独计入 `ControlPowerCost` |
| 灵魂（Soul） | 槽位 **可选**；数据来自 **`SoulConfig`**（[SPEC_04 §9.9](SPEC_04_Technical.md)）。有灵魂：消耗该行，写入其 `SoulId`/`ClassId`/`AttackMode`/技能/优先级/`MoveStyle`/SpiritCost/ControlPowerCost。无灵魂：不扣仓库；`SoulId=Soul_00`；其余灵魂侧字段读 `Soul_00`；**强制** `ClassId=Class_Servants`。`AttackMode ∈ { Melee, Ranged }`。**不**改写三维属性本身；并行 `Skills` 本 Demo **不施放**（实例 `SoldierSkills` 施放见 §3.12 SkillCast） |
| 职业（Class） | 由实例 `ClassId` 解析 **`ClassConfig`**（[SPEC_04 §9.9b](SPEC_04_Technical.md)）。提供：`ClassName`（命名与外观 `ClassAffinity`）、`BaseClass`（基础职业：`战士`/`射手`/`法师`/`刺客`；加载器仍接受旧值 `盗贼`；**预留**后续魔法书等条件，**不**参与命名/外观/`PrimaryStat`/战斗派生）、`PromoteClass`（转职职业：可选文字；空=无；本轮仅填表/加载，应用点 **TBD**）、`PrimaryStat ∈ { Strength, Agility, Intelligence }`、`CombatConvertCoeffs`（`键_数值|…`；缺键/空串回退 **`CombatConstantConfig`**）、`BaseMoveSpeed`（士兵基础移速；§3.11 MoveSpeed 维 `Base`）、以及 `AttackRange` / `MeleeWindupSeconds` / `RangedProjectileSpeed` / `RangedTimeoutSeconds`、`DefaultSkillIds`（制造默认士兵技能）。示例语义：战士→Strength、射手→Agility、法师→Intelligence、仆从（`Class_Servants`）→与战士同主属性样例（以 `PrimaryStat` 为准，非 ClassName 硬编码） |
| 士兵技能（SoldierSkills） | 实例绑定列表 `{ SkillId, SkillLevel }[]`；权威表 **`SkillConfig`**（[SPEC_04 §9.21](SPEC_04_Technical.md)）。制造时由最终 `ClassId` 的 `DefaultSkillIds` 授予（见下）；**无**消耗经验升级。灵魂/宝石/外置 `Skills` **并行**（同 Id 合并 **TBD**）。**Demo 战斗施放**见 §3.12 SkillCast / D-069（PushMap `Skill_03` 连发 + `Skill_01` 格挡 + `Skill_02` 舒适）+ D-073（`Skill_04`～`Skill_12` EffectKind 管线） |
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
| 技能二次判定 | 仅当该士兵 `ΣSkillBonus ≠ 0` 时：每次 **释放技能** 后再用 **同一完整最终失控率** 独立 roll 一次；已叛变则跳过；Demo 见 §3.12 SkillCast / D-069 |
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
| 士兵栏 | 画面底部 UI：池内士兵以方格左对齐向右排列（**已上阵也保留在栏内**）；Mode1 **80×80**；Mode2 以 `FormationEditorRoot_Mode2` 的 `SoldierSlotTemplate` 宽高为准（Demo **125×180**）；栏内按住左右拖 = 横滑；方格文案上行为 `ClassName`（经 `ClassId` → `Manufacture_ClassConfig`；缺行回退 `ClassId`）、下行 `Lv.{ClassLevel}`（缺行按 0） |
| Mode2 士兵栏悬浮框 | 仅 `FormationEditorRoot_Mode2`（UM / Defend·PushMap Prepare）：指针停在**有兵** `SoldierSlot` 上展示悬浮框（UI-021）；标题=`ClassName`（缺行回退 `ClassId`）；标牌=`{ClassLevel}级`、种族 `RaceConfig.DisplayNameKey`（空则 `RaceId`）、`BaseClass` 中文（`Unspecified` 隐藏）、`PromoteClass`（空隐藏）；属性=静态 `MaxHP=ceil(BodyLife+Str×MaxHpStrengthMult)` + StaticStat 力量/敏捷/智力，`PrimaryStat` 对应行标「(主属性)」；技能=实例 `SoldierSkills` 顺序，名=`SkillConfig.DisplayName`（缺行回退 `SkillId`），图标文件名=`SkillId`（`Resources/UI/Skills/{SkillId}`；缺图仍显示名；**不**用 `IconAssetId` 作路径）；每个技能 `Icon` 右上角常驻 **5×5** 方块（`Icon/EffectStatus` 最后子节点），绿 = `SkillConfig.EffectImplemented=1`，红 = `0`（缺行当 0；D-070）。指针离开槽位、栏内横滑、向上拖起上阵、编辑器关闭 → 隐藏。空槽 / Mode1 Prefab **不显示**。悬浮框不拦截射线（不挡拖拽） |
| 上阵操作 | 左键按住方格 **向上拖** → 该格 **变亮**；拖出士兵栏后光标处出现 **Idle 待机模型** 跟手；在地图内松手 → `TryDeployAt` 写坐标（放下即存）；上阵后栏内该格 **保持变亮且不隐藏** |
| 改位 / 下阵 | 已上阵可在战场再拖改位（`TrySetPosition`）；拖回士兵栏或松手在 **地图外**（`DigMapBounds` 外）→ `TryUndeploy` / 取消上阵并回栏，同时 **关闭** 该格变亮 |
| 控制力 HUD | 画面左上角显示 `ΣControlPowerCost / ControlPowerCap` |
| 离开 | UM：「返回」关编辑器回主屏；Defend：「开战」（UI-009，≥1）关编辑器进 Combat |
| Mode2 完成钮 | 仅 `FormationEditorRoot_Mode2`：`SoldierBar` 上方右侧 `CompleteButton`（UM/Prepare **均显示**）；其**正上方**叠放 `StartBattleButton`（Prepare 开战）；UM 宿主点击 Complete = 关编辑器并触发与主屏相同的阶段结束；Mode1 Prefab **无** Complete 钮 |
| Mode2 一键上阵 / 下阵 | 仅 Mode2：`CompleteButton` 左侧「一键上阵」（D-074）；其**正上方**「一键下阵」（D-097）将全部已上阵士兵 `TryUndeploy` 回栏并裁剪战术组 |
| 战术阵型目录条 | **UI-030：** 左缘竖排阵型目录按钮；点击尝试新建一组（见 §3.18 / D-093） |
| 战术阵型组卡牌 | **UI-035：** 顶中已建组卡牌；点选高亮士兵栏并在预览头顶显示阵型图标；选中卡下解散（见 §3.18 / D-093） |
| 阵型旋转 / 镜头 | 已激活整阵左键按住/拖动时 `Q`/`E` ±15°（§3.18 / D-092）；编辑器内滚轮缩放同 Combat 相机常量 |
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
| One-click deploy / undeploy | Mode2: “One-click deploy” left of `CompleteButton` (D-074); “One-click undeploy” stacked **directly above** it (D-097) — undeploys all deployed soldiers to the SoldierBar and prunes tactical groups; visible in UM and Prepare |
| StartBattle placement | Mode2: `StartBattleButton` stacked **directly above** `CompleteButton` on the same bottom-right edge (shown in Defend/PushMap Prepare; UM host may hide) |
| Path preview controls | Mode2 **PushMap Prepare only**: Quick Preview above `StartBattleButton`; `CameraPathSlider` above that (left WP_Start / right WP_End); hidden in Defend / UM; hide or disable if `CameraFollowPath` missing |
| Soldier-bar tooltip | Inside Mode2 Formation editor: pointer over occupied cell → UI-021 tooltip (see BattleFormation above); also in Prepare |
| Soldier-bar cell size | Mode2 `SoldierSlotTemplate` RectTransform width/height is authoritative (Demo **125×180**, prefab-authored); `FormationSoldierBarView` reads the template at runtime and applies it to clones and Content; must **not** overwrite with hardcoded 80×80 |
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
| Formula | Per attribute `S`: `Base(S) = Σ` filled BodyParts' `StatBonus(S)` (missing dim **0**); **exception**: when `S=MoveSpeed`, `Base(MoveSpeed) = ClassConfig.BaseMoveSpeed` (via instance `ClassId`; missing/≤0 → **3.5**), **do not** sum BodyPart `StatBonus(MoveSpeed)` |
| Participants | All filled body slots (incl. optional Head); empty slots excluded (**MoveSpeed dim excepted**, see above) |
| Field source | `BodyPartConfig.StatBonus` (encoding: [SPEC_04 §9.12](SPEC_04_Technical.md)); MoveSpeed also [§9.9b `ClassConfig.BaseMoveSpeed`](SPEC_04_Technical.md) |
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
| BaseStats | Sum of filled BodyPart `StatBonus` per dim: `Base(S)=Σ StatBonus(S)` (above; **MoveSpeed excepted**: `Base(MoveSpeed)` from `ClassConfig.BaseMoveSpeed`). Fixed five: **HP, MoveSpeed, Strength, Agility, Intelligence**. Targeting / AttackRange / hit / death in §3.12; NormalAttack / ASPD / SkillCD / final MaxHP derives below and in §3.12 |
| Race | Weighted pick from BodyParts (above); data from **`RaceConfig`** ([SPEC_04 §9.11](SPEC_04_Technical.md)). Five-dim `RaceAdjustCoeff` (missing dim = **0**; may be +/-). No separate ControlPowerCost term |
| Soul | Slot **optional**; **`SoulConfig`** ([SPEC_04 §9.9](SPEC_04_Technical.md)). If filled: consume that row; write its SoulId/ClassId/AttackMode/skills/priority/MoveStyle/SpiritCost/ControlPowerCost. If empty: no warehouse consume; `SoulId=Soul_00`; other soul-side fields from `Soul_00`; **force** `ClassId=Class_Servants`. `AttackMode ∈ { Melee, Ranged }`. Does **not** rewrite the three dims; parallel `Skills` **not cast** this Demo (instance `SoldierSkills` casts = §3.12 SkillCast) |
| Class | Resolved from instance `ClassId` via **`ClassConfig`** ([SPEC_04 §9.9b](SPEC_04_Technical.md)): `ClassName` (naming + appearance `ClassAffinity`), `BaseClass` (base class CSV: `战士`/`射手`/`法师`/`刺客`; runtime enum Warrior/Archer/Mage/Thief; loader still accepts legacy `盗贼`; **reserved** for future MagicBook conditions; **not** used in naming / appearance / `PrimaryStat` / combat derives), `PromoteClass` (optional promote-class text; empty = none; fill/load this slice; application **TBD**), `PrimaryStat ∈ { Strength, Agility, Intelligence }`, `CombatConvertCoeffs` (`Key_Value|…`; missing key / empty → **`CombatConstantConfig`**), `BaseMoveSpeed` (soldier base move speed; MoveSpeed-dim `Base` in §3.11), plus `AttackRange` / `MeleeWindupSeconds` / `RangedProjectileSpeed` / `RangedTimeoutSeconds`, `DefaultSkillIds` (default soldier skills at manufacture). Example semantics: Warrior→Strength, Archer→Agility, Mage→Intelligence, Servants (`Class_Servants`)→same PrimaryStat sample as Warrior (`PrimaryStat` wins; not ClassName hardcoding) |
| SoldierSkills | Instance list `{ SkillId, SkillLevel }[]`; catalog **`SkillConfig`** ([SPEC_04 §9.21](SPEC_04_Technical.md)). Granted at manufacture from final `ClassId` `DefaultSkillIds` (below); **no** exp-spend upgrade. Soul/Gem/ExtraEquipment `Skills` remain **parallel** (same-Id merge **TBD**). **Demo combat casts** = §3.12 SkillCast / D-069 (PushMap `Skill_03` burst + `Skill_01` block + `Skill_02` Comfort) + D-073 (`Skill_04`–`Skill_12` EffectKind pipeline) |
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
| Extra skill rolls | Only if this soldier's `ΣSkillBonus ≠ 0`: on **each skill cast**, roll again with the **same full FinalLossChance**; skip if already Rebel; Demo = §3.12 SkillCast / D-069 |
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
| Soldier bar | Bottom UI: pool soldiers as left-aligned cells (**deployed cells remain in bar**); Mode1 **80×80**; Mode2 uses `FormationEditorRoot_Mode2` `SoldierSlotTemplate` width/height (Demo **125×180**); horizontal drag inside bar = scroll; cell text: upper line `ClassName` (via `ClassId` → `Manufacture_ClassConfig`; missing row → fallback `ClassId`), lower line `Lv.{ClassLevel}` (missing row → 0) |
| Mode2 soldier-bar tooltip | `FormationEditorRoot_Mode2` only (UM / Defend·PushMap Prepare): pointer over an **occupied** `SoldierSlot` shows hover tooltip (UI-021); title=`ClassName` (missing row → `ClassId`); badges=`{ClassLevel}级`, race `RaceConfig.DisplayNameKey` (else `RaceId`), `BaseClass` Chinese (`Unspecified` hidden), `PromoteClass` (empty hidden); stats=static `MaxHP=ceil(BodyLife+Str×MaxHpStrengthMult)` + StaticStat Str/Agi/Int with「(主属性)」on the `PrimaryStat` row; skills=instance `SoldierSkills` order, name=`SkillConfig.DisplayName` (missing row → `SkillId`), icon filename=`SkillId` (`Resources/UI/Skills/{SkillId}`; missing sprite still shows name; do **not** use `IconAssetId` as path); each skill `Icon` keeps a **5×5** square at top-right (`Icon/EffectStatus` last child): green = `SkillConfig.EffectImplemented=1`, red = `0` (missing row → 0; D-070). Hide when pointer leaves the slot, bar scrolls horizontally, lift-to-deploy, or editor closes. Empty slot / Mode1 Prefab **no tooltip**. Tooltip does not block raycasts (must not eat drag) |
| Deploy | LMB hold cell, drag **up** → cell **highlights**; after leaving bar, Idle model follows cursor; release on map → `TryDeployAt` (persist immediately); deployed cell **stays highlighted and visible** |
| Reposition / undeploy | Drag deployed units to move (`TrySetPosition`); drag back to bar or release **outside map** (`DigMapBounds`) → `TryUndeploy` / cancel and **clear** cell highlight |
| ControlPower HUD | Top-left: `ΣControlPowerCost / ControlPowerCap` |
| Leave | UM: Return closes editor; Defend: StartBattle (UI-009, ≥1) closes editor → Combat |
| Mode2 Complete | `FormationEditorRoot_Mode2` only: `CompleteButton` above `SoldierBar` (right); `StartBattleButton` stacked **directly above** it (Prepare StartBattle); **Complete visible in UM and Prepare**; UM host click Complete = close editor + same stage end as main Complete; Mode1 Prefab has **no** Complete button |
| Mode2 one-click deploy / undeploy | Mode2 only: “One-click deploy” left of `CompleteButton` (D-074); “One-click undeploy” stacked **directly above** it (D-097) `TryUndeploy`s all deployed soldiers back to the bar and prunes tactical groups |
| Tactical catalog strip | **UI-030:** left-edge catalog buttons; click tries to create one group (§3.18 / D-093) |
| Tactical group cards | **UI-035:** top-center cards for existing groups; select highlights the soldier bar and shows the formation icon overhead on the preview; Disband under the selected card (§3.18 / D-093) |
| Squad rotate / camera | While LMB holding/dragging an active squad: `Q`/`E` ±15° (§3.18 / D-092); editor scroll zoom uses Combat camera constants |
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

**状态：框架已定义（ModeSelect 选模式/关卡 / 准备可改布阵/开战/部署/护盾/倒计时刷怪/寻路/胜负/失控叛变/士兵战斗选敌·AttackMode·攻击距离·命中方案D·死亡分层·普攻攻击值·攻速；Primary 取自 ClassConfig；CombatConvertCoeffs 与 AttackRange 等命中列见 ClassConfig / MonsterConfig；**Demo D-069：PushMap 忠诚兵施放 Skill_03 连发 + Skill_01 格挡 + Skill_02 舒适**；**D-073：Skill_04～12 EffectKind 登记制（SE-00～09 完成）**；怪物技能仍不施放）；禁止解析 Description 自然语言作效果器；怪物→士兵伤害细节仍 TBD；**出生点 / NavMesh：Demo 最小约定已关闭（见下），精确 OutsideMap 几何后置****

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
| 技能二次 roll | 该士兵 `ΣSkillBonus ≠ 0` 时，每次释放技能再用完整 `FinalLossChance` roll；已叛变跳过；Demo 见 SkillCast / D-069 |
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
| 选目标 | 按该怪 `TargetSelect`：`Nearest`（就近带，见下）/ `PreferWarrior`（优先士兵，士兵支路同最近带）/ `PreferProtagonist`（优先主角） |
| 攻击模式 | `AttackMode`：`Melee` / `Ranged`；怪物侧 `AttackRange` 与命中参数取自 `MonsterConfig`（可复用士兵命中方案 D 语义） |
| 对士兵 | 使用 `AttackPower` **直接扣士兵当前 HP**（本批无护甲/减伤） |
| 对主角 | 普通攻击只扣护盾 1 点（见上）；不用 `AttackPower` |
| 技能 | `Skills` 字段引用技能 ID+CD；技能效果表 **TBD**；**第一版 Demo 不生效**（只打普通攻击；实现时可忽略或配空） |
| 掉落 | 击杀时按 `LootDrop`（编码 `Id;Count|Id;Count|…`；**不是**坟墓品质表的 `DropMode` / `Id;Weight;Count`） |

**目标选择与寻路（怪物）**

| 规则 | 说明 |
|------|------|
| 选目标 | 按 `MonsterConfig.TargetSelect`（见上） |
| 士兵最近带 | `Nearest` 与 `PreferWarrior` 的士兵支路：合法士兵（Defend=可战；PushMap/SE=忠诚可战；含 Aggro 发现门闩）先求 `dMin`；**最近带** `bandMax = dMin×(1+NearestTargetBandRelative)+NearestTargetBandSlack`（← `CombatConstantConfig`，样例 `0.25` / `0.5`）。带内优先 **无 TargetFocus** 的空闲兵，再取带内最近；若当前聚焦仍在带内且与新候选同档（同空闲/同交战）则粘滞不换。主角比较：用带内最佳士兵与主角比绝对距离（主角不进带）。`PreferProtagonist` 主角优先不变 |
| TargetFocus | 每怪至多一条当前攻击锁定（`TargetFocusRegistry`）；选中士兵时 `SetFocus`，无目标/死亡/被动未激怒时 `ClearFocus`。进距释放 AttackSlot **不**清聚焦 |
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
| FormationSlot | **§3.18 战术阵型（D-084 / D-093 / D-094）：** 已入组的成员：目的地 = **该组**虚拟中心 + 朝向旋转后的 Pattern 槽位本地偏移，接敌**不**改 AttackSlot。已到槽且敌人在组朝向前方扇形内才停步攻击。守槽时软碰撞 incoming 为 0。每组中心独立（PushMap 跟 FlowField / Defend 守该组组阵点） |
| 与遇敌暂停关系 | PushMap：**MP-05 已接线** — 忠诚兵进入遇敌检测（中心距存活怪 ≤ **`max(武器触及, 该怪 AlertRadius)`**；武器触及 = `max(怪 AttackRange, 士兵 AttackRange) + 怪BodyRadius + 士兵BodyRadius + ArriveEpsilon`；`AlertRadius` 缺省=该怪 `AttackRange`）时改 `GoalKind=AttackSlot`（认领槽 + LocalDetour），**停跟** Objective FlowField；离开后释放槽并恢复 `GoalKind=Objective`。无空闲槽时**不**硬暂停：保持 `GoalKind=Objective` 继续跟场/绕行。**不是**全图 EngageZone 选敌（以免放弃占领去追远处怪）。命中仍须进入 `AttackRange`（方案 D）；见 §3.14 |
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
| **明确不做（跟随）** | **不**引入「跟随主角 / 军队半径聚团」作为**默认**移动态（对标 BMH `EMoveType::NORMAL` + `ArmyRadius` + `MinionFollowSpeed`）。未激活 / 已解散战术阵型的士兵无目标时仍走既有 `FormationHome` / PushMap `Objective` FlowField，**不是**粘随主角。**例外（§3.18 / D-094）：** 已激活战术阵型成员走 `GoalKind=FormationSlot`（虚拟中心 + 槽位，接敌不离槽），**不是** Follow 主角，也**不是** BMH ArmyRadius 全军粘团 |
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
| Demo 边界 | **Demo（D-069 + D-073）**：PushMap 忠诚兵可读/施放实例 `SoldierSkills` 中的 **`Skill_03`（连发）**；持有 **`Skill_01`** 时怪物普攻命中可格挡；持有 **`Skill_02`** 且满血时 Outgoing 伤害 +5%～+25%；持有并已 `EffectImplemented=1` 的 **`Skill_04`～`Skill_12`** 经 EffectKind 管线生效。不读灵魂、宝石、额外装备的并行 `Skills` 列表。怪物技能、Defend 接线 **后置**。`SkillCooldown` 公式驱动 `BaseCooldownSeconds>0` 的 Mode2 技能 CD |
| 适用范围 | 非叛变士兵在 `Combat` 中的普攻 / 攻速 / **主动技能**流程；`Skill_01` 格挡为受击被动钩子（不占用该流程）；`Skill_02` 舒适为 Outgoing 倍率钩子（不占用该流程）；复活等效果仍后置 |
| EngageZone | 候选敌人 = 存活且 **位置在 EngageZone 内** 的怪物；区外（含仍在 `OutsideMap` 外围、尚未进入选敌区的怪）**不可选** |
| 选目标 | **默认**：EngageZone 内 **距离最近** 的存活敌人 |
| FormationHome | 开战部署时锁定的布阵世界坐标（该士兵 `BattleFormation` 上阵位）；战斗中不随 Prepare 再编辑变化 |
| 无目标 → 自动返回 | **非叛变**士兵：当前目标死亡（或其它原因）后若 EngageZone **无**下一可选目标 → **自动返回** `FormationHome`（`GoalKind=FormationHome`；大规模栈下直趋或轻量路径，见上 MassCombatPathing）；抵达后无目标则在该点待机；**不**追区外目标 |
| 返回途中选敌 | 自动返回过程中仍按 `TargetRetargetInterval` **继续搜索** EngageZone；一旦出现可选目标 → **立即中断返回**，改为追击 / 进入攻击流程（改 `AttackSlot`） |
| 叛变与返回 | **Rebel 不**自动返回布阵点（仍就近打主角/兵/怪） |
| AttackPriority | `SoulConfig.AttackPriority` **本批不参与**选目标；枚举与 `TargetSelect` 对齐，字段保留 |
| AttackMode | 取自实例 `AttackMode`（有灵魂←`SoulConfig`；Mode2 无灵魂←`ClassConfig`）：`Melee` / `Ranged` / `Parabola`。怪物仍仅 `Melee` / `Ranged`。配置示例（非 ClassName 硬编码）：战士类→`Melee`+`Strength`；射手类→`Parabola`+`Agility`；法师类→`Ranged`+`Intelligence`（主属性维取自 `ClassConfig.PrimaryStat`） |
| 法师与射手 | **法师**仍走 `Ranged`（进距 → `AttackWindup` → 直线追瞄弹 → 碰撞命中/超时未命中）。**射手**走 `Parabola`（见下命中方案 D）。规则层伤害维差异仍是 `PrimaryStat`（法师智力 / 射手数敏捷）。远程怪不使用 `Parabola` |
| AttackRange | 近战与远程均有攻击距离（士兵取 `ClassConfig.AttackRange`；怪物取 `MonsterConfig.AttackRange`）；判定用 **XZ** 中心距（忽略 Y）。须先移动至认领 **AttackSlot**（或已进距则停步）后，再进入攻击态。**v0.82.57：** 已在 `AttackRange` 内 → 停步挥刀（对齐怪物，禁止为贴环而向外走）；未进距 → 目的地取「比当前位置更靠近目标且仍进距的槽」，否则取内收点（ArriveEpsilon 落地仍进距）。**v0.84.53：** 槽只有在「槽心距 + ArriveEpsilon 仍进距」时才可作为目的地；默认余量 0.05 小于到达阈值 0.08 时沿槽方向收到内收半径，避免站在环外侧被判到达却不射击。满环且已进距时释放过期认领，改打当前交战目标 |
| 重选 / 寻路 | 与怪物共用 `TargetRetargetInterval`：周期性在 EngageZone 内重选最近敌人并 **重算/换认领 AttackSlot**；无候选时 `GoalKind=FormationHome`；共享推进目标走 FlowField（PushMap） |
| 追击卡住强制换目标 | **方案 A 保底（v0.84.25；v0.84.26 修回归）：** 忠诚兵 `GoalKind=AttackSlot` 且 **未**进 `AttackRange`（非前摇/非进距停步挥刀），连续滑动窗累计位移不足达 `ChaseStuckRetargetSeconds`（默认 **1**，窗宽用 `StuckDetectWindowSeconds` / ε=`StuckDisplacementEpsilon`）→ 本轮选敌 **排除**当前及冷却黑名单怪、**不走**粘滞迟滞，仅在原检测半径/EngageZone 内选**仍有空闲 AttackSlot**的次近存活怪并 `TryClaim`（`TryClaim` 须先占新槽再 Release 旧槽，禁止满环时空放）。无可用空槽候选 → **保持**原目标。强制换目标后 `ChaseStuckRetargetCooldownSeconds`（默认 **1**）内不再触发且旧目标进黑名单。Rebel 不接。不改 LocalDetour/软碰撞；进距停步时脚下 Debug「追击」标签语义不变 |
| 命中方案 D | **近战**（`AttackMode=Melee`）：`AttackWindup`（时长=`MeleeWindupSeconds`）计时结束 → `HitConfirm`：若目标仍存活且仍在 `AttackRange` 内则结算伤害，否则挥空；**远程**（`AttackMode=Ranged`，士兵与远程怪）：决定攻击时即播攻击动作并进入同一 `AttackWindup`（时长=`MeleeWindupSeconds`；`0`=立刻出手）；**前摇结束**后士兵才生成弹道（目标已死/出距 → 不发射），弹道再经 **碰撞命中** / **超时未命中** 结算；远程怪本片无弹道通道 → 前摇结束再即时结算伤害（目标无效则挥空）。**抛物线射击**（`AttackMode=Parabola`，仅射手士兵；Defend+PushMap+SearchExtract）：最大接战仍用 `AttackRange`（`CombatReach`，含双方半径）；攻击槽用远程环，临时近战不改槽、不改实例枚举。前摇与 `Ranged` 相同。前摇结束按 XZ **中心距**与朝向当前目标的前方扇形（`ParabolaForwardArcDegrees`，默认 **60**）判定：扇形内有敌人且中心距 &lt; `ParabolaMeleeRange`（默认 **0.25**，不乘 `AttackRange`）→ 临时近战，`HitConfirm` 距离门用 `ParabolaMeleeRange`（多名取最近；该敌死亡或离开后若仍有人在近战距内则继续，否则回到抛物线）；否则扇形内有友军（同 `IsRebel` 的其他存活士兵；忠诚兵另计主角；不含自己）且中心距 &lt; `1×AttackRange` **并且近于当前目标**（挡在去目标的路上）→ 不发射，攻速间隔照扣；否则目标中心距 &lt; `ParabolaArcMinDistance`（默认 **1.25**）→ 与 `Ranged` 相同的直线追瞄软碰撞，不掷命中率；若按弹速飞到命中半径的时间短于 **0.2s**（含出生时已在命中半径内），表现层把飞抵目标拉长到 **0.2s** 再结算；否则在接战距离内发射抛物线，发射时按 `ParabolaHitRate`（默认 **0.6**）掷骰并锁定：命中弹追踪目标，抵达且目标仍活才结算 `OutgoingDamage`（中途死亡不结算）；未命中弹穿过目标，落到瞄准方向后方 `ParabolaMissOvershoot`（默认 **0.8**）处，停留 `ParabolaMissLingerSeconds`（默认 **1.5**）后消失且不扣血、不触发贯穿。近战门优先于友军挡射。表现：`RangedWindupHoldFrame`（1 基；≤0=不停顿）在前摇未结束时把 Attack clip 停在该帧，前摇结束再播后续帧并出手。规则层确认伤害，View 只播动作/弹道/停顿 |
| 普攻伤害 | `HitConfirm`（或远程命中）后：对怪物 `HP -= OutgoingDamage`（本批无护甲）；`OutgoingDamage = NormalAttackPower × (1 + Skill_02 Comfort) × Π(D-073 OnOutgoingDamageSettle muls)`；Comfort 满血且持有 `Skill_02` 时随等级 5%～25%，否则 0；管线倍率缺省 1；见下公式与 SkillCast |
| 怪物走跑步态 | Defend+PushMap：`MoveSpeed`=走速；`RunSpeed`=跑速（缺/≤0→回退走速）；`WalkToRunSeconds`（缺→**0.5**；`0`=一开跑）。每次进入「正在移动」从走开始；持续走满阈值→跑（移速+`RunAnims`/`IsRun`）；离开移动（攻击/死亡/进距 Idle/受堵/击晕/无 steer）立即退出跑并清计时；有效移速=`gaitSpeed×Aggro倍率×减速`（`ActiveMoveMult`/`PassiveMoveMult`）；详见 [SPEC_04 §9.19](SPEC_04_Technical.md)、[§15.5](SPEC_04_Technical.md) |
| 移动动画播放速率 | 表现层（Defend+PushMap+SearchExtract）：移动中 `animator.speed = clamp(有效移速 / 标称速度, 0.5, 2)`。标称：士兵 **3.5**；怪物走 **0.5**、跑 **1.0**。有效移速沿用现有计算（士兵含 `ChaseMoveSpeedMult`；怪物含 gait×Aggro 倍率×减速），**不**再乘一次。停步恢复 **1**。死亡 / 复活 / 远程停帧优先于该倍率。不改位移与 HitConfirm。详见 [SPEC_04 §15.5](SPEC_04_Technical.md) |
| 攻击动画播放速率 | 表现层（Defend+PushMap+SearchExtract）：每次 `PlayAttack` 采样一次当前攻击 clip 长度，`animator.speed = clamp(clipLength × 有效攻速, 0.5, 2)` 并锁存至该次动作结束。有效攻速 = 该次攻击间隔所用值（士兵 `AttackSpeed`；PushMap/SearchExtract 怪 `AttackSpeed × 攻速减速`，采样一次、不再乘第二次；Defend 怪无减速，乘数为 1）。未传有效攻速或采不到 clip → **1**（制造预览攻击保持 1）。远程停帧仍把速度打到 **0**，前摇时长仍为 `MeleeWindupSeconds`；解除时恢复该次 `attackRate`。攻击 clip 结束且移动通道未接管 → 恢复 **1**。死亡锁存 / 复活倒放优先。不改 HitConfirm 与 `1/AttackSpeed` 间隔。详见 [SPEC_04 §15.5](SPEC_04_Technical.md) |
| 怪物动画选型 | 表现层（Defend+PushMap）：`MonsterConfig.NormalAttackAnims` / `WalkAnims` / `RunAnims`（`\|` 池）；普攻每次随机基名播 `{基名}_{dir}`；走/跑在 Bind 与复活完成各抽一次；移动时按走跑步态门控播 Walk/`IsWalk` 或 Run/`IsRun`；士兵不读本池（普攻见下一行；走跑仍 `IsRun`/`RunBT`）；**v0.83.97：** 2D Zombie Pack `{基名}_{dir}` clip 须绑视觉行（表序 E/SE/S/SW/W/NW/N/NE），不得按 `DirIndex` 行号直绑。详见 [SPEC_04 §15.5](SPEC_04_Technical.md) |
| 士兵普攻动作 | 表现层（Defend+PushMap+SearchExtract）：默认按 `ClassConfig.NormalAttackAnims`（`动作ID;权重\|…`）加权抽基名播 `{基名}_{dir}`；空/非法/当前朝向无状态 → `Attack1`。`AttackMode=Parabola`：前摇开始预判前方扇形近战门——预判临时近战 → 用 `ParabolaMeleeAttackAnims`（特殊普通攻击动作；空→`Attack1`）且不停远程帧；否则仍用 `NormalAttackAnims`（远程直线/抛物线）。前摇结束结算时序不变。不改伤害。详见 [SPEC_04 §9.9b](SPEC_04_Technical.md)、[§15.5](SPEC_04_Technical.md) |
| 怪物尸体投射 | **规则+表现**（Defend+PushMap；D-083）：致命击后尸体沿 **抛物线**飞向终点（取代纯 XZ 线性滑移）；`distance`/`方向`/`Die`/`Die2` 同源 [SPEC_04 §15.5](SPEC_04_Technical.md)。`distance ≥ DeathDie2KnockbackThreshold` → 飞行扫掠 + 落地可砸其它存活怪（`OutgoingDamage×DeathCorpseSmashDamageMul`）；低于阈值仅飞不砸。砸击致死 **不**连锁投射。`MonsterCombatDead` 亦飞砸；完成后仍 Delay→倒放复活。士兵无击退 |
| 怪物死亡击飞 | **废止为独立条目**；并入上「怪物尸体投射」。历史：水平滑移 + 无 Y 抛物线 + 无砸击 |
| 攻速 | 两次攻击**开始**间隔 = `1 / AttackSpeed`；`AttackWindup` **计入**该周期内（不另加在周期外） |
| 技能 CD | 实际冷却见下式；`CooldownMode=Mode2`：**释放提交后**进入 CD（不等连发全部命中）。`CooldownMode=Mode1` 本 Demo 不驱动。详见 SkillCast |
| 战斗死亡 | 无宝石士兵 `HP ≤ 0` → `CombatDead`（可被战斗中复活技能拉起；**TBD**）；不触发 §3.11 物资去向 |
| 宝石特例 | `GemIds` 非空且 `HP ≤ 0` → **立即** `PermanentDeath`（§3.11 物资去向） |
| 彻底死亡结算 | 本阶段胜利 `Ended` **或** LevelFailure 时：仍为 `CombatDead` 且无「战斗结束复活」类技能 → `PermanentDeath`（实例消失、布阵位空） |
| 叛变 | **Rebel 不受 EngageZone 限制**；选目标仍为就近主角 / 其他士兵 / 敌人（见上）；攻击距离与命中走士兵通道（方案 D，按该兵 `AttackMode`）；对士兵/怪物普攻同用 `NormalAttackPower`；**不**施放 `SoldierSkills` 主动技能（含 `Skill_03`） |

**怪物尸体投射（DeathCorpseProjectile；D-083 规则锁；Defend + PushMap）**

| 规则 | 说明 |
|------|------|
| 合一语义 | 抛物线击飞与尸体砸人 **同一通道**：尸体 Transform 沿轨迹运动；规则层在飞行/落地检测命中并扣 HP |
| 触发 | 怪物 `RemainingHp≤0` 进入死亡表现时均启动（彻底击杀 + PushMap **`MonsterCombatDead` 假死**） |
| 击飞距离 | 与历史击飞同源：`raw=(OutgoingDamage/MaxHp)×DeathKnockbackRatioCoeff`；`distance=clamp(Min,Max,raw)`；`OutgoingDamage` = 致命击扣血前打出值（含 Comfort / D-073；**不是** `min(伤害, RemainingHp)`） |
| 击飞方向 | 基准 `normalize(M−S)_xz` 为 0°；在 `±DeathKnockbackDirectionSpreadHalfDegrees` 扇区内按 `DeathKnockbackDirectionRandomStepDegrees` 整数倍均匀随机偏角后 `RotateY`；两键 `≤0` 时固定基准方向。详见 [SPEC_04 §15.5](SPEC_04_Technical.md) |
| 砸击门闩 | **`distance < DeathDie2KnockbackThreshold`**（← `CombatConstantConfig`，样例 `1`）→ **仅**抛物线位移 + 死亡 latch；**不产生**砸击伤害。`distance ≥ 阈值` → 启用飞行扫掠 + 落地砸击 |
| 可砸目标 | **仅**其它 **存活**怪物（`RemainingHp>0`、可选中、非飞行尸体自身）。**不含**己方士兵、主角护盾 |
| 伤害时机 | **飞行途中**软碰撞扫掠 + **落地瞬间**落点检测；同一尸体飞行对同一 `targetRuntimeId` **只结算一次**（`alreadyHit` 集合；途中已命中者落地 **不**重复） |
| 砸击伤害 | 规则层独立通道：`CorpseSmashDamage = killerOutgoingDamage × DeathCorpseSmashDamageMul`（← `CombatConstantConfig`）。**不**叠 `Skill_02` Comfort、**不**走 D-073 `OnOutgoingDamageSettle` 管线 |
| 连锁 | 砸击致死 **不**再启动尸体投射；在原位 `PlayDie` latch（**无**击飞位移、**无**砸击） |
| 假死复活 | `MonsterCombatDead` 同样抛物线（砸击按门闩）；飞行 + latch **完成后**仍走原 `DelaySeconds` → 倒放复活；无敌 / `AlertRadius` 首次覆盖契约 **不变**（§3.14）；**假死 latch `RGB×0.7`**（彻底死亡 0.4 的一半变暗深度），经 Delay/倒放/无敌保持 |
| 击杀事件 | 原致命击 `MonsterKilled` / `NotifyKilled` 契约 **不变**（假死仍 **不计**击杀）。砸击扣血走 `TryApplyCorpseSmashDamage`；砸死触发正常 `MonsterKilled`，但 **不**触发新投射 |
| 表现边界 | View 驱动尸体抛物线位移 + 死亡 latch；腾空有高度时在地面投影处显示同步黑影（大小随高度插值；参数 ← `CombatConstantConfig`）；规则层确认伤害；禁止 View 直接改目标 HP。详见 [SPEC_04 §15.5](SPEC_04_Technical.md) |

**SkillCast（士兵技能施放；D-069 连发/格挡/舒适 + D-073 EffectKind 登记制 / 方案 B+）**

| 规则 | 说明 |
|------|------|
| 首片战场 | **PushMap**；Defend 本片不接线 |
| 谁施放 | 非 CombatDead；只读实例 `SoldierSkills` + `SkillConfig(SkillId, SkillLevel)`。**主动 / 有 CD**（`Skill_03`、`Skill_05` 提交、`Skill_07` 内置 CD、`Skill_09` 叠层 Tick、`Skill_12`）仅非 Rebel。**无 CD 被动**（格挡、舒适、先发制人、震晕、精英克制、贯穿、灼烧）持有即对怪生效，含 Rebel 对怪输出。`Skill_05` 致死拦截为自保、不视为主动施放、Rebel 持有亦可 |
| 本片技能 | **D-069：** 主动 **`Skill_03` 连发** + 被动 **`Skill_01` 格挡** + 被动 **`Skill_02` 舒适**（硬映射，见下）。**D-073：** `Skill_04`～`Skill_12` 走 EffectKind 管线（见下登记制）。不读灵魂/宝石/外置并行 `Skills` |
| Skill_01 钩子 | **独立被动钩子**，**不**占用普攻通道、**不**进 CD、**不**触发失控二次 roll。插入点 = 怪物普攻 `TryApplyMonsterDamageToWarrior` 结算伤害前。`ExtraActivationCondition=敌人普攻命中Self`。硬映射 `SkillEffect_01_*` → Lv1～5 概率 **10%/15%/20%/25%/30%**（不解析 Description）；成功则本次伤害变为 **0**（仍判命中，仍发 `WarriorDamageSettled`）。不格挡远程弹道（当前怪→兵无弹道通道；弹道后置）。Defend 本片不接线 |
| Skill_02 钩子 | **独立 Outgoing 倍率钩子**，**不**占用普攻通道、**不**进 CD、**不**触发失控二次 roll。插入点 = PushMap `SettleMonsterDamage`（近战/远程 HitConfirm，含 `Skill_03` 连发每一击）扣怪 HP 前。`ExtraActivationCondition=自身血量=100%` → 该次结算时 `RemainingHp >= MaxHp`（开战 clamp 后即满血）。硬映射 `SkillEffect_02_*` → Lv1～5 **+5%/+10%/+15%/+20%/+25%**（不解析 Description）。`本次伤害 = NormalAttackPower × (1 + bonus)`；**不**改写存储的 `NormalAttackPower`。受伤后立即失效；格挡成功伤害为 0 则仍满血。连发 3 击各自独立检查满血（中途受伤则后续击无加成）。Defend 本片不接线 |
| 插入点 | `Skill_03`：CD 剩余 ≤0 **且** 当前攻击目标存活 **且** 已进 `AttackRange` → **立即**开始；可插队替换**即将开始**的普攻（不等 `1/AttackSpeed`）。**不**打断进行中的普攻前摇 / 已射出弹道 |
| 通道 | **占用普攻通道**：连发每一次走命中方案 D（近战前摇→HitConfirm / 远程弹道）。连发次数内 **跳过**普攻间隔；发完后普攻间隔从 `1/AttackSpeed` 重计。不改 EngageZone / AttackSlot / FormationHome；Burst 时停步（同 Windup） |
| Skill_03 结算 | 硬映射 `SkillEffect_03_*` → 连续 **3** 次方案 D 命中（基底为 `NormalAttackPower`，不解析 Description）；每一击走 `SettleMonsterDamage`，可再叠 `Skill_02` 舒适倍率。目标中途死亡 → 中止剩余次数（CD 已走）。未命中（出距/弹道超时）不补刀 |
| CD | 开战 CD=0。`CooldownMode=Mode2`：**成功提交**瞬间写入 `SkillCooldown` 并开始倒数（`Skill_03` / `Skill_05` / `Skill_07` / `Skill_12`；`Skill_09` 用 `TickSeconds` 作叠层节拍，权威仍读 `SkillConfig.BaseCooldownSeconds=10`）。`SkillCooldown = max(SkillCdFloor, BaseCooldownSeconds − SkillCdIntDiv / max(Int,1))`。`BaseCooldownSeconds=0` 的被动不进 CD |
| 失控二次 roll | 仅当该士兵 `ΣSkillBonus ≠ 0`：每次 Mode2 **成功提交 CD**（`BaseCooldownSeconds>0` 且本次写入 CD）后再用完整 `FinalLossChance` 独立 roll；已 Rebel 跳过。`Skill_04`/`Skill_06`/`Skill_08`/`Skill_10`/`Skill_11` 无 CD **不** roll。样例行 `LossOfControlChanceBonus=0` 时本技能单独不触发 |
| 表现 | View 占用攻击通道播动作/弹道；规则层仍走既有 HitConfirm HP 通道。Debug 标签可附 CD 剩余。**CombatSkillIcon（UI-025 / D-071）：** 规则发事件、不碰 Transform。头顶瞬时图标 **35×35** 屏幕像素，静止 **0.6s** 后沿世界 **+Z**（与 DamagePopup 同轴）上飘 **0.3s** 并淡出；同兵未结束图标向**屏幕右侧**排开（间距 **4 px**，消失后左移靠齐）。持续效果脚下 **20×20**（绿圈之上、略偏屏幕下方）。D-069：`Skill_03` 提交 / `Skill_01` 格挡成功 → 头顶飘；`Skill_02` 满血 → 脚下持续 **且** 生效瞬间头顶飘一次。D-073：Handler 经同一对事件 `SkillIconPopup` / `SkillPersistChanged`（瞬时 PROC→Popup；持续状态如无敌/叠层→Persist）；**不**在 Session 按 `SkillId` 分支发图标。同 `SkillId` 脚下只保留 1 个。`CombatDead` / 销毁立即清该兵全部图标。变焦时屏幕像素不变。图标 `Resources/UI/Skills/{SkillId}`；缺图空框。Defend 不接线 |

**SkillEffectKind 登记制（D-073 / 方案 B+；对齐 MagicBook `EffectPayload`）**

| 规则 | 说明 |
|------|------|
| 禁止硬分支 | `PushMapSessionService` / View **禁止** `if (skillId == "Skill_XX")` 或按 `SkillId` 的 `switch`。新技能 = 登记表新增 Token + 表行填 `EffectKind`/`EffectParams`/`TriggerHook` + 实现一个 `ISkillEffectHandler` 并注册 |
| EffectKind | `SkillEffectConfig.EffectKind` = 单一 PascalCase Token（`^[A-Za-z][A-Za-z0-9]*$`）；空 = 未实现（跳过）。Token **必须**出现在 [SPEC_04 §9.21b](SPEC_04_Technical.md) 登记表；未登记 → 空 apply + Warning |
| EffectParams | `Key=Value` 或 `Key=Value\|Key=Value\|…`（管道分隔，同 MagicBook）。等级差写在 **各行** Params（或 CD 读 `SkillConfig.BaseCooldownSeconds`）。**不解析** `Description` / `ExtraActivationCondition` 自然语言；CSV 措辞仅作语义对齐 |
| TriggerHook | 管线插入点（可增补）：`OnOutgoingDamageSettle` / `OnIncomingDamageSettle` / `OnWarriorAaHitConfirm` / `OnWarriorTargetAcquired` / `OnWarriorWouldDie` / `OnProjectileHit` / `OnSkillInternalCooldown` |
| Pipeline | `SkillEffectPipeline.Dispatch(hook, context)`：实例 `SoldierSkills` → `SkillConfig.SkillEffectId` → `SkillEffectConfig` → 过滤 `TriggerHook` + 非空 Kind → 已注册 Handler。Session **只**在既有结算点调用 Dispatch / Status Tick |
| CombatStatusService | 统一 Tick + 查询。**士兵 bucket**（按战士 RuntimeId）：无敌。**怪物 bucket**（按怪 RuntimeId）：击晕 / 减速 / 灼烧 DoT。死亡 / `CombatDead` 清该实体状态。View **不**写状态 |
| Outgoing 叠乘 | `OutgoingDamage = NAP × (1 + Comfort_D069) × Π(OnOutgoingDamageSettle Handler muls)`。Comfort 仍为 D-069 硬映射，在管线前或等价乘入。**不**改写存储 `NormalAttackPower`。灼烧 DoT 走独立通道：`TickDamage = 源NAP × TickDamageMul`，**不**叠 Comfort |
| D-069 保留 | `Skill_01`/`Skill_02`/`Skill_03` **本片保留** `SoldierSkillCast` 硬映射，**不**阻塞 SE-01～09。后续可迁为 Kind（意图：格挡→`OnIncomingDamageSettle` 概率伤害 0；舒适→`OutgoingMulWhenFullHp`；连发→占用普攻通道的专用 Hook），**不**作为本垂直切片验收 |

**Skill_04～Skill_12 规则摘要（对齐 Mode2 CSV；数值权威=各行 EffectParams / SkillConfig.BaseCooldownSeconds）：**

| SkillId | 名 | EffectKind | TriggerHook | 摘要 |
|---------|----|------------|-------------|------|
| `Skill_04` | 先发制人 | `OutgoingMulOnNewTargetFirstHit` | `OnOutgoingDamageSettle` | 对**新选定目标**的**第一次普攻** Outgoing × `Mul`（Lv1～5：`1.2`～`1.6`）；换目标后下一发首击可再触发；无独立 CD |
| `Skill_05` | 坚挺 | `CheatDeathInvincible` | `OnWarriorWouldDie` | 本次攻击将使 Self HP≤0 时拦截为 **HP=1** + 无敌 **1～5s**（先于 CombatDead / 宝石 PermanentDeath）；`BaseCooldownSeconds=60` Mode2 提交后进 CD |
| `Skill_06` | 震晕 | `OnAaHitChanceAoeStun` | `OnWarriorAaHitConfirm` | 普攻命中（近战 HitConfirm / 远程弹道命中）**10%** 对目标 + 半径 **1.5** 存活怪击晕 **1～5s**；Stun 期间怪不能攻击/移动；圆心=被命中怪 XZ |
| `Skill_07` | 冰冻 | `OnAaHitAoeSlow` | `OnWarriorAaHitConfirm` | 普攻命中后该敌半径 **1.5** 内存活怪攻/移速 × `0.5`，持续 **2～6s**；内部 CD 读 `BaseCooldownSeconds=10`（成功 PROC 后提交，独立于 `Skill_03` 通道） |
| `Skill_08` | 精英克制 | `OutgoingMulVsMonsterType` | `OnOutgoingDamageSettle` | 目标 `MonsterType=Elite` 时 Outgoing × `Mul`（`1.5`～`1.9`）；无 CD |
| `Skill_09` | 渐入佳境 | `StackingOutgoingMulTimed` | `OnSkillInternalCooldown` + `OnOutgoingDamageSettle` | 每 **10s** 叠一层 `StackBonus`（`0.03`～`0.12`），总加成 cap **0.6**；Outgoing 乘 `1+currentBonus`；开战起 Tick；受伤**不清**层 |
| `Skill_10` | 贯穿 | `RangedPierceExtraHits` | `OnProjectileHit` | 远程箭矢命中后**不消失**，**保持当前速度方向**继续飞，再命中 `ExtraHitCount` **1～5** 名未命中敌（各 `DamageMul=1`）；`alreadyHitRuntimeIds` 防重复；**不**每弹道 A*；无弹道（近战）不触发 |
| `Skill_11` | 灼烧 | `OnAaHitApplyBurn` | `OnWarriorAaHitConfirm` | 普攻命中施加 Burn：每 **1s** 造成源 NAP × **0.2**，持续 **2～6s**；再施加 `StackMode=RefreshDuration`（叠时不叠伤）；无技能 CD |
| `Skill_12` | 瞬移 | `RetargetFarthestTeleportBehind` | `OnWarriorTargetAcquired` | 开始寻找新攻击目标时改选 EngageZone 内 XZ **最远**存活怪，Warp 到其**背后**（目标朝向反方向 × 双方 `BodyRadius` + `ArriveEpsilon`，再 `NavMesh.SamplePosition`）；失败不进 CD、走默认最近；CD **60/50/40/30/20s** |

**战斗派生公式（士兵）：**

令 `Primary` = 士兵 `ClassId` → `ClassConfig.PrimaryStat` 对应维的属性值；`Str` / `Agi` / `Int` 分别为力量 / 敏捷 / 智力。制造/布阵静态展示取 §3.11 `StaticStat`；开战与战斗中取 `FinalStat`；Buff 变更时重算下列派生项。分母用 `max(·, 1)`。

下列系数取自 `ClassConfig.CombatConvertCoeffs`（有键覆盖）；**缺键 / 空串**回退 **`CombatConstantConfig`**（[SPEC_04 §9.20b](SPEC_04_Technical.md)）。样例常量表：`NormalAttackPrimaryMult=15`、`AttackSpeedBase=0.5`、`AttackSpeedAgiDiv=60`、`SkillCdIntDiv=30`、`SkillCdFloor=0.1`。命中参数（`AttackRange` / 前摇 / 弹速 / 超时）取自职业表独立列（怪物取 `MonsterConfig` 同名列）。

```
NormalAttackPower = Primary × NormalAttackPrimaryMult

AttackSpeed = AttackSpeedBase + AttackSpeedAgiDiv / max(Agi, 1)
  // 单位：次/秒；攻击开始间隔 = 1 / AttackSpeed

SkillCooldown = max(SkillCdFloor, SkillConfig.BaseCooldownSeconds - SkillCdIntDiv / max(Int, 1))
  // 单位：秒；SkillConfig 见 SPEC_04 §9.21；Mode2 释放提交后进 CD（D-069 Skill_03；D-073 Skill_05/07/12）
```

| 派生项 | 静态展示 | 战斗运行时 |
|--------|----------|------------|
| Primary / Str / Agi / Int | `StaticStat` | `FinalStat` |
| MaxHP | §3.11：`ceil(BodyLife + StaticStat(Strength)×MaxHpStrengthMult)` | `ceil(BodyLife + FinalStat(Strength)×MaxHpStrengthMult)`；`BodyLife` 不变 |
| NormalAttackPower / AttackSpeed / SkillCooldown | 上式 + 静态属性 | 上式 + 战斗属性（Skill_03 CD 用运行时值） |

士兵攻击状态机要点：

```
Idle/Move → (target in EngageZone) → Move to AttackRange
  → wait until attack-start interval elapsed (1/AttackSpeed)
  → AttackWindup (within interval)
  → AttackMode=Melee: HitConfirm → monster HP -= OutgoingDamage (if still valid + in range) → Recovery
  → AttackMode=Ranged: AttackWindup (MeleeWindupSeconds; PlayAttack + optional RangedWindupHoldFrame hold)
       → windup end + target valid: spawn projectile → hit: HP -= OutgoingDamage; or timeout miss → Recovery
       → windup end + target invalid: no projectile (miss) → Recovery
  → AttackMode=Parabola (archer soldiers): same windup; then forward-arc melee / friendly block / straight shot / arc+hit-rate (see 命中方案 D)
  → SkillCast Skill_03 (loyal, CD ready, in AttackRange): occupy AA channel; 3× scheme D; Mode2 CD starts on commit
  → Skill_01 block (PushMap): on monster AA hit Self, roll 10%–30% by level → damage 0 (still a hit)
  → Skill_02 comfort (PushMap): on outgoing HitConfirm (incl. each Skill_03 hit), if RemainingHp>=MaxHp → NAP × (1+5%–25% by level)
  → SkillEffectPipeline (PushMap, D-073): Dispatch at existing settle points (no SkillId branch)
  → CombatStatusService.Tick: Invincible (warrior) / Stun / Slow / Burn (monster)
  → no EngageZone target (loyal) → ReturnToFormationHome (keep retargeting; abort on new target)
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
       // PushMap Skill_01: before subtract, roll block chance → damage may become 0 (still a hit)
       // Demo: MonsterConfig.Skills unused — normal attacks only
  → Each non-Rebel soldier (WarriorCombat):
       candidates = living monsters inside EngageZone (outside not selectable)
       target = nearest candidate (AttackPriority unused this batch)
       if no candidate → NavMesh to FormationHome (StartBattle deploy pos); keep retargeting; abort return on new target
       AttackMode from SoulConfig; Primary=FinalStat(ClassConfig.PrimaryStat via ClassId); NormalAttackPower=Primary×NormalAttackPrimaryMult
       AttackSpeed=0.5+60/max(Agi,1); interval=1/AttackSpeed; windup within interval
       Skill_03 (PushMap, D-069): if CD ready + in AttackRange → occupy AA channel, 3× scheme D; Mode2 CD on commit
       Skill_01 (PushMap, D-069 SC-02): on monster AA hit, independent hook may zero this damage (still a hit)
       Skill_02 (PushMap, D-069 SC-03): on outgoing HitConfirm, if RemainingHp>=MaxHp → NAP × (1+5%–25% by level)
       SkillEffectPipeline (PushMap, D-073): Session Dispatch only; no SkillId branch
       move into AttackRange → AttackWindup
       Melee: HitConfirm → monster HP -= OutgoingDamage; Ranged: projectile hit same / timeout miss
       HP≤0 + no gems → CombatDead; HP≤0 + gems → immediate PermanentDeath (§3.11)
       every TargetRetargetInterval: reselect nearest in EngageZone / repath (or FormationHome if none)
  → Skill-cast LossOfControl re-roll if ΣSkillBonus≠0 (skip if already Rebel)
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

**Status: Framework defined (ModeSelect mode/level pick / Prepare / StartBattle / deploy / Shield / countdown spawn / pathing / win-lose / LossOfControl Rebel / WarriorCombat EngageZone·AttackMode·AttackRange·hit scheme D·death layers·NormalAttackPower·AttackSpeed; Primary from ClassConfig; CombatConvertCoeffs and AttackRange hit columns on ClassConfig / MonsterConfig; **Demo D-069: PushMap loyal soldiers cast Skill_03 burst + Skill_01 block + Skill_02 Comfort**; **D-073: Skill_04–12 EffectKind registry (SE-00–09 done)**; monster skills still unused); do not parse Description natural language as an effect engine; monster→soldier damage edge cases still TBD; **spawn / NavMesh Demo-min closed below; exact OutsideMap geometry deferred****

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
| Extra skill rolls | If soldier `ΣSkillBonus ≠ 0`, on **each skill cast** roll again with full `FinalLossChance`; skip if already Rebel; Demo = SkillCast / D-069 |
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
| TargetSelect | `Nearest` (nearest band below) / `PreferWarrior` (soldier branch uses same band) / `PreferProtagonist` |
| AttackMode | `Melee` / `Ranged`; monster `AttackRange` and hit params from `MonsterConfig` (may reuse soldier hit scheme D) |
| Vs soldier | Use `AttackPower` to **subtract from soldier current HP** directly (no armor/mitigation this batch) |
| Vs protagonist | Normal attack reduces Shield by 1 only (above); do not use `AttackPower` |
| Skills | `Skills` references SkillId+CD; skill-effect table **TBD**; **unused in Demo v1** (normal attacks only; ignore or leave empty at implement time) |
| Loot | On kill, `LootDrop` (encoding `Id;Count|Id;Count|…`; **not** GraveQuality `DropMode` / `Id;Weight;Count`) |

**Targeting & pathfinding (monsters)**

| Rule | Notes |
|------|-------|
| Select target | Per `MonsterConfig.TargetSelect` (above) |
| Soldier nearest band | For `Nearest` and `PreferWarrior` soldier branch: among legal soldiers (Defend=combat-active; PushMap/SE=loyal combat-active; Aggro detect gate applies) compute `dMin`; **band** `bandMax = dMin×(1+NearestTargetBandRelative)+NearestTargetBandSlack` (← `CombatConstantConfig`, sample `0.25` / `0.5`). Inside band prefer soldiers with **no TargetFocus**, then nearest; if current focus still in band and same tier (free/engaged) as the new pick, keep sticky. Vs protagonist: compare band-best soldier to protagonist by absolute distance (protagonist not in band). `PreferProtagonist` unchanged |
| TargetFocus | At most one attack lock per monster (`TargetFocusRegistry`); `SetFocus` when a soldier is chosen; `ClearFocus` on none/death/passive unprovoked. Releasing AttackSlot in range does **not** clear focus |
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
| FormationSlot | **§3.18 TacticalFormation (D-084 / D-093 / D-094):** grouped members: destination = **that group's** virtual center + pattern slot offset rotated by facing; engage does **not** switch to AttackSlot. They pause to swing only after arriving, and only at enemies inside the group's forward arc. Incoming soft collision is 0 while holding. Each group has its own center (PushMap FlowField / Defend holds that group's deploy point) |
| Vs engage pause | PushMap: **MP-05 wired** — loyal soldiers entering engage detect (center dist to a living monster ≤ **`max(weapon reach, that monster's AlertRadius)`**; weapon reach = `max(monster AttackRange, soldier AttackRange) + monsterBody + soldierBody + ArriveEpsilon`; missing `AlertRadius` defaults to that monster's `AttackRange`) switch to `GoalKind=AttackSlot` (claim slot + LocalDetour) and **leave** Objective FlowField; on clear, release slot and resume `GoalKind=Objective`. If no free slot: **do not** hard-pause — keep `GoalKind=Objective` and continue field/detour. **Not** map-wide EngageZone targeting (would abandon Capture to chase distant enemies). Hits still require `AttackRange` (scheme D); see §3.14 |
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
| **Explicitly out (Follow)** | **Do not** add “follow protagonist / army-radius blob” as **default** locomotion (BMH `EMoveType::NORMAL` + `ArmyRadius` + follow-speed curves). Soldiers **not** in an active tactical formation still use `FormationHome` / PushMap `Objective` FlowField when idle — **not** sticky follow. **Exception (§3.18 / D-094):** active tactical-formation members use `GoalKind=FormationSlot` (virtual center + slots; engage does not leave the slot) — **not** follow protagonist, **not** BMH ArmyRadius blob |
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
| Demo scope | **Demo (D-069 + D-073)**: PushMap loyal soldiers may read/cast **`Skill_03` (burst)** from instance `SoldierSkills`; holding **`Skill_01`** may block monster AA hits; holding **`Skill_02`** at full HP adds +5%–+25% outgoing; holding **`Skill_04`–`Skill_12`** with `EffectImplemented=1` applies via the EffectKind pipeline. Do **not** read Soul/Gem/ExtraEquipment parallel `Skills`. Monster skills and Defend wiring **deferred**. `SkillCooldown` formula drives Mode2 skills with `BaseCooldownSeconds>0` |
| Scope | Non-Rebel soldiers’ normal-attack / ASPD / **active-skill** flow in `Combat`; `Skill_01` block is an on-hit passive hook (does not occupy that flow); `Skill_02` Comfort is an outgoing-mul hook (does not occupy that flow); revive still deferred |
| EngageZone | Candidate enemies = living monsters **inside EngageZone**; outside (incl. still-`OutsideMap` spawns not yet in zone) **not selectable** |
| Target select | **Default**: nearest living enemy inside EngageZone |
| FormationHome | World position locked at StartBattle deploy (that soldier’s `BattleFormation` slot); does not change from Prepare edits mid-combat |
| No target → auto-return | **Loyal** (non-Rebel) soldiers: after current target dies (or otherwise) if EngageZone has **no** next candidate → **auto-return** to `FormationHome` (`GoalKind=FormationHome`; straight or light path under MassCombatPathing); idle there if still no target; **do not** chase outside zone |
| Retarget while returning | During auto-return, still search EngageZone every `TargetRetargetInterval`; on a new candidate → **abort return** and chase / attack (switch to `AttackSlot`) |
| Rebel vs return | **Rebels do not** auto-return to formation (keep nearest protagonist / soldiers / enemies) |
| AttackPriority | `SoulConfig.AttackPriority` **unused** for targeting this batch; same enum as `TargetSelect`; field kept |
| AttackMode | From the instance `AttackMode` (soul placed → `SoulConfig`; Mode2 no-soul → `ClassConfig`): `Melee` / `Ranged` / `Parabola`. Monsters stay `Melee` / `Ranged` only. Config examples (not ClassName hardcoding): Warrior-like→`Melee`+`Strength`; Archer-like→`Parabola`+`Agility`; Mage-like→`Ranged`+`Intelligence` (PrimaryStat dim from `ClassConfig.PrimaryStat`) |
| Mage vs Archer | **Mages** stay on `Ranged` (enter range → `AttackWindup` → homing projectile → collision hit / timeout miss). **Archers** use `Parabola` (hit scheme D below). Rules-layer damage-dim difference remains `PrimaryStat` (Mage Intelligence / Archer Agility). Ranged monsters do not use `Parabola` |
| AttackRange | Both Melee and Ranged have AttackRange (soldiers: `ClassConfig.AttackRange`; monsters: `MonsterConfig.AttackRange`); check uses **XZ** center distance (ignore Y). Must move to claimed **AttackSlot** (or hold if already in range) before attack state. **v0.82.57:** already in `AttackRange` → hold and swing (monster parity; do not walk out to a farther ring); not yet in range → dest is a closer in-range slot, else an inward close point (ArriveEpsilon still lands in range). **v0.84.53:** a slot is a dest only when slot distance + ArriveEpsilon is still in range; with default margin 0.05 below arrive epsilon 0.08, inset along the slot angle to the closing radius so the outer shell is not treated as arrived while still out of range. A full ring while already in range releases a stale claim and fires at the current engage target |
| Retarget / path | Same `TargetRetargetInterval` as monsters: periodically reselect nearest in EngageZone and **reclaim/recompute AttackSlot**; if none, `GoalKind=FormationHome`; shared advance goals use FlowField (PushMap) |
| Chase-stuck force retarget | **Approach A safety net (v0.84.25; v0.84.26 regression fix):** loyal soldier with `GoalKind=AttackSlot` and **not** yet in `AttackRange` (not windup / not in-range hold-swing), whose sliding-window XZ displacement stays insufficient for `ChaseStuckRetargetSeconds` (default **1**; window=`StuckDetectWindowSeconds`, ε=`StuckDisplacementEpsilon`) → this select pass **excludes** the claimed + cooldown-blacklist ids, **skips** sticky hysteresis, picks the next-nearest living enemy inside the same detect radius / EngageZone **that still has a free AttackSlot**, then `TryClaim` (`TryClaim` must secure the new slot before Release; forbid empty-release on a full ring). No free-slot alternate → **keep** the current claim. After a forced switch, no further force for `ChaseStuckRetargetCooldownSeconds` (default **1**) and the old id stays blacklisted. Rebels unwired. Does not change LocalDetour / soft collision; in-range Debug foot label「追击」semantics unchanged |
| Hit scheme D | **Melee** (`AttackMode=Melee`): end of `AttackWindup` (duration=`MeleeWindupSeconds`) → `HitConfirm` if target still alive and in `AttackRange`, else miss; **Ranged** (`AttackMode=Ranged`, soldiers and ranged monsters): on attack commit play attack anim and enter the same `AttackWindup` (duration=`MeleeWindupSeconds`; `0`=fire immediately); **only after windup** do soldiers spawn a projectile (dead/out-of-range target → no spawn), then settle on **collision hit** / **timeout miss**; ranged monsters this slice have no projectile channel → settle instant damage at windup end (invalid target → miss). **Parabola** (`AttackMode=Parabola`, archer soldiers only; Defend+PushMap+SearchExtract): max engage still uses `AttackRange` via `CombatReach` (both body radii); attack slots stay on the ranged ring; temporary melee does not change the slot or the instance enum. Windup matches `Ranged`. At windup end, XZ **center** distance and a forward sector aimed at the current target (`ParabolaForwardArcDegrees`, default **60**) decide: an enemy inside the sector with center distance &lt; `ParabolaMeleeRange` (default **0.25**, not scaled by `AttackRange`) → temporary melee whose `HitConfirm` range is `ParabolaMeleeRange` (nearest if several; after that enemy dies or leaves, stay in melee while another remains inside, else return to parabola); else a friendly inside the sector with center distance &lt; `1×AttackRange` **and closer than the current target** (on the way to the target; other living soldiers with the same `IsRebel`; loyal soldiers also count the protagonist; exclude self) → no projectile, attack interval still consumed; else target center distance &lt; `ParabolaArcMinDistance` (default **1.25**) → same homing soft-hit as `Ranged`, no accuracy roll; if time-to-hit-radius at projectile speed would be under **0.2s** (including already inside the hit radius at spawn), presentation stretches the flight to **0.2s** before settling; else, while still inside engage range, spawn an arc and roll `ParabolaHitRate` (default **0.6**) once at fire: a hit tracks the target and settles `OutgoingDamage` only if it arrives and the target is still alive (death mid-flight does not settle); a miss passes through and lands `ParabolaMissOvershoot` (default **0.8**) behind the aim point, lingers `ParabolaMissLingerSeconds` (default **1.5**), then despawns with no damage and no pierce. The melee gate outranks the friendly block. Presentation: `RangedWindupHoldFrame` (1-based; ≤0=no hold) freezes the Attack clip on that frame while windup remains, then resumes and fires. Rules confirm damage; View plays anim/projectile/hold only |
| Normal damage | On `HitConfirm` (or ranged hit): monster `HP -= OutgoingDamage` (no armor this batch); `OutgoingDamage = NormalAttackPower × (1 + Skill_02 Comfort) × Π(D-073 OnOutgoingDamageSettle muls)` — Comfort is 5%–25% by level when holding `Skill_02` at full HP, else 0; pipeline muls default to 1; see formulas and SkillCast |
| Monster walk/run gait | Defend+PushMap: `MoveSpeed`=walk; `RunSpeed`=run (missing/≤0→walk); `WalkToRunSeconds` (missing→**0.5**; `0`=run immediately). Each move bout starts walk; after threshold→run (speed + `RunAnims`/`IsRun`); leaving move (attack/death/in-range idle/stuck/stun/no steer) exits run and clears timer; effective speed=`gaitSpeed×Aggro mult×slow` (`ActiveMoveMult`/`PassiveMoveMult`); see [SPEC_04 §9.19](SPEC_04_Technical.md), [§15.5](SPEC_04_Technical.md) |
| Move anim playback rate | Presentation (Defend+PushMap+SearchExtract): while moving, `animator.speed = clamp(effectiveSpeed / referenceSpeed, 0.5, 2)`. References: soldiers **3.5**; monster walk **0.5**, run **1.0**. Effective speed is the existing value (soldiers include `ChaseMoveSpeedMult`; monsters include gait×Aggro mult×slow) and is **not** multiplied again. Stop restores **1**. Death / revive / ranged hold outrank this rate. Displacement and HitConfirm unchanged. See [SPEC_04 §15.5](SPEC_04_Technical.md) |
| Attack anim playback rate | Presentation (Defend+PushMap+SearchExtract): each `PlayAttack` samples the current attack clip length once and locks `animator.speed = clamp(clipLength × effectiveAttackSpeed, 0.5, 2)` until that swing ends. Effective attack speed is the value already used for that swing's start interval (soldiers: `AttackSpeed`; PushMap/SearchExtract monsters: `AttackSpeed × attack slow`, sampled once and not multiplied again; Defend monsters have no slow, multiplier **1**). Missing effective speed or unreadable clip → **1** (manufacture preview attacks stay at 1). Ranged hold still sets speed to **0**; windup duration stays `MeleeWindupSeconds`; release restores that swing's `attackRate`. When the attack clip ends and locomotion does not own playback → restore **1**. Death latch / revive reverse-play outrank it. HitConfirm and the `1/AttackSpeed` interval unchanged. See [SPEC_04 §15.5](SPEC_04_Technical.md) |
| Monster anim pools | Presentation (Defend+PushMap): `MonsterConfig.NormalAttackAnims` / `WalkAnims` / `RunAnims` (`\|` pools); each attack picks a random base → `{base}_{dir}`; walk/run resampled on Bind and post-revive; move plays Walk/`IsWalk` or Run/`IsRun` per gait gate; soldiers do not read this pool (normal attacks: next row; walk/run stay `IsRun`/`RunBT`); **v0.83.97:** 2D Zombie Pack `{base}_{dir}` clips must bind the visual sheet row (top-to-bottom E/SE/S/SW/W/NW/N/NE), not DirIndex row numbers. See [SPEC_04 §15.5](SPEC_04_Technical.md) |
| Soldier normal-attack anim | Presentation (Defend+PushMap+SearchExtract): default weighted pick from `ClassConfig.NormalAttackAnims` (`base;weight\|…`) → `{base}_{dir}`; empty/illegal/missing facing state → `Attack1`. `AttackMode=Parabola`: at windup start, preview the forward-arc melee gate — temporary melee → `ParabolaMeleeAttackAnims` (special normal-attack anims; empty→`Attack1`) with no ranged hold frame; else keep `NormalAttackAnims` (straight/arc). Windup-end fire resolution unchanged. Damage unchanged. See [SPEC_04 §9.9b](SPEC_04_Technical.md), [§15.5](SPEC_04_Technical.md) |
| Monster corpse projectile | **Rules + presentation** (Defend+PushMap; D-083): on fatal hit corpse flies a **parabolic arc** to endpoint (replaces pure XZ linear slide); `distance` / direction / `Die` / `Die2` same source [SPEC_04 §15.5](SPEC_04_Technical.md). When `distance ≥ DeathDie2KnockbackThreshold` → flight sweep + landing may smash other living monsters (`OutgoingDamage×DeathCorpseSmashDamageMul`); below threshold fly only, no smash. Smash kills **no** chain projectile. `MonsterCombatDead` also flies/smashes; then original Delay→reverse revive. Soldiers have no knockback |
| Monster death knockback | **Retired as standalone row**; merged into «Monster corpse projectile» above. Legacy: horizontal slide, no Y arc, no smash |
| Attack speed | Interval between attack **starts** = `1 / AttackSpeed`; `AttackWindup` is **inside** that interval (not added outside) |
| Skill CD | Actual cooldown per formula below; `CooldownMode=Mode2`: CD starts **on cast commit** (do not wait for all burst hits). `CooldownMode=Mode1` unused this Demo. See SkillCast |
| CombatDead | Soldier with no gems and `HP ≤ 0` → `CombatDead` (revivable by in-combat revive skills; **TBD**); no §3.11 material fate |
| Gem exception | Non-empty `GemIds` and `HP ≤ 0` → **immediate** `PermanentDeath` (§3.11 material fate) |
| PermanentDeath settle | On stage victory `Ended` **or** LevelFailure: still `CombatDead` and no end-of-battle revive skill → `PermanentDeath` (instance gone; formation slot empty) |
| Rebel | **Rebels ignore EngageZone**; targeting remains nearest protagonist / other soldiers / enemies (above); AttackRange and hit use soldier channel (scheme D per that soldier’s `AttackMode`); vs soldier/monster also uses `NormalAttackPower`; **do not** cast `SoldierSkills` active skills (incl. `Skill_03`) |

**Monster corpse projectile (DeathCorpseProjectile; D-083 rules lock; Defend + PushMap)**

| Rule | Notes |
|------|-------|
| Unified semantics | Parabolic knockback and corpse smash share **one channel**: corpse `Transform` follows the arc; rules layer detects hits during flight / landing and subtracts HP |
| Trigger | Starts on any monster death presentation entry (`RemainingHp≤0`): true kill + PushMap **`MonsterCombatDead` fake death** |
| Knockback distance | Same as legacy knockback: `raw=(OutgoingDamage/MaxHp)×DeathKnockbackRatioCoeff`; `distance=clamp(Min,Max,raw)`; `OutgoingDamage` = pre-HP-clamp dealt on the fatal hit (incl. Comfort / D-073; **not** `min(damage, RemainingHp)`) |
| Knockback direction | Base `normalize(M−S)_xz` is 0°; uniform random yaw in `±DeathKnockbackDirectionSpreadHalfDegrees` at `DeathKnockbackDirectionRandomStepDegrees` granularity, then `RotateY`; both keys `≤0` → fixed base direction. See [SPEC_04 §15.5](SPEC_04_Technical.md) |
| Smash gate | **`distance < DeathDie2KnockbackThreshold`** (← `CombatConstantConfig`, sample `1`) → parabolic move + death latch only; **no** smash damage. `distance ≥ threshold` → flight sweep + landing smash enabled |
| Smash targets | **Only** other **living** monsters (`RemainingHp>0`, selectable, not the flying corpse). **Excludes** loyal soldiers and protagonist shield |
| Damage timing | **In-flight** soft sweep + **landing** impact at endpoint; each corpse flight settles **once per** `targetRuntimeId` (`alreadyHit` set; mid-flight hits are **not** repeated on landing) |
| Smash damage | Rules-only channel: `CorpseSmashDamage = killerOutgoingDamage × DeathCorpseSmashDamageMul` (← `CombatConstantConfig`). **Does not** stack `Skill_02` Comfort or D-073 `OnOutgoingDamageSettle` pipeline |
| Chain | Smash kills **do not** start another corpse projectile; death latch at death position (**no** knockback displacement, **no** smash) |
| Fake-death revive | `MonsterCombatDead` uses the same arc (smash per gate); after flight + latch completes, original `DelaySeconds` → reverse revive unchanged; invincible / first-revive `AlertRadius` contracts unchanged (§3.14); **fake-death latch `RGB×0.7`** (half the darken depth of true death 0.4), held through Delay/reverse/invincible |
| Kill events | Original fatal `MonsterKilled` / `NotifyKilled` contracts unchanged (fake death still **not** counted as kill). Smash damage via `TryApplyCorpseSmashDamage`; smash kill fires normal `MonsterKilled` but **does not** trigger a new projectile |
| Presentation | View drives parabolic corpse motion + death latch; rules confirm damage; View must not write target HP directly. See [SPEC_04 §15.5](SPEC_04_Technical.md) |

**SkillCast (soldier skill cast; D-069 burst/block/Comfort + D-073 EffectKind registry / Approach B+)**

| Rule | Notes |
|------|-------|
| First battlefield | **PushMap**; Defend not wired this slice |
| Who casts | Not CombatDead; instance `SoldierSkills` + `SkillConfig(SkillId, SkillLevel)` only. **Actives / CD-gated** (`Skill_03`, `Skill_05` commit, `Skill_07` internal CD, `Skill_09` stack tick, `Skill_12`) loyal-only. **No-CD passives** (block, Comfort, FirstStrike, Stun, Elite Bane, Pierce, Burn) apply if held, including Rebel outgoing vs monsters. `Skill_05` lethal intercept is self-save, not an active cast; Rebels who hold it may trigger |
| This slice | **D-069:** active **`Skill_03` burst** + passive **`Skill_01` block** + passive **`Skill_02` Comfort** (hard-map, below). **D-073:** `Skill_04`–`Skill_12` via the EffectKind pipeline (registry below). Do not read Soul/Gem/ExtraEquipment parallel `Skills` |
| Skill_01 hook | **Independent on-hit hook**; does **not** occupy the AA channel, does **not** start CD, does **not** fire extra LOC roll. Insert = before monster AA `TryApplyMonsterDamageToWarrior` subtracts HP. `ExtraActivationCondition=敌人普攻命中Self`. Hard-map `SkillEffect_01_*` → Lv1–5 **10%/15%/20%/25%/30%** (do not parse Description); success → this hit’s damage **0** (still a hit; still fire `WarriorDamageSettled`). Do not block ranged projectiles (monster→soldier has no projectile channel yet). Defend not wired this slice |
| Skill_02 hook | **Independent outgoing-mul hook**; does **not** occupy the AA channel, does **not** start CD, does **not** fire extra LOC roll. Insert = before PushMap `SettleMonsterDamage` (melee/ranged HitConfirm, including each `Skill_03` burst hit) subtracts monster HP. `ExtraActivationCondition=自身血量=100%` → `RemainingHp >= MaxHp` at that settle (full HP after StartBattle clamp). Hard-map `SkillEffect_02_*` → Lv1–5 **+5%/+10%/+15%/+20%/+25%** (do not parse Description). `this hit = NormalAttackPower × (1 + bonus)`; does **not** rewrite stored `NormalAttackPower`. Drops immediately when damaged; a blocked 0-damage hit still counts as full HP. Each of 3 burst hits checks independently (mid-burst damage drops remaining hits). Defend not wired this slice |
| Insert point | `Skill_03`: remaining CD ≤0 **and** current target alive **and** in `AttackRange` → start **immediately**; may cut in line ahead of the **upcoming** auto-attack (do not wait `1/AttackSpeed`). **Do not** interrupt an in-progress AA windup / already-fired projectile |
| Channel | **Occupies the auto-attack channel**: each burst hit uses scheme D (melee windup→HitConfirm / ranged projectile). Skip AA interval between burst hits; after burst, AA interval restarts from `1/AttackSpeed`. Do not change EngageZone / AttackSlot / FormationHome; hold feet during burst (same as Windup) |
| Skill_03 settle | Hard-map `SkillEffect_03_*` → **3** sequential scheme-D hits (base `NormalAttackPower`; do not parse Description); each hit goes through `SettleMonsterDamage` and may stack `Skill_02` Comfort. Target dies mid-burst → abort remaining hits (CD already running). Misses (out of range / projectile timeout) are not retried |
| CD | StartBattle CD=0. `CooldownMode=Mode2`: write `SkillCooldown` on **successful commit** (`Skill_03` / `Skill_05` / `Skill_07` / `Skill_12`; `Skill_09` uses `TickSeconds` as the stack cadence, still reading `SkillConfig.BaseCooldownSeconds=10`). `SkillCooldown = max(SkillCdFloor, BaseCooldownSeconds − SkillCdIntDiv / max(Int,1))`. Passives with `BaseCooldownSeconds=0` do not start CD |
| Extra LOC roll | Only if this soldier’s `ΣSkillBonus ≠ 0`: after each Mode2 **successful CD commit** (`BaseCooldownSeconds>0` and this hit wrote CD), roll full `FinalLossChance` once; skip if already Rebel. `Skill_04`/`Skill_06`/`Skill_08`/`Skill_10`/`Skill_11` have no CD and do **not** roll. Sample rows `LossOfControlChanceBonus=0` do not trigger from that skill alone |
| Presentation | View occupies the attack channel for anim/projectile; rules still settle via existing HitConfirm HP channel. Debug label may append remaining CD. **CombatSkillIcon (UI-025 / D-071):** rules fire events and do not touch Transforms. Overhead instant icon **35×35** screen px; hold **0.6s** then rise world **+Z** (same axis as DamagePopup) for **0.3s** and fade; extra live icons stack **screen-right** (gap **4 px**; compact left on despawn). Persistent effects sit at feet at **20×20** (above the green circle, slightly screen-down). D-069: `Skill_03` commit / `Skill_01` block success → overhead; `Skill_02` full HP → persist at feet **and** one overhead popup on activate. D-073: handlers use the same `SkillIconPopup` / `SkillPersistChanged` pair (instant PROC → Popup; persist states such as Invincible / stacks → Persist); do **not** branch on `SkillId` in Session to fire icons. Same `SkillId` persist keeps 1. `CombatDead` / destroy clears that soldier’s icons immediately. Zoom keeps screen pixels. Icons `Resources/UI/Skills/{SkillId}`; missing sprite → empty frame. Defend not wired |

**SkillEffectKind registry (D-073 / Approach B+; aligned with MagicBook `EffectPayload`)**

| Rule | Notes |
|------|-------|
| No hard branches | `PushMapSessionService` / Views **must not** `if (skillId == "Skill_XX")` or `switch` on `SkillId`. A new skill = register a token + fill `EffectKind`/`EffectParams`/`TriggerHook` on the row + implement one `ISkillEffectHandler` and Register |
| EffectKind | `SkillEffectConfig.EffectKind` = one PascalCase token (`^[A-Za-z][A-Za-z0-9]*$`); empty = unimplemented (skip). Token **must** appear in the [SPEC_04 §9.21b](SPEC_04_Technical.md) catalog; unregistered → empty apply + Warning |
| EffectParams | `Key=Value` or `Key=Value\|Key=Value\|…` (pipe; same as MagicBook). Per-level numbers live on **each row’s** Params (or CD from `SkillConfig.BaseCooldownSeconds`). Do **not** parse `Description` / `ExtraActivationCondition` natural language; CSV wording is semantic alignment only |
| TriggerHook | Pipeline insert points (extensible): `OnOutgoingDamageSettle` / `OnIncomingDamageSettle` / `OnWarriorAaHitConfirm` / `OnWarriorTargetAcquired` / `OnWarriorWouldDie` / `OnProjectileHit` / `OnSkillInternalCooldown` |
| Pipeline | `SkillEffectPipeline.Dispatch(hook, context)`: instance `SoldierSkills` → `SkillConfig.SkillEffectId` → `SkillEffectConfig` → filter `TriggerHook` + non-empty Kind → registered handler. Session **only** calls Dispatch / Status Tick at existing settle points |
| CombatStatusService | Unified Tick + query. **Warrior bucket** (warrior RuntimeId): Invincible. **Monster bucket** (monster RuntimeId): Stun / Slow / Burn DoT. Death / `CombatDead` clears that entity. Views **do not** write status |
| Outgoing multiply | `OutgoingDamage = NAP × (1 + Comfort_D069) × Π(OnOutgoingDamageSettle handler muls)`. Comfort stays D-069 hard-map, multiplied before or equivalently with the pipeline. Do **not** rewrite stored `NormalAttackPower`. Burn DoT is a separate channel: `TickDamage = source NAP × TickDamageMul`; does **not** stack Comfort |
| D-069 kept | `Skill_01`/`Skill_02`/`Skill_03` **keep** `SoldierSkillCast` hard-map this slice and do **not** block SE-01–09. Later they may migrate to Kind (intent: block → chance-zero on `OnIncomingDamageSettle`; Comfort → `OutgoingMulWhenFullHp`; burst → a dedicated occupy-AA hook). Not an acceptance item for this vertical |

**Skill_04–Skill_12 rule summaries (aligned with Mode2 CSV; numbers from per-row EffectParams / `SkillConfig.BaseCooldownSeconds`):**

| SkillId | Name | EffectKind | TriggerHook | Summary |
|---------|------|------------|-------------|---------|
| `Skill_04` | FirstStrike | `OutgoingMulOnNewTargetFirstHit` | `OnOutgoingDamageSettle` | First **normal attack** on a **newly selected** target: outgoing × `Mul` (Lv1–5: `1.2`–`1.6`); re-triggers after a target switch; no separate CD |
| `Skill_05` | Unyielding | `CheatDeathInvincible` | `OnWarriorWouldDie` | When this hit would put Self at HP≤0, intercept to **HP=1** + Invincible **1–5s** (before CombatDead / gem PermanentDeath); `BaseCooldownSeconds=60` Mode2 CD on commit |
| `Skill_06` | Stun | `OnAaHitChanceAoeStun` | `OnWarriorAaHitConfirm` | On AA hit (melee HitConfirm / ranged projectile hit), **10%** stun the target + living monsters in radius **1.5** for **1–5s**; stunned monsters cannot attack or move; circle center = hit monster XZ |
| `Skill_07` | Freeze | `OnAaHitAoeSlow` | `OnWarriorAaHitConfirm` | On AA hit, living monsters in radius **1.5** of that enemy get attack/move × `0.5` for **2–6s**; internal CD from `BaseCooldownSeconds=10` (commit on successful PROC; independent of `Skill_03` channel) |
| `Skill_08` | Elite Bane | `OutgoingMulVsMonsterType` | `OnOutgoingDamageSettle` | When target `MonsterType=Elite`, outgoing × `Mul` (`1.5`–`1.9`); no CD |
| `Skill_09` | Warming Up | `StackingOutgoingMulTimed` | `OnSkillInternalCooldown` + `OnOutgoingDamageSettle` | Every **10s** add one stack of `StackBonus` (`0.03`–`0.12`), total bonus cap **0.6**; outgoing × `1+currentBonus`; ticks from StartBattle; damage does **not** clear stacks |
| `Skill_10` | Pierce | `RangedPierceExtraHits` | `OnProjectileHit` | Ranged projectile **does not despawn** on hit; **keeps current velocity**; hits `ExtraHitCount` **1–5** additional unhit enemies (`DamageMul=1` each); `alreadyHitRuntimeIds` prevent repeats; **no** per-projectile A*; melee (no projectile) does not trigger |
| `Skill_11` | Burn | `OnAaHitApplyBurn` | `OnWarriorAaHitConfirm` | On AA hit apply Burn: every **1s** deal source NAP × **0.2** for **2–6s**; re-apply `StackMode=RefreshDuration` (refresh duration, not damage); no skill CD |
| `Skill_12` | Blink | `RetargetFarthestTeleportBehind` | `OnWarriorTargetAcquired` | When starting to pick a new attack target, retarget the **farthest** living monster in EngageZone (XZ) and Warp **behind** it (opposite facing × both `BodyRadius` + `ArriveEpsilon`, then `NavMesh.SamplePosition`); failure does not start CD (keep default nearest); CD **60/50/40/30/20s** |

**Combat derive formulas (soldiers):**

Let `Primary` = the attribute dim selected by soldier `ClassId` → `ClassConfig.PrimaryStat`; `Str` / `Agi` / `Int` = Strength / Agility / Intelligence. Manufacture / formation UI uses §3.11 `StaticStat`; StartBattle and combat use `FinalStat`; recalc derives when Buffs change. Denominators use `max(·, 1)`.

Coeffs from `ClassConfig.CombatConvertCoeffs` (present keys override); **missing key / empty** fall back to **`CombatConstantConfig`** ([SPEC_04 §9.20b](SPEC_04_Technical.md)). Sample constants: `NormalAttackPrimaryMult=15`, `AttackSpeedBase=0.5`, `AttackSpeedAgiDiv=60`, `SkillCdIntDiv=30`, `SkillCdFloor=0.1`. Hit params (`AttackRange` / windup / projectile / timeout) from the same table's separate columns (monsters: `MonsterConfig` same-named columns).

```
NormalAttackPower = Primary × NormalAttackPrimaryMult

AttackSpeed = AttackSpeedBase + AttackSpeedAgiDiv / max(Agi, 1)
  // attacks per second; attack-start interval = 1 / AttackSpeed

SkillCooldown = max(SkillCdFloor, SkillConfig.BaseCooldownSeconds - SkillCdIntDiv / max(Int, 1))
  // seconds; SkillConfig in SPEC_04 §9.21; Mode2 CD starts on commit (D-069 Skill_03; D-073 Skill_05/07/12)
```

| Derive | Static UI | Combat runtime |
|--------|-----------|----------------|
| Primary / Str / Agi / Int | `StaticStat` | `FinalStat` |
| MaxHP | §3.11: `ceil(BodyLife + StaticStat(Strength)×MaxHpStrengthMult)` | `ceil(BodyLife + FinalStat(Strength)×MaxHpStrengthMult)`; `BodyLife` unchanged |
| NormalAttackPower / AttackSpeed / SkillCooldown | formulas + static attrs | formulas + combat attrs (`Skill_03` CD uses runtime value) |

Soldier attack state machine (sketch):

```
Idle/Move → (target in EngageZone) → Move to AttackRange
  → wait until attack-start interval elapsed (1/AttackSpeed)
  → AttackWindup (within interval)
  → AttackMode=Melee: HitConfirm → monster HP -= OutgoingDamage (if still valid + in range) → Recovery
  → AttackMode=Ranged: AttackWindup (MeleeWindupSeconds; PlayAttack + optional RangedWindupHoldFrame hold)
       → windup end + target valid: spawn projectile → hit: HP -= OutgoingDamage; or timeout miss → Recovery
       → windup end + target invalid: no projectile (miss) → Recovery
  → AttackMode=Parabola (archer soldiers): same windup; then forward-arc melee / friendly block / straight shot / arc+hit-rate (see 命中方案 D)
  → SkillCast Skill_03 (loyal, CD ready, in AttackRange): occupy AA channel; 3× scheme D; Mode2 CD starts on commit
  → Skill_01 block (PushMap): on monster AA hit Self, roll 10%–30% by level → damage 0 (still a hit)
  → Skill_02 comfort (PushMap): on outgoing HitConfirm (incl. each Skill_03 hit), if RemainingHp>=MaxHp → NAP × (1+5%–25% by level)
  → SkillEffectPipeline (PushMap, D-073): Dispatch at existing settle points (no SkillId branch)
  → CombatStatusService.Tick: Invincible (warrior) / Stun / Slow / Burn (monster)
  → no EngageZone target (loyal) → ReturnToFormationHome (keep retargeting; abort on new target)
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
       // PushMap Skill_01: before subtract, roll block chance → damage may become 0 (still a hit)
       // Demo: MonsterConfig.Skills unused — normal attacks only
  → Each non-Rebel soldier (WarriorCombat):
       candidates = living monsters inside EngageZone (outside not selectable)
       target = nearest candidate (AttackPriority unused this batch)
       if no candidate → NavMesh to FormationHome (StartBattle deploy pos); keep retargeting; abort return on new target
       AttackMode from SoulConfig; Primary=FinalStat(ClassConfig.PrimaryStat via ClassId); NormalAttackPower=Primary×NormalAttackPrimaryMult
       AttackSpeed=0.5+60/max(Agi,1); interval=1/AttackSpeed; windup within interval
       Skill_03 (PushMap, D-069): if CD ready + in AttackRange → occupy AA channel, 3× scheme D; Mode2 CD on commit
       Skill_01 (PushMap, D-069 SC-02): on monster AA hit, independent hook may zero this damage (still a hit)
       Skill_02 (PushMap, D-069 SC-03): on outgoing HitConfirm, if RemainingHp>=MaxHp → NAP × (1+5%–25% by level)
       SkillEffectPipeline (PushMap, D-073): Session Dispatch only; no SkillId branch
       move into AttackRange → AttackWindup
       Melee: HitConfirm → monster HP -= OutgoingDamage; Ranged: projectile hit same / timeout miss
       HP≤0 + no gems → CombatDead; HP≤0 + gems → immediate PermanentDeath (§3.11)
       every TargetRetargetInterval: reselect nearest in EngageZone / repath (or FormationHome if none)
  → Skill-cast LossOfControl re-roll if ΣSkillBonus≠0 (skip if already Rebel)
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
| 士兵战斗 | 同 §3.12 WarriorCombat（EngageZone / AttackMode Melee\|Ranged / 命中方案 D / SkillCast）；**D-069 PushMap Skill_03 连发 + Skill_01 格挡 + Skill_02 舒适**；**D-073 Skill_04～12 EffectKind 管线** |
| 不复用 | Defend 倒计时刷怪（`WaveSpawnConfig` / `SpawnRemainingSeconds`）；清场胜利条件（刷怪行全触发+全灭） |
| 表现机位 | 与 Defend **同为正交俯视**（`Euler(90,0,0)`）；Combat 须启用专用战斗相机，不得落到场景透视主相机（见 [SPEC_04 §6](SPEC_04_Technical.md)） |
| 镜头跟随 | Combat 专属跟随（Prepare 仍用 FormationCamera，正交 Size ← **`PushMapPrepareOrthoSize`**（样例 `4.5`；**不**用 `max(half)−CameraOrthoSizeMargin`））。**Prepare 路径预览（可选，默认不播）：** 仅 `PushMapPrepare`；`FormationEditorRoot_Mode2` 在 `StartBattleButton` 上方提供「快速预览」与 `CameraPathSlider`。滑动条 `value∈[0,1]` ↔ `CameraFollowPath` 弧长进度 `s`（左=`WP_Start`、右=`WP_End`，沿折线均匀）；拖动 → 即时 Snap FormationCamera 世界 XZ；拖动打断正在播的预览。进入 Prepare 保持地图中心机位（**不**自动从 `WP_End` 扫）；滑动条初值 = 当前机位投影 `s`（失败则 0）。「快速预览」：Snap 到作者路点 **末点**（`WP_End`）→ 沿烘焙折线 **反向**移到 **起点**（`WP_Start`）；世界 XZ 恒速 **`PushMapCameraIntroSpeed` ← `CombatConstantConfig`**（样例 `1.5`）；每个作者路点停留 **`PushMapCameraIntroWaypointDwellSeconds` ← 同表**（样例 `0.5`）；播放中同步推动滑动条；结束后停在 `WP_Start`。缺轨 / Bake 失败 / 作者路点 &lt; 2 → 隐藏或禁用预览控件。**开战：** 进入 `Combat` 后 **无** `IsCombatIntroActive` 门闩；部署后立刻启动计时与玩法 → `CameraFollowMode`：`Auto`（默认）= 跟随地图 **`CameraFollowPath`** 折线上的机位，**不**粘士兵 Transform。进度 `s∈[0,1]` = 场上**忠诚存活**（`!IsRebel` 且非 `CombatDead`）士兵把世界 XZ **投影到折线**后的**最大值**；领头兵死亡/叛变/失活 → `s` 变小 → `SmoothDamp` **回退**（不 Snap）。无可跟随忠诚兵 → **定格**最后机位（不跟主角、不回地图中心）。缺 `CameraFollowPath` 或未烘焙折线 → warn + **回退**旧行为（粘随距 **CurrentObjective** 最近忠诚兵）。Auto 表现：世界 XZ 圆形死区 **`CameraFollowDeadzone` ← 同表**（样例 `0.15`）内忽略目标小幅位移（镜头不动）；超出后以 **`CameraFollowSmoothTime` ← 同表**（样例 `0.25`）对 XZ 做 `SmoothDamp` 缓动追赶（Y/旋转不变）；`EnterAuto` / 开战启用 → **立刻 Snap** 到**当时**的折线点（或回退士兵）XZ（清零 damp 速度）；进度回退**不** Snap。`Manual` = 左键拖动画布，镜头 XZ 平移；底中「恢复跟随」（`ResumeFollow`）**仅手动态显示**，点击 → `Auto` 并隐藏。机位高度 **`CameraHeightY` ← 同表**；开战默认 `PushMapCameraOrthoSize` ← 同表（样例 `2`）；Combat 滚轮缩放 Size，钳制 **`[CameraOrthoSizeMin, CameraOrthoSizeMax]`**（样例 `[0.5, 20]`；前滚拉近变小、后滚拉远变大）；缩放不切换跟随模式；恢复跟随不重置 Size |

**阶段内子状态（PushMapPhase）**

| 子状态 | 说明 |
|--------|------|
| `Prepare` | 加载地图与布阵；可编辑布阵；含「开战」；可选路径预览（快速预览 + 滑动条，默认不播）；**开战预览刷怪**（同开战非陷阱行，Idle 展示，无 AI/伤害）；不可制造 |
| `Combat` | 部署单位后立刻推进玩法（无开战镜头门闩）；目标点推进；刷怪点/陷阱；AggroMode；护盾与失控运行中 |
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
| 士兵推进 | **全队共当前目标**：所有忠诚士兵以 `CurrentObjective` 为共享目的地；移动走 **FlowField**（§3.12 MassCombatPathing 方案 B）；可途中被 EngageZone 内敌人打断选敌（改 AttackSlot）；无候选则继续采样流向当前目标的场。**§3.18（D-084 / D-093 / D-094）：** 已入组的成员改由**该组虚拟中心**跟 FlowField，成员 `GoalKind=FormationSlot`，接敌不改 AttackSlot；未入组/已解散成员仍走上句语义 |
| 推进与占领 | 占领仅看「是否到达」；圈内有存活怪物 **不**阻止占领，也 **不**单独暂停推进（遇敌改 AttackSlot 见下） |
| Demo 遇敌暂停推进 | 完整 §3.12 WarriorCombat 接入前：忠诚兵中心距任一存活怪 ≤ `max(AttackRange, 怪BodyRadius+士兵BodyRadius)` 时 **暂停推进**（停跟场、清本地速度；勿依赖全开 RVO）；离开后恢复采样 FlowField。正式 Engage 选敌后改 `GoalKind=AttackSlot`；命中仍后置完整 WarriorCombat |
| FlowField 重建 | `CurrentObjective` 切换、开战 Bake（含 AirWall 不可走）、可走面变更 → 重建指向新目标的场；同目标单位共享一场 |
| 到达＝占领 | **任一**忠诚士兵进入当前目标 `CaptureZone` → **立即占领（Capture）**；无需全部到达；**无**计时、**无**「圈内无怪」附加条件。**移动：** 进入圈后 **不再** 采样 FlowField 趋近目标格中心（避免全队挤死零向量格）；圈内以 LocalDetour **软分离** 维持站位间距；圈外继续跟场；场向量≈0 且仍在圈外时直趋目标回退 |
| 占领效果 | ① 该目标点本场标记已占领；② **关联该目标的刷怪点本场不再刷怪**；③ 已刷出且仍存活的怪物 **保留** 直至击杀；④ 可发放配置奖励并写入副本解锁钩子（见下）；⑤ 当前目标切换为下一未占领目标；**若无下一目标且地图有 `BossPoint` → 全队推进目标改指 BossPoint（v0.74.10 BOSS 引导，见下「Demo BOSS 引导边界」）**；无 `BossPoint` 时士兵保持当前位置/就近守备直至 BOSS 通关或失败 |
| 占领奖励 | 来自 `PushMapGameplayConfig` 或目标点绑定配置的物资/精魂等；奖励项统一先命中 `ItemCatalogConfig`，再分发到仓库 / 精魂 / 魔法书 / 主角装备；**不**入账阶段经验 |
| 副本解锁 | 配置的 `DungeonUnlockIds` 写入存档集合钩子；**副本玩法正文 TBD** |
| 规则归属 | `CurrentObjective` 链与占领判定/事件归属 **规则层**（`PushMapSessionService`）；地图 `ObjectivePoint`/`CaptureZone` 仅作者标记，运行时不自管 Tick；表现层每帧上报「是否有忠诚兵在当前圈内」 |
| Demo 到达边界 | 表现层扫描忠诚 `PushMapAdvanceView`：`!IsRebel && CaptureZone.ContainsXZ` → `TickCapture(true)`；Rebel 不触发占领；`CaptureSeconds` 配置列可加载但 **规则忽略** |
| Demo 刷怪 AI 边界 | PM-05 怪物 AI 用 Defend 默认追击语义（就近忠诚兵/主角；进 `AttackRange` 普攻；对主角扣盾）；AggroMode 四态后置 **PM-06**（见下「Demo AggroMode 边界」）；BOSS 通关结算见「Demo BOSS 通关边界」 |
| Demo AggroMode 边界 | PM-06 四态实现：主动态（`ActiveChase`/`StationaryActive`）发现半径用 `AlertRadius`（与 `AttackRange` 并列）且**仅**对忠诚士兵触发主动发现；被动态（`PassiveChase`/`StationaryPassive`）须先被攻击→挑衅优先＝对该怪的士兵 **真实 HitConfirm**（PM-12）；可保留「忠诚兵首次进入该怪 `AttackRange`」兜底，以免远程未命中永远不激怒；原地态（`Stationary*`）不移动，仅 `AttackRange` 内攻击。技能施放 / 副本玩法正文 **不做** |
| Demo BOSS 通关边界 | PM-07：击杀 `IsBoss` 行生成且落于 `BossPoint` 的 BOSS → `Ended` → 入账 `StageExpReward`（`AddExperience`）；**不**立即 `TryAdvanceStage`。`Shield≤0` **或** 已登记士兵≥1 且场上无忠诚存活（`!IsRebel && !CombatDead`）→ LevelFailure **不**入账本阶段经验。叛变写入规则层 `IsRebel` 供判定。**击杀契约（PM-12 起）：** 怪物 `RemainingHp≤0` → 表现层 `NotifyKilled`（事件带 `outgoingDamage`；击飞见 SPEC_04 §15.5）；BOSS 另 `TryNotifyBossKilled`。占领时发放 `CaptureLoot`（**不含**经验），并先经 `ItemCatalogConfig` 解析后再累加本场展示 ledger；写 `DungeonUnlockIds`；通关同样写解锁钩子。`IsBoss` 与 `BossPoint`：缺标记 warn |
| Demo 战斗结算 / 奖励 UI | **UI-017 / UI-018（方案 A 扩展）：** 胜负均先弹 UI-017。**胜利：**标题「胜利」；耗时 `mm:ss`、击杀怪物总数、**阵亡士兵总数**、四基础职业阵亡（图标+数量）；Continue → UI-018（已入账 Exp + CaptureLoot；无额外发放）→ 结束关卡 + `LevelSelectPanel`。**失败：**标题「失败」；阵亡士兵总数；「返回主界面」→ `AbortLevel` + TitleMenu（UI-027）；「重新开始」→ `AbortLevel` 后立刻重进同 `LevelId`+`OptionId`（Prepare；不清存档槽；已 PermanentDeath 不复活）。阵亡统计归属 Session（`BattleCasualtyStats`）。Defend 同款 UI **不做** |
| Demo 士兵攻击 / WarriorCombat 边界 | **PM-12（方案 B）：** PushMap 忠诚士兵战斗对齐 §3.12 方案 D——`AttackMode=Melee`：`AttackWindup` 结束 → `HitConfirm`（目标仍存活且在 `AttackRange`）→ 怪 `HP -= NormalAttackPower`；`AttackMode=Ranged`：生成弹道（复用 Defend `ProjectileView`）→ 软碰撞命中再结算 / 超时未命中不结算。`NormalAttackPower` / `AttackSpeed` / 前摇 / 弹速取自开战登记（`WarriorCombatMath` + `ClassConfig`，镜像 Defend）。保留 FlowField / AttackSlot / 粘滞选敌；**移除**固定 `PushMapAttackAnimSeconds` 仅播动作、无结算的旧边界。怪物本片仍可不驱动 Animator（§15.5） |
| Demo 遇敌检测（警戒半径） | **v0.82.55 方案 C：** PushMap 忠诚选敌半径 = `max(武器触及, 该怪 AlertRadius)`（武器触及见 §3.12）。近战职业 `AttackRange` 仅 0.35 时武器触及约 0.86，三人围 `Monster_12`（`AlertRadius=4`）时旁边友军也会改 `AttackSlot`。**v0.82.57：** 已「追击」但未进距时须继续贴近（内收点/更近槽），已进距则停步挥刀；命中距离用 XZ。仍**不是**全图 EngageZone。 |
| Demo 伤害飘字边界 | **DamagePopup（PM-12/13）：** 规则层命中成功后，在**被击目标**头顶显示 `-受伤值`（数值与本次结算伤害一致）。敌方怪物：红色；我方士兵：白色；敌我字号均为 **12**。出现后 **0.5s** 内世界坐标 `position.z` 相对起点从 **+0** 线性增至 **+0.5**，随后销毁（不做 Y 轴持续上浮）。主角护盾受击**不要求**飘字。防守战本需求 **不做** |
| Demo 受伤闪烁边界 | **HitFlash（PM-12/13）：** 与飘字同一命中成功事件。目标子树 Renderer 临时亮色（`MaterialPropertyBlock`，勿永久改共享材质）。怪物亮**红**；士兵亮**白**。共 **2** 次脉冲（立即 1 + 再 1），每次持续 **0.1s**，**中间不灭**（紧接）→ 视觉连续亮约 **0.2s** 后恢复本色。闪烁未结束再次受伤 → 从头刷新。未命中/挥空不闪。防守战本需求 **不做** |
| Demo 友军脚下圈边界 | **AllyFootCircle：** Defend/PushMap Combat **忠诚存活**士兵脚下绿描边 + 内黑 α=**160/255**；半径=`BodyRadius`；localPos `(0,-0.05,-0.2)`；rotation X=**-30**；Order In Layer=`50`；跟随移动；叛变/死亡隐藏；见 [SPEC_04 §9.7](SPEC_04_Technical.md) |
| Demo 镜头迷雾边界 | **CameraFog（v0.83.05）：** PushMap **仅 `Combat`** 显示 `DigFogCanvas`；`Prepare`/`Ended` 与 Meta 弹窗打开时隐藏；由全局 `CameraFogService` 驱动，见 §3.10 |
| Demo 战斗技能图标边界 | **CombatSkillIcon（UI-025 / D-071 / 方案 A）：** PushMap 仅。头顶 35×35 静止 0.6s 后世界 +Z 上飘 0.3s 淡出；同兵右排 4px。`Skill_03` 提交与 `Skill_01` 格挡成功头顶飘；`Skill_02` 满血脚下 20×20 且生效瞬间头顶飘一次，受伤收起。`CombatDead` 立即清图标。规则事件 `SkillIconPopup` / `SkillPersistChanged`。Defend **不做** |
| Demo 战斗指示器边界 | **CombatIndicator（UI-033 / D-089 / 方案 A）：** PushMap + SearchExtract **仅 `Combat`** 显示。ScreenSpaceOverlay，参考分辨率 1920×1080；根锚点顶中 `(0.5,1)`，`anchoredPosition.y=-10`；根 `localScale=0.75`；`raycastTarget=false`。中央背景 `HPPK_UI_1`（`CenterBg`）：**子节点**左蓝/右红 = **当前存活数**（不含死亡 0.5s 残留格；Legacy Text BestFit 字号 26～42）。**敌方开场未知：** 本场尚未出现过 `EnemyAliveCount>0` 且当前为 0 → 显示 **`?`**；一旦现身过，本场后续含清零均显示数字；闩锁在 `ResetBattleState` 清零。我方始终显示数字。左侧我方每兵一格 `HPPK_UI_2`（右对齐靠中心）；右侧敌方每怪一格 `HPPK_UI_3`（左对齐靠中心）。简画：`ClassConfig.SilhouetteIconAssetId` / `MonsterConfig.SilhouetteIconAssetId` → `Resources/UI/Icons/{Id}`（缺图空框仍显示格）。**排序（稳定）：** 我方 `ClassLevel` 降序 → 同级职业表 `TableOrder` 升序 → 同 ClassId 开战登记序；敌方 `MonsterType` 降序 → 同类型怪物表 `TableOrder` 升序 → 同 MonsterId 刷出登记序。**HP% 着色（tint 白描简画）：** `p≥66%` 绿；`33%<p<66%` 橙；`0<p≤33%` 红；永久死亡（HP=0 且不可复活）→ 灰 + 中心 `HPPK_UI_4`，**0.5s** 后移除该格并重对齐。**复活算活着：** `RevivePhase≠None` 或仍有 `RevivesRemaining` 的怪不进死亡格、中央计数仍算存活。叛变不入左/右两侧。单行超出半屏可用宽 → **截断不显示第 2～N 行**（中央全量存活数仍更新）。View **0.2s** 抽照；死亡倒计时仅 View 记忆；不改战斗伤害/复活规则热路。Prepare / Ended / UI-017 结算期间隐藏。Defend **不做** |
| Demo 离屏刷怪边缘提示边界 | **OffScreenSpawnHint（UI-034 / D-090 / 方案 A）：** PushMap + SearchExtract **仅 `Combat` 真实刷怪**。一次刷怪请求完成后，用该次 **`basePos`（SpawnPoint / `ResolveSpawnPosition`）** 相对战斗主相机 `WorldToViewportPoint`：视口 `[0,1]²` 内 → 不出；在外 → 钳到最靠近方向的屏幕边缘（边距 `OffScreenSpawnHintEdgeMarginPx`）显示 `EnemyAttack_1`（`Resources/UI/Icons/EnemyAttack_1`；缺图空框仍显示；图标不旋转；`localScale=OffScreenSpawnHintDisplayScale` 默认 **1.3**）。开场 `OffScreenSpawnHintIntroBlinkSeconds`（默认 **0.4**）内闪红 `OffScreenSpawnHintIntroBlinkCount`（默认 **2**）次（白↔红，alpha=1），再常亮 `OffScreenSpawnHintHoldSeconds`（默认 **2**）。同帧多组可各出一枚。PushMap **`PreparePreview` 不出**；Prepare / Ended / UI-017 / UI-032 期间清空进行中提示。Defend **不做**。常量见 [SPEC_04 §9.20b](SPEC_04_Technical.md)。issues `.scratch/offscreen-spawn-hint/` |
| Demo 选敌粘滞边界 | v0.74.10：遇敌选敌带**粘滞迟滞**——已认领怪仍存活且仍在其遇敌检测半径内时，仅当新候选中心距比已认领目标近超过 **`EngageStickHysteresisMargin`（默认 0.15 世界单位，常量）** 才切换认领，否则保持原认领。原因：密集怪群叠加 SoftCollision 微推使「严格最近」目标逐帧翻飞，破坏 AttackSlot / 前摇稳定性。粘滞不改变检测半径与槽位合法性；目标死亡/出圈仍正常切换（**不再**绑定已废止的 `DemoKillEngageSeconds`）。**v0.84.25：** 追击卡住强制换目标（§3.12）本轮绕过粘滞并排除当前认领；非卡住路径粘滞不变 |
| Demo 追击卡住换目标边界 | **v0.84.25 方案 A；v0.84.26 修回归：** PushMap / SearchExtract / Defend 忠诚兵共用。未进距滑动窗卡住 → 排除当前/黑名单、绕过粘滞、**仅**认领仍有空槽的次近；`TryClaim` 先占新槽再 Release；无空槽保持原目标；冷却防甩。常量见 [SPEC_04 §9.20b](SPEC_04_Technical.md)。不改 LocalDetour；StuckHold 仍独立 |
| Demo BOSS 引导边界 | v0.74.10：目标链耗尽（`CurrentObjectiveOrder=0`，全部占领）且地图有 `BossPoint` → `FlowField` 重建指向 BossPoint 世界 XZ（`CurrentObjectiveChanged(0)` 触发一次；无目标点地图开战后立即重建）；此时 `ObjectiveArriveRadius` 收紧为 **`BossAdvanceArriveRadius`（默认 0.35，常量）**，保证士兵贴近 BOSS 点；**v0.82.55：** 主动态 BOSS 另靠 `AlertRadius` 把附近友军拉进 `AttackSlot`（`Monster_12`=4）；`Stationary*` BOSS 不移动时本引导仍是唯一接近手段；无 `BossPoint` → 维持「保持当前位置/就近守备」原语义；镜头跟随仍走 `CameraFollowPath` 最大投影（不跟士兵本人） |
| Demo 士兵受击边界 | **PM-13：** 怪物对忠诚士兵按 `MonsterConfig.AttackPower` 扣士兵 `RemainingHp`（无护甲）；持有 `Skill_01` 时结算前按等级概率将本次伤害变为 0（仍判命中，仍发白飘字/白 HitFlash）。`HP≤0` → `CombatDead`（停手；宝石/PermanentDeath 对齐 §3.12 Demo 最小即可）。对主角仍 `Shield -= 1`（忽略 AttackPower；不要求主角飘字/闪烁） |
|| Demo 八向朝向稳定边界 | **v0.83.31 方案 B（Defend+PushMap 敌我）：** 移动八向跟 MassMove **意图方向** `LastDesired`（FlowField / 槽位直线），**不**跟 LocalDetour steer / 软碰撞冲量；`IsRun` 仍由 steer 判定。开始攻击时朝目标切 **一次** 后冻结至 Attack1 结束（移动打断解锁）。停步 / 进距 Idle / 受堵停滞 **不**每帧追目标。既有扇区迟滞+最短保持仍作用于 `SetFacing`。不改攻击判定 / 槽位 / 寻路。**v0.83.97：** Zombie Pack `MonsterModel_02/04/05/07/08` 的 `{Attack*}_{dir}` 须按视觉行绑 clip。详见 SPEC_04 §15.5 |
| Demo 怪物死亡复活边界 | **PushMap + SearchExtract（D-074；方案 B 共享 `IMonsterDeathSkillHost`）：** 持有 `MonsterConfig.Skills` 中 `MonsterSelfReviveOnDeath` 效果（§9.21c）的怪 HP≤0 → **MonsterCombatDead**（表现 `NotifyKilled(fakeDeathCorpse=true)` + **尸体投射抛物线**（D-083；达阈值可砸其它怪）；**不计**击杀 / BOSS / Loot）；抛物线+latch 完成后等待 `DelaySeconds` → **倒放前**按 `TargetSelect` 选敌并 `ForceSetFacing`（尸体 `_dead` 期间仍写入）决定 8 向 `DirIndex`（倒放 **优先** Creator `Die2` Trigger 对应 clip，Controller 无 `Die2` 则倒放原 `Die` latch clip；该朝向保持进 Idle，复活后 `Attack1_*` 与当前目标 8 向一致）→ HP=`MaxHp×ReviveHpRatio` + 无敌 `InvincibleSeconds`（不可选中）；**首次复活**若 EffectParams 含 `AlertRadius` → 该实例警戒半径改为该值（怪选敌 + 士兵遇敌检测均读实例值；**不**改表行；第二次及以后复活不再改）；**假死 latch `RGB×0.7` 变暗**（彻底死亡 `×0.4` 的一半深度），经 Delay/倒放/无敌保持，至无敌结束恢复；`MaxReviveCount` 用尽 → 彻底死亡走 `MonsterKilled`，`PlayDie` latch **`RGB×0.4`**。**SearchExtract 点清场**（倒计时胜利规则层清怪）**跳过** `TryInterceptDeath`，已假死实例取消复活、不二次击飞。Defend **不接线** |

**刷怪（非 WaveSpawn 倒计时）**

| 规则 | 说明 |
|------|------|
| 驱动表 | `PushMapSpawnConfig`：按 `GameplayConfigId` + `SpawnPointId`；一点可多行（多种怪物） |
| 初始朝向 | 行字段 `InitialFacing`（[SPEC_04 §9.23](SPEC_04_Technical.md)）：刷出 Idle 八向。`0`=该行每只怪各自随机 `1~8`；`1~8`=固定罗盘方向（1正上…8左上）。表现映射至 Animator `DirIndex`（§15.5）；Prepare 预览 / 开战 / 陷阱刷怪共用。缺省 `5`（正下） |
| 无陷阱刷怪 | 开战瞬间：若该刷怪点 **未** 绑定陷阱，且其 **关联目标点尚未占领**（或未关联目标=全局，开战即符合）→ 按行生成 |
| Prepare 开战预览刷怪 | 进入 `Prepare` 后：按与「无陷阱刷怪」**同一资格**实例化怪物外观（含 BOSS 行），播 Idle；**不**跑 AI / 攻击 / 占领 / 陷阱；**不**计入 `PendingBoss` / Session HP 登记。陷阱绑定行 **不**预览。点「开战」→ **先销毁**预览实例 → Bake NavMesh → 部署 → 正式 `FireStartBattleSpawns`（与 Combat 刷怪一致） |
| 陷阱刷怪 | 绑定 `TrapZoneId` 的刷怪点：我方忠诚士兵 **首次**进入该陷阱区且关联目标未占领 → 生成；本场每点默认触发一次（重复进入不重复刷，除非配置另开 **TBD**） |
| 占领停刷 | 关联目标已占领 → 该点不再新刷；场上已有怪保留 |
| 占地散开 | 同点 `SpawnCount>1` 或邻近已刷存活怪：按各自 `MonsterConfig.BodyRadius` 在可走面上错开落点，使 XZ 占地圆不重叠（NavMesh 采样失败时可略收缩环半径，最终可回退采样点）；陷阱后刷同样避让场上已有怪。**Demo 收紧（v0.73.9）：** `SamplePosition` 仅局部吸附（≈`max(0.75, BodyRadius×2.5)`）；命中点相对刷怪点 `basePos` 的 XZ 距离不得超过牵引上限（≈当前环/螺旋半径 + 半径余量，绝对上限约 `max(3, BodyRadius×10)`）；越界命中视为失败。挤满时 **优先重叠回退基点**，禁止为避让而跨空气墙吸附到菱形外侧空白 NavMesh |
| 持续避让 | Combat 中 **移动**怪：刷出散开仍按 `BodyRadius`；`NavMeshAgent.radius = min(BodyRadius, max(0.05, AttackRange − BodyAppearanceConfig.DefaultBodyRadius(0.1) − 0.05))`，保证中心距可进入 `AttackRange`（避免 RVO 半径大于攻击距导致永远无法交战）；`Stationary*` 不主动位移避让；PushMap 怪物 Demo：`NavMeshAgent.height=0.1`（`PushMapMonsterAgentView`）；**我方士兵**：`NavMeshAgent.radius` 取自 `BodyAppearanceConfig.BodyRadius`（缺省 `0.1`）、`height=0.1` |
| 禁止 | PushMap **不**使用 `RemainingCombatSeconds` / `WaveSpawnConfig` 倒计时激活 |
| Demo 实现边界 | PM-05（刷怪/陷阱）：怪物 AI 暂用 Defend 默认追击（非四态，见 §9.23 契约）；AggroMode 四态与 BOSS 通关结算 **后置**（PM-06/07）；刷怪资格 / 触发状态归 `PushMapSessionService`，位置由表现层按 `SpawnPointId` / `BossPoint` 解析；**Prepare**：`FirePreparePreviewSpawns`（`PushMapSpawnTrigger.PreparePreview`）刷非陷阱行 Idle；**开战顺序**：销毁预览 → Bake NavMesh → 部署忠诚兵 → `FireStartBattleSpawns`；占地散开与 Agent 半径见 **PM-10** |
| Demo 可走面边界 | `DigMapBounds`（及 `WalkSurface` / `EngageZone`）须覆盖样例图上目标点 / 刷怪点 / BossPoint；NavMesh Runtime Bake 以 `DigMapBounds` 为准；标记落在界外会导致推进/刷怪无法上网格。样例 `PushMap_Demo_01` 空气墙须保持目标点可走（见上「Demo 空气墙边界」作者硬约束） |

**BOSS 与胜负**

| 规则 | 说明 |
|------|------|
| 通关 | 击杀 **BossPoint** 生成的 BOSS 怪物（`PushMapSpawnConfig.IsBoss=1`）→ `PushMapPhase=Ended` → 阶段胜利 → 入账 `StageExpReward` → UI-017（含阵亡）→ UI-018 → 结束关卡并打开 LevelSelectPanel（Demo；不自动下一阶段） |
| 失败 | `Shield ≤ 0` **或** 无忠诚存活士兵 → LevelFailure；**不**入账本阶段经验 → UI-017（失败布局）→「返回主界面」Abort+TitleMenu / 「重新开始」Abort+重进同选项 |
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
  → Fire PreparePreview non-trap PushMapSpawn rows (Idle visuals; no AI)
  → Edit BattleFormation (shared) + optional path preview / slider
  → StartBattle (≥1 deployed)
       → destroy Prepare preview monsters
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
       → Shield ≤ 0 OR no living loyal → LevelFailure → UI-017 defeat → TitleMenu / Restart
       → Boss from BossPoint killed → Ended → credit Exp → UI-017 → UI-018 → LevelSelect
```

**待实现优先级（规则库；非 Demo §3.8）**

| 优先级 | 内容 |
|--------|------|
| P0 | 地图标记契约、配置表、Stage 接线、目标点占领、刷怪/陷阱、BOSS 通关、护盾失败（已落地 PM-01～05/07）；战斗结算/奖励 UI（UI-017/018；含阵亡与失败双按钮） |
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
| WarriorCombat | Same §3.12 (EngageZone / AttackMode Melee\|Ranged / hit scheme D / SkillCast); **D-069 PushMap Skill_03 burst + Skill_01 block + Skill_02 Comfort**; **D-073 Skill_04–12 EffectKind pipeline** |
| Not reused | Defend countdown spawns (`WaveSpawnConfig` / `SpawnRemainingSeconds`); clear-all victory (all rows fired + all killed) |
| Presentation camera | Same orthographic top-down as Defend (`Euler(90,0,0)`); Combat must enable a dedicated battle camera — must not fall back to scene perspective Main Camera (see [SPEC_04 §6](SPEC_04_Technical.md)) |
| Camera follow | Combat follow only (Prepare keeps FormationCamera; ortho Size ← **`PushMapPrepareOrthoSize`** (sample `4.5`; **not** `max(half)−CameraOrthoSizeMargin`)). **Prepare path preview (optional, off by default):** PushMapPrepare only; `FormationEditorRoot_Mode2` stacks Quick Preview and `CameraPathSlider` above `StartBattleButton`. Slider `value∈[0,1]` ↔ `CameraFollowPath` arc-length `s` (left=`WP_Start`, right=`WP_End`, uniform along polyline); drag → immediate FormationCamera world-XZ Snap; drag interrupts an in-flight preview. Entering Prepare keeps map-center framing (**no** auto sweep from `WP_End`); slider init = projected `s` of current pose (else 0). Quick Preview: Snap to author waypoint **end** (`WP_End`) → move **reverse** along baked polyline to **start** (`WP_Start`); constant world-XZ speed **`PushMapCameraIntroSpeed` ← `CombatConstantConfig`** (sample `1.5`); dwell **`PushMapCameraIntroWaypointDwellSeconds` ← same table** (sample `0.5`) at each author waypoint; syncs the slider while playing; ends at `WP_Start`. Missing path / bake failure / author waypoints &lt; 2 → hide or disable preview controls. **StartBattle:** entering `Combat` has **no** `IsCombatIntroActive` latch; after deploy, start clock + gameplay immediately → `CameraFollowMode`: `Auto` (default) = follow a look-at on map **`CameraFollowPath`**, **not** a soldier Transform. Progress `s∈[0,1]` = **max** polyline projection of living **loyal** soldiers (`!IsRebel` and not `CombatDead`); lead death/rebel/inactive → `s` shrinks → `SmoothDamp` **retreats** (no Snap). No followable loyal → **freeze** last pose (not protagonist, not map center). Missing `CameraFollowPath` or empty bake → warn + **fallback** to sticky-follow closest loyal to **CurrentObjective**. Auto presentation: ignore small target motion inside world-XZ circular deadzone **`CameraFollowDeadzone` ← same table** (sample `0.15`) (camera holds); outside → XZ `SmoothDamp` with **`CameraFollowSmoothTime` ← same table** (sample `0.25`) (Y/rotation unchanged); `EnterAuto` / StartBattle enable → **immediate Snap** to the **current** rail point (or fallback soldier) XZ (clear damp velocity); progress retreat does **not** Snap. `Manual` = LMB drag pans camera XZ; bottom-center `ResumeFollow` **Manual-only**, click → `Auto` and hide. Height **`CameraHeightY` ← same table**; StartBattle default `PushMapCameraOrthoSize` ← same table (sample `2`); Combat scroll-wheel zooms Size clamped **`[CameraOrthoSizeMin, CameraOrthoSizeMax]`** (sample `[0.5, 20]`; forward zoom-in smaller / back zoom-out larger); zoom does not switch follow mode; ResumeFollow does not reset Size |

**PushMapPhase**

| Phase | Notes |
|-------|-------|
| `Prepare` | Load map + formation; editable formation; StartBattle; optional path preview (Quick Preview + slider, off by default); **StartBattle-eligible spawn preview** (same non-trap rows, Idle, no AI/damage); no manufacture |
| `Combat` | After deploy, gameplay starts immediately (no StartBattle camera-intro latch); objective push; spawn/trap; AggroMode; Shield + LOC running |
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
| Advance | **Shared current objective** for all loyal soldiers; movement via **FlowField** (§3.12 MassCombatPathing Approach B); may interrupt for EngageZone enemies (`AttackSlot`); if none, keep sampling field toward current objective. **§3.18 (D-084 / D-093 / D-094):** grouped members use **that group's virtual center** on FlowField and `GoalKind=FormationSlot`, and do not switch to AttackSlot when engaging; non-members / dissolved still use the sentence above |
| Advance vs Capture | Capture depends only on **arrive**; living monsters in zone do **not** block Capture and do **not** alone pause advance (engage → AttackSlot below) |
| Demo engage pause | Until full §3.12 WarriorCombat is wired: loyal soldiers **pause advance** when center distance to any living monster ≤ `max(AttackRange, monsterBodyRadius+soldierBodyRadius)` (stop following field / clear local velocity; do not rely on full RVO); resume FlowField sampling when clear. Formal Engage → `GoalKind=AttackSlot`; hits still deferred to full WarriorCombat |
| FlowField rebuild | On `CurrentObjective` change, StartBattle bake (incl. AirWall non-walkable), or walkable change → rebuild field toward new goal; same-goal units share one field |
| Arrive = Capture | **Any** loyal soldier entering current `CaptureZone` → **immediate Capture**; not all need arrive; **no** timer; **no** “no monsters in zone” extra condition. **Move:** once inside the zone, **stop** sampling FlowField toward the goal cell center (avoids pile-up on the zero-vector cell); inside zone use LocalDetour **soft separation** for spacing; outside keep following the field; if SampleDir≈0 while still outside, fall back to steer toward the goal |
| On Capture | Mark captured; **stop future spawns** for linked points; **keep** living spawned monsters; grant configured loot + dungeon unlock hook; advance current objective; **if none remains and the map has a `BossPoint` → shared advance goal redirects to the BossPoint (v0.74.10 Boss guidance, see "Demo Boss guidance edge" below)**; without a `BossPoint` soldiers hold position / guard nearby until Boss clear or failure |
| Capture loot | Reward items from config; each item resolves through `ItemCatalogConfig`, then dispatches to warehouse / Spirit / MagicBook / protagonist gear; **no** stage Exp |
| Dungeon unlock | Write `DungeonUnlockIds` to save-slot set hook; **dungeon gameplay TBD** |
| Rules ownership | Objective chain + capture check/events live in **rules layer** (`PushMapSessionService`); map `ObjectivePoint`/`CaptureZone` are authoring markers only, no runtime self-ticking; presentation reports each frame whether any loyal is in the current zone |
| Demo arrive edge | Presentation scans loyal `PushMapAdvanceView`: `!IsRebel && CaptureZone.ContainsXZ` → `TickCapture(true)`; Rebels do not trigger Capture; `CaptureSeconds` config column may load but is **ignored by rules** |
| Demo spawn AI edge | PM-05 monster AI uses Defend default-chase semantics (nearest loyal soldier / protagonist; attack in `AttackRange`; Shield hit on protagonist); AggroMode four-state deferred to **PM-06** (see "Demo AggroMode edge" below); Boss-clear settlement see "Demo Boss-clear edge" |
| Demo AggroMode edge | PM-06 four-state: active stances (`ActiveChase`/`StationaryActive`) detect via `AlertRadius` (alongside `AttackRange`) and **only** on loyal soldiers; passive stances (`PassiveChase`/`StationaryPassive`) must be attacked first → provoke prefers a real soldier **HitConfirm** on that monster (PM-12); may keep first loyal entry into `AttackRange` as fallback so a ranged miss cannot forever leave the passive idle; stationary stances (`Stationary*`) never move, attack only inside `AttackRange`. Skill casts / dungeon gameplay body **not** done |
| Demo Boss-clear edge | PM-07: kill Boss from `IsBoss` row at `BossPoint` → `Ended` → credit `StageExpReward` (`AddExperience`); **do not** immediately `TryAdvanceStage`. `Shield≤0` **or** registered warriors≥1 with no living loyal (`!IsRebel && !CombatDead`) → LevelFailure with **no** stage Exp. Sync Rebel into rules `IsRebel`. **Kill contract (from PM-12):** monster `RemainingHp≤0` → View `NotifyKilled` (event carries `outgoingDamage`; knockback: SPEC_04 §15.5); Boss also `TryNotifyBossKilled`. Capture grants `CaptureLoot` (**no** Exp), resolves it through `ItemCatalogConfig`, accumulates display ledger, and writes `DungeonUnlockIds`; Boss-clear also writes unlocks. Missing `BossPoint` with `IsBoss` → warn |
| Demo battle settlement / reward UI | **UI-017 / UI-018 (Approach A extended):** always show UI-017 on win/lose. **Victory:** title Victory; time `mm:ss`, monsters killed, **loyal casualty total**, four BaseClass casualty rows (icons+counts); Continue → reward popup (already-credited Exp + CaptureLoot only; no extra grants) → end Level + `LevelSelectPanel`. **Defeat:** title Defeat; casualty total; Return to Title → `AbortLevel` + TitleMenu (UI-027); Restart → `AbortLevel` then immediately re-enter same `LevelId`+`OptionId` (Prepare; keep save slot; PermanentDeath not revived). Casualty stats owned by Session (`BattleCasualtyStats`). Defend counterpart **not** done |
| Demo soldier attack / WarriorCombat edge | **PM-12 (Approach B):** PushMap loyal WarriorCombat aligns with §3.12 scheme D — `AttackMode=Melee`: `AttackWindup` end → `HitConfirm` (target alive + in `AttackRange`) → monster `HP -= NormalAttackPower`; `AttackMode=Ranged`: spawn projectile (reuse Defend `ProjectileView`) → soft-collision hit settles / timeout miss does not. `NormalAttackPower` / `AttackSpeed` / windup / projectile params from StartBattle registry (`WarriorCombatMath` + `ClassConfig`, mirrored from Defend). Keep FlowField / AttackSlot / sticky engage; **remove** the old anim-only `PushMapAttackAnimSeconds` loop with no settlement. Monsters may still skip Animator this slice (§15.5) |
| Demo engage detect (AlertRadius) | **v0.82.55 Approach C:** PushMap loyal engage radius = `max(weapon reach, that monster's AlertRadius)` (weapon reach in §3.12). Melee `AttackRange` 0.35 yields ~0.86 weapon reach; three `Class_BaseWarrior` vs `Monster_12` (`AlertRadius=4`) all convert to `AttackSlot`. **v0.82.57:** while chasing, keep closing if not in range (inward close point / closer slot); hold and swing once in range; hit distance is XZ. Still **not** map-wide EngageZone. |
| Demo DamagePopup edge | **DamagePopup (PM-12/13):** after rules confirm a hit, show `-damage` above the **hit target** (value matches settled damage). Enemy monsters: red; loyal soldiers: white; font size **12** for both. Over **0.5s**, world `position.z` lerps relative start **+0→+0.5**, then despawn (no sustained Y rise). Protagonist Shield hits **do not** require a popup. Defend mode out of scope for this request |
| Demo HitFlash edge | **HitFlash (PM-12/13):** same successful-hit event as DamagePopup. Temporarily tint target subtree Renderers (MaterialPropertyBlock; do not permanently mutate shared materials). Monster bright **red**; soldier bright **white**. **2** pulses (immediate + one more), each **0.1s**, **no off gap** between them → ≈**0.2s** continuous tint then restore. Hit again mid-flash → restart from t=0. Miss / whiff → no flash. Defend mode out of scope |
| Demo AllyFootCircle edge | **AllyFootCircle:** Defend/PushMap Combat **loyal living** soldiers: green-stroke foot circle + black fill α=**160/255**; radius=`BodyRadius`; localPos `(0,-0.05,-0.2)`; rotation X=**-30**; Order In Layer=`50`; follows movement; hide on Rebel/CombatDead; see [SPEC_04 §9.7](SPEC_04_Technical.md) |
| Demo camera fog edge | **CameraFog (v0.83.05):** PushMap shows `DigFogCanvas` **only in `Combat`**; hide for `Prepare`/`Ended` and while Meta overlays open; driven by global `CameraFogService`; see §3.10 |
| Demo CombatSkillIcon edge | **CombatSkillIcon (UI-025 / D-071 / Approach A):** PushMap only. Overhead 35×35 holds 0.6s then world +Z rise 0.3s fade; stack screen-right 4px. `Skill_03` commit and `Skill_01` block success popup overhead; `Skill_02` full-HP persist 20×20 at feet plus one overhead on activate, hide when damaged. `CombatDead` clears icons immediately. Rules events `SkillIconPopup` / `SkillPersistChanged`. Defend **out of scope** |
| Demo CombatIndicator edge | **CombatIndicator (UI-033 / D-089 / Approach A):** PushMap + SearchExtract **`Combat` only**. ScreenSpaceOverlay, ref 1920×1080; root anchor top-center `(0.5,1)`, `anchoredPosition.y=-10`; root `localScale=0.75`; `raycastTarget=false`. Center bg `HPPK_UI_1` (`CenterBg`): **child** left-blue / right-red = **current alive counts** (exclude 0.5s dead linger slots; Legacy Text BestFit font size 26–42). **Enemy unknown at open:** while this battle has never seen `EnemyAliveCount>0` and current is 0 → show **`?`**; once revealed, later zeros show as `0`; latch clears on `ResetBattleState`. Ally always numeric. Left ally one slot/soldier `HPPK_UI_2` (right-align toward center); right enemy one slot/monster `HPPK_UI_3` (left-align toward center). Silhouettes: `ClassConfig.SilhouetteIconAssetId` / `MonsterConfig.SilhouetteIconAssetId` → `Resources/UI/Icons/{Id}` (missing → empty frame, slot still shown). **Stable sort:** allies `ClassLevel` desc → same-level class-table `TableOrder` asc → same ClassId StartBattle register order; enemies `MonsterType` desc → same-type monster-table `TableOrder` asc → same MonsterId spawn register order. **HP% tint (white silhouette):** `p≥66%` green; `33%<p<66%` orange; `0<p≤33%` red; permanent dead (HP=0 and not revivable) → gray + center `HPPK_UI_4`, remove after **0.5s** and re-align. **Revivable counts as alive:** `RevivePhase≠None` or `RevivesRemaining>0` never enter dead-slot path; center count still includes them. Rebels excluded from both sides. One row exceeding half-screen usable width → **truncate** (do not show rows 2…N; center full alive counts still update). View polls every **0.2s**; death linger only in View memory; no combat damage/revive hot-path events. Hide in Prepare / Ended / UI-017 settlement. Defend **out of scope** |
| Demo OffScreenSpawnHint edge | **OffScreenSpawnHint (UI-034 / D-090 / Approach A):** PushMap + SearchExtract **Combat real spawns only**. After each spawn request, test **`basePos` (SpawnPoint / `ResolveSpawnPosition`)** with combat camera `WorldToViewportPoint`: inside `[0,1]²` → no hint; outside → clamp to nearest screen edge (margin `OffScreenSpawnHintEdgeMarginPx`) and show `EnemyAttack_1` (`Resources/UI/Icons/EnemyAttack_1`; missing → empty frame still shown; no rotation; `localScale=OffScreenSpawnHintDisplayScale` default **1.3**). Intro: `OffScreenSpawnHintIntroBlinkSeconds` (default **0.4**) with `OffScreenSpawnHintIntroBlinkCount` (default **2**) red blinks (white↔red, alpha=1), then solid hold `OffScreenSpawnHintHoldSeconds` (default **2**). Multiple groups may each show one. PushMap **skips `PreparePreview`**; clear active hints in Prepare / Ended / UI-017 / UI-032. Defend **none**. Constants: [SPEC_04 §9.20b](SPEC_04_Technical.md). issues `.scratch/offscreen-spawn-hint/` |
| Demo engage stickiness edge | v0.74.10: engage target selection carries **sticky hysteresis** — while the claimed monster is alive and still inside its engage detect radius, switch claims only if a new candidate's center distance is closer by more than **`EngageStickHysteresisMargin` (default 0.15 world units, constant)**; otherwise keep the current claim. Rationale: dense packs + SoftCollision micro-pushes flip-flop the strictly-nearest target per frame and destabilize AttackSlot / windup. Stickiness changes neither detect radius nor slot legality; target death / leaving range still switches normally (**no longer** tied to retired `DemoKillEngageSeconds`). **v0.84.25:** chase-stuck force retarget (§3.12) bypasses stickiness and excludes the claimed id for that select pass; sticky path unchanged otherwise |
| Demo chase-stuck retarget edge | **v0.84.25 Approach A; v0.84.26 regression fix:** shared by PushMap / SearchExtract / Defend loyals. Out-of-range sliding-window stuck → exclude current/blacklist, skip stickiness, claim next-nearest **with a free slot only**; `TryClaim` secures new before Release; no free slot → keep claim; cooldown vs thrash. Constants in [SPEC_04 §9.20b](SPEC_04_Technical.md). LocalDetour unchanged; StuckHold stays independent |
| Demo Boss guidance edge | v0.74.10: when the objective chain is exhausted (`CurrentObjectiveOrder=0`, all captured) and the map has a `BossPoint` → rebuild the `FlowField` toward the BossPoint world XZ (fired once by `CurrentObjectiveChanged(0)`; maps with no objectives rebuild right after StartBattle); `ObjectiveArriveRadius` tightens to **`BossAdvanceArriveRadius` (default 0.35, constant)** so soldiers close on the Boss point; **v0.82.55:** active-stance bosses also pull nearby allies into `AttackSlot` via `AlertRadius` (`Monster_12`=4); for `Stationary*` bosses this guidance is still the only approach means; no `BossPoint` → keep the original "hold position / guard nearby" semantics; camera follow still uses `CameraFollowPath` max projection (does not follow the soldier) |
| Demo soldier-hit edge | **PM-13:** monsters subtract soldier `RemainingHp` by `MonsterConfig.AttackPower` (no armor); holding `Skill_01` may zero this hit’s damage by level chance before subtract (still a hit; still white popup/HitFlash). `HP≤0` → `CombatDead` (stop acting; gems / PermanentDeath follow §3.12 Demo-min). Protagonist hits still `Shield -= 1` (ignore AttackPower; no protagonist popup/flash required) |
|| Demo 8-dir facing stabilization edge | **v0.83.31 Approach B (Defend+PushMap both factions):** move 8-dir follows MassMove **intent** `LastDesired` (FlowField / slot line), **not** LocalDetour steer / soft-collision impulse; `IsRun` still from steer. On attack start, snap **once** toward the target then freeze until Attack1 ends (unlock if move interrupts). Idle / in-range / StuckHold do **not** retarget facing every frame. Existing sector hysteresis + min dwell still apply to `SetFacing`. Attack checks / slots / pathing unchanged. **v0.83.97:** Zombie Pack `MonsterModel_02/04/05/07/08` `{Attack*}_{dir}` clips must bind visual rows. See SPEC_04 §15.5 |
| Demo monster death-revive edge | **PushMap + SearchExtract (D-074; Approach B shared `IMonsterDeathSkillHost`):** monsters with `MonsterSelfReviveOnDeath` in `MonsterConfig.Skills` (§9.21c) at HP≤0 → **MonsterCombatDead** (presentation `NotifyKilled(fakeDeathCorpse=true)` + **corpse projectile parabolic arc** (D-083; smash other monsters when threshold met); **no** kill / BOSS / Loot); after arc+latch wait `DelaySeconds` → **before** reverse-play pick target via `TargetSelect` and `ForceSetFacing` (still writes 8-dir `DirIndex` while corpse `_dead`) → reverse **prefer** Creator `Die2` Trigger clip (else latched `Die` clip); that facing kept into Idle; post-revive `Attack1_*` matches target 8-dir → HP=`MaxHp×ReviveHpRatio` + invincible `InvincibleSeconds`; **first revive** if EffectParams has `AlertRadius` → instance detect radius becomes that value (monster TargetSelect + soldier engage both read the instance; **do not** mutate the table row; later revives do not change it); **fake-death latch `RGB×0.7` darken** (half depth of true death 0.4), held through Delay/reverse/invincible until invincible ends; `MaxReviveCount` exhausted → true death `MonsterKilled`, latch **`RGB×0.4`**. **SearchExtract point-clear** (countdown success rules wipe) **skips** `TryInterceptDeath`; already fake-dead instances cancel revive and do not re-knockback. Defend **unwired** |

**Spawning (not WaveSpawn countdown)**

| Rule | Notes |
|------|-------|
| Table | `PushMapSpawnConfig` by `GameplayConfigId` + `SpawnPointId`; multi-rows per point OK |
| Initial facing | Row field `InitialFacing` ([SPEC_04 §9.23](SPEC_04_Technical.md)): Idle 8-dir on spawn. `0`=each monster on the row rolls `1~8`; `1~8`=fixed compass facing (1=up … 8=up-left). Presentation maps to Animator `DirIndex` (§15.5); shared by Prepare preview / StartBattle / trap. Default `5` (down) |
| Non-trap | At StartBattle: if no trap bind and linked objective uncaptured → spawn rows |
| Prepare StartBattle-spawn preview | On entering `Prepare`: instantiate the same non-trap-eligible rows (incl. Boss) as Idle visuals; **no** AI / attack / capture / trap; **no** `PendingBoss` / Session HP register. Trap-bound rows are **not** previewed. On StartBattle → **destroy** preview instances → Bake NavMesh → deploy → formal `FireStartBattleSpawns` |
| Trap | Bound `TrapZoneId`: first loyal enter while objective uncaptured → spawn once per point this battle (re-enter no re-spawn unless config TBD) |
| Capture stop | Captured linked objective → no new spawns; living remain |
| Footprint spread | Same-point `SpawnCount>1` or nearby living monsters: stagger on walkable NavMesh by each `MonsterConfig.BodyRadius` so XZ footprint circles do not overlap (may shrink ring on SamplePosition failure; final fallback to sampled base); trap spawns likewise avoid existing living monsters. **Demo tighten (v0.73.9):** `SamplePosition` is local only (≈`max(0.75, BodyRadius×2.5)`); accept hit only if XZ distance from spawn `basePos` ≤ leash (≈ current ring/spiral radius + radius slack; absolute cap ≈`max(3, BodyRadius×10)`); over-leash hits fail. When packed, **prefer overlap at base** — do not snap across AirWalls onto empty outer-diamond NavMesh |
| Ongoing avoidance | **Moving** monsters in Combat: spawn spread still uses `BodyRadius`; `NavMeshAgent.radius = min(BodyRadius, max(0.05, AttackRange − BodyAppearanceConfig.DefaultBodyRadius(0.1) − 0.05))` so centers can enter `AttackRange` (RVO radius must not exceed attack reach); `Stationary*` do not relocate; PushMap monsters Demo: `NavMeshAgent.height=0.1` (`PushMapMonsterAgentView`); **loyal soldiers:** `NavMeshAgent.radius` from `BodyAppearanceConfig.BodyRadius` (default `0.1`), `height=0.1` |
| Forbidden | PushMap does **not** use `RemainingCombatSeconds` / `WaveSpawnConfig` activation |
| Demo impl boundary | PM-05 (spawn/trap): monsters use Defend default-chase AI (not four-state, §9.23 contract); AggroMode four-state and Boss-clear settlement **deferred** (PM-06/07); spawn eligibility/trigger state in `PushMapSessionService`; position resolved by View via `SpawnPointId` / `BossPoint`; **Prepare**: `FirePreparePreviewSpawns` (`PushMapSpawnTrigger.PreparePreview`) Idle non-trap rows; **StartBattle order**: destroy preview → Bake NavMesh → deploy loyal soldiers → `FireStartBattleSpawns`; footprint spread + agent radius → **PM-10** |
| Demo walkable edge | `DigMapBounds` (and `WalkSurface` / `EngageZone`) must cover sample objectives / spawn points / BossPoint; runtime NavMesh bake uses `DigMapBounds`; markers outside the diamond leave advance/spawns unable to sit on NavMesh. Sample `PushMap_Demo_01` AirWalls must keep objectives walkable (see Demo AirWall edge authoring hard rule) |

**Boss & win/lose**

| Rule | Notes |
|------|-------|
| Clear | Kill Boss from **BossPoint** (`IsBoss=1`) → Ended → credit `StageExpReward` → UI-017 (incl. casualties) → UI-018 → end Level + LevelSelectPanel (Demo; no auto next stage) |
| Fail | `Shield ≤ 0` **or** no living loyal soldiers → LevelFailure; **no** stage Exp → UI-017 (defeat layout) → Return to Title Abort+TitleMenu / Restart Abort+re-enter same option |
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
  → Shield≤0 OR no living loyal → LevelFailure → UI-017 defeat → TitleMenu / Restart
  → Boss kill → Ended + Exp → UI-017 → UI-018 → LevelSelect
```

---

---

## 3.15 自动制造（AutoManufacture；Mode2）

### 简体中文

**状态：规则库已关闭（本轮）；Demo 实现见 §3.8 D-050～D-055 / D-058 / D-068 / D-072 / `.scratch/mode2-auto-manufacture/issues/`；其余魔法书效果行与弹窗装入另专题**

当关卡当前阶段 `玩法类型 = AutoManufacture` 时进入本阶段（Mode2 样例运作表：Dig → **本阶段** → UpgradeManufacture）。配置表见 [SPEC_04 §9.9b / §9.12 / §9.24](SPEC_04_Technical.md)。Mode1 **不**进入本阶段。

**阶段边界**

| 规则 | 说明 |
|------|------|
| 进入 | 上一阶段 Dig 经 DigStageSummary 玩家确认后，关卡驱动进入本阶段 |
| 结束 | 算法跑完（造到不能再造 + 自动上阵）后播 **AutoManufacturePresentation**（UI-016；本批 0 兵则跳过 StepA/B 尸骸雨/复活/套书，仍挂 AM 壳层约 1s 显示 Tips），再 **自动**交还 §3.9；**无**玩家确认 |
| 结算 | **无**独立阶段结算 |
| 材料不足 | 进入时仓库连 1 套最低配方都没有 → 0 兵入临时仓库；仍执行「清空布阵」再进下一阶段 |
| 0 兵 Tips | 本批造兵数 = 0（含最低配方不足、无主要手等停造且未造出任何兵）→ 打开 AM 壳层（背景/Dim/书槽），屏幕中上部 Tips「无士兵可制造」，停留约 **1 秒**；**不**播尸骸雨/复活/套书；**不**阻塞玩家确认（无确认钮）；然后自动交还 |
| Demo 诊断日志 | `RunBatch` 结束（含 0 兵）打 Console：`stopReason`、按槽 Head/Torso/ArmPrimary/ArmSecondary/Leg 计数、各槽 `BodyLevel` 分布、每条 `BodyPartId` 堆叠；选料时打锚点主要手 `BodyPartId`/`BodyLevel`。**不**改变选料规则 |
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
| 主要手 | `BodySlot=Arm` 且 `IsPrimaryHand=1`；按 `BodyLevel` **降序**依次尝试作锚点（同级按 `BodyPartId`）。当前锚点无法凑齐次要手/头/躯/腿（近似品质）或职业解析失败 → **跳过该主要手（不消耗）** 并打日志，试下一档；能成套时用当时尝试中最高的一档。无可用主要手 → **停造** |
| 次要手 | `IsPrimaryHand=0` 的 Arm；先滤近似品质；优先 `ClassRestrict` 与主要手有交集；同级随机；配不到则 **跳过当前主要手**（不是整批立刻停造） |
| 其余部位 | 顺序固定：**头 → 躯干 → 腿1 → 腿2**；锚点 = 当前尝试主要手的 `BodyLevel` / `BodyPrimaryStat` / `RaceId`；优先级：近似品质 → `BodyPrimaryStat` 相同 → `RaceId` 相同 → 在满足近似品质的剩余中随机；满足近似但无主属性相同则在近似集内随机；配不到则跳过当前主要手 |
| 主要手 ClassRestrict 空 | 视为该件配置错误：打 Error 后 **跳过该主要手**（不阻断更低档可成套材料）；若全部主要手皆空/不可用 → **停造** |

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
| 槽位 | 主角默认 **6** 特殊装备槽（一书占 1 槽）；下标 **0→5 = 左→右**（UI-016 Step2 脉冲与 UI-023 展示同一顺序） |
| 唯一 | 同 `MagicBookId` 默认可叠装；`IsUnique=1` 不可再装第二本 |
| 概率型 | `IsProbabilistic=1` 标记该书为概率触发魔法。`ForceClass` 的 `Chance` **真正 roll**；其它 Token 本轮仍不读本列。`0`=无概率 |
| 触发 | **不在造兵循环内套书。** 本批 flush→池且造兵数>0 后，UI-016 Step2：聚焦该兵 → 6 槽自左→右伸缩（空槽照跳、无效果）；**仅当前缩放到峰值的那一槽**对聚焦兵执行其 `EffectPayload`（须 `EffectPhase` 含 `SoldierManufacture`）。表现层只回调槽索引，规则在钩子内解析 Token |
| 「还原」 | `MagicBook_Restore`：`EffectPayload=RaceWeightPick`、`EffectParams` 空；**该书槽脉冲时**用实例 `SourceItemIds` 对应部位 `RaceId` 权重 **1** 重抽种族并重载 `RaceAdjustCoeff`；`IsUnique=1` |
| 「战士强化」 | `MagicBook_WarriorEnhance`：`EffectPayload=StatMul`、`EffectParams=Stat=Primary\|Mul=1.15\|ClassId=Class_BaseWarrior,Class_Warrior`；`IsUnique=0` 可叠；仅 Mode2；种族不过滤；职业须为枯骨战士或战士（不含近卫/狂战等）；见下 `StatMul` / `Primary` |
| 「士兵技能升级」 | `MagicBook_SoldierSkillLevel`：`EffectPayload=SoldierSkillLevelAdd`、`EffectParams=SkillId=Skill_01\|Delta=1`；`IsUnique=0` 可叠；**该书槽脉冲时**立刻升已有技能（无二次扫描）；见下 |
| 「职业进阶」 | 四本 `IsUnique=1` `IsProbabilistic=1` `EffectPayload=ForceClass` `Chance=0.25`：`MagicBook_WarriorAdvance`（`RequireClassId=Class_BaseWarrior`→`ClassId=Class_Warrior`）等。精确 ClassId；仅 Mode2 |
| 指定种族 | `ForceRace`：必填 `RaceId`；**该书槽脉冲时**改写种族（未实现 Token 仍空 apply） |
| 指定职业 | `ForceClass`：必填 `ClassId`；可选 `RequireClassId` / `Chance`（语义同前）。**该书槽脉冲时**判定；命中则改写 `ClassId`/`AttackMode`，并 **Clear 后重授** 新职业 `DefaultSkillIds`@Lv1。若「技能升级」在「进阶」左边，进阶会清掉已加等级 |
| 属性倍率 | `StatMul`：语义同前；`BodySum` 用实例 `SourceItemIds` 反查躯体 `StatBonus`；**该书槽脉冲时**写入 Base |
| 属性加算 / 品质偏移 | `StatAdd` / `QualityDelta`：登记语义同前；未实现则空 apply |
| 士兵技能升级 | `SoldierSkillLevelAdd`：必填 `SkillId`、`Delta`；**该书槽脉冲时**立刻 apply；仅当实例 **已有** 该 `SkillId` 时 `SkillLevel += Delta`（钳制 SkillConfig 最小/最大级）；无该技能则跳过（**不新授**）。**无**消耗经验升级 |
| 每槽后定稿 | apply 后立刻重算 StaticStat / MaxHP / 造型外观 / 命名；`RemainingHP=MaxHP`；**不得清空**已烘进的 `VisualStyleId` / `VisualModelScale`；士兵卡立刻刷新职业名与 `Lv.N`；Idle 仍在该兵 6 槽全部结束后揭示 |
| 特效外观 | 见下「6b. 特效外观定稿」；**不是** `EffectPayload` Token |
| Fallback | 演出未能启动或阶段 `Exit` 中断 → 对未处理槽按左→右瞬时 apply 剩余书，再 persist + 上阵 |
| 编码 | `EffectPayload` 须为 [SPEC_04 §9.24](SPEC_04_Technical.md) 已登记 Token；一书一 Token |
| 其它书 | 未登记 / 未实现 Token 仍空 apply + 警告日志 |
| 排序 | 任意两槽 `SpecialEquipSlotsService.TrySwap(indexA, indexB)`（含空槽=搬书）；越界/未绑档失败；成功立即 persist + `Changed` |
| 仓库 | **无**独立魔法书仓库；装入仍 Tools GM `TryEquip`（UI-019 / D-061） |
| UI | InSaveShell「魔法书」打开 UI-023（共享 `BookRow` + 拖拽排序 D-068 + 点槽删除 D-072）；不从弹窗装入。AM Step2 进行中：已脉冲槽不回滚；未脉冲槽读当前槽（空则跳过） |

**4b. 战斗魔法书（Combat 环节）**

| 规则 | 说明 |
|------|------|
| 触发 | Defend / PushMap **`StartBattle` 后**、`TryRegisterWarrior` 登记**每个上阵士兵**时；读取主角 6 槽（左→右），非单兵装备 |
| Token | `EffectPhase` 含 `Combat` 且 `EffectPayload=StatMul`；参数见 [SPEC_04 §9.24](SPEC_04_Technical.md) Combat `StatMul` 行 |
| 样例 | 8 本战斗属性书：`MagicBook_CombatMaxHpLow/High`、`CombatStrengthLow/High`、`CombatAgilityLow/High`、`CombatIntelligenceLow/High`；低阶 `Mul=1.15`、高阶 `Mul=1.30`；`IsUnique=1` `IsProbabilistic=0` |
| 叠乘 | 同维多书连乘（例生命初+高 = `1.15×1.30`）；不同维可共存 |
| 持久化 | **不**写入 `WarriorInstance.BaseStats`；战斗结束效果消失；**不**触发 VisualStyle / 体型步进 |
| 实现 | `CombatMagicBookStatMul.Aggregate` → `TryRegisterWarrior` 乘 BodyLife / StaticStat 后再派生 HP/Atk/ASPD/CD |

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

**6b. 特效外观定稿（`VisualStyle`；与造型 `AppearanceId` 独立）**

Mode2 Step2 **仅当该书 `EffectPayload` 真正命中**（skip / miss / 无效 Token **不**写入）才处理体型步进与 `MagicBookConfig.VisualStyleId`。闸门与 `WarriorVisualStyleBake.TryApply` 调用点一致（含 `SoldierSkillLevelAdd` 等级已被钳在上下限、数值未变仍算命中）。

**命中附带体型步进（与 VisualStyle 通道并存；空 `VisualStyleId` 也执行）**

| 规则 | 说明 |
|------|------|
| 触发 | Token **真正命中**（与下材质/放大通道同一闸门） |
| 步进 | `VisualModelScale *= WarriorVisualModelScalePerHit`（← `CombatConstantConfig`，样例 **1.15**；`≤0` 视为 1.15） |
| 再叠 | 若该书为 `Style_ScaleModel`，同次再 `*= VisualIntensityAdd`（见下） |
| 上限 | 每次 apply **末尾** `VisualModelScale = min(·, WarriorVisualModelScaleMax)`（样例 **3**；缺键兜底同值） |
| 下游 | 世界 `Visual.localScale=(k,k,k)`；`BodyRadius`/`AttackRange` 均 ×k（布阵/Defend/PushMap 复用存档字段） |

特效另分 **两条独立 VisualStyle 通道**（一书一 `VisualStyleId`，可被不同书分别命中后共存）：

**材质通道（AllIn1；每兵一套赢家）**

| 规则 | 说明 |
|------|------|
| 空列 | `VisualStyleId` 空 → 该书无材质/放大通道特效；**仍**执行上表体型步进 |
| 放大 Id | `Style_ScaleModel` / 别名 `放大模型` → **不**走本通道（见下） |
| 覆盖 | 实例尚无材质 style，**或** 该书 `VisualPriority` **大于** 当前 `VisualPriority` → 换成该书 style，`VisualIntensity = VisualIntensityAdd`（列空缺省 1） |
| 同 Id 叠加 | 该书 style **等于** 当前材质赢家 → `VisualIntensity += VisualIntensityAdd` |
| 低优先 | 否则保留材质赢家（低优先书 **不**给赢家加强度） |
| 套用 | 保卫 / 推图 / 布阵场上预览 / 拖拽预览 / **UI-016 士兵卡 Camera+RT** Instantiate 后：Catalog 取材质赋 `sharedMaterial`（禁止 `new Material` / 运行时 `EnableKeyword`），MPB 写强度；缺 `AllIn1AtlasUvDriver` 则补 |

**放大通道（`Style_ScaleModel`；可与材质共存；叠在命中步进之后）**

| 规则 | 说明 |
|------|------|
| 识别 | `VisualStyleId` 为 `Style_ScaleModel` 或中文别名 `放大模型`；**不**参与材质优先级竞争、**不**改 `VisualStyleId`/`VisualPriority`/`VisualIntensity` |
| 系数 | `VisualIntensityAdd` 即再乘系数（例 `1.5`）；列空缺省 1；`≤0` 视为 1 |
| 叠加 | 在命中步进之后：`VisualModelScale *= VisualIntensityAdd`，再夹 Max |
| 表现 | 子节点 `Visual.localScale = (k,k,k)`（`App_0_00` Visual 预制为 `(1,1,1)`，根仍 `(1.2,1.2,1.2)`，世界视觉约 `1.2k`） |
| 碰撞 / 寻路 | 出场 `BodyRadius × k`（含 `NavMeshAgent.radius`、软碰撞、布阵螺旋占位） |
| 攻击距离 | 战斗状态 `AttackRange = ClassConfig.AttackRange × k`；`CombatReach` 仍为 `AttackRange + 双方 BodyRadius` |
| Mode1 / GM 添加士兵 | `VisualModelScale=1`（无书） |

共用：`RefinalizeInstance` 可改 `AppearanceId`，**不得**清空 `VisualStyle*` 或 `VisualModelScale`。UI-016 士兵卡揭示为世界 Instantiation + Camera/RT，**套用** AllIn1 与 `VisualModelScale`；布阵底栏缩略图本轮 **不**套 AllIn1、**不**缩放。

Demo 样例：`MagicBook_WarriorEnhance`→`Style_WarriorGlow` P20 Add1（命中仍 ×1.15）；四本 `*Advance`→`Style_AdvanceOutline` P30 Add1（仅 hit）；`MagicBook_SoldierSkillLevel`→`Style_SkillAberration` P10 Add1；`MagicBook_Restore` 默认可空（命中仍 ×1.15；手验可再填 `Style_ScaleModel` / `VisualIntensityAdd=1.5`）。

新增 Style 的材质 / Catalog / Excel Bake 步骤见 [SPEC_04 §15.2](SPEC_04_Technical.md)「新增 VisualStyle 预设」。放大模型 **不必**建 `.mat`。

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

**11. 自动制造演出（阶段表现；UI-016 / D-055；方案 A 尸骸雨+法阵复活 + 单槽节拍套书）**

规则层同步跑批（选料→无书造兵→flush→批次记录；**此时不上阵**）。本批造兵数 **>0** 时挂表现 Prefab：StepA 落下尸骸 → StepB 逐兵法阵闪红+谜底从法阵中心与套书并行上升变兵 → 全部播完再自动上阵 → 交还驱动；**0 兵**挂 AM 壳层约 1s + Tips「无士兵可制造」，**不**播 StepA/B、**不**套书，交还后 **不**自动开布阵。

| 步骤 | 规则 |
|------|------|
| StepA | **落下尸骸（整批一次）：** 件数 = 本批全部士兵 `SourceItemIds`（已消耗躯体；不含仓库余料）。每件落下时标记所属 `WarriorId`（供 StepB 吸入）。外观 = `BodyPartConfig.ArtAssetId`（源 `Assets/Art/UI/Dig/{ArtAssetId}.png`；运行时 `Resources/UI/Dig/{ArtAssetId}`；缺图按 `BodySlot` 回退同槽已有图，禁止空件）。Sprite 适配最长边 = `AutoMfgBodyMaxEdgePx`（样例 **49**）。从画面水平中心、屏幕外正上方落下；每 `AutoMfgBodyDropIntervalSeconds`（样例 **0.3**）随机落下 `AutoMfgBodyDropCountMin`～`Max`（样例 **3～5**）件，**总数不变**。重力 + 弱碰撞，可在 2D 维度堆叠；碰撞为**可旋转 OBB**（SAT，非 AABB）：边长 = Sprite 适配尺寸 × `AutoMfgColliderInset`（样例 **0.72**，小于外观以便斜插更紧）；落下随机转角 ±`AutoMfgSpawnAngleMaxDeg`（样例 **50**）并带角速度；**绝对 Z 转角夹紧** ±`AutoMfgBodyAngleMaxDeg`（样例 **270**，触限清角速度，禁止无限翻滚）；互撞/落地可保持斜角。对象池 + 落地 Sleep。堆地板 Y = `AutoMfgPileFloorYPx`（样例 **−300**，相对 `AmBodyRainLayer`）。堆静止后进入 StepB。法阵 `MagicCircle_1` 本地 Y=`AutoMfgMagicCircleYPx`（样例 **−50**；同 2D 演出层；**绘制层级低于躯体**）。 |
| StepB | **逐兵循环：** 若已有落地兵，先全体向左平移一个 pitch（短时插值样例 **0.2s**；中心距=`AutoMfgSoldierMaxEdgePx`×`AutoMfgSoldierVisualScale`×`AutoMfgSoldierPitchFactor`，样例 Factor **0.5** → **256**）。激活法阵持续快速闪红（`AutoMfgMagicCircleFlashHz`）→ 在法阵中心显示谜底 `UnknownSoldier_1`（脚底 pivot 对齐法阵 `anchoredPosition`；sizeDelta 最长边 `AutoMfgSoldierMaxEdgePx` 样例 **64**，`localScale`=`AutoMfgSoldierVisualScale` 样例 **8**）→ **与**上方 6 书框左→右依次伸缩**同时进行**：每一槽期间谜底上升全程的 **1/6**；**峰值**对该兵执行**仅该槽**魔法书（规则同现网）并谜底闪烁（空槽仍脉冲、仍占一拍、仍上升 1/6、仍闪烁、无效果）；闪烁时长 ≈ 该槽脉冲时长 × **0.4**。**上升期间**将该兵 StepA 堆中已标记 `WarriorId` 的躯体件（`SourceItemIds` 对应落下件）按 `AutoMfgBodyAbsorbStaggerSeconds`（样例 **0.06**）错开起飞，追谜底视觉中心（脚底 + 半身高×scale；目标随上升更新），单件飞入 `AutoMfgBodyAbsorbFlySeconds`（样例 **0.35**），飞行中 Image 按 `AutoMfgBodyAbsorbFlashHz`（样例 **8**）白↔红闪；到点从堆移除并消失（回池）；其余兵的件留堆。第 6 槽结束时脚底恰好在落地线 `AutoMfgSoldierLandYPx`（样例 **−410**）→ 变为该兵 `AppearanceId` 外观（Idle + VisualStyle，同尺寸）**定住**（**不再**重力下落）；脚下影 Demo 隐藏（`AutoMfgSoldierShadowAlpha` 样例 **0**；宽/高/Offset 键保留）；新兵水平居中。复活士兵与尸骸/法阵 **碰撞忽略**。每完成 **3** 兵速率 × `1.25^floor(completed/3)`（书脉冲、每拍上升、闪烁、变兵 hold、**躯体飞入 stagger/fly** **共用**同一 speed）。`AutoMfgReviveSpawnOffsetYPx` Demo **不再**作出生 Y（键保留）。 |
| StepC | 全部士兵 StepB 完成后 **先按最终 ClassId 自动上阵**，再进 `UpgradeManufacture` 并**自动打开**布阵编辑器 |

**备选士兵行（Legacy；默认隐藏）：** Prefab 内 `SoldierScroll` 传送带 + Camera+RT 揭示逻辑 **保留不删**；`AutoMfgUseLegacySoldierRow=1` 时切回旧 Step1/2（中央卡行+谜底揭示）。默认 **0** = 尸骸雨流程。

**2D 演出层（方案 A）：** Overlay Canvas 保留背景/Dim/BookRow；`AmBodyRainLayer`（1920×1080 Image；本地 Y=`AutoMfgBodyRainLayerYPx` 样例 **−340**）叠在 Dim 之上；像素重力 + **可转 OBB（SAT，碰撞体内缩）** 弱碰撞堆叠躯体（法阵/复活仍无躯体互撞）；法阵本地 Y=`AutoMfgMagicCircleYPx`（样例 **−50**）；复活件 **底部 pivot**。独立正交相机 RT 会被 Overlay 挡住。参数权威：`CombatConstantConfig` `AutoMfg*` 键（[SPEC_04 §9.20b](SPEC_04_Technical.md)）。

```
AutoManufacture stage
  → Clear BattleFormation
  → while warehouse can craft min recipe:
       pick parts; ClassId from hands; Base=Σ StatBonus; Race default (§3.11)
       Grant DefaultSkillIds@Lv1; finalize StaticStat/Appearance; flush → WarriorPool
  → Replace AutoManufactureBatchRecord
  → if crafted>0: UI-016 StepA body rain → StepB (circle flash + mystery rise parallel books + morph)
  → DeployBatch by final ClassId into FormationClassZone
  → Auto return → UpgradeManufacture (+ auto-open Formation if presentation ran)
```

**待实现优先级（规则已锁；编码另切片）**

| 优先级 | 内容 |
|--------|------|
| P0 | 表扩列 + AutoManufacture 阶段 + 选料/职业/属性循环 + 临时仓库 + 自动上阵 + Mode2 UM 差分 |
| P1 | 魔法书 6 槽存档 + 空钩子；制造记录弹窗（最近一批）；自动制造演出 UI-016 |
| P2 | 其余魔法书效果 / 弹窗装入 / 灵魂手动装配 |

### English

**Status: Rules library closed (this round); Demo impl §3.8 D-050–D-055 / D-058 / D-068 / D-072 / `.scratch/mode2-auto-manufacture/issues/`; remaining MagicBook effect rows and grant-from-popup later**

Entered when Level stage `GameplayType = AutoManufacture` (Mode2 sample LevelOperation: Dig → **this stage** → UpgradeManufacture). Config: [SPEC_04 §9.9b / §9.12 / §9.24](SPEC_04_Technical.md). Mode1 does **not** enter this stage.

**Stage boundary**

| Rule | Notes |
|------|-------|
| Enter | After Dig DigStageSummary player confirm |
| End | When algo finishes (craft until cannot + auto-deploy) → play **AutoManufacturePresentation** (UI-016; if batch 0 skip StepA/B rain/revive/books but still mount AM shell ~1s with Tips) → **automatic** return to §3.9; **no** player confirm |
| Settlement | **No** independent stage settlement |
| Insufficient stock | If warehouse cannot craft even one min recipe → 0 soldiers in temp warehouse; still **clear formation** then advance |
| Zero-craft Tips | Batch crafted count = 0 (incl. min-recipe short / no PrimaryHand stop with zero crafts) → open AM shell (background/Dim/books), upper-center Tips「无士兵可制造」for ~**1s**; **no** rain/revive/book apply; then auto-advance |
| Demo diagnostics | End of `RunBatch` (incl. 0 craft) logs `stopReason`, slot counts Head/Torso/ArmPrimary/ArmSecondary/Leg, per-slot BodyLevel histogram, each `BodyPartId` stack, and the anchor PrimaryHand id/level. **Does not** change pick rules |
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
| PrimaryHand | `Arm` + `IsPrimaryHand=1`; try as anchor in **BodyLevel descending** order (tie: `BodyPartId`). If this anchor cannot complete Secondary/Head/Torso/Legs (approx) or class resolve fails → **skip that PrimaryHand (do not consume)** and try the next; a successful kit still uses the highest completable. None usable → **stop crafting** |
| SecondaryHand | `IsPrimaryHand=0` Arms; filter approx; prefer ClassRestrict overlap with Primary; random among ties; miss → **skip current PrimaryHand** (do not abort the whole batch yet) |
| Remaining | Fixed order **Head → Torso → Leg1 → Leg2**; anchor = current PrimaryHand BodyLevel / BodyPrimaryStat / RaceId; priority: approx → same BodyPrimaryStat → same RaceId → random among approx; if approx but no BodyPrimaryStat match → random in approx set; miss → skip current PrimaryHand |
| Empty Primary ClassRestrict | Config error for that part: log Error then **skip that PrimaryHand** (do not block lower completable kits); if every PrimaryHand is empty/unusable → **stop crafting** |

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
| Slots | Default **6** special slots (one book per slot); index **0→5 = left→right** (UI-016 Step2 pulse and UI-023 display share this order) |
| Unique | Same MagicBookId stackable unless `IsUnique=1` |
| Probabilistic | `IsProbabilistic=1` marks chance-trigger. `ForceClass` `Chance` **rolls**; other tokens ignore this column this round |
| Trigger | **Not during the craft loop.** After flush→pool with crafted>0, UI-016 Step2: focus soldier → 6 slots pulse left→right (empty slots pulse, no effect); **only the slot at peak scale** applies its `EffectPayload` (`EffectPhase` must include `SoldierManufacture`). View callbacks slot index only; Core parses tokens |
| Restore | `MagicBook_Restore`: `RaceWeightPick`; **on that slot's pulse**, weight-1 re-pick race from instance `SourceItemIds` part RaceIds and reload `RaceAdjustCoeff`; `IsUnique=1` |
| Warrior Enhance | `MagicBook_WarriorEnhance`: `StatMul` / `Stat=Primary` / `ClassId=Class_BaseWarrior,Class_Warrior`; stackable; Mode2 only |
| Soldier skill level | `MagicBook_SoldierSkillLevel`: `SoldierSkillLevelAdd`; **on that slot's pulse** immediately; no second pass |
| Class advance | Four `ForceClass` books with `Chance=0.25`; `WarriorAdvance` `RequireClassId=Class_BaseWarrior`→`Class_Warrior`; Mode2 only |
| Force race | `ForceRace`: required `RaceId`; apply **on that slot's pulse** (unimplemented → empty apply) |
| Force class | `ForceClass`: on pulse; on hit rewrite class and **Clear then re-grant** new `DefaultSkillIds`@Lv1. Skill-level books left of advance are wiped by promotion |
| Stat mul | `StatMul`: `BodySum` from `SourceItemIds`; apply on that slot's pulse |
| Stat add / Quality | Registered; unimplemented → empty apply |
| Soldier skill level | `SoldierSkillLevelAdd`: on pulse; only if instance already has `SkillId`; clamp to SkillConfig range; **no** new grant |
| After each slot | Refinalize StaticStat / MaxHP / body appearance / name; `RemainingHP=MaxHP`; **must not** clear baked `VisualStyleId` / `VisualModelScale`; card refreshes class name / `Lv.N`; Idle still after that soldier's 6 slots |
| Visual style | See **6b** below; **not** an `EffectPayload` token |
| Fallback | Presentation fail or stage Exit → instant `ApplyRemaining` left→right, then persist + deploy |
| Encoding | Registered `EffectPayload` tokens ([SPEC_04 §9.24](SPEC_04_Technical.md)) |
| Other books | Unregistered / unimplemented → empty apply + warn |
| Reorder | Any two slots `SpecialEquipSlotsService.TrySwap(indexA, indexB)` (empty slot = move book); out-of-range / unbound save fails; success persists immediately + `Changed` |
| Warehouse | **No** independent MagicBook warehouse; grant still Tools GM `TryEquip` (UI-019 / D-061) |
| UI | InSaveShell MagicBook opens UI-023 (shared `BookRow` + drag reorder D-068 + click-slot delete D-072); no grant-from-popup. During AM Step2: already-pulsed slots do not roll back; remaining pulses read current slots (empty = skip) |

**4b. Combat MagicBooks (`Combat` phase)**

| Rule | Notes |
|------|-------|
| Trigger | After Defend / PushMap **`StartBattle`**, on each deployed soldier `TryRegisterWarrior`; reads protagonist 6 slots (left→right), not per-soldier gear |
| Token | `EffectPhase` includes `Combat` and `EffectPayload=StatMul`; params per [SPEC_04 §9.24](SPEC_04_Technical.md) Combat `StatMul` row |
| Samples | Eight combat stat books: `MagicBook_CombatMaxHpLow/High`, `CombatStrengthLow/High`, `CombatAgilityLow/High`, `CombatIntelligenceLow/High`; low `Mul=1.15`, high `Mul=1.30`; `IsUnique=1` `IsProbabilistic=0` |
| Stack | Same-dim books multiply (e.g. HP low+high = `1.15×1.30`); different dims coexist |
| Persist | **Does not** write `WarriorInstance.BaseStats`; effect ends when combat ends; **no** VisualStyle / scale step |
| Impl | `CombatMagicBookStatMul.Aggregate` → `TryRegisterWarrior` multiplies BodyLife / StaticStat before HP/Atk/ASPD/CD derives |

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

**6b. VisualStyle finalize (independent of `AppearanceId`)**

Mode2 Step2 applies the body-scale step and `MagicBookConfig.VisualStyleId` **only on a real token hit** (skip / miss / invalid do **not** write). Same gate as `WarriorVisualStyleBake.TryApply` call sites (incl. `SoldierSkillLevelAdd` when level is already clamped and unchanged).

**Hit body-scale step (coexists with VisualStyle channels; runs even if `VisualStyleId` empty)**

| Rule | Notes |
|------|-------|
| Trigger | Real token **hit** (same gate as material/scale channels below) |
| Step | `VisualModelScale *= WarriorVisualModelScalePerHit` (← `CombatConstantConfig`, sample **1.15**; `≤0` treated as 1.15) |
| Extra | If book is `Style_ScaleModel`, same hit then `*= VisualIntensityAdd` (below) |
| Cap | End of each apply: `VisualModelScale = min(·, WarriorVisualModelScaleMax)` (sample **3**) |
| Downstream | World `Visual.localScale=(k,k,k)`; `BodyRadius`/`AttackRange` both ×k (formation/Defend/PushMap read persisted field) |

VisualStyle also has **two independent channels** (one `VisualStyleId` per book; different books may land both):

**Material channel (AllIn1; one winner per soldier)**

| Rule | Notes |
|------|-------|
| Empty | Empty `VisualStyleId` → no material/scale-channel visual; **still** runs the body-scale step above |
| Scale Id | `Style_ScaleModel` / alias `放大模型` → **not** this channel (below) |
| Replace | No current material style, **or** book `VisualPriority` **>** current → set style, `VisualIntensity = VisualIntensityAdd` (empty column defaults to 1) |
| Same Id | Same as current material winner → `VisualIntensity += VisualIntensityAdd` |
| Lower | Else keep material winner (loser does **not** add intensity) |
| Apply | After Defend / PushMap / formation battlefield / drag-preview / **UI-016 card Camera+RT** Instantiate: Catalog `sharedMaterial` (no `new Material` / runtime `EnableKeyword`) + MPB intensity; add `AllIn1AtlasUvDriver` if missing |

**Scale channel (`Style_ScaleModel`; coexists with material; after hit step)**

| Rule | Notes |
|------|-------|
| Match | `VisualStyleId` is `Style_ScaleModel` or ZH alias `放大模型`; does **not** compete for material; does **not** change `VisualStyleId`/`VisualPriority`/`VisualIntensity` |
| Factor | `VisualIntensityAdd` is the extra multiplier (e.g. `1.5`); empty defaults to 1; `≤0` treated as 1 |
| Stack | After hit step: `VisualModelScale *= VisualIntensityAdd`, then clamp Max |
| Visual | Child `Visual.localScale = (k,k,k)` (`App_0_00` Visual prefab is `(1,1,1)`, root stays `(1.2,1.2,1.2)`, world ≈ `1.2k`) |
| Collision | Spawn `BodyRadius × k` (`NavMeshAgent.radius`, soft collision, formation spiral footprints) |
| Attack | Combat `AttackRange = ClassConfig.AttackRange × k`; `CombatReach` remains `AttackRange + both BodyRadii` |
| Mode1 / GM grant | `VisualModelScale=1` (no book) |

Shared: `RefinalizeInstance` may change `AppearanceId` and **must not** clear `VisualStyle*` or `VisualModelScale`. UI-016 card reveal is world Instantiate + Camera/RT and **does** apply AllIn1 and `VisualModelScale`; formation bar thumbs **do not** this round.

Demo samples: `MagicBook_WarriorEnhance`→`Style_WarriorGlow` P20 Add1 (hit still ×1.15); four `*Advance`→`Style_AdvanceOutline` P30 Add1 (hit only); `MagicBook_SoldierSkillLevel`→`Style_SkillAberration` P10 Add1; `MagicBook_Restore` may stay empty (hit still ×1.15; hand-check may also set `Style_ScaleModel` / `VisualIntensityAdd=1.5`).

New Style mats / Catalog / Excel Bake: [SPEC_04 §15.2](SPEC_04_Technical.md) “Adding a VisualStyle preset”. Scale-model needs **no** `.mat`.

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

**11. AutoManufacture presentation (stage View; UI-016 / D-055; Approach A body-rain + circle revive + per-slot MagicBook)**

Rules run the batch synchronously (pick→craft without books→flush→batch record; **no deploy yet**). When crafted **>0**, mount presentation: StepA body rain → StepB per-soldier circle flash + mystery rises from circle center in parallel with books then morph → then deploy by final ClassId → advance; **0 craft** mounts AM shell ~1s + Tips「无士兵可制造」, **no** StepA/B or books, then advance **without** auto-open Formation.

| Step | Rules |
|------|-------|
| StepA | **Body rain (once per batch):** count = all batch soldiers' `SourceItemIds` (consumed parts only; no warehouse leftovers). Each drop is tagged with owning `WarriorId` (for StepB absorb). Visual = `BodyPartConfig.ArtAssetId` (source `Assets/Art/UI/Dig/{ArtAssetId}.png`; runtime `Resources/UI/Dig/{ArtAssetId}`; missing art falls back by `BodySlot`; never spawn empty). Fit max-edge = `AutoMfgBodyMaxEdgePx` (sample **49**). Drop from screen-top center; every `AutoMfgBodyDropIntervalSeconds` (sample **0.3**) drop random `AutoMfgBodyDropCountMin`–`Max` (sample **3–5**); **total unchanged**. Gravity + soft 2D collision pile; collider is a **rotatable OBB** (SAT, not AABB): edge = fitted sprite size × `AutoMfgColliderInset` (sample **0.72**, smaller than the sprite so slanted pieces nest tighter); spawn yaw ±`AutoMfgSpawnAngleMaxDeg` (sample **50**) with angular velocity; **absolute Z angle clamped** ±`AutoMfgBodyAngleMaxDeg` (sample **270**, zero ω at limit; no endless tumble); pieces may rest at slants. Pool + Sleep on settle. Pile floor Y = `AutoMfgPileFloorYPx` (sample **−300**, vs `AmBodyRainLayer`). `MagicCircle_1` local Y=`AutoMfgMagicCircleYPx` (sample **−50**; same 2D layer; **draw order below body pieces**). Enter StepB after settle. |
| StepB | **Per soldier:** if any already landed, shift them left by one pitch first (short lerp sample **0.2s**; pitch=`AutoMfgSoldierMaxEdgePx`×`AutoMfgSoldierVisualScale`×`AutoMfgSoldierPitchFactor`, sample Factor **0.5** → **256**). Circle flashes red (`AutoMfgMagicCircleFlashHz`) → spawn mystery `UnknownSoldier_1` at circle center (feet pivot aligns circle `anchoredPosition`; sizeDelta max-edge `AutoMfgSoldierMaxEdgePx` sample **64**, `localScale`=`AutoMfgSoldierVisualScale` sample **8**) → **in parallel** with 6 book pulses left→right: each slot rises **1/6** of the path; **peak** applies that slot only + mystery flash (empty slots still pulse, take a beat, rise 1/6, flash, no effect); flash duration ≈ pulse duration × **0.4**. **During rise**, that soldier's StepA pile pieces tagged by `WarriorId` (from `SourceItemIds` drops) stagger-launch every `AutoMfgBodyAbsorbStaggerSeconds` (sample **0.06**), chase mystery visual center (feet + half height×scale; target updates as mystery rises), fly `AutoMfgBodyAbsorbFlySeconds` (sample **0.35**), Image white↔red at `AutoMfgBodyAbsorbFlashHz` (sample **8**); on arrive remove from pile and despawn (pool); other soldiers' pieces stay. At end of slot 6 feet at `AutoMfgSoldierLandYPx` (sample **−410**) → morph to `AppearanceId` (Idle + VisualStyle, same size) **in place** (**no** gravity fall); foot shadow Demo-hidden (`AutoMfgSoldierShadowAlpha` sample **0**; W/H/Offset keys retained); new soldier at horizontal center. Revived soldiers **ignore** body-part/circle collisions. Rate × `1.25^floor(completed/3)` every 3 soldiers (book pulse, per-slot rise, flash, morph hold, **body absorb stagger/fly** **share** the same speed). `AutoMfgReviveSpawnOffsetYPx` is **not** Demo spawn Y (key retained). |
| StepC | After all StepB → **DeployBatch by final ClassId** → enter UM and auto-open FormationEditor |

**Legacy soldier row (hidden by default):** Prefab `SoldierScroll` conveyor + Camera+RT reveal **kept**; `AutoMfgUseLegacySoldierRow=1` restores old Step1/2. Default **0** = body-rain flow.

**2D presentation layer (Approach A):** Overlay keeps background/Dim/BookRow; `AmBodyRainLayer` (1920×1080 Images; local Y=`AutoMfgBodyRainLayerYPx` sample **−340**) above Dim; pixel-space gravity + **rotatable OBB (SAT, collider inset)** soft pile for bodies (circle/revive still ignore body-part collisions); circle local Y=`AutoMfgMagicCircleYPx` (sample **−50**); revive pieces use **bottom pivot**. A separate ortho camera RT is covered by Overlay. Tunables: `CombatConstantConfig` `AutoMfg*` ([SPEC_04 §9.20b](SPEC_04_Technical.md)).

```
AutoManufacture stage
  → Clear BattleFormation
  → craft without MagicBook → flush → WarriorPool; Replace batch record
  → if crafted>0: UI-016 StepA → StepB (circle + mystery rise parallel books + morph)
  → DeployBatch by final ClassId
  → UpgradeManufacture (+ auto-open Formation if presentation ran)
```

**Impl priority (rules locked; coding in separate slices)**

| Priority | Content |
|----------|---------|
| P0 | Table columns + AutoManufacture stage + pick/class/stat loop + temp warehouse + auto-deploy + Mode2 UM diffs |
| P1 | MagicBook 6-slot save + empty hook; ManufactureRecord popup (last batch); AutoManufacture presentation UI-016 |
| P2 | Remaining MagicBook effects / grant-from-popup / manual soul |

---

## 3.16 主角装备（ProtagonistEquipment）

### 简体中文

**状态：规则库已关闭（本轮）；实现与 Demo 验收另授权；获取来源 / 制造·战斗 Token 登记表仍 TBD；正式仓 UI 本轮只读（UI-022 / D-067）**

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
| Dig | `EffectDomain` 含 `Dig` 时，解析当前等级行 `EquipEffect`（编码同科技 `AttributeModifiers`：`Attr_Value\|…`）。**静态键**（`DigDamage` / `DigCursorRadius` / `DigStageDurationBonus` / `GraveSpawnWeightBonus_{QualityId}` / `DigProcessSpawnCountBonus` 等）并入 `DigProtagonistCapabilities`；**事件键** `DigOnGraveClear`、`Explosive*`、`DigLightningIntervalSec` / `DigLightningFrameSec` / `DigLightningPreviewSec` **不**并入 caps（见下「Dig 事件型效果」） |
| 能力叠加 | `DigProtagonistCapabilities` 重算 = Σ 已学会科技 `AttributeModifiers` **+** Σ 仓内各装备当前行 Dig 域**静态** `EquipEffect`（**按键加法**） |
| 制造 / 战斗 | 域枚举已锁；`SoldierManufacture` / `Combat` 的 Token 登记表与 handler **TBD**（**不**复用 MagicBook `EffectPayload` 列名空间；另立 `ProtagonistEquipEffect` Token 表） |
| 触发时机 | 获得 / 转化 / 划入公共经验升级 / 卸除（若日后支持）后重算；进档加载后亦重算 |

**持久化意图（SaveSlot + CampaignMode）**

| 字段 | 说明 |
|------|------|
| `EquipCommonExp` | 非负整数（或 number） |
| `OwnedEquip[]` | `{ EquipId, Level, CurrentExp }[]` |

键名意图见 [SPEC_04 §6](SPEC_04_Technical.md)；**PE-02 已实现**（`ProtagonistEquipmentService` + PlayerPrefs）。Dig caps 合并 **PE-03 已实现**（`TechTreeService` 科技+Dig 装备加法）。炸药事件调度 **D-077 已实现**（`DigExplosiveScheduler`）。引雷落雷调度 **D-078 已实现**（`DigLightningScheduler`）。复活铲清坟产兵 **D-091 已实现**（`DigReviveShovelEffectConfig`）。

**Dig 事件型效果（炸药）**

仓内拥有 `Equip_Explosives` 且 `EffectDomain` 含 `Dig`、当前行可解析 `DigOnGraveClear` + `Explosive*` 时生效（`DigOnGraveClear` **非**炸药独占，复活铲亦可解析该键）。**仅**玩家 DigAction 直接消除的坟墓 HP 归零时按 Token 值掷概率（`_1` = 100%）；**爆炸伤害清坟不触发**新炸药桶。命中则：以该坟 `WorldPosition` 为圆心，在半径 `ExplosiveThrowRadius` 的圆环上均匀随机角度采样落点；落点须在地图可放置 IsoDiamond 内（`MapFootprintMath.ContainsXZ`），不合格重试至 `PlacementMaxRetries`；仍失败则 Warning 且本次不投掷。炸药桶飞行 `ExplosiveFlightSec` 后落地，再经 `ExplosiveFuseSec` 引信，对半径 `ExplosiveBlastRadius` 内未清除坟墓造成 `ExplosiveBlastDamage`（**独立于** `DigDamage`；爆炸清坟仍正常结算掉落/奖励）。爆炸瞬间在落点显示与地板同倾角的红色半透明圆圈，持续 `ExplosiveRingSec` 后消失。表现精灵 `ZYT_1`（`Art/Defend/Projectile/ZYT_1.png`）。**引雷 `ClearGraveByLightning` 删除的坟墓不走本流程。** 与复活铲同一次玩家清坟可各自掷概率、互不抢占。

**Dig 事件型效果（引雷）**

仓内拥有且 `EffectDomain` 含 `Dig`、当前行可解析 `DigLightningIntervalSec`（>0）时生效（Demo 装备=`Equip_Elctr`）。仅在 Dig **有效倒计时未归零** 期间 Tick。阶段 `Begin` 后先等待 **完整** 当前间隔再落第一次（Lv1=15s，Lv2=13s，Lv3=11s，Lv4=9s，Lv5=7s）；之后每满一间隔再落。升级即时改间隔：等待剩余大于新间隔则钳制到新间隔。每次落雷：若场上有未清除坟墓，等权随机一座；否则在可放置 IsoDiamond 内随机一点（避障规则同坟墓生成采样，`PlacementMaxRetries`）。命中坟墓时：按该坟品质 `LootDrop` **表项扫描**（`LootDropParser.ParseWeighted`，**不**掷 `DropMode`）收集 `IsPrimaryHand=1` 的躯体材料；多件等权随机一件；取其 `ClassRestrict`（多值 `|` 则等权随机一个 ClassId）与 `RaceId`，按 GM 发兵同款写入 `WarriorPool`（Dig 中 **不**自动布阵）。无主要手或 `ClassRestrict` 空 → **仍删除该坟、不产兵**。删除走 `ClearGraveByLightning`：**不**结算 LootDrop 入仓、**不**飞 DigReward、**不**触发 `DigOnGraveClear`/炸药/复活铲。无坟落点只播闪电、不产兵。表现：落点播放一次序列帧 `Art/Defend/Projectile/ShanDian_1/Elctr_0`～`Elctr_3`（**4 帧**，`DigLightningFrameSec` 默认 0.05s，总约 0.20s）；**每一帧**以其 Sprite **CustomPivot** 锚定在落点（坟墓中心 / 无坟随机点）；若产兵则在坟位 Instantiate 该兵 `AppearanceId` 模型（预览 **Scale XYZ=2**）、待机 `DigLightningPreviewSec`（默认 2s）后销毁 **View**（士兵实例保留；预览放大不写入实例）。

**Dig 事件型效果（复活铲）**

仓内拥有 `Equip_ReviveShovel` 且 `EffectDomain` 含 `Dig`、当前行可解析 `DigOnGraveClear`（>0）时生效（Mode2 Demo；`DigLightningPreviewSec` 默认 2）。**仅**玩家 DigAction 直接消除的坟墓 HP 归零时按 Token 值掷概率（L1=`_0.2` … L5=`_1`）；**爆炸清坟与引雷 `ClearGraveByLightning` 不触发**。命中则对**刚挖开的这座坟**复用引雷产兵路径：扫描该坟品质 **未结算** `LootDrop` 表项（`LootDropParser.ParseWeighted`，**不**掷 `DropMode`）中 `IsPrimaryHand=1`；多件等权随机一件；取其 `ClassRestrict`（多值 `|` 则等权随机一个 ClassId）与 `RaceId`，按 GM 发兵同款写入 `WarriorPool`（Dig 中 **不**自动布阵）。无主要手或 `ClassRestrict` 空 → **不产兵**。该坟 **仍**正常结算 LootDrop 入仓、飞 DigReward，且可同时触发炸药。表现：若产兵则坟位 Instantiate 该兵 `AppearanceId` 模型（预览 **Scale XYZ=2**）、待机 `DigLightningPreviewSec` 后销毁 **View**（与引雷共用预览通道；**不**播 `Elctr_*` 闪电序列帧）。

**正式仓 UI（本轮只读；UI-022 / D-067）**

| 规则 | 说明 |
|------|------|
| 入口 | InSaveShell 左下「装备」打开居中 Modal（Mode1/Mode2 均有） |
| 展示 | 只读 `ProtagonistEquipmentService.OwnedEquips`：当前等级行 `DisplayName`（空则 `EquipId`）+ `Lv.{Level}` + `Description` + `IconAssetId`（非空则加载图标） |
| 空态 | 仓空 → 文案「尚未拥有装备」 |
| 刷新 | 订阅 `Changed`（Tools GM `DebugGrantAtLevel` / Dig HUD `TryAcquire` 后列表更新） |
| 生效 | 仓内拥有即按当前等级行生效（无需再装配；同「定位」表） |
| 本轮不做 | 升级、划入 `EquipCommonExp`、卸下、从弹窗发放 |

**Demo 装备目录**

仅当前等级行生效，故 `EquipEffect` 为该级**累计**加成。Demo 基数 `DigCursorRadius=0.6`（`Tech_Root`）。旧样例 `Equip_DigRing`（挖坟之环）已删除。

| EquipId | DisplayName | 等级 | EffectDomain | EquipEffect（当前行） | ExpToNextLevel | ConvertExpValue |
|---------|-------------|------|--------------|----------------------|----------------|-----------------|
| `Equip_IronShovel` | 铁铲 | 1～5 | `Dig` | 相对基数每级 +10%：L1 `DigCursorRadius_0.06` … L5 `_0.30`（满级 +科技根项 = **0.90**） | L1–4 = **1**；L5 空 | 每级 **1** |
| `Equip_MinerLamp` | 矿灯 | 1～5 | `Dig` | 每级 Q4/Q5/Q6 生成权重累计 +10：L1 `GraveSpawnWeightBonus_Q4_10\|…_Q5_10\|…_Q6_10` … L5 `_50`；表中缺席视为 0 再加成 | L1–4 = **1**；L5 空 | 每级 **1** |
| `Equip_Explosives` | 炸药 | 1～5 | `Dig` | 事件：`DigOnGraveClear_1` + `ExplosiveThrowRadius_4` + `ExplosiveBlastRadius_2` + `ExplosiveBlastDamage_{13/18/23/28/33}` + `ExplosiveFlightSec_0.5` + `ExplosiveFuseSec_0.8` + `ExplosiveRingSec_0.5` | L1–4 = **1**；L5 空 | 每级 **1** |
| `Equip_Elctr` | 引雷 | 1～5 | `Dig` | 事件：`DigLightningIntervalSec_{15/13/11/9/7}` + `DigLightningFrameSec_0.05` + `DigLightningPreviewSec_2` | L1–4 = **1**；L5 空 | 每级 **1**（Mode2 样例 ConvertExp 为 1～5） |
| `Equip_Detector` | 探测器 | 1～5 | `Dig` | 静态：过程生成 M 累计 +1/级：L1 `DigProcessSpawnCountBonus_1` … L5 `_5`（**不**改 N） | L1–4 = **1**；L5 空 | 每级 **1**（Mode2 样例 ConvertExp 为 1～5） |
| `Equip_HumanToken` | 人类信物 | 1～5 | `Dig` | 静态：Q16～Q19 生成权重累计 L1=`_10` … L5=`_30`（`GraveSpawnWeightBonus_Q16_*\|…_Q19_*`）；表缺席视为 0 再插入 | L1–4 = **1**；L5 空 | Mode2 样例 ConvertExp 为 1～5 |
| `Equip_ElfToken` | 精灵信物 | 1～5 | `Dig` | 静态：Q20～Q23 生成权重累计 L1=`_10` … L5=`_30` | L1–4 = **1**；L5 空 | Mode2 样例 ConvertExp 为 1～5 |
| `Equip_OrcToken` | 兽人信物 | 1～5 | `Dig` | 静态：Q24～Q27 生成权重累计 L1=`_10` … L5=`_30` | L1–4 = **1**；L5 空 | Mode2 样例 ConvertExp 为 1～5 |
| `Equip_ReviveShovel` | 复活铲 | 1～5 | `Dig` | 事件：`DigOnGraveClear_{0.2/0.4/0.6/0.8/1}` + `DigLightningPreviewSec_2`（Mode2） | L1–4 = **1**；L5 空 | Mode2 样例 ConvertExp 为 1～5 |

**明确非范围（本轮规则录入）**

- 不改 MagicBook / SpecialEquipSlots / 材料 Warehouse / ExtraEquipment
- 装备升级 / 划入公共经验 / 卸下 UI、商店、Dig 掉落入账后置；仓只读见 §3.8 **D-067**；Demo 垂直见 **D-059**（表加载 + 仓 + Dig caps + GM）

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

**Status: Rules closed this pass; implementation / Demo acceptance require separate authorization; acquire sources and Manufacture/Combat token registry still TBD; formal warehouse UI this round is read-only (UI-022 / D-067)**

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
| Dig | When domain includes `Dig`, parse current-row `EquipEffect` (same encoding as tech `AttributeModifiers`). **Static keys** (`DigDamage` / `DigCursorRadius` / `DigStageDurationBonus` / `GraveSpawnWeightBonus_{QualityId}` / `DigProcessSpawnCountBonus` etc.) merge into `DigProtagonistCapabilities`; **event keys** `DigOnGraveClear`, `Explosive*`, `DigLightningIntervalSec` / `DigLightningFrameSec` / `DigLightningPreviewSec` **do not** merge into caps (see “Dig event effects” below) |
| Cap stack | `DigProtagonistCapabilities` = Σ learned tech modifiers **+** Σ owned current-row Dig **static** `EquipEffect` (**additive per key**) |
| Manufacture / Combat | Domain enum locked; Token registry / handlers **TBD** (**not** MagicBook `EffectPayload` namespace; separate `ProtagonistEquipEffect` token table) |
| When | Recalc after acquire / convert / common-Exp spend / (future unequip); also after save load |

**Persistence intent (SaveSlot + CampaignMode)**

| Field | Notes |
|-------|-------|
| `EquipCommonExp` | Non-negative |
| `OwnedEquip[]` | `{ EquipId, Level, CurrentExp }[]` |

Key intent: [SPEC_04 §6](SPEC_04_Technical.md); **PE-02 implemented** (`ProtagonistEquipmentService` + PlayerPrefs). Dig caps merge **PE-03 implemented** (`TechTreeService` tech + Dig gear additive). Explosive event scheduler **D-077 implemented** (`DigExplosiveScheduler`). Lightning scheduler **D-078 implemented** (`DigLightningScheduler`). Revive-shovel grave-clear spawn **D-091 implemented** (`DigReviveShovelEffectConfig`).

**Dig event effects (Explosives)**

Applies while `Equip_Explosives` is owned, `EffectDomain` includes `Dig`, and the current row parses `DigOnGraveClear` + `Explosive*` (`DigOnGraveClear` is **not** explosives-only; Revive Shovel may parse the same key). On **player DigAction** grave HP reaching 0 only, roll the token value (`_1` = 100%); **graves cleared by blast damage do not** trigger a new barrel. On hit: sample a landing point on the circle of radius `ExplosiveThrowRadius` around that grave’s `WorldPosition`; the point must lie in the placeable IsoDiamond (`MapFootprintMath.ContainsXZ`); retry up to `PlacementMaxRetries`; if all fail, Warning and skip this throw. The barrel flies for `ExplosiveFlightSec`, then fuses for `ExplosiveFuseSec`, then deals `ExplosiveBlastDamage` to uncleared graves within `ExplosiveBlastRadius` (**independent of** `DigDamage`; blast clears still settle loot/rewards normally). At blast, show a red translucent disc matching floor tilt for `ExplosiveRingSec`. Sprite `ZYT_1` (`Art/Defend/Projectile/ZYT_1.png`). Graves removed by lightning `ClearGraveByLightning` **do not** enter this path. The same player clear may also roll Revive Shovel independently.

**Dig event effects (Lightning)**

Applies while owned, `EffectDomain` includes `Dig`, and the current row parses `DigLightningIntervalSec` > 0 (Demo gear = `Equip_Elctr`). Ticks only while Dig effective duration has not reached 0. After stage `Begin`, wait a **full** current interval before the first strike (Lv1=15s, Lv2=13s, Lv3=11s, Lv4=9s, Lv5=7s); then repeat each interval. Level-up updates the interval immediately (clamp remaining wait to the new interval if larger). Each strike: if any uncleared grave exists, pick one uniformly; else sample a placeable IsoDiamond point (same obstacle retries as grave spawn, `PlacementMaxRetries`). On a grave: scan that quality’s `LootDrop` **table entries** (`LootDropParser.ParseWeighted`, **no** `DropMode` roll) for `IsPrimaryHand=1` body parts; if several, pick one uniformly; take `ClassRestrict` (if `|`-separated, pick one ClassId uniformly) and `RaceId`, grant via the same path as GM soldier grant into `WarriorPool` (**no** auto-deploy during Dig). No primary hand or empty `ClassRestrict` → **still delete the grave, spawn no soldier**. Deletion uses `ClearGraveByLightning`: **no** LootDrop warehouse credit, **no** DigReward flyer, **no** `DigOnGraveClear`/explosives/Revive Shovel. A no-grave landing plays lightning only. Presentation: play sequence `Art/Defend/Projectile/ShanDian_1/Elctr_0`–`Elctr_3` (**4 frames**, `DigLightningFrameSec` default 0.05s, ~0.20s total) once at the point; **each frame** anchors its Sprite **CustomPivot** to the strike point (grave center / no-grave sample); if a soldier was granted, Instantiate its `AppearanceId` model at the grave (preview **Scale XYZ=2**), idle for `DigLightningPreviewSec` (default 2s), then destroy the **View** (instance remains in the pool; preview scale is View-only).

**Dig event effects (Revive Shovel)**

Applies while `Equip_ReviveShovel` is owned, `EffectDomain` includes `Dig`, and the current row parses `DigOnGraveClear` > 0 (Mode2 Demo; `DigLightningPreviewSec` default 2). On **player DigAction** grave HP reaching 0 only, roll the token (L1=`_0.2` … L5=`_1`); **blast clears and lightning `ClearGraveByLightning` do not** trigger. On hit, reuse the Lightning soldier-grant path on **that just-cleared grave**: scan the quality’s **unsettled** `LootDrop` table entries (`LootDropParser.ParseWeighted`, **no** `DropMode` roll) for `IsPrimaryHand=1`; if several, pick one uniformly; take `ClassRestrict` (if `|`-separated, pick one ClassId uniformly) and `RaceId`, grant via the same path as GM soldier grant into `WarriorPool` (**no** auto-deploy during Dig). No primary hand or empty `ClassRestrict` → **spawn no soldier**. The grave **still** settles LootDrop into warehouse and flies DigReward, and may also trigger explosives. Presentation: if a soldier was granted, Instantiate its `AppearanceId` model at the grave (preview **Scale XYZ=2**), idle for `DigLightningPreviewSec`, then destroy the **View** (shared preview channel with Lightning; **no** `Elctr_*` bolt frames).

**Formal warehouse UI (read-only this round; UI-022 / D-067)**

| Rule | Notes |
|------|-------|
| Entry | InSaveShell bottom-left Equipment opens centered Modal (Mode1 and Mode2) |
| Display | Read-only `ProtagonistEquipmentService.OwnedEquips`: current-level `DisplayName` (else `EquipId`) + `Lv.{Level}` + `Description` + `IconAssetId` (load sprite if set) |
| Empty | Empty warehouse → 「尚未拥有装备」 |
| Refresh | Subscribe `Changed` (Tools GM `DebugGrantAtLevel` / Dig HUD `TryAcquire` updates the list) |
| Apply | Owned applies at current-level row (no extra slot; same as Positioning) |
| Not this round | Level-up, spend `EquipCommonExp`, unequip, grant-from-popup |

**Demo gear catalog**

Only the current-level row applies, so `EquipEffect` is the **cumulative** bonus at that level. Demo base `DigCursorRadius=0.6` (`Tech_Root`). Former sample `Equip_DigRing` (Dig Ring) **removed**.

| EquipId | DisplayName | Levels | EffectDomain | EquipEffect (current row) | ExpToNextLevel | ConvertExpValue |
|---------|-------------|--------|--------------|---------------------------|----------------|-----------------|
| `Equip_IronShovel` | Iron Shovel | 1–5 | `Dig` | +10% of base per level: L1 `DigCursorRadius_0.06` … L5 `_0.30` (max + tech root = **0.90**) | L1–4 = **1**; L5 empty | **1** each level |
| `Equip_MinerLamp` | Miner Lamp | 1–5 | `Dig` | Q4/Q5/Q6 spawn-weight cumulative +10 per level: L1 `GraveSpawnWeightBonus_Q4_10\|…_Q5_10\|…_Q6_10` … L5 `_50`; missing table Id treated as 0 then bonus | L1–4 = **1**; L5 empty | **1** each level |
| `Equip_Explosives` | Explosives | 1–5 | `Dig` | Event: `DigOnGraveClear_1` + `ExplosiveThrowRadius_4` + `ExplosiveBlastRadius_2` + `ExplosiveBlastDamage_{13/18/23/28/33}` + `ExplosiveFlightSec_0.5` + `ExplosiveFuseSec_0.8` + `ExplosiveRingSec_0.5` | L1–4 = **1**; L5 empty | **1** each level |
| `Equip_Elctr` | Lightning (引雷) | 1–5 | `Dig` | Event: `DigLightningIntervalSec_{15/13/11/9/7}` + `DigLightningFrameSec_0.05` + `DigLightningPreviewSec_2` | L1–4 = **1**; L5 empty | **1** each level (Mode2 sample ConvertExp 1–5) |
| `Equip_Detector` | Detector | 1–5 | `Dig` | Static: process-spawn M cumulative +1/level: L1 `DigProcessSpawnCountBonus_1` … L5 `_5` (**does not** change N) | L1–4 = **1**; L5 empty | **1** each level (Mode2 sample ConvertExp 1–5) |
| `Equip_HumanToken` | Human Token | 1–5 | `Dig` | Static: Q16–Q19 spawn-weight cumulative L1=`_10` … L5=`_30` (`GraveSpawnWeightBonus_Q16_*\|…_Q19_*`); missing table Id = 0 then insert | L1–4 = **1**; L5 empty | Mode2 sample ConvertExp 1–5 |
| `Equip_ElfToken` | Elf Token | 1–5 | `Dig` | Static: Q20–Q23 spawn-weight cumulative L1=`_10` … L5=`_30` | L1–4 = **1**; L5 empty | Mode2 sample ConvertExp 1–5 |
| `Equip_OrcToken` | Orc Token | 1–5 | `Dig` | Static: Q24–Q27 spawn-weight cumulative L1=`_10` … L5=`_30` | L1–4 = **1**; L5 empty | Mode2 sample ConvertExp 1–5 |
| `Equip_ReviveShovel` | Revive Shovel | 1–5 | `Dig` | Event: `DigOnGraveClear_{0.2/0.4/0.6/0.8/1}` + `DigLightningPreviewSec_2` (Mode2) | L1–4 = **1**; L5 empty | Mode2 sample ConvertExp 1–5 |

**Out of scope this rules pass**

- No MagicBook / SpecialEquipSlots / material Warehouse / ExtraEquipment changes
- Equipment level-up / spend common Exp / unequip UI, shop, Dig loot credit deferred; warehouse read-only = §3.8 **D-067**; Demo vertical = **D-059** (table load + warehouse + Dig caps + GM)

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

## 3.17 阵容羁绊（FormationBond）

### 简体中文

**状态：规则库已关闭（本轮 Demo 片）；实现须负责人授权；本 Demo 片仅配置 + 激活判定 + UI 展示，羁绊 Buff 战斗 Handler 后置**

Mode1 / Mode2 **共用机制**；配置表按 `CampaignMode` 各自 CSV 根（[SPEC_04 §14.5](SPEC_04_Technical.md)）。表结构见 [SPEC_04 §9.26](SPEC_04_Technical.md)。

**组成**

| 概念 | 说明 |
|------|------|
| 羁绊激活条件 | 统计**已上阵**士兵的限定属性；满足表行 `ActivationCondition` 时该行可激活 |
| 羁绊效果库 | `FormationBondConfig` 全表；布阵每次变更阵容时重新统计 |
| 羁绊 Buff | 可激活的技能效果；`BondBuff` FK → `SkillEffectConfig.SkillEffectId`（本 Demo 片只配置与展示） |

**激活规则**

| 规则 | 说明 |
|------|------|
| 统计源 | 仅 `BattleFormationService.Entries`（**非**全 `WarriorPool`） |
| 职业 | `WarriorInstance.ClassId` → `ClassConfig`；条件可配 `ClassId` 精确匹配或 `BaseClass`（基础职业：战士/射手/法师/刺客） |
| 种族 | `WarriorInstance.RaceId` |
| 主属性 | `ClassConfig.PrimaryStat`（`Strength` / `Agility` / `Intelligence`） |
| 同级互斥 | 同一 `BondId` 多等级：**仅最高满足条件的等级激活**；更高等级激活时低等级**停用**（详情 UI 可标「已被高等级替代」） |
| 战斗展示 | 开战时锁定当前上阵快照评估；战斗中不因士兵死亡改变羁绊 HUD（实际 Buff 生效另片） |
| 效果解析 | **禁止**解析 `Description` 自然语言；激活条件走结构化 DSL；Buff 正文走 `SkillEffectConfig` |

**UI（Demo 片）**

| 位置 | 行为 |
|------|------|
| 布阵 Prepare | `FormationCanvas` 左上：「查看阵容羁绊」按钮 + 已激活羁绊图标行；阵容变更实时刷新 |
| Defend / PushMap Combat | 战斗 HUD 左上同构；`BondHudRoot` 的 `anchoredPosition.y = -70`；展示开战锁定的激活列表；`BondDetailModal` 默认关闭，仅点击「查看阵容羁绊」按钮打开，点击 `CloseButton` 再次隐藏；实现约束：`CloseButton` 事件绑定必须在 `Configure()` 中补齐（避免 `Awake()` 早于引用赋值导致监听丢失） |

**激活条件 DSL（首版）**

| Kind | 参数 |
|------|------|
| `DeployClassCount` | `BaseClass=战士` **或** `ClassId=Class_Guardian`（二选一）；`Min=N` |
| `DeployRaceCount` | `RaceId=…`；`Min=N` |
| `DeployTotalCount` | `Min=N` |
| `DeployPrimaryStatCount` | `Stat=Strength\|Agility\|Intelligence`；`Min=N` |

格式：`{Kind}|Key=Value|…`（`|` 分隔）。

```
EvaluateBonds(formation, pool, configs)
  → stats = CountDeployed(formation.Entries, pool, configs)
  → for each FormationBondConfig row: meets = TestCondition(row, stats)
  → per BondId: activeLevel = max BondLevel where meets
  → UI: icons = rows where BondLevel == activeLevel and meets
```

### English

**Status: Rules closed this Demo slice; implementation requires authorization; this slice = config + activation eval + UI only; bond Buff combat handlers deferred**

Shared across Mode1 / Mode2; tables per `CampaignMode` CSV root ([SPEC_04 §14.5](SPEC_04_Technical.md)). Schema: [SPEC_04 §9.26](SPEC_04_Technical.md).

**Components**

| Concept | Notes |
|---------|-------|
| Bond activation condition | Count limited attributes of **deployed** soldiers; row activates when `ActivationCondition` met |
| Bond effect library | Full `FormationBondConfig`; re-evaluated on every formation change |
| Bond Buff | Activatable skill effect; `BondBuff` FK → `SkillEffectConfig.SkillEffectId` (this slice: config + display only) |

**Activation rules**

| Rule | Notes |
|------|-------|
| Source | `BattleFormationService.Entries` only (**not** full `WarriorPool`) |
| Class | `WarriorInstance.ClassId` → `ClassConfig`; condition may use exact `ClassId` or `BaseClass` |
| Race | `WarriorInstance.RaceId` |
| Primary stat | `ClassConfig.PrimaryStat` |
| Level exclusivity | Same `BondId`: **only highest satisfied `BondLevel` active**; lower levels suppressed |
| Combat display | Snapshot at StartBattle; HUD does not change on soldier death (Buff apply deferred) |
| Parsing | **Do not** parse `Description` for effects; conditions = structured DSL; Buff = `SkillEffectConfig` |

**UI (Demo slice)**

| Location | Behavior |
|----------|----------|
| Formation Prepare | Top-left on `FormationCanvas`: view button + active bond icon row; live refresh |
| Defend / PushMap Combat | Same top-left HUD; `BondHudRoot` anchoredPosition.y = -70; StartBattle snapshot; `BondDetailModal` default hidden, shown only after clicking the view button; `CloseButton` hides it again; Implementation constraint: `CloseButton` must be wired in `Configure()` (so runtime `AddComponent` paths don’t miss the `Awake()` binding) |

**Condition DSL (v1):** `DeployClassCount`, `DeployRaceCount`, `DeployTotalCount`, `DeployPrimaryStatCount` — see Chinese table above.

---

## 3.18 战术阵型（TacticalFormation）

### 简体中文

**状态：D-084 框架已落地。D-093 修订成员关系（TFG-01～05 已编码；待验收）。D-094 修订战斗：守槽、前方 90° 才攻击、守槽不受挤、最大编号递补（TFH-00～04）。**

Mode1 / Mode2 **共用机制**；配置表按 `CampaignMode` 各自 CSV 根（[SPEC_04 §14.5](SPEC_04_Technical.md)）。表结构见 [SPEC_04 §9.30](SPEC_04_Technical.md)。**勿与** §3.11/§3.12 `BattleFormation`（上阵坐标持久化）、§3.17 `FormationBond`（属性计数 Buff、无空间站位）混淆。

**定位**

| 概念 | 说明 |
|------|------|
| 战术阵型 | 拥有阵型技能的士兵经左缘按钮手动建成空间编队（已上阵优先，不足时从士兵栏未上阵补人并上阵）；同 `FormationId` 可多组；Combat 每组一个**虚拟中心**，成员保持 Pattern 相对站位 |
| 阵型技能 | 魔法书 Token `GrantFormationSkill`（Mode2 制造 Step2 单槽脉冲）写入实例 `SoldierSkills`；`SkillConfig.FormationId` 指向 `TacticalFormationConfig` |
| 虚拟中心 | 规则层纯数据移动体（可选无 Visual 锚；**不**登记为 `MassMoveScheduler` agent，避免 SoftCollision 幽灵体）；PushMap 由 RuntimeService 沿 FlowField 方向积分；Defend 守组阵时中心点 |
| 与 Follow 关系 | **不是**粘随主角 / BMH `ArmyRadius`；是独立 `GoalKind=FormationSlot` + 中心驱动（修订 §3.12 B+「不做 Follow」口径） |

**魔法书授予（GrantFormationSkill）**

| 规则 | 说明 |
|------|------|
| Token | `EffectPayload=GrantFormationSkill`；`EffectPhase=SoldierManufacture`；登记见 [SPEC_04 §9.24](SPEC_04_Technical.md) |
| 参数 | **必填** `FormationId`（阵型身份，**不是**某一 `FormationLevel` 行）；**可选** `ClassId` / `RaceId` / `BaseClass` / `Chance`（过滤语义对齐 `ForceClass` / `StatMul`） |
| 命中 | 向实例 `SoldierSkills` **追加**该阵型 `FormationSkillId`@Lv1（已有同 `SkillId` 不重复；等级可用 `SoldierSkillLevelAdd` 二次扫描） |
| 未命中 | 空 apply + 日志；不授予技能、不参与组阵 |
| Mode1 | 手动制造本 Demo **不跑**本 Token（与其它制造书一致）。**例外：** Tools GM「添加士兵」（D-064）在已装备该书时 **仅**应用 `GrantFormationSkill`（不跑其它制造 Token；便于 Mode1 Defend 手验） |

**配置：表 + Pattern Prefab**

| 层 | 职责 |
|----|------|
| `TacticalFormationConfig`（§9.30） | 复合主键 `(FormationId, FormationLevel)`；同 Id 各行的展示、`FormationSkillId`、人数、`PrefabId` 必须一致；`StatModifiers` 与专属技能列按等级行走 |
| Pattern Prefab `Assets/Prefabs/Formation/Patterns/{PrefabId}.prefab` | 根 = **中心**；根 forward = **朝向**（XZ）；子节点 `Slot_*` = 士兵**相对位置**；组件字段 = **通用移动参数**（`LeashRadius`、`SlotArriveEpsilon`、`CenterMoveSpeedMul`、`FacingTurnRate`、`KeepFormationWhileEngage` 等，见 §9.30） |

**布阵手动建组（D-093 修订 D-084 自动单实例；仍覆盖职业区）**

| 规则 | 说明 |
|------|------|
| 不再自动成组 | 布阵变更、一键上阵、AutoManufacture `DeployBatch` **不**把闲兵吸进组。已有组只裁剪：下阵成员移出；剩余 **&lt; `MinMemberCount`** → 整组解散并退回职业区螺旋（D-052 同算法） |
| 创建 | **仅**左缘目录按钮（UI-030）。一次点击产生一个 `GroupInstanceId`。同 `FormationId` 可同时多组 |
| 资格 | **未入任何组**，且 `SoldierSkills` 中**任一**技能的 `SkillConfig.FormationId` 等于被点阵型（禁止只看第一个阵型技能）。候选顺序：已上阵合格兵（上阵先后 / `WarriorId`）在前，士兵栏未上阵合格兵（库顺序）在后。资格查询独立于组 Id / 存档 / 战斗键；**不**按职业筛掉入组资格 |
| 人数 | 候选合计 ≥ `MinMemberCount` 才建组；纳入人数 = `min(候选数, MaxMemberCount, 槽位数)`。**分槽**（谁进组、站哪格）从**全部候选**里抽，不先按人数截断：按槽下标，`TacticalFormationSlot.PreferredClass`（=`ClassConfig.BaseClass`；缺组件 / `Unspecified` = 无偏好）优先匹配尚未入槽的同 `BaseClass` 且 `ClassLevel` 最高者（同分保留候选顺序，已上阵在前）；无偏好或无匹配则按候选剩余顺序填该槽。被分到槽的栏内士兵才上阵。战斗递补**不**读 PreferredClass。不满 Max 也可以建。合计不足 Min：**不动**已有坐标、**不**上阵。栏内某人上阵失败：撤回本点击新上阵的人；撤回后已上阵候选仍 ≥ Min 则只用这些已上阵候选重新分槽，否则整次失败 |
| 一人一组 | 已入组的士兵不参与下一次创建，也不能同时属于另一个阵型 |
| Snap | 阵心 = 本次纳入的**已上阵**成员质心；一个已上阵都没有时 = 调用方传入的镜头画面中心地面点（地图相对 XZ），未传入则失败且不动。朝向 = PushMap Prepare 指向当前/首 `Objective`，Defend Prepare 指向 `EngageZone` 内侧 / 地图默认 +Z。未上阵成员先上阵到槽位坐标，再与已上阵一起写回，**覆盖**原职业区螺旋位 |
| 拖拽 | 组成员 **禁止**单独拖散；命中任一成员 = **整组**拖动（只改中心，保持相对偏移与朝向）。键 = `GroupInstanceId`，不是 `FormationId` |
| 旋转（Q/E） | 整组左键**按住/拖动**期间：`Q` = 逆时针、`E` = 顺时针，每按一次绕当前阵心旋转 **`FormationRotateStepDegrees=15`**；即时写回成员坐标与 `FacingYawDegrees`。未按住整组时 Q/E **无效** |
| 默认八向（D-095） | 已入组成员的布阵预览八向 = 该组 `FacingYawDegrees` 量化到 45° 扇区（0°=世界 +Z=北）。入组与 `Q`/`E` 改朝向后预览跟着换；未跨扇区不换精灵。未入组预览与士兵栏缩略图仍固定朝南（`DirIndex=2`） |
| 朝向保留 | 新建组按地图目标自动朝向；已有组被裁剪时 **保留** `FacingYawDegrees`（含玩家旋转） |
| 布阵滚轮 | 共享 `FormationEditor` 打开时：鼠标滚轮拉近/拉远；步进/夹限与 Combat 相同；指针在士兵栏等阻挡 UI 上时忽略 |
| 目录条 UI-030 | 左缘展示表内全部阵型（同阵型各等级 **1** 个按钮）；点击只尝试建组 |
| 组卡牌 UI-035 | 顶中一组一张；视口最多 8 张，超出左右滑动。点卡选中该 `GroupInstanceId`：士兵栏仅高亮成员，布阵预览头顶显示阵型图标（不进战斗）；选中卡下方「解散」= 退回职业区并删组 |
| 存档 | 组（`GroupInstanceId`、`FormationId`、成员 Id、朝向）与 `BattleFormation` 一起持久化。等级不存，读档重算。旧档无组字段 = 没有组，**不**按站位自动还原 |
| 与羁绊 | 并行；不改变羁绊统计源（仍按上阵名单计数） |

**战斗：虚拟中心 + FormationSlot + 守槽（D-094 修订 D-084 的接敌 leash）**

| 规则 | 说明 |
|------|------|
| 开战快照 | 每组独立锁定：中心世界位、朝向、成员↔槽位、Pattern 移动参数（含 `FrontArcDegrees`）、当时算出的 `FormationLevel` 与对应属性行。运行时键 = `GroupInstanceId`（禁止再用 `FormationId` 当唯一实例） |
| 中心移动 | 每组一个虚拟中心。PushMap：`Tick` 沿 FlowField 方向积分（速度 = 该组代表成员 `MoveSpeed × CenterMoveSpeedMul`）；Defend：中心守该组组阵点。整组仍跟着中心走，不是冻在世界坐标上 |
| 成员移动 | `GoalKind=FormationSlot`；`DesiredDestination = center + Rot(facing) * slotLocal`。接敌**不**改 `AttackSlot`、**不**认领攻击环。未进入 `SlotArriveEpsilon` 继续走向槽位。守槽时该成员受到的软碰撞修正为 **0**（仍可推开别人）。表现八向（D-095）：走路与待机跟该组 `FacingYawDegrees`；开始攻击仍朝敌人一次，结束后回到阵型朝向。走跑仍由 steer 决定 |
| 眼前攻击 | 已在槽上，且敌人中心落在**该组朝向**前方 `FrontArcDegrees`（缺省 **90**，左右各 45°）内，并进入该兵 `AttackRange`：暂停位移，沿用现有命中。**D-096：** 扇形外若双方身体贴靠（XZ 中心距 ≤ 双方 `BodyRadius` 之和）亦可原地挥刀；不追、不改 `GoalKind`；开打仍朝敌人一次。多名候选取最近（扇形内进距与贴身扇外合并）。未贴身且扇外，或扇形内但射程外：不追。未到槽先回槽再打 |
| Leash | `LeashRadius` 字段保留在 Pattern 上，**不再**决定离槽或是否开打 |
| 槽位递补 | 死亡或 Rebel 腾出槽位，且该组仍 ≥ `MinMemberCount`：活着成员里槽下标最大者，若大于空槽下标，换到该空槽。编号 = `Slot_*` 名字排序后的下标（`Slot_00` = 0）。最大编号自己腾出则高位留空，**不**把中间的人依次前移。同一帧多个空槽按编号从小到大依次补。不写回布阵坐标 |
| Rebel | 退出阵型（无加成）；腾槽后按上条递补；走既有 Rebel 选敌 |
| 解散 | **该组**存活成员（非 Rebel、非 `CombatDead`）**< `MinMemberCount`** → 只解散这一组：不递补；撤 overlay；成员 GoalKind 回 PushMap `Objective` 或 Defend 个人 Home（**解散瞬间世界坐标记为新 Home**）。其它组不受影响 |
| 不回写 Prepare | 战斗中阵亡 / 解散 / 递补 **不**写回 Prepare 布阵坐标 |
| 未入组 | 仍走原来的 AttackSlot / Objective / FormationHome |

**属性与专属技能加成**

| 规则 | 说明 |
|------|------|
| 生效窗 | 仅该组 **激活态**。开战按当时成员算等级并锁行；成员死亡 / Rebel 后若该组仍 ≥ Min，按**剩余成员**重算等级并换 overlay；&lt; Min 则解散 |
| 等级 | `Floor(成员 ClassConfig.ClassLevel 之和 / 人数)`（缺职业行按 0；正整数等价于整数除法）。玩家不点选等级 |
| 查表 | 复合主键 `(FormationId, FormationLevel)`。取 **≤ 计算等级** 的最大表等级（只向下）。没有任何 ≤ 的行：组仍成立，`StatModifiers` 为空并 Warning |
| 属性 | 命中行的 `StatModifiers`；结算对齐 Combat `StatMul` overlay，与魔法书 Combat StatMul **乘积叠加**；**不改** `WarriorInstance.BaseStats`。换档或解散时按剩余 Combat StatMul 重算派生属性，`RemainingHp` **钳制**到新 MaxHP |
| 专属技能 | 跟**同一等级行**的 `ExclusiveSkillIds` / `ExclusiveSkillEffectIds`（只读拼接，不写实例 `SoldierSkills`，不进存档） |
| 与羁绊 | 可叠加；同一 `SkillEffectId` 禁止双计（加载期 Warning） |

**实现切片：** D-084 框架 `.scratch/tactical-formation/issues/` TF-00～06（已落地）。D-093 `.scratch/tactical-formation-groups/issues/` TFG-01～05 已编码（待验收）。

```
TryCreateGroup(formationId)
  → candidates = deployed matches (deploy order) then bar matches (pool order)
  → if candidates < Min → no-op (no deploy)
  → take = min(candidates, Max, slots)
  → assign those slots from the full candidate list (do not truncate first):
       preferred slot → remaining same BaseClass, highest ClassLevel (tie = earlier candidate)
       unspecified / no match → next leftover candidate
  → center = centroid of assigned members who were already deployed, else view-center ground point
  → deploy only the assigned bar members; failed deploy rolls back this click's new deploys
  → if deployed candidates still ≥ Min → reassign from those deployed candidates only, else no-op
  → snap; new GroupInstanceId
  → level = Floor(mean ClassLevel); stat row = greatest FormationLevel <= level
Prune on deploy change
  → drop missing members; if remaining < Min → disband to class zones
StartBattle
  → one CombatLock per GroupInstanceId (center, facing, member→slot, level row)
OnMemberDeath / Rebel
  → if living >= Min → highest slot index fills a lower vacated index; recompute level and swap overlay
  → else dissolve that group only (no fill)
```

### English

**Status: D-084 framework landed. D-093 revises membership (TFG-01–05 coded; pending playtest). D-094 revises combat: hold the slot, swing only inside the forward 90° arc, no incoming shove while holding, highest slot index fills a lower vacancy (TFH-00–04).**

Shared across Mode1 / Mode2; tables per `CampaignMode` CSV root ([SPEC_04 §14.5](SPEC_04_Technical.md)). Schema: [SPEC_04 §9.30](SPEC_04_Technical.md). **Distinct from** §3.11/§3.12 `BattleFormation` (deploy coords persistence) and §3.17 `FormationBond` (stat-count buffs, no spatial layout).

**Role**

| Concept | Notes |
|---------|-------|
| TacticalFormation | Soldiers with a Formation Skill are manually grouped from the left-edge button (deployed first; shortfall filled from undeployed SoldierBar and deployed onto the field); the same `FormationId` may have many groups; Combat gives **each group** a virtual center and members keep pattern offsets |
| Formation Skill | MagicBook token `GrantFormationSkill` (Mode2 manufacture Step2 per-slot pulse) writes instance `SoldierSkills`; `SkillConfig.FormationId` → `TacticalFormationConfig` |
| Virtual center | Rules-layer mover (optional no-Visual; **not** a `MassMoveScheduler` agent, no SoftCollision ghost); PushMap: RuntimeService integrates along FlowField dir; Defend holds deploy center |
| vs Follow | **Not** sticky follow protagonist / BMH `ArmyRadius`; uses `GoalKind=FormationSlot` + center drive (revises §3.12 B+ “no Follow” wording) |

**MagicBook grant (`GrantFormationSkill`)**

| Rule | Notes |
|------|-------|
| Token | `EffectPayload=GrantFormationSkill`; `EffectPhase=SoldierManufacture`; registry [SPEC_04 §9.24](SPEC_04_Technical.md) |
| Params | **Required** `FormationId` (formation identity, **not** a specific `FormationLevel` row); **optional** `ClassId` / `RaceId` / `BaseClass` / `Chance` (filters align with `ForceClass` / `StatMul`) |
| Hit | **Append** formation `FormationSkillId`@Lv1 to `SoldierSkills` (skip duplicate SkillId; level via `SoldierSkillLevelAdd` second pass) |
| Miss | Empty apply + log; no skill, no squad |
| Mode1 | Demo manufacture **does not** run this token. **Exception:** Tools GM Add Soldier (D-064) applies **only** `GrantFormationSkill` when that book is equipped (no other manufacture tokens; Mode1 Defend handcheck) |

**Config: table + Pattern Prefab**

| Layer | Role |
|-------|------|
| `TacticalFormationConfig` (§9.30) | Composite PK `(FormationId, FormationLevel)`; display, `FormationSkillId`, headcount, and `PrefabId` must match across levels of one Id; `StatModifiers` and exclusive-skill columns follow the level row |
| Pattern Prefab `Assets/Prefabs/Formation/Patterns/{PrefabId}.prefab` | Root = **center**; root forward = **facing** (XZ); child `Slot_*` = **relative positions**; component = **shared move params** (`LeashRadius`, etc., §9.30) |

**Editor manual groups (D-093 revises D-084 auto single-instance; still snaps over class zones)**

| Rule | Notes |
|------|-------|
| No auto-group | Formation changes, one-click deploy, and AutoManufacture `DeployBatch` do **not** pull idle soldiers into a group. Existing groups only prune: undeployed members leave; remaining **&lt; MinMemberCount** → disband the group back to the class-zone spiral (D-052) |
| Create | **Only** the left-edge catalog button (UI-030). One click creates one `GroupInstanceId`. The same `FormationId` may have many groups at once |
| Eligibility | **In no group**, and **any** `SoldierSkills` entry whose `SkillConfig.FormationId` equals the clicked formation (do not stop at the first formation skill). Candidate order: deployed matches first (deploy order / `WarriorId`), then undeployed SoldierBar matches (pool order). The eligibility query stays separate from group id / save / combat key; it does **not** drop candidates by class |
| Headcount | Create only if candidates **≥ MinMemberCount**; take `min(candidates, MaxMemberCount, slot count)`. **Slot assignment** (who joins and where they stand) draws from the **full** candidate list and does not truncate first: by slot index, `TacticalFormationSlot.PreferredClass` (= `ClassConfig.BaseClass`; missing component / `Unspecified` = no preference) picks the not-yet-assigned member with matching `BaseClass` and highest `ClassLevel` (ties keep candidate order, deployed first); a slot with no preference or no match takes the next leftover candidate. Only bar soldiers who receive a slot are deployed. Combat slot fill does **not** read PreferredClass. A partial Max is valid. Below Min **does not move** positions and **does not deploy**. If a bar deploy fails: undeploy everyone this click just deployed; if the remaining deployed candidates are still ≥ Min, reassign from those deployed candidates only, otherwise the click fails |
| One group | A soldier already in a group is skipped and cannot belong to another formation at the same time |
| Snap | Center = centroid of the **deployed** members taken this click; if none are deployed, the caller-supplied view-center ground point (map-relative XZ), and a missing point fails without moving anyone. Facing = PushMap toward current/first Objective, Defend toward EngageZone interior / default +Z. Undeployed members deploy onto their slot coords, then all taken members are written together; **overrides** the class-zone spiral |
| Drag | Members **cannot** be dragged individually; hit any member → **whole group** drag (center only). Key = `GroupInstanceId`, not `FormationId` |
| Rotate (Q/E) | While LMB **holding/dragging** the group: `Q` = CCW, `E` = CW, **`FormationRotateStepDegrees=15`** per press about the current center; writes member coords + `FacingYawDegrees`. Q/E **ignored** when not holding a group |
| Default 8-dir (D-095) | A grouped member's formation-preview 8-dir = that group's `FacingYawDegrees` quantized to 45° sectors (0°=world +Z=north). The preview updates on join and when `Q`/`E` changes facing; the sprite does not change until a sector boundary is crossed. Ungrouped previews and soldier-bar thumbnails stay fixed south (`DirIndex=2`) |
| Facing keep | A new group auto-faces the map target; pruning an existing group **keeps** `FacingYawDegrees` (including player rotate) |
| Editor scroll zoom | While the shared `FormationEditor` is open: mouse wheel zooms; step/clamp same as Combat; ignore when the pointer is over the soldier bar or other blocking UI |
| Catalog UI-030 | Left edge lists every formation in the table (one button per `FormationId` across levels); click only tries to create a group |
| Group cards UI-035 | Top center, one card per group; viewport shows at most 8, scroll both ways past that. Click selects that `GroupInstanceId`: soldier bar highlights only its members; formation-preview puppets show the formation icon overhead (not in combat); Disband under the selected card returns members to class zones and deletes the group |
| Save | Groups (`GroupInstanceId`, `FormationId`, member ids, facing) persist with `BattleFormation`. Level is not stored; recompute on load. Old saves with no group field = no groups; do **not** infer groups from standing positions |
| vs bonds | Parallel; bond stats unchanged |

**Combat: virtual center + FormationSlot + hold (D-094 revises D-084 engage leash)**

| Rule | Notes |
|------|-------|
| StartBattle snapshot | Each group locks its own center, facing, member↔slot map, pattern move params (including `FrontArcDegrees`), and the `FormationLevel` row computed from current members. Runtime key = `GroupInstanceId` (do not use `FormationId` as the instance key) |
| Center | One virtual center per group. PushMap: `Tick` integrates along FlowField dir (speed = that group's representative `MoveSpeed × CenterMoveSpeedMul`); Defend: hold that group's deploy center. The group still follows the center; it is not frozen in world space |
| Members | `GoalKind=FormationSlot`; destination = center + rotated slot local offset. Engage does **not** switch to `AttackSlot` and does **not** claim an attack ring. Until `SlotArriveEpsilon`, keep seeking the slot. While holding, that member's incoming soft-collision correction is **0** (they may still push others). Presentation facing (D-095): walk and idle follow that group's `FacingYawDegrees`; attack start still snaps toward the enemy once, then returns to formation facing. Gait still comes from steer |
| Front attack | Once on the slot, pause and use the existing hit only when the enemy center is inside the **group facing** forward `FrontArcDegrees` (default **90**, 45° each side) and within that soldier's `AttackRange`. **D-096:** outside the arc, body contact (XZ center distance ≤ sum of both `BodyRadius`) also allows an in-place swing; no chase, keep `GoalKind`; attack start still snaps toward the enemy once. Several candidates → nearest among in-arc-in-range and contact-outside-arc. Outside the arc without contact, or inside the arc but out of range: do not chase. Reach the slot before swinging |
| Leash | `LeashRadius` stays on the Pattern and **no longer** decides leaving the slot or whether to swing |
| Slot fill | After a death or Rebel vacates a slot, if the group is still ≥ `MinMemberCount`: the living member with the highest slot index steps into that lower index when their index is greater. Index = `Slot_*` name order (`Slot_00` = 0). If the vacated index is already the highest, the high slot stays empty — **do not** compact everyone forward. Several vacancies in one frame fill from the lowest index upward. Do not write Prepare coords |
| Rebel | Leave squad (no bonuses); the vacated slot fills as above; existing Rebel targeting |
| Dissolve | Living members of **that group** (non-Rebel, non-CombatDead) **&lt; MinMemberCount** → dissolve only that group with no fill: drop overlay; fallback PushMap `Objective` or Defend personal Home (**current world pos as new Home**). Other groups stay |
| No Prepare rewrite | Combat death / dissolve / fill does **not** write back Prepare coords |
| Ungrouped | Still use AttackSlot / Objective / FormationHome |

**Stat & exclusive-skill bonuses**

| Rule | Notes |
|------|-------|
| Window | That group only while active. StartBattle computes level from current members and locks the row; after a death / Rebel, if the group is still ≥ Min, recompute level from **remaining** members and swap overlay; below Min, dissolve |
| Level | `Floor(sum of members' ClassConfig.ClassLevel / count)` (missing class row = 0; for positive ints this is integer division). The player does not pick the level |
| Lookup | Composite PK `(FormationId, FormationLevel)`. Use the **greatest table level ≤ computed level** (never round up). If no such row: the group still exists, `StatModifiers` empty, Warning |
| Stats | Matched row `StatModifiers`; Combat `StatMul`-style overlay, **multiplied** with magic-book Combat StatMul; **does not** mutate `WarriorInstance.BaseStats`. On tier change or dissolve, recompute derived stats from remaining Combat StatMul; clamp `RemainingHp` to new MaxHP |
| Exclusive skills | `ExclusiveSkillIds` / `ExclusiveSkillEffectIds` from the **same level row** (read-only merge, no instance `SoldierSkills` write, not persisted) |
| vs bonds | Stack OK; duplicate `SkillEffectId` → load-time Warning |

**Slices:** D-084 framework `.scratch/tactical-formation/issues/` TF-00–06 (landed). D-093 `.scratch/tactical-formation-groups/issues/` TFG-01–05 coded (pending playtest).

```
TryCreateGroup(formationId)
  → candidates = deployed matches (deploy order) then bar matches (pool order)
  → if candidates < Min → no-op (no deploy)
  → take = min(candidates, Max, slots)
  → assign those slots from the full candidate list (do not truncate first):
       preferred slot → remaining same BaseClass, highest ClassLevel (tie = earlier candidate)
       unspecified / no match → next leftover candidate
  → center = centroid of assigned members who were already deployed, else view-center ground point
  → deploy only the assigned bar members; failed deploy rolls back this click's new deploys
  → if deployed candidates still ≥ Min → reassign from those deployed candidates only, else no-op
  → snap; new GroupInstanceId
  → level = Floor(mean ClassLevel); stat row = greatest FormationLevel <= level
Prune on deploy change
  → drop missing members; if remaining < Min → disband to class zones
StartBattle
  → one CombatLock per GroupInstanceId (center, facing, member→slot, level row)
OnMemberDeath / Rebel
  → if living >= Min → highest slot index fills a lower vacated index; recompute level and swap overlay
  → else dissolve that group only (no fill)
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
- [x] 士兵战斗（WarriorCombat）：EngageZone 最近选敌、AttackMode（SoulConfig）、AttackRange（ClassConfig）、命中方案 D、CombatDead / PermanentDeath / 宝石特例（§3.12）；**Demo D-069：PushMap Skill_03 连发 + Skill_01 格挡 + Skill_02 舒适**；**D-073：Skill_04～12 EffectKind 登记制（SE-00～09 完成）**（法师=远程+Intelligence，同射手通道）
- [x] 护盾（Shield）：开战取 `ProtagonistMaxHP`；普通攻击命中 −1（含叛变士兵）；归零 LevelFailure（§3.12）
- [x] 失控判定时机与叛变 AI（开战锁定 Degree；就近目标；技能二次完整率 roll——**D-069 成功施放后若 ΣSkillBonus≠0 可触发**）（§3.11 / §3.12）
- [ ] 防守阶段结算其余字段；关卡失败结算 UI / 字段
- [x] PushMap / SearchExtract 怪物死亡复活技能（D-074 / §9.21c `MonsterSelfReviveOnDeath`）：共享 `IMonsterDeathSkillHost`；`MonsterCombatDead` + 彻底死亡才 `MonsterKilled`；SearchExtract 点清场跳过假死；Defend 后置
- [ ] 怪物技能效果表其余 EffectKind；技能命中主角是否扣盾；士兵复活技能（另专题；主动连发 + 格挡 + 舒适见 D-069；Skill_04～12 见 D-073）；精确 OutsideMap 出生几何（Demo 后置）
- [x] 科技树画布 Demo 垂直（方案 A：学习扣点 + Dig 能力重算可验；非 §3.8 P0）
- [ ] 科技树节点具体数值/图标 polish 与功能系统名完整枚举
- [x] Title 显示设置（UI-028）：分辨率 + 窗口/无边框/独占全屏；机台级 PlayerPrefs；与 UI-007 科技树分离
- [ ] 设置项清单其余（音量/画质/语言等仍 TBD；进档 UI-007 仍仅科技树）
- [ ] 存档完整字段（显示名、时间戳、局内进度等）
- [x] ToolsPanel Demo GM：增加主角装备 / 增加魔法书（D-061 / UI-019）
- [x] ToolsPanel Demo GM：添加士兵（D-064 / UI-020）
- [x] InSaveShell 装备仓只读弹窗（UI-022 / D-067）与魔法书槽排序（UI-023 / D-068）；实现 EM-01～03
- [x] InSaveShell 魔法书弹窗删除（UI-023 / D-072）：点占用槽 → 槽下「删除」→ 二次确认 `TryUnequip`
- [x] 士兵战斗技能施放 SkillCast（D-069）：PushMap `Skill_03` 连发（方案 C）+ `Skill_01` 格挡（方案 B）+ `Skill_02` 舒适（方案 A）
- [x] PushMap 士兵技能效果 Skill_04～12（D-073 / 方案 B+）：EffectKind 登记制 + CombatStatusService + Pipeline；SE-00～09 完成（含 Skill_12 瞬移）
- [x] 战斗技能图标 CombatSkillIcon（UI-025 / D-071）：PushMap 头顶飘 + `Skill_02` 脚下持续
- [ ] 战斗指示器 CombatIndicator（UI-033 / D-089）：PushMap + SearchExtract Combat 顶中敌我单位格；issues `.scratch/combat-indicator/`
- [ ] 离屏刷怪边缘提示 OffScreenSpawnHint（UI-034 / D-090）：屏外 SpawnPoint 边缘 `EnemyAttack_1`；issues `.scratch/offscreen-spawn-hint/`
- [x] 沙盘入口 Sandbox（UI-036 / D-098）：难度点击 / 占用档 / 结束进沙盘；方格进挖坟、商店、自动造兵或 COC 占位；issues `.scratch/coc-sandbox/`。次数只展示与 COC 占位由 D-099 修订
- [ ] COC 战斗（UI-037 / UI-038 / D-099）：洒兵、战场迷雾、占领点、胜负与沙盘剩余次数；issues `.scratch/coc-combat/`。空地图进入（切片 02）、刷怪待机（切片 03a，方案 A）、洒兵战斗（切片 03b，方案 A）、走向最终 BOSS（切片 03c，方案 A）与胜负（切片 03d，方案 A）、静态迷雾（切片 04a，方案 A）、临时亮起/变暗（切片 04b，方案 A）、占领（切片 05，方案 A）、长按展开（切片 06）已落地
- [x] 战术阵型 TacticalFormation 框架（§3.18 / D-084）：GrantFormationSkill、TacticalFormationConfig、Pattern Prefab、虚拟中心+FormationSlot+leash、激活/解散 overlay；TF-01～06 已落地。**自动、每种最多一组**由 D-093 修订
- [x] 布阵战术阵型目录条（UI-030 / D-085）：原左侧已 snap 小队点选已接线；行为改为 D-093 创建按钮
- [ ] 战术阵型多组与等级（UI-030 / UI-035 / D-093）：手动多组、等级查表、左缘目录、顶中卡牌、按组战斗已编码（TFG-01～05）；待验收
- [x] 推图战（PushMap）框架：GameplayType、目标点/判定圈占领、空气墙、刷怪点/陷阱、BOSS 通关、AggroMode、复用 Defend 护盾/失控（§3.14）
- [x] Mode2 自动制造（AutoManufacture）规则关闭：流水线 Dig→AutoManufacture→UM；最低配方头+躯干+双臂（含主要手）+双腿；近似品质 |Δ|≤1；职业由双手 ClassRestrict；不计 Spirit/Control；无 SoulId；魔法书表+6槽+钩子骨架；清空布阵后按 PlacementOrder/职业区上阵（§3.15）
- [x] Mode2 制造记录弹窗（UI-015 / D-054）：最近一批只读摘要；布阵右侧入口；方案 A `AutoManufactureBatchRecordService`
- [x] Mode2 自动制造演出（UI-016 / D-055）：Step1–3；方案 A `AutoManufacturePresentationController` + UM 自动开布阵
- [x] Mode2 魔法书「还原」`RaceWeightPick`（种族定稿前探测）
- [x] Mode2 魔法书「战士强化」`StatMul`/`Primary`（D-058；可叠；Dig HUD GM 装备）
- [x] 士兵技能体系框架关闭：`SkillConfig` 为士兵技能权威表；职业 `DefaultSkillIds`；实例 `SoldierSkills`；Mode2 `SoldierSkillLevelAdd` 只升已有技能（§3.11 / §3.15 / SPEC_04 §9.21）
- [x] 士兵技能垂直 D-062（SS-01～04：表加载 + 池持久化 + Mode1/Mode2 授予）；战斗施放见 D-069
- [x] Mode2 魔法书职业进阶 D-063（`ForceClass` + `RequireClassId`/`Chance`；四本 `MagicBook_*Advance`）
- [ ] 全职业 DefaultSkillIds（Demo 样例仅战士=`Skill_01`，见 SS-01）；CastTarget 第七枚举名（样例书 `MagicBook_SoldierSkillLevel` 已由 SS-04 落地）
- [ ] `SoldierSkills` 与灵魂/宝石 `Skills` 同 Id 时的合并规则
- [ ] Mode2 魔法书从弹窗装入与其余效果行；灵魂手动装配（槽排序见 D-068；弹窗删除见 D-072；§3.15 另专题）
- [x] 主角装备（ProtagonistEquipment）规则关闭：装备仓库 / 同 Id 转化 / EquipCommonExp / 等级 / Dig 与科技加法叠加（§3.16 / SPEC_04 §9.25）；获取来源与制造·战斗 Token **TBD**
- [x] 主角装备 Dig 垂直 D-059（PE-01～PE-04：表加载 + 仓/存档 + Dig caps 合并 + Dig HUD GM）
- [x] 主角装备矿灯 D-060（PE-05～PE-08：SPEC + 5 级表行 + 生成权重活叠加 + Dig HUD GM）
- [x] 主角装备炸药 D-077（EXP-01～03：事件 Token + 调度/AoE + 抛物线/`ZYT_1`/贴地红圈 + Dig HUD GM）
- [x] 主角装备引雷 D-078（事件 Token + 定时落雷清坟入兵 + 序列帧/待机预览 + Dig HUD GM）
- [x] 主角装备探测器 D-079（静态键 `DigProcessSpawnCountBonus` + 过程生成 M 加法 + ItemCatalog/商店池 + Dig HUD GM）
- [x] 主角装备种族信物 D-080（`GraveSpawnWeightBonus` Q16–Q27 品质带 + ItemCatalog/商店池 + Dig HUD GM；Mode2 Prefab 至 Q27）
- [ ] 主角装备获取来源（Dig 掉落 / 商店等）与升级/划公共经验/卸下 UI（仓只读见 D-067；GM 手验属 D-059 / D-060 / D-077 / D-078 / D-079 / D-080）
- [ ] 主角装备 `SoldierManufacture` / `Combat` 效果 Token 登记表
- [x] PushMap 边界锁定：到达 `CaptureZone` 即占领（无计时/无「无怪」条件）；占领后已刷怪保留；全队共当前目标；无陷阱开战刷；无倒计时刷怪；仅 BOSS 通关入账经验；MapId=`Ground_*`|`PushMap_*`
- [x] 大规模战斗寻路（方案 B）规则锁定：FlowField（共享目标）+ AttackSlot（追击/攻击）+ LocalDetour（友军左右绕）；容量双方约 200；实现见 `.scratch/mass-pathing/issues/`（§3.12 / SPEC_04 §9.7）
- [ ] 推图战副本玩法正文
- [ ] 陷阱刷怪是否允许配置「可重复触发」
- [x] 大规模寻路实现切片（MP-01～MP-07）与 200v200 压测验收（入口已落地；约定机型 Stopwatch 数字见 `.scratch/mass-pathing/issues/07-perf-stress-200.md`）
- [x] 搜打撤框架（§3.19 / D-087）：有序搜集点、进圈倒计时、方向波次刷怪、布阵重定位、继续/离开 UI；方案 A 独立 Session；**配置列已锁定**（工作坊 2026-09-02）；issues `.scratch/mode3-search-extract/`；**SE-01～SE-09 已落地**（点奖励 + Continue/Leave + 全灭 → AbortLevel + LevelSelect；D-087 Demo 手验仍须负责人勾选）

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
- [x] WarriorCombat: EngageZone nearest target, AttackMode (SoulConfig), AttackRange (ClassConfig), hit scheme D, CombatDead / PermanentDeath / gem exception (§3.12); **Demo D-069: PushMap Skill_03 burst + Skill_01 block + Skill_02 Comfort**; **D-073: Skill_04–12 EffectKind registry (SE-00–09 done)** (Mage = Ranged+Intelligence, same channel as Archer)
- [x] Shield: StartBattle = `ProtagonistMaxHP`; normal hit −1 (incl. Rebel); 0 → LevelFailure (§3.12)
- [x] LossOfControl timing and Rebel AI (lock Degree at StartBattle; nearest target; extra full-rate roll after skill cast — **D-069 fires when ΣSkillBonus≠0**) (§3.11 / §3.12)
- [x] Demo acceptance expanded: Meta shell + Dig→UM→Defend pipeline (SPEC_03 §3.8 D-001～D-043); UM `GameplayConfigId` = ignore
- [ ] Defend settlement other fields; LevelFailure settlement UI / fields
- [x] PushMap / SearchExtract monster death-revive (D-074 / §9.21c `MonsterSelfReviveOnDeath`): shared `IMonsterDeathSkillHost`; `MonsterCombatDead` until true death `MonsterKilled`; SearchExtract point-clear skips fake death; Defend unwired
- [ ] Monster skill-effect table remaining EffectKinds; whether skill hits reduce Shield; soldier revive (later; active burst + block + Comfort = D-069); exact OutsideMap spawn geometry (post-Demo)
- [x] TechTree canvas Demo vertical (Approach A: spend+learn + Dig caps recalc verifiable; not §3.8 P0)
- [ ] Concrete tech-node values/icon polish & full feature-system enum
- [ ] Settings item list (TechTree entry closed; other settings TBD)
- [ ] Full save fields (name, timestamp, progress, etc.)
- [x] ToolsPanel Demo GM: Grant Protagonist Equipment / Grant MagicBook (D-061 / UI-019)
- [x] ToolsPanel Demo GM: Add Soldier (D-064 / UI-020)
- [x] InSaveShell equipment warehouse read-only popup (UI-022 / D-067) and MagicBook slot reorder (UI-023 / D-068); impl EM-01–03
- [x] InSaveShell MagicBook popup delete (UI-023 / D-072): click occupied slot → Delete under slot → confirm `TryUnequip`
- [x] Soldier SkillCast (D-069): PushMap `Skill_03` burst (Approach C) + `Skill_01` block (Approach B) + `Skill_02` Comfort (Approach A)
- [x] PushMap soldier skill effects Skill_04–12 (D-073 / Approach B+): EffectKind registry + CombatStatusService + Pipeline; SE-00–09 complete (incl. Skill_12 Blink)
- [x] CombatSkillIcon (UI-025 / D-071): PushMap overhead popup + `Skill_02` foot persist
- [ ] CombatIndicator (UI-033 / D-089): PushMap + SearchExtract Combat top-center ally/enemy unit slots; issues `.scratch/combat-indicator/`
- [ ] OffScreenSpawnHint (UI-034 / D-090): off-viewport SpawnPoint edge `EnemyAttack_1`; issues `.scratch/offscreen-spawn-hint/`
- [x] Sandbox entry (UI-036 / D-098): difficulty click / occupied enter / end open the Sandbox; cells enter Dig, Shop, AutoManufacture, or the COC placeholder; issues `.scratch/coc-sandbox/`. Display-only counts and the COC placeholder are revised by D-099
- [ ] COC combat (UI-037 / UI-038 / D-099): deploy soldiers, battlefield fog, capture points, win/loss, and Sandbox remaining counts; issues `.scratch/coc-combat/`. Empty-map enter (slice 02), spawn-idle (slice 03a, Approach A), deploy-fight (slice 03b, Approach A), Final Boss march (slice 03c, Approach A), win/loss (slice 03d, Approach A), static fog (slice 04a, Approach A), reveal/explored-dark (slice 04b, Approach A), capture (slice 05, Approach A), and card-hold expand (slice 06) have landed
- [x] TacticalFormation framework (§3.18 / D-084): GrantFormationSkill, TacticalFormationConfig, Pattern Prefab, virtual center+FormationSlot+leash, activate/dissolve overlay; TF-01–06 landed. **Auto, max one group per formation** revised by D-093
- [x] Formation catalog strip (UI-030 / D-085): original snapped-squad select is wired; behavior becomes the D-093 create button
- [ ] Tactical multi-group + level (UI-030 / UI-035 / D-093): manual groups, level lookup, left-edge catalog, top-center cards, and per-group combat coded (TFG-01–05); pending playtest
- [ ] Remaining ToolsPanel entries / polish
- [x] PushMap framework: GameplayType, objectives/CaptureZone, AirWall, SpawnPoint/Trap, Boss clear, AggroMode, reuse Defend Shield/LOC (§3.14)
- [x] Mode2 AutoManufacture rules closed: Dig→AutoManufacture→UM; min recipe Head+Torso+2Arm(incl PrimaryHand)+2Leg; approx |Δ|≤1; class from hand ClassRestrict; no Spirit/Control; no SoulId; MagicBook schema+6 slots+hook stub; clear formation then PlacementOrder/class-zone deploy (§3.15)
- [x] Mode2 ManufactureRecord popup (UI-015 / D-054): last-batch read-only summary; entry right of Formation; Approach A `AutoManufactureBatchRecordService`
- [x] Mode2 AutoManufacture presentation (UI-016 / D-055): Step1–3; Approach A `AutoManufacturePresentationController` + UM auto-open Formation
- [x] Mode2 MagicBook Restore `RaceWeightPick` (probe before race finalize)
- [x] Mode2 MagicBook Warrior Enhance `StatMul`/`Primary` (D-058; stackable; Dig HUD GM equip)
- [x] Soldier-skill framework closed: `SkillConfig` is soldier-skill authority; class `DefaultSkillIds`; instance `SoldierSkills`; Mode2 `SoldierSkillLevelAdd` only raises existing skills (§3.11 / §3.15 / SPEC_04 §9.21)
- [x] Soldier-skill vertical D-062 (SS-01–04: table load + pool persist + Mode1/Mode2 grant); combat cast = D-069
- [x] Mode2 MagicBook class advance D-063 (`ForceClass` + `RequireClassId`/`Chance`; four `MagicBook_*Advance`)
- [ ] Full-class DefaultSkillIds (Demo sample Warrior=`Skill_01` only, SS-01); 7th CastTarget enum name (sample book `MagicBook_SoldierSkillLevel` landed in SS-04)
- [ ] Same-Id merge between `SoldierSkills` and Soul/Gem `Skills`
- [ ] Mode2 MagicBook grant-from-popup + remaining effects; manual soul attach (slot reorder = D-068; popup delete = D-072; §3.15 later)
- [x] ProtagonistEquipment rules closed: equipment warehouse / same-Id convert / EquipCommonExp / levels / Dig additive with tech (§3.16 / SPEC_04 §9.25); acquire sources and Manufacture/Combat tokens **TBD**
- [x] ProtagonistEquipment Dig vertical D-059 (PE-01–PE-04: table load + warehouse/persist + Dig caps merge + Dig HUD GM)
- [x] ProtagonistEquipment Miner Lamp D-060 (PE-05–PE-08: SPEC + 5-level rows + live spawn-weight overlay + Dig HUD GM)
- [x] ProtagonistEquipment Explosives D-077 (EXP-01–03: event token + scheduler/AoE + parabola/`ZYT_1`/ground ring + Dig HUD GM)
- [x] ProtagonistEquipment Lightning D-078 (event token + timed strike/clear/grant + sequence/idle preview + Dig HUD GM)
- [x] ProtagonistEquipment Detector D-079 (static key `DigProcessSpawnCountBonus` + process-spawn M bonus + ItemCatalog/shop pool + Dig HUD GM)
- [x] ProtagonistEquipment race tokens D-080 (`GraveSpawnWeightBonus` Q16–Q27 bands + ItemCatalog/shop pool + Dig HUD GM; Mode2 Prefabs through Q27)
- [ ] ProtagonistEquipment acquire sources (Dig loot / shop) and level-up / spend common Exp / unequip UI (warehouse read-only = D-067; GM handcheck is D-059 / D-060 / D-077 / D-078 / D-079 / D-080)
- [ ] ProtagonistEquipment `SoldierManufacture` / `Combat` effect Token registry
- [x] PushMap boundary locks: Capture on arrive to `CaptureZone` (no timer / no “clear monsters” condition); keep living after Capture; shared current objective; non-trap spawn at StartBattle; no countdown spawn; Exp only on Boss clear; MapId=`Ground_*`|`PushMap_*`
- [x] Mass combat pathing (Approach B) rules locked: FlowField (shared goals) + AttackSlot (chase/attack) + LocalDetour (friendly L/R); ~200/side capacity; impl `.scratch/mass-pathing/issues/` (§3.12 / SPEC_04 §9.7)
- [ ] PushMap dungeon gameplay body
- [ ] Whether trap spawns may be configured as re-triggerable
- [x] Mass pathing implementation slices (MP-01–MP-07) and 200v200 stress acceptance (entry shipped; agreed-machine Stopwatch numbers in `.scratch/mass-pathing/issues/07-perf-stress-200.md`)
- [x] SearchExtract framework (§3.19 / D-087): ordered gather points, zone countdown, directional wave spawns, formation relocate, continue/leave UI; Approach A independent Session; **config columns locked** (workshop 2026-09-02); issues `.scratch/mode3-search-extract/`; **SE-01–SE-09 landed** (point loot + Continue/Leave + wipe → AbortLevel + LevelSelect; D-087 Demo handcheck still needs owner)

---

## 3.19 搜打撤（SearchExtract）

### 简体中文

**状态：规则已锁定（方案 A：独立 Session；复用 PushMap 地图标记与 §3.12 战斗管线）；Demo 实现须另授权并拆切片（见 `.scratch/mode3-search-extract/issues/`）。**

当关卡当前阶段子关卡选项 `GameplayType = SearchExtract` 时进入本阶段。**挂载于 Mode2**（`CampaignMode=Mode2`；存档、自动制造、配置根仍 Mode2）；**不是**第三个 `CampaignMode`。**勿与** §3.14 PushMap「进圈即占领」、§3.12 Defend 全局倒计时刷怪（`WaveSpawnConfig`）混淆。

依赖 §3.11 **战斗布阵**。配置见 [SPEC_04 §9.32](SPEC_04_Technical.md) `SearchExtractGameplayConfig`、[§9.33](SPEC_04_Technical.md) `SearchExtractWaveSpawnConfig`、[§9.31](SPEC_04_Technical.md) 子关卡 `GatherPointCount` / `GatherPointRewards`（**列名已锁定，工作坊 2026-09-02**）、[§9.19](SPEC_04_Technical.md) `MonsterConfig`。

**与 PushMap / Defend 的关系（方案 A 复用边界）**

| 复用 | 说明 |
|------|------|
| 子状态 | `Prepare` → `Combat` → `Ended`（`SearchExtractPhase`；语义对齐 PushMapPhase） |
| 布阵 / 开战 | 同一 `FormationEditor`；开战须 ≥1 上阵；控制力超额允许开战 |
| 地图标记 | `ObjectivePoint` + `CaptureZone`、`AirWall`、`SpawnPoint`、`EngageZone` / `WalkSurface`（作者约束同 §3.14） |
| 士兵战斗 | 同 §3.12 WarriorCombat + MassCombatPathing + §3.18 战术阵型（倒计时中各组阵心相对全体上阵质心平移到当前搜集 Objective）；刷出怪物必须进入同一 MassMove Tick 与 AttackSlot 追击刷新（同 PushMap；禁止仅靠 NavMeshAgent 自主位移） |
| 怪物死亡技能 | 同 §3.14 D-074：Session 实现 `IMonsterDeathSkillHost`，登记 `MonsterConfig.Skills`（样例 `Monster_01`=`MSkill_SelfRevive_99`）；HP≤0 可假死复活。**点清场**规则层彻底死亡，不拦截复活 |
| 镜头 | 正交战斗相机同 PushMap；**进圈前 / Continue 后接近** = `CameraFollowPath` 轨跟随；**点激活～GatherCountdown** = **HoldFraming**（视口包围+迟滞；见下「倒计时战斗镜头」）；**不**改 PushMap 默认轨语义（[SPEC_04 §6](SPEC_04_Technical.md)/[§9.20b](SPEC_04_Technical.md)） |
| 战斗指示器 | 同 §3.14 UI-033 / D-089：`Combat` 显示共享 `CombatIndicatorHud`；Prepare / Ended / UI-017 / UI-032 决策期间隐藏；可复活怪算活着 |
| 离屏刷怪边缘提示 | 同 §3.14 UI-034 / D-090：Combat 真实刷怪且 `basePos` 视口外 → 边缘 `EnemyAttack_1` 闪红 2 次+常亮 2s、×1.3；Prepare / Ended / UI-017 / UI-032 清空 |
| 不复用 | PushMap 进圈 instant Capture、开战瞬间 `PushMapSpawnConfig`、BOSS 通关、Defend `WaveSpawnConfig` 全局剩余秒、PushMap `Shield≤0` 失败 |
| 失败 | **仅** 搜集中忠诚兵全灭 → **整关** `LevelFailure` → UI-017 战败（返回主界面 / 重新开始；不清档） |
| 护盾 | Demo **默认不驱动**战斗主角护盾失败（不放 `BattleProtagonist` / 不扣 `Shield`）；失控/Rebel 仍开战锁定 |

**阶段内子状态（SearchExtractPhase）**

| 子状态 | 说明 |
|--------|------|
| `Prepare` | 加载地图与布阵；可编辑布阵；含「开战」；不可制造 |
| `Combat` | 部署后推进搜集点链；进圈激活倒计时与刷怪；布阵中心重定位与战斗并行 |
| `Ended` | 「离开」子关卡通关，或 LevelFailure |

**搜集点链（有序；对齐 PushMap `ObjectiveOrder`）**

| 规则 | 说明 |
|------|------|
| 数量 N | 由当前子关卡选项配置决定，**1～N**；运行时取地图 `ObjectiveOrder` 升序前 **N** 个 `ObjectivePoint` |
| 当前点 | 开战后 = 最小未完成 Order；单点成功后 Continue → 下一未完成 Order |
| 开战至进圈前 | StartBattle 后、当前点**尚未**激活时：全队忠诚兵以当前 `ObjectivePoint` 世界 XZ 为 `FormationHome` 目标接近（DesiredDestination；**不**要求 FlowField）；**不**启动搜集倒计时 / 刷怪 / 阵型偏移 relocate |
| 激活 | **任一**忠诚士兵进入当前点 `CaptureZone` → **首次**激活该点「搜集倒计时」并启动该点刷怪资格（**不是** PushMap Capture） |
| 重复进圈 | 已激活且未完成的点：重复进圈 **不**重置倒计时、 **不**重复激活 |
| 推进 | 单点胜利后须玩家点「继续搜集」才切换当前点（非自动链式推进） |

**搜集倒计时与刷怪**

| 规则 | 说明 |
|------|------|
| 倒计时 | 规则层 `GatherCountdownSeconds`（仅 §9.32 玩法表全局；§9.33 **无** per-point 覆盖）；自首次激活起递减；归零时判定单点胜负 |
| HUD | Combat **顶栏**显示剩余整秒（Demo；实现后置 SE-04） |
| 与就位关系 | 进圈瞬间 **同时**启动倒计时与各行刷怪配方的 **首刷前置**计时；布阵重定位 **并行**，**不等**全员就位才开刷 |
| 刷怪驱动 | `SearchExtractWaveSpawnConfig`：按 `GameplayConfigId` + `GatherPointOrder` + `WaveIndex` **一行一配方**（`WaveIndex` 仅为同点复合键，**不**串行链式）；点激活后各行**独立**计时：`FirstWaveDelaySeconds` 后**首刷**本行 `MonsterId`×`SpawnCount`；其后每过 `WaveIntervalSeconds` **再刷同一行**一次，共重复 `RepeatSpawnCount` 次（总刷次数=`1+RepeatSpawnCount`）；`Repeat=0` 则只首刷（Interval 可填 0）；`Repeat>0` 须 Interval>0；同点各行 Delay/Interval/Repeat **可不同** |
| 方向 | 仅地图 `SpawnPointId`（Demo **不**用 Defend `ClockDirection`）；缺标记 → 该波跳过并 Warning |
| 停刷 | 单点胜利瞬间：该点 **不再**新刷；场上存活怪 **立即彻底死亡**（规则层清场，**不**走 `MonsterSelfReviveOnDeath`；已假死取消复活、不二次击飞；非击杀计数） |
| 掉落 | 搜集中手动杀怪 **无**额外掉落（**不**走 `MonsterConfig` Loot）；仅倒计时胜利清场 |
| 禁止 | **不**使用 Defend `RemainingCombatSeconds` / `WaveSpawnConfig` 全局倒计时；**不**使用 PushMap 陷阱/BOSS 通关语义 |

**布阵中心重定位（刷怪前/进行中）**

| 规则 | 说明 |
|------|------|
| 时机 | **点激活之后**（与倒计时/刷怪并行）；开战未进圈阶段仅「接近 Objective 中心」，见上表 |
| 中心 | 当前 `ObjectivePoint` 世界 XZ 为 **布阵中心** |
| 目标位 | 各忠诚兵趋近 `开战 BattleFormation` 相对该中心的世界偏移（`PositionX/Z − FormationHome` 或等价快照） |
| 空气墙 | 路径遇 `AirWall` 停在最后可走点；**不**穿墙 |
| 战术阵型 | 倒计时重定位期间：全体上阵质心对齐当前 Objective；每组阵心 = Objective +（该组开战世界阵心 − 该质心）。开战世界阵心 = 地图中心 + 布阵锁里的地图相对阵心。成员 `GoalKind=FormationSlot` 仍为该组阵心 + 旋转槽位，因此保持布阵时相对全体质心的偏移（多组不叠在同一点）。进圈前的接近阶段不走此平移 |
| 与战斗 | 重定位与遇敌 AttackSlot **并行**；PushMap 遇敌检测语义可复用 |

**倒计时战斗镜头（HoldFraming；方案 B）**

| 规则 | 说明 |
|------|------|
| 时机 | **仅**当前搜集点已激活且 `GatherCountdown` 进行中；StartBattle→进圈前 / Continue 后接近下一点 → **轨跟随**（同 PushMap `CameraFollowPath` 最大投影） |
| 目标 | 半径内忠诚存活士兵尽量入镜；正交 Size 适中；**禁止**高频平移/拉伸（防抖） |
| 输出 | 仅 `look-at`（世界 XZ）+ `orthographicSize`；姿态仍走 `ApplyCombatCameraPose` |
| 钳制 | Size ∈ `[SearchExtractHoldOrthoSizeMin, Max]`（再与全局 `CameraOrthoSizeMin/Max` 取交）；look-at 距当前 **Objective** 世界 XZ ≤ `SearchExtractHoldMaxPanRadius` |
| 采样 | 忠诚存活（`!IsRebel` 且非 `CombatDead`）且距 Objective ≤ 半径；**圈外兵允许出镜、不追**；**不**框选怪物 |
| 入镜 | **相机视口坐标** AABB（`WorldToViewportPoint`；因 60° 倾角，**禁止**用世界 XZ 四向极值当入镜依据） |
| 迟滞 | 外框（含边垫 + 顶 HUD 垫）越出 → **立刻**拉远/平移；全体在内框内连续满 `HoldZoomInDelaySeconds` → 才允许慢收紧；拉远快、拉近慢 |
| 中心 | look-at = 士兵视口 AABB 反投中心与 Objective **加权**（`SearchExtractHoldObjectiveBias`；1=钉 Objective）；激活瞬间**不 Snap**，以当时机位 SmoothDamp 过渡 |
| UI-032 | 决策期间 **冻结**最后一帧 Hold 目标（清场聚拢不突然拉近）；Continue → 关 Hold；**强制** `CameraFollowMode=Auto`；look-at 回 `CameraFollowPath` 最大投影（`CameraFollowDeadzone` / `CameraFollowSmoothTime`，**不 Snap**）；`orthographicSize` 以同一 `CameraFollowSmoothTime` **SmoothDamp** 恢复为 `PushMapCameraOrthoSize`（与推图开战默认 Size 同源；滚轮改 Size 则取消本次恢复）；Leave / Ended / 全灭 → Disable |
| Manual | 拖拽仍进 Manual；「恢复跟随」在 Hold 窗口 → **HoldFraming**（非轨）；滚轮改 Size 后 Hold **不**抢回（同 PushMap） |
| 常量 | 全部 ← Mode2 `CombatConstantConfig`（[SPEC_04 §9.20b](SPEC_04_Technical.md)）；禁止硬编码 |
| 不做 | 改 PushMap Combat 轨跟随；框选怪物；改搜集/刷怪规则 |

**单点胜利**

| 规则 | 说明 |
|------|------|
| 条件 | 搜集倒计时 **结束** 且场上 **≥1** 忠诚士兵存活（`!IsRebel && !CombatDead`） |
| 效果 | ① 全体忠诚士兵 **无敌**（`CombatStatusService`）；② 停刷；③ 存活怪立即死亡；④ 入账 **该点** 奖励（经 `ItemCatalogConfig`）；⑤ 弹出 **UI-032** |
| UI-032 | 底中「继续搜集」「离开」；**最后一点** 隐藏/禁用「继续搜集」，仅「离开」 |
| 决策停步 | UI-032 弹出后全体忠诚兵延迟 `SearchExtractDecisionIdleDelaySeconds`（§9.20b；默认 **1s**）→ 暂停 MassMove → Idle；Continue / Leave / 全灭时取消计时；Continue 后解除暂停以便再走位 |
| 单点自动离开 | **仅当**本关 `GatherPointCount=1`：LeaveButton 文案 `离开 (N)` 倒计时 `SearchExtractDecisionAutoLeaveSeconds`（默认 **3s**）归零后等价点击 Leave；玩家可随时手动 Leave 抢先；`N>1`（含最后一点）**不**自动离开 |
| Continue | 解除无敌；**不**复活 `CombatDead`；当前点标记完成；以 **下一** Objective 为布阵中心再走位；下一激活仍须 1 兵进圈 |
| Leave | 子关卡通关 → 若 §9.32 `StageExpReward`>0 则 `AddExperience` → **UI-017 胜利** → Continue → §3.9 发放子关卡行 `Reward`（若有）/ 解锁 / 回路线；**离开时不补发**已入账点奖励 |

**失败与经验**

| 规则 | 说明 |
|------|------|
| 全灭 | 当前搜集 **进行中**（已激活、倒计时未结束或 UI-032 未选）且 **无** 忠诚存活 → **整关** LevelFailure → **UI-017 战败**（阵亡总数；「返回主界面」→ TitleMenu；「重新开始」→ Abort 后重进同选项）；**不**静默 Abort |
| 重开 | UI-017「重新开始」：`AbortLevel` 后立刻重进同 `LevelId`+`OptionId`（Prepare）；**不清**存档槽；已 PermanentDeath 不复活 |
| 经验 | Leave 时入账 `StageExpReward`（§9.32；`≥0`；**0=不发**）后弹 **UI-017 胜利**（阵亡总数+四职业；可隐藏击杀行）→ Continue → 子关卡 `Reward` / 解锁 / 回路线（§3.9）；点奖励与子关卡 `Reward` 仍分离；全灭 **不**入账本阶段经验 |

**配置职责（列名已锁定；工作坊 2026-09-02）**

| 层 | 职责 |
|----|------|
| `SubLevelConfig` | `GameplayType=SearchExtract`；`GatherPointCount`（int N）；`GatherPointRewards`（`N:ItemId;Count\|…`，`\|` 分段且 `N:` 开头为新点） |
| `SearchExtractGameplayConfig` | `GameplayConfigId` → `MapId`、`GatherCountdownSeconds`（全局）、`StageExpReward` |
| `SearchExtractWaveSpawnConfig` | 一行一配方：`GatherPointOrder` + `WaveIndex` + `SpawnPointId` + Delay/Interval/`RepeatSpawnCount` + `MonsterId`/`SpawnCount` |
| 地图 Prefab | Objective 数 ≥ N；Objective 不得落 AirWall 内（§3.14 作者硬约束）；Demo 权威图 `SearchExtract_Lv1_01`（另保留 `SearchExtract_Demo_01` 作 SE-02 方案 B 参考副本，自 `PushMap_Demo_01` 复制，**不**改写 PushMap 原图）；图须含 `FormationClassZone`（Mode2 全 ClassId；供 Prepare「一键上阵」D-074；**不**回写 PushMap 原图）；运行时须 `DefendPrefabCatalog.Maps` 绑定 |

```
Enter SearchExtract (SubLevel GameplayType=SearchExtract)
  → SearchExtractPhase = Prepare
  → Instantiate Prefabs/Maps/{MapId}; load markers; take first N objectives by Order
  → Edit BattleFormation (one-click needs FormationClassZone) → StartBattle (≥1 deployed)
  → Combat: CurrentGatherPoint = min incomplete Order
  → Approach current Objective center (FormationHome; no countdown/spawn yet)
  → First loyal in CaptureZone → start GatherCountdown + wave timers + relocate to formation offsets around Objective
  → Tick independent per-row spawn recipes (FirstDelay → optional Interval repeats) until point success or wipe
  → Countdown end + ≥1 loyal → invincible, stop spawn, kill living monsters, grant point loot, UI-032
  → UI-032: after IdleDelay loyals pause+Idle; if GatherPointCount=1 AutoLeave countdown on Leave
  → Continue → next point OR Leave → credit StageExp → UI-017 victory → Continue → SubLevel clear (§3.9)
  → No loyal during active gather → LevelFailure → UI-017 defeat → TitleMenu / Restart
```

**字段已锁定（工作坊 2026-09-02；Excel 后置 SE-01）**

| 项 | 决议 |
|----|------|
| 倒计时 HUD | Combat 顶栏显示剩余整秒（Demo）；`SearchExtractCountdownCanvas/CountdownBar` 锚点顶中，`anchoredPosition.y=-123` |
| 路线图摘要 | UI-031 **同时**展示点奖励摘要与子关卡 `Reward`；入账时机分离（点=单点胜利；关=Leave） |
| 搜集中手动清怪掉落 | **无**额外掉落；仅倒计时胜利清场 |
| `GatherCountdownSeconds` | 仅玩法表全局；刷怪表不加覆盖列 |

**实现切片：** `.scratch/mode3-search-extract/issues/` SE-00～09；**P1 HoldFraming：** `.scratch/search-extract-hold-camera/` SE-CAM-00～03 **已关**（样例锁定 v0.84.27 初值）；验收见 §3.8 **D-087**。

**待实现优先级（规则库；非 Demo §3.8 P0）**

| 优先级 | 内容 |
|--------|------|
| P0 | SPEC 关闭、**配置表字段已确认**、Stage 接线、进圈倒计时、重定位、方向刷怪、单点胜利 UI、多点链、全灭失败 |
| P1 | 倒计时 HUD polish；**HoldFraming 守点镜头**（方案 B；SE-CAM-00～03 **已关**；样例锁定初值；Play Mode 手验待负责人）；样例关卡手验 |
| P2 | 与 PushMap 共用地图的内容管线 polish |

### English

**Status: Rules locked (Approach A: independent Session; reuse PushMap map markers + §3.12 combat pipeline); Demo implementation requires separate authorization and issue splits (see `.scratch/mode3-search-extract/issues/`).**

Entered when the current SubLevel option has `GameplayType = SearchExtract`. **Runs under Mode2** (`CampaignMode=Mode2`; save, AutoManufacture, config root unchanged); **not** a third `CampaignMode`. **Distinct from** §3.14 PushMap instant Capture and §3.12 Defend global countdown spawns.

Depends on §3.11 **BattleFormation**. Config: [SPEC_04 §9.32](SPEC_04_Technical.md) `SearchExtractGameplayConfig`, [§9.33](SPEC_04_Technical.md) `SearchExtractWaveSpawnConfig`, [§9.31](SPEC_04_Technical.md) SubLevel `GatherPointCount` / `GatherPointRewards` (**columns locked, workshop 2026-09-02**), [§9.19](SPEC_04_Technical.md) `MonsterConfig`.

**Relation to PushMap / Defend (Approach A reuse boundary)**

| Reuse | Notes |
|-------|-------|
| Phases | `Prepare` → `Combat` → `Ended` (`SearchExtractPhase`; aligned with PushMapPhase) |
| Formation / StartBattle | Same `FormationEditor`; ≥1 deployed; ControlPower overflow allowed |
| Map markers | `ObjectivePoint` + `CaptureZone`, `AirWall`, `SpawnPoint`, `EngageZone` / `WalkSurface` |
| Warrior combat | Same §3.12 + MassCombatPathing + §3.18 TF (during countdown each group's center keeps its offset from the StartBattle army centroid, and that centroid maps onto the current gather Objective); spawned monsters must join the same MassMove Tick and AttackSlot chase refresh as PushMap (no NavMeshAgent-only wander) |
| Monster death skills | Same §3.14 D-074: Session implements `IMonsterDeathSkillHost` and registers `MonsterConfig.Skills` (sample `Monster_01`=`MSkill_SelfRevive_99`); HP≤0 may fake-death revive. **Point-clear** is true death (no SelfRevive intercept) |
| Camera | Ortho battle camera same as PushMap; **pre-activation / post-Continue approach** = `CameraFollowPath` rail follow; **point active～GatherCountdown** = **HoldFraming** (viewport framing + hysteresis; see Chinese「倒计时战斗镜头」); **Continue** forces Auto, SmoothDamps Size back to `PushMapCameraOrthoSize` with `CameraFollowSmoothTime` (no look-at Snap); **does not** change PushMap default rail ([SPEC_04 §6](SPEC_04_Technical.md)/[§9.20b](SPEC_04_Technical.md)) |
| Combat indicator | Same §3.14 UI-033 / D-089: show shared `CombatIndicatorHud` in `Combat`; hide during Prepare / Ended / UI-017 / UI-032 decision; revivable monsters count as alive |
| Off-screen spawn edge hint | Same §3.14 UI-034 / D-090: Combat real spawn with `basePos` off-viewport → edge `EnemyAttack_1` 2 red blinks + hold 2s at ×1.3; clear on Prepare / Ended / UI-017 / UI-032 |
| Not reused | PushMap instant Capture, StartBattle `PushMapSpawnConfig`, Boss clear, Defend `WaveSpawnConfig`, PushMap `Shield≤0` fail |
| Failure | **Only** all loyal dead during active gather → **whole Level** `LevelFailure` → UI-017 defeat (Return to Title / Restart; no save wipe) |
| Shield | Demo **default:** no battle protagonist shield fail; LossOfControl/Rebel still at StartBattle |

**Phases, gather chain, countdown/spawn, formation relocate, HoldFraming camera, point success, failure:** same semantics as Chinese block above.

**Config layers:** SubLevel (`GatherPointCount` + `GatherPointRewards` encoding `N:ItemId;Count|…`); `SearchExtractGameplayConfig` (global `GatherCountdownSeconds`, `StageExpReward` credited on Leave); `SearchExtractWaveSpawnConfig` (one independent recipe per row: FirstDelay from point activation, then Interval×`RepeatSpawnCount` re-spawns of the same row; `SpawnPointId` only); map Prefab ≥ N objectives. Demo authority map `SearchExtract_Lv1_01` (keeps `SearchExtract_Demo_01` as SE-02 Approach B reference copy of `PushMap_Demo_01`; **do not** rewrite the PushMap source; maps carry `FormationClassZone` for Prepare one-click; runtime bind via `DefendPrefabCatalog.Maps`).

**Pre-activation approach (Approach A):** after StartBattle and before gather activation, loyal soldiers approach the current `ObjectivePoint` XZ via `FormationHome` DesiredDestination (no FlowField required); countdown / waves / formation-offset relocate start only on first loyal `CaptureZone` entry.

**Locked (workshop 2026-09-02):** Combat top-bar remaining seconds; UI-031 shows both point-loot summary and SubLevel `Reward` (credit still split); no manual-kill loot; countdown is gameplay-table global only.

**Slices:** `.scratch/mode3-search-extract/issues/` SE-00–09; **P1 HoldFraming:** `.scratch/search-extract-hold-camera/` SE-CAM-00–03 **closed** (samples locked at v0.84.27 initials); acceptance **D-087**. Excel landing = SE-01 **done**.

---

## 3.20 沙盘（Sandbox / COC 入口）

### 简体中文

**状态：入口已定义（方案 A：壳层沙盘会话旁路）。剩余次数切片 01 已落地（`SandboxProgressService`：打开当前难度时才首次写入）。COC 战斗见 §3.21 / D-099。**

沙盘是难度选择之后的关卡入口，替代这些路径上的 UI-031。UI-031、子关卡表、`LevelOperationDriver` 的解锁/通关，以及挖坟 / 商店 / 自动造兵模块**内部**都不改。COC 战斗是新模块（§3.21），不改推图与搜打撤模块内部。

**导航**

- **新建进档**、工具「关卡」：仍先打开 `DifficultySelectHost`（UI-029）。
- 点击普通 / 困难 / 地狱任一栏：打开该 `DifficultyId`（`Diff_Normal` / `Diff_Hard` / `Diff_Hell`）的沙盘。不再 Toast「还未制作」。
- **进入占用档**：跳过难度界面，打开沙盘，难度默认 `Diff_Normal`。
- 从沙盘进入的玩法结束（挖坟结算确认、商店关闭、自动造兵批次结束，以及 COC 胜利或剩余次数仍大于 0 的单局失败）回到**同一难度**沙盘。COC 最终失败回 Title（§3.21）。
- 驱动器 `LevelEnded` 回到最近一次沙盘难度；尚无记录时用 `Diff_Normal`。
- 沙盘「返回」回到难度选择。

**方格**

数据来自 [SPEC_04 §9.1c](SPEC_04_Technical.md) `Level_SandboxNodeConfig`。按 `DifficultyId` 过滤，`SortOrder` 升序从左到右。每格显示 `DisplayName` 与**存档剩余次数**（未通关：「可进入 N 次」；已通关：「已通关」且不可进入）。`RepeatEnterCount` 只在该存档第一次见到此 `NodeId` 时写成剩余次数；之后改表不回写旧档。方格之间**没有**解锁前置。

**剩余次数**

- 挖坟 / 商店 / 自动造兵：点击且剩余大于 0 时进入并立刻扣 1。剩余为 0 时点击不进入。
- COC：剩余大于 0 且未通关才能进入；进入不扣次。扣次与通关见 §3.21。
- 占领点按 [SPEC_04 §9.38](SPEC_04_Technical.md) 给**同一难度**的挖坟 / 商店 / 自动造兵节点增加剩余次数。已通关节点不加、也不因此重新打开。

**进入（不经 `TrySelectGameplayOption`）**

壳层自建 `LevelStageContext` 后调用已有 `IStageModule.Enter`。会话标记为沙盘时，完成回调 `Exit` 后重开沙盘，**不**调用 `TryAdvanceStage`（因此不写路线通关、不发子关卡奖励、不重开 UI-031）。

| GameplayType | 行为 |
|--------------|------|
| `Dig` | `GameplayConfigId` 必须能解析到 `DigGameplayConfig`（样例 `Dig_01`），再进 `DigStageRoot` |
| `Shop` | 现有 `ShopStageModule`；ConfigId 忽略 |
| `AutoManufacture` | 现有 `AutoManufactureStageModule`；ConfigId 忽略；`LevelId` 为空时地图仍由现有解析落到 `Ground_01` |
| `CocCombat` | `GameplayConfigId` 必须能解析到 `CocGameplayConfig`。壳层 `SetState(CocCombat)` 后进入 `CocCombatStageModule`。胜负与回程见 §3.21 |

### English

**Status: entry defined (Approach A: shell Sandbox session bypass). Remaining-count slice 01 has landed (`SandboxProgressService`: a node is recorded the first time that difficulty is opened). COC combat: §3.21 / D-099.**

The Sandbox is the level entry after difficulty select, replacing UI-031 on these paths. UI-031, the SubLevel table, `LevelOperationDriver` unlock/clear, and Dig / Shop / AutoManufacture module **internals** stay unchanged. COC combat is a new module (§3.21) and does not change PushMap or SearchExtract internals.

**Navigation**

- **Create enter** and Tools Level still open `DifficultySelectHost` (UI-029) first.
- Clicking Normal / Hard / Hell opens the Sandbox for that `DifficultyId` (`Diff_Normal` / `Diff_Hard` / `Diff_Hell`). No Toast「还未制作」.
- **Enter occupied** skips the difficulty host and opens the Sandbox at `Diff_Normal`.
- Gameplay entered from the Sandbox (Dig summary confirm, Shop close, AutoManufacture batch end, and COC victory or a round loss that still leaves remaining count above 0) returns to the **same difficulty** Sandbox. COC final loss returns to Title (§3.21).
- Driver `LevelEnded` returns to the last Sandbox difficulty, or `Diff_Normal` if none was chosen.
- Sandbox Back returns to difficulty select.

**Cells**

Rows come from [SPEC_04 §9.1c](SPEC_04_Technical.md) `Level_SandboxNodeConfig`. Filter by `DifficultyId`; left-to-right by ascending `SortOrder`. Each cell shows `DisplayName` and the **saved remaining count** (uncleared: 「可进入 N 次」; cleared: 「已通关」 and not enterable). `RepeatEnterCount` is copied into remaining count only the first time that save sees this `NodeId`; later table edits do not rewrite old saves. Cells have **no** unlock gates.

**Remaining count**

- Dig / Shop / AutoManufacture: a click with remaining above 0 enters and decrements by 1 immediately. A click at 0 does not enter.
- COC: enter only when remaining is above 0 and the node is not cleared; entering does not decrement. Decrement and clear are §3.21.
- Capture points add remaining count to Dig / Shop / AutoManufacture nodes of the **same difficulty** ([SPEC_04 §9.38](SPEC_04_Technical.md)). Cleared nodes gain nothing and do not reopen.

**Enter (not via `TrySelectGameplayOption`)**

The shell builds a `LevelStageContext` and calls the existing `IStageModule.Enter`. While the session is marked Sandbox, the complete callback `Exit`s and reopens the Sandbox, and does **not** call `TryAdvanceStage` (no route clear, no SubLevel reward, no UI-031).

| GameplayType | Behavior |
|--------------|----------|
| `Dig` | `GameplayConfigId` must resolve to `DigGameplayConfig` (sample `Dig_01`), then `DigStageRoot` |
| `Shop` | Existing `ShopStageModule`; ConfigId ignored |
| `AutoManufacture` | Existing `AutoManufactureStageModule`; ConfigId ignored; empty `LevelId` still resolves the map to `Ground_01` |
| `CocCombat` | `GameplayConfigId` must resolve to `CocGameplayConfig`. The shell `SetState(CocCombat)` then enters `CocCombatStageModule`. Win, loss, and return are §3.21 |

---

## 3.21 COC 战斗（CocCombat）

### 简体中文

**状态：规则已定义（D-099）。切片 02（方案 B）已从沙盘进入地图。切片 03a（方案 A）开战按刷怪表刷出怪物，并复用推图怪物表现停在刷怪点（不传主角、不传我方，因此不追击；不改推图会话）。切片 03b（方案 A）底部按 `ClassId` 洒兵：点在 NavMesh 可行走且空气墙外才放人，并立刻从士兵库删除。士兵复用推图选敌与 AttackSlot；命中由本局 `CocFieldCombatSession` 结算（不发经验、不护盾、不失控、不跑技能爆发）。切片 03c（方案 A）：检测范围内没有敌人时，士兵沿 NavMesh 走向本图唯一 `FinalBoss`（空气墙已标不可走）；进入检测范围后改走现有选敌。小 BOSS 死亡不结算通关。场上有存活我方后，怪物才沿用 `AggroMode` 追击。切片 03d（方案 A）：最终 BOSS 死亡 → 节点 `Cleared`、不扣次、回沙盘；退出或场上全死且库空 → 扣 1 次，剩余 &gt;0 回沙盘，否则回 Title。已洒出的不回库。迷雾切片 04a（方案 A，格子盖章）已画静态未探索黑雾。切片 04b（方案 A，同格三态就地更新）已写入临时亮起与变暗：存活士兵按 `RevealRadius` 盖章，离开或死亡后变暗；已发现普通怪在变暗区或多边形外保持可见；临时亮起与变暗仍禁洒；重进重建格子即重置探索。切片 05（方案 A，同格第四态）已激活占领点：写入存档、按半径盖永久亮起，并给同一难度未通关的挖坟 / 商店 / 自动造兵加次数。重进后再按存档盖章，不重复发放。切片 06：职业卡按住左键达到 `CardHoldSeconds` 后，在该卡上方横排展开组内全部士兵。未满时长离开卡牌则取消。展开后按住并左右拖动可滑动，松手收起。展开格不可点选洒出，不改池顺序，下一次仍放走该组最前一名。**

COC 战斗从沙盘进入，用洒兵代替布阵。地图刷怪、NavMesh、空气墙，以及士兵的选敌、移动和攻击，沿用推图 / §3.12 已有战斗。不使用布阵、战术阵型、护盾、失控或叛变。不发经验，不弹推图结算（UI-017 / UI-018）。不放主角。

**进入**

沙盘节点 `GameplayType=CocCombat`，剩余次数大于 0，且未通关。`GameplayConfigId` 解析 [SPEC_04 §9.35](SPEC_04_Technical.md) `CocGameplayConfig`。壳层 `SetState(CocCombat)` 后 `CocCombatStageModule.Enter`。不经 `TrySelectGameplayOption` / `TryAdvanceStage`。没有 Prepare：进图即战斗，怪物已刷出，底部即可洒兵。切片 02 进图。切片 03a 在对应 `SpawnPoint` 刷出 `Normal` / `MiniBoss` / `FinalBoss` 并停住。切片 03b 在底部洒兵并让士兵打进入检测范围的敌人。切片 03c：没有检测范围内敌人时走向唯一最终 BOSS；路上再遇敌改打。切片 03d：胜负见本节「胜负」；「退出」即单局失败。切片 04a：多边形内的格子是未探索黑雾，普通怪隐藏。切片 04b：存活士兵周围半径 `RevealRadius` 变透明，离开或死亡后变暗；点击或 NavMesh 落点落在未探索、临时亮起或变暗格上不放人。多边形外仍可洒。切片 05：士兵中心进入占领点圆即激活一次；永久亮起格透明度 0 且可洒兵。没洒出的士兵留在库里；已洒出的不回来。

**士兵库与卡牌（UI-037 / UI-038）**

士兵来自挖坟和自动制造已经进入 `WarriorPool` 的实例。按 `Manufacture_ClassConfig.ClassId` 分组；组内顺序是池里该职业的现有顺序。数量为 0 的职业不显示卡牌。

- 卡牌显示：职业名、组内数量、组内第一名的外形。
- 点击卡牌：该卡选中。再次点击另一张则改选。
- 洒出：选中后，在可洒区域按下鼠标左键一次，把该组最前一名放到点击处的可行走点，并立刻从士兵库删除。按住左键时，每 `DeployHoldIntervalSeconds`（默认 0.1 秒）再放走当前最前一名。点在不可洒、不可走或空气墙内则这次不放人。平移镜头按住鼠标右键拖动；左键不拖镜头。推图与搜打撤仍是左键拖动。
- 长按卡牌达到 `CardHoldSeconds`（默认 1 秒）：UI-038 在该卡上方横排展开组内全部士兵，样式与职业卡相同（外形、名字、从 1 起的序号；序号 1 是下一次洒出的那一名）。不论该卡是否已选中，按住满时长即可展开，不需先点击选中。一行，超出视口可左右拖动滑动。未满时长指针离开卡牌则取消。松手收起。只查看，不改变池顺序，也不能点某一名单独洒出。
- 「退出」无二次确认，立即记为单局失败。

**可洒区域**

只在这两处可放人：迷雾多边形以外；已激活占领点的永久亮起圆以内。临时亮起和变暗区域不能洒兵。点击点还须落在 NavMesh 可行走处，且不在空气墙内。

**战斗**

士兵进入地图后，用现有选敌方式追击并攻击。找不到敌人时，沿 NavMesh 最短路径走向本图唯一最终 BOSS，路径排除空气墙。有敌人进入该兵自己的攻击半径后，改为攻击该目标并继续战斗。

开局怪物停在刷怪点。场上还没有我方士兵时不追击。出现我方士兵后，沿用怪物表现上已有的追击（`AggroMode`）。本玩法不掷失控、不产生叛变。

**战场迷雾**

黑雾画在士兵和地图元素之上，与 `CameraFogOverlay`、`MapEdgeFog` 分开。多边形写在地图预制体上：点数可增，按顺序自动连成一个面。多边形以外默认可看见怪物，也可洒兵。

每一局重新计算探索。占领点的永久亮起不重置。

| 状态 | 条件 | 表现 |
|------|------|------|
| 无士兵信息 | 多边形内，本局尚未被临时亮起或永久亮起覆盖的部分 | 透明度 `UnexploredAlpha`（默认 0.9）。能看见地图。隐藏普通怪。最终 BOSS 与小 BOSS 始终可见 |
| 临时亮起 | 以我方存活士兵为圆心、地面 XZ 半径 `RevealRadius`（默认 2） | 透明度 0。能看见圆内的怪。圆内普通怪记为已发现 |
| 变暗 | 某块区域曾被临时亮起，随后士兵死亡或离开，该块不再被任何临时亮起覆盖 | 透明度 `ExploredAlpha`（默认 0.7）。已发现且正站在变暗区、永久亮起或多边形外的普通怪保持可见。走进仍是「无士兵信息」的区域时，普通怪重新隐藏 |
| 永久亮起 | 占领点已激活 | 配置半径的地面 XZ 圆，透明度 0，跨局保留。圆内可洒兵，怪可见 |

半径都是地面上的圆。等距镜头下看起来会带地图倾角。画雾选定方案 A（格子盖章）。地面格宽 0.25。切片 04b 在同一张格子上就地写入 `Revealed` / `Explored`：每帧按存活士兵 `RevealRadius` 盖章；不再被盖住的格变暗。雾片贴在地面上，`sortingOrder` 250，盖住地图和士兵（200），低于弹体（320）。最终 BOSS 与小 BOSS 的精灵提到 280，画在雾片之上。雾片按格写入贴图（一格一像素，与硬边雾片同量级）。边缘过渡 0.5 在雾片着色器里对周围格做加权，不在 CPU 上逐像素重算。交界处半透明、不保留直角锯齿。格宽 0.25 与三态规则不变。普通怪中心落在未探索格就隐藏；圆内记为已发现；已发现且站在变暗区或多边形外保持可见，走进未探索格再隐藏。洒兵的点击点或 NavMesh 落点落在未探索、临时亮起或变暗格则这次不放人。多边形外（`Outside`）仍可洒。重进时销毁并重建格子，本局探索清空，再按存档把已激活占领点盖成永久亮起。

**占领点**

位置在地图预制体上。半径来自 `CocCapturePointConfig`。我方士兵中心进入该地面圆即激活；每个占领点只激活一次。激活写入存档，并按 `CocCaptureRewardConfig` 给同一难度的挖坟 / 商店 / 自动造兵节点增加剩余次数。再次进入不再发放。已通关的目标节点不加次数。选定方案 A（同格第四态）：进图重建雾格后，已激活的圆把格盖成永久亮起；临时亮起不改写这些格。单局失败重进后探索重置，永久亮起按存档再盖。

**胜负**

- 胜利：本图唯一 `FinalBoss` 死亡。不扣剩余次数。该沙盘节点标记通关，方格显示「已通关」且不能再进，然后回同一难度沙盘。
- 单局失败：玩家点「退出」；或场上我方士兵全部死亡，且士兵库里已经没有可洒的士兵。已洒到场上的士兵不回库（退出时仍活着的也删除）。没洒出的留在库里。然后剩余次数扣 1。
- 扣完后剩余仍大于 0：回同一难度沙盘。再次进入时，怪物、迷雾探索和场上单位全部重置；已激活占领点与其永久亮起保留。
- 扣完后剩余为 0：最终失败，回 Title。占领点与已消耗的士兵保持存档结果。剩余为 0 的节点不能再进。
- 小 BOSS 不作为胜利条件。每张 COC 地图的刷怪配置恰好一行 `FinalBoss`。

**明确不做**

布阵与战术阵型、护盾、失控/叛变、主角、经验与推图结算弹窗、搜打撤搜集点与守点镜头。

### English

**Status: rules defined (D-099). Slice 02 (Approach B) enters the map from the Sandbox. Slice 03a (Approach A) spawns from the spawn table and reuses the PushMap monster view, held idle on the spawn point (no protagonist and no friendlies are passed, so they do not chase; the PushMap session is unchanged). Slice 03b (Approach A) deploys from bottom `ClassId` cards: a click places a soldier only on walkable NavMesh outside air walls and removes that soldier from the pool immediately. Soldiers reuse PushMap target select and AttackSlot; hits settle on this round's `CocFieldCombatSession` (no experience, shield, loss-of-control, or skill burst). Slice 03c (Approach A): with no enemy in detect range, soldiers follow the NavMesh toward this map's single `FinalBoss` (AirWalls already non-walkable); inside detect range they reuse existing target select. Mini Boss death does not clear the round. Monsters use `AggroMode` chase only after a living friendly is on the field. Slice 03d (Approach A): Final Boss death → node `Cleared`, no decrement, return to Sandbox; Quit or wipe (all field dead and pool empty) → decrement by 1, remaining &gt;0 back to Sandbox else Title. Deployed soldiers do not return to the pool. Slice 04a (Approach A) draws static unseen fog. Slice 04b (Approach A, in-place three-state grid) writes temporary reveal and explored-dark: living soldiers stamp `RevealRadius`, cells they leave or die out of go dark; discovered normal monsters stay visible in explored-dark or outside polygons; temporary reveal and explored-dark still block deploy; re-entry rebuilds the grid and resets exploration. Slice 05 (Approach A, fourth grid state) activates a capture once, saves it, stamps permanent light, and adds remaining count to uncleared Dig / Shop / AutoManufacture nodes of the same difficulty. Re-entry re-stamps from the save and does not grant again. Slice 06: holding left-click on a class card for `CardHoldSeconds` expands every soldier in that group in one row above the card. Leaving the card before the threshold cancels. After it opens, dragging left or right scrolls, and releasing dismisses it. Peek cells cannot deploy a chosen soldier, the pool order stays unchanged, and the next deploy is still the front of that group.**

COC combat is entered from the Sandbox and replaces formation with deploy-soldiers. Map spawns, NavMesh, air walls, and soldier target select, move, and attack reuse PushMap / §3.12 combat. No formation, tactical formation, shield, loss-of-control, or rebels. No experience and no PushMap settlement (UI-017 / UI-018). No protagonist.

**Enter**

Sandbox node `GameplayType=CocCombat`, remaining count above 0, and not cleared. `GameplayConfigId` resolves [SPEC_04 §9.35](SPEC_04_Technical.md) `CocGameplayConfig`. The shell `SetState(CocCombat)` then `CocCombatStageModule.Enter`. Not via `TrySelectGameplayOption` / `TryAdvanceStage`. There is no Prepare: the map starts in combat with monsters already spawned and the bottom bar ready to deploy. Slice 02 enters the map. Slice 03a spawns `Normal` / `MiniBoss` / `FinalBoss` on the matching `SpawnPoint` and holds them idle. Slice 03b deploys from the bottom bar and has soldiers attack enemies that enter detect range. Slice 03c: with no enemy in detect range they march to the single Final Boss; on the way they switch back to fight when an enemy enters detect range. Slice 03d: win/loss follows this section's "Win and loss"; Quit is a round loss. Slice 04a paints unseen fog cells inside polygons and hides normal monsters there. Slice 04b: a living soldier's `RevealRadius` circle goes transparent, then explored-dark after they leave or die; a click or NavMesh point on unseen, temporary-reveal, or explored-dark cells places nobody. Outside polygons, deploy remains allowed. Slice 05: a soldier center entering a capture circle activates it once; permanent-light cells are alpha 0 and allow deploy. Soldiers never deployed stay in the pool; deployed soldiers do not return.

**Pool and cards (UI-037 / UI-038)**

Soldiers are `WarriorPool` instances already created by Dig and AutoManufacture. Group by `Manufacture_ClassConfig.ClassId`; order inside a group is the pool order of that class. A class with count 0 shows no card.

- A card shows the class name, the count in the group, and the appearance of the first soldier in the group.
- Click a card to select it. Clicking another card switches the selection.
- Deploy: while selected, one left-click on an allowed area places the front soldier of that group on a walkable point at the click and removes that soldier from the pool immediately. Holding left-click places the new front soldier every `DeployHoldIntervalSeconds` (default 0.1s). A click in a forbidden area, off the NavMesh, or inside an air wall places nobody. Pan the camera by holding the right mouse button; the left button does not pan. PushMap and SearchExtract still pan with the left button.
- Holding a card for `CardHoldSeconds` (default 1s) opens UI-038 above that card: every soldier in the group, in the same card style (appearance, name, and a 1-based index; index 1 is the next deploy). Works whether or not the card is already selected; no prior click-select is required. One row; drag horizontally when it overflows the viewport. Leaving the card before the threshold cancels. Releasing dismisses it. Inspect only: pool order stays unchanged, and a chosen soldier cannot be deployed.
- Quit has no confirm dialog and is an immediate round loss.

**Where deploy is allowed**

Only outside fog polygons, and inside the permanent-light circle of an activated capture point. Temporary reveal and explored-dark areas cannot receive soldiers. The click must also land on walkable NavMesh and outside air walls.

**Combat**

After a soldier enters the map, existing target select moves and attacks. With no enemy, the soldier follows the shortest NavMesh path to this map's single Final Boss, excluding air walls. When an enemy enters that soldier's own attack range, the soldier switches to that target and keeps fighting.

Monsters start idle on their spawn points. They do not chase while no friendly soldier is on the field. After a friendly exists, they use the existing monster chase (`AggroMode`). This mode does not roll loss-of-control and does not create rebels.

**Battlefield fog**

Black fog draws above soldiers and map elements, separate from `CameraFogOverlay` and `MapEdgeFog`. Polygons live on the map prefab: the point count can grow, and points connect in order into one face. Outside every polygon, monsters are visible and deploy is allowed.

Exploration recalculates every round. Permanent light from capture points does not reset.

| State | When | Presentation |
|-------|------|----------------|
| Unseen | Inside a polygon, not covered this round by temporary reveal or permanent light | Alpha `UnexploredAlpha` (default 0.9). The map shows. Normal monsters hide. The Final Boss and Mini Bosses stay visible |
| Temporary reveal | Ground-plane XZ circle of radius `RevealRadius` (default 2) around each living friendly | Alpha 0. Monsters in the circle show. Normal monsters in the circle become discovered |
| Explored dark | An area was temporarily revealed, then the soldier died or left, and no temporary reveal still covers it | Alpha `ExploredAlpha` (default 0.7). Discovered normal monsters that are standing in explored-dark, permanent light, or outside polygons stay visible. A normal monster that walks into a still-unseen area hides again |
| Permanent light | Capture point activated | Ground-plane XZ circle of the configured radius, alpha 0, kept across rounds. Deploy is allowed inside, and monsters there are visible |

Radii are circles on the ground. Under the isometric camera they appear tilted with the map. Fog drawing is Approach A (grid stamp). Ground cell size is 0.25. Slice 04b writes `Revealed` / `Explored` in place on the same grid: each frame stamps living soldiers' `RevealRadius`; cells no longer covered go dark. The sheet lies on the ground at `sortingOrder` 250, above the map and soldiers (200) and below projectiles (320). Final Boss and Mini Boss sprites are raised to 280 so they draw above the sheet. The sheet writes one texel per cell, the same cost as the hard-edged sheet. A shader fades edges over 0.5 ground units by weighting neighboring cells, instead of rebuilding every texel on the CPU. Borders stay translucent and are not square stairs. Cell size 0.25 and the three states stay the same. A normal monster whose center is on an unseen cell is hidden; one inside the circle becomes discovered; a discovered monster in explored-dark or outside polygons stays visible and hides again on an unseen cell. A deploy click or NavMesh point on unseen, temporary-reveal, or explored-dark cells places nobody. Outside (`Outside`) still allows deploy. Re-entry destroys and rebuilds the grid, so this round's exploration clears, then activated capture circles are stamped permanent again from the save.

**Capture points**

Positions are on the map prefab. Radius comes from `CocCapturePointConfig`. A friendly soldier's center entering that ground circle activates it; each capture point activates once. Activation is saved and, per `CocCaptureRewardConfig`, adds remaining count to Dig / Shop / AutoManufacture nodes of the same difficulty. Re-entry does not grant again. A cleared target node gains nothing. Approach A (fourth grid state): after the fog grid is rebuilt, activated circles stamp cells permanent; temporary reveal does not overwrite them. A round-loss re-entry resets exploration and re-stamps permanent light from the save.

**Win and loss**

- Victory: this map's single `FinalBoss` dies. Remaining count is not decremented. The Sandbox node is marked cleared, the cell shows 「已通关」 and cannot be entered, then the game returns to the same difficulty Sandbox.
- Round loss: the player presses Quit; or every friendly on the field is dead and the pool has no soldier left to deploy. Deployed soldiers do not return to the pool (ones still alive on quit are removed too). Soldiers never deployed stay in the pool. Then remaining count decrements by 1.
- If remaining is still above 0: return to the same difficulty Sandbox. The next entry resets monsters, fog exploration, and units on the field; activated capture points and their permanent light remain.
- If remaining is 0: final loss, return to Title. Capture points and consumed soldiers stay as saved. A node at 0 cannot be entered.
- Mini Bosses are not a win condition. Each COC map's spawn config has exactly one `FinalBoss` row.

**Explicitly out**

Formation and tactical formation, shield, loss-of-control / rebels, the protagonist, experience and the PushMap settlement popup, SearchExtract gather points and hold camera.

---

## 维护说明

### 简体中文

- 新模块从下一个可用 `## 3.x` 节起写；大节变更记入 SPEC_00 Changelog。
- 中英文双块同步；未决标 `TBD` / `未定义`。

### English

- Add new modules as the next `## 3.x` section; log major changes in SPEC_00 Changelog.
- Keep bilingual blocks in sync; mark open items `TBD` / `Undefined`.
