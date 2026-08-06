using System;
using UnityEngine;

namespace Gravedigger2026.Core.Dig
{
    public sealed class DigGraveRuntime
    {
        public int InstanceId;
        public string QualityId;
        public float MaxHP;
        public float CurrentHP;
        public Vector3 WorldPosition;
        public float ObstacleRadius;
        /// <summary>Local XZ convex hit hull (root space). Null/short → circle fallback.</summary>
        public Vector2[] HitLocalXZ;
        public float HitBoundingRadius;
        public bool IsBusy;
        public bool IsCleared;
        public string LootDropEncoded;

        public bool HasHitPolygon => HitLocalXZ != null && HitLocalXZ.Length >= 3;

        public float RemainingHpPercent => MaxHP <= 0f ? 0f : CurrentHP / MaxHP;

        /// <summary>1 = high (&gt;65%), 2 = mid (30–65%), 3 = low (&lt;30%).</summary>
        public int IconStyleTier
        {
            get
            {
                var pct = RemainingHpPercent * 100f;
                if (pct > 65f)
                {
                    return 1;
                }

                if (pct >= 30f)
                {
                    return 2;
                }

                return 3;
            }
        }
    }
}
