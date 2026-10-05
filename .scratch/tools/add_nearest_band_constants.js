const XLSX = require('xlsx');
const paths = [
  'Gravedigger2026/Assets/ConfigTables/Excel/通用_常量表_Combat_CombatConstantConfig.xlsx',
  'Gravedigger2026/Assets/ConfigTables/Mode2/Excel/通用_常量表_Combat_CombatConstantConfig.xlsx'
];
const rows = [
  [
    'NearestTargetBandRelative',
    '最近目标带相对系数',
    0.25,
    'Monster soldier nearest band relative; bandMax=dMin*(1+R)+Slack',
    '怪物士兵最近带相对系数；bandMax=dMin×(1+本值)+Slack'
  ],
  [
    'NearestTargetBandSlack',
    '最近目标带绝对松弛',
    0.5,
    'Monster soldier nearest band absolute slack (world units)',
    '怪物士兵最近带绝对加项（世界单位）'
  ]
];

for (const p of paths) {
  const wb = XLSX.readFile(p);
  const name = wb.SheetNames[0];
  const sheet = wb.Sheets[name];
  const data = XLSX.utils.sheet_to_json(sheet, { header: 1, defval: '' });
  const existing = new Set(data.map((r) => String(r[0] || '')));
  const missing = rows.filter((r) => !existing.has(r[0]));
  if (missing.length === 0) {
    console.log('skip', p);
    continue;
  }

  const range = XLSX.utils.decode_range(sheet['!ref']);
  const origin = { r: range.e.r + 1, c: 0 };
  XLSX.utils.sheet_add_aoa(sheet, missing, { origin });
  XLSX.writeFile(wb, p);
  console.log('appended', p, missing.map((r) => r[0]).join(','));
}
