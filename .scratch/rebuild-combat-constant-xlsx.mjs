/**
 * Rebuild CombatConstantConfig xlsx (3-row header) from CSV for Mode1 + Mode2.
 * Usage: node .scratch/rebuild-combat-constant-xlsx.mjs
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";
import { createRequire } from "module";
import { spawnSync } from "child_process";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, "..");
const assets = path.join(root, "Gravedigger2026", "Assets", "ConfigTables");

function ensureJszip() {
  try {
    return createRequire(import.meta.url)("jszip");
  } catch {
    const r = spawnSync("npm", ["install", "jszip@3", "--no-save"], {
      cwd: root,
      shell: true,
      encoding: "utf8",
    });
    if (r.status !== 0) {
      console.error(r.stderr || r.stdout);
      process.exit(1);
    }
    return createRequire(import.meta.url)("jszip");
  }
}

function parseCsv(text) {
  const lines = text.replace(/^\uFEFF/, "").split(/\r?\n/).filter((l) => l.length > 0);
  const rows = [];
  for (const line of lines) {
    const cells = [];
    let cur = "";
    let inQ = false;
    for (let i = 0; i < line.length; i++) {
      const ch = line[i];
      if (inQ) {
        if (ch === '"' && line[i + 1] === '"') {
          cur += '"';
          i++;
        } else if (ch === '"') {
          inQ = false;
        } else {
          cur += ch;
        }
      } else if (ch === '"') {
        inQ = true;
      } else if (ch === ",") {
        cells.push(cur);
        cur = "";
      } else {
        cur += ch;
      }
    }
    cells.push(cur);
    rows.push(cells);
  }
  return rows;
}

function xmlEscape(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

function colLetter(n) {
  // 0 -> A
  let s = "";
  let n1 = n + 1;
  while (n1 > 0) {
    const rem = (n1 - 1) % 26;
    s = String.fromCharCode(65 + rem) + s;
    n1 = Math.floor((n1 - 1) / 26);
  }
  return s;
}

function buildWorkbook(dataRows) {
  // dataRows: array of {key, zh, value, comment, commentZh}
  const zhHeader = ["常量键", "主键中文翻译", "数值", "备注", "备注中文解释"];
  const typeHeader = ["主键", "展示用中文名；运行时不读", "float", "英文备注；运行时不读", "中文说明；运行时不读"];
  const enHeader = ["ConstantKey", "ConstantKeyZh", "Value", "Comment", "CommentZh"];

  const strings = [];
  const add = (s) => {
    const i = strings.length;
    strings.push(s);
    return i;
  };

  const zhIdx = zhHeader.map(add);
  const typeIdx = typeHeader.map(add);
  const enIdx = enHeader.map(add);
  const dataIdx = dataRows.map((r) => [
    add(r.key),
    add(r.zh),
    null, // numeric
    add(r.comment),
    add(r.commentZh),
  ]);

  const sst = [
    `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`,
    `<sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" count="${strings.length}" uniqueCount="${strings.length}">`,
    ...strings.map((t) => `<si><t>${xmlEscape(t)}</t></si>`),
    `</sst>`,
  ].join("");

  const rowXml = (r, cells) => {
    const parts = [`<row r="${r}" spans="1:5">`];
    for (let c = 0; c < 5; c++) {
      const ref = `${colLetter(c)}${r}`;
      const cell = cells[c];
      if (cell && cell.t === "n") {
        parts.push(`<c r="${ref}"><v>${cell.v}</v></c>`);
      } else {
        parts.push(`<c r="${ref}" t="s"><v>${cell.v}</v></c>`);
      }
    }
    parts.push(`</row>`);
    return parts.join("");
  };

  const sheetRows = [
    rowXml(1, zhIdx.map((v) => ({ v }))),
    rowXml(2, typeIdx.map((v) => ({ v }))),
    rowXml(3, enIdx.map((v) => ({ v }))),
  ];
  for (let i = 0; i < dataRows.length; i++) {
    const r = 4 + i;
    const idx = dataIdx[i];
    sheetRows.push(
      rowXml(r, [
        { v: idx[0] },
        { v: idx[1] },
        { t: "n", v: dataRows[i].value },
        { v: idx[3] },
        { v: idx[4] },
      ])
    );
  }

  const lastRow = 3 + dataRows.length;
  const sheet = [
    `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`,
    `<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">`,
    `<dimension ref="A1:E${lastRow}"/>`,
    `<sheetViews><sheetView workbookViewId="0"/></sheetViews>`,
    `<sheetFormatPr defaultRowHeight="15"/>`,
    `<sheetData>`,
    ...sheetRows,
    `</sheetData>`,
    `</worksheet>`,
  ].join("");

  return { sst, sheet };
}

async function writeXlsx(JSZip, templatePath, outPath, dataRows) {
  const buf = fs.readFileSync(templatePath);
  const zip = await JSZip.loadAsync(buf);
  const { sst, sheet } = buildWorkbook(dataRows);
  zip.file("xl/sharedStrings.xml", sst);
  zip.file("xl/worksheets/sheet1.xml", sheet);
  const out = await zip.generateAsync({
    type: "nodebuffer",
    compression: "DEFLATE",
  });
  fs.writeFileSync(outPath, out);
  console.log("Wrote", outPath);
}

function loadCsv(csvPath) {
  const rows = parseCsv(fs.readFileSync(csvPath, "utf8"));
  const header = rows[0];
  const iKey = header.indexOf("ConstantKey");
  const iZh = header.indexOf("ConstantKeyZh");
  const iVal = header.indexOf("Value");
  const iC = header.indexOf("Comment");
  const iCzh = header.indexOf("CommentZh");
  return rows.slice(1).map((r) => ({
    key: r[iKey],
    zh: r[iZh] || "",
    value: r[iVal],
    comment: r[iC] || "",
    commentZh: r[iCzh] || "",
  }));
}

const JSZip = ensureJszip();
const targets = [
  {
    csv: path.join(assets, "Csv", "Combat_CombatConstantConfig.csv"),
    xlsx: path.join(assets, "Excel", "通用_常量表_Combat_CombatConstantConfig.xlsx"),
  },
  {
    csv: path.join(assets, "Mode2", "Csv", "Combat_CombatConstantConfig.csv"),
    xlsx: path.join(assets, "Mode2", "Excel", "通用_常量表_Combat_CombatConstantConfig.xlsx"),
  },
];

for (const t of targets) {
  const data = loadCsv(t.csv);
  await writeXlsx(JSZip, t.xlsx, t.xlsx, data);
}
