using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PhysicsSimulation;

internal static class LegacyBenchmark
{
    public static void Run() { 
        // ============================================================
        // WORLD SELECTION - Change only this variable
        // ============================================================
        WorldPreset world = AppSettings.BenchmarkWorld;
        // WorldPreset.SunEarth
        // WorldPreset.Miniature
        // WorldPreset.MiniatureExtended
        // WorldPreset.PerturbedSystem
        // ============================================================

        var integrators = AppSettings.CreateBenchmarkIntegrators();

        double orbitalPeriod = WorldFactory.GetReferencePeriod(world);
        Console.WriteLine($"Selected world: {world}");
        Console.WriteLine($"Reference orbital period: {orbitalPeriod:F6} seconds");
        Console.WriteLine();

        // ============================================================
        // EXPERIMENT 0: LINEAR AND ANGULAR MOMENTUM CONSERVATION
        // ============================================================
        Console.WriteLine("MOMENTUM CONSERVATION TEST (100 reference periods)");
        Console.WriteLine("Integrator | Initial |P| | Final |P| | Relative P drift | Initial |L| | Final |L| | Relative L drift");

        const int momentumTestOrbits = 100;
        const int momentumStepsPerOrbit = 100;
        double momentumDt = orbitalPeriod / momentumStepsPerOrbit;

        foreach (var integrator in integrators)
        {
            var simulation = BenchmarkRunner.CreateSimulation(
                integrator.Factory(momentumDt), world);

            Vector3Double initialMomentum = simulation.GetTotalMomentum();
            Vector3Double initialAngularMomentum = GetTotalAngularMomentum(simulation);

            double momentumScale = 0.0;
            double angularMomentumScale = 0.0;

            foreach (var body in simulation.Bodies)
            {
                momentumScale += body.Mass * body.Speed.GetMagnitude();
                angularMomentumScale += body.Mass *
                    Cross(body.Position, body.Speed).GetMagnitude();
            }

            int totalSteps = momentumTestOrbits * momentumStepsPerOrbit;
            for (int step = 0; step < totalSteps; step++)
                simulation.Update(momentumDt);

            Vector3Double finalMomentum = simulation.GetTotalMomentum();
            Vector3Double finalAngularMomentum = GetTotalAngularMomentum(simulation);

            double relativeMomentumDrift = RelativeDrift(
                (finalMomentum - initialMomentum).GetMagnitude(), momentumScale);
            double relativeAngularMomentumDrift = RelativeDrift(
                (finalAngularMomentum - initialAngularMomentum).GetMagnitude(),
                angularMomentumScale);

            Console.WriteLine(
                $"{integrator.Name,10} | " +
                $"{initialMomentum.GetMagnitude(),11:E4} | " +
                $"{finalMomentum.GetMagnitude(),11:E4} | " +
                $"{relativeMomentumDrift,16:E4} | " +
                $"{initialAngularMomentum.GetMagnitude(),11:E4} | " +
                $"{finalAngularMomentum.GetMagnitude(),11:E4} | " +
                $"{relativeAngularMomentumDrift,16:E4}");
        }

        Console.WriteLine();

        // ============================================================
        // EXPERIMENT A: CONVERGENCE
        // ============================================================
        double endTime = 20.0 * orbitalPeriod;
        double referenceDt = orbitalPeriod / 2000.0;
        double[] timeSteps =
        {
            orbitalPeriod / 100.0,
            orbitalPeriod / 80.0,
            orbitalPeriod / 60.0,
            orbitalPeriod / 50.0,
            orbitalPeriod / 40.0,
            orbitalPeriod / 30.0,
            orbitalPeriod / 25.0
        };

        Vector3Double reference = BenchmarkRunner.ComputeReferencePosition(
            endTime, referenceDt, world);
        var annualResults = new List<BenchmarkResult>();

        Console.WriteLine("CONVERGENCE TEST (20 reference periods)");
        Console.WriteLine("Integrator | dt (s) | final |relative energy error| | normalized position error");

        foreach (var integrator in integrators)
        {
            foreach (double dt in timeSteps)
            {
                int sampleEverySteps = Math.Max(
                    1, (int)Math.Ceiling(0.5 * orbitalPeriod / dt));

                BenchmarkResult result = BenchmarkRunner.Run(
                    integrator.Name, integrator.Factory, dt, endTime,
                    sampleEverySteps, reference, world);
                annualResults.Add(result);

                Console.WriteLine(
                    $"{integrator.Name,10} | {dt,10:G6} | " +
                    $"{Math.Abs(result.RelativeEnergyErrors[^1]),28:E6} | " +
                    $"{result.FinalPositionError,28:E6}");
            }
        }

        // ============================================================
        // EXPERIMENT B: LONG-TERM ENERGY CONSERVATION
        // ============================================================
        const int longTermOrbits = 1000;
        const int stepsPerOrbit = 100;
        const int sampleEveryStepsLong = 10;
        double longStep = orbitalPeriod / stepsPerOrbit;
        double longDuration = longTermOrbits * orbitalPeriod;
        var longTermResults = new List<BenchmarkResult>();

        Console.WriteLine();
        Console.WriteLine($"LONG-TERM ENERGY TEST: {longTermOrbits} reference periods");
        Console.WriteLine($"Integration step: {longStep:G6} seconds");
        Console.WriteLine("Integrator | maximum absolute relative energy error | final signed relative error");

        foreach (var integrator in integrators)
        {
            BenchmarkResult result = BenchmarkRunner.Run(
                integrator.Name, integrator.Factory, longStep, longDuration,
                sampleEveryStepsLong, referencePosition: null, world: world);
            longTermResults.Add(result);

            double maximumError = 0.0;
            foreach (double error in result.RelativeEnergyErrors)
                maximumError = Math.Max(maximumError, Math.Abs(error));

            Console.WriteLine(
                $"{integrator.Name,10} | {maximumError,38:E6} | " +
                $"{result.RelativeEnergyErrors[^1],28:E6}");
        }

        // ============================================================
        // DISPLAY RESULTS
        // ============================================================
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        using var form = new BenchmarkForm();
        form.DisplayResults(annualResults, longTermResults, orbitalPeriod);
        Application.Run(form);
    }

    private static Vector3Double GetTotalAngularMomentum(PhysicSimulation simulation)
    {
        var total = new Vector3Double(0.0, 0.0, 0.0);
        foreach (var body in simulation.Bodies)
            total += Cross(body.Position, body.Speed) * body.Mass;
        return total;
    }

    private static Vector3Double Cross(Vector3Double a, Vector3Double b)
    {
        return new Vector3Double(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);
    }

    private static double RelativeDrift(double absoluteDrift, double scale)
    {
        return scale > 0.0 ? absoluteDrift / scale : absoluteDrift;
    }
}
