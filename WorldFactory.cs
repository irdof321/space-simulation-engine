
using System;
using PhysicsSimulation;

public enum WorldPreset
{
    SunEarth,
    Miniature,
    MiniatureExtended,
    PerturbedSystem
}

public static class WorldFactory
{
    public const double AstronomicalUnit = 1.496e11;
    public const double Year = 365.25 * 24.0 * 3600.0;

    public static PhysicSimulation Create(
        WorldPreset preset,
        Integrator integrator)
    {
        var simulation = new PhysicSimulation(integrator);

        switch (preset)
        {
            case WorldPreset.SunEarth:
                CreateSunEarth(simulation);
                break;

            case WorldPreset.Miniature:
                CreateMiniature(simulation);
                break;

            case WorldPreset.MiniatureExtended:
                CreateMiniatureExtended(simulation);
                break;

            case WorldPreset.PerturbedSystem:
                CreatePerturbedSystem(simulation);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(preset));
        }

        simulation.Init();
        return simulation;
    }

    public static double GetReferenceDistance(WorldPreset preset)
    {
        return preset == WorldPreset.SunEarth
            ? AstronomicalUnit
            : 2000.0;
    }

    public static double GetReferencePeriod(WorldPreset preset)
    {
        if (preset == WorldPreset.SunEarth)
            return Year;

        // Approximate inner orbital period for the miniature presets.
        const double gravitationalConstant = 6.67430e-11;
        const double centralMass = 1.0e18;
        const double orbitalRadius = 2000.0;

        return 2.0 * Math.PI * Math.Sqrt(
            Math.Pow(orbitalRadius, 3) /
            (gravitationalConstant * centralMass));
    }

    private static void AddBody(
        PhysicSimulation simulation,
        double mass,
        double orbitalRadius)
    {
        simulation.AddCelestialBody(
            new CelestBody(
                mass,
                new Vector3Double(orbitalRadius, 0, 0)));
    }

    private static void CreateSunEarth(PhysicSimulation simulation)
    {
        AddBody(simulation, 1.989e30, 0.0);
        AddBody(simulation, 5.972e24, AstronomicalUnit);
    }

    private static void CreateMiniature(PhysicSimulation simulation)
    {
        // Central body: 10^18 kg
        AddBody(simulation, 1.0e18, 0.0);

        // Three small orbiting worlds
        AddBody(simulation, 2.0e10, 2000.0);
        AddBody(simulation, 5.0e10, 4000.0);
        AddBody(simulation, 3.0e10, 7000.0);
    }

    private static void CreateMiniatureExtended(
        PhysicSimulation simulation)
    {
        AddBody(simulation, 1.0e18, 0.0);

        AddBody(simulation, 2.0e10, 2000.0);
        AddBody(simulation, 5.0e10, 3200.0);
        AddBody(simulation, 3.0e10, 4700.0);
        AddBody(simulation, 8.0e10, 6500.0);
        AddBody(simulation, 4.0e10, 9000.0);
        AddBody(simulation, 6.0e10, 12000.0);
    }

    private static void CreatePerturbedSystem(
        PhysicSimulation simulation)
    {
        AddBody(simulation, 1.0e18, 0.0);

        // More massive orbiting bodies produce stronger interactions.
        AddBody(simulation, 2.0e15, 2000.0);
        AddBody(simulation, 5.0e15, 2800.0);
        AddBody(simulation, 3.0e15, 4100.0);
        AddBody(simulation, 8.0e15, 6000.0);
        AddBody(simulation, 4.0e15, 9000.0);
    }
}
