# OSH-02 — Stage 接线

**status:** done  
**spec_refs:** SPEC_03 §3.14 / §3.19；UI-034 / D-090

## 完成

- `SearchExtractStageController`：`HandleSpawnRequested` 后 `TryShow(basePos)`；与 CombatIndicator 同显隐
- `PushMapStageController`：`HandlePushMapSpawnRequested` 在 `Trigger != PreparePreview` 时 `TryShow(basePos)`；销毁世界时清 Hint Canvas
