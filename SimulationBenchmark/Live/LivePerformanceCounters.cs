using System;
using System.Diagnostics;

// Observes playback performance without changing simulation steps or rendering.
internal sealed class LivePerformanceCounters
{
    private readonly Stopwatch _windowClock = Stopwatch.StartNew();

    private long _physicsTicks;
    private long _trailTicks;
    private long _statusTicks;
    private long _tickTicks;
    private long _paintTicks;
    private int _simulationTicks;
    private int _paintCount;
    private int _physicsSteps;

    public void RecordSimulationTick(
        int steps,
        long physicsTicks,
        long trailTicks,
        long statusTicks,
        long totalTickTicks)
    {
        _simulationTicks++;
        _physicsSteps += steps;
        _physicsTicks += physicsTicks;
        _trailTicks += trailTicks;
        _statusTicks += statusTicks;
        _tickTicks += totalTickTicks;
    }

    public void RecordPaint(long paintTicks)
    {
        _paintTicks += paintTicks;
        _paintCount++;
    }

    public bool TryGetReport(out string report, bool force = false)
    {
        double elapsedSeconds = _windowClock.Elapsed.TotalSeconds;
        if ((!force && elapsedSeconds < 1.0) || _simulationTicks == 0)
        {
            report = string.Empty;
            return false;
        }

        double ticksPerMillisecond = Stopwatch.Frequency / 1000.0;
        double physicsMs = _physicsTicks / ticksPerMillisecond / _simulationTicks;
        double trailsMs = _trailTicks / ticksPerMillisecond / _simulationTicks;
        double statusMs = _statusTicks / ticksPerMillisecond / _simulationTicks;
        double fullTickMs = _tickTicks / ticksPerMillisecond / _simulationTicks;
        double paintMs = _paintCount > 0
            ? _paintTicks / ticksPerMillisecond / _paintCount
            : 0.0;

        double fps = _paintCount / elapsedSeconds;
        double stepsPerSecond = _physicsSteps / elapsedSeconds;

        report =
            $"FPS actual: {fps,5:F1}   |   " +
            $"Physics: {physicsMs,6:F2} ms/tick   |   " +
            $"Trails: {trailsMs,6:F2} ms/tick   |   " +
            $"Status: {statusMs,6:F2} ms/tick   |   " +
            $"Draw: {paintMs,6:F2} ms/paint   |   " +
            $"Total tick: {fullTickMs,6:F2} ms   |   " +
            $"{stepsPerSecond:F0} steps/s";

        _windowClock.Restart();
        _physicsTicks = 0;
        _trailTicks = 0;
        _statusTicks = 0;
        _tickTicks = 0;
        _paintTicks = 0;
        _simulationTicks = 0;
        _paintCount = 0;
        _physicsSteps = 0;
        return true;
    }
}
