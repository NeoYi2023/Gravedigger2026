# -*- coding: utf-8 -*-
"""Retune OffScreenSpawnHint: red blink 2x in 0.4s + hold 2s + scale 1.3."""
from __future__ import annotations

import io
import math
import re
import sys
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path

from openpyxl import load_workbook

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Gravedigger2026" / "Assets" / "ConfigTables"
EXCEL_NAME = "通用_常量表_Combat_CombatConstantConfig.xlsx"
CSV_NAME = "Combat_CombatConstantConfig.csv"

OLD_KEYS = {
    "OffScreenSpawnHintDurationSeconds",
    "OffScreenSpawnHintBlinkPeriodSeconds",
}

NEW_ROWS = [
    (
        "OffScreenSpawnHintIntroBlinkSeconds",
        "离屏来袭开场闪红秒",
        0.4,
        "UI-034: intro red-blink window seconds",
        "UI-034：开场闪红总时长秒",
    ),
    (
        "OffScreenSpawnHintIntroBlinkCount",
        "离屏来袭开场闪红次数",
        2,
        "UI-034: red blink count inside intro window",
        "UI-034：开场窗口内闪红次数",
    ),
    (
        "OffScreenSpawnHintHoldSeconds",
        "离屏来袭常亮秒",
        2,
        "UI-034: solid white hold seconds after intro blink",
        "UI-034：闪红后常亮秒数",
    ),
    (
        "OffScreenSpawnHintDisplayScale",
        "离屏来袭显示缩放",
        1.3,
        "UI-034: icon localScale while visible",
        "UI-034：显示期间图标 localScale",
    ),
]

EN_COL = re.compile(r"^[A-Za-z_][A-Za-z0-9_.]*$")
MAX_SCAN = 3
_MAX_DECIMAL_PLACES = 10


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
    raise ValueError("no English header")


def escape_csv_field(value: str) -> str:
    if any(ch in value for ch in [",", '"', "\n", "\r"]):
        return '"' + value.replace('"', '""') + '"'
    return value


def build_csv(rows: list[list[str]]) -> str:
    lines = []
    for r_i, row in enumerate(rows):
        if r_i > 0 and all(not (c or "").strip() for c in row):
            continue
        lines.append(",".join(escape_csv_field(c or "") for c in row))
    return "\n".join(lines) + ("\n" if lines else "")


def patch_excel(xlsx_path: Path) -> None:
    wb = load_workbook(xlsx_path)
    ws = wb.active
    # Delete old keys (bottom-up)
    for r in range(ws.max_row, 3, -1):
        key = ws.cell(r, 1).value
        if key in OLD_KEYS:
            ws.delete_rows(r, 1)
            print(f"DEL {key} from {xlsx_path.name}")

    existing = {ws.cell(r, 1).value for r in range(1, ws.max_row + 1)}
    # Find insert point: after EdgeMargin or before IconSize or after any OffScreen key
    insert_at = None
    for r in range(1, ws.max_row + 1):
        if ws.cell(r, 1).value == "OffScreenSpawnHintEdgeMarginPx":
            insert_at = r  # insert before EdgeMargin — put new keys first block
            break
    if insert_at is None:
        for r in range(1, ws.max_row + 1):
            if ws.cell(r, 1).value == "OffScreenSpawnHintIconSizePx":
                insert_at = r
                break
    if insert_at is None:
        raise SystemExit(f"no OffScreenSpawnHintEdgeMarginPx/IconSize in {xlsx_path}")

    to_add = [row for row in NEW_ROWS if row[0] not in existing]
    if to_add:
        ws.insert_rows(insert_at, amount=len(to_add))
        for i, row in enumerate(to_add):
            for c, val in enumerate(row, start=1):
                ws.cell(insert_at + i, c).value = val
        print(f"INS {len(to_add)} rows at {insert_at} in {xlsx_path.name}")
    else:
        print(f"SKIP new keys already present in {xlsx_path.name}")

    wb.save(xlsx_path)
    wb.close()


def bake(excel_dir: Path, csv_dir: Path) -> None:
    xlsx = excel_dir / EXCEL_NAME
    with open(xlsx, "rb") as raw:
        data = raw.read()
    wb = load_workbook(io.BytesIO(data), read_only=True, data_only=True)
    ws = wb.active
    rows = [[cell_to_csv_text(v) for v in row] for row in ws.iter_rows(values_only=True)]
    wb.close()
    header_i = find_header_index(rows)
    text = build_csv(rows[header_i:])
    out = csv_dir / CSV_NAME
    out.write_text(text, encoding="utf-8", newline="\n")
    keys = [ln.split(",", 1)[0] for ln in text.splitlines()[1:]]
    for k in OLD_KEYS:
        if k in keys:
            raise SystemExit(f"old key still in {out}: {k}")
    for row in NEW_ROWS:
        if row[0] not in keys:
            raise SystemExit(f"missing {row[0]} in {out}")
    print(f"BAKED {out}")


def must_replace(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise SystemExit(f"missing: {label}")
    return text.replace(old, new, 1)


def patch_docs() -> None:
    # SPEC_00
    p = ROOT / "SPEC_00_Index.md"
    t = p.read_text(encoding="utf-8")
    t = must_replace(t, "**文档版本 / Document Version:** v0.84.46", "**文档版本 / Document Version:** v0.84.47", "ver")
    zh_row = (
        "| 2026-09-09 | v0.84.47 | UI-034 调参：开场 0.4s 内闪红 2 次，再常亮 2s；显示 `localScale=1.3`；常量改 `IntroBlink*`/`HoldSeconds`/`DisplayScale`（移除 Duration/BlinkPeriod）。同步 SPEC_03/04、CONTEXT、`OffScreenSpawnHintView` |\n"
        "| 2026-09-09 | v0.84.46 |"
    )
    t = must_replace(t, "| 2026-09-09 | v0.84.46 |", zh_row, "zh changelog")
    en_row = (
        "| 2026-09-09 | v0.84.47 | UI-034 retune: 2 red blinks in 0.4s then solid hold 2s; display scale 1.3; constants IntroBlink*/HoldSeconds/DisplayScale (drop Duration/BlinkPeriod). Synced SPEC_03/04, CONTEXT, OffScreenSpawnHintView |\n"
        "| 2026-09-09 | v0.84.46 |"
    )
    # English section second occurrence of v0.84.46 changelog
    first = t.find("| 2026-09-09 | v0.84.46 |")
    second = t.find("| 2026-09-09 | v0.84.46 |", first + 1)
    if second < 0:
        raise SystemExit("en changelog missing")
    # After zh replace, first is the zh 46 under 47; second should be English
    # Find English table entry
    en_marker = "| 2026-09-09 | v0.84.46 | Off-screen spawn edge hint"
    if en_marker not in t:
        raise SystemExit("en 46 marker missing")
    t = t.replace(
        en_marker,
        "| 2026-09-09 | v0.84.47 | UI-034 retune: 2 red blinks in 0.4s then solid hold 2s; display scale 1.3; constants IntroBlink*/HoldSeconds/DisplayScale (drop Duration/BlinkPeriod). Synced SPEC_03/04, CONTEXT, OffScreenSpawnHintView |\n"
        + en_marker,
        1,
    )
    p.write_text(t, encoding="utf-8")
    print("SPEC_00 OK")

    # SPEC_03 replacements
    p = ROOT / "SPEC_03_GameRules.md"
    t = p.read_text(encoding="utf-8")
    t = must_replace(
        t,
        "闪烁 `OffScreenSpawnHintDurationSeconds`（默认 1.5s）后消失",
        "开场 `OffScreenSpawnHintIntroBlinkSeconds`（默认 0.4s）内闪红 `OffScreenSpawnHintIntroBlinkCount`（默认 2）次，再常亮 `OffScreenSpawnHintHoldSeconds`（默认 2s）；显示缩放 `OffScreenSpawnHintDisplayScale`（默认 1.3）",
        "zh term",
    )
    t = must_replace(
        t,
        "blink for `OffScreenSpawnHintDurationSeconds` (default 1.5s) then hide",
        "2 red blinks in `OffScreenSpawnHintIntroBlinkSeconds` (default 0.4s, count `OffScreenSpawnHintIntroBlinkCount`=2), then solid hold `OffScreenSpawnHintHoldSeconds` (default 2s); display scale `OffScreenSpawnHintDisplayScale` (default 1.3)",
        "en term",
    )
    t = must_replace(
        t,
        "图标不旋转；alpha 周期闪烁，总时长 `OffScreenSpawnHintDurationSeconds`（默认 1.5）；同帧多组可各出一枚",
        "图标不旋转；开场 0.4s 内闪红 2 次（`Image` 白↔红，alpha=1），再常亮 2s；显示 `localScale=1.3`；同帧多组可各出一枚",
        "zh ui034",
    )
    t = must_replace(
        t,
        "icon does not rotate; alpha blink for `OffScreenSpawnHintDurationSeconds` (default 1.5); multiple groups may each show one",
        "icon does not rotate; 2 red blinks in 0.4s (white↔red, alpha=1) then solid 2s; display `localScale=1.3`; multiple groups may each show one",
        "en ui034",
    )
    t = must_replace(
        t,
        "边缘 `EnemyAttack_1` 闪烁 1.5s；Prepare 预览不出",
        "边缘 `EnemyAttack_1` 开场闪红 2 次（0.4s）后常亮 2s、放大 1.3；Prepare 预览不出",
        "zh d090",
    )
    t = must_replace(
        t,
        "edge `EnemyAttack_1` blinks 1.5s; Prepare preview skipped",
        "edge `EnemyAttack_1` 2 red blinks in 0.4s then hold 2s at scale 1.3; Prepare preview skipped",
        "en d090",
    )
    t = must_replace(
        t,
        "（`Resources/UI/Icons/EnemyAttack_1`；缺图空框仍闪；图标不旋转）。Alpha 周期闪烁，总时长 `OffScreenSpawnHintDurationSeconds`（默认 **1.5**；周期 `OffScreenSpawnHintBlinkPeriodSeconds`）。同帧多组可各出一枚。",
        "（`Resources/UI/Icons/EnemyAttack_1`；缺图空框仍显示；图标不旋转；`localScale=OffScreenSpawnHintDisplayScale` 默认 **1.3**）。"
        "开场 `OffScreenSpawnHintIntroBlinkSeconds`（默认 **0.4**）内闪红 `OffScreenSpawnHintIntroBlinkCount`（默认 **2**）次（白↔红，alpha=1），"
        "再常亮 `OffScreenSpawnHintHoldSeconds`（默认 **2**）。同帧多组可各出一枚。",
        "zh edge",
    )
    t = must_replace(
        t,
        "(`Resources/UI/Icons/EnemyAttack_1`; missing → empty frame still blinks; no rotation). Alpha blink for `OffScreenSpawnHintDurationSeconds` (default **1.5**; period `OffScreenSpawnHintBlinkPeriodSeconds`). Multiple groups may each show one.",
        "(`Resources/UI/Icons/EnemyAttack_1`; missing → empty frame still shown; no rotation; `localScale=OffScreenSpawnHintDisplayScale` default **1.3**). "
        "Intro: `OffScreenSpawnHintIntroBlinkSeconds` (default **0.4**) with `OffScreenSpawnHintIntroBlinkCount` (default **2**) red blinks (white↔red, alpha=1), "
        "then solid hold `OffScreenSpawnHintHoldSeconds` (default **2**). Multiple groups may each show one.",
        "en edge",
    )
    t = must_replace(
        t,
        "边缘 `EnemyAttack_1` 闪 1.5s；Prepare / Ended / UI-017 / UI-032 清空",
        "边缘 `EnemyAttack_1` 闪红 2 次+常亮 2s、×1.3；Prepare / Ended / UI-017 / UI-032 清空",
        "zh se",
    )
    t = must_replace(
        t,
        "edge `EnemyAttack_1` blink 1.5s; clear on Prepare / Ended / UI-017 / UI-032",
        "edge `EnemyAttack_1` 2 red blinks + hold 2s at ×1.3; clear on Prepare / Ended / UI-017 / UI-032",
        "en se",
    )
    p.write_text(t, encoding="utf-8")
    print("SPEC_03 OK")

    # SPEC_04
    p = ROOT / "SPEC_04_Technical.md"
    t = p.read_text(encoding="utf-8")
    old_zh = (
        "| `OffScreenSpawnHintDurationSeconds` | 离屏来袭提示时长 | `1.5` | UI-034：边缘图标闪烁总秒数后消失 |\n"
        "| `OffScreenSpawnHintBlinkPeriodSeconds` | 离屏来袭闪烁周期 | `0.25` | UI-034：alpha 一亮一灭周期秒 |\n"
    )
    new_zh = (
        "| `OffScreenSpawnHintIntroBlinkSeconds` | 离屏来袭开场闪红秒 | `0.4` | UI-034：开场闪红总时长 |\n"
        "| `OffScreenSpawnHintIntroBlinkCount` | 离屏来袭开场闪红次数 | `2` | UI-034：开场窗口内闪红次数 |\n"
        "| `OffScreenSpawnHintHoldSeconds` | 离屏来袭常亮秒 | `2` | UI-034：闪红后常亮秒数 |\n"
        "| `OffScreenSpawnHintDisplayScale` | 离屏来袭显示缩放 | `1.3` | UI-034：显示期间图标 localScale |\n"
    )
    t = must_replace(t, old_zh, new_zh, "zh keys")
    old_en = (
        "| `OffScreenSpawnHintDurationSeconds` | 离屏来袭提示时长 | `1.5` | UI-034: edge icon blink total seconds then hide |\n"
        "| `OffScreenSpawnHintBlinkPeriodSeconds` | 离屏来袭闪烁周期 | `0.25` | UI-034: alpha on/off period seconds |\n"
    )
    new_en = (
        "| `OffScreenSpawnHintIntroBlinkSeconds` | 离屏来袭开场闪红秒 | `0.4` | UI-034: intro red-blink window seconds |\n"
        "| `OffScreenSpawnHintIntroBlinkCount` | 离屏来袭开场闪红次数 | `2` | UI-034: red blink count inside intro window |\n"
        "| `OffScreenSpawnHintHoldSeconds` | 离屏来袭常亮秒 | `2` | UI-034: solid hold seconds after intro blink |\n"
        "| `OffScreenSpawnHintDisplayScale` | 离屏来袭显示缩放 | `1.3` | UI-034: icon localScale while visible |\n"
    )
    t = must_replace(t, old_en, new_en, "en keys")
    # §6 mention if any duration wording
    if "闪烁时长/周期/边距/尺寸" in t:
        t = must_replace(
            t,
            "闪烁时长/周期/边距/尺寸 ← §9.20b `OffScreenSpawnHint*`",
            "开场闪红/常亮/缩放/边距/尺寸 ← §9.20b `OffScreenSpawnHint*`",
            "zh §6",
        )
    if "duration/period/margin/size ← §9.20b `OffScreenSpawnHint*`" in t:
        t = must_replace(
            t,
            "duration/period/margin/size ← §9.20b `OffScreenSpawnHint*`",
            "intro blink/hold/scale/margin/size ← §9.20b `OffScreenSpawnHint*`",
            "en §6",
        )
    p.write_text(t, encoding="utf-8")
    print("SPEC_04 OK")

    # CONTEXT
    p = ROOT / "CONTEXT.md"
    t = p.read_text(encoding="utf-8")
    t = must_replace(
        t,
        "屏幕边缘 `EnemyAttack_1` 闪 1.5s；Prepare 预览不出；UI-034 / D-090",
        "屏幕边缘 `EnemyAttack_1` 闪红 2 次（0.4s）+常亮 2s、×1.3；Prepare 预览不出；UI-034 / D-090",
        "context",
    )
    p.write_text(t, encoding="utf-8")
    print("CONTEXT OK")


def main() -> int:
    patch_docs()
    for excel_dir, csv_dir in [
        (ASSETS / "Excel", ASSETS / "Csv"),
        (ASSETS / "Mode2" / "Excel", ASSETS / "Mode2" / "Csv"),
    ]:
        patch_excel(excel_dir / EXCEL_NAME)
        bake(excel_dir, csv_dir)
    return 0


if __name__ == "__main__":
    sys.exit(main())
