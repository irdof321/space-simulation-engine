using System;
using System.Drawing;
using PhysicsSimulation;

// EDIT THIS FILE to add/remove planets, moons, initial Kepler elements, and plot views.
// Solar-system-like parameters are illustrative, NOT astronomical ephemerides.
internal static class SolarSystemPreset
{
    // Comet C1: illustrative high-eccentricity test particle (not a real comet ephemeris).
    // With a = 3 AU, e = 0.82, its perihelion is 0.54 AU and aphelion is 5.46 AU.
    // Its Kepler period around a solar-mass star is about 5.2 years.
    private const double CometMassKg = 1.0e14;
    private const double CometSemiMajorAxisAu = 4.0;
    private const double CometEccentricity = 0.82;

    public static OrbitalScene Create()
    {
        return AppSettings.LiveScene switch
        {
            LiveScenePreset.OriginalSolarSystem => CreateOriginalSolarSystem(),
            LiveScenePreset.TwoDistantSystems => CreateTwoDistantSystems(),
            _ => throw new InvalidOperationException("Unknown live scene preset.")
        };
    }

    private static OrbitalScene CreateOriginalSolarSystem()
    {
        var scene = new OrbitalScene();
        OrbitalSystem root = scene.Root;

        // 1. Primary star must be the FIRST member of this multi-body system.
        scene.AddBody(root, "Sun", 1.989e30,
            Color.FromArgb(247, 188, 72), radiusPixels: 7, isStar: true);

        // 2. Earth-Moon sub-system. Earth is its first member.
        OrbitalSystem earthSystem = scene.AddSystem(root,
            OrbitAu(1.000, 0.0167, 2.86, 45.84, 14.32, 63.03));

        scene.AddBody(earthSystem, "Earth", 5.972e24,
            Color.FromArgb(80, 166, 244), radiusPixels: 5);

        scene.AddBody(earthSystem, "Moon", 7.348e22,
            Color.FromArgb(219, 224, 232),
            orbit: OrbitKm(384_400, 0.055, 5.16, 17.19, 40.11, 68.75),
            isSatellite: true);

        // 3. Additional planets: one AddBody call per planet.
        scene.AddBody(root, "Mars", 6.417e23,
            Color.FromArgb(225, 117, 84),
            orbit: OrbitAu(1.523, 0.0934, 6.88, 63.03, 20.05, 114.59));

        scene.AddBody(root, "Jupiter", 1.898e27,
            Color.FromArgb(208, 170, 126), radiusPixels: 5,
            orbit: OrbitAu(5.204, 0.0489, 4.58, 34.38, 80.21, 229.18));

        // Extremely light, strongly elliptical Sun-orbiting comet.
        AddCometC1(scene, root);

        // Example: add another planet by uncommenting and adjusting this:
        // scene.AddBody(root, "Venus", 4.867e24,
        //     Color.FromArgb(230, 193, 117),
        //     orbit: OrbitAu(0.723, 0.0068, 3.4, 0, 0, 180));

        // 4. Views (automatically include any non-satellite planet in range).
        scene.AddView("OUTER SYSTEM", "Sun", 6.5 * AppSettings.AstronomicalUnit);
        scene.AddView("INNER SYSTEM", "Sun", 2.8 * AppSettings.AstronomicalUnit);
        scene.AddView("EARTH - MOON", "Earth", 650_000 * 1000.0,
            showSatellites: true);
        // Dedicated view with no unrelated planets: makes the comet ellipse visible.
        scene.AddView("COMET C1 - SUN", "Sun", 6.2 * AppSettings.AstronomicalUnit,
            visibleBodyNames: new[] { "Sun", "Comet C1" });

        // 5. Distances in the header and periodic console report.
        scene.AddDistanceReadout("Earth-Sun", "Earth", "Sun", DistanceUnit.AstronomicalUnits);
        scene.AddDistanceReadout("Earth-Moon", "Earth", "Moon", DistanceUnit.Kilometers);
        scene.AddDistanceReadout("Mars-Sun", "Mars", "Sun", DistanceUnit.AstronomicalUnits);
        scene.AddDistanceReadout("Jupiter-Sun", "Jupiter", "Sun", DistanceUnit.AstronomicalUnits);
        scene.AddDistanceReadout("Comet-Sun", "Comet C1", "Sun", DistanceUnit.AstronomicalUnits);

        return scene;
    }

    // Two far-apart planetary systems, initialized recursively with Kepler orbits.
    // Their mutual two-body orbital period is about twelve million years.
    // They are still coupled by N-body gravity: this is NOT two separate physics simulations.
    private static OrbitalScene CreateTwoDistantSystems()
    {
        var scene = new OrbitalScene();

        // ============================================================
        // SYSTEM A: Sun + Earth/Moon + Mars + Jupiter
        // ============================================================

        // The primary root member does not require orbital elements.
        OrbitalSystem systemA = scene.AddSystem(scene.Root);

        scene.AddBody(systemA, "Sun", 1.989e30,
            Color.FromArgb(247, 188, 72), radiusPixels: 7, isStar: true);

        OrbitalSystem earthSystem = scene.AddSystem(systemA,
            OrbitAu(1.000, 0.0167, 2.86, 45.84, 14.32, 63.03));

        scene.AddBody(earthSystem, "Earth", 5.972e24,
            Color.FromArgb(80, 166, 244), radiusPixels: 5);
        scene.AddBody(earthSystem, "Moon", 7.348e22,
            Color.FromArgb(219, 224, 232),
            orbit: OrbitKm(384_400, 0.055, 5.16, 17.19, 40.11, 68.75),
            isSatellite: true);

        scene.AddBody(systemA, "Mars", 6.417e23,
            Color.FromArgb(225, 117, 84),
            orbit: OrbitAu(1.523, 0.0934, 6.88, 63.03, 20.05, 114.59));

        scene.AddBody(systemA, "Jupiter", 1.898e27,
            Color.FromArgb(208, 170, 126), radiusPixels: 5,
            orbit: OrbitAu(5.204, 0.0489, 4.58, 34.38, 80.21, 229.18));

        // Comet belongs to system A and feels gravity from ALL bodies in both systems.
        AddCometC1(scene, systemA);

        // ============================================================
        // SYSTEM B: Fictional star Aster + Nysa/Moon + Vesper + Glacia
        // ============================================================

        // This is the relative binary-star orbit between BOTH system barycenters.
        // The semi-major axis is one light-year, so their mutual motion over
        // a few years is negligible compared with local planetary orbits.
        const double separationLightYears = 1.0;
        OrbitalSystem systemB = scene.AddSystem(scene.Root,
            OrbitAu(separationLightYears * AppSettings.LightYear / AppSettings.AstronomicalUnit,
                0.0, 0.0, 0.0, 0.0, 0.0));

        scene.AddBody(systemB, "Aster", 1.75e30,
            Color.FromArgb(231, 162, 108), radiusPixels: 7, isStar: true);

        OrbitalSystem nysaSystem = scene.AddSystem(systemB,
            OrbitAu(0.82, 0.035, 7.0, 45.0, 32.0, 150.0));
        scene.AddBody(nysaSystem, "Nysa", 3.2e24,
            Color.FromArgb(109, 205, 189), radiusPixels: 5);
        scene.AddBody(nysaSystem, "Nysa Moon", 1.6e22,
            Color.FromArgb(199, 215, 226),
            orbit: OrbitKm(240_000, 0.025, 8.0, 11.0, 55.0, 80.0),
            isSatellite: true);

        scene.AddBody(systemB, "Vesper", 8.0e24,
            Color.FromArgb(186, 130, 231), radiusPixels: 5,
            orbit: OrbitAu(1.65, 0.12, 9.0, 140.0, 50.0, 250.0));

        scene.AddBody(systemB, "Glacia", 2.5e26,
            Color.FromArgb(138, 181, 237), radiusPixels: 6,
            orbit: OrbitAu(3.2, 0.07, 3.0, 210.0, 80.0, 340.0));

        // ============================================================
        // VIEWS: wide overview plus each system at its own orbital scale.
        // ============================================================

        scene.AddView("TWO STAR SYSTEMS", "Sun", 1.25 * AppSettings.LightYear,
            starsOnly: true);
        scene.AddView("SYSTEM A - SUN", "Sun", 6.5 * AppSettings.AstronomicalUnit);
        scene.AddView("SYSTEM B - ASTER", "Aster", 4.2 * AppSettings.AstronomicalUnit);
        scene.AddView("EARTH - MOON", "Earth", 650_000 * 1000.0,
            showSatellites: true);
        scene.AddView("NYSA - MOON", "Nysa", 430_000 * 1000.0,
            showSatellites: true);
        scene.AddView("COMET C1 - SUN", "Sun", 6.2 * AppSettings.AstronomicalUnit,
            visibleBodyNames: new[] { "Sun", "Comet C1" });

        // The first two readouts appear in the live header.
        scene.AddDistanceReadout("Sun-Aster", "Sun", "Aster", DistanceUnit.LightYears);
        scene.AddDistanceReadout("Earth-Sun", "Earth", "Sun", DistanceUnit.AstronomicalUnits);
        scene.AddDistanceReadout("Nysa-Aster", "Nysa", "Aster", DistanceUnit.AstronomicalUnits);
        scene.AddDistanceReadout("Earth-Moon", "Earth", "Moon", DistanceUnit.Kilometers);
        scene.AddDistanceReadout("Nysa-Moon", "Nysa", "Nysa Moon", DistanceUnit.Kilometers);
        scene.AddDistanceReadout("Comet-Sun", "Comet C1", "Sun", DistanceUnit.AstronomicalUnits);

        return scene;
    }

    private static void AddCometC1(OrbitalScene scene, OrbitalSystem parent)
    {
        scene.AddBody(parent, "Comet C1", CometMassKg,
            Color.FromArgb(117, 248, 221), radiusPixels: 4.5f,
            orbit: OrbitAu(
                CometSemiMajorAxisAu, CometEccentricity,
                25.0, // Inclination in degrees
                35.0, // Longitude of ascending node
                55.0, // Argument of periapsis
                0.0), // True anomaly: start at perihelion
            isComet: true);
    }

    // Use astronomical units / kilometers and DEGREES for convenient configuration.
    // The physics engine continues to receive SI units and radians.
    private static OrbitalParameters OrbitAu(
        double semiMajorAxisAu, double eccentricity, double inclinationDeg,
        double ascendingNodeDeg, double periapsisDeg, double anomalyDeg)
    {
        return OrbitMeters(semiMajorAxisAu * AppSettings.AstronomicalUnit,
            eccentricity, inclinationDeg, ascendingNodeDeg, periapsisDeg, anomalyDeg);
    }

    private static OrbitalParameters OrbitKm(
        double semiMajorAxisKm, double eccentricity, double inclinationDeg,
        double ascendingNodeDeg, double periapsisDeg, double anomalyDeg)
    {
        return OrbitMeters(semiMajorAxisKm * 1000.0,
            eccentricity, inclinationDeg, ascendingNodeDeg, periapsisDeg, anomalyDeg);
    }

    private static OrbitalParameters OrbitMeters(
        double semiMajorAxisMeters, double eccentricity, double inclinationDeg,
        double ascendingNodeDeg, double periapsisDeg, double anomalyDeg)
    {
        const double radiansPerDegree = Math.PI / 180.0;
        return new OrbitalParameters(
            semiMajorAxisMeters, eccentricity,
            inclinationDeg * radiansPerDegree,
            ascendingNodeDeg * radiansPerDegree,
            periapsisDeg * radiansPerDegree,
            anomalyDeg * radiansPerDegree);
    }
}
