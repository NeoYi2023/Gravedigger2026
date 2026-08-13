# -*- coding: utf-8 -*-
"""Insert BodyPartConfig.DisplayName after BodyPartId; fill 种族展示名+部位中文."""
from __future__ import annotations

import csv
from pathlib import Path

from openpyxl import Workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"
XLSX_NAME = "制造_躯体材料配置表_Manufacture_BodyPartConfig.xlsx"
CSV_NAME = "Manufacture_BodyPartConfig.csv"

RACE_ZH = {
    "Race_Human": "人类",
    "Race_Elf": "精灵",
    "Race_Orc": "兽人",
    "Race_Undead": "亡灵",
    "Race_Dwarf": "矮人",
    "Race_Goblin": "地精",
    "Race_Demon": "恶魔",
    "Race_Angel": "天使",
    "Race_Beast": "野兽",
    "Race_Construct": "构装体",
}

SLOT_ZH = {
    "Head": "头部",
    "Torso": "躯干",
    "Arm": "手臂",
    "Leg": "腿部",
}

FIELD_ZH = {
    "BodyPartId": ("躯体ID", "主键；可被 LootDrop / 仓库引用；与 MaterialId 同命名空间不得冲突"),
    "DisplayName": ("道具名称", "仓库 / DigStageSummary 展示名；未启用 i18n 时直接当展示串；空则 UI 回退 BodyPartId"),
    "BodyLevel": ("躯体等级", "≥0；参与制造时外观平均等级"),
    "BodySlot": ("躯体部位", "Head / Torso / Arm / Leg"),
    "RaceId": ("种族", "FK → RaceConfig；参与加权定种族"),
    "ControlPowerCost": ("控制力占用值", "≥0；计入制造 BodyCost"),
    "SpiritCost": ("精魂消耗", "≥0；缺省 0；计入制造总精魂消耗"),
    "StatBonus": ("增加的属性值", "五项基础属性平坦加成；制造时按维 Σ 得 Base(S)"),
    "AutoConvert": ("超上限兑精魂", "每 1 个超出材料兑精魂数；0=超出丢弃且不兑"),
    "Description": ("文字介绍", "展示文案；若启用 i18n 可为本地化 Key"),
    "ArtAssetId": ("外形美术素材ID", "部位单件外观 / 仓库 UI 可用资源 Id"),
    "IsPrimaryHand": ("主要手", "仅 Arm 有意义；1=主要手（Mode2 选料锚点）；缺省 0"),
    "ClassRestrict": ("职业限定", "可产出的 ClassId 多值；Mode2 双手交集定职业"),
    "BodyPrimaryStat": ("躯体主属性", "Strength | Agility | Intelligence；Mode2 选其余部位匹配键"),
}


def display_name(race_id: str, slot: str) -> str:
    race = RACE_ZH.get(race_id.strip(), race_id.replace("Race_", "") if race_id else "")
    part = SLOT_ZH.get(slot.strip(), slot.strip())
    return f"{race}{part}"


def read_csv(path: Path) -> tuple[list[str], list[list[str]]]:
    with path.open(encoding="utf-8", newline="") as f:
        rows = list(csv.reader(f))
    if not rows:
        raise SystemExit(f"empty csv: {path}")
    header = [c.strip() for c in rows[0]]
    while header and header[-1] == "":
        header.pop()
    data = []
    for row in rows[1:]:
        if not any(c.strip() for c in row):
            continue
        padded = list(row) + [""] * max(0, len(header) - len(row))
        data.append(padded[: len(header)])
    return header, data


def write_csv(path: Path, header: list[str], data: list[list[str]]) -> None:
    with path.open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(header)
        w.writerows(data)


def write_xlsx(path: Path, header: list[str], data: list[list[str]]) -> None:
    zh_names = [FIELD_ZH.get(en, (en, ""))[0] for en in header]
    zh_notes = [FIELD_ZH.get(en, ("", ""))[1] for en in header]
    wb = Workbook()
    ws = wb.active
    ws.title = "Sheet1"
    ws.append(zh_names)
    ws.append(zh_notes)
    ws.append(header)
    for row in data:
        padded = list(row) + [""] * max(0, len(header) - len(row))
        ws.append(padded[: len(header)])
    path.parent.mkdir(parents=True, exist_ok=True)
    wb.save(path)


def migrate_root(csv_dir: Path, xlsx_dir: Path, label: str) -> None:
    csv_path = csv_dir / CSV_NAME
    xlsx_path = xlsx_dir / XLSX_NAME
    header, data = read_csv(csv_path)
    col = {name: i for i, name in enumerate(header) if name}
    if "BodyPartId" not in col:
        raise SystemExit(f"{label}: missing BodyPartId in {csv_path}")
    if "DisplayName" in col:
        insert_at = col["DisplayName"]
        new_header = header
        already = True
    else:
        insert_at = col["BodyPartId"] + 1
        new_header = header[:insert_at] + ["DisplayName"] + header[insert_at:]
        already = False

    race_i = col.get("RaceId")
    slot_i = col.get("BodySlot")
    new_data: list[list[str]] = []
    for row in data:
        padded = list(row) + [""] * max(0, len(header) - len(row))
        race = padded[race_i] if race_i is not None and race_i < len(padded) else ""
        slot = padded[slot_i] if slot_i is not None and slot_i < len(padded) else ""
        name = display_name(race, slot)
        if already:
            padded[insert_at] = name
            new_data.append(padded[: len(new_header)])
        else:
            new_data.append(padded[:insert_at] + [name] + padded[insert_at : len(header)])

    write_csv(csv_path, new_header, new_data)
    write_xlsx(xlsx_path, new_header, new_data)
    print(f"{label}: rows={len(new_data)} cols={len(new_header)} → {csv_path.name} + {xlsx_path.name}")
    for r in new_data[:3]:
        print(f"  sample {r[0]} DisplayName={r[insert_at]}")


def main() -> None:
    migrate_root(ROOT / "Csv", ROOT / "Excel", "Mode1")
    migrate_root(ROOT / "Mode2" / "Csv", ROOT / "Mode2" / "Excel", "Mode2")


if __name__ == "__main__":
    main()
