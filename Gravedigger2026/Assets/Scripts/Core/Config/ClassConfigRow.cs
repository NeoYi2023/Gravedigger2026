namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// One row of Manufacture_ClassConfig (SPEC_04 §9.9b).
    /// </summary>
    public sealed class ClassConfigRow
    {
        public string ClassId;
        public string ClassName;
        public StatKind PrimaryStat;
        public string CombatConvertCoeffs;
        public float AttackRange;
        public float MeleeWindupSeconds;
        public float RangedProjectileSpeed;
        public float RangedTimeoutSeconds;
    }
}
