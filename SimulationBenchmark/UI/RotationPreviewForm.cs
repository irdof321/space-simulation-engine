using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

// Optional modeless preview: the existing orbit window and its renderer
// remain unchanged. The preview has its own clock for visible spin.
internal sealed class RotationPreviewForm : Form
{
    private readonly LiveSimulationSession _session;
    private readonly ComboBox _bodySelector;
    private readonly ComboBox _durationSelector;
    private readonly CheckBox _physicalTimeCheck;
    private readonly Label _infoLabel;
    private readonly BodyRotationCanvas _canvas;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly List<BodyMarker> _bodies = new();

    private double _previewTime;
    private long _previousTick;
    private static readonly double[] PreviewSecondsPerTurn = { 3, 8, 16, 32 };

    public RotationPreviewForm(LiveSimulationSession session)
    {
        _session = session;
        Text = "Space Simulation - Axial rotation preview";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(470, 480);
        MinimumSize = new Size(486, 400);
        BackColor = Color.FromArgb(15, 23, 37);
        ForeColor = Color.FromArgb(226, 237, 249);
        Font = new Font("Segoe UI", 10f);

        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 117,
            Padding = new Padding(16, 10, 16, 8),
            BackColor = Color.FromArgb(20, 30, 47)
        };
        var bodyLabel = new Label
        {
            Text = "Body:", AutoSize = true, Location = new Point(15, 17)
        };
        _bodySelector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(70, 11), Size = new Size(153, 30)
        };
        foreach (BodyMarker marker in session.Scene.Markers)
        {
            _bodies.Add(marker);
            _bodySelector.Items.Add(marker.Name);
        }

        var durationLabel = new Label
        {
            Text = "Preview:", AutoSize = true, Location = new Point(238, 17)
        };
        _durationSelector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(310, 11), Size = new Size(118, 30)
        };
        foreach (double seconds in PreviewSecondsPerTurn)
            _durationSelector.Items.Add($"{seconds:0} s / turn");
        _durationSelector.SelectedIndex = 1;

        _physicalTimeCheck = new CheckBox
        {
            Text = "Follow simulation time (may appear stationary at large time steps)",
            AutoSize = true, Location = new Point(17, 49),
            ForeColor = Color.FromArgb(212, 223, 238)
        };
        _infoLabel = new Label
        {
            AutoEllipsis = true,
            Text = "", Location = new Point(17, 80),
            Size = new Size(425, 27),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            ForeColor = Color.FromArgb(157, 184, 209),
            Font = new Font("Segoe UI", 9f)
        };

        toolbar.Controls.Add(bodyLabel);
        toolbar.Controls.Add(_bodySelector);
        toolbar.Controls.Add(durationLabel);
        toolbar.Controls.Add(_durationSelector);
        toolbar.Controls.Add(_physicalTimeCheck);
        toolbar.Controls.Add(_infoLabel);

        _canvas = new BodyRotationCanvas { Dock = DockStyle.Fill };
        Controls.Add(_canvas);
        Controls.Add(toolbar);

        _bodySelector.SelectedIndexChanged += (_, _) => SelectBody();
        _physicalTimeCheck.CheckedChanged += (_, _) => UpdateInfo();
        int initialIndex = _bodies.FindIndex(b => b.Name == "Earth");
        _bodySelector.SelectedIndex = initialIndex >= 0 ? initialIndex : 0;

        _timer = new System.Windows.Forms.Timer { Interval = 50 }; // 20 fps, separate from orbital FPS
        _timer.Tick += (_, _) => TickPreview();
        Shown += (_, _) =>
        {
            _previousTick = Stopwatch.GetTimestamp();
            _timer.Start();
        };
        FormClosed += (_, _) => _timer.Stop();
    }

    private BodyMarker SelectedBody => _bodies[_bodySelector.SelectedIndex];

    private void SelectBody()
    {
        _previewTime = 0;
        _canvas.SetBody(SelectedBody);
        UpdateInfo();
    }

    private void UpdateInfo()
    {
        if (_bodySelector.SelectedIndex < 0) return;
        BodyMarker marker = SelectedBody;
        double hours = marker.Spin.Period / 3600.0;
        string period = hours >= 48
            ? $"{hours / 24.0:0.##} days"
            : $"{hours:0.##} hours";
        _infoLabel.Text = _physicalTimeCheck.Checked
            ? $"Spin period: {period}  |  Live simulation time"
            : $"Spin period: {period}  |  Visual preview: accelerated";
    }

    private void TickPreview()
    {
        if (_bodySelector.SelectedIndex < 0)
            return;
        long now = Stopwatch.GetTimestamp();
        double seconds = Math.Max(0, Math.Min(0.25,
            (now - _previousTick) / (double)Stopwatch.Frequency));
        _previousTick = now;

        BodyMarker marker = SelectedBody;
        double clock;
        if (_physicalTimeCheck.Checked)
        {
            clock = _session.Simulation.Time;
        }
        else
        {
            double secondsPerTurn = PreviewSecondsPerTurn[_durationSelector.SelectedIndex];
            _previewTime = (_previewTime + seconds * marker.Spin.Period / secondsPerTurn)
                % marker.Spin.Period;
            clock = _previewTime;
        }
        _canvas.SetTime(clock);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
