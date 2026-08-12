import re
from pathlib import Path

MAPS = Path(r"E:\Work\Cursor\Gravedigger2026\Gravedigger2026\Gravedigger2026\Assets\Prefabs\Maps")
SCRIPT_GUID = "f06a11c1a55e46d2b8e3c4d5e6f70819"
ZONES = [
    ("Class_Warrior", -2.0, -1.2),
    ("Class_Knight", -1.0, -1.2),
    ("Class_Paladin", 0.0, -1.2),
    ("Class_Berserker", 1.0, -1.2),
    ("Class_Rogue", 2.0, -1.2),
    ("Class_Servants", 0.0, -0.2),
    ("Class_Archer", -2.0, 1.0),
    ("Class_Ranger", -1.0, 1.0),
    ("Class_Mage", 0.0, 1.0),
    ("Class_Warlock", 1.0, 1.0),
    ("Class_Priest", 2.0, 1.0),
]
HALF = (0.45, 0.35)


def fid(base, n):
    return base + n


def main():
    for map_i, name in enumerate(
        ["Ground_01", "Ground_02", "Ground_03", "Ground_04", "Ground_05"], start=1
    ):
        path = MAPS / f"{name}.prefab"
        text = path.read_text(encoding="utf-8")
        if "FormationClassZones" in text:
            print(f"skip {name}: already has FormationClassZones")
            continue

        m = re.search(
            r"--- !u!4 &(\d+)\nTransform:\n(?:.*\n)*?  m_Children:\n((?:  - \{fileID: \d+\}\n)*)  m_Father: \{fileID: 0\}",
            text,
        )
        if not m:
            print(f"FAIL {name}: root transform not found")
            continue
        root_tf = int(m.group(1))
        children_block = m.group(2)

        base = 9200600000000000000 + map_i * 100000
        root_go = fid(base, 1)
        root_tr = fid(base, 2)
        blocks = []
        zone_children = []
        for zi, (cid, rx, rz) in enumerate(ZONES):
            go_id = fid(base, 10 + zi * 3)
            tr_id = fid(base, 11 + zi * 3)
            mb_id = fid(base, 12 + zi * 3)
            zone_children.append(f"  - {{fileID: {tr_id}}}")
            blocks.append(
                f"""--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {tr_id}}}
  - component: {{fileID: {mb_id}}}
  m_Layer: 0
  m_Name: {cid}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{tr_id}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: {rx}, y: 0.05, z: {rz}}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {root_tr}}}
  m_RootOrder: {zi}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{mb_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  _classId: {cid}
  _halfExtents: {{x: {HALF[0]}, y: {HALF[1]}}}
"""
            )

        parent_block = f"""--- !u!1 &{root_go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {root_tr}}}
  m_Layer: 0
  m_Name: FormationClassZones
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{root_tr}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_go}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
{chr(10).join(zone_children)}
  m_Father: {{fileID: {root_tf}}}
  m_RootOrder: {children_block.count('fileID')}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
"""

        new_children = children_block + f"  - {{fileID: {root_tr}}}\n"
        text2 = text[: m.start(2)] + new_children + text[m.end(2) :]
        text2 = text2.rstrip() + "\n" + parent_block + "".join(blocks)
        path.write_text(text2, encoding="utf-8", newline="\n")
        print(f"patched {name}: +FormationClassZones ({len(ZONES)} zones)")


if __name__ == "__main__":
    main()
