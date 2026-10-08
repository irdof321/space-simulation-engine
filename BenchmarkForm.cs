using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ScottPlot.WinForms;

public sealed class BenchmarkForm : Form
{
    private readonly FormsPlot energyYearPlot = new();
    private readonly FormsPlot positionConvergencePlot = new();
    private readonly FormsPlot energyConvergencePlot = new();
    private readonly FormsPlot longTermEnergyPlot = new();

    public BenchmarkForm()
    {
        Text = "Physics Simulation - Integrator Benchmark";
        Width = 1600;
        Height = 1000;
        StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        Controls.Add(layout);

        foreach (FormsPlot plot in new[]
        {
            energyYearPlot, positionConvergencePlot,
            energyConvergencePlot, longTermEnergyPlot
        })
        {
            plot.Dock = DockStyle.Fill;
        }

        layout.Controls.Add(energyYearPlot, 0, 0);
        layout.Controls.Add(positionConvergencePlot, 1, 0);
        layout.Controls.Add(energyConvergencePlot, 0, 1);
        layout.Controls.Add(longTermEnergyPlot, 1, 1);
    }

    public void DisplayResults(
        IReadOnlyList<BenchmarkResult> convergenceResults,
        IReadOnlyList<BenchmarkResult> longTermResults,
        double referencePeriod)
    {
        if (referencePeriod <= 0 || !double.IsFinite(referencePeriod))
            throw new ArgumentOutOfRangeException(nameof(referencePeriod));

        ShowEnergyEvolution(energyYearPlot, convergenceResults,
            "Energy error - convergence experiment (20 reference orbits)",
            referencePeriod);
        ShowConvergence(positionConvergencePlot, convergenceResults,
            result => result.FinalPositionError,
            "Position convergence", "Relative final position error");
        ShowConvergence(energyConvergencePlot, convergenceResults,
            result => Math.Abs(result.RelativeEnergyErrors[^1]),
            "Final energy error convergence", "Absolute relative energy error");
        ShowEnergyEvolution(longTermEnergyPlot, longTermResults,
            "Energy error - long-term experiment (1000 reference orbits)",
            referencePeriod);
    }

    private static void ShowEnergyEvolution(
        FormsPlot formsPlot,
        IReadOnlyList<BenchmarkResult> results,
        string title,
        double referencePeriod)
    {
        formsPlot.Plot.Clear();
        foreach (BenchmarkResult result in results)
        {
            if (result.Times.Count == 0)
                continue;

            double[] xs = result.Times.Select(t => t / referencePeriod).ToArray();
            double[] ys = result.RelativeEnergyErrors.ToArray();
            var scatter = formsPlot.Plot.Add.Scatter(xs, ys);
            scatter.LegendText = $"{result.IntegratorName}, dt={result.Dt:G4} s";
        }

        formsPlot.Plot.Title(title);
        formsPlot.Plot.XLabel("Time (reference orbital periods)");
        formsPlot.Plot.YLabel("(E - E0) / |E0|");
        formsPlot.Plot.ShowLegend();
        formsPlot.Plot.Axes.AutoScale();
        formsPlot.Refresh();
    }

    private static void ShowConvergence(
        FormsPlot formsPlot,
        IReadOnlyList<BenchmarkResult> results,
        Func<BenchmarkResult, double> errorSelector,
        string title,
        string yLabel)
    {
        formsPlot.Plot.Clear();
        foreach (var group in results.GroupBy(r => r.IntegratorName))
        {
            var valid = group
                .Select(r => (Dt: r.Dt, Error: errorSelector(r)))
                .Where(p => p.Dt > 0 && double.IsFinite(p.Error) && p.Error > 0)
                .OrderBy(p => p.Dt)
                .ToArray();
            if (valid.Length < 2)
                continue;

            double[] xs = valid.Select(p => Math.Log10(p.Dt)).ToArray();
            double[] ys = valid.Select(p => Math.Log10(p.Error)).ToArray();
            double slope = FitSlope(xs, ys);
            var scatter = formsPlot.Plot.Add.Scatter(xs, ys);
            scatter.LegendText = $"{group.Key}, slope={slope:F3}";
        }

        formsPlot.Plot.Title(title);
        formsPlot.Plot.XLabel("log10(dt / s)");
        formsPlot.Plot.YLabel($"log10({yLabel})");
        formsPlot.Plot.ShowLegend();
        formsPlot.Plot.Axes.AutoScale();
        formsPlot.Refresh();
    }

    private static double FitSlope(double[] xs, double[] ys)
    {
        double meanX = xs.Average();
        double meanY = ys.Average();
        double numerator = 0.0;
        double denominator = 0.0;

        for (int i = 0; i < xs.Length; i++)
        {
            numerator += (xs[i] - meanX) * (ys[i] - meanY);
            denominator += (xs[i] - meanX) * (xs[i] - meanX);
        }

        return denominator == 0.0 ? double.NaN : numerator / denominator;
    }
}
