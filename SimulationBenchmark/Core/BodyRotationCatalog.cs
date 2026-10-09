using System;
using PhysicsSimulation;

// Illustrative, independent spin metadata for the live benchmark.
// The renderer never alters orbital forces, positions, or integrator state.
internal static class BodyRotationCatalog
{
    public static AxialRotation Create(string name, bool isSatellite, bool isStar, bool isComet)
    {
        // Familiar bodies use approximate sidereal spin periods. Their axes are
        // illustrative directions in the simulation's fixed XYZ frame.
        switch (name)
        {
            case "Sun":
                return new AxialRotation(TiltedAxis(7.25, 0), 25.38 * AppSettings.SecondsPerDay, 0);
            case "Earth":
                return new AxialRotation(TiltedAxis(23.44, 90), 86_164.09, 0);
            case "Moon":
                return new AxialRotation(TiltedAxis(6.68, 55), 27.321661 * AppSettings.SecondsPerDay, 0);
            case "Mars":
                return new AxialRotation(TiltedAxis(25.19, 140), 88_642.7, 0.4);
            case "Jupiter":
                return new AxialRotation(TiltedAxis(3.13, 35), 35_730.0, 0.2);
        }

        // Fictional bodies: deterministic illustrative axes, periods and phases.
        var rng = new Random(StableSeed(name));
        double inclinationDegrees = 8 + 55 * rng.NextDouble();
        double azimuthDegrees = 360 * rng.NextDouble();
        double periodHours = isStar ? 24 * (8 + 25 * rng.NextDouble())
            : isSatellite ? 24 * (2 + 18 * rng.NextDouble())
            : isComet ? 6 + 24 * rng.NextDouble()
            : 9 + 70 * rng.NextDouble();
        double phase = 2 * Math.PI * rng.NextDouble();
        return new AxialRotation(
            TiltedAxis(inclinationDegrees, azimuthDegrees),
            periodHours * 3600.0, phase);
    }

    private static Vector3Double TiltedAxis(double inclinationDegrees, double azimuthDegrees)
    {
        double tilt = inclinationDegrees * Math.PI / 180.0;
        double azimuth = azimuthDegrees * Math.PI / 180.0;
        return new Vector3Double(
            Math.Sin(tilt) * Math.Cos(azimuth),
            Math.Sin(tilt) * Math.Sin(azimuth),
            Math.Cos(tilt));
    }

    public static int StableSeed(string name)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (char character in name)
            {
                hash ^= character;
                hash *= 16777619;
            }
            return (int)(hash & 0x7FFFFFFF);
        }
    }
}
