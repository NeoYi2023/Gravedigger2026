# -*- coding: utf-8 -*-
"""Wire Grave_Q4..Q20 Prefab sprites + catalog. Run from repo root."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PREFABS = ROOT / "Gravedigger2026" / "Assets" / "Prefabs" / "Dig"
ART = ROOT / "Gravedigger2026" / "Assets" / "Art" / "Dig" / "Graves"
CATALOG = ROOT / "Gravedigger2026" / "Assets" / "Settings" / "Dig" / "DigPrefabCatalog.asset"

SPRITE_GUIDS = {
    1: "f8956667088391c42b565e51da03590c",
    2: "ca686531d10119a4bb798a8d973d4bcb",
    3: "5517f370ba49d64478d7db6d366da1b5",
    4: "414216f8eee30f2458369b0a8e6fb982",
    5: "30eb75b80b72750459b4271bad9105af",
    6: "28a03894d31674f46b0e1cb01478a64c",
    7: "b4754c680c1a03d428a57036a712bea5",
    8: "36b325106c54703498f49f97f36dfdc3",
    9: "7384123a2c9837b4a9d297ec7ac140eb",
    10: "e66d23fd987fa664b95ccd761b7dde60",
    11: "9ed3d629d19818d4fa24059fce08002b",
    12: "975b55ae802599f4d9a579f61d5343ae",
    13: "61d7d9f2994d9314c91b1bb954212223",
    14: "d23cc9de730773148a79537b4b1c7e66",
    15: "0bd90063de9112a48bc125a6922c7176",
    16: "ac5109505a9e8784d97a610d4c2d4ca1",
    17: "8f7db956ac7be0745a38b2beec55021e",
    18: "99599a3190d257c418fc77e042ec39a3",
    19: "6a7ab045e3db29345b44edf28895dafe",
    20: "003e5b8fbd3247c40bb8b6573db36c12",
}

PREFAB_META_GUIDS = {
    1: "643e16af11f99e64bbf956ec54476d22",
    2: "34d75d9ab48225e498a8cc86902f4244",
    3: "16afab489a6a664439905ba1f4f5c4a5",
    4: "d218f15c78116834aa52f0a6ca38d5fc",
    5: "357387e537b561b43b5c683b3e654c7c",
    6: "714e33bb916950844977da42312d9578",
    7: "0333f1fb411a06a4fa7e1bb4365e072e",
    8: "e625a18b6f1c2ec4689eb4cc0b50f955",
    9: "6bb1da158e191484a9eeb1dc6cc794ce",
    10: "31b9dff5e5bbc7049957e08a87e33014",
    11: "8c4a1d2e9f6b4703a5e8c1d0b7f39214",
    12: "1b7e3c90a4d64f2c8e5a9f0b6d214873",
    13: "5f2a8c1d6e4b4930b7c9e0f3a185d246",
    14: "9d0e4b7a2c185f36e8a1d4c0b7f52963",
    15: "2e8c5a1f7d4b6039c0e6a9b3d185f472",
    16: "7a3f0e9c4b185d26a8e1c5d0f294b637",
    17: "4c1d8e2a6f0b4935e7a9c3d0b185f258",
    18: "6e9b2c4d0a185f37c8e1a5d9f304b761",
    19: "0a5f8c2e1d4b6937e9a0c4d8b215f380",
    20: "3d7e1a9c5b0f4826e8a2c4d1f096b573",
}

HIT_FILEIDS = {
    1: "3388266283352513021",
    2: "5708107255241471660",
    3: "4168185900607394526",
    4: "7486813538534902642",
    5: "1693568431137058820",
    6: "4977455862453037760",
    7: "5246172817453610438",
    8: "5157394068897717930",
    9: "3566463593494963628",
    10: "380464014271222964",
}

HULLS = {
    1: """  _localXZ:
  - {x: -0.5000001, y: -0.21999998}
  - {x: -0.46000013, y: -0.39000002}
  - {x: -0.1900001, y: -0.46}
  - {x: 0.03999996, y: -0.50000006}
  - {x: 0.4700001, y: -0.43000007}
  - {x: 0.46000007, y: -0.19000007}
  - {x: -0.029999988, y: 0.21000002}
  - {x: -0.25000003, y: 0.23000003}
  - {x: -0.29000002, y: 0.18000004}
  _boundingRadius: 0.63702446""",
    2: """  _localXZ:
  - {x: -0.5000001, y: -0.099999964}
  - {x: -0.48000014, y: -0.21999998}
  - {x: -0.17000006, y: -0.38000005}
  - {x: 0.27, y: -0.4100001}
  - {x: 0.49000007, y: -0.28000006}
  - {x: 0.5000001, y: -0.13000005}
  - {x: 0.11000006, y: 0.33}
  - {x: 0.010000037, y: 0.41000006}
  - {x: -0.26000002, y: 0.30000004}
  - {x: -0.4500001, y: 0.030000042}
  _boundingRadius: 0.5643581""",
    3: """  _localXZ:
  - {x: -0.5000001, y: -0.12999997}
  - {x: -0.38000014, y: -0.34}
  - {x: -0.08000006, y: -0.48000005}
  - {x: 0.049999967, y: -0.50000006}
  - {x: 0.29000002, y: -0.50000006}
  - {x: 0.5000001, y: -0.32000005}
  - {x: 0.4600001, y: -0.14000006}
  - {x: 0.13000005, y: 0.37000003}
  - {x: 0.040000048, y: 0.46000004}
  - {x: -0.27, y: 0.36000004}
  _boundingRadius: 0.593633""",
    4: """  _localXZ:
  - {x: -0.5200001, y: -0.07999996}
  - {x: -0.5600002, y: -0.35999995}
  - {x: -0.3400001, y: -0.51000005}
  - {x: -0.030000059, y: -0.59000003}
  - {x: 0.37, y: -0.5300001}
  - {x: 0.57000005, y: -0.4200001}
  - {x: 0.5200001, y: -0.15000005}
  - {x: 0.110000074, y: 0.49000004}
  - {x: -0.039999954, y: 0.58000004}
  - {x: -0.27, y: 0.39000008}
  _boundingRadius: 0.7080255""",
}

META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

README_TEMPLATE = """# Grave_Q{n}

坟墓品质源图。Prefab：`Assets/Prefabs/Dig/Grave_Q{n}.prefab`。
可选子资源命名：IconStyle1 / IconStyle2 / IconStyle3（>65% / 30–65% / <30% HP）。
"""

PARENT_README = """坟墓视觉父目录。子文件夹名 = Grave Prefab 逻辑名（Grave_Q1…Q20）。每品质可再放 IconStyle1/2/3（HP% 样式）。
"""


def family(n: int) -> int:
    return ((n - 1) % 4) + 1


def file_prefix(n: int) -> str:
    return f"870000{n:02d}0000000000"


def hit_fileid(n: int) -> str:
    if n in HIT_FILEIDS:
        return HIT_FILEIDS[n]
    return f"{file_prefix(n)}8"


def sprite_size(n: int) -> str:
    return "{x: 1.2, y: 1.2}" if family(n) == 4 else "{x: 1, y: 1}"


def prefab_yaml(n: int) -> str:
    p = file_prefix(n)
    hit_id = hit_fileid(n)
    sprite_guid = SPRITE_GUIDS[n]
    hull = HULLS[family(n)]
    size = sprite_size(n)
    return f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &{p}1
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {p}2}}
  - component: {{fileID: {p}3}}
  - component: {{fileID: {hit_id}}}
  - component: {{fileID: {p}4}}
  m_Layer: 0
  m_Name: Grave_Q{n}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{p}2
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {p}1}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: {p}6}}
  m_Father: {{fileID: 0}}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!114 &{p}3
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {p}1}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: b85b3c3e8eb15914c808b7c973f80994, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  _radius: 0.55
--- !u!114 &{hit_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {p}1}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: c4e8f2a19d7b4e6a8f3c5d1e0b9a8765, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
{hull}
--- !u!114 &{p}4
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {p}1}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 087c375487ae4e842ac5c1061e0e58d3, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  _bodyRenderer: {{fileID: {p}7}}
--- !u!1 &{p}5
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {p}6}}
  - component: {{fileID: {p}7}}
  m_Layer: 0
  m_Name: Sprite
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{p}6
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {p}5}}
  m_LocalRotation: {{x: 0.000000030908623, y: 0.7071068, z: 0.7071068, w: -0.000000030908623}}
  m_LocalPosition: {{x: 0, y: 0.25, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {p}2}}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!212 &{p}7
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {p}5}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 200
  m_Sprite: {{fileID: 21300000, guid: {sprite_guid}, type: 3}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {size}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
"""


def write_catalog() -> None:
    text = CATALOG.read_text(encoding="utf-8")
    marker = "  _graves:"
    idx = text.find(marker)
    if idx < 0:
        raise SystemExit("DigPrefabCatalog _graves not found")
    head = text[: idx + len(marker)]
    lines = [head.rstrip("\n")]
    for n in range(1, 21):
        p = file_prefix(n)
        guid = PREFAB_META_GUIDS[n]
        lines.append(f"  - QualityId: Q{n}")
        lines.append(f"    Prefab: {{fileID: {p}1, guid: {guid}, type: 3}}")
    lines.append("")
    CATALOG.write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    PREFABS.mkdir(parents=True, exist_ok=True)
    for n in range(1, 21):
        prefab_path = PREFABS / f"Grave_Q{n}.prefab"
        meta_path = PREFABS / f"Grave_Q{n}.prefab.meta"
        if n >= 4:
            prefab_path.write_text(prefab_yaml(n), encoding="utf-8")
        if n >= 11:
            meta_path.write_text(META_TEMPLATE.format(guid=PREFAB_META_GUIDS[n]), encoding="utf-8")
        readme = ART / f"Grave_Q{n}" / "README.md"
        if readme.parent.exists():
            readme.write_text(README_TEMPLATE.format(n=n), encoding="utf-8")
    (ART / "README.md").write_text(PARENT_README, encoding="utf-8")
    write_catalog()
    print("wired Grave_Q1..Q20 prefabs + catalog")


if __name__ == "__main__":
    main()
