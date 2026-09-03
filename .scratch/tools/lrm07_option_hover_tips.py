# -*- coding: utf-8 -*-
"""LRM-07: inject OptionHoverTips under Box in LevelRouteSelectRoot.prefab."""
from __future__ import annotations

import hashlib
import re
from pathlib import Path

PREFAB = (
    Path(__file__).resolve().parents[2]
    / "Gravedigger2026"
    / "Assets"
    / "Prefabs"
    / "Level"
    / "LevelRouteSelectRoot.prefab"
)
IMG_GUID = "fe87c0e1cc204ed48ad3b37840f39efc"
TXT_GUID = "5f7201a12d95ffc409449d95f23cf332"
FONT = "{fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}"
BOX_RT = "4049123271203536760"
CLOSE_RT = "8240037456082055713"


def fid(key: str) -> int:
    n = int(hashlib.md5(key.encode("utf-8")).hexdigest()[:15], 16)
    return n if n > 100000 else n + 100000


def go_block(gid: int, name: str, comps: list[int], active: int = 1) -> str:
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
        f"  m_IsActive: {active}",
    ]
    return "\n".join(lines)


def rect_block(
    rid: int,
    gid: int,
    father: str,
    children: list[int],
    anchor_min: str,
    anchor_max: str,
    pivot: str,
    pos: str,
    size: str,
    root_order: int = 0,
) -> str:
    child_lines = "\n".join(f"  - {{fileID: {c}}}" for c in children) if children else "  []"
    if children:
        child_block = "  m_Children:\n" + "\n".join(f"  - {{fileID: {c}}}" for c in children)
    else:
        child_block = "  m_Children: []"
    return "\n".join(
        [
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
            child_block,
            f"  m_Father: {{fileID: {father}}}",
            f"  m_RootOrder: {root_order}",
            "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}",
            f"  m_AnchorMin: {anchor_min}",
            f"  m_AnchorMax: {anchor_max}",
            f"  m_AnchoredPosition: {pos}",
            f"  m_SizeDelta: {size}",
            f"  m_Pivot: {pivot}",
        ]
    )


def canvas_renderer(cid: int, gid: int) -> str:
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


def image_block(iid: int, gid: int, color: str) -> str:
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
            f"  m_Color: {color}",
            "  m_RaycastTarget: 0",
            "  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}",
            "  m_Maskable: 1",
            "  m_OnCullStateChanged:",
            "    m_PersistentCalls:",
            "      m_Calls: []",
            "  m_Sprite: {fileID: 0}",
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


def text_block(tid: int, gid: int, sample: str, font_size: int) -> str:
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
            f"  m_Text: {sample}",
            f"  m_FontData:",
            "    m_Font: " + FONT,
            f"    m_FontSize: {font_size}",
            "    m_FontStyle: 0",
            "    m_BestFit: 0",
            "    m_MinSize: 10",
            "    m_MaxSize: 40",
            "    m_Alignment: 0",
            "    m_AlignByGeometry: 0",
            "    m_RichText: 1",
            "    m_HorizontalOverflow: 0",
            "    m_VerticalOverflow: 1",
            "    m_LineSpacing: 1",
            "  m_Color: {r: 1, g: 1, b: 1, a: 1}",
        ]
    )


def make_text_child(key: str, name: str, sample: str, font_size: int, y: float, h: float, father: str):
    gid = fid(f"{key}.go")
    rid = fid(f"{key}.rt")
    cid = fid(f"{key}.cr")
    tid = fid(f"{key}.txt")
    blocks = [
        go_block(gid, name, [rid, cid, tid]),
        rect_block(
            rid,
            gid,
            father,
            [],
            "{x: 0.5, y: 1}",
            "{x: 0.5, y: 1}",
            "{x: 0.5, y: 1}",
            f"{{x: 0, y: {y}}}",
            f"{{x: 300, y: {h}}}",
        ),
        canvas_renderer(cid, gid),
        text_block(tid, gid, sample, font_size),
    ]
    return rid, tid, "\n".join(blocks)


def main() -> None:
    text = PREFAB.read_text(encoding="utf-8")
    if "m_Name: OptionHoverTips" in text:
        print("OptionHoverTips already present; skip.")
        return

    tips_go = fid("tips.go")
    tips_rt = fid("tips.rt")
    tips_cr = fid("tips.cr")
    tips_img = fid("tips.img")

    type_rt, type_tid, type_yaml = make_text_child("tips.type", "Type", "Type", 14, -10, 22, str(tips_rt))
    title_rt, title_tid, title_yaml = make_text_child("tips.title", "Title", "Title", 18, -36, 26, str(tips_rt))
    desc_rt, desc_tid, desc_yaml = make_text_child(
        "tips.desc", "Description", "Description", 14, -78, 48, str(tips_rt)
    )
    reward_rt, reward_tid, reward_yaml = make_text_child(
        "tips.reward", "Reward", "\u5956\u52b1\uff1a\u2014", 13, -132, 22, str(tips_rt)
    )

    tips_yaml = "\n".join(
        [
            go_block(tips_go, "OptionHoverTips", [tips_rt, tips_cr, tips_img], active=0),
            rect_block(
                tips_rt,
                tips_go,
                BOX_RT,
                [type_rt, title_rt, desc_rt, reward_rt],
                "{x: 0.5, y: 0.5}",
                "{x: 0.5, y: 0.5}",
                "{x: 0.5, y: 0}",
                "{x: 0, y: 0}",
                "{x: 320, y: 160}",
                root_order=5,
            ),
            canvas_renderer(tips_cr, tips_go),
            image_block(tips_img, tips_go, "{r: 0.08, g: 0.09, b: 0.12, a: 0.94}"),
            type_yaml,
            title_yaml,
            desc_yaml,
            reward_yaml,
        ]
    )

    # Add tips RT as Box child (before CloseButton if present).
    box_children_pat = re.compile(
        rf"(--- !u!224 &{BOX_RT}\nRectTransform:.*?m_Children:\n)((?:  - \{{fileID: \d+\}}\n)+)",
        re.S,
    )

    def inject_child(m: re.Match) -> str:
        header, kids = m.group(1), m.group(2)
        lines = [ln.strip() for ln in kids.strip().splitlines() if ln.strip()]
        tip_line = f"  - {{fileID: {tips_rt}}}"
        close_line = f"  - {{fileID: {CLOSE_RT}}}"
        if tip_line in lines:
            return m.group(0)
        if close_line in lines:
            lines = [ln for ln in lines if ln != close_line]
            lines.append(tip_line)
            lines.append(close_line)
        else:
            lines.append(tip_line)
        return header + "\n".join(lines) + "\n"

    new_text, n = box_children_pat.subn(inject_child, text, count=1)
    if n != 1:
        raise SystemExit("Failed to inject OptionHoverTips under Box children")

    # Wire LevelRouteSelectView serialized fields.
    view_pat = re.compile(
        r"(  _mapScroll: \{fileID: \d+\})\n(--- !u!1 &)",
    )
    wire = (
        f"  _mapScroll: {{fileID: 63531467001434980}}\n"
        f"  _optionHoverTipsRoot: {{fileID: {tips_go}}}\n"
        f"  _optionTipsType: {{fileID: {type_tid}}}\n"
        f"  _optionTipsTitle: {{fileID: {title_tid}}}\n"
        f"  _optionTipsDescription: {{fileID: {desc_tid}}}\n"
        f"  _optionTipsReward: {{fileID: {reward_tid}}}\n"
        f"--- !u!1 &"
    )
    # Keep whatever _mapScroll fileID already is
    def wire_view(m: re.Match) -> str:
        return f"{m.group(1)}\n  _optionHoverTipsRoot: {{fileID: {tips_go}}}\n  _optionTipsType: {{fileID: {type_tid}}}\n  _optionTipsTitle: {{fileID: {title_tid}}}\n  _optionTipsDescription: {{fileID: {desc_tid}}}\n  _optionTipsReward: {{fileID: {reward_tid}}}\n{m.group(2)}"

    new_text, n2 = view_pat.subn(wire_view, new_text, count=1)
    if n2 != 1:
        raise SystemExit("Failed to wire LevelRouteSelectView tips fields")

    # Append tips YAML before EOF
    new_text = new_text.rstrip() + "\n" + tips_yaml + "\n"
    PREFAB.write_text(new_text, encoding="utf-8")
    print(f"Injected OptionHoverTips into {PREFAB}")


if __name__ == "__main__":
    main()
