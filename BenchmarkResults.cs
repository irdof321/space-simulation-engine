using System;
using System.Collections.Generic;

public sealed class BenchmarkResult
{
    public string IntegratorName { get; }
    public double Dt { get; }
    public List<double> Times { get; } = new();
    public List<double> RelativeEnergyErrors { get; } = new();
    public double FinalPositionError { get; private set; } = double.NaN;

    public BenchmarkResult(string integratorName, double dt)
    {
        IntegratorName = integratorName;
        Dt = dt;
    }

    public void AddMeasurement(double time, double relativeEnergyError)
    {
        Times.Add(time);
        RelativeEnergyErrors.Add(relativeEnergyError);
    }

    public void SetFinalPositionError(double error) => FinalPositionError = error;
}
