using System;
using PhysicsSimulation.Mathematics;

// Standalone mathematical tests for QuaternionDouble and AxialRotation.
// These tests do not depend on Unity, WinForms, or N-body integration.
internal static class AxialRotationTests
{
    private const double Tolerance = 1e-11;

    public static void Run()
    {
        int passed = 0;
        int failed = 0;

        void Check(bool condition, string name)
        {
            Console.WriteLine($"[{(condition ? "PASS" : "FAIL")}] {name}");
            if (condition) passed++;
            else failed++;
        }

        bool Near(double actual, double expected, double scale = 1.0)
        {
            if (!double.IsFinite(actual) || !double.IsFinite(expected))
                return false;

            return Math.Abs(actual - expected)
                <= Tolerance * Math.Max(1.0, Math.Max(scale, Math.Abs(expected)));
        }

        bool NearVector(Vector3Double actual, Vector3Double expected)
        {
            double scale = Math.Max(actual.GetMagnitude(), expected.GetMagnitude());
            return Near(actual.X, expected.X, scale)
                && Near(actual.Y, expected.Y, scale)
                && Near(actual.Z, expected.Z, scale);
        }

        // Quaternions q and -q encode the SAME 3D rotation. Check their
        // action on a basis instead of comparing W/X/Y/Z directly.
        bool SameRotation(QuaternionDouble a, QuaternionDouble b)
        {
            return NearVector(a.RotateVector(new Vector3Double(1, 0, 0)),
                              b.RotateVector(new Vector3Double(1, 0, 0)))
                && NearVector(a.RotateVector(new Vector3Double(0, 1, 0)),
                              b.RotateVector(new Vector3Double(0, 1, 0)))
                && NearVector(a.RotateVector(new Vector3Double(0, 0, 1)),
                              b.RotateVector(new Vector3Double(0, 0, 1)));
        }

        double Dot(Vector3Double a, Vector3Double b)
        {
            return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        }

        Vector3Double Scale(Vector3Double v, double scalar)
        {
            return new Vector3Double(v.X * scalar, v.Y * scalar, v.Z * scalar);
        }

        bool Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
                return false;
            }
            catch (TException)
            {
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        Console.WriteLine("========================================");
        Console.WriteLine("QUATERNION / AXIAL ROTATION TESTS");
        Console.WriteLine("========================================");

        var zero = new Vector3Double(0, 0, 0);
        var x = new Vector3Double(1, 0, 0);
        var y = new Vector3Double(0, 1, 0);
        var z = new Vector3Double(0, 0, 1);
        var minusX = new Vector3Double(-1, 0, 0);
        var minusY = new Vector3Double(0, -1, 0);

        Console.WriteLine();
        Console.WriteLine("--- Quaternion algebra ---");

        QuaternionDouble quarterZ = QuaternionDouble.FromAxisAngle(z, Math.PI / 2);
        QuaternionDouble identity = QuaternionDouble.FromAxisAngle(z, 0);

        Check(NearVector(identity.RotateVector(new Vector3Double(2, -3, 4)),
                         new Vector3Double(2, -3, 4)),
              "Zero-angle quaternion leaves a vector unchanged");

        Check(NearVector(quarterZ.RotateVector(x), y),
              "+90 degrees about Z sends +X to +Y");
        Check(NearVector(quarterZ.RotateVector(y), minusX),
              "+90 degrees about Z sends +Y to -X");
        Check(NearVector(quarterZ.RotateVector(z), z),
              "Rotation leaves its axis fixed");

        Vector3Double arbitraryVector = new Vector3Double(2.5, -3.5, 7.0);
        Vector3Double rotated = quarterZ.RotateVector(arbitraryVector);
        Check(Near(rotated.GetMagnitude(), arbitraryVector.GetMagnitude()),
              "Rotation preserves vector length");

        QuaternionDouble tilted = QuaternionDouble.FromAxisAngle(
            new Vector3Double(2, 3, 4), 1.234);
        var tiltedAxis = new Vector3Double(2, 3, 4).GetUnit();
        Vector3Double tiltedResult = tilted.RotateVector(arbitraryVector);
        Check(Near(tiltedResult.GetMagnitude(), arbitraryVector.GetMagnitude()),
              "Tilted-axis rotation preserves vector length");
        Check(Near(Dot(tiltedResult, tiltedAxis), Dot(arbitraryVector, tiltedAxis)),
              "Tilted-axis rotation preserves component along the axis");
        Check(NearVector(tilted.RotateVector(tiltedAxis), tiltedAxis),
              "Tilted-axis rotation leaves its axis fixed");

        // Directly evaluate the independently derived Rodrigues formula.
        double theta = 1.234;
        Vector3Double cross = Vector3Double.Cross(tiltedAxis, arbitraryVector);
        Vector3Double expectedRodrigues =
            Scale(arbitraryVector, Math.Cos(theta))
            + Scale(cross, Math.Sin(theta))
            + Scale(tiltedAxis,
                Dot(tiltedAxis, arbitraryVector) * (1.0 - Math.Cos(theta)));
        Check(NearVector(tiltedResult, expectedRodrigues),
              "Quaternion rotation agrees with Rodrigues' formula");

        QuaternionDouble negativeTilted = new QuaternionDouble
        {
            W = -tilted.W,
            X = -tilted.X,
            Y = -tilted.Y,
            Z = -tilted.Z
        };
        Check(SameRotation(tilted, negativeTilted),
              "q and -q represent the same rotation");

        Check(SameRotation(tilted * tilted.Conjugate(), identity),
              "Multiplying a unit quaternion by its conjugate is identity");
        Check(NearVector(tilted.Conjugate().RotateVector(tiltedResult), arbitraryVector),
              "Conjugate reverses the rotation");

        QuaternionDouble quarterX = QuaternionDouble.FromAxisAngle(x, Math.PI / 2);
        Vector3Double composed = (quarterZ * quarterX).RotateVector(arbitraryVector);
        Vector3Double successive = quarterZ.RotateVector(
            quarterX.RotateVector(arbitraryVector));
        Check(NearVector(composed, successive),
              "Quaternion product composes rotations in the correct order");

        QuaternionDouble raw = new QuaternionDouble { W = 2, X = -3, Y = 4, Z = 5 };
        QuaternionDouble normalized = raw.GetUnit();
        double squaredNorm = normalized.W * normalized.W
            + normalized.X * normalized.X
            + normalized.Y * normalized.Y
            + normalized.Z * normalized.Z;
        Check(Near(squaredNorm, 1), "GetUnit produces a unit quaternion");
        Check(Throws<InvalidOperationException>(() =>
            new QuaternionDouble().GetUnit()),
            "Zero quaternion cannot be normalized");

        Console.WriteLine();
        Console.WriteLine("--- AxialRotation: T = 10 s, initial angle = 0 ---");

        var spin = new AxialRotation(z, 10.0, 0.0);
        Check(NearVector(spin.GetOrientation(0).RotateVector(x), x),
              "t = 0 s: zero rotation");
        Check(NearVector(spin.GetOrientation(2.5).RotateVector(x), y),
              "t = 2.5 s: quarter turn");
        Check(NearVector(spin.GetOrientation(5.0).RotateVector(x), minusX),
              "t = 5 s: half turn");
        Check(NearVector(spin.GetOrientation(7.5).RotateVector(x), minusY),
              "t = 7.5 s: three-quarter turn");
        Check(SameRotation(spin.GetOrientation(10.0), spin.GetOrientation(0)),
              "t = 10 s: full turn matches initial orientation");
        Check(SameRotation(spin.GetOrientation(12.5), spin.GetOrientation(2.5)),
              "t = 12.5 s: orientation repeats after one period");
        Check(NearVector(spin.GetOrientation(-2.5).RotateVector(x), minusY),
              "Negative time rotates backward");
        Check(SameRotation(spin.GetOrientation(1_000_000_000_002.5),
                           spin.GetOrientation(2.5)),
              "Large simulation time preserves periodic orientation");

        var phased = new AxialRotation(z, 10.0, Math.PI / 2);
        Check(NearVector(phased.GetOrientation(0).RotateVector(x), y),
              "Nonzero initial angle is applied at t = 0");

        // The constructor should normalize its axis rather than require unit input.
        var scaledAxisSpin = new AxialRotation(new Vector3Double(0, 0, 9), 10, 0);
        Check(SameRotation(scaledAxisSpin.GetOrientation(2.5), spin.GetOrientation(2.5)),
              "Axis magnitude does not change the rotation");

        // Orientation is a pure function of time, not a frame-by-frame accumulator.
        QuaternionDouble past = spin.GetOrientation(3.75);
        spin.GetOrientation(9.25);
        Check(SameRotation(past, spin.GetOrientation(3.75)),
              "Repeated evaluation at the same time is deterministic");

        Console.WriteLine();
        Console.WriteLine("--- Invalid inputs ---");
        Check(Throws<ArgumentException>(() => new AxialRotation(zero, 10, 0)),
              "Zero spin axis is rejected");
        Check(Throws<ArgumentException>(() => new AxialRotation(z, 0, 0)),
              "Zero spin period is rejected");
        Check(Throws<ArgumentException>(() => new AxialRotation(z, -10, 0)),
              "Negative spin period is rejected");
        Check(Throws<ArgumentException>(() => new AxialRotation(z, double.NaN, 0)),
              "Non-finite spin period is rejected");
        Check(Throws<ArgumentException>(() => new AxialRotation(z, 10, double.NaN)),
              "Non-finite initial angle is rejected");

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine($"PASSED: {passed}");
        Console.WriteLine($"FAILED: {failed}");
        Console.WriteLine("========================================");

        if (failed > 0)
            throw new InvalidOperationException(
                $"{failed} quaternion / axial rotation test(s) failed.");

        Console.WriteLine("All quaternion / axial rotation tests passed.");
    }
}
