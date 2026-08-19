---
title: Mode2 商店 — `ShopStageRoot` UI（UI-026）
status: done
difficulty: 2
demo_scope: 进入 Mode2 后在 InSaveShell 打开全屏商店，展示玩家信息/6 槽商品/刷新与购买按钮
spec_refs:
  - SPEC_03 §3.5（InSaveShell 左下按钮堆叠新增「商店」）
  - SPEC_03 §3.5 Mode2 商店系统（本片规则）
  - SPEC_03 §3.6 UI-026 商店全屏界面
  - SPEC_04 §10 Mode2 商店系统：`ShopStageRoot` prefab 与 View 责任
selected_approach: 全屏 Prefab 实例化/销毁（ShopStageRoot）
---

## 目标

1. 新建 `ShopStageRoot` 全屏 prefab（`m_IsActive` 初始隐藏或由 View 控制）。
2. UI 布局固定语义：
   - 左侧：玩家信息（精魂总值 SpiritEssence、装备栏摘要、魔法书栏 6 槽占用）
   - 右侧：待售商品区（6 项 slot0..5；归类 A/B 分区显示；每项展示图标/名称/价格）
   - 底部：刷新商品按钮（显示下一次刷新价格；不可用时置灰/禁用）
3. 绑定交互：
   - 点击刷新 → 触发 service 刷新请求
   - 点击购买 → 触发 service purchase 请求
   - 关闭 → 销毁实例并恢复 InSaveShell

## 约束

- UI 不直接实现抽样/扣款逻辑；只通过 Service 读取当前 state 并派发事件。
- 图标/名称来源使用配置/ItemCatalog 解析后的结果（不要在 View 内写死资源路径）。

## 验收关注点

- UI-026：布局可见性、按钮可用性（依赖 refresh 行缺失/slot sold 状态）。
- 关闭再打开：依据 `ShopProgress` 恢复 sold/empty 状态与当前 offers。

## 完成情况

1. `InSaveShellView` 已增加“商店”入口（运行时克隆装备按钮），并在 `MetaShellController` 中串接 `ShopRequested`。
2. 新增 `ShopStageRootView`：纯代码搭建全屏 UI（玩家信息 + 6 槽商品 + 刷新/购买 + 关闭销毁）。
3. UI 打开时会调用 `ShopOfferRefreshService.TryAutoRefreshOnceIfPending` 消化 `pendingOpenOnNewUnlock`，避免重复生成；刷新/购买点击均派发到对应 Service。

