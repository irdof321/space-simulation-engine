using System;
using System.Drawing;

// Deterministic synthetic visual markings. Not a physically simulated surface
// or a scientific texture map; each body keeps the same features on every run.
internal sealed class ProceduralBodySurface
{
    public const int TextureWidth = 512;
    public const int TextureHeight = 256;

    private readonly int[] _pixels = new int[TextureWidth * TextureHeight];
    private readonly SurfaceKind _kind;

    private enum SurfaceKind { Star, Gas, Rocky, EarthLike, Ice, Moon, Comet }

    public ProceduralBodySurface(BodyMarker marker)
    {
        _kind = Classify(marker);
        int seed = BodyRotationCatalog.StableSeed(marker.Name);
        for (int y = 0; y < TextureHeight; y++)
        {
            double v = (y + 0.5) / TextureHeight;
            double latitude = (0.5 - v) * Math.PI;
            for (int x = 0; x < TextureWidth; x++)
            {
                double u = (x + 0.5) / TextureWidth;
                // Longitude wraps: the same markings appear at both map edges.
                double continental = Fractal(u * 5, v * 5, 5, seed, 4);
                double detail = Fractal(u * 24, v * 24, 24, seed + 19, 3);
                double bands = 0.5 + 0.5 * Math.Sin(latitude * 24 + continental * 3.8);
                Color color;
                switch (_kind)
                {
                    case SurfaceKind.Star:
                        double bright = Clamp01(0.40 + 0.65 * continental + 0.25 * detail);
                        color = Mix(Color.FromArgb(184, 76, 28), Color.FromArgb(255, 221, 111), bright);
                        break;
                    case SurfaceKind.EarthLike:
                        if (Math.Abs(latitude) > 1.27 && continental > 0.28)
                            color = Mix(Color.FromArgb(186, 216, 225), Color.White, detail);
                        else if (continental > 0.53)
                            color = Mix(Color.FromArgb(31, 86, 48), Color.FromArgb(186, 167, 94),
                                Clamp01((continental - 0.53) * 2.6 + detail * 0.3));
                        else
                            color = Mix(Color.FromArgb(8, 38, 101), Color.FromArgb(30, 137, 193), detail);
                        break;
                    case SurfaceKind.Gas:
                        color = Mix(Shift(marker.Color, 0.58), Shift(marker.Color, 1.48),
                            Clamp01(0.20 + 0.58 * bands + 0.23 * detail));
                        break;
                    case SurfaceKind.Ice:
                        color = Mix(Color.FromArgb(76, 120, 158), Color.FromArgb(225, 239, 247),
                            Clamp01(0.30 + 0.50 * continental + 0.24 * detail));
                        break;
                    case SurfaceKind.Moon:
                        color = Mix(Color.FromArgb(64, 73, 84), Color.FromArgb(207, 211, 213),
                            Clamp01(0.16 + 0.55 * continental + 0.20 * detail));
                        break;
                    case SurfaceKind.Comet:
                        color = Mix(Color.FromArgb(35, 44, 49), Color.FromArgb(133, 151, 143),
                            Clamp01(0.15 + 0.65 * continental + 0.20 * detail));
                        break;
                    default:
                        color = Mix(Shift(marker.Color, 0.48), Shift(marker.Color, 1.42),
                            Clamp01(0.20 + 0.65 * continental + 0.18 * detail));
                        break;
                }
                _pixels[y * TextureWidth + x] = color.ToArgb();
            }
        }
    }

    public int Sample(double longitude, double latitude)
    {
        double u = (longitude + Math.PI) / (2.0 * Math.PI);
        u -= Math.Floor(u);
        double v = Clamp01(0.5 - latitude / Math.PI);
        int x = Math.Min(TextureWidth - 1, (int)(u * TextureWidth));
        int y = Math.Min(TextureHeight - 1, (int)(v * TextureHeight));
        return _pixels[y * TextureWidth + x];
    }

    private static SurfaceKind Classify(BodyMarker marker)
    {
        if (marker.IsStar) return SurfaceKind.Star;
        if (marker.IsComet) return SurfaceKind.Comet;
        if (marker.Name == "Earth" || marker.Name == "Nysa") return SurfaceKind.EarthLike;
        if (marker.Name == "Jupiter" || marker.Name == "Glacia") return SurfaceKind.Gas;
        if (marker.Name == "Vesper") return SurfaceKind.Ice;
        if (marker.IsSatellite) return SurfaceKind.Moon;
        return SurfaceKind.Rocky;
    }

    private static Color Shift(Color color, double factor) => Color.FromArgb(
        (int)Math.Min(255, Math.Max(0, color.R * factor)),
        (int)Math.Min(255, Math.Max(0, color.G * factor)),
        (int)Math.Min(255, Math.Max(0, color.B * factor)));

    private static Color Mix(Color a, Color b, double t)
    {
        t = Clamp01(t);
        return Color.FromArgb(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));

    // Smooth value noise with periodic longitude, preventing a texture seam.
    private static double Fractal(double x, double y, int longitudePeriod,
        int seed, int octaves)
    {
        double sum = 0;
        double weight = 0.5;
        double weightTotal = 0;
        int period = longitudePeriod;
        for (int octave = 0; octave < octaves; octave++)
        {
            sum += weight * SmoothNoise(x, y, period, seed + octave * 233);
            weightTotal += weight;
            x *= 2;
            y *= 2;
            period *= 2;
            weight *= 0.5;
        }
        return sum / weightTotal;
    }

    private static double SmoothNoise(double x, double y, int period, int seed)
    {
        int x0 = (int)Math.Floor(x);
        int y0 = (int)Math.Floor(y);
        double fx = x - x0;
        double fy = y - y0;
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);
        double a = Hash(x0, y0, period, seed);
        double b = Hash(x0 + 1, y0, period, seed);
        double c = Hash(x0, y0 + 1, period, seed);
        double d = Hash(x0 + 1, y0 + 1, period, seed);
        return (a + (b - a) * fx) * (1 - fy) +
               (c + (d - c) * fx) * fy;
    }

    private static double Hash(int x, int y, int period, int seed)
    {
        x = ((x % period) + period) % period;
        unchecked
        {
            uint h = (uint)seed ^ ((uint)x * 374761393u) ^ ((uint)y * 668265263u);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h ^ (h >> 16)) / (double)uint.MaxValue;
        }
    }
}
