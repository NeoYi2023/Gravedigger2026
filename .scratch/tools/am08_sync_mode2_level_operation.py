# -*- coding: utf-8 -*-
"""AM-08: sync Mode2 Level_LevelOperationConfig Excel from CSV.

Authority: Mode2 CSV already has Dig→AutoManufacture→UM→PushMap (AM-03).
Excel was left stale; runtime reads CSV only — this aligns the human source.

Run: py -3 .scratch/tools/am08_sync_mode2_level_operation.py
"""
from __future__ import annotations

import csv
from pathlib import Path

from openpyxl import Workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"
CSV_PATH = ROOT / "Mode2" / "Csv" / "Level_LevelOperationConfig.csv"
XLSX_PATH = ROOT / "Mode2" / "Excel" / "关卡_关卡运作表_Level_LevelOperationConfig.xlsx"

HEADER = ["LevelId", "StageNumber", "GameplayType", "GameplayConfigId"]
ZH_NAMES = ["关卡ID", "阶段编号", "玩法类型", "玩法配置ID"]
ZH_NOTES = [
    "同 ID 多行 = 该关全部阶段",
    "同关卡内升序执行；建议同关卡内唯一",
    "如 Dig / AutoManufacture / UpgradeManufacture / Defend / PushMap",
    "Dig→DigGameplayConfig；Defend→RecommendedConfigId；PushMap→PushMapGameplayConfig；UM/AutoManufacture→忽略",
]


def main() -> None:
    with CSV_PATH.open(encoding="utf-8-sig", newline="") as f:
        rows = [[r[h] for h in HEADER] for r in csv.DictReader(f)]

    # Expected Mode2 Level_01 pipeline (locked P1)
    l01 = [r for r in rows if r[0] == "Level_01"]
    types = [r[2] for r in l01]
    assert types == ["Dig", "AutoManufacture", "UpgradeManufacture", "PushMap"], types

    wb = Workbook()
    ws = wb.active
    ws.title = "Sheet1"
    ws.append(ZH_NAMES)
    ws.append(ZH_NOTES)
    ws.append(HEADER)
    for row in rows:
        ws.append(row)
    XLSX_PATH.parent.mkdir(parents=True, exist_ok=True)
    wb.save(XLSX_PATH)
    print(f"Wrote {XLSX_PATH} ({len(rows)} data rows)")
    print("Level_01:", " → ".join(types))


if __name__ == "__main__":
    main()
