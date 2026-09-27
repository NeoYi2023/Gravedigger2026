# -*- coding: utf-8 -*-
"""Insert the UI-035 group-card shell into both formation editor prefabs."""
from pathlib import Path

ROOT = Path(r"f:\CursorGame_Git\Gravedigger2026\Gravedigger2026\Assets\Prefabs\Formation")
PREFABS = [
    ROOT / "FormationEditorRoot.prefab",
    ROOT / "FormationEditorRoot_Mode2.prefab",
]
VIEW_GUID = "a7c4e1b29d0f4e6a8b3c5d7e9f102346"
CANVAS_CHILD = "  - {fileID: 9200000000000000002}\n"
SQUAD_CHILD = "  - {fileID: 9100000000000000002}\n"
FIELD = "  _groupCardBar: {fileID: 9200000000000000003}\n"
SQUAD_FIELD = "  _tacticalSquadBar: {fileID: 9100000000000000003}\n"


def image_block(file_id, go_id, r, g, b, a, raycast, preserve=0):
    return f"""--- !u!114 &{file_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {{r: {r}, g: {g}, b: {b}, a: {a}}}
  m_RaycastTarget: {raycast}
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {{fileID: 0}}
  m_Type: 0
  m_PreserveAspect: {preserve}
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
"""


def canvas_renderer(file_id, go_id):
    return f"""--- !u!222 &{file_id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_CullTransparentMesh: 1
"""


def rect(file_id, go_id, father, children, name_unused, amin, amax, pos, size, pivot, root_order):
    child_lines = "\n".join(f"  - {{fileID: {c}}}" for c in children)
    if child_lines:
        child_lines = "\n" + child_lines
    return f"""--- !u!224 &{file_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:{child_lines if child_lines else " []"}
  m_Father: {{fileID: {father}}}
  m_RootOrder: {root_order}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {amin[0]}, y: {amin[1]}}}
  m_AnchorMax: {{x: {amax[0]}, y: {amax[1]}}}
  m_AnchoredPosition: {{x: {pos[0]}, y: {pos[1]}}}
  m_SizeDelta: {{x: {size[0]}, y: {size[1]}}}
  m_Pivot: {{x: {pivot[0]}, y: {pivot[1]}}}
"""


def go(file_id, name, components, active=1):
    comps = "\n".join(f"  - component: {{fileID: {c}}}" for c in components)
    return f"""--- !u!1 &{file_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comps}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: {active}
"""


def text_block(file_id, go_id, text, size, align):
    return f"""--- !u!114 &{file_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 5f7201a12d95ffc409449d95f23cf332, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 0
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {{fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}}
    m_FontSize: {size}
    m_FontStyle: 0
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: {align}
    m_AlignByGeometry: 0
    m_RichText: 0
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 1
    m_LineSpacing: 1
  m_Text: "{text}"
"""


def button_block(file_id, go_id, target, transition):
    return f"""--- !u!114 &{file_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 4e29b1a8efbd4b44bb3f3716e73f07ff, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {{fileID: 0}}
    m_SelectOnDown: {{fileID: 0}}
    m_SelectOnLeft: {{fileID: 0}}
    m_SelectOnRight: {{fileID: 0}}
  m_Transition: {transition}
  m_Colors:
    m_NormalColor: {{r: 1, g: 1, b: 1, a: 1}}
    m_HighlightedColor: {{r: 0.96, g: 0.96, b: 0.96, a: 1}}
    m_PressedColor: {{r: 0.78, g: 0.78, b: 0.78, a: 1}}
    m_SelectedColor: {{r: 0.96, g: 0.96, b: 0.96, a: 1}}
    m_DisabledColor: {{r: 0.78, g: 0.78, b: 0.78, a: 0.5}}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {{fileID: 0}}
    m_PressedSprite: {{fileID: 0}}
    m_SelectedSprite: {{fileID: 0}}
    m_DisabledSprite: {{fileID: 0}}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {{fileID: {target}}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
"""


def build_block(canvas_rt):
    parts = []
    parts.append(go("9200000000000000001", "TacticalFormationGroupCardRoot",
                    ["9200000000000000002", "9200000000000000003"], active=0))
    parts.append(rect(
        "9200000000000000002", "9200000000000000001", canvas_rt,
        ["9200000000000000012", "9200000000000000042"],
        "", (0.5, 1), (0.5, 1), (0, -8), (714, 128), (0.5, 1), 99))
    parts.append(f"""--- !u!114 &9200000000000000003
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 9200000000000000001}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {VIEW_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  _viewport: {{fileID: 9200000000000000022}}
  _content: {{fileID: 9200000000000000032}}
  _scroll: {{fileID: 9200000000000000013}}
  _cardTemplate: {{fileID: 9200000000000000041}}
  _root: {{fileID: 9200000000000000001}}
""")
    parts.append(go("9200000000000000011", "Scroll",
                    ["9200000000000000012", "9200000000000000013",
                     "9200000000000000014", "9200000000000000015"]))
    parts.append(rect(
        "9200000000000000012", "9200000000000000011", "9200000000000000002",
        ["9200000000000000022"], "", (0, 0), (1, 1), (0, 0), (0, 0), (0.5, 0.5), 0))
    parts.append(f"""--- !u!114 &9200000000000000013
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 9200000000000000011}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 1aa08ab6e0800fa44ae55d278d1423e3, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Content: {{fileID: 9200000000000000032}}
  m_Horizontal: 0
  m_Vertical: 0
  m_MovementType: 2
  m_Elasticity: 0.1
  m_Inertia: 1
  m_DecelerationRate: 0.135
  m_ScrollSensitivity: 24
  m_Viewport: {{fileID: 9200000000000000022}}
  m_HorizontalScrollbar: {{fileID: 0}}
  m_VerticalScrollbar: {{fileID: 0}}
  m_HorizontalScrollbarVisibility: 2
  m_VerticalScrollbarVisibility: 2
  m_HorizontalScrollbarSpacing: 0
  m_VerticalScrollbarSpacing: 0
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []
""")
    parts.append(canvas_renderer("9200000000000000014", "9200000000000000011"))
    parts.append(image_block("9200000000000000015", "9200000000000000011", 0.1, 0.12, 0.16, 0.55, 0))

    parts.append(go("9200000000000000021", "Viewport",
                    ["9200000000000000022", "9200000000000000023",
                     "9200000000000000024", "9200000000000000025"]))
    parts.append(rect(
        "9200000000000000022", "9200000000000000021", "9200000000000000012",
        ["9200000000000000032"], "", (0, 0), (1, 1), (0, 0), (0, 0), (0, 1), 0))
    parts.append(canvas_renderer("9200000000000000023", "9200000000000000021"))
    parts.append(image_block("9200000000000000024", "9200000000000000021", 1, 1, 1, 0.01, 0))
    parts.append(f"""--- !u!114 &9200000000000000025
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 9200000000000000021}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 3312d7739989d2b4e91e6319e9a96d76, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Padding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Softness: {{x: 0, y: 0}}
""")

    parts.append(go("9200000000000000031", "Content",
                    ["9200000000000000032", "9200000000000000033", "9200000000000000034"]))
    parts.append(rect(
        "9200000000000000032", "9200000000000000031", "9200000000000000022",
        [], "", (0, 0.5), (0, 0.5), (0, 0), (0, 128), (0, 0.5), 0))
    parts.append(f"""--- !u!114 &9200000000000000033
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 9200000000000000031}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 30649d3a9faa99c48a7b1166b86bf2a0, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Padding:
    m_Left: 0
    m_Right: 0
    m_Top: 0
    m_Bottom: 0
  m_ChildAlignment: 3
  m_Spacing: 6
  m_ChildForceExpandWidth: 0
  m_ChildForceExpandHeight: 0
  m_ChildControlWidth: 0
  m_ChildControlHeight: 0
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0
--- !u!114 &9200000000000000034
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 9200000000000000031}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 3245ec927659c4140ac4f8d17403cc18, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_HorizontalFit: 2
  m_VerticalFit: 0
""")

    parts.append(go("9200000000000000041", "CardTemplate",
                    ["9200000000000000042", "9200000000000000043", "9200000000000000044",
                     "9200000000000000045", "9200000000000000046"], active=0))
    parts.append(rect(
        "9200000000000000042", "9200000000000000041", "9200000000000000002",
        ["9200000000000000052", "9200000000000000062", "9200000000000000072",
         "9200000000000000082", "9200000000000000092"],
        "", (0.5, 0.5), (0.5, 0.5), (0, 0), (84, 128), (0.5, 0.5), 1))
    parts.append(canvas_renderer("9200000000000000043", "9200000000000000041"))
    parts.append(image_block("9200000000000000044", "9200000000000000041", 0.18, 0.22, 0.3, 0.95, 1))
    parts.append(button_block("9200000000000000045", "9200000000000000041", "9200000000000000044", 0))
    parts.append(f"""--- !u!114 &9200000000000000046
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 9200000000000000041}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 306cc8c2b49d7114eaa3623786fc2126, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_IgnoreLayout: 0
  m_MinWidth: 84
  m_MinHeight: 128
  m_PreferredWidth: 84
  m_PreferredHeight: 128
  m_FlexibleWidth: -1
  m_FlexibleHeight: -1
  m_LayoutPriority: 1
""")

    parts.append(go("9200000000000000051", "Icon",
                    ["9200000000000000052", "9200000000000000053", "9200000000000000054"]))
    parts.append(rect(
        "9200000000000000052", "9200000000000000051", "9200000000000000042",
        [], "", (0.5, 1), (0.5, 1), (0, -4), (48, 48), (0.5, 1), 0))
    parts.append(canvas_renderer("9200000000000000053", "9200000000000000051"))
    parts.append(image_block("9200000000000000054", "9200000000000000051", 1, 1, 1, 1, 0, preserve=1))

    parts.append(go("9200000000000000061", "Name",
                    ["9200000000000000062", "9200000000000000063", "9200000000000000064"]))
    parts.append(rect(
        "9200000000000000062", "9200000000000000061", "9200000000000000042",
        [], "", (0.5, 1), (0.5, 1), (0, -54), (78, 18), (0.5, 1), 1))
    parts.append(canvas_renderer("9200000000000000063", "9200000000000000061"))
    parts.append(text_block("9200000000000000064", "9200000000000000061", "", 12, 4))

    parts.append(go("9200000000000000071", "Level",
                    ["9200000000000000072", "9200000000000000073", "9200000000000000074"]))
    parts.append(rect(
        "9200000000000000072", "9200000000000000071", "9200000000000000042",
        [], "", (0.5, 1), (0.5, 1), (0, -72), (78, 16), (0.5, 1), 2))
    parts.append(canvas_renderer("9200000000000000073", "9200000000000000071"))
    parts.append(text_block("9200000000000000074", "9200000000000000071", "Lv1", 11, 4))

    parts.append(go("9200000000000000081", "Count",
                    ["9200000000000000082", "9200000000000000083", "9200000000000000084"]))
    parts.append(rect(
        "9200000000000000082", "9200000000000000081", "9200000000000000042",
        [], "", (0.5, 1), (0.5, 1), (0, -88), (78, 16), (0.5, 1), 3))
    parts.append(canvas_renderer("9200000000000000083", "9200000000000000081"))
    parts.append(text_block("9200000000000000084", "9200000000000000081", "0\\u4EBA", 11, 4))

    parts.append(go("9200000000000000091", "Disband",
                    ["9200000000000000092", "9200000000000000093", "9200000000000000094",
                     "9200000000000000095"], active=0))
    parts.append(rect(
        "9200000000000000092", "9200000000000000091", "9200000000000000042",
        ["9200000000000000102"], "", (0.5, 0), (0.5, 0), (0, 4), (74, 22), (0.5, 0), 4))
    parts.append(canvas_renderer("9200000000000000093", "9200000000000000091"))
    parts.append(image_block("9200000000000000094", "9200000000000000091", 0.55, 0.28, 0.24, 0.98, 1))
    parts.append(button_block("9200000000000000095", "9200000000000000091", "9200000000000000094", 1))

    parts.append(go("9200000000000000101", "Label",
                    ["9200000000000000102", "9200000000000000103", "9200000000000000104"]))
    parts.append(rect(
        "9200000000000000102", "9200000000000000101", "9200000000000000092",
        [], "", (0, 0), (1, 1), (0, 0), (0, 0), (0.5, 0.5), 0))
    parts.append(canvas_renderer("9200000000000000103", "9200000000000000101"))
    parts.append(text_block("9200000000000000104", "9200000000000000101", "\\u89E3\\u6563", 12, 4))
    return "\n".join(parts)


def find_canvas_rt(text):
    marker = "  m_Name: FormationCanvas\n"
    idx = text.find(marker)
    if idx < 0:
        raise RuntimeError("FormationCanvas not found")
    head = text.rfind("--- !u!1 &", 0, idx)
    block = text[head:idx]
    line = [ln for ln in block.splitlines() if ln.strip().startswith("- component:")][0]
    return line.split("fileID: ")[1].split("}")[0]


def patch(path: Path):
    text = path.read_text(encoding="utf-8")
    if "TacticalFormationGroupCardRoot" in text:
        print(f"skip {path.name}")
        return
    if SQUAD_CHILD not in text or SQUAD_FIELD not in text:
        raise RuntimeError(f"squad bar anchor missing in {path.name}")
    canvas_rt = find_canvas_rt(text)
    text = text.replace(SQUAD_CHILD, SQUAD_CHILD + CANVAS_CHILD, 1)
    text = text.replace(SQUAD_FIELD, SQUAD_FIELD + FIELD, 1)
    if not text.endswith("\n"):
        text += "\n"
    text += build_block(canvas_rt)
    if not text.endswith("\n"):
        text += "\n"
    path.write_text(text, encoding="utf-8", newline="\n")
    print(f"patched {path.name} canvas={canvas_rt}")


if __name__ == "__main__":
    for prefab in PREFABS:
        patch(prefab)
