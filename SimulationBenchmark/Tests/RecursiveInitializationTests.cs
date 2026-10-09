using System;
using System.Collections.Generic;
using PhysicsSimulation;

internal static class RecursiveInitializationTests
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

        bool Near(double actual, double expected, double scale)
        {
            if (!double.IsFinite(actual) || !double.IsFinite(expected))
                return false;

            double tolerance = 1e-10 * Math.Max(1.0, Math.Abs(scale));

            return Math.Abs(actual - expected) <= tolerance;
        }

        bool NearVector(
            Vector3Double actual,
            Vector3Double expected,
            double scale)
        {
            return Near(actual.X, expected.X, scale)
                && Near(actual.Y, expected.Y, scale)
                && Near(actual.Z, expected.Z, scale);
        }

        // ============================================================
        // 1. CREATE CELESTIAL BODIES
        // ============================================================

        var sun = new CelestBody(
            1.989e30,
            new Vector3Double(5e9, 0, 0));

        var earth = new CelestBody(
            5.972e24,
            new Vector3Double(-1e9, 0, 0));

        var moon = new CelestBody(
            7.348e22,
            new Vector3Double(3e8, 0, 0));

        // ============================================================
        // 2. DEFINE ORBITAL PARAMETERS
        // ============================================================

        var moonOrbit = new OrbitalParameters(
            384_400_000.0,
            0.055,
            0.09,
            0.3,
            0.7,
            1.2);

        var earthSunOrbit = new OrbitalParameters(
            1.496e11,
            0.0167,
            0.05,
            0.8,
            0.25,
            1.1);

        // ============================================================
        // 3. BUILD HIERARCHY
        // ============================================================

        var earthSystem = new OrbitalSystem();

        earthSystem.AddMember(new OrbitalMember(earth));
        earthSystem.AddMember(new OrbitalMember(moon, moonOrbit));

        var solarSystem = new OrbitalSystem();

        solarSystem.AddMember(new OrbitalMember(sun));
        solarSystem.AddMember(
            new OrbitalMember(earthSystem, earthSunOrbit));

        // ============================================================
        // 4. EXPECTED RELATIVE ORBITAL STATES
        // ============================================================

        State expectedMoonOrbit = KeplerConverter.Convert(
            moonOrbit,
            Constants.G * (earth.Mass + moon.Mass));

        State expectedEarthSunOrbit = KeplerConverter.Convert(
            earthSunOrbit,
            Constants.G * (sun.Mass + earthSystem.TotalMass));

        // ============================================================
        // 5. INITIALIZE RECURSIVELY
        // ============================================================

        Console.WriteLine("========================================");
        Console.WriteLine("RECURSIVE ORBITAL INITIALIZATION");
        Console.WriteLine("========================================");

        OrbitalInitializer.Initialize(solarSystem);

        // ============================================================
        // TEST A: EARTH-MOON ORBIT
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Earth-Moon orbit ---");

        Vector3Double moonRelativePosition =
            moon.Position - earth.Position;

        Vector3Double moonRelativeVelocity =
            moon.Speed - earth.Speed;

        Check(
            NearVector(
                moonRelativePosition,
                expectedMoonOrbit.Y,
                expectedMoonOrbit.Y.GetMagnitude()),
            "Moon: correct relative position");

        Check(
            NearVector(
                moonRelativeVelocity,
                expectedMoonOrbit.YDot,
                expectedMoonOrbit.YDot.GetMagnitude()),
            "Moon: correct relative velocity");

        // ============================================================
        // TEST B: EARTH-SUN ORBIT
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Earth-Sun orbit ---");

        Vector3Double earthSystemRelativePosition =
            earthSystem.CenterOfMass - sun.Position;

        Vector3Double earthSystemRelativeVelocity =
            earthSystem.CenterOfMassVelocity - sun.Speed;

        Check(
            NearVector(
                earthSystemRelativePosition,
                expectedEarthSunOrbit.Y,
                expectedEarthSunOrbit.Y.GetMagnitude()),
            "Earth-Moon system: correct relative position");

        Check(
            NearVector(
                earthSystemRelativeVelocity,
                expectedEarthSunOrbit.YDot,
                expectedEarthSunOrbit.YDot.GetMagnitude()),
            "Earth-Moon system: correct relative velocity");

        // ============================================================
        // TEST C: GLOBAL BARYCENTER
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Global barycenter ---");

        Vector3Double zero = new Vector3Double(0, 0, 0);

        Check(
            NearVector(
                solarSystem.CenterOfMass,
                zero,
                earthSunOrbit.SemiMajorAxis),
            "Solar system: barycenter at origin");

        Check(
            NearVector(
                solarSystem.CenterOfMassVelocity,
                zero,
                expectedEarthSunOrbit.YDot.GetMagnitude()),
            "Solar system: barycenter velocity is zero");

        // ============================================================
        // TEST D: RECURSIVE BODY TRAVERSAL
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Recursive body traversal ---");

        var bodies = new List<CelestBody>(solarSystem.GetBodies());

        Check(
            bodies.Count == 3 &&
            ReferenceEquals(bodies[0], sun) &&
            ReferenceEquals(bodies[1], earth) &&
            ReferenceEquals(bodies[2], moon),
            "Hierarchy contains the correct three bodies");

        // ============================================================
        // TEST E: REPEATED INITIALIZATION
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Repeated initialization ---");

        Vector3Double initialSunPosition = sun.Position;
        Vector3Double initialEarthPosition = earth.Position;
        Vector3Double initialMoonPosition = moon.Position;

        Vector3Double initialSunVelocity = sun.Speed;
        Vector3Double initialEarthVelocity = earth.Speed;
        Vector3Double initialMoonVelocity = moon.Speed;

        OrbitalInitializer.Initialize(solarSystem);

        Check(
            NearVector(sun.Position, initialSunPosition, earthSunOrbit.SemiMajorAxis) &&
            NearVector(earth.Position, initialEarthPosition, earthSunOrbit.SemiMajorAxis) &&
            NearVector(moon.Position, initialMoonPosition, earthSunOrbit.SemiMajorAxis),
            "Repeated initialization preserves positions");

        double velocityScale = expectedEarthSunOrbit.YDot.GetMagnitude();

        Check(
            NearVector(sun.Speed, initialSunVelocity, velocityScale) &&
            NearVector(earth.Speed, initialEarthVelocity, velocityScale) &&
            NearVector(moon.Speed, initialMoonVelocity, velocityScale),
            "Repeated initialization preserves velocities");

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
                $"{failed} recursive orbital initialization test(s) failed.");
        }

        Console.WriteLine("All recursive initialization tests passed.");
    }
}
