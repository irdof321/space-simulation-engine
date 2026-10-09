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
            QuaternionDouble vectorQuat = new QuaternionDouble
            {
                W = 0,
                X = vector.X,
                Y = vector.Y,
                Z = vector.Z
            };
            QuaternionDouble rotatedQuat = this * vectorQuat * this.Conjugate();
            return new Vector3Double(rotatedQuat.X, rotatedQuat.Y, rotatedQuat.Z);

        }

        public QuaternionDouble GetUnit()
        {
            double magnitude = Math.Sqrt(W * W + X * X + Y * Y + Z * Z);
            if (magnitude == 0 || !double.IsFinite(magnitude))
            {
                throw new InvalidOperationException("Cannot normalize a zero quaternion.");
            }
            return new QuaternionDouble
            {
                W = W / magnitude,
                X = X / magnitude,
                Y = Y / magnitude,
                Z = Z / magnitude
            };
        }

        public List<QuaternionDouble> Slerp(QuaternionDouble target, int steps)
        {
            List<QuaternionDouble> result = new List<QuaternionDouble>();
            QuaternionDouble start = this.GetUnit();
            target = target.GetUnit();
            double dot = start.W * target.W + start.X * target.X + start.Y * target.Y + start.Z * target.Z;
            if (dot < 0.0f)
            {
                target.W = -target.W;
                target.X = -target.X;
                target.Y = -target.Y;
                target.Z = -target.Z;
                dot = -dot;
            }
            const double DOT_THRESHOLD = 0.9995;
            if (dot > DOT_THRESHOLD)
            {
                for (int i = 0; i <= steps; i++)
                {
                    double t = (double)i / steps;
                    QuaternionDouble interpolated = new QuaternionDouble
                    {
                        W = start.W + t * (target.W - start.W),
                        X = start.X + t * (target.X - start.X),
                        Y = start.Y + t * (target.Y - start.Y),
                        Z = start.Z + t * (target.Z - start.Z)
                    };
                    result.Add(interpolated.GetUnit());
                }
                return result;
            }
            double theta_0 = Math.Acos(dot);
            double sin_theta_0 = Math.Sin(theta_0);
            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double theta_t = theta_0 * t;
                double sin_theta_t = Math.Sin(theta_t);
                double s0 = Math.Cos(theta_t) - dot * sin_theta_t / sin_theta_0;
                double s1 = sin_theta_t / sin_theta_0;
                QuaternionDouble interpolated = new QuaternionDouble
                {
                    W = s0 * start.W + s1 * target.W,
                    X = s0 * start.X + s1 * target.X,
                    Y = s0 * start.Y + s1 * target.Y,
                    Z = s0 * start.Z + s1 * target.Z
                };
                result.Add(interpolated.GetUnit());
            }
            return result;
        }
    }
}
