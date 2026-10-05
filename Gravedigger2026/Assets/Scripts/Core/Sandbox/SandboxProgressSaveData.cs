using System;

namespace Gravedigger2026.Core.Sandbox
{
    /// <summary>
    /// PlayerPrefs JSON for SandboxProgress (SPEC_04 §6 / SPEC_03 §3.20).
    /// </summary>
    [Serializable]
    public sealed class SandboxProgressSaveData
    {
        public SandboxNodeProgressSaveData[] Nodes = Array.Empty<SandboxNodeProgressSaveData>();

        /// <summary>Activated CapturePointIds. Slice 05 writes these; slice 01 only round-trips them.</summary>
        public string[] Captures = Array.Empty<string>();
    }

    [Serializable]
    public sealed class SandboxNodeProgressSaveData
    {
        public string NodeId;
        public int RemainingEnterCount;
        public bool Cleared;
    }
}
