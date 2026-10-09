using System.Collections.Generic;

namespace PhysicsSimulation
{
    public class CelestBody : OrbitalObject, ISimulatable
    {
        private Vector3Double _speed;
        private Vector3Double _position;
        private double _mass;

        public Vector3Double Speed
        {
            get { return _speed; }
            set { _speed = value; }
        }
        public double Mass
        {
            get { return _mass; }
        }
        public Vector3Double Position
        {
            get { return _position; }
        }


        public CelestBody(double mass, Vector3Double position)
        {
            _mass = mass;
            _position = position;
        }

        //OrbitalObject implementation
        public override double TotalMass { get { return _mass; } }
        public override Vector3Double CenterOfMass { get { return _position; } }
        public override Vector3Double CenterOfMassVelocity { get { return _speed; } }

        public override void TranslatePosition(Vector3Double displacement)
        {
            _position += displacement;
        }

        public override void TranslateVelocity(Vector3Double deltaVelocity)
        {
            _speed += deltaVelocity;
        }

        public override IEnumerable<CelestBody> GetBodies()
        {
            yield return this;
        }

        // Interface implementation for ISimulatable
        public State GetState()
        {
            State s = new State()
            {
                Y = _position,
                YDot = _speed
            };
            return s;
        }

        public void SetState(State state)
        {
            _position = state.Y;
            _speed = state.YDot;
        }


    }

}
