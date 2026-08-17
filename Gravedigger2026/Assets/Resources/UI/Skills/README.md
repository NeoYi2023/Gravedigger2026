# 士兵技能图标 / Soldier skill icons

投放目录（SPEC_04 §2 / UI-021 / D-065）。

- 文件名 = `SkillId`（无前缀），例如 `Skill_01.png`
- 运行时：`Resources.Load<Sprite>("UI/Skills/" + SkillId)`
- 缺图：悬浮框仍显示技能名，图标槽留空
- **不要**用 `SkillConfig.IconAssetId` 作为本目录路径

导入时请设 Texture Type = Sprite (2D and UI)。
