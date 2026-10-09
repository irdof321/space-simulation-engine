using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

// WinForms is only a playback controller: it never calculates Kepler orbits.
internal sealed class OrbitalLiveForm : Form
{
    private readonly LiveSimulationSession _session;
    private readonly System.Windows.Forms.Timer _physicsTimer;
    private readonly System.Windows.Forms.Timer _displayTimer;
    private readonly OrbitalCanvas _canvas;
    private readonly Label _statusLabel;
    private readonly Label _performanceLabel;
    private readonly LivePerformanceCounters _performance = new();
    private readonly Button _pauseButton;
    private readonly ComboBox _speedSelector;
    private readonly ComboBox _refreshSelector;

    private int _completedSteps;
    private int _stepsPerUpdate = AppSettings.InitialStepsPerUpdate;
    private bool _completed;

    public bool Completed => _completed;

    public OrbitalLiveForm(LiveSimulationSession session)
    {
        _session = session;

        Text = "Space Simulation - Live N-body Orbits";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1350, 730);
        MinimumSize = new Size(970, 530);
        BackColor = Color.FromArgb(12, 19, 31);
        ForeColor = Color.FromArgb(231, 238, 246);
        Font = new Font("Segoe UI", 10f);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 83,
            Padding = new Padding(19, 11, 19, 5),
            BackColor = Color.FromArgb(18, 27, 43)
        };

        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 31,
            Text = "LIVE ORBITAL SIMULATION  •  XY PROJECTION",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.White
        };
        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(178, 197, 219),
            Font = new Font("Consolas", 10f),
            AutoEllipsis = true
        };
        header.Controls.Add(_statusLabel);
        header.Controls.Add(title);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 91,
            Padding = new Padding(18, 11, 18, 8),
            BackColor = Color.FromArgb(18, 27, 43)
        };

        _pauseButton = new Button
        {
            Text = "Pause",
            Location = new Point(18, 11),
            Size = new Size(104, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(47, 102, 157),
            ForeColor = Color.White
        };
        _pauseButton.FlatAppearance.BorderSize = 0;
        _pauseButton.Click += (_, _) => TogglePause();

        var speedLabel = new Label
        {
            Text = "Simulation:",
            AutoSize = true,
            Location = new Point(147, 18),
            ForeColor = Color.FromArgb(207, 222, 238)
        };

        _speedSelector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(233, 12),
            Size = new Size(165, 30),
            FlatStyle = FlatStyle.Flat
        };
        int[] speeds = { 2, 8, 32, 128, 512 };
        foreach (int speed in speeds)
            _speedSelector.Items.Add($"{speed} steps / update");

        _speedSelector.SelectedIndex = Array.IndexOf(speeds, AppSettings.InitialStepsPerUpdate);
        if (_speedSelector.SelectedIndex < 0)
            _speedSelector.SelectedIndex = 1;
        _stepsPerUpdate = speeds[_speedSelector.SelectedIndex];
        _speedSelector.SelectedIndexChanged += (_, _) =>
            _stepsPerUpdate = speeds[_speedSelector.SelectedIndex];

        var refreshLabel = new Label
        {
            Text = "Display:",
            AutoSize = true,
            Location = new Point(423, 18),
            ForeColor = Color.FromArgb(207, 222, 238)
        };

        _refreshSelector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(490, 12),
            Size = new Size(110, 30),
            FlatStyle = FlatStyle.Flat
        };
        int[] refreshRates = { 1, 2, 5, 10, 15, 30, 60 };
        foreach (int refreshRate in refreshRates)
            _refreshSelector.Items.Add($"{refreshRate} FPS");
        _refreshSelector.SelectedIndex = Array.IndexOf(
            refreshRates, AppSettings.AnimationFramesPerSecond);
        if (_refreshSelector.SelectedIndex < 0)
            _refreshSelector.SelectedIndex = 1;
        _refreshSelector.SelectedIndexChanged += (_, _) =>
        {
            // Updating the display frequency never changes the physics time step.
            if (_displayTimer != null)
            {
                int fps = refreshRates[_refreshSelector.SelectedIndex];
                _displayTimer.Interval = Math.Max(1, (int)Math.Round(1000.0 / fps));
            }
        };

        var hint = new Label
        {
            Text = $"Fixed physics step: {AppSettings.TimeStepHours:G} h   |   " +
                   $"Physics updates: {AppSettings.PhysicsUpdatesPerSecond}/s target",
            AutoSize = true,
            Location = new Point(615, 18),
            ForeColor = Color.FromArgb(154, 174, 198),
            Font = new Font("Segoe UI", 9f)
        };

        _performanceLabel = new Label
        {
            Text = "Measuring performance...",
            AutoSize = false,
            Dock = DockStyle.Bottom,
            Height = 27,
            ForeColor = Color.FromArgb(155, 218, 186),
            Font = new Font("Consolas", 9f),
            AutoEllipsis = true
        };

        footer.Controls.Add(_performanceLabel);
        footer.Controls.Add(_pauseButton);
        footer.Controls.Add(speedLabel);
        footer.Controls.Add(_speedSelector);
        footer.Controls.Add(refreshLabel);
        footer.Controls.Add(_refreshSelector);
        footer.Controls.Add(hint);

        _canvas = new OrbitalCanvas(session.Scene) { Dock = DockStyle.Fill };
        _canvas.PaintMeasured += _performance.RecordPaint;
        Controls.Add(_canvas);
        Controls.Add(footer);
        Controls.Add(header);

        _canvas.RecordTrailSample();
        UpdateStatus();

        if (AppSettings.PhysicsUpdatesPerSecond <= 0 ||
            AppSettings.AnimationFramesPerSecond <= 0)
        {
            throw new InvalidOperationException(
                "Physics and display update frequencies must be positive.");
        }

        // Both timers execute on the WinForms UI thread. They have independent
        // schedules, but a slow paint can delay physics ticks temporarily.
        // The expensive canvas is repainted only by the display timer.
        _physicsTimer = new System.Windows.Forms.Timer
        {
            Interval = Math.Max(1,
                (int)Math.Round(1000.0 / AppSettings.PhysicsUpdatesPerSecond))
        };
        _displayTimer = new System.Windows.Forms.Timer
        {
            Interval = Math.Max(1,
                (int)Math.Round(1000.0 / refreshRates[_refreshSelector.SelectedIndex]))
        };

        _physicsTimer.Tick += (_, _) => AdvancePhysics();
        _displayTimer.Tick += (_, _) => _canvas.Invalidate();
        Shown += (_, _) =>
        {
            _physicsTimer.Start();
            _displayTimer.Start();
        };
        FormClosed += (_, _) =>
        {
            _physicsTimer.Stop();
            _displayTimer.Stop();
        };
    }

    private void TogglePause()
    {
        if (_completed)
            return;

        if (_physicsTimer.Enabled)
        {
            _physicsTimer.Stop();
            _displayTimer.Stop();
            _pauseButton.Text = "Resume";
        }
        else
        {
            _physicsTimer.Start();
            _displayTimer.Start();
            _pauseButton.Text = "Pause";
        }
    }

    private void AdvancePhysics()
    {
        long tickStart = Stopwatch.GetTimestamp();
        long physicsTicks = 0;
        long trailTicks = 0;
        long statusTicks = 0;

        try
        {
            int stepsThisTick = Math.Min(_stepsPerUpdate,
                _session.TotalSteps - _completedSteps);

            for (int step = 0; step < stepsThisTick; step++)
            {
                long physicsStart = Stopwatch.GetTimestamp();
                _session.Simulation.Update(_session.TimeStepSeconds);
                physicsTicks += Stopwatch.GetTimestamp() - physicsStart;
                _completedSteps++;

                if (_completedSteps % AppSettings.TrailSampleEverySteps == 0 ||
                    _completedSteps == _session.TotalSteps)
                {
                    long trailStart = Stopwatch.GetTimestamp();
                    _canvas.RecordTrailSample();
                    trailTicks += Stopwatch.GetTimestamp() - trailStart;
                }

                if (_completedSteps % _session.StepsPerYear == 0)
                    _session.PrintSample();
            }

            long statusStart = Stopwatch.GetTimestamp();
            _session.ValidateFiniteState();
            UpdateStatus();
            statusTicks += Stopwatch.GetTimestamp() - statusStart;

            // Redraw only at the configured display frequency.

            if (_completedSteps >= _session.TotalSteps)
            {
                _completed = true;
                _physicsTimer.Stop();
                _displayTimer.Stop();
                _canvas.Invalidate(); // Draw the final state once.
                _pauseButton.Text = "Finished";
                _pauseButton.Enabled = false;
            }

            _performance.RecordSimulationTick(
                stepsThisTick,
                physicsTicks,
                trailTicks,
                statusTicks,
                Stopwatch.GetTimestamp() - tickStart);

            if (_performance.TryGetReport(out string report, force: _completed))
                _performanceLabel.Text = report;
        }
        catch (Exception error)
        {
            _physicsTimer.Stop();
            _displayTimer.Stop();
            _pauseButton.Text = "Stopped";
            _pauseButton.Enabled = false;
            MessageBox.Show(this, error.Message, "Simulation error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateStatus()
    {
        _statusLabel.Text = _session.FormatStatus();
    }
}
