using System.Text;
using Gravedigger2026.Core.Combat;

namespace Gravedigger2026.Gameplay.Defend
{
    /// <summary>
    /// D-083 unified scene-free checks: parabolic sampling + corpse smash rules (SPEC_03 §3.12 / SPEC_04 §15.5).
    /// </summary>
    public static class CorpseProjectileCorrectnessChecks
    {
        public static string RunAll()
        {
            var sb = new StringBuilder();
            AppendIfFailed(sb, MonsterDeathPresentationCorrectnessChecks.RunAll());
            AppendIfFailed(sb, CorpseSmashCombatCorrectnessChecks.RunAll());
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void AppendIfFailed(StringBuilder sb, string error)
        {
            if (string.IsNullOrEmpty(error))
            {
                return;
            }

            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.Append(error.TrimEnd());
        }
    }
}
