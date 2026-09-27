---
title: TFG-04 顶中阵型组卡牌、头顶图标与解散
status: done
difficulty: 2
demo_scope: in-scope
spec_refs:
  - SPEC_03 UI-035
  - SPEC_03 UI-030
  - SPEC_03 §3.11 布阵编辑器
  - SPEC_03 §3.8 D-093
  - SPEC_04 §13 Prefab 优先
approach: A
depends_on:
  - TFG-02
---

## 目标

已建组以卡牌出现在布阵画面顶部中间。点卡选中该组，预览里组成员头顶显示阵型图标，选中卡下方出现解散。

## 需求指令

预制体优先：卡牌条挂在 `FormationEditorRoot_Mode2` 的 `FormationCanvas` 顶中。Mode1 的 `FormationEditorRoot` 挂同一组件（否则 Mode1 无法选中或解散）。视口宽度正好放下 8 张卡；组数 ≤8 时不需要滑动；超过 8 张用横向 `ScrollRect`，左右都能滑。不要在运行时用代码从零搭整条层级（卡实例可以按数据生成，壳必须在 Prefab 上）。

一张卡对应一个 `GroupInstanceId`。卡面至少有：图标、`DisplayName`、当前阵型等级、人数。等级来自 TFG-02 快照（平均 `ClassLevel` 向下取整后再向下就近查表的那个表等级；无行时仍显示计算等级或「无加成」，须能看出这组没有属性行）。

点击卡：选中该组。士兵栏只高亮这些成员（这是原 D-085 高亮的新入口）。`FormationBattlefieldPreview` 在这些站桩头顶显示该阵型图标；取消选中或换卡时图标跟着走。图标只在布阵预览，不带进战斗。

「解散」只出现在选中卡的下方，并随该卡滚动。点击调用 `TryDisbandGroup`：成员回职业区，卡消失，清选中与头顶图标。

规则层不要直接改 `Transform`。预览图标由 View 读当前选中的 `GroupInstanceId`。

## 不做

- 改左缘按钮的数据源（TFG-03；可与本片并行，但不要互相把选中态写回按钮）
- 战斗头顶图标、战斗多中心（TFG-05）
- 职业筛选

## 验收

- [x] 建组后顶中出现卡；两组同名楔阵是两张卡，等级与人数可区分
- [x] 第 9 张卡需要横向滑动才能看到，且能滑回来
- [x] 点卡后只有该组成员士兵栏高亮，且预览头顶有阵型图标
- [x] 解散后该组站位回到职业区，卡与图标消失，其它组还在
- [x] Mode2 与 Mode1 都能选中并解散

## 依赖

- TFG-02（组快照与解散 API）
