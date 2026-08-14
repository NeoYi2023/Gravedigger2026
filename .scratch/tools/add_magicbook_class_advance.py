# -*- coding: utf-8 -*-
"""Add four class-advance MagicBook rows (D-063) to Mode1+Mode2 MagicBookConfig."""
from __future__ import annotations

import csv
from pathlib import Path

from openpyxl import Workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"
XLSX_NAME = "制造_魔法书配置表_Manufacture_MagicBookConfig.xlsx"
CSV_NAME = "Manufacture_MagicBookConfig.csv"

FIELD_ZH = {
    "MagicBookId": ("魔法书ID", "主键"),
    "IsUnique": ("是否唯一", "1=同 Id 不可叠装第二本；0=默认可叠（各占一槽）"),
    "IsProbabilistic": ("概率型", "1=概率触发；ForceClass 的 Chance 真正 roll；0=无概率"),
    "EffectPhase": ("生效环节", "Phase 或 Phase|Phase|…；至少 SoldierManufacture / Combat"),
    "EffectPayload": ("魔法书效果", "已登记 PascalCase Token；空=无效果；禁止中文或内联参数"),
    "EffectParams": ("魔法书效果参数", "Key=Value 或 Key=Value|…；空=无参/缺省"),
    "IconAssetId": ("魔法书Icon", "UI 图标资源 Id"),
    "DisplayName": ("魔法书名称", "展示名；若启用 i18n 可为 Key"),
    "Description": ("魔法书介绍", "展示文案"),
}

ADVANCE_ROWS = [
    {
        "MagicBookId": "MagicBook_WarriorAdvance",
        "IsUnique": "1",
        "IsProbabilistic": "1",
        "EffectPhase": "SoldierManufacture",
        "EffectPayload": "ForceClass",
        "EffectParams": "ClassId=Class_Warrior|RequireClassId=Class_Warrior_0|Chance=0.25",
        "IconAssetId": "",
        "DisplayName": "战士进阶",
        "Description": "装备后，Mode2 制造职业为 Class_Warrior_0 的士兵时，25% 概率变为 Class_Warrior",
    },
    {
        "MagicBookId": "MagicBook_ArcherAdvance",
        "IsUnique": "1",
        "IsProbabilistic": "1",
        "EffectPhase": "SoldierManufacture",
        "EffectPayload": "ForceClass",
        "EffectParams": "ClassId=Class_Archer|RequireClassId=Class_Archer_0|Chance=0.25",
        "IconAssetId": "",
        "DisplayName": "射手进阶",
        "Description": "装备后，Mode2 制造职业为 Class_Archer_0 的士兵时，25% 概率变为 Class_Archer",
    },
    {
        "MagicBookId": "MagicBook_MageAdvance",
        "IsUnique": "1",
        "IsProbabilistic": "1",
        "EffectPhase": "SoldierManufacture",
        "EffectPayload": "ForceClass",
        "EffectParams": "ClassId=Class_Mage|RequireClassId=Class_Mage_0|Chance=0.25",
        "IconAssetId": "",
        "DisplayName": "法师进阶",
        "Description": "装备后，Mode2 制造职业为 Class_Mage_0 的士兵时，25% 概率变为 Class_Mage",
    },
    {
        "MagicBookId": "MagicBook_RogueAdvance",
        "IsUnique": "1",
        "IsProbabilistic": "1",
        "EffectPhase": "SoldierManufacture",
        "EffectPayload": "ForceClass",
        "EffectParams": "ClassId=Class_Rogue|RequireClassId=Class_Rogue_0|Chance=0.25",
        "IconAssetId": "",
        "DisplayName": "盗贼进阶",
        "Description": "装备后，Mode2 制造职业为 Class_Rogue_0 的士兵时，25% 概率变为 Class_Rogue",
    },
]


def read_csv(path: Path) -> tuple[list[str], list[list[str]]]:
    with path.open(encoding="utf-8-sig", newline="") as f:
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


def col_index(header: list[str], name: str) -> int:
    try:
        return header.index(name)
    except ValueError as exc:
        raise SystemExit(f"missing column {name}") from exc


def apply(header: list[str], data: list[list[str]]) -> list[list[str]]:
    id_i = col_index(header, "MagicBookId")
    by_id = {row["MagicBookId"]: row for row in ADVANCE_ROWS}
    out = []
    seen = set()
    for row in data:
        padded = list(row) + [""] * max(0, len(header) - len(row))
        padded = padded[: len(header)]
        book_id = padded[id_i]
        if book_id in by_id:
            seen.add(book_id)
            for key, value in by_id[book_id].items():
                padded[col_index(header, key)] = value
        out.append(padded)
    for row in ADVANCE_ROWS:
        if row["MagicBookId"] in seen:
            continue
        new_row = [""] * len(header)
        for key, value in row.items():
            new_row[col_index(header, key)] = value
        out.append(new_row)
    return out


def migrate_root(csv_dir: Path, xlsx_dir: Path, label: str) -> None:
    csv_path = csv_dir / CSV_NAME
    xlsx_path = xlsx_dir / XLSX_NAME
    header, data = read_csv(csv_path)
    new_data = apply(header, data)
    write_csv(csv_path, header, new_data)
    write_xlsx(xlsx_path, header, new_data)
    print(f"{label}: rows={len(new_data)} → {csv_path}")
    for r in new_data:
        print(f"  {r}")


def main() -> None:
    migrate_root(ROOT / "Csv", ROOT / "Excel", "Mode1")
    migrate_root(ROOT / "Mode2" / "Csv", ROOT / "Mode2" / "Excel", "Mode2")


if __name__ == "__main__":
    main()
