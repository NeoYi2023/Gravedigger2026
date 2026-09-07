using System;
using System.Collections.Generic;
using Gravedigger2026.Core.Config;
using Gravedigger2026.Core.UpgradeManufacture;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Pure C# builder for CombatIndicator (UI-033 / D-089 Approach A).
    /// No UnityEngine; no per-call List allocation beyond reused scratch.
    /// </summary>
    public sealed class CombatIndicatorSnapshotBuilder
    {
        private static readonly Comparison<CombatIndicatorSlotData> AllyComparer = CompareAllies;
        private static readonly Comparison<CombatIndicatorSlotData> EnemyComparer = CompareEnemies;

        private readonly CombatIndicatorSnapshot _snapshot = new CombatIndicatorSnapshot();

        public CombatIndicatorSnapshot Build(
            IReadOnlyList<CombatIndicatorWarriorRead> warriors,
            IReadOnlyList<CombatIndicatorMonsterRead> monsters,
            WarriorPoolService pool,
            ConfigCsvRepository configs)
        {
            _snapshot.Clear();
            if (warriors != null)
            {
                for (var i = 0; i < warriors.Count; i++)
                {
                    var w = warriors[i];
                    if (w.IsRebel || string.IsNullOrEmpty(w.WarriorId))
                    {
                        continue;
                    }

                    var classId = string.Empty;
                    var classLevel = 0;
                    var tableOrder = 0;
                    var iconId = string.Empty;
                    if (pool != null && pool.TryGet(w.WarriorId, out var inst) && inst != null)
                    {
                        classId = inst.ClassId ?? string.Empty;
                    }

                    if (configs != null
                        && !string.IsNullOrEmpty(classId)
                        && configs.TryGetClass(classId, out var classRow)
                        && classRow != null)
                    {
                        classLevel = classRow.ClassLevel < 0 ? 0 : classRow.ClassLevel;
                        tableOrder = classRow.TableOrder;
                        iconId = classRow.SilhouetteIconAssetId ?? string.Empty;
                    }

                    var permanentDead = w.IsPermanentDead
                                        || w.RemainingHp <= 0f
                                        || w.IsCombatDead;
                    var countsAlive = !w.IsPermanentDead && w.RemainingHp > 0f && !w.IsCombatDead;
                    var ratio = ResolveHpRatio(w.RemainingHp, w.MaxHp, countsAlive, permanentDead);

                    var slot = new CombatIndicatorSlotData
                    {
                        UnitId = w.WarriorId,
                        CatalogId = classId,
                        SilhouetteIconAssetId = iconId,
                        HpRatio = ratio,
                        ShowDeathOverlay = permanentDead,
                        CountsAsAlive = countsAlive,
                        SortPrimary = classLevel,
                        TableOrder = tableOrder,
                        RegisterOrder = w.RegisterOrder
                    };
                    _snapshot.Allies.Add(slot);
                    if (countsAlive)
                    {
                        _snapshot.AllyAliveCount++;
                    }
                }
            }

            if (monsters != null)
            {
                for (var i = 0; i < monsters.Count; i++)
                {
                    var m = monsters[i];
                    if (string.IsNullOrEmpty(m.RuntimeId))
                    {
                        continue;
                    }

                    var monsterType = 0;
                    var tableOrder = 0;
                    var iconId = string.Empty;
                    if (configs != null
                        && !string.IsNullOrEmpty(m.MonsterId)
                        && configs.TryGetMonster(m.MonsterId, out var monsterRow)
                        && monsterRow != null)
                    {
                        monsterType = (int)monsterRow.MonsterType;
                        tableOrder = monsterRow.TableOrder;
                        iconId = monsterRow.SilhouetteIconAssetId ?? string.Empty;
                    }

                    var revivable = m.RevivePhase != MonsterRevivePhase.None || m.RevivesRemaining > 0;
                    var countsAlive = revivable
                                      || (m.RemainingHp > 0f && m.IsAlive && !m.IsCombatDead);
                    var permanentDead = !countsAlive;
                    var ratio = ResolveHpRatio(m.RemainingHp, m.MaxHp, countsAlive, permanentDead);

                    var slot = new CombatIndicatorSlotData
                    {
                        UnitId = m.RuntimeId,
                        CatalogId = m.MonsterId ?? string.Empty,
                        SilhouetteIconAssetId = iconId,
                        HpRatio = ratio,
                        ShowDeathOverlay = permanentDead,
                        CountsAsAlive = countsAlive,
                        SortPrimary = monsterType,
                        TableOrder = tableOrder,
                        RegisterOrder = m.RegisterOrder
                    };
                    _snapshot.Enemies.Add(slot);
                    if (countsAlive)
                    {
                        _snapshot.EnemyAliveCount++;
                    }
                }
            }

            _snapshot.Allies.Sort(AllyComparer);
            _snapshot.Enemies.Sort(EnemyComparer);
            ComputeFingerprints(_snapshot);
            return _snapshot;
        }

        private static float ResolveHpRatio(float remaining, float maxHp, bool countsAlive, bool permanentDead)
        {
            if (permanentDead)
            {
                return 0f;
            }

            if (maxHp <= 0.01f)
            {
                return countsAlive ? 1f : 0f;
            }

            var ratio = remaining / maxHp;
            if (ratio < 0f)
            {
                ratio = 0f;
            }
            else if (ratio > 1f)
            {
                ratio = 1f;
            }

            // Reviving (alive for count, HP==0): keep red living tint, not death overlay.
            if (countsAlive && ratio <= 0f)
            {
                return 0.01f;
            }

            return ratio;
        }

        private static int CompareAllies(CombatIndicatorSlotData a, CombatIndicatorSlotData b)
        {
            var c = b.SortPrimary.CompareTo(a.SortPrimary);
            if (c != 0)
            {
                return c;
            }

            c = a.TableOrder.CompareTo(b.TableOrder);
            if (c != 0)
            {
                return c;
            }

            c = a.RegisterOrder.CompareTo(b.RegisterOrder);
            if (c != 0)
            {
                return c;
            }

            return string.CompareOrdinal(a.UnitId, b.UnitId);
        }

        private static int CompareEnemies(CombatIndicatorSlotData a, CombatIndicatorSlotData b)
        {
            return CompareAllies(a, b);
        }

        private static void ComputeFingerprints(CombatIndicatorSnapshot snap)
        {
            unchecked
            {
                var membership = 17;
                var appearance = 19;
                appearance = appearance * 31 + snap.AllyAliveCount;
                appearance = appearance * 31 + snap.EnemyAliveCount;
                Accumulate(snap.Allies, ref membership, ref appearance);
                Accumulate(snap.Enemies, ref membership, ref appearance);
                snap.MembershipFingerprint = membership;
                snap.AppearanceFingerprint = appearance;
            }
        }

        private static void Accumulate(
            List<CombatIndicatorSlotData> slots,
            ref int membership,
            ref int appearance)
        {
            for (var i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                var idHash = s.UnitId != null ? s.UnitId.GetHashCode() : 0;
                membership = membership * 31 + idHash;
                membership = membership * 31 + (s.ShowDeathOverlay ? 1 : 0);
                appearance = appearance * 31 + idHash;
                appearance = appearance * 31 + HpTintBucket(s.HpRatio, s.ShowDeathOverlay);
                appearance = appearance * 31 + (s.ShowDeathOverlay ? 1 : 0);
            }
        }

        /// <summary>0 gray/dead · 1 red · 2 orange · 3 green.</summary>
        public static int HpTintBucket(float hpRatio, bool showDeathOverlay)
        {
            if (showDeathOverlay || hpRatio <= 0f)
            {
                return 0;
            }

            if (hpRatio >= 0.66f)
            {
                return 3;
            }

            if (hpRatio > 0.33f)
            {
                return 2;
            }

            return 1;
        }
    }
}
