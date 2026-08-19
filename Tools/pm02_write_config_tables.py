# -*- coding: utf-8 -*-
"""Write OpenXML .xlsx (inline strings) + UTF-8 CSV for PM-02 tables."""
from __future__ import annotations

import csv
import shutil
import zipfile
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path("Gravedigger2026/Assets/ConfigTables")
EXCEL = ROOT / "Excel"
CSV_DIR = ROOT / "Csv"

CONTENT_TYPES = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
</Types>
"""

RELS = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>
"""

WB_RELS = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>
"""

WORKBOOK = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="Sheet1" sheetId="1" r:id="rId1"/>
  </sheets>
</workbook>
"""

STYLES = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <fonts count="1"><font><sz val="11"/><name val="Calibri"/></font></fonts>
  <fills count="1"><fill><patternFill patternType="none"/></fill></fills>
  <borders count="1"><border/></borders>
  <cellStyleXfs count="1"><xf/></cellStyleXfs>
  <cellXfs count="1"><xf/></cellXfs>
</styleSheet>
"""


def col_name(idx: int) -> str:
    n = idx + 1
    s = ""
    while n:
        n, r = divmod(n - 1, 26)
        s = chr(65 + r) + s
    return s


def sheet_xml(rows: list[list[str]]) -> str:
    max_c = max((len(r) for r in rows), default=1)
    max_r = max(len(rows), 1)
    dim = f"A1:{col_name(max_c - 1)}{max_r}"
    parts = [
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>',
        '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" '
        'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">',
        f'<dimension ref="{dim}"/>',
        "<sheetViews><sheetView workbookViewId=\"0\"/></sheetViews>",
        "<sheetData>",
    ]
    for ri, row in enumerate(rows, start=1):
        parts.append(f'<row r="{ri}">')
        for ci, val in enumerate(row):
            ref = f"{col_name(ci)}{ri}"
            parts.append(f'<c r="{ref}" t="str"><v>{escape(val)}</v></c>')
        parts.append("</row>")
    parts.append("</sheetData>")
    parts.append(
        f'<ignoredErrors><ignoredError numberStoredAsText="1" sqref="{dim}"/></ignoredErrors>'
    )
    parts.append("</worksheet>")
    return "".join(parts)


def write_xlsx(path: Path, rows: list[list[str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(".xlsx.tmpzip")
    if tmp.exists():
        tmp.unlink()
    with zipfile.ZipFile(tmp, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        zf.writestr("[Content_Types].xml", CONTENT_TYPES)
        zf.writestr("_rels/.rels", RELS)
        zf.writestr("xl/workbook.xml", WORKBOOK)
        zf.writestr("xl/_rels/workbook.xml.rels", WB_RELS)
        zf.writestr("xl/styles.xml", STYLES)
        zf.writestr("xl/worksheets/sheet1.xml", sheet_xml(rows))
    if path.exists():
        path.unlink()
    shutil.move(str(tmp), str(path))


def write_csv(path: Path, rows: list[list[str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as f:
        writer = csv.writer(f, lineterminator="\n")
        writer.writerows(rows)


def main() -> None:
    monster = [
        [
            "MonsterId",
            "ModelId",
            "DisplayName",
            "TargetSelect",
            "AttackMode",
            "AggroMode",
            "AlertRadius",
            "MaxHP",
            "MoveSpeed",
            "AttackPower",
            "AttackSpeed",
            "AttackRange",
            "MeleeWindupSeconds",
            "RangedProjectileSpeed",
            "RangedTimeoutSeconds",
            "Skills",
            "LootDrop",
        ],
        ["Monster_01", "MonsterModel_01", "腐尸", "Nearest", "Melee", "ActiveChase", "", "80", "0.5", "8", "0.8", "0.25", "0.3", "0", "0", "", "Iron;1|Spirit;5"],
        ["Monster_02", "MonsterModel_02", "骷髅兵", "PreferWarrior", "Ranged", "PassiveChase", "0.5", "120", "0.5", "10", "0.85", "0.25", "0", "11", "2.5", "", "Bone;1|Spirit;4"],
        ["Monster_03", "MonsterModel_03", "墓园犬", "PreferProtagonist", "Melee", "StationaryActive", "1", "160", "0.5", "12", "0.9", "0.25", "0.34", "0", "0", "", "Iron;3|Spirit;7"],
        ["Monster_04", "MonsterModel_04", "幽魂", "Nearest", "Melee", "StationaryPassive", "", "200", "0.5", "14", "0.95", "0.25", "0.36", "0", "0", "", "Bone;1|Spirit;6"],
        ["Monster_05", "MonsterModel_05", "石像鬼", "PreferWarrior", "Ranged", "ActiveChase", "0.4", "240", "0.5", "16", "1", "0.25", "0", "14", "2.5", "", "Iron;2|Spirit;9"],
        ["Monster_06", "MonsterModel_06", "巫妖徒", "PreferProtagonist", "Melee", "PassiveChase", "", "280", "0.5", "18", "1.05", "0.25", "0.4", "0", "0", "", "Bone;1|Spirit;8"],
        ["Monster_07", "MonsterModel_07", "骨龙幼崽", "Nearest", "Melee", "ActiveChase", "", "320", "0.5", "20", "1.1", "0.25", "0.42", "0", "0", "", "Iron;1|Spirit;11"],
        ["Monster_08", "MonsterModel_08", "血仆", "PreferWarrior", "Ranged", "ActiveChase", "0.6", "360", "0.5", "22", "1.15", "0.25", "0", "17", "2.5", "", "Bone;1|Spirit;10"],
        ["Monster_09", "MonsterModel_09", "暗影刺客", "PreferProtagonist", "Melee", "StationaryActive", "0.8", "400", "0.5", "24", "1.2", "0.25", "0.46", "0", "0", "", "Iron;3|Spirit;13"],
        ["Monster_10", "MonsterModel_10", "巨型僵尸", "Nearest", "Melee", "ActiveChase", "1.2", "440", "0.5", "26", "1.25", "0.25", "0.48", "0", "0", "", "Bone;1|Spirit;12"],
    ]

    gameplay = [
        [
            "GameplayConfigId",
            "MapId",
            "DisplayName",
            "StageExpReward",
            "CaptureLoot",
            "DungeonUnlockIds",
            "CaptureSeconds",
            "Notes",
        ],
        [
            "PushMap_01",
            "PushMap_Demo_01",
            "推图样例一",
            "100",
            "Iron;2|Spirit;10",
            "Dungeon_Stub_01",
            "5",
            "PM-02 sample",
        ],
    ]

    spawn = [
        [
            "GameplayConfigId",
            "SpawnPointId",
            "MonsterId",
            "SpawnCount",
            "LinkedObjectiveOrder",
            "TrapZoneId",
            "IsBoss",
            "SpawnOrder",
        ],
        # non-trap start spawn linked to objective 1
        ["PushMap_01", "SP_01", "Monster_01", "2", "1", "", "0", "1"],
        ["PushMap_01", "SP_01", "Monster_02", "1", "1", "", "0", "2"],
        # trap spawn
        ["PushMap_01", "SP_02", "Monster_03", "2", "1", "TZ_01", "0", "1"],
        # non-trap for objective 2
        ["PushMap_01", "SP_03", "Monster_04", "1", "2", "", "0", "1"],
        # BOSS
        ["PushMap_01", "Boss_01", "Monster_10", "1", "0", "", "1", "1"],
    ]

    write_csv(CSV_DIR / "Defend_MonsterConfig.csv", monster)
    write_xlsx(EXCEL / "防守_怪物配置表_Defend_MonsterConfig.xlsx", monster)

    write_csv(CSV_DIR / "PushMap_PushMapGameplayConfig.csv", gameplay)
    write_xlsx(EXCEL / "推图战_推图战配置表_PushMap_PushMapGameplayConfig.xlsx", gameplay)

    write_csv(CSV_DIR / "PushMap_PushMapSpawnConfig.csv", spawn)
    write_xlsx(EXCEL / "推图战_刷怪配置表_PushMap_PushMapSpawnConfig.xlsx", spawn)

    print("wrote monster / pushmap gameplay / pushmap spawn csv+xlsx")


if __name__ == "__main__":
    main()
