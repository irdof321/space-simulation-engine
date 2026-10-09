# Space Simulation — Separate simulation speed and display refresh

This patch applies to the working **two distant systems + Comet C1 + performance counters** `SimulationBenchmark` project.

## Installation

Back up your project, then replace **only**:

- `Configuration/AppSettings.cs`
- `UI/OrbitalLiveForm.cs`

Keep your current `UI/OrbitalCanvas.cs`, `Live/LivePerformanceCounters.cs`, scene presets, physics engine, tests and benchmarks unchanged.

## New independent controls

- **Simulation: 2 / 8 / 32 / 128 / 512 steps/update**: how many fixed-size physical integrations happen on each simulation timer tick.
- **Display: 1 / 2 / 5 / 10 / 15 / 30 FPS**: how often the program *requests* a repaint of the orbital canvas.
- **Pause** stops both timers; **Resume** restarts both.

Edit `Configuration/AppSettings.cs` for the startup defaults:

```csharp
public const int PhysicsUpdatesPerSecond = 30;
public const int InitialStepsPerUpdate = 8;
public const int AnimationFramesPerSecond = 2;
```

The **physics integration time step stays at `TimeStepHours = 6.0`**, regardless of playback and refresh controls.

Theoretical physical throughput is `steps/update × physics updates/second`. For example, 128 × 30 = 3840 six-hour integration steps per wall-clock second *if the UI keeps up*. With the old code, every simulation update requested a full, expensive redraw. With this patch, only the display timer requests redraws, so at 2 FPS the 200 ms canvas paint cost is incurred about twice per second rather than as frequently as the application can manage.

## Important UI-thread limitation

This is **two independent schedules, not two independent execution threads**. Both `System.Windows.Forms.Timer` callbacks and the actual painting run on the same WinForms UI thread. A slow canvas paint still blocks physics for its duration, so the requested 30 physics updates/second and 10/15/30 display FPS are not guaranteed. At the measured ~200 ms per paint, start at 1 or 2 display FPS. The diagnostics line reports the **actual** FPS and steps/s. A later optional upgrade is a background physics worker with synchronized display snapshots; this patch intentionally does not add that complexity.

The original benchmark modes and test classes are unaffected. The physics engine and numerical integrator are unchanged. Source was statically checked here, but compilation/runtime behavior must be confirmed in Visual Studio on Windows.
