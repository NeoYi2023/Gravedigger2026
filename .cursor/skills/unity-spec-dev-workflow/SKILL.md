---
name: unity-spec-dev-workflow
description: >-
  Unity SPEC-first 开发流程、C# 编程规范与可扩展性约定。SPEC 优先、预制体优先、
  ScriptableObject 配置驱动、规则/表现分离、MonoBehaviour 结构与性能底线。
  在用户提出功能开发、Unity 实现、编写/审查 C# 脚本、创建 Prefab/脚本/场景、
  代码规范/重构/性能优化、Plan 模式制定计划、难度分级、工作量预估，或提及
  SPEC / Demo 开发时使用。
---

# Unity SPEC 开发流程

SPEC 文档是**唯一权威**；本 Skill 将其操作化为 Agent 执行步骤。详细规则见 [spec-map.md](spec-map.md) 及对应 SPEC 文件。编程难度与工作量预估权威定义见 [SPEC_01 §7](../../SPEC_01_Workflow.md)。

## 0. Agent Skills 编排

与 [mattpocock/skills](https://github.com/mattpocock/skills) 整合（可选）；路由见 [agent-router](../agent-router/SKILL.md)，入口见 [AGENTS.md](../../AGENTS.md)。

**每次任务启动：**

1. 读 [AGENTS.md](../../AGENTS.md) 与 [CONTEXT.md](../../CONTEXT.md)（术语表，非规则正文）
2. 不确定 flow → `agent-router`

| 阶段 / 场景 | Skill |
|-------------|-------|
| 规则录入，规则模糊 | `/grill-with-docs` 或 `spec-grill-me` → SPEC + CONTEXT + Changelog |
| 规则录入，规则已清晰 | 本 Skill §1 |
| Demo 多任务拆分 / 须拆步工作量 | `/to-issues` → `.scratch/<feature>/issues/`（须 `spec_refs`） |
| Demo 单任务编码 | §8 工作量+难度门禁 → §2 |
| 会话过长 / 换模型 | `/handoff` + `.scratch/handoffs/` 摘要 |
| 硬 bug / 性能 | `diagnosing-bugs`（自动） |

**强制：** 不使用 `/to-prd`、上游 `/implement`。编码仍走 §2。**TDD**（`/tdd`）延后至 Demo 开发启动且 SPEC_04 测试约定就绪。

## 1. 启动前检查（必须）

开始任何 Unity 实现前：

1. **确认阶段** — 读 [SPEC_01](../../SPEC_01_Workflow.md)：
   - 规则录入：禁止写代码（紧急修复除外，事后补 SPEC）
   - Demo 开发：须负责人明确授权 + SPEC_03 Demo 验收标准就绪
   - 正式开发：Demo 验收通过后在 SPEC 中定义范围
2. **定位 SPEC 章节** — 识别需求对应的 SPEC_03 / SPEC_04 章节
3. **无 SPEC 则先写 SPEC** — 更新文档、Changelog（[SPEC_00](../../SPEC_00_Index.md)）；中英文双块同步
4. **读相关技术约定** — 至少浏览 [SPEC_04 §13](../../SPEC_04_Technical.md) 及需求所属模块章节
5. **工作量与难度门禁** — 若任务需要编程开发（见 [SPEC_01 §7](../../SPEC_01_Workflow.md)）：
   - 自评工作量（可单次完成 / 须拆步）与难度 1~3，并说明理由
   - **须拆步** → **停止整包编码**，输出分步需求指令 + `/to-issues`（[§8.0](#80-工作量预估)）；本会话最多实现第一个已批准切片
   - **难度 1**（且可单次完成）→ 继续 §2
   - **难度 2/3** → **停止编码**，进入 [§8 方案比选](#8-编程难度与方案比选)

## 2. 开发执行顺序

```
读 SPEC → 更新 SPEC（含 Changelog）→ 规划资源路径 → 实现代码/资源 → 验证 Demo 边界 → 回复变更摘要
```

涉及配置表时，在「实现代码/资源」前插入：**Excel（保留三行表头）→ Bake → CSV**（详见 [SPEC_04 §14.7](../../SPEC_04_Technical.md)）。

设计问题在实现中发现时：**先更新 SPEC，再改代码**。

## 3. 资源编排决策树

按顺序判断；详细原则见 [SPEC_04 §13](../../SPEC_04_Technical.md)。

| 问题 | 是 → 做法 |
|------|-----------|
| 运行时多次 **Instantiate**，或多 Scene 复用？ | **Prefab** + Controller；路径 `Assets/Prefabs/<模块>/` |
| 策划可调数值/模板/表项？ | **配置表** → `Assets/ConfigTables/`（Excel+CSV，见 [SPEC_04 §14](../../SPEC_04_Technical.md)）；非表型单例 → **ScriptableObject** → `Assets/Settings/<模块>/` |
| UI 显示文本？ | 本地化 Key（若项目启用 i18n） |
| 高频 spawn/destroy？ | **对象池** |
| 涉及玩法状态变更？ | **规则 Service/Controller** + 事件通知 View；View 不驱动规则 |
| Scene 唯一常驻 Manager？ | 可 Scene 直挂；脚本放 `Assets/Scripts/Core/` |

**禁止：**
- 多 Scene 手工复制同一 GameObject 层级
- 脚本中硬编码策划数据
- `GameObject.Find` / 字符串路径查找（Manager 入口除外）
- 规则层直接操作 `Transform` / `Animator`

## 4. 目录与命名速查

工程与 Assets 路径以 [SPEC_04 §1–§2](../../SPEC_04_Technical.md) 为准。

| 类型 | 路径 |
|------|------|
| 脚本 Core | `Assets/Scripts/Core/` |
| 脚本 Gameplay | `Assets/Scripts/Gameplay/<模块>/` |
| 脚本 UI | `Assets/Scripts/UI/` |
| 预制体 | `Assets/Prefabs/<模块>/` |
| 配置表（Excel 源） | `Assets/ConfigTables/Excel/` |
| 配置表（CSV 产物） | `Assets/ConfigTables/Csv/` |
| 非表型 SO | `Assets/Settings/<模块>/` |
| 场景 | `Assets/Scenes/` |
| 本地化 | `Assets/Localization/` |

命名：类/方法 PascalCase；命名空间见 SPEC_04；文件名与主类名一致。

## 5. Unity C# 编程规范

**权威顺序：** [SPEC_04](../../SPEC_04_Technical.md) > 本 Skill > [Unity 官方 C# Style Guide](https://unity.com/resources/c-sharp-style-guide-unity-6)

### 5.1 命名（SPEC_04 §3）

| 类别 | 约定 |
|------|------|
| 类 / 方法 / 公共成员 | PascalCase |
| 私有字段 | camelCase，可选 `_` 前缀 |
| 接口 | `I` + PascalCase |
| 枚举 | PascalCase |
| 常量 | PascalCase 或 UPPER_SNAKE_CASE |
| 命名空间 | 以 SPEC_04 为准 |
| 文件名 | 与主类名一致 |

不强制 `m_`/`k_` 前缀；与工程内已有代码保持一致。

### 5.2 MonoBehaviour 结构

```csharp
// 推荐顺序：常量 → SerializeField → 私有字段 → 属性 → Unity 生命周期 → 公共 API → 私有方法
public class ExampleController : MonoBehaviour
{
    [SerializeField] private SomeConfig _config;

    private void Awake() { /* 缓存 GetComponent，仅一次 */ }
    private void OnEnable() { /* 订阅事件 */ }
    private void OnDisable() { /* 取消订阅，与 OnEnable 成对 */ }
    private void Update() { /* 避免每帧分配 */ }
}
```

### 5.3 架构原则

| 原则 | 做法 |
|------|------|
| 单一职责 | 一个 MonoBehaviour 一类职责；复杂逻辑抽到纯 C# Service |
| 依赖倒置 | 玩法依赖输入等接口，不依赖具体实现 |
| 规则/表现分离 | Service/Controller 发事件；View 只订阅展示 |
| 组合优于继承 | Prefab 拼装组件 |
| Inspector 引用 | `[SerializeField]` 或 Prefab 槽位，不用运行时查找 |

**禁止：** `GameObject.Find`（Manager 除外）；规则层直接操作 `Transform`/`Animator`；硬编码策划数值；玩法直接读 `Input.GetKey`。

### 5.4 性能底线

- 缓存引用；`Update` 无分配；高频对象池化；主线程访问 Unity API

### 5.5 通用原则

- Demo：优先可读性与可验证性
- **最小 diff**；关键逻辑须有 SPEC 支撑

### 5.6 代码审查清单

- [ ] 命名与命名空间符合 §5.1
- [ ] 关键逻辑有对应 SPEC 章节
- [ ] UI 字符串走本地化 Key（若启用）
- [ ] 依赖通过 SerializeField / 接口，非 Find
- [ ] 玩法不直接读 `Input.GetKey`
- [ ] `OnEnable`/`OnDisable` 成对
- [ ] `Update` 无多余分配
- [ ] 最小 diff
- [ ] 配置表变更已同步 Excel+CSV，未删三行表头（[SPEC_04 §14.7](../../SPEC_04_Technical.md)）

## 6. 完成后检查

- [ ] SPEC 与 Changelog 已更新（若设计有变）
- [ ] 未超出 SPEC_03 Demo 边界
- [ ] 通过 §5.6 审查清单
- [ ] 工作量已评估；须拆步已发 `.scratch` issues
- [ ] 难度 2/3 已在方案选定后编码
- [ ] 配置表变更：Excel 已改、已 Bake、摘要列出路径（[SPEC_04 §14.7](../../SPEC_04_Technical.md)）
- [ ] 回复摘要：SPEC 章节、新建 Prefab/SO/脚本路径

## 7. 延伸阅读

功能域 → SPEC 映射见 [spec-map.md](spec-map.md)。实现前按需 `Read` 对应 SPEC，勿凭记忆臆造规则。

## 8. 编程难度与方案比选

**权威定义：** [SPEC_01 §7](../../SPEC_01_Workflow.md)

**总则：**

- **默认提问通道：`AskQuestion`**（见 [ask-question-default.mdc](../../rules/ask-question-default.mdc)）
- 难度 2/3：**不得写代码**，直至负责人选定方案
- 题序：澄清 → **workload_split（仅须拆步）** → `task_difficulty` → `implementation_approach`（最后）

**强制流程：**

```
分析需求与 SPEC → 自评工作量 + 难度
  → 须拆步：分步需求指令 + /to-issues → AskQuestion 确认分片 → 本会话仅第一片
  → AskQuestion 确认该切片难度
  → 难度 1：可直接编码（SPEC 优先）
  → 难度 2/3：AskQuestion 比选 2~3 种方案 → 负责人选定 → 再编码
```

仅当本会话无 `AskQuestion` 工具时，才允许短纯文本回退，并注明「AskQuestion 不可用」。

### 8.0 工作量预估

详见 [SPEC_01 §7.5](../../SPEC_01_Workflow.md)。难度 **3** 一律视为须拆步。

```yaml
- id: workload_split
  prompt: "工作量判定为须拆步（{brief_reason}）。已准备 {n} 个垂直切片。请确认："
  options:
    - id: "approve_first"
      label: "确认分片；本会话只做第一个无阻塞切片"
    - id: "issues_only"
      label: "先只发布 issues / 分步指令，本会话不编码"
    - id: "adjust"
      label: "粒度需调整（我会在聊天中说明）"
```

### 8.1–8.2 难度确认

```yaml
- id: task_difficulty
  prompt: "通过我的分析，这个需求的难度为 {agent_assessed_level} 级（{brief_reason}）。请确认难度分级："
  options:
    - id: "1"
      label: "1 — 简单：范围小、改动集中，常规模型即可"
    - id: "2"
      label: "2 — 有一定难度：跨模块或需补 SPEC，建议谨慎推进"
    - id: "3"
      label: "3 — 十分困难：建议使用最强 AI 模型进行编程"
```

### 8.3 方案比选（难度 2/3 强制）

输出 2~3 种方案，用 `AskQuestion` 选定；细节写在回复或 Plan 正文。选定后记录 `选定方案：X`，再编码。

```yaml
- id: implementation_approach
  prompt: "请从以下实现方案中选择一种（难度 {level}）："
  options:
    - id: "A"
      label: "方案 A — {name}：{one_line_summary}"
    - id: "B"
      label: "方案 B — {name}：{one_line_summary}"
    - id: "C"
      label: "方案 C — {name}：{one_line_summary}"
```
