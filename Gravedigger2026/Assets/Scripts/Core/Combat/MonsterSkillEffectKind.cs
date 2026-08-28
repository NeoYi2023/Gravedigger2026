using System;
using System.Collections.Generic;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>SPEC_04 §9.21c EffectKind registry (monster skills).</summary>
    public static class MonsterSkillEffectKind
    {
        public const string MonsterSelfReviveOnDeath = "MonsterSelfReviveOnDeath";

        private static readonly HashSet<string> Registered = new HashSet<string>(StringComparer.Ordinal)
        {
            MonsterSelfReviveOnDeath
        };

        public static bool IsRegistered(string kind)
        {
            return !string.IsNullOrWhiteSpace(kind) && Registered.Contains(kind);
        }
    }
}
