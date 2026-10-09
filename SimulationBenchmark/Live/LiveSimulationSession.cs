using System;
using System.Collections.Generic;
using PhysicsSimulation;

// Owns one initialized experiment. UI code only controls its playback.
internal sealed class LiveSimulationSession
{
    public OrbitalScene Scene { get; }
    public PhysicSimulation Simulation { get; }
    public double TimeStepSeconds { get; }
    public int StepsPerYear { get; }
    public int TotalSteps { get; }
    public double InitialEnergy { get; }
    public Vector3Double InitialMomentum { get; }
    public double MomentumScale { get; }

    public double ElapsedYears => Simulation.Time / AppSettings.SecondsPerYear;
    public double TotalYears => TotalSteps * TimeStepSeconds / AppSettings.SecondsPerYear;

    private LiveSimulationSession(
        OrbitalScene scene, PhysicSimulation simulation, double timeStepSeconds,
        int stepsPerYear, int totalSteps)
    {
        Scene = scene;
        Simulation = simulation;
        TimeStepSeconds = timeStepSeconds;
        StepsPerYear = stepsPerYear;
        TotalSteps = totalSteps;
        InitialEnergy = simulation.GetTotalEnergy();
        InitialMomentum = simulation.GetTotalMomentum();

        double scale = 0;
        foreach (CelestBody body in simulation.Bodies)
            scale += body.Mass * body.Speed.GetMagnitude();
        MomentumScale = scale;
    }

    public static LiveSimulationSession Create()
    {
        if (AppSettings.SimulationYears <= 0 ||
            !double.IsFinite(AppSettings.TimeStepHours) || AppSettings.TimeStepHours <= 0)
        {
            throw new InvalidOperationException("Simulation years and time step must be positive.");
        }

        double timeStep = AppSettings.TimeStepHours * 3600.0;
        int stepsPerYear = checked((int)Math.Round(AppSettings.SecondsPerYear / timeStep));
        int totalSteps = checked(stepsPerYear * AppSettings.SimulationYears);
        if (stepsPerYear == 0)
            throw new InvalidOperationException("Physics step exceeds the yearly duration.");

        OrbitalScene scene = SolarSystemPreset.Create();
        OrbitalInitializer.Initialize(scene.Root);

        List<CelestBody> bodies = new List<CelestBody>(scene.Root.GetBodies());
        if (bodies.Count == 0 || bodies.Count != scene.Markers.Count)
            throw new InvalidOperationException("Scene hierarchy and body catalog do not agree.");

        var seen = new HashSet<CelestBody>();
        var simulation = new PhysicSimulation(AppSettings.CreateLiveIntegrator(timeStep));
        foreach (CelestBody body in bodies)
        {
            if (!seen.Add(body))
                throw new InvalidOperationException("A celestial body appears more than once in the hierarchy.");
            simulation.AddCelestialBody(body);
        }

        // Do not call the legacy PhysicSimulation.Init(), which overwrites initial orbits.
        return new LiveSimulationSession(scene, simulation, timeStep, stepsPerYear, totalSteps);
    }

    public double RelativeEnergyError => InitialEnergy == 0.0
        ? double.NaN
        : Math.Abs((Simulation.GetTotalEnergy() - InitialEnergy) / InitialEnergy);

    public double RelativeMomentumDrift => MomentumScale == 0.0
        ? 0.0
        : (Simulation.GetTotalMomentum() - InitialMomentum).GetMagnitude() / MomentumScale;

    public void ValidateFiniteState()
    {
        foreach (CelestBody body in Simulation.Bodies)
        {
            if (!double.IsFinite(body.Position.X) ||
                !double.IsFinite(body.Position.Y) ||
                !double.IsFinite(body.Position.Z) ||
                !double.IsFinite(body.Speed.X) ||
                !double.IsFinite(body.Speed.Y) ||
                !double.IsFinite(body.Speed.Z))
            {
                throw new InvalidOperationException("Simulation produced a non-finite state.");
            }
        }
    }

    public string FormatStatus()
    {
        string result = $"t = {ElapsedYears:F3} / {TotalYears:F1} years";
        int count = Math.Min(2, Scene.Readouts.Count);
        for (int i = 0; i < count; i++)
        {
            DistanceReadout readout = Scene.Readouts[i];
            result += $"    {readout.Label} = {Scene.FormatDistance(readout)}";
        }
        return result + $"    |dE/E0| = {RelativeEnergyError:E2}";
    }

    public void PrintIntro()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("HIERARCHICAL N-BODY SIMULATION");
        Console.WriteLine("========================================");
        Console.WriteLine($"Integrator: {AppSettings.LiveIntegrator} | Time step: {AppSettings.TimeStepHours:G} hours");
        Console.WriteLine($"Duration: {TotalYears:F3} years | Steps: {TotalSteps}");
        Console.WriteLine($"Bodies: {Simulation.Bodies.Count} | Initial CM: {Scene.Root.CenterOfMass.GetMagnitude():E3} m");
        Console.Write("Year");
        foreach (DistanceReadout readout in Scene.Readouts)
            Console.Write($" | {readout.Label,17}");
        Console.WriteLine(" |  |dE/E0|  | |dP|/scale");
    }

    public void PrintSample()
    {
        Console.Write($"{ElapsedYears,5:F2}");
        foreach (DistanceReadout readout in Scene.Readouts)
            Console.Write($" | {Scene.FormatDistance(readout),17}");
        Console.WriteLine($" | {RelativeEnergyError,9:E2} | {RelativeMomentumDrift,11:E2}");
        ValidateFiniteState();
    }

    public void PrintCompletion(bool completed)
    {
        if (!completed)
        {
            Console.WriteLine("Simulation window closed before the run completed.");
            return;
        }
        Console.WriteLine($"Final center-of-mass position: {Scene.Root.CenterOfMass.GetMagnitude():E4} m");
        Console.WriteLine($"Final center-of-mass velocity: {Scene.Root.CenterOfMassVelocity.GetMagnitude():E4} m/s");
        Console.WriteLine("Simulation completed.");
    }
}
