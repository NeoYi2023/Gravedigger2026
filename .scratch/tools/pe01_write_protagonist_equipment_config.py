# -*- coding: utf-8 -*-
"""PE-01: create Protagonist_ProtagonistEquipmentConfig Excel+CSV (Mode1+Mode2)."""
from __future__ import annotations

import csv
import uuid
from pathlib import Path

from openpyxl import Workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"
XLSX_NAME = "主角_装备配置表_Protagonist_ProtagonistEquipmentConfig.xlsx"
CSV_NAME = "Protagonist_ProtagonistEquipmentConfig.csv"

HEADER = [
    "EquipId",
    "EquipLevel",
    "DisplayName",
    "IconAssetId",
    "ExpToNextLevel",
    "ConvertExpValue",
    "EffectDomain",
    "EquipEffect",
    "Description",
]

ZH_NAMES = [
    "装备ID",
    "装备等级",
    "装备名称",
    "装备图标",
    "升下一级经验",
    "转化经验值",
    "装备生效功能",
    "装备效果",
    "装备描述",
]

ZH_NOTES = [
    "复合主键之一",
    "复合主键之一；从1起",
    "展示名",
    "UI图标资源Id",
    "空或≤0=满级行",
    "再获同Id转入经验",
    "Dig|SoldierManufacture|Combat",
    "Dig: Attr_Value|…",
    "展示文案",
]

# Demo 铁铲 L1–5 (SPEC_03 §3.16 / D-059): +10% of base 0.6 per level
ROWS = [
    [
        "Equip_IronShovel",
        "1",
        "铁铲",
        "Icon_IronShovel",
        "1",
        "1",
        "Dig",
        "DigCursorRadius_0.06",
        "每级增加挖坟半径10%（1级）",
    ],
    [
        "Equip_IronShovel",
        "2",
        "铁铲",
        "Icon_IronShovel",
        "1",
        "1",
        "Dig",
        "DigCursorRadius_0.12",
        "每级增加挖坟半径10%（2级）",
    ],
    [
        "Equip_IronShovel",
        "3",
        "铁铲",
        "Icon_IronShovel",
        "1",
        "1",
        "Dig",
        "DigCursorRadius_0.18",
        "每级增加挖坟半径10%（3级）",
    ],
    [
        "Equip_IronShovel",
        "4",
        "铁铲",
        "Icon_IronShovel",
        "1",
        "1",
        "Dig",
        "DigCursorRadius_0.24",
        "每级增加挖坟半径10%（4级）",
    ],
    [
        "Equip_IronShovel",
        "5",
        "铁铲",
        "Icon_IronShovel",
        "",
        "1",
        "Dig",
        "DigCursorRadius_0.30",
        "每级增加挖坟半径10%（满级）",
    ],
]


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


def write_csv(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(HEADER)
        w.writerows(ROWS)
    write_meta_csv(Path(str(path) + ".meta"))


def write_xlsx(path: Path) -> None:
    wb = Workbook()
    ws = wb.active
    ws.title = "Sheet1"
    ws.append(ZH_NAMES)
    ws.append(ZH_NOTES)
    ws.append(HEADER)
    for row in ROWS:
        ws.append(row)
    path.parent.mkdir(parents=True, exist_ok=True)
    wb.save(path)
    write_meta_xlsx(Path(str(path) + ".meta"))


def main() -> None:
    targets = [
        (ROOT / "Excel" / XLSX_NAME, ROOT / "Csv" / CSV_NAME),
        (ROOT / "Mode2" / "Excel" / XLSX_NAME, ROOT / "Mode2" / "Csv" / CSV_NAME),
    ]
    for xlsx, csv_path in targets:
        write_xlsx(xlsx)
        write_csv(csv_path)
        print(f"OK {xlsx.relative_to(ROOT.parent)} + {csv_path.relative_to(ROOT.parent)}")


if __name__ == "__main__":
    main()
