using System;
using System.Collections.Generic;
using PhysicsSimulation;

internal static class OrbitalSystemTests
{
    public static void Run()
    {
        int passed = 0;
        int failed = 0;

        void Check(bool condition, string testName)
        {
            if (condition)
            {
                Console.WriteLine($"[PASS] {testName}");
                passed++;
            }
            else
            {
                Console.WriteLine($"[FAIL] {testName}");
                failed++;
            }
        }

        bool NearlyEqual(double actual, double expected, double tolerance = 1e-12)
        {
            if (!double.IsFinite(actual) || !double.IsFinite(expected))
                return false;

            double scale = Math.Max(1.0, Math.Abs(expected));

            return Math.Abs(actual - expected) <= tolerance * scale;
        }

        bool NearlyEqualVector(Vector3Double actual, Vector3Double expected)
        {
            return NearlyEqual(actual.X, expected.X)
                && NearlyEqual(actual.Y, expected.Y)
                && NearlyEqual(actual.Z, expected.Z);
        }

        bool RejectsInvalidOperation(Action action)
        {
            try
            {
                action();
                return false;
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }

        // ============================================================
        // 1. CREATE CELESTIAL BODIES
        // ============================================================

        var sun = new CelestBody(
            1.989e30,
            new Vector3Double(0, 0, 0));

        var earth = new CelestBody(
            5.972e24,
            new Vector3Double(1.496e11, 0, 0));

        var moon = new CelestBody(
            7.348e22,
            new Vector3Double(1.496e11 + 3.844e8, 0, 0));

        var mars = new CelestBody(
            6.417e23,
            new Vector3Double(2.279e11, 0, 0));

        // ============================================================
        // 2. CREATE ORBITAL SYSTEM TREE
        // ============================================================

        var earthSystem = new OrbitalSystem();

        earthSystem.AddMember(new OrbitalMember(earth));
        earthSystem.AddMember(new OrbitalMember(moon));

        var solarSystem = new OrbitalSystem();

        solarSystem.AddMember(new OrbitalMember(sun));
        solarSystem.AddMember(new OrbitalMember(earthSystem));
        solarSystem.AddMember(new OrbitalMember(mars));

        Console.WriteLine("========================================");
        Console.WriteLine("ORBITAL SYSTEM TESTS");
        Console.WriteLine("========================================");

        // ============================================================
        // TEST A: RECURSIVE TRAVERSAL
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Recursive traversal ---");

        var bodies = new List<CelestBody>(solarSystem.GetBodies());

        Check(bodies.Count == 4, "Correct number of bodies");

        if (bodies.Count == 4)
        {
            Check(
                ReferenceEquals(bodies[0], sun) &&
                ReferenceEquals(bodies[1], earth) &&
                ReferenceEquals(bodies[2], moon) &&
                ReferenceEquals(bodies[3], mars),
                "Correct traversal order");

            Check(
                ReferenceEquals(bodies[1], earth),
                "Original object references preserved");
        }

        // ============================================================
        // TEST B: TOTAL MASS
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Total mass ---");

        double expectedEarthSystemMass = earth.Mass + moon.Mass;

        double expectedSolarSystemMass =
            sun.Mass + earth.Mass + moon.Mass + mars.Mass;

        Check(
            NearlyEqual(earthSystem.TotalMass, expectedEarthSystemMass),
            "Earth-Moon total mass");

        Check(
            NearlyEqual(solarSystem.TotalMass, expectedSolarSystemMass),
            "Solar system total mass");

        // ============================================================
        // TEST C: CENTER OF MASS
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Center of mass ---");

        double expectedEarthSystemX =
            (earth.Mass * earth.Position.X +
             moon.Mass * moon.Position.X)
            / expectedEarthSystemMass;

        double expectedSolarSystemX =
            (sun.Mass * sun.Position.X +
             earth.Mass * earth.Position.X +
             moon.Mass * moon.Position.X +
             mars.Mass * mars.Position.X)
            / expectedSolarSystemMass;

        Check(
            NearlyEqualVector(
                earthSystem.CenterOfMass,
                new Vector3Double(expectedEarthSystemX, 0, 0)),
            "Earth-Moon center of mass");

        Check(
            NearlyEqualVector(
                solarSystem.CenterOfMass,
                new Vector3Double(expectedSolarSystemX, 0, 0)),
            "Solar system center of mass");

        Check(
            expectedEarthSystemX > earth.Position.X &&
            expectedEarthSystemX < moon.Position.X,
            "Earth-Moon barycenter lies between bodies");

        // ============================================================
        // TEST D: VELOCITY TRANSLATION
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Velocity translation ---");

        Vector3Double deltaVelocity = new Vector3Double(100, 20, -5);

        Vector3Double initialRelativeVelocity = moon.Speed - earth.Speed;

        earthSystem.TranslateVelocity(deltaVelocity);

        Check(
            NearlyEqualVector(earth.Speed, deltaVelocity),
            "Earth velocity updated");

        Check(
            NearlyEqualVector(moon.Speed, deltaVelocity),
            "Moon velocity updated");

        Check(
            NearlyEqualVector(sun.Speed, new Vector3Double(0, 0, 0)) &&
            NearlyEqualVector(mars.Speed, new Vector3Double(0, 0, 0)),
            "Other branches unaffected");

        Check(
            NearlyEqualVector(
                moon.Speed - earth.Speed,
                initialRelativeVelocity),
            "Internal relative velocity preserved");

        Check(
            NearlyEqualVector(
                earthSystem.CenterOfMassVelocity,
                deltaVelocity),
            "Earth-Moon barycenter velocity updated");

        Vector3Double expectedSolarVelocity =
            deltaVelocity * (expectedEarthSystemMass / expectedSolarSystemMass);

        Check(
            NearlyEqualVector(
                solarSystem.CenterOfMassVelocity,
                expectedSolarVelocity),
            "Solar system barycenter velocity updated");

        // ============================================================
        // TEST E: POSITION TRANSLATION
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Position translation ---");

        Vector3Double initialEarthPosition = earth.Position;
        Vector3Double initialMoonPosition = moon.Position;
        Vector3Double initialSunPosition = sun.Position;
        Vector3Double initialMarsPosition = mars.Position;

        Vector3Double initialEarthBarycenter = earthSystem.CenterOfMass;
        Vector3Double initialSolarBarycenter = solarSystem.CenterOfMass;

        Vector3Double displacement =
            new Vector3Double(1000, 2000, -3000);

        earthSystem.TranslatePosition(displacement);

        Check(
            NearlyEqualVector(
                earth.Position,
                initialEarthPosition + displacement),
            "Earth position translated");

        Check(
            NearlyEqualVector(
                moon.Position,
                initialMoonPosition + displacement),
            "Moon position translated");

        Check(
            NearlyEqualVector(sun.Position, initialSunPosition) &&
            NearlyEqualVector(mars.Position, initialMarsPosition),
            "Other positions unaffected");

        Check(
            NearlyEqualVector(
                moon.Position - earth.Position,
                initialMoonPosition - initialEarthPosition),
            "Internal relative position preserved");

        Check(
            NearlyEqualVector(
                earthSystem.CenterOfMass,
                initialEarthBarycenter + displacement),
            "Earth-Moon barycenter translated");

        Vector3Double expectedSolarBarycenter =
            initialSolarBarycenter +
            displacement * (expectedEarthSystemMass / expectedSolarSystemMass);

        Check(
            NearlyEqualVector(
                solarSystem.CenterOfMass,
                expectedSolarBarycenter),
            "Solar system barycenter updated");

        // ============================================================
        // TEST F: TREE INTEGRITY
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Tree integrity ---");
        Console.WriteLine(
            $"EarthSystem parent is SolarSystem: " +
            $"{ReferenceEquals(earthSystem.Parent, solarSystem)}");

        Console.WriteLine(
            $"SolarSystem is ancestor of EarthSystem: " +
            $"{earthSystem.CheckIsAParent(solarSystem)}");

        Check(
            RejectsInvalidOperation(
                () => solarSystem.AddMember(new OrbitalMember(solarSystem))),
            "Reject direct self-reference");

        Check(
            RejectsInvalidOperation(
                () => earthSystem.AddMember(new OrbitalMember(solarSystem))),
            "Reject circular reference");

        Check(
            RejectsInvalidOperation(
                () => solarSystem.AddMember(new OrbitalMember(earth))),
            "Reject an object with an existing parent");

        Check(
            RejectsInvalidOperation(
                () => earthSystem.AddMember(new OrbitalMember(moon))),
            "Reject duplicate member");

        Check(
            RejectsInvalidOperation(
                () => solarSystem.AddMember(null!)),
            "Reject null member");

        Console.WriteLine(
            $"EarthSystem parent is SolarSystem: " +
            $"{ReferenceEquals(earthSystem.Parent, solarSystem)}");

        Console.WriteLine(
            $"SolarSystem is ancestor of EarthSystem: " +
            $"{earthSystem.CheckIsAParent(solarSystem)}");


        // ============================================================
        // TEST G: TWO-BODY ORBITAL INITIALIZATION
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Two-body orbital initialization ---");

        // Compare vectors using a scale-aware absolute tolerance.
        bool NearVector(
            Vector3Double actual,
            Vector3Double expected,
            double scale,
            double tolerance = 1e-11)
        {
            double allowedError =
                tolerance * Math.Max(1.0, Math.Abs(scale));

            return
                double.IsFinite(actual.X) &&
                double.IsFinite(actual.Y) &&
                double.IsFinite(actual.Z) &&
                Math.Abs(actual.X - expected.X) <= allowedError &&
                Math.Abs(actual.Y - expected.Y) <= allowedError &&
                Math.Abs(actual.Z - expected.Z) <= allowedError;
        }

        // Save the internal Earth-Moon configuration.
        Vector3Double initialMoonEarthPosition =
            moon.Position - earth.Position;

        Vector3Double initialMoonEarthVelocity =
            moon.Speed - earth.Speed;

        // Save Mars to ensure that it is not modified.
        Vector3Double initialMarsPosition2 = mars.Position;
        Vector3Double initialMarsVelocity2 = mars.Speed;

        // Define an inclined elliptical orbit for the
        // Earth-Moon system around the Sun.
        var orbit = new OrbitalParameters(
            1.496e11, // Semi-major axis
            0.15,     // Eccentricity
            0.2,      // Inclination
            0.5,      // Longitude of ascending node
            0.4,      // Argument of periapsis
            1.1);     // True anomaly

        double primaryMass = sun.TotalMass;
        double secondaryMass = earthSystem.TotalMass;
        double combinedMass = primaryMass + secondaryMass;

        State expectedRelativeState = KeplerConverter.Convert(
            orbit,
            Constants.G * combinedMass);

        // Initialize the orbit.
        OrbitalInitializer.InitializeTwoBody(
            sun,
            earthSystem,
            orbit);

        // ============================================================
        // TEST G1: RELATIVE POSITION AND VELOCITY
        // ============================================================

        Vector3Double relativePosition =
            earthSystem.CenterOfMass - sun.CenterOfMass;

        Vector3Double relativeVelocity =
            earthSystem.CenterOfMassVelocity -
            sun.CenterOfMassVelocity;

        double positionScale = expectedRelativeState.Y.GetMagnitude();
        double velocityScale = expectedRelativeState.YDot.GetMagnitude();

        Check(
            NearVector(
                relativePosition,
                expectedRelativeState.Y,
                positionScale),
            "Initializer: correct relative position");

        Check(
            NearVector(
                relativeVelocity,
                expectedRelativeState.YDot,
                velocityScale),
            "Initializer: correct relative velocity");

        // ============================================================
        // TEST G2: BARYCENTER
        // ============================================================

        Vector3Double combinedCenterOfMass =
            (sun.CenterOfMass * primaryMass +
             earthSystem.CenterOfMass * secondaryMass)
            / combinedMass;

        Vector3Double combinedCenterOfMassVelocity =
            (sun.CenterOfMassVelocity * primaryMass +
             earthSystem.CenterOfMassVelocity * secondaryMass)
            / combinedMass;

        Check(
            NearVector(
                combinedCenterOfMass,
                new Vector3Double(0, 0, 0),
                positionScale),
            "Initializer: barycenter at origin");

        Check(
            NearVector(
                combinedCenterOfMassVelocity,
                new Vector3Double(0, 0, 0),
                velocityScale),
            "Initializer: barycenter velocity is zero");

        // ============================================================
        // TEST G3: INDIVIDUAL BARYCENTRIC STATES
        // ============================================================

        Vector3Double expectedPrimaryPosition =
            -expectedRelativeState.Y *
            (secondaryMass / combinedMass);

        Vector3Double expectedSecondaryPosition =
            expectedRelativeState.Y *
            (primaryMass / combinedMass);

        Vector3Double expectedPrimaryVelocity =
            -expectedRelativeState.YDot *
            (secondaryMass / combinedMass);

        Vector3Double expectedSecondaryVelocity =
            expectedRelativeState.YDot *
            (primaryMass / combinedMass);

        Check(
            NearVector(
                sun.CenterOfMass,
                expectedPrimaryPosition,
                positionScale),
            "Initializer: primary barycentric position");

        Check(
            NearVector(
                earthSystem.CenterOfMass,
                expectedSecondaryPosition,
                positionScale),
            "Initializer: secondary barycentric position");

        Check(
            NearVector(
                sun.CenterOfMassVelocity,
                expectedPrimaryVelocity,
                velocityScale),
            "Initializer: primary barycentric velocity");

        Check(
            NearVector(
                earthSystem.CenterOfMassVelocity,
                expectedSecondaryVelocity,
                velocityScale),
            "Initializer: secondary barycentric velocity");

        // ============================================================
        // TEST G4: INTERNAL SYSTEM PRESERVATION
        // ============================================================

        Check(
            NearVector(
                moon.Position - earth.Position,
                initialMoonEarthPosition,
                initialMoonEarthPosition.GetMagnitude()),
            "Initializer: Earth-Moon relative position preserved");

        Check(
            NearVector(
                moon.Speed - earth.Speed,
                initialMoonEarthVelocity,
                Math.Max(1.0, initialMoonEarthVelocity.GetMagnitude())),
            "Initializer: Earth-Moon relative velocity preserved");

        // ============================================================
        // TEST G5: UNRELATED BODIES
        // ============================================================

        Check(
            NearVector(
                mars.Position,
                initialMarsPosition2,
                positionScale) &&
            NearVector(
                mars.Speed,
                initialMarsVelocity2,
                velocityScale),
            "Initializer: Mars remains unaffected");

        // ============================================================
        // TEST G6: REPEATED INITIALIZATION
        // ============================================================

        // Running initialization twice should not change
        // the resulting barycentric configuration.
        Vector3Double sunPositionBeforeRepeat = sun.Position;
        Vector3Double earthPositionBeforeRepeat = earth.Position;
        Vector3Double sunVelocityBeforeRepeat = sun.Speed;
        Vector3Double earthVelocityBeforeRepeat = earth.Speed;

        OrbitalInitializer.InitializeTwoBody(
            sun,
            earthSystem,
            orbit);

        Check(
            NearVector(sun.Position, sunPositionBeforeRepeat, positionScale) &&
            NearVector(earth.Position, earthPositionBeforeRepeat, positionScale),
            "Initializer: repeated call preserves positions");

        Check(
            NearVector(sun.Speed, sunVelocityBeforeRepeat, velocityScale) &&
            NearVector(earth.Speed, earthVelocityBeforeRepeat, velocityScale),
            "Initializer: repeated call preserves velocities");

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
            throw new InvalidOperationException($"{failed} orbital system test(s) failed.");
        }
        else
        {
            Console.WriteLine("All orbital system tests passed successfully.");
        }
    }
}
