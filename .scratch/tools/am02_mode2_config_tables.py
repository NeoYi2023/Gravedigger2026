# -*- coding: utf-8 -*-
"""AM-02: Mode2 BodyPart/Class expand + empty MagicBook (Approach A).

Writes CSV then 3-row-header Excel for:
- Mode2 Manufacture_BodyPartConfig / ClassConfig (new columns + sample PrimaryHand)
- Mode1 + Mode2 Manufacture_MagicBookConfig (header only)

Run from repo root: py -3 .scratch/tools/am02_mode2_config_tables.py
"""
from __future__ import annotations

import csv
import uuid
from pathlib import Path

from openpyxl import Workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"
MODE1_CSV = ROOT / "Csv"
MODE1_XLSX = ROOT / "Excel"
MODE2_CSV = ROOT / "Mode2" / "Csv"
MODE2_XLSX = ROOT / "Mode2" / "Excel"

BODY_ZH = {
    "BodyPartId": ("躯体ID", "主键；与 MaterialId 同命名空间不得冲突"),
    "BodyLevel": ("躯体等级", "≥0；参与外观平均等级"),
    "BodySlot": ("躯体部位", "Head|Torso|Arm|Leg"),
    "RaceId": ("种族", "FK → RaceConfig"),
    "ControlPowerCost": ("控制力占用值", "≥0"),
    "SpiritCost": ("精魂消耗", "≥0；Mode2 AutoManufacture 不计"),
    "StatBonus": ("增加的属性值", "属性项_数值|…；计入 Base(S)"),
    "AutoConvert": ("超上限兑精魂", "每 1 超出兑精魂数"),
    "Description": ("文字介绍", "展示文案"),
    "ArtAssetId": ("外形美术素材ID", "仓库 / DigReward 可用"),
    "IsPrimaryHand": ("主要手", "仅 Arm；1=Mode2 选料锚点；缺省 0"),
    "ClassRestrict": ("职业限定", "ClassId|…；Mode2 双手定职业"),
    "BodyPrimaryStat": ("躯体主属性", "Strength|Agility|Intelligence；Mode2 选料匹配"),
}

CLASS_ZH = {
    "ClassId": ("职业ID", "主键；被 SoulConfig.ClassId 引用"),
    "ClassName": ("职业名", "参与 WarriorName；外观 ClassAffinity 匹配键"),
    "PrimaryStat": ("主属性", "Strength|Agility|Intelligence"),
    "CombatConvertCoeffs": ("战斗换算系数", "键_数值|…；缺键回退默认"),
    "AttackRange": ("攻击距离", "进入攻击态距离"),
    "MeleeWindupSeconds": ("近战前摇", "≥0 秒；Melee 时用"),
    "RangedProjectileSpeed": ("远程弹速", "≥0；Ranged 时用"),
    "RangedTimeoutSeconds": ("远程超时", "≥0 秒"),
    "ChaseMoveSpeedMult": ("追击移速倍率", "≥0；AttackSlot 时 × MoveSpeed；缺省 1"),
    "AttackMode": ("攻击模式", "Melee|Ranged；Mode2 无灵魂时取本列"),
    "PlacementOrder": ("放置排序", "≥1；Mode2 自动上阵升序；缺省后置"),
    "DefaultAppearanceId": ("职业默认外观", "FK → BodyAppearanceConfig；空则种族 IsFallback"),
}

MAGIC_ZH = {
    "MagicBookId": ("魔法书ID", "主键"),
    "IsUnique": ("是否唯一", "1=同 Id 不可叠装第二本；0=默认可叠"),
    "EffectPhase": ("生效环节", "SoldierManufacture|Combat|…"),
    "EffectPayload": ("魔法书效果", "本轮占位串；空=无效果"),
    "IconAssetId": ("魔法书Icon", "UI 图标资源 Id"),
    "DisplayName": ("魔法书名称", "展示名或本地化 Key"),
    "Description": ("魔法书介绍", "展示文案"),
}

MAGIC_EN = list(MAGIC_ZH.keys())
MAGIC_XLSX_NAME = "制造_魔法书配置表_Manufacture_MagicBookConfig.xlsx"


def write_csv(path: Path, header: list[str], rows: list[list[str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(header)
        w.writerows(rows)


def write_xlsx(path: Path, field_zh: dict[str, tuple[str, str]], header: list[str], rows: list[list[str]]) -> None:
    zh_names = [field_zh.get(en, (en, f"（待补）{en}"))[0] for en in header]
    zh_notes = [field_zh.get(en, (en, f"（待补）{en}"))[1] for en in header]
    wb = Workbook()
    ws = wb.active
    ws.title = "Sheet1"
    ws.append(zh_names)
    ws.append(zh_notes)
    ws.append(header)
    for row in rows:
        padded = list(row) + [""] * max(0, len(header) - len(row))
        ws.append(padded[: len(header)])
    path.parent.mkdir(parents=True, exist_ok=True)
    wb.save(path)


def write_meta_csv(path: Path) -> None:
    if path.is_file():
        return
    guid = uuid.uuid4().hex
    path.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "TextScriptImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )


def write_meta_xlsx(path: Path) -> None:
    if path.is_file():
        return
    guid = uuid.uuid4().hex
    path.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "DefaultImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )


def body_rows() -> tuple[list[str], list[list[str]]]:
    header = list(BODY_ZH.keys())
    # Existing rows + AM-02 PrimaryHand samples + secondary arms for later AM.
    rows = [
        ["BP_Head_Human", "1", "Head", "Race_Human", "1", "5",
         "MaxHP_20|Strength_8|Agility_16|Intelligence_24|MoveSpeed_0.1", "1",
         "案例躯体材料 BP_Head_Human", "Art_BP_Head_Human", "0", "", "Intelligence"],
        ["BP_Torso_Human", "1", "Torso", "Race_Human", "1", "7",
         "MaxHP_25|Strength_16|Agility_24|Intelligence_4|MoveSpeed_0.2", "1.5",
         "案例躯体材料 BP_Torso_Human", "Art_BP_Torso_Human", "0", "", "Strength"],
        # PrimaryHand samples (IsPrimaryHand=1 + ClassRestrict non-empty)
        ["BP_Arm_Elf", "2", "Arm", "Race_Elf", "1", "9",
         "MaxHP_30|Strength_24|Agility_4|Intelligence_8|MoveSpeed_0.3", "2",
         "案例主要手 BP_Arm_Elf", "Art_BP_Arm_Elf", "1", "Class_Archer|Class_Ranger", "Agility"],
        ["BP_Arm_Undead", "2", "Arm", "Race_Undead", "1", "17",
         "MaxHP_50|Strength_24|Agility_4|Intelligence_8|MoveSpeed_0.1", "4",
         "案例主要手 BP_Arm_Undead", "Art_BP_Arm_Undead", "1", "Class_Mage|Class_Warlock", "Intelligence"],
        # SecondaryHand samples (IsPrimaryHand=0)
        ["BP_Arm_Human", "1", "Arm", "Race_Human", "1", "8",
         "MaxHP_28|Strength_12|Agility_12|Intelligence_8|MoveSpeed_0.2", "1.5",
         "案例次要手 BP_Arm_Human", "Art_BP_Arm_Human", "0", "Class_Archer|Class_Ranger|Class_Warrior", "Strength"],
        ["BP_Arm_Orc", "3", "Arm", "Race_Orc", "1", "16",
         "MaxHP_48|Strength_20|Agility_8|Intelligence_8|MoveSpeed_0.2", "3.5",
         "案例次要手 BP_Arm_Orc", "Art_BP_Arm_Orc", "0", "Class_Mage|Class_Warlock|Class_Berserker", "Strength"],
        ["BP_Leg_Elf", "2", "Leg", "Race_Elf", "1", "11",
         "MaxHP_35|Strength_4|Agility_8|Intelligence_16|MoveSpeed_0.1", "2.5",
         "案例躯体材料 BP_Leg_Elf", "Art_BP_Leg_Elf", "0", "", "Agility"],
        ["BP_Head_Orc", "2", "Head", "Race_Orc", "1", "13",
         "MaxHP_40|Strength_8|Agility_16|Intelligence_24|MoveSpeed_0.2", "3",
         "案例躯体材料 BP_Head_Orc", "Art_BP_Head_Orc", "0", "", "Strength"],
        ["BP_Torso_Orc", "3", "Torso", "Race_Orc", "1", "15",
         "MaxHP_45|Strength_16|Agility_24|Intelligence_4|MoveSpeed_0.3", "3.5",
         "案例躯体材料 BP_Torso_Orc", "Art_BP_Torso_Orc", "0", "", "Strength"],
        ["BP_Leg_Dwarf", "3", "Leg", "Race_Dwarf", "1", "19",
         "MaxHP_55|Strength_4|Agility_8|Intelligence_16|MoveSpeed_0.2", "4.5",
         "案例躯体材料 BP_Leg_Dwarf", "Art_BP_Leg_Dwarf", "0", "", "Strength"],
        ["BP_Head_Demon", "4", "Head", "Race_Demon", "1", "21",
         "MaxHP_60|Strength_8|Agility_16|Intelligence_24|MoveSpeed_0.3", "5",
         "案例躯体材料 BP_Head_Demon", "Art_BP_Head_Demon", "0", "", "Intelligence"],
        ["BP_Torso_Angel", "3", "Torso", "Race_Angel", "1", "23",
         "MaxHP_65|Strength_16|Agility_24|Intelligence_4|MoveSpeed_0.1", "5.5",
         "案例躯体材料 BP_Torso_Angel", "Art_BP_Torso_Angel", "0", "", "Agility"],
    ]
    return header, rows


def class_rows() -> tuple[list[str], list[list[str]]]:
    header = list(CLASS_ZH.keys())
    coeffs = "NormalAttackPrimaryMult_15|AttackSpeedBase_0.5|AttackSpeedAgiDiv_60|SkillCdIntDiv_30|SkillCdFloor_0.1"
    # ClassId, ClassName, PrimaryStat, coeffs, AttackRange, MeleeWindup, RangedSpeed, RangedTimeout,
    # ChaseMoveSpeedMult, AttackMode, PlacementOrder, DefaultAppearanceId
    rows = [
        ["Class_Warrior", "战士", "Strength", coeffs, "0.3", "0.5", "0", "0", "2", "Melee", "1", "App_01"],
        ["Class_Archer", "射手", "Agility", coeffs, "1", "0.5", "13", "1", "0.5", "Ranged", "6", "App_03"],
        ["Class_Mage", "法师", "Intelligence", coeffs, "0.8", "0.5", "14", "1", "0.5", "Ranged", "8", "App_05"],
        ["Class_Knight", "骑士", "Strength", coeffs, "0.3", "0.5", "0", "0", "2", "Melee", "2", "App_01"],
        ["Class_Rogue", "盗贼", "Agility", coeffs, "0.3", "0.5", "0", "0", "3", "Melee", "5", "App_07"],
        ["Class_Priest", "牧师", "Intelligence", coeffs, "1.2", "0.5", "17", "2", "0.5", "Ranged", "10", "App_09"],
        ["Class_Berserker", "狂战士", "Strength", coeffs, "0.3", "0.5", "0", "0", "2", "Melee", "4", "App_04"],
        ["Class_Ranger", "游侠", "Agility", coeffs, "1.1", "0.5", "19", "1", "1", "Ranged", "7", "App_03"],
        ["Class_Warlock", "术士", "Intelligence", coeffs, "0.9", "0.5", "20", "1", "1", "Ranged", "9", "App_08"],
        ["Class_Paladin", "圣骑士", "Strength", coeffs, "0.5", "0.5", "0", "0", "1.5", "Melee", "3", "App_06"],
        ["Class_Servants", "仆从", "Strength", coeffs, "0.3", "0.5", "0", "0", "2.5", "Melee", "11", ""],
    ]
    return header, rows


def write_magic(csv_dir: Path, xlsx_dir: Path) -> None:
    csv_path = csv_dir / "Manufacture_MagicBookConfig.csv"
    xlsx_path = xlsx_dir / MAGIC_XLSX_NAME
    write_csv(csv_path, MAGIC_EN, [])
    write_xlsx(xlsx_path, MAGIC_ZH, MAGIC_EN, [])
    write_meta_csv(Path(str(csv_path) + ".meta"))
    write_meta_xlsx(Path(str(xlsx_path) + ".meta"))
    print(f"OK empty MagicBook → {csv_path.relative_to(ROOT.parent)} + Excel")


def main() -> None:
    body_h, body_d = body_rows()
    class_h, class_d = class_rows()

    body_csv = MODE2_CSV / "Manufacture_BodyPartConfig.csv"
    class_csv = MODE2_CSV / "Manufacture_ClassConfig.csv"
    body_xlsx = MODE2_XLSX / "制造_躯体材料配置表_Manufacture_BodyPartConfig.xlsx"
    class_xlsx = MODE2_XLSX / "制造_职业配置表_Manufacture_ClassConfig.xlsx"

    write_csv(body_csv, body_h, body_d)
    write_xlsx(body_xlsx, BODY_ZH, body_h, body_d)
    print(f"OK Mode2 BodyPart {len(body_d)} rows → CSV+Excel")

    write_csv(class_csv, class_h, class_d)
    write_xlsx(class_xlsx, CLASS_ZH, class_h, class_d)
    print(f"OK Mode2 Class {len(class_d)} rows → CSV+Excel")

    write_magic(MODE2_CSV, MODE2_XLSX)
    write_magic(MODE1_CSV, MODE1_XLSX)
    print("AM-02 table write done (Approach A).")


if __name__ == "__main__":
    main()
