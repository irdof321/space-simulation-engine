using PhysicsSimulation.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhysicsSimulation
{

    public class PhysicSimulation
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is cr
        private List<CelestBody> bodies = new List<CelestBody>();
        private double _time = 0;

        private Integrator _integrator;

        public IReadOnlyList<CelestBody> Bodies
        {
            get { return bodies.AsReadOnly(); }
        }

        public double Time
        {
            get { return _time; }
        }

        public PhysicSimulation(Integrator integrator)
        {
            _integrator = integrator;
        }


        public void AddCelestialBody(CelestBody body)
        {
            bodies.Add(body);
        }

        public void RemoveCelestialBody(CelestBody body)
        {
            bodies.Remove(body);
        }

        public void Init()
        {
            // Init acceleration of the bodies
            CelestBody mostMassive = bodies[0];
            double totalMass = 0;

            for (int i = 0; i < bodies.Count; i++)
            {
                totalMass += bodies[i].Mass;
                if (bodies[i].Mass > mostMassive.Mass)
                {
                    mostMassive = bodies[i];
                }
            }


            foreach (CelestBody body in bodies)
            {
                if (body != mostMassive)
                {
                    Vector3Double r = mostMassive.Position - body.Position;
                    Vector3Double e_t = new Vector3Double(-r.Z, 0f, r.X).GetUnit();
                    body.Speed = Math.Sqrt(Constants.G * mostMassive.Mass / r.GetMagnitude()) * e_t;
                }
            }

            Vector3Double massCenterSpeed = new Vector3Double(0, 0, 0);
            foreach (CelestBody body in bodies)
            {
                massCenterSpeed += body.Speed * body.Mass;
            }
            massCenterSpeed /= totalMass;

            // Adjust the speed of each body to ensure the center of mass is stationary
            foreach (CelestBody body in bodies)
            {
                body.Speed -= massCenterSpeed;
            }

        }
        public State[] Gravity(State[] states, double time)
        {
            // Votre logique de calcul ici
            State[] resultats = new State[states.Length];

            // Exemple bidon de traitement
            for (int i = 0; i < states.Length; i++)
            {
                resultats[i].Y = states[i].YDot;
                for (int j = i + 1; j < states.Length; j++)
                {
                    Vector3Double r = (states[j].Y - states[i].Y);
                    Vector3Double F_i_j = Constants.G * bodies[i].Mass * bodies[j].Mass * r.GetUnit() / r.GetSquareMagnitude();
                    resultats[i].YDot += F_i_j / bodies[i].Mass;
                    resultats[j].YDot += -F_i_j / bodies[j].Mass;
                }
            }

            return resultats;
        }

        public void Update(double deltaT)
        {
            _integrator.Integrate(bodies, Gravity, _time, deltaT);
            _time += deltaT;
        }


        public double GetKineticEnergy()
        {
            double energie = 0;
            foreach( CelestBody body in bodies)
            {
                energie += body.Mass * body.Speed.GetSquareMagnitude();
            }

            return 0.5 * energie;
        }

        public double GetPotentialEnergy()
        {
            double energie = 0;
            for (int i = 0; i < bodies.Count; i++)
            {
                for (int j = i + 1; j < bodies.Count; j++)
                {
                    energie +=  bodies[i].Mass * bodies[j].Mass / (bodies[i].Position - bodies[j].Position).GetMagnitude();                }

            }

            return -Constants.G * energie;
        }

        public double GetTotalEnergy()
        {
            return GetPotentialEnergy() + GetKineticEnergy();
        }

        public Vector3Double GetTotalMomentum()
        {
            Vector3Double momentum = new Vector3Double(0, 0, 0);
            foreach (CelestBody body in bodies)
            {
                momentum += body.Mass * body.Speed;
            }
            return momentum;
        }


    }
}
