import csv
import collections
from pathlib import Path

p = Path(
    r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\Gravedigger2026\Assets\ConfigTables\Mode2\Csv\Manufacture_BodyPartConfig.csv"
)
rows = list(csv.DictReader(p.open(encoding="utf-8-sig")))
by_id = {r["BodyPartId"]: r for r in rows}
stock = collections.Counter(
    {
        "BP_Head_Human": 1,
        "BP_Torso_Human": 1,
        "BP_Arm_Elf": 1,
        "BP_Arm_Human": 1,
        "BP_Leg_Elf": 1,
        "BP_Leg_Dwarf": 1,
    }
)


def approx(a, b):
    return abs(float(a) - float(b)) <= 1.0001


primaries = [
    by_id[i]
    for i, c in stock.items()
    if c > 0 and by_id[i]["BodySlot"] == "Arm" and by_id[i]["IsPrimaryHand"] == "1"
]
primaries.sort(key=lambda r: float(r["BodyLevel"]), reverse=True)
assert primaries, "no primary"
primary = primaries[0]
assert primary["ClassRestrict"].strip()
anchor = float(primary["BodyLevel"])
stock[primary["BodyPartId"]] -= 1
secs = []
for i, c in list(stock.items()):
    r = by_id[i]
    if c < 1 or r["BodySlot"] != "Arm" or r["IsPrimaryHand"] != "0":
        continue
    if approx(r["BodyLevel"], anchor):
        secs.append(r)
assert secs, "no secondary"
print("OK primary", primary["BodyPartId"], "secondary", [s["BodyPartId"] for s in secs])
lp = Path(
    r"e:\Work\Cursor\Gravedigger2026\Gravedigger2026\Gravedigger2026\Assets\ConfigTables\Mode2\Csv\Level_LevelOperationConfig.csv"
)
print("Level_01 stages:")
for row in csv.DictReader(lp.open(encoding="utf-8-sig")):
    if row["LevelId"] == "Level_01":
        print(row)
