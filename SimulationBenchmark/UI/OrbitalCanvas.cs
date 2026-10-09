using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;
using PhysicsSimulation;

// Generic XY renderer. All bodies and views come from SolarSystemPreset.
internal sealed class OrbitalCanvas : Control
{
    private sealed class ViewState
    {
        public OrbitView View { get; }
        public BodyMarker Center { get; }
        public List<BodyMarker> VisibleBodies { get; } = new();
        public Dictionary<BodyMarker, List<PointF>> Trails { get; } = new();
        public Dictionary<BodyMarker, double> ReferenceRadii { get; } = new();

        public ViewState(OrbitView view, OrbitalScene scene)
        {
            View = view;
            Center = scene.GetMarker(view.CenterBodyName);

            foreach (BodyMarker marker in scene.Markers)
            {
                if (view.StarsOnly && !marker.IsStar)
                    continue;

                if (view.VisibleBodyNames != null && !view.VisibleBodyNames.Contains(marker.Name))
                    continue;

                if (marker.IsSatellite && !view.ShowSatellites && !ReferenceEquals(marker, Center))
                    continue;

                // Keep the plotted bodies fixed during animation; orbital crossings
                // are not allowed to change the set of labels/trails mid-flight.
                double radius = (marker.Body.Position - Center.Body.Position).GetMagnitude();
                if (radius > view.HalfRangeMeters * 1.20 && !ReferenceEquals(marker, Center))
                    continue;

                VisibleBodies.Add(marker);
                ReferenceRadii[marker] = radius;
                Trails[marker] = new List<PointF>();
            }
        }
    }

    private readonly List<ViewState> _views = new();

    // The paint duration is recorded after drawing finishes, on the UI thread.
    public event Action<long>? PaintMeasured;

    public OrbitalCanvas(OrbitalScene scene)
    {
        foreach (OrbitView view in scene.Views)
            _views.Add(new ViewState(view, scene));

        if (_views.Count == 0)
            throw new InvalidOperationException("Configure at least one orbital view.");

        if (AppSettings.TrailSampleEverySteps <= 0 || AppSettings.MaxTrailSamples < 2)
            throw new InvalidOperationException("Trail sample interval and limit must be positive.");

        DoubleBuffered = true;
        BackColor = Color.FromArgb(12, 19, 31);
        ResizeRedraw = true;
    }

    public void RecordTrailSample()
    {
        foreach (ViewState view in _views)
        {
            foreach (BodyMarker marker in view.VisibleBodies)
            {
                if (ReferenceEquals(marker, view.Center))
                    continue;

                Vector3Double relative = marker.Body.Position - view.Center.Body.Position;
                var point = new PointF(
                    (float)(relative.X / view.View.HalfRangeMeters),
                    (float)(relative.Y / view.View.HalfRangeMeters));

                List<PointF> trail = view.Trails[marker];
                trail.Add(point);
                if (trail.Count > AppSettings.MaxTrailSamples)
                    trail.RemoveAt(0);
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        long paintStart = Stopwatch.GetTimestamp();
        try
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(BackColor);

            const int margin = 16;
            const int gap = 13;
            int columns = Math.Min(3, _views.Count);
            int rows = (_views.Count + columns - 1) / columns;
            int panelWidth = Math.Max(100, (Width - 2 * margin - gap * (columns - 1)) / columns);
            int panelHeight = Math.Max(120, (Height - 2 * margin - gap * (rows - 1)) / rows);

            for (int index = 0; index < _views.Count; index++)
            {
                int column = index % columns;
                int row = index / columns;
                var rectangle = new Rectangle(
                    margin + column * (panelWidth + gap),
                    margin + row * (panelHeight + gap),
                    panelWidth,
                    panelHeight);
                DrawView(e.Graphics, rectangle, _views[index]);
            }
        }
        finally
        {
            PaintMeasured?.Invoke(Stopwatch.GetTimestamp() - paintStart);
        }
    }

    private static Rectangle DrawPanelFrame(Graphics graphics, Rectangle panel, string title, string subtitle)
    {
        using var background = new SolidBrush(Color.FromArgb(21, 31, 49));
        using var outline = new Pen(Color.FromArgb(54, 71, 94));
        using var titleFont = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var captionFont = new Font("Segoe UI", 9f);
        using var titleBrush = new SolidBrush(Color.FromArgb(232, 239, 247));
        using var subtitleBrush = new SolidBrush(Color.FromArgb(155, 176, 201));

        graphics.FillRectangle(background, panel);
        graphics.DrawRectangle(outline, panel);
        graphics.DrawString(title, titleFont, titleBrush, panel.X + 16, panel.Y + 12);
        graphics.DrawString(subtitle, captionFont, subtitleBrush, panel.X + 16, panel.Y + 37);

        return new Rectangle(panel.X + 18, panel.Y + 76,
            Math.Max(20, panel.Width - 36), Math.Max(20, panel.Height - 102));
    }

    private static PointF Map(PointF normalized, Rectangle view)
    {
        float pixelsPerUnit = Math.Min(view.Width, view.Height) / 2f;
        return new PointF(
            view.Left + view.Width / 2f + normalized.X * pixelsPerUnit,
            view.Top + view.Height / 2f - normalized.Y * pixelsPerUnit);
    }

    private static void DrawAxes(Graphics graphics, Rectangle view)
    {
        using var pen = new Pen(Color.FromArgb(47, 86, 107, 133), 1f);
        float centerX = view.Left + view.Width / 2f;
        float centerY = view.Top + view.Height / 2f;
        graphics.DrawLine(pen, view.Left, centerY, view.Right, centerY);
        graphics.DrawLine(pen, centerX, view.Top, centerX, view.Bottom);
    }

    private static void DrawReferenceCircle(
        Graphics graphics, Rectangle view, double relativeRadius, Color color)
    {
        if (!(relativeRadius > 0 && relativeRadius <= 1.0))
            return;

        PointF center = Map(new PointF(0, 0), view);
        float pixelRadius = (float)(Math.Min(view.Width, view.Height) * relativeRadius / 2.0);
        using var pen = new Pen(Color.FromArgb(52, color), 1f)
        {
            DashStyle = DashStyle.Dot
        };
        graphics.DrawEllipse(pen, center.X - pixelRadius, center.Y - pixelRadius,
            2f * pixelRadius, 2f * pixelRadius);
    }

    private static void DrawTrail(Graphics graphics, List<PointF> trail, Rectangle view, Color color, bool isComet)
    {
        if (trail.Count < 2)
            return;

        var points = new PointF[trail.Count];
        for (int i = 0; i < trail.Count; i++)
            points[i] = Map(trail[i], view);

        using var pen = new Pen(Color.FromArgb(isComet ? 220 : 165, color), isComet ? 2.2f : 1.7f);
        graphics.DrawLines(pen, points);
    }

    private static void DrawBody(Graphics graphics, PointF coordinate, Rectangle view, BodyMarker marker)
    {
        PointF center = Map(coordinate, view);
        if (!view.Contains(Point.Round(center)))
            return;

        Color color = marker.Color;
        float dotRadius = marker.RadiusPixels;
        using var glow = new SolidBrush(Color.FromArgb(45, color));
        using var fill = new SolidBrush(color);
        using var labelBrush = new SolidBrush(Color.FromArgb(223, 232, 243));
        using var labelFont = new Font("Segoe UI", 8.5f);

        graphics.FillEllipse(glow,
            center.X - dotRadius - 4, center.Y - dotRadius - 4,
            2 * (dotRadius + 4), 2 * (dotRadius + 4));
        graphics.FillEllipse(fill,
            center.X - dotRadius, center.Y - dotRadius,
            2 * dotRadius, 2 * dotRadius);
        graphics.DrawString(marker.Name, labelFont, labelBrush,
            center.X + dotRadius + 5, center.Y - 7);
    }

    private void DrawView(Graphics graphics, Rectangle panel, ViewState state)
    {
        OrbitView definition = state.View;
        string range = definition.HalfRangeMeters >= 0.1 * AppSettings.LightYear
            ? $"{definition.HalfRangeMeters / AppSettings.LightYear:0.00} ly"
            : definition.HalfRangeMeters >= 0.1 * AppSettings.AstronomicalUnit
                ? $"{definition.HalfRangeMeters / AppSettings.AstronomicalUnit:0.0} AU"
                : $"{definition.HalfRangeMeters / 1000.0:N0} km";

        Rectangle view = DrawPanelFrame(graphics, panel, definition.Title,
            $"XY projection • {definition.CenterBodyName} centered • ±{range}");

        GraphicsState clip = graphics.Save();
        graphics.SetClip(view);

        DrawAxes(graphics, view);
        foreach (BodyMarker marker in state.VisibleBodies)
        {
            if (ReferenceEquals(marker, state.Center))
                continue;
            if (!marker.IsComet) // A circular guide would misrepresent an eccentric comet orbit.
            {
                double radius = state.ReferenceRadii[marker] / definition.HalfRangeMeters;
                DrawReferenceCircle(graphics, view, radius, marker.Color);
            }
        }

        foreach (BodyMarker marker in state.VisibleBodies)
        {
            if (!ReferenceEquals(marker, state.Center))
                DrawTrail(graphics, state.Trails[marker], view, marker.Color, marker.IsComet);
        }

        foreach (BodyMarker marker in state.VisibleBodies)
        {
            Vector3Double offset = marker.Body.Position - state.Center.Body.Position;
            var normalized = new PointF(
                (float)(offset.X / definition.HalfRangeMeters),
                (float)(offset.Y / definition.HalfRangeMeters));
            DrawBody(graphics, normalized, view, marker);
        }

        graphics.Restore(clip);
    }
}
