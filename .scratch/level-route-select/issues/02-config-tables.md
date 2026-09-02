---
title: LRS-02 运作表改列 + 子关卡表 Excel/CSV
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_04 §9.1
  - SPEC_04 §9.31
  - SPEC_04 §14.7
depends_on:
  - LRS-01
---

## 目标

Mode1+Mode2：`Level_LevelOperationConfig` 改为 `GameplayOptionId1..5`；新建 `Level_SubLevelConfig`；旧线性行迁移为 1 选项/Stage 链；Mode2 Level_01 Stage2 双 Dig 分支。

## 验收

- [x] Mode1 Excel+CSV；Mode2 CSV + SubLevel Excel（运作表 Excel 若被占用需关闭后重跑脚本）
- [x] UnlockNext 指向 Stage+1；末选项空

## 落地

脚本：`.scratch/tools/lrs02_level_route_config.py`
