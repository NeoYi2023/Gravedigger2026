using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.ProtagonistEquipment
{
    /// <summary>
    /// Save-scoped protagonist equipment warehouse (SPEC_03 §3.16 / SPEC_04 §6 / §9.25).
    /// Acquire / same-Id convert / common Exp spend / level-up; PlayerPrefs when bound.
    /// Dig caps: TechTreeService subscribes to <see cref="Changed"/> (PE-03).
    /// </summary>
    public sealed class ProtagonistEquipmentService
    {
        private readonly ConfigCsvRepository _configs;
        private readonly List<OwnedEquip> _owned = new List<OwnedEquip>();
        private int _equipCommonExp;
        private int _slotIndex = -1;
        private CampaignMode _campaignMode = CampaignMode.Mode1;

        public ProtagonistEquipmentService(ConfigCsvRepository configs)
        {
            _configs = configs ?? throw new ArgumentNullException(nameof(configs));
        }

        public int BoundSlotIndex => _slotIndex;
        public CampaignMode BoundCampaignMode => _campaignMode;
        public int EquipCommonExp => _equipCommonExp;
        public IReadOnlyList<OwnedEquip> OwnedEquips => _owned;

        /// <summary>Raised after bind/clear/mutate so Dig caps can recalc (PE-03).</summary>
        public event Action Changed;

        public void BindSlot(int slotIndex, CampaignMode campaignMode)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Slot index must be 0..2.");
            }

            _slotIndex = slotIndex;
            _campaignMode = campaignMode;
            _owned.Clear();
            _equipCommonExp = 0;

            _equipCommonExp = Mathf.Max(0, PlayerPrefs.GetInt(CommonExpKey(slotIndex, campaignMode), 0));

            var warehouseRaw = PlayerPrefs.GetString(WarehouseKey(slotIndex, campaignMode), string.Empty);
            if (!string.IsNullOrEmpty(warehouseRaw))
            {
                var data = JsonUtility.FromJson<ProtagonistEquipmentSaveData>(warehouseRaw);
                ApplyLoaded(data);
            }

            Debug.Log(
                $"[ProtagonistEquipment] Bound slot={slotIndex} mode={campaignMode} " +
                $"commonExp={_equipCommonExp} owned={_owned.Count}");
            Changed?.Invoke();
        }

        public void ClearBound()
        {
            _slotIndex = -1;
            _campaignMode = CampaignMode.Mode1;
            _owned.Clear();
            _equipCommonExp = 0;
            Changed?.Invoke();
        }

        public static void DeleteSlotData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 2)
            {
                return;
            }

            PlayerPrefs.DeleteKey(CommonExpKey(slotIndex, CampaignMode.Mode1));
            PlayerPrefs.DeleteKey(CommonExpKey(slotIndex, CampaignMode.Mode2));
            PlayerPrefs.DeleteKey(WarehouseKey(slotIndex, CampaignMode.Mode1));
            PlayerPrefs.DeleteKey(WarehouseKey(slotIndex, CampaignMode.Mode2));
            PlayerPrefs.Save();
        }

        public bool TryGetOwned(string equipId, out OwnedEquip owned)
        {
            owned = FindOwned(NormalizeId(equipId));
            return owned != null;
        }

        /// <summary>
        /// First acquire → Level=1 CurrentExp=0; duplicate → ConvertExpValue into CurrentExp (or common pool if maxed).
        /// </summary>
        public bool TryAcquire(string equipId, out string error)
        {
            error = null;
            if (_slotIndex < 0)
            {
                error = "No save slot bound.";
                return false;
            }

            var id = NormalizeId(equipId);
            if (id == null)
            {
                error = "EquipId is empty.";
                return false;
            }

            if (!_configs.TryGetProtagonistEquipment(id, 1, out var levelOne) || levelOne == null)
            {
                error = $"EquipId '{id}' not in ProtagonistEquipmentConfig (missing Level 1).";
                return false;
            }

            var existing = FindOwned(id);
            if (existing == null)
            {
                _owned.Add(new OwnedEquip
                {
                    EquipId = id,
                    Level = 1,
                    CurrentExp = 0
                });
                Persist();
                Debug.Log(
                    $"[ProtagonistEquipment] Acquire first '{id}' → L1 Exp0 " +
                    $"slot={_slotIndex} mode={_campaignMode}");
                Changed?.Invoke();
                return true;
            }

            if (!_configs.TryGetProtagonistEquipment(id, existing.Level, out var currentRow) || currentRow == null)
            {
                error = $"Owned '{id}' L{existing.Level} has no config row.";
                return false;
            }

            var convert = Mathf.Max(0, currentRow.ConvertExpValue);
            if (IsMaxLevel(existing, currentRow))
            {
                _equipCommonExp += convert;
                Persist();
                Debug.Log(
                    $"[ProtagonistEquipment] Acquire maxed '{id}' → EquipCommonExp+={convert} " +
                    $"(total={_equipCommonExp}) slot={_slotIndex} mode={_campaignMode}");
                Changed?.Invoke();
                return true;
            }

            existing.CurrentExp += convert;
            TryLevelUp(existing);
            Persist();
            Debug.Log(
                $"[ProtagonistEquipment] Acquire convert '{id}' +{convert} → L{existing.Level} Exp{existing.CurrentExp} " +
                $"slot={_slotIndex} mode={_campaignMode}");
            Changed?.Invoke();
            return true;
        }

        /// <summary>Demo GM: inject into EquipCommonExp (SPEC_03 §3.10 Dig HUD / D-059).</summary>
        public bool DebugGrantCommonExp(int amount, out string error)
        {
            error = null;
            if (_slotIndex < 0)
            {
                error = "No save slot bound.";
                return false;
            }

            if (amount <= 0)
            {
                error = "Grant amount must be > 0.";
                return false;
            }

            _equipCommonExp += amount;
            Persist();
            Debug.Log(
                $"[ProtagonistEquipment] GM GrantCommonExp +{amount} → total={_equipCommonExp} " +
                $"slot={_slotIndex} mode={_campaignMode}");
            Changed?.Invoke();
            return true;
        }

        /// <summary>Spend from EquipCommonExp into one owned piece's CurrentExp, then chain level-up.</summary>
        public bool TrySpendCommonExp(string equipId, int amount, out string error)
        {
            error = null;
            if (_slotIndex < 0)
            {
                error = "No save slot bound.";
                return false;
            }

            if (amount <= 0)
            {
                error = "Spend amount must be > 0.";
                return false;
            }

            var id = NormalizeId(equipId);
            if (id == null)
            {
                error = "EquipId is empty.";
                return false;
            }

            var owned = FindOwned(id);
            if (owned == null)
            {
                error = $"Equip '{id}' is not owned.";
                return false;
            }

            if (_equipCommonExp < amount)
            {
                error = $"EquipCommonExp {_equipCommonExp} < spend {amount}.";
                return false;
            }

            _equipCommonExp -= amount;
            owned.CurrentExp += amount;
            TryLevelUp(owned);
            Persist();
            Debug.Log(
                $"[ProtagonistEquipment] SpendCommonExp '{id}' amount={amount} → " +
                $"L{owned.Level} Exp{owned.CurrentExp} commonExp={_equipCommonExp} " +
                $"slot={_slotIndex} mode={_campaignMode}");
            Changed?.Invoke();
            return true;
        }

        private void TryLevelUp(OwnedEquip owned)
        {
            while (true)
            {
                if (!_configs.TryGetProtagonistEquipment(owned.EquipId, owned.Level, out var row) || row == null)
                {
                    break;
                }

                if (row.ExpToNextLevel <= 0)
                {
                    break;
                }

                if (!_configs.TryGetProtagonistEquipment(owned.EquipId, owned.Level + 1, out _))
                {
                    break;
                }

                if (owned.CurrentExp < row.ExpToNextLevel)
                {
                    break;
                }

                owned.CurrentExp -= row.ExpToNextLevel;
                owned.Level += 1;
            }
        }

        private bool IsMaxLevel(OwnedEquip owned, ProtagonistEquipmentConfigRow currentRow)
        {
            if (currentRow == null || currentRow.ExpToNextLevel <= 0)
            {
                return true;
            }

            return !_configs.TryGetProtagonistEquipment(owned.EquipId, owned.Level + 1, out _);
        }

        private OwnedEquip FindOwned(string equipId)
        {
            if (equipId == null)
            {
                return null;
            }

            for (var i = 0; i < _owned.Count; i++)
            {
                var e = _owned[i];
                if (e != null && string.Equals(e.EquipId, equipId, StringComparison.Ordinal))
                {
                    return e;
                }
            }

            return null;
        }

        private void Persist()
        {
            if (_slotIndex < 0)
            {
                return;
            }

            PlayerPrefs.SetInt(CommonExpKey(_slotIndex, _campaignMode), Mathf.Max(0, _equipCommonExp));

            var copy = new OwnedEquip[_owned.Count];
            for (var i = 0; i < _owned.Count; i++)
            {
                var src = _owned[i];
                copy[i] = new OwnedEquip
                {
                    EquipId = src.EquipId,
                    Level = src.Level,
                    CurrentExp = src.CurrentExp
                };
            }

            var data = new ProtagonistEquipmentSaveData { Equips = copy };
            PlayerPrefs.SetString(WarehouseKey(_slotIndex, _campaignMode), JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void ApplyLoaded(ProtagonistEquipmentSaveData data)
        {
            if (data?.Equips == null)
            {
                return;
            }

            for (var i = 0; i < data.Equips.Length; i++)
            {
                var dto = data.Equips[i];
                if (dto == null || string.IsNullOrEmpty(dto.EquipId))
                {
                    continue;
                }

                _owned.Add(new OwnedEquip
                {
                    EquipId = dto.EquipId.Trim(),
                    Level = Math.Max(1, dto.Level),
                    CurrentExp = Math.Max(0, dto.CurrentExp)
                });
            }
        }

        private static string NormalizeId(string equipId)
        {
            if (string.IsNullOrEmpty(equipId))
            {
                return null;
            }

            var id = equipId.Trim();
            return id.Length == 0 ? null : id;
        }

        private static string CommonExpKey(int slotIndex, CampaignMode mode)
        {
            return SaveSlotPrefsKeys.DataKey(slotIndex, mode, SaveSlotPrefsKeys.EquipCommonExpSuffix);
        }

        private static string WarehouseKey(int slotIndex, CampaignMode mode)
        {
            return SaveSlotPrefsKeys.DataKey(
                slotIndex, mode, SaveSlotPrefsKeys.ProtagonistEquipmentWarehouseSuffix);
        }
    }
}
