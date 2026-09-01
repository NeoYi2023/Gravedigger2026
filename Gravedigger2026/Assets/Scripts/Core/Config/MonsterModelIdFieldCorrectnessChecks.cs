using System.Collections.Generic;
using System.Text;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Scene-free checks for Defend_MonsterConfig.ModelId weighted pool (SPEC_04 §9.19).
    /// </summary>
    public static class MonsterModelIdFieldCorrectnessChecks
    {
        public static string RunAll()
        {
            var sb = new StringBuilder();
            CheckLegacySingleId(sb);
            CheckWeightedPool(sb);
            CheckInvalidPool(sb);
            CheckEnumerateDistinct(sb);
            return sb.Length == 0 ? null : sb.ToString();
        }

        private static void CheckLegacySingleId(StringBuilder sb)
        {
            var pool = MonsterModelIdFieldParser.Parse("MonsterModel_05");
            if (pool.Count != 1
                || pool[0].Id != "MonsterModel_05"
                || pool[0].Weight != 1f)
            {
                sb.AppendLine("LegacySingleId: expected one entry MonsterModel_05 weight 1");
            }
        }

        private static void CheckWeightedPool(StringBuilder sb)
        {
            var pool = MonsterModelIdFieldParser.Parse("MonsterModel_05;70|MonsterModel_02;30");
            if (pool.Count != 2)
            {
                sb.AppendLine($"WeightedPool: expected 2 entries, got {pool.Count}");
                return;
            }

            if (pool[0].Id != "MonsterModel_05" || pool[0].Weight != 70f)
            {
                sb.AppendLine("WeightedPool: first entry mismatch");
            }

            if (pool[1].Id != "MonsterModel_02" || pool[1].Weight != 30f)
            {
                sb.AppendLine("WeightedPool: second entry mismatch");
            }
        }

        private static void CheckInvalidPool(StringBuilder sb)
        {
            var pool = MonsterModelIdFieldParser.Parse("MonsterModel_A;0|MonsterModel_B;0");
            if (pool.Count != 0)
            {
                sb.AppendLine("InvalidPool: zero-weight pool must parse empty");
            }

            pool = MonsterModelIdFieldParser.Parse("Bad;not_a_number");
            if (pool.Count != 0)
            {
                sb.AppendLine("InvalidPool: malformed weight must parse empty");
            }
        }

        private static void CheckEnumerateDistinct(StringBuilder sb)
        {
            var ids = new List<string>();
            foreach (var id in MonsterModelIdFieldParser.EnumerateModelIds("MonsterModel_05;50|MonsterModel_02;50"))
            {
                ids.Add(id);
            }

            if (ids.Count != 2 || ids[0] != "MonsterModel_05" || ids[1] != "MonsterModel_02")
            {
                sb.AppendLine("EnumerateDistinct: expected two sub-IDs in order");
            }
        }
    }
}
