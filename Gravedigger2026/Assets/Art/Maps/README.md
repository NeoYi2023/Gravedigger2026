Dig/Defend 共用地图源。
- `Tiles/`：Isometric Tile + Sprite（自 Example Scene `Environment/Tiles`+`Sprites` 复制；禁止运行时引用 `SmallScaleInt/`）。
- `Ground_0N/`：可选每图附加贴图；运行时 Instantiate 仍走 `Prefabs/Maps/Ground_0N.prefab`。
- Prefab 逻辑足迹为 **IsoDiamond**：半尺寸 = `PaintRadius*(cellSize.x,cellSize.y)`（Demo `cellSize≈(1,0.5)` → `(5,2.5)`）；`WalkSurface`/`EngageZone`/`DigMapBounds` 对齐砖面外轮廓。
- 打开 Unity 后建议跑一次菜单 `Gravedigger2026/Maps/Align IsoDiamond Footprints`（写入菱形 Mesh 资产）。
