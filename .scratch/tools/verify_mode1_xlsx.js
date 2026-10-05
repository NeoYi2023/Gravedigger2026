const XLSX = require('xlsx');
const p =
  'Gravedigger2026/Assets/ConfigTables/Excel/通用_常量表_Combat_CombatConstantConfig.xlsx';
const wb = XLSX.readFile(p);
const d = XLSX.utils.sheet_to_json(wb.Sheets[wb.SheetNames[0]], {
  header: 1,
  defval: ''
});
const nap = d.find((r) => r[0] === 'NormalAttackPrimaryMult');
const rel = d.find((r) => r[0] === 'NearestTargetBandRelative');
const slack = d.find((r) => r[0] === 'NearestTargetBandSlack');
console.log('NAP', JSON.stringify(nap));
console.log('REL', JSON.stringify(rel));
console.log('SLACK', JSON.stringify(slack));
console.log('last3', JSON.stringify(d.slice(-3)));
