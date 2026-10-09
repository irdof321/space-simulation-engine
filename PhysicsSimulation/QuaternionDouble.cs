using System;
using System.Collections.Generic;
using System.Text;

namespace PhysicsSimulation
{
    public struct QuaternionDouble
    {
        public double W;
        public double X;
        public double Y;
        public double Z;
    }

    public static QuaternionDouble FromAxisAngle(
    Vector3Double axis,
    double angle)
        {
            if(axis.GetSquareMagnitude() == 0)
            {
                throw new ArgumentException("Axis vector cannot be zero.");
            }
            if(!double.IsFinite(angle))
            {
                throw new ArgumentException("Angle must be a finite number.");
            }
            if (!double.IsFinite(axis.X) ||
                !double.IsFinite(axis.Y) ||
                !double.IsFinite(axis.Z))
            {
                throw new ArgumentException(
                    "Axis components must be finite.",
                    nameof(axis));
            }
            axis = axis.GetUnit();
            double halfAngle = angle / 2;
            double sinHalfAngle = Math.Sin(halfAngle);
            return new QuaternionDouble
            {
                W = Math.Cos(halfAngle),
                X = axis.X * sinHalfAngle,
                Y = axis.Y * sinHalfAngle,
                Z = axis.Z * sinHalfAngle
            };
        }

        public static QuaternionDouble operator *(
            QuaternionDouble a,
            QuaternionDouble b)
        {
            Vector3Double qA = new Vector3Double(a.X, a.Y, a.Z);
            Vector3Double qB = new Vector3Double(b.X, b.Y, b.Z);
            Vector3Double ijk = a.W*qB + b.W*qA + Vector3Double.Cross(qA, qB);
            return new QuaternionDouble
           {
               W = a.W*b.W - qA.Dot(qB),
               X = ijk.X,
               Y = ijk.Y,
               Z = ijk.Z
            };
        }

        public QuaternionDouble Conjugate()
        {
            // Return the conjugate quaternion.
            return new QuaternionDouble
            {
                W = this.W,
                X = -this.X,
                Y = -this.Y,
                Z = -this.Z
            };
        }
        public Vector3Double RotateVector(Vector3Double vector)
        {
            // TODO: Rotate the vector using this quaternion.


        }
    }
}
