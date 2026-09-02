# -*- coding: utf-8 -*-
"""LRS-02/06: Level_LevelOperationConfig → OptionId1..5 + Level_SubLevelConfig.

Migrates Mode1+Mode2 linear rows; Mode2 Level_01 Stage2 has dual Dig options.
Excel three-row headers (SPEC_04 §14.7). Writes Excel + CSV + .meta if missing.
"""
from __future__ import annotations

import csv
import io
import math
import re
import uuid
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path

from openpyxl import Workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"
EN_COL = re.compile(r"^[A-Za-z_][A-Za-z0-9_.]*$")
_MAX_DECIMAL_PLACES = 10

OP_XLSX = "关卡_关卡运作表_Level_LevelOperationConfig.xlsx"
OP_CSV = "Level_LevelOperationConfig.csv"
SUB_XLSX = "关卡_子关卡表_Level_SubLevelConfig.xlsx"
SUB_CSV = "Level_SubLevelConfig.csv"

OP_ZH = ["关卡ID", "阶段编号", "玩法选项ID1", "玩法选项ID2", "玩法选项ID3", "玩法选项ID4", "玩法选项ID5"]
OP_NOTE = [
    "同 ID 多行 = 该关全部 Stage",
    "同关升序；一行 = 一个 Stage",
    "FK → SubLevelConfig；空=无",
    "同 Stage 最多 5 套",
    "",
    "",
    "",
]
OP_EN = [
    "LevelId",
    "StageNumber",
    "GameplayOptionId1",
    "GameplayOptionId2",
    "GameplayOptionId3",
    "GameplayOptionId4",
    "GameplayOptionId5",
]

SUB_ZH = [
    "玩法选项ID",
    "玩法类型",
    "玩法配置ID",
    "玩法图标",
    "标题文字",
    "描述文字",
    "奖励",
    "解锁下一阶段选项",
]
SUB_NOTE = [
    "主键",
    "Shop/Dig/AutoManufacture/UpgradeManufacture/Defend/PushMap",
    "Dig/Defend/PushMap 查表；Shop/UM/AM 忽略",
    "Resources/UI/Levels/{IconAssetId}",
    "路线图直显",
    "路线图直显",
    "ItemId;Count|…；可空；通关发放",
    "OptId|OptId；须属 Stage+1；空=关卡胜利",
]
SUB_EN = [
    "GameplayOptionId",
    "GameplayType",
    "GameplayConfigId",
    "IconAssetId",
    "Title",
    "Description",
    "Reward",
    "UnlockNextOptionIds",
]

TITLE_BY_TYPE = {
    "Shop": "商店",
    "Dig": "挖坟",
    "AutoManufacture": "自动制造",
    "UpgradeManufacture": "升级与制造",
    "Defend": "保卫战",
    "PushMap": "推图战",
}

META_XLSX = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

META_CSV = """fileFormatVersion: 2
guid: {guid}
TextScriptImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def format_numeric_for_csv(number: float) -> str:
    if not math.isfinite(number):
        return str(number)
    if abs(number - round(number)) < 1e-9 and abs(number) < 1e15:
        return str(int(round(number)))
    quant = Decimal(1).scaleb(-_MAX_DECIMAL_PLACES)
    rounded_dec = Decimal(repr(number)).quantize(quant, rounding=ROUND_HALF_UP)
    rounded = float(rounded_dec)
    if abs(rounded - round(rounded)) < 1e-12 and abs(rounded) < 1e15:
        return str(int(round(rounded)))
    text = f"{rounded:.{_MAX_DECIMAL_PLACES}f}".rstrip("0").rstrip(".")
    return text if text not in ("", "-") else "0"


def cell_to_csv_text(value: object) -> str:
    if value is None:
        return ""
    if isinstance(value, bool):
        return "TRUE" if value else "FALSE"
    if isinstance(value, int) and not isinstance(value, bool):
        return str(value)
    if isinstance(value, float):
        return format_numeric_for_csv(value)
    return str(value)


def ensure_meta(path: Path, template: str) -> None:
    meta = path.with_suffix(path.suffix + ".meta")
    if meta.exists():
        return
    meta.write_text(template.format(guid=uuid.uuid4().hex), encoding="utf-8")


def write_xlsx(path: Path, zh: list[str], note: list[str], en: list[str], rows: list[list[object]]) -> None:
    wb = Workbook()
    ws = wb.active
    ws.title = "Sheet1"
    ws.append(zh)
    ws.append(note)
    ws.append(en)
    for row in rows:
        ws.append(row)
    path.parent.mkdir(parents=True, exist_ok=True)
    wb.save(path)
    ensure_meta(path, META_XLSX)


def write_csv(path: Path, en: list[str], rows: list[list[object]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(en)
        for row in rows:
            w.writerow([cell_to_csv_text(c) for c in row])
    ensure_meta(path, META_CSV)


def opt_id(level_id: str, stage: int, gtype: str, suffix: str = "") -> str:
    short = level_id.replace("Level_", "L")
    base = f"Opt_{short}_S{stage}_{gtype}"
    return f"{base}_{suffix}" if suffix else base


def build_mode1() -> tuple[list[list[object]], list[list[object]]]:
    # Linear chain from prior CSV
    legacy = [
        ("Level_01", 1, "Dig", "Dig_01"),
        ("Level_01", 2, "UpgradeManufacture", "PushMap_01"),
        ("Level_01", 3, "PushMap", "PushMap_01"),
        ("Level_02", 1, "Dig", "Dig_03"),
        ("Level_02", 2, "Defend", "Defend_02"),
        ("Level_02", 3, "Dig", "Dig_04"),
        ("Level_03", 1, "Dig", "Dig_05"),
        ("Level_03", 2, "UpgradeManufacture", "Dig_06"),
        ("Level_03", 3, "Defend", "Defend_03"),
        ("Level_03", 4, "Dig", "Dig_07"),
    ]
    return build_linear(legacy, branch=None)


def build_mode2() -> tuple[list[list[object]], list[list[object]]]:
    legacy = [
        ("Level_01", 1, "Shop", ""),
        ("Level_01", 2, "Dig", "Dig_01"),
        ("Level_01", 3, "AutoManufacture", ""),
        ("Level_01", 4, "UpgradeManufacture", "PushMap_01"),
        ("Level_01", 5, "PushMap", "PushMap_01"),
        ("Level_02", 1, "Shop", ""),
        ("Level_02", 2, "Dig", "Dig_02"),
        ("Level_02", 3, "AutoManufacture", ""),
        ("Level_02", 4, "UpgradeManufacture", "PushMap_02"),
        ("Level_02", 5, "PushMap", "PushMap_02"),
        ("Level_03", 1, "Shop", ""),
        ("Level_03", 2, "Dig", "Dig_03"),
        ("Level_03", 3, "AutoManufacture", ""),
        ("Level_03", 4, "UpgradeManufacture", "PushMap_03"),
        ("Level_03", 5, "PushMap", "PushMap_03"),
    ]
    # Branch: Level_01 Stage2 Dig A/B both unlock Stage3 AM
    branch = {
        ("Level_01", 2): [
            ("Dig", "Dig_01", "A", "Spirit;30", "挖坟（路线A）", "标准挖坟 Dig_01"),
            ("Dig", "Dig_02", "B", "Spirit;50", "挖坟（路线B）", "进阶挖坟 Dig_02"),
        ]
    }
    return build_linear(legacy, branch=branch)


def build_linear(
    legacy: list[tuple[str, int, str, str]],
    branch: dict[tuple[str, int], list[tuple[str, str, str, str, str, str]]] | None,
) -> tuple[list[list[object]], list[list[object]]]:
    by_level: dict[str, list[tuple[str, int, str, str]]] = {}
    for row in legacy:
        by_level.setdefault(row[0], []).append(row)

    op_rows: list[list[object]] = []
    sub_rows: list[list[object]] = []
    id_by_key: dict[tuple[str, int], list[str]] = {}

    for level_id, stages in by_level.items():
        stages = sorted(stages, key=lambda r: r[1])
        for i, (lid, stage, gtype, cfg) in enumerate(stages):
            key = (lid, stage)
            if branch and key in branch:
                ids = []
                for gtype_b, cfg_b, suf, reward, title, desc in branch[key]:
                    oid = opt_id(lid, stage, gtype_b, suf)
                    ids.append(oid)
                    # unlock filled in second pass
                    sub_rows.append([oid, gtype_b, cfg_b, oid, title, desc, reward, ""])
                id_by_key[key] = ids
            else:
                oid = opt_id(lid, stage, gtype)
                id_by_key[key] = [oid]
                title = TITLE_BY_TYPE.get(gtype, gtype)
                desc = f"{title}（{cfg or '—'}）"
                reward = "Spirit;20" if gtype in ("Dig", "Defend", "PushMap") else ""
                sub_rows.append([oid, gtype, cfg, oid, title, desc, reward, ""])

            ids = id_by_key[key]
            op_row = [lid, stage] + ids + [""] * (5 - len(ids))
            op_rows.append(op_row[:7])

    # Fill UnlockNext: each option unlocks all options of next stage (linear) or branch targets
    sub_by_id = {r[0]: r for r in sub_rows}
    for level_id, stages in by_level.items():
        stages = sorted(stages, key=lambda r: r[1])
        for i, (lid, stage, _, _) in enumerate(stages):
            cur_ids = id_by_key[(lid, stage)]
            if i + 1 >= len(stages):
                unlock = ""
            else:
                next_stage = stages[i + 1][1]
                unlock = "|".join(id_by_key[(lid, next_stage)])
            for oid in cur_ids:
                sub_by_id[oid][7] = unlock

    return op_rows, sub_rows


def emit(mode_root: Path, op_rows: list[list[object]], sub_rows: list[list[object]]) -> None:
    excel = mode_root / "Excel"
    csv_dir = mode_root / "Csv"
    write_xlsx(excel / OP_XLSX, OP_ZH, OP_NOTE, OP_EN, op_rows)
    write_csv(csv_dir / OP_CSV, OP_EN, op_rows)
    write_xlsx(excel / SUB_XLSX, SUB_ZH, SUB_NOTE, SUB_EN, sub_rows)
    write_csv(csv_dir / SUB_CSV, SUB_EN, sub_rows)
    print(f"Wrote {mode_root}: ops={len(op_rows)} subs={len(sub_rows)}")


def main() -> None:
    op1, sub1 = build_mode1()
    emit(ROOT, op1, sub1)
    op2, sub2 = build_mode2()
    emit(ROOT / "Mode2", op2, sub2)


if __name__ == "__main__":
    main()
