# -*- coding: utf-8 -*-
"""SE-01: Mode2 SearchExtract tables + SubLevel GatherPoint columns (Approach A).

Mode1 SubLevel is unchanged. Excel three-row headers (SPEC_04 §14.7).
Writes Mode2 Excel + CSV + .meta if missing.
"""
from __future__ import annotations

import csv
import math
import uuid
from decimal import Decimal, ROUND_HALF_UP
from pathlib import Path

from openpyxl import Workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"
MODE2 = ROOT / "Mode2"
_MAX_DECIMAL_PLACES = 10

SUB_XLSX = "关卡_子关卡表_Level_SubLevelConfig.xlsx"
SUB_CSV = "Level_SubLevelConfig.csv"
GP_XLSX = "搜打撤_玩法配置表_SearchExtract_SearchExtractGameplayConfig.xlsx"
GP_CSV = "SearchExtract_SearchExtractGameplayConfig.csv"
WV_XLSX = "搜打撤_刷怪波次配置表_SearchExtract_SearchExtractWaveSpawnConfig.xlsx"
WV_CSV = "SearchExtract_SearchExtractWaveSpawnConfig.csv"

SUB_ZH = [
    "玩法选项ID",
    "玩法类型",
    "玩法配置ID",
    "搜集点数量",
    "搜集点奖励",
    "玩法图标",
    "标题文字",
    "描述文字",
    "奖励",
    "解锁下一阶段选项",
]
SUB_NOTE = [
    "主键",
    "Shop/Dig/AutoManufacture/UpgradeManufacture/Defend/PushMap/SearchExtract",
    "Dig/Defend/PushMap/SearchExtract 查表；Shop/UM/AM 忽略",
    "仅 SearchExtract；≥1；其它类型空",
    "仅 SearchExtract；N:ItemId;Count|…；N: 开头为新点",
    "Resources/UI/Levels/{IconAssetId}",
    "路线图直显",
    "路线图直显；SearchExtract 另展示点奖励摘要",
    "ItemId;Count|…；可空；Leave 发放",
    "OptId|OptId；须属 Stage+1；空=关卡胜利",
]
SUB_EN = [
    "GameplayOptionId",
    "GameplayType",
    "GameplayConfigId",
    "GatherPointCount",
    "GatherPointRewards",
    "IconAssetId",
    "Title",
    "Description",
    "Reward",
    "UnlockNextOptionIds",
]

GP_ZH = ["玩法配置ID", "地图编号", "阶段经验奖励", "搜集倒计时秒"]
GP_NOTE = [
    "主键；SubLevel GameplayConfigId FK",
    "Ground_* / PushMap_* / SearchExtract_* → Prefabs/Maps/{MapId}.prefab",
    "≥0；Leave 时入账；0=不发",
    "全局 per 本行；>0；刷怪表无覆盖",
]
GP_EN = ["GameplayConfigId", "MapId", "StageExpReward", "GatherCountdownSeconds"]
GP_ROWS = [["SearchExtract_01", "SearchExtract_Demo_01", 0, 30]]

WV_ZH = [
    "玩法配置ID",
    "搜集点序号",
    "波次序号",
    "第一波前置秒",
    "波间间隔秒",
    "刷怪点ID",
    "怪物ID",
    "出现数量",
]
WV_NOTE = [
    "FK → SearchExtractGameplayConfig",
    "对齐地图 ObjectiveOrder",
    "≥1；一行一波；同点升序",
    "自点激活起；同点各波须相同",
    "第 2 波起；同点各波须相同",
    "FK 地图 SpawnPoint",
    "FK → MonsterConfig",
    "≥1",
]
WV_EN = [
    "GameplayConfigId",
    "GatherPointOrder",
    "WaveIndex",
    "FirstWaveDelaySeconds",
    "WaveIntervalSeconds",
    "SpawnPointId",
    "MonsterId",
    "SpawnCount",
]
WV_ROWS = [
    ["SearchExtract_01", 1, 1, 2, 8, "SP_01", "Monster_01", 3],
    ["SearchExtract_01", 1, 2, 2, 8, "SP_01", "Monster_01", 3],
    ["SearchExtract_01", 2, 1, 2, 8, "SP_02", "Monster_01", 3],
    ["SearchExtract_01", 2, 2, 2, 8, "SP_02", "Monster_01", 3],
]

SAMPLE_SUB = [
    "Opt_SE_Demo_01",
    "SearchExtract",
    "SearchExtract_01",
    2,
    "1:Spirit;10|2:Spirit;20",
    "Opt_SE_Demo_01",
    "搜打撤",
    "搜打撤样例 SearchExtract_01",
    "Spirit;20",
    "",
]

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


def load_existing_sub_rows(csv_path: Path) -> list[list[object]]:
    with csv_path.open("r", encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        rows: list[list[object]] = []
        for raw in reader:
            oid = (raw.get("GameplayOptionId") or "").strip()
            if not oid or oid == "Opt_SE_Demo_01":
                continue
            rows.append(
                [
                    oid,
                    (raw.get("GameplayType") or "").strip(),
                    (raw.get("GameplayConfigId") or "").strip(),
                    (raw.get("GatherPointCount") or "").strip(),
                    (raw.get("GatherPointRewards") or "").strip(),
                    (raw.get("IconAssetId") or "").strip(),
                    (raw.get("Title") or "").strip(),
                    (raw.get("Description") or "").strip(),
                    (raw.get("Reward") or "").strip(),
                    (raw.get("UnlockNextOptionIds") or "").strip(),
                ]
            )
        return rows


def main() -> None:
    excel = MODE2 / "Excel"
    csv_dir = MODE2 / "Csv"
    existing_csv = csv_dir / SUB_CSV
    if not existing_csv.exists():
        raise SystemExit(f"missing {existing_csv}")

    sub_rows = load_existing_sub_rows(existing_csv)
    sub_rows.append(SAMPLE_SUB)

    write_xlsx(excel / SUB_XLSX, SUB_ZH, SUB_NOTE, SUB_EN, sub_rows)
    write_csv(csv_dir / SUB_CSV, SUB_EN, sub_rows)
    write_xlsx(excel / GP_XLSX, GP_ZH, GP_NOTE, GP_EN, GP_ROWS)
    write_csv(csv_dir / GP_CSV, GP_EN, GP_ROWS)
    write_xlsx(excel / WV_XLSX, WV_ZH, WV_NOTE, WV_EN, WV_ROWS)
    write_csv(csv_dir / WV_CSV, WV_EN, WV_ROWS)
    print(f"Mode2 SubLevel rows={len(sub_rows)} gameplay={len(GP_ROWS)} waves={len(WV_ROWS)}")


if __name__ == "__main__":
    main()
