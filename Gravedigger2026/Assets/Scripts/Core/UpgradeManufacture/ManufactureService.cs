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
            if (string.IsNullOrEmpty(itemId))
            {
                error = "无效材料 Id";
                return false;
            }

            if (!TryResolveSlotKind(itemId, out var slotKind))
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

            var costs = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.IsEmpty)
                {
                    continue;
                }

                costs.TryGetValue(slot.ItemId, out var current);
                costs[slot.ItemId] = current + 1;
            }

            foreach (var pair in costs)
            {
                if (_warehouse.GetCount(pair.Key) < pair.Value)
                {
                    error = $"{pair.Key} 库存不足，制造中止";
                    return false;
                }
            }

            var aggregate = Aggregate();
            if (!_warehouse.TrySpendSpirit(aggregate.SpiritCost))
            {
                error = "精魂不足，制造中止";
                return false;
            }

            foreach (var pair in costs)
            {
                _warehouse.TryConsume(pair.Key, pair.Value);
            }

            var raceId = PickRace(aggregate.RaceCandidates);
            _configs.TryGetRace(raceId, out var raceRow);
            var raceAdjust = raceRow != null ? raceRow.RaceAdjustCoeff : new StatBlock();
            var className = aggregate.Class != null ? aggregate.Class.ClassName : string.Empty;
            var appearanceId = PickAppearance(ComputeAvgLevelInt(aggregate.BodyLevels), raceId, className);

            var staticStats = WarriorStatMath.ComputeStaticStats(
                aggregate.Base, aggregate.Equip, aggregate.GemMult, raceAdjust);
            var bodyLife = WarriorStatMath.ComputeBodyLife(aggregate.Base, aggregate.Equip);
            var maxHp = WarriorStatMath.ComputeMaxHP(bodyLife, staticStats.Strength);

            instance = new WarriorInstance
            {
                Id = _pool.ReserveNextId(),
                WarriorName = BuildWarriorName(aggregate, raceRow, raceId, className),
                RemainingHP = maxHp,
                RaceId = raceId,
                RaceAdjustCoeff = raceAdjust,
                BaseStats = aggregate.Base,
                AppearanceId = appearanceId,
                SoulId = aggregate.Soul.SoulId,
                ClassId = aggregate.Soul.ClassId,
                AttackMode = aggregate.Soul.AttackMode,
                GemMult = aggregate.GemMult,
                ControlPowerCost = aggregate.ControlPowerCost,
                EquipStats = aggregate.Equip,
                BodyLife = bodyLife
            };
            instance.LockedEquipIds.AddRange(aggregate.EquipIds);
            instance.GemIds.AddRange(aggregate.GemIds);

            _pool.Add(instance);
            ClearAllSlots();

            error = null;
            Debug.Log(
                $"[UM Manufacture] {instance.Id} '{instance.WarriorName}' Race={instance.RaceId} Class={instance.ClassId} " +
                $"Appearance={instance.AppearanceId} MaxHP={maxHp} ControlCost={instance.ControlPowerCost} " +
                $"Spirit-{aggregate.SpiritCost:0.##} Gems={instance.GemIds.Count} Equips={instance.LockedEquipIds.Count}");
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

            // Demo Debug kit: grant every SoulConfig row ×1 (sample Soul_01…Soul_10).
            foreach (var soul in _configs.Souls)
            {
                if (!string.IsNullOrEmpty(soul.SoulId))
                {
                    _warehouse.AddItem(soul.SoulId, 1);
                }
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
            var raceId = PickRace(aggregate.RaceCandidates);
            _configs.TryGetRace(raceId, out var raceRow);
            var raceAdjust = raceRow != null ? raceRow.RaceAdjustCoeff : new StatBlock();
            var className = aggregate.Class != null ? aggregate.Class.ClassName : string.Empty;

            var staticStats = WarriorStatMath.ComputeStaticStats(
                aggregate.Base, aggregate.Equip, aggregate.GemMult, raceAdjust);
            var bodyLife = WarriorStatMath.ComputeBodyLife(aggregate.Base, aggregate.Equip);

            var minMet = aggregate.TorsoCount >= 1
                         && aggregate.ArmCount >= 2
                         && aggregate.LegCount >= 2
                         && aggregate.Soul != null;
            var spiritEnough = _warehouse.SpiritEssence >= aggregate.SpiritCost;

            string blockReason = null;
            if (!minMet)
            {
                blockReason = "最低要求未满足：躯干 1 + 手臂 2 + 腿 2 + 灵魂 1";
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
                StaticMaxHP = WarriorStatMath.ComputeMaxHP(bodyLife, staticStats.Strength),
                TotalSpiritCost = aggregate.SpiritCost,
                ControlPowerCost = aggregate.ControlPowerCost,
                TrialRaceId = raceId,
                TrialRaceDisplayName = ResolveRaceDisplayName(raceRow, raceId),
                TrialAppearanceId = PickAppearance(ComputeAvgLevelInt(aggregate.BodyLevels), raceId, className),
                ClassId = aggregate.Soul != null ? aggregate.Soul.ClassId : null,
                ClassName = className,
                MinRequirementMet = minMet,
                SpiritEnough = spiritEnough,
                CanManufacture = minMet && spiritEnough,
                BlockReason = blockReason
            };
            preview.TrialWarriorName = BuildWarriorName(aggregate, raceRow, raceId, className);
            return preview;
        }

        private SlotAggregate Aggregate()
        {
            var aggregate = new SlotAggregate();
            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.IsEmpty)
                {
                    continue;
                }

                switch (slot.Kind)
                {
                    case ManufactureSlotKind.Head:
                    case ManufactureSlotKind.Torso:
                    case ManufactureSlotKind.Arm:
                    case ManufactureSlotKind.Leg:
                        if (_configs.TryGetBodyPart(slot.ItemId, out var part))
                        {
                            aggregate.Base.Add(part.StatBonus);
                            aggregate.SpiritCost += part.SpiritCost;
                            aggregate.ControlPowerCost += part.ControlPowerCost;
                            aggregate.BodyLevels.Add(part.BodyLevel);
                            aggregate.RaceCandidates.Add(part.RaceId);
                            if (slot.Kind == ManufactureSlotKind.Torso)
                            {
                                aggregate.TorsoCount++;
                            }
                            else if (slot.Kind == ManufactureSlotKind.Arm)
                            {
                                aggregate.ArmCount++;
                            }
                            else if (slot.Kind == ManufactureSlotKind.Leg)
                            {
                                aggregate.LegCount++;
                            }
                        }

                        break;
                    case ManufactureSlotKind.Soul:
                        if (_configs.TryGetSoul(slot.ItemId, out var soul))
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
                        if (_configs.TryGetGem(slot.ItemId, out var gem))
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
                        if (_configs.TryGetExtraEquipment(slot.ItemId, out var equip))
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

            return aggregate;
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

        private string PickRace(List<string> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            return candidates[_rng.Next(candidates.Count)];
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

        private string PickAppearance(int avgLevelInt, string raceId, string className)
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

            if (setA.Count > 0)
            {
                var setB = new List<BodyAppearanceConfigRow>();
                for (var i = 0; i < setA.Count; i++)
                {
                    if (HasClassAffinity(setA[i].ClassAffinity, className))
                    {
                        setB.Add(setA[i]);
                    }
                }

                var pool = setB.Count > 0 ? setB : setA;
                return pool[_rng.Next(pool.Count)].AppearanceId;
            }

            for (var i = 0; i < all.Count; i++)
            {
                var row = all[i];
                if (row.IsFallback && string.Equals(row.RaceId, raceId, StringComparison.Ordinal))
                {
                    return row.AppearanceId;
                }
            }

            return all[_rng.Next(all.Count)].AppearanceId;
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
