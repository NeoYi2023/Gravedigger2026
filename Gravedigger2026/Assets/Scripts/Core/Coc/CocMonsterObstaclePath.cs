using Gravedigger2026.Core.Config;
using UnityEngine;

namespace Gravedigger2026.Core.Coc
{
    /// <summary>
    /// COC monster vs destructible-obstacle path overlay (SPEC_03 §3.21 / D-100 Approach A).
    /// PushMap / Defend / SearchExtract do not call this.
    /// Slice 03a: around vs straight-drive. Slice 03b: treat-as-target segment test.
    /// </summary>
    public static class CocMonsterObstaclePath
    {
        public const string TreatAsObstacleToken = "视为障碍";
        public const string TreatAsTargetToken = "视为目标";
        public const string IgnoreObstacleToken = "无视障碍";

        public static bool ShouldDriveStraight(CocObstaclePathMode mode)
        {
            return mode == CocObstaclePathMode.IgnoreObstacle;
        }

        /// <summary>
        /// Inclusive segment vs axis-aligned square centered at the origin.
        /// <paramref name="tEnter"/> is 0 at <paramref name="a"/> and 1 at <paramref name="b"/>.
        /// </summary>
        public static bool TrySegmentHitSquare(Vector2 a, Vector2 b, float half, out float tEnter)
        {
            return TrySegmentHitRect(a, b, half, half, out tEnter);
        }

        /// <summary>
        /// Inclusive segment vs axis-aligned rectangle centered at the origin.
        /// <paramref name="halfX"/> / <paramref name="halfZ"/> are the local XZ half extents
        /// (<paramref name="a"/>/<paramref name="b"/> y is ground Z).
        /// </summary>
        public static bool TrySegmentHitRect(
            Vector2 a,
            Vector2 b,
            float halfX,
            float halfZ,
            out float tEnter)
        {
            tEnter = 0f;
            if (halfX < 0f || halfZ < 0f)
            {
                return false;
            }

            var t0 = 0f;
            var t1 = 1f;
            if (!ClipAxis(a.x, b.x - a.x, -halfX, halfX, ref t0, ref t1))
            {
                return false;
            }

            if (!ClipAxis(a.y, b.y - a.y, -halfZ, halfZ, ref t0, ref t1))
            {
                return false;
            }

            tEnter = t0;
            return true;
        }

        private static bool ClipAxis(float origin, float delta, float min, float max, ref float t0, ref float t1)
        {
            const float eps = 1e-5f;
            if (Mathf.Abs(delta) <= eps)
            {
                return origin >= min - eps && origin <= max + eps;
            }

            var inv = 1f / delta;
            var enter = (min - origin) * inv;
            var exit = (max - origin) * inv;
            if (enter > exit)
            {
                var swap = enter;
                enter = exit;
                exit = swap;
            }

            if (enter > t0)
            {
                t0 = enter;
            }

            if (exit < t1)
            {
                t1 = exit;
            }

            return t0 <= t1 + eps;
        }
    }
}
