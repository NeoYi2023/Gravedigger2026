# MonsterModel_08

怪物烘焙源。ModelId=`MonsterModel_08`。美术来源：`2D Zombie Pack 1` / `ZombieFemale2`（1920×1024，128px/格）。
Prefab：`Assets/Prefabs/Defend/Monsters/MonsterModel_08.prefab`（`Visual.localScale = 1` 与 128px 怪物对齐）。

离线烘焙：`python Tools/bake_monster_model_08_from_zombie.py` → `python Tools/regenerate_mm08_clip_guids.py`；Unity 内：`Tools/Gravedigger/Art/Repair Character Creator Export`（或 `-executeMethod Gravedigger2026.Editor.Art.CharacterCreatorExportRepair.RepairMonsterModel08Batch`）→ `Wire Monster Die2 Animators` → `Assemble Monster Model Prefabs`。

配置：`Monster_08`（血仆）→ `ModelId=MonsterModel_08`。
