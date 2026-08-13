"""Offline Ensure of D-057 FormationClassZones on Ground_01..05.

Mirrors DefendAssetBuilder.EnsureFormationClassZones (create-missing only).
Unity Editor currently holds the project lock, so this writes Prefab YAML.
"""
from __future__ import annotations

import hashlib
import re
from pathlib import Path

MAPS_DIR = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "Prefabs" / "Maps"

NEW_ZONES = [
    # class_id, rel_x, rel_z  (second front z=-1.9 / second back z=+1.7)
    ("Class_Guardian", -2.0, -1.9),
    ("Class_Brawler", 0.0, -1.9),
    ("Class_Shadowblade", 2.0, -1.9),
    ("Class_Longbowman", -2.0, 1.7),
    ("Class_BombMaster", -1.0, 1.7),
    ("Class_IceMage", 0.0, 1.7),
    ("Class_FireMage", 1.0, 1.7),
    ("Class_DarkMage", 2.0, 1.7),
]

SCRIPT_GUID = "f06a11c1a55e46d2b8e3c4d5e6f70819"
ROT_Y25 = "{x: -0, y: 0.21643962, z: -0, w: 0.976296}"

GO_RE = re.compile(r"^--- !u!1 &(\d+)\s*$", re.M)
TRANSFORM_RE = re.compile(r"^--- !u!4 &(\d+)\s*$", re.M)
MB_RE = re.compile(r"^--- !u!114 &(\d+)\s*$", re.M)


def fid(seed: str) -> int:
    n = int(hashlib.md5(seed.encode("utf-8")).hexdigest()[:15], 16)
    return n if n != 0 else 1


def split_docs(text: str) -> list[tuple[str, str]]:
    parts = re.split(r"(?=^--- !u!)", text, flags=re.M)
    header = parts[0]
    docs = parts[1:]
    return header, docs


def doc_fileid(doc: str) -> str | None:
    m = re.match(r"^--- !u!(?:\d+) &(\d+)", doc)
    return m.group(1) if m else None


def field(doc: str, name: str) -> str | None:
    m = re.search(rf"^[ \t]*{re.escape(name)}: (.*)$", doc, re.M)
    return m.group(1).strip() if m else None


def parse_half(doc: str) -> tuple[float, float] | None:
    m = re.search(r"_halfExtents: \{x: ([-\d.]+), y: ([-\d.]+)\}", doc)
    if not m:
        return None
    return float(m.group(1)), float(m.group(2))


def zone_block(go_id: int, tr_id: int, mb_id: int, class_id: str, rel_x: float, rel_z: float,
               father_id: str, root_order: int, half_x: float, half_y: float) -> str:
    def fmt_num(v: float) -> str:
        if abs(v - round(v)) < 1e-9:
            return str(int(round(v)))
        text = f"{v:.2f}".rstrip("0").rstrip(".")
        return text if text else "0"

    x_s = fmt_num(rel_x)
    z_s = fmt_num(rel_z)
    hx_s = fmt_num(half_x)
    hy_s = fmt_num(half_y)
    return (
        f"--- !u!1 &{go_id}\n"
        "GameObject:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  serializedVersion: 6\n"
        "  m_Component:\n"
        f"  - component: {{fileID: {tr_id}}}\n"
        f"  - component: {{fileID: {mb_id}}}\n"
        "  m_Layer: 0\n"
        f"  m_Name: {class_id}\n"
        "  m_TagString: Untagged\n"
        "  m_Icon: {fileID: 0}\n"
        "  m_NavMeshLayer: 0\n"
        "  m_StaticEditorFlags: 0\n"
        "  m_IsActive: 1\n"
        f"--- !u!4 &{tr_id}\n"
        "Transform:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        f"  m_GameObject: {{fileID: {go_id}}}\n"
        f"  m_LocalRotation: {ROT_Y25}\n"
        f"  m_LocalPosition: {{x: {x_s}, y: 0.05, z: {z_s}}}\n"
        "  m_LocalScale: {x: 1, y: 1, z: 1}\n"
        "  m_ConstrainProportionsScale: 0\n"
        "  m_Children: []\n"
        f"  m_Father: {{fileID: {father_id}}}\n"
        f"  m_RootOrder: {root_order}\n"
        "  m_LocalEulerAnglesHint: {x: 0, y: 25, z: 0}\n"
        f"--- !u!114 &{mb_id}\n"
        "MonoBehaviour:\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        f"  m_GameObject: {{fileID: {go_id}}}\n"
        "  m_Enabled: 1\n"
        "  m_EditorHideFlags: 0\n"
        f"  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}\n"
        "  m_Name: \n"
        "  m_EditorClassIdentifier: \n"
        f"  _classId: {class_id}\n"
        f"  _halfExtents: {{x: {hx_s}, y: {hy_s}}}\n"
    )


def add_child_to_transform(doc: str, child_id: int) -> str:
    # Insert before m_Father after m_Children list.
    if re.search(rf"- {{fileID: {child_id}}}", doc):
        return doc
    m = re.search(r"(m_Children:\n)((?:  - \{fileID: \d+\}\n)*)(  m_Father:)", doc)
    if not m:
        raise RuntimeError("cannot find m_Children on FormationClassZones transform")
    children = m.group(2)
    new_line = f"  - {{fileID: {child_id}}}\n"
    return doc[: m.start(2)] + children + new_line + doc[m.start(3) :]


def process_prefab(path: Path) -> str:
    text = path.read_text(encoding="utf-8")
    header, docs = split_docs(text)
    by_id = {}
    for d in docs:
        i = doc_fileid(d)
        if i:
            by_id[i] = d

    # Collect Paladin MonoBehaviours and pick HalfExtents=2.5 if present.
    paladin_mbs = []
    for d in docs:
        if field(d, "_classId") == "Class_Paladin":
            paladin_mbs.append(d)
    if not paladin_mbs:
        raise RuntimeError(f"{path.name}: no Class_Paladin zone")

    def mb_score(mb: str) -> tuple:
        half = parse_half(mb) or (0.0, 0.0)
        return (half[0], half[1])

    paladin_mb = max(paladin_mbs, key=mb_score)
    paladin_go = field(paladin_mb, "m_GameObject")
    paladin_go = paladin_go.split("fileID:")[1].strip(" }") if paladin_go and "fileID:" in paladin_go else None
    if not paladin_go:
        # MonoBehaviour always has m_GameObject
        m = re.search(r"m_GameObject: \{fileID: (\d+)\}", paladin_mb)
        paladin_go = m.group(1)

    paladin_tr = None
    for d in docs:
        if d.startswith("--- !u!4 &") and re.search(rf"m_GameObject: \{{fileID: {paladin_go}\}}", d):
            paladin_tr = d
            break
    if paladin_tr is None:
        raise RuntimeError(f"{path.name}: Paladin transform missing")

    father_m = re.search(r"m_Father: \{fileID: (\d+)\}", paladin_tr)
    father_id = father_m.group(1)
    father_doc = by_id[father_id]
    half = parse_half(paladin_mb) or (0.45, 0.35)

    existing_names = set()
    for d in docs:
        name = field(d, "m_Name")
        if name and name.startswith("Class_"):
            # only count children of chosen father
            if d.startswith("--- !u!1 &"):
                go_id = doc_fileid(d)
                for t in docs:
                    if t.startswith("--- !u!4 &") and re.search(rf"m_GameObject: \{{fileID: {go_id}\}}", t):
                        fm = re.search(r"m_Father: \{fileID: (\d+)\}", t)
                        if fm and fm.group(1) == father_id:
                            existing_names.add(name)
                        break

    used_ids = set(by_id.keys())
    new_docs = []
    child_ids = []
    start_order = father_doc.count("- {fileID:")
    added = []
    for i, (class_id, rel_x, rel_z) in enumerate(NEW_ZONES):
        if class_id in existing_names:
            continue
        go_id = fid(f"{path.name}:{class_id}:go")
        tr_id = fid(f"{path.name}:{class_id}:tr")
        mb_id = fid(f"{path.name}:{class_id}:mb")
        while str(go_id) in used_ids:
            go_id += 1
        while str(tr_id) in used_ids or tr_id == go_id:
            tr_id += 1
        while str(mb_id) in used_ids or mb_id in (go_id, tr_id):
            mb_id += 1
        used_ids.update((str(go_id), str(tr_id), str(mb_id)))
        root_order = start_order + len(added)
        new_docs.append(
            zone_block(go_id, tr_id, mb_id, class_id, rel_x, rel_z, father_id, root_order, half[0], half[1])
        )
        child_ids.append(tr_id)
        added.append(class_id)

    if not added:
        return f"{path.name}: already has 8 new zones"

    # update father children
    new_father = father_doc
    for cid in child_ids:
        new_father = add_child_to_transform(new_father, cid)

    out_docs = []
    replaced = False
    for d in docs:
        if doc_fileid(d) == father_id and not replaced:
            out_docs.append(new_father)
            replaced = True
        else:
            out_docs.append(d)
    out_docs.extend(new_docs)
    path.write_text(header + "".join(out_docs), encoding="utf-8", newline="\n")
    return f"{path.name}: added {', '.join(added)} half={half} father={father_id}"


def main() -> None:
    for i in range(1, 6):
        path = MAPS_DIR / f"Ground_0{i}.prefab"
        print(process_prefab(path))


if __name__ == "__main__":
    main()
