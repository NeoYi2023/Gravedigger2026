using System.Collections.Generic;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Thin warrior read for UI-033 / D-089 (Session → SnapshotBuilder).
    /// </summary>
    public readonly struct CombatIndicatorWarriorRead
    {
        public readonly string WarriorId;
        public readonly float RemainingHp;
        public readonly float MaxHp;
        public readonly bool IsCombatDead;
        public readonly bool IsPermanentDead;
        public readonly bool IsRebel;
        public readonly int RegisterOrder;

        public CombatIndicatorWarriorRead(
            string warriorId,
            float remainingHp,
            float maxHp,
            bool isCombatDead,
            bool isPermanentDead,
            bool isRebel,
            int registerOrder)
        {
            WarriorId = warriorId;
            RemainingHp = remainingHp;
            MaxHp = maxHp;
            IsCombatDead = isCombatDead;
            IsPermanentDead = isPermanentDead;
            IsRebel = isRebel;
            RegisterOrder = registerOrder;
        }
    }

    /// <summary>
    /// Thin monster read for UI-033 / D-089 (Session → SnapshotBuilder).
    /// </summary>
    public readonly struct CombatIndicatorMonsterRead
    {
        public readonly string RuntimeId;
        public readonly string MonsterId;
        public readonly float RemainingHp;
        public readonly float MaxHp;
        public readonly bool IsAlive;
        public readonly bool IsCombatDead;
        public readonly MonsterRevivePhase RevivePhase;
        public readonly int RevivesRemaining;
        public readonly int RegisterOrder;

        public CombatIndicatorMonsterRead(
            string runtimeId,
            string monsterId,
            float remainingHp,
            float maxHp,
            bool isAlive,
            bool isCombatDead,
            MonsterRevivePhase revivePhase,
            int revivesRemaining,
            int registerOrder)
        {
            RuntimeId = runtimeId;
            MonsterId = monsterId;
            RemainingHp = remainingHp;
            MaxHp = maxHp;
            IsAlive = isAlive;
            IsCombatDead = isCombatDead;
            RevivePhase = revivePhase;
            RevivesRemaining = revivesRemaining;
            RegisterOrder = registerOrder;
        }
    }

    /// <summary>One displayed unit slot after sort / config resolve.</summary>
    public struct CombatIndicatorSlotData
    {
        public string UnitId;
        public string CatalogId;
        public string SilhouetteIconAssetId;
        public float HpRatio;
        public bool ShowDeathOverlay;
        public bool CountsAsAlive;
        public int SortPrimary;
        public int TableOrder;
        public int RegisterOrder;
    }

    /// <summary>
    /// Built HUD snapshot (UI-033). Lists are owned by <see cref="CombatIndicatorSnapshotBuilder"/>;
    /// consume in the same frame.
    /// </summary>
    public sealed class CombatIndicatorSnapshot
    {
        public int AllyAliveCount;
        public int EnemyAliveCount;
        public readonly List<CombatIndicatorSlotData> Allies = new List<CombatIndicatorSlotData>(32);
        public readonly List<CombatIndicatorSlotData> Enemies = new List<CombatIndicatorSlotData>(32);

        /// <summary>Unit identity + death-overlay flags (triggers Rebuild).</summary>
        public int MembershipFingerprint;

        /// <summary>Alive counts + HP tint buckets (tint/digit only).</summary>
        public int AppearanceFingerprint;

        public void Clear()
        {
            AllyAliveCount = 0;
            EnemyAliveCount = 0;
            Allies.Clear();
            Enemies.Clear();
            MembershipFingerprint = 0;
            AppearanceFingerprint = 0;
        }
    }
}
