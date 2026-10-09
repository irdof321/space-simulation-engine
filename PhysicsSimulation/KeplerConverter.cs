using System;

namespace PhysicsSimulation
{
    public static class KeplerConverter
    {
        public static State Convert(
            OrbitalParameters parameters,
            double gravitationalParameter)
        {
            if (!double.IsFinite(gravitationalParameter) || gravitationalParameter <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gravitationalParameter),
                    "Gravitational parameter must be finite and positive.");
            }
            State state = new State();
            double p = parameters.SemiMajorAxis * (1 - parameters.Eccentricity * parameters.Eccentricity);
            double r = p / (1 + parameters.Eccentricity * Math.Cos(parameters.TrueAnomaly));
            state.Y = new Vector3Double(
                r * Math.Cos(parameters.TrueAnomaly),
                r * Math.Sin(parameters.TrueAnomaly),
                0);

            // Rotate the position vector to account for inclination, longitude of ascending node, and argument of periapsis.
            state.Y = RotateZ(state.Y, parameters.ArgumentOfPeriapsis);
            state.Y = RotateX(state.Y, parameters.Inclination);
            state.Y = RotateZ(state.Y, parameters.LongitudeOfAscendingNode);

            double velocityFactor = Math.Sqrt(gravitationalParameter / p);
            state.YDot = new Vector3Double(
                -velocityFactor * Math.Sin(parameters.TrueAnomaly),
                velocityFactor * (parameters.Eccentricity + Math.Cos(parameters.TrueAnomaly)),
                0);

            // Rotate the velocity vector to account for inclination, longitude of ascending node, and argument of periapsis.
            state.YDot = RotateZ(state.YDot, parameters.ArgumentOfPeriapsis);
            state.YDot = RotateX(state.YDot, parameters.Inclination);
            state.YDot = RotateZ(state.YDot, parameters.LongitudeOfAscendingNode);

            return state;
        }

        private static Vector3Double RotateZ(Vector3Double vector, double angle)
        {
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);
            return new Vector3Double(
                vector.X * cosAngle - vector.Y * sinAngle,
                vector.X * sinAngle + vector.Y * cosAngle,
                vector.Z);
        }

        private static Vector3Double RotateX(Vector3Double vector, double angle)
        {
            // Implement rotation around the X axis.
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);
            return new Vector3Double(
                vector.X,
                vector.Y * cosAngle - vector.Z * sinAngle,
                vector.Y * sinAngle + vector.Z * cosAngle);
        }

        private static Vector3Double RotateY(Vector3Double vector, double angle)
        {
            // Implement rotation around the Y axis.
            double cosAngle = Math.Cos(angle);
            double sinAngle = Math.Sin(angle);
            return new Vector3Double(
                vector.X * cosAngle + vector.Z * sinAngle,
                vector.Y,
                -vector.X * sinAngle + vector.Z * cosAngle);
        }
    }
}