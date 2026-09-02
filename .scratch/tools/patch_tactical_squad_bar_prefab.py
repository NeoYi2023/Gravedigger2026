# -*- coding: utf-8 -*-
from pathlib import Path
import re

ROOT = Path(r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\Gravedigger2026\Assets\Prefabs\Formation")

SQUAD_BLOCK = """
--- !u!1 &9100000000000000001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 9100000000000000002}
  - component: {fileID: 9100000000000000003}
  m_Layer: 0
  m_Name: TacticalFormationSquadBarRoot
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 0
--- !u!224 &9100000000000000002
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9100000000000000001}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 9100000000000000005}
  m_Father: {fileID: __CANVAS_RT__}
  m_RootOrder: 99
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0.5}
  m_AnchorMax: {x: 0, y: 0.5}
  m_AnchoredPosition: {x: 24, y: 0}
  m_SizeDelta: {x: 64, y: 320}
  m_Pivot: {x: 0, y: 0.5}
--- !u!114 &9100000000000000003
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9100000000000000001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b2c3d4e5f6789012345678abcdef0123, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  _buttonColumn: {fileID: 9100000000000000005}
  _root: {fileID: 9100000000000000001}
--- !u!1 &9100000000000000004
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 9100000000000000005}
  - component: {fileID: 9100000000000000006}
  - component: {fileID: 9100000000000000007}
  m_Layer: 0
  m_Name: ButtonColumn
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &9100000000000000005
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9100000000000000004}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 9100000000000000002}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 1}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 1}
--- !u!114 &9100000000000000006
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9100000000000000004}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 59f8146938fff651cb3e388106139789, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Padding:
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: 1
  m_Spacing: 8
  m_ChildForceExpandWidth: 0
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 0
  m_ChildControlHeight: 0
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
--- !u!114 &9100000000000000007
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 9100000000000000004}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 3245ec927659c4140ac4f8d17403cc18, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  m_HorizontalFit: 0
  m_VerticalFit: 2
"""


def find_canvas_rt(text: str) -> str:
    m = re.search(
        r"m_Component:\n  - component: \{fileID: (\d+)\}\n(?:  - component: \{fileID: \d+\}\n)*  m_Layer: 0\n  m_Name: FormationCanvas\n",
        text,
    )
    if not m:
        raise RuntimeError("FormationCanvas not found")
    return m.group(1)


def patch(path: Path) -> None:
    text = path.read_text(encoding="utf-8")
    if "TacticalFormationSquadBarRoot" in text:
        print(f"skip already patched: {path.name}")
        return

    canvas_rt = find_canvas_rt(text)
    marker = f"--- !u!224 &{canvas_rt}\n"
    idx = text.find(marker)
    if idx < 0:
        raise RuntimeError(f"canvas rt block not found: {canvas_rt}")

    children_start = text.find("  m_Children:", idx)
    children_end = text.find("  m_Father:", children_start)
    if children_start < 0 or children_end < 0:
        raise RuntimeError("children block not found")

    insert_line = "  - {fileID: 9100000000000000002}\n"
    if "9100000000000000002" not in text[children_start:children_end]:
        text = text[:children_end] + insert_line + text[children_end:]

    if "_tacticalSquadBar:" not in text:
        text, n = re.subn(
            r"(_bondHud: \{fileID: \d+\})\n",
            r"\1\n  _tacticalSquadBar: {fileID: 9100000000000000003}\n",
            text,
            count=1,
        )
        if n != 1:
            raise RuntimeError("_bondHud wire failed")

    block = SQUAD_BLOCK.replace("__CANVAS_RT__", canvas_rt).lstrip("\n")
    if not text.endswith("\n"):
        text += "\n"
    text += block
    path.write_text(text, encoding="utf-8")
    print(f"patched {path.name} canvas_rt={canvas_rt}")


def main() -> None:
    patch(ROOT / "FormationEditorRoot.prefab")
    patch(ROOT / "FormationEditorRoot_Mode2.prefab")


if __name__ == "__main__":
    main()
