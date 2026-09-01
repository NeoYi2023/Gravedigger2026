using UnityEngine;

namespace Gravedigger2026.Core.PushMap
{
    /// <summary>
    /// Maps PushMapSpawnConfig.InitialFacing (compass 0~8) to WarriorAnimView DirIndex
    /// (SPEC_04 §9.23 / §15.5 v0.83.58).
    /// </summary>
    public static class PushMapSpawnFacing
    {
        public const int DefaultInitialFacing = 5;

        /// <summary>
        /// Resolves a Creator DirIndex (0E…7SW). When <paramref name="initialFacing"/> is 0,
        /// rolls compass 1~8 once for this call (per monster instance).
        /// </summary>
        public static int ResolveDirIndex(int initialFacing)
        {
            var compass = initialFacing;
            if (compass == 0)
            {
                compass = Random.Range(1, 9);
            }

            return CompassToDirIndex(compass);
        }

        /// <summary>Compass 1~8 → DirIndex. Invalid values fall back to South (2).</summary>
        public static int CompassToDirIndex(int compassFacing)
        {
            switch (compassFacing)
            {
                case 1: return 3; // N
                case 2: return 4; // NE
                case 3: return 0; // E
                case 4: return 6; // SE
                case 5: return 2; // S
                case 6: return 7; // SW
                case 7: return 1; // W
                case 8: return 5; // NW
                default: return 2;
            }
        }
    }
}
