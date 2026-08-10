# -*- coding: utf-8 -*-
"""One-shot: rewrite ConfigTables Excel to 3-row headers (SPEC_04 §14.4 Approach B).

Row1 = ZH name, Row2 = ZH notes, Row3 = EN columns, Row4+ = CSV data.
Uses CSV as data source of truth. Run: py -3.11 migrate_config_excel_3row_headers.py
"""
from __future__ import annotations

import csv
import re
from pathlib import Path

from openpyxl import Workbook

ROOT = Path(__file__).resolve().parents[2] / "Gravedigger2026" / "Assets" / "ConfigTables"
EXCEL_DIR = ROOT / "Excel"
CSV_DIR = ROOT / "Csv"

# EN -> (ZH name, ZH notes). Authority: SPEC_04 §9. Truncated notes stay informative.
FIELDS: dict[str, dict[str, tuple[str, str]]] = {
    "Level_LevelOperationConfig": {
        "LevelId": ("关卡ID", "同 ID 多行 = 该关全部阶段"),
        "StageNumber": ("阶段编号", "同关卡内升序执行；建议同关卡内唯一"),
        "GameplayType": ("玩法类型", "如 Dig / UpgradeManufacture / Defend / PushMap"),
        "GameplayConfigId": (
            "玩法配置ID",
            "Dig→DigGameplayConfig；Defend→RecommendedConfigId；PushMap→PushMapGameplayConfig；UM→忽略",
        ),
    },
    "Dig_DigGameplayConfig": {
        "GameplayConfigId": ("玩法配置ID", "主键；被关卡运作表引用"),
        "DigMapId": ("挖坟地图ID", "Prefab 逻辑名；合法 Ground_01…Ground_05 → Prefabs/Maps/{Id}.prefab"),
        "LevelDurationSeconds": ("关卡时长限制", "基础时长（秒）；有效倒计时 = 本字段 + DigStageDurationBonus"),
        "InitialGraveCount": ("开局基础生成坟墓数量", "开局独立加权随机次数 N（≥0）"),
        "SpawnRate": ("倒计时过程中生成坟墓速率", "编码 N;M：每 N 秒生成 M 个"),
        "GraveSpawnWeights": ("坟墓出现概率权重", "编码 QualityId;Weight|…；Weight=0 剔除"),
    },
    "Dig_GraveQualityConfig": {
        "QualityId": ("坟墓品质ID", "主键；被 GraveSpawnWeights 引用"),
        "MaxHP": ("总血量", "生成时初始化坟的 maxHP / 当前 HP"),
        "LootDrop": ("掉落内容", "挖掘成功（HP=0）时产出；编码 Id_Count|…"),
        "IconStyleHighId": ("高血量图标ID", "剩余 HP%>65%；空=品质默认"),
        "IconStyleMidId": ("中血量图标ID", "剩余 HP% 30%–65%；空=默认"),
        "IconStyleLowId": ("低血量图标ID", "剩余 HP%<30%；空=默认"),
    },
    "Dig_MaterialConfig": {
        "MaterialId": ("材料ID", "主键；被 LootDrop 引用"),
        "AutoConvert": ("自动兑换", "堆叠超上限时每 1 个超出材料兑换的精魂数量（≥0）"),
        "AppearanceIconId": ("外观图ID", "仓库 / DigReward 等 UI 用的外观图 Id"),
        "AssetPath": ("素材路径", "主素材资源路径"),
        "WarehouseQualityOutlineId": ("仓库品质外轮廓ID", "仓库格子专用品质外轮廓美术资源 Id"),
    },
    "Dig_CurrencyConfig": {
        "CurrencyId": ("货币ID", "主键；精魂保留 Id 为 Spirit"),
        "AppearanceIconId": ("外观图ID", "UI 外观图 Id"),
        "AssetPath": ("素材路径", "主素材资源路径"),
        "WarehouseQualityOutlineId": ("仓库品质外轮廓ID", "若货币以同类格子展示，复用仓库格品质外轮廓资源 Id"),
    },
    "Defend_DefendGameplayConfig": {
        "GameplayConfigId": ("玩法配置ID", "主键；被关卡运作表 / ModeSelect 引用"),
        "BattleMapId": ("战斗地图ID", "Prefab 逻辑名；合法 Ground_01…Ground_05 → Prefabs/Maps/{Id}.prefab"),
        "WaveConfigId": ("波次配置ID", "FK → WaveSpawnConfig 分组键"),
        "CombatDurationSeconds": ("战斗总时长", "开战倒计时初值（整秒）；剩余秒用于刷怪激活"),
        "TargetRetargetIntervalSeconds": ("目标修正间隔", "怪物与士兵重算可攻击目的地的间隔（秒）；默认 1"),
    },
    "Defend_WaveSpawnConfig": {
        "WaveConfigId": ("波次配置ID", "分组键；同 ID 多行 = 该防守配置的全部刷怪事件"),
        "SpawnOrder": ("出怪顺序", "仅当多行 SpawnRemainingSeconds 相同时按升序"),
        "SpawnRemainingSeconds": ("出怪时间（剩余秒）", "RemainingCombatSeconds == 本字段时触发"),
        "MonsterId": ("怪物ID", "FK → MonsterConfig"),
        "SpawnCount": ("出现数量", "≥1"),
        "AppearLocation": ("出现位置", "InsideMap | OutsideMap"),
        "SpawnMode": ("出怪方式", "RegionRandom | ClockDirection"),
        "SpawnClockHour": ("几点钟方向", "1–12；仅 ClockDirection 有效"),
    },
    "Defend_MonsterConfig": {
        "MonsterId": ("怪物ID", "主键；被 WaveSpawnConfig / PushMapSpawnConfig 引用"),
        "ModelId": ("怪物模型ID", "→ Prefabs/Defend/Monsters/{ModelId}.prefab"),
        "DisplayName": ("怪物名称", "展示名或本地化 Key"),
        "TargetSelect": ("目标选择", "Nearest | PreferWarrior | PreferProtagonist"),
        "AttackMode": ("攻击模式", "Melee | Ranged"),
        "AggroMode": ("仇恨模式", "ActiveChase|PassiveChase|StationaryActive|StationaryPassive；空→ActiveChase"),
        "AlertRadius": ("警戒半径", "≥0；空→AttackRange"),
        "MaxHP": ("怪物血量", "生成时初始化 maxHP / 当前 HP"),
        "MoveSpeed": ("怪物移动速度", "世界单位/秒或项目统一速度单位"),
        "AttackPower": ("怪物攻击力", "仅攻击士兵时用于伤害结算"),
        "AttackSpeed": ("攻击速度", "攻击频率"),
        "AttackRange": ("攻击距离", "进入攻击态距离"),
        "MeleeWindupSeconds": ("近战前摇", "≥0 秒；Melee 时用"),
        "RangedProjectileSpeed": ("远程弹速", "≥0；Ranged 时用"),
        "RangedTimeoutSeconds": ("远程超时", "≥0 秒；超时未命中→未命中"),
        "Skills": ("怪物技能", "SkillId_CdSeconds|…；Demo v1 不施放"),
        "LootDrop": ("怪物掉落", "击杀产出；编码同 Dig LootDrop"),
    },
    "PushMap_PushMapGameplayConfig": {
        "GameplayConfigId": ("玩法配置ID", "主键；被 LevelOperationConfig / ModeSelect 引用"),
        "MapId": ("地图编号", "合法 Ground_01…05 或 PushMap_* → Prefabs/Maps/{MapId}.prefab"),
        "DisplayName": ("关卡显示名", "展示名或本地化 Key"),
        "StageExpReward": ("阶段经验奖励", "BOSS 通关入账 LifetimeExperience"),
        "CaptureLoot": ("占领默认掉落", "可选；编码 Id_Count|…"),
        "DungeonUnlockIds": ("副本解锁ID列表", "通关或占领写入存档钩子；段分隔 |；空=无"),
        "CaptureSeconds": ("占领所需秒", "加载缺省 5；判定圈连续无怪秒数"),
        "Notes": ("备注", "可选"),
    },
    "PushMap_PushMapSpawnConfig": {
        "GameplayConfigId": ("玩法配置ID", "FK → PushMapGameplayConfig"),
        "SpawnPointId": ("刷怪点编号", "与地图 Prefab SpawnPoint 标记匹配"),
        "MonsterId": ("怪物ID", "FK → MonsterConfig"),
        "SpawnCount": ("数量", "≥1"),
        "LinkedObjectiveOrder": ("关联目标点序号", "该目标占领后本行停刷；空/0=全局点"),
        "TrapZoneId": ("陷阱区域编号", "空=无陷阱开战刷；非空=忠诚士兵进入才刷"),
        "IsBoss": ("是否BOSS", "1=BossPoint 通关目标"),
        "SpawnOrder": ("同点出怪顺序", "同 SpawnPointId 多行时升序"),
    },
    "Manufacture_ProtagonistLevelConfig": {
        "Level": ("当前等级值", "主键；≥1；同表内唯一"),
        "RequiredTotalExperience": ("升到本级需要的经验总值", "生涯累计阈值（非本级增量）；1 级行通常为 0"),
        "UnlockedFeatureIds": ("升到本级解锁的功能", "仅预留；本版无运行时解锁逻辑；编码 FeatureId|…"),
        "TechPointsReward": ("升到本级奖励的科技点数", "首次进入该等级时发放（≥0）"),
        "ControlPowerCap": ("升到本级控制力上限变成的值", "该等级下控制力上限绝对值"),
        "ProtagonistMaxHP": ("升到本级主角的生命值上限", "Defend 开战时作护盾上限（Shield 初值）"),
    },
    "Manufacture_SoulConfig": {
        "SoulId": ("灵魂ID", "主键"),
        "ClassId": ("职业ID", "必填；FK → ClassConfig；制造时写入士兵实例"),
        "AttackMode": ("攻击模式", "Melee | Ranged；士兵普攻命中方案 D 分支"),
        "Skills": ("技能列表", "SkillId;Level|…"),
        "AttackPriority": ("攻击优先级", "Nearest | PreferWarrior | PreferProtagonist 等"),
        "MoveStyle": ("移动风格", "士兵移动表现/AI 风格占位"),
        "SpiritCost": ("精魂消耗", "制造时计入总精魂消耗（≥0）"),
        "ControlPowerCost": ("控制力占用", "对士兵 ControlPowerCost 的贡献（≥0）"),
    },
    "Manufacture_ClassConfig": {
        "ClassId": ("职业ID", "主键；被 SoulConfig.ClassId 引用"),
        "ClassName": ("职业名", "参与 WarriorName；外观 ClassAffinity 精确匹配键"),
        "PrimaryStat": ("主属性", "Strength | Agility | Intelligence"),
        "CombatConvertCoeffs": ("战斗换算系数", "键_数值|…；缺键回退全局默认"),
        "AttackRange": ("攻击距离", "进入攻击态距离"),
        "MeleeWindupSeconds": ("近战前摇", "≥0 秒；Melee 时用"),
        "RangedProjectileSpeed": ("远程弹速", "≥0；Ranged 时用"),
        "RangedTimeoutSeconds": ("远程超时", "≥0 秒"),
    },
    "Manufacture_GemConfig": {
        "GemId": ("宝石ID", "主键"),
        "GemType": ("宝石类型", "Ruby|Sapphire|Emerald|Topaz|Amethyst|Diamond；同类型互斥"),
        "GemMult.MaxHP": ("生命值放大系数", "缺省 0；Base(MaxHP)×本系数"),
        "GemMult.MoveSpeed": ("移动速度放大系数", "缺省 0"),
        "GemMult.Strength": ("力量放大系数", "缺省 0"),
        "GemMult.Agility": ("敏捷放大系数", "缺省 0"),
        "GemMult.Intelligence": ("智力放大系数", "缺省 0"),
        "Skills": ("额外技能", "SkillId;Level|…"),
        "SpiritCost": ("精魂消耗", "制造时计入总精魂消耗（≥0；缺省 0）"),
        "ControlPowerCost": ("控制力占用", "对士兵 ControlPowerCost 的贡献（≥0）"),
        "LossOfControlChanceBonus": ("失控概率加成", "可正可负；缺省 0；多宝石求和"),
    },
    "Manufacture_RaceConfig": {
        "RaceId": ("种族ID", "主键"),
        "DisplayNameKey": ("显示名Key", "展示名或本地化 Key"),
        "RaceAdjustCoeff.MaxHP": ("生命值种族系数", "种族对 MaxHP 的调整系数"),
        "RaceAdjustCoeff.MoveSpeed": ("移速种族系数", "种族对 MoveSpeed 的调整系数"),
        "RaceAdjustCoeff.Strength": ("力量种族系数", "种族对 Strength 的调整系数"),
        "RaceAdjustCoeff.Agility": ("敏捷种族系数", "种族对 Agility 的调整系数"),
        "RaceAdjustCoeff.Intelligence": ("智力种族系数", "种族对 Intelligence 的调整系数"),
        "LossOfControlChanceBonus": ("失控概率加成", "可正可负；缺省 0"),
    },
    "Manufacture_BodyPartConfig": {
        "BodyPartId": ("躯体部件ID", "主键；与 MaterialId 同命名空间不得冲突"),
        "BodyLevel": ("部件等级", "部件品质/等级"),
        "BodySlot": ("部件槽位", "Head|Torso|Arm|Leg 等"),
        "RaceId": ("种族倾向", "可选；影响外观抽取"),
        "ControlPowerCost": ("控制力占用", "≥0"),
        "SpiritCost": ("精魂消耗", "≥0"),
        "StatBonus": ("属性加成", "编码见 SPEC；计入 Base(S)"),
        "AutoConvert": ("自动兑换", "堆叠超上限兑精魂"),
        "Description": ("描述", "展示说明"),
        "ArtAssetId": ("美术资源ID", "仓库 / DigReward 外观可用"),
    },
    "Manufacture_BodyAppearanceConfig": {
        "AppearanceId": ("外观ID", "主键；→ Prefabs/Defend/Warriors/{AppearanceId}.prefab"),
        "AppearanceLevel": ("外观等级", "外观档次"),
        "RaceId": ("种族ID", "匹配士兵定稿种族"),
        "ClassAffinity": ("职业倾向", "精确匹配 ClassConfig.ClassName；可多值编码"),
        "Description": ("描述", "展示说明"),
        "IsFallback": ("是否兜底", "1=兜底外观"),
    },
    "Manufacture_ExtraEquipmentConfig": {
        "EquipId": ("装备ID", "主键"),
        "EquipSlot": ("装备槽位", "Mount|Wing 等"),
        "NamePrefix": ("名称前缀", "参与 WarriorName 前缀"),
        "SpiritCost": ("精魂消耗", "≥0"),
        "ControlPowerCost": ("控制力占用", "≥0"),
        "EquipStats": ("装备属性", "编码见 SPEC；计入 Equip"),
        "Skills": ("额外技能", "SkillId;Level|…"),
    },
    "Manufacture_GemSuffixNameConfig": {
        "ComboKey": ("组合键", "宝石类型组合键；主键"),
        "Suffix": ("后缀名", "WarriorName 后缀"),
    },
    "Tech_TechTreeConfig": {
        "TechId": ("科技ID", "主键"),
        "IconId": ("图标ID", "科技树节点图标"),
        "DisplayName": ("显示名", "节点标题"),
        "EffectDescription": ("效果描述", "悬停展示"),
        "UnlockNextTechIds": ("解锁后续科技", "段分隔；正向边"),
        "InitiallyUnlocked": ("初始已解锁", "1=进档自动学会"),
        "LearnCost": ("学习消耗", "科技点"),
        "TechUiFrameType": ("UI框类型", "三态框色等表现键"),
    },
    "Tech_TechEffectConfig": {
        "TechId": ("科技ID", "主键；与 TechTreeConfig 对齐"),
        "AttributeModifiers": ("属性修正", "加法求和写入 DigProtagonistCapabilities"),
        "UnlockedFeatureSystemName": ("解锁功能系统名", "写入 UnlockedFeatureSystems；可空"),
    },
    "Combat_LossOfControlConfig": {
        "TierId": ("失控段ID", "1|2|3|4；主键"),
        "DisplayName": ("显示名", "段标题"),
        "Description": ("描述", "段说明"),
        "LossOfControlChance": ("失控概率", "[0,1]；作为 TierChance"),
    },
    "Combat_SkillConfig": {
        "SkillId": ("技能ID", "复合主键之一；被灵魂/宝石/装备 Skills 引用"),
        "SkillLevel": ("技能等级", "复合主键之一；≥1；与 Skills 段内 Level 对齐"),
        "CooldownMode": ("技能CD模式", "Mode1=开战起算CD；Mode2=开战CD=0，释放后再进CD"),
        "CastTarget": ("技能释放目标", "Self|AllySingle|AllyAll|EnemySingle|EnemyAll|GroundPoint|CurrentNormalAttackTarget"),
        "ExtraActivationCondition": ("额外激活条件", "默认空；编码后续专题"),
        "DisplayName": ("技能名称", "展示文字或本地化 Key"),
        "Description": ("技能展示描述", "展示文字或本地化 Key"),
        "SkillEffectId": ("技能效果ID", "FK → SkillEffectConfig.SkillEffectId"),
        "BaseCooldownSeconds": ("基础冷却", "≥0 秒；实际 CD 见 §3.12 公式"),
        "LossOfControlChanceBonus": ("失控概率加成", "可正可负；缺省 0；按等级查行后求和"),
    },
    "Combat_SkillEffectConfig": {
        "SkillEffectId": ("技能效果ID", "主键；被 SkillConfig.SkillEffectId 引用"),
        "Notes": ("备注", "可选；可空；策划备注不驱动规则"),
    },
}


def excel_basename_to_csv_base(excel_stem: str) -> str | None:
    parts = excel_stem.split("_")
    if len(parts) != 4:
        return None
    return f"{parts[2]}_{parts[3]}"


def read_csv_rows(csv_path: Path) -> tuple[list[str], list[list[str]]]:
    with csv_path.open("r", encoding="utf-8-sig", newline="") as f:
        reader = csv.reader(f)
        rows = list(reader)
    if not rows:
        raise SystemExit(f"empty csv: {csv_path}")
    header = rows[0]
    data = [r for r in rows[1:] if any(c.strip() for c in r)]
    return header, data


def zh_for(csv_base: str, en: str) -> tuple[str, str]:
    meta = FIELDS.get(csv_base, {})
    if en in meta:
        return meta[en]
    # Fallback: keep EN as ZH name so bake still works; note flags missing SPEC entry.
    return en, f"（待补 SPEC 中文说明）{en}"


def write_xlsx(path: Path, zh_names: list[str], zh_notes: list[str], en_header: list[str], data: list[list[str]]) -> None:
    wb = Workbook()
    ws = wb.active
    ws.title = "Sheet1"
    ws.append(zh_names)
    ws.append(zh_notes)
    ws.append(en_header)
    col_count = len(en_header)
    for row in data:
        padded = list(row) + [""] * max(0, col_count - len(row))
        ws.append(padded[:col_count])
    path.parent.mkdir(parents=True, exist_ok=True)
    wb.save(path)


def main() -> None:
    if not EXCEL_DIR.is_dir() or not CSV_DIR.is_dir():
        raise SystemExit(f"missing dirs: {EXCEL_DIR} / {CSV_DIR}")

    xlsx_files = sorted(
        p for p in EXCEL_DIR.glob("*.xlsx") if not p.name.startswith("~$")
    )
    if not xlsx_files:
        raise SystemExit("no xlsx files")

    missing_fields: list[str] = []
    for xlsx in xlsx_files:
        csv_base = excel_basename_to_csv_base(xlsx.stem)
        if not csv_base:
            raise SystemExit(f"bad excel name: {xlsx.name}")
        csv_path = CSV_DIR / f"{csv_base}.csv"
        if not csv_path.is_file():
            raise SystemExit(f"missing csv for {xlsx.name}: {csv_path}")

        en_header, data = read_csv_rows(csv_path)
        zh_names: list[str] = []
        zh_notes: list[str] = []
        for en in en_header:
            zh, note = zh_for(csv_base, en)
            if csv_base not in FIELDS or en not in FIELDS[csv_base]:
                missing_fields.append(f"{csv_base}.{en}")
            zh_names.append(zh)
            zh_notes.append(note)

        write_xlsx(xlsx, zh_names, zh_notes, en_header, data)
        print(f"OK {xlsx.name} → 3-row header ({len(en_header)} cols, {len(data)} data rows)")

    if missing_fields:
        print("WARN missing FIELDS entries (used fallback):")
        for m in missing_fields:
            print(f"  {m}")
    else:
        print("All columns mapped from FIELDS dict.")


if __name__ == "__main__":
    main()
