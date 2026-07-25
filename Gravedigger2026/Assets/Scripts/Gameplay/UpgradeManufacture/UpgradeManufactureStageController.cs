using System;
using System.Collections.Generic;
using System.Text;
using Gravedigger2026.Core.UpgradeManufacture;
using UnityEngine;

namespace Gravedigger2026.Gameplay.UpgradeManufacture
{
    /// <summary>
    /// UM stage presentation: upgrade (D-030) + manufacture (D-031) + formation (D-032).
    /// </summary>
    public sealed class UpgradeManufactureStageController : MonoBehaviour
    {
        private const int PoolPreviewLines = 4;

        [SerializeField] private UpgradeManufacturePrefabCatalog _catalog;
        [SerializeField] private UpgradePanelView _upgradePanel;
        [SerializeField] private ManufacturePanelView _manufacturePanel;
        [SerializeField] private FormationPanelView _formationPanel;

        private readonly List<string> _inventoryLabels = new List<string>();
        private readonly List<string> _inventoryIds = new List<string>();
        private readonly List<string> _slotLabels = new List<string>();
        private readonly List<string> _poolLabels = new List<string>();
        private readonly List<string> _poolIds = new List<string>();
        private readonly List<string> _formationLabels = new List<string>();
        private readonly List<string> _formationIds = new List<string>();

        private ProtagonistProgressService _progress;
        private ManufactureService _manufacture;
        private WarriorPoolService _warriorPool;
        private BattleFormationService _formation;
        private Action _onComplete;
        private string _selectedWarriorId;
        private bool _active;

        public void ConfigureCatalog(UpgradeManufacturePrefabCatalog catalog)
        {
            if (catalog != null)
            {
                _catalog = catalog;
            }
        }

        public void Begin(
            ProtagonistProgressService progress,
            ManufactureService manufacture,
            WarriorPoolService warriorPool,
            BattleFormationService formation,
            Action onComplete)
        {
            End();
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _manufacture = manufacture;
            _warriorPool = warriorPool;
            _formation = formation ?? throw new ArgumentNullException(nameof(formation));
            _onComplete = onComplete;
            _selectedWarriorId = null;
            _active = true;

            _progress.Changed += HandleProgressChanged;
            if (_upgradePanel != null)
            {
                _upgradePanel.Inject100Requested += HandleInject100;
                _upgradePanel.Inject500Requested += HandleInject500;
                _upgradePanel.CompleteRequested += HandleComplete;
                _upgradePanel.Show();
            }

            if (_manufacturePanel != null && _manufacture != null)
            {
                _manufacture.Changed += HandleManufactureChanged;
                _manufacturePanel.ItemPlaceRequested += HandleItemPlaceRequested;
                _manufacturePanel.SlotClearRequested += HandleSlotClearRequested;
                _manufacturePanel.GrantKitRequested += HandleGrantKitRequested;
                _manufacturePanel.ClearSlotsRequested += HandleClearSlotsRequested;
                _manufacturePanel.ManufactureRequested += HandleManufactureRequested;
            }

            if (_warriorPool != null)
            {
                _warriorPool.Changed += HandlePoolOrFormationChanged;
            }

            _formation.Changed += HandlePoolOrFormationChanged;
            if (_formationPanel != null)
            {
                _formationPanel.DeployRequested += HandleDeployRequested;
                _formationPanel.SelectRequested += HandleSelectRequested;
                _formationPanel.UndeployRequested += HandleUndeployRequested;
                _formationPanel.NudgeNegXRequested += HandleNudgeNegX;
                _formationPanel.NudgePosXRequested += HandleNudgePosX;
                _formationPanel.NudgeNegZRequested += HandleNudgeNegZ;
                _formationPanel.NudgePosZRequested += HandleNudgePosZ;
            }

            RefreshStatus();
            RefreshManufacture();
            RefreshFormation();
        }

        public void End()
        {
            if (!_active)
            {
                return;
            }

            if (_progress != null)
            {
                _progress.Changed -= HandleProgressChanged;
            }

            if (_upgradePanel != null)
            {
                _upgradePanel.Inject100Requested -= HandleInject100;
                _upgradePanel.Inject500Requested -= HandleInject500;
                _upgradePanel.CompleteRequested -= HandleComplete;
                _upgradePanel.Hide();
            }

            if (_manufacturePanel != null && _manufacture != null)
            {
                _manufacture.Changed -= HandleManufactureChanged;
                _manufacturePanel.ItemPlaceRequested -= HandleItemPlaceRequested;
                _manufacturePanel.SlotClearRequested -= HandleSlotClearRequested;
                _manufacturePanel.GrantKitRequested -= HandleGrantKitRequested;
                _manufacturePanel.ClearSlotsRequested -= HandleClearSlotsRequested;
                _manufacturePanel.ManufactureRequested -= HandleManufactureRequested;
            }

            if (_warriorPool != null)
            {
                _warriorPool.Changed -= HandlePoolOrFormationChanged;
            }

            if (_formation != null)
            {
                _formation.Changed -= HandlePoolOrFormationChanged;
            }

            if (_formationPanel != null)
            {
                _formationPanel.DeployRequested -= HandleDeployRequested;
                _formationPanel.SelectRequested -= HandleSelectRequested;
                _formationPanel.UndeployRequested -= HandleUndeployRequested;
                _formationPanel.NudgeNegXRequested -= HandleNudgeNegX;
                _formationPanel.NudgePosXRequested -= HandleNudgePosX;
                _formationPanel.NudgeNegZRequested -= HandleNudgeNegZ;
                _formationPanel.NudgePosZRequested -= HandleNudgePosZ;
            }

            _progress = null;
            _manufacture = null;
            _warriorPool = null;
            _formation = null;
            _onComplete = null;
            _selectedWarriorId = null;
            _active = false;
        }

        private void HandleInject100()
        {
            Inject(100);
        }

        private void HandleInject500()
        {
            Inject(500);
        }

        private void Inject(long amount)
        {
            if (_progress == null)
            {
                return;
            }

            var gained = _progress.AddExperience(amount);
            Debug.Log(
                $"[UM Upgrade] Debug +{amount} Exp → Lifetime={_progress.LifetimeExperience} Level={_progress.Level} (+{gained}) Tech={_progress.TechPoints} Cap={_progress.ControlPowerCap} MaxHP={_progress.ProtagonistMaxHP}");
        }

        private void HandleComplete()
        {
            if (_formation != null)
            {
                var sb = new StringBuilder();
                sb.Append($"[UM Formation] Complete snapshot count={_formation.Entries.Count}");
                for (var i = 0; i < _formation.Entries.Count; i++)
                {
                    var e = _formation.Entries[i];
                    sb.Append($" | {e.WarriorId}@({e.PositionX:0.##},{e.PositionZ:0.##}) HP={e.RemainingHP:0}");
                }

                Debug.Log(sb.ToString());
            }

            _onComplete?.Invoke();
        }

        private void HandleProgressChanged()
        {
            RefreshStatus();
            RefreshFormation();
        }

        private void HandleManufactureChanged()
        {
            RefreshManufacture();
        }

        private void HandlePoolOrFormationChanged()
        {
            RefreshManufacture();
            RefreshFormation();
        }

        private void HandleItemPlaceRequested(string itemId)
        {
            if (_manufacture == null)
            {
                return;
            }

            if (!_manufacture.TryPlace(itemId, out var error))
            {
                Debug.Log($"[UM Manufacture] 放入失败：{error}");
                RefreshManufacture();
            }
        }

        private void HandleSlotClearRequested(int slotIndex)
        {
            _manufacture?.TryClearSlot(slotIndex);
        }

        private void HandleGrantKitRequested()
        {
            _manufacture?.GrantDebugStarterKit();
        }

        private void HandleClearSlotsRequested()
        {
            _manufacture?.ClearAllSlots();
        }

        private void HandleManufactureRequested()
        {
            if (_manufacture == null)
            {
                return;
            }

            if (!_manufacture.TryManufacture(out var instance, out var error))
            {
                Debug.Log($"[UM Manufacture] 制造失败：{error}");
                RefreshManufacture();
                return;
            }

            if (_catalog != null && !_catalog.TryGetWarriorAppearance(instance.AppearanceId, out _))
            {
                Debug.LogWarning(
                    $"[UM Manufacture] 外观 Prefab 未绑定：Assets/Prefabs/Defend/Warriors/{instance.AppearanceId}.prefab");
            }
        }

        private void HandleDeployRequested(string warriorId)
        {
            if (_formation == null || string.IsNullOrEmpty(warriorId))
            {
                return;
            }

            if (!_formation.TryDeploy(warriorId, out var error))
            {
                Debug.Log($"[UM Formation] 上阵失败：{error}");
                RefreshFormation();
                return;
            }

            _selectedWarriorId = warriorId;
            Debug.Log($"[UM Formation] 上阵 {warriorId}");
        }

        private void HandleSelectRequested(string warriorId)
        {
            if (string.IsNullOrEmpty(warriorId))
            {
                return;
            }

            _selectedWarriorId = warriorId;
            RefreshFormation();
        }

        private void HandleUndeployRequested()
        {
            if (_formation == null || string.IsNullOrEmpty(_selectedWarriorId))
            {
                return;
            }

            var id = _selectedWarriorId;
            if (!_formation.TryUndeploy(id, out var error))
            {
                Debug.Log($"[UM Formation] 下阵失败：{error}");
                RefreshFormation();
                return;
            }

            _selectedWarriorId = null;
            Debug.Log($"[UM Formation] 下阵 {id}");
        }

        private void HandleNudgeNegX()
        {
            Nudge(-BattleFormationService.DefaultNudgeStep, 0f);
        }

        private void HandleNudgePosX()
        {
            Nudge(BattleFormationService.DefaultNudgeStep, 0f);
        }

        private void HandleNudgeNegZ()
        {
            Nudge(0f, -BattleFormationService.DefaultNudgeStep);
        }

        private void HandleNudgePosZ()
        {
            Nudge(0f, BattleFormationService.DefaultNudgeStep);
        }

        private void Nudge(float dx, float dz)
        {
            if (_formation == null || string.IsNullOrEmpty(_selectedWarriorId))
            {
                return;
            }

            if (!_formation.TryNudge(_selectedWarriorId, dx, dz, out var error))
            {
                Debug.Log($"[UM Formation] 改位失败：{error}");
            }
        }

        private void RefreshStatus()
        {
            if (_upgradePanel == null || _progress == null)
            {
                return;
            }

            var next = _progress.IsMaxLevel
                ? "已满级"
                : $"下一级累计阈值 {_progress.GetNextRequiredTotalExperience()}";
            _upgradePanel.SetStatus(
                $"升级区\n" +
                $"等级 Level = {_progress.Level}\n" +
                $"生涯经验 LifetimeExperience = {_progress.LifetimeExperience}\n" +
                $"科技点 TechPoints = {_progress.TechPoints}\n" +
                $"控制力上限 ControlPowerCap = {_progress.ControlPowerCap}\n" +
                $"护盾上限语义 ProtagonistMaxHP = {_progress.ProtagonistMaxHP}\n" +
                $"{next}");
        }

        private void RefreshManufacture()
        {
            if (_manufacturePanel == null || _manufacture == null)
            {
                return;
            }

            _inventoryLabels.Clear();
            _inventoryIds.Clear();
            var inventory = _manufacture.BuildInventory();
            for (var i = 0; i < inventory.Count; i++)
            {
                var entry = inventory[i];
                _inventoryLabels.Add($"×{entry.Available}  {entry.Label}");
                _inventoryIds.Add(entry.ItemId);
            }

            if (_inventoryLabels.Count == 0)
            {
                _inventoryLabels.Add("（无可用制造材料，可先挖坟或注入 Debug 套件）");
                _inventoryIds.Add(string.Empty);
            }

            _manufacturePanel.SetInventoryLines(_inventoryLabels, _inventoryIds);

            _slotLabels.Clear();
            var slots = _manufacture.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var name = ManufactureService.DescribeSlotKind(slot.Kind);
                _slotLabels.Add(slot.IsEmpty ? $"{name}：（空）" : $"{name}：{slot.ItemId}");
            }

            _manufacturePanel.SetSlotLines(_slotLabels);

            var preview = _manufacture.GetPreview();
            _manufacturePanel.SetPreviewText(BuildPreviewText(preview));
            _manufacturePanel.SetManufactureInteractable(preview.CanManufacture);
            _manufacturePanel.SetPoolText(BuildPoolText());
        }

        private void RefreshFormation()
        {
            if (_formationPanel == null || _formation == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_selectedWarriorId) && !_formation.IsDeployed(_selectedWarriorId))
            {
                _selectedWarriorId = null;
            }

            _poolLabels.Clear();
            _poolIds.Clear();
            if (_warriorPool != null)
            {
                var warriors = _warriorPool.Warriors;
                for (var i = 0; i < warriors.Count; i++)
                {
                    var w = warriors[i];
                    if (_formation.IsDeployed(w.Id))
                    {
                        continue;
                    }

                    _poolLabels.Add($"{w.Id} {w.WarriorName}｜控 {w.ControlPowerCost:0.##}");
                    _poolIds.Add(w.Id);
                }
            }

            if (_poolLabels.Count == 0)
            {
                _poolLabels.Add("（无可上阵士兵：先制造）");
                _poolIds.Add(string.Empty);
            }

            _formationPanel.SetPoolLines(_poolLabels, _poolIds);

            _formationLabels.Clear();
            _formationIds.Clear();
            var entries = _formation.Entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var mark = string.Equals(e.WarriorId, _selectedWarriorId, StringComparison.Ordinal) ? "▶ " : string.Empty;
                var name = e.WarriorId;
                if (_warriorPool != null)
                {
                    for (var w = 0; w < _warriorPool.Warriors.Count; w++)
                    {
                        if (string.Equals(_warriorPool.Warriors[w].Id, e.WarriorId, StringComparison.Ordinal))
                        {
                            name = $"{e.WarriorId} {_warriorPool.Warriors[w].WarriorName}";
                            break;
                        }
                    }
                }

                _formationLabels.Add($"{mark}{name} @({e.PositionX:0.##},{e.PositionZ:0.##}) HP={e.RemainingHP:0}");
                _formationIds.Add(e.WarriorId);
            }

            if (_formationLabels.Count == 0)
            {
                _formationLabels.Add("（布阵为空：点击左侧士兵上阵）");
                _formationIds.Add(string.Empty);
            }

            _formationPanel.SetFormationLines(_formationLabels, _formationIds);

            var cap = _progress != null ? _progress.ControlPowerCap : 0;
            var used = _formation.SumControlPowerCost();
            var degree = _formation.ComputeLossOfControlDegree(cap);
            var degreeText = float.IsInfinity(degree)
                ? "∞"
                : degree.ToString("0.##");
            var selected = "未选中上阵士兵（点右侧列表）";
            if (!string.IsNullOrEmpty(_selectedWarriorId) && _formation.TryGetEntry(_selectedWarriorId, out var sel))
            {
                selected =
                    $"选中 {_selectedWarriorId} @({sel.PositionX:0.##},{sel.PositionZ:0.##}) 步进±{BattleFormationService.DefaultNudgeStep:0}";
            }

            _formationPanel.SetStatusText(
                $"布阵区（连续坐标 · 与 Prepare 共用 BattleFormation）\n" +
                $"控制力占用 {used:0.##} / Cap {cap}｜Degree={degreeText}\n" +
                $"{selected}");
            _formationPanel.SetActionInteractable(!string.IsNullOrEmpty(_selectedWarriorId));
        }

        private static string BuildPreviewText(ManufacturePreview preview)
        {
            var sb = new StringBuilder();
            sb.AppendLine("制造区预览（StaticStat；不含运行时 Buff）");
            sb.AppendLine(
                $"生命 {preview.StaticStats.MaxHP:0.##}｜移速 {preview.StaticStats.MoveSpeed:0.##}｜力量 {preview.StaticStats.Strength:0.##}｜敏捷 {preview.StaticStats.Agility:0.##}｜智力 {preview.StaticStats.Intelligence:0.##}");
            sb.AppendLine($"BodyLife {preview.BodyLife:0.##}｜静态 MaxHP {preview.StaticMaxHP}");
            sb.AppendLine(
                $"精魂消耗 {preview.TotalSpiritCost:0.##}｜控制力占用 {preview.ControlPowerCost:0.##}");
            sb.AppendLine(
                $"试算种族 {preview.TrialRaceId ?? "-"}｜职业 {(string.IsNullOrEmpty(preview.ClassName) ? "-" : preview.ClassName)}｜外观 {preview.TrialAppearanceId ?? "-"}");
            sb.AppendLine($"试算命名 {(string.IsNullOrEmpty(preview.TrialWarriorName) ? "-" : preview.TrialWarriorName)}");
            sb.Append(preview.CanManufacture ? "可制造" : preview.BlockReason);
            return sb.ToString();
        }

        private string BuildPoolText()
        {
            if (_warriorPool == null || _warriorPool.Warriors.Count == 0)
            {
                return "士兵池：空";
            }

            var warriors = _warriorPool.Warriors;
            var sb = new StringBuilder();
            sb.AppendLine($"士兵池：{warriors.Count} 名");
            var start = Math.Max(0, warriors.Count - PoolPreviewLines);
            for (var i = start; i < warriors.Count; i++)
            {
                var w = warriors[i];
                var deployed = _formation != null && _formation.IsDeployed(w.Id) ? "〔上阵〕" : string.Empty;
                sb.AppendLine(
                    $"{w.Id} {w.WarriorName}{deployed}｜HP {w.RemainingHP:0}｜{w.RaceId}／{w.ClassId}｜{w.AppearanceId}｜控 {w.ControlPowerCost:0.##}");
            }

            return sb.ToString();
        }
    }
}
