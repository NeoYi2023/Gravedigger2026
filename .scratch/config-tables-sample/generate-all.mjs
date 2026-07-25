/**
 * Generate all sample config tables (Excel + CSV) per SPEC_04 §9 / §14.
 * Run: node generate-all.mjs
 */
import XLSX from "xlsx";
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, "../../Gravedigger2026/Assets/ConfigTables");
const EXCEL_DIR = path.join(ROOT, "Excel");
const CSV_DIR = path.join(ROOT, "Csv");

fs.mkdirSync(EXCEL_DIR, { recursive: true });
fs.mkdirSync(CSV_DIR, { recursive: true });

function writeTable(excelBase, csvBase, headers, rows) {
  const aoa = [headers, ...rows.map((r) => headers.map((h) => (r[h] ?? "")))];
  const wb = XLSX.utils.book_new();
  const ws = XLSX.utils.aoa_to_sheet(aoa);
  XLSX.utils.book_append_sheet(wb, ws, "Sheet1");
  const xlsxPath = path.join(EXCEL_DIR, `${excelBase}.xlsx`);
  XLSX.writeFile(wb, xlsxPath);

  const csvLines = aoa.map((line) =>
    line
      .map((cell) => {
        const s = cell === null || cell === undefined ? "" : String(cell);
        if (/[",\n\r]/.test(s)) return `"${s.replace(/"/g, '""')}"`;
        return s;
      })
      .join(",")
  );
  const csvPath = path.join(CSV_DIR, `${csvBase}.csv`);
  fs.writeFileSync(csvPath, csvLines.join("\n") + "\n", "utf8");
  console.log("OK", csvBase);
}

const icon = (id) => `Icons/${id}`;
const asset = (id) => `Assets/Art/Placeholder/${id}`;
const outline = (q) => `Outline_${q}`;

// ——— Slice 1: Level + Dig ———
{
  const headers = ["LevelId", "StageNumber", "GameplayType", "GameplayConfigId"];
  const rows = [
    { LevelId: "Level_01", StageNumber: 1, GameplayType: "Dig", GameplayConfigId: "Dig_01" },
    { LevelId: "Level_01", StageNumber: 2, GameplayType: "UpgradeManufacture", GameplayConfigId: "Dig_02" },
    { LevelId: "Level_01", StageNumber: 3, GameplayType: "Defend", GameplayConfigId: "Defend_01" },
    { LevelId: "Level_02", StageNumber: 1, GameplayType: "Dig", GameplayConfigId: "Dig_03" },
    { LevelId: "Level_02", StageNumber: 2, GameplayType: "Defend", GameplayConfigId: "Defend_02" },
    { LevelId: "Level_02", StageNumber: 3, GameplayType: "Dig", GameplayConfigId: "Dig_04" },
    { LevelId: "Level_03", StageNumber: 1, GameplayType: "Dig", GameplayConfigId: "Dig_05" },
    { LevelId: "Level_03", StageNumber: 2, GameplayType: "UpgradeManufacture", GameplayConfigId: "Dig_06" },
    { LevelId: "Level_03", StageNumber: 3, GameplayType: "Defend", GameplayConfigId: "Defend_03" },
    { LevelId: "Level_03", StageNumber: 4, GameplayType: "Dig", GameplayConfigId: "Dig_07" },
  ];
  writeTable("关卡_关卡运作表_Level_LevelOperationConfig", "Level_LevelOperationConfig", headers, rows);
}

{
  // Fixed "random" DigMapId assignments (Ground_01…05); do not reshuffle on regenerate.
  const digMapIds = [
    "Ground_03",
    "Ground_01",
    "Ground_05",
    "Ground_02",
    "Ground_04",
    "Ground_01",
    "Ground_03",
    "Ground_05",
    "Ground_02",
    "Ground_04",
  ];
  const headers = [
    "GameplayConfigId",
    "DigMapId",
    "LevelDurationSeconds",
    "InitialGraveCount",
    "SpawnRate",
    "GraveSpawnWeights",
  ];
  const rows = Array.from({ length: 10 }, (_, i) => {
    const n = i + 1;
    return {
      GameplayConfigId: `Dig_${String(n).padStart(2, "0")}`,
      DigMapId: digMapIds[i],
      LevelDurationSeconds: 60 + n * 10,
      InitialGraveCount: 3 + (n % 4),
      SpawnRate: `${4 + (n % 3)};${1 + (n % 2)}`,
      GraveSpawnWeights: `Q1;${20 - n}|Q2;${10}|Q${Math.min(n, 10)};${n}`,
    };
  });
  writeTable("挖坟_挖坟配置表_Dig_DigGameplayConfig", "Dig_DigGameplayConfig", headers, rows);
}

{
  const headers = [
    "QualityId",
    "MaxHP",
    "LootDrop",
    "IconStyleHighId",
    "IconStyleMidId",
    "IconStyleLowId",
  ];
  const loot = [
    "Iron_3|Spirit_10",
    "Bone_2|Spirit_5",
    "Wood_4|Iron_1",
    "Stone_2|Spirit_8",
    "Cloth_3|Bone_1",
    "Copper_2|Spirit_12",
    "Silver_1|Spirit_15",
    "Gold_1|Crystal_1",
    "Resin_2|Wood_2",
    "BP_Head_Human_1|Spirit_20",
  ];
  const rows = Array.from({ length: 10 }, (_, i) => {
    const q = `Q${i + 1}`;
    return {
      QualityId: q,
      MaxHP: 50 + i * 25,
      LootDrop: loot[i],
      IconStyleHighId: `GraveIcon_${q}_High`,
      IconStyleMidId: `GraveIcon_${q}_Mid`,
      IconStyleLowId: `GraveIcon_${q}_Low`,
    };
  });
  writeTable("挖坟_坟墓品质定义表_Dig_GraveQualityConfig", "Dig_GraveQualityConfig", headers, rows);
}

{
  const materials = [
    "Iron",
    "Bone",
    "Wood",
    "Stone",
    "Cloth",
    "Copper",
    "Silver",
    "Gold",
    "Crystal",
    "Resin",
  ];
  const headers = [
    "MaterialId",
    "AutoConvert",
    "AppearanceIconId",
    "AssetPath",
    "WarehouseQualityOutlineId",
  ];
  const rows = materials.map((id, i) => ({
    MaterialId: id,
    AutoConvert: i === 0 ? 1 : i * 0.5,
    AppearanceIconId: icon(id),
    AssetPath: asset(id),
    WarehouseQualityOutlineId: outline(`M${i + 1}`),
  }));
  writeTable("挖坟_材料配置表_Dig_MaterialConfig", "Dig_MaterialConfig", headers, rows);
}

{
  const currencies = [
    "Spirit",
    "Coin",
    "Token",
    "Shard",
    "Dust",
    "Mark",
    "Seal",
    "Emblem",
    "Badge",
    "Ticket",
  ];
  const headers = [
    "CurrencyId",
    "AppearanceIconId",
    "AssetPath",
    "WarehouseQualityOutlineId",
  ];
  const rows = currencies.map((id, i) => ({
    CurrencyId: id,
    AppearanceIconId: icon(`Currency_${id}`),
    AssetPath: asset(`Currency_${id}`),
    WarehouseQualityOutlineId: outline(`C${i + 1}`),
  }));
  writeTable("挖坟_货币配置表_Dig_CurrencyConfig", "Dig_CurrencyConfig", headers, rows);
}

// ——— Slice 2: Defend + Combat ———
{
  // Fixed "random" BattleMapId assignments (shared Ground_01…05 pool); do not reshuffle on regenerate.
  const battleMapIds = [
    "Ground_02",
    "Ground_04",
    "Ground_01",
    "Ground_05",
    "Ground_03",
    "Ground_04",
    "Ground_02",
    "Ground_01",
    "Ground_05",
    "Ground_03",
  ];
  const headers = [
    "GameplayConfigId",
    "BattleMapId",
    "WaveConfigId",
    "CombatDurationSeconds",
    "TargetRetargetIntervalSeconds",
  ];
  const rows = Array.from({ length: 10 }, (_, i) => {
    const n = i + 1;
    return {
      GameplayConfigId: `Defend_${String(n).padStart(2, "0")}`,
      BattleMapId: battleMapIds[i],
      WaveConfigId: n <= 5 ? "Wave_01" : "Wave_02",
      CombatDurationSeconds: 90 + n * 15,
      TargetRetargetIntervalSeconds: 1,
    };
  });
  writeTable(
    "防守_防守配置表_Defend_DefendGameplayConfig",
    "Defend_DefendGameplayConfig",
    headers,
    rows
  );
}

{
  const headers = [
    "WaveConfigId",
    "SpawnOrder",
    "SpawnRemainingSeconds",
    "MonsterId",
    "SpawnCount",
    "AppearLocation",
    "SpawnMode",
    "SpawnClockHour",
  ];
  const rows = [
    { WaveConfigId: "Wave_01", SpawnOrder: 1, SpawnRemainingSeconds: 80, MonsterId: "Monster_01", SpawnCount: 3, AppearLocation: "OutsideMap", SpawnMode: "RegionRandom", SpawnClockHour: "" },
    { WaveConfigId: "Wave_01", SpawnOrder: 2, SpawnRemainingSeconds: 80, MonsterId: "Monster_02", SpawnCount: 2, AppearLocation: "OutsideMap", SpawnMode: "ClockDirection", SpawnClockHour: 3 },
    { WaveConfigId: "Wave_01", SpawnOrder: 1, SpawnRemainingSeconds: 60, MonsterId: "Monster_03", SpawnCount: 4, AppearLocation: "InsideMap", SpawnMode: "RegionRandom", SpawnClockHour: "" },
    { WaveConfigId: "Wave_01", SpawnOrder: 1, SpawnRemainingSeconds: 40, MonsterId: "Monster_04", SpawnCount: 2, AppearLocation: "OutsideMap", SpawnMode: "ClockDirection", SpawnClockHour: 6 },
    { WaveConfigId: "Wave_01", SpawnOrder: 1, SpawnRemainingSeconds: 20, MonsterId: "Monster_05", SpawnCount: 5, AppearLocation: "OutsideMap", SpawnMode: "RegionRandom", SpawnClockHour: "" },
    { WaveConfigId: "Wave_02", SpawnOrder: 1, SpawnRemainingSeconds: 100, MonsterId: "Monster_06", SpawnCount: 3, AppearLocation: "OutsideMap", SpawnMode: "RegionRandom", SpawnClockHour: "" },
    { WaveConfigId: "Wave_02", SpawnOrder: 1, SpawnRemainingSeconds: 70, MonsterId: "Monster_07", SpawnCount: 2, AppearLocation: "InsideMap", SpawnMode: "ClockDirection", SpawnClockHour: 9 },
    { WaveConfigId: "Wave_02", SpawnOrder: 2, SpawnRemainingSeconds: 70, MonsterId: "Monster_08", SpawnCount: 2, AppearLocation: "OutsideMap", SpawnMode: "ClockDirection", SpawnClockHour: 12 },
    { WaveConfigId: "Wave_02", SpawnOrder: 1, SpawnRemainingSeconds: 40, MonsterId: "Monster_09", SpawnCount: 4, AppearLocation: "OutsideMap", SpawnMode: "RegionRandom", SpawnClockHour: "" },
    { WaveConfigId: "Wave_02", SpawnOrder: 1, SpawnRemainingSeconds: 10, MonsterId: "Monster_10", SpawnCount: 1, AppearLocation: "OutsideMap", SpawnMode: "ClockDirection", SpawnClockHour: 1 },
  ];
  writeTable("防守_刷怪波次配置表_Defend_WaveSpawnConfig", "Defend_WaveSpawnConfig", headers, rows);
}

{
  const headers = [
    "MonsterId",
    "ModelId",
    "DisplayName",
    "TargetSelect",
    "AttackMode",
    "MaxHP",
    "MoveSpeed",
    "AttackPower",
    "AttackSpeed",
    "AttackRange",
    "MeleeWindupSeconds",
    "RangedProjectileSpeed",
    "RangedTimeoutSeconds",
    "Skills",
    "LootDrop",
  ];
  const names = ["腐尸", "骷髅兵", "墓园犬", "幽魂", "石像鬼", "巫妖徒", "骨龙幼崽", "血仆", "暗影刺客", "巨型僵尸"];
  const target = ["Nearest", "PreferWarrior", "PreferProtagonist"];
  const rows = Array.from({ length: 10 }, (_, i) => {
    const n = i + 1;
    const ranged = i % 3 === 1;
    return {
      MonsterId: `Monster_${String(n).padStart(2, "0")}`,
      ModelId: `MonsterModel_${String(n).padStart(2, "0")}`,
      DisplayName: names[i],
      TargetSelect: target[i % 3],
      AttackMode: ranged ? "Ranged" : "Melee",
      MaxHP: 80 + i * 40,
      MoveSpeed: 2.5 + i * 0.2,
      AttackPower: 8 + i * 2,
      AttackSpeed: Number((0.8 + i * 0.05).toFixed(2)),
      AttackRange: Number((ranged ? 6 + i * 0.3 : 1.5 + i * 0.1).toFixed(2)),
      MeleeWindupSeconds: Number((ranged ? 0 : 0.3 + i * 0.02).toFixed(2)),
      RangedProjectileSpeed: ranged ? 10 + i : 0,
      RangedTimeoutSeconds: ranged ? 2.5 : 0,
      // Demo 不施放；SkillId 含下划线时与 SkillId_Cd 编码易歧义，案例留空
      Skills: "",
      LootDrop: i % 2 === 0 ? `Iron_${1 + (i % 3)}|Spirit_${5 + i}` : `Bone_1|Spirit_${3 + i}`,
    };
  });
  writeTable("防守_怪物配置表_Defend_MonsterConfig", "Defend_MonsterConfig", headers, rows);
}

{
  const headers = ["TierId", "DisplayName", "Description", "LossOfControlChance"];
  const rows = [
    { TierId: 1, DisplayName: "轻度失控", Description: "偶发迟疑，基础失控概率较低", LossOfControlChance: 0.05 },
    { TierId: 2, DisplayName: "中度失控", Description: "情绪不稳，失控风险上升", LossOfControlChance: 0.15 },
    { TierId: 3, DisplayName: "重度失控", Description: "指挥困难，高概率失控", LossOfControlChance: 0.3 },
    { TierId: 4, DisplayName: "完全失控", Description: "几乎无法约束，极易叛变", LossOfControlChance: 0.5 },
  ];
  writeTable("战斗_失控配置表_Combat_LossOfControlConfig", "Combat_LossOfControlConfig", headers, rows);
}

{
  const headers = ["SkillId", "BaseCooldownSeconds", "LossOfControlChanceBonus"];
  const rows = Array.from({ length: 10 }, (_, i) => ({
    SkillId: `Skill_${String(i + 1).padStart(2, "0")}`,
    BaseCooldownSeconds: 5 + i,
    LossOfControlChanceBonus: i % 3 === 0 ? 0.02 : i % 3 === 1 ? 0 : -0.01,
  }));
  writeTable("战斗_技能配置表_Combat_SkillConfig", "Combat_SkillConfig", headers, rows);
}

// ——— Slice 3: Manufacture core ———
{
  const classes = [
    ["Class_Warrior", "战士", "Strength", "Melee"],
    ["Class_Archer", "射手", "Agility", "Ranged"],
    ["Class_Mage", "法师", "Intelligence", "Ranged"],
    ["Class_Knight", "骑士", "Strength", "Melee"],
    ["Class_Rogue", "盗贼", "Agility", "Melee"],
    ["Class_Priest", "牧师", "Intelligence", "Ranged"],
    ["Class_Berserker", "狂战士", "Strength", "Melee"],
    ["Class_Ranger", "游侠", "Agility", "Ranged"],
    ["Class_Warlock", "术士", "Intelligence", "Ranged"],
    ["Class_Paladin", "圣骑士", "Strength", "Melee"],
  ];
  const coeff =
    "NormalAttackPrimaryMult_1.5|AttackSpeedBase_0.5|AttackSpeedAgiDiv_60|SkillCdIntDiv_30|SkillCdFloor_0.1";
  const headers = [
    "ClassId",
    "ClassName",
    "PrimaryStat",
    "CombatConvertCoeffs",
    "AttackRange",
    "MeleeWindupSeconds",
    "RangedProjectileSpeed",
    "RangedTimeoutSeconds",
  ];
  const rows = classes.map(([id, name, prim], i) => {
    const ranged = prim !== "Strength" && (name === "射手" || name === "法师" || name === "牧师" || name === "游侠" || name === "术士");
    return {
      ClassId: id,
      ClassName: name,
      PrimaryStat: prim,
      CombatConvertCoeffs: coeff,
      AttackRange: ranged ? 5 + i * 0.2 : 1.8 + i * 0.05,
      MeleeWindupSeconds: ranged ? 0.2 : 0.35 + i * 0.01,
      RangedProjectileSpeed: ranged ? 12 + i : 0,
      RangedTimeoutSeconds: ranged ? 2 : 0,
    };
  });
  writeTable("制造_职业配置表_Manufacture_ClassConfig", "Manufacture_ClassConfig", headers, rows);
}

{
  const races = [
    ["Race_Human", "Race.Race_Human.Name"],
    ["Race_Elf", "Race.Race_Elf.Name"],
    ["Race_Orc", "Race.Race_Orc.Name"],
    ["Race_Undead", "Race.Race_Undead.Name"],
    ["Race_Dwarf", "Race.Race_Dwarf.Name"],
    ["Race_Goblin", "Race.Race_Goblin.Name"],
    ["Race_Demon", "Race.Race_Demon.Name"],
    ["Race_Angel", "Race.Race_Angel.Name"],
    ["Race_Beast", "Race.Race_Beast.Name"],
    ["Race_Construct", "Race.Race_Construct.Name"],
  ];
  const headers = [
    "RaceId",
    "DisplayNameKey",
    "RaceAdjustCoeff.MaxHP",
    "RaceAdjustCoeff.MoveSpeed",
    "RaceAdjustCoeff.Strength",
    "RaceAdjustCoeff.Agility",
    "RaceAdjustCoeff.Intelligence",
    "LossOfControlChanceBonus",
  ];
  const rows = races.map(([id, key], i) => ({
    RaceId: id,
    DisplayNameKey: key,
    "RaceAdjustCoeff.MaxHP": (i % 5) * 0.05 - 0.1,
    "RaceAdjustCoeff.MoveSpeed": ((i + 1) % 4) * 0.04 - 0.05,
    "RaceAdjustCoeff.Strength": (i % 3) * 0.06 - 0.05,
    "RaceAdjustCoeff.Agility": ((i + 2) % 3) * 0.06 - 0.05,
    "RaceAdjustCoeff.Intelligence": ((i + 1) % 3) * 0.06 - 0.05,
    LossOfControlChanceBonus: i === 6 ? 0.05 : i === 7 ? -0.03 : 0,
  }));
  writeTable("制造_种族配置表_Manufacture_RaceConfig", "Manufacture_RaceConfig", headers, rows);
}

{
  const classIds = [
    "Class_Warrior",
    "Class_Archer",
    "Class_Mage",
    "Class_Knight",
    "Class_Rogue",
    "Class_Priest",
    "Class_Berserker",
    "Class_Ranger",
    "Class_Warlock",
    "Class_Paladin",
  ];
  const modes = ["Melee", "Ranged", "Ranged", "Melee", "Melee", "Ranged", "Melee", "Ranged", "Ranged", "Melee"];
  const headers = [
    "SoulId",
    "ClassId",
    "AttackMode",
    "Skills",
    "AttackPriority",
    "MoveStyle",
    "SpiritCost",
    "ControlPowerCost",
  ];
  const prio = ["Nearest", "PreferWarrior", "PreferProtagonist"];
  const move = ["Normal", "Aggressive", "Cautious"];
  const rows = classIds.map((cid, i) => ({
    SoulId: `Soul_${String(i + 1).padStart(2, "0")}`,
    ClassId: cid,
    AttackMode: modes[i],
    Skills: i % 3 === 0 ? "" : `Skill_${String((i % 10) + 1).padStart(2, "0")};1`,
    AttackPriority: prio[i % 3],
    MoveStyle: move[i % 3],
    SpiritCost: 10 + i * 5,
    ControlPowerCost: 5 + i * 2,
  }));
  writeTable("制造_灵魂配置表_Manufacture_SoulConfig", "Manufacture_SoulConfig", headers, rows);
}

{
  const parts = [
    ["BP_Head_Human", "Head", "Race_Human", 1],
    ["BP_Torso_Human", "Torso", "Race_Human", 1],
    ["BP_Arm_Elf", "Arm", "Race_Elf", 2],
    ["BP_Leg_Elf", "Leg", "Race_Elf", 2],
    ["BP_Head_Orc", "Head", "Race_Orc", 2],
    ["BP_Torso_Orc", "Torso", "Race_Orc", 3],
    ["BP_Arm_Undead", "Arm", "Race_Undead", 2],
    ["BP_Leg_Dwarf", "Leg", "Race_Dwarf", 3],
    ["BP_Head_Demon", "Head", "Race_Demon", 4],
    ["BP_Torso_Angel", "Torso", "Race_Angel", 3],
  ];
  const headers = [
    "BodyPartId",
    "BodyLevel",
    "BodySlot",
    "RaceId",
    "ControlPowerCost",
    "SpiritCost",
    "StatBonus",
    "AutoConvert",
    "Description",
    "ArtAssetId",
  ];
  const rows = parts.map(([id, slot, race, lv], i) => ({
    BodyPartId: id,
    BodyLevel: lv,
    BodySlot: slot,
    RaceId: race,
    ControlPowerCost: 2 + i,
    SpiritCost: 5 + i * 2,
    StatBonus: `MaxHP_${20 + i * 5}|Strength_${1 + (i % 4)}|Agility_${1 + ((i + 1) % 4)}|Intelligence_${1 + ((i + 2) % 4)}|MoveSpeed_${0.1 * (1 + (i % 3))}`,
    AutoConvert: 1 + i * 0.5,
    Description: `案例躯体材料 ${id}`,
    ArtAssetId: `Art_${id}`,
  }));
  writeTable(
    "制造_躯体材料配置表_Manufacture_BodyPartConfig",
    "Manufacture_BodyPartConfig",
    headers,
    rows
  );
}

{
  const headers = [
    "AppearanceId",
    "AppearanceLevel",
    "RaceId",
    "ClassAffinity",
    "Description",
    "IsFallback",
  ];
  const rows = [
    { AppearanceId: "App_01", AppearanceLevel: 1, RaceId: "Race_Human", ClassAffinity: "战士|骑士", Description: "人类战士外观", IsFallback: 1 },
    { AppearanceId: "App_02", AppearanceLevel: 2, RaceId: "Race_Construct", ClassAffinity: "骑士", Description: "构造体保底外形", IsFallback: 1 },
    { AppearanceId: "App_03", AppearanceLevel: 2, RaceId: "Race_Elf", ClassAffinity: "射手|游侠", Description: "精灵远程外观", IsFallback: 1 },
    { AppearanceId: "App_04", AppearanceLevel: 3, RaceId: "Race_Orc", ClassAffinity: "狂战士|战士", Description: "兽人蛮力外观", IsFallback: 1 },
    { AppearanceId: "App_05", AppearanceLevel: 2, RaceId: "Race_Undead", ClassAffinity: "法师|术士", Description: "亡灵施法外观", IsFallback: 1 },
    { AppearanceId: "App_06", AppearanceLevel: 3, RaceId: "Race_Dwarf", ClassAffinity: "骑士|圣骑士", Description: "矮人重装外观", IsFallback: 1 },
    { AppearanceId: "App_07", AppearanceLevel: 1, RaceId: "Race_Goblin", ClassAffinity: "盗贼", Description: "哥布林外观", IsFallback: 1 },
    { AppearanceId: "App_08", AppearanceLevel: 4, RaceId: "Race_Demon", ClassAffinity: "术士|狂战士", Description: "恶魔外观", IsFallback: 1 },
    { AppearanceId: "App_09", AppearanceLevel: 3, RaceId: "Race_Angel", ClassAffinity: "牧师|圣骑士", Description: "天使外观", IsFallback: 1 },
    { AppearanceId: "App_10", AppearanceLevel: 2, RaceId: "Race_Beast", ClassAffinity: "", Description: "野兽保底外形", IsFallback: 1 },
  ];
  writeTable(
    "制造_躯体外观配置表_Manufacture_BodyAppearanceConfig",
    "Manufacture_BodyAppearanceConfig",
    headers,
    rows
  );
}

// ——— Slice 4: Manufacture extra ———
{
  const headers = [
    "Level",
    "RequiredTotalExperience",
    "UnlockedFeatureIds",
    "TechPointsReward",
    "ControlPowerCap",
    "ProtagonistMaxHP",
  ];
  const rows = Array.from({ length: 10 }, (_, i) => {
    const lv = i + 1;
    return {
      Level: lv,
      RequiredTotalExperience: lv === 1 ? 0 : Math.floor(100 * Math.pow(lv - 1, 1.6)),
      UnlockedFeatureIds: lv === 5 ? "Feature_Reserved_A" : lv === 10 ? "Feature_Reserved_A|Feature_Reserved_B" : "",
      TechPointsReward: lv === 1 ? 0 : 1 + Math.floor(lv / 3),
      ControlPowerCap: 20 + lv * 5,
      ProtagonistMaxHP: 3 + Math.floor(lv / 2),
    };
  });
  writeTable(
    "制造_主角升级配置表_Manufacture_ProtagonistLevelConfig",
    "Manufacture_ProtagonistLevelConfig",
    headers,
    rows
  );
}

{
  const gems = [
    ["Gem_Ruby_01", "Ruby"],
    ["Gem_Sapphire_01", "Sapphire"],
    ["Gem_Emerald_01", "Emerald"],
    ["Gem_Topaz_01", "Topaz"],
    ["Gem_Amethyst_01", "Amethyst"],
    ["Gem_Diamond_01", "Diamond"],
    ["Gem_Ruby_02", "Ruby"],
    ["Gem_Sapphire_02", "Sapphire"],
    ["Gem_Emerald_02", "Emerald"],
    ["Gem_Topaz_02", "Topaz"],
  ];
  const headers = [
    "GemId",
    "GemType",
    "GemMult.MaxHP",
    "GemMult.MoveSpeed",
    "GemMult.Strength",
    "GemMult.Agility",
    "GemMult.Intelligence",
    "Skills",
    "SpiritCost",
    "ControlPowerCost",
    "LossOfControlChanceBonus",
  ];
  const rows = gems.map(([id, type], i) => ({
    GemId: id,
    GemType: type,
    "GemMult.MaxHP": i % 2 === 0 ? 0.1 : 0,
    "GemMult.MoveSpeed": i % 3 === 0 ? 0.05 : 0,
    "GemMult.Strength": type === "Ruby" ? 0.15 : 0.02 * (i % 3),
    "GemMult.Agility": type === "Emerald" || type === "Sapphire" ? 0.12 : 0,
    "GemMult.Intelligence": type === "Amethyst" || type === "Diamond" ? 0.12 : 0,
    Skills: i % 4 === 0 ? `Skill_${String((i % 10) + 1).padStart(2, "0")};1` : "",
    SpiritCost: 20 + i * 5,
    ControlPowerCost: 3 + i,
    LossOfControlChanceBonus: i === 5 ? 0.02 : 0,
  }));
  writeTable("制造_宝石配置表_Manufacture_GemConfig", "Manufacture_GemConfig", headers, rows);
}

{
  const equips = [
    ["Equip_Mount_01", "Mount", "疾驰"],
    ["Equip_Mount_02", "Mount", "铁蹄"],
    ["Equip_Mount_03", "Mount", "幽灵"],
    ["Equip_Mount_04", "Mount", "战兽"],
    ["Equip_Mount_05", "Mount", "骨马"],
    ["Equip_Wing_01", "Wing", "羽翼"],
    ["Equip_Wing_02", "Wing", "蝠翼"],
    ["Equip_Wing_03", "Wing", "光翼"],
    ["Equip_Wing_04", "Wing", "暗翼"],
    ["Equip_Wing_05", "Wing", "机械翼"],
  ];
  const headers = [
    "EquipId",
    "EquipSlot",
    "NamePrefix",
    "SpiritCost",
    "ControlPowerCost",
    "EquipStats",
    "Skills",
  ];
  const rows = equips.map(([id, slot, prefix], i) => ({
    EquipId: id,
    EquipSlot: slot,
    NamePrefix: prefix,
    SpiritCost: 15 + i * 3,
    ControlPowerCost: 4 + i,
    EquipStats: `MaxHP_${10 + i * 2}|MoveSpeed_${0.2 + i * 0.05}|Strength_${i % 3}|Agility_${(i + 1) % 3}|Intelligence_${(i + 2) % 3}`,
    Skills: i % 5 === 0 ? `Skill_${String((i % 10) + 1).padStart(2, "0")};1` : "",
  }));
  writeTable(
    "制造_额外装备配置表_Manufacture_ExtraEquipmentConfig",
    "Manufacture_ExtraEquipmentConfig",
    headers,
    rows
  );
}

{
  const headers = ["ComboKey", "Suffix"];
  const rows = [
    { ComboKey: "Ruby", Suffix: "·红辉" },
    { ComboKey: "Sapphire", Suffix: "·蓝晶" },
    { ComboKey: "Emerald", Suffix: "·翠影" },
    { ComboKey: "Topaz", Suffix: "·金芒" },
    { ComboKey: "Amethyst", Suffix: "·紫雾" },
    { ComboKey: "Diamond", Suffix: "·星钻" },
    { ComboKey: "Amethyst|Ruby", Suffix: "·紫红双星" },
    { ComboKey: "Diamond|Sapphire", Suffix: "·蓝钻辉" },
    { ComboKey: "Emerald|Topaz", Suffix: "·翠金" },
    { ComboKey: "Amethyst|Diamond|Ruby", Suffix: "·三曜" },
  ];
  writeTable(
    "制造_宝石后缀命名表_Manufacture_GemSuffixNameConfig",
    "Manufacture_GemSuffixNameConfig",
    headers,
    rows
  );
}

// ——— Slice 5: Tech ———
{
  const headers = [
    "TechId",
    "IconId",
    "DisplayName",
    "EffectDescription",
    "UnlockNextTechIds",
    "InitiallyUnlocked",
    "LearnCost",
    "TechUiFrameType",
  ];
  const rows = [
    { TechId: "Tech_Root", IconId: "Icon_Tech_Root", DisplayName: "掘墓学基础", EffectDescription: "科技树中心根项", UnlockNextTechIds: "Tech_DigDamage|Tech_DigSpeed|Tech_DigRadius", InitiallyUnlocked: "true", LearnCost: 0, TechUiFrameType: "Root" },
    { TechId: "Tech_DigDamage", IconId: "Icon_Tech_DigDamage", DisplayName: "挖掘强化", EffectDescription: "提升挖坟伤害", UnlockNextTechIds: "Tech_Key_01", InitiallyUnlocked: "false", LearnCost: 1, TechUiFrameType: "Normal" },
    { TechId: "Tech_DigSpeed", IconId: "Icon_Tech_DigSpeed", DisplayName: "迅掘", EffectDescription: "缩短单次挖掘时长", UnlockNextTechIds: "Tech_Normal_01", InitiallyUnlocked: "false", LearnCost: 1, TechUiFrameType: "Normal" },
    { TechId: "Tech_DigRadius", IconId: "Icon_Tech_DigRadius", DisplayName: "光标扩展", EffectDescription: "扩大挖坟光标半径", UnlockNextTechIds: "Tech_Normal_02", InitiallyUnlocked: "false", LearnCost: 1, TechUiFrameType: "Normal" },
    { TechId: "Tech_DigDuration", IconId: "Icon_Tech_DigDuration", DisplayName: "延时掘进", EffectDescription: "挖坟阶段时长加成", UnlockNextTechIds: "Tech_Leaf", InitiallyUnlocked: "false", LearnCost: 2, TechUiFrameType: "Normal" },
    { TechId: "Tech_Key_01", IconId: "Icon_Tech_Key", DisplayName: "关键：深层采掘", EffectDescription: "关键节点", UnlockNextTechIds: "Tech_Capstone", InitiallyUnlocked: "false", LearnCost: 3, TechUiFrameType: "Key" },
    { TechId: "Tech_Normal_01", IconId: "Icon_Tech_N1", DisplayName: "稳健掘进", EffectDescription: "普通节点", UnlockNextTechIds: "Tech_DigDuration", InitiallyUnlocked: "false", LearnCost: 2, TechUiFrameType: "Normal" },
    { TechId: "Tech_Normal_02", IconId: "Icon_Tech_N2", DisplayName: "精准采掘", EffectDescription: "普通节点", UnlockNextTechIds: "Tech_DigDuration", InitiallyUnlocked: "false", LearnCost: 2, TechUiFrameType: "Normal" },
    { TechId: "Tech_Capstone", IconId: "Icon_Tech_Cap", DisplayName: "终焉掘墓术", EffectDescription: "顶点科技", UnlockNextTechIds: "", InitiallyUnlocked: "false", LearnCost: 5, TechUiFrameType: "Capstone" },
    { TechId: "Tech_Leaf", IconId: "Icon_Tech_Leaf", DisplayName: "收束训练", EffectDescription: "叶节点", UnlockNextTechIds: "", InitiallyUnlocked: "false", LearnCost: 2, TechUiFrameType: "Normal" },
  ];
  writeTable("科技_科技树配置表_Tech_TechTreeConfig", "Tech_TechTreeConfig", headers, rows);
}

{
  const headers = ["TechId", "AttributeModifiers", "UnlockedFeatureSystemName"];
  const rows = [
    { TechId: "Tech_Root", AttributeModifiers: "DigDamage_1", UnlockedFeatureSystemName: "" },
    { TechId: "Tech_DigDamage", AttributeModifiers: "DigDamage_2", UnlockedFeatureSystemName: "" },
    { TechId: "Tech_DigSpeed", AttributeModifiers: "DigDurationReductionSum_0.1", UnlockedFeatureSystemName: "" },
    { TechId: "Tech_DigRadius", AttributeModifiers: "DigCursorRadius_0.2", UnlockedFeatureSystemName: "" },
    { TechId: "Tech_DigDuration", AttributeModifiers: "DigStageDurationBonus_10", UnlockedFeatureSystemName: "" },
    { TechId: "Tech_Key_01", AttributeModifiers: "DigDamage_3|DigDurationReductionSum_0.05", UnlockedFeatureSystemName: "FeatureDigAdvancedQualities" },
    { TechId: "Tech_Normal_01", AttributeModifiers: "DigDurationReductionSum_0.05", UnlockedFeatureSystemName: "" },
    { TechId: "Tech_Normal_02", AttributeModifiers: "DigCursorRadius_0.1", UnlockedFeatureSystemName: "" },
    { TechId: "Tech_Capstone", AttributeModifiers: "DigDamage_5|DigStageDurationBonus_20", UnlockedFeatureSystemName: "" },
    { TechId: "Tech_Leaf", AttributeModifiers: "DigStageDurationBonus_5", UnlockedFeatureSystemName: "" },
  ];
  writeTable("科技_科技项效果配置表_Tech_TechEffectConfig", "Tech_TechEffectConfig", headers, rows);
}

console.log("\nDone. Output:", ROOT);
