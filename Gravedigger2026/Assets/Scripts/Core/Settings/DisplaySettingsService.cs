using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gravedigger2026.Core.Settings
{
    /// <summary>
    /// Machine-level display resolution / fullscreen (SPEC_04 §6 UI-028).
    /// Not save-slot scoped. Apply persists; Close without Apply discards draft in View.
    /// </summary>
    public sealed class DisplaySettingsService
    {
        private const string PrefsWidth = "Gravedigger2026.Display.Width";
        private const string PrefsHeight = "Gravedigger2026.Display.Height";
        private const string PrefsWindowMode = "Gravedigger2026.Display.WindowMode";

        private int _width;
        private int _height;
        private DisplayWindowMode _windowMode;
        private bool _hasAppliedSnapshot;

        public event Action Changed;

        public int Width => _width;
        public int Height => _height;
        public DisplayWindowMode WindowMode => _windowMode;

        /// <summary>
        /// Boot: load prefs if present and apply; otherwise snapshot current screen into memory
        /// without forcing a resize.
        /// </summary>
        public void ApplySavedOrCurrent()
        {
            if (PlayerPrefs.HasKey(PrefsWidth) && PlayerPrefs.HasKey(PrefsHeight))
            {
                _width = Mathf.Max(1, PlayerPrefs.GetInt(PrefsWidth, Screen.width));
                _height = Mathf.Max(1, PlayerPrefs.GetInt(PrefsHeight, Screen.height));
                _windowMode = ClampMode(PlayerPrefs.GetInt(PrefsWindowMode, (int)DisplayWindowMode.Windowed));
                ApplyInternal(_width, _height, _windowMode, persist: false);
            }
            else
            {
                SnapshotCurrent();
            }

            _hasAppliedSnapshot = true;
        }

        public void SnapshotCurrent()
        {
            _width = Mathf.Max(1, Screen.width);
            _height = Mathf.Max(1, Screen.height);
            _windowMode = FromFullScreenMode(Screen.fullScreenMode);
        }

        public IReadOnlyList<DisplayResolutionOption> GetResolutionOptions()
        {
            var list = new List<DisplayResolutionOption>(32);
            var seen = new HashSet<long>();
            var resolutions = Screen.resolutions;
            if (resolutions != null)
            {
                for (var i = 0; i < resolutions.Length; i++)
                {
                    var r = resolutions[i];
                    if (r.width <= 0 || r.height <= 0)
                    {
                        continue;
                    }

                    var key = Pack(r.width, r.height);
                    if (!seen.Add(key))
                    {
                        continue;
                    }

                    list.Add(new DisplayResolutionOption(r.width, r.height));
                }
            }

            list.Sort((a, b) =>
            {
                var w = b.Width.CompareTo(a.Width);
                return w != 0 ? w : b.Height.CompareTo(a.Height);
            });

            EnsureCurrentInList(list);
            return list;
        }

        public bool TryApply(int width, int height, DisplayWindowMode mode)
        {
            if (width <= 0 || height <= 0)
            {
                return false;
            }

            mode = ClampMode((int)mode);
            ApplyInternal(width, height, mode, persist: true);
            Changed?.Invoke();
            return true;
        }

        public static FullScreenMode ToFullScreenMode(DisplayWindowMode mode)
        {
            switch (mode)
            {
                case DisplayWindowMode.Borderless:
                    return FullScreenMode.FullScreenWindow;
                case DisplayWindowMode.Exclusive:
                    return FullScreenMode.ExclusiveFullScreen;
                default:
                    return FullScreenMode.Windowed;
            }
        }

        public static DisplayWindowMode FromFullScreenMode(FullScreenMode mode)
        {
            switch (mode)
            {
                case FullScreenMode.FullScreenWindow:
                case FullScreenMode.MaximizedWindow:
                    return DisplayWindowMode.Borderless;
                case FullScreenMode.ExclusiveFullScreen:
                    return DisplayWindowMode.Exclusive;
                default:
                    return DisplayWindowMode.Windowed;
            }
        }

        public static string ModeLabel(DisplayWindowMode mode)
        {
            switch (mode)
            {
                case DisplayWindowMode.Borderless:
                    return "无边框全屏";
                case DisplayWindowMode.Exclusive:
                    return "独占全屏";
                default:
                    return "窗口";
            }
        }

        private void ApplyInternal(int width, int height, DisplayWindowMode mode, bool persist)
        {
            _width = width;
            _height = height;
            _windowMode = mode;
            Screen.SetResolution(width, height, ToFullScreenMode(mode));
            if (persist)
            {
                PlayerPrefs.SetInt(PrefsWidth, width);
                PlayerPrefs.SetInt(PrefsHeight, height);
                PlayerPrefs.SetInt(PrefsWindowMode, (int)mode);
                PlayerPrefs.Save();
            }
        }

        private void EnsureCurrentInList(List<DisplayResolutionOption> list)
        {
            if (!_hasAppliedSnapshot && _width <= 0)
            {
                SnapshotCurrent();
            }

            var key = Pack(_width, _height);
            for (var i = 0; i < list.Count; i++)
            {
                if (Pack(list[i].Width, list[i].Height) == key)
                {
                    return;
                }
            }

            list.Insert(0, new DisplayResolutionOption(_width, _height));
        }

        private static DisplayWindowMode ClampMode(int raw)
        {
            if (raw < 0 || raw > (int)DisplayWindowMode.Exclusive)
            {
                return DisplayWindowMode.Windowed;
            }

            return (DisplayWindowMode)raw;
        }

        private static long Pack(int width, int height)
        {
            return ((long)width << 32) | (uint)height;
        }
    }
}
