# -*- coding: utf-8 -*-
"""Migrate Dig_GraveQualityConfig: add DropMode, rewrite LootDrop as Id_Weight_Count (M1).

Legacy LootDrop is Id_Count (last underscore). Sample rows: DropMode=1, Weight=10000.
"""
from __future__ import annotations

import csv
from pathlib import Path

from openpyxl import Workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"
XLSX_NAME = "挖坟_坟墓品质定义表_Dig_GraveQualityConfig.xlsx"
CSV_NAME = "Dig_GraveQualityConfig.csv"

FIELD_ZH = {
    "QualityId": ("坟墓品质ID", "主键；被 GraveSpawnWeights 引用"),
    "MaxHP": ("总血量", "生成时初始化坟的 maxHP / 当前 HP"),
    "DropMode": ("掉落模式", "1=每段独立万分比；2=权重抽恰好 1 项；后续可扩展"),
    "LootDrop": ("掉落内容", "挖掘成功（HP=0）时产出；编码 Id_Weight_Count|…；权重用法见 DropMode"),
    "IconStyleHighId": ("高血量图标ID", "剩余 HP%>65%；空=品质默认"),
    "IconStyleMidId": ("中血量图标ID", "剩余 HP% 30%–65%；空=默认"),
    "IconStyleLowId": ("低血量图标ID", "剩余 HP%<30%；空=默认"),
}

NEW_HEADER = [
    "QualityId",
    "MaxHP",
    "DropMode",
    "LootDrop",
    "IconStyleHighId",
    "IconStyleMidId",
    "IconStyleLowId",
]


def migrate_legacy_loot(encoded: str) -> str:
    if not encoded or not encoded.strip():
        return ""
    parts = []
    for seg in encoded.split("|"):
        seg = seg.strip()
        if not seg:
            continue
        underscore = seg.rfind("_")
        if underscore <= 0 or underscore >= len(seg) - 1:
            parts.append(f"{seg}_10000_1")
            continue
        item_id = seg[:underscore]
        count_text = seg[underscore + 1 :]
        if count_text.isdigit() and int(count_text) >= 1:
            parts.append(f"{item_id}_10000_{int(count_text)}")
        else:
            parts.append(f"{seg}_10000_1")
    return "|".join(parts)


def read_csv(path: Path) -> tuple[list[str], list[list[str]]]:
    with path.open(encoding="utf-8", newline="") as f:
        rows = list(csv.reader(f))
    if not rows:
        raise SystemExit(f"empty csv: {path}")
    return rows[0], [r for r in rows[1:] if any(c.strip() for c in r)]


def write_csv(path: Path, header: list[str], data: list[list[str]]) -> None:
    with path.open("w", encoding="utf-8", newline="") as f:
        w = csv.writer(f, lineterminator="\n")
        w.writerow(header)
        w.writerows(data)


def write_xlsx(path: Path, header: list[str], data: list[list[str]]) -> None:
    zh_names = [FIELD_ZH[en][0] for en in header]
    zh_notes = [FIELD_ZH[en][1] for en in header]
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
    col = {name: i for i, name in enumerate(header)}
    if "LootDrop" not in col:
        raise SystemExit(f"{label}: missing LootDrop in {csv_path}")
    loot_i = col["LootDrop"]
    new_data: list[list[str]] = []
    for row in data:
        padded = list(row) + [""] * max(0, len(header) - len(row))
        loot = migrate_legacy_loot(padded[loot_i] if loot_i < len(padded) else "")
        by_name = {name: padded[i] if i < len(padded) else "" for name, i in col.items()}
        new_data.append(
            [
                by_name.get("QualityId", ""),
                by_name.get("MaxHP", ""),
                "1",
                loot,
                by_name.get("IconStyleHighId", ""),
                by_name.get("IconStyleMidId", ""),
                by_name.get("IconStyleLowId", ""),
            ]
        )
    write_csv(csv_path, NEW_HEADER, new_data)
    write_xlsx(xlsx_path, NEW_HEADER, new_data)
    print(f"{label}: rows={len(new_data)} → {csv_path.name} + {xlsx_path.name}")
    for r in new_data[:3]:
        print(f"  sample {r[0]} DropMode={r[2]} LootDrop={r[3]}")


def main() -> None:
    migrate_root(ROOT / "Csv", ROOT / "Excel", "Mode1")
    migrate_root(ROOT / "Mode2" / "Csv", ROOT / "Mode2" / "Excel", "Mode2")


if __name__ == "__main__":
    main()
