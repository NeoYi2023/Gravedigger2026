using System;
using Gravedigger2026.Core.Pathing;

internal static class Runner
{
    private static int Main()
    {
        var failed = false;
        failed |= Report("SoftCollisionCorrectnessChecks (SC-01 core)", SoftCollisionCorrectnessChecks.RunAll());
        failed |= Report("AttackSlotCorrectnessChecks (MP-02 + SC-02 surround)", AttackSlotCorrectnessChecks.RunAll());
        failed |= Report("SoftCollisionWireCorrectnessChecks (SC-03 wire)", SoftCollisionWireCorrectnessChecks.RunAll());

        if (!failed)
        {
            Console.WriteLine("ALL CHECKS PASSED (SC-01 core + SC-02 surround + SC-03 wire).");
            return 0;
        }

        return 1;
    }

    private static bool Report(string name, string error)
    {
        if (error == null)
        {
            Console.WriteLine($"[PASS] {name}");
            return false;
        }

        Console.WriteLine($"[FAIL] {name}:");
        Console.WriteLine(error);
        return true;
    }
}
