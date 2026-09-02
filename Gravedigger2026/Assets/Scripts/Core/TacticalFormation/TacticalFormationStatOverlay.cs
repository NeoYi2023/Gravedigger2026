using System;
using System.Collections.Generic;
using System.Globalization;
using Gravedigger2026.Core.Combat;
using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.TacticalFormation
{
    /// <summary>
    /// Parses <c>TacticalFormationConfig.StatModifiers</c> into a Combat StatMul overlay
    /// (SPEC_04 §9.30: <c>Stat=…|Mul=…</c> pairs; Stat in MaxHP/Strength/Agility/Intelligence/MoveSpeed/All).
    /// </summary>
    public static class TacticalFormationStatOverlay
    {
        public static CombatStatMulBuff Parse(string encoded, string formationId = null)
        {
            if (string.IsNullOrWhiteSpace(encoded))
            {
                return CombatStatMulBuff.Identity;
            }

            var maxHpMul = 1f;
            var strengthMul = 1f;
            var agilityMul = 1f;
            var intelligenceMul = 1f;
            var moveSpeedMul = 1f;
            string pendingStat = null;
            var parts = encoded.Split('|');
            for (var i = 0; i < parts.Length; i++)
            {
                var seg = parts[i]?.Trim();
                if (string.IsNullOrEmpty(seg))
                {
                    continue;
                }

                var eq = seg.IndexOf('=');
                if (eq <= 0 || eq >= seg.Length - 1)
                {
                    Debug.LogWarning(
                        $"[TacticalFormation] StatModifiers ignored (need Key=Value) " +
                        $"formation={formationId} seg='{seg}'");
                    continue;
                }

                var key = seg.Substring(0, eq).Trim();
                var value = seg.Substring(eq + 1).Trim();
                if (string.Equals(key, "Stat", StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(pendingStat))
                    {
                        Debug.LogWarning(
                            $"[TacticalFormation] StatModifiers missing Mul after Stat='{pendingStat}' " +
                            $"formation={formationId}");
                    }

                    pendingStat = value;
                    continue;
                }

                if (string.Equals(key, "Mul", StringComparison.Ordinal))
                {
                    if (string.IsNullOrEmpty(pendingStat))
                    {
                        Debug.LogWarning(
                            $"[TacticalFormation] StatModifiers Mul without Stat formation={formationId}");
                        continue;
                    }

                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var mul)
                        || mul < 0f)
                    {
                        Debug.LogWarning(
                            $"[TacticalFormation] StatModifiers invalid Mul='{value}' formation={formationId}");
                        pendingStat = null;
                        continue;
                    }

                    ApplyStat(pendingStat, mul, formationId,
                        ref maxHpMul, ref strengthMul, ref agilityMul, ref intelligenceMul, ref moveSpeedMul);
                    pendingStat = null;
                    continue;
                }

                Debug.LogWarning(
                    $"[TacticalFormation] StatModifiers unknown Key '{key}' ignored formation={formationId}");
            }

            if (!string.IsNullOrEmpty(pendingStat))
            {
                Debug.LogWarning(
                    $"[TacticalFormation] StatModifiers missing Mul after Stat='{pendingStat}' " +
                    $"formation={formationId}");
            }

            return new CombatStatMulBuff(maxHpMul, strengthMul, agilityMul, intelligenceMul, moveSpeedMul);
        }

        public static CombatStatMulBuff CombineWithMemberLocks(
            CombatStatMulBuff magicBook,
            IReadOnlyList<TacticalFormationCombatLock> locks,
            string warriorId)
        {
            if (locks == null || string.IsNullOrEmpty(warriorId))
            {
                return magicBook;
            }

            for (var i = 0; i < locks.Count; i++)
            {
                var members = locks[i].MemberIds;
                if (members == null)
                {
                    continue;
                }

                for (var m = 0; m < members.Length; m++)
                {
                    if (string.Equals(members[m], warriorId, StringComparison.Ordinal))
                    {
                        return magicBook.Multiply(locks[i].StatMul);
                    }
                }
            }

            return magicBook;
        }

        public static bool TryGetForWarrior(
            IReadOnlyList<TacticalFormationCombatLock> locks,
            string warriorId,
            out CombatStatMulBuff statMul)
        {
            statMul = CombatStatMulBuff.Identity;
            if (locks == null || string.IsNullOrEmpty(warriorId))
            {
                return false;
            }

            for (var i = 0; i < locks.Count; i++)
            {
                var members = locks[i].MemberIds;
                if (members == null)
                {
                    continue;
                }

                for (var m = 0; m < members.Length; m++)
                {
                    if (!string.Equals(members[m], warriorId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    statMul = locks[i].StatMul;
                    return !statMul.IsIdentity;
                }
            }

            return false;
        }

        private static void ApplyStat(
            string statText,
            float mul,
            string formationId,
            ref float maxHpMul,
            ref float strengthMul,
            ref float agilityMul,
            ref float intelligenceMul,
            ref float moveSpeedMul)
        {
            if (string.Equals(statText, "All", StringComparison.Ordinal))
            {
                maxHpMul *= mul;
                strengthMul *= mul;
                agilityMul *= mul;
                intelligenceMul *= mul;
                moveSpeedMul *= mul;
                return;
            }

            if (!Enum.TryParse(statText, false, out StatKind kind))
            {
                Debug.LogWarning(
                    $"[TacticalFormation] StatModifiers invalid Stat='{statText}' formation={formationId}");
                return;
            }

            switch (kind)
            {
                case StatKind.MaxHP:
                    maxHpMul *= mul;
                    break;
                case StatKind.Strength:
                    strengthMul *= mul;
                    break;
                case StatKind.Agility:
                    agilityMul *= mul;
                    break;
                case StatKind.Intelligence:
                    intelligenceMul *= mul;
                    break;
                case StatKind.MoveSpeed:
                    moveSpeedMul *= mul;
                    break;
                default:
                    Debug.LogWarning(
                        $"[TacticalFormation] StatModifiers unsupported Stat='{statText}' formation={formationId}");
                    break;
            }
        }
    }
}
