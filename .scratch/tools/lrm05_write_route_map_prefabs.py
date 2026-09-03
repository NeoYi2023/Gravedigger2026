# -*- coding: utf-8 -*-
"""LRM-05: write Demo LevelRouteMap_{LevelId} Prefabs (Unity YAML). Prefer Editor Ensure menu when Unity is available."""
from __future__ import annotations

import hashlib
from pathlib import Path

OUT = (
    Path(__file__).resolve().parents[2]
    / "Gravedigger2026"
    / "Assets"
    / "Prefabs"
    / "Level"
)
SPRITE_GUID = "69454701b569433fb4ef84523571c22b"
IMG_GUID = "fe87c0e1cc204ed48ad3b37840f39efc"
TXT_GUID = "5f7201a12d95ffc409449d95f23cf332"
FONT = "{fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}"
MAP_W = 1450.0
MAP_H = 4350.0
PIN = 56.0

LEVELS = {
    "Level_01": {
        "prefab_guid": "9d2c8a1f4b6e47c0a3f5e7d9b1c2a4e6",
        "pins": [
            ("Opt_L01_S1_Shop", 725, 280),
            ("Opt_L01_S2_Dig_A", 480, 720),
            ("Opt_L01_S2_Dig_B", 980, 720),
            ("Opt_L01_S3_AutoManufacture", 725, 1180),
            ("Opt_L01_S4_UpgradeManufacture", 725, 1680),
            ("Opt_L01_S5_PushMap", 520, 2200),
            ("Opt_SE_Demo_01", 980, 2200),
        ],
    },
    "Level_02": {
        "prefab_guid": "1a3b5c7d9e0f2468ace0bdf13579cafe",
        "pins": [
            ("Opt_L02_S1_Shop", 725, 280),
            ("Opt_L02_S2_Dig", 725, 720),
            ("Opt_L02_S3_AutoManufacture", 725, 1180),
            ("Opt_L02_S4_UpgradeManufacture", 725, 1680),
            ("Opt_L02_S5_PushMap", 725, 2200),
        ],
    },
    "Level_03": {
        "prefab_guid": "2468ace0bdf13579cafe1a3b5c7d9e0f",
        "pins": [
            ("Opt_L03_S1_Shop", 725, 280),
            ("Opt_L03_S2_Dig", 725, 720),
            ("Opt_L03_S3_AutoManufacture", 725, 1180),
            ("Opt_L03_S4_UpgradeManufacture", 725, 1680),
            ("Opt_L03_S5_PushMap", 725, 2200),
        ],
    },
}


def fid(key: str) -> int:
    n = int(hashlib.md5(key.encode("utf-8")).hexdigest()[:15], 16)
    return n if n > 100000 else n + 100000


def go_block(gid: int, name: str, comps: list[int]) -> str:
    lines = [
        f"--- !u!1 &{gid}",
        "GameObject:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  serializedVersion: 6",
        "  m_Component:",
    ]
    for c in comps:
        lines.append(f"  - component: {{fileID: {c}}}")
    lines += [
        "  m_Layer: 0",
        f"  m_Name: {name}",
        "  m_TagString: Untagged",
        "  m_Icon: {fileID: 0}",
        "  m_NavMeshLayer: 0",
        "  m_StaticEditorFlags: 0",
        "  m_IsActive: 1",
    ]
    return "\n".join(lines)


def rt_block(
    rid: int,
    gid: int,
    children: list[int],
    father: int,
    root_order: int,
    amin: tuple[float, float],
    amax: tuple[float, float],
    pos: tuple[float, float],
    size: tuple[float, float],
    pivot: tuple[float, float],
) -> str:
    lines = [
        f"--- !u!224 &{rid}",
        "RectTransform:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        f"  m_GameObject: {{fileID: {gid}}}",
        "  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}",
        "  m_LocalPosition: {x: 0, y: 0, z: 0}",
        "  m_LocalScale: {x: 1, y: 1, z: 1}",
        "  m_ConstrainProportionsScale: 0",
    ]
    if children:
        lines.append("  m_Children:")
        for c in children:
            lines.append(f"  - {{fileID: {c}}}")
    else:
        lines.append("  m_Children: []")
    father_s = "{fileID: 0}" if father == 0 else f"{{fileID: {father}}}"
    lines += [
        f"  m_Father: {father_s}",
        f"  m_RootOrder: {root_order}",
        "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}",
        f"  m_AnchorMin: {{x: {amin[0]}, y: {amin[1]}}}",
        f"  m_AnchorMax: {{x: {amax[0]}, y: {amax[1]}}}",
        f"  m_AnchoredPosition: {{x: {pos[0]}, y: {pos[1]}}}",
        f"  m_SizeDelta: {{x: {size[0]}, y: {size[1]}}}",
        f"  m_Pivot: {{x: {pivot[0]}, y: {pivot[1]}}}",
    ]
    return "\n".join(lines)


def cr_block(cid: int, gid: int) -> str:
    return "\n".join(
        [
            f"--- !u!222 &{cid}",
            "CanvasRenderer:",
            "  m_ObjectHideFlags: 0",
            "  m_CorrespondingSourceObject: {fileID: 0}",
            "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}",
            f"  m_GameObject: {{fileID: {gid}}}",
            "  m_CullTransparentMesh: 1",
        ]
    )


def image_block(iid: int, gid: int, color: tuple[float, float, float, float], sprite: bool, raycast: int) -> str:
    spr = f"{{fileID: 21300000, guid: {SPRITE_GUID}, type: 3}}" if sprite else "{fileID: 0}"
    r, g, b, a = color
    return "\n".join(
        [
            f"--- !u!114 &{iid}",
            "MonoBehaviour:",
            "  m_ObjectHideFlags: 0",
            "  m_CorrespondingSourceObject: {fileID: 0}",
            "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}",
            f"  m_GameObject: {{fileID: {gid}}}",
            "  m_Enabled: 1",
            "  m_EditorHideFlags: 0",
            f"  m_Script: {{fileID: 11500000, guid: {IMG_GUID}, type: 3}}",
            "  m_Name: ",
            "  m_EditorClassIdentifier: ",
            "  m_Material: {fileID: 0}",
            f"  m_Color: {{r: {r}, g: {g}, b: {b}, a: {a}}}",
            f"  m_RaycastTarget: {raycast}",
            "  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}",
            "  m_Maskable: 1",
            "  m_OnCullStateChanged:",
            "    m_PersistentCalls:",
            "      m_Calls: []",
            f"  m_Sprite: {spr}",
            "  m_Type: 0",
            "  m_PreserveAspect: 0",
            "  m_FillCenter: 1",
            "  m_FillMethod: 4",
            "  m_FillAmount: 1",
            "  m_FillClockwise: 1",
            "  m_FillOrigin: 0",
            "  m_UseSpriteMesh: 0",
            "  m_PixelsPerUnitMultiplier: 1",
        ]
    )


def text_block(tid: int, gid: int, value: str) -> str:
    return "\n".join(
        [
            f"--- !u!114 &{tid}",
            "MonoBehaviour:",
            "  m_ObjectHideFlags: 0",
            "  m_CorrespondingSourceObject: {fileID: 0}",
            "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}",
            f"  m_GameObject: {{fileID: {gid}}}",
            "  m_Enabled: 1",
            "  m_EditorHideFlags: 0",
            f"  m_Script: {{fileID: 11500000, guid: {TXT_GUID}, type: 3}}",
            "  m_Name: ",
            "  m_EditorClassIdentifier: ",
            "  m_Material: {fileID: 0}",
            "  m_Color: {r: 1, g: 1, b: 1, a: 1}",
            "  m_RaycastTarget: 0",
            "  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}",
            "  m_Maskable: 1",
            "  m_OnCullStateChanged:",
            "    m_PersistentCalls:",
            "      m_Calls: []",
            "  m_FontData:",
            f"    m_Font: {FONT}",
            "    m_FontSize: 11",
            "    m_FontStyle: 0",
            "    m_BestFit: 1",
            "    m_MinSize: 7",
            "    m_MaxSize: 11",
            "    m_Alignment: 4",
            "    m_AlignByGeometry: 0",
            "    m_RichText: 1",
            "    m_HorizontalOverflow: 0",
            "    m_VerticalOverflow: 1",
            "    m_LineSpacing: 1",
            f"  m_Text: {value}",
        ]
    )


def write_level(level_id: str, info: dict) -> None:
    root_name = f"LevelRouteMap_{level_id}"
    prefix = f"lrm.{level_id}"
    root_go = fid(prefix + ".root.go")
    root_rt = fid(prefix + ".root.rt")
    bg_go = fid(prefix + ".bg.go")
    bg_rt = fid(prefix + ".bg.rt")
    bg_cr = fid(prefix + ".bg.cr")
    bg_img = fid(prefix + ".bg.img")

    pin_ids = []
    for name, x, y in info["pins"]:
        p = prefix + ".pin." + name
        pin_ids.append(
            {
                "name": name,
                "x": x,
                "y": y,
                "go": fid(p + ".go"),
                "rt": fid(p + ".rt"),
                "cr": fid(p + ".cr"),
                "img": fid(p + ".img"),
                "lgo": fid(p + ".lgo"),
                "lrt": fid(p + ".lrt"),
                "lcr": fid(p + ".lcr"),
                "ltxt": fid(p + ".ltxt"),
            }
        )

    child_rts = [bg_rt] + [p["rt"] for p in pin_ids]
    parts = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:"]
    parts.append(go_block(root_go, root_name, [root_rt]))
    parts.append(
        rt_block(root_rt, root_go, child_rts, 0, 0, (0, 0), (0, 0), (0, 0), (MAP_W, MAP_H), (0, 0))
    )
    parts.append(go_block(bg_go, "Background", [bg_rt, bg_cr, bg_img]))
    parts.append(rt_block(bg_rt, bg_go, [], root_rt, 0, (0, 0), (1, 1), (0, 0), (0, 0), (0.5, 0.5)))
    parts.append(cr_block(bg_cr, bg_go))
    parts.append(image_block(bg_img, bg_go, (1, 1, 1, 1), True, 0))

    for i, p in enumerate(pin_ids):
        parts.append(go_block(p["go"], p["name"], [p["rt"], p["cr"], p["img"]]))
        parts.append(
            rt_block(
                p["rt"],
                p["go"],
                [p["lrt"]],
                root_rt,
                i + 1,
                (0, 0),
                (0, 0),
                (p["x"], p["y"]),
                (PIN, PIN),
                (0.5, 0.5),
            )
        )
        parts.append(cr_block(p["cr"], p["go"]))
        parts.append(image_block(p["img"], p["go"], (0.18, 0.72, 0.82, 0.88), False, 1))
        parts.append(go_block(p["lgo"], "Label", [p["lrt"], p["lcr"], p["ltxt"]]))
        parts.append(
            rt_block(p["lrt"], p["lgo"], [], p["rt"], 0, (0, 0), (1, 1), (0, 0), (-4, -4), (0.5, 0.5))
        )
        parts.append(cr_block(p["lcr"], p["lgo"]))
        parts.append(text_block(p["ltxt"], p["lgo"], p["name"]))

    OUT.mkdir(parents=True, exist_ok=True)
    prefab_path = OUT / f"{root_name}.prefab"
    prefab_path.write_text("\n".join(parts) + "\n", encoding="utf-8")
    (OUT / f"{root_name}.prefab.meta").write_text(
        "fileFormatVersion: 2\n"
        f"guid: {info['prefab_guid']}\n"
        "PrefabImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf-8",
    )
    print(f"wrote {prefab_path} pins={len(pin_ids)}")


def main() -> None:
    for level_id, info in LEVELS.items():
        write_level(level_id, info)


if __name__ == "__main__":
    main()
