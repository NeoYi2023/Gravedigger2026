#!/usr/bin/env python3
"""Rebuild Ground_01..05 Prefabs: replace Cube GroundVisual with Isometric Tilemap + WalkSurface.

Keeps DigMapBounds / EngageZone / DefendSpawnPoints. Safe structural rewrite for Demo maps.
"""
from __future__ import annotations

import re
from pathlib import Path

MAPS_DIR = Path(r"E:\Work\Cursor\Gravedigger2026\Gravedigger2026\Gravedigger2026\Assets\Prefabs\Maps")

TILE_G1 = "cd3afce6972f0c243d9684873b8858fa"
TILE_TEST = "6dcbc7b01aa6449474f56ce044070616"
TILE_BLACK = "7ff226dcb8a8c42463aaea45edf8cf06"
SPR_G1 = "65f9597453b0dac395bd854cd25bc228"
SPR_TEST = "1e8af6c9e6f7735252b8526c6f4f6efb"
SPR_BLACK = "4f873d9fc00ecf5c5e58350c23f797ac"

PAINT_RADIUS = 5
Q90 = "{x: 0.7071068, y: 0, z: 0, w: 0.7071068}"

# Stable synthetic fileIDs for injected objects (unlikely to collide with existing).
ID_WALK_GO, ID_WALK_TF, ID_WALK_MF, ID_WALK_MR, ID_WALK_COL = (
    9100000000000000001,
    9100000000000000002,
    9100000000000000003,
    9100000000000000004,
    9100000000000000005,
)
ID_GRID_GO, ID_GRID_TF, ID_GRID = (
    9100000000000000010,
    9100000000000000011,
    9100000000000000012,
)
ID_TM_GO, ID_TM_TF, ID_TM, ID_TMR = (
    9100000000000000020,
    9100000000000000021,
    9100000000000000022,
    9100000000000000023,
)


def paint_pattern(variant: int):
    use_a = variant % 2 == 0
    fill_guid, alt_guid = (TILE_G1, TILE_TEST) if use_a else (TILE_TEST, TILE_G1)
    fill_spr, alt_spr = (SPR_G1, SPR_TEST) if use_a else (SPR_TEST, SPR_G1)
    tiles = []
    ref = [0, 0, 0]
    for y in range(-PAINT_RADIUS, PAINT_RADIUS + 1):
        for x in range(-PAINT_RADIUS, PAINT_RADIUS + 1):
            on_border = abs(x) == PAINT_RADIUS or abs(y) == PAINT_RADIUS
            if on_border:
                idx = 2
            elif variant >= 2 and ((x + y + variant) & 1) == 0:
                idx = 1
            else:
                idx = 0
            ref[idx] += 1
            tiles.append((x, y, idx))
    return {
        "tiles": tiles,
        "fill_guid": fill_guid,
        "alt_guid": alt_guid,
        "border_guid": TILE_BLACK,
        "fill_spr": fill_spr,
        "alt_spr": alt_spr,
        "border_spr": SPR_BLACK,
        "ref": ref,
        "count": len(tiles),
        "origin": -PAINT_RADIUS,
        "size": PAINT_RADIUS * 2 + 1,
    }


def build_visual_yaml(root_tf: str, paint: dict) -> str:
    tile_lines = []
    for x, y, idx in paint["tiles"]:
        tile_lines.append(
            f"""  - first: {{x: {x}, y: {y}, z: 0}}
    second:
      serializedVersion: 2
      m_TileIndex: {idx}
      m_TileSpriteIndex: {idx}
      m_TileMatrixIndex: 0
      m_TileColorIndex: 0
      m_TileObjectToInstantiateIndex: 65535
      dummyAlignment: 0
      m_AllTileFlags: 1073741825"""
        )
    tiles_yaml = "\n".join(tile_lines)
    r0, r1, r2 = paint["ref"]
    n = paint["count"]
    o = paint["origin"]
    s = paint["size"]
    return f"""--- !u!1 &{ID_WALK_GO}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {ID_WALK_TF}}}
  - component: {{fileID: {ID_WALK_MF}}}
  - component: {{fileID: {ID_WALK_MR}}}
  - component: {{fileID: {ID_WALK_COL}}}
  m_Layer: 0
  m_Name: WalkSurface
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{ID_WALK_TF}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ID_WALK_GO}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: -0.05, z: 0}}
  m_LocalScale: {{x: 10, y: 0.1, z: 10}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {root_tf}}}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!33 &{ID_WALK_MF}
MeshFilter:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ID_WALK_GO}}}
  m_Mesh: {{fileID: 10202, guid: 0000000000000000e000000000000000, type: 0}}
--- !u!23 &{ID_WALK_MR}
MeshRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ID_WALK_GO}}}
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
  - {{fileID: 10303, guid: 0000000000000000f000000000000000, type: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 1
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
--- !u!65 &{ID_WALK_COL}
BoxCollider:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ID_WALK_GO}}}
  m_Material: {{fileID: 0}}
  m_IsTrigger: 0
  m_Enabled: 1
  serializedVersion: 2
  m_Size: {{x: 1, y: 1, z: 1}}
  m_Center: {{x: 0, y: 0, z: 0}}
--- !u!1 &{ID_GRID_GO}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {ID_GRID_TF}}}
  - component: {{fileID: {ID_GRID}}}
  m_Layer: 0
  m_Name: GroundTilemap
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{ID_GRID_TF}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ID_GRID_GO}}}
  m_LocalRotation: {Q90}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: {ID_TM_TF}}}
  m_Father: {{fileID: {root_tf}}}
  m_RootOrder: 1
  m_LocalEulerAnglesHint: {{x: 90, y: 0, z: 0}}
--- !u!156049354 &{ID_GRID}
Grid:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ID_GRID_GO}}}
  m_Enabled: 1
  m_CellSize: {{x: 2, y: 1, z: 2}}
  m_CellGap: {{x: 0, y: 0, z: 0}}
  m_CellLayout: 2
  m_CellSwizzle: 0
--- !u!1 &{ID_TM_GO}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {ID_TM_TF}}}
  - component: {{fileID: {ID_TM}}}
  - component: {{fileID: {ID_TMR}}}
  m_Layer: 0
  m_Name: Tilemap
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{ID_TM_TF}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ID_TM_GO}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {ID_GRID_TF}}}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!1839735485 &{ID_TM}
Tilemap:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ID_TM_GO}}}
  m_Enabled: 1
  m_Tiles:
{tiles_yaml}
  m_AnimatedTiles: {{}}
  m_TileAssetArray:
  - m_RefCount: {r0}
    m_Data: {{fileID: 11400000, guid: {paint['fill_guid']}, type: 2}}
  - m_RefCount: {r1}
    m_Data: {{fileID: 11400000, guid: {paint['alt_guid']}, type: 2}}
  - m_RefCount: {r2}
    m_Data: {{fileID: 11400000, guid: {paint['border_guid']}, type: 2}}
  m_TileSpriteArray:
  - m_RefCount: {r0}
    m_Data: {{fileID: 21300000, guid: {paint['fill_spr']}, type: 3}}
  - m_RefCount: {r1}
    m_Data: {{fileID: 21300000, guid: {paint['alt_spr']}, type: 3}}
  - m_RefCount: {r2}
    m_Data: {{fileID: 21300000, guid: {paint['border_spr']}, type: 3}}
  m_TileMatrixArray:
  - m_RefCount: {n}
    m_Data:
      e00: 1
      e01: 0
      e02: 0
      e03: 0
      e10: 0
      e11: 1
      e12: 0
      e13: 0
      e20: 0
      e21: 0
      e22: 1
      e23: 0
      e30: 0
      e31: 0
      e32: 0
      e33: 1
  m_TileColorArray:
  - m_RefCount: {n}
    m_Data: {{r: 1, g: 1, b: 1, a: 1}}
  m_TileObjectToInstantiateArray: []
  m_AnimationFrameRate: 1
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_Origin: {{x: {o}, y: {o}, z: 0}}
  m_Size: {{x: {s}, y: {s}, z: 1}}
  m_TileAnchor: {{x: 0.5, y: 0.5, z: 0}}
  m_TileOrientation: 0
  m_TileOrientationMatrix:
    e00: 1
    e01: 0
    e02: 0
    e03: 0
    e10: 0
    e11: 1
    e12: 0
    e13: 0
    e20: 0
    e21: 0
    e22: 1
    e23: 0
    e30: 0
    e31: 0
    e32: 0
    e33: 1
--- !u!483693784 &{ID_TMR}
TilemapRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {ID_TM_GO}}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}
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
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_ChunkSize: {{x: 32, y: 32, z: 32}}
  m_ChunkCullingBounds: {{x: 0, y: 0, z: 0}}
  m_MaxChunkCount: 16
  m_MaxFrameAge: 16
  m_SortOrder: 3
  m_Mode: 0
  m_DetectChunkCullingBounds: 0
  m_MaskInteraction: 0
"""


def find_root_transform(text: str, map_id: str) -> str:
    m = re.search(
        rf"m_Component:\n  - component: \{{fileID: (\d+)\}}\n  - component: \{{fileID: \d+\}}\n  m_Layer: 0\n  m_Name: {map_id}\n",
        text,
    )
    if not m:
        raise RuntimeError(f"root transform not found for {map_id}")
    return m.group(1)


def find_ground_visual_transform(text: str) -> str | None:
    m = re.search(
        r"^--- !u!1 &\d+\n"
        r"GameObject:\n"
        r"(?:  .*\n)*?"
        r"  - component: \{fileID: (\d+)\}\n"
        r"(?:  - component: \{fileID: \d+\}\n)*"
        r"  m_Layer: 0\n"
        r"  m_Name: GroundVisual\n",
        text,
        re.M,
    )
    return m.group(1) if m else None


def strip_named_object(text: str, object_name: str) -> str:
    """Remove a named GameObject and all components that reference its GameObject fileID."""
    # Match only the GO whose immediate m_Name is object_name (do not span other GOs).
    m = re.search(
        rf"^--- !u!1 &(\d+)\n"
        rf"GameObject:\n"
        rf"(?:  .*\n)*?"
        rf"  m_Name: {re.escape(object_name)}\n",
        text,
        re.M,
    )
    if not m:
        return text
    go_id = m.group(1)
    parts = re.split(r"(?=^--- !u!)", text, flags=re.M)
    kept = []
    for part in parts:
        if not part.strip():
            continue
        if re.search(rf"^--- !u!1 &{go_id}\n", part, re.M):
            continue
        if re.search(rf"m_GameObject: \{{fileID: {go_id}\}}", part):
            continue
        kept.append(part)
    return "".join(kept)


def strip_ground_visual(text: str) -> str:
    return strip_named_object(text, "GroundVisual")


def update_root_children(text: str, root_tf: str, remove_tf: str | None) -> str:
    pattern = rf"(--- !u!4 &{root_tf}\nTransform:\n(?:.*\n)*?  m_Children:\n)((?:  - \{{fileID: \d+\}}\n)+)(  m_Father: \{{fileID: 0\}})"
    m = re.search(pattern, text)
    if not m:
        raise RuntimeError(f"root children block not found for transform {root_tf}")
    ids = re.findall(r"fileID: (\d+)", m.group(2))
    kept = [i for i in ids if i != remove_tf and i not in {str(ID_WALK_TF), str(ID_GRID_TF)}]
    new_children = (
        f"  - {{fileID: {ID_WALK_TF}}}\n"
        f"  - {{fileID: {ID_GRID_TF}}}\n"
        + "".join(f"  - {{fileID: {i}}}\n" for i in kept)
    )
    return text[: m.start(2)] + new_children + text[m.end(2) :]


def rewrite_map(path: Path, variant: int) -> None:
    map_id = path.stem
    text = path.read_text(encoding="utf-8").replace("\r\n", "\n").replace("\r", "\n")

    # Always purge legacy cube visual first (even if Tilemap already present).
    gv_tf = find_ground_visual_transform(text)
    text = strip_ground_visual(text)
    assert "m_Name: GroundVisual" not in text, f"{map_id}: failed to strip GroundVisual"

    if "m_Name: GroundTilemap" in text:
        root_tf = find_root_transform(text, map_id)
        text = update_root_children(text, root_tf, gv_tf)
        path.write_text(text, encoding="utf-8", newline="\n")
        print(f"{map_id}: purged GroundVisual only (Tilemap kept) gvTf={gv_tf}")
        return

    root_tf = find_root_transform(text, map_id)
    text = update_root_children(text, root_tf, gv_tf)

    paint = paint_pattern(variant)
    visual = build_visual_yaml(root_tf, paint)
    if text.startswith("%YAML"):
        text = re.sub(
            r"(^%YAML 1\.1\n%TAG.+\n)",
            r"\1" + visual,
            text,
            count=1,
            flags=re.M,
        )
    else:
        text = visual + text

    path.write_text(text, encoding="utf-8", newline="\n")
    print(
        f"{map_id}: ok rootTf={root_tf} tiles={paint['count']} "
        f"refs={paint['ref']} removedGvTf={gv_tf}"
    )


def main() -> None:
    for i in range(5):
        rewrite_map(MAPS_DIR / f"Ground_0{i+1}.prefab", i)


if __name__ == "__main__":
    main()
