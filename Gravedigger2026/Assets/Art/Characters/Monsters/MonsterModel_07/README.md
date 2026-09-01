# MonsterModel_07

怪物烘焙源。ModelId=`MonsterModel_07`。美术来源：`2D Zombie Pack 1` / `ZombieFemale6`（1920×1024，128px/格）。
Prefab：`Assets/Prefabs/Defend/Monsters/MonsterModel_07.prefab`（`Visual.localScale = 1` 与 128px 怪物对齐）。

离线烘焙：`python Tools/bake_monster_model_07_from_zombie.py` → `python Tools/regenerate_mm07_clip_guids.py`；Unity 内：`Tools/Gravedigger/Art/Repair Character Creator Export`（或 `-executeMethod Gravedigger2026.Editor.Art.CharacterCreatorExportRepair.RepairMonsterModel07Batch`）→ `Wire Monster Die2 Animators` → `Assemble Monster Model Prefabs`。

配置：`Monster_07`（骨龙幼崽）→ `ModelId=MonsterModel_07`。
