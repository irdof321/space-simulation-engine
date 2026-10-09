using System;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Select what to run in Configuration/AppSettings.cs.
        switch (AppSettings.Mode)
        {
            case RunMode.LiveOrbits:
                LiveSimulationRunner.Run();
                break;
            case RunMode.LegacyBenchmark:
                LegacyBenchmark.Run();
                break;
            case RunMode.OrbitalSystemTests:
                OrbitalSystemTests.Run();
                break;
            case RunMode.KeplerConverterTests:
                KeplerConverterTests.Run();
                break;
            case RunMode.RecursiveInitializationTests:
                RecursiveInitializationTests.Run();
                break;
            case RunMode.MultiBodyInitializationTests:
                MultiBodyInitializationTests.Run();
                break;
            case RunMode.AxialRotationTests:
                AxialRotationTests.Run();
                break;
            case RunMode.AllTests:
                OrbitalSystemTests.Run();
                KeplerConverterTests.Run();
                RecursiveInitializationTests.Run();
                MultiBodyInitializationTests.Run();
                AxialRotationTests.Run();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(AppSettings.Mode));
        }
    }
}
