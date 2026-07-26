using System;

namespace Gravedigger2026.Core.Defend
{
    /// <summary>
    /// Degree → TierId and FinalLossChance helpers (SPEC_03 §3.11 / §3.12).
    /// </summary>
    public static class LossOfControlMath
    {
        /// <summary>
        /// Maps locked Degree to TierId 1..4; returns 0 when Degree ≤ 0 (not out of control).
        /// </summary>
        public static int MapTierId(float degree)
        {
            if (degree <= 0f)
            {
                return 0;
            }

            if (degree <= 0.35f)
            {
                return 1;
            }

            if (degree <= 0.7f)
            {
                return 2;
            }

            if (degree <= 1f)
            {
                return 3;
            }

            return 4;
        }

        public static float ClampChance(float chance)
        {
            if (chance < 0f)
            {
                return 0f;
            }

            if (chance > 1f)
            {
                return 1f;
            }

            return chance;
        }

        public static float ComputeFinalLossChance(
            float tierChance,
            float raceBonus,
            float gemBonusSum,
            float skillBonusSum = 0f)
        {
            return ClampChance(tierChance + raceBonus + gemBonusSum + skillBonusSum);
        }
    }
}
