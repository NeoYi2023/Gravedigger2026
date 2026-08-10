// Offline verification copies — method bodies verbatim from:
//   Gravedigger2026/Assets/Scripts/Gameplay/Defend/WarriorAnimView.cs
//     (DirIndexFromXZ / StabilizeDirIndex / DirIndexToUnitXZ / DirIndexToSector, v0.75.21;
//      MonoBehaviour dropped; accessibility widened so the Runner can call them)
// Keep in sync with the source file; not part of the Unity build (.scratch).
using UnityEngine;

namespace Gravedigger2026.Gameplay.Defend
{
    public static class WarriorAnimView
    {
        public const float FacingHysteresisDegrees = 12f;
        public const float FacingSwitchMinDwellSeconds = 0.12f;

        // DirIndex (0E 1W 2S 3N 4NE 5NW 6SE 7SW) → quantization sector of DirIndexFromXZ.
        private static readonly int[] DirIndexToSector = { 2, 6, 4, 0, 1, 7, 3, 5 };

        /// <summary>
        /// SPEC_04 §15.5 DirIndex: 0E 1W 2S 3N 4NE 5NW 6SE 7SW. +X=E, +Z=N.
        /// </summary>
        public static int DirIndexFromXZ(Vector3 worldDirXZ)
        {
            worldDirXZ.y = 0f;
            if (worldDirXZ.sqrMagnitude < 0.0001f)
            {
                return 2;
            }

            var n = worldDirXZ.normalized;
            // atan2(x,z): 0 = +Z (N), +90° = +X (E)
            var deg = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            if (deg < 0f)
            {
                deg += 360f;
            }

            // 8 sectors centered on cardinals/diagonals (45° each)
            var sector = Mathf.RoundToInt(deg / 45f) % 8;
            switch (sector)
            {
                case 0: return 3; // N
                case 1: return 4; // NE
                case 2: return 0; // E
                case 3: return 6; // SE
                case 4: return 2; // S
                case 5: return 7; // SW
                case 6: return 1; // W
                case 7: return 5; // NW
                default: return 2;
            }
        }

        /// <summary>
        /// Keeps the current DirIndex unless the raw direction passes the current sector
        /// boundary by more than <paramref name="hysteresisDeg"/> (sector half-width 22.5°).
        /// </summary>
        public static int StabilizeDirIndex(int currentDirIndex, Vector3 rawDirXZ, float hysteresisDeg)
        {
            var candidate = DirIndexFromXZ(rawDirXZ);
            if (currentDirIndex < 0 || currentDirIndex > 7 || candidate == currentDirIndex)
            {
                return candidate;
            }

            rawDirXZ.y = 0f;
            if (rawDirXZ.sqrMagnitude < 0.0001f)
            {
                return currentDirIndex;
            }

            var n = rawDirXZ.normalized;
            var deg = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            if (deg < 0f)
            {
                deg += 360f;
            }

            var currentCenterDeg = DirIndexToSector[currentDirIndex] * 45f;
            var delta = Mathf.Abs(Mathf.DeltaAngle(deg, currentCenterDeg));
            return delta > 22.5f + hysteresisDeg ? candidate : currentDirIndex;
        }

        /// <summary>Unit XZ vector at the sector center of <paramref name="dirIndex"/> (round-trips through DirIndexFromXZ).</summary>
        public static Vector3 DirIndexToUnitXZ(int dirIndex)
        {
            var rad = DirIndexToSector[dirIndex] * 45f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }
    }
}
