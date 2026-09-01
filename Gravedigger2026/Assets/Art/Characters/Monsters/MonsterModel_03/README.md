# MonsterModel_03

怪物烘焙源。ModelId=`MonsterModel_03`。美术来源：`2D Zombie Pack 1` / `ZombieMonster3`（2880×1536，192px/格）。
Prefab：`Assets/Prefabs/Defend/Monsters/MonsterModel_03.prefab`（`Visual.localScale = 2/3` 与 128px 怪物对齐；Root `(3.5,3.5,3)` Boss 体型）。

离线烘焙：`python Tools/bake_monster_model_03_from_zombie.py`；Unity 内：`Tools/Gravedigger/Art/Repair Character Creator Export` → `Wire Monster Die2 Animators` → `Assemble Monster Model Prefabs`。

批跑 Repair：`-executeMethod Gravedigger2026.Editor.Art.CharacterCreatorExportRepair.RepairMonsterModel03Batch`
