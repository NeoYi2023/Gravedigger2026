# Demo 切片计划 — `demo-gameplay-pipeline`

负责人确认（2026-07-25）：

- 本轮：**只落 issues，不改 SPEC、不编程**
- 实现顺序偏好：**先 Meta 壳（S1），再玩法**
- 玩法推荐顺序（清单默认）：Dig → UpgradeManufacture → Defend；科技树可选后置

## 执行顺序

| 序 | Issue | 难度 | demo_scope | 阻塞 |
|----|-------|------|------------|------|
| 0 | [00-expand-demo-scope](issues/00-expand-demo-scope.md) | 1 | spec-gate | — |
| 1 | [01-meta-shell](issues/01-meta-shell.md) | 2 | in-scope（现行 §3.8） | 建议先完成 00，或至少与 00 同步验收文案 |
| 2 | [02-config-level-driver](issues/02-config-level-driver.md) | 2 | planned | 00、01 |
| 3 | [03-dig-vertical](issues/03-dig-vertical.md) | 2 | planned | 00、02 |
| 4a | [04a-um-upgrade](issues/04a-um-upgrade.md) | 2 | planned | 00、03（Dig 产出材料/精魂更易验） |
| 4b | [04b-um-manufacture](issues/04b-um-manufacture.md) | 3 | planned | 04a |
| 4c | [04c-um-formation](issues/04c-um-formation.md) | 2 | planned | 04b |
| 5a | [05a-defend-prepare-shield](issues/05a-defend-prepare-shield.md) | 2 | planned | 04c |
| 5b | [05b-defend-spawn-path](issues/05b-defend-spawn-path.md) | 2 | planned | 05a |
| 5c | [05c-defend-warrior-combat](issues/05c-defend-warrior-combat.md) | 3 | planned | 05b |
| 5d | [05d-defend-losecontrol-settle](issues/05d-defend-losecontrol-settle.md) | 2 | planned | 05c |
| 6 | [06-tech-tree-optional](issues/06-tech-tree-optional.md) | 2 | planned | 可选；最早可在 03 后并行 |

## 约定

- 美术：临时资源进 Prefab；正式资源后由负责人替换
- 单会话最多实现 **一个** 无阻塞 issue
- `planned` 项须在 **00** 将 SPEC_03 §3.8 / SPEC_04 §6 扩大后方可标为 `in-scope` 并编码
- **地图（SPEC v0.29.0）**：Dig `DigMapId` 与 Defend `BattleMapId` 共用 `Ground_01`…`Ground_05` → `Assets/Prefabs/Maps/{Id}.prefab`；已同步到 issues 00 / 02 / 03 / 04c / 05a / 05b
