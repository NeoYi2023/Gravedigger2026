import csv
import uuid
from pathlib import Path

from openpyxl import Workbook

ROOT = Path(r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\Gravedigger2026\Assets\ConfigTables")
EXCEL_NAME = "战斗_怪物技能效果配置表_Combat_MonsterSkillEffectConfig.xlsx"

HEADERS_ZH = ["怪物技能ID", "名称", "效果种类", "效果参数"]
HEADERS_NOTE = [
    "主键；与 MonsterConfig.Skills 段内 SkillId 对齐",
    "展示名",
    "登记制 Token；首版 MonsterSelfReviveOnDeath",
    "DelaySeconds / ReviveHpRatio / InvincibleSeconds / ReviveAnimSeconds / MaxReviveCount / AlertRadius (first revive only)",
]
HEADERS_EN = ["MonsterSkillId", "DisplayName", "EffectKind", "EffectParams"]

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def read_csv_rows(csv_path: Path):
    with csv_path.open("r", encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        return [
            [
                row["MonsterSkillId"],
                row["DisplayName"],
                row["EffectKind"],
                row["EffectParams"],
            ]
            for row in reader
        ]


def write_workbook(target: Path, data_rows):
    wb = Workbook()
    ws = wb.active
    ws.title = "Sheet1"
    ws.append(HEADERS_ZH)
    ws.append(HEADERS_NOTE)
    ws.append(HEADERS_EN)
    for row in data_rows:
        ws.append(row)
    target.parent.mkdir(parents=True, exist_ok=True)
    wb.save(target)


def write_meta(meta_path: Path):
    if meta_path.exists():
        return
    meta_path.write_text(META_TEMPLATE.format(guid=uuid.uuid4().hex), encoding="utf-8")


def main():
    pairs = [
        (ROOT / "Excel", ROOT / "Csv" / "Combat_MonsterSkillEffectConfig.csv"),
        (ROOT / "Mode2" / "Excel", ROOT / "Mode2" / "Csv" / "Combat_MonsterSkillEffectConfig.csv"),
    ]

    for excel_dir, csv_path in pairs:
        data = read_csv_rows(csv_path)
        xlsx = excel_dir / EXCEL_NAME
        write_workbook(xlsx, data)
        write_meta(xlsx.with_suffix(xlsx.suffix + ".meta"))
        print(f"Wrote {xlsx} ({len(data)} rows)")


if __name__ == "__main__":
    main()
