// Facing-stabilizer correctness checks (v0.75.21, SPEC_04 §15.5).
// Angle convention matches WarriorAnimView.DirIndexFromXZ: atan2(x, z) — 0°=+Z (N), 90°=+X (E).
using System.Text;
using Gravedigger2026.Gameplay.Defend;
using UnityEngine;

namespace Gravedigger2026.Gameplay.PushMap
{
    public static class FacingStabilizerCorrectnessChecks
    {
        private const float Hysteresis = WarriorAnimView.FacingHysteresisDegrees;

        public static string RunAll()
        {
            var sb = new StringBuilder();

            // 1) Boundary oscillation never switches: current E (center 90°), steer flapping
            //    60°↔120° (candidates NE/SE, both 30° from center ≤ 22.5+12) must hold E.
            for (var i = 0; i < 50; i++)
            {
                var deg = i % 2 == 0 ? 60f : 120f;
                Check(sb, WarriorAnimView.StabilizeDirIndex(0, Dir(deg), Hysteresis) == 0,
                    $"boundary oscillation holds E (deg={deg})");
            }

            // 2) Beyond hysteresis switches: E → raw 126° (delta 36 > 34.5) → SE(6).
            Check(sb, WarriorAnimView.StabilizeDirIndex(0, Dir(126f), Hysteresis) == 6,
                "beyond hysteresis switches E→SE");

            // 3) Tight threshold (22.5+12=34.5): current N (center 0°); 34.4° keeps, 34.6° switches.
            Check(sb, WarriorAnimView.StabilizeDirIndex(3, Dir(34.4f), Hysteresis) == 3,
                "34.4° within hysteresis keeps N");
            Check(sb, WarriorAnimView.StabilizeDirIndex(3, Dir(34.6f), Hysteresis) == 4,
                "34.6° past hysteresis switches N→NE");

            // 4) Zero vector keeps current (DirIndexFromXZ default S must not hijack).
            Check(sb, WarriorAnimView.StabilizeDirIndex(0, Vector3.zero, Hysteresis) == 0,
                "zero vector keeps E");
            Check(sb, WarriorAnimView.StabilizeDirIndex(5, Vector3.zero, Hysteresis) == 5,
                "zero vector keeps NW");

            // 5) Invalid current → candidate passthrough (first-frame init).
            Check(sb, WarriorAnimView.StabilizeDirIndex(-1, Dir(270f), Hysteresis) == 1,
                "invalid current -1 → W");
            Check(sb, WarriorAnimView.StabilizeDirIndex(8, Dir(180f), Hysteresis) == 2,
                "invalid current 8 → S");

            // 6) Round-trip: DirIndexToUnitXZ feeds DirIndexFromXZ back to the same index.
            for (var i = 0; i < 8; i++)
            {
                var back = WarriorAnimView.DirIndexFromXZ(WarriorAnimView.DirIndexToUnitXZ(i));
                Check(sb, back == i, $"DirIndexToUnitXZ round-trip {i} (got {back})");
            }

            // 7) Full 180° flip switches immediately: E → W.
            Check(sb, WarriorAnimView.StabilizeDirIndex(0, Dir(270f), Hysteresis) == 1,
                "full flip E→W");

            // 8) Legitimate large turn not delayed: N → raw 50° (delta 50 > 34.5) → NE(4).
            Check(sb, WarriorAnimView.StabilizeDirIndex(3, Dir(50f), Hysteresis) == 4,
                "large turn N→NE switches");

            // 9) Zero hysteresis degrades to plain quantization: N, raw 23° (delta 23 > 22.5) → NE.
            Check(sb, WarriorAnimView.StabilizeDirIndex(3, Dir(23f), 0f) == 4,
                "zero hysteresis = plain quantization");
            //    ...and with the locked 12° the same 30° steer holds N.
            Check(sb, WarriorAnimView.StabilizeDirIndex(3, Dir(30f), Hysteresis) == 3,
                "30° steer holds N under locked hysteresis");

            // 10) Locked constants match SPEC.
            Check(sb, Mathf.Approximately(WarriorAnimView.FacingHysteresisDegrees, 12f),
                "FacingHysteresisDegrees == 12");
            Check(sb, Mathf.Approximately(WarriorAnimView.FacingSwitchMinDwellSeconds, 0.12f),
                "FacingSwitchMinDwellSeconds == 0.12");

            return sb.Length == 0 ? null : sb.ToString();
        }

        private static Vector3 Dir(float deg)
        {
            var rad = deg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        }

        private static void Check(StringBuilder sb, bool ok, string name)
        {
            if (!ok)
            {
                sb.AppendLine($"[FAIL] {name}");
            }
        }
    }
}
