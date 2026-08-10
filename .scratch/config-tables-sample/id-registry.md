# 配置表案例 — 共享 Id 登记表

供分片 1–5 外键对齐。**禁止**自行发明未登记主键（尤其 `MaterialId` / `BodyPartId` 同命名空间）。

约定：Id 一律 string 语义；资源字段用占位路径即可。

---

## Level / Dig / Defend 玩法键

| 用途 | Ids |
|------|-----|
| LevelId | `Level_01`, `Level_02`, `Level_03` |
| DigGameplayConfigId | `Dig_01` … `Dig_10` |
| DefendGameplayConfigId | `Defend_01` … `Defend_10` |
| WaveConfigId | `Wave_01`, `Wave_02` |
| DigMapId / BattleMapId（共用地面变体） | `Ground_01` … `Ground_05`（废弃原 `BattleMap_01`…`BattleMap_10`） |

## 坟墓品质

`Q1` … `Q10`

## 材料（MaterialConfig）— 勿与 BodyPart 冲突

`Iron`, `Bone`, `Wood`, `Stone`, `Cloth`, `Copper`, `Silver`, `Gold`, `Crystal`, `Resin`

## 货币（CurrencyConfig）

`Spirit`（保留精魂）, `Coin`, `Token`, `Shard`, `Dust`, `Mark`, `Seal`, `Emblem`, `Badge`, `Ticket`

## 职业（ClassConfig）

| ClassId | ClassName（示例） |
|---------|-------------------|
| `Class_Servants` | 仆从 |
| `Class_Warrior` | 战士 |
| `Class_Archer` | 射手 |
| `Class_Mage` | 法师 |
| `Class_Knight` | 骑士 |
| `Class_Rogue` | 盗贼 |
| `Class_Priest` | 牧师 |
| `Class_Berserker` | 狂战士 |
| `Class_Ranger` | 游侠 |
| `Class_Warlock` | 术士 |
| `Class_Paladin` | 圣骑士 |

## 种族（RaceConfig）

`Race_Human`, `Race_Elf`, `Race_Orc`, `Race_Undead`, `Race_Dwarf`, `Race_Goblin`, `Race_Demon`, `Race_Angel`, `Race_Beast`, `Race_Construct`

## 灵魂（SoulConfig）

`Soul_00`（系统默认：无灵魂制造；`ClassId=Class_Servants`）、`Soul_01` … `Soul_10`（建议依次绑定上表 Class 顺序）

## 躯体材料（BodyPartConfig）— 与 Material 同命名空间

`BP_Head_Human`, `BP_Torso_Human`, `BP_Arm_Elf`, `BP_Leg_Elf`, `BP_Head_Orc`, `BP_Torso_Orc`, `BP_Arm_Undead`, `BP_Leg_Dwarf`, `BP_Head_Demon`, `BP_Torso_Angel`

## 躯体外观（BodyAppearanceConfig）

`App_01` … `App_10`（含各主要种族保底；`App_02` → `Race_Construct`）

## 宝石（GemConfig）

`Gem_Ruby_01`, `Gem_Sapphire_01`, `Gem_Emerald_01`, `Gem_Topaz_01`, `Gem_Amethyst_01`, `Gem_Diamond_01`, `Gem_Ruby_02`, `Gem_Sapphire_02`, `Gem_Emerald_02`, `Gem_Topaz_02`

## 额外装备（ExtraEquipmentConfig）

`Equip_Mount_01`, `Equip_Mount_02`, `Equip_Mount_03`, `Equip_Mount_04`, `Equip_Mount_05`, `Equip_Wing_01`, `Equip_Wing_02`, `Equip_Wing_03`, `Equip_Wing_04`, `Equip_Wing_05`

## 宝石后缀 ComboKey（GemSuffixNameConfig）

按 GemType **字典序**拼接。预登记 10 个：

1. `Ruby`
2. `Sapphire`
3. `Emerald`
4. `Topaz`
5. `Amethyst`
6. `Diamond`
7. `Amethyst|Ruby`
8. `Diamond|Sapphire`
9. `Emerald|Topaz`
10. `Amethyst|Diamond|Ruby`

## 怪物（MonsterConfig）

`Monster_01` … `Monster_10`  
ModelId 占位：`MonsterModel_01` … `MonsterModel_10`

## 技能（SkillConfig / SkillEffectConfig）

`Skill_01` … `Skill_10`（样例默认 `SkillLevel=1`）  
`SkillEffect_01` … `SkillEffect_10`

## 失控档（LossOfControlConfig）

仅 `TierId` = `1`, `2`, `3`, `4`（不可凑 10 行）

## 科技（TechTree / TechEffect）

`Tech_Root`, `Tech_DigDamage`, `Tech_DigSpeed`, `Tech_DigRadius`, `Tech_DigDuration`, `Tech_Key_01`, `Tech_Normal_01`, `Tech_Normal_02`, `Tech_Capstone`, `Tech_Leaf`

## 主角等级

`Level` 主键 = `1` … `10`（整数，非字符串前缀）
