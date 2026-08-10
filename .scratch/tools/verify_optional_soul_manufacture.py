# -*- coding: utf-8 -*-
import csv
import pathlib
import sys

root = pathlib.Path(
    r"e:/Work/Cursor/Gravedigger2026/Gravedigger2026/Gravedigger2026/Assets/ConfigTables/Csv"
)
classes = list(csv.DictReader((root / "Manufacture_ClassConfig.csv").open(encoding="utf-8-sig")))
souls = list(csv.DictReader((root / "Manufacture_SoulConfig.csv").open(encoding="utf-8-sig")))
cs = next(r for r in classes if r["ClassId"] == "Class_Servants")
s0 = next(r for r in souls if r["SoulId"] == "Soul_00")
assert cs["ClassName"] == "仆从", cs
assert s0["ClassId"] == "Class_Servants"
assert s0["AttackMode"] == "Melee"
assert s0["SpiritCost"] in ("0", "0.0")

svc_path = pathlib.Path(
    r"e:/Work/Cursor/Gravedigger2026/Gravedigger2026/Gravedigger2026/Assets/Scripts/Core/UpgradeManufacture/ManufactureService.cs"
)
svc = svc_path.read_text(encoding="utf-8")
assert 'DefaultSoulId = "Soul_00"' in svc
assert 'NoSoulClassId = "Class_Servants"' in svc
assert "最低要求未满足：躯干 1 + 手臂 2 + 腿 2" in svc
assert "&& aggregate.Soul != null" not in svc
assert "TryApplyDefaultSoulIfMissing" in svc
assert "UsedDefaultSoul" in svc
assert "slot.Kind == ManufactureSlotKind.Soul" in svc
assert "ResolveInstanceClassId" in svc

# SPEC snippets
spec03 = pathlib.Path(
    r"e:/Work/Cursor/Gravedigger2026/Gravedigger2026/SPEC_03_GameRules.md"
).read_text(encoding="utf-8")
assert "Class_Servants" in spec03
assert "Soul_00" in spec03
assert "1 躯干 + 2 手臂 + 2 腿 + 1 灵魂" not in spec03

print("OK: config + ManufactureService + SPEC_03 invariants")
sys.exit(0)
