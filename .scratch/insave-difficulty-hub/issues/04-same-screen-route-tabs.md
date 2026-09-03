# UI-029 / UI-031 难度三栏同屏 + 路线 LevelId 页签手验

## 前置

1. Unity 打开工程后跑菜单（写回 Prefab）：
   - `Gravedigger2026/Meta/Ensure InSaveShellPanel (UI-029)`（或等价 Ensure Meta 菜单）
   - `Gravedigger2026/Level/Ensure LevelRouteSelectRoot Prefab (UI-031)`
2. 进档 Mode2，或工具「关卡」打开 Hub。

## 清单

- [ ] 进档默认三栏（普通/困难/地狱）**同屏**左右排列，各约 1/3 视口
- [ ] 底部可见难度描述提示；鼠标悬停各栏显示对应描述
- [ ] 点困难 / 地狱 → Toast「还未制作」；不进入路线
- [ ] 点普通 → 打开 `LevelRouteSelectRoot`；Box 顶部有 LevelId 页签；默认末项关卡
- [ ] 切换页签 → 换关路线图（未开战时可切；开战中提示不可切）
- [ ] 点 Stage 可选项仍进入玩法；关闭路线（未开战）→ 回难度三栏
- [ ] 栏内**不再**嵌 `LevelSelectPanel` / 无「进入」底钮作为主路径
- [ ] 左下商店/装备/魔法书与右上工具位置不变

## 实现备注

- 脚本：`DifficultySelectHostView`、`InSaveShellView`、`LevelRouteSelectView`、`LevelRouteSelectRuntimeFactory`、`MetaShellController`、`MetaShellAssetBuilder`、`LevelRouteSelectAssetBuilder`
- SPEC：v0.83.82（SPEC_03/04/00、CONTEXT）
