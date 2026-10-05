const fs = require("fs");
const path = require("path");

const src = "F:/CursorGame_Git/Gravedigger2026/Gravedigger2026/Assets/Prefabs/Maps/SearchExtract_Lv2_01.prefab";
const dest = "F:/CursorGame_Git/Gravedigger2026/Gravedigger2026/Assets/Prefabs/Maps/Coc_Lv2_01.prefab";
const destMeta = dest + ".meta";
const catalog = "F:/CursorGame_Git/Gravedigger2026/Gravedigger2026/Assets/Settings/Defend/DefendPrefabCatalog.asset";

const prefabGuid = "c0c04d011b24c0e8a7d6e5f4a3b20101";
const spawnScript = "f8c2a5e61b7d820e3a4b5c6d7e8f9012";
const fogScript = "c0c0f09a11b24c0e8a7d6e5f4a3b20102";
const captureScript = "c0c0ca9711b24c0e8a7d6e5f4a3b20103";
const markersTransform = "2092522387770036821";
const rootGameObject = "2120705586636139558";

const text = fs.readFileSync(src, "utf8").replace(/\r\n/g, "\n");
const splitAt = text.indexOf("--- !u!");
const header = text.slice(0, splitAt);
const docs = text.slice(splitAt).split(/\n(?=--- !u!)/);

function headerOf(doc) {
  const match = doc.match(/^--- !u!(\d+) &(\d+)/);
  if (!match) {
    throw new Error("Bad document header: " + doc.slice(0, 80));
  }
  return { type: match[1], id: match[2] };
}

const byId = new Map();
for (const doc of docs) {
  const info = headerOf(doc);
  byId.set(info.id, { ...info, doc });
}

function gameObjectName(doc) {
  const match = doc.match(/\n  m_Name: ([^\n]+)/);
  return match ? match[1] : "";
}

function componentIds(doc) {
  const ids = [];
  const re = /- component: \{fileID: (\d+)\}/g;
  let match;
  while ((match = re.exec(doc))) {
    ids.push(match[1]);
  }
  return ids;
}

function childTransformIds(doc) {
  const ids = [];
  const re = /\{fileID: (\d+)\}/g;
  const children = doc.split("m_Children:")[1];
  if (!children) {
    return ids;
  }
  const section = children.split("m_Father:")[0];
  let match;
  while ((match = re.exec(section))) {
    ids.push(match[1]);
  }
  return ids;
}

const drop = new Set();

function dropGameObject(goId) {
  if (drop.has(goId)) {
    return;
  }
  const go = byId.get(goId);
  if (!go || go.type !== "1") {
    return;
  }
  drop.add(goId);
  for (const componentId of componentIds(go.doc)) {
    drop.add(componentId);
    const component = byId.get(componentId);
    if (component && component.type === "4") {
      for (const child of childTransformIds(component.doc)) {
        const childTransform = byId.get(child);
        if (!childTransform) {
          continue;
        }
        const childGo = childTransform.doc.match(/m_GameObject: \{fileID: (\d+)\}/);
        if (childGo) {
          dropGameObject(childGo[1]);
        }
      }
    }
  }
}

for (const entry of byId.values()) {
  if (entry.type !== "1") {
    continue;
  }
  const name = gameObjectName(entry.doc);
  if (name.startsWith("Objective_") || name.startsWith("BossPoint")) {
    dropGameObject(entry.id);
  }
}

let kept = docs.filter((doc) => !drop.has(headerOf(doc).id));
kept = kept.map((doc) => {
  if (!doc.includes("m_Children:")) {
    return doc;
  }
  return doc.replace(/\n  - \{fileID: (\d+)\}/g, (line, id) => (drop.has(id) ? "" : line));
});

function block(id, body) {
  return `--- !u!${body.startsWith("GameObject:") ? "1" : body.startsWith("Transform:") ? "4" : "114"} &${id}\n${body}\n`;
}

function gameObject(id, transformId, extraComponents, name) {
  const components = [`  - component: {fileID: ${transformId}}`, ...extraComponents.map((c) => `  - component: {fileID: ${c}}`)];
  return block(id, `GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
${components.join("\n")}
  m_Layer: 0
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1`);
}

function transform(id, goId, father, position, children) {
  const childLines = children.length === 0
    ? "  m_Children: []"
    : "  m_Children:\n" + children.map((child) => `  - {fileID: ${child}}`).join("\n");
  return block(id, `Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: ${position[0]}, y: ${position[1]}, z: ${position[2]}}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
${childLines}
  m_Father: {fileID: ${father}}
  m_RootOrder: 20
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}`);
}

function behaviour(id, goId, scriptGuid, fields) {
  const extra = fields ? `\n${fields}` : "";
  return block(id, `MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${scriptGuid}, type: 3}
  m_Name: 
  m_EditorClassIdentifier: ${extra}`);
}

const added = [];
const markerChildren = [];

function addSpawn(go, tr, mb, name, idName, position) {
  added.push(gameObject(go, tr, [mb], name));
  added.push(transform(tr, go, markersTransform, position, []));
  added.push(behaviour(mb, go, spawnScript, `_spawnPointId: ${idName}`));
  markerChildren.push(tr);
}

addSpawn("8800000000000000101", "8800000000000000102", "8800000000000000103", "SpawnPoint_SP_Mini", "SP_Mini", [4.26, 0.05, 4.63]);
addSpawn("8800000000000000111", "8800000000000000112", "8800000000000000113", "SpawnPoint_SP_Final", "SP_Final", [8.26, 0.05, 6.63]);

const fogChildren = ["8800000000000000212", "8800000000000000222", "8800000000000000232"];
added.push(gameObject("8800000000000000201", "8800000000000000202", ["8800000000000000203"], "CocFog_01"));
added.push(transform("8800000000000000202", "8800000000000000201", markersTransform, [0.26, 0.05, 8.63], fogChildren));
added.push(behaviour("8800000000000000203", "8800000000000000201", fogScript, ""));
const fogPoints = [
  ["8800000000000000211", "8800000000000000212", "P0", [-3, 0, -2]],
  ["8800000000000000221", "8800000000000000222", "P1", [3, 0, -2]],
  ["8800000000000000231", "8800000000000000232", "P2", [0, 0, 3]],
];
for (const [go, tr, name, position] of fogPoints) {
  added.push(gameObject(go, tr, [], name));
  added.push(transform(tr, go, "8800000000000000202", position, []));
}
markerChildren.push("8800000000000000202");

function addCapture(go, tr, mb, name, idName, position) {
  added.push(gameObject(go, tr, [mb], name));
  added.push(transform(tr, go, markersTransform, position, []));
  added.push(behaviour(mb, go, captureScript, `_capturePointId: ${idName}`));
  markerChildren.push(tr);
}
addCapture("8800000000000000301", "8800000000000000302", "8800000000000000303", "CocCapture_CP_Normal", "CP_Normal", [-3.74, 0.05, 4.63]);
addCapture("8800000000000000311", "8800000000000000312", "8800000000000000313", "CocCapture_CP_Hard", "CP_Hard", [0.26, 0.05, 10.63]);
addCapture("8800000000000000321", "8800000000000000322", "8800000000000000323", "CocCapture_CP_Hell", "CP_Hell", [6.26, 0.05, 0.63]);

let body = kept.join("\n");
body = body.replace("\n  m_Name: SearchExtract_Lv2_01\n", "\n  m_Name: Coc_Lv2_01\n");
const markerHeader = `--- !u!4 &${markersTransform}\n`;
const markerStart = body.indexOf(markerHeader);
if (markerStart < 0) {
  throw new Error("PushMapMarkers transform missing");
}
const markerEnd = body.indexOf("\n--- !u!", markerStart + markerHeader.length);
const markerDoc = body.slice(markerStart, markerEnd);
const insertion = markerChildren.map((id) => `  - {fileID: ${id}}`).join("\n");
const patchedMarker = markerDoc.replace("\n  m_Father:", `\n${insertion}\n  m_Father:`);
if (patchedMarker === markerDoc) {
  throw new Error("Failed to insert marker children");
}
body = body.slice(0, markerStart) + patchedMarker + body.slice(markerEnd);
fs.writeFileSync(dest, header + body + added.join(""), "utf8");

fs.writeFileSync(destMeta, `fileFormatVersion: 2
guid: ${prefabGuid}
PrefabImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
`, "utf8");

let catalogText = fs.readFileSync(catalog, "utf8");
if (!catalogText.includes("MapId: Coc_Lv2_01")) {
  const entry = `  - MapId: Coc_Lv2_01\n    Prefab: {fileID: ${rootGameObject}, guid: ${prefabGuid}, type: 3}\n`;
  catalogText = catalogText.replace("  _warriorAppearances:\n", entry + "  _warriorAppearances:\n");
  fs.writeFileSync(catalog, catalogText, "utf8");
}

console.log(`Dropped ${drop.size} objects. Wrote ${dest}`);
