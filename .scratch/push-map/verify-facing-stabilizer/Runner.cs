using System;
using Gravedigger2026.Gameplay.PushMap;

internal static class Runner
{
    private static int Main()
    {
        var error = FacingStabilizerCorrectnessChecks.RunAll();
        if (error == null)
        {
            Console.WriteLine("ALL CHECKS PASSED (WarriorAnimView facing stabilizer, v0.75.21).");
            return 0;
        }

        Console.WriteLine("[FAIL] FacingStabilizerCorrectnessChecks:");
        Console.WriteLine(error);
        return 1;
    }
}
