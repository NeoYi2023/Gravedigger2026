# Art — 美术源素材根目录

- **用途**：存放各系统美术源（图、spritesheet、Clip、Controller、贴图、音效等）。
- **运行时**：游戏 Instantiate / Catalog 只引用 `Assets/Prefabs/<模块>/`，不要把 `Art/` 当作运行时唯一入口。
- **禁止**：游戏实际引用的成品不要落在 `Assets/SmallScaleInt/`（见 SPEC_04 §15）。
- **Sprites**：本版不另建顶层 `Sprites/`；2D 图标统一落在 `Art/UI/`（及 `Placeholder/` 过渡）。
