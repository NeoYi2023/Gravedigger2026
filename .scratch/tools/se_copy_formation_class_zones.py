#!/usr/bin/env python3
"""Copy FormationClassZones subtree from Ground_01 into SearchExtract_Demo_01.

Unity Editor menu is preferred when available; this offline graft unblocks
Prepare one-click deploy (SPEC_03 §3.19 / D-074) without rewriting PushMap.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
MAPS = ROOT / "Gravedigger2026" / "Assets" / "Prefabs" / "Maps"
SRC = MAPS / "Ground_01.prefab"
DST = MAPS / "SearchExtract_Demo_01.prefab"
ZONES_ROOT_GO = "9200600000000100001"
ZONES_ROOT_TF = "9200600000000100002"
DST_ROOT_TF = "704938194719251401"


def parse_blocks(text: str) -> list[tuple[str, str, str]]:
    """Return list of (file_id, header_line, full_block) for each --- !u! object."""
    parts = re.split(r"(?=^--- !u!\d+ &)", text, flags=re.M)
    out: list[tuple[str, str, str]] = []
    for part in parts:
        if not part.startswith("--- !u!"):
            continue
        m = re.match(r"--- !u!\d+ &(\d+)", part)
        if not m:
            continue
        header = part.split("\n", 1)[0]
        out.append((m.group(1), header, part if part.endswith("\n") else part + "\n"))
    return out


def collect_subtree_ids(blocks: list[tuple[str, str, str]], root_go: str) -> set[str]:
    go_components: dict[str, list[str]] = {}
    transform_go: dict[str, str] = {}
    transform_children: dict[str, list[str]] = {}

    for fid, header, body in blocks:
        if header.startswith("--- !u!1 &"):
            go_components[fid] = re.findall(r"- component: \{fileID: (\d+)\}", body)
        elif header.startswith("--- !u!4 &"):
            gm = re.search(r"m_GameObject: \{fileID: (\d+)\}", body)
            if gm:
                transform_go[fid] = gm.group(1)
            cm = re.search(r"m_Children:(.*?)m_Father:", body, re.S)
            transform_children[fid] = (
                re.findall(r"fileID: (\d+)", cm.group(1)) if cm else []
            )

    go_to_tf = {go_id: tf_id for tf_id, go_id in transform_go.items()}

    keep: set[str] = set()
    queue = [root_go]
    while queue:
        go = queue.pop()
        if go in keep:
            continue
        keep.add(go)
        for comp in go_components.get(go, []):
            keep.add(comp)
        tf = go_to_tf.get(go)
        if not tf:
            continue
        keep.add(tf)
        for child_tf in transform_children.get(tf, []):
            keep.add(child_tf)
            child_go = transform_go.get(child_tf)
            if child_go and child_go not in keep:
                queue.append(child_go)

    return keep


def ensure_child_link(dst_text: str, root_tf: str, child_tf: str) -> str:
    pattern = (
        rf"(--- !u!4 &{root_tf}\nTransform:.*?m_Children:\n)"
        rf"(.*?)"
        rf"(  m_Father:)"
    )
    m = re.search(pattern, dst_text, re.S)
    if not m:
        raise SystemExit(f"Root Transform {root_tf} not found in destination prefab")
    children_block = m.group(2)
    if f"{{fileID: {child_tf}}}" in children_block:
        return dst_text
    insert = children_block.rstrip("\n") + f"\n  - {{fileID: {child_tf}}}\n"
    return dst_text[: m.start(2)] + insert + dst_text[m.end(2) :]


def main() -> int:
    if not SRC.is_file() or not DST.is_file():
        print(f"Missing prefab: SRC={SRC.exists()} DST={DST.exists()}", file=sys.stderr)
        return 1

    src_text = SRC.read_text(encoding="utf-8")
    dst_text = DST.read_text(encoding="utf-8")
    if "m_Name: FormationClassZones" in dst_text:
        print("SearchExtract_Demo_01 already has FormationClassZones — skip.")
        return 0

    blocks = parse_blocks(src_text)
    keep_ids = collect_subtree_ids(blocks, ZONES_ROOT_GO)
    if ZONES_ROOT_GO not in keep_ids or ZONES_ROOT_TF not in keep_ids:
        print("Failed to collect FormationClassZones subtree", file=sys.stderr)
        return 1

    # Preserve Ground_01 block order
    grafted = []
    for fid, _, body in blocks:
        if fid in keep_ids:
            grafted.append(body.rstrip("\n") + "\n")

    dst_text = ensure_child_link(dst_text, DST_ROOT_TF, ZONES_ROOT_TF)
    if not dst_text.endswith("\n"):
        dst_text += "\n"
    dst_text += "".join(grafted)
    DST.write_text(dst_text, encoding="utf-8")
    print(
        f"Grafted FormationClassZones ({len(keep_ids)} objects) "
        f"from Ground_01 → SearchExtract_Demo_01"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
