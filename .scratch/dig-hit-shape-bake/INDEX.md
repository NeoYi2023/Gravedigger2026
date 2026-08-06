# Dig 命中形烘焙 — `dig-hit-shape-bake`

负责人确认：难度 **2**；**选定方案 B**（离线烘焙精灵轮廓 → 低顶凸包；规则层圆–凸包相交；与 `DigObstacleRadius` 分离）。

## 执行顺序

| 序 | Issue | 难度 | demo_scope | 阻塞 |
|----|-------|------|------------|------|
| 1 | [01-spec-bake-hit-shape](issues/01-spec-bake-hit-shape.md) | 2 | in-scope（**done**） | — |
| 2 | [02-runtime-circle-hull](issues/02-runtime-circle-hull.md) | 2 | in-scope（**done**） | 01 |

## 约定

- 一会话优先一片；本批按用户要求可连续做完 01+02
- issue 须含 `spec_refs`；不使用 `/to-prd`
