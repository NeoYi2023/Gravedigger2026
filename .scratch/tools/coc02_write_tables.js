const fs = require("fs");
const path = require("path");
const XLSX = require("xlsx");

const root = path.join(
  "F:\\CursorGame_Git\\Gravedigger2026\\Gravedigger2026\\Assets\\ConfigTables"
);
const mode2Excel = path.join(root, "Mode2", "Excel");
const mode2Csv = path.join(root, "Mode2", "Csv");

const cocIds = {
  SB_N_Coc: "Coc_Normal",
  SB_H_Coc: "Coc_Hard",
  SB_L_Coc: "Coc_Hell",
};

const spawnRows = [];
for (const gameplay of ["Coc_Normal", "Coc_Hard", "Coc_Hell"]) {
  spawnRows.push([gameplay, "SP_01", "Monster_01", "3", "Normal", "1"]);
  spawnRows.push([gameplay, "SP_Mini", "Monster_01", "1", "MiniBoss", "1"]);
  spawnRows.push([gameplay, "SP_Final", "Monster_01", "1", "FinalBoss", "1"]);
}

const tables = [
  {
    excel: "COC_玩法配置表_Coc_CocGameplayConfig.xlsx",
    csv: "Coc_CocGameplayConfig.csv",
    zh: ["玩法配置ID", "地图编号", "临时揭示半径", "未探索透明度", "变暗透明度", "卡牌长按秒数", "按住洒兵间隔"],
    note: [
      "主键；沙盘 CocCombat 行引用",
      "Prefab 逻辑名 Coc_*",
      "地面 XZ 圆半径；缺省 2；≤0 加载失败",
      "缺省 0.9；须在 (0, 1]",
      "缺省 0.7；须在 (0, 1]",
      "缺省 1；≤0 加载失败",
      "缺省 0.1；≤0 加载失败",
    ],
    en: ["GameplayConfigId", "MapId", "RevealRadius", "UnexploredAlpha", "ExploredAlpha", "CardHoldSeconds", "DeployHoldIntervalSeconds"],
    data: [
      ["Coc_Normal", "Coc_Lv2_01", "2", "0.9", "0.7", "1", "0.1"],
      ["Coc_Hard", "Coc_Lv2_01", "2", "0.9", "0.7", "1", "0.1"],
      ["Coc_Hell", "Coc_Lv2_01", "2", "0.9", "0.7", "1", "0.1"],
    ],
  },
  {
    excel: "COC_刷怪配置表_Coc_CocSpawnConfig.xlsx",
    csv: "Coc_CocSpawnConfig.csv",
    zh: ["玩法配置ID", "刷怪点编号", "怪物ID", "数量", "刷怪角色", "同点顺序"],
    note: ["外键 → CocGameplayConfig", "与地图 SpawnPoint 匹配", "外键 → MonsterConfig", "≥1；FinalBoss 必须为 1", "Normal / MiniBoss / FinalBoss", "同 SpawnPointId 多行时升序"],
    en: ["GameplayConfigId", "SpawnPointId", "MonsterId", "SpawnCount", "SpawnRole", "SpawnOrder"],
    data: spawnRows,
  },
  {
    excel: "COC_占领点配置表_Coc_CocCapturePointConfig.xlsx",
    csv: "Coc_CocCapturePointConfig.csv",
    zh: ["占领点ID", "玩法配置ID", "永久亮起半径"],
    note: ["主键；与地图 CocCapturePoint 匹配", "外键 → CocGameplayConfig", "地面 XZ 圆；> 0"],
    en: ["CapturePointId", "GameplayConfigId", "Radius"],
    data: [
      ["CP_Normal", "Coc_Normal", "3"],
      ["CP_Hard", "Coc_Hard", "3"],
      ["CP_Hell", "Coc_Hell", "3"],
    ],
  },
  {
    excel: "COC_占领奖励配置表_Coc_CocCaptureRewardConfig.xlsx",
    csv: "Coc_CocCaptureRewardConfig.csv",
    zh: ["奖励ID", "占领点ID", "目标沙盘节点", "增加次数"],
    note: ["主键", "外键 → CocCapturePointConfig", "外键 → SandboxNodeConfig.NodeId；须同难度且为挖坟/商店/自动造兵", "≥ 1"],
    en: ["RewardId", "CapturePointId", "TargetNodeId", "AddCount"],
    data: [
      ["R_N", "CP_Normal", "SB_N_Dig", "1"],
      ["R_H", "CP_Hard", "SB_H_Shop", "1"],
      ["R_L", "CP_Hell", "SB_L_AM", "1"],
    ],
  },
];

function writeCsv(filePath, en, data) {
  const lines = [en.join(",")];
  for (const row of data) {
    lines.push(row.join(","));
  }
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  fs.writeFileSync(filePath, lines.join("\n") + "\n", "utf8");
}

function writeTable(table) {
  const aoa = [table.zh, table.note, table.en, ...table.data];
  const wb = XLSX.utils.book_new();
  const ws = XLSX.utils.aoa_to_sheet(aoa);
  XLSX.utils.book_append_sheet(wb, ws, "Sheet1");
  const excelPath = path.join(mode2Excel, table.excel);
  XLSX.writeFile(wb, excelPath);
  writeCsv(path.join(mode2Csv, table.csv), table.en, table.data);
  console.log(`Wrote ${table.excel} (${table.data.length} rows)`);
}

function cellText(value) {
  if (value === undefined || value === null) {
    return "";
  }
  return String(value).trim();
}

function patchSandbox(excelPath, csvPath) {
  const wb = XLSX.readFile(excelPath);
  const sheetName = wb.SheetNames[0];
  const ws = wb.Sheets[sheetName];
  const rows = XLSX.utils.sheet_to_json(ws, { header: 1, raw: false, defval: "" });
  let headerIndex = -1;
  for (let i = 0; i < Math.min(3, rows.length); i++) {
    const header = rows[i].map(cellText);
    if (header.includes("GameplayConfigId") && header.includes("NodeId")) {
      headerIndex = i;
      break;
    }
  }
  if (headerIndex < 0) {
    throw new Error(`No English header in ${excelPath}`);
  }

  const header = rows[headerIndex].map(cellText);
  const nodeCol = header.indexOf("NodeId");
  const configCol = header.indexOf("GameplayConfigId");
  let changed = 0;
  for (let r = headerIndex + 1; r < rows.length; r++) {
    const nodeId = cellText(rows[r][nodeCol]);
    if (cocIds[nodeId]) {
      rows[r][configCol] = cocIds[nodeId];
      changed += 1;
    }
  }
  if (changed !== 3) {
    throw new Error(`${excelPath} updated ${changed} COC rows, expected 3`);
  }

  const width = header.findIndex((cell) => cell === "");
  const colCount = width < 0 ? header.length : width;
  const en = header.slice(0, colCount);
  const data = [];
  for (let r = headerIndex + 1; r < rows.length; r++) {
    const row = [];
    let empty = true;
    for (let c = 0; c < colCount; c++) {
      const text = cellText(rows[r][c]);
      if (text) {
        empty = false;
      }
      row.push(text);
    }
    if (!empty) {
      data.push(row);
    }
  }

  const out = [rows[0].slice(0, colCount), rows[1].slice(0, colCount), en, ...data];
  const next = XLSX.utils.aoa_to_sheet(out);
  wb.Sheets[sheetName] = next;
  XLSX.writeFile(wb, excelPath);
  writeCsv(csvPath, en, data);
  console.log(`Patched ${path.basename(excelPath)} (${changed} COC ids)`);
}

for (const table of tables) {
  writeTable(table);
}

patchSandbox(
  path.join(root, "Excel", "关卡_沙盘节点表_Level_SandboxNodeConfig.xlsx"),
  path.join(root, "Csv", "Level_SandboxNodeConfig.csv")
);
patchSandbox(
  path.join(mode2Excel, "关卡_沙盘节点表_Level_SandboxNodeConfig.xlsx"),
  path.join(mode2Csv, "Level_SandboxNodeConfig.csv")
);
