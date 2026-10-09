using System;
using System.Collections.Generic;
using System.Text;

namespace PhysicsSimulation
{
    public abstract class OrbitalObject
    {
        protected OrbitalObject? _parent;
        public OrbitalObject? Parent { get => _parent; internal set => _parent = value; }

        public bool CheckIsAParent(OrbitalObject potentialAncestor)
        {
            if (Parent == null)
            {
                return false;
            }

            if (ReferenceEquals(Parent, potentialAncestor))
            {
                return true;
            }

            return Parent.CheckIsAParent(potentialAncestor);
        }

        public abstract double TotalMass { get; }
        public abstract Vector3Double CenterOfMass { get; }
        public abstract Vector3Double CenterOfMassVelocity { get; }

        public abstract void TranslatePosition(Vector3Double displacement);
        public abstract void TranslateVelocity(Vector3Double deltaVelocity);
        public abstract IEnumerable<CelestBody> GetBodies();
    }
}
