using System;
using Gravedigger2026.Core.Pathing;

internal static class Runner
{
    private static int Main()
    {
        var error = FlowFieldCorrectnessChecks.RunAll();
        if (error == null)
        {
            Console.WriteLine("ALL CHECKS PASSED (FlowField gradient direction field, v0.74.11).");
            return 0;
        }

        Console.WriteLine("[FAIL] FlowFieldCorrectnessChecks:");
        Console.WriteLine(error);
        return 1;
    }
}
