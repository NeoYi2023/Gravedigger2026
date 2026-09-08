# -*- coding: utf-8 -*-
"""Insert AutoMfgColliderInset / AutoMfgSpawnAngleMaxDeg into CombatConstantConfig, then Bake CSV."""
from __future__ import annotations

import io
import math
import re
import sys
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path

from openpyxl import load_workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"
EXCEL_NAME = "通用_常量表_Combat_CombatConstantConfig.xlsx"
CSV_NAME = "Combat_CombatConstantConfig.csv"
EN_COL = re.compile(r"^[A-Za-z_][A-Za-z0-9_.]*$")
MAX_SCAN = 3
_MAX_DECIMAL_PLACES = 10

# Full AutoMfg block (Mode1 Excel is missing the original rain keys).
AUTOMFG_ROWS = [
    (
        "AutoMfgBodyDropIntervalSeconds",
        "自动制造尸骸落下间隔",
        0.3,
        "Seconds between body-rain drop batches",
        "UI-016 StepA：每批次掉落间隔秒",
    ),
    (
        "AutoMfgDropCountMin",
        "自动制造尸骸每批最少",
        3,
        "Min pieces per drop batch",
        "StepA 每间隔随机落下件数下限",
    ),
    (
        "AutoMfgDropCountMax",
        "自动制造尸骸每批最多",
        5,
        "Max pieces per drop batch",
        "StepA 每间隔随机落下件数上限",
    ),
    (
        "AutoMfgGravityScale",
        "自动制造尸骸重力",
        2,
        "Overlay pixel-physics gravity scale (981 px/s2 x this)",
        "Overlay 像素物理重力倍率（981 px/s² × 本值）",
    ),
    (
        "AutoMfgBounciness",
        "自动制造尸骸弹性",
        0.15,
        "Pixel-physics soft bounce on floor / piece hits",
        "像素物理落地/互撞弱弹",
    ),
    (
        "AutoMfgFriction",
        "自动制造尸骸摩擦",
        0.4,
        "Pixel-physics horizontal + angular damping",
        "像素物理水平阻尼 + 角速度阻尼",
    ),
    (
        "AutoMfgColliderInset",
        "自动制造尸骸碰撞内缩",
        0.72,
        "OBB edge vs fitted sprite size (<1 lets slanted pieces nest)",
        "OBB 边长相对 Sprite 适配尺寸的比例（<1 使斜角更紧）",
    ),
    (
        "AutoMfgSpawnAngleMaxDeg",
        "自动制造尸骸落下转角",
        50,
        "Max abs spawn Z yaw (deg); also scales initial angular velocity",
        "落下随机 Z 转角绝对值上限（度）；并作初始角速度尺度",
    ),
    (
        "AutoMfgSpawnJitterXPx",
        "自动制造尸骸水平微扰",
        80,
        "Random spawn X jitter in ref pixels",
        "落点相对屏幕中心 X 微扰",
    ),
    (
        "AutoMfgMagicCircleFlashHz",
        "自动制造法阵闪红频率",
        8,
        "Magic circle red flash Hz during StepB",
        "StepB 法阵红色闪烁 Hz",
    ),
    (
        "AutoMfgReviveSpawnOffsetYPx",
        "自动制造复活出生上偏移",
        400,
        "Revive spawn Y offset from pile base px",
        "问号/士兵出生相对堆底向上像素",
    ),
    (
        "AutoMfgPileFloorYPx",
        "自动制造尸骸堆地板 Y",
        -220,
        "Body pile floor Y in ref pixels",
        "2D 演出层堆地板相对屏幕中心 Y",
    ),
    (
        "AutoMfgSoldierLandYPx",
        "自动制造士兵落地 Y",
        -320,
        "Revived soldier idle land Y px",
        "复活士兵落地相对屏幕中心 Y",
    ),
    (
        "AutoMfgUseLegacySoldierRow",
        "自动制造旧士兵行开关",
        0,
        "1=legacy SoldierScroll conveyor",
        "1=显示中央传送带旧流程；0=尸骸雨默认",
    ),
]
NEW_ROWS = AUTOMFG_ROWS


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


def sanitize_embedded_float_noise(text: str) -> str:
    if not text or "." not in text:
        return text

    def repl(match: re.Match[str]) -> str:
        token = match.group(0)
        frac = token.split(".", 1)[1]
        looks_noisy = len(frac) > 10 or "000000" in frac or "999999" in frac
        if not looks_noisy:
            return token
        try:
            number = float(token)
        except ValueError:
            return token
        return format_numeric_for_csv(number)

    return re.sub(r"-?\d+\.\d+", repl, text)


def cell_to_csv_text(value: object) -> str:
    if value is None:
        return ""
    if isinstance(value, bool):
        return "TRUE" if value else "FALSE"
    if isinstance(value, int) and not isinstance(value, bool):
        return str(value)
    if isinstance(value, float):
        return format_numeric_for_csv(value)
    if isinstance(value, Decimal):
        return format_numeric_for_csv(float(value))
    return sanitize_embedded_float_noise(str(value))


def is_english_header(row: list[str]) -> bool:
    saw = False
    for cell in row:
        if cell is None or str(cell).strip() == "":
            continue
        saw = True
        if not EN_COL.match(str(cell).strip()):
            return False
    return saw


def find_header_index(rows: list[list[str]]) -> int:
    for i, row in enumerate(rows[:MAX_SCAN]):
        if is_english_header(row):
            return i
    raise ValueError("no English header in first 3 rows")


def escape_csv_field(value: str) -> str:
    if any(ch in value for ch in [",", '"', "\n", "\r"]):
        return '"' + value.replace('"', '""') + '"'
    return value


def build_csv(rows: list[list[str]]) -> str:
    lines: list[str] = []
    for r_i, row in enumerate(rows):
        if r_i > 0 and all(not (c or "").strip() for c in row):
            continue
        lines.append(",".join(escape_csv_field(c or "") for c in row))
    return "\n".join(lines) + ("\n" if lines else "")


def insert_rows(xlsx_path: Path) -> None:
    wb = load_workbook(xlsx_path)
    ws = wb.active
    header1 = [ws.cell(1, c).value for c in range(1, 6)]
    header2 = [ws.cell(2, c).value for c in range(1, 6)]
    header3 = [ws.cell(3, c).value for c in range(1, 6)]
    if header3 != ["ConstantKey", "ConstantKeyZh", "Value", "Comment", "CommentZh"]:
        raise SystemExit(f"unexpected header row 3 in {xlsx_path}: {header3}")
    if not header1[0] or not header2[0]:
        raise SystemExit(f"missing doc rows 1-2 in {xlsx_path}")

    existing = {ws.cell(r, 1).value for r in range(1, ws.max_row + 1)}
    missing = [row for row in AUTOMFG_ROWS if row[0] not in existing]
    if not missing:
        print(f"SKIP already has AutoMfg keys: {xlsx_path}")
        wb.close()
        return

    insert_at = None
    last_auto = None
    scale_max = None
    for r in range(1, ws.max_row + 1):
        key = ws.cell(r, 1).value
        if key and str(key).startswith("AutoMfg"):
            last_auto = r
        if key == "WarriorVisualModelScaleMax":
            scale_max = r
    if last_auto is not None:
        insert_at = last_auto + 1
        # Keep collider/angle keys next to friction when only those two are missing.
        if {row[0] for row in missing} <= {"AutoMfgColliderInset", "AutoMfgSpawnAngleMaxDeg"}:
            for r in range(1, ws.max_row + 1):
                if ws.cell(r, 1).value == "AutoMfgFriction":
                    insert_at = r + 1
                    break
    elif scale_max is not None:
        insert_at = scale_max + 1
    else:
        insert_at = ws.max_row + 1

    ws.insert_rows(insert_at, amount=len(missing))
    for i, row in enumerate(missing):
        for c, val in enumerate(row, start=1):
            ws.cell(insert_at + i, c).value = val

    if [ws.cell(1, c).value for c in range(1, 6)] != header1:
        raise SystemExit(f"row 1 mutated in {xlsx_path}")
    if [ws.cell(2, c).value for c in range(1, 6)] != header2:
        raise SystemExit(f"row 2 mutated in {xlsx_path}")

    wb.save(xlsx_path)
    wb.close()
    print(f"INSERTED {len(missing)} OBB rows at Excel row {insert_at}: {xlsx_path}")


def bake(excel_dir: Path, csv_dir: Path) -> None:
    xlsx = excel_dir / EXCEL_NAME
    with open(xlsx, "rb") as raw:
        data = raw.read()
    wb = load_workbook(io.BytesIO(data), read_only=True, data_only=True)
    ws = wb.active
    rows: list[list[str]] = []
    for row in ws.iter_rows(values_only=True):
        cells = [cell_to_csv_text(v) for v in row]
        rows.append(cells)
    wb.close()
    if not rows:
        raise SystemExit(f"empty sheet {xlsx}")
    max_cols = max(len(r) for r in rows)
    for r in rows:
        if len(r) < max_cols:
            r.extend([""] * (max_cols - len(r)))
    header_i = find_header_index(rows)
    if header_i < 2:
        raise SystemExit(f"expected 3-row header, got English at row {header_i} in {xlsx}")
    text = build_csv(rows[header_i:])
    out = csv_dir / CSV_NAME
    csv_dir.mkdir(parents=True, exist_ok=True)
    out.write_text(text, encoding="utf-8", newline="\n")
    keys = [ln.split(",", 1)[0] for ln in text.splitlines()[1:]]
    missing = [k[0] for k in AUTOMFG_ROWS if k[0] not in keys]
    if missing:
        raise SystemExit(f"Bake missing keys {missing} in {out}")
    print(f"BAKED {xlsx.name} → {out}")


def main() -> int:
    targets = [
        (ROOT / "Excel" / EXCEL_NAME, ROOT / "Excel", ROOT / "Csv"),
        (ROOT / "Mode2" / "Excel" / EXCEL_NAME, ROOT / "Mode2" / "Excel", ROOT / "Mode2" / "Csv"),
    ]
    for xlsx, excel_dir, csv_dir in targets:
        if not xlsx.is_file():
            raise SystemExit(f"missing {xlsx}")
        insert_rows(xlsx)
        bake(excel_dir, csv_dir)
    return 0


if __name__ == "__main__":
    sys.exit(main())
