using System;
using System.Collections.Generic;
using PhysicsSimulation;

internal static class KeplerConverterTests
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

        bool NearlyEqual(
            double actual,
            double expected,
            double scale = 1.0,
            double tolerance = 1e-10)
        {
            if (!double.IsFinite(actual) || !double.IsFinite(expected))
                return false;

            return Math.Abs(actual - expected) <=
                tolerance * Math.Max(Math.Abs(scale), Math.Abs(expected));
        }

        bool NearlyEqualVector(
            Vector3Double actual,
            Vector3Double expected,
            double scale)
        {
            return NearlyEqual(actual.X, expected.X, scale)
                && NearlyEqual(actual.Y, expected.Y, scale)
                && NearlyEqual(actual.Z, expected.Z, scale);
        }

        double Dot(Vector3Double a, Vector3Double b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        Vector3Double Cross(Vector3Double a, Vector3Double b)
        {
            return new Vector3Double(
                a.Y * b.Z - a.Z * b.Y,
                a.Z * b.X - a.X * b.Z,
                a.X * b.Y - a.Y * b.X);
        }

        // ============================================================
        // TEST PARAMETERS
        // ============================================================

        const double mu = 3.986004418e14; // Earth's gravitational parameter
        const double a = 10_000_000.0;    // Semi-major axis in meters

        double circularSpeed = Math.Sqrt(mu / a);

        Console.WriteLine("========================================");
        Console.WriteLine("KEPLER CONVERTER PHYSICS TESTS");
        Console.WriteLine("========================================");

        // Constructor order:
        // semiMajorAxis, eccentricity, inclination,
        // longitudeOfAscendingNode, argumentOfPeriapsis,
        // trueAnomaly

        // ============================================================
        // TEST A: CIRCULAR ORBIT
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Circular orbit ---");

        var circularParameters = new OrbitalParameters(
            a, 0, 0, 0, 0, 0);

        State circular = KeplerConverter.Convert(
            circularParameters, mu);

        Check(
            NearlyEqualVector(
                circular.Y,
                new Vector3Double(a, 0, 0),
                a),
            "Circular orbit: initial position");

        Check(
            NearlyEqualVector(
                circular.YDot,
                new Vector3Double(0, circularSpeed, 0),
                circularSpeed),
            "Circular orbit: initial velocity");

        Check(
            NearlyEqual(circular.Y.GetMagnitude(), a),
            "Circular orbit: correct radius");

        Check(
            NearlyEqual(circular.YDot.GetMagnitude(), circularSpeed),
            "Circular orbit: correct speed");

        Check(
            NearlyEqual(
                Dot(circular.Y, circular.YDot),
                0,
                a * circularSpeed),
            "Circular orbit: velocity perpendicular to radius");

        // ============================================================
        // TEST B: TRUE ANOMALY
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- True anomaly ---");

        var quarterOrbitParameters = new OrbitalParameters(
            a, 0, 0, 0, 0, Math.PI / 2);

        State quarterOrbit = KeplerConverter.Convert(
            quarterOrbitParameters, mu);

        Check(
            NearlyEqualVector(
                quarterOrbit.Y,
                new Vector3Double(0, a, 0),
                a),
            "Circular orbit: position at 90 degrees");

        Check(
            NearlyEqualVector(
                quarterOrbit.YDot,
                new Vector3Double(-circularSpeed, 0, 0),
                circularSpeed),
            "Circular orbit: velocity at 90 degrees");

        // ============================================================
        // TEST C: ORBITAL INCLINATION
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Inclination ---");

        var inclinedParameters = new OrbitalParameters(
            a, 0, Math.PI / 2, 0, 0, 0);

        State inclined = KeplerConverter.Convert(
            inclinedParameters, mu);

        Check(
            NearlyEqualVector(
                inclined.Y,
                new Vector3Double(a, 0, 0),
                a),
            "Inclination 90 degrees: initial position");

        Check(
            NearlyEqualVector(
                inclined.YDot,
                new Vector3Double(0, 0, circularSpeed),
                circularSpeed),
            "Inclination 90 degrees: velocity along Z");

        // ============================================================
        // TEST D: ARGUMENT OF PERIAPSIS
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Argument of periapsis ---");

        var periapsisRotationParameters = new OrbitalParameters(
            a, 0, 0, 0, Math.PI / 2, 0);

        State periapsisRotation = KeplerConverter.Convert(
            periapsisRotationParameters, mu);

        Check(
            NearlyEqualVector(
                periapsisRotation.Y,
                new Vector3Double(0, a, 0),
                a),
            "Argument of periapsis: position rotation");

        Check(
            NearlyEqualVector(
                periapsisRotation.YDot,
                new Vector3Double(-circularSpeed, 0, 0),
                circularSpeed),
            "Argument of periapsis: velocity rotation");

        // ============================================================
        // TEST E: LONGITUDE OF ASCENDING NODE
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Longitude of ascending node ---");

        var nodeParameters = new OrbitalParameters(
            a, 0, 0, Math.PI / 2, 0, 0);

        State nodeRotation = KeplerConverter.Convert(
            nodeParameters, mu);

        Check(
            NearlyEqualVector(
                nodeRotation.Y,
                new Vector3Double(0, a, 0),
                a),
            "Ascending node: position rotation");

        Check(
            NearlyEqualVector(
                nodeRotation.YDot,
                new Vector3Double(-circularSpeed, 0, 0),
                circularSpeed),
            "Ascending node: velocity rotation");

        // ============================================================
        // TEST F: COMBINED 3D ROTATIONS
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Combined 3D rotations ---");

        var combinedParameters = new OrbitalParameters(
            a, 0, Math.PI / 2, Math.PI / 2, 0, Math.PI / 2);

        State combined = KeplerConverter.Convert(
            combinedParameters, mu);

        Check(
            NearlyEqualVector(
                combined.Y,
                new Vector3Double(0, 0, a),
                a),
            "Combined rotations: expected 3D position");

        Check(
            NearlyEqualVector(
                combined.YDot,
                new Vector3Double(0, -circularSpeed, 0),
                circularSpeed),
            "Combined rotations: expected 3D velocity");

        // ============================================================
        // TEST G: ELLIPTICAL ORBIT
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Elliptical orbit ---");

        const double e = 0.4;

        var periapsisParameters = new OrbitalParameters(
            a, e, 0, 0, 0, 0);

        var apoapsisParameters = new OrbitalParameters(
            a, e, 0, 0, 0, Math.PI);

        State periapsis = KeplerConverter.Convert(
            periapsisParameters, mu);

        State apoapsis = KeplerConverter.Convert(
            apoapsisParameters, mu);

        double expectedPeriapsisRadius = a * (1 - e);
        double expectedApoapsisRadius = a * (1 + e);

        double expectedPeriapsisSpeed =
            Math.Sqrt(mu * (1 + e) / (a * (1 - e)));

        double expectedApoapsisSpeed =
            Math.Sqrt(mu * (1 - e) / (a * (1 + e)));

        Check(
            NearlyEqual(
                periapsis.Y.GetMagnitude(),
                expectedPeriapsisRadius),
            "Elliptical orbit: periapsis radius");

        Check(
            NearlyEqual(
                apoapsis.Y.GetMagnitude(),
                expectedApoapsisRadius),
            "Elliptical orbit: apoapsis radius");

        Check(
            NearlyEqual(
                periapsis.YDot.GetMagnitude(),
                expectedPeriapsisSpeed),
            "Elliptical orbit: periapsis speed");

        Check(
            NearlyEqual(
                apoapsis.YDot.GetMagnitude(),
                expectedApoapsisSpeed),
            "Elliptical orbit: apoapsis speed");

        Check(
            periapsis.YDot.GetMagnitude() >
            apoapsis.YDot.GetMagnitude(),
            "Elliptical orbit: faster at periapsis");

        Check(
            NearlyEqual(
                Dot(periapsis.Y, periapsis.YDot),
                0,
                expectedPeriapsisRadius * expectedPeriapsisSpeed),
            "Periapsis: velocity perpendicular to radius");

        Check(
            NearlyEqual(
                Dot(apoapsis.Y, apoapsis.YDot),
                0,
                expectedApoapsisRadius * expectedApoapsisSpeed),
            "Apoapsis: velocity perpendicular to radius");

        // ============================================================
        // TEST H: PHYSICAL INVARIANTS
        // ============================================================

        Console.WriteLine();
        Console.WriteLine("--- Physical invariants ---");

        var generalParameters = new OrbitalParameters(
            a, e, 0.7, 1.1, 0.4, 1.3);

        State general = KeplerConverter.Convert(
            generalParameters, mu);

        double radius = general.Y.GetMagnitude();
        double speed = general.YDot.GetMagnitude();

        // Vis-viva equation
        double expectedSpeedSquared = mu * (2.0 / radius - 1.0 / a);

        Check(
            NearlyEqual(
                speed * speed,
                expectedSpeedSquared),
            "Vis-viva equation");

        // Specific orbital energy
        double specificEnergy =
            0.5 * speed * speed - mu / radius;

        double expectedSpecificEnergy = -mu / (2.0 * a);

        Check(
            NearlyEqual(
                specificEnergy,
                expectedSpecificEnergy),
            "Specific orbital energy");

        // Specific angular momentum
        Vector3Double angularMomentum = Cross(general.Y, general.YDot);

        double expectedAngularMomentum =
            Math.Sqrt(mu * a * (1.0 - e * e));

        Check(
            NearlyEqual(
                angularMomentum.GetMagnitude(),
                expectedAngularMomentum),
            "Specific angular momentum magnitude");

        // Angular momentum direction from inclination and node
        double inclination = generalParameters.Inclination;
        double ascendingNode = generalParameters.LongitudeOfAscendingNode;

        Vector3Double expectedAngularMomentumDirection =
            new Vector3Double(
                Math.Sin(inclination) * Math.Sin(ascendingNode),
                -Math.Sin(inclination) * Math.Cos(ascendingNode),
                Math.Cos(inclination));

        Vector3Double actualAngularMomentumDirection =
            angularMomentum / angularMomentum.GetMagnitude();

        Check(
            NearlyEqualVector(
                actualAngularMomentumDirection,
                expectedAngularMomentumDirection,
                1.0),
            "Angular momentum direction in 3D");

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
                $"{failed} Kepler converter test(s) failed.");
        }

        Console.WriteLine("All Kepler converter tests passed.");
    }
}
