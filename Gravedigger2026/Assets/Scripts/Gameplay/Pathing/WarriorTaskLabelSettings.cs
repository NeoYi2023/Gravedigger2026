using System;

namespace Gravedigger2026.Gameplay.Pathing
{
    /// <summary>
    /// Demo Debug toggle for soldier GoalKind foot labels (SPEC_04 §9.7). Default on.
    /// </summary>
    public static class WarriorTaskLabelSettings
    {
        private static bool _enabled = true;

        public static event Action<bool> EnabledChanged;

        public static bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;
                EnabledChanged?.Invoke(_enabled);
            }
        }

        public static void Toggle()
        {
            Enabled = !Enabled;
        }
    }
}
