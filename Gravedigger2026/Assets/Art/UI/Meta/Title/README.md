# Title screen background

登录主界面（UI-027）与存档选择（UI-001）共享的全屏背景图；同目录亦含商店 / 自动制造等全屏页背景。

## 建议命名

| 文件 | 用途 |
|------|------|
| `Title_Background_Static.png` | 当前静态背景（Builder 自动引用） |
| `Title_GameName.png` | 登录主界面顶中游戏名（`TitleMenuPanel/GameName`） |
| `Title_Shop_1.png` | Mode2 商店全屏背景（UI-026 / `ShopStageRoot/Background`；`AspectRatioFitter` EnvelopeParent） |
| `Title_AutoManufacture_1.png` | 自动制造演出全屏背景（UI-016） |
| `Item_Difficulty_1.png` / `Item_Difficulty_2.png` | 进档难度栏贴图（UI-029）；三栏同屏；`NormalColumn` 使用 `preserveAspect`；悬停显示描述；仅普通进 UI-031 |
| `Level_Map_1.jpg` | （历史）原 MapHost 地图底图；UI-029 改版后 **不再**使用 |

后续动图/分层素材可同目录追加；实现层 `TitleScreenBackground` 可扩展为多 Image 或 Animation。

## 关联

- Prefab：`Assets/Prefabs/Meta/TitleMenuPanel.prefab`、`MetaShellRoot` 内 `TitleScreenBackground` / `TitleMenuPanel/GameName`；进档壳 `InSaveShellPanel.prefab`（UI-029）
- 商店：`Assets/Prefabs/Shop/ShopStageRoot.prefab`；菜单 `Ensure Shop Background (UI-026)`
- 设置：`TitleSettingsPanel`（UI-028）— Title「设置」→「显示」页签；菜单 `Ensure TitleSettingsPanel (UI-028)`
- 进档难度 Hub：菜单 `Ensure InSaveShellPanel (UI-029)`
- 脚本：`TitleMenuView`、`TitleSettingsPanelView`、`DifficultySelectHostView`、`MetaShellAssetBuilder`、`ShopStageRootView`、`ShopAssetBuilder`
