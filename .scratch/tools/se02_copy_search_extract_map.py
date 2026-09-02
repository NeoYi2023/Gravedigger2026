# -*- coding: utf-8 -*-
"""SE-02 Approach B fallback: copy PushMap_Demo_01 YAML → SearchExtract_Demo_01.

Prefer Unity menu Gravedigger2026/SearchExtract/Ensure Sample Map Prefab.
This script is for machines without the Editor binary.
"""
from __future__ import annotations

import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets"
MAPS = ROOT / "Prefabs" / "Maps"
CATALOG = ROOT / "Settings" / "Defend" / "DefendPrefabCatalog.asset"
SRC = MAPS / "PushMap_Demo_01.prefab"
DST = MAPS / "SearchExtract_Demo_01.prefab"
DST_META = DST.with_suffix(".prefab.meta")
NEW_GUID = "c4e8a1b27d5f4e6a9b0c1d2e3f405162"

OBJ_GO = "8800112233445566771"
OBJ_TF = "8800112233445566772"
OBJ_CAP = "8800112233445566773"
OBJ_PT = "8800112233445566774"

OBJECTIVE_02 = f"""--- !u!1 &{OBJ_GO}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {OBJ_TF}}}
  - component: {{fileID: {OBJ_CAP}}}
  - component: {{fileID: {OBJ_PT}}}
  m_Layer: 0
  m_Name: Objective_02
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{OBJ_TF}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {OBJ_GO}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 9.51, y: 0.05, z: 4.95}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: 2092522387770036821}}
  m_RootOrder: 17
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{OBJ_CAP}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {OBJ_GO}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: c5f9d2b38e4a5f7b0d1e2f3a4b5c6d7e, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  _radius: 2
--- !u!114 &{OBJ_PT}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {OBJ_GO}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: d6a0e3c49f5b608c1e2f3a4b5c6d7e8f, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  _objectiveOrder: 2
  _captureZone: {{fileID: {OBJ_CAP}}}
"""

META = f"""fileFormatVersion: 2
guid: {NEW_GUID}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def main() -> None:
    if not SRC.exists():
        raise SystemExit(f"missing {SRC}")
    shutil.copyfile(SRC, DST)
    text = DST.read_text(encoding="utf-8")
    if "m_Name: Objective_02" not in text:
        marker = "--- !u!1 &2056678869900902072\n"
        if marker not in text:
            raise SystemExit("PushMapMarkers GameObject block not found")
        text = text.replace(marker, OBJECTIVE_02 + marker, 1)
        needle = "  - {fileID: 6111222333444555602}\n"
        insert = needle + f"  - {{fileID: {OBJ_TF}}}\n"
        if needle not in text:
            raise SystemExit("PushMapMarkers child list tail not found")
        text = text.replace(needle, insert, 1)
    text = text.replace("  m_Name: PushMap_Demo_01\n", "  m_Name: SearchExtract_Demo_01\n", 1)
    text = text.replace(
        "  m_LocalPosition: {x: 10.51, y: 0.09, z: 5.39}",
        "  m_LocalPosition: {x: 10.51, y: 0.09, z: 5.14}",
        1,
    )
    text = text.replace(
        "  m_LocalPosition: {x: 10.36, y: 0.05, z: 5.36}",
        "  m_LocalPosition: {x: 8.11, y: 0.05, z: 4.25}",
        1,
    )
    DST.write_text(text, encoding="utf-8")
    DST_META.write_text(META, encoding="utf-8")

    catalog = CATALOG.read_text(encoding="utf-8")
    entry = (
        "  - MapId: SearchExtract_Demo_01\n"
        f"    Prefab: {{fileID: 2120705586636139558, guid: {NEW_GUID}, type: 3}}\n"
    )
    if "MapId: SearchExtract_Demo_01" not in catalog:
        catalog = catalog.replace(
            "  - MapId: PushMap_Demo_03\n"
            "    Prefab: {fileID: 2120705586636139558, guid: 435a23c017f68c54da6fb127320eac73, type: 3}\n",
            "  - MapId: PushMap_Demo_03\n"
            "    Prefab: {fileID: 2120705586636139558, guid: 435a23c017f68c54da6fb127320eac73, type: 3}\n"
            + entry,
            1,
        )
        CATALOG.write_text(catalog, encoding="utf-8")
    print(f"wrote {DST} guid={NEW_GUID}")


if __name__ == "__main__":
    main()
