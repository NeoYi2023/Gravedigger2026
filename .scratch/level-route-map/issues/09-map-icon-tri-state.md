---
title: UI-031 map Icon tri-state verify
status: pending_playmode
spec_refs:
  - SPEC_03 §3.6 UI-031
  - SPEC_03 §3.8 D-086
  - SPEC_04 §6
---

## Play Mode 验收（负责人勾选）

- [ ] 未解锁钉点：Icon 明显变暗（RGB×0.4）、不可点、无 Checkmark、无脉冲
- [ ] 可选择钉点：慢闪（alpha）+ ±10% 缩放（≈1.6s）；悬停 Tips / 点击进关正常
- [ ] 已通关钉点：正常亮度 + Icon 下方内侧 Checkmark；不可再点
- [ ] 切 LevelId 页签 / 通关返回重建后，三态仍正确
- [ ] Stage 行回退模式：仍用卡片底色，不受地图 Icon 三态影响
- [ ] 缺 `Resources/UI/Icons/Checkmark` 时 Console Warning，不挡打开路线图
