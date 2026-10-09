using System;

namespace PhysicsSimulation
{
    public static class OrbitalInitializer
    {
        public static void InitializeTwoBody(
            OrbitalObject primary,
            OrbitalObject secondary,
            OrbitalParameters orbit)
        {
            // TODO: Initialize barycentric positions and velocities.
            State stateRelativeOrbit = KeplerConverter.Convert(orbit, Constants.G * (primary.TotalMass + secondary.TotalMass));
            Vector3Double r = stateRelativeOrbit.Y;
            Vector3Double v = stateRelativeOrbit.YDot;

            primary.TranslatePosition(-r * (secondary.TotalMass / (primary.TotalMass + secondary.TotalMass)) - primary.CenterOfMass);
            secondary.TranslatePosition(r * (primary.TotalMass / (primary.TotalMass + secondary.TotalMass)) - secondary.CenterOfMass);
            primary.TranslateVelocity(-v * (secondary.TotalMass / (primary.TotalMass + secondary.TotalMass)) - primary.CenterOfMassVelocity);
            secondary.TranslateVelocity(v * (primary.TotalMass / (primary.TotalMass + secondary.TotalMass)) - secondary.CenterOfMassVelocity);
        }

        public static void Initialize(OrbitalSystem system)
        {
            // Step 1: Initialize nested systems first.
            foreach (var member in system.Members)
            {
                if (member.OrbitalObject is OrbitalSystem childSystem)
                {
                    Initialize(childSystem);
                }
            }

            // Step 2: Initialize the current two-body system.
            if (system.Members.Count == 2)
            {
                OrbitalMember primary = system.Members[0];
                OrbitalMember secondary = system.Members[1];

                if (secondary.OrbitalParameters == null)
                {
                    throw new InvalidOperationException(
                        "Secondary orbital parameters are required.");
                }

                InitializeTwoBody(
                    primary.OrbitalObject,
                    secondary.OrbitalObject,
                    secondary.OrbitalParameters);
            }else if (system.Members.Count > 2)
            {
                OrbitalObject primary = system.Members[0].OrbitalObject;
                for (int i = 1; i < system.Members.Count; i++)
                {
                    OrbitalMember secondary = system.Members[i];
                    if (secondary.OrbitalParameters == null)
                    {
                        throw new InvalidOperationException(
                            $"Secondary orbital parameters are required for member {i}.");
                    }
                    State stateRelativeOrbit = KeplerConverter.Convert(secondary.OrbitalParameters, Constants.G * (primary.TotalMass + secondary.OrbitalObject.TotalMass));
                    Vector3Double r = stateRelativeOrbit.Y;
                    Vector3Double v = stateRelativeOrbit.YDot;

                    secondary.OrbitalObject.TranslatePosition(
                        primary.CenterOfMass + r - secondary.OrbitalObject.CenterOfMass);

                    secondary.OrbitalObject.TranslateVelocity(
                        primary.CenterOfMassVelocity + v - secondary.OrbitalObject.CenterOfMassVelocity);


                }
                system.TranslatePosition(-system.CenterOfMass);
                system.TranslateVelocity(-system.CenterOfMassVelocity);
            }
        }
    }
}