using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.Dig;
using UnityEngine;

namespace Gravedigger2026.Core.UpgradeManufacture
{
    /// <summary>
    /// Soldier manufacture rules (SPEC_03 §3.11 「制造士兵」): strict slots, preview, Spirit gate,
    /// Race / BodyAppearance finalize, naming, warehouse deduction and WarriorInstance output.
    /// </summary>
    public sealed class ManufactureService
    {
        public const float DebugKitSpiritGrant = 500f;
        public const string ErrorMaterialInsufficient = "材料不足";
        public const string ErrorSpiritInsufficient = "精魂不足";
        public const string ErrorNoRecipe = "无再造配方";

        /// <summary>System default SoulConfig when manufacture Soul slot is empty (SPEC_03 §3.11).</summary>
        public const string DefaultSoulId = "Soul_00";

        /// <summary>Forced ClassId when manufacture Soul slot is empty (SPEC_03 §3.11).</summary>
        public const string NoSoulClassId = "Class_Servants";

        private static readonly ManufactureSlotKind[] SlotLayout =
        {
            ManufactureSlotKind.Head,
            ManufactureSlotKind.Torso,
            ManufactureSlotKind.Arm,
            ManufactureSlotKind.Arm,
            ManufactureSlotKind.Leg,
            ManufactureSlotKind.Leg,
            ManufactureSlotKind.Soul,
            ManufactureSlotKind.Gem,
            ManufactureSlotKind.Gem,
            ManufactureSlotKind.Gem,
            ManufactureSlotKind.Gem,
            ManufactureSlotKind.Gem,
            ManufactureSlotKind.Gem,
            ManufactureSlotKind.Mount,
            ManufactureSlotKind.Wing
        };

        private readonly ConfigCsvRepository _configs;
        private readonly WarehouseService _warehouse;
        private readonly WarriorPoolService _pool;
        private readonly System.Random _rng;
        private readonly List<ManufactureSlot> _slots = new List<ManufactureSlot>();

        private ManufacturePreview _preview;
        private bool _previewDirty = true;

        public ManufactureService(
            ConfigCsvRepository configs,
            WarehouseService warehouse,
            WarriorPoolService pool)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
            _warehouse = warehouse ?? throw new ArgumentNullException(nameof(warehouse));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _rng = new System.Random(Environment.TickCount);

            for (var i = 0; i < SlotLayout.Length; i++)
            {
                _slots.Add(new ManufactureSlot(SlotLayout[i]));
            }
        }

        public IReadOnlyList<ManufactureSlot> Slots => _slots;

        public event Action Changed;

        /// <summary>
        /// Visual BodyAppearance gate (SPEC_03 §3.11): Head+Torso+Arm×2+Leg×2+Mount+Wing filled.
        /// Soul and gems do not gate. Does not change manufacture commit requirements.
        /// </summary>
        public bool AreNonGemSlotsFilled()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Kind == ManufactureSlotKind.Gem
                    || slot.Kind == ManufactureSlotKind.Soul)
                {
                    continue;
                }

                if (slot.IsEmpty)
                {
                    return false;
                }
            }

            return true;
        }

        public void ClearAllSlots()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                _slots[i].ItemId = null;
            }

            MarkChanged();
        }

        public bool TryClearSlot(int index)
        {
            if (index < 0 || index >= _slots.Count || _slots[index].IsEmpty)
            {
                return false;
            }

            _slots[index].ItemId = null;
            MarkChanged();
            return true;
        }

        /// <summary>
        /// Routes an item to the first legal empty slot of its kind (SPEC_03 §3.11 「拖入」).
        /// </summary>
        public bool TryPlace(string itemId, out string error)
        {
            error = null;
            if (!TryValidatePlace(itemId, out var slotKind, out error))
            {
                return false;
            }

            var index = FindEmptySlot(slotKind);
            if (index < 0)
            {
                error = $"{DescribeSlotKind(slotKind)} 槽位已满";
                return false;
            }

            _slots[index].ItemId = itemId;
            MarkChanged();
            return true;
        }

        /// <summary>
        /// Places into a specific empty slot when kinds match (drag-drop onto a cell).
        /// </summary>
        public bool TryPlaceAt(int slotIndex, string itemId, out string error)
        {
            error = null;
            if (slotIndex < 0 || slotIndex >= _slots.Count)
            {
                error = "无效槽位";
                return false;
            }

            if (!TryValidatePlace(itemId, out var slotKind, out error))
            {
                return false;
            }

            var slot = _slots[slotIndex];
            if (slot.Kind != slotKind)
            {
                error = $"类型不符：需要 {DescribeSlotKind(slot.Kind)}";
                return false;
            }

            if (!slot.IsEmpty)
            {
                error = $"{DescribeSlotKind(slot.Kind)} 槽位已占用";
                return false;
            }

            slot.ItemId = itemId;
            MarkChanged();
            return true;
        }

        private bool TryValidatePlace(string itemId, out ManufactureSlotKind slotKind, out string error)
        {
            error = null;
            slotKind = ManufactureSlotKind.Head;
            if (string.IsNullOrEmpty(itemId))
            {
                error = "无效材料 Id";
                return false;
            }

            if (!TryResolveSlotKind(itemId, out slotKind))
            {
                error = $"未知材料 {itemId}（不属于躯体/灵魂/宝石/外置装备表）";
                return false;
            }

            if (GetAvailable(itemId) <= 0)
            {
                error = $"{itemId} 可用库存不足";
                return false;
            }

            if (slotKind == ManufactureSlotKind.Gem
                && _configs.TryGetGem(itemId, out var gem)
                && HasGemType(gem.GemType))
            {
                error = $"宝石类型 {gem.GemType} 已镶嵌（同类型互斥）";
                return false;
            }

            return true;
        }

        public ManufacturePreview GetPreview()
        {
            if (_previewDirty || _preview == null)
            {
                _preview = BuildPreview();
                _previewDirty = false;
            }

            return _preview;
        }

        public List<ManufactureInventoryEntry> BuildInventory()
        {
            var result = new List<ManufactureInventoryEntry>();
            foreach (var pair in _warehouse.Materials)
            {
                var itemId = pair.Key;
                if (!TryResolveSlotKind(itemId, out var slotKind))
                {
                    continue;
                }

                var available = pair.Value - CountPlaced(itemId);
                result.Add(new ManufactureInventoryEntry
                {
                    ItemId = itemId,
                    Label = DescribeItem(itemId, slotKind),
                    Available = available,
                    SlotKind = slotKind
                });
            }

            result.Sort((a, b) =>
            {
                var byKind = a.SlotKind.CompareTo(b.SlotKind);
                return byKind != 0 ? byKind : string.CompareOrdinal(a.ItemId, b.ItemId);
            });
            return result;
        }

        /// <summary>
        /// Commits manufacture: deducts placed items + Spirit, finalizes Race / Appearance / name,
        /// and pushes the WarriorInstance snapshot into the deployable pool.
        /// </summary>
        public bool TryManufacture(out WarriorInstance instance, out string error)
        {
            instance = null;
            var preview = GetPreview();
            if (!preview.CanManufacture)
            {
                error = preview.BlockReason;
                return false;
            }

            var sourceItemIds = CollectOccupiedItemIds();
            if (!TryConsumeRecipe(sourceItemIds, out var aggregate, out error))
            {
                return false;
            }

            instance = BuildWarriorFromAggregate(aggregate, sourceItemIds);
            _pool.Add(instance);
            ClearAllSlots();

            error = null;
            Debug.Log(
                $"[UM Manufacture] {instance.Id} '{instance.WarriorName}' Race={instance.RaceId} Class={instance.ClassId} " +
                $"Appearance={instance.AppearanceId} MaxHP={instance.RemainingHP} ControlCost={instance.ControlPowerCost} " +
                $"Spirit-{aggregate.SpiritCost:0.##} Gems={instance.GemIds.Count} Equips={instance.LockedEquipIds.Count} " +
                $"Skills={SoldierSkillGrant.FormatSummary(instance.SoldierSkills)}");
            return true;
        }

        /// <summary>
        /// Remakes a new warrior from an existing instance's recipe without mutating manufacture slots.
        /// </summary>
        public bool TryRemanufacture(string sourceWarriorId, out WarriorInstance instance, out string error)
        {
            instance = null;
            if (!_pool.TryGet(sourceWarriorId, out var source) || source == null)
            {
                error = "士兵不存在";
                return false;
            }

            if (source.SourceItemIds == null || source.SourceItemIds.Count == 0)
            {
                error = ErrorNoRecipe;
                return false;
            }

            if (!TryConsumeRecipe(source.SourceItemIds, out var aggregate, out error))
            {
                return false;
            }

            instance = BuildWarriorFromAggregate(aggregate, source.SourceItemIds);
            _pool.Add(instance);

            error = null;
            Debug.Log(
                $"[UM Remanufacture] from={sourceWarriorId} → {instance.Id} '{instance.WarriorName}' " +
                $"Race={instance.RaceId} Class={instance.ClassId} Appearance={instance.AppearanceId} " +
                $"Skills={SoldierSkillGrant.FormatSummary(instance.SoldierSkills)}");
            return true;
        }

        /// <summary>
        /// Legacy save repair (SPEC_04 §6): warriors whose StatBlock fields were dropped by JsonUtility
        /// (BaseStats all-zero) but still have SourceItemIds — rebuild Base/Equip/GemMult/RaceAdjust/BodyLife
        /// and persist. Preserves Id / RaceId / AppearanceId / RemainingHP / ControlPowerCost / names /
        /// SoldierSkills / VisualStyle* (must not clear baked skills or visual style).
        /// </summary>
        public int RepairMissingStatSnapshots()
        {
            var repaired = 0;
            var warriors = _pool.Warriors;
            for (var i = 0; i < warriors.Count; i++)
            {
                var warrior = warriors[i];
                if (!NeedsStatSnapshotRepair(warrior))
                {
                    continue;
                }

                if (!TryApplyRecipeStatSnapshot(warrior))
                {
                    Debug.LogWarning(
                        $"[UM SaveRepair] Failed to rebuild stats for {warrior.Id} " +
                        $"(SourceItemIds={warrior.SourceItemIds.Count}).");
                    continue;
                }

                repaired++;
            }

            if (repaired > 0)
            {
                _pool.NotifyMutated();
                Debug.Log($"[UM SaveRepair] Rebuilt StatBlock snapshots for {repaired} warrior(s).");
            }

            return repaired;
        }

        private static bool NeedsStatSnapshotRepair(WarriorInstance warrior)
        {
            return warrior != null
                   && warrior.BaseStats.IsAllZero
                   && warrior.SourceItemIds != null
                   && warrior.SourceItemIds.Count > 0;
        }

        private bool TryApplyRecipeStatSnapshot(WarriorInstance warrior)
        {
            var aggregate = AggregateFromItemIds(warrior.SourceItemIds);
            if (!TryApplyDefaultSoulIfMissing(aggregate, out _))
            {
                // Still repair body/equip even if default soul config is missing.
            }

            if (aggregate.Base.IsAllZero && aggregate.Equip.IsAllZero)
            {
                return false;
            }

            var raceId = !string.IsNullOrEmpty(warrior.RaceId)
                ? warrior.RaceId
                : PickRace(aggregate.RaceCandidates);
            _configs.TryGetRace(raceId, out var raceRow);
            var raceAdjust = raceRow != null ? raceRow.RaceAdjustCoeff : new StatBlock();

            warrior.BaseStats = aggregate.Base;
            warrior.EquipStats = aggregate.Equip;
            warrior.GemMult = aggregate.GemMult;
            warrior.RaceAdjustCoeff = raceAdjust;
            warrior.BodyLife = WarriorStatMath.ComputeBodyLife(aggregate.Base, aggregate.Equip);
            // SoldierSkills / VisualStyle: leave as loaded. Repair must not clear baked skills or visual style.

            if (warrior.GemIds.Count == 0 && aggregate.GemIds.Count > 0)
            {
                warrior.GemIds.AddRange(aggregate.GemIds);
            }

            if (warrior.LockedEquipIds.Count == 0 && aggregate.EquipIds.Count > 0)
            {
                warrior.LockedEquipIds.AddRange(aggregate.EquipIds);
            }

            if (aggregate.Soul != null)
            {
                if (string.IsNullOrEmpty(warrior.SoulId))
                {
                    warrior.SoulId = aggregate.Soul.SoulId;
                }

                if (string.IsNullOrEmpty(warrior.ClassId))
                {
                    warrior.ClassId = aggregate.UsedDefaultSoul
                        ? NoSoulClassId
                        : aggregate.Soul.ClassId;
                }

                warrior.AttackMode = aggregate.Soul.AttackMode;
            }

            return true;
        }

        /// <summary>
        /// Demo-only: grants one legal parts set + every SoulConfig row ×1 + Spirit so D-031 can be
        /// hand-verified before Soul / Gem / ExtraEquipment acquisition rules exist.
        /// </summary>
        public void GrantDebugStarterKit()
        {
            GrantFirstBodyPart(BodySlot.Head, 1);
            GrantFirstBodyPart(BodySlot.Torso, 1);
            GrantFirstBodyPart(BodySlot.Arm, 2);
            GrantFirstBodyPart(BodySlot.Leg, 2);

            // Demo Debug kit: grant placeable SoulConfig rows ×1 (skip system default Soul_00).
            foreach (var soul in _configs.Souls)
            {
                if (string.IsNullOrEmpty(soul.SoulId)
                    || string.Equals(soul.SoulId, DefaultSoulId, StringComparison.Ordinal))
                {
                    continue;
                }

                _warehouse.AddItem(soul.SoulId, 1);
            }

            var gemsByType = new Dictionary<GemType, string>();
            foreach (var gem in _configs.Gems)
            {
                if (!gemsByType.TryGetValue(gem.GemType, out var existing)
                    || string.CompareOrdinal(gem.GemId, existing) < 0)
                {
                    gemsByType[gem.GemType] = gem.GemId;
                }
            }

            for (var type = GemType.Ruby; type <= GemType.Diamond; type++)
            {
                if (gemsByType.TryGetValue(type, out var gemId))
                {
                    _warehouse.AddItem(gemId, 1);
                }
            }

            GrantFirstEquip(EquipSlot.Mount);
            GrantFirstEquip(EquipSlot.Wing);
            _warehouse.AddSpirit(DebugKitSpiritGrant);

            MarkChanged();
            Debug.Log($"[UM Manufacture] Debug 制造套件已注入；精魂 +{DebugKitSpiritGrant:0.##}。");
        }

        private void GrantFirstBodyPart(BodySlot slot, int count)
        {
            string picked = null;
            foreach (var part in _configs.BodyParts)
            {
                if (part.BodySlot != slot)
                {
                    continue;
                }

                if (picked == null || string.CompareOrdinal(part.BodyPartId, picked) < 0)
                {
                    picked = part.BodyPartId;
                }
            }

            if (picked != null)
            {
                _warehouse.AddItem(picked, count);
            }
        }

        private void GrantFirstEquip(EquipSlot slot)
        {
            string picked = null;
            foreach (var equip in _configs.ExtraEquipments)
            {
                if (equip.EquipSlot != slot)
                {
                    continue;
                }

                if (picked == null || string.CompareOrdinal(equip.EquipId, picked) < 0)
                {
                    picked = equip.EquipId;
                }
            }

            if (picked != null)
            {
                _warehouse.AddItem(picked, 1);
            }
        }

        private ManufacturePreview BuildPreview()
        {
            var aggregate = Aggregate();
            string defaultSoulError = null;
            var defaultSoulOk = TryApplyDefaultSoulIfMissing(aggregate, out defaultSoulError);

            var raceId = PickRace(aggregate.RaceCandidates);
            _configs.TryGetRace(raceId, out var raceRow);
            var raceAdjust = raceRow != null ? raceRow.RaceAdjustCoeff : new StatBlock();
            var className = aggregate.Class != null ? aggregate.Class.ClassName : string.Empty;
            var appearanceId = PickAppearance(
                ComputeAvgLevelInt(aggregate.BodyLevels),
                ref raceId,
                ref raceRow,
                ref raceAdjust,
                className);

            var staticStats = WarriorStatMath.ComputeStaticStats(
                aggregate.Base, aggregate.Equip, aggregate.GemMult, raceAdjust);
            var bodyLife = WarriorStatMath.ComputeBodyLife(aggregate.Base, aggregate.Equip);

            var minMet = aggregate.TorsoCount >= 1
                         && aggregate.ArmCount >= 2
                         && aggregate.LegCount >= 2;
            var spiritEnough = _warehouse.SpiritEssence >= aggregate.SpiritCost;

            string blockReason = null;
            if (!minMet)
            {
                blockReason = "最低要求未满足：躯干 1 + 手臂 2 + 腿 2";
            }
            else if (!defaultSoulOk)
            {
                blockReason = defaultSoulError;
            }
            else if (!spiritEnough)
            {
                blockReason =
                    $"精魂不足：需要 {aggregate.SpiritCost:0.##}，持有 {_warehouse.SpiritEssence:0.##}";
            }

            var preview = new ManufacturePreview
            {
                BaseStats = aggregate.Base,
                EquipStats = aggregate.Equip,
                GemMult = aggregate.GemMult,
                RaceAdjustCoeff = raceAdjust,
                StaticStats = staticStats,
                BodyLife = bodyLife,
                StaticMaxHP = WarriorStatMath.ComputeMaxHP(bodyLife, staticStats.Strength, _configs.GetMaxHpStrengthMult()),
                TotalSpiritCost = aggregate.SpiritCost,
                ControlPowerCost = aggregate.ControlPowerCost,
                TrialRaceId = raceId,
                TrialRaceDisplayName = ResolveRaceDisplayName(raceRow, raceId),
                TrialAppearanceId = appearanceId,
                ClassId = ResolveInstanceClassId(aggregate),
                ClassName = className,
                MinRequirementMet = minMet,
                SpiritEnough = spiritEnough,
                CanManufacture = minMet && defaultSoulOk && spiritEnough,
                BlockReason = blockReason
            };
            preview.TrialWarriorName = BuildWarriorName(aggregate, raceRow, raceId, className);
            return preview;
        }

        private SlotAggregate Aggregate()
        {
            return AggregateFromItemIds(CollectOccupiedItemIds());
        }

        private List<string> CollectOccupiedItemIds()
        {
            var ids = new List<string>();
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (!slot.IsEmpty && !string.IsNullOrEmpty(slot.ItemId))
                {
                    ids.Add(slot.ItemId);
                }
            }

            return ids;
        }

        private bool TryConsumeRecipe(
            IReadOnlyList<string> sourceItemIds,
            out SlotAggregate aggregate,
            out string error)
        {
            aggregate = AggregateFromItemIds(sourceItemIds);
            if (aggregate.TorsoCount < 1
                || aggregate.ArmCount < 2
                || aggregate.LegCount < 2)
            {
                error = ErrorMaterialInsufficient;
                return false;
            }

            if (!TryApplyDefaultSoulIfMissing(aggregate, out error))
            {
                return false;
            }

            var costs = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < sourceItemIds.Count; i++)
            {
                var id = sourceItemIds[i];
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                costs.TryGetValue(id, out var current);
                costs[id] = current + 1;
            }

            foreach (var pair in costs)
            {
                if (_warehouse.GetCount(pair.Key) < pair.Value)
                {
                    error = ErrorMaterialInsufficient;
                    return false;
                }
            }

            if (_warehouse.SpiritEssence < aggregate.SpiritCost
                || !_warehouse.TrySpendSpirit(aggregate.SpiritCost))
            {
                error = ErrorSpiritInsufficient;
                return false;
            }

            foreach (var pair in costs)
            {
                _warehouse.TryConsume(pair.Key, pair.Value);
            }

            error = null;
            return true;
        }

        private WarriorInstance BuildWarriorFromAggregate(
            SlotAggregate aggregate,
            IReadOnlyList<string> sourceItemIds)
        {
            var raceId = PickRace(aggregate.RaceCandidates);
            _configs.TryGetRace(raceId, out var raceRow);
            var raceAdjust = raceRow != null ? raceRow.RaceAdjustCoeff : new StatBlock();
            var classId = ResolveInstanceClassId(aggregate);
            var className = aggregate.Class != null ? aggregate.Class.ClassName : string.Empty;
            var appearanceId = PickAppearance(
                ComputeAvgLevelInt(aggregate.BodyLevels),
                ref raceId,
                ref raceRow,
                ref raceAdjust,
                className);

            var staticStats = WarriorStatMath.ComputeStaticStats(
                aggregate.Base, aggregate.Equip, aggregate.GemMult, raceAdjust);
            var bodyLife = WarriorStatMath.ComputeBodyLife(aggregate.Base, aggregate.Equip);
            var maxHp = WarriorStatMath.ComputeMaxHP(bodyLife, staticStats.Strength, _configs.GetMaxHpStrengthMult());

            var instance = new WarriorInstance
            {
                Id = _pool.ReserveNextId(),
                WarriorName = BuildWarriorName(aggregate, raceRow, raceId, className),
                RemainingHP = maxHp,
                RaceId = raceId,
                RaceAdjustCoeff = raceAdjust,
                BaseStats = aggregate.Base,
                AppearanceId = appearanceId,
                SoulId = aggregate.Soul.SoulId,
                ClassId = classId,
                AttackMode = aggregate.Soul.AttackMode,
                GemMult = aggregate.GemMult,
                ControlPowerCost = aggregate.ControlPowerCost,
                EquipStats = aggregate.Equip,
                BodyLife = bodyLife,
                SourceSpiritCost = aggregate.SpiritCost
            };
            instance.LockedEquipIds.AddRange(aggregate.EquipIds);
            instance.GemIds.AddRange(aggregate.GemIds);
            for (var i = 0; i < sourceItemIds.Count; i++)
            {
                if (!string.IsNullOrEmpty(sourceItemIds[i]))
                {
                    instance.SourceItemIds.Add(sourceItemIds[i]);
                }
            }

            SoldierSkillGrant.GrantDefaultSkillsAtLevel1(instance, _configs);
            return instance;
        }

        private SlotAggregate AggregateFromItemIds(IReadOnlyList<string> itemIds)
        {
            var aggregate = new SlotAggregate();
            if (itemIds == null)
            {
                return aggregate;
            }

            for (var i = 0; i < itemIds.Count; i++)
            {
                var itemId = itemIds[i];
                if (string.IsNullOrEmpty(itemId) || !TryResolveSlotKind(itemId, out var kind))
                {
                    continue;
                }

                ApplyItemToAggregate(aggregate, itemId, kind);
            }

            return aggregate;
        }

        /// <summary>
        /// When no Soul was slotted: apply Soul_00 costs/fields and force Class_Servants.
        /// Does not consume warehouse Soul_00.
        /// </summary>
        private bool TryApplyDefaultSoulIfMissing(SlotAggregate aggregate, out string error)
        {
            error = null;
            if (aggregate.Soul != null)
            {
                return true;
            }

            if (!_configs.TryGetSoul(DefaultSoulId, out var defaultSoul) || defaultSoul == null)
            {
                error = $"缺少默认灵魂配置 {DefaultSoulId}";
                return false;
            }

            if (!_configs.TryGetClass(NoSoulClassId, out var classRow) || classRow == null)
            {
                error = $"缺少职业配置 {NoSoulClassId}";
                return false;
            }

            aggregate.Soul = defaultSoul;
            aggregate.SpiritCost += defaultSoul.SpiritCost;
            aggregate.ControlPowerCost += defaultSoul.ControlPowerCost;
            aggregate.Class = classRow;
            aggregate.UsedDefaultSoul = true;
            return true;
        }

        private static string ResolveInstanceClassId(SlotAggregate aggregate)
        {
            if (aggregate == null)
            {
                return null;
            }

            if (aggregate.UsedDefaultSoul)
            {
                return NoSoulClassId;
            }

            return aggregate.Soul != null ? aggregate.Soul.ClassId : null;
        }

        private void ApplyItemToAggregate(SlotAggregate aggregate, string itemId, ManufactureSlotKind kind)
        {
            switch (kind)
            {
                case ManufactureSlotKind.Head:
                case ManufactureSlotKind.Torso:
                case ManufactureSlotKind.Arm:
                case ManufactureSlotKind.Leg:
                    if (_configs.TryGetBodyPart(itemId, out var part))
                    {
                        aggregate.Base.Add(part.StatBonus);
                        aggregate.SpiritCost += part.SpiritCost;
                        aggregate.ControlPowerCost += part.ControlPowerCost;
                        aggregate.BodyLevels.Add(part.BodyLevel);
                        aggregate.RaceCandidates.Add(part.RaceId);
                        if (kind == ManufactureSlotKind.Torso)
                        {
                            aggregate.TorsoCount++;
                        }
                        else if (kind == ManufactureSlotKind.Arm)
                        {
                            aggregate.ArmCount++;
                        }
                        else if (kind == ManufactureSlotKind.Leg)
                        {
                            aggregate.LegCount++;
                        }
                    }

                    break;
                case ManufactureSlotKind.Soul:
                    if (_configs.TryGetSoul(itemId, out var soul))
                    {
                        aggregate.Soul = soul;
                        aggregate.SpiritCost += soul.SpiritCost;
                        aggregate.ControlPowerCost += soul.ControlPowerCost;
                        if (_configs.TryGetClass(soul.ClassId, out var classRow))
                        {
                            aggregate.Class = classRow;
                        }
                    }

                    break;
                case ManufactureSlotKind.Gem:
                    if (_configs.TryGetGem(itemId, out var gem))
                    {
                        aggregate.GemMult.Add(gem.GemMult);
                        aggregate.SpiritCost += gem.SpiritCost;
                        aggregate.ControlPowerCost += gem.ControlPowerCost;
                        aggregate.GemIds.Add(gem.GemId);
                        aggregate.GemTypes.Add(gem.GemType);
                    }

                    break;
                case ManufactureSlotKind.Mount:
                case ManufactureSlotKind.Wing:
                    if (_configs.TryGetExtraEquipment(itemId, out var equip))
                    {
                        aggregate.Equip.Add(equip.EquipStats);
                        aggregate.SpiritCost += equip.SpiritCost;
                        aggregate.ControlPowerCost += equip.ControlPowerCost;
                        aggregate.EquipIds.Add(equip.EquipId);
                        if (!string.IsNullOrEmpty(equip.NamePrefix))
                        {
                            aggregate.NamePrefixes.Add(equip.NamePrefix);
                        }
                    }

                    break;
            }
        }

        private string BuildWarriorName(
            SlotAggregate aggregate,
            RaceConfigRow raceRow,
            string raceId,
            string className)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < aggregate.NamePrefixes.Count; i++)
            {
                sb.Append(aggregate.NamePrefixes[i]);
            }

            sb.Append(ResolveRaceDisplayName(raceRow, raceId));
            sb.Append(className ?? string.Empty);
            sb.Append(ResolveGemSuffix(aggregate.GemTypes));
            return sb.ToString();
        }

        private string ResolveGemSuffix(List<GemType> gemTypes)
        {
            if (gemTypes == null || gemTypes.Count == 0)
            {
                return string.Empty;
            }

            var names = new List<string>(gemTypes.Count);
            for (var i = 0; i < gemTypes.Count; i++)
            {
                names.Add(gemTypes[i].ToString());
            }

            names.Sort(StringComparer.Ordinal);
            var comboKey = string.Join("|", names.ToArray());
            return _configs.TryGetGemSuffix(comboKey, out var suffix) ? suffix ?? string.Empty : string.Empty;
        }

        private static string ResolveRaceDisplayName(RaceConfigRow raceRow, string raceId)
        {
            if (raceRow == null)
            {
                return raceId ?? string.Empty;
            }

            // i18n is off: SPEC_04 §9.11 allows using DisplayNameKey directly as the display string.
            return string.IsNullOrEmpty(raceRow.DisplayNameKey) ? raceRow.RaceId : raceRow.DisplayNameKey;
        }

        /// <summary>Mode1: same-race else Race_Undead (SPEC_03 §3.11). No MagicBook Restore.</summary>
        private static string PickRace(List<string> candidates)
        {
            return RaceResolve.ResolveDefaultRace(candidates);
        }

        private static int ComputeAvgLevelInt(List<float> bodyLevels)
        {
            if (bodyLevels == null || bodyLevels.Count == 0)
            {
                return 0;
            }

            var sum = 0d;
            for (var i = 0; i < bodyLevels.Count; i++)
            {
                sum += bodyLevels[i];
            }

            var oneDecimal = Math.Round(sum / bodyLevels.Count, 1, MidpointRounding.AwayFromZero);
            return (int)Math.Round(oneDecimal, 0, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Appearance pick; if set A empty, rewrite race to Race_Undead once then re-pick (SPEC_03 §3.11).
        /// Class mismatch (B empty) keeps IsFallback — does not rewrite race.
        /// </summary>
        private string PickAppearance(
            int avgLevelInt,
            ref string raceId,
            ref RaceConfigRow raceRow,
            ref StatBlock raceAdjust,
            string className)
        {
            return PickAppearanceCore(
                avgLevelInt,
                ref raceId,
                ref raceRow,
                ref raceAdjust,
                className,
                allowUndeadRewrite: true);
        }

        private string PickAppearanceCore(
            int avgLevelInt,
            ref string raceId,
            ref RaceConfigRow raceRow,
            ref StatBlock raceAdjust,
            string className,
            bool allowUndeadRewrite)
        {
            var all = _configs.BodyAppearances;
            if (all == null || all.Count == 0 || string.IsNullOrEmpty(raceId))
            {
                return null;
            }

            var setA = new List<BodyAppearanceConfigRow>();
            for (var i = 0; i < all.Count; i++)
            {
                var row = all[i];
                if (row.AppearanceLevel == avgLevelInt && string.Equals(row.RaceId, raceId, StringComparison.Ordinal))
                {
                    setA.Add(row);
                }
            }

            if (setA.Count == 0)
            {
                if (allowUndeadRewrite
                    && !string.Equals(raceId, RaceResolve.UndeadRaceId, StringComparison.Ordinal))
                {
                    raceId = RaceResolve.UndeadRaceId;
                    _configs.TryGetRace(raceId, out raceRow);
                    raceAdjust = raceRow != null ? raceRow.RaceAdjustCoeff : new StatBlock();
                    return PickAppearanceCore(
                        avgLevelInt,
                        ref raceId,
                        ref raceRow,
                        ref raceAdjust,
                        className,
                        allowUndeadRewrite: false);
                }

                var undeadFallback = TryPickRaceFallback(all, raceId);
                if (!string.IsNullOrEmpty(undeadFallback))
                {
                    return undeadFallback;
                }

                return all[_rng.Next(all.Count)].AppearanceId;
            }

            var setB = new List<BodyAppearanceConfigRow>();
            for (var i = 0; i < setA.Count; i++)
            {
                if (HasClassAffinity(setA[i].ClassAffinity, className))
                {
                    setB.Add(setA[i]);
                }
            }

            if (setB.Count > 0)
            {
                return setB[_rng.Next(setB.Count)].AppearanceId;
            }

            // Class mismatch: do not use unmatched set A — race IsFallback instead (SPEC_03 §3.11).
            var fallbackId = TryPickRaceFallback(all, raceId);
            if (!string.IsNullOrEmpty(fallbackId))
            {
                return fallbackId;
            }

            return all[_rng.Next(all.Count)].AppearanceId;
        }

        private static string TryPickRaceFallback(IReadOnlyList<BodyAppearanceConfigRow> all, string raceId)
        {
            for (var i = 0; i < all.Count; i++)
            {
                var row = all[i];
                if (row.IsFallback && string.Equals(row.RaceId, raceId, StringComparison.Ordinal))
                {
                    return row.AppearanceId;
                }
            }

            return null;
        }

        private static bool HasClassAffinity(string classAffinity, string className)
        {
            if (string.IsNullOrEmpty(classAffinity) || string.IsNullOrEmpty(className))
            {
                return false;
            }

            var parts = classAffinity.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                if (string.Equals(parts[i].Trim(), className, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryResolveSlotKind(string itemId, out ManufactureSlotKind slotKind)
        {
            if (_configs.TryGetBodyPart(itemId, out var part))
            {
                switch (part.BodySlot)
                {
                    case BodySlot.Head:
                        slotKind = ManufactureSlotKind.Head;
                        return true;
                    case BodySlot.Torso:
                        slotKind = ManufactureSlotKind.Torso;
                        return true;
                    case BodySlot.Arm:
                        slotKind = ManufactureSlotKind.Arm;
                        return true;
                    default:
                        slotKind = ManufactureSlotKind.Leg;
                        return true;
                }
            }

            if (_configs.TryGetSoul(itemId, out _))
            {
                slotKind = ManufactureSlotKind.Soul;
                return true;
            }

            if (_configs.TryGetGem(itemId, out _))
            {
                slotKind = ManufactureSlotKind.Gem;
                return true;
            }

            if (_configs.TryGetExtraEquipment(itemId, out var equip))
            {
                slotKind = equip.EquipSlot == EquipSlot.Mount
                    ? ManufactureSlotKind.Mount
                    : ManufactureSlotKind.Wing;
                return true;
            }

            slotKind = ManufactureSlotKind.Head;
            return false;
        }

        private string DescribeItem(string itemId, ManufactureSlotKind slotKind)
        {
            switch (slotKind)
            {
                case ManufactureSlotKind.Head:
                case ManufactureSlotKind.Torso:
                case ManufactureSlotKind.Arm:
                case ManufactureSlotKind.Leg:
                    if (_configs.TryGetBodyPart(itemId, out var part))
                    {
                        return
                            $"{itemId}｜{DescribeSlotKind(slotKind)}｜{part.RaceId}｜Lv{part.BodyLevel.ToString(CultureInfo.InvariantCulture)}｜精魂{part.SpiritCost:0.##}";
                    }

                    break;
                case ManufactureSlotKind.Soul:
                    if (_configs.TryGetSoul(itemId, out var soul))
                    {
                        var className = _configs.TryGetClass(soul.ClassId, out var classRow)
                            ? classRow.ClassName
                            : soul.ClassId;
                        return $"{itemId}｜灵魂｜{className}｜{soul.AttackMode}｜精魂{soul.SpiritCost:0.##}";
                    }

                    break;
                case ManufactureSlotKind.Gem:
                    if (_configs.TryGetGem(itemId, out var gem))
                    {
                        return $"{itemId}｜宝石｜{gem.GemType}｜精魂{gem.SpiritCost:0.##}";
                    }

                    break;
                default:
                    if (_configs.TryGetExtraEquipment(itemId, out var equip))
                    {
                        return
                            $"{itemId}｜{DescribeSlotKind(slotKind)}｜{equip.NamePrefix}｜精魂{equip.SpiritCost:0.##}";
                    }

                    break;
            }

            return itemId;
        }

        public static string DescribeSlotKind(ManufactureSlotKind kind)
        {
            switch (kind)
            {
                case ManufactureSlotKind.Head: return "头部";
                case ManufactureSlotKind.Torso: return "躯干";
                case ManufactureSlotKind.Arm: return "手臂";
                case ManufactureSlotKind.Leg: return "腿部";
                case ManufactureSlotKind.Soul: return "灵魂";
                case ManufactureSlotKind.Gem: return "宝石";
                case ManufactureSlotKind.Mount: return "坐骑";
                default: return "翅膀";
            }
        }

        private bool HasGemType(GemType gemType)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Kind != ManufactureSlotKind.Gem || slot.IsEmpty)
                {
                    continue;
                }

                if (_configs.TryGetGem(slot.ItemId, out var gem) && gem.GemType == gemType)
                {
                    return true;
                }
            }

            return false;
        }

        private int FindEmptySlot(ManufactureSlotKind kind)
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Kind == kind && _slots[i].IsEmpty)
                {
                    return i;
                }
            }

            return -1;
        }

        private int CountPlaced(string itemId)
        {
            var count = 0;
            for (var i = 0; i < _slots.Count; i++)
            {
                if (string.Equals(_slots[i].ItemId, itemId, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private int GetAvailable(string itemId)
        {
            return _warehouse.GetCount(itemId) - CountPlaced(itemId);
        }

        private void MarkChanged()
        {
            _previewDirty = true;
            Changed?.Invoke();
        }

        private sealed class SlotAggregate
        {
            public StatBlock Base;
            public StatBlock Equip;
            public StatBlock GemMult;
            public float SpiritCost;
            public float ControlPowerCost;
            public SoulConfigRow Soul;
            public ClassConfigRow Class;
            public bool UsedDefaultSoul;
            public int TorsoCount;
            public int ArmCount;
            public int LegCount;

            public readonly List<float> BodyLevels = new List<float>();
            public readonly List<string> RaceCandidates = new List<string>();
            public readonly List<string> GemIds = new List<string>();
            public readonly List<GemType> GemTypes = new List<GemType>();
            public readonly List<string> EquipIds = new List<string>();
            public readonly List<string> NamePrefixes = new List<string>();
        }
    }
}
