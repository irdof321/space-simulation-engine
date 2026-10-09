using System;
using System.Collections.Generic;
using System.Text;

namespace PhysicsSimulation
{

    public class OrbitalSystem : OrbitalObject
    {
        private readonly List<OrbitalMember> _members;

        public IReadOnlyList<OrbitalMember> Members => _members.AsReadOnly();

        public void AddMember(OrbitalMember member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member), "OrbitalMember cannot be null.");
            }
            if(member.OrbitalObject == null)
            {
                throw new ArgumentException("OrbitalMember must have a valid OrbitalObject.", nameof(member));
            }
            if (ReferenceEquals(member.OrbitalObject, this))
            {
                throw new ArgumentException(
                    "An OrbitalSystem cannot contain itself.",
                    nameof(member));
            }
            if (this.CheckIsAParent(member.OrbitalObject))
            {
                throw new ArgumentException("An OrbitalSystem cannot be a member of one of its own children.", nameof(member));
            }
            if (member.OrbitalObject.Parent != null)
            {
                throw new ArgumentException("OrbitalObject is already a member of another OrbitalSystem.", nameof(member));
            }
            _members.Add(member);
            member.OrbitalObject.Parent = this;
        }

        // Constructor
        public OrbitalSystem()
        {
            _members = new List<OrbitalMember>();
        }

        // Implement abstract properties and methods from OrbitalObject
        public override double TotalMass
        {
            get
            {
                double totalMass = 0;
                foreach (var member in _members)
                {
                    totalMass += member.OrbitalObject.TotalMass;
                }
                return totalMass;
            }
        }

        public override Vector3Double CenterOfMass
        {
            get
            {
                Vector3Double centerOfMass = new Vector3Double(0, 0, 0);
                foreach (var member in _members)
                {
                    centerOfMass += member.OrbitalObject.CenterOfMass * member.OrbitalObject.TotalMass;
                }
                return centerOfMass / TotalMass;
            }
        }

        public override Vector3Double CenterOfMassVelocity
        {
            get
            {
                Vector3Double centerOfMassVelocity = new Vector3Double(0, 0, 0);
                foreach (var member in _members)
                {
                    centerOfMassVelocity += member.OrbitalObject.CenterOfMassVelocity * member.OrbitalObject.TotalMass;
                }
                return centerOfMassVelocity / TotalMass;
            }
        }

        public override void TranslatePosition(Vector3Double displacement)
        {
            foreach (var member in _members)
            {
                member.OrbitalObject.TranslatePosition(displacement);
            }
        }

        public override void TranslateVelocity(Vector3Double deltaVelocity)
        {
            foreach (var member in _members)
            {
                member.OrbitalObject.TranslateVelocity(deltaVelocity);
            }
        }

        public override IEnumerable<CelestBody> GetBodies()
        {
            foreach (var member in _members)
            {
                foreach (var body in member.OrbitalObject.GetBodies())
                {
                    yield return body;
                }
            }
        }


    }
}
