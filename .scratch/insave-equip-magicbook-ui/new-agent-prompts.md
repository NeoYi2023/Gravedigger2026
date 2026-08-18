# InSaveShell 装备 / 魔法书弹窗 — NewAgent 可粘贴分步指令

**用途：** 每个 New Agent **只粘贴一个分片**整块正文（含下方 ``` 内全文）。  
**执行序：** EM-00 → EM-01 → EM-02 → EM-03（EM-02 与 EM-03 均依赖 EM-01；EM-02 与 EM-03 可分开会话）。  
**权威：** `.scratch/insave-equip-magicbook-ui/INDEX.md` + 对应 `issues/`。  
**选定方案：** **A**（共享 `BookRow.prefab`；`TrySwap` + `Changed`；装备仓只读）。

**总约束：**

1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；设计变更先改 SPEC/Changelog 再编码。
2. 本会话 **只实现本片**；禁止顺手做下一片。
3. 魔法书 **无**独立仓库；装入仍 Tools GM `TryEquip`；本片 **不做**卸下。
4. Tools GM 与 Dig HUD GM **保留**。
5. 装备弹窗 **只读**（不升级 / 不划公共经验）。
6. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。

---

## 分片 EM-00 — SPEC 关闭（先做）

**标题：** InSaveEquipMagicBook · EM-00 · SPEC UI-022/023 + D-067/068

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 EM-00（方案 A / InSaveShell 装备·魔法书弹窗）。

【授权与边界】
- 本会话只做 EM-00 SPEC，禁止 EM-01～03 编码。
- CONTEXT 视需要补「装备仓弹窗 / 魔法书排序 UI」一句。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/insave-equip-magicbook-ui/INDEX.md
3. .scratch/insave-equip-magicbook-ui/issues/00-spec-close.md
4. SPEC_03 §3.5 / §3.6 / §3.8 / §3.15 / §3.16
5. SPEC_04 §6
6. SPEC_00 Changelog（现行版本号 v0.82.67+）

【目标】
1) SPEC_03 §3.5：InSaveShell 左下 BackButton 上方增「装备」「魔法书」；点击打开居中 Modal；Tools GM 保留。
2) SPEC_03 §3.6：新增 UI-022（装备仓只读）、UI-023（魔法书 BookRow + 拖拽排序）。
3) SPEC_03 §3.8：新增 D-067、D-068（P1）。
4) SPEC_03 §3.15：删「不做装备/卸下 UI」；锁槽 0→5 左→右启动顺序；TrySwap 含空槽；无魔法书仓库；本轮不卸下。
5) SPEC_03 §3.16：正式仓 UI 只读 OwnedEquip。
6) SPEC_04 §6：TrySwap + Changed + BookRow.prefab 共享。
7) SPEC_00 Changelog；spec-map.md；中英双块同步。

【范围】
- 改：SPEC_03、SPEC_04 §6、SPEC_00、spec-map.md、（可选）CONTEXT

【不做】
- 任何 C# / Prefab

【验收】
- UI-022/023、D-067/068 中英已写
- Changelog 已记
- 勾 issue；INDEX EM-00→done
```

---

## 分片 EM-01 — 入口按钮 + 弹窗壳

**标题：** InSaveEquipMagicBook · EM-01 · 按钮 + Modal 空壳

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 EM-01（方案 A / D-067/068 壳层）。

【授权与边界】
- Demo 已授权；本会话只做 EM-01，禁止 EM-02（装备列表）与 EM-03（BookRow/拖拽）。
- 禁止整表重建 MetaShellRoot；用 Ensure 外科补丁。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/insave-equip-magicbook-ui/INDEX.md
3. .scratch/insave-equip-magicbook-ui/issues/01-shell-buttons-panels.md
4. SPEC_03 §3.5 / §3.6 UI-022 / UI-023
5. SPEC_04 §6
6. 参考：MetaShellAssetBuilder.EnsureGmGrantListPanelOnExistingPrefab、LevelSelectPanelView、InSaveShellView

【目标】
InSaveShellPanel 左下：BackButton 上方竖排「装备」「魔法书」（各 160×48，间距 8）；点击打开居中 Modal（dim + box + title + close）。EquipmentWarehousePanel / MagicBookSlotsPanel 空壳；MagicBook 壳留 BookRowHost 占位。

【范围】
- MetaShellAssetBuilder：BuildInSaveShell + EnsureInSaveEquipMagicBookPanels 菜单
- InSaveShellView：EquipmentRequested / MagicBookRequested + Show/Hide 两面板
- MetaShellController：打开/关闭空壳
- MetaShellRoot.prefab（Ensure 菜单）

【不做】
- 装备列表内容（EM-02）
- BookRow / TrySwap / 拖拽（EM-03）

【流程门禁】
- EM-00 SPEC 须 done；方案 A 已锁。

【验收】
- 三按钮顺序与布局正确
- 两 Modal 可开可关
- Ensure 菜单可补丁现有 Prefab
- 勾 issue；INDEX EM-01→done
```

---

## 分片 EM-02 — 装备仓只读列表

**标题：** InSaveEquipMagicBook · EM-02 · UI-022 / D-067

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 EM-02（方案 A / 装备仓只读 UI-022）。

【授权与边界】
- 本会话只做 EM-02，禁止 EM-03（魔法书拖拽）。
- 装备弹窗只读：不升级、不划公共经验、不卸下。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/insave-equip-magicbook-ui/INDEX.md
3. .scratch/insave-equip-magicbook-ui/issues/02-equipment-warehouse-readonly.md
4. SPEC_03 §3.6 UI-022 / §3.8 D-067 / §3.16
5. SPEC_04 §9.25
6. 参考：ProtagonistEquipmentService、GmGrantListPanelView 滚动结构

【目标】
「装备」弹窗展示 OwnedEquips：DisplayName + Lv.{Level} + Description + Icon（Resources）；空仓「尚未拥有装备」；订阅 Changed。

【范围】
- 新建 EquipmentWarehousePanelView（Assets/Scripts/UI/）
- InSaveShellView / MetaShellController 注入 Service + Configs
- MetaShellAssetBuilder 滚动列表接线

【不做】
- 升级 / EquipCommonExp / 卸下
- 魔法书 BookRow（EM-03）

【流程门禁】
- EM-01 done；方案 A 已锁。

【验收】
- GM 发放后列表可见铁铲/矿灯
- 空仓文案正确
- Changed 刷新
- D-067 可勾；INDEX EM-02→done
```

---

## 分片 EM-03 — 共享 BookRow + 拖拽排序

**标题：** InSaveEquipMagicBook · EM-03 · UI-023 / D-068

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 EM-03（方案 A / 魔法书排序 UI-023）。

【授权与边界】
- 本会话只做 EM-03。
- 不新建魔法书仓库；不卸下；装入仍 Tools GM TryEquip。
- 共享 BookRow.prefab：弹窗与 AutoManufacturePresentationRoot 嵌套同一份。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/insave-equip-magicbook-ui/INDEX.md
3. .scratch/insave-equip-magicbook-ui/issues/03-magicbook-reorder.md
4. SPEC_03 §3.6 UI-023 / §3.8 D-068 / §3.15
5. SPEC_04 §6 / §9.24
6. 参考：SpecialEquipSlotsService、AutoMfgMagicBookSlotView、AutoManufacturePresentationController.BindBooks、AmAssetBuilder

【目标】
1) SpecialEquipSlotsService.TrySwap + Changed。
2) 抽出 Assets/Prefabs/AutoManufacture/BookRow.prefab；AM 演出 nested instance。
3) MagicBookSlotsPanel 嵌套 BookRow；AllowReorder 左键拖拽 Swap（含空槽）；立即 persist。
4) AutoManufacturePresentationController 订阅 Changed 刷新 BookRow。

【范围】
- SpecialEquipSlotsService.TrySwap / Changed
- BookRow.prefab + AmAssetBuilder / PresentationController  refactor
- MagicBookSlotsPanelView + MagicBookSlotDragHandler
- MetaShellController 接线

【不做】
- 卸下 / 弹窗内 TryEquip
- 装备列表（EM-02 已完成部分勿回退）

【流程门禁】
- EM-01 done；方案 A 已锁。

【验收】
- 拖拽 occupied↔occupied、occupied→empty 正确 persist
- AM 演出 BookRow 与弹窗同步
- Step2 左→右顺序随 Swap 变化（手验 2 本）
- D-068 可勾；INDEX EM-03→done
```
