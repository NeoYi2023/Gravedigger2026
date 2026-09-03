# Level Route Map（UI-031 底图 + 选项坐标）— Issue 索引



**选定方案：** C — 每关 Prefab `LevelRouteMap_{LevelId}` 承载底图 + `GameplayOptionId` 钉点；底图仍 Art + Resources；`RouteMapAssetId` 仍表驱动文件名  

**权威 SPEC：** [SPEC_03 §3.9](../../SPEC_03_GameRules.md) · [SPEC_03 UI-031 / D-086](../../SPEC_03_GameRules.md) · [SPEC_04 §9.1](../../SPEC_04_Technical.md) · [SPEC_04 §9.31](../../SPEC_04_Technical.md) · [SPEC_04 §2](../../SPEC_04_Technical.md)  

**难度：** 2（须拆步）  

**Changelog：** v0.83.95



| ID | 文件 | 状态 | 说明 |

|----|------|------|------|

| LRM-01 | [01-spec-close.md](issues/01-spec-close.md) | done | SPEC / CONTEXT / Changelog（方案 B 底图+表内 MapPos） |

| LRM-02 | [02-config-loader.md](issues/02-config-loader.md) | done | Excel 扩列 + Bake + Row/加载器/Snapshot |

| LRM-03 | [03-prefab-view.md](issues/03-prefab-view.md) | ready for handcheck | Prefab + View 竖图坐标；Resources；须跑 Ensure 菜单 |

| LRM-04 | [04-spec-prefab-pins.md](issues/04-spec-prefab-pins.md) | done | 方案 C SPEC 闭合：钉点改每关地图 Prefab |

| LRM-05 | [05-editor-per-level-map.md](issues/05-editor-per-level-map.md) | ready for handcheck | Editor Ensure/Sync `LevelRouteMap_{LevelId}` |

| LRM-06 | [06-runtime-drop-mappos.md](issues/06-runtime-drop-mappos.md) | ready for handcheck | 运行时读 Prefab 钉点；删子关卡 MapPos 列 |

| LRM-07 | [07-map-option-tips.md](issues/07-map-option-tips.md) | ready for handcheck | 地图模式仅 Icon；悬停 Tips 展示 Type/Title/Description/Reward |

| LRM-08 | [08-clear-return-camera.md](issues/08-clear-return-camera.md) | ready for handcheck | 通关返回：对准刚通关 → 0.5s → 平滑至新解锁前沿 |



**建议执行序：** LRM-01 → LRM-02 → LRM-03 → **LRM-04 → LRM-05 → LRM-06 → LRM-07 → LRM-08**



**坐标约定：** 底图左下 `(0,0)`，Y 向上；单位 = 展示宽 1450 UI 像素；点 = 选项卡中心。  

**底图：** 运作表 `RouteMapAssetId`（同 LevelId 首个非空）；源 `Art/UI/SubLevelMaps/`；运行时 `Resources/UI/SubLevelMaps/`。  

**钉点：** `Assets/Prefabs/Level/LevelRouteMap_{LevelId}.prefab` 子节点名 = `GameplayOptionId`（方案 C；子关卡表已无 MapPos）。


