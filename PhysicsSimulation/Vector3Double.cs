using System;

namespace PhysicsSimulation
{
    public struct Vector3Double
    {
        private double _x;
        private double _y;
        private double _z;

        public Vector3Double(double x = 0, double y = 0, double z = 0)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        public double Z
        {
            get { return _z; }
            set { _z = value; }
        }

        public double GetSquareMagnitude()
        {
            return _x * _x + _y * _y + _z * _z;
        }

        public double GetMagnitude()
        {
            return Math.Sqrt(GetSquareMagnitude());
        }

        public Vector3Double GetUnit()
        {
            double magnitude = GetMagnitude();

            if (magnitude == 0)
            {
                throw new DivideByZeroException("Magnitude is zero");
            }

            return this / magnitude;
        }

        public double Dot(Vector3Double v)
        {
            return (_x * v.X) + (_y * v.Y) + (_z * v.Z);
        }

        public static Vector3Double operator+ (Vector3Double v1, Vector3Double v2)
        {
            return new Vector3Double(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Vector3Double operator* (double scalar, Vector3Double v)
        {
            return new Vector3Double(scalar * v._x, scalar * v._y, scalar * v._z);
        }

        public static Vector3Double operator* (Vector3Double v, double scalar)
        {
            return scalar * v;
        }

        public static Vector3Double operator/ (Vector3Double v1, double val)
        {
            if (val == 0)
            {
                throw new DivideByZeroException("Val is zero");
            }

            return new Vector3Double(v1.X / val, v1.Y / val, v1.Z / val);
        }
        public static Vector3Double operator/ (double val, Vector3Double v1)
        {
            return v1 / val;
        }

        public static Vector3Double operator- (Vector3Double v)
        {
            return new Vector3Double(-v._x, -v._y, -v._z);
        }

        public static Vector3Double operator- (Vector3Double v1, Vector3Double v2)
        {
            return v1 + (-v2);
        }

        public static bool operator !=(Vector3Double v1, Vector3Double v2)
        {
            return !(v1 == v2);
        }
        public static bool operator==(Vector3Double v1, Vector3Double v2)
        {
            return (v1.X == v2.X) && (v1.Y == v2.Y) && (v1.Z == v2.Z);
        }

        public static Vector3Double Cross(Vector3Double u, Vector3Double v)
        {
            return new Vector3Double(
                u.Y * v.Z - u.Z * v.Y,
                u.Z * v.X - u.X * v.Z,
                u.X * v.Y - u.Y * v.X
            );
        }
    }
}