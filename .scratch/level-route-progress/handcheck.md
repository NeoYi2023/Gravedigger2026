# D-088 关卡路线进度存档 — 手验清单

**状态：** 编码已落地；须负责人 Play Mode 勾选

## 步骤

1. Mode2 进档 → 普通难度 → 通关 ≥1 子关卡 → 回路线图见 Cleared
2. 返回存档选 → 再进同一槽 → 打开同 LevelId → Cleared / 下一 Stage Selectable 与离开前一致
3. 切到其他 LevelId 页签再切回 → 进度仍在
4. 删档 → 新档该关仅 Stage1 解锁
5. 整关 Victory 后再进该关 → 已通关选项仍为 Cleared、不可再点

## 相关

- SPEC_03 §3.9 / §3.8 D-088
- `LevelRouteProgressService` + `LevelOperationDriver` 水合
