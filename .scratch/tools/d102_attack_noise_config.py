# -*- coding: utf-8 -*-
"""D-102 slice 01: attack-noise / berserk columns (SPEC_04 §14.7). Sample values all 0."""
from __future__ import annotations

import sys
from pathlib import Path

from openpyxl import load_workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"

CLASS_XLSX = "制造_职业配置表_Manufacture_ClassConfig.xlsx"
MONSTER_XLSX = "防守_怪物配置表_Defend_MonsterConfig.xlsx"
SKILL_EFFECT_XLSX = "战斗_技能效果配置表_Combat_SkillEffectConfig.xlsx"

CLASS_PK = "ClassId"
MONSTER_PK = "MonsterId"
SKILL_PK = "SkillEffectId"

CLASS_COLS = [
    ("AttackNoiseValue", "攻击噪声值", "0=无噪声；>0=每次普通攻击HitConfirm增加的噪声值（D-102）"),
    ("NoiseRadius", "噪声半径", "战斗地图半径；圆心=攻击目标点（D-102）"),
]
MONSTER_COLS = [
    ("BerserkNoiseThreshold", "狂暴要求值", "0=不会狂暴；>0=噪声累积达标后改AggroMode一次（D-102）"),
    ("BerserkAggroMode", "狂暴后仇恨模式", "空=达标也不切；四值同AggroMode；PassiveChase/StationaryPassive另挑衅（D-102）"),
]
SKILL_COLS = [
    ("AttackNoiseValue", "攻击噪声值", "0=无噪声；>0=该效果TriggerHook匹配时增加的噪声值（D-102）"),
    ("NoiseRadius", "噪声半径", "战斗地图半径；圆心=施法目标点（D-102）"),
]

sys.path.insert(0, str(Path(__file__).resolve().parent))
from bake_config_tables_py import (  # noqa: E402
    build_csv,
    excel_to_csv_base,
    find_header_index,
    read_sheet_rows,
)


def find_en_header_row(ws, pk: str) -> int:
    for i, row in enumerate(ws.iter_rows(min_row=1, max_row=3, values_only=True), 1):
        cells = [str(c).strip() if c is not None else "" for c in row]
        if pk in cells:
            return i
    raise RuntimeError(f"no EN header with {pk} in first 3 rows")


def header_list(ws, header_row: int) -> list[str]:
    row = next(ws.iter_rows(min_row=header_row, max_row=header_row, values_only=True))
    names = [str(c).strip() if c is not None else "" for c in row]
    while names and names[-1] == "":
        names.pop()
    return names


def append_column(ws, header_row: int, en: str, zh: str, note: str) -> None:
    names = header_list(ws, header_row)
    if en in names:
        return
    insert_at = len(names) + 1
    ws.insert_cols(insert_at)
    if header_row >= 3:
        ws.cell(1, insert_at, zh)
        ws.cell(2, insert_at, note)
    ws.cell(header_row, insert_at, en)


def col_index(names: list[str], en: str) -> int:
    return names.index(en) + 1


def fill_zero_or_empty(ws, header_row: int, pk: str, columns: list[tuple[str, str, str]], empty_text: bool) -> None:
    for en, zh, note in columns:
        append_column(ws, header_row, en, zh, note)
    names = header_list(ws, header_row)
    id_col = col_index(names, pk)
    value_cols = [col_index(names, en) for en, _, _ in columns]
    for r in range(header_row + 1, ws.max_row + 1):
        pk_val = ws.cell(r, id_col).value
        if pk_val is None or not str(pk_val).strip():
            continue
        for col in value_cols:
            current = ws.cell(r, col).value
            if current is not None and str(current).strip() != "":
                continue
            ws.cell(r, col, "" if empty_text else 0)


def patch_workbook(path: Path, pk: str, columns: list[tuple[str, str, str]], empty_text: bool) -> None:
    if not path.is_file():
        print(f"SKIP missing {path}")
        return
    wb = load_workbook(path)
    ws = wb.active
    header_row = find_en_header_row(ws, pk)
    fill_zero_or_empty(ws, header_row, pk, columns, empty_text)
    wb.save(path)
    print(f"OK {path}")


def bake_one(excel_dir: Path, csv_dir: Path, xlsx_name: str) -> None:
    xlsx = excel_dir / xlsx_name
    if not xlsx.is_file():
        print(f"SKIP bake missing {xlsx}")
        return
    rows = read_sheet_rows(xlsx)
    header_i = find_header_index(rows)
    text = build_csv(rows[header_i:])
    out = csv_dir / f"{excel_to_csv_base(xlsx.stem)}.csv"
    out.write_text(text, encoding="utf-8", newline="\n")
    header = text.splitlines()[0] if text else ""
    print(f"BAKE {xlsx.name} → {out.name} header={header}")


def main() -> None:
    pairs = [
        (ROOT / "Excel", ROOT / "Csv"),
        (ROOT / "Mode2" / "Excel", ROOT / "Mode2" / "Csv"),
    ]
    for excel_dir, csv_dir in pairs:
        patch_workbook(excel_dir / CLASS_XLSX, CLASS_PK, CLASS_COLS, empty_text=False)
        patch_workbook(excel_dir / MONSTER_XLSX, MONSTER_PK, MONSTER_COLS, empty_text=True)
        patch_workbook(excel_dir / SKILL_EFFECT_XLSX, SKILL_PK, SKILL_COLS, empty_text=False)
        bake_one(excel_dir, csv_dir, CLASS_XLSX)
        bake_one(excel_dir, csv_dir, MONSTER_XLSX)
        bake_one(excel_dir, csv_dir, SKILL_EFFECT_XLSX)


if __name__ == "__main__":
    main()
