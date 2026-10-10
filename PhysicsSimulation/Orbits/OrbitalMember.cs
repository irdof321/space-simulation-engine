using PhysicsSimulation.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

namespace PhysicsSimulation.Orbits
{
    public class OrbitalParameters
    {
        public double SemiMajorAxis { get;  }
        public double Eccentricity { get;  }
        public double Inclination { get;  }
        public double LongitudeOfAscendingNode { get;  }
        public double ArgumentOfPeriapsis { get;  }
        public double TrueAnomaly { get;  }
        public OrbitalParameters(double semiMajorAxis, double eccentricity, double inclination, double longitudeOfAscendingNode, double argumentOfPeriapsis, double trueAnomaly)
        {
            if(semiMajorAxis <= 0 || !double.IsFinite(semiMajorAxis))
            {
                throw new ArgumentException("Semi-major axis must be greater than zero.");
            }
            if(eccentricity < 0 || eccentricity >= 1 || !double.IsFinite(eccentricity))
            {
                throw new ArgumentException("Eccentricity must be between 0 and 1.");
            }
            if(inclination < 0 || inclination > Math.PI || !double.IsFinite(inclination))
            {
                throw new ArgumentException("Inclination must be between 0 and π radians.");
            }
            if(longitudeOfAscendingNode < 0 || longitudeOfAscendingNode >= 2 * Math.PI || !double.IsFinite(longitudeOfAscendingNode))
            {
                throw new ArgumentException("Longitude of ascending node must be between 0 and 2π radians.");
            }
            if(argumentOfPeriapsis < 0 || argumentOfPeriapsis >= 2 * Math.PI || !double.IsFinite(argumentOfPeriapsis))
            {
                throw new ArgumentException("Argument of periapsis must be between 0 and 2π radians.");
            }
            if(trueAnomaly < 0 || trueAnomaly >= 2 * Math.PI || !double.IsFinite(trueAnomaly))
            {
                throw new ArgumentException("True anomaly must be between 0 and 2π radians.");
            }

            SemiMajorAxis = semiMajorAxis;
            Eccentricity = eccentricity;
            Inclination = inclination;
            LongitudeOfAscendingNode = longitudeOfAscendingNode;
            ArgumentOfPeriapsis = argumentOfPeriapsis;
            TrueAnomaly = trueAnomaly;
        }
    }

    public class OrbitalMember
    {
        private readonly OrbitalParameters? _orbitalParameters;
        private readonly OrbitalObject _orbitalObject;

        public OrbitalParameters? OrbitalParameters { get { return _orbitalParameters; } }
        public OrbitalObject OrbitalObject { get { return _orbitalObject; } }

        public OrbitalMember(OrbitalObject orbitalObject, OrbitalParameters? orbitalParameters = null)
        {
            if (orbitalObject == null)
            {
                throw new ArgumentNullException(nameof(orbitalObject), "Orbital object cannot be null.");
            }
            _orbitalParameters = orbitalParameters;
            _orbitalObject = orbitalObject;
        }


    }
}
