using System;
using Gravedigger2026.Core.Pathing;

internal static class Runner
{
    private static int Main()
    {
        var error = AttackSlotCorrectnessChecks.RunAll();
        if (error == null)
        {
            Console.WriteLine("ALL CHECKS PASSED (MP-02 + SC-02 Surround).");
            return 0;
        }

        Console.WriteLine("FAILED:");
        Console.WriteLine(error);
        return 1;
    }
}
