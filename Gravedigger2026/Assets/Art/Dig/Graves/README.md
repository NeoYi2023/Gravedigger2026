坟墓视觉父目录。子文件夹名 = Grave Prefab 逻辑名（Grave_Q1…Q20）。每品质可再放 IconStyle1/2/3（HP% 样式）。

## 源图约定（游戏可用）

- 文件：`Grave_Qn/Grave_Qn.png`，RGBA，透明底；勿留抠图绿边 / 残黑点。
- Unity 导入：Texture Type = **Sprite (2D and UI)**，Alpha Is Transparency = On，Pixels Per Unit = **100**（与现有 `.meta` 一致；**勿改 GUID**）。
- 换图后：Unity 菜单 `Gravedigger2026/Dig/Bake All Grave Hit Shapes` 重烘焙命中凸包。
- 批量清理脚本（可选）：仓库根目录 `python .scratch/tools/process_grave_sprites.py --only 1-15`。
