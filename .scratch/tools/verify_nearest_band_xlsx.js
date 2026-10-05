const XLSX = require('xlsx');
const p =
  'Gravedigger2026/Assets/ConfigTables/Mode2/Excel/通用_常量表_Combat_CombatConstantConfig.xlsx';
const wb = XLSX.readFile(p);
const d = XLSX.utils.sheet_to_json(wb.Sheets[wb.SheetNames[0]], {
  header: 1,
  defval: ''
});
for (let i = 0; i < 6; i++) {
  console.log(i, JSON.stringify(d[i]));
}
const hit = d.findIndex((r) => r[0] === 'HitConfirmSlack');
console.log('hit', hit);
console.log(JSON.stringify(d[hit]));
console.log(JSON.stringify(d[hit + 1]));
console.log(JSON.stringify(d[hit + 2]));
