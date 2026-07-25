# Demo 切片计划 — `demo-gameplay-pipeline`

负责人确认（2026-07-25）：

- **00 已完成（SPEC v0.30.0）**：Demo 验收已扩大；后续 `planned` 已改为 `in-scope`，可按序编码
- **01 已完成（SPEC v0.31.0）**：Meta 壳方案 A（PlayerPrefs / Boot / Prefab UI / Toast / Debug 切态）
- **02 已完成（SPEC v0.32.0）**：配置表 CSV 加载 + `LevelOperationDriver` 方案 A；Tools 关卡启 `Level_01`；UM ConfigId 忽略；MapId 仅解析/日志
- **03 已完成（SPEC v0.33.0）**：Dig 垂直切片方案 A（`DigStageModule` + `DigSessionService` + Prefab Catalog；DigStageSummary→推进阶段）
- **04a 已完成（SPEC v0.34.0）**：UM 升级区方案 A（`UpgradeManufactureStageModule` + `ProtagonistProgressService` + Debug 注入连升）
- **04b 已完成（SPEC v0.35.0）**：UM 制造区方案 A（`ManufactureService` 15 严格槽位 + 预览/精魂闸门 + `WarriorPoolService`；临时 `Prefabs/Defend/Warriors/{AppearanceId}`）
- **04c 已完成（SPEC v0.36.0）**：UM 布阵区方案 A（`BattleFormationService` 连续坐标 + `FormationPanelView`；与 Prepare 共用；控制力占用展示）
- 实现顺序偏好：**先 Meta 壳（S1），再玩法**
- 玩法推荐顺序（清单默认）：Dig → UpgradeManufacture → Defend；科技树可选后置

## 执行顺序

| 序 | Issue | 难度 | demo_scope | 阻塞 |
|----|-------|------|------------|------|
| 0 | [00-expand-demo-scope](issues/00-expand-demo-scope.md) | 1 | spec-gate（**done**） | — |
| 1 | [01-meta-shell](issues/01-meta-shell.md) | 2 | in-scope（**done**） | —（建议本片先于玩法） |
| 2 | [02-config-level-driver](issues/02-config-level-driver.md) | 2 | in-scope（**done**） | 00、01 |
| 3 | [03-dig-vertical](issues/03-dig-vertical.md) | 2 | in-scope（**done**） | 00、02 |
| 4a | [04a-um-upgrade](issues/04a-um-upgrade.md) | 2 | in-scope（**done**） | 00、03（Dig 产出材料/精魂更易验） |
| 4b | [04b-um-manufacture](issues/04b-um-manufacture.md) | 3 | in-scope（**done**） | 04a |
| 4c | [04c-um-formation](issues/04c-um-formation.md) | 2 | in-scope（**done**） | 04b |
| 5a | [05a-defend-prepare-shield](issues/05a-defend-prepare-shield.md) | 2 | in-scope | 04c |
| 5b | [05b-defend-spawn-path](issues/05b-defend-spawn-path.md) | 2 | in-scope | 05a |
| 5c | [05c-defend-warrior-combat](issues/05c-defend-warrior-combat.md) | 3 | in-scope | 05b |
| 5d | [05d-defend-losecontrol-settle](issues/05d-defend-losecontrol-settle.md) | 2 | in-scope | 05c |
| 6 | [06-tech-tree-optional](issues/06-tech-tree-optional.md) | 2 | in-scope（可选；非 §3.8 P0） | 可选；最早可在 03 后并行 |

## 约定

- 美术：临时资源进 Prefab；正式资源后由负责人替换
- 单会话最多实现 **一个** 无阻塞 issue
- **00 已关闭**：SPEC_03 §3.8 / SPEC_04 §6 已扩大；UM `GameplayConfigId`=忽略；Defend Demo 最小刷怪点/NavMesh 已写入
- **地图（SPEC v0.29.0）**：Dig `DigMapId` 与 Defend `BattleMapId` 共用 `Ground_01`…`Ground_05` → `Assets/Prefabs/Maps/{Id}.prefab`；已同步到 issues 00 / 02 / 03 / 04c / 05a / 05b
