# 配置表案例 — NewAgent 可粘贴需求正文

前置：已完成分片 0（[id-registry.md](id-registry.md)）。落盘约定见计划；表头用 SPEC §9 字段 EN；Excel+CSV 双交付；`openpyxl`/Node `xlsx` 均可。

工作区根：`f:\CursorGame_Git\Gravedigger2026`  
Unity 资源根：`Gravedigger2026/Assets/ConfigTables/`

---

## 分片 1 — Level + Dig

**标题：** 配置表案例 · 分片1 · Level+Dig（5表×10行，Excel+CSV）

**任务：**
1. 读 `SPEC_04_Technical.md` §9.1–§9.5、§14，以及 `.scratch/config-tables-sample/id-registry.md`
2. 生成以下 5 表（各 10 行），Excel + CSV 内容一致：
   - `关卡_关卡运作表_Level_LevelOperationConfig` / `Level_LevelOperationConfig`
   - `挖坟_挖坟配置表_Dig_DigGameplayConfig` / `Dig_DigGameplayConfig`
   - `挖坟_坟墓品质定义表_Dig_GraveQualityConfig` / `Dig_GraveQualityConfig`
   - `挖坟_材料配置表_Dig_MaterialConfig` / `Dig_MaterialConfig`
   - `挖坟_货币配置表_Dig_CurrencyConfig` / `Dig_CurrencyConfig`（须含 `Spirit`）
3. FK 仅用登记表 Id；LootDrop/GraveSpawnWeights 编码符合 SPEC
4. 不写 Unity C#、不改 SPEC（除非字段冲突）
5. 回复新建路径列表

---

## 分片 2 — Defend + Combat

**标题：** 配置表案例 · 分片2 · Defend+Combat（含失控仅4行）

**任务：**
1. 读 SPEC_04 §9.7、§9.18–§9.21、§14 + id-registry
2. 生成：
   - DefendGameplayConfig ×10
   - WaveSpawnConfig ×10
   - MonsterConfig ×10
   - LossOfControlConfig ×**4**（TierId 1–4 only）
   - SkillConfig ×10（仅骨架三列）
3. 磁盘名见 §9「磁盘名」；Excel+CSV
4. 验收同统一清单

---

## 分片 3 — Manufacture 核心

**标题：** 配置表案例 · 分片3 · Manufacture 核心（Class/Race/Soul/BodyPart/Appearance）

**任务：**
1. 读 SPEC_04 §9.9、§9.9b、§9.11–§9.13 + id-registry
2. 生成 Class / Race / Soul / BodyPart / BodyAppearance 各 10 行 Excel+CSV
3. BodyPartId 不得与 Material Id 冲突；Soul.ClassId → 登记表 Class；Appearance 每种族至多 1 个 IsFallback=1

---

## 分片 4 — Manufacture 扩展（依赖分片 3）

**标题：** 配置表案例 · 分片4 · Manufacture 扩展（升级/宝石/装备/后缀）

**任务：**
1. 确认分片 3 已落盘；读 §9.8、§9.10、§9.14、§9.15
2. 生成 ProtagonistLevel / Gem / ExtraEquipment / GemSuffixName 各 10 行
3. GemType 六类均出现；ComboKey 用登记表预登记的 10 个

---

## 分片 5 — Tech

**标题：** 配置表案例 · 分片5 · Tech（树+效果，10+10）

**任务：**
1. 读 SPEC_04 §9.16–§9.17 + id-registry
2. TechTreeConfig ×10：第一行 Root + InitiallyUnlocked=true；UnlockNextTechIds 成树
3. TechEffectConfig ×10：与 TechId 1:1；AttributeModifiers 用 DigDamage 等已文档化键
4. Excel+CSV 双交付

---

## 统一验收

- 路径：`Assets/ConfigTables/Excel/` + `Csv/`
- 表头 = SPEC 字段 EN（嵌套列保留点号）
- bool = `true`/`false`；资源占位即可
- 回复文件清单 + 与登记表偏差（若有）
