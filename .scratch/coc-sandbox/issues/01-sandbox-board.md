---
title: 沙盘界面与难度导航
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.20
  - SPEC_03 §3.8 D-098
  - SPEC_04 §9.1c
---

难度三栏、占用档进入、关卡结束都打开沙盘。方格来自 `Level_SandboxNodeConfig`，按难度横排，次数只展示。

## Acceptance

- [x] Excel 三行表头保留；Mode1 / Mode2 均有 Excel 与 Bake 后的 CSV
- [x] 任意难度点击进入对应沙盘，不再打开 UI-031
- [x] 占用档进入打开 `Diff_Normal` 沙盘
- [x] 返回回到难度选择
