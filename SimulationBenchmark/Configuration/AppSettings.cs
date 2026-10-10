using System;
using System.Collections.Generic;
using PhysicsSimulation.Dynamics;

// This is the main entry point for changing runs and numerical parameters.
internal enum RunMode
{
    LiveOrbits,
    LegacyBenchmark,
    AllTests,
    OrbitalSystemTests,
    KeplerConverterTests,
    RecursiveInitializationTests,
    MultiBodyInitializationTests,
    AxialRotationTests
}

internal enum LiveScenePreset
{
    OriginalSolarSystem,
    TwoDistantSystems
}

internal enum IntegratorKind
{
    Euler,
    RK4,
    Verlet
}

internal static class AppSettings
{
    // Change this to run the original convergence / energy benchmark or tests.
    public const RunMode Mode = RunMode.LiveOrbits;

    // Change this to switch between the original and two-system scenes.
    public const LiveScenePreset LiveScene = LiveScenePreset.TwoDistantSystems;

    // Live N-body simulation settings.
    // Six years are enough to see Comet C1 make one approximately 5.2-year orbit.
    public const int SimulationYears = 6000;
    public const double TimeStepHours = 1.0;
    public const IntegratorKind LiveIntegrator = IntegratorKind.Verlet;
    // The physics runs N steps on every physics timer tick, regardless of paint rate.
    // WinForms timers share the UI thread: expensive frames may delay ticks.
    public const int PhysicsUpdatesPerSecond = 30;
    public const int InitialStepsPerUpdate = 8;

    // Target display refresh rate (adjustable while running).
    // 2 FPS leaves time for physics even when each full redraw costs ~200 ms.
    public const int AnimationFramesPerSecond = 2;
    public const int TrailSampleEverySteps = 8;
    public const int MaxTrailSamples = 3500;

    // Original integrator benchmark settings.
    public const WorldPreset BenchmarkWorld = WorldPreset.MiniatureExtended;
    public const bool BenchmarkEuler = false;
    public const bool BenchmarkRK4 = true;
    public const bool BenchmarkVerlet = true;

    public const double SecondsPerDay = 86_400.0;
    public const double SecondsPerYear = 365.25 * SecondsPerDay;
    public const double AstronomicalUnit = 1.496e11;
    public const double LightYear = 9.4607304725808e15;

    public static Integrator CreateLiveIntegrator(double timeStep)
    {
        return LiveIntegrator switch
        {
            IntegratorKind.Euler => new EulerIntegrator(timeStep),
            IntegratorKind.RK4 => new RK4Integrator(timeStep),
            IntegratorKind.Verlet => new VerletIntegrator(timeStep),
            _ => throw new InvalidOperationException("Unknown integrator.")
        };
    }

    public static (string Name, Func<double, Integrator> Factory)[] CreateBenchmarkIntegrators()
    {
        var integrators = new List<(string Name, Func<double, Integrator> Factory)>();

        if (BenchmarkEuler)
            integrators.Add(("Euler", dt => new EulerIntegrator(dt)));
        if (BenchmarkRK4)
            integrators.Add(("RK4", dt => new RK4Integrator(dt)));
        if (BenchmarkVerlet)
            integrators.Add(("Verlet", dt => new VerletIntegrator(dt)));

        if (integrators.Count == 0)
            throw new InvalidOperationException("Select at least one benchmark integrator.");

        return integrators.ToArray();
    }
}
