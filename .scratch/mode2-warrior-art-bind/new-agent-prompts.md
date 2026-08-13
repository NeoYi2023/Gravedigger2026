# Mode2 士兵外观绑定 — NewAgent 可粘贴分步指令

**用途：** 每个 New Agent **只粘贴一个分片**整块正文。  
**执行序：** WA-01 → WA-02（**WA-00 已由文档 Agent 完成，勿再开**）。  
**权威：** `.scratch/mode2-warrior-art-bind/INDEX.md` + 对应 `issues/`；SPEC_00 v0.80.4。  
**选定方案：** **B**（扩展 `WarriorAppearancePrefabAssembler` From-Art）。

**总约束：**

1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；设计变更先改 SPEC/Changelog 再编码。
2. 本会话 **只实现本片**；禁止顺手做下一片。
3. **禁止** `DefendAssetBuilder.GenerateAll`（会覆盖 PushMap 地图 Catalog 绑定）。
4. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。

---

## 分片 WA-01 — From-Art 组装 14 士兵 Prefab + Catalog（先做）

**标题：** Mode2 · WA-01 · 扩展 Assembler 组装 App_0_* / App_4_41 / App_5_51 并绑定 Catalog

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 WA-01（方案 B / D-056）。

【授权与边界】
- Demo 已授权本片；本会话只做 WA-01，禁止 WA-02（职业布阵区）。
- 禁止调用 DefendAssetBuilder.GenerateAll。
- 禁止重新导出/Repair Character Creator（Art 已就绪）。
- 禁止改运行时选外观算法。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/mode2-warrior-art-bind/INDEX.md
3. .scratch/mode2-warrior-art-bind/issues/01-assemble-warrior-prefabs.md
4. SPEC_03 §3.8 D-056
5. SPEC_04 §15.2 士兵 AppearanceId 段 + §6「士兵外观 From-Art」
6. 参考：MonsterModelPrefabAssembler.TryAssemble、WarriorAppearancePrefabAssembler、CloneApp02FallbackAppearances.RefreshWarriorCatalogBindings

【目标】
扩展 WarriorAppearancePrefabAssembler：缺 Prefab 且 Art 就绪则从 Art 创建根+Visual；已有 Prefab 只确保 Visual。然后刷新 Defend/UM Catalog 士兵绑定。产出 14 个 Prefab：App_0_01/02/03、App_0_11/12/13、App_0_21/22/23、App_0_31/32/33、App_4_41、App_5_51。

【范围】
- 改：WarriorAppearancePrefabAssembler.cs（可抽共享 Catalog 刷新，或复用 CloneApp02 私有方法改为 internal）
- 新增：上述 14 个 Warriors/{Id}.prefab
- 改：DefendPrefabCatalog.asset、UpgradeManufacturePrefabCatalog.asset 士兵列表

【不做】
- FormationClassZone（WA-02）
- GenerateAll / 覆盖 App_01–10、App_90–99 的美术引用

【流程门禁】
- 整包难度 2 已确认、方案 B 已选定；本片若有 Catalog 刷新实现细节分歧再用 AskQuestion，否则直接编码。

【验收】
- 14 Prefab 存在且 Visual 绑对应 Art Controller
- Catalog.TryGetWarriorAppearance 对 App_4_41 / App_5_51 / App_0_* 成功
- Mode2 造出圣骑士/暗黑法师可见烘焙模型；无 Appearance Prefab missing 日志
```

---

## 分片 WA-02 — Ground_* 补 8 个职业布阵区

**标题：** Mode2 · WA-02 · EnsureFormationClassZones 覆盖全部 Mode2 ClassId

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 WA-02（D-057）。

【授权与边界】
- 本会话只做 WA-02；禁止改 WA-01 已组装的外观 Prefab/Catalog（除非 Ensure 误伤）。
- 禁止 DefendAssetBuilder.GenerateAll。
- 既有 11 个职业区坐标不得改。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/mode2-warrior-art-bind/issues/02-formation-class-zones.md
3. SPEC_03 §3.8 D-057 / §3.15 自动上阵
4. SPEC_04 §13 FormationClassZone 第二前/后排坐标
5. 参考：DefendAssetBuilder.EnsureFormationClassZones / EnsureOneClassZone

【目标】
EnsureFormationClassZones 增加 8 区并写回 Ground_01…Ground_05，使 Class_DarkMage 等自动上阵不再留池。

【布局锁】
- 第二前排 z=−1.9：Guardian (−2.0)、Brawler (0.0)、Shadowblade (2.0)
- 第二后排 z=+1.7：Longbowman (−2.0)、BombMaster (−1.0)、IceMage (0.0)、FireMage (1.0)、DarkMage (2.0)
- HalfExtents 与现区相同；localEuler Y=25°

【不做】
- 改螺旋算法 / ClassConfig.PlacementOrder
- PushMap 专用地图职业区

【流程门禁】
- 难度 1；布局已锁；可直接编码。

【验收】
- 各 Ground_* 含 Class_DarkMage 等 8 区
- Mode2 暗黑法师自动上阵落入后排新区；圣骑士仍走既有 Paladin 区
- 不再打 No FormationClassZone for ClassId=Class_DarkMage
```
