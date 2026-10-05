using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gravedigger2026.Core.Combat
{
    /// <summary>
    /// Scene-free checks for nearest-band + TargetFocus free-first (SPEC_03 §3.12).
    /// </summary>
    public static class MonsterNearestWarriorPickerCorrectnessChecks
    {
        private const float Relative = 0.25f;
        private const float Slack = 0.5f;

        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckFreePreferredInsideBand(sb);
            CheckOutsideBandIgnored(sb);
            CheckStickySameTier(sb);
            CheckSwitchTierWhenFreeAppears(sb);
            CheckFocusCountExcludingSelf(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void CheckFreePreferredInsideBand(StringBuilder sb)
        {
            var focus = new TargetFocusRegistry();
            focus.SetFocus("mOther", "wNear");
            var monster = Vector3.zero;
            var candidates = new List<MonsterNearestWarriorPicker.Candidate>
            {
                new MonsterNearestWarriorPicker.Candidate("wNear", new Vector3(1f, 0f, 0f)),
                new MonsterNearestWarriorPicker.Candidate("wFree", new Vector3(1.2f, 0f, 0f))
            };

            var pick = MonsterNearestWarriorPicker.Pick(
                monster, candidates, focus, "mSelf", null, Relative, Slack);
            if (pick != "wFree")
            {
                sb.AppendLine($"FreePreferred: expected wFree, got {pick}");
            }
        }

        private static void CheckOutsideBandIgnored(StringBuilder sb)
        {
            var focus = new TargetFocusRegistry();
            focus.SetFocus("mOther", "wNear");
            var monster = Vector3.zero;
            // dMin=1; bandMax=1*1.25+0.5=1.75; wFar at 3 is outside
            var candidates = new List<MonsterNearestWarriorPicker.Candidate>
            {
                new MonsterNearestWarriorPicker.Candidate("wNear", new Vector3(1f, 0f, 0f)),
                new MonsterNearestWarriorPicker.Candidate("wFarFree", new Vector3(3f, 0f, 0f))
            };

            var pick = MonsterNearestWarriorPicker.Pick(
                monster, candidates, focus, "mSelf", null, Relative, Slack);
            if (pick != "wNear")
            {
                sb.AppendLine($"OutsideBand: expected wNear (only in-band), got {pick}");
            }
        }

        private static void CheckStickySameTier(StringBuilder sb)
        {
            var focus = new TargetFocusRegistry();
            focus.SetFocus("mSelf", "wA");
            var monster = Vector3.zero;
            var candidates = new List<MonsterNearestWarriorPicker.Candidate>
            {
                new MonsterNearestWarriorPicker.Candidate("wB", new Vector3(1f, 0f, 0f)),
                new MonsterNearestWarriorPicker.Candidate("wA", new Vector3(1.1f, 0f, 0f))
            };

            var pick = MonsterNearestWarriorPicker.Pick(
                monster, candidates, focus, "mSelf", "wA", Relative, Slack);
            if (pick != "wA")
            {
                sb.AppendLine($"StickySameTier: expected keep wA, got {pick}");
            }
        }

        private static void CheckSwitchTierWhenFreeAppears(StringBuilder sb)
        {
            var focus = new TargetFocusRegistry();
            focus.SetFocus("mSelf", "wEngaged");
            focus.SetFocus("mOther", "wEngaged");
            var monster = Vector3.zero;
            var candidates = new List<MonsterNearestWarriorPicker.Candidate>
            {
                new MonsterNearestWarriorPicker.Candidate("wEngaged", new Vector3(1f, 0f, 0f)),
                new MonsterNearestWarriorPicker.Candidate("wFree", new Vector3(1.2f, 0f, 0f))
            };

            var pick = MonsterNearestWarriorPicker.Pick(
                monster, candidates, focus, "mSelf", "wEngaged", Relative, Slack);
            if (pick != "wFree")
            {
                sb.AppendLine($"SwitchTier: expected switch to wFree, got {pick}");
            }
        }

        private static void CheckFocusCountExcludingSelf(StringBuilder sb)
        {
            var focus = new TargetFocusRegistry();
            focus.SetFocus("mSelf", "w1");
            if (focus.GetFocusCount("w1") != 1)
            {
                sb.AppendLine("FocusCount: expected 1 after SetFocus");
            }

            if (focus.GetFocusCountExcluding("w1", "mSelf") != 0)
            {
                sb.AppendLine("FocusCountExcluding: self must not count as engaged");
            }

            focus.ClearFocus("mSelf");
            if (focus.GetFocusCount("w1") != 0)
            {
                sb.AppendLine("FocusCount: expected 0 after ClearFocus");
            }
        }
    }
}
