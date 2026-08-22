#!/usr/bin/env python3
"""Rebuild FantasyTileset.prefab to current Tile/Sprite GUIDs; fix animated tiles."""
from __future__ import annotations

import os
import re
from pathlib import Path

ROOT = Path(r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\Gravedigger2026\Assets")
ENV = ROOT / "Art" / "Maps" / "Environment"
ANIM_SRC = ROOT / "SmallScaleInt" / "Fantasy kingdom Tileset" / "Animations" / "Animated Tiles"
TILES = ENV / "Tiles"
SPRITES = ENV / "Sprites"
ANIM_TILES = ENV / "Animated tiles"
PREFAB = ENV / "FantasyTileset.prefab"

GUID_RE = re.compile(r"(?m)^guid:\s*(\S+)")
SPRITE_ID_RE = re.compile(
    r"internalIDToNameTable:\s*\n\s*-\s*first:\s*\n\s*213:\s*(-?\d+)"
)
SPRITE_REF_RE = re.compile(
    r"(?m)^\s*m_Sprite:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]+),\s*type:\s*(\d+)\}"
)


def read_text(p: Path) -> str:
    return p.read_text(encoding="utf-8")


def meta_guid(meta: Path) -> str | None:
    m = GUID_RE.search(read_text(meta))
    return m.group(1) if m else None


def sprite_info(png_meta: Path) -> tuple[str, str] | None:
    raw = read_text(png_meta)
    g = GUID_RE.search(raw)
    if not g:
        return None
    f = SPRITE_ID_RE.search(raw)
    if f:
        return g.group(1), f.group(1)
    # Older single-sprite metas may lack internalIDToNameTable
    if "spriteMode: 1" in raw or "textureType: 8" in raw:
        return g.group(1), "21300000"
    return None


def collect_static_tiles() -> list[dict]:
    items = []
    for asset in sorted(TILES.glob("*.asset")):
        raw = read_text(asset)
        sm = SPRITE_REF_RE.search(raw)
        if not sm:
            continue
        fid, guid = sm.group(1), sm.group(2)
        tmeta = asset.with_suffix(".asset.meta")
        tguid = meta_guid(tmeta)
        if not tguid:
            continue
        items.append(
            {
                "name": asset.stem,
                "tile_guid": tguid,
                "sprite_file_id": fid,
                "sprite_guid": guid,
            }
        )
    return items


def build_sprite_guid_set() -> set[str]:
    s = set()
    for meta in SPRITES.glob("*.png.meta"):
        g = meta_guid(meta)
        if g:
            s.add(g)
    return s


def rebuild_prefab(tiles: list[dict]) -> None:
    cols = 50
    n = len(tiles)
    rows = (n + cols - 1) // cols
    origin_x, origin_y = 0, 0

    # Keep stable fileIDs from original prefab header structure
    go_root = "879004016973603870"
    tr_root = "6446791226200638889"
    grid_id = "2893716341905879073"
    go_layer = "1534443778029750641"
    tr_layer = "1286912993970383214"
    tilemap_id = "8680168539864770354"
    renderer_id = "6395343413827271580"

    lines: list[str] = []
    lines.append("%YAML 1.1")
    lines.append("%TAG !u! tag:unity3d.com,2011:")
    lines.append("--- !u!1 &" + go_root)
    lines.append("GameObject:")
    lines.append("  m_ObjectHideFlags: 0")
    lines.append("  m_CorrespondingSourceObject: {fileID: 0}")
    lines.append("  m_PrefabInstance: {fileID: 0}")
    lines.append("  m_PrefabAsset: {fileID: 0}")
    lines.append("  serializedVersion: 6")
    lines.append("  m_Component:")
    lines.append(f"  - component: {{fileID: {tr_root}}}")
    lines.append(f"  - component: {{fileID: {grid_id}}}")
    lines.append("  m_Layer: 0")
    lines.append("  m_Name: FantasyTileset")
    lines.append("  m_TagString: Untagged")
    lines.append("  m_Icon: {fileID: 0}")
    lines.append("  m_NavMeshLayer: 0")
    lines.append("  m_StaticEditorFlags: 0")
    lines.append("  m_IsActive: 1")
    lines.append("--- !u!4 &" + tr_root)
    lines.append("Transform:")
    lines.append("  m_ObjectHideFlags: 0")
    lines.append("  m_CorrespondingSourceObject: {fileID: 0}")
    lines.append("  m_PrefabInstance: {fileID: 0}")
    lines.append("  m_PrefabAsset: {fileID: 0}")
    lines.append(f"  m_GameObject: {{fileID: {go_root}}}")
    lines.append("  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}")
    lines.append("  m_LocalPosition: {x: 0, y: 0, z: 0}")
    lines.append("  m_LocalScale: {x: 1, y: 1, z: 1}")
    lines.append("  m_ConstrainProportionsScale: 0")
    lines.append("  m_Children:")
    lines.append(f"  - {{fileID: {tr_layer}}}")
    lines.append("  m_Father: {fileID: 0}")
    lines.append("  m_RootOrder: 0")
    lines.append("  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}")
    lines.append("--- !u!156049354 &" + grid_id)
    lines.append("Grid:")
    lines.append("  m_ObjectHideFlags: 0")
    lines.append("  m_CorrespondingSourceObject: {fileID: 0}")
    lines.append("  m_PrefabInstance: {fileID: 0}")
    lines.append("  m_PrefabAsset: {fileID: 0}")
    lines.append(f"  m_GameObject: {{fileID: {go_root}}}")
    lines.append("  m_Enabled: 1")
    lines.append("  m_CellSize: {x: 4, y: 2, z: 4}")
    lines.append("  m_CellGap: {x: 0, y: 0, z: 0}")
    lines.append("  m_CellLayout: 2")
    lines.append("  m_CellSwizzle: 0")
    lines.append("--- !u!1 &" + go_layer)
    lines.append("GameObject:")
    lines.append("  m_ObjectHideFlags: 0")
    lines.append("  m_CorrespondingSourceObject: {fileID: 0}")
    lines.append("  m_PrefabInstance: {fileID: 0}")
    lines.append("  m_PrefabAsset: {fileID: 0}")
    lines.append("  serializedVersion: 6")
    lines.append("  m_Component:")
    lines.append(f"  - component: {{fileID: {tr_layer}}}")
    lines.append(f"  - component: {{fileID: {tilemap_id}}}")
    lines.append(f"  - component: {{fileID: {renderer_id}}}")
    lines.append("  m_Layer: 0")
    lines.append("  m_Name: Layer1")
    lines.append("  m_TagString: Untagged")
    lines.append("  m_Icon: {fileID: 0}")
    lines.append("  m_NavMeshLayer: 0")
    lines.append("  m_StaticEditorFlags: 0")
    lines.append("  m_IsActive: 1")
    lines.append("--- !u!4 &" + tr_layer)
    lines.append("Transform:")
    lines.append("  m_ObjectHideFlags: 0")
    lines.append("  m_CorrespondingSourceObject: {fileID: 0}")
    lines.append("  m_PrefabInstance: {fileID: 0}")
    lines.append("  m_PrefabAsset: {fileID: 0}")
    lines.append(f"  m_GameObject: {{fileID: {go_layer}}}")
    lines.append("  m_LocalRotation: {x: -0, y: -0, z: -0, w: 1}")
    lines.append("  m_LocalPosition: {x: 0, y: 0, z: 0}")
    lines.append("  m_LocalScale: {x: 1, y: 1, z: 1}")
    lines.append("  m_ConstrainProportionsScale: 0")
    lines.append("  m_Children: []")
    lines.append(f"  m_Father: {{fileID: {tr_root}}}")
    lines.append("  m_RootOrder: 0")
    lines.append("  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}")
    lines.append("--- !u!1839735485 &" + tilemap_id)
    lines.append("Tilemap:")
    lines.append("  m_ObjectHideFlags: 0")
    lines.append("  m_CorrespondingSourceObject: {fileID: 0}")
    lines.append("  m_PrefabInstance: {fileID: 0}")
    lines.append("  m_PrefabAsset: {fileID: 0}")
    lines.append(f"  m_GameObject: {{fileID: {go_layer}}}")
    lines.append("  m_Enabled: 1")
    lines.append("  m_Tiles:")

    for i, t in enumerate(tiles):
        x = i % cols
        y = -(i // cols)
        lines.append(f"  - first: {{x: {x}, y: {y}, z: 0}}")
        lines.append("    second:")
        lines.append("      serializedVersion: 2")
        lines.append(f"      m_TileIndex: {i}")
        lines.append(f"      m_TileSpriteIndex: {i}")
        lines.append("      m_TileMatrixIndex: 0")
        lines.append("      m_TileColorIndex: 0")
        lines.append("      m_TileObjectToInstantiateIndex: 65535")
        lines.append("      dummyAlignment: 0")
        lines.append("      m_AllTileFlags: 1073741825")

    lines.append("  m_AnimatedTiles: {}")
    lines.append("  m_TileAssetArray:")
    for t in tiles:
        lines.append("  - m_RefCount: 1")
        lines.append(
            f"    m_Data: {{fileID: 11400000, guid: {t['tile_guid']}, type: 2}}"
        )

    lines.append("  m_TileSpriteArray:")
    for t in tiles:
        lines.append("  - m_RefCount: 1")
        lines.append(
            f"    m_Data: {{fileID: {t['sprite_file_id']}, guid: {t['sprite_guid']}, type: 3}}"
        )

    lines.append("  m_TileMatrixArray:")
    lines.append(f"  - m_RefCount: {n}")
    lines.append("    m_Data:")
    lines.append("      e00: 1")
    lines.append("      e01: 0")
    lines.append("      e02: 0")
    lines.append("      e03: 0")
    lines.append("      e10: 0")
    lines.append("      e11: 1")
    lines.append("      e12: 0")
    lines.append("      e13: 0")
    lines.append("      e20: 0")
    lines.append("      e21: 0")
    lines.append("      e22: 1")
    lines.append("      e23: 0")
    lines.append("      e30: 0")
    lines.append("      e31: 0")
    lines.append("      e32: 0")
    lines.append("      e33: 1")
    lines.append("  m_TileColorArray:")
    lines.append(f"  - m_RefCount: {n}")
    lines.append("    m_Data: {r: 1, g: 1, b: 1, a: 1}")
    lines.append("  m_TileObjectToInstantiateArray: []")
    lines.append("  m_AnimationFrameRate: 1")
    lines.append("  m_Color: {r: 1, g: 1, b: 1, a: 1}")
    lines.append(f"  m_Origin: {{x: {origin_x}, y: {-rows + 1}, z: 0}}")
    lines.append(f"  m_Size: {{x: {cols}, y: {rows}, z: 1}}")
    lines.append("  m_TileAnchor: {x: 0.5, y: 0.5, z: 0}")
    lines.append("  m_TileOrientation: 0")
    lines.append("  m_TileOrientationMatrix:")
    lines.append("    e00: 1")
    lines.append("    e01: 0")
    lines.append("    e02: 0")
    lines.append("    e03: 0")
    lines.append("    e10: 0")
    lines.append("    e11: 1")
    lines.append("    e12: 0")
    lines.append("    e13: 0")
    lines.append("    e20: 0")
    lines.append("    e21: 0")
    lines.append("    e22: 1")
    lines.append("    e23: 0")
    lines.append("    e30: 0")
    lines.append("    e31: 0")
    lines.append("    e32: 0")
    lines.append("    e33: 1")
    lines.append("--- !u!483693784 &" + renderer_id)
    lines.append("TilemapRenderer:")
    lines.append("  m_ObjectHideFlags: 0")
    lines.append("  m_CorrespondingSourceObject: {fileID: 0}")
    lines.append("  m_PrefabInstance: {fileID: 0}")
    lines.append("  m_PrefabAsset: {fileID: 0}")
    lines.append(f"  m_GameObject: {{fileID: {go_layer}}}")
    lines.append("  m_Enabled: 1")
    lines.append("  m_CastShadows: 0")
    lines.append("  m_ReceiveShadows: 0")
    lines.append("  m_DynamicOccludee: 0")
    lines.append("  m_StaticShadowCaster: 0")
    lines.append("  m_MotionVectors: 1")
    lines.append("  m_LightProbeUsage: 0")
    lines.append("  m_ReflectionProbeUsage: 0")
    lines.append("  m_RayTracingMode: 0")
    lines.append("  m_RayTraceProcedural: 0")
    lines.append("  m_RenderingLayerMask: 1")
    lines.append("  m_RendererPriority: 0")
    lines.append("  m_Materials:")
    lines.append("  - {fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}")
    lines.append("  m_StaticBatchInfo:")
    lines.append("    firstSubMesh: 0")
    lines.append("    subMeshCount: 0")
    lines.append("  m_StaticBatchRoot: {fileID: 0}")
    lines.append("  m_ProbeAnchor: {fileID: 0}")
    lines.append("  m_LightProbeVolumeOverride: {fileID: 0}")
    lines.append("  m_ScaleInLightmap: 1")
    lines.append("  m_ReceiveGI: 1")
    lines.append("  m_PreserveUVs: 0")
    lines.append("  m_IgnoreNormalsForChartDetection: 0")
    lines.append("  m_ImportantGI: 0")
    lines.append("  m_StitchLightmapSeams: 1")
    lines.append("  m_SelectedEditorRenderState: 0")
    lines.append("  m_MinimumChartSize: 4")
    lines.append("  m_AutoUVMaxDistance: 0.5")
    lines.append("  m_AutoUVMaxAngle: 89")
    lines.append("  m_LightmapParameters: {fileID: 0}")
    lines.append("  m_SortingLayerID: 0")
    lines.append("  m_SortingLayer: 0")
    lines.append("  m_SortingOrder: 0")
    lines.append("  m_ChunkSize: {x: 32, y: 32, z: 32}")
    lines.append("  m_ChunkCullingBounds: {x: 0, y: 0, z: 0}")
    lines.append("  m_MaxChunkCount: 16")
    lines.append("  m_MaxFrameAge: 16")
    lines.append("  m_SortOrder: 3")
    lines.append("  m_Mode: 0")
    lines.append("  m_DetectChunkCullingBounds: 0")
    lines.append("  m_MaskInteraction: 0")

    PREFAB.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Wrote prefab with {n} tiles ({cols}x{rows})")


def index_png_metas(folder: Path) -> dict[str, tuple[str, str]]:
    """basename -> (guid, fileId)"""
    out = {}
    for meta in folder.rglob("*.png.meta"):
        info = sprite_info(meta)
        if not info:
            continue
        out[meta.name[: -len(".png.meta")]] = info
    return out


def fix_animated_tiles() -> None:
    """Fix animated tile sprite fileIDs to match current TextureImporter internal IDs."""
    # Build guid -> fileId across Animations Animated Tiles + Environment Sprites
    guid_to_fid: dict[str, str] = {}
    for folder in (ANIM_SRC, SPRITES):
        if not folder.exists():
            continue
        for meta in folder.rglob("*.png.meta"):
            info = sprite_info(meta)
            if info:
                guid_to_fid[info[0]] = info[1]

    fixed_assets = 0
    fixed_refs = 0
    unresolved = 0

    for asset in sorted(ANIM_TILES.glob("*.asset")):
        raw = read_text(asset)

        def repl(m: re.Match[str]) -> str:
            nonlocal fixed_refs, unresolved
            fid, guid, typ = m.group(1), m.group(2), m.group(3)
            if guid not in guid_to_fid:
                unresolved += 1
                return m.group(0)
            new_fid = guid_to_fid[guid]
            if new_fid == fid:
                return m.group(0)
            fixed_refs += 1
            return f"{{fileID: {new_fid}, guid: {guid}, type: {typ}}}"

        new_raw = re.sub(
            r"\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]+),\s*type:\s*(\d+)\}",
            repl,
            raw,
        )
        # Do not touch script reference (guid 13b75c95...)
        if new_raw != raw:
            asset.write_text(new_raw, encoding="utf-8")
            fixed_assets += 1
            print(f"  updated {asset.name}")
        else:
            print(f"  unchanged {asset.name}")

    print(
        f"Animated tiles assets_updated={fixed_assets} refs_fixed={fixed_refs} unresolved={unresolved}"
    )


def main() -> None:
    sprite_guids = build_sprite_guid_set()
    all_tiles = collect_static_tiles()
    valid = [t for t in all_tiles if t["sprite_guid"] in sprite_guids]
    invalid = [t for t in all_tiles if t["sprite_guid"] not in sprite_guids]
    print(f"static tiles valid={len(valid)} invalid={len(invalid)}")
    if invalid:
        print("invalid sample:", ", ".join(t["name"] for t in invalid[:20]))
    rebuild_prefab(valid)
    print("Fixing animated tiles...")
    fix_animated_tiles()


if __name__ == "__main__":
    main()
