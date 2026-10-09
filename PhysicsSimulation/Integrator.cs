using System;
using System.Collections.Generic;
using System.Text;

namespace PhysicsSimulation
{
    public struct State
    {
        public Vector3Double Y;
        public Vector3Double YDot;
    }

    public interface ISimulatable
    {
        State GetState();
        void SetState(State state);
    }

    public delegate State[] DynamicsFunction(State[] states, double time);
    public abstract class Integrator
    {
        // Next integration step proposed by the numerical method
        protected abstract double Dt { get; set; }

        public void Integrate(
            IReadOnlyList<ISimulatable> objects,
            DynamicsFunction F,
            double time,
            double DeltaT)
        {
            if (!double.IsFinite(DeltaT) || DeltaT < 0)
                throw new ArgumentOutOfRangeException(nameof(DeltaT));

            double elapsedTime = 0;

            while (elapsedTime < DeltaT)
            {
                // Remaining simulation time
                double remainingTime = DeltaT - elapsedTime;

                // Limit the proposed step to the remaining time
                double requestedDt = Math.Min(Dt, remainingTime);

                if (!double.IsFinite(requestedDt) || requestedDt <= 0)
                    throw new InvalidOperationException("Invalid integration step.");

                // Step may accept a smaller duration
                double actualDt = Step(
                    objects,
                    F,
                    time + elapsedTime,
                    requestedDt
                );

                if (!double.IsFinite(actualDt) ||
                    actualDt <= 0 ||
                    actualDt > requestedDt)
                {
                    throw new InvalidOperationException(
                        "Integrator returned an invalid integration duration."
                    );
                }

                double nextElapsedTime = elapsedTime + actualDt;

                if (nextElapsedTime <= elapsedTime)
                    throw new InvalidOperationException(
                        "Integration step is too small to advance simulation time."
                    );

                elapsedTime = nextElapsedTime;
            }
        }

        protected abstract double Step(
            IReadOnlyList<ISimulatable> objects,
            DynamicsFunction F,
            double time,
            double dt
        );
    }

    public class EulerIntegrator : Integrator
    {

        protected override double Dt { get; set; }

        public EulerIntegrator(double dt)
        {
            if(dt <= 0 || !double.IsFinite(dt))
            {
                throw new ArgumentOutOfRangeException(nameof(dt));
            }
            Dt = dt;
        }
        protected override double Step(IReadOnlyList<ISimulatable> objects, DynamicsFunction F, double time, double dt)
        {
            State[] states = new State[objects.Count];

            for(int i = 0; i < objects.Count; i++)
            {
                states[i] = objects[i].GetState();
            }

            State[] derivatedStates = F(states, time);

            if (derivatedStates.Length != states.Length)
            {
                throw new InvalidOperationException(
                    "Dynamics function returned a different number of states than expected."
                );
            }

            for (int i = 0;i < states.Length; i++)
            {
                states[i].Y += derivatedStates[i].Y * dt;
                states[i].YDot += derivatedStates[i].YDot * dt;
                objects[i].SetState(states[i]);
            }

            return dt;

        }
    }

    public class RK4Integrator : Integrator
    {
        protected override double Dt { get; set; }

        public RK4Integrator(double dt)
        {
            if (dt <= 0 || !double.IsFinite(dt))
            {
                throw new ArgumentOutOfRangeException(nameof(dt));
            }
            Dt = dt;
        }

        protected override double Step(
            IReadOnlyList<ISimulatable> objects,
            DynamicsFunction F,
            double time,
            double dt)
        {
            State[] states = new State[objects.Count];

            for (int i = 0; i < objects.Count; i++)
            {
                states[i] = objects[i].GetState();
            }

            // Compute k1
            State[] k1States = F(states, time);

            if (k1States.Length != states.Length)
            {
                throw new InvalidOperationException(
                    "Dynamics function returned a different number of states than expected."
                );
            }

            // Compute k2
            State[] k2States = (State[])states.Clone();
            for (int i = 0; i < states.Length; i++)
            {
                k2States[i].Y += k1States[i].Y * (dt / 2);
                k2States[i].YDot += k1States[i].YDot * (dt / 2);
            }
            k2States = F(k2States, time + dt / 2);

            // Compute k3
            State[] k3States = (State[])states.Clone();
            for (int i = 0; i < states.Length; i++)
            {
                k3States[i].Y += k2States[i].Y * (dt / 2);
                k3States[i].YDot += k2States[i].YDot * (dt / 2);
            }
            k3States = F(k3States, time + dt / 2);


            // Compute k4
            State[] k4States = (State[])states.Clone();
            for (int i = 0; i < states.Length; i++)
            {
                k4States[i].Y += k3States[i].Y * dt;
                k4States[i].YDot += k3States[i].YDot * dt;
            }
            k4States = F(k4States, time + dt );


            // Update states
            for (int i = 0; i < states.Length; i++)
            {
                states[i].Y += (k1States[i].Y + 2 * k2States[i].Y + 2 * k3States[i].Y + k4States[i].Y) * (dt / 6);
                states[i].YDot += (k1States[i].YDot + 2 * k2States[i].YDot + 2 * k3States[i].YDot + k4States[i].YDot) * (dt / 6);
                objects[i].SetState(states[i]);
            }

            return dt;
        }
    }

    public class VerletIntegrator : Integrator
    {
        protected override double Dt { get; set; }
        public VerletIntegrator(double dt)
        {
            if (dt <= 0 || !double.IsFinite(dt))
            {
                throw new ArgumentOutOfRangeException(nameof(dt));
            }
            Dt = dt;

        }

        protected override double Step(
            IReadOnlyList<ISimulatable> objects,
            DynamicsFunction F,
            double time,
            double dt)
        {

            State[] states_0 = new State[objects.Count];

            for (int i = 0; i < objects.Count; i++)
            {
                states_0[i] = objects[i].GetState();
            }

            State[] derivatedStates = F(states_0, time);

            // Update positions
            for (int i = 0; i < states_0.Length; i++)
            {
                states_0[i].Y += states_0[i].YDot * dt + 0.5 * derivatedStates[i].YDot * dt * dt;
            }

            // Compute new derivatives
            State[] newDerivatives = F(states_0, time + dt);

            // Update velocities
            for (int i = 0; i < states_0.Length; i++)
            {
                states_0[i].YDot += 0.5 * (derivatedStates[i].YDot + newDerivatives[i].YDot) * dt;
                objects[i].SetState(states_0[i]);
            }

            return dt;
        }
    }



}
