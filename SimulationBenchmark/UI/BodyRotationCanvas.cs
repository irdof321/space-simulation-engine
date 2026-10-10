using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PhysicsSimulation.Mathematics;

// A separate, small 3D globe preview. This never draws in OrbitalCanvas.
// Camera direction is chosen for an equatorial view of nearly-Z spin axes.
internal sealed class BodyRotationCanvas : Control
{
    private const int TextureSize = 224;
    private readonly Bitmap _bitmap = new Bitmap(TextureSize, TextureSize,
        PixelFormat.Format32bppArgb);
    private readonly int[] _argb = new int[TextureSize * TextureSize];
    private readonly double[] _sampleX = new double[TextureSize * TextureSize];
    private readonly double[] _sampleY = new double[TextureSize * TextureSize];
    private readonly double[] _sampleZ = new double[TextureSize * TextureSize];
    private readonly double[] _light = new double[TextureSize * TextureSize];

    private static readonly Vector3Double CameraRight = new Vector3Double(1, 0, 0);
    private static readonly Vector3Double CameraUp = new Vector3Double(0, 0.31, 0.95).GetUnit();
    private static readonly Vector3Double CameraForward =
        Vector3Double.Cross(CameraRight, CameraUp).GetUnit();

    private BodyMarker? _marker;
    private ProceduralBodySurface? _surface;
    private double _time;

    public BodyRotationCanvas()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(15, 23, 37);

        // Precompute sphere geometry and lighting once, not each animation frame.
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                int i = y * TextureSize + x;
                double nx = (x + 0.5 - TextureSize / 2.0) / (TextureSize / 2.0);
                double ny = (TextureSize / 2.0 - y - 0.5) / (TextureSize / 2.0);
                double r2 = nx * nx + ny * ny;
                _sampleZ[i] = r2 < 1 ? Math.Sqrt(1 - r2) : -1;
                _sampleX[i] = nx;
                _sampleY[i] = ny;
                _light[i] = Math.Max(0, -0.47 * nx + 0.37 * ny + 0.80 * _sampleZ[i]);
            }
        }
    }

    public void SetBody(BodyMarker marker)
    {
        _marker = marker;
        _surface = new ProceduralBodySurface(marker);
        Invalidate();
    }

    public void SetTime(double time)
    {
        _time = time;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (_marker == null || _surface == null)
            return;

        DrawSphereTexture(_marker, _surface, _time);

        int size = Math.Min(Width - 44, Height - 80);
        size = Math.Max(10, Math.Min(size, 350));
        int left = (Width - size) / 2;
        int top = Math.Max(21, (Height - size) / 2 - 5);
        var bounds = new Rectangle(left, top, size, size);

        // Back half of the rotation axis, then globe, then front half.
        DrawAxis(e.Graphics, bounds, _marker.Spin.Axis, behind: true);
        using (var halo = new SolidBrush(Color.FromArgb(35, _marker.Color)))
            e.Graphics.FillEllipse(halo, bounds.X - 9, bounds.Y - 9,
                bounds.Width + 18, bounds.Height + 18);
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
        e.Graphics.DrawImage(_bitmap, bounds);
        using (var rim = new Pen(Color.FromArgb(105, 207, 224, 245), 1f))
            e.Graphics.DrawEllipse(rim, bounds);
        DrawAxis(e.Graphics, bounds, _marker.Spin.Axis, behind: false);

        using var caption = new Font("Segoe UI", 9.5f);
        using var captionBrush = new SolidBrush(Color.FromArgb(169, 188, 210));
        string text = $"{_marker.Name}  •  synthetic surface";
        SizeF textSize = e.Graphics.MeasureString(text, caption);
        e.Graphics.DrawString(text, caption, captionBrush,
            (Width - textSize.Width) / 2, Math.Min(Height - 26, bounds.Bottom + 17));
    }

    private static void DrawAxis(Graphics g, Rectangle bounds, Vector3Double axis, bool behind)
    {
        // Plane coordinates for the camera basis; forward is toward the viewer.
        double x = Dot(axis, CameraRight);
        double y = Dot(axis, CameraUp);
        double z = Dot(axis, CameraForward);
        float cx = bounds.Left + bounds.Width / 2f;
        float cy = bounds.Top + bounds.Height / 2f;
        float radius = bounds.Width / 2f;
        // Sign determines which pole is in front of the planet.
        int sign = behind ? (z > 0 ? -1 : 1) : (z > 0 ? 1 : -1);
        float ex = cx + sign * (float)(x * radius * 1.23);
        float ey = cy - sign * (float)(y * radius * 1.23);
        using var pen = new Pen(Color.FromArgb(behind ? 95 : 230, 177, 220, 251),
            behind ? 1.3f : 2.0f);
        if (behind) pen.DashStyle = DashStyle.Dash;
        g.DrawLine(pen, cx, cy, ex, ey);
        if (!behind)
        {
            using var brush = new SolidBrush(Color.FromArgb(228, 204, 236, 255));
            g.FillEllipse(brush, ex - 3, ey - 3, 6, 6);
        }
    }

    private void DrawSphereTexture(BodyMarker marker, ProceduralBodySurface surface, double time)
    {
        QuaternionDouble inverse = marker.Spin.GetOrientation(time).Conjugate();
        Vector3Double right = inverse.RotateVector(CameraRight);
        Vector3Double up = inverse.RotateVector(CameraUp);
        Vector3Double forward = inverse.RotateVector(CameraForward);
        Vector3Double north = marker.Spin.Axis;
        Vector3Double reference = Math.Abs(north.Z) > 0.9
            ? new Vector3Double(0, 1, 0)
            : new Vector3Double(0, 0, 1);
        Vector3Double east = Vector3Double.Cross(reference, north).GetUnit();
        Vector3Double tangent = Vector3Double.Cross(north, east);

        double ex = Dot(right, east), ey = Dot(up, east), ez = Dot(forward, east);
        double tx = Dot(right, tangent), ty = Dot(up, tangent), tz = Dot(forward, tangent);
        double nx = Dot(right, north), ny = Dot(up, north), nz = Dot(forward, north);

        for (int i = 0; i < _argb.Length; i++)
        {
            double z = _sampleZ[i];
            if (z < 0)
            {
                _argb[i] = 0;
                continue;
            }
            double x = _sampleX[i], y = _sampleY[i];
            double longitude = Math.Atan2(tx * x + ty * y + tz * z,
                                          ex * x + ey * y + ez * z);
            double latitude = Math.Asin(Math.Max(-1, Math.Min(1,
                nx * x + ny * y + nz * z)));
            int pixel = surface.Sample(longitude, latitude);
            // Diffuse illumination, with enough ambient light for the night side
            // to keep its procedural markings visible.
            double lighting = Math.Min(1, 0.27 + 0.74 * _light[i]);
            int red = (int)(((pixel >> 16) & 255) * lighting);
            int green = (int)(((pixel >> 8) & 255) * lighting);
            int blue = (int)((pixel & 255) * lighting);
            // Soft edge for a non-jagged planetary silhouette.
            double edge = (1 - Math.Sqrt(x * x + y * y)) * TextureSize / 2.0;
            int alpha = (int)(255 * Math.Max(0, Math.Min(1, edge)));
            _argb[i] = (alpha << 24) | (red << 16) | (green << 8) | blue;
        }

        var imageRect = new Rectangle(0, 0, TextureSize, TextureSize);
        BitmapData pixels = _bitmap.LockBits(imageRect, ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            Marshal.Copy(_argb, 0, pixels.Scan0, _argb.Length);
        }
        finally
        {
            _bitmap.UnlockBits(pixels);
        }
    }

    private static double Dot(Vector3Double a, Vector3Double b) =>
        a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    protected override void Dispose(bool disposing)
    {
        if (disposing) _bitmap.Dispose();
        base.Dispose(disposing);
    }
}
