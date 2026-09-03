#!/usr/bin/env python3
"""Fix PushMap_Demo_* FormationClassZones: replace overflow fileIDs (>Int64) with safe IDs.

Previous inject used BASE=9300600000000100000 which exceeds Int64.MaxValue and breaks Unity import.
"""
from __future__ import annotations

import csv
import re
from pathlib import Path

ROOT = Path(r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\Gravedigger2026")
CSV_PATH = ROOT / "Assets/ConfigTables/Mode2/Csv/Manufacture_ClassConfig.csv"
MAPS = [
    ROOT / "Assets/Prefabs/Maps/PushMap_Demo_01.prefab",
    ROOT / "Assets/Prefabs/Maps/PushMap_Demo_02.prefab",
    ROOT / "Assets/Prefabs/Maps/PushMap_Demo_03.prefab",
]

ZONE_SCRIPT = "f06a11c1a55e46d2b8e3c4d5e6f70819"
ROOT_SCRIPT = "7c4e9a2b8d1f4c6e9a3b5d7e1f0a2c48"
HALF = (3.85, 2.0)
KNOWN_REL = {
    "Class_Warrior": (-2.0, -1.2),
    "Class_Knight": (-1.0, -1.2),
    "Class_Paladin": (0.0, -1.2),
    "Class_Berserker": (1.0, -1.2),
    "Class_Rogue": (2.0, -1.2),
    "Class_Servants": (0.0, -0.2),
    "Class_Archer": (-2.0, 1.0),
    "Class_Ranger": (-1.0, 1.0),
    "Class_Mage": (0.0, 1.0),
    "Class_Warlock": (1.0, 1.0),
    "Class_Priest": (2.0, 1.0),
    "Class_Guardian": (-2.0, -1.9),
    "Class_Brawler": (0.0, -1.9),
    "Class_Shadowblade": (2.0, -1.9),
    "Class_Longbowman": (-2.0, 1.7),
    "Class_BombMaster": (-1.0, 1.7),
    "Class_IceMage": (0.0, 1.7),
    "Class_FireMage": (1.0, 1.7),
    "Class_DarkMage": (2.0, 1.7),
    "Class_BaseWarrior": (-1.0, -1.9),
    "Class_BaseRogue": (1.0, -1.9),
    "Class_BaseArcher": (-2.0, 2.4),
    "Class_BaseMage": (0.0, 2.4),
}
WP_START_LOCAL = (-0.13, 0.05, 0.11)
ROOT_TRANSFORM_ID = "704938194719251401"
# Must be < Int64.MaxValue (9223372036854775807). SearchExtract uses 92006…; use 88006….
BASE_ID = 8800600000000100000
OLD_ID_PREFIX = "93006000000001"


def load_class_ids() -> list[str]:
    with CSV_PATH.open(encoding="utf-8-sig", newline="") as f:
        rows = list(csv.DictReader(f))
    out: list[str] = []
    seen: set[str] = set()
    for row in rows:
        cid = (row.get("ClassId") or "").strip()
        if not cid or cid in seen:
            continue
        seen.add(cid)
        out.append(cid)
    return out


def resolve_rel(class_id: str, fallback_index: int) -> tuple[float, float, int]:
    if class_id in KNOWN_REL:
        x, z = KNOWN_REL[class_id]
        return x, z, fallback_index
    col = fallback_index % 5
    row = fallback_index // 5
    fallback_index += 1
    return -2.0 + col * 1.0, 3.1 + row * 0.7, fallback_index


def mesh_renderer_yaml(rid: int, go_id: int) -> str:
    return f"""--- !u!23 &{rid}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 0
  m_CastShadows: 1
  m_ReceiveShadows: 1
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 2
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 3
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_AdditionalVertexStreams: {{fileID: 0}}
"""


def class_zone_yaml(
    class_id: str,
    order: int,
    rel_x: float,
    rel_z: float,
    zones_tf: int,
    go_id: int,
    tf_id: int,
    zone_mb: int,
    mf_id: int,
    mc_id: int,
    mr_id: int,
) -> str:
    return f"""--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tf_id}}}
  - component: {{fileID: {zone_mb}}}
  - component: {{fileID: {mf_id}}}
  - component: {{fileID: {mc_id}}}
  - component: {{fileID: {mr_id}}}
  m_Layer: 0
  m_Name: {class_id}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tf_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: {rel_x}, y: 0.05, z: {rel_z}}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {zones_tf}}}
  m_RootOrder: {order}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{zone_mb}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {ZONE_SCRIPT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  _classId: {class_id}
  _halfExtents: {{x: {HALF[0]}, y: {HALF[1]}}}
--- !u!33 &{mf_id}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Mesh: {{fileID: 0}}
--- !u!64 &{mc_id}
MeshCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Material: {{fileID: 0}}
  m_IsTrigger: 0
  m_Enabled: 1
  serializedVersion: 4
  m_Convex: 0
  m_CookingOptions: 30
  m_Mesh: {{fileID: 0}}
{mesh_renderer_yaml(mr_id, go_id)}"""


def build_block(class_ids: list[str]) -> tuple[str, int]:
    assert BASE_ID + 200 < (1 << 63) - 1
    zones_go = BASE_ID + 1
    zones_tf = BASE_ID + 2
    zones_mb = BASE_ID + 3
    next_id = BASE_ID + 10

    child_tfs: list[int] = []
    body_parts: list[str] = []
    fallback = 0
    for i, cid in enumerate(class_ids):
        rel_x, rel_z, fallback = resolve_rel(cid, fallback)
        go_id = next_id
        tf_id = next_id + 1
        zone_mb = next_id + 2
        mf_id = next_id + 3
        mc_id = next_id + 4
        mr_id = next_id + 5
        next_id += 10
        child_tfs.append(tf_id)
        body_parts.append(
            class_zone_yaml(
                cid, i, rel_x, rel_z, zones_tf, go_id, tf_id, zone_mb, mf_id, mc_id, mr_id
            )
        )

    children_yaml = "\n".join(f"  - {{fileID: {tf}}}" for tf in child_tfs)
    header = f"""--- !u!1 &{zones_go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {zones_tf}}}
  - component: {{fileID: {zones_mb}}}
  m_Layer: 0
  m_Name: FormationClassZones
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{zones_tf}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {zones_go}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: {WP_START_LOCAL[0]}, y: {WP_START_LOCAL[1]}, z: {WP_START_LOCAL[2]}}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{children_yaml}
  m_Father: {{fileID: {ROOT_TRANSFORM_ID}}}
  m_RootOrder: 5
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{zones_mb}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {zones_go}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {ROOT_SCRIPT}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
"""
    return header + "".join(body_parts), zones_tf


def strip_old_zones(text: str) -> str:
    # Remove root child refs to overflow / new zone transforms.
    text = re.sub(
        r"\n  - \{fileID: 9300600000000100002\}",
        "",
        text,
    )
    text = re.sub(
        r"\n  - \{fileID: 8800600000000100002\}",
        "",
        text,
    )

    # Drop any previously injected FormationClassZones blocks (old or new prefix).
    # Match from FormationClassZones GameObject through EOF if it was appended, or until next top-level that's not ours.
    # Safer: remove all YAML documents whose &id starts with 93006 or 88006.
    parts = re.split(r"(?=^--- !u!)", text, flags=re.M)
    kept: list[str] = []
    for part in parts:
        if not part:
            continue
        m = re.match(r"^--- !u!\d+ &(\d+)", part)
        if m:
            fid = m.group(1)
            if fid.startswith("93006") or fid.startswith("88006"):
                continue
        kept.append(part)
    text = "".join(kept)
    # Also remove orphaned FormationClassZones name if any leftover (shouldn't).
    return text


def patch_prefab(path: Path, class_ids: list[str]) -> None:
    text = path.read_text(encoding="utf-8")
    text = strip_old_zones(text)
    block, zones_tf = build_block(class_ids)

    pattern = re.compile(
        rf"(--- !u!4 &{ROOT_TRANSFORM_ID}\nTransform:.*?m_Children:\n)((?:  - \{{fileID: \d+\}}\n)+)(  m_Father:)",
        re.DOTALL,
    )
    match = pattern.search(text)
    if not match:
        raise RuntimeError(f"Could not find root Transform children in {path.name}")

    children_block = match.group(2)
    if f"{{fileID: {zones_tf}}}" not in children_block:
        children_block = children_block + f"  - {{fileID: {zones_tf}}}\n"
    text = text[: match.start()] + match.group(1) + children_block + match.group(3) + text[match.end() :]

    if not text.endswith("\n"):
        text += "\n"
    text += block

    # Sanity: no overflow IDs
    for m in re.finditer(r"&(\d{16,})", text):
        n = int(m.group(1))
        if n > (1 << 63) - 1:
            raise RuntimeError(f"{path.name} still has overflow fileID {n}")

    path.write_text(text, encoding="utf-8")
    print(f"OK fixed {len(class_ids)} zones → {path.name} (BASE={BASE_ID})")


def main() -> None:
    class_ids = load_class_ids()
    if not class_ids:
        raise SystemExit("No ClassIds loaded")
    for path in MAPS:
        if not path.exists():
            print(f"MISSING {path}")
            continue
        patch_prefab(path, class_ids)


if __name__ == "__main__":
    main()
