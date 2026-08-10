using System;
using UnityEngine;

/// <summary>
/// Offline copy of Gravedigger2026.Gameplay.Combat.StuckHoldTracker (SPEC_04 §15.5 v0.75.30).
/// Keep in sync with Assets/Scripts/Gameplay/Combat/StuckHoldTracker.cs.
/// </summary>
internal sealed class StuckHoldTrackerCopy
{
    public const float DetectWindowSeconds = 0.5f;
    public const float DisplacementEpsilon = 0.2f;
    public const float HoldSeconds = 1f;

    private Vector3 _windowStartPos;
    private float _windowTimer;
    private float _holdTimer;
    private bool _holding;
    private bool _hasWindowStart;

    public bool IsHolding => _holding;

    public void Reset()
    {
        _holding = false;
        _windowTimer = 0f;
        _holdTimer = 0f;
        _hasWindowStart = false;
    }

    public void Tick(bool wantsMove, Vector3 worldPos, float dt)
    {
        if (dt < 0f)
        {
            dt = 0f;
        }

        if (_holding)
        {
            _holdTimer += dt;
            if (_holdTimer >= HoldSeconds)
            {
                _holding = false;
                _holdTimer = 0f;
                _windowTimer = 0f;
                _windowStartPos = worldPos;
                _hasWindowStart = true;
            }

            return;
        }

        if (!wantsMove)
        {
            _windowTimer = 0f;
            _windowStartPos = worldPos;
            _hasWindowStart = true;
            return;
        }

        if (!_hasWindowStart)
        {
            _windowStartPos = worldPos;
            _hasWindowStart = true;
            _windowTimer = 0f;
        }

        _windowTimer += dt;
        if (_windowTimer < DetectWindowSeconds)
        {
            return;
        }

        var dx = worldPos.x - _windowStartPos.x;
        var dz = worldPos.z - _windowStartPos.z;
        var displacementSqr = dx * dx + dz * dz;
        var epsilon = DisplacementEpsilon;
        if (displacementSqr < epsilon * epsilon)
        {
            _holding = true;
            _holdTimer = 0f;
        }

        _windowTimer = 0f;
        _windowStartPos = worldPos;
    }
}

internal static class StuckHoldCorrectnessChecks
{
    public static string RunAll()
    {
        if (!Near(StuckHoldTrackerCopy.DetectWindowSeconds, 0.5f) ||
            !Near(StuckHoldTrackerCopy.DisplacementEpsilon, 0.2f) ||
            !Near(StuckHoldTrackerCopy.HoldSeconds, 1f))
        {
            return "constants mismatch";
        }

        var t = new StuckHoldTrackerCopy();
        var pos = new Vector3(1f, 0f, 1f);
        t.Tick(true, pos, 0.4f);
        if (t.IsHolding)
        {
            return "held before detect window";
        }

        t.Tick(true, pos, 0.2f);
        if (!t.IsHolding)
        {
            return "expected hold after 0.5s with zero displacement";
        }

        t.Tick(true, pos, 0.99f);
        if (!t.IsHolding)
        {
            return "hold ended before 1s";
        }

        t.Tick(true, pos, 0.02f);
        if (t.IsHolding)
        {
            return "hold should end after 1s";
        }

        t.Reset();
        t.Tick(true, Vector3.zero, 0.4f);
        t.Tick(false, Vector3.zero, 0.1f);
        t.Tick(true, Vector3.zero, 0.4f);
        if (t.IsHolding)
        {
            return "wantsMove=false should reset detect window";
        }

        t.Tick(true, Vector3.zero, 0.2f);
        if (!t.IsHolding)
        {
            return "full window after reset should enter hold";
        }

        // Moving enough within window → no hold
        t.Reset();
        t.Tick(true, Vector3.zero, 0.25f);
        t.Tick(true, new Vector3(0.25f, 0f, 0f), 0.25f);
        if (t.IsHolding)
        {
            return "displacement >= epsilon should not hold";
        }

        return null;
    }

    private static bool Near(float a, float b) => Math.Abs(a - b) < 1e-5f;
}

internal static class Runner
{
    private static int Main()
    {
        var error = StuckHoldCorrectnessChecks.RunAll();
        if (error == null)
        {
            Console.WriteLine("ALL CHECKS PASSED (StuckHoldTracker, v0.75.30).");
            return 0;
        }

        Console.WriteLine("[FAIL] StuckHoldCorrectnessChecks:");
        Console.WriteLine(error);
        return 1;
    }
}
