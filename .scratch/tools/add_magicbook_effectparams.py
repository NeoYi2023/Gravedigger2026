# -*- coding: utf-8 -*-
"""Insert MagicBookConfig.EffectParams after EffectPayload (SPEC_04 §9.24 v0.80.8+)."""
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
    "EffectPhase": ("生效环节", "Phase 或 Phase|Phase|…；至少 SoldierManufacture / Combat"),
    "EffectPayload": ("魔法书效果", "已登记 PascalCase Token；空=无效果；禁止中文或内联参数"),
    "EffectParams": ("魔法书效果参数", "Key=Value 或 Key=Value|…；空=无参/缺省"),
    "IconAssetId": ("魔法书Icon", "UI 图标资源 Id"),
    "DisplayName": ("魔法书名称", "展示名；若启用 i18n 可为 Key"),
    "Description": ("魔法书介绍", "展示文案"),
}


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
    with path.open("w", encoding="utf-8", newline="") as f:  # no BOM
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
    if "EffectPayload" not in col:
        raise SystemExit(f"{label}: missing EffectPayload in {csv_path}")
    if "EffectParams" in col:
        new_header = header
        new_data = data
    else:
        insert_at = col["EffectPayload"] + 1
        new_header = header[:insert_at] + ["EffectParams"] + header[insert_at:]
        new_data = []
        for row in data:
            padded = list(row) + [""] * max(0, len(header) - len(row))
            new_data.append(padded[:insert_at] + [""] + padded[insert_at : len(header)])

    write_csv(csv_path, new_header, new_data)
    write_xlsx(xlsx_path, new_header, new_data)
    print(f"{label}: rows={len(new_data)} cols={len(new_header)} → {csv_path.name} + {xlsx_path.name}")
    print(f"  header={new_header}")
    for r in new_data[:3]:
        print(f"  sample {r}")


def main() -> None:
    migrate_root(ROOT / "Csv", ROOT / "Excel", "Mode1")
    migrate_root(ROOT / "Mode2" / "Csv", ROOT / "Mode2" / "Excel", "Mode2")


if __name__ == "__main__":
    main()
