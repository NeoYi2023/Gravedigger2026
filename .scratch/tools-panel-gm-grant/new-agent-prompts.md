# ToolsPanel GM 发放 — NewAgent 可粘贴分步指令

**用途：** 每个 New Agent **只粘贴一个分片**整块正文（含下方 ``` 内全文）。  
**执行序：** TP-00 → TP-01 → TP-02。  
**权威：** `.scratch/tools-panel-gm-grant/INDEX.md` + 对应 `issues/`。  
**选定方案：** **A**（通用 `GmGrantListPanel`；装备 `TryAcquire`；魔法书 `TryEquip`）。

**总约束：**

1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；设计变更先改 SPEC/Changelog 再编码。
2. 本会话 **只实现本片**；禁止顺手做下一片。
3. 魔法书 **不**新建仓库。
4. Dig HUD 现有 GM **保留**。
5. **不**做正式装备仓 UI / 魔法书装配 UI / 制造·战斗 Token handler。
6. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。

---

## 分片 TP-00 — SPEC 关闭（先做）

**标题：** ToolsPanel GM · TP-00 · SPEC D-061 / UI-019

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 TP-00（方案 A / ToolsPanel GM 发放）。

【授权与边界】
- 本会话只做 TP-00 SPEC，禁止 TP-01 / TP-02 编码。
- CONTEXT 无需新术语。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/tools-panel-gm-grant/INDEX.md
3. .scratch/tools-panel-gm-grant/issues/00-spec-close.md
4. SPEC_03 §3.5 / §3.6 / §3.8
5. SPEC_04 §6
6. SPEC_00 Changelog（现行版本号）

【目标】
1) SPEC_03 §3.5：ToolsPanel 本期条目增加「增加主角装备」「增加魔法书」；点击 → 关 ToolsPanel → GmGrantListPanel。
2) SPEC_03 §3.6：UI-003 扩写；新增 UI-019 GmGrantListPanel。
3) SPEC_03 §3.8：新增 D-061（P1）：列表发放装备 TryAcquire + 魔法书 TryEquip。
4) SPEC_04 §6：ToolsPanel Demo GM 一句。
5) SPEC_00 Changelog bump；中英双块同步。

规则要点：
- 按钮文案 DisplayName，空则 Id
- 装备按 EquipId 去重（Level 1）；点一次 TryAcquire
- 魔法书全表；点一次 TryEquip（无仓库；唯一已装/槽满失败）
- Toast + 日志；列表可连点；Dig HUD GM 保留

【范围】
- 改：SPEC_03、SPEC_04 §6、SPEC_00

【不做】
- 任何 C# / Prefab

【验收】
- D-061 / UI-019 中英已写
- Changelog 已记
- 勾 issue；INDEX TP-00→done
```

---

## 分片 TP-01 — 增加主角装备

**标题：** ToolsPanel GM · TP-01 · GmGrantListPanel + TryAcquire

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 TP-01（方案 A / D-061）。

【授权与边界】
- Demo 已授权本片；本会话只做 TP-01，禁止 TP-02（增加魔法书 / TryEquip）。
- 不改 TryAcquire 契约。
- 不删除 Dig HUD GM。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/tools-panel-gm-grant/INDEX.md
3. .scratch/tools-panel-gm-grant/issues/01-equip-grant.md
4. SPEC_03 §3.5 / §3.6 UI-019 / §3.8 D-061
5. SPEC_04 §6
6. 参考：LevelSelectPanelView + MetaShellAssetBuilder.EnsureLevelSelectPanelOnExistingPrefab + MetaShellController.HandleLevel

【目标】
ToolsPanel「增加主角装备」打开 GmGrantListPanel；列出当前模式 ProtagonistEquipmentConfig 去重 EquipId（Level 1 DisplayName）；点一次 ProtagonistEquipmentService.TryAcquire；Toast 成功/失败；列表保持打开。

【范围】
- 新建 Assets/Scripts/UI/GmGrantListPanelView.cs
- 改 ToolsPanelView / InSaveShellView / MetaShellController
- 改 MetaShellAssetBuilder：加高 ToolsPanel；Ensure GmGrantListPanel (UI-019)

【不做】
- 「增加魔法书」按钮与 TryEquip（TP-02）
- 正式装备仓 UI

【流程门禁】
- 整包难度 2、方案 A 已在 INDEX 锁定；本片可直接编码。

【验收】
- 列表至少含铁铲、矿灯
- 未持有点一次入仓 L1；再点转化经验
- Ensure 菜单可补 Prefab
- 勾 issue；INDEX TP-01→done
```

---

## 分片 TP-02 — 增加魔法书

**标题：** ToolsPanel GM · TP-02 · TryEquip 复用 GmGrantListPanel

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 TP-02（方案 A / D-061）。

【授权与边界】
- 本会话只做 TP-02。
- 不新建魔法书仓库。
- 不改 TryEquip 契约。
- 不删除 Dig HUD「装备战士强化」。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/tools-panel-gm-grant/INDEX.md
3. .scratch/tools-panel-gm-grant/issues/02-magicbook-grant.md
4. SPEC_03 §3.5 / §3.8 D-061 / §3.15
5. SPEC_04 §9.24
6. 参考：SpecialEquipSlotsService.TryEquip；TP-01 GmGrantListPanelView

【目标】
ToolsPanel「增加魔法书」打开同一 GmGrantListPanel；MagicBookConfig 全表；点一次 TryEquip 装入第一个空槽；唯一已装或槽满 Toast 失败；列表保持打开。

【范围】
- ToolsPanelView 第二入口
- MetaShellController 组装 MagicBooks → TryEquip
- Builder 重排含本按钮；不新建第二 Overlay

【不做】
- 正式装配 UI / 仓库

【流程门禁】
- 方案 A 已锁；可直接编码。

【验收】
- 列表含还原、战士强化
- 可叠书可连点至槽满失败；IsUnique=1 已装失败
- D-061 可勾
- 勾 issue；INDEX TP-02→done
```
