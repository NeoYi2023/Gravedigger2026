Dig/Defend 共用地图源。
- `Tiles/`：Isometric Tile + Sprite（自 Example Scene `Environment/Tiles`+`Sprites` 复制；禁止运行时/已提交 Prefab 引用 `SmallScaleInt/`）。
- `RuleTiles/`：Isometric Rule Tile（MapAutoTile 自动接边刷笔；见下「地图自动接边」）。
- `Palettes/FantasyTileset.prefab`：可由 `FantasyTilesetPaletteBuilder` 从 Environment/Tiles 重建（非刷图权威盘）。
- `Palettes/FantasyTileset_A.prefab`：**权威刷图 Palette**（MapAutoTile / 日常刷图选此资产）。
- **格子校正（可重复）：** 菜单 `Gravedigger2026/Maps/Correct FantasyTileset_A From FantasyTileset`（批跑：`-executeMethod Gravedigger2026.Editor.Maps.FantasyTilesetALayoutAligner.CorrectFantasyTilesetAFromFantasyTileset`）。按厂商盘 `SmallScaleInt/.../Environment/FantasyTileset` 的 **Tile 资源名** 把 Art 同名砖摆回对应坐标（不复制 SSI 的 Sprite 缓存偏移）；SSI 没有的 Art 砖溢到参考包围盒右侧；特例 **`Ground F4_W`** 原地保留（SSI 无此砖则不删）；`RT_WallA` 重钉固定槽。跑完后须**重开 Tile Palette** 窗口。旧菜单 `Align FantasyTileset_A Layout From SSI` 会调用同一实现。
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
