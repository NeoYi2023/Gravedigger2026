# SearchExtract（搜打撤 / D-087）— Issue 索引



**选定方案：** A — 独立 `SearchExtractStageModule` + Session；复用 PushMap 地图标记与 §3.12 战斗管线  

**权威 SPEC：** [SPEC_03 §3.19](../../SPEC_03_GameRules.md) · [SPEC_04 §9.31～§9.33](../../SPEC_04_Technical.md) · [SPEC_04 §6](../../SPEC_04_Technical.md)  

**难度：** 3（须拆步；Demo 须另授权）  

**Changelog：** SPEC_00 v0.83.81



| ID | 文件 | 依赖 | 难度 | 状态 |

|----|------|------|------|------|

| SE-00 | [00-spec-close.md](issues/00-spec-close.md) | — | 3 | **done** |

| — | [workshop-config-fields.md](issues/workshop-config-fields.md) | SE-00 | — | **done**（2026-09-02 签字；解锁 SE-01） |

| SE-01 | [01-config-tables.md](issues/01-config-tables.md) | 字段工作坊签字 | 2 | **done**（方案 A） |

| SE-02 | [02-map-markers-sample.md](issues/02-map-markers-sample.md) | SE-00 | 2 | **done**（方案 B） |

| SE-03 | [03-stage-module-wire.md](issues/03-stage-module-wire.md) | SE-01 | 3 | **done**（方案 A 平行克隆） |

| SE-04 | [04-zone-countdown-activate.md](issues/04-zone-countdown-activate.md) | SE-03 | 3 | **done**（方案 A） |

| SE-05 | [05-formation-relocate.md](issues/05-formation-relocate.md) | SE-04 | 3 | **done**（方案 A） |

| SE-06 | [06-directional-wave-spawn.md](issues/06-directional-wave-spawn.md) | SE-05, SE-01 | 3 | **done**（方案 A） |

| SE-07 | [07-point-success-ui.md](issues/07-point-success-ui.md) | SE-06 | 2 | **done**（方案 A） |

| SE-08 | [08-multi-point-rewards.md](issues/08-multi-point-rewards.md) | SE-07 | 2 | **done**（方案 A） |

| SE-09 | [09-sample-handcheck.md](issues/09-sample-handcheck.md) | SE-08 | 2 | **done**（方案 A；D-087 Demo 手验仍须负责人勾选） |



**编号约定：** 文件名前缀 `NN-` 与 **SE-NN** 一一对应（`00` = SE-00）；`workshop-config-fields.md` 为 SE-01 前置工作坊，无 SE 编号。



**建议执行序：** SE-00 → **字段工作坊** → SE-01 → SE-02 ∥ SE-03（SE-03 须 SE-01）→ SE-04 → SE-05 → SE-06 → SE-07 → SE-08 → SE-09



**并行：** SE-02 可与 SE-01 并行（纯 Prefab 作者）；SE-03 须等 SE-01 加载器。



**不做：** 修改 PushMap `Capture` 语义；新增 `CampaignMode=Mode3`；本会话 SE-00 不编码。
