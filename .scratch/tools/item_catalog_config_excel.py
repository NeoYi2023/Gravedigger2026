# -*- coding: utf-8 -*-
"""Create Item_ItemCatalogConfig Excel (Mode1 + Mode2) from existing CSV."""
from __future__ import annotations

import csv
import uuid
from pathlib import Path

from openpyxl import Workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"
XLSX_NAME = "通用_道具汇总表_Item_ItemCatalogConfig.xlsx"
CSV_NAME = "Item_ItemCatalogConfig.csv"

FIELD_ZH = {
    "ItemId": ("道具ID", "主键；奖励系统公共 Id；CaptureLoot 等奖励字段只认本列"),
    "DisplayName": ("道具名", "道具公共展示名；未启用 i18n 时直接当展示串；空则 UI 回退 ItemId"),
    "IconAssetId": ("道具图标", "奖励弹窗/通用掉落 UI 使用的专属图标资源 Id"),
    "ItemType": ("道具类型", "Currency | Material | BodyPart | MagicBook | ProtagonistEquipment"),
    "SourceTable": ("管理道具属性配置表", "该道具权威来源表名；运行时据此校验与分发"),
    "Description": ("道具描述", "通用描述文案；奖励展示优先取本列"),
    "SellPrice": ("售卖价格", "≥ 0；本轮仅存档与展示，未接出售系统时不驱动运行时"),
}

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def read_csv(path: Path) -> tuple[list[str], list[list[str]]]:
    with path.open(encoding="utf-8", newline="") as f:
        rows = list(csv.reader(f))
    header = [c.strip() for c in rows[0]]
    data: list[list[str]] = []
    for row in rows[1:]:
        if not any(c.strip() for c in row):
            continue
        padded = list(row) + [""] * max(0, len(header) - len(row))
        data.append(padded[: len(header)])
    return header, data


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


def ensure_meta(xlsx_path: Path) -> None:
    meta_path = xlsx_path.with_suffix(xlsx_path.suffix + ".meta")
    if meta_path.exists():
        return
    meta_path.write_text(META_TEMPLATE.format(guid=uuid.uuid4().hex), encoding="utf-8")


def main() -> None:
    for label, csv_dir, xlsx_dir in (
        ("Mode1", ROOT / "Csv", ROOT / "Excel"),
        ("Mode2", ROOT / "Mode2" / "Csv", ROOT / "Mode2" / "Excel"),
    ):
        csv_path = csv_dir / CSV_NAME
        xlsx_path = xlsx_dir / XLSX_NAME
        header, data = read_csv(csv_path)
        write_xlsx(xlsx_path, header, data)
        ensure_meta(xlsx_path)
        print(f"{label}: rows={len(data)} cols={len(header)} -> {xlsx_path}")


if __name__ == "__main__":
    main()
