/**
 * Insert ClassConfig.ParabolaMeleeAttackAnims after NormalAttackAnims
 * in Mode1+Mode2 Excel, then bake CSV (EN header row onward).
 */
const XLSX = require("xlsx");
const fs = require("fs");

const files = [
  {
    xlsx:
      "F:/CursorGame_Git/Gravedigger2026/Gravedigger2026/Assets/ConfigTables/Mode2/Excel/制造_职业配置表_Manufacture_ClassConfig.xlsx",
    csv: "F:/CursorGame_Git/Gravedigger2026/Gravedigger2026/Assets/ConfigTables/Mode2/Csv/Manufacture_ClassConfig.csv",
  },
  {
    xlsx:
      "F:/CursorGame_Git/Gravedigger2026/Gravedigger2026/Assets/ConfigTables/Excel/制造_职业配置表_Manufacture_ClassConfig.xlsx",
    csv: "F:/CursorGame_Git/Gravedigger2026/Gravedigger2026/Assets/ConfigTables/Csv/Manufacture_ClassConfig.csv",
  },
];

function cellStr(v) {
  if (v == null || v === undefined) return "";
  if (typeof v === "number") {
    if (Number.isFinite(v) && Math.abs(v - Math.round(v)) < 1e-9) {
      return String(Math.round(v));
    }
    return String(v);
  }
  return String(v);
}

function escapeCsv(v) {
  const s = cellStr(v);
  if (/[",\n\r]/.test(s)) {
    return '"' + s.replace(/"/g, '""') + '"';
  }
  return s;
}

function findHeaderIndex(rows) {
  for (let i = 0; i < Math.min(3, rows.length); i++) {
    const row = rows[i];
    let saw = false;
    let ok = true;
    for (const c of row) {
      const t = cellStr(c).trim();
      if (!t) continue;
      saw = true;
      if (!/^[A-Za-z_][A-Za-z0-9_.]*$/.test(t)) {
        ok = false;
        break;
      }
    }
    if (saw && ok) return i;
  }
  throw new Error("no EN header in first 3 rows");
}

function insertValueForRow(r, headerI) {
  const zhName = "特殊普通攻击动作";
  const zhDesc =
    "Parabola临时近战普攻动作权重池；编码同NormalAttackAnims；空→Attack1；非Parabola可空";
  const enName = "ParabolaMeleeAttackAnims";
  if (headerI === 0) return r === 0 ? enName : "";
  if (headerI === 1) {
    if (r === 0) return zhName;
    if (r === 1) return enName;
    return "";
  }
  // headerI === 2
  if (r === 0) return zhName;
  if (r === 1) return zhDesc;
  if (r === 2) return enName;
  return "";
}

for (const f of files) {
  const wb = XLSX.readFile(f.xlsx, { cellDates: false });
  const sheetName = wb.SheetNames[0];
  const ws = wb.Sheets[sheetName];
  const rows = XLSX.utils.sheet_to_json(ws, { header: 1, defval: "", raw: true });
  const headerI = findHeaderIndex(rows);
  const header = rows[headerI].map(cellStr);

  if (!header.includes("ParabolaMeleeAttackAnims")) {
    let insertAt = header.indexOf("NormalAttackAnims");
    if (insertAt < 0) insertAt = header.length;
    else insertAt += 1;

    for (let r = 0; r < rows.length; r++) {
      while (rows[r].length < insertAt) rows[r].push("");
      rows[r].splice(insertAt, 0, insertValueForRow(r, headerI));
    }

    wb.Sheets[sheetName] = XLSX.utils.aoa_to_sheet(rows);
    XLSX.writeFile(wb, f.xlsx);
    console.log("inserted", f.xlsx, "at", insertAt);
  } else {
    console.log("already present", f.xlsx);
  }

  const wb2 = XLSX.readFile(f.xlsx, { cellDates: false });
  const rows2 = XLSX.utils.sheet_to_json(wb2.Sheets[wb2.SheetNames[0]], {
    header: 1,
    defval: "",
    raw: true,
  });
  const hI = findHeaderIndex(rows2);
  const exportRows = rows2
    .slice(hI)
    .filter((row, idx) => idx === 0 || row.some((c) => cellStr(c).trim() !== ""));
  const csv = exportRows.map((row) => row.map(escapeCsv).join(",")).join("\n") + "\n";
  fs.writeFileSync(f.csv, csv, "utf8");
  console.log("baked", f.csv);
  console.log("header", exportRows[0].join(","));
}
