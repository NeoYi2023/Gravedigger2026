# FormationClassZone IsoDiamond — NewAgent 可粘贴分步指令

**用途：** 每个 New Agent **只粘贴一个分片**整块正文。  
**执行序：** FZ-01 → FZ-02（**FZ-00 已由文档 Agent 完成，勿再开**）。  
**权威：** `.scratch/formation-class-zone-isodiamond/INDEX.md` + 对应 `issues/`；SPEC_00 v0.82.15。  
**选定方案：** **A**（WalkSurface 同形 IsoDiamond）。

**总约束：**

1. 先读 Skill `unity-spec-dev-workflow` + 本片 `spec_refs`；设计变更先改 SPEC/Changelog 再编码。
2. 本会话 **只实现本片**；禁止顺手做下一片。
3. **禁止** `DefendAssetBuilder.GenerateAll`。
4. 完成后：勾 issue 验收、更新 INDEX 状态、回复变更文件清单。

---

## 分片 FZ-01 — 运行时 IsoDiamond（先做）

**标题：** Mode2 · FZ-01 · FormationClassZone IsoDiamond mesh/Contains/螺旋

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 FZ-01（方案 A / D-052）。

【授权与边界】
- Demo 已授权本片；本会话只做 FZ-01，禁止 FZ-02（Ensure 写 Prefab）。
- 禁止 DefendAssetBuilder.GenerateAll。
- 禁止改 PlacementOrder / 区中心布局 / HalfExtents 数值。
- 禁止把职业区网格交给 DefendNavMeshBaker。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/formation-class-zone-isodiamond/INDEX.md
3. .scratch/formation-class-zone-isodiamond/issues/01-runtime-isodiamond.md
4. SPEC_03 §3.8 D-052 / §3.15 自动上阵
5. SPEC_04 §13 FormationClassZone
6. 参考：WalkSurfaceIsoDiamond / MapFootprintMath.BuildDiamondMesh

【目标】
FormationClassZone 用与 WalkSurface 相同的 IsoDiamond 网格划定空间；Contains 与螺旋改为 |dx|/hx+|dz|/hz；Snapshot 去掉 RotationYDegrees。职业区网格/Contains/Gizmo 必须走 minExtent≈0.05，禁止 Sanitize 下限 0.5。

【范围】
- FormationClassZone.cs / FormationClassZonesRoot.cs / FormationClassZonesRootGizmos.cs
- FormationClassZoneSnapshot.cs / FormationClassZoneCollector.cs
- FormationZoneSpiralSearch.cs
- MapFootprintMath 小尺寸重载

【不做】
- Ensure 写回 Ground_*（FZ-02）

【流程门禁】
- 整包难度 2 已确认、方案 A 已选定；按 SPEC 直接编码。

【验收】
- ContainsXZ 与 WalkSurface 同公式
- 螺旋不再用 OBB/RotationYDegrees
- Play 模式 MeshCollider.enabled=false
- 小 HalfExtents 不被撑到 0.5
```

---

## 分片 FZ-02 — Ensure 写回 Ground_*

**标题：** Mode2 · FZ-02 · Ensure FormationClassZones identity + Mesh

**粘贴正文：**

```
你正在实现 Gravedigger2026 切片 FZ-02（D-052 / D-057）。

【授权与边界】
- 本会话只做 FZ-02。
- 禁止 DefendAssetBuilder.GenerateAll。
- 不覆盖既有区世界坐标与 HalfExtents。

【必读】
1. .cursor/skills/unity-spec-dev-workflow/SKILL.md
2. .scratch/formation-class-zone-isodiamond/issues/02-ensure-prefabs.md
3. SPEC_04 §13 FormationClassZone
4. 参考：DefendAssetBuilder.EnsureFormationClassZones / WalkSurfaceIsoDiamond

【目标】
Ensure 删除 IsoTileYaw 作者框；父/子 identity；补 Mesh 组件；菜单写回 Ground_01…Ground_05。

【不做】
- 改螺旋算法 / PlacementOrder
- PushMap 专用地图职业区

【流程门禁】
- 难度 1；Ensure 契约已锁；可直接编码。

【验收】
- 各 Ground_* FormationClassZones Y=0
- 子区 MeshFilter+MeshCollider；菱形顶点世界 N/E/S/W
- 未调用 GenerateAll
```
