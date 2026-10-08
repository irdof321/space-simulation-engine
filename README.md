# Benchmark updates

Replace `Program.cs` and `BenchmarkForm.cs` in your existing `SimulationBenchmark` project.
Keep your current `BenchmarkRunner.cs`, `BenchmarkResult.cs` and `WorldFactory.cs`.

- The angular momentum test is implemented entirely in Program.cs: L = sum(m * (r x v)). No changes to PhysicsSimulation are required.
- The console prints initial/final linear and angular momentum and normalized drifts for RK4 and Verlet.
- The two time-series plots display time in **reference orbital periods**; the log-log convergence plots retain seconds for dt.
- All duration titles reflect the actual benchmark runs.
- The angular momentum test uses angular momentum about the coordinate origin, which is conserved for an isolated Newtonian system. Nonzero total angular momentum is expected.
- The `Vector3Double` type is assumed to expose public X, Y, Z members. If your names differ, adapt the `Cross` method.
- Any old `BenchmarkForm.Designer.cs` must not declare a conflicting form class (this form is constructed programmatically).

Note: '1000 reference periods' means 1000 periods of the chosen reference orbit, not 1000 completed orbits of every planet. The underlying engine and integration step handling are unchanged.
