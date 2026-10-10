using System;
using System.Collections.Generic;
using PhysicsSimulation.Bodies;
using PhysicsSimulation.Common;
using PhysicsSimulation.Dynamics;
using PhysicsSimulation.Mathematics;
using PhysicsSimulation.Orbits;

internal static class MultiBodyInitializationTests
{
    public static void Run()
    {
        int passed = 0;
        int failed = 0;

        void Check(bool condition, string name)
        {
            Console.WriteLine($"[{(condition ? "PASS" : "FAIL")}] {name}");
            if (condition)
                passed++;
            else
                failed++;
        }

        bool Near(double actual, double expected, double scale, double tolerance = 1e-10)
        {
            if (!double.IsFinite(actual) || !double.IsFinite(expected))
                return false;

            return Math.Abs(actual - expected) <=
                tolerance * Math.Max(1.0, Math.Abs(scale));
        }

        bool NearVector(
            Vector3Double actual,
            Vector3Double expected,
            double scale,
            double tolerance = 1e-10)
        {
            return Near(actual.X, expected.X, scale, tolerance)
                && Near(actual.Y, expected.Y, scale, tolerance)
                && Near(actual.Z, expected.Z, scale, tolerance);
        }

        Vector3Double zero = new Vector3Double(0, 0, 0);

        // ============================================================
        // 1. CREATE BODIES WITH NONZERO INITIAL POSITIONS AND VELOCITIES
        // ============================================================

        var sun = new CelestBody(1.989e30, new Vector3Double(5e9, -2e9, 1e9));
        var earth = new CelestBody(5.972e24, new Vector3Double(-3e10, 1e9, 5e8));
        var moon = new CelestBody(7.348e22, new Vector3Double(-3e10 + 3.844e8, 1.1e9, 5e8));
        var mars = new CelestBody(6.417e23, new Vector3Double(-4e11, 3e10, -1e10));
        var jupiter = new CelestBody(1.898e27, new Vector3Double(3e11, -4e10, 2e10));

        sun.TranslateVelocity(new Vector3Double(12, -8, 4));
        earth.TranslateVelocity(new Vector3Double(-120, 230, 10));
        moon.TranslateVelocity(new Vector3Double(-80, 280, -25));
        mars.TranslateVelocity(new Vector3Double(130, -210, 18));
        jupiter.TranslateVelocity(new Vector3Double(-45, 35, 80));

        // ============================================================
        // 2. DEFINE ORBITAL PARAMETERS (SI UNITS, ANGLES IN RADIANS)
        // ============================================================

        var moonOrbit = new OrbitalParameters(
            384_400_000.0, 0.055, 0.09, 0.3, 0.7, 1.2);

        var earthOrbit = new OrbitalParameters(
            1.496e11, 0.0167, 0.05, 0.8, 0.25, 1.1);

        var marsOrbit = new OrbitalParameters(
            2.279e11, 0.0934, 0.12, 1.1, 0.35, 2.0);

        var jupiterOrbit = new OrbitalParameters(
            7.785e11, 0.0489, 0.08, 0.6, 1.4, 4.0);

        // ============================================================
        // 3. BUILD THE HIERARCHY
        // ============================================================

        // SolarSystem
        // |-- Sun
        // |-- EarthSystem
        // |   |-- Earth
        // |   `-- Moon
        // |-- Mars
        // `-- Jupiter

        var earthSystem = new OrbitalSystem();
        earthSystem.AddMember(new OrbitalMember(earth));
        earthSystem.AddMember(new OrbitalMember(moon, moonOrbit));

        var solarSystem = new OrbitalSystem();
        solarSystem.AddMember(new OrbitalMember(sun));
        solarSystem.AddMember(new OrbitalMember(earthSystem, earthOrbit));
        solarSystem.AddMember(new OrbitalMember(mars, marsOrbit));
        solarSystem.AddMember(new OrbitalMember(jupiter, jupiterOrbit));

        var bodies = new[] { sun, earth, moon, mars, jupiter };

        // Compute reference relative states before initialization.
        State expectedMoon = KeplerConverter.Convert(
            moonOrbit, Constants.G * (earth.Mass + moon.Mass));
        State expectedEarthSystem = KeplerConverter.Convert(
            earthOrbit, Constants.G * (sun.Mass + earthSystem.TotalMass));
        State expectedMars = KeplerConverter.Convert(
            marsOrbit, Constants.G * (sun.Mass + mars.Mass));
        State expectedJupiter = KeplerConverter.Convert(
            jupiterOrbit, Constants.G * (sun.Mass + jupiter.Mass));

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("MULTI-BODY ORBITAL INITIALIZATION");
        Console.WriteLine("========================================");

        OrbitalInitializer.Initialize(solarSystem);

        // ============================================================
        // TEST A: NESTED EARTH-MOON ORBIT
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Nested Earth-Moon orbit ---");

        Check(
            NearVector(moon.Position - earth.Position,
                expectedMoon.Y, expectedMoon.Y.GetMagnitude()),
            "Moon: correct position relative to Earth");

        Check(
            NearVector(moon.Speed - earth.Speed,
                expectedMoon.YDot, expectedMoon.YDot.GetMagnitude()),
            "Moon: correct velocity relative to Earth");

        // ============================================================
        // TEST B: ALL PLANETARY ORBITS RELATIVE TO THE SUN
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Orbits relative to the Sun ---");

        Check(
            NearVector(earthSystem.CenterOfMass - sun.Position,
                expectedEarthSystem.Y, expectedEarthSystem.Y.GetMagnitude()),
            "Earth-Moon barycenter: correct heliocentric position");

        Check(
            NearVector(earthSystem.CenterOfMassVelocity - sun.Speed,
                expectedEarthSystem.YDot, expectedEarthSystem.YDot.GetMagnitude()),
            "Earth-Moon barycenter: correct heliocentric velocity");

        Check(
            NearVector(mars.Position - sun.Position,
                expectedMars.Y, expectedMars.Y.GetMagnitude()),
            "Mars: correct heliocentric position");

        Check(
            NearVector(mars.Speed - sun.Speed,
                expectedMars.YDot, expectedMars.YDot.GetMagnitude()),
            "Mars: correct heliocentric velocity");

        Check(
            NearVector(jupiter.Position - sun.Position,
                expectedJupiter.Y, expectedJupiter.Y.GetMagnitude()),
            "Jupiter: correct heliocentric position");

        Check(
            NearVector(jupiter.Speed - sun.Speed,
                expectedJupiter.YDot, expectedJupiter.YDot.GetMagnitude()),
            "Jupiter: correct heliocentric velocity");

        // ============================================================
        // TEST C: GLOBAL BARYCENTER AND TOTAL MOMENTUM
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Global barycenter and momentum ---");

        double totalMass = 0.0;
        Vector3Double massWeightedPosition = zero;
        Vector3Double totalMomentum = zero;

        foreach (CelestBody body in bodies)
        {
            totalMass += body.Mass;
            massWeightedPosition += body.Position * body.Mass;
            totalMomentum += body.Speed * body.Mass;
        }

        Vector3Double independentlyComputedBarycenter =
            massWeightedPosition / totalMass;

        Vector3Double independentlyComputedBarycenterVelocity =
            totalMomentum / totalMass;

        double positionScale = jupiterOrbit.SemiMajorAxis;
        double velocityScale = expectedJupiter.YDot.GetMagnitude();

        Check(
            Near(totalMass, solarSystem.TotalMass, totalMass),
            "Global mass equals sum of leaf masses");

        Check(
            NearVector(solarSystem.CenterOfMass, zero, positionScale),
            "Composite barycenter is at origin");

        Check(
            NearVector(solarSystem.CenterOfMassVelocity, zero, velocityScale),
            "Composite barycenter velocity is zero");

        Check(
            NearVector(independentlyComputedBarycenter, zero, positionScale),
            "Mass-weighted leaf barycenter is at origin");

        Check(
            NearVector(independentlyComputedBarycenterVelocity, zero, velocityScale),
            "Total linear momentum is approximately zero");

        // ============================================================
        // TEST D: TREE TRAVERSAL AND BODY IDENTITY
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Tree traversal ---");

        var flattenedBodies = new List<CelestBody>(solarSystem.GetBodies());

        bool correctBodyOrder = flattenedBodies.Count == bodies.Length;
        if (correctBodyOrder)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                if (!ReferenceEquals(flattenedBodies[i], bodies[i]))
                {
                    correctBodyOrder = false;
                    break;
                }
            }
        }

        Check(correctBodyOrder, "Five bodies returned in order, without cloning");

        // ============================================================
        // TEST E: REPEATED INITIALIZATION
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Repeated initialization ---");

        Vector3Double[] positionsBeforeRepeat = new Vector3Double[bodies.Length];
        Vector3Double[] velocitiesBeforeRepeat = new Vector3Double[bodies.Length];

        for (int i = 0; i < bodies.Length; i++)
        {
            positionsBeforeRepeat[i] = bodies[i].Position;
            velocitiesBeforeRepeat[i] = bodies[i].Speed;
        }

        OrbitalInitializer.Initialize(solarSystem);

        bool positionsPreserved = true;
        bool velocitiesPreserved = true;

        for (int i = 0; i < bodies.Length; i++)
        {
            positionsPreserved &= NearVector(
                bodies[i].Position, positionsBeforeRepeat[i], positionScale);

            velocitiesPreserved &= NearVector(
                bodies[i].Speed, velocitiesBeforeRepeat[i], velocityScale);
        }

        Check(positionsPreserved, "Second initialization preserves all five positions");
        Check(velocitiesPreserved, "Second initialization preserves all five velocities");

        // ============================================================
        // RESULTS
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine($"PASSED: {passed}");
        Console.WriteLine($"FAILED: {failed}");
        Console.WriteLine("========================================");

        if (failed > 0)
        {
            throw new InvalidOperationException(
                $"{failed} multi-body orbital initialization test(s) failed.");
        }

        Console.WriteLine("All multi-body orbital initialization tests passed.");
    }
}
