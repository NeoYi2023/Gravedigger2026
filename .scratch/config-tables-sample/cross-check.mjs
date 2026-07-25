import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const csvDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../../Gravedigger2026/Assets/ConfigTables/Csv"
);

function load(name) {
  const text = fs.readFileSync(path.join(csvDir, name), "utf8").trim();
  const lines = text.split(/\r?\n/);
  const headers = parseCsvLine(lines[0]);
  return lines.slice(1).map((line) => {
    const cols = parseCsvLine(line);
    const o = {};
    headers.forEach((h, i) => {
      o[h] = cols[i] ?? "";
    });
    return o;
  });
}

function parseCsvLine(line) {
  const cols = [];
  let cur = "";
  let q = false;
  for (let i = 0; i < line.length; i++) {
    const c = line[i];
    if (c === '"') {
      q = !q;
      continue;
    }
    if (c === "," && !q) {
      cols.push(cur);
      cur = "";
      continue;
    }
    cur += c;
  }
  cols.push(cur);
  return cols;
}

const errs = [];
const digs = new Set(load("Dig_DigGameplayConfig.csv").map((r) => r.GameplayConfigId));
const defends = new Set(load("Defend_DefendGameplayConfig.csv").map((r) => r.GameplayConfigId));
const waves = new Set(load("Defend_WaveSpawnConfig.csv").map((r) => r.WaveConfigId));
const monsters = new Set(load("Defend_MonsterConfig.csv").map((r) => r.MonsterId));
const qualities = new Set(load("Dig_GraveQualityConfig.csv").map((r) => r.QualityId));
const materials = new Set(load("Dig_MaterialConfig.csv").map((r) => r.MaterialId));
const bodyParts = new Set(load("Manufacture_BodyPartConfig.csv").map((r) => r.BodyPartId));
const classes = new Set(load("Manufacture_ClassConfig.csv").map((r) => r.ClassId));
const races = new Set(load("Manufacture_RaceConfig.csv").map((r) => r.RaceId));
const techs = new Set(load("Tech_TechTreeConfig.csv").map((r) => r.TechId));

if (!load("Dig_CurrencyConfig.csv").some((r) => r.CurrencyId === "Spirit")) {
  errs.push("Currency missing Spirit");
}
for (const id of materials) {
  if (bodyParts.has(id)) errs.push(`Material/BodyPart collision: ${id}`);
}
for (const r of load("Level_LevelOperationConfig.csv")) {
  if (r.GameplayType === "Dig" && !digs.has(r.GameplayConfigId)) {
    errs.push(`Level Dig FK miss ${r.GameplayConfigId}`);
  }
  if (r.GameplayType === "Defend" && !defends.has(r.GameplayConfigId)) {
    errs.push(`Level Defend FK miss ${r.GameplayConfigId}`);
  }
}
for (const r of load("Defend_DefendGameplayConfig.csv")) {
  if (!waves.has(r.WaveConfigId)) errs.push(`Defend Wave FK miss ${r.WaveConfigId}`);
}
for (const r of load("Defend_WaveSpawnConfig.csv")) {
  if (!monsters.has(r.MonsterId)) errs.push(`Wave Monster FK miss ${r.MonsterId}`);
}
for (const r of load("Dig_DigGameplayConfig.csv")) {
  for (const seg of r.GraveSpawnWeights.split("|")) {
    const q = seg.split(";")[0];
    if (q && !qualities.has(q)) errs.push(`Dig weight Q miss ${q}`);
  }
}
for (const r of load("Manufacture_SoulConfig.csv")) {
  if (!classes.has(r.ClassId)) errs.push(`Soul Class FK miss ${r.ClassId}`);
}
for (const r of load("Manufacture_BodyPartConfig.csv")) {
  if (!races.has(r.RaceId)) errs.push(`BodyPart Race FK miss ${r.RaceId}`);
}
for (const r of load("Manufacture_BodyAppearanceConfig.csv")) {
  if (!races.has(r.RaceId)) errs.push(`Appearance Race FK miss ${r.RaceId}`);
}
const fb = {};
for (const r of load("Manufacture_BodyAppearanceConfig.csv")) {
  if (String(r.IsFallback) === "1") fb[r.RaceId] = (fb[r.RaceId] || 0) + 1;
}
for (const [race, n] of Object.entries(fb)) {
  if (n > 1) errs.push(`IsFallback>1 for ${race}`);
}
for (const r of load("Tech_TechEffectConfig.csv")) {
  if (!techs.has(r.TechId)) errs.push(`TechEffect FK miss ${r.TechId}`);
}
const loc = load("Combat_LossOfControlConfig.csv");
if (loc.length !== 4) errs.push(`LossOfControl rows=${loc.length}`);
const tiers = loc
  .map((r) => r.TierId)
  .sort()
  .join(",");
if (tiers !== "1,2,3,4") errs.push(`TierIds=${tiers}`);

const expected = {
  "Combat_LossOfControlConfig.csv": 4,
};
for (const f of fs.readdirSync(csvDir).filter((x) => x.endsWith(".csv"))) {
  const n = load(f).length;
  const exp = expected[f] ?? 10;
  if (n !== exp) errs.push(`${f} rows=${n} expected=${exp}`);
}

const excelDir = path.resolve(csvDir, "../Excel");
const xlsx = fs.readdirSync(excelDir).filter((x) => x.endsWith(".xlsx"));
if (xlsx.length !== 21) errs.push(`Excel count=${xlsx.length}`);

console.log(errs.length ? `ERRORS:\n${errs.join("\n")}` : "CROSS-CHECK OK");
process.exit(errs.length ? 1 : 0);
