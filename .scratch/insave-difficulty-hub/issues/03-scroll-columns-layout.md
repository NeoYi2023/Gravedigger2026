# UI-029 / D-081 三栏横滑手验

## 前置

1. Unity 打开工程后跑一次菜单：`Gravedigger2026/Meta/Ensure InSaveShellPanel (UI-029)`（写回 Prefab；未跑时运行时也会在 `NormalLevelHost` 为空时自动重建层级）。
2. 进档 Mode2 或工具「关卡」打开 Hub。

## 清单

- [ ] 无中央 `MapHost` / 无 `Level_Map_1` 地图底图
- [ ] 三栏等宽同一行，可左右滑动
- [ ] 默认普通栏居中（占满视口），栏内可见 `LevelSelectPanel` 关卡按钮
- [ ] 困难/地狱栏内无关卡按钮；点栏 → Toast「还未制作」
- [ ] 选普通时 Hard/Hell **不**缩小、**不**变暗
- [ ] 自动选列表末项 LevelId；「进入」→ Stage 1
- [ ] 左下商店/装备/魔法书与右上工具位置不变

## 实现备注

- 脚本：`DifficultySelectHostView`、`InSaveShellView`、`MetaShellAssetBuilder`
- SPEC：v0.83.71（SPEC_03/04/00、CONTEXT）
