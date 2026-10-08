
using System;
using System.Collections.Generic;
using PhysicsSimulation;

public static class BenchmarkRunner
{
    public const double Year = 365.25 * 24.0 * 3600.0;
    public const double AstronomicalUnit = 1.496e11;

    public static PhysicSimulation CreateSimulation(
        Integrator integrator,
        WorldPreset world = WorldPreset.SunEarth)
    {
        return WorldFactory.Create(world, integrator);
    }

    public static Vector3Double GetRelativePosition(
        PhysicSimulation simulation)
    {
        if (simulation.Bodies.Count < 2)
        {
            throw new InvalidOperationException(
                "The simulation must contain at least two bodies.");
        }

        return simulation.Bodies[1].Position
             - simulation.Bodies[0].Position;
    }

    public static Vector3Double ComputeReferencePosition(
        double endTime,
        double referenceDt,
        WorldPreset world = WorldPreset.SunEarth)
    {
        var simulation = CreateSimulation(
            new RK4Integrator(referenceDt),
            world);

        simulation.Update(endTime);

        return GetRelativePosition(simulation);
    }

    // Samples energy without changing the requested integration step.
    // Measurements are taken after an integer number of steps.
    public static BenchmarkResult Run(
        string integratorName,
        Func<double, Integrator> integratorFactory,
        double dt,
        double endTime,
        int sampleEverySteps,
        Vector3Double? referencePosition = null,
        WorldPreset world = WorldPreset.SunEarth)
    {
        if (dt <= 0 || !double.IsFinite(dt))
        {
            throw new ArgumentOutOfRangeException(nameof(dt));
        }

        if (endTime <= 0 || !double.IsFinite(endTime))
        {
            throw new ArgumentOutOfRangeException(nameof(endTime));
        }

        if (sampleEverySteps <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sampleEverySteps));
        }

        var simulation = CreateSimulation(
            integratorFactory(dt),
            world);

        var result = new BenchmarkResult(integratorName, dt);

        double initialEnergy = simulation.GetTotalEnergy();

        result.AddMeasurement(0.0, 0.0);

        // Execute complete steps with a constant time step.
        long fullSteps = (long)Math.Floor(endTime / dt);

        double remaining = endTime - fullSteps * dt;

        bool hasRemainder = remaining > 1e-9 * dt;

        for (long step = 1; step <= fullSteps; step++)
        {
            simulation.Update(dt);

            if (step % sampleEverySteps == 0 ||
                (step == fullSteps && !hasRemainder))
            {
                RecordEnergy(
                    simulation,
                    result,
                    initialEnergy);
            }
        }

        // Complete the requested duration for convergence tests.
        if (hasRemainder)
        {
            simulation.Update(remaining);

            RecordEnergy(
                simulation,
                result,
                initialEnergy);
        }

        // Compute final position error against the reference simulation.
        if (referencePosition.HasValue)
        {
            double referenceDistance =
                WorldFactory.GetReferenceDistance(world);

            double positionError =
                (GetRelativePosition(simulation) - referencePosition.Value)
                .GetMagnitude() / referenceDistance;

            result.SetFinalPositionError(positionError);
        }

        return result;
    }

    private static void RecordEnergy(
        PhysicSimulation simulation,
        BenchmarkResult result,
        double initialEnergy)
    {
        double currentEnergy = simulation.GetTotalEnergy();

        double relativeEnergyError =
            (currentEnergy - initialEnergy) /
            Math.Abs(initialEnergy);

        result.AddMeasurement(
            simulation.Time,
            relativeEnergyError);
    }
}
