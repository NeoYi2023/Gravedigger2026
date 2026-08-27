Dig/Defend 共用地图源。
- `Tiles/`：Isometric Tile + Sprite（自 Example Scene `Environment/Tiles`+`Sprites` 复制；禁止运行时/已提交 Prefab 引用 `SmallScaleInt/`）。
- `RuleTiles/`：Isometric Rule Tile（MapAutoTile 自动接边刷笔；见下「地图自动接边」）。
- `Palettes/FantasyTileset.prefab`：可由 `FantasyTilesetPaletteBuilder` 从 Environment/Tiles 重建（非刷图权威盘）。
- `Palettes/FantasyTileset_A.prefab`：**权威刷图 Palette**（MapAutoTile / 日常刷图选此资产）。
- **格子校正（可重复）：** 菜单 `Gravedigger2026/Maps/Correct FantasyTileset_A From FantasyTileset`（批跑：`-executeMethod Gravedigger2026.Editor.Maps.FantasyTilesetALayoutAligner.CorrectFantasyTilesetAFromFantasyTileset`）。按厂商盘 `SmallScaleInt/.../Environment/FantasyTileset` 的 **Tile 资源名** 把 Art 同名砖摆回对应坐标（不复制 SSI 的 Sprite 缓存偏移）；SSI 没有的 Art 砖溢到参考包围盒右侧；特例 **`Ground F4_W`** 原地保留（SSI 无此砖则不删）；`RT_WallA` 重钉固定槽。跑完后须**重开 Tile Palette** 窗口。旧菜单 `Align FantasyTileset_A Layout From SSI` 会调用同一实现。
- **地图 Prefab SSI→Art 重绑：** 菜单 `Gravedigger2026/Maps/Remap PushMap_Demo_03 SSI Tiles To Art (FantasyTileset_A names)`（批跑：`-executeMethod Gravedigger2026.Editor.Maps.MapTileSsiToArtRemapper.RemapPushMapDemo03`）。将 `PushMap_Demo_03` 的 `GroundTilemap` 下各层仍指向 `SmallScaleInt/` 的 Tile 按名换成 Art `Environment/Tiles` / `Animated tiles` / `RuleTiles` 同名砖（与 FantasyTileset_A 笔刷一致）。
- Tile 图标约定：`Environment/Tiles` 里每个 Tile 的 Sprite 必须与同名 `Environment/Sprites` 一致（如 `Stone A12_E` → `Stone A12_E`）。菜单：`Rebind Environment Tile Sprites By Name`；若 Palette 预览仍错，再跑 `Refresh FantasyTileset_A Sprite Cache`（重建 Tilemap 的 `TileSpriteArray` 缓存）。校正菜单已内含重绑 + 缓存刷新。
- Vendor `SmallScaleInt/Fantasy kingdom Tileset/Example scene/Scripts` 不编入（Unity 6 API vs 工程 2021.3；由 `FantasyTilesetExampleCompileGuard` 停编）。
- `Ground_0N/`：可选每图附加贴图；运行时 Instantiate 仍走 `Prefabs/Maps/Ground_0N.prefab`。
- Prefab 逻辑足迹为 **IsoDiamond**：半尺寸 = `PaintRadius*(cellSize.x,cellSize.y)`（Demo `cellSize≈(1,0.5)` → `(5,2.5)`）；`WalkSurface`/`EngageZone`/`DigMapBounds` 对齐砖面外轮廓。
- 打开 Unity 后建议跑一次菜单 `Gravedigger2026/Maps/Align IsoDiamond Footprints`（写入菱形 Mesh 资产）。

## 地图自动接边（MapAutoTile）

编辑器刷图约定（非玩法规则）；权威见 SPEC_04 §13。依赖已安装的 `com.unity.2d.tilemap.extras`。

1. 在 `Art/Maps/RuleTiles/` 创建 **Isometric Rule Tile**（勿用正交 Rule Tile 当本工程等距地图权威刷笔）。
2. 规则里引用的 Sprite / 普通 Tile 仍放 `Art/Maps/Tiles/` 或 `Environment/Tiles`；禁止 Prefab / 运行时引用 `SmallScaleInt/`。
3. 将 Rule Tile 挂到权威刷图 Palette **`Palettes/FantasyTileset_A`**。
4. Tile Palette 选中该刷笔，拖/刷一片区域 → 边界自动换成边/角配套砖。
5. 适用：地面与其它装饰/地形配套套装。不适用：FlowingWater 的 `Water`/`Foam` 层。
6. 不改变 `WalkSurface` / NavMesh / 空气墙 / 占领。

### 首套：Wall A（`RT_WallA`）

- 菜单：`Gravedigger2026/Maps/Ensure Wall A Rule Tile (RT_WallA)` → 钉到 **FantasyTileset_A** 固定格 **`(30, -43)`**（禁止 `(-1,0)`，会搅乱 SSI 布局）。
- 刷法：Tile Palette → **FantasyTileset_A** → 格子 `(30, -43)` 选 `RT_WallA`（图标 = Wall A1_N），填区域自动接边。

## 流动水面（FlowingWater）

连续流动水面是 **地图表现约定**（非玩法规则）；权威见 SPEC_04 §13。资源：`Shaders/Water/`（Built-in shader ×2、贴图 ×2、`Water.mat` / `Foam.mat`）。禁止 Prefab / 运行时引用 `SmallScaleInt/`。

### 两层刷法

在地图 Prefab 的 **同一 Grid**（Isometric；**RotX ≈ 90°**，砖面在 XZ）下增加两层 Tilemap：

| 层名 | TilemapRenderer | 材质 | Order | 用途 |
|------|-----------------|------|-------|------|
| `Water` | **Chunk** | `Water.mat` | 低于 Foam | 水体主体连续滚动 |
| `Foam` | **Individual** | `Foam.mat` | 高于 Water | 岸边白边（须 Individual 才出 rim） |

1. 用 Palette 在 `Water` 层刷水域 Tile（与示例同套水砖即可；alpha 当遮罩，不必是蓝色图）。
2. 在同一范围（或仅岸线邻接格）刷 `Foam` 层（`WaterRipples 1`～`13`）。
3. Shader 世界 UV 已约定为 **xz**；勿改回厂商 `xy`（本工程只会沿一条轴流动）。
4. 水面不参与 NavMesh / 空气墙 / 占领；可走/阻挡仍只看 `WalkSurface` / `AirWall` 等既有标记。

### 批量 Ensure（MW-02）

菜单（改 Prefab，不改玩法标记）：

- `Gravedigger2026/Maps/Ensure Flowing Water Layers (preserve paint)` — 缺层则补；已有 Water 砖则不重刷
- `Gravedigger2026/Maps/Ensure Flowing Water Layers (force Demo pond)` — 强制刷 Demo 菱形小池（mask=`BLACK TILE`，Foam=`WaterRipples 1`～`13`）

目标：`Ground_01`…`05`、`PushMap_Demo_01`…`03`。打开工程时 one-shot 会跑一次（preserve）。Batch：`-executeMethod Gravedigger2026.Editor.Maps.MapFlowingWaterBuilder.EnsureFlowingWaterBatch`。

## 地图边缘迷雾（MapEdgeFog）

遮可玩区外空白 + 氛围的 **地图表现约定**（非玩法规则）；权威见 SPEC_04 §13。**方案 A 已锁：世界空间边缘雾**，挂在各地图 Prefab 上。

与 `CameraFogOverlay`（全屏 UI 暗角）职责分离，可叠加：遮空白靠本约定，屏幕氛围可继续用既有镜头迷雾。

### 摆放约定

1. 父节点建议：`MapEdgeFog`（地图 Prefab 根下）；随地图 Instantiate/Destroy。
2. 相对 `DigMapBounds` / IsoDiamond：**外侧**放 1 个环状软边雾片，或至多 4 条边雾片（`SpriteRenderer` / alpha 渐变 Quad）。
3. Sorting：高于地面 / Water / Foam，低于单位与战斗特效。
4. 配置：Prefab `SerializeField`（贴图、颜色、Alpha、尺寸/偏移、启用），或共享 SO → `Assets/Settings/Maps/`；贴图可复用 `Fog_1.png` 等 `Art/Maps/Fog_*.png`。
5. **禁止**：粒子当遮罩、每帧 C# 动画、挂 `transform.root`、参与 NavMesh / 空气墙 / 占领。
6. **Transform 归属：** 默认不自动回写。手动摆位置/角度后，Inspector 保持 **Auto Fit To Bounds = 关**（进 Play 不会弹回）；若要按 `DigMapBounds` 重算，勾选 Auto Fit 或组件上调用 `FitToBounds()`。Ensure 对已有 `MapEdgeFog` **不**覆盖已摆姿势。

### Ensure（ME-01）

菜单：`Gravedigger2026/Maps/Ensure Map Edge Fog`  
（one-shot：打开工程会跑一次；Batch：`-executeMethod Gravedigger2026.Editor.Maps.MapEdgeFogBuilder.EnsureMapEdgeFogBatch`）

目标：`Ground_01`…`05`、`PushMap_Demo_01`…`03`。子物体名 `MapEdgeFog`，组件 `MapEdgeFogView`，默认 Sprite=`Fog_1.png`。

