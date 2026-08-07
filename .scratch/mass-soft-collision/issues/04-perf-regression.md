# SC-04 — 200v200 性能回归

**状态：** todo  
**难度：** 2  
**依赖：** SC-03；复用 MP-07 压测入口

## 目标

SoftCollision 并入后，移动逻辑仍导向 ≤~2.5 ms/帧；超预算走既有回退序，并可增「降 repulsion 邻域半径 / 加大 SoftCollision 分帧」。

## 范围

- 扩展 `MassPathingPerfStress` 含 SoftCollision.Tick
- 记录对比数字（粘贴到 issue 评论或 SPEC）

## 不做

- 新功能

## 验收

- Editor Menu 可跑；数字可复现；无 Follow 相关分配
