from pathlib import Path
from PIL import Image
import math
import re

ROOT = Path(r"E:\Work\Cursor\Gravedigger2026\Gravedigger2026\Gravedigger2026")
PREFABS = ROOT / "Assets/Prefabs/Dig"
ART = ROOT / "Assets/Art/Dig/Graves"
SCRIPT_GUID = "c4e8f2a19d7b4e6a8f3c5d1e0b9a8765"
PPU = 100.0
ALPHA = 0.15
STEP = 2
MAX_VERTS = 12


def cross(o, a, b):
    return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])


def convex_hull(pts):
    pts = sorted(set((round(x, 6), round(y, 6)) for x, y in pts))
    if len(pts) <= 2:
        return pts
    lower = []
    for p in pts:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    upper = []
    for p in reversed(pts):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    return lower[:-1] + upper[:-1]


def simplify_radial(hull, max_verts):
    if len(hull) <= max_verts:
        return hull
    cx = sum(p[0] for p in hull) / len(hull)
    cy = sum(p[1] for p in hull) / len(hull)
    indexed = sorted(
        ((math.atan2(p[1] - cy, p[0] - cx), p) for p in hull), key=lambda t: t[0]
    )
    out = []
    for i in range(max_verts):
        target = -math.pi + (i / max_verts) * 2 * math.pi
        best = min(
            indexed,
            key=lambda t: abs(((t[0] - target + math.pi) % (2 * math.pi)) - math.pi),
        )
        p = best[1]
        if not out or (out[-1][0] - p[0]) ** 2 + (out[-1][1] - p[1]) ** 2 > 1e-8:
            out.append(p)
    return out if len(out) >= 3 else hull


def outline_from_png(path):
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    px = im.load()
    pts = []
    pivot_x = (w - 1) * 0.5
    pivot_y = (h - 1) * 0.5
    for y in range(0, h, STEP):
        for x in range(0, w, STEP):
            if px[x, y][3] / 255.0 < ALPHA:
                continue
            edge = False
            for oy in (-1, 0, 1):
                for ox in (-1, 0, 1):
                    if ox == 0 and oy == 0:
                        continue
                    nx, ny = x + ox, y + oy
                    if (
                        nx < 0
                        or ny < 0
                        or nx >= w
                        or ny >= h
                        or px[nx, ny][3] / 255.0 < ALPHA
                    ):
                        edge = True
                        break
                if edge:
                    break
            if not edge:
                continue
            sx = (x - pivot_x) / PPU
            sy = (y - pivot_y) / PPU
            # Sprite Euler(-90,0,180) → root XZ = (-sx, -sy)
            pts.append((-sx, -sy))
    return pts


def main():
    for qi in range(1, 11):
        q = f"Q{qi}"
        prefab = PREFABS / f"Grave_{q}.prefab"
        text = prefab.read_text(encoding="utf-8")
        if SCRIPT_GUID in text:
            print(f"  skip existing {q}")
            continue
        m = re.search(r"--- !u!1 &(\d+)\nGameObject:", text)
        if not m:
            print("no go", q)
            continue
        go_id = m.group(1)
        hit_id = go_id[:-1] + "8" if go_id.endswith("1") else go_id + "8"

        png = ART / f"Grave_{q}" / f"Grave_{q}.png"
        verts = []
        radius = 0.55
        if png.exists():
            pts = outline_from_png(png)
            hull = convex_hull(pts)
            verts = simplify_radial(hull, MAX_VERTS)
            if verts:
                radius = max(0.05, max(math.hypot(x, y) for x, y in verts))
            print(f"{q}: verts={len(verts)} r={radius:.3f} from png")
        else:
            print(f"{q}: no png — empty hull (runtime circle fallback)")

        pat = re.compile(
            r"(m_Name: Grave_"
            + re.escape(q)
            + r"\n(?:  .*\n)*?  m_Component:\n(?:  - component: \{fileID: \d+\}\n){3})",
        )
        text2, n = pat.subn(
            lambda mm: mm.group(1) + f"  - component: {{fileID: {hit_id}}}\n",
            text,
            count=1,
        )
        if not n:
            print("WARN inject component failed", q)
            continue
        text = text2

        if verts:
            verts_block = "\n".join(
                f"  - {{x: {x:.6f}, y: {y:.6f}}}" for x, y in verts
            )
            local_yaml = f"  _localXZ:\n{verts_block}"
        else:
            local_yaml = "  _localXZ: []"

        block = f"""--- !u!114 &{hit_id}
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
{local_yaml}
  _boundingRadius: {radius:.6f}
"""
        first = text.find("--- !u!1 &")
        second = text.find("--- !u!1 &", first + 10)
        if second > 0:
            text = text[:second] + block + text[second:]
        else:
            text = text + "\n" + block
        prefab.write_text(text, encoding="utf-8")
        print(f"  wrote DigHitShape on {q}")

    print("done")


if __name__ == "__main__":
    main()
