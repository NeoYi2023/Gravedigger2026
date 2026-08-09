using System;

namespace Gravedigger2026.Core.Config
{
    /// <summary>
    /// Five-dim stat container used for Base / Equip / GemMult / RaceAdjust (SPEC_03 §3.11).
    /// Must be <see cref="SerializableAttribute"/> for PlayerPrefs JsonUtility nested fields (SPEC_04 §6).
    /// </summary>
    [Serializable]
    public struct StatBlock
    {
        public float MaxHP;
        public float MoveSpeed;
        public float Strength;
        public float Agility;
        public float Intelligence;

        public bool IsAllZero =>
            MaxHP == 0f && MoveSpeed == 0f && Strength == 0f && Agility == 0f && Intelligence == 0f;

        public float Get(StatKind kind)
        {
            switch (kind)
            {
                case StatKind.MaxHP: return MaxHP;
                case StatKind.MoveSpeed: return MoveSpeed;
                case StatKind.Strength: return Strength;
                case StatKind.Agility: return Agility;
                case StatKind.Intelligence: return Intelligence;
                default: return 0f;
            }
        }

        public void Set(StatKind kind, float value)
        {
            switch (kind)
            {
                case StatKind.MaxHP:
                    MaxHP = value;
                    break;
                case StatKind.MoveSpeed:
                    MoveSpeed = value;
                    break;
                case StatKind.Strength:
                    Strength = value;
                    break;
                case StatKind.Agility:
                    Agility = value;
                    break;
                case StatKind.Intelligence:
                    Intelligence = value;
                    break;
            }
        }

        public void Add(StatKind kind, float value)
        {
            Set(kind, Get(kind) + value);
        }

        public void Add(in StatBlock other)
        {
            MaxHP += other.MaxHP;
            MoveSpeed += other.MoveSpeed;
            Strength += other.Strength;
            Agility += other.Agility;
            Intelligence += other.Intelligence;
        }
    }
}
