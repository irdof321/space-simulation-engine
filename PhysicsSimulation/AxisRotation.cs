using PhysicsSimulation;
using System;

public class AxialRotation
{
    public Vector3Double Axis { get; }
    public double Period { get; }
    public double InitialAngle { get; }

    public AxialRotation(Vector3Double axis, double period, double initialAngle)
    {
        if (axis.GetSquareMagnitude() == 0)
        {
            throw new ArgumentException("Axis vector cannot be zero.");
        }
        if (period <= 0 || !double.IsFinite(period))
        {
            throw new ArgumentException("Period must be a positive finite number.");
        }
        if (!double.IsFinite(initialAngle))
        {
            throw new ArgumentException("Initial angle must be a finite number.");
        }
        if (!double.IsFinite(axis.X) ||
            !double.IsFinite(axis.Y) ||
            !double.IsFinite(axis.Z))
        {
            throw new ArgumentException("Axis components must be finite.");
        }
        Axis = axis.GetUnit();
        Period = period;
        InitialAngle = initialAngle;
    }

    public QuaternionDouble GetOrientation(double time)
    {
        // TODO
        time = time % Period;
        double angle = InitialAngle + (2 * Math.PI * time) / Period;
        return QuaternionDouble.FromAxisAngle(Axis, angle);
    }
}