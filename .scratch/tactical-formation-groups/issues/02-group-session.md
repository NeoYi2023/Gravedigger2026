---
title: TFG-02 手动阵型组会话与布阵存档
status: done
difficulty: 3
demo_scope: in-scope
spec_refs:
  - SPEC_03 §3.18 布阵手动建组
  - SPEC_03 §3.8 D-093
  - SPEC_04 §6 BattleFormationSaveData
  - SPEC_04 §9.7 Prepare
  - SPEC_04 §9.30 等级查表
approach: A
depends_on:
  - TFG-01
---

## 目标

用显式的组替换「每次布阵变更按 `FormationId` 重算唯一小队」。组能活过关闭编辑器与读档。左缘按钮与卡牌留给 TFG-03/04，本片用可调用的规则 API 验收。

## 需求指令

成员关系的主人是与 `BattleFormationService` 一起存档的组列表，不是 `FormationEditorController.Begin` 里新建又丢掉的 `TacticalFormationLayoutService`。

`BattleFormationSaveData` 增加组数组：`GroupInstanceId`、`FormationId`、成员 `WarriorId`、`FacingYawDegrees`。不存等级，不存中心（中心由成员坐标得到）。旧 JSON 没有该字段 = 没有组，禁止按站位猜组。

规则 API（纯 C#，名字可按工程习惯微调，语义不可变）：

- `TryCreateGroup(formationId)`：候选 = 已上阵、未入任何组、任一 `SoldierSkills` 的 `SkillConfig.FormationId` 等于该 Id（不要只看第一个阵型技能）。人数 &lt; Min 则返回失败且不改坐标。否则取 `min(候选, Max, 槽位数)`，排序仍为上阵先后再 `WarriorId`，在质心 snap 到 Pattern 槽位，朝向规则同现有首次激活。新 `GroupInstanceId`。
- 资格收集放在单独查询（例如 `TacticalFormationMemberQuery`）。以后加职业筛选只改这个查询，不改组 Id、存档字段和战斗键。本片不加职业列。
- `TryDisbandGroup(groupInstanceId)`：成员退回职业区螺旋，删除该组。
- 布阵变更只裁剪：下阵的人移出；剩余 &lt; Min 则整组走解散。不要把新上阵的闲兵吸进已有组，也不要按 `FormationId` 重排成员。裁剪保留 `FacingYawDegrees`。
- 拖拽与 Q/E 旋转按 `GroupInstanceId`（命中成员找到其所在组）。
- 等级 = `Floor(成员 ClassLevel 之和 / 人数)`，缺职业行按 0。属性行用 TFG-01 的向下就近查询，挂在组快照上供 UI/战斗读。无匹配行则组仍在，属性为空并 Warning。
- `AutoFormationDeployService` 与一键上阵去掉「上阵后自动 `EvaluateAndApply` 成组」。它们只负责放进职业区。

编辑器 `Begin` 从存档恢复组，不要在打开时自动建组。

## 不做

- 左缘按钮改数据源（TFG-03）
- 顶中卡牌与头顶图标（TFG-04）
- 战斗运行时改键（TFG-05）。本片快照仍可被旧 `BuildLocks` 读到，但若仍按 `FormationId` 去重，第二组会在战斗里被丢掉——那是 TFG-05 的范围，本片在注释或日志里点明即可，不要提前改 Runtime 键。

## 验收

- [x] 12 名持同一阵型技能、Min=3 Max=6：连续两次创建得到两组各最多 6 人；第三次候选不足 Min 则失败
- [x] 已入组的人不进入另一阵型或同阵型的下一组
- [x] 士兵同时持有两个阵型技能时，点第二个阵型仍可入选（未入组时）
- [x] 关闭再打开布阵编辑器，组还在；旧档读入后没有组
- [x] 下阵到剩余 &lt; Min：该组解散并退回职业区；另一组不动
- [x] 一键上阵 / 自动制造上阵不再自动 snap

## 依赖

- TFG-01（身份行与等级行已能加载）
