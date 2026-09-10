---
title: AM-RAIN-03 复活循环接线 + 手验
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.8 D-055
  - SPEC_03 §3.15 StepB/C
depends_on:
  - AM-RAIN-02
approach: A
---

## 目标

隐藏 SoldierScroll（保留）；逐兵闪红+书脉冲+问号变兵落到法阵下；播完上阵进 UM。

## 验收

- [x] Legacy 行默认隐藏；`AutoMfgUseLegacySoldierRow=1` 可切回
- [x] 书峰值仍 ApplyAtSlot（`CoPulseBook` 不变）
- [x] 复活与尸骸碰撞忽略（IgnoreLayerCollision）
- [ ] D-055 Play Mode 手验（负责人勾选）

## Play Mode 手验清单（负责人）

前置：Unity 打开工程刷新 Resources；Mode2 进档；Dig 攒够料进入 AutoManufacture。

| # | 步骤 | 期望 | 勾选 |
|---|------|------|------|
| 1 | 进入 AM 演出 | 无中央士兵传送带；可见书槽；躯体从顶部落下堆叠；`AmBodyRainLayer` Y≈**−340** | [ ] |
| 2 | 观察落下节奏 | 约每 0.3s 落 3～5 件；件数=本批消耗；躯体最长边≈49；转角不超过 ±270°；Layer 内地板按 Mode2 表（样例 0）；法阵在躯体下层 | [ ] |
| 3 | 堆下法阵 | `MagicCircle` 本地 Y≈**−50**；绘制低于躯体；每兵套书时闪红 | [ ] |
| 4 | 书脉冲 | 6 槽左→右；峰值套书（Console/属性变化） | [ ] |
| 5 | 复活 | 出生 Y≈**400**（底部 pivot）；问号→士兵（最长边 64 × scale8）→脚底落到 LandY（Mode2 样例 300）；`Shadow` 本地 Y≈**−32** | [ ] |
| 6 | 碰撞 | 士兵穿过尸骸堆；可与其它已复活兵相撞 | [ ] |
| 7 | 多兵循环 | 下一兵重复闪红+书+复活 | [ ] |
| 8 | 结束 | 上阵进 UM 并自动开布阵 | [ ] |
| 9 | 0 兵 | Tips「无士兵可制造」；跳过演出 | [ ] |
| 10 | Legacy | 常量 `AutoMfgUseLegacySoldierRow=1` 恢复旧传送带 | [ ] |

### v0.84.44 布局静态核对（Agent）

- Mode2 CSV：`BodyRainLayerYPx=-340`、`MagicCircleYPx=-50`、`ReviveSpawnOffsetYPx=400`、`ShadowOffsetYPx=-32`
- 代码：`ApplyLayerLayout` / `EnsureMagicCircle` 直写 PosY；`AmReviveSoldierPiece` pivot `(0.5,0)` + `FitSize`；落地 `pos.y=_landY`
- Play Mode 勾选仍由负责人完成
