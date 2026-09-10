# OSH-03 — Play Mode 手验清单

**status:** open  
**spec_refs:** SPEC_03 UI-034 / D-090；SPEC_00 v0.84.47  
**难度：** 1

由负责人在 Unity Play Mode 勾选。

## 搜打撤（SearchExtract）

- [ ] Combat 中屏外 `SpawnPoint` 波次刷怪 → 对应屏幕边缘出现 `EnemyAttack_1`
- [ ] 刚出现时 **0.4s 内闪红 2 次**（白↔红，非透明度闪）
- [ ] 随后 **常亮约 2s** 后消失（总约 2.4s）
- [ ] 显示期间图标约 **放大 30%**（`localScale=1.3`）
- [ ] 屏内 SpawnPoint 刷怪 → **无**边缘图标
- [ ] 同帧/连续多组屏外刷怪 → 可各出一枚、互不抢生命周期
- [ ] UI-032 决策 / Ended → 进行中提示被清空

## 推图（PushMap）

- [ ] Prepare 预览刷怪 → **无**边缘图标
- [ ] 开战 `StartBattle` 屏外刷怪 → 有边缘图标（闪红 2 次 + 常亮 2s + ×1.3）
- [ ] 陷阱 `Trap` 屏外刷怪 → 有边缘图标
- [ ] 屏内刷怪 → 无图标
- [ ] 离开 Combat / 结算弹窗 → 无残留图标

## 备注

- 常量可在 `Combat_CombatConstantConfig` 调 `OffScreenSpawnHintIntroBlink*` / `HoldSeconds` / `DisplayScale` 后重进关验证
- 缺 `Resources/UI/Icons/EnemyAttack_1` 时应仍见空框闪红/常亮（不应抛异常）
