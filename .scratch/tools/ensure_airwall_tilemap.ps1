$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = "F:\CursorGame_Git\Gravedigger2026\Gravedigger2026"
$utf8 = New-Object System.Text.UTF8Encoding $false
$paintGuid = "a1c0e7b24d5f4a6e8b9c0d1e2f3a4b5c"
$layerGuid = "b2d1f8c35e6a4b7f9c0d1e2f3a4b5c6d"
$solidSpriteGuid = "a11a1101b2c3d4e5f60718293a4b5c6d"
$specialSpriteGuid = "a11a1102b2c3d4e5f60718293a4b5c6e"
$solidTileGuid = "a11a1103b2c3d4e5f60718293a4b5c6f"
$specialTileGuid = "a11a1104b2c3d4e5f60718293a4b5c70"
$paletteGuid = "a11a1105b2c3d4e5f60718293a4b5c71"

function Write-Utf8([string]$path, [string]$text) {
    $dir = Split-Path $path -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
    [System.IO.File]::WriteAllText($path, $text, $utf8)
}

function New-DiamondPng([string]$path, [System.Drawing.Color]$color) {
    $bmp = New-Object System.Drawing.Bitmap 64, 32
    $clear = [System.Drawing.Color]::FromArgb(0, 0, 0, 0)
    for ($y = 0; $y -lt 32; $y++) {
        for ($x = 0; $x -lt 64; $x++) {
            $nx = (($x + 0.5) / 64.0) * 2.0 - 1.0
            $ny = (($y + 0.5) / 32.0) * 2.0 - 1.0
            if ([Math]::Abs($nx) + [Math]::Abs($ny) -le 1.0) {
                $bmp.SetPixel($x, $y, $color)
            } else {
                $bmp.SetPixel($x, $y, $clear)
            }
        }
    }
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function New-SpriteMeta([string]$guid, [string]$spriteId) {
@"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 12
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMasterTextureLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 64
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: $spriteId
    internalID: 21300000
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  spritePackingTag: 
  pSDRemoveMatte: 0
  pSDShowRemoveMatteOption: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
}

function New-TileAsset([string]$name, [string]$spriteGuid, [int]$special) {
@"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $paintGuid, type: 3}
  m_Name: $name
  m_EditorClassIdentifier: 
  m_Sprite: {fileID: 21300000, guid: $spriteGuid, type: 3}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_Transform:
    e00: 1
    e01: 0
    e02: 0
    e03: 0
    e10: 0
    e11: 1
    e12: 0
    e13: 0
    e20: 0
    e21: 0
    e22: 1
    e23: 0
    e30: 0
    e31: 0
    e32: 0
    e33: 1
  m_InstancedGameObject: {fileID: 0}
  m_Flags: 1
  m_ColliderType: 0
  _supportsSpecialMove: $special
"@
}

function New-AssetMeta([string]$guid) {
@"
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
}

$spriteDir = Join-Path $root "Assets\Art\Maps\Tiles\AirWall\Sprites"
$tileDir = Join-Path $root "Assets\Art\Maps\Tiles\AirWall"
New-Item -ItemType Directory -Force -Path $spriteDir | Out-Null

$solidPng = Join-Path $spriteDir "AirWallTile.png"
$specialPng = Join-Path $spriteDir "AirWallSpecialTile.png"
New-DiamondPng $solidPng ([System.Drawing.Color]::FromArgb(140, 89, 140, 255))
New-DiamondPng $specialPng ([System.Drawing.Color]::FromArgb(140, 89, 242, 115))
Write-Utf8 ($solidPng + ".meta") (New-SpriteMeta $solidSpriteGuid "a11a1101b2c3d4e5f60718293a4b5c6d")
Write-Utf8 ($specialPng + ".meta") (New-SpriteMeta $specialSpriteGuid "a11a1102b2c3d4e5f60718293a4b5c6e")

$solidTile = Join-Path $tileDir "AirWallTile.asset"
$specialTile = Join-Path $tileDir "AirWallSpecialTile.asset"
Write-Utf8 $solidTile (New-TileAsset "AirWallTile" $solidSpriteGuid 0)
Write-Utf8 ($solidTile + ".meta") (New-AssetMeta $solidTileGuid)
Write-Utf8 $specialTile (New-TileAsset "AirWallSpecialTile" $specialSpriteGuid 1)
Write-Utf8 ($specialTile + ".meta") (New-AssetMeta $specialTileGuid)

$identity = @"
    e00: 1
    e01: 0
    e02: 0
    e03: 0
    e10: 0
    e11: 1
    e12: 0
    e13: 0
    e20: 0
    e21: 0
    e22: 1
    e23: 0
    e30: 0
    e31: 0
    e32: 0
    e33: 1
"@

$palette = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &100001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 100002}
  - component: {fileID: 100003}
  m_Layer: 0
  m_Name: AirWall
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100002
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100001}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 100005}
  m_Father: {fileID: 0}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!156049354 &100003
Grid:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100001}
  m_Enabled: 1
  m_CellSize: {x: 1, y: 0.5, z: 2}
  m_CellGap: {x: 0, y: 0, z: 0}
  m_CellLayout: 2
  m_CellSwizzle: 0
--- !u!1 &100004
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 100005}
  - component: {fileID: 100006}
  - component: {fileID: 100007}
  m_Layer: 0
  m_Name: Layer1
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &100005
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100004}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 100002}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1839735485 &100006
Tilemap:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100004}
  m_Enabled: 1
  m_Tiles:
  - first: {x: 0, y: 0, z: 0}
    second:
      serializedVersion: 2
      m_TileIndex: 0
      m_TileSpriteIndex: 0
      m_TileMatrixIndex: 0
      m_TileColorIndex: 0
      m_TileObjectToInstantiateIndex: 65535
      dummyAlignment: 0
      m_AllTileFlags: 1
  - first: {x: 1, y: 0, z: 0}
    second:
      serializedVersion: 2
      m_TileIndex: 1
      m_TileSpriteIndex: 1
      m_TileMatrixIndex: 0
      m_TileColorIndex: 0
      m_TileObjectToInstantiateIndex: 65535
      dummyAlignment: 0
      m_AllTileFlags: 1
  m_AnimatedTiles: {}
  m_TileAssetArray:
  - m_RefCount: 1
    m_Data: {fileID: 11400000, guid: $solidTileGuid, type: 2}
  - m_RefCount: 1
    m_Data: {fileID: 11400000, guid: $specialTileGuid, type: 2}
  m_TileSpriteArray:
  - m_RefCount: 1
    m_Data: {fileID: 21300000, guid: $solidSpriteGuid, type: 3}
  - m_RefCount: 1
    m_Data: {fileID: 21300000, guid: $specialSpriteGuid, type: 3}
  m_TileMatrixArray:
  - m_RefCount: 2
    m_Data:
$identity
  m_TileColorArray:
  - m_RefCount: 2
    m_Data: {r: 1, g: 1, b: 1, a: 1}
  m_TileObjectToInstantiateArray: []
  m_AnimationFrameRate: 1
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_Origin: {x: 0, y: 0, z: 0}
  m_Size: {x: 2, y: 1, z: 1}
  m_TileAnchor: {x: 0.5, y: 0.5, z: 0}
  m_TileOrientation: 0
  m_TileOrientationMatrix:
$identity
--- !u!483693784 &100007
TilemapRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 100004}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
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
  m_LightmapParameters: {fileID: 0}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_ChunkSize: {x: 32, y: 32, z: 32}
  m_ChunkCullingBounds: {x: 0, y: 0, z: 0}
  m_MaxChunkCount: 16
  m_MaxFrameAge: 16
  m_SortOrder: 0
  m_Mode: 0
  m_DetectChunkCullingBounds: 0
  m_MaskInteraction: 0
"@
$palettePath = Join-Path $root "Assets\Art\Maps\Palettes\AirWall.prefab"
Write-Utf8 $palettePath $palette
Write-Utf8 ($palettePath + ".meta") @"
fileFormatVersion: 2
guid: $paletteGuid
PrefabImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

function Add-AirWallLayer([string]$prefabPath) {
    $text = [System.IO.File]::ReadAllText($prefabPath)
    if ($text.Contains("m_Name: AirWallTilemap")) {
        Write-Output "present $prefabPath"
        return
    }
    $nl = "`n"
    if ($text.Contains("`r`n")) { $nl = "`r`n" }
    $nameIdx = $text.IndexOf("m_Name: GroundTilemap")
    if ($nameIdx -lt 0) { throw "No GroundTilemap in $prefabPath" }
    $headerIdx = $text.LastIndexOf("--- !u!1 &", $nameIdx)
    $block = $text.Substring($headerIdx, $nameIdx - $headerIdx)
    $comp = [regex]::Match($block, "m_Component:\r?\n  - component: \{fileID: (\d+)\}")
    if (-not $comp.Success) { throw "No transform component in $prefabPath" }
    $transformId = $comp.Groups[1].Value
    $transformHeader = "--- !u!4 &$transformId"
    $tIdx = $text.IndexOf($transformHeader)
    if ($tIdx -lt 0) { throw "Transform $transformId missing in $prefabPath" }
    $childIdx = $text.IndexOf("m_Children:", $tIdx)
    $fatherIdx = $text.IndexOf("m_Father:", $childIdx)
    $nextDoc = $text.IndexOf("--- !u!", $tIdx + $transformHeader.Length)
    if ($childIdx -lt 0 -or $fatherIdx -lt 0 -or ($nextDoc -ge 0 -and $fatherIdx -gt $nextDoc)) {
        throw "Children block not found for $transformId in $prefabPath"
    }
    $childrenBlock = $text.Substring($childIdx, $fatherIdx - $childIdx)
    $existing = [regex]::Matches($childrenBlock, "\{fileID: (\d+)\}")
    $rootOrder = $existing.Count
    $go = 880000000000010001
    while ($text.Contains("&$go") -or $text.Contains("{fileID: $go}")) { $go += 17 }
    $tr = $go + 1
    $tm = $go + 2
    $rd = $go + 3
    $sc = $go + 4
    $insert = "  - {fileID: $tr}$nl"
    if ($childrenBlock -match "m_Children:\s*\[\]") {
        $text = $text.Remove($childIdx, $fatherIdx - $childIdx).Insert($childIdx, "m_Children:$nl$insert")
    } else {
        $lineStart = $text.LastIndexOf("`n", $fatherIdx)
        $text = $text.Insert($lineStart + 1, $insert)
    }
    $chunk = @"
--- !u!1 &$go
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: $tr}
  - component: {fileID: $tm}
  - component: {fileID: $rd}
  - component: {fileID: $sc}
  m_Layer: 0
  m_Name: AirWallTilemap
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &$tr
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $go}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: $transformId}
  m_RootOrder: $rootOrder
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!1839735485 &$tm
Tilemap:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $go}
  m_Enabled: 1
  m_Tiles: {}
  m_AnimatedTiles: {}
  m_TileAssetArray: []
  m_TileSpriteArray: []
  m_TileMatrixArray: []
  m_TileColorArray: []
  m_TileObjectToInstantiateArray: []
  m_AnimationFrameRate: 1
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_Origin: {x: 0, y: 0, z: 0}
  m_Size: {x: 0, y: 0, z: 1}
  m_TileAnchor: {x: 0.5, y: 0.5, z: 0}
  m_TileOrientation: 0
  m_TileOrientationMatrix:
    e00: 1
    e01: 0
    e02: 0
    e03: 0
    e10: 0
    e11: 1
    e12: 0
    e13: 0
    e20: 0
    e21: 0
    e22: 1
    e23: 0
    e30: 0
    e31: 0
    e32: 0
    e33: 1
--- !u!483693784 &$rd
TilemapRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $go}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 0
  m_ReflectionProbeUsage: 0
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
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
  m_LightmapParameters: {fileID: 0}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 8
  m_ChunkSize: {x: 32, y: 32, z: 32}
  m_ChunkCullingBounds: {x: 0, y: 0, z: 0}
  m_MaxChunkCount: 16
  m_MaxFrameAge: 16
  m_SortOrder: 0
  m_Mode: 0
  m_DetectChunkCullingBounds: 0
  m_MaskInteraction: 0
--- !u!114 &$sc
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $go}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $layerGuid, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
"@
    $chunk = $chunk -replace "`r`n", $nl
    $chunk = $chunk -replace "`n", $nl
    if (-not $text.EndsWith($nl)) { $text += $nl }
    $text += $chunk
    if (-not $text.EndsWith($nl)) { $text += $nl }
    [System.IO.File]::WriteAllText($prefabPath, $text, $utf8)
    Write-Output "added $prefabPath father=$transformId order=$rootOrder"
}

$maps = @(
    "Assets\Prefabs\Maps\PushMap_Demo_01.prefab",
    "Assets\Prefabs\Maps\PushMap_Demo_02.prefab",
    "Assets\Prefabs\Maps\PushMap_Demo_03.prefab",
    "Assets\Prefabs\Maps\SearchExtract_Demo_01.prefab",
    "Assets\Prefabs\Maps\SearchExtract_Lv1_01.prefab",
    "Assets\Prefabs\Maps\SearchExtract_Lv2_01.prefab",
    "Assets\Prefabs\Maps\Coc_Lv2_01.prefab"
)
foreach ($rel in $maps) {
    Add-AirWallLayer (Join-Path $root $rel)
}
Write-Output "done"
